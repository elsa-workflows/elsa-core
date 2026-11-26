# Tenant-agnostic workflows share one row per unscheduled trigger

**Behavior change for multitenant hosts.** With multitenancy enabled (`TenantsOptions.IsEnabled`), the triggers of a tenant-agnostic (`"*"`) workflow definition are no longer copied to every tenant. Hosts without multitenancy see no change. The decision is recorded in [Tenant-agnostic workflows share one row per unscheduled trigger](../../../docs/adr/2026-11-20-tenant-agnostic-workflows-share-one-row-per-unscheduled-trigger.md).

## What changes

- **Scheduled and unscheduled triggers.** A trigger is scheduled if a trigger scheduler schedules it by its trigger ID. The new `RuntimeOptions.ScheduledTriggerNames` holds their names; the scheduling module adds `Elsa.Timer`, `Elsa.Cron` and `Elsa.StartAt`. Every other trigger (HTTP endpoints, events, webhooks, message triggers) is unscheduled. `DefaultTriggerScheduler`, `CreateSchedulesStartupTask` and `DeleteSchedules` select triggers by this set. `Delay` bookmarks are scheduled as before.
- **One shared row per unscheduled trigger.** Each unscheduled trigger of a `"*"` definition is stored once, stamped `"*"`. Every tenant sees it, and the instance it starts belongs to the tenant that receives the request or stimulus.
- **One row per tenant per scheduled trigger.** Timers, cron and start-at triggers of a `"*"` definition are stored once per active tenant, stamped with that tenant, and scheduled in that tenant. A registered `"*"` tenant gets none.
- **Publishing.** Publishing, republishing, retracting or deleting a `"*"` definition replaces the shared rows once and reconciles the scheduled rows of every active tenant, so a removed timer is unscheduled in every tenant. The host enters `SharedTenantWriteScope` for both `WorkflowDefinition` and `StoredTrigger` around these operations; Elsa enters the `StoredTrigger` scope itself only for the provider import and for tenant activation.
- **Tenant activation.** Activation reconciles the shared rows of every published `"*"` definition, including those authored in the database, then the tenant's scheduled rows. Before the tenant's schedules start, it removes and unschedules the tenant's scheduled rows whose definition version is no longer published, for example because the definition was deleted while the tenant was inactive.
- **Tenant overrides.** A tenant overrides a `"*"` definition while it has a published version of its own definition with the same definition ID. The trigger store then does not return the shared rows of that definition to the tenant, and the tenant gets no scheduled rows of it. A draft alone does not override. When the tenant retracts or deletes its last published version, the shared triggers apply to it again.
- **Stores.** The Dapper trigger store honors `TriggerFilter.TenantAgnostic` on reads, and it and the MongoDB trigger store delete `"*"` rows inside a `SharedTenantWriteScope` for `StoredTrigger`. A `"*"` tenant context alone does not let the Dapper trigger store delete them.

## Upgrading

Before the upgrade, each tenant's activation wrote rows stamped with that tenant for every trigger of a `"*"` definition. After the upgrade:

- The first activation writes the shared rows of every published `"*"` definition. Each tenant's first activation then removes its own rows of unscheduled triggers of `"*"` definitions. A copy is identified by its `WorkflowDefinitionVersionId`, which names a version of the `"*"` definition, so the rows of a tenant's own definition with the same definition ID are kept.
- The first activation of a registered `"*"` tenant removes its scheduled rows of `"*"` definitions and unschedules their jobs.
- **Transition window.** Until a tenant's first activation after the upgrade, it can see both its old row and the new shared row for the same trigger. HTTP then answers 500 ("The call is ambiguous and matches multiple workflows") for that trigger, and a stimulus starts two instances, one per row.

Hosts that cannot accept this window delete the tenant-stamped rows of unscheduled triggers of `"*"` definitions before they start the new version. The first activation then writes the shared rows; until it has, those triggers are not found rather than matched twice. For the EF Core and Dapper table names (add your schema, for example `"Elsa".`, where your provider uses one):

```sql
DELETE FROM Triggers
WHERE COALESCE(TenantId, '') <> '*'
  AND Name NOT IN ('Elsa.Timer', 'Elsa.Cron', 'Elsa.StartAt')
  AND WorkflowDefinitionVersionId IN (SELECT Id FROM WorkflowDefinitions WHERE TenantId = '*');
```

Add the names of any trigger types that a custom trigger scheduler schedules to the `NOT IN` list. Tenants with their own database need the same statement in their database.

## What to do

- **Custom trigger schedulers.** A host or module with its own `ITriggerScheduler` adds the names of the triggers it schedules to `RuntimeOptions.ScheduledTriggerNames`:

  ```csharp
  services.Configure<RuntimeOptions>(options => options.ScheduledTriggerNames.Add("MyModule.MyTimer"));
  ```

  Otherwise those triggers are classified as unscheduled, stored as one shared row, and never loaded by the scheduler, so they do not run.
- **Shared authoring.** A host that publishes, retracts or deletes `"*"` definitions enters `SharedTenantWriteScope.Enter<WorkflowDefinition>()` and `SharedTenantWriteScope.Enter<StoredTrigger>()` around the operation.
- **Obsolete constructors.** The constructors of `DefaultTriggerScheduler` and `DeleteSchedules` without `IOptions<RuntimeOptions>` are obsolete. They schedule the triggers of the scheduling module.
