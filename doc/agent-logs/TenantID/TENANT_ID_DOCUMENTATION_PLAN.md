# Tenant ID Convention: Documentation Improvement Plan

**Goal:** Make the current tenant ID convention clearer through better documentation without changing the implementation.

---

## Current Documentation Status

### Existing Documentation
- ✅ ADR-0008: Empty string as default tenant ID
- ✅ ADR-0009: Null tenant ID for tenant-agnostic entities
- ✅ Code comments in some files (e.g., ActivityDescriptor.cs)
- ⚠️ No wiki page
- ⚠️ Limited code comments in key locations
- ⚠️ No visual diagrams

### Gap Analysis

| Location | Current | Gap | Priority |
|----------|---------|-----|----------|
| **Code Comments** | Minimal | Need more explanations | 🔴 High |
| **Constants** | Good | Already have `Tenant.DefaultTenantId` | ✅ Done |
| **ADRs** | Excellent | Well-explained decisions | ✅ Done |
| **Wiki** | Missing | No consolidated guide | 🟠 Medium |
| **Developer Onboarding** | Missing | No checklist for new devs | 🟠 Medium |
| **Visual Diagrams** | Missing | Helpful for understanding | 🟡 Low |
| **API Docs** | Partial | Explain TenantId behavior | 🔴 High |

---

## Immediate Improvements (Ready to Implement)

### 1. Enhanced Code Comments

#### Location: `src/modules/Elsa.Common/Multitenancy/Entities/Tenant.cs`

**Current:**
```csharp
public const string DefaultTenantId = "";
```

**Improved:**
```csharp
/// <summary>
/// The tenant ID for the default tenant.
/// 
/// This is an empty string by design (ADR-0008) to:
/// 1. Support dictionary creation (null is not a valid dictionary key)
/// 2. Maintain backward compatibility with existing data
/// 3. Clearly distinguish from tenant-agnostic entities (which use null)
/// 
/// Semantics:
/// - TenantId = "" → Default tenant (visible only in default context)
/// - TenantId = null → Tenant-agnostic (visible to all tenants)
/// - TenantId = "other" → Specific tenant (visible only to that tenant)
/// 
/// See: ADR-0008 and ADR-0009
/// </summary>
public const string DefaultTenantId = "";
```

#### Location: `src/modules/Elsa.Common/Entities/Entity.cs`

**Add class-level comment:**
```csharp
/// <summary>
/// Base class for all entities that participate in Elsa's multitenancy system.
/// </summary>
/// <remarks>
/// <para>
/// Tenant ID Conventions:
/// - <c>null</c>: Tenant-agnostic entity (visible to all tenants, not stored as empty string)
/// - Empty string (<c>""</c>): Belongs to the default tenant (ADR-0008)
/// - Any other string: Belongs to a specific tenant with that ID
/// </para>
/// <para>
/// Example usage:
/// <code>
/// // System activity, visible to all tenants
/// var activity = new ActivityDescriptor { TenantId = null };
/// 
/// // Default tenant workflow
/// var workflow = new WorkflowDefinition { TenantId = Tenant.DefaultTenantId };
/// 
/// // Acme Corp specific workflow
/// var tenantWorkflow = new WorkflowDefinition { TenantId = "acme-corp" };
/// </code>
/// </para>
/// <para>
/// Query Behavior:
/// EF Core automatically applies a global query filter:
/// <c>WHERE TenantId = @currentContext OR TenantId IS NULL</c>
/// 
/// This means:
/// - Records always see their own tenant data
/// - Records always see tenant-agnostic (null) data
/// - Records never see other tenants' data
/// </para>
/// </remarks>
public abstract class Entity
{
    /// <summary>
    /// Gets or sets the tenant ID.
    /// 
    /// Null means tenant-agnostic (shared across all tenants).
    /// Empty string means default tenant.
    /// Any other value means specific tenant with that ID.
    /// 
    /// See ADR-0008 and ADR-0009 for rationale.
    /// </summary>
    public string? TenantId { get; set; }
    
    // ... rest of class
}
```

#### Location: `src/modules/Elsa.Workflows.Core/Models/ActivityDescriptor.cs`

**Improve existing comment:**
```csharp
/// <summary>
/// Tenant ID for this activity descriptor.
/// 
/// Convention:
/// - null = Tenant-agnostic (system activity, visible to all tenants)
/// - "" = Default tenant activity (visible only in default context)
/// - "tenant-id" = Specific tenant activity (visible only to that tenant)
/// 
/// See ADR-0009 for the rationale behind null for tenant-agnostic.
/// </summary>
public string? TenantId { get; set; }
```

