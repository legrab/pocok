// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.BackgroundWork;

/// <summary>Provides awaited, non-overlapping repeated execution.</summary>
public static class TaskRepeater
{
    /// <summary>Repeats an operation according to the supplied options.</summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="options">Repeater configuration.</param>
    /// <param name="cancellationToken">Cancels the delay or current operation.</param>
    /// <returns>A task representing the complete repeat lifecycle.</returns>
    public static Task RepeatAsync(
        Func<CancellationToken, ValueTask> operation,
        TaskRepeaterOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.TimeProvider);

        if (options.Interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Interval must be positive.");
        }

        if (options.InitialDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "InitialDelay cannot be negative.");
        }

        if (options.MaximumIterations is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaximumIterations must be positive when supplied.");
        }

        BackgroundWorkFailure.Validate(options.FailurePolicy, options.OnFailure, nameof(options));
        return RepeatCoreAsync(operation, options, cancellationToken);
    }

    private static async Task RepeatCoreAsync(
        Func<CancellationToken, ValueTask> operation,
        TaskRepeaterOptions options,
        CancellationToken cancellationToken)
    {
        if (options.InitialDelay > TimeSpan.Zero)
        {
            await Task.Delay(options.InitialDelay, options.TimeProvider, cancellationToken).ConfigureAwait(false);
        }

        var iterations = 0;
        while (options.MaximumIterations is null || iterations < options.MaximumIterations.Value)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Exception? failure = null;
            try
            {
                if (options.ShouldContinue is not null && !options.ShouldContinue())
                {
                    return;
                }

                await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            iterations++;

            if (failure is not null)
            {
                if (options.FailurePolicy == BackgroundWorkFailurePolicy.Stop)
                {
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
                }

                await BackgroundWorkFailure.HandleAsync(
                    failure,
                    options.OnFailure!,
                    cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (options.MaximumIterations is not null && iterations >= options.MaximumIterations.Value)
            {
                return;
            }

            await Task.Delay(options.Interval, options.TimeProvider, cancellationToken).ConfigureAwait(false);
        }
    }
}
