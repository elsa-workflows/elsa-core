using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions;
using Elsa.Workflows.Core.Services;
using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.StorageDrivers.List;

public class List : ElsaEndpointWithoutRequest<Response>
{
    private readonly IStorageDriverManager _registry;

    public List(IStorageDriverManager registry)
    {
        _registry = registry;
    }

    public override void Configure()
    {
        Get("/descriptors/storage-drivers");
        ConfigurePermissions("read:storage-drivers");
    }

    public override Task<Response> ExecuteAsync(CancellationToken ct)
    {
        var drivers = _registry.List();
        var descriptors = drivers.Select(x => new StorageDriverDescriptor(x.Id, x.DisplayName)).ToList();
        var response = new Response(descriptors);

        return Task.FromResult(response);
    }
}