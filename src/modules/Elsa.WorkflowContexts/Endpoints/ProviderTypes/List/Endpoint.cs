using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.WorkflowContexts.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;

namespace Elsa.WorkflowContexts.Endpoints.ProviderTypes.List;

[PublicAPI]
internal class List : ElsaEndpointWithoutRequest<Response>
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

    public override Task<Response> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new Response(_providerDescriptors, _providerDescriptors.Count));
    }
}

[PublicAPI]
internal record Response(ICollection<WorkflowContextProviderDescriptor> Descriptors, int Count);

[PublicAPI]
internal record WorkflowContextProviderDescriptor(string Name, Type Type);