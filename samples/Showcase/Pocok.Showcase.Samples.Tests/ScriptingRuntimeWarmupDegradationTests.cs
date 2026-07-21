// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.Execution;
using Pocok.Showcase.Scripting;

namespace Pocok.Showcase.Samples.Tests;

[TestFixture]
public sealed class ScriptingRuntimeWarmupDegradationTests
{
    [Test]
    public async Task FailedOptionalProbeDisablesOnlyThatEngine()
    {
        var javaScript = ProbeAdapter.Success(
            ScriptEngineId.JavaScript,
            new ScriptEngineCapabilities(true, true, true, true, true));
        var csharpInner = ProbeAdapter.Failure(
            ScriptEngineId.CSharp,
            "scripting.execution.timeout");
        var pythonInner = ProbeAdapter.Success(ScriptEngineId.Python);
        var csharp = new RuntimeGatedScriptEngineAdapter(csharpInner);
        var python = new RuntimeGatedScriptEngineAdapter(pythonInner);
        var availability = new ScriptingRuntimeAvailability([csharp, python]);
        var registry = new ScriptEngineRegistry([javaScript, csharp, python]);
        var runner = new ScriptRunner(registry);
        var warmup = new ScriptingRuntimeWarmupService(
            runner,
            registry,
            new ScriptingShowcaseOptions
            {
                TrustedEnginesEnabled = true,
                RequireTrustedEnginesAvailable = false
            },
            availability);

        await warmup.StartAsync(CancellationToken.None);

        ScriptEngineDescriptor csharpDescriptor =
            registry.Descriptors.Single(item => item.Id == ScriptEngineId.CSharp);
        csharpDescriptor.IsAvailable.ShouldBeFalse();
        csharpDescriptor.UnavailableCode.ShouldBe("scripting.execution.timeout");
        registry.Descriptors.Single(item => item.Id == ScriptEngineId.JavaScript)
            .IsAvailable.ShouldBeTrue();
        registry.Descriptors.Single(item => item.Id == ScriptEngineId.Python)
            .IsAvailable.ShouldBeTrue();

        ScriptResult<object?> rejected = await runner.ExecuteAsync(
            new ScriptExecutionRequest(
                ScriptEngineId.CSharp,
                "after-warmup",
                "21 * 2"));

        rejected.IsSuccess.ShouldBeFalse();
        rejected.Failure!.Code.ShouldBe("scripting.execution.timeout");
        csharpInner.ExecuteCount.ShouldBe(1);
    }

    [Test]
    public async Task InitiallyUnavailableOptionalEngineCanDegrade()
    {
        var javaScript = ProbeAdapter.Success(
            ScriptEngineId.JavaScript,
            new ScriptEngineCapabilities(true, true, true, true, true));
        var csharp = new RuntimeGatedScriptEngineAdapter(
            new UnavailableScriptEngineAdapter(
                ScriptEngineId.CSharp,
                "C#",
                "scripting.csharp.worker_missing",
                "Private C# worker assets are missing."));
        var python = new RuntimeGatedScriptEngineAdapter(
            ProbeAdapter.Success(ScriptEngineId.Python));
        var availability = new ScriptingRuntimeAvailability([csharp, python]);
        var registry = new ScriptEngineRegistry([javaScript, csharp, python]);
        var warmup = new ScriptingRuntimeWarmupService(
            new ScriptRunner(registry),
            registry,
            new ScriptingShowcaseOptions
            {
                TrustedEnginesEnabled = true,
                RequireTrustedEnginesAvailable = false
            },
            availability);

        await warmup.StartAsync(CancellationToken.None);

        ScriptEngineDescriptor descriptor =
            registry.Descriptors.Single(item => item.Id == ScriptEngineId.CSharp);
        descriptor.IsAvailable.ShouldBeFalse();
        descriptor.UnavailableCode.ShouldBe("scripting.csharp.worker_missing");
    }

    [Test]
    public async Task StrictModeStillFailsWhenOptionalProbeFails()
    {
        var javaScript = ProbeAdapter.Success(
            ScriptEngineId.JavaScript,
            new ScriptEngineCapabilities(true, true, true, true, true));
        var csharp = new RuntimeGatedScriptEngineAdapter(
            ProbeAdapter.Failure(
                ScriptEngineId.CSharp,
                "scripting.execution.timeout"));
        var python = new RuntimeGatedScriptEngineAdapter(
            ProbeAdapter.Success(ScriptEngineId.Python));
        var availability = new ScriptingRuntimeAvailability([csharp, python]);
        var registry = new ScriptEngineRegistry([javaScript, csharp, python]);
        var warmup = new ScriptingRuntimeWarmupService(
            new ScriptRunner(registry),
            registry,
            new ScriptingShowcaseOptions
            {
                TrustedEnginesEnabled = true,
                RequireTrustedEnginesAvailable = true
            },
            availability);

        InvalidOperationException exception =
            await Should.ThrowAsync<InvalidOperationException>(
                async () => await warmup.StartAsync(CancellationToken.None));

        exception.Message.ShouldContain("csharp");
        exception.Message.ShouldContain("scripting.execution.timeout");
    }

    private sealed class ProbeAdapter : IScriptEngineAdapter
    {
        private readonly ScriptResult<object?> _result;

        private ProbeAdapter(
            ScriptEngineId engineId,
            ScriptEngineCapabilities capabilities,
            ScriptResult<object?> result)
        {
            Descriptor = new ScriptEngineDescriptor(
                engineId,
                engineId.Value,
                true,
                capabilities);
            Validator = new ValidValidator(engineId);
            _result = result;
        }

        public int ExecuteCount { get; private set; }

        public ScriptEngineDescriptor Descriptor { get; }

        public IScriptValidator Validator { get; }

        public ValueTask<ScriptResult<object?>> ExecuteAsync(
            ValidatedScript script,
            ScriptExecutionOptions options,
            CancellationToken cancellationToken = default)
        {
            ExecuteCount++;
            return ValueTask.FromResult(_result);
        }

        public static ProbeAdapter Success(
            ScriptEngineId engineId,
            ScriptEngineCapabilities? capabilities = null)
        {
            return new ProbeAdapter(
                engineId,
                capabilities ?? new ScriptEngineCapabilities(true, true, false, false, false),
                ScriptResult.Success<object?>(42));
        }

        public static ProbeAdapter Failure(
            ScriptEngineId engineId,
            string code)
        {
            return new ProbeAdapter(
                engineId,
                new ScriptEngineCapabilities(true, true, false, false, false),
                ScriptResult.Failed<object?>(
                    new ScriptFailure(code, "Probe failed safely.")));
        }
    }

    private sealed class ValidValidator(ScriptEngineId engineId) : IScriptValidator
    {
        public ScriptEngineId EngineId { get; } = engineId;

        public ValueTask<ScriptValidationResult> ValidateAsync(
            ScriptExecutionRequest request,
            ScriptExecutionOptions options,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(ScriptValidationResult.Valid());
        }
    }
}
