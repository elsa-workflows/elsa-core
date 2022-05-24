using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.Workflows.Management.Services;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "BulkPublish")]
public class BulkPublish : Controller
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowPublisher _workflowPublisher;
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;

    public BulkPublish(IWorkflowDefinitionStore store, IWorkflowPublisher workflowPublisher, WorkflowSerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _workflowPublisher = workflowPublisher;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    [HttpPost]
    public async Task<IActionResult> HandleAsync(CancellationToken cancellationToken)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var model = (await Request.ReadFromJsonAsync<BulkPublishWorkflowDefinitionsRequest>(serializerOptions, cancellationToken))!;
        var published = new List<string>();
        var notFound = new List<string>();
        var alreadyPublished = new List<string>();

        foreach (var definitionId in model.DefinitionIds)
        {
            var definition = await _store.FindByDefinitionIdAsync(definitionId, VersionOptions.Latest, cancellationToken);

            if (definition == null)
            {
                notFound.Add(definitionId);
                continue;
            }

            if (definition.IsPublished)
            {
                alreadyPublished.Add(definitionId);
                continue;
            }

            await _workflowPublisher.PublishAsync(definition, cancellationToken);
            published.Add(definitionId);
        }

        return Json(new BulkPublishWorkflowDefinitionsResponse(published, alreadyPublished, notFound), serializerOptions);
    }

    public record BulkPublishWorkflowDefinitionsRequest(ICollection<string> DefinitionIds);

    public record BulkPublishWorkflowDefinitionsResponse(ICollection<string> Published, ICollection<string> AlreadyPublished, ICollection<string> NotFound);
}