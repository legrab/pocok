# Licensing

## Purpose and security boundary

Pocok licensing provides offline entitlement enforcement for .NET applications distributed without source code. It is
intended to stop accidental redistribution, casual copying, expired use, and uncomplicated binary patching. It does not
make a managed client uncrackable. An attacker who controls the process can rewrite IL, replace assemblies, hook methods,
or remove every local guard.

The useful boundary is therefore layered and explicit:

1. The issuer signs claims with an ECDSA private key that never ships with the application.
2. The application trusts one or more public keys and verifies every loaded license.
3. AppDefaults rejects startup or logs a warning, then reloads and revalidates periodically.
4. Business operations call `Demand` close to privileged work instead of relying only on startup state.
5. `[RequiresLicense]` provides discoverable metadata for application-owned endpoint filters, interceptors, or command
   pipelines. It does not silently scan assemblies or create proxy objects.

Do not corrupt or dispose the application service provider as an enforcement trick. That creates nondeterministic failure,
can damage unrelated work, and still gives an attacker one obvious branch to patch. Fail-fast startup, observable host
shutdown, and repeated operation-level guards are simpler to test and harder to bypass accidentally.

## Package layout

| Project | Role | Publication state |
|---|---|---|
| `Pocok.Licensing` | License models, signing, optional encryption, verification, runtime validation, DI service, guards | Experimental until the repository release gate passes |
| `Pocok.AppDefaults.Licensing` | Configuration binding, startup enforcement, periodic reload and enforcement | Experimental until the repository release gate passes |
| `Pocok.Licensing.Keygen` | Non-packaged issuer and machine-fingerprint CLI | Repository tool |
| `Pocok.Licensing.LicenseChecker` | Non-packaged standalone diagnostic checker | Repository tool |

The runnable `samples/Licensing.Console` project demonstrates host integration. It creates an ephemeral private key only to
keep the sample self-contained. Production applications must never contain issuer private keys.

## License format and cryptography

A license is a versioned JSON envelope containing a canonical JSON payload and an ECDSA P-256 signature using SHA-256 and
the fixed-width IEEE P1363 signature format. The envelope contains a key identifier so applications can trust old and new
public keys during rotation.

Optional confidentiality uses a second versioned envelope:

- PBKDF2-SHA256 derives a 256-bit key from the distribution secret and a random 16-byte salt;
- AES-256-GCM uses a random 12-byte nonce and a 16-byte authentication tag;
- the format, algorithm, and KDF iteration count are authenticated as associated data;
- the signed inner envelope is always verified after decryption.

Encryption is not the authenticity boundary. A decryption secret embedded in a client can eventually be recovered. Use it
only when hiding customer names, module lists, or dates from casual inspection is useful. The signature is what prevents a
customer from issuing or modifying licenses.

The reader rejects unknown fields, unsupported format or algorithm identifiers, excessive payload sizes, malformed Base64,
non-P-256 keys, wrong signature sizes, invalid signatures, and invalid claim invariants. Expected failures return
`LicenseValidationResult`; invalid caller arguments and broken configuration throw.

## Claim semantics

A license can combine any of these restrictions:

- `AllModules`, or a case-insensitive set of explicit `Modules`;
- `ValidFromUtc`, inclusive;
- `ValidUntilUtc`, exclusive;
- `MaximumProcessRuntime`, where the exact limit is valid and a greater elapsed runtime fails;
- zero or more 64-character SHA-256 `MachineFingerprints`;
- a license-scoped HMAC-SHA256 digest of a high-entropy pre-shared key;
- non-secret metadata for issuer bookkeeping.

Maximum process runtime resets when the process restarts. It is not a consumable duration across executions. Offline
absolute dates are vulnerable to system-clock rollback. Persisted runtime budgets, rollback-resistant checkpoints,
revocation, floating seats, and online leases require a later stateful or online extension.

## Issuer workflow

Keep issuer material under `.local/`, a secret store, an HSM, or a dedicated issuance service. `.local/` is ignored by Git.
The repository CLI refuses to overwrite keys or licenses unless `--force` is supplied.

```powershell
# Create an ECDSA P-256 key pair. Only the public key is deployed with applications.
dotnet run --project src/Licensing.Keygen -- keys `
  --private .local/license-private.pem `
  --public .local/license-public.pem

# Generate a high-entropy Base64 secret for PSK or optional envelope encryption.
dotnet run --project src/Licensing.Keygen -- secret --bytes 32

# Obtain the fingerprint for the current machine.
dotnet run --project src/Licensing.Keygen -- machine

# Issue a signed license. Prefer secret files so secrets do not enter shell history.
dotnet run --project src/Licensing.Keygen -- issue `
  --private .local/license-private.pem `
  --key-id production-2026 `
  --id ACME-001 `
  --customer ACME `
  --module Reporting `
  --module Export `
  --valid-from 2026-08-01T00:00:00Z `
  --valid-until 2027-08-01T00:00:00Z `
  --max-runtime 30.00:00:00 `
  --machine <fingerprint> `
  --psk-file .local/runtime-psk.txt `
  --encrypt-secret-file .local/distribution-secret.txt `
  --metadata contract=2026-42 `
  --out .local/license.pocok
```

