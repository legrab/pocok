// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Pocok.AppDefaults.Logging.Tests;

public sealed class LoggingDefaultsConfiguratorTests
{
    [Test]
    public void ConfigurationIsBoundBeforeDelegateOverrides()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Pocok:Logging:AddSimpleConsole"] = "false",
            ["Pocok:Logging:MinimumLevel"] = "Warning",
            ["Pocok:Logging:CategoryMinimumLevels:Microsoft"] = "Error"
        });

        builder.AddPocokLoggingDefaults(options => options.MinimumLevel = LogLevel.Debug);
        using IHost host = builder.Build();
        LoggingDefaultsOptions options = host.Services.GetRequiredService<IOptions<LoggingDefaultsOptions>>().Value;
        LoggerFilterOptions filters = host.Services.GetRequiredService<IOptions<LoggerFilterOptions>>().Value;

        options.AddSimpleConsole.ShouldBeFalse();
        options.MinimumLevel.ShouldBe(LogLevel.Debug);
        options.CategoryMinimumLevels["Microsoft"].ShouldBe(LogLevel.Error);
        host.Services.GetService<LoggingDefaultsOptions>().ShouldBeNull();
        filters.MinLevel.ShouldBe(LogLevel.Debug);
        filters.Rules.ShouldContain(rule => rule.CategoryName == "Microsoft" && rule.LogLevel == LogLevel.Error);
    }

    [Test]
    public void DuplicateApplicationIsRejected()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddPocokLoggingDefaults(options => options.MinimumLevel = LogLevel.Warning);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            builder.AddPocokLoggingDefaults(options => options.MinimumLevel = LogLevel.Trace));

        exception.Message.ShouldContain("already been applied");
    }

    [Test]
    public void ApplicationCanOverrideMinimumLevelAfterDefaults()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddPocokLoggingDefaults(options => options.MinimumLevel = LogLevel.Warning);
        builder.Logging.SetMinimumLevel(LogLevel.Trace);

        using IHost host = builder.Build();
        host.Services.GetRequiredService<IOptions<LoggerFilterOptions>>().Value.MinLevel.ShouldBe(LogLevel.Trace);
    }

    [Test]
    public void DefaultPolicyDoesNotRegisterAnotherConsoleProvider()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        var providerRegistrationsBefore =
            builder.Services.Count(descriptor => descriptor.ServiceType == typeof(ILoggerProvider));

        builder.AddPocokLoggingDefaults();

        var providerRegistrationsAfter =
            builder.Services.Count(descriptor => descriptor.ServiceType == typeof(ILoggerProvider));
        providerRegistrationsAfter.ShouldBe(providerRegistrationsBefore);

        using IHost host = builder.Build();
        host.Services.GetRequiredService<IOptions<LoggingDefaultsOptions>>().Value.AddSimpleConsole.ShouldBeFalse();
    }

    [Test]
    public void ExistingProvidersArePreservedByDefault()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        var provider = new NullLoggerProvider();
        builder.Logging.AddProvider(provider);

        builder.AddPocokLoggingDefaults(options => options.AddSimpleConsole = false);
        using IHost host = builder.Build();

        host.Services.GetServices<ILoggerProvider>().ShouldContain(provider);
    }

    [Test]
    public void ExistingProvidersAreClearedOnlyWhenExplicitlyRequested()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        var provider = new NullLoggerProvider();
        builder.Logging.AddProvider(provider);

        builder.AddPocokLoggingDefaults(options =>
        {
            options.AddSimpleConsole = false;
            options.ClearProviders = true;
        });
        using IHost host = builder.Build();

        host.Services.GetServices<ILoggerProvider>().ShouldNotContain(provider);
    }

    [Test]
    public void ConsoleOnlySettingsAreIgnoredWhenConsoleRegistrationIsDisabled()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        Should.NotThrow(() => builder.AddPocokLoggingDefaults(options =>
        {
            options.AddSimpleConsole = false;
            options.TimestampFormat = string.Empty;
        }));
    }

    private sealed class NullLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new NullLogger();
        }

        public void Dispose()
        {
        }
    }

    private sealed class NullLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }
}
