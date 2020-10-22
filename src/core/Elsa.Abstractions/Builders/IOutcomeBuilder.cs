using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface IOutcomeBuilder : IBuilder
    {
        IWorkflowBuilder WorkflowBuilder { get; }
        IActivityBuilder Source { get; }
        string? Outcome { get; }
        IConnectionBuilder Then(string activityName);
        IWorkflowBlueprint Build();
    }
}