// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Hosting;
using Pocok.Scripting.Execution;
using Pocok.Showcase.Scripting;

namespace Pocok.Showcase.Samples.Tests;

[TestFixture]
public sealed class ScriptingRuntimeWarmupBackgroundServiceTests
{
    [Test]
    public async Task HostedWarmupReturnsBeforeApplicationStartsAndProbeCompletes()
    {
        var probe = new BlockingJavaScriptAdapter();
        var registry = new ScriptEngineRegistry(
        [
            probe,
            new UnavailableScriptEngineAdapter(
                ScriptEngineId.CSharp,
                "C#",
                "scripting.engine.trusted_only",
                "C# requires explicit enablement."),
            new UnavailableScriptEngineAdapter(
                ScriptEngineId.Python,
                "Python",
                "scripting.engine.trusted_only",
                "Python requires explicit enablement.")
        ]);
        var coordinator = new ScriptingRuntimeWarmupService(
            new ScriptRunner(registry),
            registry,
            new ScriptingShowcaseOptions());
        var lifetime = new TestApplicationLifetime();
        var hosted = new ScriptingRuntimeWarmupBackgroundService(
            coordinator,
            lifetime);

        try
        {
            await hosted.StartAsync(CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(1));

            probe.Started.Task.IsCompleted.ShouldBeFalse();

            lifetime.SignalStarted();
            await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(1));

            _ = hosted.ExecuteTask.ShouldNotBeNull();
            hosted.ExecuteTask!.IsCompleted.ShouldBeFalse();
        }
        finally
        {
            probe.Release();
            await hosted.StopAsync(CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(1));
        }
    }

    private sealed class BlockingJavaScriptAdapter : IScriptEngineAdapter
    {
        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public BlockingJavaScriptAdapter()
        {
            Descriptor = new ScriptEngineDescriptor(
                ScriptEngineId.JavaScript,
                "JavaScript",
                true,
                new ScriptEngineCapabilities(true, true, true, true, true));
            Validator = new ValidValidator();
        }

        public TaskCompletionSource<bool> Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ScriptEngineDescriptor Descriptor { get; }

        public IScriptValidator Validator { get; }

        public async ValueTask<ScriptResult<object?>> ExecuteAsync(
            ValidatedScript script,
            ScriptExecutionOptions options,
            CancellationToken cancellationToken = default)
        {
            Started.TrySetResult(true);
            await _release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return ScriptResult.Success<object?>(42);
        }

        public void Release()
        {
            _release.TrySetResult(true);
        }

        private sealed class ValidValidator : IScriptValidator
        {
            public ScriptEngineId EngineId => ScriptEngineId.JavaScript;

            public ValueTask<ScriptValidationResult> ValidateAsync(
                ScriptExecutionRequest request,
                ScriptExecutionOptions options,
                CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult(ScriptValidationResult.Valid());
            }
        }
    }

    private sealed class TestApplicationLifetime : IHostApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource _started = new();
        private readonly CancellationTokenSource _stopped = new();
        private readonly CancellationTokenSource _stopping = new();

        public CancellationToken ApplicationStarted => _started.Token;
        public CancellationToken ApplicationStopping => _stopping.Token;
        public CancellationToken ApplicationStopped => _stopped.Token;

        public void StopApplication()
        {
            _stopping.Cancel();
        }

        public void SignalStarted()
        {
            _started.Cancel();
        }

        public void Dispose()
        {
            _stopping.Cancel();
        }
    }
}
