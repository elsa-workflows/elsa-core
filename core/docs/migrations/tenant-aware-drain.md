# Graceful-shutdown drain persists instances under their own tenant

**Behavior change.** When a drain force-cancels execution cycles (deadline exceeded or operator force), it now reads and marks each instance under the tenant the cycle ran in. Before, it used its own scope, which has no tenant on host stop. Instances of named tenants were then not found, stayed `Running`/`Executing` and were never recovered, and the execution log got a `PersistenceFailure` entry saying that no instance row was persisted.

## Drain

- `ExecutionCycleRegistry` records the tenant that is current when a cycle begins.
- `DrainOrchestrator` reads and writes each instance in a scope created by `ITenantScopeFactory` for that tenant. A tenant with its own database is read from its own database. The `WorkflowInterrupted` log entry is written in the same scope.
- A cycle without a recorded tenant is persisted in a scope without a tenant, as before.
- Each instance is persisted in its own scope, instead of one scope shared by all instances of the drain.
- Instances of named tenants that a drain marks `Interrupted` are now recovered by the interrupted-workflow scan on the next start.

## Tenant scopes

- Disposing a `TenantScope` (from `ITenantScopeFactory.CreateScope`) now restores the previous tenant in the code that disposed it. Before, code that kept running after `await using` a tenant scope stayed under that scope's tenant until its async method returned.

## What to do

- Code that creates `DrainOrchestrator` or `ExecutionCycleRegistry` with the `[Obsolete]` constructors keeps the old behavior. Switch to the new constructors, which take an `ITenantScopeFactory` and an `ITenantAccessor`.
- A custom `IExecutionCycleRegistry` records no tenant, so the drain persists its cycles without a tenant, as before.
