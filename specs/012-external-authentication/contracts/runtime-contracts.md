# Runtime and Extension Contracts

The signatures are design contracts. Exact namespaces may change only if the implementation preserves names and responsibilities documented here.

## Protocol Adapter

```csharp
public interface IExternalAuthenticationAdapter
{
    string Type { get; }
    ExternalAuthenticationAdapterDescriptor Describe();
    ValueTask<ConnectionValidationResult> ValidateAsync(
        ConnectionValidationContext context,
        CancellationToken cancellationToken = default);
    ValueTask<ExternalAuthorizationRequest> CreateAuthorizationRequestAsync(
        ExternalAuthorizationContext context,
        CancellationToken cancellationToken = default);
    ValueTask<ExternalAuthenticationResult> AuthenticateCallbackAsync(
        ExternalCallbackContext context,
        CancellationToken cancellationToken = default);
    ValueTask<ConnectionTestResult> TestAsync(
        ConnectionTestContext context,
        CancellationToken cancellationToken = default);
    ValueTask<ExternalLogoutRequest?> CreateLogoutRequestAsync(
        ExternalLogoutContext context,
        CancellationToken cancellationToken = default);
}
```

Rules:

- Adapter instances are registered at startup and selected by stable `Type`.
- Contexts provide immutable effective connection snapshots, transient resolved secrets, protected adapter state, outbound HTTP policy, and clock—not persistence services or arbitrary management APIs.
- Settings are supplied as `JsonElement` plus schema version. The adapter owns parse, validate, and migration.
- `ExternalAuthenticationResult` contains only normalized identity and bounded projected claims. It never exposes provider tokens to the broker response or persistence layer.
- Adapter failures use typed internal categories and redacted diagnostics.

### Descriptor

```csharp
public sealed record ExternalAuthenticationAdapterDescriptor(
    string Type,
    string DisplayName,
    string Description,
    int SettingsVersion,
    IReadOnlyList<SettingFieldDescriptor> Fields,
    ExternalAuthenticationAdapterCapabilities Capabilities,
    CustomEditorContract? CustomEditor);
```

`SettingFieldDescriptor` includes name, value type, required state, UI hint, default, allowed values, validation, secret-binding flag, unsafe flag, conditional visibility, help text, and redaction behavior.

## Adapter Registry

```csharp
public interface IExternalAuthenticationAdapterRegistry
{
    IReadOnlyCollection<ExternalAuthenticationAdapterDescriptor> ListDescriptors();
    bool TryGet(string type, out IExternalAuthenticationAdapter adapter);
}
```

The registry includes installed adapters only. Deployment policy filters which types database-owned connections may select.

## Connection Sources and Registry

```csharp
public interface IIdentityProviderConnectionSource
{
    string Name { get; }
    ConnectionSourceOwnership Ownership { get; }
    ValueTask<ConnectionSourceSnapshot> GetSnapshotAsync(
        ConnectionScope scope,
        CancellationToken cancellationToken = default);
}

public interface IIdentityProviderConnectionRegistry
{
    ValueTask<EffectiveConnectionRegistry> GetAsync(
        string targetTenantId,
        CancellationToken cancellationToken = default);
    ValueTask<EffectiveIdentityProviderConnection?> FindByKeyAsync(
        string targetTenantId,
        string key,
        CancellationToken cancellationToken = default);
    ValueTask<EffectiveIdentityProviderConnection?> FindByIdAsync(
        string targetTenantId,
        string connectionId,
        CancellationToken cancellationToken = default);
}
```

`EffectiveConnectionRegistry` includes active, invalid, archived, and shadowed administrative entries, the shared source version, and effective Login Methods. Public discovery maps only the safe Login Method projection.

## Database Connection Store

```csharp
public interface IIdentityProviderConnectionStore
{
    ValueTask<Page<IdentityProviderConnection>> FindAsync(
        ConnectionFilter filter,
        CancellationToken cancellationToken = default);
    ValueTask<IdentityProviderConnection?> FindByIdAsync(
        string id,
        CancellationToken cancellationToken = default);
    ValueTask<ConnectionMutationResult> CreateAsync(
        IdentityProviderConnection connection,
        CancellationToken cancellationToken = default);
    ValueTask<ConnectionMutationResult> UpdateAsync(
        IdentityProviderConnection connection,
        long expectedRevision,
        CancellationToken cancellationToken = default);
}
```

