// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections.ObjectModel;
using System.Globalization;
using Pocok.Localization.FileResources;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.Web.Services;

public sealed class ShowcaseTextCatalog : IShowcaseText, IAsyncDisposable
{
    private readonly IReadOnlyDictionary<string, FileStringLocalizer> _localizers;
    private readonly bool _development;

    public ShowcaseTextCatalog(
        IEnumerable<ShowcaseResourceRegistration> registrations,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(environment);
        _development = environment.IsDevelopment();
        var localizers = new Dictionary<string, FileStringLocalizer>(StringComparer.OrdinalIgnoreCase);
        foreach (ShowcaseResourceRegistration registration in registrations)
        {
            if (localizers.ContainsKey(registration.ResourceNamespace))
                throw new InvalidOperationException(
                    $"Duplicate showcase resource namespace '{registration.ResourceNamespace}'.");

            localizers.Add(registration.ResourceNamespace, new FileStringLocalizer(new FileStringLocalizerOptions
            {
                RootDirectory = registration.RootDirectory,
                BaseName = registration.BaseName,
                WatchForChanges = false,
                MaximumFileSizeBytes = 262_144
            }));
        }

        _localizers = new ReadOnlyDictionary<string, FileStringLocalizer>(localizers);
    }

    public string GetText(string resourceNamespace, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!_localizers.TryGetValue(resourceNamespace, out FileStringLocalizer? localizer))
            return _development ? $"[{resourceNamespace}:{key}]" : key;

        var value = localizer[key];
        return value.ResourceNotFound && _development ? $"[{resourceNamespace}:{key}]" : value.Value;
    }

    public bool Contains(string resourceNamespace, string key, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        if (!_localizers.TryGetValue(resourceNamespace, out FileStringLocalizer? localizer)) return false;
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.ReadOnly((CultureInfo)culture.Clone());
            return !localizer[key].ResourceNotFound;
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (FileStringLocalizer localizer in _localizers.Values)
            await localizer.DisposeAsync().ConfigureAwait(false);
    }
}
