# Breaking change: refresh-token user resolution

`/identity/refresh-token` now resolves the caller only when every `sub` and inbound-mapped `NameIdentifier` value agrees on one non-blank id. A missing, blank, conflicting, or unknown subject is refused with **401**, the same as an invalid token. Previously a missing user produced `200` with `isAuthenticated: false`, and the user was looked up by name — so deleting and recreating an account with the same name inherited still-valid refresh tokens.

3.0–3.7 also issued refresh tokens without `sub`, but the refresh scheme already rejects those because they lack `token_use`. 3.8.0-preview1 is the only release whose refresh-scheme-accepted tokens lacked `sub`, and they had a 2-hour lifetime, so no Elsa-issued refresh token still in use lacks a subject. Clients that treated `200` + `isAuthenticated: false` as "please sign in again" should treat `401` the same way.

See also [authorization-model.md](authorization-model.md) and [identity-tenancy-security.md](../wiki/identity-tenancy-security.md).
