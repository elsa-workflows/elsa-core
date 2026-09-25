using Elsa.Studio.ExternalAuthentication.Models;
using Refit;

namespace Elsa.Studio.ExternalAuthentication.Client;

/// <summary>Studio operations contract for redacted diagnostics, preview, recovery, and session administration.</summary>
public interface IExternalAuthenticationOperationsApi
{
    [Post("/external-authentication/connections/{connectionId}/test")]
    Task<ConnectionTestResult> TestAsync(string connectionId, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Post("/external-authentication/connections/{connectionId}/preview")]
    Task<PreviewInitiation> InitiatePreviewAsync(string connectionId, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    [Get("/external-authentication/previews/{previewHandle}")]
    Task<PreviewResultDocument> GetPreviewResultAsync(string previewHandle, CancellationToken cancellationToken = default);

    [Get("/external-authentication/sessions")]
    Task<ListExternalAuthenticationSessionsResponse> ListSessionsAsync(
        string? userId = null,
        string? connectionId = null,
        string? status = null,
        string? cursor = null,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    [Delete("/external-authentication/sessions/{sessionId}")]
    Task RevokeSessionAsync(string sessionId, [Body] RevokeExternalAuthenticationSessionRequest request, CancellationToken cancellationToken = default);

    [Post("/external-authentication/connections/{connectionId}/disable")]
    Task DisableWithRecoveryOverrideAsync(
        string connectionId,
        [Header("If-Match")] string ifMatch,
        [Query] bool confirmFinalLoginPathOverride,
        [Query] bool revokeActiveSessions = false,
        CancellationToken cancellationToken = default);
}
