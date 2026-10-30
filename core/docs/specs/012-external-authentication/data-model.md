# Data Model: External Authentication

## Conventions

- Stable connection record IDs identify management resources and transient broker/preview/observation records. Immutable logical Connection Keys identify durable External Identity Links and long-lived External Authentication Sessions.
- Connections are host-wide within the currently connected server environment; no persisted Deployment Target/Server Environment field exists.
- Timestamps are UTC `DateTimeOffset`.
- Revisions are monotonically increasing 64-bit integers.
- Secret values, provider tokens, completion codes, refresh tokens, raw subjects, and unrestricted claims are never stored in public connection documents.
- JSON fields are opaque, versioned extension settings. Their provider owns validation and migration.

## IdentityProviderConnection

Represents one database-owned provider trust relationship. Configuration-owned connections use the same domain model but are materialized from options and never written through the store.

| Field | Type | Rules |
| --- | --- | --- |
| `Id` | string | Stable record ID used by management and transient broker records |
| `ConnectionKey` | string | Immutable normalized logical key; globally unique within the host target |
| `Ownership` | enum | `Configuration`, `Studio`, or `StudioOverride` |
| `OverridesConfiguration` | bool | True only for an explicit complete override of the same key |
| `AdapterType` | string | Must identify an installed, deployment-allowed adapter |
| `AdapterSettingsVersion` | int | Positive schema version |
| `AdapterSettings` | JSON object | Opaque to registry; adapter validates/migrates |
| `SecretBindings` | map<string, SecretBinding> | Names declared by descriptor; contains no values |
| `DisplayName` | string | Required, bounded, safe plain text |
| `IconId` | string? | Trusted server asset identifier only |
| `DisplayOrder` | int | Deterministic tie-break by normalized key |
| `IsPreferred` | bool | At most one effective preferred method; presentation only, never auto-redirect |
| `IsEnabled` | bool | Administrative intent; requires validity to be effective |
| `ArchivedAt` | DateTimeOffset? | Archive is logical deletion |
| `UnlinkedPolicy` | PolicySelection? | Null means deployment default |
| `DefaultRoleIds` | set<string> | Applied only to JIT-created users; actor-authorized on save |
| `MatcherPolicy` | MatcherPolicySettings? | Present only for the matcher-based Unlinked Identity Policy |
| `ClaimProjection` | ClaimProjection | Allowlist, value limits, and redaction policy |
| `UpstreamLogoutMode` | enum | `Disabled`, `UserChoice`, or `Always` |
| `Revision` | long | Changes on every mutation; concurrency token |
| `MaterialRevision` | string | Non-secret fingerprint of all sign-in-affecting state |
| `CreatedAt` / `UpdatedAt` | DateTimeOffset | Audit metadata |

### Constraints

- Database unique index on `ConnectionKey` for active/disabled Studio documents in one environment.
- A configuration collision is accepted only through the explicit Studio Override operation.
- An override is a full document; there is no field-level source merge.
- A disabled override continues shadowing. An archived override reveals configuration. Restore resumes shadowing and returns disabled.
- `ConnectionKey` survives archive/restore and is immutable.
- Display-only changes advance `Revision` but may retain `MaterialRevision`.
- Adapter settings, Secret Binding generation, policy/matcher settings, claim projection, static create-user roles, override state, enablement, or archive state affect `MaterialRevision`.

### State Transitions

```text
Draft/Disabled --validate + enable--> Enabled
Enabled --disable------------------> Disabled
Draft/Disabled/Enabled --archive---> Archived
Archived --restore-----------------> Disabled
```

Invalid or unresolved configuration never becomes effectively enabled. Test observations do not change lifecycle state.

For an override, `Disabled` still shadows configuration, `Archived` reveals configuration, and `restore` resumes the shadow in `Disabled`.

## SecretBinding

| Field | Type | Rules |
| --- | --- | --- |
| `Ownership` | enum | `Managed` or `External` |
| `ResolverType` | string | Installed, deployment-allowed resolver |
| `Reference` | string | Non-secret lookup key |
| `ExpectedType` | string? | Optional resolver/type constraint |
| `ExpectedScope` | string? | Optional resolver/scope constraint |

Resolution returns a transient value and opaque nonreversible generation fingerprint. The fingerprint contributes to material revision but is not exposed or stored in API models.

Managed bindings are lifecycle-managed through Elsa Secrets and may be replaced/removed through authorized APIs. External bindings use the built-in configuration-key resolver or another deployment resolver and remain value-read-only in Studio.

## PolicySelection

| Field | Type | Rules |
| --- | --- | --- |
| `Type` | string | Installed, deployment-allowed Unlinked Identity Policy |
| `SettingsVersion` | int | Positive schema version |
| `Settings` | JSON object | Provider-owned validation/migration |

Built-in types are `Reject`, `CreateUser`, and the generic matcher-based policy.

## MatcherPolicySettings

