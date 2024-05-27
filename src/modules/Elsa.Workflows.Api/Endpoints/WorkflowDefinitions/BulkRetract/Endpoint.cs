using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkRetract;

[PublicAPI]
internal class BulkRetract(IWorkflowDefinitionStore store, IWorkflowDefinitionPublisher workflowDefinitionPublisher, IAuthorizationService authorizationService)
    : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/bulk-actions/retract/workflow-definitions/by-definition-ids");
        ConfigurePermissions("retract:workflow-definitions");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var authorizationResult = authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Result.Succeeded)
        {
            await SendForbiddenAsync(cancellationToken);
            return null!;
        }

        var retracted = new List<string>();
        var notFound = new List<string>();
        var notPublished = new List<string>();
        var skipped = new List<string>();

        foreach (var definitionId in request.DefinitionIds)
        {
            var definitions = (await store.FindManyAsync(new WorkflowDefinitionFilter
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

            if (published.IsReadonly)
            {
                skipped.Add(definitionId);
                continue;
            }

            await workflowDefinitionPublisher.RetractAsync(published, cancellationToken);
            retracted.Add(definitionId);
        }

        return new Response(retracted, notPublished, notFound, skipped);
    }
}