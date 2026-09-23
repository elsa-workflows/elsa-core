# PostgreSQL provider for Connections lifecycle state

The pilot PostgreSQL provider is `Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql`. It registers PostgreSQL persistence for the existing `ConnectionsElsaDbContext` and lifecycle/binding stores; it does not add a second connection model or secret store. The project remains nonpackable until the release-unit manifest enables it.

## Provider and migration scope

The provider uses the repository's centralized Npgsql EF Core packages: Npgsql EF provider 9.0.4 with Npgsql 9.0.3 for `net8.0`/`net9.0`, and Npgsql EF provider 10.0.2 with Npgsql 10.0.3 for `net10.0`. The integration suite currently runs against the Testcontainers image `postgres:16-alpine` on `net10.0`; all declared target frameworks are build-checked separately.

Register it through the Connections persistence feature and let Elsa's EF Core migration path discover this provider assembly. The initial migration creates the lifecycle records, logical credential bindings, cleanup tombstones, and offboarding operations in the Elsa schema. The provider currently has one initial PostgreSQL migration. Reapplying migrations against a database with rows is covered; there is no previously released PostgreSQL Connections schema to upgrade, and no SQLite-to-PostgreSQL data migration is provided or claimed. Future migrations must preserve existing lifecycle and recovery state.

```csharp
module.Configure<EFCoreConnectionsPersistenceFeature>(feature =>
    feature.UsePostgreSql(connectionString));
```

The migration identity is provider-specific; keep the Connections provider configured when running migrations. `EnsureCreated` does not validate migration discovery and is not a substitute for the migration path. The initial migration's `Down` deliberately throws: dropping these tables can discard unresolved provider outcomes and cleanup history. Roll back by restoring the full predeployment database backups and matching key/application version, as described in [credential lifecycle operations](credential-lifecycle-operations.md).

## Database roles and data boundaries

Use a dedicated schema owner or deployment identity to create the schema and apply migrations. The runtime identity needs schema usage and table-level `SELECT`, `INSERT`, `UPDATE`, and `DELETE` for the Connections lifecycle tables. The pilot model uses application-generated string identifiers and does not require table sequences. Scope privileges to the Elsa schema and these tables; do not grant database-owner privileges to the runtime identity merely to run application requests.

These tables contain connection metadata, generation identifiers, workflow binding references, leases/fences, and safe outcome codes. OAuth access and refresh tokens are stored as encrypted managed Secrets payloads in the configured Secrets persistence database. Configure the existing Secrets encryption key outside both databases, keep it stable and backed up separately, and do not put key material in a PostgreSQL connection string, logs, migration artifacts, or this repository.

Back up the Connections and Secrets databases together with the matching encryption key and application version. For rollback, restore the complete predeployment backups and matching key as described in [credential lifecycle operations](credential-lifecycle-operations.md). Restoring only Connections can leave encrypted generations or workflow bindings inconsistent; restoring only Secrets can leave lifecycle pointers and durable operations inconsistent.

## Verified scope and limitations

- The integration tests use a disposable PostgreSQL 16 container and the actual Connections feature registration/migration assembly.
- Tests cover initial migration and populated-database reapplication, exact tenant/environment store scoping, lifecycle revision/fence CAS, disconnect, offboarding queue/claim/completion, cleanup tombstones, and a two-worker unique-binding insert race. The classifier accepts only PostgreSQL SQLSTATE `23505` as a duplicate binding conflict.
- Builds cover `net8.0`, `net9.0`, and `net10.0`; the new container-backed integration suite runs on `net10.0` only.
- This pilot has no preexisting PostgreSQL upgrade path, cross-provider import, automatic SQLite-to-PostgreSQL conversion, or separate-process restart proof. The separate worker conformance task owns that last proof.
- Managed credentials remain unusable if the encryption key is missing or mismatched. Existing credential-lifecycle tests cover those key failure cases; this provider integration suite does not exercise a production key-management service.
- No production deployment, hosted PostgreSQL service, failover, TLS policy, or production privilege grant has been tested.
