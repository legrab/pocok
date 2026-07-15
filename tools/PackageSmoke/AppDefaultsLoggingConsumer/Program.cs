// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pocok.AppDefaults.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.AddPocokLoggingDefaults(options =>
{
    options.AddSimpleConsole = false;
    options.MinimumLevel = LogLevel.Warning;
});
using var host = builder.Build();
return host.Services.GetRequiredService<LoggingDefaultsOptions>().MinimumLevel == LogLevel.Warning ? 0 : 1;
