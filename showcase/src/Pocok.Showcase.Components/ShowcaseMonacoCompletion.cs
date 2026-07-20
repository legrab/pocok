// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Showcase.Components;

/// <summary>Describes one small engine-owned Monaco completion item.</summary>
public sealed record ShowcaseMonacoCompletion(
    string Label,
    string InsertText,
    string? Documentation = null);
