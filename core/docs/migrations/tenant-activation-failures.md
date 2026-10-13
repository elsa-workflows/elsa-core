# Tenant activation failures are no longer swallowed

**Breaking change.** Before this change, an exception from a tenant-activated handler (for example a failing EF Core migration startup task) was logged and ignored. The remaining startup work for that tenant was skipped, but the host kept running and reported healthy.

Now:

- The exception is still logged (`Error occurred while processing tenant activated event`) and is then rethrown, so tenant activation fails. During host startup this fails startup.
- A tenant whose activation fails is not kept registered. The next activation or refresh retries it.
- If initial activation fails, tenants that already activated are deactivated again, so the tenant service does not stay half-started.
- Tenant deactivated and deleted handlers are unchanged: failures are logged and the remaining handlers still run.

**What to do:** fix the cause of the failing startup task (the log shows the original exception). If you relied on startup continuing past such a failure, that is no longer supported; for migrations, `Elsa:RunMigrations=false` still lets you apply them out of band.
