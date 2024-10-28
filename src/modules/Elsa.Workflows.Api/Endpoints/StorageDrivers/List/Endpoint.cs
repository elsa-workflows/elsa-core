using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Elsa.Abstractions;
using Elsa.Extensions;

namespace Elsa.Workflows.Api.Endpoints.StorageDrivers.List;

/// <summary>
/// Returns a list of registered <see cref="IStorageDriver"/> implementations.
/// </summary>
public class List : ElsaEndpointWithoutRequest<Response>
{
    private readonly IStorageDriverManager _registry;

    /// <inheritdoc />
    public List(IStorageDriverManager registry)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/descriptors/storage-drivers");
        ConfigurePermissions("read:*", "read:storage-drivers");
    }

    /// <inheritdoc />
    public override Task<Response> ExecuteAsync(CancellationToken ct)
    {
        var drivers = _registry.List();
        var descriptors = drivers.Select(FromDriver).OrderByDescending(x => x.Priority).ToList();
        var response = new Response(descriptors);

        return Task.FromResult(response);
    }

    private static StorageDriverDescriptor FromDriver(IStorageDriver driver)
    {
        var type = driver.GetType();
        var deprecated = type.GetCustomAttribute<ObsoleteAttribute>() != null;
        var displayName = type.GetCustomAttribute<DisplayAttribute>()?.Name ?? type.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? type.Name.Replace("StorageDriver", "");
        var priority = driver.Priority;
        return new StorageDriverDescriptor(type.GetSimpleAssemblyQualifiedName(), displayName, priority, deprecated);
    }
}