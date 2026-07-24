using Elsa.Api.Client.Resources.ExternalAuthentication.Connections.Models;
using Elsa.Api.Client.Resources.ExternalAuthentication.Connections.Requests;
using Elsa.Api.Client.Resources.ExternalAuthentication.Connections.Responses;
using Refit;

namespace Elsa.Api.Client.Resources.ExternalAuthentication.Connections.Contracts;

/// <summary>
/// Client for administrator-managed external authentication connections.
/// </summary>
public interface IExternalAuthenticationConnectionsApi
{
    [Get("/external-authentication/connections")]
    Task<ListExternalAuthenticationConnectionsResponse> ListAsync([Query] ListExternalAuthenticationConnectionsRequest request, CancellationToken cancellationToken = default);

    [Get("/external-authentication/connections/{connectionId}")]
    Task<ExternalAuthenticationConnection> GetAsync(string connectionId, CancellationToken cancellationToken = default);

    [Post("/external-authentication/connections")]
    Task<ExternalAuthenticationConnection> CreateAsync([Body] SaveExternalAuthenticationConnectionRequest request, CancellationToken cancellationToken = default);

    [Put("/external-authentication/connections/{connectionId}")]
    Task<ExternalAuthenticationConnection> UpdateAsync(string connectionId, [Body] SaveExternalAuthenticationConnectionRequest request, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Post("/external-authentication/connections/{connectionId}/enable")]
    Task<ExternalAuthenticationConnection> EnableAsync(string connectionId, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Post("/external-authentication/connections/{connectionId}/disable")]
    Task<ExternalAuthenticationConnection> DisableAsync(string connectionId, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Delete("/external-authentication/connections/{connectionId}")]
    Task<ExternalAuthenticationConnection> ArchiveAsync(string connectionId, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Post("/external-authentication/connections/{connectionId}/restore")]
    Task<ExternalAuthenticationConnection> RestoreAsync(string connectionId, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Post("/external-authentication/connections/{connectionId}/validate")]
    Task<ValidateExternalAuthenticationConnectionResponse> ValidateAsync(string connectionId, CancellationToken cancellationToken = default);

    [Put("/external-authentication/connections/{connectionId}/secret-bindings/{fieldName}")]
    Task<ExternalAuthenticationConnection> ReplaceSecretBindingAsync(string connectionId, string fieldName, [Body] SaveExternalAuthenticationSecretBindingRequest request, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Delete("/external-authentication/connections/{connectionId}/secret-bindings/{fieldName}")]
    Task<ExternalAuthenticationConnection> RemoveSecretBindingAsync(string connectionId, string fieldName, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);
}
