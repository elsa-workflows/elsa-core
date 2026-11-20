using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Api.Security;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Publish;

[PublicAPI]
internal class Publish(
    IWorkflowDefinitionStore store,
    IWorkflowDefinitionPublisher workflowDefinitionPublisher,
    IWorkflowDefinitionLinker linker,
    IAuthorizationService authorizationService,
    IWorkflowDefinitionService workflowDefinitionService,
    WorkflowDefinitionScriptAuthorizationService scriptAuthorizationService)
    : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/workflow-definitions/{definitionId}/publish");
        RequirePermission(Elsa.Workflows.Api.Permissions.WorkflowPermissions.Definitions, "publish");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        await using var mutationLock = await WorkflowDefinitionMutationLock.AcquireAsync(HttpContext.RequestServices, cancellationToken);

        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = request.DefinitionId,
            VersionOptions = VersionOptions.LatestOrPublished
        };

        var targets = (await store.FindManyAsync(filter, cancellationToken)).Select(version => version.ShallowClone()).ToArray();
        var definition = targets.FirstOrDefault(version => version.IsLatest)?.ShallowClone();

        if (definition == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        var authorizationResult = await authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(definition), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Succeeded)
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        if (!definition.IsPublished)
        {
            if (!await authorizationService.CanMutateAsync(User, targets))
            {
                await Send.ForbiddenAsync(cancellationToken);
                return;
            }
        }

        var workflowGraph = await workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        var scriptAuthorizationResult = await scriptAuthorizationService.AuthorizeAsync(workflowGraph.Workflow, cancellationToken);
        if (!scriptAuthorizationResult.Succeeded)
        {
            await WorkflowDefinitionScriptAuthorizationFailure.SendAsync(scriptAuthorizationResult, message => AddError(message), Send.ErrorsAsync, cancellationToken);
            return;
        }

        var isPublished = definition.IsPublished;
        PublishWorkflowDefinitionResult? result;
        try
        {
            result = !isPublished ? await workflowDefinitionPublisher.PublishAsync(definition, targets, cancellationToken) : null;
        }
        catch (WorkflowDefinitionEditConflictException)
        {
            AddError("The workflow definition changed or is protected.");
            await Send.ErrorsAsync(StatusCodes.Status409Conflict, cancellationToken);
            return;
        }

        if (result is { Succeeded: false })
        {
            foreach (var validationError in result.ValidationErrors)
                AddError(validationError.Message);

            await Send.ErrorsAsync(400, cancellationToken);
            return;
        }

        var validationErrors = result?.ValidationErrors.Where(error => !error.IsWarning).Select(error => error.Message).ToList() ?? [];
        var mappedDefinition = await linker.MapAsync(definition, cancellationToken);
        var response = new Response(mappedDefinition, isPublished, result?.AffectedWorkflows.WorkflowDefinitions.Count ?? 0, validationErrors)
        {
            ValidationWarnings = result?.ValidationErrors.Where(error => error.IsWarning).Select(error => error.Message).ToList() ?? []
        };
        await Send.OkAsync(response, cancellationToken);
    }
}
