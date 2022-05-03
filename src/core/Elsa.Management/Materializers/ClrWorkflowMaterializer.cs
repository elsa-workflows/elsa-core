using System.Text.Json;
using Elsa.Builders;
using Elsa.Management.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Management.Materializers;

public class ClrWorkflowMaterializer : IWorkflowMaterializer
{
    public const string MaterializerName = "CLR";

    private readonly WorkflowSerializerOptionsProvider _workflowSerializerOptionsProvider;
    private readonly IServiceProvider _serviceProvider;
    
    public ClrWorkflowMaterializer(
        WorkflowSerializerOptionsProvider workflowSerializerOptionsProvider,
        IServiceProvider serviceProvider
    )
    {
        _workflowSerializerOptionsProvider = workflowSerializerOptionsProvider;
        _serviceProvider = serviceProvider;
    }

    public string Name => MaterializerName;

    public async ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var serializerOptions = _workflowSerializerOptionsProvider.CreatePersistenceOptions();
        var providerContext = JsonSerializer.Deserialize<ClrWorkflowMaterializerContext>(definition.MaterializerContext!, serializerOptions)!;
        var workflowBuilderType = providerContext.WorkflowBuilderType;
        var workflowBuilder = (IWorkflow)ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, workflowBuilderType);
        var workflowDefinitionBuilder = new WorkflowDefinitionBuilder();
        var workflow = await workflowDefinitionBuilder.BuildWorkflowAsync(workflowBuilder, cancellationToken);

        return workflow;
    }
}

public record ClrWorkflowMaterializerContext(Type WorkflowBuilderType);