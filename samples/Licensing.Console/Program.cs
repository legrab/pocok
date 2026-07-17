// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pocok.AppDefaults.Licensing;
using Pocok.Licensing.Documents;
using Pocok.Licensing.Runtime;

// Demo only. Production issuers must keep private signing keys outside application deployments.
var (privateKey, publicKey) = LicenseCryptography.CreateSigningKeyPair();
var license = LicenseCryptography.Sign(new LicenseDocument
{
    LicenseId = "sample-license",
    Customer = "Pocok sample",
    IssuedAtUtc = TimeProvider.System.GetUtcNow(),
    Modules = ["Reporting"]
}, "sample", privateKey);

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.AddPocokLicensingDefaults(options =>
{
    options.LicenseText = license;
    options.TrustedPublicKeys["sample"] = publicKey;
    options.RequiredModules = ["Reporting"];
});

using IHost host = builder.Build();
await host.StartAsync();
ILicenseService licenses = host.Services.GetRequiredService<ILicenseService>();
Console.WriteLine($"Reporting licensed: {licenses.HasModule("Reporting")}");
await host.StopAsync();
