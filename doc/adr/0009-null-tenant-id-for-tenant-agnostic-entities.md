# 9. Null Tenant ID for Tenant-Agnostic Entities

Date: 2026-01-31

## Status

Accepted

## Context

The multitenancy system in Elsa supports both tenant-specific and tenant-agnostic entities. The convention established in ADR-0008 uses an empty string (`""`) as the tenant ID for the default tenant. However, we also need a way to represent entities that are **tenant-agnostic** - entities that should be visible and accessible across all tenants.

Previously, the system did not properly distinguish between:
1. **Default tenant entities** (should only be visible to the default tenant with `TenantId = ""`)
2. **Tenant-agnostic entities** (should be visible to all tenants regardless of their tenant context)

This caused issues where:
- Activity descriptors for workflow definitions with empty/null tenant IDs were not accessible when a different tenant context was active
- EF Core query filters only matched records with an exact tenant ID match, excluding tenant-agnostic records
- The `ActivityRegistry` methods did not properly handle tenant-agnostic descriptors

## Decision

We will use `null` as the tenant ID to represent **tenant-agnostic entities** - entities that should be accessible across all tenants. This decision includes:

1. **Convention**:
   - `null` = tenant-agnostic (visible to all tenants)
   - Empty string `""` = default tenant (visible only to default tenant)
   - Any other string = specific tenant (visible only to that tenant)

2. **EF Core Query Filter**: Update `SetTenantIdFilter` to return records where:
   - `TenantId == dbContext.TenantId` (tenant-specific match), OR
   - `TenantId == null` (tenant-agnostic records)

3. **Activity Registry**: Update all query methods to include descriptors where:
   - `TenantId == tenantAccessor.TenantId` (tenant-specific match), OR
   - `TenantId == null` (tenant-agnostic descriptors)

4. **Composite Keys**: Make `TenantId` nullable in composite keys (e.g., `(string? TenantId, string Type, int Version)`)

## Consequences

### Positive

- **Tenant-agnostic entities**: System-level entities (like built-in activities) can be shared across all tenants without duplication
- **Proper isolation**: Tenant-specific entities remain isolated to their respective tenants
- **Clear semantics**: `null` explicitly means "no tenant restriction" while empty string means "default tenant"
- **Database efficiency**: Tenant-agnostic entities are stored once and queried once, not duplicated per tenant
- **Consistent behavior**: Both EF Core queries and in-memory collections (like `ActivityRegistry`) follow the same tenant filtering rules

### Negative

- **Two conventions**: Developers must understand the distinction between `null` (agnostic) and `""` (default tenant)
- **Nullable handling**: Code must properly handle nullable `TenantId` fields in composite keys and comparisons

### Neutral

- This convention aligns with common multitenancy patterns where `null` represents "global" or "shared" resources
- The distinction between default tenant and tenant-agnostic is necessary for proper multitenancy architecture
