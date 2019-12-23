using System;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityBuilder
    {
        IActivity Activity { get; }
        IActivityBuilder StartWith<T>(Action<T>? setup = default, string? name = default) where T: class, IActivity;
        IActivityBuilder Add<T>(Action<T>? setup = default, string? name = default) where T : class, IActivity;
        IOutcomeBuilder When(string outcome);
        IActivityBuilder Then<T>(Action<T>? setup = default, Action<IActivityBuilder>? branch = default, string? name = default) where T : class, IActivity;
        IActivityBuilder WithName(string name);
        IActivityBuilder WithDisplayName(string displayName);
        IActivityBuilder WithDescription(string description);
        IWorkflowBuilder Then(string activityName);
        IActivityBuilder Then(IActivityBuilder targetActivity);
        WorkflowBlueprint Build();
        IActivity BuildActivity();
    }
}