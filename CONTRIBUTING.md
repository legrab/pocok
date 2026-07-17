# Contributing

Open an issue before substantial API or package-boundary work. Small corrections may go directly to a pull request.

Contributions should:

- solve a general problem without application-specific contracts;
- preserve explicit null, cancellation, culture, time, concurrency, and ownership semantics;
- include focused tests and update public documentation;
- avoid adding dependencies when the framework is sufficient;
- pass build, test, packaging, and public-content audits.

By contributing, you license your contribution under Apache License 2.0 and confirm that you have the right to do so.
## Continuous integration

Pull requests use dependency-aware affected validation. Review the generated impact summary when a change selects more or less work than expected. Add the `ci:full` label when complete validation is warranted; pushes to `main` are always complete.

Run the repository-owned planning tests before changing project, package, sample, test, or CI metadata:

```pwsh
./tools/Ci/Test-CiTooling.ps1
./tools/Ci/Resolve-CiImpact.ps1 -ChangedFiles 'src/Conversion/ValueConverter.cs'
```

See [`docs/ci.md`](docs/ci.md) for impact semantics, local affected validation, coverage comparison, and discoverability conventions.

