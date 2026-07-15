// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;

namespace Pocok.Conversion;

internal static class EnumConversion
{
    internal static ConversionResult<object?> Convert(object value, Type targetType, ConversionContext context)
    {
        object parsed;
        if (value is string text)
        {
            if (!Enum.TryParse(targetType, text, true, out var parsedValue) || parsedValue is null)
                return ConversionFailures.InvalidEnum(targetType);

            parsed = parsedValue;
        }
        else if (TypeShape.IsNumeric(value.GetType()) || value.GetType().IsEnum)
        {
            Type underlyingType = Enum.GetUnderlyingType(targetType);
            ConversionResult<object?> numericResult = NumericConversion.Convert(value, underlyingType, context);
            if (numericResult.IsFailure) return numericResult;

            parsed = Enum.ToObject(targetType, numericResult.Value!);
        }
        else
        {
            return ConversionFailures.Unsupported(value.GetType(), targetType);
        }

        return IsAccepted(parsed, targetType, context.Enums)
            ? ConversionResult<object?>.Success(parsed)
            : ConversionFailures.InvalidEnum(targetType);
    }

    private static bool IsAccepted(object value, Type enumType, EnumPolicy policy)
    {
        if (Enum.IsDefined(enumType, value)) return true;

        if (policy != EnumPolicy.DefinedValuesAndFlags ||
            enumType.GetCustomAttributes(typeof(FlagsAttribute), false).Length == 0)
            return false;

        Type underlyingType = Enum.GetUnderlyingType(enumType);
        var valueBits = GetBits(value, underlyingType);
        ulong allowedBits = 0;
        var zeroIsDefined = false;

        foreach (var definedValue in Enum.GetValues(enumType))
        {
            var bits = GetBits(definedValue, underlyingType);
            allowedBits |= bits;
            zeroIsDefined |= bits == 0;
        }

        return (valueBits != 0 || zeroIsDefined) && (valueBits & ~allowedBits) == 0;
    }

    private static ulong GetBits(object value, Type underlyingType)
    {
        return Type.GetTypeCode(underlyingType) switch
        {
            TypeCode.SByte => unchecked((ulong)System.Convert.ToSByte(value, CultureInfo.InvariantCulture)),
            TypeCode.Int16 => unchecked((ulong)System.Convert.ToInt16(value, CultureInfo.InvariantCulture)),
            TypeCode.Int32 => unchecked((ulong)System.Convert.ToInt32(value, CultureInfo.InvariantCulture)),
            TypeCode.Int64 => unchecked((ulong)System.Convert.ToInt64(value, CultureInfo.InvariantCulture)),
            _ => System.Convert.ToUInt64(value, CultureInfo.InvariantCulture)
        };
    }
}
