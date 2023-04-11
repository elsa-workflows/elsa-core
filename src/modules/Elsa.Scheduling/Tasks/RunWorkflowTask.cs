using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Tasks;

/// <summary>
/// A task that runs a workflow.
/// </summary>
public class RunWorkflowTask : ITask
{
    private readonly DispatchWorkflowDefinitionRequest _request;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunWorkflowTask"/> class.
    /// </summary>
    public RunWorkflowTask(DispatchWorkflowDefinitionRequest request)
    {
        _request = request;
    }
    
    /// <inheritdoc />
    public async ValueTask ExecuteAsync(TaskExecutionContext context)
    {
        var workflowDispatcher = context.ServiceProvider.GetRequiredService<IWorkflowDispatcher>();
        await workflowDispatcher.DispatchAsync(_request, context.CancellationToken);
    }
}