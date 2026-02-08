# 9. Asterisk Sentinel Value for Tenant-Agnostic Entities

Date: 2026-01-31

## Status

Accepted

## Context

The multitenancy system in Elsa supports both tenant-specific and tenant-agnostic entities. The convention established in ADR-0008 uses an empty string (`""`) as the tenant ID for the default tenant. However, we also need a way to represent entities that are **tenant-agnostic** - entities that should be visible and accessible across all tenants.

Previously, the system did not properly distinguish between:
1. **Default tenant entities** (should only be visible to the default tenant with `TenantId = ""`)
2. **Tenant-agnostic entities** (should be visible to all tenants regardless of their tenant context)

This caused issues where:
- Activity descriptors for built-in activities were being wiped out when different tenants were activated
- The `ActivityRegistry` used a single global dictionary that was replaced during tenant activation via `Interlocked.Exchange`, causing race conditions
- EF Core query filters only matched records with an exact tenant ID match, excluding tenant-agnostic records
- Workflows with no explicit tenant ID could not be found when a specific tenant context was active

### Why Use a Sentinel Value Instead of Null?

We considered using `null` as the marker for tenant-agnostic entities, but chose an explicit sentinel value (`"*"`) instead for several reasons:

1. **Explicit Intent**: A sentinel value makes it crystal clear in code and database queries that an entity is intentionally tenant-agnostic, not accidentally missing a tenant assignment
2. **Simpler Composite Keys**: Avoids nullable handling complexity in composite keys like `(string TenantId, string Type, int Version)` which would become `(string? TenantId, ...)`
3. **Clearer SQL Queries**: Database queries with `TenantId = '*'` are more explicit than `TenantId IS NULL`
4. **Better Logging**: Seeing `"*"` in logs immediately signals tenant-agnostic behavior
5. **Architecture Alignment**: Works seamlessly with the three-dictionary ActivityRegistry architecture where agnostic entities have their own dedicated registry

## Decision

We will use the asterisk character (`"*"`) as a sentinel value to represent **tenant-agnostic entities** - entities that should be accessible across all tenants. This decision includes:

### 1. Convention

- `"*"` (represented by constant `Tenant.AgnosticTenantId`) = tenant-agnostic (visible to all tenants)
- `""` (represented by constant `Tenant.DefaultTenantId`) = default tenant (visible only to default tenant)
- Any other non-null string = specific tenant (visible only to that tenant)
- `null` = not yet assigned (will be normalized to either agnostic or current tenant by handlers)

### 2. Activity Registry Architecture

Implement a **three-dictionary architecture** in `ActivityRegistry` to properly isolate tenant-specific and tenant-agnostic descriptors:

- **`_tenantRegistries`**: `ConcurrentDictionary<string, TenantRegistryData>` - Per-tenant activity descriptors (e.g., workflow-as-activities)
- **`_agnosticRegistry`**: `TenantRegistryData` - Shared tenant-agnostic descriptors (e.g., built-in activities)
- **`_manualActivityDescriptors`**: `ISet<ActivityDescriptor>` - Legacy support for manually registered activities

Key behaviors:
- Descriptors with `TenantId = null` or `TenantId = "*"` are stored in `_agnosticRegistry`
- Descriptors with any other `TenantId` are stored in the corresponding tenant's registry in `_tenantRegistries`
- `RefreshDescriptorsAsync()` updates only the affected tenant's registry, not the entire global dictionary
- Find methods **always prefer tenant-specific descriptors over agnostic ones**, even if agnostic has a higher version number

### 3. EF Core Query Filter

Update `SetTenantIdFilter` to return records where:
- `TenantId == dbContext.TenantId` (tenant-specific match), OR
- `TenantId == "*"` (tenant-agnostic records)

### 4. Entity Handlers

