// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Signals.Operations;

/// <summary>
///     Stable error codes produced by the shared signal runtime.
/// </summary>
public static class SignalRuntimeErrorCodes
{
    /// <summary>The source factory could not provide a source.</summary>
    public const string SourceUnavailable = "signals.runtime.source-unavailable";

    /// <summary>The returned source identity did not match the requested address.</summary>
    public const string SourceMismatch = "signals.runtime.source-mismatch";

    /// <summary>The source does not support subscriptions.</summary>
    public const string SubscribeUnsupported = "signals.runtime.subscribe-unsupported";

    /// <summary>The source does not support point-in-time reads.</summary>
    public const string ReadUnsupported = "signals.runtime.read-unsupported";

    /// <summary>The underlying point-in-time read failed.</summary>
    public const string ReadFailed = "signals.runtime.read-failed";

    /// <summary>The source does not support writes.</summary>
    public const string WriteUnsupported = "signals.runtime.write-unsupported";

    /// <summary>The source returned weaker write evidence than requested.</summary>
    public const string WriteConsistencyNotMet = "signals.runtime.write-consistency-not-met";

    /// <summary>The underlying source write failed.</summary>
    public const string WriteFailed = "signals.runtime.write-failed";

    /// <summary>The typed value converter failed unexpectedly.</summary>
    public const string ConversionFailed = "signals.runtime.conversion-failed";

    /// <summary>The underlying source subscription failed.</summary>
    public const string SubscriptionFailed = "signals.runtime.subscription-failed";
}
