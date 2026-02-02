using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Runtime.Features;
using Elsa.Workflows.Runtime.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Providers;

/// <summary>
/// Provides workflows to the system that are registered with <see cref="WorkflowRuntimeFeature"/>
/// </summary>
[UsedImplicitly]
public class ClrWorkflowsProvider(
    IOptions<RuntimeOptions> options,
    IWorkflowBuilderFactory workflowBuilderFactory,
    IServiceProvider serviceProvider) : IWorkflowsProvider
{
    /// <inheritdoc />
    public string Name => "CLR";

    /// <inheritdoc />
    public async ValueTask<IEnumerable<MaterializedWorkflow>> GetWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        var buildWorkflowTasks = options.Value.Workflows.Values.Select(async x => await BuildWorkflowAsync(x, cancellationToken)).ToList();
        var workflowDefinitions = await Task.WhenAll(buildWorkflowTasks);
        return workflowDefinitions;
    }

    private async Task<MaterializedWorkflow> BuildWorkflowAsync(Func<IServiceProvider, ValueTask<IWorkflow>> workflowFactory, CancellationToken cancellationToken)
    {
        var builder = workflowBuilderFactory.CreateBuilder();
        var workflowBuilder = await workflowFactory(serviceProvider);
        var workflowBuilderType = workflowBuilder.GetType();
        await workflowBuilder.BuildAsync(builder, cancellationToken);
        var workflow = await builder.BuildWorkflowAsync(cancellationToken);
        var versionSuffix = $"v{workflow.Version}";
        var definitionId = string.IsNullOrEmpty(workflow.Identity.DefinitionId) ? workflowBuilderType.Name : $"{workflow.Identity.DefinitionId}";
        var id = string.IsNullOrEmpty(workflow.Identity.Id) ? $"{workflowBuilderType.Name}:{versionSuffix}" : $"{workflow.Identity.Id}";
        
        workflow.Identity = workflow.Identity with
        {
            Id = id,
            DefinitionId = definitionId,
            TenantId = workflow.Identity.TenantId
        };

        var materializerContext = new ClrWorkflowMaterializerContext(workflowBuilder.GetType());
        return new(workflow, Name, ClrWorkflowMaterializer.MaterializerName, materializerContext);
    }
}