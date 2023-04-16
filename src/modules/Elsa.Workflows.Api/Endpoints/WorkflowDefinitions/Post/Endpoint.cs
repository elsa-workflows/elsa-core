using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;
using Medallion.Threading;
using System.Text.Json;
using Elsa.Workflows.Api.Mappers;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Management.Contracts;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Post;

[PublicAPI]
internal class Post : ElsaEndpoint<SaveWorkflowDefinitionRequest, WorkflowDefinitionResponse, WorkflowDefinitionMapper>
{
    private readonly IApiSerializer _serializer;
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
    private readonly VariableDefinitionMapper _variableDefinitionMapper;
    private readonly IDistributedLockProvider _distributedLockProvider;

    public Post(
        IApiSerializer serializer,
        IWorkflowDefinitionPublisher workflowDefinitionPublisher,
        VariableDefinitionMapper variableDefinitionMapper,
        IDistributedLockProvider distributedLockProvider)
    {
        _serializer = serializer;
        _workflowDefinitionPublisher = workflowDefinitionPublisher;
        _variableDefinitionMapper = variableDefinitionMapper;
        _distributedLockProvider = distributedLockProvider;
    }

    public override void Configure()
    {
        Post("/workflow-definitions");
        ConfigurePermissions("write:workflow-definitions");
    }

    public override async Task HandleAsync(SaveWorkflowDefinitionRequest request, CancellationToken cancellationToken)
    {
        var definitionId = request.DefinitionId;
        var resourceName = $"{GetType().FullName}:{(!string.IsNullOrWhiteSpace(definitionId) ? definitionId : Guid.NewGuid().ToString())}";

        await using var handle = await _distributedLockProvider.AcquireLockAsync(resourceName, TimeSpan.FromMinutes(1), cancellationToken);

        var draft = !string.IsNullOrWhiteSpace(definitionId)
            ? await _workflowDefinitionPublisher.GetDraftAsync(definitionId, VersionOptions.Latest, cancellationToken)
            : default;

        var isNew = draft == null;

        // Create a new workflow in case no existing definition was found.
        if (isNew)
        {
            draft = _workflowDefinitionPublisher.New();

            if (!string.IsNullOrWhiteSpace(definitionId))
                draft.DefinitionId = definitionId;
        }

        // Update the draft with the received model.
        var root = request.Root ?? new Sequence();
        var serializerOptions = _serializer.CreateOptions();
        
        // Ignore the root activity when serializing the workflow definition.
        serializerOptions.Converters.Add(new JsonIgnoreCompositeRootConverterFactory());
        
        var stringData = JsonSerializer.Serialize(root, serializerOptions);
        var variables = _variableDefinitionMapper.Map(request.Variables).ToList();
        var inputs = request.Inputs ?? new List<InputDefinition>();
        var outputs = request.Outputs ?? new List<OutputDefinition>();
        var outcomes = request.Outcomes ?? new List<string>();

        draft!.StringData = stringData;
        draft.MaterializerName = JsonWorkflowMaterializer.MaterializerName;
        draft.Name = request.Name?.Trim();
        draft.Description = request.Description?.Trim();
        draft.CustomProperties = request.CustomProperties ?? new Dictionary<string, object>();
        draft.Variables = variables;
        draft.Inputs = inputs;
        draft.Outputs = outputs;
        draft.Outcomes = outcomes;
        draft.Options = request.Options;
        draft.UsableAsActivity = request.UsableAsActivity;
        draft = request.Publish ? await _workflowDefinitionPublisher.PublishAsync(draft, cancellationToken) : await _workflowDefinitionPublisher.SaveDraftAsync(draft, cancellationToken);

        var response = await Map.FromEntityAsync(draft, cancellationToken);

        if (isNew)
            await SendCreatedAtAsync<Get.Get>(new { definitionId }, response, cancellation: cancellationToken);
        else
        {
            await HttpContext.Response.WriteAsJsonAsync(response, serializerOptions, cancellationToken);
        }
    }
}