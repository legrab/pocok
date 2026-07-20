// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.AspNetCore.Components;
using Pocok.Showcase.Components;

namespace Pocok.Showcase.Tests;

[TestFixture]
public sealed class MonacoEditorContractTests
{
    [Test]
    public void WrapperExposesTheBufferedEngineAwareContract()
    {
        typeof(IComponent).IsAssignableFrom(typeof(ShowcaseMonacoEditor)).ShouldBeTrue();
        typeof(ShowcaseMonacoEditor).GetProperty(nameof(ShowcaseMonacoEditor.Language))!.PropertyType
            .ShouldBe(typeof(string));
        typeof(ShowcaseMonacoEditor).GetProperty(nameof(ShowcaseMonacoEditor.ResetRevision))!.PropertyType
            .ShouldBe(typeof(long));
        typeof(ShowcaseMonacoEditor).GetProperty(nameof(ShowcaseMonacoEditor.Completions))!.PropertyType
            .ShouldBe(typeof(IReadOnlyList<ShowcaseMonacoCompletion>));
        typeof(ShowcaseMonacoEditor).GetMethod(nameof(ShowcaseMonacoEditor.FlushAsync))
            .ShouldNotBeNull();
        typeof(ShowcaseMonacoEditor).GetMethod(nameof(ShowcaseMonacoEditor.FlushAndGetValueAsync))
            .ShouldNotBeNull();
    }

    [Test]
    public void ExecutionControlsCanResolveInputAfterAnEditorFlush()
    {
        Type controls = typeof(ShowcaseExecutionControls);
        controls.GetProperty(nameof(ShowcaseExecutionControls.BeforeRun))!.PropertyType
            .ShouldBe(typeof(EventCallback));
        controls.GetProperty(nameof(ShowcaseExecutionControls.InputResolver))!.PropertyType
            .ShouldBe(typeof(Func<Task<object?>>));
    }

    [Test]
    public void LocalInteropOwnsCompletionAndThemeDisposal()
    {
        string root = TestSupport.RepositoryRoot;
        string script = File.ReadAllText(Path.Combine(
            root,
            "showcase",
            "src",
            "Pocok.Showcase.Components",
            "wwwroot",
            "monacoEditor.js"));

        script.ShouldNotContain("https://");
        script.ShouldContain("providers.get(id)?.dispose()");
        script.ShouldContain("themeObserver.disconnect()");
        script.ShouldContain("instances.delete(id)");
    }
}