#### Location: `src/modules/Elsa.Workflows.Core/Models/WorkflowIdentity.cs`

**Add parameter comment:**
```csharp
/// <summary>
/// Represents the unique identity of a workflow.
/// </summary>
/// <param name="DefinitionId">The workflow definition ID.</param>
/// <param name="Version">The workflow version.</param>
/// <param name="Id">The workflow instance ID.</param>
/// <param name="TenantId">
/// The tenant ID for this workflow.
/// 
/// Convention:
/// - null = Tenant-agnostic (visible to all tenants)
/// - "" = Default tenant (visible only in default context)
/// - "tenant-id" = Specific tenant (visible only to that tenant)
/// 
/// Defaults to null (tenant-agnostic) if not specified.
/// </param>
public record WorkflowIdentity(
    string DefinitionId, 
    int Version, 
    string Id, 
    string? TenantId = null)
{
}
```

---

### 2. Enhanced Extension Method Documentation

#### Location: `src/modules/Elsa.Common/Multitenancy/Extensions/TenantsProviderExtensions.cs`

**Current:**
```csharp
/// <summary>
/// Normalizes a tenant ID by converting null to empty string, ensuring consistency with the default tenant convention.
/// </summary>
public static string NormalizeTenantId(this string? tenantId) => tenantId ?? Tenant.DefaultTenantId;
```

**Improved:**
```csharp
/// <summary>
/// Normalizes a tenant ID by converting null to empty string.
/// </summary>
/// <remarks>
/// <para>
/// This method enforces the convention that null and empty string are both treated as the default tenant
/// in string-based contexts (like dictionaries).
/// </para>
/// <para>
/// Behavior:
/// <list type="table">
/// <item>
/// <term>Input</term>
/// <term>Output</term>
/// </item>
/// <item>
/// <term>null</term>
/// <term>""</term>
/// </item>
/// <item>
/// <term>""</term>
/// <term>""</term>
/// </item>
/// <item>
/// <term>"acme-corp"</term>
/// <term>"acme-corp"</term>
/// </item>
/// </list>
/// </para>
/// <para>
/// Usage:
/// <code>
/// // In dictionary creation - null is not a valid dictionary key
/// var tenantDict = tenants.ToDictionary(x => x.Id.NormalizeTenantId());
/// 
/// // In comparisons
/// var currentTenantId = tenantAccessor.Tenant?.Id.NormalizeTenantId();
/// </code>
/// </para>
/// <para>
/// Important: This is a normalization for string-based operations only.
/// In the database and entity objects, null and "" retain their distinct meanings:
/// - null = tenant-agnostic
/// - "" = default tenant
/// </para>
/// <para>
/// See ADR-0008 for rationale.
/// </para>
/// </remarks>
public static string NormalizeTenantId(this string? tenantId) => tenantId ?? Tenant.DefaultTenantId;
```

---

### 3. Enhanced ActivityRegistry Documentation

#### Location: `src/modules/Elsa.Workflows.Core/Services/ActivityRegistry.cs`

**Add class-level comment:**
```csharp
/// <summary>
/// Registry for activity descriptors in the Elsa workflow engine.
/// </summary>
/// <remarks>
/// <para>
/// The ActivityRegistry maintains a collection of activity descriptors organized by tenant context.
/// It supports three types of activities:
/// </para>
/// <list type="bullet">
/// <item>
/// <term>System Activities (TenantId = null)</term>
/// <description>
/// Built-in activities available to all tenants (e.g., If, While, WriteLine).
/// These are the core activities that every tenant can use.
/// </description>
/// </item>
/// <item>
/// <term>Default Tenant Activities (TenantId = "")</term>
/// <description>
/// Activities registered in the default tenant context.
/// Only visible when querying from the default tenant.
/// </description>
/// </item>
/// <item>
/// <term>Specific Tenant Activities (TenantId = "tenant-id")</term>
/// <description>
/// Activities registered for a specific tenant.
/// Only visible when querying from that tenant's context.
/// </description>
/// </item>
/// </list>
/// <para>
/// Lookup Strategy:
/// When finding an activity, the registry uses a two-step approach:
/// <code>
/// 1. Try tenant-specific: Find(type, version, "acme-corp")
/// 2. Fall back to system: Find(type, version, null)
/// </code>
/// This allows tenants to use system activities while also supporting
/// tenant-specific overrides.
/// </para>
/// <para>
/// See ADR-0008 and ADR-0009 for the tenant ID convention rationale.
/// </para>
/// </remarks>
public class ActivityRegistry(/* ... */) : IActivityRegistry
```

---

### 4. Enhanced SetTenantIdFilter Documentation

