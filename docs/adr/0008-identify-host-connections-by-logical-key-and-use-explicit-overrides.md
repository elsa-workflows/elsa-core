# Identify host connections by logical key and use explicit overrides

**Status**: Accepted; supersedes the scope and source-precedence portions of [0002](0002-compose-a-scoped-connection-registry.md)

**Date**: 2026-07-24

## Decision

External Authentication v1 manages Identity Provider Connections host-wide within the currently connected Elsa server environment. This environment is context, not a persisted or editable connection field. Each connection has a database/configuration record ID for management and transient broker state plus an immutable logical Connection Key that survives source changes, archive/restore, and persisted overrides. External Identity Links and long-lived sessions use the logical key; links use `(targetTenantId, connectionKey, issuer, subject)`. Host-wide connection administration does not remove Elsa User tenancy.

Configuration remains the deployment baseline. Studio may create an explicit persisted override for the same key, but the override is a complete replacement document: no settings, secrets, policy, presentation, or lifecycle fields are merged across sources. The effective registry reports both provenance and the shadow relationship. A disabled override continues to shadow the baseline and therefore disables that logical connection. Archiving or removing the override deliberately reveals the configuration connection again; restoring it resumes the full shadow.

Tenant-specific connection administration is deferred. Adding it later requires an explicit specification and migration; v1 does not pre-model a Deployment Target entity or editable discriminator.

## Rationale

An immutable logical key makes a connection recognizable across configuration and Studio ownership without exposing database row identity as a public authentication identifier. Full shadowing is predictable and avoids security-sensitive field inheritance. Host-wide scope keeps v1 operationally understandable while preserving a named extension point for tenant targeting.
