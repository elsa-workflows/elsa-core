using System.Threading.Tasks;
using Elsa.DataMasking.Core.Contracts;
using Elsa.DataMasking.Core.Models;

namespace Elsa.DataMasking.Core.Abstractions;

/// <summary>
/// A base class for activity state filters.
/// </summary>
public abstract class ActivityStateFilter : IActivityStateFilter
{
    async ValueTask IActivityStateFilter.ApplyAsync(ActivityStateFilterContext context) => await ApplyAsync(context);

    protected virtual Task ApplyAsync(ActivityStateFilterContext context)
    {
        Apply(context);
        return Task.CompletedTask;
    }

    protected virtual void Apply(ActivityStateFilterContext context)
    {
    }
}