// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;

namespace Pocok.Conversion.Tests.Scalars;

public sealed class EnumConversionTests
{
    private readonly ValueConverter _converter = new();

    [Test]
    public void EnumNamesAreMatchedOrdinallyIgnoringCase()
    {
        _converter.Convert<Mode>("ready").Value.ShouldBe(Mode.Ready);
        _converter.Convert<Mode>("missing").Error!.Code.ShouldBe(ConversionErrorCodes.InvalidEnum);
    }

    [Test]
    public void NumericEnumValueMustBeDefined()
    {
        _converter.Convert<Mode>(1).Value.ShouldBe(Mode.Ready);
        _converter.Convert<Mode>(9).Error!.Code.ShouldBe(ConversionErrorCodes.InvalidEnum);
    }

    [Test]
    public void FlagsCombinationIsAcceptedByStrictDefault()
    {
        _converter.Convert<Access>("Read, Write").Value.ShouldBe(Access.Read | Access.Write);
        _converter.Convert<Access>(3).Value.ShouldBe(Access.Read | Access.Write);
    }

    [Test]
    public void DefinedOnlyPolicyRejectsFlagsCombination()
    {
        var context = new ConversionContext(
            CultureInfo.InvariantCulture,
            enums: EnumPolicy.DefinedValuesOnly);

        _converter.Convert<Access>(3, context).Error!.Code.ShouldBe(ConversionErrorCodes.InvalidEnum);
    }

    [Test]
    public void UndeclaredFlagBitsAreRejected()
    {
        _converter.Convert<Access>(8).Error!.Code.ShouldBe(ConversionErrorCodes.InvalidEnum);
    }

    [Test]
    public void ZeroRequiresADeclaredZeroFlag()
    {
        _converter.Convert<AccessWithoutZero>(0).Error!.Code.ShouldBe(ConversionErrorCodes.InvalidEnum);
    }

    [Test]
    public void EnumNumericOverflowUsesNumericPolicyFirst()
    {
        _converter.Convert<ByteCode>(300).Error!.Code.ShouldBe(ConversionErrorCodes.Overflow);

        var context = new ConversionContext(CultureInfo.InvariantCulture, OverflowPolicy.Saturate);
        _converter.Convert<ByteCode>(300, context).Value.ShouldBe(ByteCode.Maximum);
    }

    [Test]
    public void SourceEnumCanConvertByUnderlyingValue()
    {
        _converter.Convert<Mode>(OtherMode.Ready).Value.ShouldBe(Mode.Ready);
    }

    private enum Mode
    {
        Unknown,
        Ready
    }

    private enum OtherMode
    {
        Ready = 1
    }

    [Flags]
    private enum Access
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4
    }

    [Flags]
    private enum AccessWithoutZero
    {
        Read = 1,
        Write = 2
    }

    private enum ByteCode : byte
    {
        None = 0,
        Maximum = byte.MaxValue
    }
}