| Field | Type | Rules |
| --- | --- | --- |
| `MatcherType` | string | One installed, deployment-allowed `IExternalUserMatcher` |
| `MatcherSettingsVersion` | int | Positive schema version |
| `MatcherSettings` | JSON object | Matcher-owned validation/migration |
| `NoMatchAction` | enum | `Reject` or `CreateUser` |

The matcher descriptor declares required normalized claim types. Those claims are supplied ephemerally and discarded after evaluation. One match proposes an existing user; no match follows `NoMatchAction`; ambiguous results and errors reject. V1 ships no Elsa first-party verified-email matcher. `DefaultRoleIds` apply only when `CreateUser` executes.

## ClaimProjection

| Field | Type | Rules |
| --- | --- | --- |
| `AllowedClaimTypes` | set<string> | Only these normalized claims survive sign-in processing |
| `RedactedClaimTypes` | set<string> | Values hidden in preview/diagnostics |
| `MaximumClaimCount` | int | Secure bounded default; deployment may lower |
| `MaximumValueLength` | int | Per scalar/array member |
| `MaximumTotalBytes` | int | Entire normalized projection |

Values are only strings or string arrays. Every value carries adapter/provider provenance during evaluation. Claims outside the projection are discarded.

## ExternalIdentityLink

Associates one provider identity with one tenant-owned Elsa User. Connection deployment remains host-wide in v1.

| Field | Type | Rules |
| --- | --- | --- |
| `Id` | string | Immutable |
| `TenantId` | string | Broker-resolved target Elsa tenant |
| `ConnectionKey` | string | Immutable logical connection key |
| `Issuer` | string | Validated canonical issuer namespace |
| `SubjectHash` | string | Keyed hash of canonical provider subject |
| `SubjectHint` | string? | Optional masked operator hint; never the raw subject |
| `UserId` | string | Elsa User in the same target tenant |
| `CreatedAt` | DateTimeOffset | Creation/prelink time |
| `LastSignedInAt` | DateTimeOffset? | Safe operational metadata |

### Constraints

- Unique index: `(TenantId, ConnectionKey, Issuer, SubjectHash)`.
- Foreign key to `User`; broker enforces tenant equality.
- Connection archive preserves links.
- Explicit unlink removes the active association and emits a notification.
- Concurrent JIT and prelink use one atomic create-link-or-get-existing operation.

## User Migration

The existing `User` entity changes:

| Field | Change |
| --- | --- |
| `HashedPassword` | Nullable; both password fields are present or absent together |
| `HashedPasswordSalt` | Nullable |

Credential-less users cannot authenticate through legacy or broker-local password validation. Existing password-backed rows require no data change.

JIT provisioning generates a globally unique internal `User.Name`, leaves password fields null, and assigns authorized default/matcher roles in the User-store write. The link store independently arbitrates the external identity tuple; a losing or failed link writer removes only the credential-less User it created before returning or propagating the failure.

## AuthenticationClient

Deployment-configured registration for an Elsa client.

| Field | Type | Rules |
| --- | --- | --- |
| `ClientId` | string | Unique immutable identifier |
| `DisplayName` | string | Operator-facing |
| `ClientType` | enum | `Confidential` or `Public` |
| `CallbackUris` | set<URI> | Exact HTTPS matches; development loopback may be explicitly allowed |
| `LogoutCallbackUris` | set<URI> | Exact matches |
| `AllowedOrigins` | set<origin> | Required for public clients; no wildcard |
| `AllowedReturnPathPrefixes` | set<local path> | Segment-boundary prefixes for post-authentication client-local navigation |
| `SecretBinding` | SecretBinding? | Required for confidential clients; forbidden for public clients |
| `IsEnabled` | bool | Disabled clients cannot initiate or exchange |

Authentication Clients are not Elsa API Applications and contain no roles or permissions.

## BrokerTransaction

Shared, protected, short-lived provider/local/preview correlation state.

| Field | Type | Rules |
| --- | --- | --- |
| `HandleHash` | string | Keyed hash of random browser-visible state handle |
| `Purpose` | enum | `ExternalSignIn`, `LocalSignIn`, `Preview`, `UpstreamLogout` |
| `ClientId` | string | Registered Authentication Client or preview purpose |
| `CallbackUri` | URI | Exact registered callback |
| `ReturnPath` | string | Validated client-local path |
| `TenantId` | string | Broker-resolved target Elsa tenant |
| `ConnectionId` | string? | Connection record ID required for external/preview |
| `ConnectionMaterialRevision` | string? | Captured effective revision |
| `SecretGenerationFingerprint` | string? | Protected, nonreversible |
| `PkceChallenge` | string | S256 only |
| `ProviderNonce` | string? | External/preview |
| `ProtectedPayload` | bytes | Data-protected adapter state |
| `ExpiresAt` | DateTimeOffset | Default 10 minutes |
| `ConsumedAt` | DateTimeOffset? | Atomic single-use marker |

EF persistence also stores the normalized `ExpiresAtUtcTicks` companion used in the same compare-and-swap predicate as `ConsumedAt`, because not every supported provider can order `DateTimeOffset` values directly.

The public handle contains no protected payload. Atomic take transitions pending to consumed; expired or mismatched state is never revived.

