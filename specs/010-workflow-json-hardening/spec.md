# Feature Specification: Workflow JSON Type Hardening

**Feature Branch**: `codex/7541-workflow-json-hardening`
**Created**: 2026-05-29
**Status**: Draft
**Input**: User description: "GitHub issue #7541: Redo workflow JSON type hardening with dedicated alias design"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Load Existing Workflows Safely (Priority: P1)

Operators can upgrade Elsa without losing access to persisted workflows whose JSON contains existing type identifiers, including CLR type names emitted by earlier versions.

**Why this priority**: Backward compatibility is required before hardening can be reintroduced without breaking production workflows.

**Independent Test**: Load representative persisted workflow JSON that contains legacy type names and verify the workflow can be read while unknown or unsafe type identifiers are rejected.

**Acceptance Scenarios**:

1. **Given** a persisted workflow that references a known legacy type name, **When** the workflow is deserialized after upgrade, **Then** the workflow loads successfully.
2. **Given** a persisted workflow that references an unknown or untrusted type name, **When** the workflow is deserialized, **Then** the workflow is rejected with a clear failure instead of resolving arbitrary types.
3. **Given** a persisted workflow that contains abstract, interface, open generic, or collection type identifiers where a concrete workflow type is required, **When** the workflow is deserialized, **Then** the identifier is rejected.

---

### User Story 2 - Use Consistent Type Identifiers in APIs (Priority: P2)

API consumers receive consistent type identifiers for workflow-facing options such as incident handling strategies and can submit those identifiers back without relying on implementation-specific names.

**Why this priority**: The reported incident strategy dropdown failure came from inconsistent contracts between API descriptors and hardened workflow JSON reads.

**Independent Test**: Request descriptor data for incident strategies, select a returned option, and verify a workflow or workflow definition using that option can be accepted and read.

**Acceptance Scenarios**:

1. **Given** API descriptor data for incident handling strategies, **When** the options are returned, **Then** every option uses the documented workflow type identifier contract.
2. **Given** a client submits a supported incident strategy identifier from the descriptor payload, **When** the workflow payload is processed, **Then** the identifier resolves to the intended strategy.
3. **Given** older clients submit legacy CLR identifiers during the compatibility window, **When** the workflow payload is processed, **Then** supported legacy identifiers continue to resolve.

---

### User Story 3 - Register Workflow-Serializable Types Explicitly (Priority: P3)

Module and extension authors can explicitly register the types and legacy names that are valid in workflow serialization payloads without using expression configuration as the trust boundary.

**Why this priority**: Hardening must be extensible for built-in modules, custom workflow types, runtime payloads, and third-party extensions.

**Independent Test**: Register a custom workflow-facing type and legacy identifier, then verify alias-based payloads and supported legacy payloads resolve while unrelated types do not.

**Acceptance Scenarios**:

1. **Given** a module registers a workflow-serializable type with an alias, **When** workflow JSON references the alias, **Then** the type resolves successfully.
2. **Given** a module registers a supported legacy name for a workflow-serializable type, **When** compatibility JSON references the legacy name, **Then** the type resolves successfully.
3. **Given** a type is registered only for expression use, **When** workflow JSON references it, **Then** workflow JSON resolution does not accept it unless it is also registered for workflow serialization.

### Edge Cases

- Legacy JSON references a CLR type moved between assemblies or renamed after earlier persistence.
- Payloads reference unknown, untrusted, abstract, interface, open generic, array, dictionary, or collection types.
- JSON islands contain values that resemble type metadata but should remain normal JSON data.
- Runtime trigger and bookmark payloads include polymorphic values that need the same trust model as workflow definitions.
- Custom workflow types are registered by host applications or extensions after core services are configured.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST keep workflow JSON type resolution separate from expression type alias configuration.
- **FR-002**: The system MUST provide a dedicated registry or resolver for workflow-serializable type identifiers.
- **FR-003**: The system MUST allow built-in modules, extensions, and host applications to register workflow-serializable aliases.
- **FR-004**: The system MUST allow supported legacy type names to be registered for backward-compatible reads.
- **FR-005**: The system MUST document which legacy identifiers remain readable during the compatibility window and how unsupported identifiers fail.
- **FR-006**: The system MUST reject unknown, untrusted, abstract, open generic, interface, and inappropriate collection type identifiers when resolving workflow JSON types.
- **FR-007**: The system MUST continue loading existing workflows that reference supported CLR type names during the compatibility window.
- **FR-008**: The system MUST serialize new workflow-facing payloads with the documented type identifier contract where the payload belongs to that contract.
- **FR-009**: The system MUST keep public API payloads that expose workflow type identifiers internally consistent for request and response flows.
- **FR-010**: The system MUST cover the incident handling strategy option flow with a regression test.
- **FR-011**: The system MUST cover persisted workflow JSON, alias JSON, runtime trigger or bookmark payloads, JSON islands, and custom workflow types with tests.
- **FR-012**: The system MUST document the security model, trust boundaries, and migration behavior before hardened resolution is reintroduced.

### Key Entities *(include if feature involves data)*

- **Workflow Type Identifier**: A stable value used in workflow-facing JSON or API payloads to identify an allowed type.
- **Workflow-Serializable Type Registration**: A trusted registration that maps a workflow type identifier and optional legacy names to a concrete allowed type.
- **Compatibility Window**: The supported period or mode in which selected legacy CLR type names remain readable.
- **Public Type Identifier Contract**: The documented request and response behavior for API payloads that expose workflow type identifiers.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All representative legacy workflow JSON fixtures covered by tests load successfully when they use supported legacy identifiers.
- **SC-002**: Tests demonstrate rejection for unknown, untrusted, abstract, interface, open generic, and inappropriate collection type identifiers.
- **SC-003**: Incident strategy descriptor and submit flows pass a regression test that uses the same identifier contract in both directions.
- **SC-004**: New workflow JSON hardening tests run without depending on expression type alias configuration.
- **SC-005**: Documentation explains the trust model and compatibility behavior clearly enough for module authors to register workflow-serializable types without reading implementation code.

## Assumptions

- The compatibility window accepts known Elsa workflow-related CLR type names and explicitly registered host or extension legacy names, not arbitrary CLR resolution.
- Public API type identifier changes can be transitional: aliases are preferred for new payloads while supported legacy values remain readable.
- Existing workflow JSON fixtures are sufficient to represent persisted workflow compatibility risks, with new fixtures added where gaps are found.
- Cloud vault, external migration tooling, and data store schema changes are outside this feature unless existing tests prove they are required.