The store returns typed `Created`, `Updated`, `NotFound`, `DuplicateKey`, and `RevisionConflict` results. Archive, restore, enable, and disable are domain-service mutations using the same compare-and-swap update.

## Secret Binding

```csharp
public interface ISecretBindingResolver
{
    string Type { get; }
    ValueTask<SecretBindingState> GetStateAsync(
        SecretBinding binding,
        CancellationToken cancellationToken = default);
    ValueTask<ResolvedSecretBinding> ResolveAsync(
        SecretBinding binding,
        CancellationToken cancellationToken = default);
}

public sealed record ResolvedSecretBinding(
    SensitiveString Value,
    string GenerationFingerprint);
```

`SensitiveString` deliberately avoids meaningful `ToString()` output and is disposed/cleared where practical. Generation fingerprints are keyed and nonreversible. Management models map only `SecretBindingState.IsConfigured` and `IsResolvable`.

## Unlinked Identity Policy

```csharp
public interface IUnlinkedIdentityPolicy
{
    string Type { get; }
    UnlinkedIdentityPolicyDescriptor Describe();
    ValueTask<UnlinkedIdentityDecision> EvaluateAsync(
        UnlinkedIdentityContext context,
        CancellationToken cancellationToken = default);
}

public abstract record UnlinkedIdentityDecision
{
    public sealed record Reject(string SafeReason) : UnlinkedIdentityDecision;
    public sealed record CreateUser(UserCreationProposal Proposal) : UnlinkedIdentityDecision;
    public sealed record LinkExistingUser(
        string UserId,
        string AuthorizationBasis) : UnlinkedIdentityDecision;
}
```

`UnlinkedIdentityContext` provides target tenant, immutable connection snapshot, normalized identity, projected claims, and versioned policy settings. It does not expose unrestricted provider claims or grant persistence access.

The broker validates any `LinkExistingUser` tenant match and performs the same atomic unique-link operation as administrator prelinking.

Built-in types:

- `reject`
- `create-user`

## Atomic Identity Resolution

```csharp
public interface IExternalIdentityResolver
{
    ValueTask<ExternalIdentityResolution> ResolveAsync(
        ExternalIdentityResolutionContext context,
        CancellationToken cancellationToken = default);
}

public interface IExternalIdentityProvisioner
{
    ValueTask<ProvisioningResult> CreateLinkOrGetExistingAsync(
        ProvisioningRequest request,
        CancellationToken cancellationToken = default);
}
```

The provisioner owns one transaction spanning generated-name reservation, credential-less User creation, unique link creation, and cleanup/convergence after a uniqueness race.

## Permission Grant Sources

```csharp
public interface IPermissionGrantSource
{
    string Type { get; }
    PermissionGrantSourceDescriptor Describe();
    ValueTask<PermissionGrantResult> GetGrantsAsync(
        PermissionGrantContext context,
        CancellationToken cancellationToken = default);
}

public sealed record PermissionGrant(
    string Permission,
    string SourceType,
    string SourceReference);

public sealed record PermissionGrantResult(
    IReadOnlyCollection<PermissionGrant> Grants,
    IReadOnlyCollection<PermissionGrantWarning> Warnings);
```

V1 source types:

- `elsa-roles`
- `claim-mapping`
- `group-mapping`

The composition service preserves provenance for preview/diagnostics, applies deployment allow/deny boundaries, and emits distinct permission strings to token issuance.

### Grant Configuration Authorization

```csharp
public interface IPermissionDelegationAuthorizer
{
    ValueTask<PermissionDelegationResult> AuthorizeAsync(
        ClaimsPrincipal actor,
        IReadOnlyCollection<GrantSourceSelection> selections,
        CancellationToken cancellationToken = default);
}
```

