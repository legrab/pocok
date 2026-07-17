// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Licensing.Documents;

internal sealed record EncryptedLicenseEnvelope
{
    internal const string CurrentFormat = "pocok-license-encrypted/v1";
    internal const string CurrentAlgorithm = "PBKDF2-SHA256+A256GCM";

    public required string Format { get; init; }
    public required string Algorithm { get; init; }
    public required int KdfIterations { get; init; }
    public required string Salt { get; init; }
    public required string Nonce { get; init; }
    public required string Ciphertext { get; init; }
    public required string Tag { get; init; }
}
