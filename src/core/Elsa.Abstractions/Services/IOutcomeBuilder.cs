using System;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IOutcomeBuilder
    {
        IActivityBuilder Source { get; }
        string Outcome { get; }
        IActivityBuilder Then<T>(Action<T> setup = default, string id = default) where T : class, IActivity;
        WorkflowDefinition Build();
        IConnectionBuilder Then(string activityId);
    }
}