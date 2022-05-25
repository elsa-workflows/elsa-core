using System.Text.Json;
using Elsa.Builders;
using Elsa.Workflows.Management.Materializers;
using Elsa.Persistence.Entities;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.WorkflowProviders;

/// <summary>
/// Provides workflows to the system that are registered with <see cref="ElsaRuntimeOptions"/>
/// </summary>
public class ClrWorkflowDefinitionProvider : IWorkflowDefinitionProvider
{
    private readonly IIdentityGraphService _identityGraphService;
    private readonly WorkflowSerializerOptionsProvider _workflowSerializerOptionsProvider;
    private readonly ISystemClock _systemClock;
    private readonly IServiceProvider _serviceProvider;
    private readonly ElsaRuntimeOptions _options;

    public ClrWorkflowDefinitionProvider(
        IOptions<ElsaRuntimeOptions> options,
        IIdentityGraphService identityGraphService,
        WorkflowSerializerOptionsProvider workflowSerializerOptionsProvider,
        ISystemClock systemClock,
        IServiceProvider serviceProvider
    )
    {
        _identityGraphService = identityGraphService;
        _workflowSerializerOptionsProvider = workflowSerializerOptionsProvider;
        _systemClock = systemClock;
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
        var workflowBuilderType = workflowBuilder.GetType();

        builder.WithDefinitionId(workflowBuilderType.Name);
        await workflowBuilder.BuildAsync(builder, cancellationToken);

        var workflow = builder.BuildWorkflow();
        _identityGraphService.AssignIdentities(workflow);

        var workflowJson = JsonSerializer.Serialize(workflow.Root, _workflowSerializerOptionsProvider.CreatePersistenceOptions());
        var materializerContext = new ClrWorkflowMaterializerContext(workflowBuilder.GetType());
        var materializerContextJson = JsonSerializer.Serialize(materializerContext, _workflowSerializerOptionsProvider.CreatePersistenceOptions());
        var name = string.IsNullOrWhiteSpace(workflow.WorkflowMetadata.Name) ? workflowBuilderType.Name : workflow.WorkflowMetadata.Name.Trim();

        var definition = new WorkflowDefinition
        {
            Id = workflow.Identity.Id,
            DefinitionId = workflow.Identity.DefinitionId,
            Version = workflow.Identity.Version,
            Name = name,
            Description = workflow.WorkflowMetadata.Description,
            Metadata = workflow.Metadata,
            Variables = workflow.Variables,
            Tags = builder.Tags,
            ApplicationProperties = workflow.ApplicationProperties,
            IsLatest = workflow.Publication.IsLatest,
            IsPublished = workflow.Publication.IsPublished,
            CreatedAt = workflow.WorkflowMetadata.CreatedAt == default ? _systemClock.UtcNow : workflow.WorkflowMetadata.CreatedAt,
            MaterializerName = ClrWorkflowMaterializer.MaterializerName,
            MaterializerContext = materializerContextJson,
            StringData = workflowJson
        };

        return new WorkflowDefinitionResult(definition, workflow);
    }
}