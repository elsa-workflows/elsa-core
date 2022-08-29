using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions;
using Elsa.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Persistence.Services;
using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkPublish;

public class BulkPublish : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;

    public BulkPublish(IWorkflowDefinitionStore store, IWorkflowDefinitionPublisher workflowDefinitionPublisher, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _workflowDefinitionPublisher = workflowDefinitionPublisher;
    }

    public override void Configure()
    {
        Post("/bulk-actions/publish/workflow-definitions/by-definition-id");
        ConfigurePermissions("publish:workflow-definitions");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var published = new List<string>();
        var notFound = new List<string>();
        var alreadyPublished = new List<string>();

        foreach (var definitionId in request.DefinitionIds)
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

        return new Response(published, alreadyPublished, notFound);
    }
}