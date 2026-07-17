// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pocok.Licensing.Documents;

internal static class LicenseCanonicalizer
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = false
    };

    internal static byte[] SerializePayload(LicenseDocument license)
    {
        var payload = new PayloadModel
        {
            LicenseId = license.LicenseId,
            Customer = license.Customer,
            IssuedAtUtc = license.IssuedAtUtc,
            ValidFromUtc = license.ValidFromUtc,
            ValidUntilUtc = license.ValidUntilUtc,
            MaximumProcessRuntimeTicks = license.MaximumProcessRuntime?.Ticks,
            AllModules = license.AllModules,
            Modules = license.Modules.Order(StringComparer.Ordinal).ToArray(),
            MachineFingerprints = license.MachineFingerprints.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            PresharedKeyHash = license.PresharedKeyHash,
            Metadata = license.Metadata
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
        };
        return JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
    }

    internal static LicenseDocument DeserializePayload(ReadOnlySpan<byte> payload)
    {
        PayloadModel model = JsonSerializer.Deserialize<PayloadModel>(payload, JsonOptions)
                             ?? throw new JsonException("License payload is empty.");
        return new LicenseDocument
        {
            LicenseId = model.LicenseId ?? throw new JsonException("licenseId is required."),
            Customer = model.Customer,
            IssuedAtUtc = model.IssuedAtUtc,
            ValidFromUtc = model.ValidFromUtc,
            ValidUntilUtc = model.ValidUntilUtc,
            MaximumProcessRuntime = model.MaximumProcessRuntimeTicks is { } ticks ? TimeSpan.FromTicks(ticks) : null,
            AllModules = model.AllModules,
            Modules = model.Modules ?? [],
            MachineFingerprints = model.MachineFingerprints ?? [],
            PresharedKeyHash = model.PresharedKeyHash,
            Metadata = model.Metadata ?? new Dictionary<string, string>(StringComparer.Ordinal)
        };
    }

    private sealed class PayloadModel
    {
        public string? LicenseId { get; init; }
        public string? Customer { get; init; }
        public DateTimeOffset IssuedAtUtc { get; init; }
        public DateTimeOffset? ValidFromUtc { get; init; }
        public DateTimeOffset? ValidUntilUtc { get; init; }
        public long? MaximumProcessRuntimeTicks { get; init; }
        public bool AllModules { get; init; }
        public string[]? Modules { get; init; }
        public string[]? MachineFingerprints { get; init; }
        public string? PresharedKeyHash { get; init; }
        public Dictionary<string, string>? Metadata { get; init; }
    }
}