#### Location: `src/modules/Elsa.Persistence.EFCore.Common/EntityHandlers/SetTenantIdFilter.cs`

**Add class documentation:**
```csharp
/// <summary>
/// Applies a global query filter to enforce tenant isolation in EF Core.
/// </summary>
/// <remarks>
/// <para>
/// This filter is applied automatically to all entities and ensures that:
/// 1. Each query only returns data for the current tenant context
/// 2. Tenant-agnostic data (TenantId = null) is visible to all tenants
/// 3. System resources are never isolated by tenant
/// </para>
/// <para>
/// Filter Logic:
/// <code>
/// WHERE (TenantId = @currentTenantId OR TenantId IS NULL)
/// </code>
/// </para>
/// <para>
/// Example Query Results (for "acme-corp" context):
/// <list type="table">
/// <item>
/// <term>TenantId Value</term>
/// <term>Included?</term>
/// </item>
/// <item>
/// <term>"acme-corp"</term>
/// <term>✓ Yes (matches current context)</term>
/// </item>
/// <item>
/// <term>null</term>
/// <term>✓ Yes (tenant-agnostic/system)</term>
/// </item>
/// <item>
/// <term>""</term>
/// <term>✗ No (belongs to default tenant)</term>
/// </item>
/// <item>
/// <term>"customer-a"</term>
/// <term>✗ No (different tenant)</term>
/// </item>
/// </list>
/// </para>
/// <para>
/// See ADR-0008 and ADR-0009 for rationale.
/// </para>
/// </remarks>
public class SetTenantIdFilter : IEntityModelCreatingHandler
```

---

## Medium-Term Improvements

### 5. Developer Onboarding Checklist

**Location:** `CONTRIBUTING.md` or `docs/multitenancy.md`

**Content:**
```markdown
## Multitenancy Concepts

New developers should understand Elsa's tenant ID conventions:

### Understanding Tenant IDs

Elsa uses three distinct tenant ID values to represent different scopes:

1. **Null (`null`)** - Tenant-Agnostic
   - Visible to all tenants
   - Used for system activities, built-in features
   - Example: `ActivityDescriptor { TenantId = null }`

2. **Empty String (`""`)** - Default Tenant
   - Visible only when running in the default tenant context
   - Constant: `Tenant.DefaultTenantId`
   - Example: `new Workflow { TenantId = Tenant.DefaultTenantId }`

3. **Any Other String** - Specific Tenant
   - Visible only to that specific tenant
   - Example: `new Workflow { TenantId = "acme-corp" }`

### Key Files to Review

When working with multitenancy, review these files in order:

1. **ADRs** (decision rationale)
   - `doc/adr/0008-empty-string-as-default-tenant-id.md`
   - `doc/adr/0009-null-tenant-id-for-tenant-agnostic-entities.md`

2. **Core Classes**
   - `src/modules/Elsa.Common/Multitenancy/Entities/Tenant.cs` - Constants and default tenant
   - `src/modules/Elsa.Common/Entities/Entity.cs` - Base entity with TenantId property

3. **Implementation**
   - `src/modules/Elsa.Persistence.EFCore.Common/EntityHandlers/SetTenantIdFilter.cs` - Query filter
   - `src/modules/Elsa.Workflows.Core/Services/ActivityRegistry.cs` - In-memory registry logic

### Testing Multitenancy

When writing tests involving tenants:

```csharp
// System activity (visible to all)
new ActivityDescriptor { TenantId = null }

// Default tenant activity
new ActivityDescriptor { TenantId = Tenant.DefaultTenantId }

// Specific tenant activity
new ActivityDescriptor { TenantId = "test-tenant" }
```

See `test/unit/Elsa.Common.UnitTests/Multitenancy/TenantIdNormalizationTests.cs` for examples.
```

---

### 6. Wiki Page: Multitenancy Guide

**Suggested Location:** `docs/features/multitenancy.md`

**Content Structure:**
```markdown
# Multitenancy in Elsa

## Quick Reference

| Tenant ID | Meaning | Visibility | Example |
|-----------|---------|-----------|---------|
| `null` | Tenant-agnostic | All tenants | Built-in activities |
| `""` | Default tenant | Default context only | `Tenant.DefaultTenantId` |
| `"acme-corp"` | Specific tenant | That tenant only | Custom tenant ID |

## Architecture

[Diagram showing three tenant types]

## Query Behavior

### Example: Finding Activities from "acme-corp" context

```
Registry contains:
- (null, "WriteLine", 1) - System activity
- ("", "CustomWorkflow", 1) - Default tenant activity
- ("acme-corp", "AcmeActivity", 1) - Acme Corp activity

Lookup: Find("CustomWorkflow", 1)
Result: NOT FOUND (default tenant, not current context)

