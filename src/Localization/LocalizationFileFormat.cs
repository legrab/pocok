// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Localization;

/// <summary>Identifies a supported external localization file format.</summary>
public enum LocalizationFileFormat
{
    /// <summary>A UTF-8 JSON object containing string leaves.</summary>
    Json,

    /// <summary>A string-only XML resource file.</summary>
    Resx
}
