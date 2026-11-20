# A host may register "*" as the tenant that authors tenant-agnostic entities

Date: 2026-11-02

## Status

Accepted; supersedes section 5 ("Reserved Character Constraint") of [0009](0009-asterisk-sentinel-value-for-tenant-agnostic-entities.md)

## Context

[0009](0009-asterisk-sentinel-value-for-tenant-agnostic-entities.md) marks tenant-agnostic entities with `TenantId = "*"`, which every tenant can see. Its section 4 requires that code sets `"*"` explicitly to create such an entity, and its section 5 reserves `"*"` so that it cannot be a tenant ID.

Elsa's own tenant-agnostic workflow definitions come from code: workflow providers such as CLR workflows set `"*"` on the definitions they import. A definition authored in Studio cannot be tenant-agnostic. The save endpoint creates the draft without a `TenantId` (`WorkflowDefinitionPublisher.NewAsync`), so the store stamps the current tenant, and the request's `TenantId` is not used.

Some multitenant hosts want operators to author shared workflow definitions in Studio: one definition that every tenant sees and runs. The only way to do that without changing Elsa is a session whose current tenant is `"*"`. Such a host lists a tenant with ID `"*"` in its `ITenantsProvider` and resolves the authoring session to it. Until 3.11, Elsa did not reject this: `ApplyTenantId` kept `"*"`, the query filters show the rows to every tenant, and nothing validates tenant IDs against section 5. Since 3.11, writing or deleting a `"*"` row requires shared-write authority: the code must run inside `SharedTenantWriteScope.Enter<TEntity>()` (`core/docs/migrations/tenant-owned-persistence.md`). The default tenant and a missing tenant context have none, and neither does a `"*"` tenant context. On such a host, every save in the authoring session and every start, resume and recovery of a `"*"` instance fails with "Writing a shared record requires trusted host authority."

Code that restarts tenant-agnostic work outside a request does not know about such a tenant. Interrupted-workflow recovery restarts a `"*"` instance under `TenantKey.From("*")`, which is the default tenant `""`. The resumed run stamps its new rows `""`, and the EF Core store refuses to replace the instance's existing `"*"` rows ("tenant ownership mismatch"), so the instance never recovers.

## Decision

- `"*"` keeps its meaning from 0009: on an entity, it means every tenant. Elsa never creates a tenant with ID `"*"`, and its own resolvers never resolve one. The header resolver ignores a `"*"` header, because the client chooses its value.
- A host may register a tenant with ID `"*"`. Entities saved in that tenant's context are tenant-agnostic. Tenant validation must not reject this ID.
- Code that runs in the context of the `"*"` tenant has shared-write authority for every entity type, as if it ran inside `SharedTenantWriteScope`. Registering the tenant and resolving a session or a background operation to it are the host's decisions, and they are the trusted host operation that shared writes require. Code that runs under a named tenant, the default tenant or no tenant still needs an explicit grant.
- When Elsa restarts tenant-agnostic work outside a request, starting with interrupted-workflow recovery, it uses the registered `"*"` tenant. Without one, it uses the default tenant, as before.
- Code that runs outside a request and writes tenant-owned rows picks its ambient tenant this way: an instance of a named tenant runs under that tenant (and is skipped with a warning if the tenant is not found); a `"*"` instance runs under the registered `"*"` tenant, or the default tenant if there is none; an instance with an empty or null tenant ID runs under the default tenant. `TenantKey.From` maps `"*"` to `""` and is only for identity and de-duplication, never for choosing the ambient tenant.
- A registered `"*"` tenant belongs to the shared store. A host must not give it a database of its own (a connection string in its configuration).

## Consequences

### Positive

- Hosts can let operators author tenant-agnostic workflow definitions with the existing Studio and API, without a new "save as shared" option.
- Hosts that do not register `"*"` see no change.

### Negative

- Code that maps `"*"` to the default tenant to pick the tenant it runs under has to check for a registered `"*"` tenant first. This record covers interrupted-workflow recovery. Trigger indexing and scheduling of tenant-agnostic workflows are not decided here.
- The ID that marks shared data can also be a tenant, so "the current tenant is `"*"`" and "the entity is shared" have to be told apart by context.
- Every session that the host resolves to `"*"` can write every shared record. A host must resolve `"*"` only for trusted operators, and a custom resolver must not take the tenant ID from a value the client chooses.
