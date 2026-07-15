# Pocok.Conversion

`Pocok.Conversion` converts runtime values without process-global registration or serializer fallback. Create a
`ValueConverter` instance, then use the strict invariant defaults or pass a `ConversionContext` that names every relaxed
policy.

Supported targets include booleans, characters, strings, signed and unsigned integral values, floating-point values,
decimal, GUID, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, enums and flags, arrays, common generic
collections, dictionaries, `KeyValuePair<TKey,TValue>`, and `DictionaryEntry`.

```csharp
IValueConverter converter = new ValueConverter();

ConversionResult<int> strict = converter.Convert<int>("42");
ConversionResult<byte> saturated = converter.Convert<byte>(300, new ConversionContext(
    CultureInfo.InvariantCulture,
    overflow: OverflowPolicy.Saturate));
```

## Contract

- Strict defaults use invariant culture, reject overflow and fractional loss, disable numeric booleans, and accept only
  round-trip temporal text.
- Null succeeds only for nullable targets unless `NullPolicy.UseDefault` is selected.
- Enum names are matched ordinally without regard to case. Numeric enum values still pass the selected numeric overflow
  and loss policies.
- Valid flags combinations may contain only bits present in declared enum members.
- `DateTime` text accepts UTC or unspecified round-trip values. Local values and offset-bearing `DateTime` text are
  rejected; use `DateTimeOffset` when an offset matters.
- Collection conversion is recursive and stops at the first failed item. Strings are never treated as arbitrary
  enumerables.
- The converter is stateless and safe for concurrent calls.
- Failures use stable safe error codes; invalid API arguments still throw.
- No serializer fallback exists. Object/JSON conversion is intentionally outside this package.

This package is releasable alpha software. Its reviewed public surface is enforced by package validation and repository
tests.

## Resource bounds and extensibility

Each conversion has an explicit maximum depth and collection-item budget. Nested failures include a path such as
`$[2].value`. Custom strategies are immutable, explicitly ordered, and receive a bounded continuation rather than
converter internals.

The package uses reflection for runtime-selected collection construction. Applications publishing trimmed or NativeAOT
binaries must preserve concrete collection constructors they select dynamically. See the `Conversion.Trimmed` sample and
run publish validation for the actual application graph.
