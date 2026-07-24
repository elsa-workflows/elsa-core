# REST API Contract: External Authentication

All paths are relative to Elsa's configured API route prefix (for example, `/elsa/api`). JSON uses camel case. Timestamps use RFC 3339 UTC. IDs are opaque strings.

## Common Response Rules

### Error

```json
{
  "error": "flow_expired",
  "message": "The sign-in attempt expired. Start again.",
  "correlationId": "01JZ..."
}
```

Public categories:

- `invalid_request`
- `method_unavailable`
- `authentication_failed`
- `identity_unlinked`
- `flow_expired`
- `flow_changed`
- `access_denied`
- `rate_limited`
- `temporarily_unavailable`
- `server_error`

Management APIs may additionally use `not_found`, `validation_failed`, `conflict`, `forbidden`, and `precondition_failed`, with a safe `details` object containing field errors or current revision. Provider response bodies and tenant/user existence details never appear.

### Status Codes

| Status | Use |
| --- | --- |
| `200` | Successful read, update, action, or token response |
| `201` | Created resource |
| `204` | Successful action with no body |
| `302` / `303` | Browser redirect from authorization/logout flow |
| `400` | Invalid request or validation |
| `401` | Missing/invalid client or Elsa authentication |
| `403` | Authenticated caller lacks permission |
| `404` | Resource or safe public method not available |
| `409` | Uniqueness, lifecycle, or concurrency conflict |
| `412` | `If-Match` revision does not match |
| `429` | Rate limited; includes `Retry-After` |
| `503` | Safe temporary provider/store failure |

### Concurrency

Connection detail responses include:

```http
ETag: "17"
```

Every database-owned mutation requires `If-Match: "17"`. Configuration-owned resources reject mutation with `403` and category `forbidden`.

## Anonymous and Broker APIs

### Discover Login Methods

```http
GET /external-authentication/login-methods?clientId=elsa-studio-wasm
```

The server derives tenant context from trusted host middleware, route context, invitation, or registered client context—not an arbitrary tenant query parameter.

```json
{
  "methods": [
    {
      "id": "local",
      "key": "local",
      "kind": "local",
      "displayName": "Elsa account",
      "iconId": "elsa",
      "order": 0,
      "isDefault": false,
      "initiationUrl": "/elsa/api/external-authentication/local/authorize"
    },
    {
      "id": "01JZCONNECTION",
      "key": "contoso",
      "kind": "external",
      "displayName": "Contoso",
      "iconId": "building",
      "order": 10,
      "isDefault": true,
      "initiationUrl": "/elsa/api/external-authentication/authorize/contoso"
    }
  ],
  "automaticMethodKey": "contoso"
}
```

Response contains no authority, adapter type, upstream client identifier, tenant identifier, health, secret, or remote asset URL. `Cache-Control: no-store`.

### Initiate External Sign-in

```http
GET /external-authentication/authorize/{connectionKey}
    ?client_id=elsa-studio-wasm
    &redirect_uri=https%3A%2F%2Fstudio.example%2Fauthentication%2Fexternal%2Fcallback
    &response_type=code
    &code_challenge={base64url}
    &code_challenge_method=S256
    &return_path=%2Fworkflows
```

Success: `302` to the provider. Elsa stores protected correlation state and never forwards `return_path` to the provider except inside server-owned state.

Failures redirect only to the exact registered client callback with safe `error` and `correlation_id` when the request is sufficiently valid to trust that callback; otherwise return JSON error.

### Initiate Broker-local Sign-in

```http
POST /external-authentication/local/authorize
Content-Type: application/json
```

```json
{
  "clientId": "elsa-studio-wasm",
  "redirectUri": "https://studio.example/authentication/external/callback",
  "responseType": "code",
  "codeChallenge": "{base64url}",
  "codeChallengeMethod": "S256",
  "returnPath": "/workflows",
  "username": "admin",
  "password": "..."
}
```

Success: `200`.

