using System.Text.Json;
using Elsa.Workflows.Core.Builders;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Persistence.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Materializers;

public class ClrWorkflowMaterializer : IWorkflowMaterializer
{
    public const string MaterializerName = "CLR";

    private readonly WorkflowSerializerOptionsProvider _workflowSerializerOptionsProvider;
    private readonly IWorkflowDefinitionBuilderFactory _workflowDefinitionBuilderFactory;
    private readonly IServiceProvider _serviceProvider;
    
    public ClrWorkflowMaterializer(
        WorkflowSerializerOptionsProvider workflowSerializerOptionsProvider,
        IWorkflowDefinitionBuilderFactory workflowDefinitionBuilderFactory,
        IServiceProvider serviceProvider
    )
    {
        _workflowSerializerOptionsProvider = workflowSerializerOptionsProvider;
        _workflowDefinitionBuilderFactory = workflowDefinitionBuilderFactory;
        _serviceProvider = serviceProvider;
    }

    public string Name => MaterializerName;

    public async ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var serializerOptions = _workflowSerializerOptionsProvider.CreatePersistenceOptions();
        var providerContext = JsonSerializer.Deserialize<ClrWorkflowMaterializerContext>(definition.MaterializerContext!, serializerOptions)!;
        var workflowBuilderType = providerContext.WorkflowBuilderType;
        var workflowBuilder = (IWorkflow)ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, workflowBuilderType);
        var workflowDefinitionBuilder = _workflowDefinitionBuilderFactory.CreateBuilder();
        var workflow = await workflowDefinitionBuilder.BuildWorkflowAsync(workflowBuilder, cancellationToken);

        return workflow;
    }
}

public record ClrWorkflowMaterializerContext(Type WorkflowBuilderType);