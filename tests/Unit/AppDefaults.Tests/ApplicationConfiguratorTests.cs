// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Hosting;

namespace Pocok.AppDefaults.Tests;

public sealed class ApplicationConfiguratorTests
{
    [Test]
    public void ConfiguratorsRunInExplicitOrderAgainstTheSameBuilder()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        var calls = new List<string>();

        IHostApplicationBuilder returned = builder.ConfigureWith(
            new RecordingConfigurator("first", calls),
            new RecordingConfigurator("second", calls));

        returned.ShouldBeSameAs(builder);
        calls.ShouldBe(["first", "second"]);
    }

    [Test]
    public void EnumerableOverloadPreservesEnumerationOrder()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        var calls = new List<string>();
        IApplicationConfigurator[] configurators =
        [
            new RecordingConfigurator("one", calls),
            new RecordingConfigurator("two", calls)
        ];

        builder.ConfigureWith(configurators.AsEnumerable());

        calls.ShouldBe(["one", "two"]);
    }

    [Test]
    public void NullInputsAreRejectedAtTheCompositionBoundary()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        Should.Throw<ArgumentNullException>(() => ApplicationConfiguratorExtensions.ConfigureWith(null!));
        Should.Throw<ArgumentNullException>(() => builder.ConfigureWith(null!));
        Should.Throw<ArgumentNullException>(() => builder.ConfigureWith([null!]));
    }

    private sealed class RecordingConfigurator(string name, ICollection<string> calls) : IApplicationConfigurator
    {
        public void Configure(IHostApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            calls.Add(name);
        }
    }
}
