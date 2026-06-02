# Persistence vNext Examples And Compatibility

Issue: #7622

## Status

Persistence vNext is implemented in this branch as Elsa-internal packages. The examples below use the current Elsa package names and extension methods. The extraction boundary and future external package names are defined in [Persistence vNext Extraction Boundaries](persistence-vnext-extraction-boundaries.md).

## ASP.NET Core Modular Monolith Example

This example configures an Elsa-backed ASP.NET Core modular monolith that uses modular persistence with SQLite and registers an application-owned module manifest.

```csharp
using Elsa.Extensions;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Sqlite.Extensions;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ModularPersistence")
    ?? "Data Source=App_Data/modular-persistence.db";

builder.Services.AddElsa(elsa =>
{
    elsa.UseModularPersistence(persistence =>
    {
        persistence.UseSqliteProvider(connectionString);
        persistence.RegisterManifest(CustomerManifest.Create());
        persistence.ConfigureOptions = options =>
        {
            options.MaterializationRetryCount = 3;
            options.MaterializationRetryDelay = TimeSpan.FromSeconds(2);
        };
    });
});

var app = builder.Build();
app.MapGet("/", () => "Elsa modular persistence host");
app.Run();

static class CustomerManifest
{
    public static StorageManifestDescriptor Create() =>
        new(
            "sample.customers",
            new StorageManifestVersion(1),
            [
                new StorageUnitDescriptor(
                    "Customers",
                    [
                        new StorageFieldDescriptor("Id", StorageFieldType.String, true),
                        new StorageFieldDescriptor("Email", StorageFieldType.String, true),
                        new StorageFieldDescriptor("DisplayName", StorageFieldType.String),
                        new StorageFieldDescriptor("CreatedAt", StorageFieldType.DateTimeOffset, true)
                    ],
                    [
                        new StorageKeyDescriptor("PK_Customers", ["Id"])
                    ],
                    [
                        new StorageIndexDescriptor(
                            "UX_Customers_Email",
                            [new StorageIndexFieldDescriptor("Email")],
                            true)
                    ])
            ]);
}
```

The corresponding connection string can be configured as:

```json
{
  "ConnectionStrings": {
    "ModularPersistence": "Data Source=App_Data/modular-persistence.db"
  }
}
```

## Provider Configuration Examples

The provider snippets assume the matching provider extension namespace is imported, for example `Elsa.ModularPersistence.SqlServer.Extensions`.

SQLite is the local development and single-node sample provider:

```csharp
elsa.UseModularPersistence(persistence =>
{
    persistence.UseSqliteProvider("Data Source=App_Data/modular-persistence.db");
    persistence.RegisterManifest(CustomerManifest.Create());
});
```

SQL Server uses the same portable document/index schema and can opt in to provider-optimized declared indexes:

```csharp
elsa.UseModularPersistence(persistence =>
{
    persistence.UseSqlServerProvider(
        builder.Configuration.GetConnectionString("ModularPersistenceSqlServer")!,
        options => options.UseOptimizedIndexes = true);
    persistence.RegisterManifest(CustomerManifest.Create());
});
```

PostgreSQL uses the relational contract and can opt in to JSONB expression indexes:

```csharp
elsa.UseModularPersistence(persistence =>
{
    persistence.UsePostgreSqlProvider(
        builder.Configuration.GetConnectionString("ModularPersistencePostgreSql")!,
        options => options.UseOptimizedJsonbIndexes = true);
    persistence.RegisterManifest(CustomerManifest.Create());
});
```

MongoDB uses native MongoDB collections and indexes instead of the relational document/index tables:

```csharp
using Elsa.ModularPersistence.MongoDb.Options;

elsa.UseModularPersistence(persistence =>
{
    persistence.UseMongoDbProvider(
        "mongodb://localhost:27017",
        "ElsaModularPersistence",
        options => options.CollectionStrategy = MongoDbCollectionStrategy.SharedCollection);
    persistence.RegisterManifest(CustomerManifest.Create());
});
```

## Runtime Entity Example

Runtime entity definitions are draft/published schemas backed by the modular document store. A host can create a draft definition through the admin API, publish it, then use the runtime entity endpoints for data.

