// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.AspNetCore.Components;
using Pocok.Showcase.Components;

namespace Pocok.Showcase.Tests;

[TestFixture]
public sealed class CodeAssistEditorStateTests
{
    [Test]
    public void BufferedTextAreaExposesTheStandardBindingContract()
    {
        typeof(IComponent).IsAssignableFrom(typeof(ShowcaseBufferedTextArea)).ShouldBeTrue();
        typeof(ShowcaseBufferedTextArea).GetProperty(nameof(ShowcaseBufferedTextArea.Value))!.PropertyType
            .ShouldBe(typeof(string));
        typeof(ShowcaseBufferedTextArea).GetProperty(nameof(ShowcaseBufferedTextArea.ValueChanged))!.PropertyType
            .ShouldBe(typeof(EventCallback<string>));
    }

    [Test]
    public void CodeAssistEditorExposesAnOptionalValueAction()
    {
        typeof(ShowcaseCodeAssistEditor).GetProperty(nameof(ShowcaseCodeAssistEditor.ActionLabel))!.PropertyType
            .ShouldBe(typeof(string));
        typeof(ShowcaseCodeAssistEditor).GetProperty(nameof(ShowcaseCodeAssistEditor.ActionRequested))!.PropertyType
            .ShouldBe(typeof(EventCallback<string>));
    }

    [Test]
    public void LocalInputAndParentAcknowledgementDoNotRewriteTheRenderedValue()
    {
        var value = new BufferedEditorValue();
        value.SetParameter("first\nsecond");

        value.SetInput("first\nsecond // comment");
        bool replaced = value.SetParameter("first\nsecond // comment");

        replaced.ShouldBeFalse();
        value.CurrentValue.ShouldBe("first\nsecond // comment");
        value.RenderedValue.ShouldBe("first\nsecond");
        value.Revision.ShouldBe(0);
        value.HasUncommittedInput.ShouldBeFalse();
    }

    [Test]
    public void ExternalValueChangeForcesANewTextareaEvenWhenItMatchesTheOldRenderedValue()
    {
        var value = new BufferedEditorValue();
        value.SetParameter("sample");
        value.SetInput("sample edited");
        value.SetParameter("sample edited");

        bool replaced = value.SetParameter("sample");

        replaced.ShouldBeTrue();
        value.CurrentValue.ShouldBe("sample");
        value.RenderedValue.ShouldBe("sample");
        value.Revision.ShouldBe(1);
    }

    [Test]
    public async Task DebounceCommitsOnlyTheLatestScheduledValue()
    {
        var committed = new List<string>();
        using var committer = new DebouncedValueCommitter<string>(
            TimeSpan.FromMilliseconds(25),
            value =>
            {
                committed.Add(value);
                return Task.CompletedTask;
            });

        Task first = committer.ScheduleAsync("first");
        Task second = committer.ScheduleAsync("second");
        await Task.WhenAll(first, second);

        committed.ShouldBe(["second"]);
    }

    [Test]
    public async Task FlushCancelsTheDelayAndCommitsImmediately()
    {
        var committed = new List<string>();
        using var committer = new DebouncedValueCommitter<string>(
            TimeSpan.FromMinutes(1),
            value =>
            {
                committed.Add(value);
                return Task.CompletedTask;
            });

        Task pending = committer.ScheduleAsync("pending");
        await committer.FlushAsync("blurred");
        await pending;

        committed.ShouldBe(["blurred"]);
    }
}
