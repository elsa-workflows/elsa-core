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

The persistence objects still default to the `Elsa` schema, but now belong to `ExternalAuthenticationElsaDbContext` with its own migration history. Since no release ever shipped the old migration, apply the new `Initial` migration directly; there is no baseline or history-rewriting step.

Deliberate differences from the pre-release schema:

- **`FK_ExternalIdentityLinks_Users_UserId` is gone**, along with the index EF generated for it. The two contexts can now target different databases, so a cross-aggregate foreign key is no longer expressible. `ExternalIdentityLinks.UserId` is a plain column covered by `IX_ExternalIdentityLink_TenantId_UserId`. User deletion is instead coordinated through `IUserDeletionDependencyContributor`; External Authentication blocks deletion while links remain and returns a conflict instead of relying on an unhandled database exception.
- **The `ExternalAuthenticationClients` table is dropped.** It had no readers or writers; authentication clients come from `ExternalAuthenticationOptions`.
- **An unissued refresh token is represented by no row.** `ExternalAuthenticationSessions.CurrentRefreshTokenHash` is replaced by the optional one-to-one `ExternalAuthenticationSessionRefreshTokens` table. Its non-null `Hash` remains uniquely indexed, so callback completion no longer has to persist a synthetic `unissued:*` value before the first token is minted.
- **Oracle JSON and protected-payload columns are now `NCLOB`/`BLOB`.** They were previously inferred as `NVARCHAR2(2000)`/`RAW(2000)`, which a real OpenID Connect discovery document or a data-protected broker transaction overflows at runtime. Indexed columns are unchanged, since Oracle cannot index a LOB.

The regenerated migrations also take `IElsaDbContextSchema` and honour a configured schema name. The pre-release migrations hardcoded `schema: "Elsa"`, so a non-default `SchemaName` did not work.

## Just-in-time provisioning

`EFCoreExternalIdentityProvisioner` now resolves users through `IUserProvider` and writes them through `IUserStore`, matching what `InMemoryExternalIdentityProvisioner` already did. Two effects:

- JIT provisioning works with any user directory, not only EF-backed Identity. It previously queried `IdentityElsaDbContext.Users` directly and failed for configuration-defined users.
- User creation and link creation are no longer represented as one database transaction. Provider-independent user resolution, role validation, generated-name collision handling, and compensation are shared by every persistence implementation. The unique `IX_ExternalIdentityLink_Identity` index guarantees at most one link per `(TenantId, ConnectionKey, Issuer, SubjectHash)`. A writer that loses the race or observes a failed link write removes the credential-less user it created; an observed cleanup failure fails the operation and issues no credentials. User deletion and link publication perform complementary post-write checks so either concurrent ordering removes the link or restores the User instead of leaving a dangling reference. Abrupt process termination between stores can leave a credential-less user, but never a usable authentication path or a second identity link.

## API compatibility

The unused `GET /external-authentication/descriptors/runtime` endpoint and its generated-client `ExternalAuthenticationRuntimeDescriptor` contract were removed. The endpoint duplicated deployment configuration, had no runtime consumer, and had not shipped in a stable release. Clients should use the specific adapter, policy, grant-source, matcher, and permission descriptor endpoints.

JIT provisioning remains meaningful only with `StoreBasedUserProvider`. With `ConfigurationBasedUserProvider` or `AdminUserProvider`, a created user is written to a store the provider never reads — pre-existing behaviour, unchanged here.

## Scope

This breaks the **runtime/feature** coupling, not the **package** coupling: `Elsa.ExternalAuthentication.Persistence.EFCore.<Provider>` still pulls in `Elsa.Persistence.EFCore` transitively through `Elsa.Persistence.EFCore.<Provider>`. `Elsa.Secrets.Persistence.EFCore*` has the same property. Removing that would require splitting an `Elsa.Persistence.EFCore.<Provider>.Common` out of the provider packages.
