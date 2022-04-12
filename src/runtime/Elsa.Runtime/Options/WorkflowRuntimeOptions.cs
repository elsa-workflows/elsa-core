using Elsa.Contracts;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Runtime.Options;

public class WorkflowRuntimeOptions
{
    /// <summary>
    /// A list of workflow builders configured during application startup.
    /// </summary>
    public IDictionary<string, Func<IServiceProvider, IWorkflow>> Workflows { get; set; } = new Dictionary<string, Func<IServiceProvider, IWorkflow>>();

    /// <summary>
    /// A factory that instantiates a concrete <see cref="IWorkflowInvoker"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowInvoker> WorkflowInvokerFactory { get; set; } = sp => ActivatorUtilities.CreateInstance<DefaultWorkflowInvoker>(sp);

    /// <summary>
    /// A factory that instantiates a concrete <see cref="IWorkflowDispatcher"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowDispatcher> WorkflowDispatcherFactory { get; set; } = sp => ActivatorUtilities.CreateInstance<TaskBasedWorkflowDispatcher>(sp);
}