No management endpoint may persist grant settings without this authorization.

## Permission Descriptors

```csharp
public interface IPermissionDescriptorProvider
{
    IEnumerable<PermissionDescriptor> GetDescriptors();
}

public interface IPermissionDescriptorRegistry
{
    IReadOnlyCollection<PermissionDescriptor> List();
}
```

The registry is advisory. It never rejects an unknown permission string.

## Broker State

```csharp
public interface IExternalAuthenticationStateStore
{
    ValueTask PutAsync<T>(
        string purpose,
        string handleHash,
        T value,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);
    ValueTask<TakeResult<T>> TryTakeAsync<T>(
        string purpose,
        string handleHash,
        CancellationToken cancellationToken = default);
}
```

`TryTakeAsync` is atomic across nodes. It returns `Taken`, `NotFound`, `Expired`, or `AlreadyConsumed`. Implementations protect payloads at rest and enforce maximum serialized size.

Connection/session stores expose similar atomic operations for:

- AuthorizationGrant consumption.
- Refresh-token compare-and-swap rotation.
- One-time Preview Result read.
- External session revocation.

## Token Issuance

```csharp
public sealed record TokenIssuanceContext(
    User User,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    IReadOnlyCollection<Claim> AdditionalClaims,
    string? ExternalAuthenticationSessionId);

public interface IElsaTokenService
{
    ValueTask<IssuedAccessToken> IssueAccessTokenAsync(
        TokenIssuanceContext context,
        CancellationToken cancellationToken = default);
}

public interface IExternalAuthenticationTokenIssuer
{
    ValueTask<ExternalTokenResponse> IssueAsync(
        ExternalAuthenticationSession session,
        CancellationToken cancellationToken = default);
    ValueTask<ExternalTokenResponse> RefreshAsync(
        string clientId,
        SensitiveString refreshToken,
        CancellationToken cancellationToken = default);
}
```

Existing `IAccessTokenIssuer.IssueTokensAsync(User)` remains unchanged and delegates JWT construction to `IElsaTokenService`.

## Connection Observation

```csharp
public interface IConnectionObservationStore
{
    ValueTask<ConnectionObservation?> FindLatestAsync(
        string connectionId,
        CancellationToken cancellationToken = default);
    ValueTask SaveLatestAsync(
        ConnectionObservation observation,
        CancellationToken cancellationToken = default);
}
```

It stores one redacted latest observation per connection, not history.

## Security Notifications

All records implement `INotification` and contain `SecurityEventContext` with actor ID, tenant ID, Connection ID when known, Elsa User ID when known, timestamp, outcome, correlation ID, and redacted summary.

Event families:

- `IdentityProviderConnectionChanged`
- `IdentityProviderConnectionLifecycleChanged`
- `IdentityProviderConnectionSecretBindingChanged`
- `IdentityProviderConnectionTested`
- `IdentityProviderConnectionPreviewed`
- `ExternalIdentityLinkChanged`
- `ExternalAuthenticationSessionRevoked`
- `ExternalSignInCompleted`

The module publishes after the committed outcome. It does not persist notification history.

## Registration Shape

```csharp
services.AddElsa(elsa =>
{
    elsa.UseIdentity(identity => { /* existing token options */ });

    elsa.UseExternalAuthentication(external =>
    {
        external.Configure(options =>
            configuration.GetSection("ExternalAuthentication").Bind(options));

        external.UseOpenIdConnect();
        external.UseElsaSecrets();
    });
});
```

Configuration-first hosts omit database-store registration. Hosts using EF Identity persistence register the external-authentication EF integration with the same `IdentityElsaDbContext`.

Custom extensions register through DI and an explicit feature method:

```csharp
external.AddAdapter<MyGitHubAdapter>();
external.AddUnlinkedIdentityPolicy<MyAdmissionPolicy>();
external.AddPermissionGrantSource<MyEntitlementGrantSource>();
services.AddSingleton<IPermissionDescriptorProvider, MyModulePermissionDescriptors>();
```

Extension code is trusted deployment code. Runtime settings can select only registered and deployment-allowed types.
