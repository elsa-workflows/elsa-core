# A host may register "*" as the tenant that authors tenant-agnostic entities

Date: 2026-11-02

## Status

Proposed; supersedes section 5 ("Reserved Character Constraint") of [0009](0009-asterisk-sentinel-value-for-tenant-agnostic-entities.md)

## Context

[0009](0009-asterisk-sentinel-value-for-tenant-agnostic-entities.md) marks tenant-agnostic entities with `TenantId = "*"`, which every tenant can see. Its section 4 requires that code sets `"*"` explicitly to create such an entity, and its section 5 reserves `"*"` so that it cannot be a tenant ID.

Elsa's own tenant-agnostic workflow definitions come from code: workflow providers such as CLR workflows set `"*"` on the definitions they import. A definition authored in Studio cannot be tenant-agnostic. The save endpoint creates the draft without a `TenantId` (`WorkflowDefinitionPublisher.NewAsync`), so the store stamps the current tenant, and the request's `TenantId` is not used.

Some multitenant hosts want operators to author shared workflow definitions in Studio: one definition that every tenant sees and runs. The only way to do that without changing Elsa is a session whose current tenant is `"*"`. Such a host lists a tenant with ID `"*"` in its `ITenantsProvider` and resolves the authoring session to it. Elsa does not reject this: `ApplyTenantId` keeps `"*"`, the query filters show the rows to every tenant, and nothing validates tenant IDs against section 5.

Code that restarts tenant-agnostic work outside a request does not know about such a tenant. Interrupted-workflow recovery restarts a `"*"` instance under `TenantKey.From("*")`, which is the default tenant `""`. The resumed run stamps its new rows `""`, and the EF Core store refuses to replace the instance's existing `"*"` rows ("tenant ownership mismatch"), so the instance never recovers.

## Decision

- `"*"` keeps its meaning from 0009: on an entity, it means every tenant. Elsa never creates a tenant with ID `"*"`, and its own resolvers never resolve one.
- A host may register a tenant with ID `"*"`. Entities saved in that tenant's context are tenant-agnostic. Tenant validation must not reject this ID.
- When Elsa restarts tenant-agnostic work outside a request, starting with interrupted-workflow recovery, it uses the registered `"*"` tenant. Without one, it uses the default tenant, as before.

## Consequences

### Positive

- Hosts can let operators author tenant-agnostic workflow definitions with the existing Studio and API, without a new "save as shared" option.
- Hosts that do not register `"*"` see no change.

### Negative

- Code that maps `"*"` to the default tenant to pick the tenant it runs under has to check for a registered `"*"` tenant first. This record covers interrupted-workflow recovery. Trigger indexing and scheduling of tenant-agnostic workflows are not decided here.
- The ID that marks shared data can also be a tenant, so "the current tenant is `"*"`" and "the entity is shared" have to be told apart by context.
