# Feature Specification: Persistence vNext

**Feature Branch**: `codex/persistence-vnext-poc`
**Created**: 2026-06-02
**Status**: Draft
**Input**: User description: "Execute the Persistence vNext roadmap end to end: provider-neutral modular persistence for Elsa, with relational and document database support, runtime-defined entities, portable document/index storage by default, provider-optimized physicalization by policy, PR review/merge flow, and issue completion tracking."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Module-Owned Storage Intent (Priority: P1)

As an Elsa module author, I want to declare the storage shape and query indexes my module needs once, without referencing SQL Server, SQLite, EF Core, MongoDB, or any provider-specific migration package.

**Why this priority**: This removes the current per-module/per-provider migration maintenance burden and establishes the foundation for every later slice.

**Independent Test**: A module can register a versioned storage manifest, and separate relational and document planners can produce provider-family plans from the same manifest.

**Acceptance Scenarios**:

1. **Given** a module manifest with a storage unit, fields, key, and indexes, **When** a relational planner processes it, **Then** it produces a table/index plan without requiring provider-specific module code.
2. **Given** the same module manifest, **When** a document planner processes it, **Then** it produces a collection/index plan without requiring relational descriptors.

---

### User Story 2 - Provider-Owned Materialization (Priority: P1)

As an Elsa operator, I want the selected persistence provider to materialize schema/indexes from module manifests so modules do not ship provider-specific migrations.

**Why this priority**: This is the central replacement for EF Core migrations as schema authority.

**Independent Test**: SQLite can execute generated schema/index operations and record the applied schema version; SQL Server can render provider-specific DDL from the same manifest.

**Acceptance Scenarios**:

1. **Given** a manifest version that has not been applied, **When** SQLite startup materialization runs, **Then** required tables/indexes are created and the version is recorded.
2. **Given** the same manifest version already recorded, **When** SQLite startup materialization runs again, **Then** no schema operations are re-applied.

---

### User Story 3 - Portable Document Store (Priority: P1)

As an Elsa module author, I want to persist and query module documents using declared indexes, so simple modules do not need custom EF Core, SQL, or MongoDB code.

**Why this priority**: A YesSQL-like document/index model is the practical way to support both relational and native document databases.

**Independent Test**: A document can be saved, loaded, deleted, and queried by declared indexed fields on SQLite.

**Acceptance Scenarios**:

1. **Given** a document type with indexed fields, **When** the document is saved, **Then** provider-owned index records are maintained.
2. **Given** a query against a declared index, **When** the query executes, **Then** matching documents are returned with paging and sorting where supported.
3. **Given** a query against an undeclared index, **When** the query is validated, **Then** it fails with a clear provider-neutral error.

---

### User Story 4 - Native Document Provider Support (Priority: P2)

As an Elsa deployer, I want to run the same module persistence model on MongoDB so Elsa remains database agnostic.

**Why this priority**: The abstraction is not successful unless document databases can use native collections/indexes rather than relational tables.

**Independent Test**: The same document/index contract passes against MongoDB with native index creation.

**Acceptance Scenarios**:

1. **Given** a storage manifest, **When** MongoDB materialization runs, **Then** collections and native indexes are created.
2. **Given** indexed document queries, **When** they execute against MongoDB, **Then** they use the provider's native query/index model.

---

### User Story 5 - Runtime-Defined Entities (Priority: P2)

As an Elsa administrator, I want to define entities at runtime and query them by declared indexed fields without creating a physical table per entity by default.

**Why this priority**: Runtime-defined entities unlock admin-defined domain concepts while preserving provider portability.

**Independent Test**: An admin-defined entity definition can be published, materialized, used for CRUD, and queried by declared indexes.

**Acceptance Scenarios**:

1. **Given** a draft entity definition, **When** it is validated and published, **Then** the selected provider materializes required indexes.
2. **Given** an instance of that runtime entity, **When** it is saved, **Then** it is stored as a document and queryable by declared indexes.

---

### User Story 6 - Provider-Optimized Physicalization (Priority: P3)

As an Elsa operator, I want to opt hot entities or indexes into provider-optimized physical shapes without changing module code.

**Why this priority**: Portable defaults must not block high-performance production workloads.

