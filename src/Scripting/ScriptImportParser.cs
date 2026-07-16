// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Text.RegularExpressions;

namespace Pocok.Scripting;

/// <summary>Parses the neutral <c>// #import Name from Module</c> directive.</summary>
public static partial class ScriptImportParser
{
    /// <summary>Gets the editor completion pattern for an import name.</summary>
    public const string EditorExportCompletionPrefixPattern = @"//\s*#import\s+\w*$";

    /// <summary>Gets the editor completion pattern for an import module.</summary>
    public const string EditorModuleCompletionPrefixPattern = @"//\s*#import\s+(?<Name>[A-Za-z0-9_]+)\s+from\s+\w*$";

    /// <summary>Finds import references in source order.</summary>
    public static IReadOnlyList<ScriptReference> FindImports(string? content)
    {
        if (string.IsNullOrEmpty(content)) return [];
        return [.. ImportRegex().Matches(content).Select(static match =>
            new ScriptReference(match.Groups["Name"].Value, match.Groups["Module"].Value))];
    }

    internal static string RemoveImports(string content) => ImportRegex().Replace(content, string.Empty);

    internal static string ImportedComment(ScriptReference reference, int depth) =>
        $"// {new string(' ', depth * 2)}#imported {reference.Name} from {reference.Module}";

    [GeneratedRegex(@"^[ \t]*//[ \t]*#import[ \t]*(?<Name>[A-Za-z0-9_]+)[ \t]+from[ \t]+(?<Module>[A-Za-z0-9_]+)[ \t]*(;[^\r\n]*)?$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex ImportRegex();
}
