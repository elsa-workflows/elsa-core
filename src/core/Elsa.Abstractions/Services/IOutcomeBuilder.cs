using System;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IOutcomeBuilder
    {
        IActivityBuilder Source { get; }
        string Outcome { get; }
        IActivityBuilder Then<T>(Action<T> setup = default, Action<IActivityBuilder> branch = default, string name = default) where T : class, IActivity;
        WorkflowDefinitionVersion Build();
        IConnectionBuilder Then(string activityName);
    }
}