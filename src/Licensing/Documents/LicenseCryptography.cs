// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Pocok.Licensing.Validation;

namespace Pocok.Licensing.Documents;

/// <summary>Creates keys, signs license claims, and optionally wraps signed licenses using authenticated encryption.</summary>
public static class LicenseCryptography
{
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private const int Pbkdf2Iterations = 600_000;
    private const int MinimumAcceptedPbkdf2Iterations = 100_000;
    private const int MaximumAcceptedPbkdf2Iterations = 2_000_000;

    /// <summary>Creates an ECDSA P-256 PKCS#8 private key and SubjectPublicKeyInfo public key.</summary>
    /// <returns>The PEM-encoded key pair.</returns>
    public static (string PrivateKeyPem, string PublicKeyPem) CreateSigningKeyPair()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return (key.ExportPkcs8PrivateKeyPem(), key.ExportSubjectPublicKeyInfoPem());
    }

    /// <summary>Creates a cryptographically random Base64 secret suitable for license encryption or PSK use.</summary>
    /// <param name="byteLength">The secret length in bytes. The default is 32.</param>
    /// <returns>A Base64-encoded random secret.</returns>
    public static string CreateRandomSecret(int byteLength = 32)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(byteLength, 16);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(byteLength, 1024);
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteLength));
    }

    /// <summary>Creates the versioned HMAC digest stored in a license for a high-entropy pre-shared key.</summary>
    /// <param name="presharedKey">The pre-shared key.</param>
    /// <param name="licenseId">The license identifier that scopes the digest.</param>
    /// <returns>A lowercase SHA-256 hexadecimal digest.</returns>
    public static string CreatePresharedKeyHash(string presharedKey, string licenseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(presharedKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(licenseId);
        var key = Encoding.UTF8.GetBytes(presharedKey);
        var material = Encoding.UTF8.GetBytes($"pocok-license-psk/v1\0{licenseId}");
        try
        {
            return Convert.ToHexStringLower(HMACSHA256.HashData(key, material));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(material);
        }
    }

    /// <summary>Signs a canonical license payload with an ECDSA P-256 private key.</summary>
    /// <param name="license">The license claims.</param>
    /// <param name="keyId">The public-key identifier embedded in the envelope.</param>
    /// <param name="privateKeyPem">The issuer-only PKCS#8 private key.</param>
    /// <returns>A signed JSON license envelope.</returns>
    public static string Sign(LicenseDocument license, string keyId, string privateKeyPem)
    {
        ArgumentNullException.ThrowIfNull(license);
        if (!LicenseClaimsValidator.IsIdentifier(keyId, 256))
            throw new ArgumentException(
                "The signing key identifier must contain 1 to 256 non-control characters without surrounding whitespace.",
                nameof(keyId));
        ArgumentException.ThrowIfNullOrWhiteSpace(privateKeyPem);
        if (LicenseClaimsValidator.FindError(license) is { } error)
            throw new ArgumentException(error, nameof(license));

        using var key = ECDsa.Create();
        key.ImportFromPem(privateKeyPem);
        if (key.KeySize != 256)
            throw new CryptographicException("The signing key must use ECDSA P-256.");

        var payload = LicenseCanonicalizer.SerializePayload(license);
        var signature = key.SignData(
            payload,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        var envelope = new LicenseEnvelope
        {
            Format = LicenseEnvelope.CurrentFormat,
            Algorithm = LicenseEnvelope.CurrentAlgorithm,
            KeyId = keyId,
            Payload = Convert.ToBase64String(payload),
            Signature = Convert.ToBase64String(signature)
        };
        return JsonSerializer.Serialize(envelope, LicenseCanonicalizer.JsonOptions);
    }

    /// <summary>Wraps a signed license using PBKDF2-SHA256 and AES-256-GCM.</summary>
    /// <param name="signedLicense">The signed license envelope.</param>
    /// <param name="secret">The wrapping secret.</param>
    /// <returns>An authenticated encrypted JSON envelope.</returns>
    /// <remarks>Encryption hides license contents but does not replace signature verification.</remarks>
    public static string Encrypt(string signedLicense, string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signedLicense);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        if (signedLicense.Length > LicenseClaimsValidator.MaximumLicenseTextCharacters)
            throw new ArgumentException("The signed license is too large.", nameof(signedLicense));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            secret,
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            KeySize);
        var plaintext = Encoding.UTF8.GetBytes(signedLicense);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        try
        {
            using AesGcm aes = new(key, TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, CreateAssociatedData(Pbkdf2Iterations));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintext);
        }

        var envelope = new EncryptedLicenseEnvelope
        {
            Format = EncryptedLicenseEnvelope.CurrentFormat,
            Algorithm = EncryptedLicenseEnvelope.CurrentAlgorithm,
            KdfIterations = Pbkdf2Iterations,
            Salt = Convert.ToBase64String(salt),
            Nonce = Convert.ToBase64String(nonce),
            Ciphertext = Convert.ToBase64String(ciphertext),
            Tag = Convert.ToBase64String(tag)
        };
        return JsonSerializer.Serialize(envelope, LicenseCanonicalizer.JsonOptions);
    }

    internal static string Decrypt(EncryptedLicenseEnvelope envelope, string secret)
    {
        if (envelope.Format != EncryptedLicenseEnvelope.CurrentFormat ||
            envelope.Algorithm != EncryptedLicenseEnvelope.CurrentAlgorithm)
            throw new NotSupportedException("The encrypted license format or algorithm is unsupported.");
        if (envelope.KdfIterations is < MinimumAcceptedPbkdf2Iterations or > MaximumAcceptedPbkdf2Iterations)
            throw new FormatException("The encrypted license KDF iteration count is outside the accepted range.");

        var salt = Convert.FromBase64String(envelope.Salt);
        var nonce = Convert.FromBase64String(envelope.Nonce);
        var ciphertext = Convert.FromBase64String(envelope.Ciphertext);
        var tag = Convert.FromBase64String(envelope.Tag);
        if (salt.Length != SaltSize || nonce.Length != NonceSize || tag.Length != TagSize ||
            ciphertext.Length > LicenseClaimsValidator.MaximumLicenseTextCharacters)
            throw new FormatException("The encrypted license contains invalid field sizes.");

        var key = Rfc2898DeriveBytes.Pbkdf2(
            secret,
            salt,
            envelope.KdfIterations,
            HashAlgorithmName.SHA256,
            KeySize);
        var plaintext = new byte[ciphertext.Length];
        try
        {
            using AesGcm aes = new(key, TagSize);
            aes.Decrypt(
                nonce,
                ciphertext,
                tag,
                plaintext,
                CreateAssociatedData(envelope.KdfIterations));
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private static byte[] CreateAssociatedData(int iterations)
    {
        return Encoding.UTF8.GetBytes(
            $"{EncryptedLicenseEnvelope.CurrentFormat}\0{EncryptedLicenseEnvelope.CurrentAlgorithm}\0{iterations}");
    }
}
