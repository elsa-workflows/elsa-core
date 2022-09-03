using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Options;

public class WorkflowRuntimeOptions
{
    /// <summary>
    /// A list of workflow builders configured during application startup.
    /// </summary>
    public IDictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>> Workflows { get; set; } = new Dictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>>();

    /// <summary>
    /// A list of <see cref="IWorkflowStateExporter"/> providers. Each provider will be invoked when running a workflow.
    /// </summary>
    public ISet<Func<IServiceProvider, IWorkflowStateExporter>> WorkflowStateExporters { get; set; } = new HashSet<Func<IServiceProvider, IWorkflowStateExporter>>();
}