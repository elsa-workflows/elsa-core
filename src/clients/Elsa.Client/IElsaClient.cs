using Elsa.Client.Services;

namespace Elsa.Client
{
    public interface IElsaClient
    {
        IActivitiesApi ActivitiesApi { get; }
        IWorkflowDefinitionsApi WorkflowDefinitions { get; }
    }
}