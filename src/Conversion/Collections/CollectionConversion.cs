// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Pocok.Conversion.Internal;

namespace Pocok.Conversion.Collections;

[RequiresUnreferencedCode(ConversionTrimming.IncompatibleMessage)]
internal static class CollectionConversion
{
    internal static bool IsPairOrCollectionTarget(Type targetType)
    {
        return targetType == typeof(DictionaryEntry) ||
               IsKeyValuePair(targetType) ||
               targetType.IsArray ||
               TryGetDictionaryTypes(targetType, out _, out _) ||
               TryGetEnumerableElementType(targetType, out _);
    }

    internal static ConversionResult<object?> Convert(
        object value,
        Type targetType,
        ConversionSession session,
        string path,
        int depth)
    {
        if (targetType == typeof(DictionaryEntry))
        {
            if (!TryReadPair(value, out var key, out var pairValue))
                return ConversionFailures.Unsupported(value.GetType(), targetType, path);

            return key is null
                ? ConversionFailures.Collection("A dictionary entry key cannot be null.", path: path + ".key")
                : ConversionResult<object?>.Success(new DictionaryEntry(key, pairValue));
        }

        if (IsKeyValuePair(targetType)) return ConvertPair(value, targetType, session, path, depth);

        if (TryGetDictionaryTypes(targetType, out Type keyType, out Type valueType))
            return ConvertDictionary(value, targetType, keyType, valueType, session, path, depth);

        return ConvertSequence(value, targetType, session, path, depth);
    }

    private static ConversionResult<object?> ConvertPair(
        object value,
        Type targetType,
        ConversionSession session,
        string path,
        int depth)
    {
        if (!TryReadPair(value, out var key, out var pairValue))
            return ConversionFailures.Unsupported(value.GetType(), targetType, path);

        Type[] genericArguments = targetType.GetGenericArguments();
        ConversionResult<object?> keyResult = session.ConvertNested(key, genericArguments[0], path + ".key", depth);
        if (keyResult.IsFailure) return keyResult;

        ConversionResult<object?> valueResult =
            session.ConvertNested(pairValue, genericArguments[1], path + ".value", depth);
        if (valueResult.IsFailure) return valueResult;

        return ConversionResult<object?>.Success(Activator.CreateInstance(targetType, keyResult.Value,
            valueResult.Value));
    }

    private static ConversionResult<object?> ConvertDictionary(
        object value,
        Type targetType,
        Type keyType,
        Type valueType,
        ConversionSession session,
        string path,
        int depth)
    {
        if (!TypeShape.IsEnumerableSource(value))
            return ConversionFailures.Unsupported(value.GetType(), targetType, path);

        ConversionResult<object?> instanceResult =
            CreateDictionaryInstance(value.GetType(), targetType, keyType, valueType, path);
        if (instanceResult.IsFailure) return instanceResult;

        var instance = instanceResult.Value!;
        Type mutableDictionaryInterface = typeof(IDictionary<,>).MakeGenericType(keyType, valueType);
        MethodInfo? addMethod =
            mutableDictionaryInterface.GetMethod(nameof(IDictionary<int, int>.Add), [keyType, valueType]);
        MethodInfo? containsKeyMethod =
            mutableDictionaryInterface.GetMethod(nameof(IDictionary<int, int>.ContainsKey), [keyType]);
        if (addMethod is null || containsKeyMethod is null)
            return ConversionFailures.Collection("The target dictionary does not expose the required operations.",
                path: path);

        var index = 0;
        foreach (var item in (IEnumerable)value)
        {
            var itemPath = $"{path}[{index}]";
            ConversionResult<object?> budgetResult = session.ConsumeItem(itemPath);
            if (budgetResult.IsFailure) return budgetResult;

            if (item is null || !TryReadPair(item, out var key, out var pairValue))
                return ConversionFailures.Collection("A dictionary source item is not a key/value pair.",
                    path: itemPath);

            ConversionResult<object?> keyResult = session.ConvertNested(key, keyType, itemPath + ".key", depth);
            if (keyResult.IsFailure) return keyResult;

            if (keyResult.Value is null)
                return ConversionFailures.Collection("A converted dictionary key cannot be null.",
                    path: itemPath + ".key");

            ConversionResult<object?> valueResult =
                session.ConvertNested(pairValue, valueType, itemPath + ".value", depth);
            if (valueResult.IsFailure) return valueResult;

            try
            {
                if ((bool)containsKeyMethod.Invoke(instance, [keyResult.Value])!)
                    return ConversionFailures.DuplicateKey(itemPath + ".key");

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
                    exception.InnerException,
                    itemPath);
            }

            index++;
        }

