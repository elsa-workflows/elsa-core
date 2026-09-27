# Decide connection metadata inspection with its own host policy

- Status: Proposed
- Date: 2026-09-27
- Related: #8194, #8221, #8345, #8406; supersedes the authorization seam of [Authorize connection metadata inspection separately](2026-09-25-connection-metadata-inspection-boundary.md)

## Context

The first inspection slice asked the host's `IConnectionUseAuthorizer` for a new purpose, `inspect:metadata`. That authorizer already answers `use` and every `manage:*` purpose, so a host cannot register a use or management policy without also answering inspection. Hosts written before inspection existed had no reason to handle the new purpose.

The in-repo hosts show what that does. The opt-in worker host (`WorkerConnectionUseAuthorizer`) checks `connections.use` for `use` and `connections.manage` for every other purpose, so a manager would pass inspection. The persisted export fixture and the workflow binding fixture allow every purpose in scope. In all three, inspection is granted by a policy that never mentions it. #8406 requires the opposite: inspection defaults to deny when its host policy is absent, and a management permission, binding or workflow-use grant never satisfies it.

## Decision

Metadata inspection has a dedicated host contract, `IConnectionMetadataInspectionAuthorizer`. It receives the authenticated principal and a `ConnectionMetadataInspectionRequest` carrying the ambient tenant, the host-configured `ConnectionInspectionOptions.EnvironmentId` and the connection ID. `ConnectionsFeature` registers a deny-all default with `TryAdd`, so inspection stays denied until the host registers its own policy. `DefaultConnectionMetadataInspector` no longer calls `IConnectionUseAuthorizer`, and Core no longer sends the `inspect:metadata` purpose.

The DTO, fail-closed checks and scoping from the earlier decision are unchanged. The inspector returns nothing for an unauthenticated principal, a default or agnostic tenant, a blank environment, missing lifecycle persistence or a denied decision, and it reads the store only after the decision allows the request. `ConnectionInspectionMetadata` stays limited to connection ID, provider ID, provider account ID, status and revision. A disconnected connection is still inspectable, and its metadata reports `Disconnected`.

This matches the sharing decision. `IConnectionCredentialShareAuthorizer` is already its own deny-by-default contract, separate from grant management, binding management, binding use and credential use.

## Consequences

A host enables inspection by registering `IConnectionMetadataInspectionAuthorizer` and setting `ConnectionInspectionOptions.EnvironmentId`. A host that answered `inspect:metadata` in its use authorizer is denied inspection until it moves that rule to the new contract. That failure is visible, and it is safer than letting a use or management rule keep answering. `Elsa.Connections` is not packable, so no published package identity changes. The `DefaultConnectionMetadataInspector` constructor now takes the new authorizer instead of `IConnectionUseAuthorizer`. That breaks only direct in-repo construction. The serialized DTO is unchanged.

A regression test registers a use authorizer that allows every purpose and no inspection policy, then asserts that inspection is denied, the store is not read and the use authorizer is never called. Registering an inspection policy is what allows it. Before this change the denial case failed. The wiring, test inventory and remaining legacy and provider work are in [Connection inspection and sharing boundary](../integration-program/secrets/connection-inspection-and-sharing-boundary.md).
