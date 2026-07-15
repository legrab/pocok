// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Hosting;

namespace Pocok.AppDefaults;

/// <summary>
///     Applies one explicit, startup-time application configuration concern to a host builder.
/// </summary>
public interface IApplicationConfigurator
{
    /// <summary>Configures the supplied builder without building an intermediate service provider.</summary>
    public void Configure(IHostApplicationBuilder builder);
}
