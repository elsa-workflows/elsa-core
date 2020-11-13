using Elsa.Client.Services;

namespace Elsa.Client
{
    public class ElsaClient : IElsaClient
    {
        public ElsaClient(IActivitiesApi activitiesApi, IWorkflowDefinitionsApi workflowDefinitionsApi)
        {
            ActivitiesApi = activitiesApi;
            WorkflowDefinitions = workflowDefinitionsApi;
        }

        public IActivitiesApi ActivitiesApi { get; }
        public IWorkflowDefinitionsApi WorkflowDefinitions { get; }
    }
}