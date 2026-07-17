// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pocok.Licensing;
using Pocok.Licensing.Documents;
using Pocok.Licensing.Runtime;
using Pocok.Licensing.Validation;
using Shouldly;

namespace Pocok.AppDefaults.Licensing.Tests;

[TestFixture]
public sealed class LicensingDefaultsTests
{
    [Test]
    public async Task ValidLicenseAllowsHostStartup()
    {
        var (privateKey, publicKey) = LicenseCryptography.CreateSigningKeyPair();
        var license = LicenseCryptography.Sign(new LicenseDocument
        {
            LicenseId = "host-test",
            IssuedAtUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture),
            Modules = ["Reporting"]
        }, "test", privateKey);

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddPocokLicensingDefaults(options =>
        {
            options.LicenseText = license;
            options.TrustedPublicKeys["test"] = publicKey;
            options.RequiredModules = ["Reporting"];
        });
        using IHost host = builder.Build();

        await host.StartAsync();
        host.Services.GetRequiredService<ILicenseService>().Current.IsValid.ShouldBeTrue();
        await host.StopAsync();
    }

    [Test]
    public void BlockingFailureRejectsHostStartup()
    {
        var previousExitCode = Environment.ExitCode;
        try
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder();
            builder.AddPocokLicensingDefaults(options =>
            {
                options.LicensePath = "missing-license.pocok";
                options.BlockingExitCode = 79;
            });
            using IHost host = builder.Build();

            Assert.ThrowsAsync<LicenseException>(() => host.StartAsync());
            Environment.ExitCode.ShouldBe(79);
        }
        finally
        {
            Environment.ExitCode = previousExitCode;
        }
    }

    [Test]
    public async Task WarningFailureAllowsHostStartup()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddPocokLicensingDefaults(options =>
        {
            options.LicensePath = "missing-license.pocok";
            options.FailureBehavior = LicenseFailureBehavior.Warn;
        });
        using IHost host = builder.Build();

        await host.StartAsync();
        host.Services.GetRequiredService<ILicenseService>().Current.Code.ShouldBe(LicenseValidationCode.Missing);
        await host.StopAsync();
    }

    [Test]
    public void ConfigurationIsBoundBeforeDelegateAndRelativePathsUseContentRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"pocok-appdefaults-license-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = root });
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Pocok:Licensing:LicensePath"] = "configured/license.pocok",
                ["Pocok:Licensing:FailureBehavior"] = "Warn",
                ["Pocok:Licensing:TrustedPublicKeyFiles:test"] = "keys/public.pem"
            });

            builder.AddPocokLicensingDefaults(options => options.RequiredModules = ["Reporting"]);
            using IHost host = builder.Build();
            LicenseOptions options = host.Services.GetRequiredService<IOptions<LicenseOptions>>().Value;

            options.LicensePath.ShouldBe(Path.Combine(root, "configured/license.pocok"));
            options.TrustedPublicKeyFiles["test"].ShouldBe(Path.Combine(root, "keys/public.pem"));
            options.FailureBehavior.ShouldBe(LicenseFailureBehavior.Warn);
            options.RequiredModules.ShouldBe(["Reporting"]);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ApplyingDefaultsTwiceIsRejected()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddPocokLicensingDefaults(options => options.FailureBehavior = LicenseFailureBehavior.Warn);

        Should.Throw<InvalidOperationException>(() =>
            builder.AddPocokLicensingDefaults(options => options.FailureBehavior = LicenseFailureBehavior.Warn));
    }
}
