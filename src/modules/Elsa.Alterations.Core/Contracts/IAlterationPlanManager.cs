using Elsa.Alterations.Core.Entities;

namespace Elsa.Alterations.Core.Contracts;

public interface IAlterationPlanManager
{
    Task<AlterationPlan?> GetPlanAsync(string planId, CancellationToken cancellationToken = default);
    Task<bool> GetIsAllJobsCompletedAsync(string planId, CancellationToken cancellationToken = default);
    Task CompletePlanAsync(AlterationPlan plan, CancellationToken cancellationToken = default);
}