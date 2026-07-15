// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pocok.AppDefaults.Modularity;
using Pocok.Modularity;

var builder = Host.CreateApplicationBuilder(args);
builder.AddPocokModularityDefaults(defaults => defaults.PluginDirectory = "missing-plugins");
using var host = builder.Build();
return host.Services.GetRequiredService<IModuleCatalog>().Modules.Count == 0 ? 0 : 1;
