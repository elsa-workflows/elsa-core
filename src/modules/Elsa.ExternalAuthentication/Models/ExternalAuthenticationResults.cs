namespace Elsa.ExternalAuthentication.Models;

public sealed record ConnectionValidationResult(bool IsValid, IReadOnlyCollection<ConnectionValidationError> Errors, IReadOnlyCollection<string> Warnings);
public sealed record ConnectionValidationError(string Field, string Code, string Message);
public sealed record ConnectionTestResult(ConnectionObservationStatus Status, string Category, string Summary, IReadOnlyCollection<string> Warnings);
public sealed record ExternalAuthorizationRequest(Uri NavigationUri, byte[] ProtectedAdapterState);
public sealed record ExternalAuthenticationResult(ExternalIdentity Identity, IReadOnlyDictionary<string, IReadOnlyCollection<string>> ProjectedClaims, IReadOnlyCollection<string> Warnings);
public sealed record ExternalLogoutRequest(Uri NavigationUri, byte[] ProtectedAdapterState);
public sealed record PermissionGrant(string Permission, string SourceType, string SourceReference);
public sealed record PermissionGrantWarning(string Code, string Message);
public sealed record PermissionGrantResult(IReadOnlyCollection<PermissionGrant> Grants, IReadOnlyCollection<PermissionGrantWarning> Warnings);
public sealed record PermissionDelegationResult(bool IsAuthorized, IReadOnlyCollection<string> UnauthorizedPermissions);
public sealed record UserCreationProposal(string UserNamePrefix, string? DisplayName = null);
public sealed record ExternalIdentityResolution(string UserId, bool WasProvisioned);
public sealed record ProvisioningRequest(string TenantId, string ConnectionId, ExternalIdentity Identity, UserCreationProposal? Proposal = null, string? ExistingUserId = null);
public sealed record ProvisioningResult(string UserId, ExternalIdentityLink Link, bool WasCreated, bool WasLinkCreated = false);
public sealed record ExternalTokenResponse(string AccessToken, string TokenType, long ExpiresIn, string RefreshToken, long RefreshExpiresIn, long ExternalSessionExpiresIn);
public sealed record AdapterSettingsMigrationResult(int SettingsVersion, System.Text.Json.JsonElement Settings, bool WasMigrated);

public abstract record TakeResult<T>
{
    private TakeResult() { }
    public sealed record Taken(T Value) : TakeResult<T>;
    public sealed record NotFound : TakeResult<T>;
    public sealed record Expired : TakeResult<T>;
    public sealed record AlreadyConsumed : TakeResult<T>;
}

public sealed class SensitiveString : IDisposable
{
    private char[]? _characters;

    public SensitiveString(string value) => _characters = value.ToCharArray();

    public string Reveal() => _characters is null ? throw new ObjectDisposedException(nameof(SensitiveString)) : new string(_characters);

    public void Dispose()
    {
        if (_characters is null)
            return;

        Array.Clear(_characters);
        _characters = null;
    }

    public override string ToString() => "[REDACTED]";
}
