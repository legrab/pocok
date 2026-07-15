// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Pocok.Conversion.Tests;

public sealed class PublicApiBaselineTests
{
    [Test]
    public void AbstractionsApiMatchesReviewedBaseline() =>
        AssertBaseline(
            typeof(IValueConverter).Assembly,
            "Conversion.Abstractions.PublicAPI.Shipped.txt");

    [Test]
    public void ImplementationApiMatchesReviewedBaseline() =>
        AssertBaseline(
            typeof(ValueConverter).Assembly,
            "Conversion.PublicAPI.Shipped.txt");

    private static void AssertBaseline(Assembly assembly, string baselineName)
    {
        var expected = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, baselineName));
        var actual = assembly.ExportedTypes
            .SelectMany(type => new[] { FormatType(type) }.Concat(GetMembers(type)))
            .Order(StringComparer.Ordinal)
            .ToArray();

        actual.ShouldBe(expected);
    }

    private static IEnumerable<string> GetMembers(Type type)
    {
        const BindingFlags declaredPublic = BindingFlags.Public |
                                            BindingFlags.Instance |
                                            BindingFlags.Static |
                                            BindingFlags.DeclaredOnly;

        if (type.IsEnum)
        {
            foreach (var name in Enum.GetNames(type))
            {
                var value = Convert.ToInt64(Enum.Parse(type, name), CultureInfo.InvariantCulture);
                yield return $"{TypeName(type)}.{name} = {value}";
            }
        }
        else
        {
            foreach (var field in type.GetFields(declaredPublic)
                         .Where(field => field.IsLiteral && !IsCompilerGenerated(field)))
            {
                yield return $"{TypeName(type)}.{field.Name}: {FormatType(field.FieldType)} = {FormatConstant(field.GetRawConstantValue())}";
            }
        }

        foreach (var constructor in type.GetConstructors(declaredPublic)
                     .Where(member => !IsCompilerGenerated(member)))
        {
            yield return $"{TypeName(type)}.ctor({FormatParameters(constructor.GetParameters())})";
        }

        foreach (var property in type.GetProperties(declaredPublic)
                     .Where(member => !IsCompilerGenerated(member)))
        {
            yield return $"{TypeName(type)}.{property.Name}: {FormatType(property.PropertyType)}";
        }

        foreach (var method in type.GetMethods(declaredPublic)
                     .Where(method => !method.IsSpecialName && !IsCompilerGenerated(method)))
        {
            yield return $"{TypeName(type)}.{method.Name}({FormatParameters(method.GetParameters())}) -> {FormatType(method.ReturnType)}";
        }
    }

    private static bool IsCompilerGenerated(MemberInfo member) =>
        member.GetCustomAttribute<CompilerGeneratedAttribute>() is not null;

    private static string FormatParameters(IEnumerable<ParameterInfo> parameters) =>
        string.Join(", ", parameters.Select(parameter => FormatType(parameter.ParameterType)));

    private static string FormatType(Type type)
    {
        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        var name = (type.GetGenericTypeDefinition().FullName ?? type.Name).Split('`')[0];
        var arguments = string.Join(", ", type.GetGenericArguments().Select(FormatType));
        return $"{name}<{arguments}>";
    }

    private static string FormatConstant(object? value) => value switch
    {
        string text => $"\"{text}\"",
        null => "null",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)!
    };

    private static string TypeName(Type type) => FormatType(type).Split('.').Last();
}
