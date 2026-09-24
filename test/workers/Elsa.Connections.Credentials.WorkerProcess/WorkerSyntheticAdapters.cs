using System.Net.Http.Json;
using System.Security.Claims;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;

namespace Elsa.Connections.Credentials.WorkerProcess;

internal sealed class WorkerConnectionUseAuthorizer : IConnectionUseAuthorizer
{
    public Task<bool> AuthorizeAsync(ConnectionUseRequest request, CancellationToken cancellationToken = default)
    {
        var settings = WorkerSettings.FromEnvironment();
        var identity = request.Principal.Identity;
        if (request.Kind == ConnectionUseKind.BackgroundSystem)
        {
            var allowedSystemScope = string.Equals(Environment.GetEnvironmentVariable("ELSA_TEST_ALLOW_BACKGROUND"), "true", StringComparison.OrdinalIgnoreCase);
            var allowed = allowedSystemScope && identity?.IsAuthenticated == true && identity.AuthenticationType == "Elsa.Connections.Server" &&
                          request.Principal.HasClaim("elsa:identity-kind", "system") &&
                          request.TenantId == settings.TenantId && request.EnvironmentId == settings.EnvironmentId &&
                          request.Purpose is "use" or "manage:reconcile";
            return Task.FromResult(allowed);
        }

        var inScope = request.TenantId == request.Principal.FindFirstValue("tenant") &&
                      request.EnvironmentId == request.Principal.FindFirstValue("environment");
        var hasPermission = request.Purpose == "use"
            ? request.Principal.HasClaim("permission", "connections.use")
            : request.Principal.HasClaim("permission", "connections.manage");
        var validConnectScope = request.Purpose != "manage:connect" || string.IsNullOrEmpty(request.ConnectionId);
        var allowedHuman = request.Kind == ConnectionUseKind.Human && identity?.IsAuthenticated == true &&
                           identity.AuthenticationType == "synthetic-process" && inScope && hasPermission && validConnectScope;
        return Task.FromResult(allowedHuman);
    }
}

internal sealed class WorkerBindingUseAuthorizer : IConnectionCredentialBindingUseAuthorizer
{
    public Task<bool> AuthorizeAsync(ConnectionCredentialBindingUseRequest request, CancellationToken cancellationToken = default)
    {
        var settings = WorkerSettings.FromEnvironment();
        var allowed = string.Equals(Environment.GetEnvironmentVariable("ELSA_TEST_ALLOW_BINDING_USE"), "true", StringComparison.OrdinalIgnoreCase) &&
                      request.TenantId == settings.TenantId && request.EnvironmentId == settings.EnvironmentId &&
                      !string.IsNullOrWhiteSpace(request.LogicalBindingId) && !string.IsNullOrWhiteSpace(request.ConnectionId) &&
                      !string.IsNullOrWhiteSpace(request.WorkflowInstanceId);
        return Task.FromResult(allowed);
    }
}

internal sealed class WorkerBindingManagementAuthorizer : IConnectionCredentialBindingManagementAuthorizer
{
    public Task<bool> AuthorizeAsync(ClaimsPrincipal principal, ConnectionCredentialBindingManagementRequest request, CancellationToken cancellationToken = default)
    {
        var allowed = principal.Identity?.IsAuthenticated == true && principal.Identity.AuthenticationType == "synthetic-process" &&
                      principal.HasClaim("permission", "connections.manage") &&
                      request.TenantId == principal.FindFirstValue("tenant") &&
                      request.EnvironmentId == principal.FindFirstValue("environment");
        return Task.FromResult(allowed);
    }
}

internal sealed class SyntheticHttpCredentialProvider : IConnectionCredentialProvider, IConnectionOffboardingProvider
{
    private readonly HttpClient _httpClient = new();
    private readonly Uri _baseAddress = new(Required("ELSA_TEST_PROVIDER_ADDRESS"));
    private readonly TimeProvider _timeProvider = WorkerSettings.FromEnvironment().TimeProvider;

    public bool SupportsStableOperationIdIdempotency(ConnectionOffboardingOperationKind kind) => kind switch
    {
        ConnectionOffboardingOperationKind.TokenPairRevocation => IsEnabled("ELSA_TEST_REVOCATION_IDEMPOTENT"),
        ConnectionOffboardingOperationKind.InstallationUninstall => IsEnabled("ELSA_TEST_UNINSTALL_IDEMPOTENT"),
        _ => false
    };

    public async Task<CredentialMaterial> RefreshAsync(string providerId, string accountId, string refreshToken, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            new Uri(_baseAddress, "refresh"),
            new SyntheticRefreshRequest(providerId, accountId, refreshToken),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SyntheticRefreshResponse>(cancellationToken: cancellationToken)
                     ?? throw new InvalidOperationException("synthetic_refresh_response_missing");
        return new CredentialMaterial(result.AccessToken, result.RefreshToken, _timeProvider.GetUtcNow().AddSeconds(result.ExpiresInSeconds));
    }

    public async Task<ConnectionOffboardingProviderResult> RevokeTokenPairAsync(
        string providerId,
        string providerAccountId,
        string operationId,
        CredentialMaterial credentials,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            new Uri(_baseAddress, "revoke"),
            new SyntheticOffboardingRequest(providerId, providerAccountId, operationId, credentials.AccessToken, credentials.RefreshToken),
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return ConnectionOffboardingProviderResult.UnknownOutcome;
        }

        await ProcessBoundary.PauseAsync("offboarding-provider-success");
        return ConnectionOffboardingProviderResult.Succeeded;
    }

    public async Task<ConnectionOffboardingProviderResult> UninstallInstallationAsync(
        string providerId,
        string providerAccountId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            new Uri(_baseAddress, "uninstall"),
            new SyntheticUninstallRequest(providerId, providerAccountId, operationId),
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return ConnectionOffboardingProviderResult.UnknownOutcome;
        }

        await ProcessBoundary.PauseAsync("offboarding-provider-success");
        return ConnectionOffboardingProviderResult.Succeeded;
    }

    private static bool IsEnabled(string name) =>
        string.Equals(Environment.GetEnvironmentVariable(name), "true", StringComparison.OrdinalIgnoreCase);

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : throw new InvalidOperationException("required_environment_missing");
}

internal sealed record SyntheticRefreshRequest(string ProviderId, string AccountId, string RefreshToken);
internal sealed record SyntheticRefreshResponse(string AccessToken, string RefreshToken, int ExpiresInSeconds);
internal sealed record SyntheticOffboardingRequest(string ProviderId, string ProviderAccountId, string OperationId, string AccessToken, string RefreshToken);
internal sealed record SyntheticUninstallRequest(string ProviderId, string ProviderAccountId, string OperationId);
