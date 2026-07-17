// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Pocok.Licensing;

return await RunAsync(args).ConfigureAwait(false);

static async Task<int> RunAsync(string[] args)
{
    try
    {
        var parsed = CliArguments.Parse(args);
        if (parsed.Flag("help")) return PrintHelp(0);
        return parsed.Command.ToLowerInvariant() switch
        {
            "keys" => await CreateKeysAsync(parsed).ConfigureAwait(false),
            "machine" => PrintMachine(),
            "secret" => PrintSecret(parsed),
            "issue" => await IssueAsync(parsed).ConfigureAwait(false),
            "help" or "--help" or "-h" or "" => PrintHelp(0),
            _ => PrintHelp(2, $"Unknown command '{parsed.Command}'.")
        };
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

static async Task<int> CreateKeysAsync(CliArguments arguments)
{
    var privatePath = arguments.One("private", "license-private.pem");
    var publicPath = arguments.One("public", "license-public.pem");
    EnsureWritable(privatePath, arguments.Flag("force"));
    EnsureWritable(publicPath, arguments.Flag("force"));
    EnsureParentDirectory(privatePath);
    EnsureParentDirectory(publicPath);

    (var privateKey, var publicKey) = LicenseCryptography.CreateSigningKeyPair();
    await File.WriteAllTextAsync(privatePath, privateKey, new UTF8Encoding(false)).ConfigureAwait(false);
    await File.WriteAllTextAsync(publicPath, publicKey, new UTF8Encoding(false)).ConfigureAwait(false);
    Console.WriteLine($"Created signing key pair. Private: {privatePath}; public: {publicPath}");
    Console.WriteLine("Keep the private key outside application deployments and source control.");
    return 0;
}

static int PrintMachine()
{
    Console.WriteLine(new DefaultMachineFingerprintProvider().GetFingerprint());
    return 0;
}

static int PrintSecret(CliArguments arguments)
{
    var bytes = arguments.Int32("bytes", 32);
    Console.WriteLine(LicenseCryptography.CreateRandomSecret(bytes));
    return 0;
}

static async Task<int> IssueAsync(CliArguments arguments)
{
    var licenseId = arguments.Required("id");
    var privateKey = await File.ReadAllTextAsync(arguments.Required("private")).ConfigureAwait(false);
    var psk = await arguments.SecretAsync("psk", "psk-file").ConfigureAwait(false);
    var encryptionSecret = await arguments.SecretAsync("encrypt-secret", "encrypt-secret-file").ConfigureAwait(false);

    var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var item in arguments.Many("metadata"))
    {
        var separator = item.IndexOf('=');
        if (separator <= 0) throw new ArgumentException("--metadata values must use key=value format.");
        metadata.Add(item[..separator], item[(separator + 1)..]);
    }

    var license = new LicenseDocument
    {
        LicenseId = licenseId,
        Customer = arguments.Optional("customer"),
        IssuedAtUtc = arguments.Date("issued-at") ?? TimeProvider.System.GetUtcNow(),
        ValidFromUtc = arguments.Date("valid-from"),
        ValidUntilUtc = arguments.Date("valid-until"),
        MaximumProcessRuntime = arguments.Duration("max-runtime"),
        AllModules = arguments.Flag("all-modules"),
        Modules = arguments.Many("module"),
        MachineFingerprints = arguments.Many("machine"),
        PresharedKeyHash = psk is null ? null : LicenseCryptography.CreatePresharedKeyHash(psk, licenseId),
        Metadata = metadata
    };

    var signed = LicenseCryptography.Sign(
        license,
        arguments.One("key-id", "default"),
        privateKey);
    var output = encryptionSecret is null ? signed : LicenseCryptography.Encrypt(signed, encryptionSecret);
    var outputPath = arguments.One("out", "license.pocok");
    EnsureWritable(outputPath, arguments.Flag("force"));
    EnsureParentDirectory(outputPath);
    await File.WriteAllTextAsync(outputPath, output, new UTF8Encoding(false)).ConfigureAwait(false);
    Console.WriteLine($"Issued license '{license.LicenseId}' to {outputPath}.");
    return 0;
}

static void EnsureWritable(string path, bool force)
{
    if (!force && File.Exists(path))
        throw new IOException($"File '{path}' already exists. Pass --force to overwrite it.");
}

static void EnsureParentDirectory(string path)
{
    var directory = Path.GetDirectoryName(Path.GetFullPath(path));
    if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
}

static int PrintHelp(int exitCode, string? error = null)
{
    if (error is not null) Console.Error.WriteLine(error);
    Console.WriteLine("""
                      Pocok licensing issuer

                        keys [--private private.pem] [--public public.pem] [--force]
                        machine
                        secret [--bytes 32]
                        issue --private private.pem --id LIC-001 [options]

                      Issue options:
                        --key-id production              Signing key identifier, default: default
                        --customer Name                  Customer or installation label
                        --module Reporting               Repeatable module entitlement
                        --all-modules                    License every module
                        --valid-from ISO-8601            Inclusive UTC start
                        --valid-until ISO-8601           Exclusive UTC end
                        --max-runtime d.hh:mm:ss         Maximum runtime of one process
                        --machine SHA256                 Repeatable machine fingerprint
                        --psk SECRET | --psk-file PATH   High-entropy runtime pre-shared key
                        --encrypt-secret SECRET | --encrypt-secret-file PATH
                                                         Optional license-content encryption
                        --metadata key=value             Repeatable non-secret metadata
                        --out license.pocok              Output path
                        --force                          Overwrite an existing output

                      Do not distribute the private signing key. Prefer secret files over command-line secrets.
                      """);
    return exitCode;
}

internal sealed class CliArguments
{
    private readonly Dictionary<string, List<string>> _values;

    private CliArguments(string command, Dictionary<string, List<string>> values)
    {
        Command = command;
        _values = values;
    }

    public string Command { get; }

    public static CliArguments Parse(string[] args)
    {
        var command = args.FirstOrDefault() ?? "help";
        var values = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "all-modules", "force", "help" };
        for (var index = 1; index < args.Length; index++)
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

        ValidateKnownOptions(command, values.Keys);
        return new CliArguments(command, values);
    }

    private static void ValidateKnownOptions(string command, IEnumerable<string> keys)
    {
        HashSet<string> allowed = command.ToLowerInvariant() switch
        {
            "keys" => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "private", "public", "force", "help" },
            "machine" or "help" or "--help" or "-h" or "" => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "help" },
            "secret" => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bytes", "help" },
            "issue" => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "private", "id", "key-id", "customer", "issued-at", "valid-from", "valid-until",
                "max-runtime", "all-modules", "module", "machine", "psk", "psk-file",
                "encrypt-secret", "encrypt-secret-file", "metadata", "out", "force", "help"
            },
            _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };

        foreach (var key in keys)
            if (!allowed.Contains(key))
                throw new ArgumentException($"Unknown option '--{key}' for command '{command}'.");
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
        var value = Optional(key);
        return value is not null && bool.TryParse(value, out var parsed) && parsed;
    }

    public int Int32(string key, int defaultValue)
    {
        return Optional(key) is { } value
            ? int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture)
            : defaultValue;
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
