// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Security.Cryptography;
using System.Text.Json;

namespace Pocok.Licensing;

/// <summary>Reads, decrypts when requested, and verifies signed license envelopes.</summary>
public static class LicenseReader
{
    /// <summary>Reads and verifies a signed or encrypted license.</summary>
    /// <param name="licenseText">The complete license envelope.</param>
    /// <param name="trustedPublicKeys">Trusted ECDSA P-256 public keys indexed by key identifier.</param>
    /// <param name="decryptionSecret">The optional secret for an encrypted outer envelope.</param>
    /// <returns>A verified license or a stable expected-failure result.</returns>
    public static LicenseValidationResult ReadAndVerify(
        string licenseText,
        IReadOnlyDictionary<string, string> trustedPublicKeys,
        string? decryptionSecret = null)
    {
        ArgumentNullException.ThrowIfNull(trustedPublicKeys);
        if (string.IsNullOrWhiteSpace(licenseText))
            return LicenseValidationResult.Failure(LicenseValidationCode.Missing, "No license content was provided.");
        if (licenseText.Length > LicenseClaimsValidator.MaximumLicenseTextCharacters)
            return LicenseValidationResult.Failure(LicenseValidationCode.Malformed,
                "The license exceeds the supported size limit.");

        try
        {
            using (var probe = JsonDocument.Parse(licenseText))
            {
                if (probe.RootElement.ValueKind != JsonValueKind.Object ||
                    !probe.RootElement.TryGetProperty("format", out JsonElement formatElement) ||
                    formatElement.ValueKind != JsonValueKind.String)
                    return LicenseValidationResult.Failure(LicenseValidationCode.Malformed,
                        "The license envelope is malformed.");

                var format = formatElement.GetString();
                if (format == EncryptedLicenseEnvelope.CurrentFormat)
                {
                    if (string.IsNullOrWhiteSpace(decryptionSecret))
                        return LicenseValidationResult.Failure(
                            LicenseValidationCode.DecryptionSecretRequired,
                            "The license is encrypted but no decryption secret was configured.");

                    EncryptedLicenseEnvelope encrypted =
                        JsonSerializer.Deserialize<EncryptedLicenseEnvelope>(licenseText,
                            LicenseCanonicalizer.JsonOptions) ??
                        throw new JsonException("The encrypted license envelope is empty.");
                    if (encrypted.Algorithm != EncryptedLicenseEnvelope.CurrentAlgorithm)
                        return LicenseValidationResult.Failure(
                            LicenseValidationCode.UnsupportedFormat,
                            "The encrypted license format or algorithm is unsupported.");

                    licenseText = LicenseCryptography.Decrypt(encrypted, decryptionSecret);
                }
                else if (format != LicenseEnvelope.CurrentFormat)
                {
                    return LicenseValidationResult.Failure(
                        LicenseValidationCode.UnsupportedFormat,
                        "The license format is unsupported.");
                }
            }

            LicenseEnvelope envelope =
                JsonSerializer.Deserialize<LicenseEnvelope>(licenseText, LicenseCanonicalizer.JsonOptions) ??
                throw new JsonException("The signed license envelope is empty.");
            if (envelope.Format != LicenseEnvelope.CurrentFormat ||
                envelope.Algorithm != LicenseEnvelope.CurrentAlgorithm)
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.UnsupportedFormat,
                    "The signed license format or algorithm is unsupported.");
            if (!LicenseClaimsValidator.IsIdentifier(envelope.KeyId, 256))
                return LicenseValidationResult.Failure(LicenseValidationCode.Malformed,
                    "The license signing key identifier is invalid.");
            if (!trustedPublicKeys.TryGetValue(envelope.KeyId, out var publicKeyPem) ||
                string.IsNullOrWhiteSpace(publicKeyPem) || publicKeyPem.Length > 65_536)
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.UntrustedSigningKey,
                    $"Signing key '{envelope.KeyId}' is not trusted.");

            var payload = Convert.FromBase64String(envelope.Payload);
            var signature = Convert.FromBase64String(envelope.Signature);
            try
            {
                if (payload.Length == 0 || payload.Length > LicenseClaimsValidator.MaximumPayloadBytes ||
                    signature.Length != 64)
                    return LicenseValidationResult.Failure(LicenseValidationCode.Malformed,
                        "The signed license fields have invalid sizes.");

                using var key = ECDsa.Create();
                key.ImportFromPem(publicKeyPem);
                if (key.KeySize != 256)
                    return LicenseValidationResult.Failure(
                        LicenseValidationCode.UntrustedSigningKey,
                        "The trusted signing key must use ECDSA P-256.");
                if (!key.VerifyData(
                        payload,
                        signature,
                        HashAlgorithmName.SHA256,
                        DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                    return LicenseValidationResult.Failure(
                        LicenseValidationCode.InvalidSignature,
                        "The license signature is invalid.");

                LicenseDocument license = LicenseCanonicalizer.DeserializePayload(payload);
                if (LicenseClaimsValidator.FindError(license) is { } claimError)
                    return LicenseValidationResult.Failure(
                        LicenseValidationCode.Malformed,
                        $"The signed license claims are invalid: {claimError}");
                return LicenseValidationResult.Success(license);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(payload);
                CryptographicOperations.ZeroMemory(signature);
            }
        }
        catch (Exception exception) when (exception is
                                              JsonException or
                                              FormatException or
                                              CryptographicException or
                                              InvalidOperationException or
                                              NotSupportedException or
                                              ArgumentException)
        {
            return LicenseValidationResult.Failure(
                LicenseValidationCode.Malformed,
                "The license could not be decoded, decrypted, or verified.");
        }
    }
}
