# Research: Extensible Activity Output Converters

## Runtime seam and assignment ordering

**Decision**: Orchestrate configured conversion in `ActivityExecutionContext.Set`. Preserve the existing two-call branch for unconfigured outputs. For configured outputs, record the native value first, bypass invocation for null, convert and validate into a local value, then write the destination atomically.

**Rationale**: This is the only centralized boundary that has both the output binding and native activity result. Recording first preserves diagnostics when conversion fails.

**Alternatives considered**: Converting in the lower expression setter would affect non-activity assignment and cannot preserve the native result independently.

## Bound-value write path

**Decision**: Add a binder-specific destination write that writes an already validated Bound Value directly to the output memory reference and workflow-output dictionary.

**Rationale**: The existing lower setter calls `Output.ParseValue`, which converts through the native `Output<T>` type and would undo a type-changing conversion.

**Alternatives considered**: Changing `Output.ParseValue` globally would alter existing output/input coercion behavior.

## Registration and scoped resolution

**Decision**: Register converter implementations as keyed `IOutputConverter` services using their Converter ID. Store immutable registrations/descriptors separately in a singleton registry, reject ordinal duplicates and case-only variants during registration, and resolve the keyed converter from the active execution/validation scope for each invocation.

**Rationale**: Keyed DI supports multiple converter implementations and arbitrary lifetimes without caching scoped instances or exposing implementation types through persisted/API models.

**Alternatives considered**: A singleton dictionary of converter instances violates scoped lifetimes. Resolving an unkeyed implementation type is ambiguous when one implementation serves multiple IDs.

## Invocation contract

**Decision**: Use a non-generic synchronous `IOutputConverter` receiving an immutable `OutputConversionContext` containing the non-null native value, declared source and destination types, and cloned settings represented as `JsonElement`.

**Rationale**: Descriptor types provide compatibility metadata while the non-generic interface supports runtime discovery. `JsonElement` prevents mutation of persisted settings. Dependencies use constructor injection.

**Alternatives considered**: Passing `ActivityExecutionContext` or `IServiceProvider` creates hidden workflow mutations. Generic-only converter interfaces complicate heterogeneous registry invocation.

## Destination resolution and nulls

**Decision**: Centralize destination resolution for runtime memory metadata and definition graphs. At runtime, a destination permits null when its CLR type is a reference type or `Nullable<T>`; non-nullable value types reject null.

**Rationale**: Elsa currently has no nullable-reference metadata on variables or workflow outputs. CLR representability is deterministic and avoids expanding this feature into a repository-wide nullability model.

**Alternatives considered**: Adding nullability metadata to all variable/output definition and client models would be a separate breaking feature.

## Definition validation lifecycle

**Decision**: Add a dedicated `WorkflowDefinitionValidating` handler that visits the materialized workflow graph and validates configured bindings during publication/import acceptance. Repeat all safety checks during runtime assignment.

**Rationale**: Elsa's existing validator is notification-extensible and publication already invokes it. Runtime validation covers registration drift and programmatically executed definitions.

**Alternatives considered**: Validating every draft save changes current draft semantics. Validating only at runtime gives poor author feedback.

## Settings validation

**Decision**: Use centrally versioned JsonSchema.Net for synchronous standards-compliant schema evaluation, then invoke optional converter-owned custom settings validation. Parse/cache descriptor schemas, never settings-bound converter instances.

**Rationale**: Core has no existing JSON Schema validator and an ad hoc subset would not satisfy the advertised contract. JsonSchema.Net supports the repository's .NET targets and System.Text.Json model.

**Alternatives considered**: NJsonSchema centers Newtonsoft.Json and asynchronous schema parsing. Custom-only validation would make descriptors misleading to Studio.

## Structured conversion faults

**Decision**: Add `OutputConversionException`, a failure-stage enum, and a narrow safe-exception-metadata interface. Copy only that safe metadata into persisted `ExceptionState` and its API-client mirror; preserve converter exceptions as inner exceptions.

**Rationale**: Custom exception properties are otherwise lost after persistence. Serializing arbitrary `Exception.Data` could leak native values or settings.

**Alternatives considered**: Message-only diagnostics are not machine-readable. Persisting the full exception data bag violates privacy.

## Descriptor API

**Decision**: Expose `GET /descriptors/output-converters` with required source and destination type-name filters and `read:output-converters` permission. Map server descriptors to safe string type names, display metadata, and optional settings schema.

**Rationale**: This matches existing descriptor endpoints and prevents implementation types or instances from crossing the API boundary.

**Alternatives considered**: Returning every converter forces client-side type semantics. Reusing activity-descriptor permission couples unrelated capabilities.

## Studio authoring

**Decision**: Add an output-converter service using the generated API client, enrich binding targets with destination type metadata, and extend the Outputs tab with compatible converter selection. Provide a scoped schema editor for object properties and raw JSON fallback for absent/unsupported schemas.

**Rationale**: The current Outputs tab is the single binding seam. Studio has no reusable generic JSON Schema renderer, so a narrow settings component meets the feature without claiming a platform-wide renderer.

**Alternatives considered**: A hard-coded catalog violates server ownership. Raw JSON only does not satisfy schema-driven authoring.

## Version skew

**Decision**: Omit converter JSON when absent, preserve unknown/current converter configuration during unrelated Studio edits, and hide or disable discovery controls when connected to an older server without erasing persisted data.

**Rationale**: Core/API/Studio may be upgraded at different times, and old workflow definitions must remain valid.

**Alternatives considered**: Clearing unknown configuration silently changes workflow semantics.
