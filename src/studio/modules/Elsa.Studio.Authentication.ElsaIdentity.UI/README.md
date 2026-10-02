# Elsa.Studio.Authentication.ElsaIdentity.UI

Provides the Elsa Identity login UI for Elsa Studio.

## What you get
- An Elsa Identity login-method catalog and credentials component for the shared `/login` shell
- A default unauthorized component provider that redirects to `/login?returnUrl=...`
- A simple app-bar login component (optional)

## Usage
### 1) Configure ElsaIdentity (Elsa Identity)
This UI module assumes you are using **Elsa Identity** (username/password against the Elsa backend API).

In your host (Server or WASM), register ElsaIdentity and the identity flow, then add the UI:

```csharp
// Platform services:
builder.Services.AddElsaIdentity();

// Core + Elsa Identity flow:
builder.Services.AddElsaIdentityCore().UseElsaIdentityAuth();

// UI (this package):
builder.Services.AddElsaIdentityUI();

// Shared login shell (composition root):
builder.Services.AddAuthenticationUI();
```

### 2) Switching providers (hosts)
Elsa Studio hosts can switch providers using configuration:

- `Authentication:Provider` = `OpenIdConnect` or `ElsaIdentity`
- `Authentication:OpenIdConnect` contains OIDC settings when using `OpenIdConnect`

> Note: You still need to configure the backend URL to point to your Elsa API.
