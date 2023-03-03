using System.ComponentModel.DataAnnotations;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.ActivationValidators;

/// <summary>
/// Always allow the creation of a new workflow instance.
/// </summary>
[Display(Name = "Always", Description = "Always allow the creation of a new workflow instance.")]
public class AllowAlwaysStrategy : IWorkflowActivationStrategy
{
    /// <summary>
    /// Always allow creating a new instance. 
    /// </summary>
    public ValueTask<bool> GetAllowActivationAsync(WorkflowInstantiationStrategyContext context) => new(true);
}