# Pocok.Scripting Showcase slice

This trusted Showcase plugin exercises the engine-neutral `Pocok.Scripting` runner with the JavaScript, C#, and Python adapters.

JavaScript is available in the public Showcase. C# and Python are registered as truthful unavailable descriptors unless the operator explicitly enables `Showcase__TrustedScriptEnginesEnabled=true` in a private or local deployment and supplies the required runtime paths. No browser-controlled setting can widen that boundary.

Every conceptual sample provides equivalent JavaScript, C#, and Python source. The page keeps independent circuit-local source for each engine, flushes pending editor content before Run, engine changes, and sample resets, and displays only limits reported by the selected adapter.

The shared Monaco wrapper is internal to the Showcase. It serves pinned local assets, uses engine-aware syntax and bounded completion records, and falls back to the existing buffered textarea when initialization or JavaScript interop fails.

Validation, process separation, timeouts, and resource bounds are guardrails. They are not an operating-system sandbox. The plugin does not expose filesystem, network, reflection, process, service-provider, secret, or arbitrary host-object capabilities.

## Trusted local proof

After building the worker and with CPython 3.14 installed, configure trusted execution explicitly before starting the Showcase:

```powershell
$env:Showcase__TrustedScriptEnginesEnabled = 'true'
$env:DOTNET_HOST_PATH = (Get-Command dotnet).Source
$env:POCOK_CSHARP_WORKER_DIRECTORY = (Resolve-Path 'src/Scripting.CSharp.Worker/bin/Release/net10.0').Path
$env:POCOK_PYTHON_EXECUTABLE = (Get-Command python).Source
$env:POCOK_PYTHON_WORKER_PATH = (Resolve-Path 'src/Scripting.Python/Worker/pocok_worker.py').Path
```

The public deployment leaves `Showcase__TrustedScriptEnginesEnabled` unset or `false`.
