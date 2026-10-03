# Breaking change: refresh-token user resolution

`/identity/refresh-token` now resolves the caller only by the token's `sub` (or the inbound-mapped `NameIdentifier`). A missing, blank, or unknown subject is refused with **401**, the same as an invalid token. Previously a missing user produced `200` with `isAuthenticated: false`, and the user was looked up by name — so deleting and recreating an account with the same name inherited still-valid refresh tokens.

Only 3.8.0-preview1 issued Elsa refresh tokens without `sub`, and they had a 2-hour lifetime, so no Elsa-issued refresh token still in use lacks a subject. Clients that treated `200` + `isAuthenticated: false` as "please sign in again" should treat `401` the same way.

See also [authorization-model.md](authorization-model.md) and [identity-tenancy-security.md](../wiki/identity-tenancy-security.md).