**Independent Test**: A storage policy can be changed from portable document storage to optimized indexes or native physicalization, and the provider reports what physical changes will be applied.

**Acceptance Scenarios**:

1. **Given** a hot index, **When** an optimized index policy is selected, **Then** the provider materializes a stronger provider-specific index.
2. **Given** unsupported physicalization, **When** validation runs, **Then** the provider reports the unsupported capability before applying changes.

### Edge Cases

- Multiple app instances start concurrently and attempt materialization.
- A schema version is already applied.
- A provider does not support a requested index or field type.
- A runtime entity definition is changed while existing documents exist.
- A query references a field that is not indexed.
- A physicalization policy is unsupported by the selected provider.
- A document save succeeds but index maintenance fails.
- MongoDB transaction support is unavailable in a deployment topology.
- Existing EF Core-backed data must be imported into vNext storage.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support versioned storage manifests contributed by modules.
- **FR-002**: System MUST support provider-neutral storage units, fields, keys, and indexes.
- **FR-003**: System MUST expose planner contracts so relational and document providers can consume the same manifest.
- **FR-004**: System MUST expose provider capability validation before materialization.
- **FR-005**: System MUST support provider-owned schema/index materialization.
- **FR-006**: System MUST record applied schema versions per manifest.
- **FR-007**: System MUST support idempotent startup materialization.
- **FR-008**: System MUST support a portable document store for runtime-defined and module-defined documents.
- **FR-009**: System MUST support portable queries only through declared indexes.
- **FR-010**: System MUST return clear errors for unsupported or unindexed portable queries.
- **FR-011**: System MUST support relational providers through document tables and index storage.
- **FR-012**: System MUST support MongoDB through native collections and native indexes.
- **FR-013**: System MUST support runtime-defined entity definitions with draft, published, and retired states.
- **FR-014**: System MUST support provider-optimized physicalization policies as an opt-in.
- **FR-015**: System MUST allow existing Elsa modules to migrate gradually from EF Core persistence.
- **FR-016**: System MUST keep EF Core migrations out of the vNext schema authority path.
- **FR-017**: System MUST expose diagnostics for manifest versions, provider status, and materialization failures.
- **FR-018**: System MUST provide validation and tests for each supported provider.

### Key Entities *(include if feature involves data)*

- **Persistence Manifest**: Versioned module or runtime schema declaration.
- **Storage Unit**: Provider-neutral persisted document/entity shape.
- **Field**: Provider-neutral stored value declaration.
- **Index**: Declared query path and uniqueness constraint.
- **Provider Capability**: Provider-reported support for storage and query features.
- **Document Envelope**: Stored document metadata and payload.
- **Runtime Entity Definition**: Admin-defined entity manifest with lifecycle metadata.
- **Schema History Entry**: Record of an applied manifest version.
- **Physicalization Policy**: Storage optimization mode selected per storage unit or index.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A module can declare persistence once and generate relational and document plans from the same manifest.
- **SC-002**: SQLite materialization applies a manifest once and skips re-application on subsequent runs.
- **SC-003**: A portable document store can save, load, delete, and query documents by declared indexes on SQLite.
- **SC-004**: SQL Server and PostgreSQL providers pass the same portable document/index contract tests as SQLite.
- **SC-005**: MongoDB provider passes the same document/index contract tests using native collections and indexes.
- **SC-006**: Runtime-defined entities can be published and used without creating one physical table per entity by default.
- **SC-007**: Unsupported provider capabilities are detected before materialization.
- **SC-008**: At least one existing Elsa module runs on vNext persistence without EF Core migrations.
- **SC-009**: Provider-specific migration projects are not required for the migrated vNext module.
- **SC-010**: Hot-path physicalization can be enabled for at least one provider without changing module manifest code.

## Assumptions

- Runtime-defined entities use document storage by default.
- Portable queries are index-driven and intentionally limited.
- EF Core can remain a runtime query implementation where useful, but EF Core migrations are not the schema authority.
- Provider-specific escape hatches are allowed when declared explicitly.
- Workflow runtime hot paths are evaluated later and are not the first migration target.
- Secrets is the preferred first real Elsa module migration candidate.
- The reusable core should be extractable outside Elsa after validation against multiple providers.
