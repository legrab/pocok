// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using Pocok.Scripting.CSharp.Worker;

const int SupportedProtocolVersion = 1;
string[] defaultImports =
[
    "System",
    "System.Collections.Generic",
    "System.Linq",
    "System.Threading",
    "System.Threading.Tasks"
];
string[] bannedCapabilityNames =
[
    "File",
    "Directory",
    "Process",
    "Environment",
    "Assembly",
    "Activator",
    "Registry",
    "HttpClient",
    "WebRequest",
    "Socket",
    "DllImport",
    "GetType",
    "Console",
    "AppDomain",
    "GC",
    "Marshal",
    "NativeLibrary",
    "AssemblyLoadContext",
    "Thread",
    "ThreadPool"
];

string input = await Console.In.ReadToEndAsync();
WorkerRequest? request;
try
{
    request = JsonSerializer.Deserialize<WorkerRequest>(input);
}
catch (JsonException)
{
    await WriteAsync(WorkerResponse.Fail(
        "scripting.csharp.protocol",
        "The worker request is invalid."));
    return;
}

if (request is null || string.IsNullOrWhiteSpace(request.Operation))
{
    await WriteAsync(WorkerResponse.Fail(
        "scripting.csharp.protocol",
        "The worker request is missing."));
    return;
}
if (request.ProtocolVersion != SupportedProtocolVersion)
{
    await WriteAsync(WorkerResponse.Fail(
        "scripting.csharp.protocol_version",
        "The C# worker protocol version is unsupported."));
    return;
}

string[] allowedImports = request.AllowedImports?
    .Where(static value => !string.IsNullOrWhiteSpace(value))
    .Distinct(StringComparer.Ordinal)
    .ToArray() ?? [];
string[] allowedReferencePaths = request.AllowedReferencePaths?
    .Where(static value => !string.IsNullOrWhiteSpace(value))
    .Select(Path.GetFullPath)
    .Distinct(OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal)
    .ToArray() ?? [];

IReadOnlyList<WorkerDiagnostic> policy = ValidatePolicy(
    request.Source,
    allowedImports,
    defaultImports,
    bannedCapabilityNames);
if (policy.Any(static item => item.Severity == "error"))
{
    await WriteAsync(WorkerResponse.Fail(policy));
    return;
}

foreach (string referencePath in allowedReferencePaths)
{
    if (!File.Exists(referencePath) ||
        !string.Equals(Path.GetExtension(referencePath), ".dll", StringComparison.OrdinalIgnoreCase))
    {
        await WriteAsync(WorkerResponse.Fail(
            "scripting.csharp.reference_unavailable",
            "A configured C# metadata reference is unavailable."));
        return;
    }
}

ScriptOptions scriptOptions = ScriptOptions.Default
    .WithReferences(GetDefaultReferences())
    .WithImports(defaultImports.Concat(allowedImports));
if (allowedReferencePaths.Length > 0)
{
    scriptOptions = scriptOptions.AddReferences(
        allowedReferencePaths.Select(static path => MetadataReference.CreateFromFile(path)));
}

var script = CSharpScript.Create<object?>(
    request.Source,
    scriptOptions,
    typeof(ScriptGlobals));
ImmutableArray<Diagnostic> compileDiagnostics = script.Compile();
WorkerDiagnostic[] diagnostics = compileDiagnostics
    .Where(static item => item.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error)
    .Select(ToDiagnostic)
    .Take(100)
    .ToArray();
if (compileDiagnostics.Any(static item => item.Severity == DiagnosticSeverity.Error))
{
    await WriteAsync(WorkerResponse.Fail(diagnostics));
    return;
}

if (request.Operation == "validate")
{
    await WriteAsync(new WorkerResponse(
        true,
        null,
        null,
        null,
        null,
        null,
        diagnostics));
    return;
}
if (request.Operation != "execute")
{
    await WriteAsync(WorkerResponse.Fail(
        "scripting.csharp.protocol",
        "Unknown worker operation."));
    return;
}

