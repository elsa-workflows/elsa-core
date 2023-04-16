using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Runtime.Contracts;
using Humanizer;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Export;

/// <summary>
/// Exports the specified workflow definition as JSON download.
/// </summary>
public class Export : ElsaEndpoint<Request>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IApiSerializer _serializer;
    private readonly VariableDefinitionMapper _variableDefinitionMapper;

    /// <inheritdoc />
    public Export(
        IWorkflowDefinitionStore store,
        IWorkflowDefinitionService workflowDefinitionService,
        IApiSerializer serializer,
        VariableDefinitionMapper variableDefinitionMapper)
    {
        _store = store;
        _workflowDefinitionService = workflowDefinitionService;
        _serializer = serializer;
        _variableDefinitionMapper = variableDefinitionMapper;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/workflow-definitions/{definitionId}/export");
        ConfigurePermissions("read:workflow-definitions");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var versionOptions = request.VersionOptions != null ? VersionOptions.FromString(request.VersionOptions) : VersionOptions.Latest;
        var definition = (await _store.FindManyAsync(new WorkflowDefinitionFilter { DefinitionId = request.DefinitionId, VersionOptions = versionOptions }, cancellationToken: cancellationToken)).FirstOrDefault();

        if (definition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        var variables = _variableDefinitionMapper.Map(workflow.Variables).ToList();

        var model = new WorkflowDefinitionModel(
            definition.Id,
            definition.DefinitionId,
            definition.Name,
            definition.Description,
            definition.CreatedAt,
            definition.Version,
            variables,
            definition.Inputs,
            definition.Outputs,
            definition.Outcomes,
            definition.CustomProperties,
            definition.UsableAsActivity,
            definition.IsLatest,
            definition.IsPublished,
            workflow.Root);

        var serializerOptions = _serializer.CreateOptions();
        
        // Exclude composite activities from being serialized.
        serializerOptions.Converters.Add(new JsonIgnoreCompositeRootConverterFactory());
        
        var binaryJson = JsonSerializer.SerializeToUtf8Bytes(model, serializerOptions);
        var hasWorkflowName = !string.IsNullOrWhiteSpace(definition.Name);
        var workflowName = hasWorkflowName ? definition.Name!.Trim() : definition.DefinitionId;

        var fileName = hasWorkflowName
            ? $"{workflowName.Underscore().Dasherize().ToLowerInvariant()}.json"
            : $"workflow-definition-{workflowName.Underscore().Dasherize().ToLowerInvariant()}.json";

        await SendBytesAsync(binaryJson, fileName, cancellation: cancellationToken);
    }
}