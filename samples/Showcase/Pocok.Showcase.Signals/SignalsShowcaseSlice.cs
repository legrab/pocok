// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Showcase.Contracts;
using Pocok.Signals.Operations;
using Pocok.Signals.Runtime;
using Pocok.Signals.Sources;
using Pocok.Signals.Writing;

namespace Pocok.Showcase.Signals;

public sealed record SignalsInput
{
    public string SampleId { get; init; } = "read";
    public string Mode { get; init; } = "read";
    public bool IncludeOptional { get; init; }
    public bool IncludeLifecycle { get; init; }
    public int Capacity { get; init; } = 32;
}

public sealed record SignalsOutput(string Code);

public sealed class SignalsShowcaseSlice : ShowcaseSlice<SignalsInput, SignalsOutput>, IShowcasePackageCoverage
{
    private static readonly IReadOnlyList<ShowcaseSample<SignalsInput>> SampleDefinitions =
    [
        new(
            "read",
            "Samples.read.Name",
            "Samples.read.Description",
            () => new SignalsInput
            {
                SampleId = "read",
                Mode = "read",
                Capacity = 32,
                IncludeOptional = false,
                IncludeLifecycle = false
            },
            true,
            "Recipe generated",
            "purpose",
            "recipe"),
        new(
            "write",
            "Samples.write.Name",
            "Samples.write.Description",
            () => new SignalsInput
            {
                SampleId = "write",
                Mode = "write",
                Capacity = 32,
                IncludeOptional = true,
                IncludeLifecycle = false
            },
            false,
            "Recipe generated",
            "purpose",
            "recipe"),
        new(
            "subscribe",
            "Samples.subscribe.Name",
            "Samples.subscribe.Description",
            () => new SignalsInput
            {
                SampleId = "subscribe",
                Mode = "subscribe",
                Capacity = 32,
                IncludeOptional = false,
                IncludeLifecycle = true
            },
            false,
            "Recipe generated",
            "purpose",
            "recipe")
    ];

    public IReadOnlyList<string> CoveredPackageIds { get; } =
    [
        "Pocok.Signals"
    ];

    public override ShowcaseSliceDescriptor Descriptor { get; } = new(
        "pocok.showcase.signals",
        "Pocok.Signals",
        "signals",
        "Capability",
        "Experimental",
        "Package.Name",
        "Package.Summary",
        15,
        "src/Signals/README.md",
        false,
        ShowcaseImplementationStatus.Available,
        "signals",
        "1.0.0");

    public override Type PageComponentType => typeof(SignalsPage);
    public override IReadOnlyList<ShowcaseSample<SignalsInput>> TypedSamples => SampleDefinitions;
    public override ShowcaseGuide Guide { get; } = new(
        [new ShowcaseGuideSection("purpose", "Guide.Purpose.Title", ["Guide.Purpose.Body"], ["recipe"])],
        [
            new ShowcaseCodeSnippet(
                "recipe",
                "Guide.Snippet.Title",
                "csharp",
                "Select a preset and adjust the constrained options.")
        ]);

    public override ValueTask<SignalsOutput> ExecuteAsync(
        SignalsInput input,
        ShowcaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string code = SignalsRecipeRenderer.Render(input);
        context.Output.Write(code);
        return ValueTask.FromResult(new SignalsOutput(code));
    }

    protected override ShowcaseRunResult CreateRunResult(SignalsOutput output, TimeSpan elapsed) =>
        new(
            ShowcaseRunStatus.Success,
            "Recipe generated",
            codePreview: output.Code,
            elapsed: elapsed,
            tipKeys: ["Tips.Generated", "Tips.Ownership"]);
}

public static class SignalsRecipeRenderer
{
    public static IReadOnlyList<string> Modes { get; } = ["read", "write", "subscribe"];

    public static string Render(SignalsInput input)
    {
        string staleAfter = input.IncludeOptional
            ? "TimeSpan.FromSeconds(30)"
            : "null";
        string runtimeDeclaration = input.IncludeLifecycle
            ? "var runtime = new SignalRuntime("
            : "await using var runtime = new SignalRuntime(";
        string runtimeTry = input.IncludeLifecycle ? "\ntry\n{" : string.Empty;
        string connectionDeclaration = input.IncludeLifecycle
            ? "SignalConnection<double> signal = connected.Value!;"
            : "await using SignalConnection<double> signal = connected.Value!;";
        string connectionTry = input.IncludeLifecycle ? "\ntry\n{" : string.Empty;
        string setup = $$"""
            SignalSourceFactory factory = ResolveSourceAsync;
            {{runtimeDeclaration}}
                factory,
                new SignalRuntimeOptions(
                    subscriberCapacity: {{Math.Clamp(input.Capacity, 1, 256)}},
                    reconnectDelay: TimeSpan.FromSeconds(2),
                    staleAfter: {{staleAfter}}));{{runtimeTry}}
            SignalAddress address = new(new SourceId("plant"), "temperature/outlet");
            SignalResult<SignalConnection<double>> connected = await runtime.ConnectAsync<double>(address, cancellationToken);
            if (!connected.IsSuccess) return;
            {{connectionDeclaration}}{{connectionTry}}
            """;
        string operation = input.Mode switch
        {
            "write" => """
                SignalResult<SignalWriteResult<double>> result = await signal.WriteAsync(
                    21.5,
                    SignalWriteConsistency.Acknowledged,
                    cancellationToken);
                """,
            "subscribe" => """
                await foreach (SignalSample<double> sample in signal.Samples(cancellationToken))
                {
                    if (!sample.HasValue || sample.Quality is SignalQuality.Bad or SignalQuality.Failed) continue;
                    Console.WriteLine($"{sample.Value} at {sample.ObservedAt:O}");
                }
                """,
            _ => """
                SignalResult<SignalSample<double>> result = await signal.ReadAsync(cancellationToken);
                if (result.IsSuccess && result.Value!.HasValue)
                    Console.WriteLine($"{result.Value.Value} ({result.Value.Quality})");
                """
        };
        string disposal = input.IncludeLifecycle
            ? "\n}\nfinally\n{\n    await signal.DisposeAsync();\n}\n}\nfinally\n{\n    await runtime.DisposeAsync();\n}"
            : string.Empty;
        const string header = """
            // Install Pocok.Signals.
            using Pocok.Signals.Operations;
            using Pocok.Signals.Runtime;
            using Pocok.Signals.Sources;
            using Pocok.Signals.Writing;

            """;
        return header + setup + "\n" + operation + disposal;
    }

    internal static async Task CompileProofAsync(SignalSourceFactory factory, CancellationToken cancellationToken)
    {
        await using var runtime = new SignalRuntime(factory, new SignalRuntimeOptions());
        SignalAddress address = new(new SourceId("demo"), "value");
        SignalResult<SignalConnection<int>> result = await runtime.ConnectAsync<int>(address, cancellationToken);
        if (!result.IsSuccess) return;
        await using SignalConnection<int> connection = result.Value!;
        _ = await connection.ReadAsync(cancellationToken);
        _ = await connection.WriteAsync(1, SignalWriteConsistency.Acknowledged, cancellationToken);
        _ = connection.Samples(cancellationToken);
    }
}
