// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Pocok.Licensing;

return await RunAsync(args).ConfigureAwait(false);

static async Task<int> RunAsync(string[] args)
{
    try
    {
        var parsed = Arguments.Parse(args);
        if (parsed.Flag("help")) return PrintHelp(0);

        var licensePath = parsed.Required("license");
        var trusted = new Dictionary<string, string>(StringComparer.Ordinal);
        var publicKeys = parsed.Many("public");
        if (publicKeys.Length == 0) throw new ArgumentException("At least one --public argument is required.");
        foreach (var publicKey in publicKeys)
        {
            var (keyId, path) = SplitKeyReference(publicKey, parsed.One("key-id", "default"));
            trusted.Add(keyId, await File.ReadAllTextAsync(path).ConfigureAwait(false));
        }

        var decryptionSecret = await parsed.SecretAsync("decrypt-secret", "decrypt-secret-file").ConfigureAwait(false);
        var psk = await parsed.SecretAsync("psk", "psk-file").ConfigureAwait(false);
        LicenseValidationResult verified = LicenseReader.ReadAndVerify(
            await File.ReadAllTextAsync(licensePath).ConfigureAwait(false),
            trusted,
            decryptionSecret);
        LicenseValidationResult result = verified.IsValid
            ? LicenseValidator.Validate(verified.License!, new LicenseValidationContext
            {
                UtcNow = parsed.Date("utc-now") ?? TimeProvider.System.GetUtcNow(),
                ProcessRuntime = parsed.Duration("runtime") ?? TimeSpan.Zero,
                MachineFingerprint = parsed.Optional("machine") ??
                                     new DefaultMachineFingerprintProvider().GetFingerprint(),
                PresharedKey = psk,
                RequiredModule = parsed.Optional("module")
            })
            : verified;

        if (parsed.Flag("json"))
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                result.IsValid,
                Code = result.Code.ToString(),
                result.Message,
                result.License?.LicenseId,
                result.Module
            }));
        else
            Console.WriteLine($"{result.Code}: {result.Message}");

        return result.IsValid ? 0 : 3;
    }
    catch (Exception exception) when (exception is
                                          ArgumentException or
                                          FormatException or
                                          IOException or
                                          UnauthorizedAccessException or
                                          CryptographicException)
    {
        Console.Error.WriteLine(exception.Message);
        return 2;
    }
}

static (string KeyId, string Path) SplitKeyReference(string value, string defaultKeyId)
{
    var separator = value.IndexOf('=');
    return separator > 0 ? (value[..separator], value[(separator + 1)..]) : (defaultKeyId, value);
}

static int PrintHelp(int exitCode)
{
    Console.WriteLine("""
                      Pocok license checker

                        --license license.pocok --public [keyId=]public.pem [options]

                      Options:
                        --public production=production.pem   Repeatable trusted signing key
                        --key-id default                     Key id for an unqualified --public path
                        --module Reporting                   Required module
                        --machine SHA256                     Override local machine fingerprint
                        --psk SECRET | --psk-file PATH       Runtime pre-shared key
                        --decrypt-secret SECRET | --decrypt-secret-file PATH
                                                             License-content decryption secret
                        --utc-now ISO-8601                   Override current UTC time for diagnostics
                        --runtime d.hh:mm:ss                 Override process runtime for diagnostics
                        --json                               Machine-readable result

                      Exit codes: 0 valid, 2 invalid invocation or I/O, 3 license rejected.
                      """);
    return exitCode;
}

internal sealed class Arguments
{
    private readonly Dictionary<string, List<string>> _values;

    private Arguments(Dictionary<string, List<string>> values)
    {
        _values = values;
    }

    public static Arguments Parse(string[] args)
    {
        var values = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "help", "json" };
        for (var index = 0; index < args.Length; index++)
        {
            var token = args[index];
            if (!token.StartsWith("--", StringComparison.Ordinal) || token.Length == 2)
                throw new ArgumentException($"Unexpected argument '{token}'. Options must start with --.");

            var key = token[2..];
            string value;
            if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
                value = args[++index];
            else if (flags.Contains(key))
                value = "true";
            else
                throw new ArgumentException($"--{key} requires a value.");

            if (!values.TryGetValue(key, out List<string>? entries)) values[key] = entries = [];
            entries.Add(value);
        }

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "help", "license", "public", "key-id", "module", "machine", "psk", "psk-file",
            "decrypt-secret", "decrypt-secret-file", "utc-now", "runtime", "json"
        };
        foreach (var key in values.Keys)
            if (!allowed.Contains(key))
                throw new ArgumentException($"Unknown option '--{key}'.");

        return new Arguments(values);
    }

    public string Required(string key)
    {
        return Optional(key) ?? throw new ArgumentException($"--{key} is required.");
    }

    public string One(string key, string defaultValue)
    {
        return Optional(key) ?? defaultValue;
    }

    public string? Optional(string key)
    {
        return _values.TryGetValue(key, out List<string>? values) ? values[^1] : null;
    }

    public string[] Many(string key)
    {
        return _values.TryGetValue(key, out List<string>? values) ? values.ToArray() : [];
    }

    public bool Flag(string key)
    {
        return Optional(key) is { } value && bool.TryParse(value, out var parsed) && parsed;
    }

    public DateTimeOffset? Date(string key)
    {
        return Optional(key) is { } value
            ? DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
                .ToUniversalTime()
            : null;
    }

    public TimeSpan? Duration(string key)
    {
        return Optional(key) is { } value
            ? TimeSpan.Parse(value, CultureInfo.InvariantCulture)
            : null;
    }

    public async Task<string?> SecretAsync(string inlineKey, string fileKey)
    {
        var inline = Optional(inlineKey);
        var path = Optional(fileKey);
        if (inline is not null && path is not null)
            throw new ArgumentException($"Use either --{inlineKey} or --{fileKey}, not both.");
        return path is null ? inline : (await File.ReadAllTextAsync(path).ConfigureAwait(false)).Trim();
    }
}
