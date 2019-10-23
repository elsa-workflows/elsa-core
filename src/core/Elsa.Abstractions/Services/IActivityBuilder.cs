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
        IWorkflowBuilder Then(string activityId);
        WorkflowDefinitionVersion Build();
        ActivityDefinition BuildActivity();
    }
}