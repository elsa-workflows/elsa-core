# 6. Tenant Deleted Event

Date: 2025-08-05

## Status

Accepted

## Context

As outlined in issue [#6661](https://github.com/elsa-workflows/elsa-core/issues/6661), there is a need to differentiate between **Tenant Deactivating** and **Tenant Deleting** events. 

Currently, the `TenantDeactivated` event is used to unregister timer-based triggers. However, this causes the triggers to be unregistered when the application host shuts down, which is not the desired behavior. Instead, we want the triggers to remain registered until the tenant is explicitly deleted.

## Decision

To address this, we will introduce a new event called `TenantDeleted`. This event will be raised when a tenant is deleted and will be responsible for unregistering timer-based triggers. This ensures that the triggers remain active until the tenant is explicitly deleted.

## Consequences

- Timer-based triggers will no longer be unregistered during tenant deactivation. Instead, they will remain active until the tenant is deleted.
- The `TenantDeactivated` event will continue to be used for deactivating tenants without affecting the registration of timer-based triggers.
- The new `TenantDeleted` event will specifically handle the cleanup of resources associated with a tenant when it is deleted, ensuring a clear separation of responsibilities.
