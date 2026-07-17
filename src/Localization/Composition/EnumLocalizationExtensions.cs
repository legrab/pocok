// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Localization;

namespace Pocok.Localization.Composition;

/// <summary>Provides deterministic resource-key fallback for enum display values.</summary>
public static class EnumLocalizationExtensions
{
    /// <summary>Looks up <c>EnumType.Member</c>, then the bare member name.</summary>
    public static string Translate<TEnum>(this TEnum value, IStringLocalizer localizer)
        where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(localizer);
        return TranslateCore(typeof(TEnum).Name, value.ToString(), localizer);
    }

    /// <summary>Looks up the boxed enum's type-qualified key, then the bare member name.</summary>
    public static string Translate(this Enum value, IStringLocalizer localizer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(localizer);
        return TranslateCore(value.GetType().Name, value.ToString(), localizer);
    }

    private static string TranslateCore(string typeName, string memberName, IStringLocalizer localizer)
    {
        LocalizedString prefixed = localizer[$"{typeName}.{memberName}"];
        return prefixed.ResourceNotFound ? localizer[memberName] : prefixed;
    }
}
