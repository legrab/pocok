// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Pocok.Modularity;

namespace Pocok.Samples.ModularCommunicator.EchoPlugin;

public sealed class EchoCommunicatorModule : IServiceModule
{
    public void ConfigureServices(IServiceCollection services, ModuleContext context)
    {
        services.AddSingleton<ICommunicator>(new EchoCommunicator(context.Identity.Id));
    }

    private sealed class EchoCommunicator(string id) : ICommunicator
    {
        public string Id { get; } = id;

        public string Send(string request)
        {
            return $"{Id}: {request}";
        }
    }
}
