// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Showcase.Contracts;

public interface IShowcasePackageCoverage
{
    public IReadOnlyList<string> CoveredPackageIds { get; }
}
