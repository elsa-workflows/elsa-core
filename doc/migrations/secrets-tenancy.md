# Secrets become tenant-scoped

`Secret` had no notion of tenancy. It did not derive from `Elsa.Common.Entities.Entity`, so it carried no
`TenantId` and no query filter applied to it — in a multi-tenant deployment every tenant could see and resolve
every other tenant's secrets. Permissions did not help: `secrets:view` is evaluated against the caller, not
against which tenant owns the secret, so any caller holding it reached the whole set.

`Secret` now derives from `Entity` and is filtered like every other user-facing entity.

## What you have to do

Apply the `SecretTenancy` migration for your provider. There is no data step, and nothing else is required for
a single-tenant deployment.

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

## Secret names are now unique per tenant

The unique index moves from `NormalizedName` to `(TenantId, NormalizedName)`, matching what `User`, `Role` and
`Application` did in the same release. Two tenants may now each hold a secret called `smtp-password`; before,
the first tenant to claim a name took it globally.

Downgrading recreates the global unique index and **will fail if two tenants hold the same secret name by
then**. Reconcile the duplicates first.

### Null-tenant rows sit outside the index

"Unique per tenant" is enforced by the database only for rows whose `TenantId` is non-null. SQL Server
creates the composite index with a `[TenantId] IS NOT NULL` filter, and SQLite, PostgreSQL and MySQL treat
nulls as distinct in unique indexes — either way, null-tenant rows never collide in it. Only Oracle, where
equal nulls do count as duplicates in a composite unique index, still rejects them.

This matters more than it sounds, because null is the common case. With multitenancy **disabled** — the
default single-tenant deployment — nothing ever assigns a `TenantId`, so every row keeps null and the schema
no longer enforces secret-name uniqueness at all. Uniqueness then rests on the repository's read-before-write
check, which blocks sequential duplicates but not two concurrent creates racing past it. The old global index
was the backstop for exactly that race; accepting its loss for null rows is a consequence of the no-backfill
decision above. The same gap applies in a multi-tenant deployment's default tenant: pre-upgrade null rows and
new `""`-tenant rows are distinct index keys, so the index cannot stop a new default-tenant secret from
colliding by name with a legacy row.

If you want the database guarantee back in single-tenant mode, backfill `TenantId` to `""` yourself — the
`SetTenantIdFilter` null-compatibility clause keeps any stragglers visible — but Elsa does not do this for
you.

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
