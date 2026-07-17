// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.ComponentModel.DataAnnotations;

namespace Pocok.Showcase.Web.Services;

public sealed class ShowcaseOptions
{
    public const string SectionName = "Showcase";

    public bool RequireCompleteCatalog { get; set; }
    public string? PublicRepositoryBaseUrl { get; set; }

    public bool InAppLogConsoleEnabled { get; set; } = true;

    [Range(4, 512)]
    public int InAppLogCapacity { get; set; } = 64;

    public LogLevel InAppLogMinimumLevel { get; set; } = LogLevel.Information;

    [Range(80, 512)]
    public int InAppLogMaximumTextLength { get; set; } = 240;

    [Range(1, 256)]
    public int QueueCapacity { get; set; } = 16;

    public TimeSpan RunTimeout { get; set; } = TimeSpan.FromSeconds(5);

    [Range(1_024, 1_048_576)]
    public int MaximumInputBytes { get; set; } = 65_536;

    [Range(1_024, 1_048_576)]
    public int MaximumOutputCharacters { get; set; } = 131_072;

    [Range(1, 1_000)]
    public int MaximumTemporaryFiles { get; set; } = 32;
}