```json
{
  "redirectUri": "https://studio.example/authentication/external/callback?code={opaque}&state={clientState}"
}
```

The client navigates only after confirming the returned URI matches its own registered origin/path. Invalid username, invalid password, missing Local Credential, or unknown user all return `401 authentication_failed`.

This route is additive. Existing `/identity/login` remains unchanged.

### Provider Callback

```http
GET /external-authentication/callback/{connectionId}?code={providerCode}&state={opaqueState}
```

Adapters MAY register a POST callback variant when their protocol requires it. The fixed Connection ID is checked against protected state. Success: `302` to the exact Authentication Client callback:

```text
https://studio.example/authentication/external/callback?code={elsaCode}&state={clientState}
```

No provider or Elsa token appears in the URL.

### Exchange Authorization Code

```http
POST /external-authentication/token
Content-Type: application/x-www-form-urlencoded
Origin: https://studio.example
```

```text
grant_type=authorization_code
&code={opaque}
&client_id=elsa-studio-wasm
&redirect_uri=https%3A%2F%2Fstudio.example%2Fauthentication%2Fexternal%2Fcallback
&code_verifier={verifier}
```

Confidential clients additionally authenticate with HTTP Basic client credentials resolved from deployment configuration. Public clients must not send or be required to hold a client secret.

```json
{
  "accessToken": "{elsa-jwt}",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "refreshToken": "{opaque}",
  "refreshExpiresIn": 7200,
  "externalSessionExpiresIn": 28800
}
```

The completion code is consumed atomically whether exchange succeeds or fails after validation. Replay returns `400 invalid_request`.

### Refresh External Session

```http
POST /external-authentication/token
Content-Type: application/x-www-form-urlencoded
```

```text
grant_type=refresh_token
&refresh_token={opaque}
&client_id=elsa-studio-wasm
```

Response has the same token shape and always rotates `refreshToken`. Reuse of a superseded refresh token revokes the External Authentication Session and returns `401 access_denied`.

### Begin Logout

```http
POST /external-authentication/logout
Authorization: Bearer {elsa-access-token}
Content-Type: application/json
```

```json
{
  "clientId": "elsa-studio-wasm",
  "postLogoutRedirectUri": "https://studio.example/authentication/external/logout-callback",
  "mode": "local"
}
```

`mode` is `local` or `upstream`. Upstream is allowed only by adapter capability and connection mode.

```json
{
  "completed": false,
  "navigationUrl": "/elsa/api/external-authentication/logout/continue/{opaqueHandle}",
  "redirectUri": null
}
```

When local logout is complete with no provider navigation, `completed` is `true` and `redirectUri` is the exact registered post-logout callback. The one-time continuation owns provider redirect. Provider callback:

```http
GET /external-authentication/logout/callback/{connectionId}?state={opaqueState}
```

It redirects only to the exact registered post-logout callback.

## Descriptor APIs

All descriptor APIs require `external-authentication:connections:read`.

```http
GET /external-authentication/descriptors/adapters
GET /external-authentication/descriptors/policies
GET /external-authentication/descriptors/permission-sources
GET /external-authentication/descriptors/permissions
```

Field descriptor:

```json
{
  "name": "authority",
  "displayName": "Authority",
  "description": "Base provider authority.",
  "valueType": "uri",
  "uiHint": "uri",
  "isRequired": true,
  "isSecretBinding": false,
  "isUnsafe": false,
  "defaultValue": null,
  "allowedValues": [],
  "visibleWhen": null,
  "validation": {
    "maximumLength": 2048,
    "pattern": null
  }
}
```

Adapter descriptor:

```json
{
  "type": "openid-connect",
  "displayName": "OpenID Connect",
  "description": "Authenticate through an OpenID Connect provider.",
  "settingsVersion": 1,
  "fields": [],
  "capabilities": ["test", "preview", "upstream-logout"],
  "customEditor": null
}
```

`customEditor`, when present, contains `key` and `contractVersion`. A missing/incompatible editor falls back to generic fields.

## Connection Management

