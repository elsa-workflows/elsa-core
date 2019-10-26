using System;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityBuilder
    {
        string Id { get; set; }
        ActivityDefinition Activity { get; }
        IActivityBuilder StartWith<T>(Action<T> setup = default, string id = default) where T: class, IActivity;
        IActivityBuilder Add<T>(Action<T> setup = default, string id = null) where T : class, IActivity;
        IOutcomeBuilder When(string outcome);
        IActivityBuilder Then<T>(Action<T> setup = null, Action<IActivityBuilder> branch = null, string id = null) where T : class, IActivity;
        IActivityBuilder WithId(string id);
        IActivityBuilder WithDisplayName(string displayName);
        IActivityBuilder WithDescription(string description);
        IWorkflowBuilder Then(string activityId);
        IActivityBuilder Then(IActivityBuilder targetActivity);
        WorkflowDefinitionVersion Build();
        ActivityDefinition BuildActivity();
    }
}