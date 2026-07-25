# Match unlinked identities with trusted user matchers

**Status**: Accepted; supersedes the v1 permission-mapping portion of [0004](0004-separate-external-identity-from-elsa-authorization.md)

**Date**: 2026-07-24

## Decision

Elsa remains the only authority that expands Roles into `permissions` claims. External Authentication v1 does not expose claim-to-permission, group-to-permission, wildcard, or pass-through mapping in Studio.

Each connection selects one Unlinked Identity Policy. The generic matcher-based policy selects exactly one deployed `IExternalUserMatcher`. It supplies only the matcher's declared required claims, holds them ephemerally, and accepts a single unambiguous existing-user result. No match follows the connection's configured `Reject` or `CreateUser` fallback. Ambiguous matches and matcher errors reject authentication. V1 ships the framework but no Elsa first-party verified-email matcher.

`defaultRoleIds` are static configuration used only when `CreateUser` creates a new Elsa User, including the matcher policy's create-user no-match fallback. Save-time role-assignment authorization applies to those static roles. Matchers never select roles or permissions.

## Rationale

Keeping user matching inside an explicit Unlinked Identity Policy prevents accidental email/name linking and lets deployments add narrowly reviewed matchers later. Static create-user roles preserve Elsa's authorization boundary without deriving authorization from claims.
