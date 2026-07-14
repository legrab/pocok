# Publication Policy

## Repository visibility

Every standalone Pocok repository is public from its first commit so its CI and package audits run in the same environment users will see. Public repository visibility does not automatically approve a package release.

## Package state

All packages begin as internal. Publishing to NuGet is allowlist-only and requires an explicit review after package contents have been built and inspected.

## Package tiers

| Tier | Meaning | Compatibility |
|---|---|---|
| `Internal` | Personal convenience or unfinished infrastructure. | No external promise. |
| `Experimental` | Independently useful package whose API is still changing. | Breaking changes allowed and documented. |
| `PublicCandidate` | Documented, tested, packaged, and awaiting final release review. | Proposed semantic-versioning policy. |
| `Public` | Explicitly approved and released package. | Documented semantic-versioning policy. |

The internal `Foundation` facade remains internal permanently. Public packages never depend on it.

## Public admission gate

A package must:

- solve a general problem without company, customer, project, operational, or domain-specific contracts;
- contain no complete-application, domain-orchestration, application-UI, branding, activation, deployment, or runtime-content material;
- have a cohesive API and a smaller dependency cone than the problem it solves;
- document nullability, cancellation, culture, time, equality, serialization, concurrency, lifecycle, and security semantics where relevant;
- avoid process-global mutable initialization, hidden service locators, and implicit environment behavior;
- have focused unit tests, contract tests for adapters, and an external local-feed consumer smoke test;
- build deterministic packages with README, license, symbols, Source Link, and reviewed public API metadata;
- pass secret, company/product-name, task-reference, binary-content, dependency, vulnerability, and license scans;
- have one synthetic sample and a named maintainer.

Passing automation never grants publication by itself.

## Public allowlist

`Pocok.Primitives` is allowlisted for NuGet publication through the
`primitives-v*` release workflow after its tagged build passes the package,
public-content, and external-consumer checks.

| Package | Tier | Package review | Publication approved |
|---|---|---:|---:|
| `Pocok.Primitives` | `Public` | Yes | Yes |

The workflow uses MinVer to derive the package version from the release tag.
The NuGet trusted-publishing policy must target repository owner `legrab`,
repository `pocok`, and workflow file `publish-primitives.yml`.
