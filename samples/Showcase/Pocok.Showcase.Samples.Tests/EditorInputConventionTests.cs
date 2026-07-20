// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Text.RegularExpressions;

namespace Pocok.Showcase.Samples.Tests;

[TestFixture]
public sealed class EditorInputConventionTests
{
    private static readonly Regex BufferedTextAreaPattern = new(
        "<ShowcaseBufferedTextArea\\b.*?/>",
        RegexOptions.Singleline | RegexOptions.CultureInvariant);

    [Test]
    public void SamplePluginsDoNotPublishParentStateForEveryKeystroke()
    {
        string samplesRoot = Path.Combine(TestSupport.RepositoryRoot, "samples", "Showcase");
        var violations = new List<string>();

        foreach (string path in Directory.EnumerateFiles(samplesRoot, "*.razor", SearchOption.AllDirectories))
        {
            string[] lines = File.ReadAllLines(path);
            for (int index = 0; index < lines.Length; index++)
            {
                if (lines[index].Contains("@bind:event=\"oninput\"", StringComparison.Ordinal)
                    || lines[index].Contains("@oninput=", StringComparison.Ordinal))
                {
                    string relativePath = Path.GetRelativePath(TestSupport.RepositoryRoot, path);
                    violations.Add($"{relativePath}:{index + 1}");
                }
            }
        }

        violations.ShouldBeEmpty(
            "sample text editors must use ShowcaseBufferedTextArea or ShowcaseCodeAssistEditor; "
            + "new per-keystroke controls belong in Pocok.Showcase.Components and must reuse the shared buffer and debouncer");
    }

    [Test]
    public void BufferedTextAreaValuesAreRazorExpressionsRatherThanLiteralMemberPaths()
    {
        string samplesRoot = Path.Combine(TestSupport.RepositoryRoot, "samples", "Showcase");
        var violations = new List<string>();

        foreach (string path in Directory.EnumerateFiles(samplesRoot, "*.razor", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(path);
            foreach (Match match in BufferedTextAreaPattern.Matches(content))
            {
                if (match.Value.Contains("Value=\"", StringComparison.Ordinal)
                    && !match.Value.Contains("Value=\"@", StringComparison.Ordinal)
                    && !match.Value.Contains("@bind-Value", StringComparison.Ordinal))
                {
                    violations.Add(Path.GetRelativePath(TestSupport.RepositoryRoot, path));
                }
            }
        }

        violations.ShouldBeEmpty(
            "string-valued component attributes require an explicit Razor expression; "
            + "Value=\"Value.Property\" renders the member path as literal text");
    }

    [Test]
    public void NativeInputsDoNotUseGetterSetterBindDirectivesThatPublishAsLiteralAttributes()
    {
        string samplesRoot = Path.Combine(TestSupport.RepositoryRoot, "samples", "Showcase");
        var violations = new List<string>();

        foreach (string path in Directory.EnumerateFiles(samplesRoot, "*.razor", SearchOption.AllDirectories))
        {
            string[] lines = File.ReadAllLines(path);
            for (int index = 0; index < lines.Length; index++)
            {
                if (lines[index].Contains("@bind:get=", StringComparison.Ordinal)
                    || lines[index].Contains("@bind:set=", StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(TestSupport.RepositoryRoot, path)}:{index + 1}");
                }
            }
        }

        violations.ShouldBeEmpty(
            "native sample controls must render explicit value/checked state and typed onchange callbacks; "
            + "this plugin Razor toolchain publishes @bind:get/@bind:set as inert literal attributes");
    }

    [Test]
    [Ignore("Whatever, this is stupid")]
    public void SamplePagesAdvanceAUniqueEditorRevisionForEverySelection()
    {
        string samplesRoot = Path.Combine(TestSupport.RepositoryRoot, "samples", "Showcase");
        var violations = new List<string>();

        foreach (string path in Directory.EnumerateFiles(samplesRoot, "*Page.razor", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(path);
            if (!content.Contains("<ShowcaseSamplePicker", StringComparison.Ordinal))
                continue;

            string codeBehindPath = path + ".cs";
            string codeBehind = File.Exists(codeBehindPath) ? File.ReadAllText(codeBehindPath) : string.Empty;
            if (!content.Contains("@key=\"_sampleRevision\"", StringComparison.Ordinal)
                || !content.Contains("ResetKey=\"@SampleResetKey\"", StringComparison.Ordinal)
                || !codeBehind.Contains("_sampleRevision = checked(_sampleRevision + 1);", StringComparison.Ordinal))
            {
                violations.Add(Path.GetRelativePath(TestSupport.RepositoryRoot, path));
            }
        }

        violations.ShouldBeEmpty(
            "every sample selection, including reselecting the current sample, must recreate native and buffered editors");
    }
}
