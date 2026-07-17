// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.BackgroundWork;

var observedValue = 0;
TaskObservation observation = Task.FromResult(42).Observe(
    onFault: exception => Console.Error.WriteLine(exception),
    configure: options => options.OnSuccess(
        value => value == 42,
        value => observedValue = value));
TaskObservationResult observationResult = await observation.Completion;

var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var releaseFirstRun = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var coalescedExecutions = 0;
await using (var runner = new CoalescingTaskRunner(async _ =>
{
    var execution = Interlocked.Increment(ref coalescedExecutions);
    if (execution == 1)
    {
        firstRunStarted.SetResult();
        await releaseFirstRun.Task;
    }
}))
{
    Task first = runner.RequestAsync();
    await firstRunStarted.Task;
    Task second = runner.RequestAsync();
    Task third = runner.RequestAsync();
    releaseFirstRun.SetResult();
    await Task.WhenAll(first, second, third);
}

var debouncedExecutions = 0;
await using (var runner = new DebouncedTaskRunner(
    _ =>
    {
        debouncedExecutions++;
        return ValueTask.CompletedTask;
    },
    new DebouncedTaskRunnerOptions { QuietPeriod = TimeSpan.FromMilliseconds(10) }))
{
    Task first = runner.RequestAsync();
    Task second = runner.RequestAsync();
    await Task.WhenAll(first, second);
}

var repeatedExecutions = 0;
await TaskRepeater.RepeatAsync(
    _ =>
    {
        repeatedExecutions++;
        return ValueTask.CompletedTask;
    },
    new TaskRepeaterOptions
    {
        Interval = TimeSpan.FromMilliseconds(1),
        MaximumIterations = 2
    });

Console.WriteLine(
    $"observed={observedValue} coalesced={coalescedExecutions} " +
    $"debounced={debouncedExecutions} repeated={repeatedExecutions}");

return observationResult.Outcome == TaskObservationOutcome.Succeeded &&
       observationResult.ObserverException is null &&
       observedValue == 42 &&
       coalescedExecutions == 2 &&
       debouncedExecutions == 1 &&
       repeatedExecutions == 2
    ? 0
    : 1;
