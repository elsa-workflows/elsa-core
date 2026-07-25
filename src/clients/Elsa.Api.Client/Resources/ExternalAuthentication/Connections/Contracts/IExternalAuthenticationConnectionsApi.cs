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
    Task<ExternalAuthenticationConnection> DisableAsync(
        string connectionId,
        [Header("If-Match")] string ifMatch,
        [Query] bool confirmFinalLoginPathOverride = false,
        [Query] bool revokeActiveSessions = false,
        CancellationToken cancellationToken = default);

    [Delete("/external-authentication/connections/{connectionId}")]
    Task<ExternalAuthenticationConnection> ArchiveAsync(
        string connectionId,
        [Header("If-Match")] string ifMatch,
        [Query] bool confirmFinalLoginPathOverride = false,
        CancellationToken cancellationToken = default);

    [Post("/external-authentication/connections/{connectionId}/restore")]
    Task<ExternalAuthenticationConnection> RestoreAsync(string connectionId, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Post("/external-authentication/connections/{connectionId}/validate")]
    Task<ValidateExternalAuthenticationConnectionResponse> ValidateAsync(string connectionId, CancellationToken cancellationToken = default);

    [Post("/external-authentication/connections/{connectionId}/test")]
    Task<TestExternalAuthenticationConnectionResponse> TestAsync(string connectionId, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Post("/external-authentication/connections/{connectionId}/preview")]
    Task<InitiateExternalAuthenticationPreviewResponse> InitiatePreviewAsync(string connectionId, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Get("/external-authentication/previews/{previewHandle}")]
    Task<ExternalAuthenticationPreviewResult> GetPreviewResultAsync(string previewHandle, CancellationToken cancellationToken = default);

    [Put("/external-authentication/connections/{connectionId}/secret-bindings/{fieldName}/managed")]
    Task<ExternalAuthenticationConnection> ReplaceManagedSecretAsync(string connectionId, string fieldName, [Body] SaveManagedExternalAuthenticationSecretRequest request, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Delete("/external-authentication/connections/{connectionId}/secret-bindings/{fieldName}")]
    Task<ExternalAuthenticationConnection> RemoveSecretBindingAsync(string connectionId, string fieldName, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);
}
