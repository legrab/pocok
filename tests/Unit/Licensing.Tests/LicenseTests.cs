// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Pocok.Licensing.Tests;

[TestFixture]
public sealed class LicenseTests
{
    [Test]
    public void SignedLicenseRoundTripsAndChecksModules()
    {
        (var privateKey, var publicKey) = LicenseCryptography.CreateSigningKeyPair();
        LicenseDocument license = NewLicense(modules: ["Reporting"]);

        var text = LicenseCryptography.Sign(license, "test", privateKey);
        LicenseValidationResult read = LicenseReader.ReadAndVerify(text, Trusted(publicKey));

        read.IsValid.ShouldBeTrue();
        LicenseValidator.Validate(read.License!, Context(requiredModule: "Reporting")).IsValid.ShouldBeTrue();
        LicenseValidator.Validate(read.License!, Context(requiredModule: "Admin")).Code
            .ShouldBe(LicenseValidationCode.ModuleMissing);
    }

    [Test]
    public void PayloadOrSignatureTamperingIsRejected()
    {
        (var privateKey, var publicKey) = LicenseCryptography.CreateSigningKeyPair();
        var text = LicenseCryptography.Sign(NewLicense(true), "test", privateKey);
        JsonObject envelope = JsonNode.Parse(text)!.AsObject();
        var payload = envelope["payload"]!.GetValue<string>();
        envelope["payload"] = FlipBase64Character(payload);

        LicenseReader.ReadAndVerify(envelope.ToJsonString(), Trusted(publicKey)).Code
            .ShouldBe(LicenseValidationCode.InvalidSignature);
    }

    [Test]
    public void WrongSigningKeyIsRejected()
    {
        (var privateKey, var _) = LicenseCryptography.CreateSigningKeyPair();
        (var _, var otherPublicKey) = LicenseCryptography.CreateSigningKeyPair();
        var text = LicenseCryptography.Sign(NewLicense(true), "test", privateKey);

        LicenseReader.ReadAndVerify(text, Trusted(otherPublicKey)).Code
            .ShouldBe(LicenseValidationCode.InvalidSignature);
        LicenseReader.ReadAndVerify(text, new Dictionary<string, string>()).Code
            .ShouldBe(LicenseValidationCode.UntrustedSigningKey);
    }

    [Test]
    public void EncryptedLicenseRequiresSecretAndDetectsWrongSecret()
    {
        (var privateKey, var publicKey) = LicenseCryptography.CreateSigningKeyPair();
        var signed = LicenseCryptography.Sign(NewLicense(true), "test", privateKey);
        var encrypted = LicenseCryptography.Encrypt(signed, "a-high-entropy-encryption-secret");

        LicenseReader.ReadAndVerify(encrypted, Trusted(publicKey)).Code
            .ShouldBe(LicenseValidationCode.DecryptionSecretRequired);
        LicenseReader.ReadAndVerify(encrypted, Trusted(publicKey), "wrong-high-entropy-secret").Code
            .ShouldBe(LicenseValidationCode.Malformed);
        LicenseReader.ReadAndVerify(encrypted, Trusted(publicKey), "a-high-entropy-encryption-secret").IsValid
            .ShouldBeTrue();
    }

    [Test]
    public void TimeRuntimeMachinePskAndModuleBoundariesAreEnforced()
    {
        const string psk = "a-high-entropy-runtime-preshared-key";
        CultureInfo provider = CultureInfo.InvariantCulture;
        LicenseDocument license = NewLicense(
            validFrom: DateTimeOffset.Parse("2026-01-01T00:00:00Z", provider),
            validUntil: DateTimeOffset.Parse("2027-01-01T00:00:00Z", provider),
            maximumRuntime: TimeSpan.FromHours(1),
            machines: [Machine],
            pskHash: LicenseCryptography.CreatePresharedKeyHash(psk, "test"),
            modules: ["Reporting"]);

        LicenseValidator.Validate(
                license,
                Context(
                    DateTimeOffset.Parse("2026-01-01T00:00:00Z", provider),
                    TimeSpan.FromHours(1),
                    Machine,
                    psk,
                    "reporting"))
            .IsValid.ShouldBeTrue();
        LicenseValidator.Validate(license, Context(DateTimeOffset.Parse("2025-12-31T23:59:59Z", provider))).Code
            .ShouldBe(LicenseValidationCode.NotYetValid);
        LicenseValidator.Validate(license, Context(DateTimeOffset.Parse("2027-01-01T00:00:00Z", provider))).Code
            .ShouldBe(LicenseValidationCode.Expired);
        LicenseValidator.Validate(license, Context(runtime: TimeSpan.FromHours(1) + TimeSpan.FromTicks(1))).Code
            .ShouldBe(LicenseValidationCode.RuntimeExceeded);
        LicenseValidator.Validate(license, Context(machine: OtherMachine)).Code
            .ShouldBe(LicenseValidationCode.MachineMismatch);
        LicenseValidator.Validate(license, Context(machine: Machine, psk: "wrong")).Code
            .ShouldBe(LicenseValidationCode.PresharedKeyMismatch);
    }

