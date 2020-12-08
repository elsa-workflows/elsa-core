using Elsa.Client.Services;

namespace Elsa.Client
{
    public class ElsaClient : IElsaClient
    {
        public ElsaClient(IActivitiesApi activities, IWorkflowDefinitionsApi workflowDefinitions, IWorkflowRegistryApi workflowRegistry, IWorkflowInstanceApi workflowInstances)
        {
            Activities = activities;
            WorkflowDefinitions = workflowDefinitions;
            WorkflowRegistry = workflowRegistry;
            WorkflowInstances = workflowInstances;
        }

        public IActivitiesApi Activities { get; }
        public IWorkflowDefinitionsApi WorkflowDefinitions { get; }
        public IWorkflowRegistryApi WorkflowRegistry { get; }
        public IWorkflowInstanceApi WorkflowInstances { get; }
    }
}