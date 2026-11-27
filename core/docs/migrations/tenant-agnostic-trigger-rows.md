# Tenant-agnostic workflows share one row per unscheduled trigger

**Behavior change for multitenant hosts.** With multitenancy enabled (`TenantsOptions.IsEnabled`), the triggers of a tenant-agnostic (`"*"`) workflow definition are no longer copied to every tenant. Hosts without multitenancy see no change. The decision is recorded in [Tenant-agnostic workflows share one row per unscheduled trigger](../../../docs/adr/2026-11-20-tenant-agnostic-workflows-share-one-row-per-unscheduled-trigger.md).

## What changes

- **Scheduled and unscheduled triggers.** A trigger is scheduled if a trigger scheduler schedules it by its trigger ID. The new `RuntimeOptions.ScheduledTriggerNames` holds their names; the scheduling module adds `Elsa.Timer`, `Elsa.Cron` and `Elsa.StartAt`. Every other trigger (HTTP endpoints, events, webhooks, message triggers) is unscheduled. `DefaultTriggerScheduler`, `CreateSchedulesStartupTask` and `DeleteSchedules` select triggers by this set. `Delay` bookmarks are scheduled as before.
- **One shared row per unscheduled trigger.** Each unscheduled trigger of a `"*"` definition is stored once, stamped `"*"`. Every tenant sees it, and the instance it starts belongs to the tenant that receives the request or stimulus.
- **One row per tenant per scheduled trigger.** Timers, cron and start-at triggers of a `"*"` definition are stored once per active tenant, stamped with that tenant, and scheduled in that tenant. A registered `"*"` tenant gets none.
- **Publishing.** Publishing, republishing, retracting or deleting a `"*"` definition replaces the shared rows once and reconciles the scheduled rows of every active tenant, so a removed timer is unscheduled in every tenant. If reconciling one tenant fails, Elsa logs the error and continues with the other tenants; that tenant's timers, cron and start-at triggers of the definition stay as they were (missing, outdated or, after a delete, still firing) until its next activation reconciles them. The host enters `SharedTenantWriteScope` for both `WorkflowDefinition` and `StoredTrigger` around these operations; Elsa enters the `StoredTrigger` scope itself only for the provider import and for tenant activation.
- **Tenant activation.** Activation reconciles the shared rows of every published `"*"` definition the tenant sees, including those authored in the database, then the tenant's scheduled rows. Before the tenant's schedules start, it removes and unschedules the tenant's scheduled copies of `"*"` definition versions that are no longer published, for example because the definition was deleted while the tenant was inactive; the scheduled rows of the tenant's own definitions are not touched. The triggers of a `"*"` definition version are derived, and its shared rows reconciled, once per process, not once per tenant, so an activation that changes nothing costs a fixed number of queries. Each definition whose copies a tenant's activation does change costs more (its lock, a publication check and a re-read of the tenant's rows), and so does a tenant with its own database, which derives and reconciles every definition itself.
- **Tenant overrides.** A tenant overrides a `"*"` definition while it has a published version of its own definition with the same definition ID. The trigger store then does not return the shared rows of that definition to the tenant, and the tenant gets no scheduled rows of it. A draft alone does not override. When the tenant retracts or deletes its last published version, the shared triggers apply to it again.
- **HTTP endpoints with tenant route prefixes.** With the route-prefix tenant resolver, the endpoint of a `"*"` workflow is routed as `{tenantPrefix}/<base path>/<path>`, so it matches under every prefix, including one that names no tenant. Such a prefix leaves the tenant unresolved, and unless the host sets `MultitenancyOptions.RejectUnresolvedTenants`, the request falls back to the default tenant and runs the shared workflow there.
- **Replaced trigger indexers.** These rules are implemented by Elsa's `TriggerIndexer`. A host that registers another `ITriggerIndexer` gets none of them on activation, after deletions or across tenants; Elsa logs a warning once when it finds one.
- **Stores.** The Dapper trigger store honors `TriggerFilter.TenantAgnostic` on reads. The Dapper and MongoDB trigger stores delete `"*"` rows only inside a `SharedTenantWriteScope` for `StoredTrigger`; a `"*"` tenant context alone does not let them. `TriggerFilter.TenantAgnostic` no longer widens a MongoDB `DeleteManyAsync` to other tenants: it always deletes within the current tenant, plus the `"*"` rows under the grant.
- **Workflow definition summaries.** `WorkflowDefinitionSummary` has a new `TenantId` property, so the workflow definitions list endpoint now also returns `tenantId`.
- **Trigger cache on shell hosts.** On hosts that use shell features with EF Core runtime persistence, the trigger store is now decorated by the trigger cache when `CachingWorkflowRuntimeFeature` is enabled. Before, the EF Core registration bypassed the cache there.

