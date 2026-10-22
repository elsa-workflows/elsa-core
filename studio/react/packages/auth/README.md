# Elsa React Studio authentication Preview

Shared authentication strategies, React context and permission checks for Studio
modules. This is an unpublished 3.11 Preview workspace package. Configuration
types and schemas are exported by `@elsa-workflows/react-studio-sdk/config`.

The host creates a strategy with `createAuthStrategy(authConfig, serverConfig)`
and supplies it to `<AuthProvider strategy={strategy}>`. Modules call `useAuth()`
and `usePermissions()` from this package, sharing the host's context and current
server. Modules must not create a second provider or store tokens themselves.

The existing Elsa Identity, API key, OIDC, Elsa External Authentication and no-auth
strategies are retained. OIDC uses authorization code with PKCE and resolves the
default callback against the host's `<base>` mount. Tokens remain scoped by server
URL. The package exposes advisory client permission checks; the server remains
responsible for authorizing every request.
