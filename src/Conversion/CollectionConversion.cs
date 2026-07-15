// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Pocok.Primitives;

namespace Pocok.Conversion;

internal static class CollectionConversion
{
    internal static bool IsPairOrCollectionTarget(Type targetType) =>
        targetType == typeof(DictionaryEntry) ||
        IsKeyValuePair(targetType) ||
        targetType.IsArray ||
        TryGetDictionaryTypes(targetType, out _, out _) ||
        TryGetEnumerableElementType(targetType, out _);

    internal static Result<object?> Convert(
        object value,
        Type targetType,
        ConversionContext context,
        Func<object?, Type, ConversionContext, Result<object?>> convert)
    {
        if (targetType == typeof(DictionaryEntry))
        {
            if (!TryReadPair(value, out var key, out var pairValue))
            {
                return ConversionFailures.Unsupported(value.GetType(), targetType);
            }

            return key is null
                ? ConversionFailures.Collection("A dictionary entry key cannot be null.")
                : Result<object?>.Success(new DictionaryEntry(key, pairValue));
        }

        if (IsKeyValuePair(targetType))
        {
            return ConvertPair(value, targetType, context, convert);
        }

        if (TryGetDictionaryTypes(targetType, out var keyType, out var valueType))
        {
            return ConvertDictionary(value, targetType, keyType, valueType, context, convert);
        }

        return ConvertSequence(value, targetType, context, convert);
    }

    private static Result<object?> ConvertPair(
        object value,
        Type targetType,
        ConversionContext context,
        Func<object?, Type, ConversionContext, Result<object?>> convert)
    {
        if (!TryReadPair(value, out var key, out var pairValue))
        {
            return ConversionFailures.Unsupported(value.GetType(), targetType);
        }

        var genericArguments = targetType.GetGenericArguments();
        var keyResult = convert(key, genericArguments[0], context);
        if (keyResult.IsFailure)
        {
            return keyResult;
        }

        var valueResult = convert(pairValue, genericArguments[1], context);
        if (valueResult.IsFailure)
        {
            return valueResult;
        }

        return Result<object?>.Success(Activator.CreateInstance(targetType, keyResult.Value, valueResult.Value));
    }

    private static Result<object?> ConvertDictionary(
        object value,
        Type targetType,
        Type keyType,
        Type valueType,
        ConversionContext context,
        Func<object?, Type, ConversionContext, Result<object?>> convert)
    {
        if (!TypeShape.IsEnumerableSource(value))
        {
            return ConversionFailures.Unsupported(value.GetType(), targetType);
        }

        var instanceResult = CreateDictionaryInstance(value.GetType(), targetType, keyType, valueType);
        if (instanceResult.IsFailure)
        {
            return instanceResult;
        }

        var instance = instanceResult.Value!;
        var mutableDictionaryInterface = typeof(IDictionary<,>).MakeGenericType(keyType, valueType);
        var addMethod = mutableDictionaryInterface.GetMethod(nameof(IDictionary<int, int>.Add), [keyType, valueType]);
        if (addMethod is null)
        {
            return ConversionFailures.Collection("The target dictionary does not expose the required add operation.");
        }

        foreach (var item in (IEnumerable)value)
        {
            if (item is null || !TryReadPair(item, out var key, out var pairValue))
            {
                return ConversionFailures.Collection("A dictionary source item is not a key/value pair.");
            }

            var keyResult = convert(key, keyType, context);
            if (keyResult.IsFailure)
            {
                return keyResult;
            }

            if (keyResult.Value is null)
            {
                return ConversionFailures.Collection("A converted dictionary key cannot be null.");
            }

            var valueResult = convert(pairValue, valueType, context);
            if (valueResult.IsFailure)
            {
                return valueResult;
            }

            try
            {
                addMethod.Invoke(instance, [keyResult.Value, valueResult.Value]);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is OperationCanceledException)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
            catch (TargetInvocationException exception) when (exception.InnerException is ArgumentException)
            {
                return ConversionFailures.Collection(
                    "A converted dictionary item could not be added to the target.",
                    exception.InnerException);
            }
        }

        return Result<object?>.Success(instance);
    }

