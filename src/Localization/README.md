# Pocok.Localization

Compatibility tier: experimental alpha. The package is packable and tested but is not release-eligible until its Windows and Ubuntu acceptance gate passes.

`Pocok.Localization` provides deterministic composition over standard .NET `IStringLocalizer` providers, enum-resource fallback, resource-file culture parsing, and an external file-backed localizer for JSON and string-only RESX resources.

## External JSON and RESX files

`FileStringLocalizer` loads one logical resource base set from a configured root directory:

```text
Resources/Messages.json
Resources/Messages.de.json
Resources/Messages.de-DE.json
Resources/Messages.resx
Resources/Messages.de.resx
Resources/Messages.de-DE.resx
```

```csharp
await using var files = new FileStringLocalizer(new FileStringLocalizerOptions
{
    RootDirectory = AppContext.BaseDirectory,
    BaseName = "Resources/Messages",
    WatchForChanges = true
});
```

JSON resources may be flat or nested. Nested properties are flattened with dots:

```json
{
  "Greeting": "Hello {0}",
  "Navigation": {
    "Settings": "Settings"
  }
}
```

The resulting keys are `Greeting` and `Navigation.Settings`. JSON files must use UTF-8; an optional UTF-8 byte-order mark is accepted, while UTF-16 and malformed UTF-8 are rejected. JSON accepts only objects and string leaves. Arrays, numbers, booleans, nulls, duplicate properties, and flattened-key collisions are rejected.

RESX loading accepts only plain `<data name="..."><value>...</value></data>` string entries. Type metadata, MIME metadata, file references, DTDs, and external entities are rejected. The package does not deserialize arbitrary RESX objects.

## Culture and format precedence

Resource selection uses `CultureInfo.CurrentUICulture`. For `de-AT`, each key falls back through `de-AT`, `de`, and then invariant resources. Formatting uses `CultureInfo.CurrentCulture` and normal `string.Format` behavior.

For same-culture conflicts, `FormatPrecedence` is ordered from highest to lowest precedence. JSON wins over RESX by default. A culture-specific resource always wins over a parent or invariant resource regardless of format.

Keys use ordinal, case-sensitive comparison. Enumeration is deterministic, emits exact culture before parents, and suppresses later duplicate keys.

## Reloading and watching

`ReloadAsync` parses every matching file into a new immutable snapshot and publishes it atomically only when the complete resource set is valid. Readers therefore observe either the previous complete snapshot or the new complete snapshot.

Reload failures retain the last-known-good snapshot, update `Status`, and throw to the manual caller. Retry timing uses `TimeProvider`. By default, deleting a previously loaded source also retains the last-known-good snapshot. Set `MissingFileBehavior` to `RemoveMissingResources` when deletion should publish a new snapshot.

Optional file watching uses `Pocok.BackgroundWork.DebouncedTaskRunner` to collapse event bursts. Watcher-driven failures are stored in `Status`; they do not terminate the process. Dispose the localizer asynchronously to stop watching. The final snapshot remains readable after disposal, while further reloads are rejected.

## Composition and enum translation

`CompositeStringLocalizer` accepts providers in precedence order. The first provider containing a key wins. This makes external resources easy to place before embedded or application-specific providers:

```csharp
var localizer = new CompositeStringLocalizer([
    files,
    embeddedLocalizer
]);
```

`EnumLocalizationExtensions.Translate` looks up `EnumType.Member` first and the bare member name second. `ResourceCulture` parses a culture suffix from a resource filename without mutating process culture.

The package does not provide database localization, assembly scanning, a global `IStringLocalizerFactory`, TypeScript generation, source-key analysis, named interpolation, remote resources, or global culture mutation.
