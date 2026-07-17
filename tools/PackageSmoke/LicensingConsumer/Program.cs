// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Pocok.Licensing.Documents;
using Pocok.Licensing.Runtime;

(string privateKey, string publicKey) = LicenseCryptography.CreateSigningKeyPair();
string license = LicenseCryptography.Sign(new LicenseDocument
{
    LicenseId = "smoke",
    IssuedAtUtc = TimeProvider.System.GetUtcNow(),
    Modules = ["Smoke"]
}, "smoke", privateKey);

using ServiceProvider provider = new ServiceCollection()
    .AddPocokLicensing(options =>
    {
        options.LicenseText = license;
        options.TrustedPublicKeys["smoke"] = publicKey;
    })
    .BuildServiceProvider();
return (await provider.GetRequiredService<ILicenseService>().RefreshAsync("Smoke")).IsValid ? 0 : 1;
