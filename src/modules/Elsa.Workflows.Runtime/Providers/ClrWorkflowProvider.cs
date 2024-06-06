using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Features;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Providers;

/// <summary>
/// Provides workflows to the system that are registered with <see cref="WorkflowRuntimeFeature"/>
/// </summary>
[UsedImplicitly]
public class ClrWorkflowProvider : IWorkflowProvider
{
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly RuntimeOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClrWorkflowProvider"/> class.
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
    public async ValueTask<IEnumerable<MaterializedWorkflow>> GetWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        var buildWorkflowTasks = _options.Workflows.Values.Select(async x => await BuildWorkflowAsync(x, cancellationToken)).ToList();
        var workflowDefinitions = await Task.WhenAll(buildWorkflowTasks);
        return workflowDefinitions;
    }

    private async Task<MaterializedWorkflow> BuildWorkflowAsync(Func<IServiceProvider, ValueTask<IWorkflow>> workflowFactory, CancellationToken cancellationToken)
    {
        var builder = _workflowBuilderFactory.CreateBuilder();
        var workflowBuilder = await workflowFactory(_serviceProvider);
        var workflowBuilderType = workflowBuilder.GetType();

        builder.DefinitionId = workflowBuilderType.Name;
        builder.Id = $"{workflowBuilderType.Name}:1.0";
        await workflowBuilder.BuildAsync(builder, cancellationToken);

        var workflow = await builder.BuildWorkflowAsync(cancellationToken);
        var materializerContext = new ClrWorkflowMaterializerContext(workflowBuilder.GetType());
        return new MaterializedWorkflow(workflow, Name, ClrWorkflowMaterializer.MaterializerName, materializerContext);
    }
}