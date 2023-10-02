using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Enums;
using Elsa.Common.Entities;

namespace Elsa.Alterations.Core.Entities;

/// <summary>
/// A plan that contains a list of alterations to be applied to a set of workflow instances.
/// </summary>
public class AlterationPlan : Entity
{
    /// <summary>
    /// The alterations to be applied.
    /// </summary>
    public ICollection<IAlteration> Alterations { get; set; } = new List<IAlteration>();
    
    /// <summary>
    /// The IDs of the workflow instances that this plan applies to.
    /// </summary>
    public ICollection<string> WorkflowInstanceIds { get; set; } = new List<string>();

    /// <summary>
    /// The status of the plan.
    /// </summary>
    public AlterationPlanStatus Status { get; set; }
    
    /// <summary>
    /// The date and time at which the plan was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// The date and time at which the plan was started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; set; }
    
    /// <summary>
    /// The date and time at which the plan was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}