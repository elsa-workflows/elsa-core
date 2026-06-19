using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Publish;

[PublicAPI]
internal class Publish(IWorkflowDefinitionStore store, IWorkflowDefinitionPublisher workflowDefinitionPublisher, IWorkflowDefinitionLinker linker, IAuthorizationService authorizationService)
    : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/workflow-definitions/{definitionId}/publish");
        ConfigurePermissions("publish:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = request.DefinitionId,
            VersionOptions = VersionOptions.Latest
        };

        var definition = await store.FindAsync(filter, cancellationToken);

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

        var isPublished = definition.IsPublished;
        var result = !isPublished ? await workflowDefinitionPublisher.PublishAsync(definition, cancellationToken) : null;

        if (result is { Succeeded: false })
        {
            foreach (var validationError in result.ValidationErrors)
                AddError(validationError.Message);

            await Send.ErrorsAsync(400, cancellationToken);
            return;
        }

        var validationErrors = result?.ValidationErrors.Select(e => e.Message).ToList() ?? [];
        var mappedDefinition = await linker.MapAsync(definition, cancellationToken);
        var response = new Response(mappedDefinition, isPublished, result?.AffectedWorkflows.WorkflowDefinitions.Count ?? 0, validationErrors);
        await Send.OkAsync(response, cancellationToken);
    }
}