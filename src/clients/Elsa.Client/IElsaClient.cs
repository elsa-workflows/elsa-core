using Elsa.Client.Services;
using Elsa.Client.Webhooks.Services;

namespace Elsa.Client
{
    public interface IElsaClient
    {
        IActivitiesApi Activities { get; }
        IWorkflowDefinitionsApi WorkflowDefinitions { get; }
        IWorkflowRegistryApi WorkflowRegistry { get; }
        IWorkflowInstancesApi WorkflowInstances { get; }
        IWebhookDefinitionsApi WebhookDefinitions { get; }
    }
}