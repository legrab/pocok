# Planned package slices

The permanent host architecture and the `Pocok.Conversion` and `Pocok.Scripting` slices are implemented. The immutable package catalog keeps these other current non-retired packages visible for later implementation, in order:

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
12. `Pocok.Licensing`
13. `Pocok.AppDefaults.Licensing`

Each slice must remain package-owned, use the shared execution and UI contracts, and pass publication plus readiness before another slice is added. Licensing should include the key-generation and license-checker CLI workflows in one guide rather than creating separate pages. Retired packages, tests, benchmarks, and release tools are not showcase targets.
