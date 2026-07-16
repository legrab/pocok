# Pocok.Localization

Compatibility tier: experimental alpha. This package is a neutral composition boundary for standard .NET string localizers and is not release-eligible yet.

`CompositeStringLocalizer` accepts an ordered set of `IStringLocalizer` instances. The first provider that contains a requested resource wins. Missing keys fall back to the key and remain marked with `ResourceNotFound`; enumeration preserves provider order and suppresses duplicate resource names using ordinal comparison.

`ResourceCulture` resolves a valid two- or three-letter language tag, or a language tag with a region/script suffix, from the final resource-file name segment. Callers provide the fallback culture explicitly; the package never mutates process or thread culture.

The package owns no database, filesystem, resource assembly, caching, dependency-injection, logging, or application-specific localization policy. Providers own their resource loading and lifetime. The composite is immutable after construction and safe for concurrent reads when its providers are safe for concurrent reads.
