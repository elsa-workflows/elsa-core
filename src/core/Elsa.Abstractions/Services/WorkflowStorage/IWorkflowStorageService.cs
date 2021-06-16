using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Providers.WorkflowStorage;

namespace Elsa.Services.WorkflowStorage
{
    public interface IWorkflowStorageService
    {
        IWorkflowStorageProvider GetProviderByNameOrDefault(string? providerName = null);
        IEnumerable<IWorkflowStorageProvider> ListProviders();
        ValueTask SaveAsync(string? providerName, WorkflowStorageContext context, string key, object? value, CancellationToken cancellationToken = default);
        ValueTask<object?> LoadAsync(string? providerName, WorkflowStorageContext context, string key, CancellationToken cancellationToken = default);
        ValueTask DeleteAsync(string? providerName, WorkflowStorageContext context, string key, CancellationToken cancellationToken = default);
        ValueTask DeleteAsync(string? providerName, WorkflowStorageContext context, CancellationToken cancellationToken = default);
    }
}