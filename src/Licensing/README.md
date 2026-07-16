# Pocok.Licensing

Offline-first licensing primitives for .NET 10 applications.

```csharp
services.AddPocokLicensing(options =>
{
    options.LicensePath = "license.pocok";
    options.TrustedPublicKeyFiles["production-2026"] = "license-public.pem";
});

ILicenseService licenses = provider.GetRequiredService<ILicenseService>();
LicenseValidationResult result = await licenses.RefreshAsync(cancellationToken: cancellationToken);
licenses.Demand("Export");
```

Licenses are canonical JSON claims signed with ECDSA P-256. Applications receive public keys only. Optional
PBKDF2-SHA256 plus AES-256-GCM wrapping hides license contents but never replaces signature verification. Claims support
modules, inclusive start and exclusive end dates, maximum process runtime, machine fingerprints, and a license-scoped
pre-shared-key digest.

Registration and `Validate` perform no license-file I/O. `RefreshAsync` reads license and public-key files with
cancellation;
subsequent validation is synchronous and safe for concurrent callers. Before the first refresh, validation returns
`Missing`. Expected rejection returns `LicenseValidationResult`; `Demand` converts a failed result into
`LicenseException`.

The package is an offline deterrent, not an unbreakable DRM boundary. Keep private signing keys outside client
deployments
and place operation-level demands near valuable work. See [
`docs/licensing.md`](https://github.com/legrab/pocok/blob/main/docs/licensing.md) for the complete
workflow, threat model, security semantics, machine identity behavior, migration notes, and release gate.
