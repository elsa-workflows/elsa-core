using System.Text.Json;
using Elsa.Builders;
using Elsa.Management.Materializers;
using Elsa.Persistence.Entities;
using Elsa.Runtime.Options;
using Elsa.Runtime.Services;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.Extensions.Options;

namespace Elsa.Runtime.WorkflowProviders;

/// <summary>
/// Provides workflows to the system that are registered with <see cref="WorkflowRuntimeOptions"/>
/// </summary>
public class ClrWorkflowDefinitionProvider : IWorkflowDefinitionProvider
{
    private readonly IIdentityGraphService _identityGraphService;
    private readonly WorkflowSerializerOptionsProvider _workflowSerializerOptionsProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkflowRuntimeOptions _options;

    public ClrWorkflowDefinitionProvider(
        IOptions<WorkflowRuntimeOptions> options,
        IIdentityGraphService identityGraphService,
        WorkflowSerializerOptionsProvider workflowSerializerOptionsProvider,
        IServiceProvider serviceProvider
    )
    {
        _identityGraphService = identityGraphService;
        _workflowSerializerOptionsProvider = workflowSerializerOptionsProvider;
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public string Name => "CLR";

    public async ValueTask<IEnumerable<WorkflowDefinitionResult>> GetWorkflowDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var workflowDefinitionTasks = _options.Workflows.Values.Select(async x => await BuildWorkflowDefinition(x, cancellationToken)).ToList();
        var workflowDefinitions = await Task.WhenAll(workflowDefinitionTasks);
        return workflowDefinitions;
    }

    private async Task<WorkflowDefinitionResult> BuildWorkflowDefinition(Func<IServiceProvider, ValueTask<IWorkflow>> workflowFactory, CancellationToken cancellationToken)
    {
        var builder = new WorkflowDefinitionBuilder();
        var workflowBuilder = await workflowFactory(_serviceProvider);

        builder.WithDefinitionId(workflowBuilder.GetType().Name);
        await workflowBuilder.BuildAsync(builder, cancellationToken);

        var workflow = builder.BuildWorkflow();
        _identityGraphService.AssignIdentities(workflow);

        var workflowJson = JsonSerializer.Serialize(workflow, _workflowSerializerOptionsProvider.CreatePersistenceOptions());
        var materializerContext = new ClrWorkflowMaterializerContext(workflowBuilder.GetType());
        var materializerContextJson = JsonSerializer.Serialize(materializerContext, _workflowSerializerOptionsProvider.CreatePersistenceOptions());

        var definition = new WorkflowDefinition
        {
            Id = workflow.Identity.Id,
            DefinitionId = workflow.Identity.DefinitionId,
            Version = workflow.Identity.Version,
            Name = workflow.WorkflowMetadata.Name,
            Description = workflow.WorkflowMetadata.Description,
            Metadata = workflow.Metadata,
            Variables = workflow.Variables,
            Tags = workflow.Tags,
            ApplicationProperties = workflow.ApplicationProperties,
            IsLatest = workflow.Publication.IsLatest,
            IsPublished = workflow.Publication.IsPublished,
            CreatedAt = workflow.WorkflowMetadata.CreatedAt,
            MaterializerName = ClrWorkflowMaterializer.MaterializerName,
            MaterializerContext = materializerContextJson,
            StringData = workflowJson
        };

        return new WorkflowDefinitionResult(definition, workflow);
    }
}