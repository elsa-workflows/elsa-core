using System;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface IOutcomeBuilder
    {
        IActivityBuilder Source { get; }
        string Outcome { get; }
        IActivityBuilder Then<T>(Action<T>? setup = default, Action<IActivityBuilder>? branch = default, string? name = default) where T : class, IActivity;
        Activities.Containers.Flowchart Build();
        IConnectionBuilder Then(string activityName);
    }
}