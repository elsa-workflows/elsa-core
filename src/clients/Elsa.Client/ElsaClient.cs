using Elsa.Client.Services;

namespace Elsa.Client
{
    public class ElsaClient : IElsaClient
    {
        public ElsaClient(IWorkflowDefinitionsApi workflowDefinitionsApi)
        {
            WorkflowDefinitions = workflowDefinitionsApi;
        }

        public IWorkflowDefinitionsApi WorkflowDefinitions { get; }
    }
}