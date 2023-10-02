using Elsa.Alterations.Core.Contracts;

namespace Elsa.Alterations.Core.Models;

/// <summary>
/// Represents a new alteration plan.
/// </summary>
public class NewAlterationPlan
{
    /// <summary>
    /// The alterations to be applied.
    /// </summary>
    public ICollection<IAlteration> Alterations { get; set; } = new List<IAlteration>();
    
    /// <summary>
    /// The IDs of the workflow instances that this plan applies to.
    /// </summary>
    public ICollection<string> WorkflowInstanceIds { get; set; } = new List<string>();
}