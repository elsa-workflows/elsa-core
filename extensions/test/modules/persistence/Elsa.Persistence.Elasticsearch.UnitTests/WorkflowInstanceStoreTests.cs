using System.Text;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Elsa.Persistence.Elasticsearch.Common;
using Elsa.Persistence.Elasticsearch.Modules.Management;
using Elsa.Persistence.Elasticsearch.Modules.Runtime;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Enums;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Logging.Abstractions;
using HttpMethod = Elastic.Transport.HttpMethod;

namespace Elsa.Persistence.Elasticsearch.UnitTests;

public class WorkflowInstanceStoreTests
{
    [Fact]
    public async Task AddUsesTheSameDocumentIdAsSaveWithCreateOnlySemantics()
    {
        const string body = """
            {"_index":"workflow-instances","_id":"instance-1","_version":1,"result":"created",
             "_shards":{"total":1,"successful":1,"failed":0},"_seq_no":0,"_primary_term":1}
            """;
        using var invoker = CreateInvoker(201, body);
        var requests = new List<(HttpMethod Method, string? Path)>();
        var store = CreateStore(invoker, details => requests.Add((details.HttpMethod, details.Uri?.AbsolutePath)));
        var instance = new WorkflowInstance { Id = "instance-1" };

        await store.SaveAsync(instance);
        await store.AddAsync(instance);

        Assert.Equal([
            (HttpMethod.PUT, "/workflow-instances/_doc/instance-1"),
            (HttpMethod.PUT, "/workflow-instances/_create/instance-1")
        ], requests);
    }

    [Fact]
    public async Task AddReportsAConflictWhenTheDocumentAlreadyExists()
    {
        using var invoker = CreateErrorInvoker(409, "version_conflict_engine_exception");
        var store = CreateStore(invoker);

        await Assert.ThrowsAsync<WorkflowInstanceAlreadyExistsException>(async () =>
            await store.AddAsync(new WorkflowInstance { Id = "instance-1" }));
    }

    [Theory]
    [InlineData(409, "index_closed_exception")]
    [InlineData(403, "security_exception")]
    [InlineData(503, "unavailable_shards_exception")]
    public async Task AddSurfacesOtherErrorsWithoutReportingADuplicate(int status, string errorType)
    {
        using var invoker = CreateErrorInvoker(status, errorType);
        var store = CreateStore(invoker);

        var exception = await Assert.ThrowsAsync<Exception>(async () =>
            await store.AddAsync(new WorkflowInstance { Id = "instance-1" }));

        Assert.Contains(errorType, exception.Message);
    }

