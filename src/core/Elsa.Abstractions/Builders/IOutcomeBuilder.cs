using System;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface IOutcomeBuilder : IBuilder
    {
        ICompositeActivityBuilder WorkflowBuilder { get; }
        IActivityBuilder Source { get; }
        string? Outcome { get; }
        IConnectionBuilder ThenNamed(string activityName);
        IConnectionBuilder Then(IActivityBuilder targetActivity, Action<IActivityBuilder>? branch = default);
        IWorkflowBlueprint Build();
    }
}