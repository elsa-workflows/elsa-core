using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Persistence.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkDelete;

public class BulkDelete : Endpoint<Request, Response>
{
    private readonly IWorkflowInstanceStore _store;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    public BulkDelete(IWorkflowInstanceStore store, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    public override void Configure()
    {
        Post("");
    }

    [Microsoft.AspNetCore.Mvc.HttpPost]
    public async Task<IActionResult> HandleAsync(CancellationToken cancellationToken)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var model = await Request.ReadFromJsonAsync<Request>(serializerOptions, cancellationToken);
        var count = await _store.DeleteManyAsync(model!.Ids, cancellationToken);

        return Json(new Response(count), serializerOptions);
    }
}

public class Request
{
    public ICollection<string> Ids { get; set; } = default!;
}

public class Response
{
    public Response(int deletedCount)
    {
        DeletedCount = deletedCount;
    }

    [JsonPropertyName("deleted")] public int DeletedCount { get; init; }
}