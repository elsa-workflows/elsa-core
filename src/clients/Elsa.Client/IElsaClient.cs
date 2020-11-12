using Elsa.Client.Services;

namespace Elsa.Client
{
    public interface IElsaClient
    {
        IWorkflowDefinitionsApi WorkflowDefinitions { get; }
    }
}