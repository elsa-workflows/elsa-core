# Owned backend runtime configuration

`start_pair` generates a fresh random 32-byte Secrets encryption key for every
owned runtime. Its base64 encoding enters only the backend environment as
`Fixture__SecretsEncryptionKey`. It is absent from materialized files, process
arguments, browser handles, Studio environment, readiness and retained evidence.
The package-based Secrets feature configures `SecretsOptions.EncryptionKey`
through `ConfigureOptions`; it rejects missing or incorrectly sized keys when
Secrets is enabled. Stopping the owned process ends the key's lifetime.

The Python `permission_grants(request)` function supplies the exact allowlisted
array as private backend environment configuration. Inherited `Fixture` settings
are removed before the owned settings are supplied. Readiness reports the exact
profile and actual grant array, plus independent `workflow_contexts_enabled` and
`secrets_enabled` registration flags. These are configuration facts, not proof of
endpoint authorization or browser behavior.

- `full` retains the original `*` grant.
- `denied` retains the original four read grants and remains the broad write-denial
  profile.
- `deny-secrets` permits workflow viewing, editing, publishing and execution, and
  WorkflowContexts descriptor reads, while granting no Secrets permission.
- `deny-workflow-contexts` permits the same workflow operations and Secrets
  operations, while omitting `read:workflow-context-provider-descriptors`.

The explicit editor grants use the published Core 3.8.4 legacy endpoint vocabulary
at `33181ae3048f628f591a0155b5665a8e4d1bcea2`, including
`write:workflow-definitions` and `exec:workflow-definitions`. Core 3.9.0 at
`6436609a1a3874d3fea7ccf690897792f5a1f702` and accepted candidate source
`ba5b348aa2414fdf7c19d9d8806e87b7c91a4205` use the resource catalog, including
`workflows/definitions:write` and `workflows/definitions:execute`. The canonical
Secrets module likewise changes from 3.8.4 `read:secrets`/`write:secrets` to
3.9/3.10 `secrets:view`/`secrets:write`. WorkflowContexts retains its published
descriptor permission in all selected versions. Neither targeted denial profile
uses a wildcard, and feature registration does not change its grants.

Synthetic Secrets creation can use the existing authenticated package API:
`POST {backend_url}/secrets` with a unique synthetic `name` and `value` under the
full profile. The canonical request defaults to a text secret in the encrypted
store, and its response returns metadata rather than the secret value. The
browser owner must keep synthetic values and authorization inputs private and
verify the returned identity/reference using the real UI/API. No fixture seed
endpoint or unauthenticated production endpoint is added. The exact existing
picker/descriptors API and versioned reference shape must be observed before a
journey claims successful secret selection or execution.

## Released baseline feature boundary

The production execution policy uses WorkflowContexts without Secrets registration
for the 24 released 3.8.4/3.9.0 shell/editor/export records. All twelve accepted
3.10.0 candidate records retain both features and the complete representative
Secrets/context/permission journey. Each execution receipt binds
`requested_backend_features`, `permission_profile` and `feature_policy`, including
failed preflight records. This policy changes neither Studio host glue nor package
closure, and does not imply that released EF Secrets is compatible or unsupported.

The local 3.9.0/net10.0 Secrets-enabled baseline attempt failed before backend
readiness with singleton `ISecretManager`/`ISecretResolver` consuming the scoped
`IDbContextFactory<SecretsElsaDbContext>`. Its private failure log is preserved;
SHA-256 `3d047b98f027d745ae39a73c216ae3d92c4ee8252f4e32ed2dfefc3c3723687a`
identifies the evidence without retaining its raw contents here. No browser smoke
or Secrets compatibility passed in that attempt.

Published 3.8.4 source
[`ServiceCollectionExtensions.cs:23-24`](https://github.com/elsa-workflows/elsa-core/blob/33181ae3048f628f591a0155b5665a8e4d1bcea2/src/modules/Elsa.Secrets/Extensions/ServiceCollectionExtensions.cs#L23-L24)
and 3.9.0 source
[`ServiceCollectionExtensions.cs:25-26`](https://github.com/elsa-workflows/elsa-core/blob/6436609a1a3874d3fea7ccf690897792f5a1f702/src/modules/Elsa.Secrets/Extensions/ServiceCollectionExtensions.cs#L25-L26)
register singleton manager/resolver descriptors. Their published
[`EFCoreSecretsPersistenceFeature.cs:24-26`](https://github.com/elsa-workflows/elsa-core/blob/6436609a1a3874d3fea7ccf690897792f5a1f702/src/modules/Elsa.Secrets.Persistence.EFCore/Features/EFCoreSecretsPersistenceFeature.cs#L24-L26)
appends scoped descriptors instead of replacing those singletons; 3.8.4 has the
same registration at those lines. The actual persistence base defaults the factory
to scoped at
[`PersistenceFeatureBase.cs:29`](https://github.com/elsa-workflows/elsa-core/blob/6436609a1a3874d3fea7ccf690897792f5a1f702/src/modules/Elsa.Persistence.EFCore.Common/PersistenceFeatureBase.cs#L29)
and also registers scoped stores. Source therefore predicts the same optional
composition defect for 3.8.4; that prediction is not an observed 3.8.4 runtime result.
Changing only the factory lifetime cannot repair the remaining scoped graph.

Accepted candidate source already replaces the singleton registrations with scoped
ones in
[`EFCoreSecretsPersistenceFeature.cs:26-29`](https://github.com/elsa-workflows/elsa-core/blob/ba5b348aa2414fdf7c19d9d8806e87b7c91a4205/src/modules/Elsa.Secrets.Persistence.EFCore/Features/EFCoreSecretsPersistenceFeature.cs#L26-L29).
The fixture makes no service-descriptor repairs and leaves dependency validation
enabled. The released reference server registers management/runtime/API in
[`Program.cs:82-97`](https://github.com/elsa-workflows/elsa-core/blob/6436609a1a3874d3fea7ccf690897792f5a1f702/src/apps/Elsa.Server.Web/Program.cs#L82-L97)
without `UseSecrets`; 3.8.4 has the same core composition. The reviewed baseline
feature-absent policy is limited to #8643's required released smoke/export scope.
All baseline builds, starts, browser smoke, exports and provenance checks still
require actual evidence, and the optional Secrets failure remains a known limit.
