using System.Reflection;
using Elsa.Abstractions;
using Elsa.ExternalAuthentication.Features;
using Elsa.ExternalAuthentication.Permissions;

namespace Elsa.ExternalAuthentication.Endpoints.Runtime;

/// <summary>Publishes safe runtime metadata used by management clients to diagnose backend/client contract mismatches.</summary>
internal sealed class GetExternalAuthenticationRuntimeDescriptor : ElsaEndpointWithoutRequest<ExternalAuthenticationRuntimeDescriptor>
{
    public const int ManagementContractVersion = 1;

    public override void Configure()
    {
        Get("/external-authentication/descriptors/runtime");
        ConfigurePermissions(ExternalAuthenticationPermissions.ConnectionsRead);
    }

    public override Task<ExternalAuthenticationRuntimeDescriptor> ExecuteAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(ExternalAuthenticationFeature).Assembly;
        var productVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
            ?? productVersion;

        return Task.FromResult(new ExternalAuthenticationRuntimeDescriptor(ManagementContractVersion, productVersion, informationalVersion));
    }
}

internal sealed record ExternalAuthenticationRuntimeDescriptor(int ManagementContractVersion, string ProductVersion, string InformationalVersion);