    [Test]
    public void PresharedKeyDigestIsScopedToLicenseId()
    {
        var first = LicenseCryptography.CreatePresharedKeyHash("same-secret", "license-one");
        var second = LicenseCryptography.CreatePresharedKeyHash("same-secret", "license-two");
        first.ShouldNotBe(second);
    }

    [Test]
    public void MalformedClaimsAreRejectedBeforeSigning()
    {
        CultureInfo provider = CultureInfo.InvariantCulture;
        LicenseDocument license = NewLicense(validFrom: DateTimeOffset.Parse("2027-01-01T00:00:00Z", provider),
            validUntil: DateTimeOffset.Parse("2026-01-01T00:00:00Z", provider));
        (var privateKey, var _) = LicenseCryptography.CreateSigningKeyPair();

        Should.Throw<ArgumentException>(() => LicenseCryptography.Sign(license, "test", privateKey));
    }

    [Test]
    public async Task ServiceRefreshesChangedLicenseFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pocok-license-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var licensePath = Path.Combine(directory, "license.pocok");
            (var privateKey, var publicKey) = LicenseCryptography.CreateSigningKeyPair();
            await File.WriteAllTextAsync(licensePath,
                LicenseCryptography.Sign(NewLicense(modules: ["First"]), "test", privateKey));

            using ServiceProvider provider = new ServiceCollection()
                .AddLogging(builder => builder.SetMinimumLevel(LogLevel.None))
                .AddPocokLicensing(options =>
                {
                    options.LicensePath = licensePath;
                    options.TrustedPublicKeys["test"] = publicKey;
                })
                .BuildServiceProvider();
            ILicenseService service = provider.GetRequiredService<ILicenseService>();
            (await service.RefreshAsync("First")).IsValid.ShouldBeTrue();

            await File.WriteAllTextAsync(licensePath,
                LicenseCryptography.Sign(NewLicense(modules: ["Second"]), "test", privateKey));
            (await service.RefreshAsync("Second")).IsValid.ShouldBeTrue();
            service.Validate("First").Code.ShouldBe(LicenseValidationCode.ModuleMissing);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public async Task FailedRefreshClearsPreviouslyLoadedLicense()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pocok-license-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var licensePath = Path.Combine(directory, "license.pocok");
            (var privateKey, var publicKey) = LicenseCryptography.CreateSigningKeyPair();
            await File.WriteAllTextAsync(
                licensePath,
                LicenseCryptography.Sign(NewLicense(modules: ["Reporting"]), "test", privateKey));

            using ServiceProvider provider = new ServiceCollection()
                .AddLogging(builder => builder.SetMinimumLevel(LogLevel.None))
                .AddPocokLicensing(options =>
                {
                    options.LicensePath = licensePath;
                    options.TrustedPublicKeys["test"] = publicKey;
                })
                .BuildServiceProvider();
            ILicenseService service = provider.GetRequiredService<ILicenseService>();
            (await service.RefreshAsync("Reporting")).IsValid.ShouldBeTrue();

            await File.WriteAllTextAsync(licensePath, "not-a-license");
            LicenseValidationResult refresh = await service.RefreshAsync("Reporting");