`--all-modules` replaces explicit module restrictions. `--machine`, `--module`, and `--metadata` are repeatable. Use
`--issued-at` only for deterministic diagnostics or migration tooling. Private keys, PSKs, and encryption secrets must not
be written to logs, committed, or copied into application binaries unless the chosen feature inherently requires the
client to possess that secret.

## Standalone checking

The checker validates the signature and all requested runtime constraints. Without `--machine`, it uses the current
machine. Multiple public keys can be supplied as `keyId=path` values.

```powershell
dotnet run --project src/Licensing.LicenseChecker -- `
  --license .local/license.pocok `
  --public production-2026=.local/license-public.pem `
  --module Reporting `
  --psk-file .local/runtime-psk.txt `
  --decrypt-secret-file .local/distribution-secret.txt
```

Diagnostic overrides are available through `--machine`, `--utc-now`, and `--runtime`. `--json` returns a small
machine-readable result without license metadata or secrets. Exit codes are `0` for valid, `2` for invocation or I/O
failure, and `3` for a rejected license.

## Direct runtime integration

`Pocok.Licensing` performs no hidden file I/O during registration, construction, or `Validate`. Explicitly refresh once at
application startup. File reads are asynchronous and cancellable. Validation after loading is synchronous, serialized by
the service, and safe for concurrent callers.

```csharp
builder.Services.AddPocokLicensing(options =>
{
    options.LicensePath = "license.pocok";
    options.TrustedPublicKeyFiles["production-2026"] = "license-public.pem";
    options.PresharedKey = configuration["Licensing:PresharedKey"];
    options.DecryptionSecret = configuration["Licensing:DecryptionSecret"];
});

ILicenseService licenses = services.GetRequiredService<ILicenseService>();
LicenseValidationResult startup = await licenses.RefreshAsync(cancellationToken: stoppingToken);
if (!startup.IsValid) throw new LicenseException(startup);

licenses.Demand("Export");
bool reportingAvailable = licenses.HasModule("Reporting");
```

Inline `TrustedPublicKeys` override entries with the same key identifier loaded from `TrustedPublicKeyFiles`. Inline
`LicenseText` takes precedence over `LicensePath`. Before the first `RefreshAsync`, `Validate` returns `Missing`,
`HasModule` returns false, and `Demand` throws.

## AppDefaults integration

`Pocok.AppDefaults.Licensing` binds `Pocok:Licensing`, resolves relative license and public-key paths against the host content
root, applies an optional delegate last, validates options, registers licensing, and adds one hosted enforcement service.
Applying the defaults twice throws.

```csharp
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddPocokLicensingDefaults(options =>
{
    options.RequiredModules = ["Core"];
    options.FailureBehavior = LicenseFailureBehavior.Block;
});
```

Equivalent configuration:

```json
{
  "Pocok": {
    "Licensing": {
      "LicensePath": "license.pocok",
      "TrustedPublicKeyFiles": {
        "production-2026": "keys/license-public.pem"
      },
      "RequiredModules": ["Core"],
      "FailureBehavior": "Block",
      "RevalidationInterval": "00:05:00",
      "BlockingExitCode": 78
    }
  }
}
```

At startup, `Block` assigns `BlockingExitCode` and throws `LicenseException`, which prevents later hosted services from
starting. `Warn` logs and allows startup. During execution, the hosted service reloads the license and public keys at each
`RevalidationInterval`; `Block` assigns the exit code and requests host shutdown, while `Warn` logs and continues.
`BlockingExitCode` must be from 1 through 255 so it remains meaningful on Windows and Unix hosts. Configuration secrets
should come from environment variables or the application's secret provider, not committed JSON.

## Operation-level enforcement

A startup check is not sufficient. Guard meaningful work in more than one application-owned path where the value justifies
it, for example an API endpoint and the service that performs the export:

```csharp
public sealed class ReportExporter(ILicenseService licenses)
{
    public Task ExportAsync(CancellationToken cancellationToken)
    {
        licenses.Demand("Export");
        return ExportCoreAsync(cancellationToken);
    }
}
```

Attributes are metadata only:

```csharp
[RequiresLicense("Export")]
public Task<IResult> ExportAsync() => ...;

