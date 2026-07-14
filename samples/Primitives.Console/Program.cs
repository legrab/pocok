// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Primitives;

var input = args.FirstOrDefault() ?? "21";
var invalidNumber = new Error("number.invalid", "Enter a valid whole number.");

var parsed = int.TryParse(input, out var value)
    ? Result<int>.Success(value)
    : Result<int>.Failure(invalidNumber);

var message = parsed
    .Map(value => value * 2)
    .Match(
        value => $"Twice the value is {value}.",
        error => $"{error.Code}: {error.Message}");

Console.WriteLine(message);