    [Fact]
    public async Task DeleteUsesExactVersionAndStatusInTheWorkflowInstanceIndex()
    {
        using var invoker = CreateDeleteInvoker();
        ApiCallDetails? captured = null;
        var store = CreateStore(invoker, request => captured = request);

        await store.DeleteAsync(new WorkflowInstanceFilter
        {
            DefinitionVersionIds = ["version-a", "version-b"],
            WorkflowStatus = WorkflowStatus.Finished
        });

        Assert.NotNull(captured);
        Assert.Equal("/workflow-instances/_delete_by_query", captured.Uri?.AbsolutePath);
        using var json = JsonDocument.Parse(captured.RequestBodyInBytes!);
        var filters = json.RootElement.GetProperty("query").GetProperty("bool").GetProperty("filter");
        Assert.Equal(["version-a", "version-b"], filters[0].GetProperty("terms").GetProperty("definitionVersionId.keyword").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal("Finished", filters[1].GetProperty("term").GetProperty("status.keyword").GetProperty("value").GetString());
    }

    [Fact]
    public async Task AnEmptyWorkflowInstanceSelectionMatchesNoDocuments()
    {
        using var invoker = CreateDeleteInvoker();
        ApiCallDetails? captured = null;
        var store = CreateStore(invoker, request => captured = request);

        await store.DeleteAsync(new WorkflowInstanceFilter { Ids = [] });

        Assert.NotNull(captured);
        using var json = JsonDocument.Parse(captured.RequestBodyInBytes!);
        Assert.True(json.RootElement.GetProperty("query").GetProperty("bool").GetProperty("filter").TryGetProperty("match_none", out _));
    }

    [Fact]
    public async Task WorkflowExecutionLogDeletionUsesItsOwnIndexAndInstanceSelection()
    {
        using var invoker = CreateDeleteInvoker();
        ApiCallDetails? captured = null;
        var settings = new ElasticsearchClientSettings(invoker)
            .DefaultMappingFor<WorkflowExecutionLogRecord>(mapping => mapping.IndexName("workflow-execution-logs"))
            .DisableDirectStreaming()
            .ThrowExceptions()
            .OnRequestCompleted(request => captured = request);
        var client = new ElasticsearchClient(settings);
        var store = new ElasticWorkflowExecutionLogStore(new ElasticStore<WorkflowExecutionLogRecord>(client, NullLogger<ElasticStore<WorkflowExecutionLogRecord>>.Instance));

        await store.DeleteManyAsync(new WorkflowExecutionLogRecordFilter { WorkflowInstanceIds = ["instance-a"] });

        Assert.NotNull(captured);
        Assert.Equal("/workflow-execution-logs/_delete_by_query", captured.Uri?.AbsolutePath);
        using var json = JsonDocument.Parse(captured.RequestBodyInBytes!);
        var ids = json.RootElement.GetProperty("query").GetProperty("bool").GetProperty("filter").GetProperty("terms").GetProperty("workflowInstanceId.keyword");
        Assert.Equal("instance-a", Assert.Single(ids.EnumerateArray()).GetString());
    }

    [Fact]
    public async Task UnsupportedTimestampSelectionIsRejectedBeforeReadingOrDeletingCandidates()
    {
        using var invoker = CreateDeleteInvoker();
        var requests = 0;
        var store = CreateStore(invoker, _ => requests++);
        var filter = new WorkflowInstanceFilter
        {
            TimestampFilters = [new() { Column = nameof(WorkflowInstance.CreatedAt), Operator = TimestampFilterOperator.LessThan, Timestamp = DateTimeOffset.UnixEpoch }]
        };

        await Assert.ThrowsAsync<NotSupportedException>(async () => await store.SummarizeManyAsync(filter));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await store.DeleteAsync(filter));

        Assert.Equal(0, requests);
    }

    private static InMemoryRequestInvoker CreateDeleteInvoker() =>
        CreateInvoker(200, """{"took":2,"timed_out":false,"total":0,"deleted":0,"batches":0,"version_conflicts":0,"noops":0,"retries":{"bulk":0,"search":0},"throttled_millis":0,"requests_per_second":-1,"throttled_until_millis":0,"failures":[]}""");

    private static InMemoryRequestInvoker CreateErrorInvoker(int status, string errorType) =>
        CreateInvoker(status, $$"""{"error":{"type":"{{errorType}}","reason":"test error"},"status":{{status}}}""");

    private static InMemoryRequestInvoker CreateInvoker(int status, string body) =>
        new(Encoding.UTF8.GetBytes(body), status, headers: new(StringComparer.OrdinalIgnoreCase)
        {
            ["x-elastic-product"] = ["Elasticsearch"]
        });

    private static ElasticWorkflowInstanceStore CreateStore(InMemoryRequestInvoker invoker, Action<ApiCallDetails>? onRequestCompleted = null)
    {
        var settings = new ElasticsearchClientSettings(invoker)
            .DefaultMappingFor<WorkflowInstance>(mapping => mapping.IndexName("workflow-instances"))
            .DisableDirectStreaming()
            .ThrowExceptions();
        if (onRequestCompleted != null)
            settings.OnRequestCompleted(onRequestCompleted);

        var client = new ElasticsearchClient(settings);
        return new(new ElasticStore<WorkflowInstance>(client, NullLogger<ElasticStore<WorkflowInstance>>.Instance));
    }
}
