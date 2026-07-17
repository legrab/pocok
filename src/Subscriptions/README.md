# Pocok.Subscriptions

`Pocok.Subscriptions` is an experimental alpha package for keyed, in-process subscriptions. It extracts the reusable
part of the origin listener registry: multiple typed listeners can share a key, filter published objects, map them to a
target type, and receive synchronous delivery.

The default mapper delivers only values already compatible with the subscription type. Consumers that need conversion
must provide an explicit mapper.

The hub is thread-safe for subscription, publication, key snapshots, and disposal. Publication snapshots listeners
before invoking user handlers, so handlers may subscribe or unsubscribe without holding the hub lock. Handler exceptions
propagate to the publisher; the package does not silently isolate failures.

The package does not own transport connections, timers, retry policy, logging, dependency injection, serialization,
persistence, or network lifecycle. Retryable subscription orchestration from the origin remains deferred until a
cancellation- and time-provider-aware contract is justified.
