using Elsa.Api.Client.Resources.ExternalAuthentication.Descriptors.Models;
using Refit;

namespace Elsa.Api.Client.Resources.ExternalAuthentication.Descriptors.Contracts;

/// <summary>
/// Client for the installed External Authentication extension catalogs.
/// </summary>
public interface IExternalAuthenticationDescriptorsApi
{
    [Get("/external-authentication/descriptors/adapters")]
    Task<ICollection<ExternalAuthenticationAdapterDescriptor>> ListAdaptersAsync(CancellationToken cancellationToken = default);

    [Get("/external-authentication/descriptors/policies")]
    Task<ICollection<ExternalAuthenticationPolicyDescriptor>> ListPoliciesAsync(CancellationToken cancellationToken = default);

    [Get("/external-authentication/descriptors/permission-sources")]
    Task<ICollection<ExternalAuthenticationPermissionGrantSourceDescriptor>> ListPermissionSourcesAsync(CancellationToken cancellationToken = default);

    [Get("/external-authentication/descriptors/permissions")]
    Task<ICollection<ExternalAuthenticationPermissionDescriptor>> ListPermissionsAsync(CancellationToken cancellationToken = default);
}
