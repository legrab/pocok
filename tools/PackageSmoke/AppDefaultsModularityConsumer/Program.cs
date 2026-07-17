// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pocok.AppDefaults.Modularity;
using Pocok.Modularity.Catalog;

var builder = Host.CreateApplicationBuilder(args);
builder.AddPocokModularityDefaults(defaults => defaults.PluginDirectory = "missing-plugins");
using var host = builder.Build();
var defaults = host.Services.GetRequiredService<IOptions<ModularityDefaultsOptions>>().Value;
return host.Services.GetRequiredService<IModuleCatalog>().Modules.Count == 0 && defaults.PluginDirectory == "missing-plugins" ? 0 : 1;
