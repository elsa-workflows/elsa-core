using Elsa.Client.Services;
using Elsa.Client.Webhooks.Services;

namespace Elsa.Client
{
    public class ElsaClient : IElsaClient
    {
        public ElsaClient(
            IActivitiesApi activities,
            IWorkflowDefinitionsApi workflowDefinitions,
            IWorkflowRegistryApi workflowRegistry,
            IWorkflowInstancesApi workflowInstances,
            IWebhookDefinitionsApi webhookDefinitions,
            IScriptingApi scriptingApi)
        {
            Activities = activities;
            WorkflowDefinitions = workflowDefinitions;
            WorkflowRegistry = workflowRegistry;
            WorkflowInstances = workflowInstances;
            WebhookDefinitions = webhookDefinitions;
            Scripting = scriptingApi;
        }

        public IActivitiesApi Activities { get; }
        public IWorkflowDefinitionsApi WorkflowDefinitions { get; }
        public IWorkflowRegistryApi WorkflowRegistry { get; }
        public IWorkflowInstancesApi WorkflowInstances { get; }
        public IWebhookDefinitionsApi WebhookDefinitions { get; }
        public IScriptingApi Scripting { get; }
    }
}