## Upgrading

Before the upgrade, each tenant's activation wrote rows stamped with that tenant for every trigger of a `"*"` definition. After the upgrade:

- The first activation writes the shared rows of every published `"*"` definition. Each tenant's first activation then removes its own rows of unscheduled triggers of `"*"` definitions. A copy is identified by its `WorkflowDefinitionVersionId`, which names a version of the `"*"` definition, so the rows of a tenant's own definition with the same definition ID are kept.
- The first activation of a registered `"*"` tenant removes its scheduled rows of `"*"` definitions and unschedules their jobs.
- **Transition window.** Until a tenant's first activation after the upgrade, it can see both its old row and the new shared row for the same trigger. HTTP then answers 500 ("The call is ambiguous and matches multiple workflows") for that trigger, and a stimulus starts two instances, one per row.

Hosts that cannot accept this window stop every host that runs the old version, delete the tenant-stamped rows of unscheduled triggers of `"*"` definitions, and only then start the new version; old-version hosts stay stopped from the cleanup on, because an old host that activates a tenant writes its stamped rows again. The first activation then writes the shared rows; until it has, those triggers are not found rather than matched twice. The EF Core providers keep the `Triggers` and `WorkflowDefinitions` tables in the `Elsa` schema (or the schema set with `ElsaDbContextOptions.SchemaName`), except SQLite and MySQL, which have no schema there; Dapper creates them in the connection's default schema, so with Dapper use the statement for your database without the `Elsa` schema. The column names are the property names.

PostgreSQL and Oracle (EF Core):

```sql
DELETE FROM "Elsa"."Triggers"
WHERE ("TenantId" IS NULL OR "TenantId" <> '*')
  AND "Name" NOT IN ('Elsa.Timer', 'Elsa.Cron', 'Elsa.StartAt')
  AND "WorkflowDefinitionVersionId" IN (SELECT "Id" FROM "Elsa"."WorkflowDefinitions" WHERE "TenantId" = '*');
```

SQL Server (EF Core):

```sql
DELETE FROM [Elsa].[Triggers]
WHERE ([TenantId] IS NULL OR [TenantId] <> '*')
  AND [Name] NOT IN ('Elsa.Timer', 'Elsa.Cron', 'Elsa.StartAt')
  AND [WorkflowDefinitionVersionId] IN (SELECT [Id] FROM [Elsa].[WorkflowDefinitions] WHERE [TenantId] = '*');
```

SQLite and MySQL:

```sql
DELETE FROM Triggers
WHERE (TenantId IS NULL OR TenantId <> '*')
  AND Name NOT IN ('Elsa.Timer', 'Elsa.Cron', 'Elsa.StartAt')
  AND WorkflowDefinitionVersionId IN (SELECT Id FROM WorkflowDefinitions WHERE TenantId = '*');
```

Add the names of any trigger types that a custom trigger scheduler schedules to the `NOT IN` list. Run the statement in every database that holds trigger rows. Its subquery reads the `"*"` definitions of that same database: in the shared database, the definitions imported by workflow providers and those authored in a registered `"*"` tenant (which always uses the shared database); in the database of a tenant with its own database, the definitions that providers imported there. Elsa likewise reads, for each tenant, the `"*"` definitions of that tenant's database. With MongoDB, delete the same documents from the triggers collection: those whose `TenantId` is not `"*"`, whose `Name` is not a scheduled trigger name, and whose `WorkflowDefinitionVersionId` is the `Id` of a workflow definition with `TenantId` `"*"`. Hosts that do not clean up beforehand can instead accept the transition window until each tenant's first activation.

## What to do

- **Custom trigger schedulers.** A host or module with its own `ITriggerScheduler` adds the names of the triggers it schedules to `RuntimeOptions.ScheduledTriggerNames`:

  ```csharp
  services.Configure<RuntimeOptions>(options => options.ScheduledTriggerNames.Add("MyModule.MyTimer"));
  ```

  Otherwise those triggers are classified as unscheduled, stored as one shared row, and never loaded by the scheduler, so they do not run.
- **Shared authoring.** A host that publishes, retracts or deletes `"*"` definitions enters `SharedTenantWriteScope.Enter<WorkflowDefinition>()` and `SharedTenantWriteScope.Enter<StoredTrigger>()` around the operation.
- **Obsolete constructors.** The constructors of `DefaultTriggerScheduler` and `DeleteSchedules` without `IOptions<RuntimeOptions>` are obsolete. They schedule the triggers of the scheduling module.
