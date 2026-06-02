using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Queries;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.Workflows.PerformanceTests;

[Config(typeof(Config))]
[ShortRunJob]
public class WorkflowRuntimePersistenceCandidateBenchmark
{
    private const string WorkflowInstances = "workflow-instances";
    private const string Bookmarks = "bookmarks";
    private const string Triggers = "triggers";
    private const string WorkflowExecutionLogs = "workflow-execution-logs";
    private const string ActivityExecutionLogs = "activity-execution-logs";
    private const string RuntimeLocks = "runtime-locks";

    private readonly RuntimeStorageOperationContext _context = RuntimeStorageOperationContext.System;
    private RuntimeEntityDataService _service = null!;
    private int _counter;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var definitions = new InMemoryRuntimeStorageDefinitionStore();
        foreach (var definition in CreateDefinitions())
            await definitions.SaveAsync(definition);

        _service = new RuntimeEntityDataService(definitions, new InMemoryRuntimeEntityDocumentStoreFactoryRegistry());
        await SeedWorkflowInstancesAsync();
        await SeedBookmarksAsync();
        await SeedTriggersAsync();
        await SeedExecutionLogsAsync();
    }

    [Benchmark]
    public async ValueTask<IReadOnlyCollection<RuntimeEntityRecord>> QueryRunningWorkflowInstances() =>
        await QueryAsync(
            WorkflowInstances,
            new("Status", DocumentQueryFilterOperator.Equals, [DocumentQueryValue.String("Running")]),
            new("IsExecuting", DocumentQueryFilterOperator.Equals, [DocumentQueryValue.Boolean(true)]));

    [Benchmark]
    public async ValueTask<RuntimeEntityRecord> UpdateWorkflowInstanceOptimistically()
    {
        var id = $"workflow-update-{Interlocked.Increment(ref _counter)}";
        var created = await _service.CreateAsync(WorkflowInstances, Save(id, WorkflowInstancePayload(id, "OrderFlow", "Running", "Executing", $"corr-{id}", true)), _context);
        return await _service.UpdateAsync(WorkflowInstances, Save(id, WorkflowInstancePayload(id, "OrderFlow", "Finished", "Finished", $"corr-{id}", false), created.Version), _context);
    }

    [Benchmark]
    public async ValueTask<IReadOnlyCollection<RuntimeEntityRecord>> FindBookmarksByStimulusAndInstance() =>
        await QueryAsync(
            Bookmarks,
            new("StimulusHash", DocumentQueryFilterOperator.Equals, [DocumentQueryValue.String("stimulus-42")]),
            new("WorkflowInstanceId", DocumentQueryFilterOperator.Equals, [DocumentQueryValue.String("workflow-42")]));

    [Benchmark]
    public async ValueTask<IReadOnlyCollection<RuntimeEntityRecord>> FindTriggersByStimulusHash() =>
        await QueryAsync(Triggers, new RuntimeEntityQueryFilter("StimulusHash", DocumentQueryFilterOperator.Equals, [DocumentQueryValue.String("stimulus-42")]));

    [Benchmark]
    public async ValueTask AcquireAndReleaseRuntimeLock()
    {
        var id = $"lock-{Interlocked.Increment(ref _counter)}";
        var record = await _service.CreateAsync(RuntimeLocks, Save(id, LockPayload(id, "workflow-definition:OrderFlow")), _context);
        await _service.DeleteAsync(RuntimeLocks, record.Id, null, _context, expectedVersion: record.Version);
    }

    [Benchmark(OperationsPerInvoke = 50)]
    public async ValueTask AppendWorkflowExecutionLogBatch()
    {
        var batch = Interlocked.Increment(ref _counter);
        for (var i = 0; i < 50; i++)
        {
            var id = $"workflow-log-{batch}-{i}";
            await _service.CreateAsync(WorkflowExecutionLogs, Save(id, WorkflowLogPayload(id, $"workflow-{i % 100}", "ActivityExecuted", i)), _context);
        }
    }

    [Benchmark]
    public async ValueTask<IReadOnlyCollection<RuntimeEntityRecord>> QueryActivityLogsByWorkflowInstance() =>
        await QueryAsync(ActivityExecutionLogs, new RuntimeEntityQueryFilter("WorkflowInstanceId", DocumentQueryFilterOperator.Equals, [DocumentQueryValue.String("workflow-42")]));

    private async Task SeedWorkflowInstancesAsync()
    {
        for (var i = 0; i < 2500; i++)
        {
            var status = i % 3 == 0 ? "Running" : "Finished";
            var subStatus = status == "Running" ? "Executing" : "Finished";
            await _service.CreateAsync(
                WorkflowInstances,
                Save($"workflow-{i}", WorkflowInstancePayload($"workflow-{i}", $"Definition-{i % 20}", status, subStatus, $"corr-{i % 100}", status == "Running")),
                _context);
        }
    }

    private async Task SeedBookmarksAsync()
    {
        for (var i = 0; i < 5000; i++)
        {
            await _service.CreateAsync(
                Bookmarks,
                Save($"bookmark-{i}", BookmarkPayload($"bookmark-{i}", $"workflow-{i % 500}", $"stimulus-{i % 100}", $"Activity-{i % 25}")),
                _context);
        }
    }

    private async Task SeedTriggersAsync()
    {
        for (var i = 0; i < 5000; i++)
        {
            await _service.CreateAsync(
                Triggers,
                Save($"trigger-{i}", TriggerPayload($"trigger-{i}", $"Definition-{i % 20}", $"Version-{i % 200}", $"stimulus-{i % 100}")),
                _context);
        }
    }

    private async Task SeedExecutionLogsAsync()
    {
        for (var i = 0; i < 5000; i++)
        {
            await _service.CreateAsync(
                ActivityExecutionLogs,
                Save($"activity-log-{i}", ActivityLogPayload($"activity-log-{i}", $"workflow-{i % 500}", $"Activity-{i % 25}", i)),
                _context);
        }
    }

    private ValueTask<IReadOnlyCollection<RuntimeEntityRecord>> QueryAsync(string definitionId, params RuntimeEntityQueryFilter[] filters) =>
        _service.QueryAsync(definitionId, new RuntimeEntityQueryRequest(filters, limit: 100), _context);

    private static RuntimeEntitySaveRequest Save(string id, string payload, long? expectedVersion = null) => new(id, payload, ExpectedVersion: expectedVersion);

    private static RuntimeStorageDefinition[] CreateDefinitions() =>
    [
        Definition(
            WorkflowInstances,
            "WorkflowInstances",
            [
                Field("Id", StorageFieldType.String, required: true),
                Field("DefinitionId", StorageFieldType.String, required: true),
                Field("Status", StorageFieldType.String, required: true),
                Field("SubStatus", StorageFieldType.String, required: true),
                Field("CorrelationId", StorageFieldType.String),
                Field("IsExecuting", StorageFieldType.Boolean),
            ],
            [
                Index("IX_WorkflowInstances_Status", ["Status"]),
                Index("IX_WorkflowInstances_IsExecuting", ["IsExecuting"]),
                Index("IX_WorkflowInstances_CorrelationId", ["CorrelationId"])
            ]),
        Definition(
            Bookmarks,
            "Bookmarks",
            [
                Field("Id", StorageFieldType.String, required: true),
                Field("WorkflowInstanceId", StorageFieldType.String, required: true),
                Field("StimulusHash", StorageFieldType.String, required: true),
                Field("ActivityTypeName", StorageFieldType.String)
            ],
            [
                Index("IX_Bookmarks_WorkflowInstanceId", ["WorkflowInstanceId"]),
                Index("IX_Bookmarks_StimulusHash", ["StimulusHash"]),
                Index("IX_Bookmarks_ActivityTypeName", ["ActivityTypeName"])
            ]),
        Definition(
            Triggers,
            "Triggers",
            [
                Field("Id", StorageFieldType.String, required: true),
                Field("DefinitionId", StorageFieldType.String, required: true),
                Field("DefinitionVersionId", StorageFieldType.String, required: true),
                Field("StimulusHash", StorageFieldType.String, required: true)
            ],
            [
                Index("IX_Triggers_DefinitionId", ["DefinitionId"]),
                Index("IX_Triggers_DefinitionVersionId", ["DefinitionVersionId"]),
                Index("IX_Triggers_StimulusHash", ["StimulusHash"])
            ]),
        Definition(
            WorkflowExecutionLogs,
            "WorkflowExecutionLogs",
            [
                Field("Id", StorageFieldType.String, required: true),
                Field("WorkflowInstanceId", StorageFieldType.String, required: true),
                Field("EventName", StorageFieldType.String, required: true),
                Field("Sequence", StorageFieldType.Int32)
            ],
            [
                Index("IX_WorkflowExecutionLogs_WorkflowInstanceId", ["WorkflowInstanceId"]),
                Index("IX_WorkflowExecutionLogs_EventName", ["EventName"])
            ]),
        Definition(
            ActivityExecutionLogs,
            "ActivityExecutionLogs",
            [
                Field("Id", StorageFieldType.String, required: true),
                Field("WorkflowInstanceId", StorageFieldType.String, required: true),
                Field("ActivityTypeName", StorageFieldType.String, required: true),
                Field("Sequence", StorageFieldType.Int32)
            ],
            [
                Index("IX_ActivityExecutionLogs_WorkflowInstanceId", ["WorkflowInstanceId"]),
                Index("IX_ActivityExecutionLogs_ActivityTypeName", ["ActivityTypeName"])
            ]),
        Definition(
            RuntimeLocks,
            "RuntimeLocks",
            [
                Field("Id", StorageFieldType.String, required: true),
                Field("Resource", StorageFieldType.String, required: true),
                Field("Owner", StorageFieldType.String, required: true)
            ],
            [
                Index("IX_RuntimeLocks_Resource", ["Resource"], unique: true)
            ])
    ];

    private static RuntimeStorageDefinition Definition(
        string id,
        string storageUnitName,
        IEnumerable<RuntimeStorageFieldDefinition> fields,
        IEnumerable<RuntimeStorageIndexDefinition> indexes) =>
        new(id, $"runtime.{id}", storageUnitName, fields, indexes, ["benchmark:runtime"])
        {
            State = RuntimeStorageDefinitionState.Published
        };

    private static RuntimeStorageFieldDefinition Field(string name, StorageFieldType type, bool required = false) => new(name, type, required);

    private static RuntimeStorageIndexDefinition Index(string name, IEnumerable<string> fields, bool unique = false) => new(name, fields, unique);

    private static string WorkflowInstancePayload(string id, string definitionId, string status, string subStatus, string correlationId, bool isExecuting) =>
        $$"""{"Id":"{{id}}","DefinitionId":"{{definitionId}}","Status":"{{status}}","SubStatus":"{{subStatus}}","CorrelationId":"{{correlationId}}","IsExecuting":{{isExecuting.ToString().ToLowerInvariant()}}}""";

    private static string BookmarkPayload(string id, string workflowInstanceId, string stimulusHash, string activityTypeName) =>
        $$"""{"Id":"{{id}}","WorkflowInstanceId":"{{workflowInstanceId}}","StimulusHash":"{{stimulusHash}}","ActivityTypeName":"{{activityTypeName}}"}""";

    private static string TriggerPayload(string id, string definitionId, string definitionVersionId, string stimulusHash) =>
        $$"""{"Id":"{{id}}","DefinitionId":"{{definitionId}}","DefinitionVersionId":"{{definitionVersionId}}","StimulusHash":"{{stimulusHash}}"}""";

    private static string WorkflowLogPayload(string id, string workflowInstanceId, string eventName, int sequence) =>
        $$"""{"Id":"{{id}}","WorkflowInstanceId":"{{workflowInstanceId}}","EventName":"{{eventName}}","Sequence":{{sequence}}}""";

    private static string ActivityLogPayload(string id, string workflowInstanceId, string activityTypeName, int sequence) =>
        $$"""{"Id":"{{id}}","WorkflowInstanceId":"{{workflowInstanceId}}","ActivityTypeName":"{{activityTypeName}}","Sequence":{{sequence}}}""";

    private static string LockPayload(string id, string resource) =>
        $$"""{"Id":"{{id}}","Resource":"{{resource}}","Owner":"benchmark"}""";
}
