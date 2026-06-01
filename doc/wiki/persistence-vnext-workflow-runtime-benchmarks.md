# Persistence vNext Workflow Runtime Benchmarks

Issue: #7617

These benchmarks evaluate workflow runtime persistence shapes against the Persistence vNext runtime entity document path. They are not EF Core provider benchmarks and they do not claim production provider throughput. The goal is to make the workflow runtime decision in #7618 evidence-backed by measuring candidate document/index shapes before moving any hot runtime store.

## Scope

Covered scenarios:

- Workflow instance query by status and executing flag.
- Workflow instance optimistic update.
- Bookmark lookup by stimulus hash and workflow instance ID.
- Trigger lookup by stimulus hash.
- Synthetic lock acquire and release using an optimistic document create/delete lease shape.
- Workflow execution log append volume.
- Activity execution log query by workflow instance ID.

The benchmark intentionally separates management/catalog-style workflow instance access from runtime execution hot paths such as bookmark/trigger lookup and high-volume log queries.

## Command

BenchmarkDotNet requires `dotnet` on `PATH` when it builds the generated benchmark harness. In this local environment, `dotnet` lives under `/usr/local/share/dotnet`, so the verified command was:

```sh
PATH=/usr/local/share/dotnet:$PATH /usr/local/share/dotnet/dotnet run -c Release --project test/performance/Elsa.Workflows.PerformanceTests/Elsa.Workflows.PerformanceTests.csproj -- --filter '*WorkflowRuntimePersistenceCandidateBenchmark*'
```

Project build validation:

```sh
/usr/local/share/dotnet/dotnet build test/performance/Elsa.Workflows.PerformanceTests/Elsa.Workflows.PerformanceTests.csproj --no-restore
```

## Baseline Result

Environment:

- BenchmarkDotNet v0.15.8
- macOS Tahoe 26.5, Apple M2
- .NET SDK 10.0.300
- Runtime .NET 10.0.8, Arm64 RyuJIT
- Job: `ShortRun`, 3 warmups, 3 measured iterations, 1 launch

| Method | Mean | StdDev | Allocated |
| --- | ---: | ---: | ---: |
| QueryRunningWorkflowInstances | 458.525 us | 19.6977 us | 96.17 KB |
| UpdateWorkflowInstanceOptimistically | 28.377 us | 5.2045 us | 15.48 KB |
| FindBookmarksByStimulusAndInstance | 7,679.100 us | 752.6207 us | 1188.57 KB |
| FindTriggersByStimulusHash | 7,625.610 us | 1,602.2331 us | 1184.86 KB |
| AcquireAndReleaseRuntimeLock | 6.566 us | 0.8335 us | 9.81 KB |
| AppendWorkflowExecutionLogBatch | 5.415 us | 0.2544 us | 6.64 KB |
| QueryActivityLogsByWorkflowInstance | 9,968.678 us | 4,922.0500 us | 1180.11 KB |

## Interpretation

Workflow instance status/executing queries are plausible candidates for document-first persistence when they stay index-backed and avoid full workflow-state blob reads. Optimistic instance update is also cheap in the synthetic runtime entity path, but real workflow instance updates still need workflow-state payload size and compression benchmarks before migration.

Bookmark and trigger lookups are hot runtime paths. In this synthetic in-memory document path, they are much slower and allocate roughly 1.1 MB per lookup because the benchmark helper parses document JSON while scanning. That reinforces the current rule: do not migrate bookmark or trigger stores to vNext until provider-backed declared indexes, optimized indexes, or specialized provider paths prove equivalent hot-path behavior.

Log append is cheap per record in the synthetic path, but high-volume runtime logs need real provider batch insert and query benchmarks. Activity-log queries are slow in this harness for the same reason as bookmark and trigger lookups, so log/journal querying should remain specialized until provider-backed indexed paging is measured.

The lock benchmark only models optimistic create/delete lease mechanics. It does not replace the existing distributed lock providers. Runtime lock decisions still need contention and lease-expiry semantics, especially for per-instance locks, resume-filter locks, trigger-index locks, and bookmark queue worker coordination.

## Decision Input For #7618

- Management/catalog/metadata reads are reasonable vNext candidates when backed by declared indexes.
- Workflow instance state writes need additional large-state payload and compression measurements before approval.
- Bookmark, trigger, queue, activity log, and workflow log hot paths should remain specialized unless provider-specific physicalization proves competitive.
- Runtime locks should remain explicit distributed-lock provider concerns, not implicit document-store behavior, unless a dedicated lease model is designed and benchmarked.
