// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Pocok.Licensing;

internal sealed class LicenseService : ILicenseService, IDisposable
{
    private static readonly Action<ILogger, LicenseValidationCode, string, Exception?> LogRefreshFailure =
        LoggerMessage.Define<LicenseValidationCode, string>(
            LogLevel.Warning,
            new EventId(1, nameof(LogRefreshFailure)),
            "License refresh failed with {Code}: {Message}");

    private readonly ILicenseClock _clock;
    private readonly ILogger<LicenseService> _logger;
    private readonly IMachineFingerprintProvider _machine;
    private readonly LicenseOptions _options;
    private readonly SemaphoreSlim _reloadGate = new(1, 1);

    private readonly object _stateGate = new();

    private LicenseValidationResult _current = LicenseValidationResult.Failure(
        LicenseValidationCode.Missing,
        "The license has not been loaded yet.");

    private bool _disposed;
    private LicenseDocument? _license;

    public LicenseService(
        IOptions<LicenseOptions> options,
        ILicenseClock clock,
        IMachineFingerprintProvider machine,
        ILogger<LicenseService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        LicenseOptionsValidator.Validate(_options);
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _machine = machine ?? throw new ArgumentNullException(nameof(machine));
        _logger = logger ?? NullLogger<LicenseService>.Instance;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _reloadGate.Dispose();
        GC.SuppressFinalize(this);
    }

    public LicenseValidationResult Current
    {
        get
        {
            lock (_stateGate)
            {
                return _current;
            }
        }
    }

    public async ValueTask<LicenseValidationResult> RefreshAsync(
        string? requiredModule = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _reloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            LicenseValidationResult loaded = await LoadAndVerifyAsync(cancellationToken).ConfigureAwait(false);
            LicenseValidationResult result;
            lock (_stateGate)
            {
                _license = loaded.License;
                _current = loaded.IsValid ? EvaluateUnsafe(requiredModule) : loaded;
                result = _current;
            }

            if (!result.IsValid)
                LogRefreshFailure(_logger, result.Code, result.Message, null);
            return result;
        }
        finally
        {
            _reloadGate.Release();
        }
    }

    public LicenseValidationResult Validate(string? requiredModule = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_stateGate)
        {
            return _license is null ? _current : EvaluateUnsafe(requiredModule);
        }
    }

    public bool HasModule(string moduleIdentifier)
    {
        ValidateModuleArgument(moduleIdentifier);
        return Validate(moduleIdentifier).IsValid;
    }

    public void Demand(string moduleIdentifier)
    {
        ValidateModuleArgument(moduleIdentifier);
        LicenseValidationResult result = Validate(moduleIdentifier);
        if (!result.IsValid) throw new LicenseException(result);
    }


    private static void ValidateModuleArgument(string module)
    {
        if (!LicenseClaimsValidator.IsIdentifier(module, 256))
            throw new ArgumentException(
                "The module identifier must contain 1 to 256 non-control characters without surrounding whitespace.",
                nameof(module));
    }

    private LicenseValidationResult EvaluateUnsafe(string? requiredModule)
    {
        if (_license is null) return _current;
        try
        {
            _current = LicenseValidator.Validate(_license, new LicenseValidationContext
            {
                UtcNow = _clock.UtcNow,
                ProcessRuntime = _clock.ProcessRuntime,
                MachineFingerprint = _machine.GetFingerprint(),
                PresharedKey = _options.PresharedKey,
                RequiredModule = requiredModule
            });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            _current = LicenseValidationResult.Failure(
                LicenseValidationCode.Malformed,
                "Runtime facts required for license validation could not be read.",
                _license,
                requiredModule);
        }

        return _current;
    }

    private async ValueTask<LicenseValidationResult> LoadAndVerifyAsync(CancellationToken cancellationToken)
    {
        try
        {
            var text = _options.LicenseText;
            if (string.IsNullOrWhiteSpace(text))
            {
                if (!File.Exists(_options.LicensePath))
                    return LicenseValidationResult.Failure(
                        LicenseValidationCode.Missing,
                        $"License file '{_options.LicensePath}' was not found.");
                text = await File.ReadAllTextAsync(_options.LicensePath, cancellationToken).ConfigureAwait(false);
            }

            var trustedKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach ((var keyId, var path) in _options.TrustedPublicKeyFiles.OrderBy(pair => pair.Key,
                         StringComparer.Ordinal))
            {
                if (!File.Exists(path))
                    return LicenseValidationResult.Failure(
                        LicenseValidationCode.UntrustedSigningKey,
                        $"Trusted public-key file '{path}' was not found.");
                trustedKeys[keyId] = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            }

            foreach ((var keyId, var publicKey) in _options.TrustedPublicKeys)
                trustedKeys[keyId] = publicKey;

            return LicenseReader.ReadAndVerify(text, trustedKeys, _options.DecryptionSecret);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            return LicenseValidationResult.Failure(
                LicenseValidationCode.Malformed,
                "The configured license or signing key could not be read.");
        }
    }
}
