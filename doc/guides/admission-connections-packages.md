# Admission and Connections package support

These seven existing module identities are selected for the consolidated Core 3.10 line under [#8661](https://github.com/elsa-workflows/elsa-core/issues/8661). Their projects use the normal Core library packaging convention, inheriting target frameworks `net8.0`, `net9.0` and `net10.0`. This source baseline preserves existing APIs and schemas; it does not claim that the new packages have passed evaluated MSBuild, archive or PostgreSQL consumer verification. Exact-head hosted evidence is required for that acceptance.

Use one tested head and version for the entire internal dependency closure. The accepted [lockstep/Core publisher policy](../adr/2026-09-28-lockstep-consolidated-release-and-publisher-cutover.md) applies: no independent version streams, historical candidate modification or publication is authorized by enabling these projects. The original stable candidate `11412848210` excludes these identities and cannot be reused as their package evidence. SQLite Connections persistence and other optional providers remain outside this promotion.

## Public setup and contract baseline

Package IDs, assembly names, namespaces, public constructors and dependency edges are unchanged. The following surfaces are the host/provider contracts to review together with their actual packed dependent types; this table is not a generated binary API compatibility report.

| Package | Public setup and contract families | Support boundary |
| --- | --- | --- |
| `Elsa.Connections` | [ConnectionsFeature](../../src/modules/Elsa.Connections/Features/ConnectionsFeature.cs); lifecycle/static-key/recovery/background-use, metadata inspection, provider, authorization and store [contracts](../../src/modules/Elsa.Connections/Contracts); connection/generation/binding/grant/offboarding [models](../../src/modules/Elsa.Connections/Models) | Tenant/environment-scoped lifecycle with managed Secrets generations. A connection ID, metadata access or lifecycle-management permission does not grant credential use or sharing. |
| `Elsa.Connections.Credentials.Workflows` | [WorkflowCredentialBindingsFeature](../../src/modules/Elsa.Connections.Credentials.Workflows/Features/WorkflowCredentialBindingsFeature.cs), [WorkflowCredentialUseGrantsFeature](../../src/modules/Elsa.Connections.Credentials.Workflows/Features/WorkflowCredentialUseGrantsFeature.cs); resolver, binding manager and grant manager [contracts](../../src/modules/Elsa.Connections.Credentials.Workflows/Contracts) | Explicit host EnvironmentId and trusted nondefault tenant. Grants bind the actual workflow instance, logical binding/revision and connection. Management/share/use policies are separate. |
| `Elsa.Connections.Credentials.Persistence.EFCore` | [ConnectionsFeature.UseEntityFrameworkCore](../../src/modules/Elsa.Connections.Credentials.Persistence.EFCore/Extensions/ConnectionsFeatureExtensions.cs), [EFCoreConnectionsPersistenceFeature](../../src/modules/Elsa.Connections.Credentials.Persistence.EFCore/Features/EFCoreConnectionsPersistenceFeature.cs), [ConnectionsElsaDbContext](../../src/modules/Elsa.Connections.Credentials.Persistence.EFCore/ConnectionsElsaDbContext.cs), public EF stores/configurations and conflict-classifier contract | Provider extension seam, not a second credential store. Preserve lifecycle revisions/fences, scope, cleanup tombstones, logical bindings and exact-instance grants. |
| `Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql` | [UsePostgreSql overloads](../../src/modules/Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql/Extensions/PostgreSqlConnectionsPersistenceFeatureExtensions.cs), context factory, conflict classifier and migrations | Selected Npgsql provider/migration assembly. Only SQLSTATE `23505` is classified as a duplicate binding conflict. |
| `Elsa.Workflows.Admission` | Classic and Shell `AdmissionFeature`; [AdmissionHostConfiguration](../../src/modules/Elsa.Workflows.Admission/AdmissionHostConfiguration.cs); [IAdmissionStore / IAdmissionDefinitionBootstrapStore](../../src/modules/Elsa.Workflows.Admission/IAdmissionStore.cs); [models/policy](../../src/modules/Elsa.Workflows.Admission/AdmissionModels.cs), bootstrap/execution/recovery services and observer boundaries | One reviewed in-process local executor, fixed workflow/subscription inventory and default-denied direct entry. It is not a transport listener or a general-purpose execution sandbox. |
| `Elsa.Workflows.Admission.Persistence.EFCore` | [UseAdmissionPersistence](../../src/modules/Elsa.Workflows.Admission.Persistence.EFCore/Extensions/AdmissionPersistenceExtensions.cs), classic persistence feature, Shell persistence base, trusted scope, [AdmissionElsaDbContext](../../src/modules/Elsa.Workflows.Admission.Persistence.EFCore/AdmissionElsaDbContext.cs), EF store and transaction-lock contract | Durable provider seam for the selected ledger; public mutable entities are persisted snapshots, never runnable permits or authority to edit rows directly. |
| `Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql` | [UsePostgreSql overloads](../../src/modules/Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql/Extensions/PostgreSqlAdmissionPersistenceExtensions.cs), [PostgreSqlAdmissionPersistenceShellFeature](../../src/modules/Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql/ShellFeatures/PostgreSqlAdmissionPersistenceShellFeature.cs), bootstrap store/session lock, context factory and migration | PostgreSQL is the selected Admission provider. No cross-provider or cross-store atomic transaction guarantee. |

The extensions expose existing EF Core/Npgsql option types, and Admission's public signatures depend on same-head workflow management/runtime types. Admission also declares `Microsoft.AspNetCore.App` as a framework reference; a Generic Host still needs that shared framework even though it must map no HTTP routes. Public implementation classes do not authorize arbitrary replacements: Admission validates concrete audited services, activity types and predefined subscription fingerprints before effects. Changing those registrations, public constructors, serialization or enum/state meanings needs renewed API/schema and host review, not merely successful package restore.

`src/PackageManifest.props` selects manifest generation for projects with `ShellFeatures`. Admission and its PostgreSQL Shell provider supply selectable features; the EF Shell base is not a standalone feature. Connections/grant packages currently have classic features only. Do not advertise automatic Shell discovery for them or require helper-only packages to invent a selectable feature. Hosted verification must inspect the actual generated manifest/runtime hints and absence where appropriate.

## Compose the two supported hosts

Configure normal workflow definition/instance/runtime persistence, Secrets and the Connections infrastructure before installing Admission's guarded composition. Use the selected same-version Core, Secrets and EF/PostgreSQL packages too; the seven IDs alone are not the full transitive closure. Retain normal library-to-library ProjectReference edges in the producer; external conformance hosts must resolve those edges as packages, never compile repository libraries.

These classic registration fragments show the public selection APIs. They are not a complete runnable host: supply the actual trusted scope, normal runtime/persistence, Secrets protection, explicit policies and reviewed `AdmissionHostConfiguration`.

```csharp
using Elsa.Connections.Features;
using Elsa.Connections.Credentials.Persistence.EFCore.Extensions;
using Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql.Extensions;
using Elsa.Connections.Credentials.Workflows.Features;
using Elsa.Workflows.Admission.Persistence.EFCore.Extensions;
using Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql.Extensions;

module.Configure<ConnectionsFeature>(feature =>
    feature.UseEntityFrameworkCore(persistence =>
        persistence.UsePostgreSql(connectionString)));
module.Configure<WorkflowCredentialBindingsFeature>(feature =>
    feature.EnvironmentId = environmentId);
module.Configure<WorkflowCredentialUseGrantsFeature>();

// Admission host only, after its supported infrastructure is selected.
module.UseAdmissionPersistence(feature =>
{
    feature.TenantId = tenantId;
    feature.EnvironmentId = environmentId;
    feature.UsePostgreSql(connectionString);
});
module.Configure<Elsa.Workflows.Admission.Features.AdmissionFeature>(feature =>
    feature.Configuration = reviewedConfiguration);
```

For Shell composition, select the actual workflow/runtime/normal PostgreSQL persistence Shell features, `Elsa.Workflows.Admission.ShellFeatures.AdmissionFeature` with its Configuration, and `PostgreSqlAdmissionPersistenceShellFeature` with TenantId, EnvironmentId, ConnectionString and explicit migration settings. Keep the existing classic Connections, binding/grant and Secrets infrastructure registrations in this supported hybrid. Selecting the Admission provider does not select other contexts or grant authority. See [isolated Admission operations](durable-event-admission.md) for provisioning, activation, final-write/checkpoint, continuation, withdrawal and recovery rules.

The positive credential-use scenario runs in a **separate non-Admission host and database**. Use real `IStaticApiKeyLifecycleService.ConnectApiKeyAsync`, `IWorkflowCredentialBindingManager.CreateAsync` and `IWorkflowCredentialGrantManager.IssueAsync` with an authenticated scope-limited principal and an actual persisted workflow instance/binding revision. Suspend that workflow before credential resolution, dispose the host, restart and resume through the normal runtime. `IWorkflowCredentialResolver.ResolveAsync` receives the real execution context and logical binding ID. Keep `StoredConnectionCredentialBindingUseAuthorizer`, real managed Secrets/stores/resolver and background-use policy; an ungranted second instance must be denied by the grant gate. Never export the resolved credential into workflow state, logs or receipts. A static key requires no provider call.

Do not make that positive host an exception inside Admission. Admission deliberately replaces public connection lifecycle, static-key, recovery and background-use services with deniers and rejects direct concrete lifecycle registration. No admission, bootstrap or activation creates a workflow-use grant. No mapped HTTP/import/Alterations/remote-runtime routes, autonomous trigger or second store-sharing executor may remain invocable in the isolated host. Trusted audited activities are not hostile-plugin isolation.

## Contexts, migrations and recovery

| Context/provider | Persisted scope | Migration boundary |
| --- | --- | --- |
| `ConnectionsElsaDbContext` / Connections PostgreSQL | Connections, generation cleanup ledger, credential bindings, credential-use grants and offboarding operations | `20260923225653_Initial`, `20260924125922_WorkflowCredentialUseGrants`, `20260924150000_DueCredentialLifecycleCandidates` |
| `AdmissionElsaDbContext` / Admission PostgreSQL | AdmissionSubscriptions and Admissions, revisions/epochs/configuration/event digests, capacity, execution ownership and checkpoints | `20261008040000_InitialAdmission`; default separate history `__AdmissionMigrationsHistory` |
| `SecretsElsaDbContext` / existing Secrets PostgreSQL | Managed secret metadata and encrypted payload/version representation in Secrets | Existing Core Secrets provider migrations; selected independently from Connections/Admission |

Keep each context's actual provider/migration assembly configured. Connections and Secrets use their configured general history settings; do not assume one context per database or invent a distinct default history name. Admission's provider supplies its separate history default and Shell selection copies shared options without changing another context's history. Apply all selected contexts through Elsa migrations, then reapply against populated state and verify histories, rows and recovery metadata. `EnsureCreated` is not migration/reapplication evidence. Package-only coexistence/reapplication is a required hosted gate, not established by this source table.

The Connections and Admission migrations deliberately reject destructive Down operations. No previously released PostgreSQL Connections/Admission schema upgrade or SQLite-to-PostgreSQL conversion is claimed. Preserve unresolved operations, ownership tombstones and exact revisions during deployment/recovery; never reset the ledger to clear capacity or make an uncertain operation runnable. Back up Connections, matching Secrets generations/protection key and workflow/Admission state with the application version. Missing/mismatched keys leave managed credentials unusable. Cross-store recovery is explicit, not a transaction spanning provider effects and all databases. See [Connections provider scope](../integration-program/secrets/postgresql-connections-provider.md) and [credential lifecycle operations](../integration-program/secrets/credential-lifecycle-operations.md).

## Package acceptance boundary

#8661 requires evaluated metadata and immutable new artifacts, followed by six net8/net9/net10 × classic/Shell build cells and **twelve runtime scenario records**, Admission plus positive grant in every cell. Run the two scenarios sequentially with distinct databases and one executor each, disposing before restart. Restore the exact produced archives through an isolated mapped feed/cache; retain actual PostgreSQL/process/cleanup, migration, package/cache/loaded-DLL and source/run evidence. Missing, skipped or incomplete records fail closed. Source-mode #8658 runtime proof and prior representative consolidated consumers are useful foundations, not these new package-consumer results.

Socket Mode ingress/ACK/reconnect, additive Slack activities and legacy Token migration, durable remote-write uncertainty, live same-thread replies, owner activation policy, production credentials/key custody and operations remain separate outcomes. So do optional-provider support, Studio browser compatibility, fresh stable-candidate approval, publication, publisher cutover, deployment and archival.
