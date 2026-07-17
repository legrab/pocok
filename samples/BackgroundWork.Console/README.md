# BackgroundWork console sample

This runnable sample demonstrates the four core `Pocok.BackgroundWork` behaviors without adding a hosting framework:

- guarded observation of an intentionally non-awaited `Task<T>`;
- coalescing multiple triggers into one active execution and one pending rerun;
- debouncing a burst into one execution after the quiet period;
- repeating asynchronous work without overlapping iterations.

Run from the repository root:

```pwsh
dotnet run --project samples/BackgroundWork.Console/Pocok.BackgroundWork.Console.csproj
```

Expected output:

```text
observed=42 coalesced=2 debounced=1 repeated=2
```

The process exits with code `0` only when every demonstrated contract is satisfied.

## What the sample proves

The observed task completes successfully and its filtered success callback stores the value `42`.

Three coalescing requests are made while the first operation is blocked. The first request owns the active execution,
while the later requests share one pending rerun. The operation therefore executes exactly twice.

Two debounce requests arrive inside the same quiet period and collapse into one execution.

The repeater executes exactly two non-overlapping iterations through `MaximumIterations`.

The short timing values keep the sample fast. Production code should use intervals appropriate to the workload, pass
cancellation tokens from the owning lifecycle, and provide explicit fault handling for intentionally detached tasks.
