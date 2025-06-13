using Elsa.Workflows.Models;

namespace Elsa.Workflows;

public interface IActivityTestRunner
{
    Task<ActivityExecutionContext> RunAsync(WorkflowGraph workflowGraph, IActivity activity, CancellationToken cancellationToken = default);
}