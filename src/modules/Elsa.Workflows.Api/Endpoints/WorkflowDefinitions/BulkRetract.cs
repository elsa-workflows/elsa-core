using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Persistence.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "BulkRetract")]
public class BulkRetract : Controller
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowPublisher _workflowPublisher;
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;

    public BulkRetract(IWorkflowDefinitionStore store, IWorkflowPublisher workflowPublisher, WorkflowSerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _workflowPublisher = workflowPublisher;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    [HttpPost]
    public async Task<IActionResult> HandleAsync(CancellationToken cancellationToken)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var model = (await Request.ReadFromJsonAsync<BulkRetractWorkflowDefinitionsRequest>(serializerOptions, cancellationToken))!;
        var retracted = new List<string>();
        var notFound = new List<string>();
        var notPublished = new List<string>();

        foreach (var definitionId in model.DefinitionIds)
        {
            var definition = await _store.FindByDefinitionIdAsync(definitionId, VersionOptions.Latest, cancellationToken);

            if (definition == null)
            {
                notFound.Add(definitionId);
                continue;
            }

            if (!definition.IsPublished)
            {
                notPublished.Add(definitionId);
                continue;
            }

            await _workflowPublisher.RetractAsync(definition, cancellationToken);
            retracted.Add(definitionId);
        }

        return Json(new BulkRetractWorkflowDefinitionsResponse(retracted, notPublished, notFound), serializerOptions);
    }

    public record BulkRetractWorkflowDefinitionsRequest(ICollection<string> DefinitionIds);

    public record BulkRetractWorkflowDefinitionsResponse(ICollection<string> Retracted, ICollection<string> NotPublished, ICollection<string> NotFound);
}