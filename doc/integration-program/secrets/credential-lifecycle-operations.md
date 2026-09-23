# Credential lifecycle architecture and operations

This guide records the boundaries and operational limits of the initial Core credential-lifecycle implementation. It covers synthetic provider integration and SQLite conformance only; it is not a production provider runbook or evidence that a provider account was connected.

## Architecture and trust boundaries

- `Elsa.Connections` owns connection metadata, tenant/environment binding, authorization, refresh orchestration, and recovery contracts. `Elsa.Connections.Persistence.EFCore` owns the durable connection row and conditional revision/fence updates. The SQLite adapter is the executable reference used by the current tests.
- `Elsa.Secrets` remains the encrypted value store. Connections creates one immutable managed Secrets generation per grant or refresh and updates the connection pointer only after the encrypted generation exists. Persisted owner/generation markers make generic Secrets update, rotate, delete, list, and resolve paths reject or hide managed generations. `IManagedSecretManager` is an internal host integration seam; HTTP callers must use the authorized Connections service.
- Human use and management are separate authorization decisions. The Connections feature defaults to deny. The host must provide an `IConnectionUseAuthorizer` that validates the authenticated actor and the requested permission against tenant, environment, and connection identity. A caller-selected system label or permission string is not sufficient evidence for background access. `IConnectionBackgroundUseService` and the recovery service are host-only contracts: they accept exact tenant/environment/connection IDs, mint their internal system identity themselves, and authorize background `use` or `manage:reconcile` before loading credentials. Keep them out of HTTP APIs and caller-controlled workflow inputs.
- `ResolveForUseAsync` returns an access-only credential. Refresh tokens stay inside lifecycle/provider operations. Connection results, secret listings, and serialized access DTOs contain no token values. The generic Secrets resolver remains a trusted low-level API for unowned secrets and does not authorize an actor.
- Tenant, environment, connection, and generation identity are checked together on reads and mutations. Copying an environment's logical connection configuration does not copy its token generation or encryption key. Do not export or promote raw token material.

## Operation and recovery behavior

Connect and refresh record an operation ID, fence, planned generation, and a two-minute lease before work can be reconciled; refresh also records the source generation. An owning operation that observes a failure marks recovery immediately. A separate worker may reconcile an in-flight operation only after the persisted lease expires; the expiry check is part of the database conditional update. Reconciliation never calls the provider with the prior refresh token.

After the provider-call boundary, a timeout, cancellation, restart, or lost persistence result is an unknown outcome. The connection moves to `RecoveryRequired` unless the already-encrypted planned generation can be verified and safely promoted for that same operation and source generation. A disconnected or newer operation is never revived by recovery. When the provider response is lost and no replacement generation exists, the owning application must arrange provider-specific reauthentication or recovery; do not retry the old token automatically.

`IConnectionLifecycleRecoveryService.ReconcileAsync` and `IConnectionBackgroundUseService.ResolveForUseAsync` are available for the host to invoke, but this slice does not install a timer, hosted worker, queue consumer, or workflow resumption handler. Hosts must not report automatic recovery, workflow credential resolution, or background renewal until they bind these trusted contracts to a host-controlled execution identity and test that scheduling boundary. Provider-specific revoke, installation uninstall, general cleanup, and workflow activity/run-grant integration remain outside this slice. Before deleting a managed generation, the lifecycle owner must prove that no current connection pointer or unresolved operation references it.

## Encryption key and deployment

`SecretsOptions.EncryptionKey` is a 16-, 24-, or 32-byte AES key supplied by the host, outside the database. Every process that reads the same Secrets data must receive the same durable key bytes. Store key material in the deployment's secret/configuration facility, never in the connection database, application logs, exports, or test artifacts. A missing or mismatched key makes encrypted generations unavailable; it does not authorize a fallback to cleartext or a retry of a provider call.

The current protector has one configured key and no key identifier, key ring, or re-encryption workflow. Replacing the configured key without first migrating encrypted values makes existing generations unreadable. Treat key rotation as unsupported until Elsa provides and tests versioned key selection and re-encryption. The current Connections projects are marked `IsPackable=false`; the code and SQLite adapter are not a released production package set.

For a future deployment that packages this slice, sequence work as follows:

1. Configure and validate the same external encryption key on every replica before enabling Connections.
2. Take and verify restorable backups of every database containing connection rows or encrypted Secrets generations. Preserve the matching key material and application artifact/version with the backup record.
3. Apply the selected provider's schema migrations using the normal Elsa deployment path, then validate that the managed-secret resolver can decrypt a synthetic generation before accepting real grants.
4. Configure a real, default-deny host authorizer and provider adapter, then test tenant/environment isolation and background recovery in that host. The synthetic authorizer and provider in the unit tests are not deployment defaults.

## Upgrade, rollback, and restore

The Secrets schema adds nullable managed-owner and generation columns, and Connections adds durable lifecycle metadata. Existing Secrets rows remain unowned. Migration `Down` methods intentionally throw rather than silently erase lifecycle ownership or connection state, so there is no supported in-place downgrade path. Do not run an older application version against the upgraded schema, and do not manually drop ownership columns or connection tables while lifecycle-managed generations exist.

If rollout must be reversed, restore the complete set of predeployment database backups and the matching predeployment application version together. Restore the key material that encrypts those databases as part of the same recovery. The restore returns the system to the backup point: connections or token generations created afterward are lost and may require users to reconnect or reauthorize with the provider. Do not combine predeployment databases with a postdeployment key or vice versa. If the required backups or matching key are unavailable, stop and escalate; do not run migration `Down` or delete managed secret rows to force an older binary to start.

## Current verification and explicit limits

The current executable proof is documented in the [validation slice](credential-lifecycle-validation.md). It exercises the SQLite persistence adapter and actual Secrets EF feature registration with synthetic credentials. It does not prove production database-provider behavior, live provider semantics, package publication, multi-replica key provisioning, scheduled background reconciliation, workflow activity integration, Studio UX, connection export/promotion, or provider offboarding. Those need their own acceptance work before making corresponding product or operational claims.
