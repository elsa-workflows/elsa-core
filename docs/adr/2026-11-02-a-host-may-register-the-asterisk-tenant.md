# A host may register "*" as the tenant that authors tenant-agnostic entities

Date: 2026-11-02

## Status

Accepted; supersedes section 5 ("Reserved Character Constraint") of [0009](0009-asterisk-sentinel-value-for-tenant-agnostic-entities.md)

## Context

[0009](0009-asterisk-sentinel-value-for-tenant-agnostic-entities.md) marks tenant-agnostic entities with `TenantId = "*"`, which every tenant can see. Its section 4 requires that code sets `"*"` explicitly to create such an entity, and its section 5 reserves `"*"` so that it cannot be a tenant ID.

Elsa's own tenant-agnostic workflow definitions come from code: workflow providers such as CLR workflows set `"*"` on the definitions they import. A definition authored in Studio cannot be tenant-agnostic. The save endpoint creates the draft without a `TenantId` (`WorkflowDefinitionPublisher.NewAsync`), so the store stamps the current tenant, and the request's `TenantId` is not used.

Some multitenant hosts want operators to author shared workflow definitions in Studio: one definition that every tenant sees and runs. The only way to do that without changing Elsa is a session whose current tenant is `"*"`. Such a host lists a tenant with ID `"*"` in its `ITenantsProvider` and resolves the authoring session to it. Section 5 of 0009 reserves `"*"`, but nothing validates tenant IDs against it.

Before 3.11, writes in that context were stamped `"*"` without further checks, including the workflow instances and runtime records of runs started there. Interrupted-workflow recovery restarted such instances under `TenantKey.From("*")`, the default tenant, and the store refused to replace their `"*"` rows, so they never recovered. Since 3.11 (#9283), writing or deleting a `"*"` row requires an entity-specific `SharedTenantWriteScope` (`core/docs/migrations/tenant-owned-persistence.md`).

## Decision

- `"*"` keeps its meaning from 0009: on an entity, it means every tenant. Elsa never creates a tenant with ID `"*"`.
- A host may register a tenant with ID `"*"`. Validation of the host's tenant list must not reject this ID. The tenant management API does not create one: it rejects `"*"` as a new tenant ID, and it does not change the ID of an existing tenant.
- A `"*"` tenant context grants no shared-write authority. Writing or deleting `"*"` rows requires an entity-specific `SharedTenantWriteScope`, entered by trusted host code at the write boundary after it has authorized the operation.
- Workflow instances are never tenant-agnostic. Runs of `"*"` definitions belong to the invoking tenant. Elsa does not persist `"*"` instances or their runtime records.
- `TenantKey.From` maps `"*"` to `""` and is only for identity and de-duplication, never for choosing the ambient tenant.
- A registered `"*"` tenant belongs to the shared store. A host must not give it a database of its own (a connection string in its configuration).

## Consequences

### Positive

- Hosts can let operators author tenant-agnostic workflow definitions with the existing Studio and API, without a new "save as shared" option.
- Hosts that do not register `"*"` see no change.

### Negative

- A host that authors in a `"*"` session must enter `SharedTenantWriteScope` for the entity types it writes there, such as workflow definitions and the triggers indexed when they are published.
- `"*"` instances persisted before 3.11 are not recovered automatically. Operators cancel them or re-stamp them to a tenant (see the migration note).
- The ID that marks shared data can also be a tenant, so "the current tenant is `"*"`" and "the entity is shared" have to be told apart by context.
