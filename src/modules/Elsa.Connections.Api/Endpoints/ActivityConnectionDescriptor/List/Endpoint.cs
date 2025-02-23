using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Models;

namespace Elsa.Connections.Api.Endpoints.ActivityConnectionDescriptor.List;

public class List(IConnectionDescriptorRegistry registry) : ElsaEndpointWithoutRequest<PagedListResponse<ConnectionDescriptor>>
{
    public override void Configure()
    {
        Get("/connection-configuration/descriptors");
        ConfigurePermissions($"{Constants.PermissionsNamespace}/descriptor:read");
    }

    public override Task<PagedListResponse<ConnectionDescriptor>> ExecuteAsync(CancellationToken ct)
    {
        var descriptors = registry.ListAll().ToList();
        return  Task.FromResult(new PagedListResponse<ConnectionDescriptor>(Page.Of(descriptors, descriptors.Count)) );
    }
}