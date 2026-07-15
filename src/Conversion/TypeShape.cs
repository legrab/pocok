// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections;

namespace Pocok.Conversion;

internal static class TypeShape
{
    internal static Type UnwrapNullable(Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    internal static bool PermitsNull(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
    }

    internal static bool IsNumeric(Type type)
    {
        Type unwrappedType = UnwrapNullable(type);
        return IsIntegral(unwrappedType) || unwrappedType == typeof(float) ||
               unwrappedType == typeof(double) || unwrappedType == typeof(decimal);
    }

    internal static bool IsIntegral(Type type)
    {
        Type unwrappedType = UnwrapNullable(type);
        return unwrappedType == typeof(byte) || unwrappedType == typeof(sbyte) ||
               unwrappedType == typeof(short) || unwrappedType == typeof(ushort) ||
               unwrappedType == typeof(int) || unwrappedType == typeof(uint) ||
               unwrappedType == typeof(long) || unwrappedType == typeof(ulong);
    }

    internal static bool IsTemporal(Type type)
    {
        Type unwrappedType = UnwrapNullable(type);
        return unwrappedType == typeof(DateTime) || unwrappedType == typeof(DateTimeOffset) ||
               unwrappedType == typeof(DateOnly) || unwrappedType == typeof(TimeOnly) ||
               unwrappedType == typeof(TimeSpan);
    }

    internal static bool IsEnumerableSource(object value)
    {
        return value is IEnumerable and not string;
    }

    internal static bool IsValidTarget(Type targetType)
    {
        return targetType != typeof(void) &&
               targetType is
               {
                   IsByRef: false, IsByRefLike: false, IsFunctionPointer: false, IsPointer: false,
                   ContainsGenericParameters: false
               };
    }
}
