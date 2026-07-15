// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Modularity.FixtureDependency;

public sealed class GreetingSuffix
{
    public static string Value => "!";
    public override string ToString() => Value;
}
