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
| Studio candidate | [`9afd3e3`](https://github.com/elsa-workflows/elsa-studio/commit/9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822) | Candidate Studio source from the import inventory. |

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

- No public API compatibility build has compiled old 3.8.1 consumers against Core 3.8.4 or the candidate for net8.0, net9.0 and net10.0.
- No SQLite, PostgreSQL or SQL Server database has been upgraded from Extensions 3.8.1; no rollback, duplicate-name, tenant or ciphertext fixture exists.
- No runtime test has activated both endpoint sets to confirm the static route conflicts or tested permission/authorization behavior after choosing one set.
- No browser test covers the current Studio list/detail/create/update/rotate/revoke/test/picker flows or the old UI's unique-name/bulk-delete behaviors.
- Extensions PR #216's [failed CI run](https://github.com/elsa-workflows/elsa-extensions/actions/runs/35703528952/job/106666898833) at `b6ed67f` reports `CS0234` for `Blazored.FluentValidation` and `CS0246` for `FluentValidationValidator` in `Elsa.Studio.Secrets` and `Elsa.Studio.WorkflowContexts` across target frameworks. Matching package versions alone did not establish build compatibility.
- The public `Elsa.Secrets.Models` 3.8.4 package has no nuspec repository commit, although earlier versions identify Extensions. Its 3.8.4 binary source revision and downstream use remain unknown.
- NuGet version indexes show publication, not download counts, private-feed usage or actual installed versions. Those must be discovered before a retirement or cutover claim.

The next implementation scope belongs in #8214: establish a bounded package/API/storage ownership slice, preserve the old public capability boundary, and produce provider-specific upgrade evidence before any publisher decision. A local issue draft is in [8214-secrets-collision-compatibility.md](issue-drafts/8214-secrets-collision-compatibility.md); it has not been created on GitHub.
