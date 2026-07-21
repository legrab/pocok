// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Pocok.Licensing.Documents;
using Pocok.Licensing.Validation;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Licensing.Models;

namespace Pocok.Showcase.Licensing;

public sealed class LicensingShowcaseSlice : ShowcaseSlice<LicensingInput, LicensingOutput>
{
    private const string MachineA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string MachineB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private static readonly IReadOnlyList<ShowcaseSample<LicensingInput>> SampleCatalog = CreateSamples();
    private static readonly ShowcaseGuide GuideCatalog = CreateGuide();

    public override ShowcaseSliceDescriptor Descriptor { get; } = new(
        "pocok.showcase.licensing",
        "Pocok.Licensing",
        "licensing",
        "Capability",
        "Experimental",
        "Package.Name",
        "Package.Summary",
        14,
        "src/Licensing/README.md",
        true,
        ShowcaseImplementationStatus.Available,
        "licensing",
        "1.0.0");

    public override Type PageComponentType => typeof(LicensingPage);
    public override IReadOnlyList<ShowcaseSample<LicensingInput>> TypedSamples => SampleCatalog;
    public override ShowcaseGuide Guide => GuideCatalog;

    public static GeneratedLicenseOutput GenerateLicense(LicensingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!TryCreateLicense(input, out LicenseDocument? license, out var error))
            throw new FormatException(error ?? "The license claims are invalid.");