### List

```http
GET /external-authentication/connections
    ?search=
    &source=configuration|database
    &scope=
    &adapterType=
    &enabled=
    &valid=
    &shadowed=
    &archived=
    &cursor=
    &pageSize=100
```

Requires `external-authentication:connections:read`. Maximum `pageSize` is 100.

```json
{
  "items": [
    {
      "id": "01JZ...",
      "key": "contoso",
      "source": "database",
      "scope": {"kind": "tenant", "tenantId": "tenant-a"},
      "adapterType": "openid-connect",
      "displayName": "Contoso",
      "iconId": "building",
      "order": 10,
      "isDefault": true,
      "enabledIntent": true,
      "effectivelyEnabled": true,
      "validity": "valid",
      "shadowed": false,
      "archived": false,
      "revision": 17,
      "materialRevision": "m-9",
      "latestObservation": {
        "status": "succeeded",
        "observedAt": "2026-07-24T12:00:00Z",
        "testedMaterialRevision": "m-9",
        "isStale": false,
        "category": "reachable",
        "summary": "Discovery and signing keys were validated."
      }
    }
  ],
  "nextCursor": null
}
```

### Create

```http
POST /external-authentication/connections
```

Requires `external-authentication:connections:create`.

```json
{
  "key": "contoso",
  "scope": {"kind": "tenant", "tenantId": "tenant-a"},
  "adapterType": "openid-connect",
  "displayName": "Contoso",
  "iconId": "building",
  "order": 10,
  "isDefault": false,
  "adapterSettingsVersion": 1,
  "adapterSettings": {},
  "secretBindings": {},
  "unlinkedPolicy": null,
  "permissionGrantSources": [],
  "claimProjection": {
    "allowedClaimTypes": ["name", "email", "groups"],
    "redactedClaimTypes": ["email"]
  },
  "upstreamLogoutMode": "disabled"
}
```

Creates a disabled draft. Success: `201`, `Location` header, `ETag: "1"`, and detail body.

### Detail and Update

```http
GET /external-authentication/connections/{connectionId}
PUT /external-authentication/connections/{connectionId}
```

Read requires `external-authentication:connections:read`; update requires `external-authentication:connections:update` and `If-Match`.

Detail includes the create fields plus lifecycle, validation, shadow/conflict diagnostics, effective policy, resolved extension availability, and secret binding state:

```json
{
  "secretBindings": {
    "clientSecret": {
      "resolverType": "elsa-secrets",
      "reference": "contoso-oidc-secret",
      "isConfigured": true,
      "isResolvable": true
    }
  }
}
```

No generation fingerprint or value is returned.

### Secret Binding Replacement/Removal

```http
PUT /external-authentication/connections/{connectionId}/secret-bindings/{fieldName}
DELETE /external-authentication/connections/{connectionId}/secret-bindings/{fieldName}
```

Requires `external-authentication:connections:update` and `If-Match`.

```json
{
  "resolverType": "elsa-secrets",
  "reference": "contoso-oidc-secret",
  "expectedType": "text",
  "expectedScope": "external-authentication"
}
```

If the Elsa Secrets bridge supports inline creation/replacement, its secret value is submitted to the Secrets API, not returned or embedded in the connection response.

### Lifecycle Actions

```http
POST /external-authentication/connections/{connectionId}/enable
POST /external-authentication/connections/{connectionId}/disable?confirmFinalLoginPathOverride=false
DELETE /external-authentication/connections/{connectionId}?confirmFinalLoginPathOverride=false
POST /external-authentication/connections/{connectionId}/restore
```

All require `If-Match`.

Disabling or archiving the final normal login method is rejected with `409 conflict` and `details.code` set to `final_login_path_guard` unless another normal/local method or deployment-owned break-glass method remains. A caller holding the deployment-configured privileged override permission may repeat the operation with `confirmFinalLoginPathOverride=true`; Studio requires a separate explicit recovery confirmation before sending it.

