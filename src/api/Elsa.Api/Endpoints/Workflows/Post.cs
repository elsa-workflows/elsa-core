using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Workflows;
using Elsa.Contracts;
using Elsa.Management.Contracts;
using Elsa.Management.Serialization;
using Elsa.Persistence.Mappers;
using Microsoft.AspNetCore.Http;

namespace Elsa.Api.Endpoints.Workflows;

public static partial class Workflows
{
    public record SaveWorkflowRequest(
        string? DefinitionId,
        string? Name,
        string? Description,
        IActivity? Root,
        ICollection<ITrigger>? Triggers,
        bool Publish);

    public static async Task<IResult> PostAsync(
        HttpContext httpContext,
        WorkflowSerializerOptionsProvider serializerOptionsProvider,
        IWorkflowPublisher workflowPublisher,
        WorkflowDefinitionMapper mapper,
        CancellationToken cancellationToken)
    {
        var serializerOptions = serializerOptionsProvider.CreateSerializerOptions();
        var model = (await httpContext.Request.ReadFromJsonAsync<SaveWorkflowRequest>(serializerOptions, cancellationToken))!;
        var definitionId = model.DefinitionId;

        // Get a workflow draft version.
        var draft = !string.IsNullOrWhiteSpace(definitionId)
            ? await workflowPublisher.GetDraftAsync(definitionId, cancellationToken)
            : default;

        var isNew = draft == null;

        // Create a new workflow in case no existing definition was found.
        if (draft == null)
        {
            draft = workflowPublisher.New();

            if (!string.IsNullOrWhiteSpace(definitionId))
                draft = draft.WithDefinitionId(definitionId);
        }

        // Update the draft with the received model.
        var root = model.Root ?? new Sequence();
        var triggers = model.Triggers ?? new List<ITrigger>();

        draft = draft with
        {
            Root = root,
            Triggers = triggers,
            Metadata = draft.Metadata with
            {
                Name = model.Name,
                Description = model.Description
            }
        };
        
        draft = model.Publish ? await workflowPublisher.PublishAsync(draft, cancellationToken) : await workflowPublisher.SaveDraftAsync(draft, cancellationToken);
        var statusCode = isNew ? StatusCodes.Status201Created : StatusCodes.Status200OK;
        
        return Results.Json(draft, serializerOptions, statusCode: statusCode);
    }
}