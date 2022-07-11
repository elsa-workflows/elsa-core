using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore.Attributes;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Persistence.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "BulkPublish")]
public class BulkPublish : Controller
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    public BulkPublish(IWorkflowDefinitionStore store, IWorkflowDefinitionPublisher workflowDefinitionPublisher, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _workflowDefinitionPublisher = workflowDefinitionPublisher;
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

            await _workflowDefinitionPublisher.PublishAsync(definition, cancellationToken);
            published.Add(definitionId);
        }

        return Json(new BulkPublishWorkflowDefinitionsResponse(published, alreadyPublished, notFound), serializerOptions);
    }

    public record BulkPublishWorkflowDefinitionsRequest(ICollection<string> DefinitionIds);

    public record BulkPublishWorkflowDefinitionsResponse(ICollection<string> Published, ICollection<string> AlreadyPublished, ICollection<string> NotFound);
}