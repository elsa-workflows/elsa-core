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

### Released 3.8.4 BPMN package availability

The backend omits `Elsa.Bpmn.Interchange` only for 3.8.4. The primary
[NuGet version index](https://api.nuget.org/v3-flatcontainer/elsa.bpmn.interchange/index.json)
observed on 2026-10-06 lists `3.9.0` alone, with no `3.8.4`; the saved response
`/tmp/elsa-bpmn-interchange-version-index.json` has SHA-256
`7947195d7d34f80375220d8832e09e41c8918a73b14e189ccdb273ca5ed4b7a2`.
The published Core 3.8.4
[`src` tree](https://github.com/elsa-workflows/elsa-core/tree/33181ae3048f628f591a0155b5665a8e4d1bcea2/src)
also contains no BPMN source paths.

The preserved initial fixture attempt requested `Elsa.Bpmn.Interchange >= 3.8.4`.
NuGet reported NU1601 and selected 3.9.0, which introduced fourteen 3.9.0 Elsa
packages into the 3.8.4 backend closure. The strict prelaunch provenance check
rejected that mixed release before runtime readiness. This was an incorrect
fixture package selection, not an observed defect in published 3.8.4 runtime
behavior. The failed diagnostic remains at
`/tmp/elsa-8643-baseline-3.8-browser-1/diagnostic.json` (SHA-256
`cd7b14e022f459146780de3a560e98396cb6e401d24ad6d2670613255d0740ba`),
and the sanitized dependency-parent evidence remains at
`/tmp/elsa-8643-baseline-3.8-mixed-dependency-parents.json` (SHA-256
`627eda11bc5fb917623ef6f307f32ade1eef60a3e840efba29be04addce38979`).

Only 3.9.0 and accepted 3.10.0 backend projects define `FIXTURE_BPMN`, which gates
`UseBpmnInterchange`. Readiness requires both `Elsa.Bpmn` and
`Elsa.BpmnInterchange` for those versions and forbids either for 3.8.4. The existing
WorkflowContexts/Secrets feature flags and Studio BPMN designer composition are
unchanged. No corrected 3.8.4 build/start/browser result is claimed by this change.
