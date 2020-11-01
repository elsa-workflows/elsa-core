using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface IOutcomeBuilder : IBuilder
    {
        ICompositeActivityBuilder WorkflowBuilder { get; }
        IActivityBuilder Source { get; }
        string? Outcome { get; }
        IConnectionBuilder Then(string activityName);
        IWorkflowBlueprint Build();
    }
}