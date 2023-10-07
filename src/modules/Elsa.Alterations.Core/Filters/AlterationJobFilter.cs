using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;

namespace Elsa.Alterations.Core.Filters;

/// <summary>
/// A filter for querying alteration jobs.
/// </summary>
public class AlterationJobFilter
{
    /// <summary>
    /// The ID of the job.
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// The ID of the plan.
    /// </summary>
    public string? PlanId { get; set; }
    
    /// <summary>
    /// The status of the job.
    /// </summary>
    public AlterationJobStatus? Status { get; set; }

    /// <summary>
    /// The statuses of the job to match.
    /// </summary>
    public ICollection<AlterationJobStatus>? Statuses { get; set; }

    /// <summary>
    /// Applies the filter to the specified query.
    /// </summary>
    public IQueryable<AlterationJob> Apply(IQueryable<AlterationJob> query)
    {
        if (!string.IsNullOrWhiteSpace(Id))
            query = query.Where(x => x.Id == Id);
        
        if (!string.IsNullOrWhiteSpace(PlanId))
            query = query.Where(x => x.PlanId == PlanId);
        
        if (Status != null)
            query = query.Where(x => x.Status == Status);
        
        if (Statuses != null)
            query = query.Where(x => Statuses.Contains(x.Status));

        return query;
    }
}