using Elsa.Alterations.Core.Entities;

namespace Elsa.Alterations.Core.Filters;

/// <summary>
/// A filter for querying alteration plans.
/// </summary>
public class AlterationPlanFilter
{
    /// <summary>
    /// The ID of the plan.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Applies the filter to the specified query.
    /// </summary>
    public IQueryable<AlterationPlan> Apply(IQueryable<AlterationPlan> query)
    {
        if (!string.IsNullOrWhiteSpace(Id))
            query = query.Where(x => x.Id == Id);

        return query;
    }
}