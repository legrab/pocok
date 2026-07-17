# Localization console sample

This runnable sample demonstrates external JSON and RESX resources composed with another standard `IStringLocalizer`.

Run from the repository root:

```pwsh
dotnet run --project samples/Localization.Console/Pocok.Localization.Console.csproj
```

Expected output:

```text
greeting=Hallo Pocok nested=Home fallback=embedded missing=True valid=True
```

The process exits with code `0` only when culture fallback, nested JSON keys, provider composition, missing-key
behavior, and snapshot status all match the expected result.

## What the sample creates

The sample writes temporary resources equivalent to:

```text
Messages.json
Messages.de.resx
```

`Messages.json` contains an invariant greeting and the nested key `Navigation.Home`. Nested JSON properties are exposed
through dot-separated keys.

`Messages.de.resx` overrides `Greeting` for German cultures. The sample switches `CurrentCulture` and `CurrentUICulture`
to `de-DE`, so lookup follows:

```text
de-DE -> de -> invariant
```

The resulting greeting comes from the German RESX file, while `Navigation.Home` falls back to invariant JSON.

A `CompositeStringLocalizer` places the file-backed localizer before a small dictionary-backed provider. `FallbackOnly`
therefore comes from the second provider, while an unknown key is returned with `ResourceNotFound` set.

The temporary directory is deleted after execution and the original process cultures are restored.
