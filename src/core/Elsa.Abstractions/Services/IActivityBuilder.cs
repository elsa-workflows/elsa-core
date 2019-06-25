using System;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityBuilder
    {
        string Id { get; set; }
        ActivityDefinition Activity { get; }
        IActivityBuilder Add<T>(Action<T> setupActivity, string id = null) where T : class, IActivity;
        IOutcomeBuilder When(string outcome);
        IActivityBuilder Then<T>(Action<T> setup = null, Action<IActivityBuilder> branch = null, string id = null) where T : class, IActivity;
        IConnectionBuilder Then(string activityId);
        WorkflowDefinition Build();
        ActivityDefinition BuildActivity();
    }
}