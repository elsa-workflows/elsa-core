using Elsa.Client.Services;

namespace Elsa.Client
{
    public class ElsaClient : IElsaClient
    {
        public ElsaClient(IActivitiesApi activitiesApi, IWorkflowDefinitionsApi workflowDefinitionsApi, IWorkflowRegistryApi workflowRegistry)
        {
            Activities = activitiesApi;
            WorkflowDefinitions = workflowDefinitionsApi;
            WorkflowRegistry = workflowRegistry;
        }

        public IActivitiesApi Activities { get; }
        public IWorkflowDefinitionsApi WorkflowDefinitions { get; }
        public IWorkflowRegistryApi WorkflowRegistry { get; }
    }
}