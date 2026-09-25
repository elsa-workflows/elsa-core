# Authorize connection metadata inspection separately

- Status: Proposed
- Date: 2026-09-25
- Related: #8194, #8221, #8345, #8406

## Context

The Connections lifecycle service returns a small nonsecret result to the caller of an authorized mutation. It has no stand-alone metadata read operation. Management and credential use already make separate host authorization calls, and the durable workflow-use grant authorizes one exact workflow instance and binding revision. A granted workflow must not gain a general metadata read permission from either path.

The published Extensions `ConnectionDefinition` has a JSON `ConnectionConfiguration` field. Its `ConnectionModel` carries that field and the legacy GET/list endpoints return `entity.ToModel()` without filtering the JSON. We have not measured installed consumers or actual customer values. The legacy model therefore cannot serve as the safe metadata DTO for the new lifecycle store. Its route and payload compatibility require a separate import decision.

## Decision

Add a host-only, single-connection metadata inspection service to Core Connections. The caller supplies an authenticated principal and connection ID. The service takes tenant from `ITenantAccessor` and environment from host configuration; a blank/default/agnostic tenant or blank environment fails closed. Before reading the scoped lifecycle store, it asks the existing `IConnectionUseAuthorizer` for a distinct human action, `inspect:metadata`. The default authorizer denies it. Hosts must validate the principal against the exact tenant, environment, connection and purpose; granting management or workflow use alone is insufficient.

The returned record whitelists connection ID, provider ID, provider account ID, status and revision. It excludes secret names, generation IDs, credential expiry/kind, operation state and token material. A denied or absent connection returns no record. This is an in-process contract; it adds no public HTTP route and no implicit sharing rule. The existing durable grant issue/withdraw path remains the only implemented sharing mechanism, scoped to one workflow instance and binding revision. Broader person/team sharing and a public UI/API need a separate reviewed contract if the program chooses them.

## Consequences and verification

Hosts must configure the inspection environment and an explicit `inspect:metadata` policy to enable reads. The lifecycle store is optional at the Connections feature boundary; without it, the inspector resolves but returns no record. No configuration or an unrelated management/use permission leaves inspection denied. Tests cover absent persistence, allowed inspection, management-only denial, authentication, tenant/environment isolation and serialization of the safe DTO. The service does not certify the legacy Extensions route or a production identity provider; those remain import and host-policy gates under #8275 and #8406.
