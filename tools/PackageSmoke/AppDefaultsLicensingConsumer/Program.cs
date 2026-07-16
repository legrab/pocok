// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pocok.AppDefaults.Licensing;
using Pocok.Licensing;

(string privateKey, string publicKey) = LicenseCryptography.CreateSigningKeyPair();
string license = LicenseCryptography.Sign(new LicenseDocument
{
    LicenseId = "smoke",
    IssuedAtUtc = TimeProvider.System.GetUtcNow(),
    Modules = ["Smoke"]
}, "smoke", privateKey);

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.AddPocokLicensingDefaults(options =>
{
    options.LicenseText = license;
    options.TrustedPublicKeys["smoke"] = publicKey;
    options.RequiredModules = ["Smoke"];
});
using IHost host = builder.Build();
await host.StartAsync();
bool valid = host.Services.GetRequiredService<ILicenseService>().Current.IsValid;
await host.StopAsync();
return valid ? 0 : 1;
