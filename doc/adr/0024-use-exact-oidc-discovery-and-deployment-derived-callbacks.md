# Use exact OIDC discovery and deployment-derived callbacks

**Status**: Accepted; refines [0018](0018-separate-provider-trust-from-broker-invariants.md)

**Date**: 2026-07-24

## Decision

The v1 OpenID Connect adapter accepts one exact absolute HTTPS `discoveryUrl` as the safe/default trust source. When deployment policy permits and the caller holds the unsafe-provider-trust permission, Advanced settings may override the discovered issuer, authorization/token endpoints, or signing keys with explicit confirmation, persistent warning, and notification. Elsa derives the provider callback from its deployment-owned external base address, fixed callback route, and immutable logical Connection Key so source ownership changes do not alter it.

The upstream OIDC client is confidential, uses authorization-code flow with mandatory S256 PKCE, and supports `client_secret_basic` and `client_secret_post`. Advanced values change trust inputs but cannot disable state/correlation/nonce, signature, issuer, audience/lifetime, callback, PKCE, or secret-redaction validation. Secrets use Managed or External Secret Bindings. Elsa supports only Elsa-initiated login/logout and discards provider access/refresh tokens after callback processing; it may retain only the protected minimum artifact required for configured upstream logout.

## Rationale

Exact discovery and derived callbacks reduce mismatched configuration and keep deployment routing out of mutable connection data. A narrow confidential-client contract provides interoperable secure defaults without exposing manual trust controls that could undermine the broker.
