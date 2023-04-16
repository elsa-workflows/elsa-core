using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkPublish;

[PublicAPI]
internal class BulkPublish : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;

    public BulkPublish(IWorkflowDefinitionStore store, IWorkflowDefinitionPublisher workflowDefinitionPublisher)
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

        var definitions = (await _store.FindManyAsync(new WorkflowDefinitionFilter
            {
                DefinitionIds = request.DefinitionIds, VersionOptions = VersionOptions.Latest
            }, cancellationToken: cancellationToken))
            .DistinctBy(x => x.DefinitionId)
            .ToDictionary(x => x.DefinitionId);

        foreach (var definitionId in request.DefinitionIds)
        {
            if(!definitions.TryGetValue(definitionId, out var definition))
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