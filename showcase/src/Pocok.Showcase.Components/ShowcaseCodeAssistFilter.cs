// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.Components;

public static class ShowcaseCodeAssistFilter
{
    public static IReadOnlyList<ShowcaseCodeAssistItem> Filter(
        ShowcaseCodeAssistCatalog catalog,
        string text,
        int cursor,
        int maximumItems = 8)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumItems, 1);
        cursor = Math.Clamp(cursor, 0, text.Length);
        string token = ReadToken(text, cursor);

        IEnumerable<ShowcaseCodeAssistItem> query = catalog.Items;
        if (token.Length > 0)
            query = query.Where(item =>
                item.Label.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                item.InsertText.Contains(token, StringComparison.OrdinalIgnoreCase));

        return query
            .OrderBy(item => Score(item, token))
            .ThenBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
            .Take(maximumItems)
            .ToArray();
    }

    public static string ReadToken(string text, int cursor)
    {
        ArgumentNullException.ThrowIfNull(text);
        cursor = Math.Clamp(cursor, 0, text.Length);
        var start = cursor;
        while (start > 0 && IsTokenCharacter(text[start - 1])) start--;
        return text[start..cursor];
    }

    private static int Score(ShowcaseCodeAssistItem item, string token)
    {
        if (token.Length == 0) return item.IsSnippet ? 2 : 1;
        if (item.Label.StartsWith(token, StringComparison.OrdinalIgnoreCase)) return 0;
        if (item.InsertText.StartsWith(token, StringComparison.OrdinalIgnoreCase)) return 1;
        return 2;
    }

    private static bool IsTokenCharacter(char character) =>
        char.IsLetterOrDigit(character) || character is '.' or '_' or ':' or '<' or '>';
}
