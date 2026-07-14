// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Primitives.Tests;

public sealed class ErrorTests
{
    [Test]
    public void ConstructorPreservesStructuredInformation()
    {
        var exception = new InvalidOperationException("diagnostic detail");

        var error = new Error("storage.unavailable", "Storage is temporarily unavailable.", exception);

        error.Code.ShouldBe("storage.unavailable");
        error.Message.ShouldBe("Storage is temporarily unavailable.");
        error.Exception.ShouldBeSameAs(exception);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void ConstructorRejectsMissingCode(string? code) =>
        Should.Throw<ArgumentException>(() => new Error(code!, "Safe message."));

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void ConstructorRejectsMissingMessage(string? message) =>
        Should.Throw<ArgumentException>(() => new Error("error.code", message!));

    [TestCaseSource(nameof(CancellationExceptions))]
    public void ConstructorRejectsCancellationDiagnostics(Exception exception) =>
        Should.Throw<ArgumentException>(() => new Error("operation.cancelled", "Operation failed.", exception));

    [Test]
    public void FromExceptionRequiresAnException() =>
        Should.Throw<ArgumentNullException>(() => Error.FromException("error.code", "Safe message.", null!));

    [Test]
    public void FromExceptionKeepsSafeMessageSeparateFromDiagnostics()
    {
        var exception = new InvalidOperationException("secret diagnostic detail");

        var error = Error.FromException("operation.failed", "The operation failed.", exception);

        error.Message.ShouldBe("The operation failed.");
        error.Exception.ShouldBeSameAs(exception);
    }

    private static IEnumerable<Exception> CancellationExceptions()
    {
        yield return new OperationCanceledException();
        yield return new TaskCanceledException();
    }
}
