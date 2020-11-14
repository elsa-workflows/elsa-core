using Elsa.Client.Services;

namespace Elsa.Client
{
    public class ElsaClient : IElsaClient
    {
        public ElsaClient(IActivitiesApi activitiesApi, IWorkflowDefinitionsApi workflowDefinitionsApi)
        {
            Activities = activitiesApi;
            WorkflowDefinitions = workflowDefinitionsApi;
        }

        public IActivitiesApi Activities { get; }
        public IWorkflowDefinitionsApi WorkflowDefinitions { get; }
    }
}