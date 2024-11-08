using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkPublish;

[PublicAPI]
internal class BulkPublish(IWorkflowDefinitionStore store, IWorkflowDefinitionPublisher workflowDefinitionPublisher, IAuthorizationService authorizationService)
    : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/bulk-actions/publish/workflow-definitions/by-definition-ids");
        ConfigurePermissions("publish:workflow-definitions");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var authorizationResult = authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Result.Succeeded)
        {
            await SendForbiddenAsync(cancellationToken);
            return null!;
        }

        var published = new List<string>();
        var notFound = new List<string>();
        var alreadyPublished = new List<string>();
        var skipped = new List<string>();
        var updatedConsumers = new List<string>();

        var definitions = (await store.FindManyAsync(new WorkflowDefinitionFilter
        {
            DefinitionIds = request.DefinitionIds,
            VersionOptions = VersionOptions.Latest
        }, cancellationToken: cancellationToken))
            .DistinctBy(x => x.DefinitionId)
            .ToDictionary(x => x.DefinitionId);

        foreach (var definitionId in request.DefinitionIds)
        {
            if (!definitions.TryGetValue(definitionId, out var definition))
            {
                notFound.Add(definitionId);
                continue;
            }

            if (definition.IsPublished)
            {
                alreadyPublished.Add(definitionId);
                continue;
            }

            if (definition.IsReadonly)
            {
                skipped.Add(definitionId);
                continue;
            }

            var result = await workflowDefinitionPublisher.PublishAsync(definition, cancellationToken);
            published.Add(definitionId);
            
            if (result.AffectedWorkflows.WorkflowDefinitions.Count > 0) 
                updatedConsumers.AddRange(result.AffectedWorkflows.WorkflowDefinitions.Select(x => x.DefinitionId));
        }

        return new Response(published, alreadyPublished, notFound, skipped, updatedConsumers);
    }
}