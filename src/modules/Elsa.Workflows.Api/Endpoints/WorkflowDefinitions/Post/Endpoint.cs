using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;
using Medallion.Threading;
using System.Text.Json;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Management.Contracts;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Post;

[PublicAPI]
internal class Post : ElsaEndpoint<SaveWorkflowDefinitionRequest, WorkflowDefinitionModel>
{
    private readonly IApiSerializer _serializer;
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
    private readonly VariableDefinitionMapper _variableDefinitionMapper;
    private readonly WorkflowDefinitionMapper _workflowDefinitionMapper;
    private readonly IDistributedLockProvider _distributedLockProvider;

    public Post(
        IApiSerializer serializer,
        IWorkflowDefinitionPublisher workflowDefinitionPublisher,
        VariableDefinitionMapper variableDefinitionMapper,
        WorkflowDefinitionMapper workflowDefinitionMapper,
        IDistributedLockProvider distributedLockProvider)
    {
        _serializer = serializer;
        _workflowDefinitionPublisher = workflowDefinitionPublisher;
        _variableDefinitionMapper = variableDefinitionMapper;
        _workflowDefinitionMapper = workflowDefinitionMapper;
        _distributedLockProvider = distributedLockProvider;
    }

    public override void Configure()
    {
        Post("/workflow-definitions");
        ConfigurePermissions("write:workflow-definitions");
    }

    public override async Task HandleAsync(SaveWorkflowDefinitionRequest request, CancellationToken cancellationToken)
    {
        var model = request.Model;
        var definitionId = model.DefinitionId;
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
        var root = model.Root ?? new Sequence();
        var serializerOptions = _serializer.CreateOptions();
        
        // Ignore the root activity when serializing the workflow definition.
        serializerOptions.Converters.Add(new JsonIgnoreCompositeRootConverterFactory());
        
        var stringData = JsonSerializer.Serialize(root, serializerOptions);
        var variables = _variableDefinitionMapper.Map(model.Variables).ToList();
        var inputs = model.Inputs ?? new List<InputDefinition>();
        var outputs = model.Outputs ?? new List<OutputDefinition>();
        var outcomes = model.Outcomes ?? new List<string>();

        draft!.StringData = stringData;
        draft.MaterializerName = JsonWorkflowMaterializer.MaterializerName;
        draft.Name = model.Name?.Trim();
        draft.ToolVersion = model.ToolVersion;
        draft.Description = model.Description?.Trim();
        draft.CustomProperties = model.CustomProperties ?? new Dictionary<string, object>();
        draft.Variables = variables;
        draft.Inputs = inputs;
        draft.Outputs = outputs;
        draft.Outcomes = outcomes;
        draft.Options = model.Options ?? new WorkflowOptions();

        if (request.Publish.GetValueOrDefault(false))
        {
            var result = await _workflowDefinitionPublisher.PublishAsync(draft, cancellationToken);

            if (!result.Succeeded)
            {
                foreach (var validationError in result.ValidationErrors) 
                    AddError(validationError.Message);

                await SendErrorsAsync(400, cancellationToken);
                return;
            }
        }
        else
        {
            await _workflowDefinitionPublisher.SaveDraftAsync(draft, cancellationToken);
        }
        
        var response = await _workflowDefinitionMapper.MapAsync(draft, cancellationToken);

        if (isNew)
            await SendCreatedAtAsync<GetByDefinitionId.GetByDefinitionId>(new { definitionId }, response, cancellation: cancellationToken);
        else
        {
            await HttpContext.Response.WriteAsJsonAsync(response, serializerOptions, cancellationToken);
        }
    }
}