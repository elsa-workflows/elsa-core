using Elsa.Abstractions;
using Elsa.Models;
using Elsa.WorkflowContexts.Contracts;
using JetBrains.Annotations;

namespace Elsa.WorkflowContexts.Endpoints.ProviderTypes.List;

[UsedImplicitly]
internal class List : ElsaEndpointWithoutRequest<ListResponse<WorkflowContextProviderDescriptor>>
{
    private readonly ICollection<WorkflowContextProviderDescriptor> _providerDescriptors;

    public List(IEnumerable<IWorkflowContextProvider> providers)
    {
        _providerDescriptors = providers
            .Select(x => x.GetType())
            .Select(x => new WorkflowContextProviderDescriptor(x.Name.Replace("WorkflowContextProvider", ""), x))
            .ToList();
    }

    public override void Configure()
    {
        Get("/workflow-contexts/provider-descriptors");
        ConfigurePermissions("read:workflow-context-provider-descriptors");
    }

    public override Task<ListResponse<WorkflowContextProviderDescriptor>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new ListResponse<WorkflowContextProviderDescriptor>(_providerDescriptors));
    }
}

[UsedImplicitly]
internal record Response(ICollection<WorkflowContextProviderDescriptor> Descriptors, int Count);

[UsedImplicitly]
internal record WorkflowContextProviderDescriptor(string Name, Type Type);