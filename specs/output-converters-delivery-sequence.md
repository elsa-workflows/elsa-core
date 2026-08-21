# Output Converters Recommended Delivery Sequence

**Status**: Feasibility assessment

**Issue**: [elsa-core #7770](https://github.com/elsa-workflows/elsa-core/issues/7770)

**Related decisions**:
[binding boundary](../doc/adr/0011-output-conversion-at-binding-is-synchronous.md),
[converter identity](../doc/adr/0012-output-converters-use-explicit-stable-identities.md), and
[server-owned discovery](../doc/adr/0013-output-converter-discovery-is-server-owned.md)

The feature should be delivered in independently verifiable phases. Each phase preserves existing output-binding behavior when no converter is configured.

## Phase 1: Contracts and persistence

- Define converter configuration, descriptor, invocation context, registry, and error contracts.
- Extend the Output Binding and API client models with the optional `converter` object.
- Round-trip the Converter ID and JSON settings through workflow serialization.
- Reject duplicate and case-only-variant Converter IDs during registration.

**Exit criteria**: Existing workflow JSON remains compatible, configured converters round-trip without CLR implementation details, and the no-converter path remains behaviorally unchanged.

## Phase 2: Runtime conversion

- Invoke the explicitly selected converter synchronously at the Output Binding boundary.
- Preserve the native Activity Output in registers, journals, APIs, and diagnostics.
- Resolve converter instances from the active workflow execution scope.
- Enforce null, source compatibility, destination compatibility, result validation, and atomic-write rules.
- Route privacy-safe Output Conversion Errors through normal activity fault handling.

**Exit criteria**: Unit and integration tests demonstrate successful conversion, native-output preservation, atomic failures, scoped resolution, retry behavior, and the unchanged default path.

## Phase 3: Definition and settings validation

- Validate Converter ID presence, compatible declared types, and a resolvable destination type when definitions are accepted or materialized.
- Support optional JSON Schema validation and converter-owned custom validation for settings.
- Repeat safety validation during execution to handle registration differences between deployments.

**Exit criteria**: Invalid static configurations are rejected before execution, while runtime drift produces the dedicated contextual fault.

## Phase 4: Descriptor discovery API

- Provide a server-owned descriptor registry and query service.
- Expose safe descriptors through Elsa's API, filterable by declared source and Destination Type.
- Include localizable display metadata and optional settings schema.
- Add matching API client contracts without exposing converter instances or CLR implementation types.

**Exit criteria**: Clients can discover and filter registered converters solely through supported API contracts.

## Phase 5: Studio authoring

- Consume the descriptor API rather than maintaining a client-side catalog.
- Show only converters compatible with the selected activity output and destination.
- Persist converter selection and settings without disturbing unrelated activity JSON.
- Provide schema-driven settings editing with a raw JSON fallback when no schema is available.
- Surface definition-validation feedback in the output-binding editor.

**Exit criteria**: A workflow author can select, configure, clear, save, reopen, and validate an Output Converter entirely through Studio.

## Phase 6: End-to-end hardening

- Add serialization, compatibility, nullability, privacy, replay, retry, multi-target, and version-skew coverage.
- Verify no converter-related work occurs on unconfigured bindings.
- Document the public extension API and provide a reference converter in tests or a sample.
- Run targeted tests followed by the affected solution-level build and test suites.

**Exit criteria**: Core, API client, and Studio scenarios pass together, public contracts are documented, and default-path behavior and performance remain unchanged.

## Dependencies and parallel work

- Phase 1 blocks the runtime, validation, and API implementations.
- Phases 2 and 4 can proceed in parallel after their shared contracts stabilize.
- Studio can prototype against fixed descriptor contracts, but Phase 4 must complete before end-to-end verification.
- Phase 3 should complete before Studio validation UX is finalized.
- Phase 6 is the release gate.

## Effort envelope

| Delivery target | Estimated effort | Complexity |
| --- | ---: | --- |
| Runtime MVP through Phase 2 | 7–10 engineer-days | Medium-high |
| Complete backend through Phase 4 | 12–18 engineer-days | High |
| End-to-end delivery through Phase 6 | 18–28 engineer-days | High |

The largest uncertainty is schema-driven Studio settings editing because the current Studio codebase does not appear to provide a reusable JSON Schema form editor.
