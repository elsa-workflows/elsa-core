using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Serialization.Converters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Publish;

[PublicAPI]
internal class Publish(IWorkflowDefinitionStore store, IWorkflowDefinitionPublisher workflowDefinitionPublisher, IApiSerializer serializer, IWorkflowDefinitionLinker linker, IAuthorizationService authorizationService)
    : ElsaEndpoint<Request, LinkedWorkflowDefinitionModel>
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
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var authorizationResult = authorizationService.AuthorizeAsync(User, new NotReadOnlyResource(definition), AuthorizationPolicies.NotReadOnlyPolicy);

        if (!authorizationResult.Result.Succeeded)
        {
            await SendForbiddenAsync(cancellationToken);
            return;
        }

        PublishWorkflowDefinitionResult? result = null;
        var isPublished = definition.IsPublished; 
        if (!isPublished)
        {
            result = await workflowDefinitionPublisher.PublishAsync(definition, cancellationToken);
        }
        
        var mappedDefinition = await linker.MapAsync(definition, cancellationToken);
        var response = new Response(mappedDefinition, isPublished, result?.ConsumingWorkflows?.Count() ?? 0);
        
        // We do not want to include composite root activities in the response.
        var serializerOptions = serializer.GetOptions().Clone();
        serializerOptions.Converters.Add(new JsonIgnoreCompositeRootConverterFactory());

        await HttpContext.Response.WriteAsJsonAsync(response, serializerOptions, cancellationToken);
    }
}