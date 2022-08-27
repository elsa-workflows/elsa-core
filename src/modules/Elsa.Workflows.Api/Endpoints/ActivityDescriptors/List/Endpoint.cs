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
        Get("/descriptors/activities");
        
        if (!EndpointSecurityOptions.SecurityIsEnabled)
        {
            AllowAnonymous();
        }
        else
        {
            Roles("Admin", "Reader");    
            Permissions("*", "list:activity-descriptors");
        }
    }

    public override Task<Response> ExecuteAsync(CancellationToken cancellationToken)
    {
        var descriptors = _registry.ListAll().ToList();
        var response = new Response(descriptors);

        return Task.FromResult(response);
    }
}