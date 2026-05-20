# Health Checks

Elsa hosts can opt in to Elsa-specific readiness checks on top of the normal ASP.NET Core process liveness check. The first slice focuses on runtime dependencies that are available through existing Elsa service contracts and avoids exposing connection strings, credentials, or provider-specific details.

## Opt In

Register ASP.NET Core health checks and then add Elsa readiness checks:

```csharp
using System;

services
    .AddHealthChecks()
    .AddElsaReadinessChecks(
        includeDistributedLocks: true,
        configureOptions: options => options.DistributedLockAcquisitionTimeout = TimeSpan.FromSeconds(1));
```

Use `includeDistributedLocks: true` when the host enables distributed runtime behavior or relies on distributed locks for workflow coordination. Leave it `false` for simple single-node hosts that do not want a lock probe.

Persistence probes are enabled by default. Set `includePersistence: false` when a host uses a custom or minimal persistence setup, or when it only needs the `elsa-runtime` readiness probe:

```csharp
services
    .AddHealthChecks()
    .AddElsaReadinessChecks(includePersistence: false);
```

Map separate endpoints for liveness and readiness:

```csharp
using Elsa.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

app.MapHealthChecks("/health/live", new()
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains(HealthCheckExtensions.ElsaTag) && check.Tags.Contains(HealthCheckExtensions.ReadinessTag),
    ResultStatusCodes =
    {
        [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});
```

The reference server also keeps `/` process-liveness compatible for existing probes by mapping it with `Predicate = _ => false`, the same as `/health/live`. Use `/health/ready` for checks that should return `503` while the runtime is paused, draining, or dependency-degraded.

## Current Elsa Readiness Checks

- `elsa-runtime`: creates a workflow runtime client and reports `Degraded` when the runtime is paused or draining, because it is process-up but not accepting new work.
- `elsa-workflow-persistence`: performs small read-only probes against workflow definitions, workflow instances, triggers, and the bookmark queue store.
- `elsa-distributed-locks`: optionally verifies that the configured distributed lock provider can acquire and release a probe lock.

The health check data only includes subsystem categories and operational state such as readiness reason and active execution-cycle count. It does not include connection strings, lock names, credentials, tenant IDs, or workflow payloads.

## Kubernetes Recommendation

Use liveness to answer "is the ASP.NET Core process alive?" and readiness to answer "can this Elsa instance accept workflow traffic?"

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: http
  periodSeconds: 10
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /health/ready
    port: http
  timeoutSeconds: 2
  periodSeconds: 10
  failureThreshold: 3
```

Do not point liveness at Elsa readiness checks. A paused, draining, or temporarily database-degraded runtime should be removed from service by readiness without forcing Kubernetes to restart the process.

When `includeDistributedLocks: true` is enabled, set the readiness probe `timeoutSeconds` higher than the configured distributed-lock acquisition timeout. The built-in distributed-lock readiness check waits up to 1 second by default to acquire its probe lock, so the example uses `timeoutSeconds: 2` to avoid intermittent Kubernetes probe timeouts while the lock provider is still healthy.