## AuthorizationGrant

Single-use Elsa completion code record.

| Field | Type | Rules |
| --- | --- | --- |
| `CodeHash` | string | Keyed hash of random code |
| `ClientId` | string | Bound client |
| `CallbackUri` | URI | Bound exact callback |
| `TenantId` | string | Bound target Elsa tenant |
| `UserId` | string | Resolved Elsa User |
| `ExternalSessionId` | string? | Null for broker-local sign-in |
| `PkceChallenge` | string | S256 |
| `ExpiresAt` | DateTimeOffset | Default 60 seconds |
| `ConsumedAt` | DateTimeOffset? | Atomic single use |

EF persistence uses the same normalized `ExpiresAtUtcTicks` companion for the atomic consume predicate.

## ExternalAuthenticationSession

| Field | Type | Rules |
| --- | --- | --- |
| `Id` | string | Included as safe session claim in access tokens |
| `TenantId` | string | Resolved target Elsa tenant |
| `UserId` | string | Elsa User |
| `ConnectionKey` | string | Source logical connection |
| `ConnectionMaterialRevision` | string | Revision at full sign-in |
| `Issuer` | string | Validated issuer |
| `SubjectHash` | string | No raw subject |
| `ProvisionedRoleIds` | JSON array | Redacted JIT provenance; not reapplied to existing users |
| `UpstreamLogoutArtifact` | protected bytes? | Minimal adapter artifact only when configured logout requires it; never an access/refresh token by default |
| `StartedAt` | DateTimeOffset | Full external sign-in |
| `LastRefreshedAt` | DateTimeOffset | Rotation time |
| `ExpiresAt` | DateTimeOffset | Maximum session age; default eight hours |
| `RefreshExpiresAt` | DateTimeOffset | Inactivity bound |
| `CurrentRefreshTokenHash` | string? | Keyed hash of current opaque token; absent until the first refresh token is issued |
| `RefreshGeneration` | long | Compare-and-swap rotation counter |
| `RevokedAt` | DateTimeOffset? | Explicit or reuse-detection revocation |
| `RevocationReason` | string? | Safe category |

EF persistence stores the optional current hash in a one-to-one `ExternalAuthenticationSessionRefreshTokens` row so the unissued state requires no sentinel value. Refresh atomically verifies current token hash and generation, rotates the token, and reevaluates current Elsa-owned role grants. It does not re-query upstream claims or mutate user roles. Reuse of a superseded token revokes the session.

## ConnectionObservation

Latest on-demand test result only.

| Field | Type | Rules |
| --- | --- | --- |
| `ConnectionId` | string | Connection record ID; primary key |
| `TestedMaterialRevision` | string | Determines freshness |
| `ObservedAt` | DateTimeOffset | UTC |
| `Status` | enum | `Succeeded`, `Failed`, `Warning` |
| `Category` | string | Stable redacted category |
| `Duration` | TimeSpan | Test duration |
| `Summary` | string | Safe bounded message |
| `Warnings` | list<string> | Safe bounded warnings |
| `CorrelationId` | string | Diagnostic correlation |

An observation is stale when its tested revision differs from the current effective material revision. No history table is required.

## PreviewResult

| Field | Type | Rules |
| --- | --- | --- |
| `HandleHash` | string | One-time result handle |
| `AdministratorId` | string | Initiating authenticated actor |
| `TenantId` | string | Preview target Elsa tenant |
| `ConnectionId` | string | Draft connection record ID |
| `MaterialRevision` | string | Exact previewed revision |
| `Issuer` | string | Validated issuer |
| `MaskedSubject` | string | Never raw subject |
| `ProjectedClaims` | JSON object | Allowlisted and descriptor-redacted only |
| `PolicyDecision` | JSON object | Proposed action; no mutation |
| `UserResolutionProjection` | JSON object | Proposed match/no-match action, safe user hint, and static create-user roles; no matcher claims |
| `Warnings` | list<string> | Safe bounded warnings |
| `ExpiresAt` | DateTimeOffset | Default 10 minutes |
| `ConsumedAt` | DateTimeOffset? | One-time read |

EF persistence uses the same normalized `ExpiresAtUtcTicks` companion for the atomic consume predicate.

## OpenIdConnectAdapterSettings v2

| Field | Type | Rules |
| --- | --- | --- |
| `DiscoveryUrl` | URI | Exact absolute HTTPS discovery-document URL |
| `ClientId` | string | Confidential upstream client identifier |
| `ClientAuthenticationMethod` | enum | Serialized as `client_secret_basic` or `client_secret_post` |
| `Scopes` | set<string> | Must include `openid`; bounded and validated |
| `AdvancedTrustOverrides` | JSON object? | Optional issuer, authorization/token endpoints, and signing keys; permission/deployment-gated and persistently warned |

The callback is not stored in adapter settings. Elsa derives it from deployment external base address, fixed callback route, and escaped immutable Connection Key. Advanced overrides replace discovery inputs but cannot disable signature/issuer/audience/lifetime validation. Confidential-client mode and provider PKCE S256 are immutable.
