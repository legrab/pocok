// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;

namespace Pocok.Modularity.Contracts;

/// <summary>
///     Defines a trusted startup module that registers ordinary services before the host is built.
/// </summary>
public interface IServiceModule
{
    /// <summary>Registers the module's services into the host service collection.</summary>
    public void ConfigureServices(IServiceCollection services, ModuleContext context);
}
