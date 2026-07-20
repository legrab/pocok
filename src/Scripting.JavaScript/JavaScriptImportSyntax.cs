// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


using System.Text.RegularExpressions;
using Pocok.Scripting.Execution;
using Pocok.Scripting.Import;

namespace Pocok.Scripting.JavaScript;

/// <summary>Parses JavaScript <c>// #import Name from Module</c> directives.</summary>
public sealed partial class JavaScriptImportSyntax : IScriptImportSyntax
{
    /// <summary>Gets the export completion prefix.</summary>
    public const string EditorExportCompletionPrefixPattern = @"//\s*#import\s+\w*$";
    /// <summary>Gets the module completion prefix.</summary>
    public const string EditorModuleCompletionPrefixPattern = @"//\s*#import\s+(?<Name>[A-Za-z0-9_]+)\s+from\s+\w*$";
    /// <inheritdoc />
    public ScriptEngineId EngineId => ScriptEngineId.JavaScript;
    /// <inheritdoc />
    public IReadOnlyList<ScriptReference> FindImports(string? content) => string.IsNullOrEmpty(content)
        ? []
        : [.. ImportRegex().Matches(content).Select(static match => new ScriptReference(
            ScriptEngineId.JavaScript, match.Groups["Name"].Value, match.Groups["Module"].Value))];
    /// <inheritdoc />
    public string RemoveImports(string content) => ImportRegex().Replace(content, string.Empty);
    /// <inheritdoc />
    public string ImportedComment(ScriptReference reference, int depth) =>
        $"// {new string(' ', depth * 2)}#imported {reference.Name} from {reference.Module}";

    [GeneratedRegex(@"^[ \t]*//[ \t]*#import[ \t]*(?<Name>[A-Za-z0-9_]+)[ \t]+from[ \t]+(?<Module>[A-Za-z0-9_]+)[ \t]*(;[^\r\n]*)?$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex ImportRegex();
}
