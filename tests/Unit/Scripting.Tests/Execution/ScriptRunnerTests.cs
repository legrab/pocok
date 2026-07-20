// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Text.Json;
using Pocok.Scripting.Execution;
using Shouldly;

namespace Pocok.Scripting.Tests.Execution;

[TestFixture]
public sealed class ScriptRunnerTests
{
    private static readonly int[] StructuredValues = [1, 2];
    [Test]
    public async Task ValidatorRunsBeforeAdapter()
    {
        var adapter = new FakeAdapter(valid: false);
        var runner = CreateRunner(adapter);

        ScriptResult<object?> result = await runner.ExecuteAsync(Request(adapter));

        result.IsSuccess.ShouldBeFalse();
        result.Failure!.Code.ShouldBe("test.rejected");
        adapter.Executions.ShouldBe(0);
    }

    [Test]
    public async Task UnknownEngineFailsSafely()
    {
        var runner = new ScriptRunner(new ScriptEngineRegistry([]));

        ScriptResult<object?> result = await runner.ExecuteAsync(
            new ScriptExecutionRequest(new ScriptEngineId("missing"), "test", "42"));

        result.Failure!.Code.ShouldBe("scripting.engine.unknown");
    }

    [Test]
    public async Task UnavailableEngineReturnsDescriptorFailure()
    {
        var adapter = new UnavailableScriptEngineAdapter(
            new ScriptEngineId("offline"),
            "Offline",
            "test.unavailable",
            "The configured runtime is unavailable.");
        var runner = CreateRunner(adapter);

        ScriptResult<object?> result = await runner.ExecuteAsync(Request(adapter));

        result.Failure!.Code.ShouldBe("test.unavailable");
        result.Failure.Message.ShouldBe("The configured runtime is unavailable.");
    }

    [Test]
    public void DuplicateEnginesAreRejected()
    {
        var adapter = new FakeAdapter();

        Should.Throw<ArgumentException>(() => new ScriptEngineRegistry([adapter, adapter]));
    }

    [Test]
    public async Task UnsupportedEngineSpecificLimitFailsBeforeExecution()
    {
        var adapter = new FakeAdapter();
        var runner = CreateRunner(adapter);

        ScriptResult<object?> result = await runner.ExecuteAsync(
            Request(adapter),
            new ScriptExecutionOptions { MaxStatements = 10 });

        result.Failure!.Code.ShouldBe("scripting.limit.unsupported");
        adapter.Executions.ShouldBe(0);
    }

    [Test]
    public async Task MissingCancellationCapabilityFailsBeforeExecution()
    {
        var adapter = new FakeAdapter(capabilities: new(true, false, false, false, false));
        var runner = CreateRunner(adapter);

        ScriptResult<object?> result = await runner.ExecuteAsync(Request(adapter));

        result.Failure!.Code.ShouldBe("scripting.limit.unsupported");
        adapter.Executions.ShouldBe(0);
    }


    [Test]
    public async Task DuplicateBindingsFailBeforeValidation()
    {
        var adapter = new FakeAdapter();
        var runner = CreateRunner(adapter);
        ScriptBinding binding = ScriptBinding.ForValue("value", 1);

        ScriptResult<object?> result = await runner.ExecuteAsync(
            Request(adapter) with { Bindings = [binding, binding] });

        result.Failure!.Code.ShouldBe("scripting.binding.duplicate");
        adapter.Validations.ShouldBe(0);
        adapter.Executions.ShouldBe(0);
    }

    [Test]
    public async Task UnexpectedValidatorExceptionDoesNotLeakDetails()
    {
        var adapter = new FakeAdapter(throwOnValidate: true);
        var runner = CreateRunner(adapter);

        ScriptResult<object?> result = await runner.ExecuteAsync(Request(adapter));

        result.Failure!.Code.ShouldBe("scripting.validation.failed");
        result.Failure.Message.ShouldNotContain("private-validator-path");
        adapter.Executions.ShouldBe(0);
    }

