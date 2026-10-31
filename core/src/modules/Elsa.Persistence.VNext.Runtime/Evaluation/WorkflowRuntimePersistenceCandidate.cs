namespace Elsa.Persistence.VNext.Runtime.Evaluation;

public record WorkflowRuntimePersistenceCandidate(
    string Name,
    WorkflowRuntimePersistenceArea Area,
    WorkflowRuntimePersistenceWorkload Workload,
    bool RequiresDistributedLocking,
    bool RequiresOrderedAppend,
    bool HasRepresentativeBenchmark,
    bool HasProviderContractTests);

public enum WorkflowRuntimePersistenceArea
{
    Management,
    Bookmarks,
    Triggers,
    ExecutionLogs,
    ActivityExecutions,
    Outbox
}

public enum WorkflowRuntimePersistenceWorkload
{
    Metadata,
    InteractiveQuery,
    HotLookup,
    Queue,
    AppendOnlyLog
}
