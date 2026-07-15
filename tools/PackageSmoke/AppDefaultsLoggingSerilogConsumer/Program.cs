// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pocok.AppDefaults.Logging.Serilog;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.AddPocokSerilogDefaults();
using var host = builder.Build();
return host.Services.GetService<Serilog.ILogger>() is not null &&
       host.Services.GetService<IOptions<SerilogDefaultsOptions>>() is not null
    ? 0
    : 1;
