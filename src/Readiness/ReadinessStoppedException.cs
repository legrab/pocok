// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Readiness;

/// <summary>
///     Reports that shutdown interrupted a pending readiness wait.
/// </summary>
public sealed class ReadinessStoppedException : Exception
{
    /// <summary>
    ///     Initializes the exception.
    /// </summary>
    public ReadinessStoppedException()
        : base("The readiness cycle stopped before becoming ready.")
    {
    }
}
