// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.Web.Services;

public sealed record ShowcasePackageCatalogEntry(
    string Id,
    string Family,
    string State,
    bool Releasable,
    string Project,
    string Summary,
    string DocumentationPath,
    int SortOrder,
    string Slug);

public sealed class ShowcasePackageCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly ReadOnlyDictionary<string, ShowcasePackageCatalogEntry> _byId;
    private readonly ReadOnlyDictionary<string, ShowcasePackageCatalogEntry> _bySlug;

    public ShowcasePackageCatalog(IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        string path = Path.Combine(environment.ContentRootPath, "Content", "package-catalog.json");
        using FileStream stream = File.OpenRead(path);
        PackageCatalogDocument document = JsonSerializer.Deserialize<PackageCatalogDocument>(
            stream,
            SerializerOptions)
            ?? throw new InvalidOperationException("The showcase package catalog is empty.");

        ShowcasePackageCatalogEntry[] packages = document.Packages
            .OrderBy(package => package.SortOrder)
            .ThenBy(package => package.Id, StringComparer.Ordinal)
            .ToArray();

        if (packages.Length == 0)
            throw new InvalidOperationException("The showcase package catalog contains no current packages.");

        ValidateUnique(packages, package => package.Id, "package id", StringComparer.Ordinal);
        ValidateUnique(packages, package => package.Slug, "slug", StringComparer.OrdinalIgnoreCase);
        foreach (ShowcasePackageCatalogEntry package in packages)
        {
            if (string.IsNullOrWhiteSpace(package.Slug)
                || package.Slug.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
                throw new InvalidOperationException($"Package '{package.Id}' has an unsafe showcase slug.");
        }

        Packages = new ReadOnlyCollection<ShowcasePackageCatalogEntry>(packages);
        _byId = new ReadOnlyDictionary<string, ShowcasePackageCatalogEntry>(
            packages.ToDictionary(package => package.Id, StringComparer.Ordinal));
        _bySlug = new ReadOnlyDictionary<string, ShowcasePackageCatalogEntry>(
            packages.ToDictionary(package => package.Slug, StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<ShowcasePackageCatalogEntry> Packages { get; }
    public IReadOnlyList<ShowcasePackageCatalogEntry> Current => Packages;

    public ShowcasePackageCatalogEntry? Find(string packageId) =>
        _byId.TryGetValue(packageId, out ShowcasePackageCatalogEntry? package) ? package : null;

    public ShowcasePackageCatalogEntry? FindBySlug(string slug) =>
        _bySlug.TryGetValue(slug, out ShowcasePackageCatalogEntry? package) ? package : null;

    private static void ValidateUnique(
        IEnumerable<ShowcasePackageCatalogEntry> packages,
        Func<ShowcasePackageCatalogEntry, string> selector,
        string label,
        StringComparer comparer)
    {
        string[] duplicates = packages
            .GroupBy(selector, comparer)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicates.Length > 0)
            throw new InvalidOperationException($"The showcase package catalog contains duplicate {label}s: {string.Join(", ", duplicates)}.");
    }

    private sealed record PackageCatalogDocument(IReadOnlyList<ShowcasePackageCatalogEntry> Packages);
}

public sealed class ShowcaseSliceCatalog
{
    private readonly ReadOnlyDictionary<string, IShowcaseSlice> _bySlug;
    private readonly ReadOnlyDictionary<string, IShowcaseSlice> _byPackageId;

    public ShowcaseSliceCatalog(
        IEnumerable<IShowcaseSlice> slices,
        ShowcasePackageCatalog packages,
        IOptions<ShowcaseOptions> options)
    {
        ArgumentNullException.ThrowIfNull(slices);
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(options);

        IShowcaseSlice[] installed = slices
            .OrderBy(slice => slice.Descriptor.SortOrder)
            .ThenBy(slice => slice.Descriptor.PackageId, StringComparer.Ordinal)
            .ToArray();

        Validate(installed, packages, options.Value.RequireCompleteCatalog);
        Installed = new ReadOnlyCollection<IShowcaseSlice>(installed);
        _bySlug = new ReadOnlyDictionary<string, IShowcaseSlice>(
            installed.ToDictionary(slice => slice.Descriptor.Slug, StringComparer.OrdinalIgnoreCase));
        _byPackageId = new ReadOnlyDictionary<string, IShowcaseSlice>(
            installed.ToDictionary(slice => slice.Descriptor.PackageId, StringComparer.Ordinal));
    }

    public IReadOnlyList<IShowcaseSlice> Installed { get; }
    public IReadOnlyList<IShowcaseSlice> All => Installed;

    public IShowcaseSlice? FindBySlug(string slug) =>
        _bySlug.TryGetValue(slug, out IShowcaseSlice? slice) ? slice : null;

    public IShowcaseSlice? FindByPackageId(string packageId) =>
        _byPackageId.TryGetValue(packageId, out IShowcaseSlice? slice) ? slice : null;

    public IReadOnlyList<ShowcasePackageFact> CreateFacts(ShowcasePackageCatalog packages)
    {
        ArgumentNullException.ThrowIfNull(packages);
        return packages.Packages.Select(package =>
        {
            IShowcaseSlice? slice = FindByPackageId(package.Id);
            return new ShowcasePackageFact(
                package.Id,
                package.Family,
                package.State,
                package.Summary,
                package.DocumentationPath,
                package.SortOrder,
                slice is null ? ShowcaseImplementationStatus.Planned : ShowcaseImplementationStatus.Available,
                package.Slug);
        }).ToArray();
    }

    private static void Validate(
        IReadOnlyList<IShowcaseSlice> installed,
        ShowcasePackageCatalog packages,
        bool requireCompleteCatalog)
    {
        EnsureUnique(installed, slice => slice.Descriptor.ModuleId, "module id", StringComparer.Ordinal);
        EnsureUnique(installed, slice => slice.Descriptor.PackageId, "package id", StringComparer.Ordinal);
        EnsureUnique(installed, slice => slice.Descriptor.Slug, "slug", StringComparer.OrdinalIgnoreCase);

        foreach (IShowcaseSlice slice in installed)
        {
            slice.Descriptor.Validate();
            if (!typeof(IComponent).IsAssignableFrom(slice.PageComponentType))
                throw new InvalidOperationException($"Page type '{slice.PageComponentType}' does not implement IComponent.");
            if (slice.Samples.Count(sample => sample.IsDefault) != 1)
                throw new InvalidOperationException($"Slice '{slice.Descriptor.PackageId}' must have exactly one default sample.");
            if (slice.Samples.Select(sample => sample.Id).Distinct(StringComparer.Ordinal).Count() != slice.Samples.Count)
                throw new InvalidOperationException($"Slice '{slice.Descriptor.PackageId}' contains duplicate sample ids.");

            ShowcasePackageCatalogEntry package = packages.Find(slice.Descriptor.PackageId)
                ?? throw new InvalidOperationException($"Slice '{slice.Descriptor.PackageId}' is absent from the package catalog.");
            if (!string.Equals(package.Slug, slice.Descriptor.Slug, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Slice '{slice.Descriptor.PackageId}' uses slug '{slice.Descriptor.Slug}', but the package catalog uses '{package.Slug}'.");
        }

        if (requireCompleteCatalog)
        {
            string[] missing = packages.Packages
                .Where(package => installed.All(slice => slice.Descriptor.PackageId != package.Id))
                .Select(package => package.Id)
                .ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException($"Strict showcase catalog is missing: {string.Join(", ", missing)}.");
        }
    }

    private static void EnsureUnique(
        IEnumerable<IShowcaseSlice> installed,
        Func<IShowcaseSlice, string> selector,
        string label,
        StringComparer comparer)
    {
        string[] duplicates = installed
            .GroupBy(selector, comparer)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicates.Length > 0)
            throw new InvalidOperationException($"Duplicate showcase {label}: {string.Join(", ", duplicates)}.");
    }
}
