using Elsa.Providers.WorkflowStorage;

namespace Elsa.Services.WorkflowStorage
{
    public interface IWorkflowStorageService
    {
        IWorkflowStorageProvider GetProviderByNameOrDefault(string? name = null);
    }
}