// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Reflection;

namespace Pocok.Hosting.Tests;

public sealed class PublicApiBaselineTests
{
    [Test]
    public void ExportedApiMatchesReviewedBaseline()
    {
        var expected = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "PublicAPI.Shipped.txt"));
        var actual = typeof(IReadinessSignal).Assembly.ExportedTypes
            .SelectMany(type => new[] { type.FullName! }.Concat(GetMembers(type)))
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
                var value = Convert.ToInt64(Enum.Parse(type, name), System.Globalization.CultureInfo.InvariantCulture);
                yield return $"{type.Name}.{name} = {value}";
            }
        }

        foreach (var constructor in type.GetConstructors(declaredPublic))
        {
            yield return $"{type.Name}.ctor({FormatParameters(constructor.GetParameters())})";
        }

        foreach (var property in type.GetProperties(declaredPublic))
        {
            yield return $"{type.Name}.{property.Name}: {FormatType(property.PropertyType)}";
        }

        foreach (var method in type.GetMethods(declaredPublic).Where(method => !method.IsSpecialName))
        {
            yield return $"{type.Name}.{method.Name}({FormatParameters(method.GetParameters())}) -> {FormatType(method.ReturnType)}";
        }
    }

    private static string FormatParameters(IEnumerable<ParameterInfo> parameters) =>
        string.Join(", ", parameters.Select(parameter => FormatType(parameter.ParameterType)));

    private static string FormatType(Type type) => type.Name;
}
