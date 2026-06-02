# Persistence vNext Workflow Instance Metadata Projection

This proof-of-concept keeps workflow execution persistence intact and adds a side-by-side metadata projection for management/catalog queries.

## Intent

Workflow instances have two very different persistence concerns:

- management metadata: ID, definition IDs, status, sub-status, correlation, timestamps, incident count, tenant, and name
- runtime state: the serialized workflow state graph used by execution hot paths

The POC models only the first concern through `Elsa.ModularPersistence`. It registers a provider-neutral storage manifest and writes metadata documents through `IDocumentStore`. Modules or hosts can query summaries, IDs, and counts without owning SQL Server, SQLite, PostgreSQL, or MongoDB migrations.

## Package

`Elsa.Workflows.Management.Persistence.ModularPersistence` contributes:

- `WorkflowInstanceMetadataStorageManifest`, the declared provider-neutral schema
- `WorkflowInstanceMetadataRecord`, a document that intentionally excludes `WorkflowState`
- `IWorkflowInstanceMetadataStore`, a side-by-side query store for summaries, IDs, and counts
- `ModularPersistenceWorkflowInstanceMetadataStore`, an implementation over `IDocumentStore`
- `WorkflowInstanceMetadataBenchmarkEstimator`, a deterministic sizing model for small and large state payload decisions

The package does not replace `IWorkflowInstanceStore` and does not alter the current EF Core persistence path.

## Query Boundary

Only declared-index-backed filters are supported. Exact ID, definition ID, definition version ID, version, parent ID, correlation ID, exact names, statuses, executing/system flags, incident presence, and timestamp ranges are mapped to `DocumentQueryFilter`.

`SearchTerm` and the contains-style `Name` filter are rejected because they require provider-specific text search or scans. A future production version should add explicit text-search capability descriptors instead of hiding a scan behind the portable API.

## Benchmark Interpretation

The estimator is not a substitute for provider benchmarks. It exists to force the workflow-state decision to stay visible:

| Scenario | Instances | Metadata bytes each | State bytes each | Metadata-only total | Full document total |
| --- | ---: | ---: | ---: | ---: | ---: |
| Small state | 1,000 | 1 KB | 8 KB | 1 MB | 9 MB |
| Large state | 1,000 | 1 KB | 512 KB | 1 MB | 513 MB |

Metadata reads should not hydrate the workflow state blob. Any proposal to write workflow state through Persistence vNext needs provider-specific benchmark evidence and a separate decision record.

## Next Steps

1. Add live provider contract tests for SQLite and MongoDB once local container support is available.
2. Add production text-search capability descriptors if management search must move to this projection.
3. Decide whether the projection is maintained synchronously with workflow instance writes, through outbox events, or by a repairable projector.
4. Benchmark write amplification against real workflow state payloads before considering a full state document path.
