# Pocok.Scripting

Compatibility tier: experimental alpha. This package is an extraction of the reusable scripting boundary from the original application and is not release-eligible yet.

The package provides:

- bounded JavaScript execution through Jint;
- no ambient CLR, filesystem, network, service-provider, or browser access;
- explicitly registered scalar values and delegate capabilities;
- cancellation, timeout, recursion, statement, source-size, and memory limits;
- neutral import parsing using the comment form: // #import Name from Module;
- deterministic transitive module resolution with cycle and missing-module diagnostics;
- an in-memory source for tests and small tools;
- typed result conversion through Pocok.Conversion.

The original application's persistence, domain objects, notifications, UI editor bindings, and product-specific script APIs are deliberately excluded. A host must expose each capability explicitly and remains responsible for authenticating and authorizing that capability.

ScriptRunner is instance-based and safe for concurrent use because each request creates a fresh Jint engine. The runner does not retain script state. Cancellation is propagated; expected script failures are returned as ScriptResult<T>.

Example:

    ScriptRunner runner = new();
    ScriptExecutionRequest request = new("sum", "add(20, 22);")
    {
        ExpectResult = true,
        Bindings = [ScriptBinding.ForFunction("add", (Func<int, int, int>)((left, right) => left + right))]
    };

    ScriptResult<int> result = await runner.ExecuteAsync<int>(request);
