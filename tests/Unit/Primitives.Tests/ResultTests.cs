// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Primitives.Tests;

public sealed class ResultTests
{
    [Test]
    public void SuccessHasNoError()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBeNull();
    }

    [Test]
    public void FailureRequiresAndPreservesError()
    {
        var error = new Error("operation.failed", "The operation failed.");

        var result = Result.Failure(error);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeSameAs(error);
        Should.Throw<ArgumentNullException>(() => Result.Failure(null!));
    }

    [Test]
    public void MatchInvokesOnlyTheActiveBranch()
    {
        var successCalls = 0;
        var failureCalls = 0;
        var error = new Error("operation.failed", "The operation failed.");

        var successValue = Result.Success().Match(
            () => ++successCalls,
            _ => ++failureCalls);
        var failureValue = Result.Failure(error).Match(
            () => ++successCalls,
            actual => actual == error ? ++failureCalls : -1);

        successValue.ShouldBe(1);
        failureValue.ShouldBe(1);
        successCalls.ShouldBe(1);
        failureCalls.ShouldBe(1);
    }

    [Test]
    public void MatchRejectsNullDelegates()
    {
        var result = Result.Success();

        Should.Throw<ArgumentNullException>(() => result.Match<string>(null!, _ => "failure"));
        Should.Throw<ArgumentNullException>(() => result.Match(() => "success", null!));
    }

    [Test]
    public void EquivalentResultsUseValueEquality()
    {
        Result.Success().ShouldBe(Result.Success());
        Result.Failure(new Error("same.code", "Same message."))
            .ShouldBe(Result.Failure(new Error("same.code", "Same message.")));
    }
}
