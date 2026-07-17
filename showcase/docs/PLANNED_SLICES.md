# Planned package slices

The permanent host architecture and the `Pocok.Conversion`, `Pocok.Scripting`, and `Pocok.Licensing` slices are implemented. The immutable package catalog keeps these other current non-retired packages visible in the collapsed **Coming soon** section:

1. `Pocok.Readiness`
2. `Pocok.AppDefaults`
3. `Pocok.AppDefaults.Logging`
4. `Pocok.AppDefaults.Logging.Serilog`
5. `Pocok.Modularity.Contracts`
6. `Pocok.Modularity`
7. `Pocok.AppDefaults.Modularity`
8. `Pocok.BackgroundWork`
9. `Pocok.Localization`
10. `Pocok.Signals`
11. `Pocok.Subscriptions`
12. `Pocok.AppDefaults.Licensing`

Each slice must remain package-owned, use the shared execution and UI contracts, and pass publication plus readiness before another slice is added. The Licensing slice demonstrates claim validation and links to the broader signing, encryption, key-generation, and license-checker workflows rather than exposing private-key operations in the public deployment. Retired packages, tests, benchmarks, and release tools are not showcase targets.