            refresh.Code.ShouldBe(LicenseValidationCode.Malformed);
            service.Validate("Reporting").Code.ShouldBe(LicenseValidationCode.Malformed);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(256)]
    public void NonPortableBlockingExitCodeIsRejected(int exitCode)
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentOutOfRangeException>(() => services.AddPocokLicensing(options =>
        {
            options.LicenseText = "not-loaded";
            options.BlockingExitCode = exitCode;
        }));
    }

    [Test]
    public void AttributeGuardDemandsDeclaredModules()
    {
        var service = new StubLicenseService();
        typeof(ProtectedFeature).GetMethod(nameof(ProtectedFeature.Execute))!.DemandWith(service);
        service.Demanded.ShouldBe(["Reporting", "Export"]);
    }

    [Test]
    public void ValidationBeforeFirstRefreshReturnsMissing()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddPocokLicensing(options => options.LicenseText = "not-loaded")
            .BuildServiceProvider();
        ILicenseService service = provider.GetRequiredService<ILicenseService>();

        service.Validate().Code.ShouldBe(LicenseValidationCode.Missing);
    }

    [Test]
    public void DuplicateRegistrationIsRejected()
    {
        var services = new ServiceCollection();
        services.AddPocokLicensing(options => options.LicenseText = "not-loaded");

        Should.Throw<InvalidOperationException>(() =>
            services.AddPocokLicensing(options => options.LicenseText = "not-loaded"));
    }

    [Test]
    public void MachineOverrideIsDeterministic()
    {
        var previous = Environment.GetEnvironmentVariable("POCOK_MACHINE_ID");
        try
        {
            Environment.SetEnvironmentVariable("POCOK_MACHINE_ID", "test-machine");
            var first = new DefaultMachineFingerprintProvider().GetFingerprint();
            var second = new DefaultMachineFingerprintProvider().GetFingerprint();
            first.ShouldBe(second);
            first.Length.ShouldBe(64);
        }
        finally
        {
            Environment.SetEnvironmentVariable("POCOK_MACHINE_ID", previous);
        }
    }

    private const string Machine = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string OtherMachine = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    private static LicenseDocument NewLicense(
        bool allModules = false,
        string[]? modules = null,
        DateTimeOffset? validFrom = null,
        DateTimeOffset? validUntil = null,
        TimeSpan? maximumRuntime = null,
        string[]? machines = null,
        string? pskHash = null)
    {
        return new LicenseDocument
        {
            LicenseId = "test",
            IssuedAtUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture),
            ValidFromUtc = validFrom,
            ValidUntilUtc = validUntil,
            MaximumProcessRuntime = maximumRuntime,
            AllModules = allModules,
            Modules = modules ?? [],
            MachineFingerprints = machines ?? [],
            PresharedKeyHash = pskHash
        };
    }

    private static Dictionary<string, string> Trusted(string publicKey)
    {
        return new Dictionary<string, string> { ["test"] = publicKey };
    }

    private static LicenseValidationContext Context(
        DateTimeOffset? utcNow = null,
        TimeSpan? runtime = null,
        string? machine = Machine,
        string? psk = null,
        string? requiredModule = null)
    {
        return new LicenseValidationContext
        {
            UtcNow = utcNow ?? DateTimeOffset.Parse("2026-06-01T00:00:00Z", CultureInfo.InvariantCulture),
            ProcessRuntime = runtime ?? TimeSpan.Zero,
            MachineFingerprint = machine,
            PresharedKey = psk,
            RequiredModule = requiredModule
        };
    }

    private static string FlipBase64Character(string value)
    {
        return (value[0] == 'A' ? "B" : "A") + value[1..];
    }

    [RequiresLicense("Reporting")]
    private sealed class ProtectedFeature
    {
        [RequiresLicense("Export")]
        public static void Execute()
        {
        }
    }

    private sealed class StubLicenseService : ILicenseService
    {
        public List<string> Demanded { get; } = [];
        public LicenseValidationResult Current => throw new NotSupportedException();

        public ValueTask<LicenseValidationResult> RefreshAsync(string? requiredModule = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public LicenseValidationResult Validate(string? requiredModule = null)
        {
            throw new NotSupportedException();
        }

        public bool HasModule(string moduleIdentifier)
        {
            throw new NotSupportedException();
        }

        public void Demand(string moduleIdentifier)
        {
            Demanded.Add(moduleIdentifier);
        }
    }
}

internal static class MemberInfoTestExtensions
{
    internal static void DemandWith(this MemberInfo member, ILicenseService service)
    {
        service.DemandFor(member);
    }
}
