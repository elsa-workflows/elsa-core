# Tenants activate in parallel

**Behavior change.** Tenants used to activate one after another. The first tenant now activates alone and the remaining tenants activate in parallel, so tenant-activated handlers, startup tasks and background tasks of different tenants run at the same time.

## Parallel activation

- On host start and for tenants added by a tenant refresh, the first tenant activates alone, because its activation applies the migrations of the database the tenants share. The remaining tenants activate in parallel, bounded by the new `Elsa.Common.Multitenancy.TenantActivationOptions.MaxDegreeOfParallelism` (default 4, like `RecurringTaskOptions.MaxDegreeOfParallelism`).
- Each activation runs the tenant's startup tasks, which usually hold a database connection, so the value also bounds the connections activation holds.
- `-1` is unbounded. Valid values are `-1` or `>= 1`; anything else fails options validation before the first tenant activates. `1` restores the previous one-after-another activation.
- `IAllTenantsActivatedEvent` fires after every parallel activation completed.
- A tenant becomes visible to `ITenantService` (`FindAsync`, `GetAsync`, `ListAsync`) when its own activation starts, not before. A startup task can look up its own tenant, but tenants whose activation has not started yet are not found.
- If one tenant fails to activate, the activations already running finish, the remaining tenants are not started (one that was already dequeued when the failure happened may still start and is then treated like a completed sibling), and the original exception is rethrown. On initial activation, every tenant that activated, including those that finished after the failure, is deactivated again and the next call retries from scratch (see [Tenant activation failures](tenant-activation-failures.md)). On a refresh, tenants that activated stay active; the failed tenant and the ones that never started are not registered and the next refresh retries them.
- Tenant refresh and tenant deactivation (for example at host shutdown) wait for an initial activation that is still in progress, and never run concurrently with each other.

```csharp
services.Configure<TenantActivationOptions>(options => options.MaxDegreeOfParallelism = 8);
```

## What to do

- Make custom `ITenantActivatedEvent` handlers, startup tasks and background tasks thread-safe if they write to shared (singleton) state. They now run for several tenants at the same time. The built-in ones touched for this are the activity registry, the MQTT subscriber manager, the Kafka worker manager and workers, and the Azure Service Bus worker manager.
- Set `MaxDegreeOfParallelism` to `1` if a custom handler cannot be made thread-safe yet.
