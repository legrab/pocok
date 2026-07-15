// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pocok.AppDefaults.Logging.Serilog;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.AddPocokSerilogDefaults();
using var host = builder.Build();
return host.Services.GetService<Serilog.ILogger>() is null ? 1 : 0;