        const string keyId = "showcase-ephemeral";
        var (privateKeyPem, publicKeyPem) = LicenseCryptography.CreateSigningKeyPair();
        var signedLicense = LicenseCryptography.Sign(license!, keyId, privateKeyPem);
        var trustedKeys = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [keyId] = publicKeyPem
        };
        LicenseValidationResult verified = LicenseReader.ReadAndVerify(signedLicense, trustedKeys);
        if (!verified.IsValid)
            throw new InvalidOperationException(
                $"The generated license could not be verified: {verified.Code}.");

        return new GeneratedLicenseOutput(keyId, signedLicense, publicKeyPem);
    }

    public override async ValueTask<LicensingOutput> ExecuteAsync(
        LicensingInput input,
        ShowcaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        await context.Progress.ReportAsync(
            "validate",
            "Parsing license claims and runtime facts.",
            cancellationToken).ConfigureAwait(false);

        if (!TryCreateLicense(input, out LicenseDocument? license, out var inputError) ||
            !TryCreateContext(input, out LicenseValidationContext? validationContext, out inputError))
            return Rejected(input, inputError ?? "The licensing input is invalid.");

        await context.Progress.ReportAsync(
            "execute",
            "Evaluating claims with Pocok.Licensing.LicenseValidator.",
            cancellationToken).ConfigureAwait(false);

        LicenseValidationResult result = LicenseValidator.Validate(license!, validationContext!);
        var claims = FormatClaims(license!);
        var runtime = FormatRuntime(validationContext!);
        var preview = CreateCodePreview(license!, validationContext!);

        context.Output.Write(result.Code.ToString());
        await context.Progress.ReportAsync(
            "complete",
            "License validation completed.",
            cancellationToken).ConfigureAwait(false);

        return new LicensingOutput(
            true,
            result.IsValid,
            Headline(result.Code),
            result.Code.ToString(),
            result.Message,
            license!.LicenseId,
            validationContext!.RequiredModule,
            claims,
            runtime,
            preview,
            TipsFor(result.Code));
    }

    protected override ShowcaseRunResult CreateRunResult(LicensingOutput output, TimeSpan elapsed)
    {
        var fields = new List<ShowcaseResultField>
        {
            new("Result.Fields.Code", output.Code, true, true),
            new("Result.Fields.LicenseId", output.LicenseId, true, true),
            new("Result.Fields.RequiredModule", output.RequiredModule ?? "(license-wide)", true, true),
            new("Result.Fields.Claims", output.ClaimsSummary, false, true),
            new("Result.Fields.Runtime", output.RuntimeSummary, false, true)
        };

        if (!output.InputAccepted)
            return new ShowcaseRunResult(
                ShowcaseRunStatus.Rejected,
                output.Headline,
                fields,
                diagnostics: [new ShowcaseDiagnostic(output.Code, output.Message, "warning")],
                codePreview: output.CodePreview,
                elapsed: elapsed,
                tipKeys: output.TipKeys);

        if (!output.IsValid)
            return new ShowcaseRunResult(
                ShowcaseRunStatus.ExpectedFailure,
                output.Headline,
                fields,
                diagnostics: [new ShowcaseDiagnostic($"licensing.{output.Code}", output.Message, "warning")],
                codePreview: output.CodePreview,
                elapsed: elapsed,
                tipKeys: output.TipKeys);

        return new ShowcaseRunResult(
            ShowcaseRunStatus.Success,
            output.Headline,
            fields,
            [
                new ShowcaseTimelineEvent(DateTimeOffset.UtcNow, "licensing",
                    "LicenseValidator returned a valid result.")
            ],
            codePreview: output.CodePreview,
            elapsed: elapsed,
            tipKeys: output.TipKeys);
    }

    private static bool TryCreateLicense(
        LicensingInput input,
        out LicenseDocument? license,
        out string? error)
    {
        license = null;
        if (!TryParseRequiredUtc(input.IssuedAtUtc, "Issued at", out DateTimeOffset issuedAt, out error) ||
            !TryParseOptionalUtc(input.ValidFromUtc, "Valid from", out DateTimeOffset? validFrom, out error) ||
            !TryParseOptionalUtc(input.ValidUntilUtc, "Valid until", out DateTimeOffset? validUntil, out error))
            return false;

        if (input.MaximumProcessRuntimeMinutes is < 0 or > 10_080)
        {
            error = "Maximum process runtime must be between 0 and 10080 minutes.";
            return false;
        }

        var modules = SplitIdentifiers(input.LicensedModules ?? string.Empty);
        var machines = SplitIdentifiers(input.LicensedMachineFingerprint ?? string.Empty);
        string? pskHash;
        try
        {
            pskHash = string.IsNullOrWhiteSpace(input.LicensePresharedKey)
                ? null
                : LicenseCryptography.CreatePresharedKeyHash(input.LicensePresharedKey, input.LicenseId);
        }
        catch (ArgumentException exception)
        {
            error = exception.Message;
            return false;
        }

        license = new LicenseDocument
        {
            LicenseId = input.LicenseId,
            Customer = string.IsNullOrWhiteSpace(input.Customer) ? null : input.Customer,
            IssuedAtUtc = issuedAt,
            ValidFromUtc = validFrom,
            ValidUntilUtc = validUntil,
            MaximumProcessRuntime = input.MaximumProcessRuntimeMinutes == 0
                ? null
                : TimeSpan.FromMinutes(input.MaximumProcessRuntimeMinutes),
            AllModules = input.AllModules,
            Modules = modules,
            MachineFingerprints = machines,
            PresharedKeyHash = pskHash
        };
        error = null;
        return true;
    }

    private static bool TryCreateContext(
        LicensingInput input,
        out LicenseValidationContext? context,
        out string? error)
    {
        context = null;
        if (input.ProcessRuntimeMinutes is < 0 or > 10_080)
        {
            error = "Process runtime must be between 0 and 10080 minutes.";
            return false;
        }

        if (!TryParseRequiredUtc(input.UtcNow, "Current UTC time", out DateTimeOffset utcNow, out error))
            return false;

        context = new LicenseValidationContext
        {
            UtcNow = utcNow,
            ProcessRuntime = TimeSpan.FromMinutes(input.ProcessRuntimeMinutes),
            MachineFingerprint = EmptyToNull(input.CurrentMachineFingerprint),
            PresharedKey = EmptyToNull(input.SuppliedPresharedKey),
            RequiredModule = EmptyToNull(input.RequiredModule)
        };
        error = null;
        return true;
    }

    private static bool TryParseRequiredUtc(
        string value,
        string field,
        out DateTimeOffset parsed,
        out string? error)
    {
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out parsed))
        {
            error = null;
            return true;
        }

        error = $"{field} must be an ISO-8601 timestamp, for example 2026-06-01T12:00:00Z.";
        return false;
    }

    private static bool TryParseOptionalUtc(
        string value,
        string field,
        out DateTimeOffset? parsed,
        out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsed = null;
            error = null;
            return true;
        }

        if (TryParseRequiredUtc(value, field, out DateTimeOffset required, out error))
        {
            parsed = required;
            return true;
        }

        parsed = null;
        return false;
    }

    private static string[] SplitIdentifiers(string value)
    {
        return value
            .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? EmptyToNull(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string FormatClaims(LicenseDocument license)
    {
        var modules = license.AllModules
            ? "all modules"
            : license.Modules.Count == 0
                ? "no modules"
                : string.Join(", ", license.Modules);
        var machine = license.MachineFingerprints.Count == 0
            ? "any machine"
            : $"{license.MachineFingerprints.Count} machine(s)";
        var psk = license.PresharedKeyHash is null ? "no PSK" : "PSK required";
        var runtime = license.MaximumProcessRuntime is null
            ? "unlimited process runtime"
            : $"max {license.MaximumProcessRuntime.Value.TotalMinutes:0} min";
        return $"{modules}; {machine}; {psk}; {runtime}";
    }

    private static string FormatRuntime(LicenseValidationContext context)
    {
        var module = context.RequiredModule ?? "license-wide";
        var machine = context.MachineFingerprint is null ? "no machine fingerprint" : "machine fingerprint supplied";
        var psk = context.PresharedKey is null ? "no PSK supplied" : "PSK supplied";
        return $"{context.UtcNow:O}; runtime {context.ProcessRuntime.TotalMinutes:0} min; {module}; {machine}; {psk}";
    }

    private static string CreateCodePreview(LicenseDocument license, LicenseValidationContext context)
    {
        var modules = license.AllModules
            ? "AllModules = true"
            : $"Modules = [{string.Join(", ", license.Modules.Select(Quote))}]";
        var requiredModule = context.RequiredModule is null ? "null" : Quote(context.RequiredModule);
        return $$"""
                 LicenseDocument license = new()
                 {
                     LicenseId = {{Quote(license.LicenseId)}},
                     {{modules}},
                     ValidUntilUtc = {{FormatNullableDate(license.ValidUntilUtc)}}
                 };

                 LicenseValidationResult result = LicenseValidator.Validate(
                     license,
                     new LicenseValidationContext
                     {
                         UtcNow = DateTimeOffset.Parse({{Quote(context.UtcNow.ToString("O", CultureInfo.InvariantCulture))}}),
                         ProcessRuntime = TimeSpan.FromMinutes({{context.ProcessRuntime.TotalMinutes.ToString("0", CultureInfo.InvariantCulture)}}),
                         RequiredModule = {{requiredModule}}
                     });
                 """;
    }

    private static string Quote(string value)
    {
        return
            $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }

    private static string FormatNullableDate(DateTimeOffset? value)
    {
        return value is null
            ? "null"
            : $"DateTimeOffset.Parse({Quote(value.Value.ToString("O", CultureInfo.InvariantCulture))})";
    }

    private static LicensingOutput Rejected(LicensingInput input, string message)
    {
        return new LicensingOutput(
            false,
            false,
            "Input rejected",
            "showcase.licensing-input",
            message,
            input.LicenseId,
            EmptyToNull(input.RequiredModule),
            "Input could not be converted into license claims.",
            "Input could not be converted into runtime facts.",
            "// Fix the highlighted input and run validation again.",
            ["Tips.ExpectedFailures"]);
    }

    private static string Headline(LicenseValidationCode code)
    {
        return code switch
        {
            LicenseValidationCode.Valid => "License valid",
            LicenseValidationCode.NotYetValid => "License not active yet",
            LicenseValidationCode.Expired => "License expired",
            LicenseValidationCode.RuntimeExceeded => "Runtime limit exceeded",
            LicenseValidationCode.MachineMismatch => "Machine mismatch",
            LicenseValidationCode.PresharedKeyRequired => "Pre-shared key required",
            LicenseValidationCode.PresharedKeyMismatch => "Pre-shared key mismatch",
            LicenseValidationCode.ModuleMissing => "Module missing",
            LicenseValidationCode.Malformed => "License claims malformed",
            _ => code.ToString()
        };
    }

    private static List<string> TipsFor(LicenseValidationCode code)
    {
        var tips = new List<string> { "Tips.Claims", "Tips.ExpectedFailures" };
        if (code is LicenseValidationCode.ModuleMissing or LicenseValidationCode.Valid)
            tips.Add("Tips.Modules");
        if (code is LicenseValidationCode.MachineMismatch)
            tips.Add("Tips.Machines");
        if (code is LicenseValidationCode.PresharedKeyRequired or LicenseValidationCode.PresharedKeyMismatch)
            tips.Add("Tips.Psk");
        return tips;
    }

    private static IReadOnlyList<ShowcaseSample<LicensingInput>> CreateSamples()
    {
        return
        [
            Sample("valid-module", "License valid", true),
            Sample("missing-module", "Module missing", requiredModule: "Admin"),
            Sample("expired", "License expired", utcNow: "2027-01-01T00:00:00Z"),
            Sample("runtime-exceeded", "Runtime limit exceeded", processRuntimeMinutes: 121),
            Sample(
                "machine-mismatch",
                "Machine mismatch",
                licensedMachine: MachineA,
                currentMachine: MachineB),
            Sample(
                "psk-mismatch",
                "Pre-shared key mismatch",
                licensePsk: "correct-high-entropy-demo-key",
                suppliedPsk: "wrong-demo-key")
        ];
    }

    private static ShowcaseSample<LicensingInput> Sample(
        string id,
        string expected,
        bool isDefault = false,
        string requiredModule = "Reporting",
        string utcNow = "2026-06-01T12:00:00Z",
        int processRuntimeMinutes = 30,
        string licensedMachine = "",
        string currentMachine = "",
        string licensePsk = "",
        string suppliedPsk = "")
    {
        return new ShowcaseSample<LicensingInput>(
            id,
            $"Samples.{id}.Name",
            $"Samples.{id}.Description",
            () => new LicensingInput
            {
                SampleId = id,
                RequiredModule = requiredModule,
                UtcNow = utcNow,
                ProcessRuntimeMinutes = processRuntimeMinutes,
                LicensedMachineFingerprint = licensedMachine,
                CurrentMachineFingerprint = currentMachine,
                LicensePresharedKey = licensePsk,
                SuppliedPresharedKey = suppliedPsk
            },
            isDefault,
            expected,
            "quick-start",
            "validator");
    }

    private static ShowcaseGuide CreateGuide()
    {
        return new ShowcaseGuide(
            [
                new ShowcaseGuideSection("purpose", "Guide.Purpose.Title", ["Guide.Purpose.Body"]),
                new ShowcaseGuideSection(
                    "quick-start",
                    "Guide.QuickStart.Title",
                    ["Guide.QuickStart.Body"],
                    ["validator"]),
                new ShowcaseGuideSection("claims", "Guide.Claims.Title", ["Guide.Claims.Body"]),
                new ShowcaseGuideSection("signatures", "Guide.Signatures.Title", ["Guide.Signatures.Body"]),
                new ShowcaseGuideSection("runtime", "Guide.Runtime.Title", ["Guide.Runtime.Body"]),
                new ShowcaseGuideSection("production", "Guide.Production.Title", ["Guide.Production.Body"])
            ],
            [
                new ShowcaseCodeSnippet(
                    "validator",
                    "Guide.Snippet.ValidatorTitle",
                    "csharp",
                    """
                    LicenseValidationResult result = LicenseValidator.Validate(
                        verifiedLicense,
                        new LicenseValidationContext
                        {
                            UtcNow = clock.GetUtcNow(),
                            ProcessRuntime = processRuntime,
                            RequiredModule = "Reporting"
                        });
                    """)
            ]);
    }
}
