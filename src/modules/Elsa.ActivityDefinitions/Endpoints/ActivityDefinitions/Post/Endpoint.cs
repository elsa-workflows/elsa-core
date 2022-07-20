using System.Text.Json;
using Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.List;
using Elsa.ActivityDefinitions.Services;
using Elsa.Api.Common;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;

namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.Post;

public class Post : ProtectedEndpoint<Request, Response>
{
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly IActivityDefinitionPublisher _activityDefinitionPublisher;
    private readonly VariableDefinitionMapper _variableDefinitionMapper;

    public Post(
        SerializerOptionsProvider serializerOptionsProvider,
        IActivityDefinitionPublisher activityDefinitionPublisher,
        VariableDefinitionMapper variableDefinitionMapper)
    {
        _serializerOptionsProvider = serializerOptionsProvider;
        _activityDefinitionPublisher = activityDefinitionPublisher;
        _variableDefinitionMapper = variableDefinitionMapper;
    }

    public override void Configure()
    {
        Post("/activity-definitions");
        ConfigureSecurity(List.SecurityConstants.Permissions, List.SecurityConstants.Policies, List.SecurityConstants.Roles);
    }

    public override async Task<Response> ExecuteAsync(Request req, CancellationToken ct)
    {
        var definitionId = req.DefinitionId;

        // Get a workflow draft version.
        var draft = !string.IsNullOrWhiteSpace(definitionId)
            ? await _activityDefinitionPublisher.GetDraftAsync(definitionId, ct)
            : default;

        var isNew = draft == null;

        // Create a new workflow in case no existing definition was found.
        if (isNew)
        {
            draft = _activityDefinitionPublisher.New();

            if (!string.IsNullOrWhiteSpace(definitionId))
                draft.DefinitionId = definitionId;
        }

        // Update the draft with the received model.
        var root = req.Root ?? new Flowchart();
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var data = JsonSerializer.Serialize(root, serializerOptions);
        var variables = _variableDefinitionMapper.Map(req.Variables).ToList();

        draft!.Data = data;
        draft.Name = req.Name?.Trim();
        draft.Description = req.Description?.Trim();
        draft.Metadata = req.Metadata ?? new Dictionary<string, object>();
        draft.Variables = variables;
        draft.ApplicationProperties = req.ApplicationProperties ?? new Dictionary<string, object>();
        draft = req.Publish ? await _activityDefinitionPublisher.PublishAsync(draft, ct) : await _activityDefinitionPublisher.SaveDraftAsync(draft, ct);

        var draftModel = new Response(
            draft.Id,
            draft.DefinitionId,
            draft.Name,
            draft.Description,
            draft.CreatedAt,
            draft.Version,
            req.Variables ?? new List<VariableDefinition>(),
            draft.Metadata,
            draft.ApplicationProperties,
            draft.IsLatest,
            draft.IsPublished,
            root);

        if (isNew)
        {
            await SendCreatedAtAsync<Get.Get>(new { Id = draft.Id }, draftModel, default, default, false, ct);
        }
        else
        {
            await SendOkAsync(draftModel, ct);
        }
        
        return draftModel;
    }
}