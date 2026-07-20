// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.Execution;
using Pocok.Scripting.Import;
using Pocok.Scripting.Modules;
using Shouldly;

namespace Pocok.Scripting.Tests.Import;

[TestFixture]
public sealed class ScriptImportTests
{
    [Test]
    public void ModuleBoundariesRejectNullDependencies()
    {
        var syntax = new FakeSyntax(ScriptEngineId.JavaScript);
        var source = new InMemoryScriptModuleSource([]);
        var resolver = new ScriptModuleResolver(source, syntax);

        Should.Throw<ArgumentNullException>(() => new ScriptModuleResolver(null!, syntax));
        Should.Throw<ArgumentNullException>(() => new ScriptModuleResolver(source, null!));
        Should.Throw<ArgumentNullException>(() => new ScriptImportInjector(null!, syntax));
        Should.Throw<ArgumentNullException>(() => new ScriptImportInjector(resolver, null!));
    }

    [Test]
    public async Task ResolverReturnsDependencyFirstOrderAndBreaksCycles()
    {
        var syntax = new FakeSyntax(ScriptEngineId.JavaScript);
        var source = new InMemoryScriptModuleSource(
        [
            Module("root", "#import middle from Test\nroot();"),
            Module("middle", "#import base from Test\nmiddle();"),
            Module("base", "#import root from Test\nbase();")
        ]);

        ScriptImportResolution result = await new ScriptModuleResolver(source, syntax)
            .ResolveAsync(new ScriptReference(ScriptEngineId.JavaScript, "root", "Test"));

        result.Scripts.Select(static script => script.Reference.Name)
            .ShouldBe(["base", "middle", "root"]);
        result.Diagnostics.ShouldContain(static diagnostic =>
            diagnostic.Code == "scripting.import.cycle");
    }

    [Test]
    public async Task InjectorExpandsTransitiveImportsAndReportsMissingModules()
    {
        var syntax = new FakeSyntax(ScriptEngineId.JavaScript);
        var source = new InMemoryScriptModuleSource(
        [
            Module("math", "function add(a, b) { return a + b; }")
        ]);
        var resolver = new ScriptModuleResolver(source, syntax);
        var injector = new ScriptImportInjector(resolver, syntax);

        InjectedScript result = await injector.InjectAsync(
            "#import math from Test\n#import missing from Test\nadd(2, 3);");

        result.Content.ShouldContain("function add");
        result.Content.ShouldNotContain("#import math from Test");
        result.Diagnostics.ShouldContain(static diagnostic =>
            diagnostic.Code == "scripting.import.missing");
    }

    [Test]
    public async Task EngineMismatchIsRejected()
    {
        var syntax = new FakeSyntax(ScriptEngineId.JavaScript);
        var source = new InMemoryScriptModuleSource([]);
        var resolver = new ScriptModuleResolver(source, syntax);

        ScriptImportResolution result = await resolver.ResolveAsync(
            new ScriptReference(ScriptEngineId.Python, "A", "M"));

        result.Diagnostics.Single().Code.ShouldBe("scripting.import.engine_mismatch");
    }

    [Test]
    public void SyntaxIgnoresImportTextInsideOrdinarySource()
    {
        var syntax = new FakeSyntax(ScriptEngineId.JavaScript);

        syntax.FindImports("""
                           const text = "#import fake from Test";
                           #import real from Test
                           """)
            .Select(static reference => reference.Name)
            .ShouldBe(["real"]);
    }

    private static ScriptModule Module(string name, string content) =>
        new(ScriptEngineId.JavaScript, name, "Test", content);

    private sealed class FakeSyntax(ScriptEngineId engineId) : IScriptImportSyntax
    {
        public ScriptEngineId EngineId => engineId;

        public IReadOnlyList<ScriptReference> FindImports(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return [];

            return content.Split('\n')
                .Select(static line => line.Trim())
                .Where(static line => line.StartsWith("#import ", StringComparison.Ordinal))
                .Select(line =>
                {
                    string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return new ScriptReference(engineId, parts[1], parts[3]);
                })
                .ToArray();
        }

        public string RemoveImports(string content) => string.Join(
            '\n',
            content.Split('\n').Where(static line =>
                !line.TrimStart().StartsWith("#import ", StringComparison.Ordinal)));

        public string ImportedComment(ScriptReference reference, int depth) =>
            $"// imported {reference.Name} from {reference.Module}";
    }
}
