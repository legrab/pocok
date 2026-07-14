// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections;

namespace Pocok.Conversion;

internal static class TypeShape
{
    internal static Type UnwrapNullable(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    internal static bool PermitsNull(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    internal static bool IsNumeric(Type type)
    {
        var unwrappedType = UnwrapNullable(type);
        return IsIntegral(unwrappedType) || unwrappedType == typeof(float) ||
               unwrappedType == typeof(double) || unwrappedType == typeof(decimal);
    }

    internal static bool IsIntegral(Type type)
    {
        var unwrappedType = UnwrapNullable(type);
        return unwrappedType == typeof(byte) || unwrappedType == typeof(sbyte) ||
               unwrappedType == typeof(short) || unwrappedType == typeof(ushort) ||
               unwrappedType == typeof(int) || unwrappedType == typeof(uint) ||
               unwrappedType == typeof(long) || unwrappedType == typeof(ulong);
    }

    internal static bool IsTemporal(Type type)
    {
        var unwrappedType = UnwrapNullable(type);
        return unwrappedType == typeof(DateTime) || unwrappedType == typeof(DateTimeOffset) ||
               unwrappedType == typeof(DateOnly) || unwrappedType == typeof(TimeOnly) ||
               unwrappedType == typeof(TimeSpan);
    }

    internal static bool IsEnumerableSource(object value) => value is IEnumerable && value is not string;

    internal static bool IsValidTarget(Type targetType) =>
        targetType != typeof(void) &&
        !targetType.IsByRef &&
        !targetType.IsByRefLike &&
        !targetType.IsFunctionPointer &&
        !targetType.IsPointer &&
        !targetType.ContainsGenericParameters;
}
