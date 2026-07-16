// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Licensing;

internal sealed record LicenseEnvelope
{
    internal const string CurrentFormat = "pocok-license/v1";
    internal const string CurrentAlgorithm = "ECDSA-P256-SHA256-P1363";

    public required string Format { get; init; }
    public required string Algorithm { get; init; }
    public required string KeyId { get; init; }
    public required string Payload { get; init; }
    public required string Signature { get; init; }
}
