// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pocok.Modularity;
using Pocok.Modularity.Catalog;

var services = new ServiceCollection();
services.AddPocokModules(new ConfigurationBuilder().Build(), options =>
{
    options.AddDirectory(Path.Combine(AppContext.BaseDirectory, "missing-plugins"));
});
using var provider = services.BuildServiceProvider();
return provider.GetRequiredService<IModuleCatalog>().Modules.Count == 0 ? 0 : 1;
