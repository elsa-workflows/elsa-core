# Secrets package lineage and compatibility evidence

This is a source and package-metadata audit for the five duplicate package IDs in the consolidation inventory. It identifies a canonical candidate and the compatibility work still required. It does not approve package retirement, publisher cutover, or a credential-data conversion.

## Source snapshots

The executable source comparison uses these exact commits:

| Snapshot | Source commit | Why it is included |
|---|---|---|
| Core Secrets EF packages 3.8.4 | [`33181ae`](https://github.com/elsa-workflows/elsa-core/commit/33181ae3048f628f591a0155b5665a8e4d1bcea2) | Public NuGet source commit for the four Core EF package IDs at 3.8.4. |
| Core candidate | [`fa1e36e`](https://github.com/elsa-workflows/elsa-core/commit/fa1e36e8890a3ccd35626844772b781c29503342) | Candidate Core implementation from the import inventory. |
| Extensions Secrets 3.8.1 | [`01bf9ad`](https://github.com/elsa-workflows/elsa-extensions/commit/01bf9ad70d0399afadae9609fe820703d3de290d) | Public NuGet source commit for the old Extensions EF/API/UI implementation at 3.8.1. |
| Extensions 3.8.4 | [`154ba15`](https://github.com/elsa-workflows/elsa-extensions/commit/154ba15fb4da85b4bebecfbe43639579cbda1d0d) | Public source commit for the separate legacy Secrets API/Core/Management/Scripting packages. |
| Studio Secrets 3.8.4 | [`9bff3f7`](https://github.com/elsa-workflows/elsa-studio/commit/9bff3f785fd13bd80a3a7ecf88fec4aec8eef7ae) | Public NuGet source commit for `Elsa.Studio.Secrets` at 3.8.4. |
| Studio candidate | [`20ceaee`](https://github.com/elsa-workflows/elsa-studio/commit/20ceaeeed7e671f0c9662003e82063026f2216de) | Current-tip Studio source selected for the pinned import rehearsal; the Secrets Refit interface and DTO source bytes match the earlier `9afd3e3` contract pin. |

The report can be reproduced with local Core and Extensions clones:

```sh
python3 scripts/integration-program/compare-secrets-contracts.py \
  --core /path/to/elsa-core \
  --extensions /path/to/elsa-extensions
python3 -m unittest discover -s scripts/integration-program -p 'test_*.py'
```

The tool reads git objects only. It extracts SQLite migration columns and route templates from the pinned source trees. Its output explicitly leaves migration execution and credential conversion unverified.

## Public package lineage

The official NuGet flat-container version indexes and the 3.8.1, 3.8.2 and 3.8.4 nuspecs were checked on 2026-09-23. All ten IDs below have a 3.8.4 artifact. The ownership statements below are limited to those inspected nuspec versions; this proves publication, not how many consumers installed a package.

| Package IDs | Published ownership visible in nuspec repository metadata |
|---|---|
| `Elsa.Secrets.Persistence.EFCore`, `.PostgreSql`, `.SqlServer`, `.Sqlite` | The inspected 3.8.1 artifacts identify Extensions commit `01bf9ad`; the 3.8.2 and 3.8.4 artifacts identify Core commit `33181ae`. |
| `Elsa.Studio.Secrets` | The inspected 3.8.1 artifact identifies Extensions commit `01bf9ad`; 3.8.2 identifies Studio commit `1c72dc0`; 3.8.4 identifies Studio commit `9bff3f7`. |
| `Elsa.Secrets.Api`, `Elsa.Secrets.Core`, `Elsa.Secrets.Management`, `Elsa.Secrets.Scripting` | Their inspected 3.8.1, 3.8.2 and 3.8.4 artifacts identify Extensions commits `01bf9ad`, `e7d05ef` and `154ba15`, respectively. The 3.8.4 nuspec dependencies preserve the legacy package graph: API → Management; Management → Core + Models; Core → Models; Scripting → Management. |
| `Elsa.Secrets.Models` | Its inspected 3.8.1 and 3.8.2 artifacts identify Extensions commits `01bf9ad` and `e7d05ef`. The 3.8.4 artifact is indexed, but that nuspec has no repository URL or commit, so its exact source revision remains unknown. |

The four duplicate persistence IDs were published from both codebases at the inspected releases. Therefore the Extensions 3.8.1 schema is a real public upgrade baseline; it is not merely an unpublished project copy. The Extensions project copies still present at commit `33fa0bfd` should not be treated as a separate 3.8.4 artifact: the inspected 3.8.4 nuspecs for the duplicate IDs point to Core or Studio commits. Preserve the old source history and public contract while resolving the upgrade path.

Official metadata endpoints: [NuGet flat-container API](https://api.nuget.org/v3/index.json), and the per-package [`index.json`](https://api.nuget.org/v3-flatcontainer/elsa.secrets.persistence.efcore/index.json) and [`3.8.4 nuspec`](https://api.nuget.org/v3-flatcontainer/elsa.secrets.persistence.efcore/3.8.4/elsa.secrets.persistence.efcore.nuspec). Replace the package ID in those paths for the other rows.

## Persistence contract

The source comparison found different public persistence surfaces:

| Core candidate | Extensions 3.8.1 |
|---|---|
| `Elsa.Secrets.Persistence.EFCore/SecretsElsaDbContext.cs`: `SecretsElsaDbContext` | `Elsa.Secrets.Persistence.EFCore/DbContext.cs`: `SecretsDbContext` |
| `EFCoreSecretsPersistenceFeature`, `SecretsFeatureExtensions.UseEntityFrameworkCore`, `ISecretRepository` | `EFCoreSecretPersistenceFeature`, `Extensions.UseEntityFrameworkCore`, `ISecretStore` |
| `Elsa.Secrets.Models.Secret` with `Name`, `DisplayName`, `TypeName`, `StoreName`, `Tags`, inline `Versions` | `Elsa.Secrets.Management.Secret` with `SecretId`, `EncryptedValue`, row-level `Version`/`IsLatest`, `ExpiresIn`, `ExpiresAt`, `LastAccessedAt`, `Owner` |

The SQLite migration comparison makes the data-shape conflict concrete:

| Source | Migration | Columns |
|---|---|---|
| Extensions 3.8.1 | `src/modules/secrets/Elsa.Secrets.Persistence.EFCore.Sqlite/Migrations/20240915164114_V3_3.cs` | `Id`, `SecretId`, `Name`, `Scope`, `EncryptedValue`, `Description`, `Version`, `IsLatest`, `Status`, `ExpiresIn`, `ExpiresAt`, `LastAccessedAt`, `TenantId`, `CreatedAt`, `UpdatedAt`, `Owner` |
| Core 3.8.4 initial migration | `src/modules/Elsa.Secrets.Persistence.EFCore.Sqlite/Migrations/Secrets/20260531141623_Initial.cs` | `Id`, `Name`, `NormalizedName`, `DisplayName`, `Description`, `TypeName`, `StoreName`, `Scope`, `Status`, `CreatedAt`, `UpdatedAt`, `Tags`, `Versions` |

Both migrations create a table named `Secrets`. The Extensions migration records a single row per secret version and encrypted value. Core stores a logical secret with serialized tags and versions. Core 3.8.4's SQLite migration directory contains only `20260531141623_Initial.cs`; it has no migration that references `V3_3` or maps the old columns. SQLite ignores the schema argument, so the Core initial migration cannot safely be presumed to apply over the old `Secrets` table. The static comparison did not execute either migration against a database. The candidate's initial migration has the same columns as the published Core initial migration; later candidate migrations add tenant behavior, so these initial-migration columns are not the candidate's effective final schema.

The released-package SQLite fixture in [PR #8278](https://github.com/elsa-workflows/elsa-core/pull/8278) now confirms the failure against exact NuGet artifact hashes. Extensions 3.8.1 applies `20240915164114_V3_3`; Core 3.8.4 then fails on pending `20260531141623_Initial` with `SQLite Error 1: 'table "Secrets" already exists'`. The fixture verifies SQLite integrity, columns, migration history, every synthetic field and ciphertext sentinel remain unchanged, then reopens both versions through a separate Extensions 3.8.1 process. This is a reproduced compatibility boundary, not a successful upgrade or a ciphertext conversion.

The separate synthetic fresh-destination bridge in [PR #8298](https://github.com/elsa-workflows/elsa-core/pull/8298) is pinned to Core source `7b06b82` and preserves legacy fields in a sidecar while writing native tenant/default-tenant rows. It verifies hashes, transactional rollback, tenant visibility, conflicts, and old-source reopen, with `cutoverAllowed=false`. The proof does not restore legacy `Owner` authorization or ID-addressed API behavior, and its pinned target is not presented as the latest Core source.

Core 3.8.4 → the candidate Core source is a separate provider migration path: the initial schema is unchanged, while the candidate adds `20260825230122_SecretTenancy.cs` and `20260914120000_SecretDefaultTenantUniqueness.cs`. Tests still need to verify these upgrades for SQLite, PostgreSQL and SQL Server, including duplicate names, null/default tenant rows, tenant ownership and rollback. The old Extensions 3.8.1 → Core 3.8.2/3.8.4 handoff also needs an explicit disposition. No credential ciphertext conversion is specified here: the crypto format, key ownership and associated-data requirements must be reviewed before any transform is proposed.

## API and Studio contract

The public Extensions 3.8.1 API is defined under `src/modules/secrets/Elsa.Secrets.Api/Endpoints/Secrets/`. The Core API is under `src/modules/Elsa.Secrets/Endpoints/Secrets/`. The static route comparison normalizes parameter names and finds these duplicate method/path pairs when both modules are activated:

| Method | Route | Extensions meaning | Core meaning |
|---|---|---|---|
| GET | `/secrets` | list old `Secret` records | list Core `SecretModel` objects |
| GET | `/secrets/{…}` | lookup by legacy row ID | lookup by Core secret name |
| POST | `/secrets` | create `SecretInputModel` | create `CreateSecretRequest` |
| POST | `/secrets/{…}` | update by legacy ID | update by Core name |
| DELETE | `/secrets/{…}` | delete by legacy ID | delete by Core name |

The rest of the APIs are also different: the old service exposes `/input`, unique-name queries and bulk delete; Core exposes rotate, revoke, test, descriptors and picker operations. In an integrated host, the legacy API module must remain inactive or be explicitly adapted; route parameter names do not avoid the conflict.

The published Studio 3.8.4 client at `src/modules/Elsa.Studio.Secrets/Client/ISecretsApi.cs` matches the Core API's name-based routes. Its `Feature.RemoteFeatureName` is `Elsa.Secrets.ShellFeatures.Secrets`, matching Core's Secrets shell feature. The Studio DTO is a subset of Core's `SecretModel` (it omits `Tags`), and the Studio create request omits Core's optional `Tags`. This is evidence of candidate alignment, not proof of UI behavior. The Extensions 3.8.1 Studio copy at `src/modules/secrets/Elsa.Studio.Secrets/Client/ISecretsApi.cs` uses IDs, `SecretInputModel`, `/input`, unique-name and bulk-delete endpoints; its menu route is `secrets`, while the current Studio page is `/security/secrets`.

The executable [API and Studio source contract](../../scripts/integration-program/verify_secrets_api_studio_contract.py) compares Core `7b06b82`, Extensions API `154ba15`, Extensions row schema `01bf9ad`, and the current-tip Studio import pin `20ceaee`. It records all ten Core routes and ten Studio Refit methods, the five normalized route collisions, and the model/query projection differences. The Core endpoint permission set is `secrets:view`, `secrets:write`, `secrets:delete`, and `secrets:test`; the legacy list/create declarations use `read:secrets` and `write:secrets`, while other legacy endpoints use `secrets:read`, `secrets:write`, and `secrets:delete`. Those spellings are not treated as aliases. The pinned legacy endpoint sources carry an `Owner` field but do not reference it in endpoint authorization code. The legacy `GET /secrets/{id}/input` decrypts and returns `SecretInputModel`; Core has no matching route and its response DTO contains none of `Value`, `EncryptedValue`, or `ProtectedValue`.

`SecretsApiContractTests` reflects the Core endpoint `Configure()` methods and checks their route/permission declarations and permission evaluator behavior. The separate `Elsa.Secrets.Api.IntegrationTests` project compiles exact Studio `20ceaee` Refit interface and DTO snapshots, then calls them against an in-process ASP.NET Core TestServer. Its Core feature registration follows the corrected Workbench Secrets block in the [reviewed canonical patch](https://github.com/elsa-workflows/elsa-core/blob/72c2731f6e0a98ec89a94e3b76c983531e82818c/scripts/integration-program/consolidated-build/workbench-canonical-secrets.patch): `UseSecrets`, EF Core SQLite, and `UseSecretsJavaScript`; the fixture substitutes a unique SQLite file and test-only encryption/authentication settings. A Python regression check compares that fixture's feature registrations with the patch. The HTTP suite verifies the ten expected Core routes appear once, descriptor names are unique, anonymous and mismatched permissions are denied, `secrets:write` permits create/update/rotate/revoke, `secrets:delete` permits delete, and `secrets:test` permits test. It also verifies that view-only cannot update/rotate/revoke/test, legacy `secrets:read` and `read:secrets` do not authorize Core view, legacy `write:secrets` does not authorize Core create, and the legacy plaintext `GET /secrets/{id}/input` returns 404. Same-name records stay isolated by the synthetic tenant-header resolver: an empty tenant cannot update/delete another tenant's record, and updating/rotating/revoking/deleting tenant A leaves tenant B unchanged. Captured responses contain neither supplied secret values nor value/ciphertext fields. This demonstrates request tenant-context isolation only: the test principal can choose the header, so membership authorization is not exercised. These are in-process HTTP contract checks with isolated SQLite and test authentication; they do not start the full Workbench host or Studio UI and do not prove browser behavior.

This HTTP fixture activates only the canonical Core Secrets feature block, with test-only SQLite configuration. It does not start the full corrected consolidated sample host, activate both endpoint assemblies together, use the production database selected by the Workbench configuration, or verify legacy-ID sidecar behavior. Static source comparison still identifies the five normalized route collisions; a dual-assembly runtime collision test remains unverified.

### Legacy ID and Owner disposition

The published legacy `GET`, update and delete routes address a **version row ID**: the old entity has both an inherited `Id` and a logical `SecretId`, and the endpoint loads the supplied ID before reading, appending a version or deleting. Core's routes address a **secret name**; `SecretVersion` has no row ID. The synthetic bridge sidecar retains the old IDs and `Owner` as provenance, but it does not recreate those endpoint semantics or authorize a caller. The old endpoint code does not use `Owner` as a permission predicate. Core's `ManagedOwnerId` and `ManagedGenerationId` are lifecycle markers and must not receive the old `Owner` value. The old `/secrets/{id}/input` route decrypts into a plaintext response and remains absent from Core.

**Decision for the first consolidated host: legacy ID and plaintext adapters are unsupported and disabled.** The [machine-readable contract](../../scripts/integration-program/secrets-api-studio-contract.json) and its source-pinned verifier fail closed if this disposition is changed without review. A sidecar row is not an alias from an old ID to a Core route. Published legacy packages remain available to existing deployments, but their actual installed-consumer count is unknown; no silent route or permission alias is promised.

Operators must keep ID-dependent clients on a **separate existing legacy host, database and Data Protection key ring** while inventorying their installed versions, route use, Owner expectations and permission tokens. Do not activate the legacy endpoint assembly beside Core in the consolidated default host. A future migration requires a reviewed provider-specific bridge, confirmed key custody, security review and approved cutover plan. Before any approved cutover, freeze writes, back up the legacy database and key ring, dry-run on a copy, reconcile every old row/logical ID with the sidecar, and update clients to Core's name-based routes and current permission tokens. Verify tenant authorization, UI/workflow references and rollback by reopening the unchanged old source. If a client cannot stop using row IDs or plaintext GET, retain the separate legacy deployment; do not route it through an unreviewed adapter. These are preparation conditions, **not** authorization or a claim that a customer conversion has run.

The initial consolidated provider build recorded these sample compile collisions before the canonical registration correction: `CS0121` at `Program.cs:622` makes `UseSecrets()` ambiguous between `Elsa.Extensions.ModuleExtensions` and `Elsa.Secrets.Extensions.ModuleExtensions`; `CS1929` at lines 627, 633, and 639 reports that legacy `SecretManagementFeature` has no `UseEntityFrameworkCore` provider extension after those colliding provider projects are excluded. The code is inside `if (useSecrets)`, but the runtime setting was false and cannot suppress compile-time errors. That pre-correction source also scheduled `UpdateExpiredSecretsRecurringTask` outside the guard at line 740. The reviewed [canonical patch in PR #8306](https://github.com/elsa-workflows/elsa-core/blob/72c2731f6e0a98ec89a94e3b76c983531e82818c/scripts/integration-program/consolidated-build/workbench-canonical-secrets.patch) switches the sample feature registration to Core Secrets and removes that legacy task. Issue [#8287](https://github.com/elsa-workflows/elsa-core/issues/8287) tracks the consolidated source build. The HTTP fixture checks the corrected Secrets feature block in isolation; this contract does not claim the full corrected default host was started or its entire HTTP route table verified.

## Legacy packages that must stay out of the default host/package graph

Alongside the five collision copies, the Extensions source tree contains these distinct legacy package IDs:

| Legacy package | Source / dependency evidence | Required boundary before import |
|---|---|---|
| `Elsa.Secrets.Api` | `SecretsApiFeature`; publishes the route set above and references `Elsa.Secrets.Management`. The old Workbench server references this project. | Do not activate beside Core's `Elsa.Secrets` endpoints. Port its required API behavior or explicitly deprecate it. |
| `Elsa.Secrets.Core` | `SecretsCoreShellFeature`; depends on `Elsa.Secrets.Models`. | Keep its old feature graph out of default package/host composition until each capability is mapped. |
| `Elsa.Secrets.Management` | `SecretManagementFeature`, `ISecretStore`, `ISecretManager`, `MemorySecretStore`, and old `Secret` entity. | Preserve released consumer support with an explicit adapter/deprecation path; do not load both old and new manager stacks by default. |
| `Elsa.Secrets.Models` | Legacy models used by Core, Management, API and Studio project references. Its public 3.8.4 nuspec has no source commit. | Retain as evidence and identify binary consumers before removal or namespace replacement. |
| `Elsa.Secrets.Scripting` | `SecretsScriptingFeature` depends on `SecretManagementFeature`; Core has a separate `Elsa.Secrets.JavaScript` implementation. | Compare persisted expression identity, registration and runtime behavior before disabling or replacing it. |

These five packages have public 3.8.4 artifacts. They must be retained and reviewed, but not activated or packed as a second Secrets implementation in the consolidated default host. The rehearsal currently treats only five same-ID project copies as collisions; the distinct legacy package graph and its route overlap must be addressed before source projects enter default discovery.

## Missing proof and known build evidence

- The package-only compile fixture at [`scripts/integration-program/secrets-package-consumer`](../../scripts/integration-program/secrets-package-consumer/README.md) now verifies one representative public EF consumer API on net8.0, net9.0 and net10.0. With exact NuGet.org package locks, the old `SecretsDbContext` consumer compiles against Extensions 3.8.1, while the Core `SecretsElsaDbContext` consumer compiles against Core 3.8.4. Compiling the same old consumer source against Core 3.8.4 fails with `CS0246` for `SecretsDbContext` on every target framework. This establishes a concrete source API incompatibility for this consumer surface; it does not establish an adapter, supported upgrade, compatibility of every old consumer, or the current Core source candidate.
- No consumer build against a locally packed current Core source candidate has been run; the compile fixture above resolves only the exact released artifacts. Its single old EF API consumer is not a representative application suite or an approved upgrade decision.
- No successful in-place migration from Extensions 3.8.1 has been demonstrated. The released-package SQLite attempt in #8278 fails closed and preserves the source. Released PostgreSQL and SQL Server collision characterizations are merged in #8340 and #8343; synthetic fresh-destination bridges #8350 and #8359 are also merged. None proves customer key custody, an in-place upgrade or production rollback.
- The #8298 SQLite bridge is synthetic and pinned to Core `7b06b82`; no live customer database, current-main source target, or production cutover is covered. The PostgreSQL and SQL Server bridge PRs pin a newer accepted Core target but remain synthetic. Legacy ID-addressed API behavior and Owner authorization are explicitly unsupported by every sidecar proof.
- No runtime host test should activate both overlapping endpoint sets by default. #8333 started an isolated corrected Workbench and paired Studio host with canonical Secrets enabled and exercised browser/API actions, but its receipt does not enumerate the entire active route table; #8326 still owns that check, the two-tenant run and the eventual imported-source repeat. The separate TestServer suite covers Core route declarations, test-auth permission gates, and synthetic tenant-context isolation, not host membership authorization.
- #8333 merged a bounded, synthetic default-tenant Workbench/Studio browser receipt for current Secrets management actions and a saved unpublished picker reference. The two-tenant actual-host/browser repeat and final imported-source repeat remain in #8326. The pinned Refit client and DTO snapshots by themselves exercise HTTP serialization and route contracts, not the Studio UI's dependency injection or rendered interactions.
- Extensions PR #216's [failed CI run](https://github.com/elsa-workflows/elsa-extensions/actions/runs/35703528952/job/106666898833) at `b6ed67f` reports `CS0234` for `Blazored.FluentValidation` and `CS0246` for `FluentValidationValidator` in `Elsa.Studio.Secrets` and `Elsa.Studio.WorkflowContexts` across target frameworks. Matching package versions alone did not establish build compatibility.
- The public `Elsa.Secrets.Models` 3.8.4 package has no nuspec repository commit, although earlier versions identify Extensions. Its 3.8.4 binary source revision and downstream use remain unknown.
- NuGet version indexes show publication, not download counts, private-feed usage or actual installed versions. Those must be discovered before a retirement or cutover claim.

The compatibility work remains tracked by [Story #8275](https://github.com/elsa-workflows/elsa-core/issues/8275) under Feature #8214. The published SQLite characterization and synthetic bridge are completed evidence slices; they do not authorize package retirement, production migration, or cutover. [Task #8301](https://github.com/elsa-workflows/elsa-core/issues/8301) covers the remaining canonical Studio/API identity and authorization contract, including runtime verification of the Core route set. Existing issue-body drafts remain in [issue-drafts](issue-drafts/8214-secrets-collision-compatibility.md) and [the task draft](issue-drafts/8214-task-secrets-sqlite-381-384.md).
