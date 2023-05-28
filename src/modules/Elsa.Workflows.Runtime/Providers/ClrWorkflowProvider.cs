using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Features;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Providers;

/// <summary>
/// Provides workflows to the system that are registered with <see cref="WorkflowRuntimeFeature"/>
/// </summary>
public class ClrWorkflowProvider : IWorkflowProvider
{
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly RuntimeOptions _options;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ClrWorkflowProvider(
        IOptions<RuntimeOptions> options,
        IWorkflowBuilderFactory workflowBuilderFactory,
        IServiceProvider serviceProvider
    )
    {
        _workflowBuilderFactory = workflowBuilderFactory;
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    /// <inheritdoc />
    public string Name => "CLR";

    /// <inheritdoc />
    public async ValueTask<IEnumerable<MaterializedWorkflow>> GetWorkflowDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var workflowDefinitionTasks = _options.Workflows.Values.Select(async x => await BuildWorkflowDefinition(x, cancellationToken)).ToList();
        var workflowDefinitions = await Task.WhenAll(workflowDefinitionTasks);
        return workflowDefinitions;
    }

    private async Task<MaterializedWorkflow> BuildWorkflowDefinition(Func<IServiceProvider, ValueTask<IWorkflow>> workflowFactory, CancellationToken cancellationToken)
    {
        var builder = _workflowBuilderFactory.CreateBuilder();
        var workflowBuilder = await workflowFactory(_serviceProvider);
        var workflowBuilderType = workflowBuilder.GetType();

        builder.DefinitionId = workflowBuilderType.Name;
        await workflowBuilder.BuildAsync(builder, cancellationToken);

        var workflow = await builder.BuildWorkflowAsync(cancellationToken);
        var materializerContext = new ClrWorkflowMaterializerContext(workflowBuilder.GetType());
        return new MaterializedWorkflow(workflow, ClrWorkflowMaterializer.MaterializerName, materializerContext);
    }
}