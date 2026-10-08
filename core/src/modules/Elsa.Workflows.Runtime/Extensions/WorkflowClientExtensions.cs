using Elsa.Workflows.Runtime.Messages;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Provides extension methods for the <see cref="IWorkflowClient"/> interface.
/// </summary>
public static class WorkflowClientExtensions
{
    /// <summary>
    /// Executes the workflow instance and waits for it to complete or reach a suspend point.
    /// </summary>
    public static Task<RunWorkflowInstanceResponse> RunInstanceAsync(this IWorkflowClient workflowClient, CancellationToken cancellationToken = default)
    {
        return workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty, cancellationToken);
    }
}