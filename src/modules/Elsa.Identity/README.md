# Elsa.Identity

## JWT Signing Key Configuration

Identity token signing requires a secure random key. Configure it through environment variables or a secrets manager and keep it out of committed appsettings files.

- Code-first hosts using `Identity:Tokens` should set `Identity__Tokens__SigningKey`.
- Shell-based hosts should set the shell feature path, for example `CShells__Shells__Default__Features__Identity__SigningKey`.
- Production startup rejects missing keys, keys shorter than 32 ASCII characters, and known public defaults. Known public defaults are tolerated only in the explicit `Development` or `Demo` environments.

## Default Admin User Bootstrap

Elsa supports bootstrapping an initial admin role and user through the `DefaultAdminUser` feature.

This is the recommended way to initialize identity access. Identity management endpoints are authorized by their own permissions. Elsa 3.9 removed the `SecurityRoot` policy and the localhost permission grant ([#8003](https://github.com/elsa-workflows/elsa-core/pull/8003)), so a fresh instance has no network-position shortcut. Configure a seeded administrator (below) or an [admin API key](#admin-api-key-bootstrap) instead. See [the authorization migration guide](../../../doc/migrations/authorization-model.md#the-securityroot-policy-and-the-localhost-grant-are-gone) for the full list of removed types and toggles.

See `doc/adr/0010-default-admin-user-bootstrap-for-initial-identity-access.md` for the architectural decision.

### New shell functionality (recommended)

When using shell-based configuration (`CShells`), configure the `DefaultAdminUser` shell feature.

Example (`appsettings.json`):

```json
{
  "CShells": {
    "Shells": [
      {
        "Name": "Default",
        "Features": {
          "Identity": {},
          "DefaultAuthentication": {},
          "DefaultAdminUser": {
            "AdminUserName": "admin",
            "AdminPassword": "REPLACE_WITH_SECURE_BOOTSTRAP_PASSWORD",
            "AdminRoleName": "admin",
            "AdminRolePermissions": ["*"]
          }
        }
      }
    ]
  }
}
```

This maps to `Elsa.Identity.ShellFeatures.DefaultAdminUserFeature` and configures `DefaultAdminUserOptions` at startup.

### Legacy feature system (code-first)

When using the legacy feature system (module configuration in code), call `UseDefaultAdmin` while configuring `Identity`.

```csharp
services.AddElsa(elsa =>
{
    elsa
        .UseIdentity(identity =>
        {
            identity.TokenOptions += options =>
            {
                options.SigningKey = builder.Configuration.GetRequiredSection("Identity:Tokens")["SigningKey"]!;
            };

            identity.UseDefaultAdmin(admin => admin
                .WithAdminUserName("admin")
                .WithAdminPassword("REPLACE_WITH_SECURE_BOOTSTRAP_PASSWORD")
                .WithAdminRoleName("admin")
                .WithAdminRolePermissions(new List<string> { "*" }));
        })
        .UseDefaultAuthentication();
});
```

You can also use the shorthand overload:

```csharp
identity.UseDefaultAdmin("admin", "REPLACE_WITH_SECURE_BOOTSTRAP_PASSWORD", "admin", new List<string> { "*" });
```

### Options

Both configuration styles set `Elsa.Identity.Options.DefaultAdminUserOptions`:

| Option | Default | Notes |
| --- | --- | --- |
| `AdminUserName` | `""` | Required to create the user. |
| `AdminPassword` | `""` | Required to create the user. Leading and trailing whitespace is trimmed before hashing. Elsa enforces no length or complexity rules, so choose a strong value. |
| `AdminRoleName` | `"admin"` | The role is stored with this value as both its id and its name. If it is blank, nothing is created. |
| `AdminRolePermissions` | `["*"]` | Permissions granted to the admin role. |

Code-first hosts do not bind a `DefaultAdminUser` configuration section automatically. Read the values from your own configuration or secret store and pass them to `UseDefaultAdmin`.

### Operational notes

- The initializer runs as a background task when each tenant is activated, and it is idempotent, so it is safe to leave configured.
- If the role already exists, any configured permissions it lacks are added. Existing permissions are never removed.
- If a user with `AdminUserName` already exists, it is left unchanged. Changing `AdminPassword` later does not change the stored password, so rotate the password with `PUT /identity/users/{id}`.
- If `AdminUserName` or `AdminPassword` is empty, the role is still created or updated, but user creation is skipped with a warning.
- Do not keep development defaults in production, and prefer environment variables or a secret manager for admin credentials.
- If no users exist and neither a default admin (`AdminUserName` and `AdminPassword`) nor an admin API key is configured, startup logs an error naming both options. Until one of them is configured, unauthenticated requests to permission-protected management endpoints get 401, and authenticated callers without the required permission get 403.

## Admin API Key Bootstrap

You can also bootstrap with the built-in `AdminApiKeyProvider`. It accepts one explicitly configured key and is disabled unless you configure one. Requests send the key as `Authorization: ApiKey <key>`.

- Shell-based hosts set `AdminApiKey` on the `DefaultAuthentication` shell feature. The key gets `*` permissions and the owner name `admin`. `UseDevelopmentAdminApiKey: true` instead enables the all-zero development key (`00000000-0000-0000-0000-000000000000`), and it takes precedence over `AdminApiKey`. Never enable it outside local development.
- Code-first hosts call `UseAdminApiKey(key)` on `DefaultAuthenticationFeature`, for example `elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey(apiKey))`. `UseAdminApiKey(options => ...)` configures `AdminApiKeyOptions` directly: `ApiKey`, `OwnerName` (default `admin`) and `Permissions` (default `["*"]`). `UseDevelopmentAdminApiKey()` enables the all-zero development key.

Enabling the admin API key replaces the default, application-based `DefaultApiKeyProvider`. While it is enabled, API keys issued to applications through `/identity/applications` are not accepted. Treat the admin key as a bootstrap or break-glass credential: create users, roles and applications with it, then remove it.

## Secret Hashing

New identity passwords, client secrets, and API keys are hashed with PBKDF2-SHA256 using 600,000 iterations, a per-record salt, and version metadata. Existing legacy SHA-256 hashes remain valid and are upgraded opportunistically after a successful user login or API-key validation.

## External Authentication Compatibility

External Authentication is additive to Elsa Identity:

- Existing `/identity/login`, `/identity/refresh-token` and `/identity/logout` contracts remain the direct local-credential flow. `POST /identity/logout` revokes the sign-in session of the refresh token in its body; see [Signing Out](../../../doc/wiki/identity-tenancy-security.md#signing-out).
- The optional broker exposes separate local and external completion endpoints that return a short-lived, PKCE-bound authorization code before issuing Elsa credentials.
- Externally provisioned users may have no local password hash or salt. Such users fail direct local login with the same public result as any other invalid credential.
- Elsa remains the issuer of access tokens and the authority for their `permissions` claim, regardless of how the user authenticated.

See [the External Authentication migration guide](../../../doc/migrations/external-authentication.md) before changing a Studio host from direct OpenID Connect to brokered mode.
