using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.Alterations.Enums;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Resources.Alterations.Models;

/// <summary>
/// A plan that contains a list of alterations to be applied to a set of workflow instances.
/// </summary>
public class AlterationPlan : Entity
{
    /// <summary>
    /// The alterations to be applied.
    /// </summary>
    public ICollection<JsonObject> Alterations { get; set; } = new List<JsonObject>();

    /// <summary>
    /// The IDs of the workflow instances that this plan applies to.
    /// </summary>
    public AlterationWorkflowInstanceFilter WorkflowInstanceFilter { get; set; } = new();

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