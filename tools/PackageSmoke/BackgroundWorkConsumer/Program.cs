// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.BackgroundWork.Coalescing;
using Pocok.BackgroundWork.Debouncing;
using Pocok.BackgroundWork.Observation;
using Pocok.BackgroundWork.Repetition;

bool successObserved = false;
TaskObservation success = Task.CompletedTask.Observe(
    onFault: _ => { },
    configure: options => options.OnSuccess(() => successObserved = true));
TaskObservationResult successResult = await success.Completion;

bool faultObserved = false;
TaskObservation fault = Task.FromException(new InvalidOperationException("expected")).Observe(
    onFault: _ => faultObserved = true);
TaskObservationResult faultResult = await fault.Completion;

int coalescedExecutions = 0;
await using (var runner = new CoalescingTaskRunner(_ =>
{
    coalescedExecutions++;
    return ValueTask.CompletedTask;
}))
{
    await runner.RequestAsync();
}

int debouncedExecutions = 0;
await using (var runner = new DebouncedTaskRunner(
    _ =>
    {
        debouncedExecutions++;
        return ValueTask.CompletedTask;
    },
    new DebouncedTaskRunnerOptions { QuietPeriod = TimeSpan.FromMilliseconds(1) }))
{
    await runner.RequestAsync();
}

int repeatedExecutions = 0;
await TaskRepeater.RepeatAsync(
    _ =>
    {
        repeatedExecutions++;
        return ValueTask.CompletedTask;
    },
    new TaskRepeaterOptions
    {
        Interval = TimeSpan.FromMilliseconds(1),
        MaximumIterations = 1
    });

return successObserved &&
       successResult.Outcome == TaskObservationOutcome.Succeeded &&
       faultObserved &&
       faultResult.Outcome == TaskObservationOutcome.Faulted &&
       coalescedExecutions == 1 &&
       debouncedExecutions == 1 &&
       repeatedExecutions == 1
    ? 0
    : 1;
