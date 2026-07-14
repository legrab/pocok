// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Primitives.Tests;

public sealed class ResultOfTTests
{
    [Test]
    public void SuccessPreservesValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe(42);
        result.Error.ShouldBeNull();
        result.TryGetValue(out var value).ShouldBeTrue();
        value.ShouldBe(42);
    }

    [Test]
    public void SuccessCanContainLegitimateNull()
    {
        var result = Result<string?>.Success(null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.TryGetValue(out var value).ShouldBeTrue();
        value.ShouldBeNull();
    }

    [Test]
    public void FailureHasErrorAndNoAccessibleValue()
    {
        var error = new Error("lookup.missing", "The value was not found.");
        var result = Result<string?>.Failure(error);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeSameAs(error);
        Should.Throw<InvalidOperationException>(() => _ = result.Value);
        result.TryGetValue(out var value).ShouldBeFalse();
        value.ShouldBeNull();
        Should.Throw<ArgumentNullException>(() => Result<string>.Failure(null!));
    }

    [Test]
    public void MatchInvokesOnlyTheActiveBranch()
    {
        var error = new Error("lookup.missing", "The value was not found.");
        var successCalls = 0;
        var failureCalls = 0;

        var successValue = Result<int>.Success(21).Match(
            value => value * 2 + successCalls++,
            _ => failureCalls++);
        var failureValue = Result<int>.Failure(error).Match(
            value => value + successCalls++,
            actual => actual == error ? ++failureCalls : -1);

        successValue.ShouldBe(42);
        failureValue.ShouldBe(1);
        successCalls.ShouldBe(1);
        failureCalls.ShouldBe(1);
    }

    [Test]
    public void MatchRejectsNullDelegates()
    {
        var result = Result<int>.Success(42);

        Should.Throw<ArgumentNullException>(() => result.Match<string>(null!, _ => "failure"));
        Should.Throw<ArgumentNullException>(() => result.Match(_ => "success", null!));
    }

    [Test]
    public void MapTransformsSuccess()
    {
        var result = Result<int>.Success(21).Map(value => value * 2);

        result.ShouldBe(Result<int>.Success(42));
    }

    [Test]
    public void MapPreservesFailureWithoutInvokingSelector()
    {
        var error = new Error("operation.failed", "The operation failed.");
        var invoked = false;

        var result = Result<int>.Failure(error).Map(value =>
        {
            invoked = true;
            return value.ToString(CultureInfo.InvariantCulture);
        });

        invoked.ShouldBeFalse();
        result.Error.ShouldBeSameAs(error);
    }

    [Test]
    public void MapRejectsNullAndPropagatesSelectorExceptions()
    {
        var result = Result<int>.Success(42);
        var exception = new InvalidOperationException("broken invariant");

        Should.Throw<ArgumentNullException>(() => result.Map<string>(null!));
        Should.Throw<InvalidOperationException>(() => result.Map<string>(_ => throw exception))
            .ShouldBeSameAs(exception);
    }

    [Test]
    public void BindChainsSuccess()
    {
        var result = Result<int>.Success(21)
            .Bind(value => Result<string>.Success((value * 2).ToString(CultureInfo.InvariantCulture)));

        result.ShouldBe(Result<string>.Success("42"));
    }

    [Test]
    public void BindPreservesFailureWithoutInvokingSelector()
    {
        var error = new Error("operation.failed", "The operation failed.");
        var invoked = false;

        var result = Result<int>.Failure(error).Bind(value =>
        {
            invoked = true;
            return Result<string>.Success(value.ToString(CultureInfo.InvariantCulture));
        });

        invoked.ShouldBeFalse();
        result.Error.ShouldBeSameAs(error);
    }

    [Test]
    public void BindRejectsInvalidSelectorBehavior()
    {
        var result = Result<int>.Success(42);
        var exception = new InvalidOperationException("broken invariant");

        Should.Throw<ArgumentNullException>(() => result.Bind<string>(null!));
        Should.Throw<InvalidOperationException>(() => result.Bind<string>(_ => null!));
        Should.Throw<InvalidOperationException>(() => result.Bind<string>(_ => throw exception))
            .ShouldBeSameAs(exception);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ToResultDiscardsOnlySuccessfulValue(bool success)
    {
        var error = new Error("operation.failed", "The operation failed.");
        var source = success ? Result<int>.Success(42) : Result<int>.Failure(error);

        var result = source.ToResult();

        result.IsSuccess.ShouldBe(success);
        result.Error.ShouldBe(success ? null : error);
    }

    [Test]
    public void PublicApiDefinesNoImplicitConversions()
    {
        var implicitOperators = new[] { typeof(Result), typeof(Result<int>), typeof(Error) }
            .SelectMany(type => type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            .Where(method => method.Name == "op_Implicit")
            .ToArray();

        implicitOperators.ShouldBeEmpty();
    }
}
