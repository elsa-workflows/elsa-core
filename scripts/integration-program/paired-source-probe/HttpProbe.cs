using System.Diagnostics.CodeAnalysis;
using Elsa.Studio.Contracts;
using Refit;

sealed class ProbeBackendClient(Uri url) : IBackendApiClientProvider
{
    public Uri Url => url;
    public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class => new(RestService.For<T>(url.ToString()));
}

sealed class SyntheticWorkflowContextProvider : Elsa.WorkflowContexts.Contracts.IWorkflowContextProvider
{
    public ValueTask<object?> LoadAsync(Elsa.Workflows.WorkflowExecutionContext context) => new((object?)"synthetic");
    public ValueTask SaveAsync(Elsa.Workflows.WorkflowExecutionContext context, object? value) => ValueTask.CompletedTask;
}
