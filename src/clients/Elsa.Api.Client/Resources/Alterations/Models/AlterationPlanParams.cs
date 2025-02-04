using Elsa.Api.Client.Resources.Alterations.Contracts;

namespace Elsa.Api.Client.Resources.Alterations.Models;

/// <summary>
/// Represents the execution of an alteration plan against a set of workflow instances defined by the given filter
/// </summary>
public class AlterationPlanParams
{
    /// <summary>
    /// The unique identifier for the alteration plan. If not specified, a new ID will be generated.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The alterations to be applied.
    /// </summary>
    public ICollection<IAlteration> Alterations { get; set; } = new List<IAlteration>();
    
    /// <summary>
    /// The IDs of the workflow instances that this plan applies to.
    /// </summary>
    public AlterationWorkflowInstanceFilter Filter { get; set; } = new();
}