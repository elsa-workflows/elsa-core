using Elsa.Client.Services;

namespace Elsa.Client
{
    internal class ElsaClient : IElsaClient
    {
        public ElsaClient(IWorkflowDefinitionsApi workflowDefinitionsApi)
        {
            WorkflowDefinitions = workflowDefinitionsApi;
        }

        public IWorkflowDefinitionsApi WorkflowDefinitions { get; }
    }
}