using Elsa.Studio.ExternalAuthentication.Models;
using Refit;

namespace Elsa.Studio.ExternalAuthentication.Client;

/// <summary>
/// Studio-local management contract. It mirrors the server contract until the generated Elsa API client is available.
/// </summary>
public interface IExternalAuthenticationConnectionsApi
{
    [Get("/external-authentication/connections")]
    Task<ListConnectionsResponse> ListAsync(string? search = null, string? source = null, string? scope = null, string? adapterType = null, bool? enabled = null, bool? valid = null, bool? shadowed = null, bool? archived = null, string? cursor = null, int pageSize = 25, CancellationToken cancellationToken = default);

    [Get("/external-authentication/connections/{connectionId}")]
    Task<ConnectionDetail> GetAsync(string connectionId, CancellationToken cancellationToken = default);

    [Get("/external-authentication/descriptors/runtime")]
    Task<ExternalAuthenticationRuntimeDescriptor> GetRuntimeAsync(CancellationToken cancellationToken = default);

    [Get("/external-authentication/descriptors/adapters")]
    Task<ICollection<AdapterDescriptor>> GetAdaptersAsync(CancellationToken cancellationToken = default);

    [Get("/external-authentication/descriptors/permission-sources")]
    Task<ICollection<PermissionGrantSourceDescriptor>> GetPermissionSourcesAsync(CancellationToken cancellationToken = default);

    [Get("/external-authentication/descriptors/policies")]
    Task<ICollection<UnlinkedIdentityPolicyDescriptor>> GetPoliciesAsync(CancellationToken cancellationToken = default);

    [Get("/external-authentication/descriptors/user-matchers")]
    Task<ICollection<ExternalUserMatcherDescriptor>> GetUserMatchersAsync(CancellationToken cancellationToken = default);

    [Get("/external-authentication/descriptors/managed-secret-resolvers")]
    Task<ManagedSecretResolverCatalog> GetManagedSecretResolversAsync(CancellationToken cancellationToken = default);

    [Get("/external-authentication/descriptors/permissions")]
    Task<ICollection<PermissionDescriptor>> GetPermissionsAsync(CancellationToken cancellationToken = default);

    [Post("/external-authentication/connections")]
    Task<ConnectionDetail> CreateAsync([Body] ConnectionMutation request, CancellationToken cancellationToken = default);

    [Put("/external-authentication/connections/{connectionId}")]
    Task<ConnectionDetail> UpdateAsync(string connectionId, [Body] ConnectionMutation request, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Post("/external-authentication/connections/{connectionId}/enable")]
    Task EnableAsync(string connectionId, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Post("/external-authentication/connections/{connectionId}/disable")]
    Task DisableAsync(string connectionId, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Delete("/external-authentication/connections/{connectionId}")]
    Task ArchiveAsync(string connectionId, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Post("/external-authentication/connections/{connectionId}/restore")]
    Task RestoreAsync(string connectionId, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Post("/external-authentication/connections/{connectionId}/validate")]
    Task<ConnectionValidationResult> ValidateAsync(string connectionId, CancellationToken cancellationToken = default);

    [Put("/external-authentication/connections/{connectionId}/secret-bindings/{fieldName}/managed")]
    Task<ConnectionDetail> ReplaceManagedSecretAsync(string connectionId, string fieldName, [Body] ManagedSecretMutation request, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Delete("/external-authentication/connections/{connectionId}/secret-bindings/{fieldName}")]
    Task RemoveSecretBindingAsync(string connectionId, string fieldName, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);
}
