namespace Elsa.Builders
{
    public interface IConnectionBuilder
    {
        IWorkflowBuilder WorkflowBuilder { get; }
        IActivityBuilder Source { get; }
        IActivityBuilder Target{ get; }
        string Outcome { get; }
    }
}