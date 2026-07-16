// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Licensing;

/// <summary>Specifies host behavior after license validation fails.</summary>
public enum LicenseFailureBehavior
{
    /// <summary>Log the failure and allow the host to continue.</summary>
    Warn,

    /// <summary>Fail startup or stop the running host.</summary>
    Block
}
