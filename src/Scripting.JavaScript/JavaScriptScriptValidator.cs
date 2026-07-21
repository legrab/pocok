// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Text;
using System.Text.RegularExpressions;
using Acornima;
using Pocok.Scripting.Execution;

namespace Pocok.Scripting.JavaScript;

/// <summary>Applies parser-backed JavaScript guardrails before Jint execution.</summary>
public sealed partial class JavaScriptScriptValidator : IScriptValidator
{
    private static readonly ParserOptions StrictParserOptions = new() { Tolerant = false };

    /// <inheritdoc />
    public ScriptEngineId EngineId => ScriptEngineId.JavaScript;

    /// <inheritdoc />
    public ValueTask<ScriptValidationResult> ValidateAsync(
        ScriptExecutionRequest request,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _ = new Parser(StrictParserOptions).ParseScript(request.Source);
        }
        catch (ParseErrorException exception)
        {
            return ValueTask.FromResult(ScriptValidationResult.From(
            [
                new ScriptValidationDiagnostic(
                    "scripting.javascript.syntax",
                    "JavaScript syntax is invalid.",
                    Line: exception.LineNumber,
                    Column: exception.Column)
            ]));
        }

        var code = StripTriviaAndStrings(request.Source);
        var diagnostics = new List<ScriptValidationDiagnostic>();

        AddIf(EvalCallRegex().IsMatch(code),
            "scripting.javascript.eval",
            "Dynamic eval is not allowed.");
        AddIf(FunctionConstructorRegex().IsMatch(code),
            "scripting.javascript.function_constructor",
            "Function construction is not allowed.");
        AddIf(DynamicImportRegex().IsMatch(code),
            "scripting.javascript.dynamic_import",
            "Dynamic import is not allowed.");

        foreach (Match alias in EvalAliasRegex().Matches(code))
        {
            var escapedAlias = Regex.Escape(alias.Groups["alias"].Value);
            if (Regex.IsMatch(code, $@"\b{escapedAlias}\s*\(", RegexOptions.CultureInvariant))
                diagnostics.Add(new ScriptValidationDiagnostic(
                    "scripting.javascript.eval_alias",
                    "Aliasing eval is not allowed."));
        }

        return ValueTask.FromResult(ScriptValidationResult.From(diagnostics));

        void AddIf(bool condition, string codeValue, string message)
        {
            if (condition)
                diagnostics.Add(new ScriptValidationDiagnostic(codeValue, message));
        }
    }

    internal static string StripTriviaAndStrings(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var result = new StringBuilder(source.Length);
        var index = 0;
        ScanCode(source, ref index, result, false);
        return result.ToString();
    }

    private static void ScanCode(
        string source,
        ref int index,
        StringBuilder result,
        bool stopAtTemplateBrace)
    {
        var braceDepth = stopAtTemplateBrace ? 1 : 0;
        while (index < source.Length)
        {
            var character = source[index];
            var next = index + 1 < source.Length ? source[index + 1] : '\0';

            if (character == '/' && next == '/')
            {
                result.Append("  ");
                index += 2;
                while (index < source.Length && source[index] is not '\r' and not '\n')
                {
                    result.Append(' ');
                    index++;
                }

                continue;
            }

            if (character == '/' && next == '*')
            {
                result.Append("  ");
                index += 2;
                while (index < source.Length)
                {
                    if (source[index] == '*' && index + 1 < source.Length && source[index + 1] == '/')
                    {
                        result.Append("  ");
                        index += 2;
                        break;
                    }

                    result.Append(source[index] is '\r' or '\n' ? source[index] : ' ');
                    index++;
                }

                continue;
            }

            if (character is '\'' or '"')
            {
                ScanQuotedString(source, ref index, result, character);
                continue;
            }

            if (character == '`')
            {
                ScanTemplate(source, ref index, result);
                continue;
            }

            if (stopAtTemplateBrace)
            {
                if (character == '{')
                {
                    braceDepth++;
                }
                else if (character == '}' && --braceDepth == 0)
                {
                    result.Append(character);
                    index++;
                    return;
                }
            }

            result.Append(character);
            index++;
        }
    }

    private static void ScanQuotedString(
        string source,
        ref int index,
        StringBuilder result,
        char quote)
    {
        result.Append(' ');
        index++;
        while (index < source.Length)
        {
            var character = source[index];
            result.Append(character is '\r' or '\n' ? character : ' ');
            index++;
            if (character == '\\' && index < source.Length)
            {
                result.Append(source[index] is '\r' or '\n' ? source[index] : ' ');
                index++;
                continue;
            }

            if (character == quote)
                return;
        }
    }

    private static void ScanTemplate(
        string source,
        ref int index,
        StringBuilder result)
    {
        result.Append(' ');
        index++;
        while (index < source.Length)
        {
            var character = source[index];
            var next = index + 1 < source.Length ? source[index + 1] : '\0';
            if (character == '\\')
            {
                result.Append(' ');
                index++;
                if (index < source.Length)
                {
                    result.Append(source[index] is '\r' or '\n' ? source[index] : ' ');
                    index++;
                }

                continue;
            }

            if (character == '`')
            {
                result.Append(' ');
                index++;
                return;
            }

            if (character == '$' && next == '{')
            {
                result.Append("${");
                index += 2;
                ScanCode(source, ref index, result, true);
                continue;
            }

            result.Append(character is '\r' or '\n' ? character : ' ');
            index++;
        }
    }

    [GeneratedRegex(@"\beval\s*\(", RegexOptions.CultureInvariant)]
    private static partial Regex EvalCallRegex();

    [GeneratedRegex(@"\b(?:new\s+)?Function\s*\(", RegexOptions.CultureInvariant)]
    private static partial Regex FunctionConstructorRegex();

    [GeneratedRegex(@"\bimport\s*\(", RegexOptions.CultureInvariant)]
    private static partial Regex DynamicImportRegex();

    [GeneratedRegex(
        @"\b(?:const|let|var)\s+(?<alias>[A-Za-z_$][A-Za-z0-9_$]*)\s*=\s*eval\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex EvalAliasRegex();
}
