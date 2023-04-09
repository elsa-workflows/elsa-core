using Elsa.Common.Models;
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
    /// <summary>
    /// Initializes a new instance of the <see cref="RunWorkflowTask"/> class.
    /// </summary>
    /// <param name="workflowDefinitionId">The ID of the workflow definition to run.</param>
    /// <param name="input">The input to pass to the workflow.</param>
    /// <param name="correlationId">The correlation ID to use for the workflow.</param>
    public RunWorkflowTask(string workflowDefinitionId, IDictionary<string, object>? input = default, string? correlationId = default)
    {
        WorkflowDefinitionId = workflowDefinitionId;
        Input = input;
        CorrelationId = correlationId;
    }

    /// <summary>
    /// The ID of the workflow definition to run.
    /// </summary>
    public string WorkflowDefinitionId { get; set; } = default!;
    
    /// <summary>
    /// The input to pass to the workflow.
    /// </summary>
    public IDictionary<string, object>? Input { get; set; }
    
    /// <summary>
    /// The correlation ID to use for the workflow.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <inheritdoc />
    public async ValueTask ExecuteAsync(TaskExecutionContext context)
    {
        var request = new DispatchWorkflowDefinitionRequest(WorkflowDefinitionId, VersionOptions.Published, Input, CorrelationId);
        var workflowDispatcher = context.ServiceProvider.GetRequiredService<IWorkflowDispatcher>();
        await workflowDispatcher.DispatchAsync(request, context.CancellationToken);
    }
}