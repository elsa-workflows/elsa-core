using System;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface IActivityBuilder
    {
        IActivity Activity { get; }
        IActivityBuilder Add<T>(Action<T>? setup = default, string? name = default) where T : class, IActivity;
        IOutcomeBuilder When(string outcome);
        IActivityBuilder Then<T>(Action<T>? setup = default, Action<IActivityBuilder>? branch = default, string? name = default) where T : class, IActivity;
        IFlowchartBuilder Then(string activityName);
        IActivityBuilder Then(IActivityBuilder targetActivity);
        IActivity BuildActivity();
    }
}