    [Test]
    public async Task StructuredResultIsNormalizedToJsonElement()
    {
        var adapter = new FakeAdapter(result: new { answer = 42, values = StructuredValues });
        var runner = CreateRunner(adapter);

        ScriptResult<object?> result = await runner.ExecuteAsync(Request(adapter));

        result.IsSuccess.ShouldBeTrue();
        var element = result.Value.ShouldBeOfType<JsonElement>();
        element.GetProperty("answer").GetInt32().ShouldBe(42);
        element.GetProperty("values").GetArrayLength().ShouldBe(2);
    }

    [Test]
    public async Task OversizedOutputFailsAfterAdapterExecution()
    {
        var adapter = new FakeAdapter(result: new string('x', 128));
        var runner = CreateRunner(adapter);

        ScriptResult<object?> result = await runner.ExecuteAsync(
            Request(adapter),
            new ScriptExecutionOptions { MaxOutputBytes = 16 });

        result.Failure!.Code.ShouldBe("scripting.output.too_large");
        adapter.Executions.ShouldBe(1);
    }

    [Test]
    public async Task UnexpectedAdapterExceptionDoesNotLeakDetails()
    {
        var adapter = new FakeAdapter(throwOnExecute: true);
        var runner = CreateRunner(adapter);

        ScriptResult<object?> result = await runner.ExecuteAsync(Request(adapter));

        result.Failure!.Code.ShouldBe("scripting.execution.failed");
        result.Failure.Message.ShouldNotContain("private-worker-path");
    }

    [Test]
    public async Task PreCancelledRequestRemainsCancellation()
    {
        var adapter = new FakeAdapter();
        var runner = CreateRunner(adapter);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            runner.ExecuteAsync(Request(adapter), cancellationToken: cancellation.Token).AsTask());
    }

    private static ScriptRunner CreateRunner(IScriptEngineAdapter adapter) =>
        new(new ScriptEngineRegistry([adapter]));

    private static ScriptExecutionRequest Request(IScriptEngineAdapter adapter) =>
        new(adapter.Descriptor.Id, "test", "42") { ExpectResult = true };

    private sealed class FakeAdapter : IScriptEngineAdapter, IScriptValidator
    {
        private readonly bool _valid;
        private readonly object? _result;
        private readonly bool _throwOnExecute;
        private readonly bool _throwOnValidate;

        public FakeAdapter(
            bool valid = true,
            object? result = null,
            bool throwOnExecute = false,
            bool throwOnValidate = false,
            ScriptEngineCapabilities? capabilities = null)
        {
            _valid = valid;
            _result = result ?? 42;
            _throwOnExecute = throwOnExecute;
            _throwOnValidate = throwOnValidate;
            Descriptor = new ScriptEngineDescriptor(
                new ScriptEngineId("fake"),
                "Fake",
                true,
                capabilities ?? new ScriptEngineCapabilities(true, true, false, false, false));
        }

        public int Validations { get; private set; }
        public int Executions { get; private set; }

        public ScriptEngineDescriptor Descriptor { get; }

        public IScriptValidator Validator => this;

        public ScriptEngineId EngineId => Descriptor.Id;

        public ValueTask<ScriptValidationResult> ValidateAsync(
            ScriptExecutionRequest request,
            ScriptExecutionOptions options,
            CancellationToken cancellationToken = default)
        {
            Validations++;
            if (_throwOnValidate)
                throw new InvalidOperationException("private-validator-path");
            return ValueTask.FromResult(_valid
                ? ScriptValidationResult.Valid()
                : ScriptValidationResult.From([new ScriptValidationDiagnostic("test.rejected", "Rejected.")]));
        }

        public ValueTask<ScriptResult<object?>> ExecuteAsync(
            ValidatedScript script,
            ScriptExecutionOptions options,
            CancellationToken cancellationToken = default)
        {
            Executions++;
            if (_throwOnExecute)
                throw new InvalidOperationException("private-worker-path");
            return ValueTask.FromResult(ScriptResult.Success<object?>(_result));
        }
    }
}
