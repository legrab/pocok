// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Pocok.Showcase.Scripting;

public sealed class ScriptingShowcaseOptions
{
    public bool TrustedEnginesEnabled { get; init; }
    public bool RequireTrustedEnginesAvailable { get; init; } = true;
    public int MaximumSourceCharacters { get; init; } = 4_000;
    public int MaximumOutputBytes { get; init; } = 8 * 1_024;
    public int MaximumTimeoutMilliseconds { get; init; } = 2_000;
    public int MaximumStatements { get; init; } = 10_000;
    public int MaximumRecursionDepth { get; init; } = 64;
    public int MaximumMemoryMegabytes { get; init; } = 16;
    public int WarmupTimeoutMilliseconds { get; init; } = 30_000;

    public static ScriptingShowcaseOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new ScriptingShowcaseOptions
        {
            TrustedEnginesEnabled = ReadBoolean(configuration, nameof(TrustedEnginesEnabled), false),
            RequireTrustedEnginesAvailable = ReadBoolean(
                configuration,
                nameof(RequireTrustedEnginesAvailable),
                true),
            MaximumSourceCharacters = ReadInteger(
                configuration,
                nameof(MaximumSourceCharacters),
                4_000,
                256,
                65_536),
            MaximumOutputBytes = ReadInteger(
                configuration,
                nameof(MaximumOutputBytes),
                8 * 1_024,
                1_024,
                1_048_576),
            MaximumTimeoutMilliseconds = ReadInteger(
                configuration,
                nameof(MaximumTimeoutMilliseconds),
                2_000,
                50,
                30_000),
            MaximumStatements = ReadInteger(
                configuration,
                nameof(MaximumStatements),
                10_000,
                100,
                1_000_000),
            MaximumRecursionDepth = ReadInteger(
                configuration,
                nameof(MaximumRecursionDepth),
                64,
                8,
                1_024),
            MaximumMemoryMegabytes = ReadInteger(
                configuration,
                nameof(MaximumMemoryMegabytes),
                16,
                4,
                1_024),
            WarmupTimeoutMilliseconds = ReadInteger(
                configuration,
                nameof(WarmupTimeoutMilliseconds),
                30_000,
                1_000,
                120_000)
        };
    }

    private static bool ReadBoolean(
        IConfiguration configuration,
        string key,
        bool fallback)
    {
        var configured = configuration[key];
        if (string.IsNullOrWhiteSpace(configured))
            return fallback;
        if (bool.TryParse(configured, out var value))
            return value;

        throw new InvalidOperationException($"{key} must be either true or false.");
    }

    private static int ReadInteger(
        IConfiguration configuration,
        string key,
        int fallback,
        int minimum,
        int maximum)
    {
        var configured = configuration[key];
        if (string.IsNullOrWhiteSpace(configured))
            return fallback;

        if (int.TryParse(configured, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            && value >= minimum
            && value <= maximum)
            return value;

        throw new InvalidOperationException(
            $"{key} must be an integer between {minimum.ToString(CultureInfo.InvariantCulture)} " +
            $"and {maximum.ToString(CultureInfo.InvariantCulture)}.");
    }
}
