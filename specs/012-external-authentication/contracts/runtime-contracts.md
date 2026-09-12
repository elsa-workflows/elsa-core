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
        CancellationToken cancellationToken = default);
}

public interface IIdentityProviderConnectionRegistry
{
    ValueTask<EffectiveConnectionRegistry> GetHostAsync(
        CancellationToken cancellationToken = default);
    ValueTask<EffectiveIdentityProviderConnection?> FindByKeyAsync(
        string connectionKey,
        CancellationToken cancellationToken = default);
}
```

`EffectiveConnectionRegistry` includes active, invalid, archived, and shadowed administrative entries, the shared source version, and effective host-wide Login Methods. A dedicated override command creates a complete Studio document for a configuration key. Disabled overrides continue shadowing; archived overrides reveal configuration.

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

Every binding declares `Managed` or `External` ownership. The built-in `configuration` resolver reads External values by configuration key and derives a keyed generation fingerprint. Only Managed bindings support value replacement/removal through management APIs.

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
- `match-external-user`

## External User Matchers

```csharp
public interface IExternalUserMatcher
{
    string Type { get; }
    ExternalUserMatcherDescriptor Describe();
    ValueTask<ExternalUserMatchResult> MatchAsync(
        ExternalUserMatchContext context,
        CancellationToken cancellationToken = default);
}

public abstract record ExternalUserMatchResult
{
    public sealed record NoMatch : ExternalUserMatchResult;
    public sealed record Match(string UserId, string AuthorizationBasis) : ExternalUserMatchResult;
    public sealed record Ambiguous : ExternalUserMatchResult;
    public sealed record Error(string SafeCategory) : ExternalUserMatchResult;
}
```

The matcher-based policy selects exactly one matcher. Its descriptor declares required normalized claim types; the policy passes only those claims and discards them after evaluation. One match may propose an existing same-tenant user. No match executes configured `Reject` or `CreateUser`; ambiguous/error rejects. No first-party verified-email matcher ships in v1.

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

The provisioner owns the operation-level invariant: credential-less User creation (including roles) completes before a link is returned, the link store atomically arbitrates the identity tuple, and every observed losing or failed link write compensates the User created by that writer. An observed compensation failure fails the operation and issues no credentials. User deletion and link publication perform complementary post-write checks: if deletion observes a concurrently published link it restores the User and reports a conflict; if publication observes a concurrently deleted User it removes the link and fails. User resolution, role validation, name generation, collision retry, and compensation are provider-independent; persistence providers implement only link storage and their native uniqueness/transaction behavior.

### Static Create-user Role Authorization

```csharp
public interface IExternalRoleAssignmentAuthorizer
{
    ValueTask<RoleAssignmentAuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal actor,
        IReadOnlyCollection<string> defaultRoleIds,
        CancellationToken cancellationToken = default);
}
```

No management endpoint may persist `defaultRoleIds` without this authorization. They apply only when CreateUser executes. User matchers never select roles. Claim-to-role/permission mapping is not a v1 contract.

### Role Deletion Dependencies

Elsa Identity owns Role deletion and coordinates installed dependency contributors:

```csharp
public interface IRoleDeletionDependencyContributor
{
    string Source { get; }

    ValueTask<RoleDeletionDependencySnapshot> InspectAsync(
        string roleId,
        CancellationToken cancellationToken = default);

    ValueTask<RoleReferenceRemovalResult> RemoveEditableReferencesAsync(
        RoleReferenceRemovalRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record RoleDeletionDependency(
    string OwnerId,
    string OwnerKey,
    string PolicyBranch,
    RoleReferenceOwnership Ownership,
    string? ConfigurationPath,
    long? ExpectedRevision,
    bool RemovesLastDefaultRole);
```

The External Authentication contributor enumerates `defaultRoleIds` in every CreateUser and matcher no-match CreateUser definition, not only effective or enabled connections. Database-owned references carry stable connection record ID, Connection Key, and expected revision. Configuration-owned references carry a sanitized configuration path and are inspection-only.

The Identity coordinator authorizes Role deletion and every affected connection/policy update before mutation. It compares an opaque dependency version plus all expected connection revisions, invokes contributors to remove editable references, re-inspects dependencies, and deletes the Role only when none remain. Configuration references always block and are never passed to removal.

V1 has no shared unit of work spanning the Role store and contributor stores, so any editable dependency makes the coordinator advertise best-effort execution during preflight. It requires explicit best-effort confirmation, applies removals before Role deletion, and never deletes the Role if any removal or reinspection fails. A partial result lists changed and remaining owner IDs without policy values and supports idempotent retry. Atomic mode is reserved for a future shared transaction boundary that actually includes both reference removal and Role deletion; contributor-local atomicity is insufficient. Any dependency whose removal empties `defaultRoleIds` requires explicit empty-default-role confirmation.

## OpenID Connect v2 Settings

The v1 adapter's current settings contract is version 2:

```csharp
public sealed record OpenIdConnectSettings(
    Uri DiscoveryUrl,
    string ClientId,
    OpenIdConnectClientAuthenticationMethod ClientAuthenticationMethod,
    IReadOnlyCollection<string> Scopes,
    OpenIdConnectProviderTrustOverrides? AdvancedTrustOverrides);

public sealed record OpenIdConnectProviderTrustOverrides(
    string? Issuer,
    Uri? AuthorizationEndpoint,
    Uri? TokenEndpoint,
    JsonElement? SigningKeys);
```

`DiscoveryUrl` is exact and HTTPS. Advanced overrides require deployment opt-in plus unsafe-provider-trust authorization and never disable validation. Elsa derives the callback from deployment external base address, the fixed callback route, and immutable Connection Key. The upstream client is confidential, provider PKCE is always S256, and client authentication serializes exactly as `client_secret_basic` or `client_secret_post`.

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

All records implement `INotification` and contain `SecurityEventContext` with actor ID, connection record ID and logical key when known, Elsa User ID when known, timestamp, outcome, correlation ID, and redacted summary.

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
            configuration.GetSection("ExternalAuthentication").BindExternalAuthenticationOptions(options));

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
