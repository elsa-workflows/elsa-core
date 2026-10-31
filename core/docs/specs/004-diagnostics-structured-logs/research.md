# Research: Diagnostics Structured Logs

## Decision: Use a consistent breaking rename

**Rationale**: The module is unpublished feature work and the old `Elsa.ServerLogs` identity miscommunicates raw console capture. Renaming assemblies, namespaces, public contracts, features, routes, permissions, docs, and tests keeps Studio and host configuration aligned before release.

**Alternatives considered**:

- Keep compatibility shims: rejected because they would preserve confusing names before the first stable release.
- Rename only package/namespace: rejected because shell feature names, routes, permissions, and public types would still imply server console logs.

## Decision: Keep structured `ILogger` capture as the module boundary

**Rationale**: The existing implementation already captures semantic `ILogger` events with levels, categories, properties, exceptions, source metadata, buffering, and live streaming. This is distinct from future raw stdout/stderr capture.

**Alternatives considered**:

- Redirect `Console.Out`/`Console.Error`: rejected as a future diagnostics console logs module concern.
- Add OpenTelemetry explorer behavior now: rejected because this module should only expose trace/span correlation fields.

## Decision: Populate message templates from `{OriginalFormat}`

**Rationale**: Microsoft logging providers expose the original template through the structured state entry named `{OriginalFormat}`. Capturing it preserves semantic search and Studio rendering without changing how rendered messages are produced.

**Alternatives considered**:

- Treat `{OriginalFormat}` as a normal property: rejected because it duplicates data and pollutes structured properties.
- Infer templates from rendered messages: rejected because rendered messages lose placeholder names.

## Decision: Capture active scopes through `IExternalScopeProvider`

**Rationale**: `ILoggerProvider` can implement `ISupportExternalScope` and receive the shared external scope provider from logging infrastructure. This is the standard way to inspect nested active scopes without owning scope lifetimes.

**Alternatives considered**:

- Store scopes inside each logger's `BeginScope`: rejected because framework logging already coordinates external scopes and multiple providers.
- Skip scopes: rejected by the spec and weakens operational context.

## Decision: Preserve bounded in-memory provider and redaction order

**Rationale**: The current bounded recent buffer, per-subscriber backpressure accounting, redaction-before-publish flow, source registry, and recursion guard satisfy the safety requirements and should survive the rename.

**Alternatives considered**:

- Introduce durable storage: rejected as out of scope.
- Add a distributed provider now: rejected because external providers can implement the existing provider boundary later.
