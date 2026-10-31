# CLR workflows are built once for all tenants

**Behavior change.** Workflows registered in code (`AddWorkflow<T>()`, provided by `ClrWorkflowsProvider`) are no longer built once per tenant. They are built once per service container, and every tenant reuses that build.

Now:

- `IWorkflow.Build` runs once, in the scope of the first tenant that is activated. A tenant with its own database gets the definition built in that first tenant's scope instead of its own.
- When the stored definition already matches the build, it is not saved again. Its `CreatedAt` is therefore no longer refreshed on every activation, and a CLR row that already matches is not re-stamped to the declared tenant.
- Triggers are still indexed per tenant.

**What to do:** make sure `Build` does not read tenant-scoped services or the current tenant. If a workflow must differ per tenant, register it through a provider that does not implement `ITenantAgnosticWorkflowsProvider`.