    private static Result<object?> ConvertSequence(
        object value,
        Type targetType,
        ConversionContext context,
        Func<object?, Type, ConversionContext, Result<object?>> convert)
    {
        if (!TypeShape.IsEnumerableSource(value) || !TryGetEnumerableElementType(targetType, out var elementType))
        {
            return ConversionFailures.Unsupported(value.GetType(), targetType);
        }

        if (targetType.IsArray && !targetType.IsSZArray)
        {
            return ConversionFailures.Unsupported(value.GetType(), targetType);
        }

        List<object?> convertedItems = [];
        foreach (var item in (IEnumerable)value)
        {
            var itemResult = convert(item, elementType, context);
            if (itemResult.IsFailure)
            {
                return itemResult;
            }

            convertedItems.Add(itemResult.Value);
        }

        if (targetType.IsArray)
        {
            var array = Array.CreateInstance(elementType, convertedItems.Count);
            for (var index = 0; index < convertedItems.Count; index++)
            {
                array.SetValue(convertedItems[index], index);
            }

            return Result<object?>.Success(array);
        }

        var instanceResult = CreateCollectionInstance(value.GetType(), targetType, elementType);
        if (instanceResult.IsFailure)
        {
            return instanceResult;
        }

        var instance = instanceResult.Value!;
        var collectionInterface = typeof(ICollection<>).MakeGenericType(elementType);
        var addMethod = instance.GetType().GetMethod(nameof(ICollection<int>.Add), [elementType]);
        if (addMethod is null && collectionInterface.IsInstanceOfType(instance))
        {
            addMethod = collectionInterface.GetMethod(nameof(ICollection<int>.Add), [elementType]);
        }

        if (addMethod is null)
        {
            return ConversionFailures.Collection("The target collection does not expose the required add operation.");
        }

        try
        {
            foreach (var item in convertedItems)
            {
                addMethod.Invoke(instance, [item]);
            }
        }
        catch (TargetInvocationException exception) when (exception.InnerException is OperationCanceledException)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is ArgumentException)
        {
            return ConversionFailures.Collection(
                "A converted item could not be added to the target collection.",
                exception.InnerException);
        }

        return Result<object?>.Success(instance);
    }

    private static Result<object?> CreateDictionaryInstance(
        Type sourceType,
        Type targetType,
        Type keyType,
        Type valueType)
    {
        var implementationType = targetType.IsInterface || targetType.IsAbstract
            ? typeof(Dictionary<,>).MakeGenericType(keyType, valueType)
            : targetType;

        if (!targetType.IsAssignableFrom(implementationType))
        {
            return ConversionFailures.Unsupported(sourceType, targetType);
        }

        return CreateInstance(implementationType, "dictionary");
    }

    private static Result<object?> CreateCollectionInstance(Type sourceType, Type targetType, Type elementType)
    {
        Type implementationType;
        if (!targetType.IsInterface && !targetType.IsAbstract)
        {
            implementationType = targetType;
        }
        else if (IsSetType(targetType))
        {
            implementationType = typeof(HashSet<>).MakeGenericType(elementType);
        }
        else
        {
            implementationType = typeof(List<>).MakeGenericType(elementType);
        }

        if (!targetType.IsAssignableFrom(implementationType))
        {
            return ConversionFailures.Unsupported(sourceType, targetType);
        }

        return CreateInstance(implementationType, "collection");
    }

    private static Result<object?> CreateInstance(Type implementationType, string kind)
    {
        try
        {
            var instance = Activator.CreateInstance(implementationType);
            return instance is null
                ? ConversionFailures.Collection($"The target {kind} could not be created.")
                : Result<object?>.Success(instance);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is OperationCanceledException)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
        catch (Exception exception) when (exception is MissingMethodException or MemberAccessException or
                                           TargetInvocationException)
        {
            return ConversionFailures.Collection($"The target {kind} could not be created.", exception);
        }
    }

    private static bool TryReadPair(object value, out object? key, out object? pairValue)
    {
        if (value is DictionaryEntry dictionaryEntry)
        {
            key = dictionaryEntry.Key;
            pairValue = dictionaryEntry.Value;
            return true;
        }

        var valueType = value.GetType();
        if (!IsKeyValuePair(valueType))
        {
            key = null;
            pairValue = null;
            return false;
        }

        key = valueType.GetProperty(nameof(KeyValuePair<int, int>.Key))!.GetValue(value);
        pairValue = valueType.GetProperty(nameof(KeyValuePair<int, int>.Value))!.GetValue(value);
        return true;
    }

    private static bool IsKeyValuePair(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);

    private static bool TryGetEnumerableElementType(Type type, out Type elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        if (TryGetGenericInterface(type, typeof(IEnumerable<>), out var enumerableInterface))
        {
            elementType = enumerableInterface.GetGenericArguments()[0];
            return true;
        }

        elementType = typeof(object);
        return false;
    }

    private static bool TryGetDictionaryTypes(Type type, out Type keyType, out Type valueType)
    {
        if (!TryGetGenericInterface(type, typeof(IDictionary<,>), out var dictionaryInterface) &&
            !TryGetGenericInterface(type, typeof(IReadOnlyDictionary<,>), out dictionaryInterface))
        {
            keyType = typeof(object);
            valueType = typeof(object);
            return false;
        }

        var genericArguments = dictionaryInterface.GetGenericArguments();
        keyType = genericArguments[0];
        valueType = genericArguments[1];
        return true;
    }

    private static bool TryGetGenericInterface(Type type, Type genericDefinition, out Type interfaceType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == genericDefinition)
        {
            interfaceType = type;
            return true;
        }

        interfaceType = type.GetInterfaces()
            .FirstOrDefault(candidate => candidate.IsGenericType &&
                                         candidate.GetGenericTypeDefinition() == genericDefinition)!;
        return interfaceType is not null;
    }

    private static bool IsSetType(Type type) =>
        type.IsGenericType &&
        (type.GetGenericTypeDefinition() == typeof(ISet<>) ||
         type.GetGenericTypeDefinition() == typeof(IReadOnlySet<>));
}
