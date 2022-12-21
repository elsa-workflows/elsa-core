using System.ComponentModel;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Strategies;

/// <summary>
/// Always allow the creation of a new workflow instance.
/// </summary>
[DisplayName("Always")]
public class AlwaysStrategy : IWorkflowInstantiationStrategy
{
    /// <summary>
    /// Always allow creating a new instance. 
    /// </summary>
    public ValueTask<bool> ShouldCreateInstanceAsync(WorkflowInstantiationStrategyContext context) => new(true);
}