Update `ApplyTenantId` handler to:
- Preserve `TenantId = "*"` (don't overwrite tenant-agnostic entities)
- Only apply current tenant ID to entities with `TenantId = null`

**Important: This is a security-by-default design.** Entities with `null` tenant ID are **never** automatically converted to tenant-agnostic (`"*"`). They are always assigned to the current tenant context. To create tenant-agnostic database entities, developers **must explicitly** set `TenantId = "*"`. This prevents accidental data leakage across tenants.

### 5. Reserved Character Constraint

The asterisk character `"*"` is **reserved** and cannot be used as an actual tenant ID. Tenant creation and validation logic should reject any attempt to create a tenant with ID `"*"`.

## Consequences

### Positive

- **Explicit tenant-agnostic marking**: The `"*"` sentinel makes intent clear in code, logs, and database
- **Proper tenant isolation**: The three-dictionary architecture prevents tenant activation from wiping out other tenants' descriptors
- **No nullable handling**: Composite keys remain `(string TenantId, ...)` instead of `(string? TenantId, ...)`
- **Tenant precedence**: Tenant-specific descriptors always take precedence over agnostic ones, allowing tenants to override built-in activities
- **Dynamic tenant management**: Tenants can be activated and deactivated at runtime without affecting each other
- **Database efficiency**: Tenant-agnostic entities are stored once and accessible to all tenants
- **Clear SQL queries**: `WHERE TenantId = current_tenant OR TenantId = '*'` is more explicit than null checks
- **Thread safety**: Per-tenant dictionaries eliminate the need for `Interlocked.Exchange` and its race conditions

### Negative

- **Reserved character**: The `"*"` character cannot be used as an actual tenant ID (low impact, as tenant IDs are typically alphanumeric)
- **Two conventions**: Developers must understand the distinction between `"*"` (agnostic) and `""` (default tenant)
- **Migration complexity**: Existing systems using `null` for agnostic entities would need data migration

### Neutral

- Using a sentinel value for special cases is a common pattern in software architecture
- The distinction between default tenant and tenant-agnostic is fundamental to proper multitenancy design
- The three-dictionary architecture adds complexity but is necessary for correct tenant isolation

## Implementation Notes

### Semantic Flow: From Entity Creation to Query

Understanding how tenant IDs flow through the system is critical:

```
┌─────────────────────────────────────────────────────────────┐
│ Entity Creation / Deserialization                           │
├─────────────────────────────────────────────────────────────┤
│ TenantId = null  →  Not yet assigned                        │
│ TenantId = "*"   →  Explicitly agnostic                     │
│ TenantId = ""    →  Default tenant                          │
│ TenantId = "foo" →  Specific tenant "foo"                   │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ ApplyTenantId Handler (before DB save)                      │
├─────────────────────────────────────────────────────────────┤
│ TenantId = "*"   →  PRESERVED (agnostic)                    │
│ TenantId = null  →  SET to current tenant from context      │
│ TenantId = ""    →  PRESERVED (default tenant)              │
│ TenantId = "foo" →  PRESERVED (specific tenant)             │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ Database Storage                                             │
├─────────────────────────────────────────────────────────────┤
│ TenantId = "*"   →  Stored as "*" (agnostic)                │
│ TenantId = ""    →  Stored as "" (default tenant)           │
│ TenantId = "foo" →  Stored as "foo" (specific tenant)       │
│ NOTE: No null values in DB after ApplyTenantId handler      │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ SetTenantIdFilter (EF Core Query)                           │
├─────────────────────────────────────────────────────────────┤
│ Returns: TenantId == current_tenant OR TenantId == "*"      │
│ Result: Tenant-specific records + agnostic records          │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ ActivityRegistry (In-Memory)                                │
├─────────────────────────────────────────────────────────────┤
│ null or "*"  → _agnosticRegistry (shared)                   │
│ TenantId=""  → _tenantRegistries[""] (default)              │
│ TenantId=X   → _tenantRegistries[X] (specific tenant X)     │
└─────────────────────────────────────────────────────────────┘
```

**Key Points:**
- **`null` is transient**: It only exists during entity creation/deserialization before `ApplyTenantId` runs
- **`"*"` is permanent**: Once set, it's preserved and stored in the database as-is
- **`NormalizeTenantId()` converts `null` → `""`**: This ensures `null` becomes the default tenant, NOT agnostic
- **Database has no nulls**: After `ApplyTenantId` handler, all entities have non-null tenant IDs
- **EF Core filters check for `"*"`**: The query filter explicitly compares against the string `"*"`, not null
- **ActivityRegistry accepts both**: For flexibility, in-memory registry treats both `null` and `"*"` as agnostic

### ActivityRegistry Behavior

When `Find(string type)` is called:
1. First check the current tenant's registry for matching descriptors
2. If found, return the highest version from the tenant-specific registry
3. Only if no tenant-specific descriptor exists, fall back to the agnostic registry
4. This ensures tenant-specific customizations always take precedence

The `GetOrCreateRegistry()` method treats both `null` and `"*"` as agnostic:
```csharp
if (tenantId is null or Tenant.AgnosticTenantId)
    return _agnosticRegistry;
```

This provides flexibility for in-memory operations where activity descriptors might temporarily have `null` tenant IDs before normalization.

### Activity Descriptors vs Database Entities: Different Rules

The system treats **in-memory activity descriptors** and **persistent database entities** differently for security and architectural reasons:

#### In-Memory Activity Descriptors (Ephemeral)
- Created on startup by activity providers
- **Built-in activities** (WriteLine, SetVariable, etc.): Created with `TenantId = null` by `ActivityDescriber`
- **Workflow-as-activities**: Created with `TenantId = definition.TenantId` by `WorkflowDefinitionActivityDescriptorFactory`
- `null` is acceptable here because descriptors are recreated on each startup and mapped to `_agnosticRegistry`
- No security risk: descriptors don't contain sensitive data, just metadata about activity types

#### Persistent Database Entities (WorkflowDefinition, etc.)
- Stored permanently in the database
- **Must explicitly set `TenantId = "*"`** to be tenant-agnostic
- `TenantId = null` is **never** converted to `"*"` - always assigned to current tenant
- **Security-by-default**: Prevents accidental data leakage across tenants
- A developer who forgets to set `TenantId` creates a tenant-specific entity, not a global one

**Example - Creating Tenant-Agnostic Workflow:**
```json
{
  "tenantId": "*",
  "name": "GlobalApprovalWorkflow",
  "description": "Shared across all tenants",
  "root": { ... }
}
```

**Why This Asymmetry Is Important:**
1. **Safety**: Database entities with null tenant ID default to current tenant (safe)
2. **Explicitness**: Tenant-agnostic entities must be intentional (require `"*"`)
3. **Different lifecycles**: Descriptors are ephemeral, entities are persistent
4. **Backward compatibility**: Built-in activities work without modification

### Workflow Import Behavior

When workflows are imported from providers (e.g., blob storage):
- Workflows without an explicit `tenantId` field in their JSON have `TenantId = null`
- During import, these are normalized to the current tenant ID via `NormalizeTenantId()` extension
- When saved to database, `ApplyTenantId` handler assigns the current tenant from context
- To create truly tenant-agnostic workflows, explicitly set `"tenantId": "*"` in the workflow JSON
- The `"*"` value will be preserved through import, save, and query operations

### Testing Considerations

- Component tests use the default tenant (`""`)
- Built-in activities use the agnostic marker (`"*"`)
- Tenant-specific tests should create explicit tenant contexts to verify proper isolation
- Unit tests should verify that `"*"` is preserved through save operations
- Integration tests should verify that `"*"` entities are returned for all tenant contexts
