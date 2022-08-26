using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Services;
using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptors.List;

public class List : EndpointWithoutRequest<Response>
{
    private readonly IActivityRegistry _registry;

    public List(IActivityRegistry registry, IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _registry = registry;
    }

    public override void Configure()
    {
        Policies(Constants.PolicyName);
        Roles("Admin");
        Permissions("all", "list:activity-descriptors");
    }

    public override Task<Response> ExecuteAsync(CancellationToken cancellationToken)
    {
        var descriptors = _registry.ListAll().ToList();
        var response = new Response(descriptors);

        return Task.FromResult(response);
    }
}