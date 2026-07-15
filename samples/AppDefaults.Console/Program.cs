// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pocok.AppDefaults;
using Pocok.AppDefaults.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.ConfigureWith(new LoggingDefaultsConfigurator(options =>
{
    options.MinimumLevel = LogLevel.Information;
    options.CategoryMinimumLevels["Microsoft.Hosting.Lifetime"] = LogLevel.Warning;
}));

using IHost host = builder.Build();
ILogger logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Pocok.Sample");
logger.LogInformation("Pocok application defaults are active.");
