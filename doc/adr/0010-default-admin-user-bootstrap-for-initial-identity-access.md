# 10. Default Admin User Bootstrap for Initial Identity Access

Date: 2026-03-15

## Status

Accepted

## Context

Elsa exposes user-management endpoints for creating, listing, updating, and deleting users. Historically, privileged identity bootstrap concerns were tied to the `SecurityRoot` policy. That approach coupled two separate concerns:

1. **Bootstrap of the first administrative identity**
2. **Ongoing authorization for user management operations**

This coupling created unnecessary friction for integrators:

- Integrators had to understand and satisfy the `SecurityRoot` policy before they could manage users.
- The bootstrap story for the first administrator was implicit rather than explicit.
- User-management endpoints mixed initialization concerns with normal permission-based authorization.
- The `SecurityRoot` policy remained overloaded with responsibilities beyond truly sensitive identity operations.

At the same time, Elsa now provides a dedicated `DefaultAdminUser` feature that can provision an initial administrative role and user during application startup. This gives integrators a clear, explicit bootstrap mechanism that does not depend on runtime access to user-management endpoints.

## Decision

We will treat **initial administrator bootstrap** and **regular user management** as separate concerns.

### 1. Use `DefaultAdminUser` for Initial Bootstrap

Integrators who need an initial administrator account should use the `DefaultAdminUser` feature.

This feature is responsible for:

- Creating the configured admin role if it does not already exist
- Creating the configured admin user if it does not already exist
- Assigning the configured admin role to that user
- Allowing bootstrap through application configuration and startup, instead of through a special runtime policy gate

### 2. Remove the `SecurityRoot` Requirement from User-Management Endpoints

The following user-management endpoints are no longer gated by the `SecurityRoot` policy:

- `POST /identity/users`
- `GET /identity/users`
- `PUT /identity/users/{id}`
- `DELETE /identity/users/{id}`

These endpoints are authorized through their normal endpoint permissions (for example `create:user`, `read:user`, `update:user`, and `delete:user`).

### 3. Keep `SecurityRoot` for Narrow, Explicit Privileged Operations

The `SecurityRoot` policy remains available for operations that are still considered privileged bootstrap or security-root capabilities, such as:

- secret hashing utilities
- privileged application creation
- privileged role creation
- other explicitly designated security-root operations

This narrows the purpose of `SecurityRoot` and keeps it from being the default answer to first-user provisioning.

## Consequences

### Positive

- **Clear bootstrap story**: Integrators have an explicit, startup-driven way to provision the first administrator.
- **Cleaner separation of concerns**: Initial identity setup is no longer entangled with day-to-day user CRUD operations.
- **Permission-first authorization**: User-management endpoints now follow the same permission-oriented model as the rest of the API.
- **Reduced operational friction**: Environments can bootstrap an admin user without relying on a special runtime policy.
- **Better extensibility**: Integrators can replace or customize admin bootstrap through feature configuration rather than endpoint-specific access assumptions.

### Negative

- **More responsibility for integrators**: Deployments must intentionally configure `DefaultAdminUser` or provide another trusted bootstrap path if no administrator exists yet.
- **Migration awareness**: Existing documentation and operational guidance that referenced `SecurityRoot` for user bootstrap must be updated.
- **Potential misconfiguration risk**: A weak or default admin password remains a deployment concern and must be handled carefully by integrators.

### Neutral

- `SecurityRoot` still exists, but its scope is narrower and more explicit.
- User-management endpoints continue to require authentication and matching permissions; only the extra `SecurityRoot` gate is removed.
- Bootstrap behavior is now feature-driven rather than endpoint-driven.

## Implementation Notes

- `DefaultAdminUserFeature` and `AdminUserInitializer` provide the startup-time bootstrap mechanism.
- User-management endpoints should document only their permission requirements, not `SecurityRoot`.
- Authentication configuration may still use `SecurityRoot` for operations that intentionally remain root-level.
- Integrators should prefer environment-specific configuration for default admin credentials and rotate them according to their security practices.

