// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Showcase.Licensing.Models;

public sealed record LicensingInput
{
    public string SampleId { get; init; } = "valid-module";
    public string LicenseId { get; init; } = "demo-license";
    public string Customer { get; init; } = "Example customer";
    public string IssuedAtUtc { get; init; } = "2026-01-01T00:00:00Z";
    public string ValidFromUtc { get; init; } = "2026-01-01T00:00:00Z";
    public string ValidUntilUtc { get; init; } = "2027-01-01T00:00:00Z";
    public int MaximumProcessRuntimeMinutes { get; init; } = 120;
    public bool AllModules { get; init; }
    public string LicensedModules { get; init; } = "Reporting, Export";
    public string RequiredModule { get; init; } = "Reporting";
    public string LicensedMachineFingerprint { get; init; } = string.Empty;
    public string CurrentMachineFingerprint { get; init; } = string.Empty;
    public string LicensePresharedKey { get; init; } = string.Empty;
    public string SuppliedPresharedKey { get; init; } = string.Empty;
    public string UtcNow { get; init; } = "2026-06-01T12:00:00Z";
    public int ProcessRuntimeMinutes { get; init; } = 30;
}

public sealed record LicensingOutput(
    bool InputAccepted,
    bool IsValid,
    string Headline,
    string Code,
    string Message,
    string LicenseId,
    string? RequiredModule,
    string ClaimsSummary,
    string RuntimeSummary,
    string CodePreview,
    IReadOnlyList<string> TipKeys);
