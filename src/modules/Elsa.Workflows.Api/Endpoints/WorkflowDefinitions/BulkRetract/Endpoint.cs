using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions;
using Elsa.Models;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Persistence.Services;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkRetract;

public class BulkRetract : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;

    public BulkRetract(IWorkflowDefinitionStore store, IWorkflowDefinitionPublisher workflowDefinitionPublisher)
    {
        _store = store;
        _workflowDefinitionPublisher = workflowDefinitionPublisher;
    }

    public override void Configure()
    {
        Post("/bulk-actions/retract/workflow-definitions/by-definition-id");
        ConfigurePermissions("retract:workflow-definitions");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var retracted = new List<string>();
        var notFound = new List<string>();
        var notPublished = new List<string>();

        foreach (var definitionId in request.DefinitionIds)
        {
            var definition = (await _store.FindManyByDefinitionIdAsync(definitionId, VersionOptions.Latest, cancellationToken)).FirstOrDefault();

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

            await _workflowDefinitionPublisher.RetractAsync(definition, cancellationToken);
            retracted.Add(definitionId);
        }

        return new Response(retracted, notPublished, notFound);
    }
}