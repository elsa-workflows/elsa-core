# Move External Authentication Persistence to Its Own Packages

External Authentication EF Core persistence used to live inside `Elsa.Persistence.EFCore`, sharing `IdentityElsaDbContext` with the Identity module. It now has its own context and its own packages, following the `Elsa.Secrets.Persistence.EFCore*` convention.

This is a **breaking configuration change** for hosts on `3.8.0-preview`. Earlier releases are unaffected, because external-authentication persistence never shipped in one.

## What you have to do

Enabling an Identity persistence feature no longer enables external-authentication persistence. Previously, `SqliteIdentityPersistence` silently replaced all nine external-authentication stores with EF Core implementations. Now you enable it explicitly:

```json
"Features": {
  "SqliteIdentityPersistence": {
    "ConnectionString": "Data Source=elsa.db;Cache=Shared"
  },
  "SqliteExternalAuthenticationPersistence": {
    "ConnectionString": "Data Source=elsa.db;Cache=Shared"
  }
}
```

Reference the matching provider package — `Elsa.ExternalAuthentication.Persistence.EFCore.{Sqlite,SqlServer,PostgreSql,MySql,Oracle}` — so the shell feature is discoverable. For the classic (non-shell) feature model, use `feature.UseEntityFrameworkCore(x => x.UseSqlite(...))` on `ExternalAuthenticationFeature`.

**If you skip this, external authentication silently falls back to the in-memory stores.** There is no error. Single-node hosts keep working until a restart; multi-node hosts break in subtler ways, because sessions, broker transactions, authorization grants, and the connection registry version stop being shared. Audit any host that relied on the old implicit behaviour.

## Schema changes

The tables are unchanged in name, columns, and indexes, and still default to the `Elsa` schema — but they now belong to `ExternalAuthenticationElsaDbContext` with its own migration history. Since no release ever shipped the old migration, apply the new `Initial` migration directly; there is no baseline or history-rewriting step.

Three deliberate differences from the pre-release schema:

- **`FK_ExternalIdentityLinks_Users_UserId` is gone**, along with the index EF generated for it. The two contexts can now target different databases, so a cross-aggregate foreign key is no longer expressible. `ExternalIdentityLinks.UserId` is a plain column covered by `IX_ExternalIdentityLink_TenantId_UserId`. **Consequence:** deleting a user with links is no longer blocked at the database level and leaves the links dangling. (The old constraint surfaced as an unhandled `DbUpdateException` → HTTP 500 from the user-delete endpoint, so it was never a usable guard.) A user-deletion dependency contributor, modelled on the existing `ExternalAuthenticationRoleDeletionDependencyContributor`, is the intended fix.
- **The `ExternalAuthenticationClients` table is dropped.** It had no readers or writers; authentication clients come from `ExternalAuthenticationOptions`.
- **Oracle JSON and protected-payload columns are now `NCLOB`/`BLOB`.** They were previously inferred as `NVARCHAR2(2000)`/`RAW(2000)`, which a real OpenID Connect discovery document or a data-protected broker transaction overflows at runtime. Indexed columns are unchanged, since Oracle cannot index a LOB.

The regenerated migrations also take `IElsaDbContextSchema` and honour a configured schema name. The pre-release migrations hardcoded `schema: "Elsa"`, so a non-default `SchemaName` did not work.

## Just-in-time provisioning

`EFCoreExternalIdentityProvisioner` now resolves users through `IUserProvider` and writes them through `IUserStore`, matching what `InMemoryExternalIdentityProvisioner` already did. Two effects:

- JIT provisioning works with any user directory, not only EF-backed Identity. It previously queried `IdentityElsaDbContext.Users` directly and failed for configuration-defined users.
- User creation and link creation are no longer in one database transaction. The unique `IX_ExternalIdentityLink_Identity` index still guarantees at most one link per `(TenantId, ConnectionKey, Issuer, SubjectHash)`. A writer that loses that race converges on the winning link and deletes the user it had just created; cleanup failures are logged and never fail the sign-in. A process crash between the two writes can strand a credential-less user, which cannot authenticate by any path.

JIT provisioning remains meaningful only with `StoreBasedUserProvider`. With `ConfigurationBasedUserProvider` or `AdminUserProvider`, a created user is written to a store the provider never reads — pre-existing behaviour, unchanged here.

## Scope

This breaks the **runtime/feature** coupling, not the **package** coupling: `Elsa.ExternalAuthentication.Persistence.EFCore.<Provider>` still pulls in `Elsa.Persistence.EFCore` transitively through `Elsa.Persistence.EFCore.<Provider>`. `Elsa.Secrets.Persistence.EFCore*` has the same property. Removing that would require splitting an `Elsa.Persistence.EFCore.<Provider>.Common` out of the provider packages.
