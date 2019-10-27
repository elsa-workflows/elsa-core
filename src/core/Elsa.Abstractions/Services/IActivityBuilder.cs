using System;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityBuilder
    {
        string Id { get; set; }
        string Name { get; set; }
        ActivityDefinition Activity { get; }
        IActivityBuilder StartWith<T>(Action<T> setup = default, string name = null) where T: class, IActivity;
        IActivityBuilder Add<T>(Action<T> setup = default, string name = null) where T : class, IActivity;
        IOutcomeBuilder When(string outcome);
        IActivityBuilder Then<T>(Action<T> setup = null, Action<IActivityBuilder> branch = null, string name = null) where T : class, IActivity;
        IActivityBuilder WithName(string name);
        IActivityBuilder WithDisplayName(string displayName);
        IActivityBuilder WithDescription(string description);
        IWorkflowBuilder Then(string activityName);
        IActivityBuilder Then(IActivityBuilder targetActivity);
        WorkflowDefinitionVersion Build();
        ActivityDefinition BuildActivity();
    }
}