try
{
    ScriptState<object?> state = await script.RunAsync(new ScriptGlobals(request.Bindings));
    object? value = state.ReturnValue;
    if (request.ExpectResult && value is null)
    {
        await WriteAsync(WorkerResponse.Fail(
            "scripting.result.missing",
            "The script was expected to return a value."));
        return;
    }

    JsonElement result = JsonSerializer.SerializeToElement(
        value,
        value?.GetType() ?? typeof(object));
    await WriteAsync(new WorkerResponse(
        true,
        result,
        null,
        null,
        null,
        null,
        diagnostics));
}
catch (CompilationErrorException exception)
{
    await WriteAsync(WorkerResponse.Fail(
        exception.Diagnostics.Select(ToDiagnostic).Take(100).ToArray()));
}
catch (Exception)
{
    await WriteAsync(WorkerResponse.Fail(
        "scripting.csharp.execution",
        "C# execution failed safely."));
}

static IReadOnlyList<MetadataReference> GetDefaultReferences()
{
    string[] allowedAssemblyNames =
    [
        "System.Private.CoreLib",
        "System.Runtime",
        "netstandard",
        "System.Collections",
        "System.Linq",
        "System.Threading",
        "System.Threading.Tasks",
        "System.Text.Json"
    ];
    var allowed = new HashSet<string>(allowedAssemblyNames, StringComparer.OrdinalIgnoreCase);
    string[] platformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries) ?? [];
    MetadataReference[] references = platformAssemblies
        .Where(path => allowed.Contains(Path.GetFileNameWithoutExtension(path)))
        .Select(static path => MetadataReference.CreateFromFile(path))
        .ToArray();
    if (references.Length == 0)
        throw new InvalidOperationException("The fixed C# framework reference set is unavailable.");
    return references;
}

static IReadOnlyList<WorkerDiagnostic> ValidatePolicy(
    string source,
    IReadOnlyList<string> allowedImports,
    IReadOnlyList<string> defaultImports,
    IReadOnlyList<string> bannedCapabilityNames)
{
    SyntaxTree tree = CSharpSyntaxTree.ParseText(
        source,
        new CSharpParseOptions(kind: SourceCodeKind.Script));
    var result = new List<WorkerDiagnostic>();
    string[] bannedRoots =
    [
        "System.IO",
        "System.Net",
        "System.Reflection",
        "System.Runtime.Loader",
        "System.Diagnostics",
        "Microsoft.Win32"
    ];

    foreach (UsingDirectiveSyntax directive in tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>())
    {
        string name = directive.Name?.ToString() ?? string.Empty;
        if (bannedRoots.Any(root => name.StartsWith(root, StringComparison.Ordinal)) ||
            (!defaultImports.Contains(name, StringComparer.Ordinal) &&
             !allowedImports.Contains(name, StringComparer.Ordinal)))
        {
            result.Add(At(
                "scripting.csharp.import_denied",
                "The requested namespace is not allowlisted.",
                directive));
        }
    }

    foreach (SyntaxTrivia trivia in tree.GetRoot().DescendantTrivia(descendIntoTrivia: true))
    {
        if (trivia.GetStructure() is ReferenceDirectiveTriviaSyntax or LoadDirectiveTriviaSyntax)
        {
            result.Add(At(
                "scripting.csharp.directive_denied",
                "#r and #load are not allowed.",
                trivia.GetStructure()!));
        }
    }

    foreach (SyntaxNode node in tree.GetRoot().DescendantNodesAndSelf())
    {
        if (node is UnsafeStatementSyntax or
            PointerTypeSyntax or
            FunctionPointerTypeSyntax or
            StackAllocArrayCreationExpressionSyntax or
            TypeOfExpressionSyntax)
        {
            result.Add(At(
                "scripting.csharp.unsafe_denied",
                "Unsafe and native code are not allowed.",
                node));
        }

        if (node is IdentifierNameSyntax identifier &&
            identifier.Identifier.ValueText == "dynamic")
        {
            result.Add(At(
                "scripting.csharp.dynamic_denied",
                "Dynamic dispatch is not allowed.",
                node));
        }

        if (node is MemberAccessExpressionSyntax member)
        {
            string expression = member.ToString().Replace("global::", string.Empty, StringComparison.Ordinal);
            string memberName = member.Name.Identifier.ValueText;
            if (bannedRoots.Any(root => expression.StartsWith(root + ".", StringComparison.Ordinal)) ||
                bannedCapabilityNames.Contains(memberName, StringComparer.Ordinal) ||
                bannedCapabilityNames.Any(name =>
                    expression.StartsWith(name + ".", StringComparison.Ordinal) ||
                    expression.Contains("." + name + ".", StringComparison.Ordinal)))
            {
                result.Add(At(
                    "scripting.csharp.capability_denied",
                    "The source requests a denied host capability.",
                    node));
            }
        }

        if (node is ObjectCreationExpressionSyntax creation)
        {
            string typeName = creation.Type.ToString().Replace("global::", string.Empty, StringComparison.Ordinal);
            if (!bannedCapabilityNames.Contains(typeName, StringComparer.Ordinal) &&
                !bannedRoots.Any(root => typeName.StartsWith(root + ".", StringComparison.Ordinal)))
                continue;
            result.Add(At(
                "scripting.csharp.capability_denied",
                "The source requests a denied host capability.",
                node));
        }

        if (node is AttributeSyntax attribute &&
            bannedCapabilityNames.Any(name =>
                attribute.Name.ToString().Equals(name, StringComparison.Ordinal) ||
                attribute.Name.ToString().Equals(name + "Attribute", StringComparison.Ordinal)))
        {
            result.Add(At(
                "scripting.csharp.native_denied",
                "Native interop attributes are not allowed.",
                node));
        }
    }

    return result
        .Concat(tree.GetDiagnostics()
            .Where(static item => item.Severity == DiagnosticSeverity.Error)
            .Select(ToDiagnostic))
        .DistinctBy(static item => (item.Code, item.Line, item.Column))
        .Take(100)
        .ToArray();
}

