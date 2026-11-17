using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Exceptions;
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
        RequirePermission(Elsa.Workflows.Api.Permissions.WorkflowPermissions.Definitions, "retract");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Succeeded)
        {
            await Send.ForbiddenAsync(cancellationToken);
            return null!;
        }

        var retracted = new List<string>();
        var notFound = new List<string>();
        var notPublished = new List<string>();
        var skipped = new List<string>();

        var targets = new List<(string DefinitionId, WorkflowDefinition Published)>();
        var plannedVersionIds = new HashSet<string>();

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
            if (published is null || !plannedVersionIds.Add(published.Id))
            {
                notPublished.Add(definitionId);
                continue;
            }

            targets.Add((definitionId, published.ShallowClone()));
        }

        if (!await authorizationService.CanMutateAsync(User, targets.Select(target => target.Published)))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return null!;
        }

        foreach (var (definitionId, published) in targets)
        {
            try
            {
                await workflowDefinitionPublisher.RetractAsync(published, cancellationToken);
                retracted.Add(definitionId);
            }
            catch (WorkflowDefinitionEditConflictException)
            {
                skipped.Add(definitionId);
            }
        }

        return new Response(retracted, notPublished, notFound, skipped);
    }
}
