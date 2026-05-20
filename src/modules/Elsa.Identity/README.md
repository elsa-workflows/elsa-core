# Elsa.Identity

## JWT Signing Key Configuration

Identity token signing requires a secure random key. Configure it through environment variables or a secrets manager and keep it out of committed appsettings files.

- Code-first hosts using `Identity:Tokens` should set `Identity__Tokens__SigningKey`.
- Shell-based hosts should set the shell feature path, for example `CShells__Shells__Default__Features__Identity__SigningKey`.
- Production startup rejects missing keys, keys shorter than 32 ASCII characters, and known public defaults. Known public defaults are tolerated only in the explicit `Development` or `Demo` environments.

## Default Admin User Bootstrap

Elsa supports bootstrapping an initial admin role and user through the `DefaultAdminUser` feature.

This is the recommended way to initialize identity access now that user-management endpoints are permission-based and no longer rely on the `SecurityRoot` policy.

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

### Operational notes

- The initializer is idempotent: existing admin role/user are not recreated.
- Do not keep development defaults in production.
- Prefer environment variables or a secret manager for admin credentials.
- After first bootstrap, rotate credentials according to your security policy.
- Localhost requests no longer satisfy `SecurityRoot` by default. Legacy localhost bootstrap requires an explicit opt-in: call `EnableLocalHostPermissionGrantForSecurityRoot()` in code-first configuration or set `EnableLocalHostPermissionGrant` on the shell `DefaultAuthentication` feature; prefer `DefaultAdminUser` instead.
