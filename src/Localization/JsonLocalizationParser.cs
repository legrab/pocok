// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections.Frozen;
using System.Text;
using System.Text.Json;

namespace Pocok.Localization;

internal static class JsonLocalizationParser
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal static FrozenDictionary<string, string> Parse(
        string path,
        bool allowComments,
        bool allowTrailingCommas)
    {
        try
        {
            var content = File.ReadAllBytes(path);
            var offset = HasUtf8ByteOrderMark(content) ? 3 : 0;
            var json = StrictUtf8.GetString(content, offset, content.Length - offset);
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = allowTrailingCommas,
                CommentHandling = allowComments ? JsonCommentHandling.Skip : JsonCommentHandling.Disallow
            });

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new FormatException("The JSON resource root must be an object.");
            }

            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            AddObject(document.RootElement, null, entries);
            return entries.ToFrozenDictionary(StringComparer.Ordinal);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or DecoderFallbackException or JsonException or FormatException)
        {
            throw new FormatException($"Failed to parse JSON localization file '{path}'. {exception.Message}", exception);
        }
    }

    private static bool HasUtf8ByteOrderMark(byte[] content) =>
        content.Length >= 3 &&
        content[0] == 0xEF &&
        content[1] == 0xBB &&
        content[2] == 0xBF;

    private static void AddObject(
        JsonElement element,
        string? prefix,
        Dictionary<string, string> entries)
    {
        var propertyNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!propertyNames.Add(property.Name))
            {
                throw new FormatException($"Duplicate JSON property '{Combine(prefix, property.Name)}'.");
            }

            ValidateSegment(property.Name);
            var key = Combine(prefix, property.Name);
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    AddObject(property.Value, key, entries);
                    break;
                case JsonValueKind.String:
                    if (!entries.TryAdd(key, property.Value.GetString()!))
                    {
                        throw new FormatException($"Duplicate flattened JSON resource key '{key}'.");
                    }

                    break;
                default:
                    throw new FormatException($"JSON resource '{key}' must be a string or nested object.");
            }
        }
    }

    private static void ValidateSegment(string segment)
    {
        if (string.IsNullOrEmpty(segment) ||
            segment[0] == '.' ||
            segment[^1] == '.')
        {
            throw new FormatException($"Invalid JSON resource property name '{segment}'.");
        }
    }

    private static string Combine(string? prefix, string segment) =>
        prefix is null ? segment : $"{prefix}.{segment}";
}
