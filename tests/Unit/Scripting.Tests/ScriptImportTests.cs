// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting;

namespace Pocok.Scripting.Tests;

public sealed class ScriptImportTests
{
    [Test]
    public void ModuleBoundariesRejectNullDependencies()
    {
        Should.Throw<ArgumentNullException>(() => new ScriptModuleResolver(null!));
        Should.Throw<ArgumentNullException>(() => new ScriptImportInjector(null!));
    }

    [Test]
    public async Task ResolverReturnsDependencyFirstOrderAndBreaksCycles()
    {
        var source = new InMemoryScriptModuleSource([
            new ScriptModule("root", "Test", "// #import middle from Test\nroot();"),
            new ScriptModule("middle", "Test", "// #import base from Test\nmiddle();"),
            new ScriptModule("base", "Test", "// #import root from Test\nbase();")
        ]);

        ScriptImportResolution result = await new ScriptModuleResolver(source)
            .ResolveAsync(new ScriptReference("root", "Test"));

        result.Scripts.Select(static script => script.Reference.Name)
            .ShouldBe(["base", "middle", "root"]);
        result.Diagnostics.ShouldContain(static diagnostic => diagnostic.Code == "scripting.import.cycle");
    }

    [Test]
    public async Task InjectorExpandsTransitiveImportsAndReportsMissingModules()
    {
        var source = new InMemoryScriptModuleSource([
            new ScriptModule("math", "Test", "function add(a, b) { return a + b; }")
        ]);
        var injector = new ScriptImportInjector(new ScriptModuleResolver(source));

        InjectedScript result = await injector.InjectAsync(
            "// #import math from Test\n// #import missing from Test\nadd(2, 3);");

        result.Content.ShouldContain("function add");
        result.Content.ShouldNotContain("#import math from Test");
        result.Diagnostics.ShouldContain(static diagnostic => diagnostic.Code == "scripting.import.missing");
    }

    [Test]
    public void ParserIgnoresImportTextInsideSourceCode()
    {
        ScriptImportParser.FindImports("""
            const text = "// #import fake from Test";
            // #import real from Test
            """)
            .Select(static reference => reference.Name)
            .ShouldBe(["real"]);
    }
}
