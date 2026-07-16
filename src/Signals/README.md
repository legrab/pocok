# Pocok.Signals

Compatibility tier: experimental alpha. This package is the neutral live-value slice extracted from the original application and is not release-eligible yet.

The package defines:

- ordinal source identity and signal addresses;
- explicit read, write, and subscription capabilities;
- point-in-time reads with capability checks and structured failures;
- quality-aware samples that distinguish uninitialized, usable, stale, bad, disconnected, and failed states;
- source timestamps, observation timestamps, and positive per-stream sequences;
- explicit write consistency evidence;
- structured expected operation failures.
- shared subscriptions with replay and bounded per-connection buffering;
- reconnect and disconnected/failed state publication;
- optional virtual-time staleness transitions;
- typed conversion at connection boundaries and typed write evidence.

The runtime resolves caller-owned sources through `SignalSourceFactory`, shares one raw subscription per address, and releases it when the last connection is disposed. Point-in-time reads use the same source entry and publish normalized evidence to active subscribers; a typed conversion failure is returned as a failed operation result while the stream still receives failed-quality evidence. It does not own protocol clients or source lifetimes. Source implementations own their external resources; every I/O method accepts a cancellation token and cancellation must propagate.

The package contains no protocol adapter, persistence, caching backend, UI binding, logging, serializer, service provider, or product-specific domain object. The runtime is thread-safe for concurrent connections and uses bounded per-connection queues; slow consumers may observe dropped samples through `DroppedSampleCount`. `SignalSample<T>` uses nullable annotations; consumers must inspect `HasValue` before interpreting `Value`, including for nullable reference values and value-type defaults.

Example:

    var address = new SignalAddress(new SourceId("line-a"), "temperature/outlet");
    var sample = new SignalSample<double>(
        21.5,
        true,
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow,
        SignalQuality.Good,
        1);