Lookup: Find("WriteLine", 1)
Result: FOUND (system/null, visible to all)

Lookup: Find("AcmeActivity", 1)
Result: FOUND (specific tenant, in current context)
```

## Common Patterns

### Creating Default Tenant Entity
```csharp
var entity = new WorkflowDefinition 
{
    TenantId = Tenant.DefaultTenantId,
    // ...
};
```

### Creating System Activity
```csharp
var activity = new ActivityDescriptor 
{
    TenantId = null,  // Tenant-agnostic
    // ...
};
```

## Database Queries

EF Core automatically applies tenant filtering. When querying from "acme-corp" context:

```sql
SELECT * FROM Workflows
WHERE TenantId = 'acme-corp' OR TenantId IS NULL
```

This ensures:
- Acme Corp sees its own workflows
- Acme Corp sees system workflows (null)
- Acme Corp doesn't see other tenants' workflows

## Related Files

- ADR-0008: Empty string as default tenant ID
- ADR-0009: Null tenant ID for tenant-agnostic entities
```

---

### 7. API Documentation

**Location:** In OpenAPI/Swagger comments or separate API docs

```csharp
/// <summary>
/// Get a workflow definition
/// </summary>
/// <param name="id">The workflow definition ID</param>
/// <returns>
/// The workflow definition with the specified ID, if it exists and is visible in the current tenant context.
/// 
/// Visibility is determined by the TenantId field:
/// - If TenantId = null: visible to all tenants (system workflow)
/// - If TenantId = "" (default): visible only in default tenant context
/// - If TenantId = "[custom]": visible only to that specific tenant
/// </returns>
[HttpGet("{id}")]
public async Task<WorkflowDefinitionDto> GetWorkflowDefinition(string id)
```

---

## Long-Term Improvements

### 8. Create a Multitenancy Architecture Decision Record

**Location:** `doc/adr/0010-multitenancy-architecture.md`

**Purpose:** Consolidate all multitenancy decisions and patterns in one place

---

## Implementation Order

### Phase 1: Quick Wins (1-2 hours)
1. Add enhanced comments to 5 key files
2. Add class-level documentation to Entity and ActivityRegistry
3. Improve ADR cross-references

### Phase 2: Developer Experience (3-5 hours)
1. Create onboarding checklist in CONTRIBUTING.md
2. Create multitenancy wiki page with examples
3. Add common patterns documentation

### Phase 3: Long-term (Once per release)
1. Review API documentation for multitenancy clarity
2. Update examples and tutorials
3. Consider consolidating into new ADR-0010

---

## Success Metrics

After implementing these improvements, you should be able to answer:

- ✅ "What does `TenantId = null` mean?" → System activity (in code comment or wiki)
- ✅ "Why use empty string instead of null?" → ADR-0008 explains it
- ✅ "How do tenants access system resources?" → ActivityRegistry documentation shows the lookup pattern
- ✅ "What does the EF Core filter do?" → SetTenantIdFilter documentation explains it
- ✅ "How do I write tests for multitenancy?" → Checklist in CONTRIBUTING.md

---

## Expected Benefits

1. **Faster Onboarding:** New developers understand the convention in minutes, not hours
2. **Fewer Bugs:** Better documentation → fewer tenant isolation bugs
3. **Better Reviews:** Code reviewers can point to documentation instead of explaining verbally
4. **Easier Maintenance:** Clear rationale for design decisions
5. **Future Changes:** If you ever do change the convention, all the reasoning is documented

---

## Files to Update (Checklist)

- [ ] `src/modules/Elsa.Common/Multitenancy/Entities/Tenant.cs` - Add constant comment
- [ ] `src/modules/Elsa.Common/Entities/Entity.cs` - Add class documentation
- [ ] `src/modules/Elsa.Workflows.Core/Models/ActivityDescriptor.cs` - Enhance TenantId comment
- [ ] `src/modules/Elsa.Workflows.Core/Models/WorkflowIdentity.cs` - Add parameter documentation
- [ ] `src/modules/Elsa.Common/Multitenancy/Extensions/TenantsProviderExtensions.cs` - Improve method comment
- [ ] `src/modules/Elsa.Workflows.Core/Services/ActivityRegistry.cs` - Add class documentation
- [ ] `src/modules/Elsa.Persistence.EFCore.Common/EntityHandlers/SetTenantIdFilter.cs` - Add class documentation
- [ ] `CONTRIBUTING.md` - Add multitenancy section
- [ ] `docs/multitenancy.md` - Create comprehensive guide
- [ ] `doc/adr/0009-null-tenant-id-for-tenant-agnostic-entities.md` - Add cross-references

