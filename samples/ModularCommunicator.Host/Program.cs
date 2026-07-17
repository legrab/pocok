// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pocok.ModularCommunicator.Contracts;
using Pocok.Modularity;
using Pocok.Modularity.Catalog;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
var pluginDirectory = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.Combine(builder.Environment.ContentRootPath, "plugins");

builder.Services.AddPocokModules(builder.Configuration, options =>
{
    options.AddDirectory(pluginDirectory);
    options.ShareAssemblyContaining<ICommunicator>();
});

using IHost host = builder.Build();
IModuleCatalog catalog = host.Services.GetRequiredService<IModuleCatalog>();
ICommunicator[] communicators = [.. host.Services.GetServices<ICommunicator>()];

foreach (ModuleDescriptor module in catalog.Modules)
    Console.WriteLine($"{module.Identity?.Id ?? "invalid"}: {module.Status}");

foreach (ICommunicator communicator in communicators) Console.WriteLine(communicator.Send("ping"));

return catalog.Modules.Any(module => module.Status == ModuleStatus.Failed) ? 1 : 0;
