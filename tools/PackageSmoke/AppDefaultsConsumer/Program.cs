// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Hosting;
using Pocok.AppDefaults;

var builder = Host.CreateApplicationBuilder(args);
var configured = false;
builder.ConfigureWith(new InlineConfigurator(() => configured = true));
return configured ? 0 : 1;

file sealed class InlineConfigurator(Action configure) : IApplicationConfigurator
{
    public void Configure(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        configure();
    }
}
