using System.Linq.Expressions;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Models;

/// <summary>
/// Represents a workflow instance ID.
/// </summary>
public class WorkflowInstanceId
{
    /// <summary>
    /// The ID of the workflow instance.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// Returns a model representing the workflow instance ID of the specified <see cref="WorkflowInstance"/>.
    /// </summary>
    public static WorkflowInstanceId FromInstance(WorkflowInstance workflowInstance)
    {
        return new()
        {
            Id = workflowInstance.Id
        };
    }

    /// <summary>
    /// Returns a model representing the workflow instance ID of the specified <see cref="WorkflowInstance"/>.
    /// </summary>
    public static Expression<Func<WorkflowInstance, WorkflowInstanceId>> FromInstanceExpression()
    {
        return workflowInstance => new()
        {
            Id = workflowInstance.Id
        };
    }
}