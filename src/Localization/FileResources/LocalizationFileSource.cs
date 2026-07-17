// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Localization.FileResources;

internal sealed record LocalizationFileSource(
    string Path,
    string CultureName,
    LocalizationFileFormat Format);
