using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Runtime.Services;
using Humanizer;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Export;

public class Export : ElsaEndpoint<Request>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly VariableDefinitionMapper _variableDefinitionMapper;

    public Export(
        IWorkflowDefinitionStore store,
        IWorkflowDefinitionService workflowDefinitionService,
        SerializerOptionsProvider serializerOptionsProvider,
        VariableDefinitionMapper variableDefinitionMapper)
    {
        _store = store;
        _workflowDefinitionService = workflowDefinitionService;
        _serializerOptionsProvider = serializerOptionsProvider;
        _variableDefinitionMapper = variableDefinitionMapper;
    }

    public override void Configure()
    {
        Get("/workflow-definitions/{definitionId}/export");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var versionOptions = request.VersionOptions != null ? VersionOptions.FromString(request.VersionOptions) : VersionOptions.Latest;
        var definition = (await _store.FindManyByDefinitionIdAsync(request.DefinitionId, versionOptions, cancellationToken)).FirstOrDefault();

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
            definition.Metadata,
            definition.ApplicationProperties,
            definition.IsLatest,
            definition.IsPublished,
            workflow.Root);

        var binaryJson = JsonSerializer.SerializeToUtf8Bytes(model, serializerOptions);
        var hasWorkflowName = !string.IsNullOrWhiteSpace(definition.Name);
        var workflowName = hasWorkflowName ? definition.Name!.Trim() : definition.DefinitionId;

        var fileName = hasWorkflowName
            ? $"{workflowName.Underscore().Dasherize().ToLowerInvariant()}.json"
            : $"workflow-definition-{workflowName.Underscore().Dasherize().ToLowerInvariant()}.json";

        await SendBytesAsync(binaryJson, fileName, cancellation: cancellationToken);
    }
}