static WorkerDiagnostic At(string code, string message, SyntaxNode node)
{
    FileLinePositionSpan span = node.GetLocation().GetLineSpan();
    return new WorkerDiagnostic(
        code,
        message,
        "error",
        span.StartLinePosition.Line + 1,
        span.StartLinePosition.Character + 1);
}

static WorkerDiagnostic ToDiagnostic(Diagnostic diagnostic)
{
    FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
    return new WorkerDiagnostic(
        "scripting.csharp." + diagnostic.Id.ToLowerInvariant(),
        diagnostic.GetMessage(CultureInfo.InvariantCulture),
        diagnostic.Severity == DiagnosticSeverity.Warning ? "warning" : "error",
        diagnostic.Location == Location.None ? null : span.StartLinePosition.Line + 1,
        diagnostic.Location == Location.None ? null : span.StartLinePosition.Character + 1);
}

static Task WriteAsync(WorkerResponse response) =>
    Console.Out.WriteAsync(JsonSerializer.Serialize(response));

internal sealed record WorkerRequest(
    int ProtocolVersion,
    string Operation,
    string Source,
    bool ExpectResult,
    IReadOnlyDictionary<string, JsonElement> Bindings,
    IReadOnlyList<string>? AllowedImports,
    IReadOnlyList<string>? AllowedReferencePaths);

internal sealed record WorkerResponse(
    bool Success,
    JsonElement? Result,
    string? Code,
    string? Message,
    int? Line,
    int? Column,
    IReadOnlyList<WorkerDiagnostic>? Diagnostics)
{
    public static WorkerResponse Fail(string code, string message) =>
        new(false, null, code, message, null, null, null);

    public static WorkerResponse Fail(IReadOnlyList<WorkerDiagnostic> diagnostics)
    {
        WorkerDiagnostic first = diagnostics.Count > 0
            ? diagnostics[0]
            : new WorkerDiagnostic(
                "scripting.csharp.validation",
                "C# validation failed.",
                "error",
                null,
                null);
        return new WorkerResponse(
            false,
            null,
            first.Code,
            first.Message,
            first.Line,
            first.Column,
            diagnostics);
    }
}

internal sealed record WorkerDiagnostic(
    string Code,
    string Message,
    string Severity,
    int? Line,
    int? Column);
