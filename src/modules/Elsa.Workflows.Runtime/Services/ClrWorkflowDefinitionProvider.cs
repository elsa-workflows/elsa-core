using System.Text.Json;
using Elsa.Common.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Features;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Provides workflows to the system that are registered with <see cref="WorkflowRuntimeFeature"/>
/// </summary>
public class ClrWorkflowDefinitionProvider : IWorkflowDefinitionProvider
{
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly ISystemClock _systemClock;
    private readonly IServiceProvider _serviceProvider;
    private readonly RuntimeOptions _options;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ClrWorkflowDefinitionProvider(
        IOptions<RuntimeOptions> options,
        IWorkflowBuilderFactory workflowBuilderFactory,
        SerializerOptionsProvider serializerOptionsProvider,
        ISystemClock systemClock,
        IServiceProvider serviceProvider
    )
    {
        _workflowBuilderFactory = workflowBuilderFactory;
        _serializerOptionsProvider = serializerOptionsProvider;
        _systemClock = systemClock;
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    /// <inheritdoc />
    public string Name => "CLR";

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowDefinitionResult>> GetWorkflowDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var workflowDefinitionTasks = _options.Workflows.Values.Select(async x => await BuildWorkflowDefinition(x, cancellationToken)).ToList();
        var workflowDefinitions = await Task.WhenAll(workflowDefinitionTasks);
        return workflowDefinitions;
    }

    private async Task<WorkflowDefinitionResult> BuildWorkflowDefinition(Func<IServiceProvider, ValueTask<IWorkflow>> workflowFactory, CancellationToken cancellationToken)
    {
        var builder = _workflowBuilderFactory.CreateBuilder();
        var workflowBuilder = await workflowFactory(_serviceProvider);
        var workflowBuilderType = workflowBuilder.GetType();

        builder.DefinitionId = workflowBuilderType.Name;
        await workflowBuilder.BuildAsync(builder, cancellationToken);

        var workflow = await builder.BuildWorkflowAsync(cancellationToken);
        var workflowJson = JsonSerializer.Serialize(workflow.Root, _serializerOptionsProvider.CreatePersistenceOptions());
        var materializerContext = new ClrWorkflowMaterializerContext(workflowBuilder.GetType());
        var materializerContextJson = JsonSerializer.Serialize(materializerContext, _serializerOptionsProvider.CreatePersistenceOptions());
        var name = string.IsNullOrWhiteSpace(workflow.WorkflowMetadata.Name) ? workflowBuilderType.Name : workflow.WorkflowMetadata.Name.Trim();

        var definition = new WorkflowDefinition
        {
            Id = workflow.Identity.Id,
            DefinitionId = workflow.Identity.DefinitionId,
            Version = workflow.Identity.Version,
            Name = name,
            Description = workflow.WorkflowMetadata.Description,
            CustomProperties = workflow.Metadata,
            Variables = workflow.Variables,
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