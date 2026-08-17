# 11. Use identity-neutral participant references for User Tasks

Date: 2026-08-17

## Status

Accepted

## Context

Elsa Core does not own a universal user model. `Elsa.Identity` is one optional host feature, while embedded deployments may use an application database, Entra ID, LDAP, or another identity platform. Coupling task assignment to one user table would make the User Tasks module unusable or duplicative for those hosts.

## Decision

User Tasks store opaque participant references composed of tenant, provider namespace, participant type, and external ID, with an optional non-authoritative display snapshot. The module has no required `Elsa.Identity` dependency or foreign key.

Authentication mapping, live group membership, directory lookup, and task authorization are replaceable host contracts. The built-in adapter maps namespaced claims. Directory resolution failure does not invalidate an exact live participant claim or fault task activation.

## Consequences

Hosts can integrate their existing identities without synchronization into Elsa. Every authorization and query path must remain tenant- and namespace-aware, and participant display may degrade to an opaque ID when no directory is available. Database constraints cannot enforce existence in an external identity system; policy and integration tests enforce relationships instead.
