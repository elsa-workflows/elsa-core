using Elsa.Api.Client.Resources.Alterations.Models;

namespace Elsa.Api.Client.Resources.Alterations.Responses;

/// <summary>
/// The response from the "Get" alteration plan endpoint
/// </summary>
public class GetAlterationPlanResponse
{
    /// <summary>
    /// The alteration plan mathching the provided ID
    /// </summary>
    public AlterationPlan Plan { get; set; } = new();
    
    /// <summary>
    /// The list of jobs that exist for that AlterationPlan
    /// </summary>
    public ICollection<AlterationJob> Jobs { get; set; } = new List<AlterationJob>();
}