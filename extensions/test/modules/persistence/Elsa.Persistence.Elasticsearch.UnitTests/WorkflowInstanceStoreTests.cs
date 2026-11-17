using System.Text;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Elsa.Persistence.Elasticsearch.Common;
using Elsa.Persistence.Elasticsearch.Modules.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Exceptions;
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
            .ThrowExceptions();
        if (onRequestCompleted != null)
            settings.OnRequestCompleted(onRequestCompleted);

        var client = new ElasticsearchClient(settings);
        return new(new ElasticStore<WorkflowInstance>(client, NullLogger<ElasticStore<WorkflowInstance>>.Instance));
    }
}
