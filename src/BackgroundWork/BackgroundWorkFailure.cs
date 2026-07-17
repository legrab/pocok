// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.BackgroundWork;

internal static class BackgroundWorkFailure
{
    internal static void Validate(
        BackgroundWorkFailurePolicy policy,
        Func<Exception, CancellationToken, ValueTask>? handler,
        string parameterName)
    {
        if (!Enum.IsDefined(policy))
        {
            throw new ArgumentOutOfRangeException(parameterName, policy, "Unknown failure policy.");
        }

        if (policy == BackgroundWorkFailurePolicy.Continue && handler is null)
        {
            throw new ArgumentException("Continue failure policy requires an OnFailure handler.", parameterName);
        }
    }

    internal static async ValueTask HandleAsync(
        Exception operationException,
        Func<Exception, CancellationToken, ValueTask> handler,
        CancellationToken cancellationToken)
    {
        try
        {
            await handler(operationException, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception handlerException)
        {
            throw new AggregateException(operationException, handlerException);
        }
    }
}
