# Feature Specification: Extensible Activity Output Converters

**Feature Branch**: `012-output-converters`

**Created**: 2026-07-30

**Status**: Draft

**Input**: Implement extensible activity output converters end to end across Elsa Core, the API client, and Elsa Studio, based on [elsa-core issue #7770](https://github.com/elsa-workflows/elsa-core/issues/7770).

## User Scenarios & Testing

### User Story 1 - Deliver a converted bound value (Priority: P1)

As a workflow author, I can explicitly select a registered Output Converter for an activity output binding so the destination receives the representation it needs while the activity's native output remains unchanged.

**Why this priority**: This is the feature's core value and can be used without design-time discovery when a workflow definition is authored through code or JSON.

**Independent Test**: Register one converter, bind an activity output to a compatible destination with that Converter ID, execute the workflow, and verify that the destination receives the converted value while activity-output observation surfaces retain the native value.

**Acceptance Scenarios**:

1. **Given** a compatible converter is explicitly configured on an output bound to a variable, **When** the activity produces a non-null native value, **Then** the variable receives the converted value and the activity output register retains the native value.
2. **Given** a compatible converter is explicitly configured on an output bound to a workflow output, **When** the activity completes, **Then** the workflow output receives the converted value and activity journals and diagnostics continue to report the native value.
3. **Given** no converter is configured, **When** an activity assigns an output, **Then** assignment follows the existing behavior without converter discovery, validation, or invocation.
4. **Given** a configured output receives null, **When** assignment occurs, **Then** conversion is bypassed and null is delivered only when the destination permits null.

---

### User Story 2 - Register a reusable converter safely (Priority: P1)

As an extension developer, I can register a deterministic Output Converter with a stable identity, compatible types, settings rules, and discoverable metadata so workflows can select it without persisting implementation details.

**Why this priority**: Workflow authors cannot use the feature until extension modules can define and register reliable converters.

**Independent Test**: Register a converter with descriptor metadata and settings validation, resolve it within a workflow execution scope, and verify identity, lifecycle, compatibility, validation, and deterministic invocation behavior.

**Acceptance Scenarios**:

1. **Given** a unique Converter ID and valid descriptor, **When** the extension is registered, **Then** the converter can be selected by that exact ID.
2. **Given** duplicate IDs or IDs differing only by case, **When** registration is built, **Then** registration fails deterministically rather than depending on registration order.
3. **Given** a converter with per-binding settings, **When** a workflow is validated and executed, **Then** the converter receives immutable JSON settings and no mutable workflow execution context or service locator.
4. **Given** a scoped converter dependency, **When** separate workflow execution scopes invoke the converter, **Then** each invocation resolves through its active workflow scope without a cached scoped instance.

---

### User Story 3 - Reject invalid converter configurations (Priority: P2)

As a workflow author or operator, I receive early, contextual validation when converter configuration is invalid and a privacy-safe activity fault if a deployment changes or conversion fails at runtime.

**Why this priority**: Persisted workflows can outlive deployments and registrations, so validation and diagnostics are required for operational safety.

**Independent Test**: Exercise invalid IDs, settings, type combinations, converter failures, and invalid results at definition acceptance and runtime, then verify rejection/fault stages, atomic destination behavior, and privacy-safe diagnostics.

**Acceptance Scenarios**:

1. **Given** a workflow references an unknown Converter ID, incompatible declared types, invalid settings, an unknown destination type, or no destination, **When** the definition is accepted or materialized, **Then** validation rejects the definition with actionable context.
2. **Given** a previously valid workflow executes in a deployment where its converter is missing or incompatible, **When** output assignment occurs, **Then** the activity faults through normal Elsa fault handling with a dedicated Output Conversion Error.
3. **Given** a converter throws or returns an invalid result, **When** assignment occurs, **Then** the destination remains unchanged, the native Activity Output remains available, and an originating exception is preserved.
4. **Given** an output or settings payload contains sensitive data, **When** a conversion error is reported, **Then** default error messages exclude native values and raw settings.

---

### User Story 4 - Discover and configure converters in Studio (Priority: P2)

As a Studio workflow author, I can discover converters compatible with the selected output and destination, configure their settings, save the workflow, and reopen it without losing configuration.

**Why this priority**: Server-owned discovery prevents Studio from duplicating extension registrations and makes the feature usable for visual workflow authors.

**Independent Test**: Open an output binding in Studio against a server with registered converters, select a destination, choose a compatible converter, edit valid settings, save and reopen the definition, and verify that the selection and settings round-trip.

**Acceptance Scenarios**:

1. **Given** an output and destination with known declared types, **When** the binding editor requests converter choices, **Then** it shows compatible server-provided descriptors using localized display metadata and falls back to the ID when metadata is unavailable.
2. **Given** a descriptor with a settings schema, **When** the author selects it, **Then** Studio provides schema-driven settings editing and surfaces validation feedback.
3. **Given** a descriptor without a settings schema, **When** the author selects it, **Then** Studio provides a raw JSON settings editor.
4. **Given** a saved binding with converter configuration, **When** the workflow is reopened or an unrelated activity property is edited, **Then** the Converter ID and settings remain intact.
5. **Given** the converter is cleared, **When** the workflow is saved, **Then** the optional converter object is removed and existing unconverted assignment behavior is restored.

### Edge Cases

- A converter configuration is present on an output with no variable or workflow-output destination.
- The destination type is `object`, a nullable value type, a non-nullable value type, a reference type with explicit nullability, unknown, or untyped.
- The native value is null, is derived from the declared source type, or violates the declared source type.
- A non-null input converts to null.
- A converter's supported source is a base class or interface of the declared output type.
- A converter's result declaration is not assignable to the destination type.
- A converter returns a runtime value inconsistent with its result declaration.
- A converter is removed, replaced incompatibly, or registered differently between validation and execution deployments.
- Two registrations use identical IDs or IDs that differ only by case.
- Settings are absent, empty, malformed, schema-invalid, or rejected by custom validation.
- An older workflow definition omits converter configuration.
- An older client edits a workflow containing converter configuration.
- A user attempts to configure multiple converters or an open-generic converter.
- Studio cannot load descriptors or localized display metadata.

## Requirements

### Functional Requirements

- **FR-001**: The system MUST support zero or one explicitly selected Output Converter on an Output Binding to a variable or workflow output.
- **FR-002**: The system MUST persist converter configuration as an optional object containing a stable Converter ID and optional JSON settings.
- **FR-003**: Workflow definitions without converter configuration MUST retain existing serialized shape and assignment behavior.
- **FR-004**: Converter configuration MUST NOT persist implementation type names, instances, descriptors, or display metadata.
- **FR-005**: The system MUST keep the Activity Output native and apply conversion only to the Bound Value delivered to the configured destination.
- **FR-006**: Activity-output registers, journals, API responses, and diagnostics MUST expose the native Activity Output.
- **FR-007**: Conversion MUST be synchronous at the Output Binding boundary.
- **FR-008**: Null native values MUST bypass converter invocation.
- **FR-009**: A converter-produced null MUST be accepted only when the destination explicitly permits null.
- **FR-010**: Conversion and result validation MUST complete before the destination is written.
- **FR-011**: A failed conversion MUST leave the destination unchanged and retain the native Activity Output for diagnostics.
- **FR-012**: Each converter MUST have a stable semantic Converter ID matched ordinally and case-sensitively.
- **FR-013**: Registration MUST reject duplicate IDs and IDs differing only by case.
- **FR-014**: Breaking changes to conversion behavior, accepted settings, or result semantics MUST use a new Converter ID.
- **FR-015**: The system MUST NOT infer converter selection from source and destination types.
- **FR-016**: A converter MUST declare its supported source type and result type.
- **FR-017**: Source compatibility MUST allow normal base-class and interface assignability from the Activity Output's declared type.
- **FR-018**: The declared converter result type and runtime result MUST be assignable to the resolvable Destination Type.
- **FR-019**: Unknown or untyped destinations MUST be invalid for converter configuration; `object` MUST remain a valid declared destination.
- **FR-020**: Open-generic converter matching and converter chains MUST remain outside the initial feature.
- **FR-021**: Converter invocation MUST receive only the native value, declared source and destination types, and immutable JSON settings.
- **FR-022**: Converter invocation MUST NOT expose mutable workflow execution state or a service locator.
- **FR-023**: Converter dependencies MUST be supplied through normal dependency registration and resolved from the active workflow execution scope.
- **FR-024**: Converter descriptors MAY be cached, but cached descriptors MUST NOT retain scoped converter instances or dependencies.
- **FR-025**: Converters MUST be documented and tested as deterministic and side-effect-free; environmental choices such as locale MUST be explicit settings.
- **FR-026**: The system MUST validate converter presence, declared-type compatibility, destination availability, and settings when a workflow definition is accepted or materialized.
- **FR-027**: The system MUST repeat converter safety validation during execution to detect deployment registration drift.
- **FR-028**: Converter settings MUST support optional JSON Schema validation and converter-owned custom validation.
- **FR-029**: Runtime resolution, settings, compatibility, invocation, and result failures MUST produce a dedicated Output Conversion Error through Elsa's normal activity fault pipeline without a converter-specific retry mechanism.
- **FR-030**: The Output Conversion Error MUST expose structured Converter ID, activity identity/type, output name, destination identity/type, and failure-stage metadata.
- **FR-031**: An originating converter exception MUST remain available as the Output Conversion Error's cause.
- **FR-032**: Default error messages MUST NOT expose native values or raw settings.
- **FR-033**: Core MUST provide a server-owned descriptor registry and query capability.
- **FR-034**: Elsa's API MUST expose descriptors filterable by declared source and Destination Type.
- **FR-035**: Descriptor API responses MUST expose the Converter ID, supported types, localizable display metadata, and optional settings schema without converter instances or implementation type names.
- **FR-036**: Studio MUST consume the descriptor API rather than maintain a hard-coded converter catalog.
- **FR-037**: Studio MUST filter converter choices by the selected Activity Output and destination types.
- **FR-038**: Studio MUST support selecting, configuring, clearing, saving, reopening, and validating Output Converter configuration.
- **FR-039**: Studio MUST offer schema-driven settings editing when a schema exists and raw JSON editing otherwise.
- **FR-040**: Core MUST NOT ship a broad production converter catalog; the extension mechanism MUST be demonstrated with a reference converter in tests or a sample.
- **FR-041**: The feature MUST NOT change activity-input conversion or general expression-result coercion.
- **FR-042**: Automated tests MUST cover serialization, compatibility, nullability, lifecycle, privacy, replay/retry behavior, API discovery, Studio round-tripping, version skew, and the unchanged no-converter path.

### Key Entities

- **Activity Output**: The native value and declared type produced by an activity.
- **Output Binding**: The association between an Activity Output and a variable or workflow-output destination, optionally carrying one converter configuration.
- **Converter Configuration**: The persisted Converter ID and optional JSON settings for one Output Binding.
- **Output Converter**: A registered deterministic transformation that produces a Bound Value from an Activity Output.
- **Converter Descriptor**: Server-owned discovery metadata describing identity, compatible types, display text, and optional settings schema.
- **Conversion Context**: The immutable invocation data supplied to an Output Converter.
- **Destination**: A variable or workflow output with a resolvable declared type and nullability.
- **Output Conversion Error**: A structured activity fault describing the stage and identities involved in a failed conversion.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All existing workflow serialization and output-assignment tests pass unchanged for bindings without converter configuration.
- **SC-002**: Unconfigured output bindings perform zero converter registry lookups or converter-related allocations, with no statistically significant throughput regression beyond 2% in a representative output-assignment benchmark.
- **SC-003**: Every static invalid configuration covered by FR-026 is rejected before execution in definition-validation tests.
- **SC-004**: Every runtime failure stage named in FR-029 produces a dedicated fault with the required structured metadata and no native value or raw settings in its default message.
- **SC-005**: A workflow author can select, configure, save, reopen, and clear a compatible converter in Studio in under two minutes without editing raw workflow JSON.
- **SC-006**: Converter configuration survives server serialization, API client round-tripping, Studio editing, and workflow reopening without loss or mutation.
- **SC-007**: Native Activity Outputs and converted Bound Values are independently observable and correct in all variable-binding and workflow-output integration scenarios.
- **SC-008**: Extension developers can register a converter and make it discoverable without changing Core or Studio source code.

## Assumptions

- Output conversion is opt-in and used for in-memory deterministic transformations.
- Converter IDs are treated as durable public semantic identifiers.
- Existing Elsa authorization applied to comparable descriptor endpoints also applies to converter discovery.
- Definition validation has access to declared activity-output and destination types.
- Studio and the Elsa API client can be released in coordination with the supporting Core API.
- JSON settings and schemas are small configuration documents rather than arbitrary payload storage.
- A raw JSON editor is an acceptable fallback when a descriptor has no schema or no specialized form control is available.

## Out of Scope

- Asynchronous converters.
- Converter chains.
- Automatic converter selection or fallback.
- Open-generic converter matching.
- Activity-input conversion.
- General expression-result coercion.
- A broad Core-owned catalog of production converters.
- Persisting converter implementation or dependency-injection details in workflows.
