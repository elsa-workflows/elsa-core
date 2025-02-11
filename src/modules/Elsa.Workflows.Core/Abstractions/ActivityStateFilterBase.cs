using JetBrains.Annotations;

namespace Elsa.Workflows;

[UsedImplicitly]
public abstract class ActivityStateFilterBase : IActivityStateFilter
{
    protected virtual Task<ActivityStateFilterResult> OnExecuteAsync(ActivityStateFilterContext context)
    {
        var result = OnExecute(context);
        return Task.FromResult(result);
    }

    protected virtual ActivityStateFilterResult OnExecute(ActivityStateFilterContext context)
    {
        return Pass();
    }
    
    protected ActivityStateFilterResult Pass() => ActivityStateFilterResult.Pass();
    protected ActivityStateFilterResult Filtered(string filteredValue) => ActivityStateFilterResult.Filtered(filteredValue);

    Task<ActivityStateFilterResult> IActivityStateFilter.ExecuteAsync(ActivityStateFilterContext context)
    {
        return OnExecuteAsync(context);
    }
}