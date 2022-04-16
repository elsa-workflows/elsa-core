using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services.Models;

namespace Elsa.Services.WorkflowStorage;

public interface IWorkflowStorageService
{
    IWorkflowStorageProvider GetProviderByNameOrDefault(string? providerName = null);
    IEnumerable<IWorkflowStorageProvider> ListProviders();
    ValueTask SaveAsync(string? providerName, WorkflowStorageContext context, string key, object? value, CancellationToken cancellationToken = default);
    ValueTask<WorkflowInputReference> SaveAsync(WorkflowInput input, WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
    ValueTask<object?> LoadAsync(string? providerName, WorkflowStorageContext context, string key, CancellationToken cancellationToken = default);
    ValueTask<object?> LoadAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
    ValueTask DeleteAsync(string? providerName, WorkflowStorageContext context, string key, CancellationToken cancellationToken = default);
    ValueTask DeleteAsync(string? providerName, WorkflowStorageContext context, CancellationToken cancellationToken = default);
}

public static class WorkflowStorageExtensions
{
    public static async ValueTask UpdateInputAsync(this IWorkflowStorageService service, WorkflowInstance workflowInstance, WorkflowInput? workflowInput, CancellationToken cancellationToken = default)
    {
        if (workflowInput != null)
            workflowInstance!.Input = await service.SaveAsync(workflowInput, workflowInstance, cancellationToken);
    }
}