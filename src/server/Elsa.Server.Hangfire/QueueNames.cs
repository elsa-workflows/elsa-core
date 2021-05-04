namespace Elsa.Server.Hangfire
{
    internal static class QueueNames
    {
        public const string WorkflowDefinitions = "workflow-definitions";
        public const string WorkflowInstances = "workflow-instances";
        public const string CorrelatedWorkflows = "correlated-workflows";
    }
}