# Secrets API and Studio contract acceptance (#8301)

**Basis:** draft history import `audit/8286-history-import-candidate` (PR #8409) at `ea25c763a5027a2f617aad5fa42915c3f2870590`, plus the commit that adds this record. That commit changes tests, the contract verifier and fixture, the contract workflow trigger list and documentation only. `src/` and `samples/` are byte-identical to `3bf0c34973a85fae4e84f7c64086f5c49fd657ee`, the head replayed by the [#8326 final-head receipt](workbench-studio-runtime/final-head-replay-3bf0c34-2026-09-27.json). Therefore that live-host receipt still describes the runtime at this head.

This record gathers evidence that is already merged and adds the tests described below. It is not a package publication, cutover, customer migration or production change.

## Pinned inputs and import reconciliation

| Input | Pin | Relationship to this head |
|---|---|---|
| Core route target | `7b06b82d0ea89c12d49c3c28da8d770bfca13faf` (selected by #8290) | Since the pin, the in-tree Core endpoints and API models differ only in the `Picker/Endpoint.cs` body (#8397, `0737e03`, `CanCreateInline` needs `secrets:write`). The route, permission and DTO table is unchanged. `verify_import_candidate` now checks this for the working tree. |
| Legacy API | Extensions `154ba15fb4da85b4bebecfbe43639579cbda1d0d` | The imported `src/extensions/secrets/Elsa.Secrets.Api` has the same Git blob as the pin for every file except `Elsa.Secrets.Api.csproj`. The import dropped that file's BOM and rewrote its project references. |
| Legacy row schema | Extensions `01bf9ad70d0399afadae9609fe820703d3de290d` | Source-only input to the verifier. |
| Studio client tested | Studio `f0eeb3c7428443b09512049fe890635fa4f7b427` | The HTTP test compiles its `ISecretsApi.cs` (blob `27f4d47`) and `SecretModels.cs` (blob `f33f976`). |

Studio reconciliation: the blobs for these two files are identical at the task-creation upstream/import pin `9afd3e3`, at the local checkout `1f3fac3`, at `20ceaee`, at `f0eeb3c` and at the newest upstream tip reachable from this head (`5b34ec3`). All five commits are ancestors of this head. The imported `src/studio/modules/Elsa.Studio.Secrets` client and DTO files are byte-identical to the test snapshots. The module differs from `f0eeb3c` in two places only: the path separators in `Elsa.Studio.Secrets.csproj`, and the `SecretsMenu` canonical remote-feature fallback (`scripts/integration-program/consolidated-build/studio-secrets-menu.patch`, recorded in the #8326 receipt). The source pin stays `f0eeb3c`. There is no reason to replace it with `1f3fac3`.

## Acceptance criteria

| # | Criterion | Status | Evidence at this head |
|---|---|---|---|
| 1 | Machine-readable fixture and executable tests pin all source commits and detect missing, duplicate or conflicting routes and DTO/permission drift | **MET** | [`secrets-api-studio-contract.json`](../../../scripts/integration-program/secrets-api-studio-contract.json) pins all four commits. [`verify_secrets_api_studio_contract.py`](../../../scripts/integration-program/verify_secrets_api_studio_contract.py) compares routes, permissions, DTO projections and the five normalized collisions at the pins (#8307, #8402, #8447). [`test_secrets_api_studio_contract.py`](../../../scripts/integration-program/test_secrets_api_studio_contract.py) has 16 tests. New tests: `test_import_candidate_tree_carries_the_pinned_route_permission_and_client_contract`, `test_import_candidate_check_fails_closed_on_route_permission_dto_or_snapshot_drift` (changed permission, duplicate route, missing route, missing legacy package, Studio-only DTO field, snapshot bytes) and `test_workflow_runs_when_an_import_candidate_source_changes`. `SecretsApiContractTests` checks the in-tree Core `Configure()` declarations. [`secrets-api-studio-contract.yml`](../../../.github/workflows/secrets-api-studio-contract.yml) now also runs when those Core, legacy or Studio sources change. |
| 2 | A default-host integration test proves one canonical Core endpoint owns every shared route, and the legacy endpoint package is inactive | **MET** (live-host receipt) | The #8471 receipt [`imported-source-route-probe-2026-09-25.json`](workbench-studio-runtime/imported-source-route-probe-2026-09-25.json) (merge `f7f6c02`) and the #8326 [`final-head-replay-3bf0c34-2026-09-27.json`](workbench-studio-runtime/final-head-replay-3bf0c34-2026-09-27.json) both record the Workbench entry point with Secrets enabled. It exposes exactly ten canonical method/template pairs, at most one handler per pair, all owned by `Elsa.Secrets`. There are no `/actions`, `/bulk-actions`, `/queries` or `/{id}/input` routes, no `Elsa.Secrets.Api`/`.Management`/`.Scripting` assembly is loaded, and none appears in the output or `deps.json`. With the default (disabled) setting, the host exposes zero Secrets routes. The receipt validator tests (`test_route_probe_rejects_legacy_routes_or_assemblies`, `test_route_probe_observes_and_rejects_each_legacy_route_family`) fail closed. In-process, `CoreSecretsFeatureRegistersExactlyOneEndpointForEachCanonicalRoute` also asserts ownership. No in-tree project other than the legacy package graph references `Elsa.Secrets.Api`. CI does not re-execute the live probe. It must be replayed whenever `src/` or the Workbench sample changes. |
| 3 | Core authorization and tenant tests cover allowed/denied principals and cross-tenant reads/writes; the report states the legacy permission comparison and whether any Owner-scoped check exists in the pinned host path | **MET** | See the next section. The new tests in [`SecretsApiStudioHttpTests.cs`](../../../test/integration/Elsa.Secrets.Api.IntegrationTests/SecretsApiStudioHttpTests.cs) are `EachCanonicalOperationAdmitsOnlyPrincipalsHoldingItsCorePermission` (10 principals × 10 operations), `PrincipalResolvedToAnotherTenantCannotReadOrChangeTheSecret` (view-only and manage, against a named and the default tenant) and `AnotherIdentityWithTheSameGrantManagesTheSecretBecauseCoreHasNoOwnerPredicate`. Earlier coverage: #8392, #8397, #8472 and #8478. Real-host coverage: the #8326 final-head receipt, 44 probes with claim-resolved tenants, a tenant admin and a roleless principal. |
| 4 | A canonical Studio/Core consumer test proves the listed operations and picker round-trip without returning stored plaintext, identifies the exact Studio commit tested and reconciles the import candidate | **MET** | `PinnedStudioRefitClientCompletesSecretOperationsWithoutReturningStoredValue` drives the pinned Studio `f0eeb3c` Refit client through list, detail, create, metadata update, descriptors, picker, rotate, test, revoke and delete. Every captured response is scanned for the supplied values and for `value`/`EncryptedValue`/`ProtectedValue`. The Studio browser receipts show the picker round trip: [`imported-source-browser-2026-09-25.json`](workbench-studio-runtime/imported-source-browser-2026-09-25.json) (`6c9c053`), the #8494 [history-import smoke](../consolidation/history-import-workbench-studio-secrets-smoke.md) (`f65aa58`) and the #8326 final-head replay. In each, an unpublished draft persists a `Secret` expression by name and type. No value appears on any screen. For the reconciliation, see the Studio paragraph above. The drift check enforces it. |
| 5 | Sidecar-dependent legacy ID test, **or** an explicit, reviewed unsupported-path and migration decision; no default compatibility route is enabled | **MET by the decision alternative** | #8360 (merged by the maintainer as `57b58b0`, 2026-09-24, an ancestor of this head) records the decision. The legacy per-version row-ID adapter, `Owner` mapping and plaintext `GET /secrets/{id}/input` are unsupported and disabled, and the sidecar is provenance only. Legacy consumer usage is unknown. ID-dependent clients stay on a separate legacy host until a reviewed bridge and a name-route client upgrade exist. The decision text and conditional migration procedure are in [secrets-collision-compatibility.md](../secrets-collision-compatibility.md#legacy-id-and-owner-disposition). `legacyIdentityDisposition` in the fixture fails verification if any field changes (`test_legacy_id_policy_fails_closed_if_adapter_or_plaintext_route_is_enabled`). The sidecar-dependent adapter test was **not** built and is not claimed. For "no default route": criterion 2's receipts, the HTTP test's 404 for `/secrets/{name}/input`, and `legacyPlaintextInputEnabled: false`. |
| 6 | Evidence records what remains unknown about real package consumers and does not equate publication with installation counts | **MET** | The [compatibility ledger](../secrets-collision-compatibility.md) states under "Public package lineage" and "Missing proof": "this proves publication, not how many consumers installed a package", and "NuGet version indexes show publication, not download counts, private-feed usage or actual installed versions". The fixture's `legacyConsumerUsage` is `unknown`, and the verifier rejects `none`. |

## Authorization matrix (criterion 3)

The TestServer hosts the canonical Core feature block: `UseSecrets`, EF Core SQLite and `UseSecretsJavaScript`, with a test authentication handler that turns a header into Elsa permission claims. Each row seeds a secret in tenant A and then sends every canonical operation. For each admitted read, the test asserts that the response contains the seeded secret, not just a 200. It then reloads the secret. A refused call must have left it unchanged, and an admitted write must be persisted. Delete is sent last.

| Principal (permission claims) | list, descriptors, detail, picker | test | update, rotate, revoke, create | delete |
|---|---|---|---|---|
| anonymous | 401 | 401 | 401 | 401 |
| authenticated, no Secrets grant (`unrelated:view`) | 403 | 403 | 403 | 403 |
| view-only (`secrets:view`) | 200 | 403 | 403 | 403 |
| manage (`secrets:*`) | 200 | 200 | 200 | 204 |
| superuser (`*`) | 200 | 200 | 200 | 204 |

Cross-tenant: a view-only principal and a manage principal were each resolved to tenant C and to the default tenant. Neither sees tenant B's secret in the list or picker, and detail returns 404. View-only update/rotate/revoke/delete/test return 403. Manage update/rotate/revoke/delete return 404, and test returns `Succeeded = false` ("not found"). Tenant B's secret keeps its ID, display name, version 1 and Active status, and still tests successfully. The same grants resolved to tenant B do see the secret, so the empty results are not vacuous. In this fixture the tenant comes from a request header the principal chooses, so tenant *membership* is not tested here. The Workbench receipts cover membership: tenants are resolved from claims ([cross-host token receipt](workbench-studio-runtime/cross-host-token-2026-09-25.json), final-head replay).

### Legacy permission comparison

The legacy endpoints at `154ba15` use `ConfigurePermissions(token)`, which also accepts `*`. Core endpoints use `RequirePermission("secrets", verb)`, which `PermissionEvaluator` evaluates with `{resource}:{verb}` matching. The HTTP matrix sends each legacy token on its own to the Core host:

| Legacy token | Legacy endpoints it guarded | Core behaviour (tested) |
|---|---|---|
| `read:secrets` | List | Authorizes no Core operation. The resource is `read`. |
| `secrets:read` | Get, GetInputModel (plaintext) | Authorizes no Core operation. Core has no `read` verb. |
| `write:secrets` | Create | Authorizes no Core operation. |
| `secrets:write` | Update, IsUniqueName, GenerateUniqueName | Create, update, rotate and revoke. It does not grant list, detail, descriptors, picker, test or delete. |
| `secrets:delete` | Delete, BulkDelete | Delete only. |
| `*` | Every legacy endpoint | Every Core operation. |

Only `*`, `secrets:write` and `secrets:delete` mean anything to Core, and only because their spelling coincides; no alias is added. Two consequences for migration:

- A principal with the complete legacy set can write and delete in Core but cannot list, read, pick or test. It needs `secrets:view`, and `secrets:test` if it tests.
- A legacy grant of `secrets:write` alone (update-only in legacy) gains create, rotate and revoke in Core.

Operators should review role grants before cutover rather than rely on token spelling.

### Owner-scoped checks

- **Legacy (`154ba15` and `01bf9ad`):** `Owner` is inherited from `Elsa.Common.Entities.ManagedEntity` by the per-version row entity. `git grep` at both pins finds it only in `Secret.Clone()`, `SecretExtensions.ToModel()` and the `SecretModel` property. The legacy API returns it but never compares it, and `SecretInputModel` has no `Owner`, so an API caller cannot set it. No legacy endpoint, manager or store uses it as a predicate. The verifier fails if a legacy endpoint starts to reference `Owner`.
- **Pinned host path (Core `Elsa.Secrets`, the only Secrets implementation loaded by the Workbench):** there is no `Owner` member. `ManagedOwnerId`/`ManagedGenerationId` are lifecycle markers: `DefaultSecretManager` refuses update, rotate, revoke, delete and test on lifecycle-managed generations for every caller, and list hides them. That check does not depend on the caller's identity and is never populated from legacy `Owner` (the fixture's `legacyOwnerMappedToManagedOwner` is `false`). `AnotherIdentityWithTheSameGrantManagesTheSecretBecauseCoreHasNoOwnerPredicate` shows that a second identity with the same grant can read, update, rotate, test, revoke and delete a secret created by another identity, and that no Core response contains an owner field.

**Conclusion:** no Owner-scoped authorization check exists in the pinned legacy API or in the consolidated host path.

## Not established here

- The TestServer covers the view-only principal against the same `PermissionEvaluator` the host uses. The real Workbench identity path, where roles expand into permission claims, has been replayed only for a `*` tenant admin and a roleless principal, not for a `secrets:view` role.
- A host alias is not a token or tenant binding (see [host aliases](../secrets-collision-compatibility.md#host-aliases-and-authenticated-tenant-scope)). Binding tokens to host aliases would need an accepted host policy and a mismatch test. None is claimed or implemented.
- The sidecar-backed legacy row-ID adapter is unsupported by decision. No customer database, key ring or client has been migrated.
- The number of installed consumers and private-feed usage of the published legacy packages remain unknown.

## Verification run for this record

Run on macOS with .NET SDK `10.0.300`, pinned through `scripts/integration-program/secrets-api-studio-contract/global.json`. Test projects target `net10.0`.

- `dotnet test test/integration/Elsa.Secrets.Api.IntegrationTests --configuration Release --framework net10.0`: 21/21 (the baseline before this change was 6/6).
- `dotnet test test/unit/Elsa.Secrets.UnitTests --configuration Release --framework net10.0`: 141/141.
- `verify_secrets_api_studio_contract.py` against Core `7b06b82`, Extensions `154ba15`/`01bf9ad` and Studio `f0eeb3c`: passed, with output byte-identical to the pre-change run.
- `test_secrets_api_studio_contract.py`: 16/16 in normal and optimized Python.
- Full `scripts/integration-program` suite: 382/382 in normal and optimized Python.
- Receipt verifiers `verify_import_source_tip_refresh.py` and `_r2` to `_r7`, and `validate_legacy_asset_dispositions.py`: all passed.
- Mutation checks, reverted afterwards:
  - Changing the Core update endpoint to require `secrets:view` failed the view-only and legacy `secrets:write` matrix rows.
  - Resolving the cross-tenant principal to the owning tenant failed all four cross-tenant cases.
