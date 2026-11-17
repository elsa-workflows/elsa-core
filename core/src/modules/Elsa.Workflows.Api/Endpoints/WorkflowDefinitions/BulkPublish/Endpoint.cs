using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Api.Security;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkPublish;

[PublicAPI]
internal class BulkPublish(
    IWorkflowDefinitionStore store,
    IWorkflowDefinitionPublisher workflowDefinitionPublisher,
    IAuthorizationService authorizationService,
    IWorkflowDefinitionService workflowDefinitionService,
    WorkflowDefinitionScriptAuthorizationService scriptAuthorizationService)
    : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/bulk-actions/publish/workflow-definitions/by-definition-ids");
        RequirePermission(Elsa.Workflows.Api.Permissions.WorkflowPermissions.Definitions, "publish");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        await using var mutationLock = await WorkflowDefinitionMutationLock.AcquireAsync(HttpContext.RequestServices, cancellationToken);

        var authorizationResult = await authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Succeeded)
        {
            await Send.ForbiddenAsync(cancellationToken);
            return null!;
        }

        var published = new List<string>();
        var notFound = new List<string>();
        var alreadyPublished = new List<string>();
        var skipped = new List<string>();
        var updatedConsumers = new List<string>();
        var failed = new List<string>();
        var warnings = new Dictionary<string, ICollection<string>>();
        var publishableDefinitions = new List<WorkflowDefinition>();

        var targets = (await store.FindManyAsync(new WorkflowDefinitionFilter
        {
            DefinitionIds = request.DefinitionIds,
            VersionOptions = VersionOptions.LatestOrPublished
        }, cancellationToken: cancellationToken)).Select(version => version.ShallowClone()).ToArray();
        if (!await authorizationService.CanMutateAsync(User, targets))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return null!;
        }

        var targetsByDefinitionId = targets.ToLookup(version => version.DefinitionId, StringComparer.Ordinal);

        foreach (var definitionId in request.DefinitionIds)
        {
            var definition = targetsByDefinitionId[definitionId].FirstOrDefault(version => version.IsLatest);
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

            publishableDefinitions.Add(definition.ShallowClone());
        }

        foreach (var definition in publishableDefinitions)
        {
            var workflowGraph = await workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
            var scriptAuthorizationResult = await scriptAuthorizationService.AuthorizeAsync(workflowGraph.Workflow, cancellationToken);
            if (!scriptAuthorizationResult.Succeeded)
            {
                await WorkflowDefinitionScriptAuthorizationFailure.SendAsync(scriptAuthorizationResult, message => AddError(message), Send.ErrorsAsync, cancellationToken);
                return null!;
            }
        }

        foreach (var definition in publishableDefinitions)
        {
            var definitionId = definition.DefinitionId;
            PublishWorkflowDefinitionResult result;
            try
            {
                result = await workflowDefinitionPublisher.PublishAsync(definition, targetsByDefinitionId[definitionId].ToArray(), cancellationToken);
            }
            catch (WorkflowDefinitionEditConflictException)
            {
                skipped.Add(definitionId);
                continue;
            }

            if (!result.Succeeded)
            {
                failed.Add(definitionId);
                continue;
            }

            published.Add(definitionId);

            if (result.ValidationErrors.Count > 0)
                warnings[definitionId] = result.ValidationErrors.Select(x => x.Message).ToList();
            
            if (result.AffectedWorkflows.WorkflowDefinitions.Count > 0) 
                updatedConsumers.AddRange(result.AffectedWorkflows.WorkflowDefinitions.Select(x => x.DefinitionId));
        }

        return new(published, alreadyPublished, notFound, skipped, updatedConsumers, failed, warnings);
    }

}
