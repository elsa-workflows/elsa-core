// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Workflows
{
    public class FinishedWorkflowModel
    {
        public string WorkflowInstanceId { get; set; } = default!;
        public object? WorkflowOutput { get; set; }
    }
}