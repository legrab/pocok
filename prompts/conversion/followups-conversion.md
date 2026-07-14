# Follow-ups: Conversion

## 🟢 LOW: Add a serializer adapter only for a proven consumer

- **Finding**: Some earlier consumers converted JSON strings or arbitrary object graphs through an implicit Newtonsoft.Json fallback.
- **Impact**: Carrying that behavior into the core would make every consumer inherit serializer policy, dependencies, and security-sensitive object construction.
- **Proposal**: When a concrete consumer needs the behavior, plan `Pocok.Conversion.NewtonsoftJson` with explicit opt-in registration, serializer settings, allowlisted target types, and separate contract/security tests.
- **Why deferred**: P1 requires a serializer-free core, and no standalone public consumer currently establishes the adapter contract.

## 🟢 LOW: Investigate byte-for-byte reproducible NuGet archives

- **Finding**: Two packs from the same sources produce different raw `.nupkg` hashes because NuGet regenerates OPC relationship and core-property metadata. Entry names and contents are identical after excluding `_rels/.rels` and `package/services/metadata/core-properties/*`.
- **Impact**: The compiled assemblies, documentation, and other meaningful package payloads are deterministic, but raw archive hashes cannot currently be compared between independent pack invocations.
- **Proposal**: Determine whether the packaging toolchain can stabilize the generated OPC metadata. If not, add a maintained normalized-payload comparator to release validation and document precisely which metadata entries it excludes.
- **Why deferred**: The public package audit, Source Link inspection, symbols, dependency allowlist, and normalized payload comparison pass; byte-identical ZIP wrappers are release hardening rather than an alpha API blocker.
