// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Channels;

namespace Pocok.Showcase.Contracts;

public enum ShowcaseImplementationStatus
{
    Planned,
    Available,
    Failed
}

public enum ShowcaseRunStatus
{
    Success,
    ExpectedFailure,
    Rejected,
    Cancelled,
    TimedOut,
    InternalFailure
}

public sealed record ShowcaseSliceDescriptor(
    string ModuleId,
    string PackageId,
    string Slug,
    string Family,
    string PackageState,
    string DisplayNameKey,
    string SummaryKey,
    int SortOrder,
    string SourceDocumentationPath,
    bool RuntimeSandboxAvailable,
    ShowcaseImplementationStatus ImplementationStatus,
    string ResourceNamespace,
    string Version)
{
    public ShowcaseSliceDescriptor Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ModuleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(PackageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(Family);
        ArgumentException.ThrowIfNullOrWhiteSpace(PackageState);
        ArgumentException.ThrowIfNullOrWhiteSpace(DisplayNameKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(SummaryKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(SourceDocumentationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(ResourceNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(Version);
        if (!Slug.All(static character => char.IsAsciiLetterOrDigit(character) || character is '-'))
            throw new ArgumentException("The showcase slug must be URL-safe.", nameof(Slug));

        return this;
    }
}

public interface IShowcaseSample
{
    public string Id { get; }
    public string NameKey { get; }
    public string DescriptionKey { get; }
    public bool IsDefault { get; }
    public string ExpectedHeadlineResult { get; }
    public string? GuideAnchor { get; }
    public string? CodeSnippetId { get; }
    public object CreateInput();
}

public sealed class ShowcaseSample<TInput> : IShowcaseSample
    where TInput : class
{
    private readonly Func<TInput> _inputFactory;

    public ShowcaseSample(
        string id,
        string nameKey,
        string descriptionKey,
        Func<TInput> inputFactory,
        bool isDefault,
        string expectedHeadlineResult,
        string? guideAnchor = null,
        string? codeSnippetId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptionKey);
        ArgumentNullException.ThrowIfNull(inputFactory);
        ArgumentNullException.ThrowIfNull(expectedHeadlineResult);
        Id = id;
        NameKey = nameKey;
        DescriptionKey = descriptionKey;
        _inputFactory = inputFactory;
        IsDefault = isDefault;
        ExpectedHeadlineResult = expectedHeadlineResult;
        GuideAnchor = guideAnchor;
        CodeSnippetId = codeSnippetId;
    }

    public string Id { get; }
    public string NameKey { get; }
    public string DescriptionKey { get; }
    public bool IsDefault { get; }
    public string ExpectedHeadlineResult { get; }
    public string? GuideAnchor { get; }
    public string? CodeSnippetId { get; }

    public object CreateInput()
    {
        return CreateTypedInput();
    }

    public TInput CreateTypedInput()
    {
        return _inputFactory();
    }
}

public sealed record ShowcaseGuideSection(
    string Id,
    string TitleKey,
    IReadOnlyList<string> ParagraphKeys,
    IReadOnlyList<string> SnippetIds)
{
    public ShowcaseGuideSection(string id, string titleKey, IEnumerable<string> paragraphKeys)
        : this(id, titleKey, paragraphKeys.ToArray(), [])
    {
    }
}

public sealed record ShowcaseCodeSnippet(string Id, string TitleKey, string Language, string Code);

public sealed record ShowcaseGuide(
    IReadOnlyList<ShowcaseGuideSection> Sections,
    IReadOnlyList<ShowcaseCodeSnippet> Snippets)
{
    public static ShowcaseGuide Empty { get; } = new([], []);
}

public sealed record ShowcaseCodeAssistItem(
    string Id,
    string Label,
    string InsertText,
    string DocumentationKey,
    string Kind,
    bool IsSnippet = false);

public sealed record ShowcaseCodeAssistSignature(string Label, string DocumentationKey);

public sealed record ShowcaseCodeAssistCatalog(
    string LanguageLabel,
    IReadOnlyList<ShowcaseCodeAssistItem> Items,
    IReadOnlyList<ShowcaseCodeAssistSignature> Signatures,
    IReadOnlyList<char> TriggerCharacters)
{
    public static ShowcaseCodeAssistCatalog Empty { get; } = new(
        string.Empty,
        [],
        [],
        []);
}

public sealed record ShowcaseResultField(string Name, string? Value, bool IsCode = false, bool LocalizeName = false);

public sealed record ShowcaseTimelineEvent(DateTimeOffset At, string Kind, string Message);

public sealed record ShowcaseDiagnostic(string Code, string Message, string Severity = "info");

public sealed record ShowcaseProgressEvent(DateTimeOffset At, string Stage, string Message);

public sealed record ShowcaseRunResult
{
    public ShowcaseRunResult(
        ShowcaseRunStatus status,
        string headline,
        IReadOnlyList<ShowcaseResultField>? fields = null,
        IReadOnlyList<ShowcaseTimelineEvent>? timeline = null,
        IReadOnlyList<ShowcaseDiagnostic>? diagnostics = null,
        string? codePreview = null,
        TimeSpan? elapsed = null,
        bool isTruncated = false,
        IReadOnlyList<string>? tipKeys = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headline);
        Status = status;
        Headline = headline;
        Fields = new ReadOnlyCollection<ShowcaseResultField>((fields ?? []).ToArray());
        Timeline = new ReadOnlyCollection<ShowcaseTimelineEvent>((timeline ?? []).ToArray());
        Diagnostics = new ReadOnlyCollection<ShowcaseDiagnostic>((diagnostics ?? []).ToArray());
        CodePreview = codePreview;
        Elapsed = elapsed ?? TimeSpan.Zero;
        IsTruncated = isTruncated;
        TipKeys = new ReadOnlyCollection<string>((tipKeys ?? []).ToArray());
    }

    public ShowcaseRunStatus Status { get; }
    public string Headline { get; }
    public IReadOnlyList<ShowcaseResultField> Fields { get; }
    public IReadOnlyList<ShowcaseTimelineEvent> Timeline { get; }
    public IReadOnlyList<ShowcaseDiagnostic> Diagnostics { get; }
    public string? CodePreview { get; }
    public TimeSpan Elapsed { get; }
    public bool IsTruncated { get; }
    public IReadOnlyList<string> TipKeys { get; }

    public static ShowcaseRunResult Rejected(string headline, string code, string message)
    {
        return new ShowcaseRunResult(ShowcaseRunStatus.Rejected, headline,
            diagnostics: [new ShowcaseDiagnostic(code, message, "warning")]);
    }
}

public interface IShowcaseText
{
    public string GetText(string resourceNamespace, string key);
    public bool Contains(string resourceNamespace, string key, CultureInfo culture);
}

public sealed record ShowcaseResourceRegistration
{
    public ShowcaseResourceRegistration(string resourceNamespace, string rootDirectory, string baseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
        ResourceNamespace = resourceNamespace;
        RootDirectory = Path.GetFullPath(rootDirectory);
        BaseName = baseName;
    }

    public string ResourceNamespace { get; }
    public string RootDirectory { get; }
    public string BaseName { get; }
}

public interface IBoundedOutputWriter
{
    public bool IsTruncated { get; }
    public void Write(string value);
    public void WriteLine(string value);
    public string GetContent();
}

public interface IShowcaseProgressWriter
{
    public ValueTask ReportAsync(string stage, string message, CancellationToken cancellationToken = default);
}

public interface ISafeTemporaryDirectoryFactory
{
    public ValueTask<IAsyncDisposable> CreateAsync(CancellationToken cancellationToken = default);
}

public sealed record ShowcaseExecutionContext(
    TimeProvider TimeProvider,
    IBoundedOutputWriter Output,
    IShowcaseProgressWriter Progress,
    CultureInfo Culture,
    CultureInfo UiCulture,
    ISafeTemporaryDirectoryFactory TemporaryDirectories,
    string CorrelationId,
    IServiceProvider Services);

public interface IShowcaseSlice
{
    public ShowcaseSliceDescriptor Descriptor { get; }
    public Type PageComponentType { get; }
    public Type InputType { get; }
    public Type OutputType { get; }
    public IReadOnlyList<IShowcaseSample> Samples { get; }
    public ShowcaseGuide Guide { get; }
    public ShowcaseCodeAssistCatalog CodeAssist { get; }

    public ValueTask<ShowcaseRunResult> ExecuteUntypedAsync(
        object input,
        ShowcaseExecutionContext context,
        CancellationToken cancellationToken);
}

public interface IShowcaseSlice<TInput, TOutput> : IShowcaseSlice
    where TInput : class
{
    public IReadOnlyList<ShowcaseSample<TInput>> TypedSamples { get; }

    public ValueTask<TOutput> ExecuteAsync(
        TInput input,
        ShowcaseExecutionContext context,
        CancellationToken cancellationToken);
}

public abstract class ShowcaseSlice<TInput, TOutput> : IShowcaseSlice<TInput, TOutput>
    where TInput : class
{
    private IReadOnlyList<IShowcaseSample>? _samples;

    public abstract ShowcaseSliceDescriptor Descriptor { get; }
    public abstract Type PageComponentType { get; }
    public Type InputType => typeof(TInput);
    public Type OutputType => typeof(TOutput);
    public abstract IReadOnlyList<ShowcaseSample<TInput>> TypedSamples { get; }
    public IReadOnlyList<IShowcaseSample> Samples => _samples ??= TypedSamples.Cast<IShowcaseSample>().ToArray();
    public abstract ShowcaseGuide Guide { get; }
    public virtual ShowcaseCodeAssistCatalog CodeAssist => ShowcaseCodeAssistCatalog.Empty;

    public abstract ValueTask<TOutput> ExecuteAsync(
        TInput input,
        ShowcaseExecutionContext context,
        CancellationToken cancellationToken);

    public async ValueTask<ShowcaseRunResult> ExecuteUntypedAsync(
        object input,
        ShowcaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);
        if (input is not TInput typedInput)
            return ShowcaseRunResult.Rejected(
                "Input rejected",
                "showcase.input-type",
                $"Expected {typeof(TInput).Name}, received {input.GetType().Name}.");

        var started = context.TimeProvider.GetTimestamp();
        TOutput output = await ExecuteAsync(typedInput, context, cancellationToken).ConfigureAwait(false);
        TimeSpan elapsed = context.TimeProvider.GetElapsedTime(started);
        return CreateRunResult(output, elapsed);
    }

    protected abstract ShowcaseRunResult CreateRunResult(TOutput output, TimeSpan elapsed);
}

public sealed class ShowcaseRunHandle : IAsyncDisposable
{
    private readonly CancellationTokenSource _cancellation;

    public ShowcaseRunHandle(
        Task<ShowcaseRunResult> completion,
        ChannelReader<ShowcaseProgressEvent> progress,
        CancellationTokenSource cancellation)
    {
        ArgumentNullException.ThrowIfNull(completion);
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(cancellation);
        Completion = completion;
        Progress = progress;
        _cancellation = cancellation;
    }

    public Task<ShowcaseRunResult> Completion { get; }
    public ChannelReader<ShowcaseProgressEvent> Progress { get; }

    public ValueTask DisposeAsync()
    {
        _cancellation.Dispose();
        return ValueTask.CompletedTask;
    }

    public void Cancel()
    {
        _cancellation.Cancel();
    }
}

public interface IShowcaseRunClient : IAsyncDisposable
{
    public ValueTask<ShowcaseRunHandle> SubmitAsync(
        IShowcaseSlice slice,
        object input,
        CultureInfo culture,
        CancellationToken cancellationToken = default);
}

public sealed record ShowcasePackageFact(
    string Id,
    string Family,
    string State,
    string Summary,
    string DocumentationPath,
    int SortOrder,
    ShowcaseImplementationStatus ImplementationStatus,
    string Slug);

public sealed record ShowcasePageContext(
    ShowcaseSliceDescriptor Descriptor,
    IShowcaseText Text,
    IReadOnlyList<IShowcaseSample> Samples,
    IShowcaseRunClient RunClient,
    CultureInfo Culture,
    IReadOnlyList<ShowcasePackageFact> Packages,
    ShowcaseCodeAssistCatalog CodeAssist,
    IShowcaseSlice Slice);
