# Research: Weaver Grounding Tools

## Decision: Start with deterministic Elsa tools, not embeddings

**Rationale**: Activity descriptors, workflow definitions, workflow instances, and incidents are structured Elsa data. Deterministic search/detail tools provide accurate, permission-aware grounding and are easier to test than vector retrieval. Embeddings may be added later for documentation or large historical logs, but they are not required for the MVP.

**Alternatives considered**:

- Prompt stuffing full catalogs and workflow graphs: rejected because it is expensive, leaky, and brittle.
- Vector database first: rejected because the first use cases require exact installed activity and workflow metadata.
- Direct Copilot database access: rejected because Elsa must enforce tenant/RBAC/redaction boundaries.

## Decision: Treat Activity Registry as the authoring foundation

**Rationale**: Workflow creation and update quality depends on installed activities, versions, inputs, outputs, trigger behavior, and constraints. `activities.search` and `activities.getDescriptor` are the minimum primitives Weaver needs to draft valid workflows.

**Alternatives considered**:

- Hard-code common activity knowledge in prompts: rejected because users install custom activities and versions.
- Expose raw `ActivityDescriptor` objects: rejected because model-facing DTOs should be stable, bounded, and redacted.

## Decision: Keep writes proposal-only

**Rationale**: AI-generated workflow mutations are high impact. Weaver should create or update proposals, run validation, and let users approve/apply through Elsa APIs. This preserves auditability and avoids hidden writes from an agent loop.

**Alternatives considered**:

- Let Copilot call workflow persistence directly: rejected because it bypasses review and baseline checks.
- Add direct action tools with confirmation prompts in MVP: rejected because Studio steering/approval UX and audit semantics should mature first.

## Decision: Split runtime inspection from operational actions

**Rationale**: Instance and incident inspection is read-only and immediately useful. Retrying, canceling, restarting, or bulk operations can be destructive and should wait for explicit action/proposal semantics.

**Alternatives considered**:

- Include operational action tools in MVP: rejected because they expand risk and require stronger confirmation UX.
- Omit runtime tools entirely: rejected because users explicitly need to ask questions about workflow instances and failures.

## Decision: Advertise grounding through provider-neutral capabilities

**Rationale**: Elsa Studio should enable context pickers and chat affordances based on Elsa-owned capabilities, not Copilot SDK features. Capability discovery also makes partial deployments understandable.

**Alternatives considered**:

- Hard-code Studio controls: rejected because modules and deployments vary.
- Expose Copilot SDK feature flags directly: rejected because Studio must stay provider-agnostic.