Draft definition payload:

```json
{
  "id": "customers",
  "schemaName": "runtime.customers",
  "storageUnitName": "Customers",
  "fields": [
    { "name": "Id", "type": "String", "isRequired": true },
    { "name": "Email", "type": "String", "isRequired": true },
    { "name": "DisplayName", "type": "String" }
  ],
  "indexes": [
    { "name": "IX_Customers_Email", "fieldNames": [ "Email" ], "isUnique": true }
  ],
  "requiredPermissions": [
    "read:customers",
    "write:customers"
  ]
}
```

Entity query payload scoped to a tenant:

```json
{
  "tenantId": "tenant-a",
  "filters": [
    {
      "fieldName": "Email",
      "operator": "Equals",
      "values": [
        { "type": "String", "textValue": "alice@example.com" }
      ]
    }
  ],
  "limit": 25,
  "offset": 0
}
```

## Provider Compatibility Matrix

| Capability | SQLite | SQL Server | PostgreSQL | MongoDB |
| --- | --- | --- | --- | --- |
| Portable document envelope | Supported | Supported | Supported | Supported |
| Declared index materialization | Generic index table | Generic index table | Generic index table | Native MongoDB indexes |
| Optimistic concurrency | Supported | Supported | Supported | Supported |
| Tenant-aware document keys | Supported | Supported | Supported | Supported |
| Tenant-scoped queries | Supported | Supported through relational query contract | Supported through relational query contract | Supported |
| Startup transactions | Supported | Supported | Supported | Single-document writes by default; transaction-per-write optional |
| Startup locking | File/database serialization through SQLite transaction | `sp_getapplock` transaction lock | advisory transaction lock | MongoDB index creation idempotency |
| Schema history table | Supported | Supported | Supported | Not applicable |
| Optimized index option | Not currently exposed | `UseOptimizedIndexes` | `UseOptimizedJsonbIndexes` | Native indexes by default |
| Native physicalized entities | Not supported | Not supported | Not supported | Collection strategy supports shared or per-type collections |
| External live contract tests | Local by default | Environment-gated | Environment-gated | Environment-gated |

Environment variables for live provider tests:

- `ELSA_MODULAR_PERSISTENCE_SQLSERVER_CONNECTION_STRING`
- `ELSA_MODULAR_PERSISTENCE_POSTGRESQL_CONNECTION_STRING`
- `ELSA_MODULAR_PERSISTENCE_MONGODB_CONNECTION_STRING`

## Documentation Map

- Manifests, providers, document store, query model, diagnostics, physicalization, and benchmarks: [Persistence](persistence.md)
- Runtime workflow benchmark baseline: [Persistence vNext Workflow Runtime Benchmarks](persistence-vnext-workflow-runtime-benchmarks.md)
- Runtime migration decision: [Persistence vNext Workflow Runtime Decision](persistence-vnext-workflow-runtime-decision.md)
- Operations, repair, security, backup/restore: [Persistence vNext Operations](persistence-vnext-operations.md)
- Extraction boundaries and API risks: [Persistence vNext Extraction Boundaries](persistence-vnext-extraction-boundaries.md)

## Semantic Versioning Policy

Before external extraction, Persistence vNext APIs are internal-to-Elsa and may change across prerelease builds.

After extraction:

- `0.x` releases may make breaking changes, but breaking changes must be documented in release notes.
- `1.0` starts when descriptors, document sessions, query model, provider capability model, and provider option names are stable.
- Patch releases must be source and binary compatible, except for security fixes that require explicit mitigation notes.
- Minor releases may add descriptor fields, query capabilities, providers, or physicalization options without changing existing behavior.
- Major releases are required for constructor signature changes, enum meaning changes, query semantics changes, provider option renames, or breaking persistence format changes.
- Provider-specific behavior must declare compatibility in the provider compatibility matrix.

Persistent data format changes require:

1. A manifest/schema version note.
2. Roll-forward migration guidance.
3. Backup/restore guidance.
4. Provider compatibility notes.
5. Tests for repeated materialization and mixed-version startup where feasible.
