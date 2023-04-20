using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkRetract;

[PublicAPI]
internal class BulkRetract : ElsaEndpoint<Request, Response>
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
            var definitions = (await _store.FindManyAsync(new WorkflowDefinitionFilter
            {
                DefinitionId = definitionId,
                VersionOptions = VersionOptions.LatestOrPublished
            }, cancellationToken: cancellationToken)).ToList();

            if (!definitions.Any())
            {
                notFound.Add(definitionId);
                continue;
            }

            var published = definitions.FirstOrDefault(d => d.IsPublished);
            if (published is null)
            {
                notPublished.Add(definitionId);
                continue;
            }

            await _workflowDefinitionPublisher.RetractAsync(published, cancellationToken);
            retracted.Add(definitionId);
        }

        return new Response(retracted, notPublished, notFound);
    }
}