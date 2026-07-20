# Showcase package coverage

The current local and canonical Docker publication contains ten package-owned plugins covering all eighteen non-retired
library packages. No current catalog package belongs in a **Coming soon** section when complete-catalog mode is enabled.

| Plugin slug | Covered package IDs | Mode |
|---|---|---|
| `app-defaults-logging` | `Pocok.AppDefaults`; `Pocok.AppDefaults.Logging`; `Pocok.AppDefaults.Logging.Serilog` | Bounded real demonstration |
| `background-work` | `Pocok.BackgroundWork` | Typed recipe builder |
| `conversion` | `Pocok.Conversion` | Bounded real package path |
| `licensing` | `Pocok.Licensing`; `Pocok.AppDefaults.Licensing` | Bounded validation demonstration and host-policy guidance |
| `localization` | `Pocok.Localization` | Bounded real package path |
| `modularity` | `Pocok.Modularity.Contracts`; `Pocok.Modularity`; `Pocok.AppDefaults.Modularity` | Typed recipe builder |
| `readiness` | `Pocok.Readiness` | Typed recipe builder |
| `scripting` | `Pocok.Scripting`; `Pocok.Scripting.JavaScript`; `Pocok.Scripting.CSharp`; `Pocok.Scripting.Python` | JavaScript public; C# and Python trusted-local only |
| `signals` | `Pocok.Signals` | Typed recipe builder |
| `subscriptions` | `Pocok.Subscriptions` | Typed recipe builder |

Multi-package coverage is declared in `pocok.module.json` through `coveredPackageIds` and validated against the generated
package catalog. A future non-retired package may appear as planned only when its catalog entry is intentionally added
before its plugin; complete-catalog publication must reject that incomplete state.

Each plugin remains package-owned, uses shared execution and UI contracts, and passes publication plus startup validation.
The Licensing plugin demonstrates claim validation and links to the broader signing, encryption, key-generation, and
license-checker workflows rather than exposing private-key operations in the public deployment. Retired packages, tests,
benchmarks, and release tools are not Showcase targets.
