using System.ComponentModel;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.ActivationValidators;

/// <summary>
/// Always allow the creation of a new workflow instance.
/// </summary>
[DisplayName("Always")]
public class AllowAlwaysStrategy : IWorkflowActivationStrategy
{
    /// <summary>
    /// Always allow creating a new instance. 
    /// </summary>
    public ValueTask<bool> GetAllowActivationAsync(WorkflowInstantiationStrategyContext context) => new(true);
}