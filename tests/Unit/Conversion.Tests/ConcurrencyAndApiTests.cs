// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Pocok.Conversion.Tests;

public sealed class ConcurrencyAndApiTests
{
    [Test]
    public async Task OneConverterSupportsConcurrentMixedConversions()
    {
        IValueConverter converter = new ValueConverter();
        var context = new ConversionContext(
            CultureInfo.InvariantCulture,
            OverflowPolicy.Saturate,
            numericLoss: NumericLossPolicy.RoundToNearest,
            numericBooleans: NumericBooleanPolicy.ZeroOrOne);

        Task[] tasks =
        [
            .. Enumerable.Range(0, 1_000)
                .Select(index => Task.Run(() =>
                {
                    string[] values = [index.ToString(CultureInfo.InvariantCulture), "2"];
                    converter.Convert<int>($"{index}.4", context).Value.ShouldBe(index);
                    converter.Convert<byte>(index, context).Value.ShouldBe((byte)Math.Min(index, byte.MaxValue));
                    converter.Convert<bool>(index % 2, context).Value.ShouldBe(index % 2 == 1);
                    converter.Convert<int[]>(values, context)
                        .Value.ShouldBe([index, 2]);
                }))
        ];

        await Task.WhenAll(tasks);
    }

    [Test]
    public void ReviewedPublicTypeSetIsStable()
    {
        var abstractionsTypes = typeof(IValueConverter).Assembly.GetExportedTypes()
            .Select(type => type.FullName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        abstractionsTypes.ShouldBe(
        [
            "Pocok.Conversion.ConversionContext", "Pocok.Conversion.ConversionErrorCodes",
            "Pocok.Conversion.ConversionFailure", "Pocok.Conversion.ConversionResult`1",
            "Pocok.Conversion.ConversionStrategyContext",
            "Pocok.Conversion.ConversionStrategyPrecedence", "Pocok.Conversion.ConversionStrategyResult",
            "Pocok.Conversion.ConversionStrategyStatus",
            "Pocok.Conversion.EnumPolicy", "Pocok.Conversion.IConversionStrategy", "Pocok.Conversion.IValueConverter",
            "Pocok.Conversion.NullPolicy",
            "Pocok.Conversion.NumericBooleanPolicy", "Pocok.Conversion.NumericLossPolicy",
            "Pocok.Conversion.OverflowPolicy", "Pocok.Conversion.TemporalTextPolicy", "Pocok.Conversion.ValueConverter"
        ]);
    }

    [Test]
    public void ReviewedContractMembersAreStable()
    {
        typeof(ConversionContext).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ShouldBe(
            [
                "Culture",
                "Enums",
                "MaximumCollectionItems",
                "MaximumDepth",
                "Nulls",
                "NumericBooleans",
                "NumericLoss",
                "Overflow",
                "Strict",
                "TemporalText"
            ]);

        MethodInfo[] interfaceMethods = typeof(IValueConverter).GetMethods();
        interfaceMethods.Length.ShouldBe(2);
        interfaceMethods.Count(method => method.IsGenericMethodDefinition).ShouldBe(1);
        interfaceMethods.All(method => method.Name == nameof(IValueConverter.Convert)).ShouldBeTrue();

        MethodInfo[] implementationMethods = typeof(ValueConverter)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        implementationMethods.Length.ShouldBe(2);
        implementationMethods.Count(method => method.IsGenericMethodDefinition).ShouldBe(1);
    }

    [Test]
    public void RuntimeConversionApisDeclareTheirTrimIncompatibility()
    {
        MethodInfo[] interfaceMethods = typeof(IValueConverter).GetMethods();
        MethodInfo[] implementationMethods = typeof(ValueConverter)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        interfaceMethods.All(method =>
            method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>() is not null).ShouldBeTrue();
        implementationMethods.All(method =>
            method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>() is not null).ShouldBeTrue();
    }

    [Test]
    public void RuntimePackageHasNoSerializerReferenceOrMutableGlobalState()
    {
        Assembly assembly = typeof(ValueConverter).Assembly;

        var referencesSerializer = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Any(name => name is "Newtonsoft.Json" or "System.Text.Json");

        referencesSerializer.ShouldBeFalse();

        var mutableStaticFields = assembly.GetTypes()
            .Where(type => !type.FullName!.Contains("+<>c"))
            .SelectMany(type => type.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(field => field is { IsLiteral: false, IsInitOnly: false })
            .Select(field => $"{field.DeclaringType!.FullName}.{field.Name}")
            .ToArray();

        mutableStaticFields.ShouldBeEmpty();
    }
}
