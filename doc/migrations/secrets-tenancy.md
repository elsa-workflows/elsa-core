# Secrets become tenant-scoped

`Secret` had no notion of tenancy. It did not derive from `Elsa.Common.Entities.Entity`, so it carried no
`TenantId` and no query filter applied to it — in a multi-tenant deployment every tenant could see and resolve
every other tenant's secrets. Permissions did not help: `secrets:view` is evaluated against the caller, not
against which tenant owns the secret, so any caller holding it reached the whole set.

`Secret` now derives from `Entity` and is filtered like every other user-facing entity.

## What you have to do

Apply the `SecretTenancy` and `SecretDefaultTenantUniqueness` migrations for your provider. The second
migration stamps leftover null `TenantId` values to `""` (the default tenant) and rebuilds the unique
index. It does **not** delete duplicate names — if two leftover rows share a name in the same tenant,
the upgrade aborts and lists the keys. Reconcile those rows, then re-run.

## Existing secrets

The migration adds `TenantId` **nullable and does not backfill it**, so rows written before the upgrade keep a
null tenant. That is deliberate rather than an omission: `SetTenantIdFilter` already treats a null `TenantId`
as belonging to the default tenant, through a clause written for exactly this case.

```
TenantId == context.TenantId || TenantId == "*" || (TenantId == null && context.TenantId == "")
```

What that means for you:

| Deployment | Existing secrets after upgrade |
| --- | --- |
| Single-tenant | Visible and unchanged. The filter is only installed when multitenancy is enabled, so nothing applies at all. |
| Multi-tenant | **Not visible** to any named tenant. Assign each secret to its owning tenant, or set `TenantId` to `*` to share it across all of them. |

The multi-tenant case is a deliberate, visible failure. The alternative — leaving every pre-existing secret
readable from every tenant — is the exposure this change exists to close.

## Shared platform secrets

Set `TenantId` to `*` (`Tenant.AgnosticTenantId`, per ADR 0009) for a secret every tenant should resolve, such
as a platform-wide SMTP credential. Agnostic secrets are visible from every tenant context.

When multitenancy is enabled, new EF-backed secrets can only be inserted for the ambient tenant. A named
tenant cannot create an agnostic secret or assign a new secret to another tenant by supplying `TenantId` in
the entity. Create shared secrets from an authorized platform context instead.

## Secret names are now unique per tenant

The unique index moves from `NormalizedName` to `(TenantId, NormalizedName)`, matching what `User`, `Role` and
`Application` did in the same release. Two tenants may now each hold a secret called `smtp-password`; before,
the first tenant to claim a name took it globally.

Downgrading recreates the global unique index and **will fail if two tenants hold the same secret name by
then**. Reconcile the duplicates first.

### Default-tenant rows use `""`

The default tenant id is `""` (`Tenant.DefaultTenantId`), the same sentinel Labels uses. New EF writes that
would have left `TenantId` null now persist `""`, so they participate in `(TenantId, NormalizedName)`.

`SecretDefaultTenantUniqueness` stamps leftover nulls from `SecretTenancy` to `""` and rebuilds that unique
index. SQL Server still creates the composite index with a `TenantId IS NOT NULL` filter; after the stamp
those rows are non-null, so they collide in it. SQLite, PostgreSQL and MySQL treat nulls as distinct — the
stamp is what brings them into the index.

Oracle is different: it stores `''` as `NULL`, so the stamp is a no-op and a `TenantId IS NOT NULL` filter
would leave every default-tenant row outside the index. That provider creates a function-based unique index
on `NVL(TenantId, CHR(1))` plus `NormalizedName` instead. `CHR(1)` is not a tenant id; it only exists so
NULL default-tenant names share one index key. The EF model snapshot cannot express `NVL`, so it looks like
the other unfiltered unique indexes; the physical Oracle index is the function-based one from this migration.

The migration does not delete duplicates: it lists leftover `(TenantId, NormalizedName)` keys and aborts,
then the unique index fails loudly if any remain.

`SetTenantIdFilter`'s null-compatibility clause still treats a stray null as the default tenant, so a row
that somehow remains unstamped stays visible there. File and InMemory already treat null and `""` as the
same tenant for uniqueness.

## The MySQL provider ships for net8.0 and net9.0 only

`Elsa.Secrets.Persistence.EFCore.MySql` targets `net8.0;net9.0`, while the Sqlite, SQL Server, PostgreSQL and
Oracle secrets providers also target `net10.0`. That is a dependency constraint, not an oversight:
`Pomelo.EntityFrameworkCore.MySql` tops out at 9.0.0, built for EF Core 9, so there is no net10.0 provider to
build against. Every MySQL project in the repository carries the same pin, and the secrets one additionally
inherits it by referencing `Elsa.Persistence.EFCore.MySql`.

A net10.0 host referencing the MySQL secrets provider resolves the net9.0 asset and runs normally, including
the `SecretTenancy` migration — migrations are ordinary C# and do not depend on the host framework. The pins
come out together once Pomelo ships for EF Core 10.

## The VNext persistence provider does not support this

`Elsa.Secrets.Persistence.VNext` stores documents keyed by secret name alone, and `Elsa.Persistence.VNext` has
no tenant concept to filter on. Rather than silently serve one tenant's secret to another, it now throws when
used outside the default tenant context. If you run multitenancy, use an Entity Framework Core secrets
provider. Single-tenant deployments are unaffected.

Making it tenant-aware means changing the document id scheme, which relocates existing documents — a storage
change to make deliberately rather than fold into this one.

## Configuration-backed secrets

`ConfigurationSecretStore` reads values from application configuration and stores only a key. The value stays
deployment-level and is not partitioned, but the `Secret` record describing it is an ordinary row and is
tenant-scoped like any other. Two tenants may each hold a record pointing at the same configuration key.