// In an existing endpoint filter, interceptor, or command pipeline:
licenses.DemandFor(methodInfo);
```

This avoids mandatory proxy frameworks, reflection scanning, and service locators. Applications that already own an
ASP.NET Core endpoint filter, MediatR behavior, interception layer, or command pipeline can call `DemandFor` there.

## Machine binding

`DefaultMachineFingerprintProvider` hashes a versioned input containing:

- `POCOK_MACHINE_ID` when present;
- otherwise Windows `MachineGuid`, Linux `machine-id`, or the machine name fallback;
- OS platform and process OS architecture context.

The raw identifier is never placed in the license. Containers, macOS, restricted hosts, cloned images, and environments
where machine identity must survive infrastructure changes should set a stable application-owned `POCOK_MACHINE_ID` or
replace `IMachineFingerprintProvider`. Changing the identifier invalidates machine-bound licenses by design.

## Key lifecycle and deployment

- Keep private signing keys outside client repositories, builds, and deployments.
- Back up private keys securely. Losing one prevents issuing compatible licenses; leaking one requires rotation.
- Rotate by issuing with a new `KeyId` while applications temporarily trust both public keys.
- Remove the old public key only after every valid old license is replaced or expired.
- Sign application binaries and packages independently. License signatures do not prove deployment provenance.
- Obfuscation, ReadyToRun, trimming, and Native AOT can raise reverse-engineering cost after compatibility testing, but
  none is an authorization boundary.
- Keep valuable server-controlled operations server-side when feasible. Local-only enforcement cannot defeat a determined
  process owner.

## Migration from origin

The original `Common.Licensing`, `Licensing.Keygen`, and `Licensing.LicenseChecker` concepts were retained where
useful: module entitlements, absolute validity, process uptime, machine identity, issuance, and standalone checking.

The old trust model is intentionally not wire-compatible. Existing licenses must be reissued because this implementation:

- replaces a shared symmetric authenticity secret with issuer-only ECDSA private keys;
- replaces fixed-salt, unauthenticated AES-CBC with random-salt PBKDF2 and authenticated AES-GCM;
- removes global static initialization and one global validation result;
- separates loading, cryptographic verification, runtime evaluation, host policy, and operation-level demands;
- uses stable result codes, strict versioned envelopes, bounded parsing, key rotation, DI, and `TimeProvider`;
- makes startup blocking real and supports periodic file reload.

No historical company, customer, or proprietary source material is copied into the public package.

## Compatibility, nullability, cancellation, and threading

- Target framework: `net10.0`.
- Nullable reference types and warnings-as-errors are enabled repository-wide.
- `RefreshAsync` accepts `CancellationToken`; cancellation propagates and does not replace the last completed state.
- Runtime validation and `Current` are synchronized. Concurrent validation is supported.
- `LicenseOptions` are resolved once during registration. Mutating the original options instance afterwards is unsupported.
- License and public-key files are re-read only by `RefreshAsync` or AppDefaults periodic enforcement.
- The default machine provider performs bounded local platform reads and can be replaced through DI.
- Expected license rejection does not throw. `Demand`, invalid arguments, invalid configuration, and cryptographic issuer
  misuse can throw.

## Release gate

The catalog remains `Experimental` and non-releasable until all executable proof succeeds on the receiving machine and CI:

```powershell
pwsh ./tools/PackageCatalog/Test-PackageCatalog.ps1
pwsh ./tools/PackageMetadata/Test-PackageMetadata.ps1
dotnet restore Pocok.slnx
dotnet format Pocok.slnx --verify-no-changes --no-restore
dotnet build Pocok.slnx -c Release --no-restore
dotnet test Pocok.slnx -c Release --no-build
pwsh ./tools/PackageSmoke/Invoke-PackageSmoke.ps1 -Mode LocalClosure -PackageIds `
  Pocok.Licensing,Pocok.AppDefaults.Licensing
pwsh ./tools/PackageSmoke/Invoke-PackageSmoke.ps1 -Mode Publication -PackageIds Pocok.Licensing
```

Also verify keygen-to-checker behavior on Windows and Linux, package contents, symbols, Source Link, trimming warnings, and
Native AOT only if those deployment modes are claimed. Publication-shaped smoke for `Pocok.AppDefaults.Licensing` can run
only after its internal `Pocok.Licensing` dependency is published to the configured public feed. Publication state must not
be changed merely because static review passes.

## Deliberately deferred extensions

- ASP.NET Core-specific endpoint-filter and middleware convenience package;
- source generator that validates `[RequiresLicense]` placement at compile time;
- signed online leases, revocation, seats, and rollback-resistant persisted checkpoints;
- issuer adapters for cloud KMS, HSM, PKCS#11, and certificate stores;
- cross-process or cross-restart consumable runtime budgets;
- a tested trimming and Native AOT compatibility tier.
