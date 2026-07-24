using Elsa.Abstractions;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Endpoints.Descriptors;

/// <summary>Publishes only startup-installed, deployment-allowed extension metadata for generic management editors.</summary>
internal sealed class ListAdapterDescriptors(IExternalAuthenticationAdapterRegistry registry, IOptions<ExternalAuthenticationOptions> options) : ElsaEndpointWithoutRequest<IReadOnlyCollection<ExternalAuthenticationAdapterDescriptor>>
{
    public override void Configure()
    {
        Get("/external-authentication/descriptors/adapters");
        ConfigurePermissions(ExternalAuthenticationPermissions.ConnectionsRead);
    }

    public override Task<IReadOnlyCollection<ExternalAuthenticationAdapterDescriptor>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var allowed = options.Value.AllowedAdapterTypes;
        IReadOnlyCollection<ExternalAuthenticationAdapterDescriptor> response = registry.ListDescriptors()
            .Where(x => allowed.Count == 0 || allowed.Contains(x.Type, StringComparer.Ordinal))
            .OrderBy(x => x.Type, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult(response);
    }
}

internal sealed class ListPolicyDescriptors(IUnlinkedIdentityPolicyRegistry registry) : ElsaEndpointWithoutRequest<IReadOnlyCollection<UnlinkedIdentityPolicyDescriptor>>
{
    public override void Configure()
    {
        Get("/external-authentication/descriptors/policies");
        ConfigurePermissions(ExternalAuthenticationPermissions.ConnectionsRead);
    }

    public override Task<IReadOnlyCollection<UnlinkedIdentityPolicyDescriptor>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(registry.ListDescriptors());
    }
}

internal sealed class ListPermissionSourceDescriptors(IPermissionGrantSourceRegistry registry) : ElsaEndpointWithoutRequest<IReadOnlyCollection<PermissionGrantSourceDescriptor>>
{
    public override void Configure()
    {
        Get("/external-authentication/descriptors/permission-sources");
        ConfigurePermissions(ExternalAuthenticationPermissions.ConnectionsRead);
    }

    public override Task<IReadOnlyCollection<PermissionGrantSourceDescriptor>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(registry.ListDescriptors());
    }
}

internal sealed class ListPermissionDescriptors(IPermissionDescriptorRegistry registry) : ElsaEndpointWithoutRequest<IReadOnlyCollection<PermissionDescriptor>>
{
    public override void Configure()
    {
        Get("/external-authentication/descriptors/permissions");
        ConfigurePermissions(ExternalAuthenticationPermissions.ConnectionsRead);
    }

    public override Task<IReadOnlyCollection<PermissionDescriptor>> ExecuteAsync(CancellationToken cancellationToken) => Task.FromResult(registry.List());
}
