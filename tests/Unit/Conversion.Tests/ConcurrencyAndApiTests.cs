// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

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
            overflow: OverflowPolicy.Saturate,
            numericLoss: NumericLossPolicy.RoundToNearest,
            numericBooleans: NumericBooleanPolicy.ZeroOrOne);

        var tasks = Enumerable.Range(0, 1_000)
            .Select(index => Task.Run(() =>
            {
                string[] values = [index.ToString(CultureInfo.InvariantCulture), "2"];
                converter.Convert<int>($"{index}.4", context).Value.ShouldBe(index);
                converter.Convert<byte>(index, context).Value.ShouldBe((byte)Math.Min(index, byte.MaxValue));
                converter.Convert<bool>(index % 2, context).Value.ShouldBe(index % 2 == 1);
                converter.Convert<int[]>(values, context)
                    .Value.ShouldBe([index, 2]);
            }))
            .ToArray();

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
            "Pocok.Conversion.ConversionContext",
            "Pocok.Conversion.ConversionErrorCodes",
            "Pocok.Conversion.EnumPolicy",
            "Pocok.Conversion.IValueConverter",
            "Pocok.Conversion.NullPolicy",
            "Pocok.Conversion.NumericBooleanPolicy",
            "Pocok.Conversion.NumericLossPolicy",
            "Pocok.Conversion.OverflowPolicy",
            "Pocok.Conversion.TemporalTextPolicy"
        ]);

        typeof(ValueConverter).Assembly.GetExportedTypes().ShouldBe([typeof(ValueConverter)]);
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
                "Nulls",
                "NumericBooleans",
                "NumericLoss",
                "Overflow",
                "Strict",
                "TemporalText"
            ]);

        var interfaceMethods = typeof(IValueConverter).GetMethods();
        interfaceMethods.Length.ShouldBe(2);
        interfaceMethods.Count(method => method.IsGenericMethodDefinition).ShouldBe(1);
        interfaceMethods.All(method => method.Name == nameof(IValueConverter.Convert)).ShouldBeTrue();

        var implementationMethods = typeof(ValueConverter)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        implementationMethods.Length.ShouldBe(2);
        implementationMethods.Count(method => method.IsGenericMethodDefinition).ShouldBe(1);
    }

    [Test]
    public void RuntimePackageHasNoSerializerReferenceOrMutableGlobalState()
    {
        var assembly = typeof(ValueConverter).Assembly;

        var referencesSerializer = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Any(name => name == "Newtonsoft.Json" || name == "System.Text.Json");

        referencesSerializer.ShouldBeFalse();

        var mutableStaticFields = assembly.GetTypes()
            .SelectMany(type => type.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(field => !field.IsLiteral && !field.IsInitOnly)
            .Select(field => $"{field.DeclaringType!.FullName}.{field.Name}")
            .ToArray();

        mutableStaticFields.ShouldBeEmpty();
    }
}
