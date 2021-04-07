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
            IWebhookDefinitionsApi webhookDefinitions)
        {
            Activities = activities;
            WorkflowDefinitions = workflowDefinitions;
            WorkflowRegistry = workflowRegistry;
            WorkflowInstances = workflowInstances;
            WebhookDefinitions = webhookDefinitions;
        }

        public IActivitiesApi Activities { get; }
        public IWorkflowDefinitionsApi WorkflowDefinitions { get; }
        public IWorkflowRegistryApi WorkflowRegistry { get; }
        public IWorkflowInstancesApi WorkflowInstances { get; }
        public IWebhookDefinitionsApi WebhookDefinitions { get; }
    }
}