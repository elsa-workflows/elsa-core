# Opt-in parallel tenant activation

**New opt-in option.** Tenants still activate one after another by default. `TenantActivationOptions.MaxDegreeOfParallelism` can activate them in parallel, so tenant-activated handlers, startup tasks and background tasks of different tenants then run at the same time. Custom ones must be thread-safe before you raise the value above 1.

## Parallel activation

- On host start and for tenants added by a tenant refresh, tenants activate one after another by default. The new `Elsa.Common.Multitenancy.TenantActivationOptions.MaxDegreeOfParallelism` (default 1) lets them activate in parallel when you raise it.
- EF Core migrations of one database (same `DbContext` type and connection string) run once per process: the first tenant to reach the migration applies it, and the other tenants of that database wait until it is done and then skip it. Tenants with their own database migrate independently.
- `RunMigrationsStartupTask` no longer takes a lock named by the task type alone. It takes a distributed lock named by the `DbContext` type and a hash of the connection string, so nodes of a cluster still migrate one database at a time while different databases migrate concurrently. A database without a connection string keeps a lock named by the type only. A failed migration is retried by the next tenant.
- `MigratedDatabaseRegistry.TryClaim` and `Release` are obsolete in favor of `MigrateOnceAsync`. `RunMigrationsStartupTask` has a new constructor that accepts `IDistributedLockProvider` and `DistributedLockingOptions`; the old one is obsolete.
- Each activation runs the tenant's startup tasks, which usually hold a database connection, so the value also bounds the connections activation holds.
- `-1` is unbounded. Valid values are `-1` or `>= 1`; anything else fails options validation before the first tenant activates. `1` (the default) activates one after another.
- `IAllTenantsActivatedEvent` fires after every parallel activation completed.
- A tenant becomes visible to `ITenantService` (`FindAsync`, `GetAsync`, `ListAsync`) when its own activation starts, not before. A startup task can look up its own tenant, but tenants whose activation has not started yet are not found.
- If one tenant fails to activate, the activations already running finish, the remaining tenants are not started (one that was already dequeued when the failure happened may still start and is then treated like a completed sibling), and the original exception is rethrown. On initial activation, every tenant that activated, including those that finished after the failure, is deactivated again and the next call retries from scratch (see [Tenant activation failures](tenant-activation-failures.md)). On a refresh, tenants that activated stay active; the failed tenant and the ones that never started are not registered and the next refresh retries them.
- Tenant refresh and tenant deactivation (for example at host shutdown) wait for an initial activation that is still in progress, and never run concurrently with each other.

```csharp
services.Configure<TenantActivationOptions>(options => options.MaxDegreeOfParallelism = 8);
```

## What to do

- Before raising `MaxDegreeOfParallelism` above 1, make custom `ITenantActivatedEvent` handlers, startup tasks and background tasks thread-safe if they write to shared (singleton) state. They then run for several tenants at the same time. The built-in ones made safe for this are the activity registry, the HTTP route table, the MQTT subscriber manager, the Kafka worker manager and workers, and the Azure Service Bus worker manager.
- Leave `MaxDegreeOfParallelism` at `1` if a custom handler cannot be made thread-safe yet.