| Action | Permission |
| --- | --- |
| Enable/disable | `external-authentication:connections:update` |
| Archive/restore | `external-authentication:connections:archive` |

Enable returns `400 validation_failed` unless structurally valid with resolvable required bindings. Restore returns disabled.

### Validate

```http
POST /external-authentication/connections/{connectionId}/validate
```

Requires read permission. Performs local structural and binding-state validation without provider traffic.

```json
{
  "valid": false,
  "errors": [
    {"field": "secretBindings.clientSecret", "code": "required", "message": "Client secret is required."}
  ],
  "warnings": []
}
```

### Test

```http
POST /external-authentication/connections/{connectionId}/test
```

Requires `external-authentication:connections:test` and `If-Match`. Runs provider traffic using the exact revision and upserts the latest shared observation.

### Preview

```http
POST /external-authentication/connections/{connectionId}/preview
GET /external-authentication/previews/{previewHandle}/authorize
GET /external-authentication/previews/{previewHandle}
GET /external-authentication/previews/callback/{connectionId}?code={providerCode}&state={opaqueState}
```

Requires `external-authentication:connections:preview`. POST requires `If-Match` and returns:

```json
{
  "navigationUrl": "/elsa/api/external-authentication/previews/{previewHandle}/authorize",
  "expiresAt": "2026-07-24T12:10:00Z"
}
```

The authorize route consumes the administrator-bound start state and redirects to the provider. The provider callback stores only a redacted result and returns safe completion status. Result GET is one-time, bound to the initiating administrator session, and returns the allowlisted Preview Result. It returns `410` after expiry/consumption and never produces a normal completion code.

## External Identity Links

### Bounded User Lookup

```http
GET /external-authentication/user-options?search=&cursor=&pageSize=25
```

Requires `external-authentication:links:manage`. Tenant comes from authenticated context. Maximum page size is 50.

```json
{
  "items": [
    {"id": "user-1", "displayName": "workflow-admin"}
  ],
  "nextCursor": null
}
```

No password, role, permission, external-link, or cross-tenant data is returned.

### List/Create/Delete

```http
GET /external-authentication/identity-links?userId=&connectionId=&cursor=&pageSize=100
POST /external-authentication/identity-links
DELETE /external-authentication/identity-links/{linkId}
```

Requires `external-authentication:links:manage`.

Create:

```json
{
  "userId": "user-1",
  "connectionId": "01JZ...",
  "issuer": "https://login.contoso.example",
  "subject": "00u1abcd..."
}
```

The subject is accepted only over TLS, normalized and immediately transformed to the stored keyed hash; it is never returned. Duplicate tuple-to-same-user is idempotent `200`; tuple-to-different-user is `409 conflict`. Delete requires explicit Studio confirmation and returns `204`.

## External Sessions

Available only when session administration is enabled.

```http
GET /external-authentication/sessions?userId=&connectionId=&status=&cursor=&pageSize=100
DELETE /external-authentication/sessions/{sessionId}
```

Read requires `external-authentication:sessions:read`; revoke requires `external-authentication:sessions:revoke`. Responses contain session ID, user ID, tenant, connection ID, started/last refreshed/expires/revoked times, and safe status—never token hashes, subject, or claim snapshot.

Revoke accepts an optional JSON body such as `{"reason":"administrator_revoked"}`. The server bounds the safe reason and returns `204`; Studio uses the stable administrator-revoked category.

## Permission Names

- `external-authentication:connections:read`
- `external-authentication:connections:create`
- `external-authentication:connections:update`
- `external-authentication:connections:archive`
- `external-authentication:connections:test`
- `external-authentication:connections:preview`
- `external-authentication:policies:manage`
- `external-authentication:provider-trust:unsafe`
- `external-authentication:permissions:delegate`
- `external-authentication:permissions:delegate-unrestricted`
- `external-authentication:links:manage`
- `external-authentication:sessions:read`
- `external-authentication:sessions:revoke`

API authorization is authoritative. Studio menu and disabled-state logic are usability affordances only.