        return ConversionResult<object?>.Success(instance);
    }

    private static ConversionResult<object?> ConvertSequence(
        object value,
        Type targetType,
        ConversionSession session,
        string path,
        int depth)
    {
        if (!TypeShape.IsEnumerableSource(value) || !TryGetEnumerableElementType(targetType, out Type elementType))
            return ConversionFailures.Unsupported(value.GetType(), targetType, path);

        if (targetType is { IsArray: true, IsSZArray: false })
            return ConversionFailures.Unsupported(value.GetType(), targetType, path);

        List<object?> convertedItems = [];
        var index = 0;
        foreach (var item in (IEnumerable)value)
        {
            var itemPath = $"{path}[{index}]";
            ConversionResult<object?> budgetResult = session.ConsumeItem(itemPath);
            if (budgetResult.IsFailure) return budgetResult;

            ConversionResult<object?> itemResult = session.ConvertNested(item, elementType, itemPath, depth);
            if (itemResult.IsFailure) return itemResult;

            convertedItems.Add(itemResult.Value);
            index++;
        }

        if (targetType.IsArray)
        {
            var array = Array.CreateInstance(elementType, convertedItems.Count);
            for (var itemIndex = 0; itemIndex < convertedItems.Count; itemIndex++)
                array.SetValue(convertedItems[itemIndex], itemIndex);

            return ConversionResult<object?>.Success(array);
        }

        ConversionResult<object?> instanceResult =
            CreateCollectionInstance(value.GetType(), targetType, elementType, path);
        if (instanceResult.IsFailure) return instanceResult;

        var instance = instanceResult.Value!;
        Type collectionInterface = typeof(ICollection<>).MakeGenericType(elementType);
        MethodInfo? addMethod = instance.GetType().GetMethod(nameof(ICollection<int>.Add), [elementType]);
        if (addMethod is null && collectionInterface.IsInstanceOfType(instance))
            addMethod = collectionInterface.GetMethod(nameof(ICollection<int>.Add), [elementType]);

        if (addMethod is null)
            return ConversionFailures.Collection("The target collection does not expose the required add operation.",
                path: path);

        try
        {
            foreach (var item in convertedItems) addMethod.Invoke(instance, [item]);
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
                exception.InnerException,
                path);
        }

        return ConversionResult<object?>.Success(instance);
    }

    private static ConversionResult<object?> CreateDictionaryInstance(
        Type sourceType,
        Type targetType,
        Type keyType,
        Type valueType,
        string path)
    {
        Type implementationType = targetType.IsInterface || targetType.IsAbstract
            ? typeof(Dictionary<,>).MakeGenericType(keyType, valueType)
            : targetType;

        if (!targetType.IsAssignableFrom(implementationType))
            return ConversionFailures.Unsupported(sourceType, targetType, path);

        return CreateInstance(implementationType, "dictionary", path);
    }

    private static ConversionResult<object?> CreateCollectionInstance(
        Type sourceType,
        Type targetType,
        Type elementType,
        string path)
    {
        Type implementationType;
        if (targetType is { IsInterface: false, IsAbstract: false })
            implementationType = targetType;
        else if (IsSetType(targetType))
            implementationType = typeof(HashSet<>).MakeGenericType(elementType);
        else
            implementationType = typeof(List<>).MakeGenericType(elementType);

        if (!targetType.IsAssignableFrom(implementationType))
            return ConversionFailures.Unsupported(sourceType, targetType, path);

        return CreateInstance(implementationType, "collection", path);
    }

    private static ConversionResult<object?> CreateInstance(Type implementationType, string kind, string path)
    {
        try
        {
            var instance = Activator.CreateInstance(implementationType);
            return instance is null
                ? ConversionFailures.Collection($"The target {kind} could not be created.", path: path)
                : ConversionResult<object?>.Success(instance);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is OperationCanceledException)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
        catch (Exception exception) when (exception is MissingMethodException or MemberAccessException
                                              or TargetInvocationException)
        {
            return ConversionFailures.Collection($"The target {kind} could not be created.", exception, path);
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

        Type valueType = value.GetType();
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

    private static bool IsKeyValuePair(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
    }

    private static bool TryGetEnumerableElementType(Type type, out Type elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        if (TryGetGenericInterface(type, typeof(IEnumerable<>), out Type enumerableInterface))
        {
            elementType = enumerableInterface.GetGenericArguments()[0];
            return true;
        }

        elementType = typeof(object);
        return false;
    }

    private static bool TryGetDictionaryTypes(Type type, out Type keyType, out Type valueType)
    {
        if (!TryGetGenericInterface(type, typeof(IDictionary<,>), out Type dictionaryInterface) &&
            !TryGetGenericInterface(type, typeof(IReadOnlyDictionary<,>), out dictionaryInterface))
        {
            keyType = typeof(object);
            valueType = typeof(object);
            return false;
        }

        Type[] genericArguments = dictionaryInterface.GetGenericArguments();
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
            .FirstOrDefault(candidate =>
                candidate.IsGenericType && candidate.GetGenericTypeDefinition() == genericDefinition)!;
        return interfaceType is not null;
    }

    private static bool IsSetType(Type type)
    {
        return type.IsGenericType &&
               (type.GetGenericTypeDefinition() == typeof(ISet<>) ||
                type.GetGenericTypeDefinition() == typeof(IReadOnlySet<>));
    }
}
