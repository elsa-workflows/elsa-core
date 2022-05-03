using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities;
using Elsa.Management.Materializers;
using Elsa.Management.Services;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Api.Endpoints.WorkflowDefinitions;

public static partial class WorkflowDefinitions
{
    public record SaveWorkflowDefinitionRequest(
        string? DefinitionId,
        string? Name,
        string? Description,
        IActivity? Root,
        bool Publish);

    public static async Task<IResult> PostAsync(
        HttpContext httpContext,
        WorkflowSerializerOptionsProvider serializerOptionsProvider,
        IWorkflowPublisher workflowPublisher,
        CancellationToken cancellationToken)
    {
        var serializerOptions = serializerOptionsProvider.CreateApiOptions();
        var model = (await httpContext.Request.ReadFromJsonAsync<SaveWorkflowDefinitionRequest>(serializerOptions, cancellationToken))!;
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
                draft.DefinitionId = definitionId;
        }

        // Update the draft with the received model.
        var root = model.Root ?? new Sequence();
        var stringData = JsonSerializer.Serialize(root, serializerOptions);

        draft.StringData = stringData;
        draft.MaterializerName = ClrWorkflowMaterializer.MaterializerName;
        draft.Name = model.Name?.Trim();
        draft.Description = model.Description?.Trim();
        draft = model.Publish ? await workflowPublisher.PublishAsync(draft, cancellationToken) : await workflowPublisher.SaveDraftAsync(draft, cancellationToken);
        
        var statusCode = isNew ? StatusCodes.Status201Created : StatusCodes.Status200OK;
        return Results.Json(draft, serializerOptions, statusCode: statusCode);
    }
}