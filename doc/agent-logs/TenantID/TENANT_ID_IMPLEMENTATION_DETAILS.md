# Technical Deep Dive: Tenant ID Convention Implementation Details

## Overview

This document provides implementation-level details for anyone considering changes to the tenant ID convention, including specific code sections, migration strategies, and risk assessment.

---

## Current Implementation Structure

### 1. Core Constant Definition

**File:** `src/modules/Elsa.Common/Multitenancy/Entities/Tenant.cs`

```csharp
public const string DefaultTenantId = "";
public static readonly Tenant Default = new() { Id = "", Name = "Default" };
```

**Current Behavior:**
- Empty string is the sentinel value
- All code uses `Tenant.DefaultTenantId` for consistency
- `Tenant.Default` provides a ready-made default tenant

### 2. Normalization Layer

**File:** `src/modules/Elsa.Common/Multitenancy/Extensions/TenantsProviderExtensions.cs`

```csharp
public static string NormalizeTenantId(this string? tenantId) 
    => tenantId ?? Tenant.DefaultTenantId;  // null or "" → ""
```

**Current Behavior:**
- Converts `null` → `""`
- Used in 7+ places throughout codebase
- Ensures consistent representation in dictionaries

**Usage Locations:**
1. `DefaultTenantAccessor.TenantId` property getter
2. `TenantResolverResult.TenantId` property getter
3. `TenantResolverContext.FindTenant()` method
4. `DefaultTenantResolverPipelineInvoker.Invoke()` - dictionary creation
5. `WorkflowDefinitionStorePopulator` - tenant comparison

### 3. EF Core Query Filter

**File:** `src/modules/Elsa.Persistence.EFCore.Common/EntityHandlers/SetTenantIdFilter.cs`

```csharp
// e => e.TenantId == dbContext.TenantId || e.TenantId == null
var equalityCheck = Expression.Equal(tenantIdProperty, tenantIdOnContext);
var nullCheck = Expression.Equal(tenantIdProperty, Expression.Constant(null, typeof(string)));
var body = Expression.OrElse(equalityCheck, nullCheck);
```

**Current Behavior:**
- **Always includes** records where `TenantId == null` regardless of current context
- This is the mechanism for tenant-agnostic visibility
- Applied automatically to all `Entity` subclasses via `IEntityModelCreatingHandler`

**Critical for:**
- Workflow execution (seeing built-in activities)
- System queries (accessing shared resources)

### 4. In-Memory Activity Registry

**File:** `src/modules/Elsa.Workflows.Core/Services/ActivityRegistry.cs`

```csharp
// Multiple filter methods all follow this pattern:
public IEnumerable<ActivityDescriptor> ListAll() 
    => _activityDescriptors.Values.Where(x => 
        x.TenantId == tenantAccessor.TenantId || x.TenantId == null);

public ActivityDescriptor? Find(string type, int version) 
    => _activityDescriptors.GetValueOrDefault((tenantAccessor.TenantId, type, version)) 
        ?? _activityDescriptors.GetValueOrDefault((null, type, version));
```

**Current Behavior:**
- Uses composite key: `(string? TenantId, string Type, int Version)`
- First tries tenant-specific lookup
- Falls back to tenant-agnostic if not found
- Handles both `null` and non-null TenantIds

**Methods affected:**
- `ListAll()` - filter logic
- `ListByProvider(Type)` - filter logic  
- `Find(string)` - composite max by version
- `Find(string, int)` - dual lookup
- `Find(Func)` - filter logic
- `FindMany(Func)` - filter logic
- `RefreshDescriptorsAsync()` - maintains both lookups

### 5. Data Model

**File:** `src/modules/Elsa.Common/Entities/Entity.cs` (base class)

```csharp
public abstract class Entity
{
    public string? TenantId { get; set; }  // Nullable!
    // ...
}
```

**Current Behavior:**
- All entities have nullable `TenantId`
- Allows `null` to be explicitly stored in database
- EF Core migrations handle null properly

### 6. Workflow Identity

**File:** `src/modules/Elsa.Workflows.Core/Models/WorkflowIdentity.cs`

```csharp
public record WorkflowIdentity(
    string DefinitionId, 
    int Version, 
    string Id, 
    string? TenantId = null)  // Nullable!
{
}
```

**Current Behavior:**
- Defaults to `null` for tenant-agnostic workflows
- Used when building workflows
- TenantId can remain null or be set to empty string

### 7. Activity Descriptor

**File:** `src/modules/Elsa.Workflows.Core/Models/ActivityDescriptor.cs`

```csharp
public class ActivityDescriptor
{
    public string? TenantId { get; set; }  // Null means tenant-agnostic
    public string TypeName { get; set; } = null!;
    public int Version { get; set; }
    // ...
}
```

**Key Comment:** "Null means tenant-agnostic"

---

## Database Migration Story

### Current Migration (As of 2025-01-31)

**Files:**
- `src/modules/Elsa.Persistence.EFCore.SqlServer/Migrations/Runtime/20260131023442_ConvertNullTenantIdToEmptyString.cs`
- `src/modules/Elsa.Persistence.EFCore.SqlServer/Migrations/Management/20260131023442_ConvertNullTenantIdToEmptyString.cs`
- Similar migrations for PostgreSQL, MySQL, SQLite

**Migration Purpose:**
Convert all historical `NULL` values to `""` to align with ADR-0008 (default tenant) and ADR-0009 (null = agnostic).

**SQL Pattern:**
```sql
UPDATE [Schema].[TableName] 
SET TenantId = '' 
WHERE TenantId IS NULL
```

**Tables Affected:**
- ActivityExecutionRecords
- BookmarkQueueItems
- Bookmarks
- KeyValuePairs
- Triggers
- WorkflowExecutionLogRecords
- WorkflowInboxMessages
- WorkflowDefinitions
- WorkflowInstances
- WorkflowDefinitionVersions
- (And others)

**Impact:** Likely 100K-500K+ records updated depending on system age

### Backward Migration Risk

The migration has a `Down()` method that reverts to `NULL`, but this is **problematic**:

```csharp
// Dangerous: loses distinction between default tenant and agnostic
migrationBuilder.Sql($@"
    UPDATE [{_schema.Schema}].[Table] 
    SET TenantId = NULL 
    WHERE TenantId = ''
");
```

**Issue:** Cannot distinguish which `""` records were originally `NULL` (default tenant) vs intentionally `""` (before agnostic feature). A down migration is unsafe.

---

## Risk Assessment: Option B Transition

### Risk Level: 🔴 CRITICAL

If switching to `TenantId = "default"`:

#### Database Migration Risks

1. **Data Loss Possibility**
   - Current system has `null` (agnostic) and `""` (default)
   - Migration: `""` → `"default"`
   - If something goes wrong, you can't reverse it safely
   - No safe rollback path

2. **Concurrent Operations**
   - Running system continues writing `""`
   - Migration in progress reads/updates same tables
   - Race conditions possible without full downtime

3. **Foreign Key Constraints**
   - Some tables may reference TenantId
   - Constraints must be dropped before migration
   - Re-enabling constraints can be slow on large datasets

4. **Performance Impact**
   - Full table scans for updates
   - Reindexing after migration
   - Could take hours to days for large installations
   - May require downtime

#### Application Code Migration Risks

1. **Staged Deployment Challenge**
   - Code must handle BOTH `""` and `"default"` during transition
   - Requires dual-read logic in many places
   - Risk of inconsistent behavior if deployment is out-of-order

2. **Client API Contracts**
   - Clients may depend on `TenantId == ""`
   - Change breaks existing integrations
   - Requires client updates and coordination

3. **Composite Key Changes**
   - Currently: `(string? TenantId, string Type, int Version)`
   - New: `(string TenantId, string Type, int Version)` with `"default"` as value
   - ActivityRegistry cache would need purging during migration
   - Could cause cache inconsistencies

#### Organizational Risks

1. **Breaking Changes**
   - High-impact change for any production system
   - Requires careful planning and coordination
   - Not suitable for patch/minor releases

2. **Documentation Debt**
   - Every document saying "empty string" must be updated
   - API documentation changes
   - Training materials update required

3. **External System Integration**
   - Any system reading your database breaks
   - Any API client expecting `TenantId = ""` breaks
   - Requires coordinated updates

---

## Detailed Comparison: Dual-Value vs Single-Value

### Option A: Current (null for agnostic, "" for default)

**Composite Key:**
```csharp
(string? TenantId, string Type, int Version)
```

**Dictionary Creation:**
```csharp
var dictionary = tenants.ToDictionary(x => x.Id.NormalizeTenantId());
// null → "" before key creation
// Works because empty string is valid key
```

**Query Examples:**
```csharp
// Check if agnostic
var isAgnostic = descriptor.TenantId == null;

// Check if default
var isDefault = descriptor.TenantId == "";

// Check if specific
var isSpecific = descriptor.TenantId != null && descriptor.TenantId != "";

// Get all visible to current context
var visible = descriptors.Where(x => 
    x.TenantId == currentTenantId || x.TenantId == null);
```

### Option B: Proposed ("default" string, null for agnostic)

**Composite Key:**
```csharp
(string? TenantId, string Type, int Version)
// Same as current, but now "default" is explicit string
```

**Dictionary Creation:**
```csharp
var dictionary = tenants.ToDictionary(x => x.Id); // No normalization needed
// Works directly without conversion
```

**Query Examples:**
```csharp
// Check if agnostic
var isAgnostic = descriptor.TenantId == null;

// Check if default
var isDefault = descriptor.TenantId == "default";

// Check if specific
var isSpecific = descriptor.TenantId != null && descriptor.TenantId != "default";

// Get all visible to current context
var visible = descriptors.Where(x => 
    x.TenantId == currentTenantId || x.TenantId == null);
```

**No functional difference** - both approaches handle filtering identically.

---

## Code Locations That Would Change (Option B)

### Must Update (Functional Impact):
1. ✅ `Tenant.DefaultTenantId` = `"default"` (instead of `""`)
2. ✅ `Tenant.Default.Id` assignment
3. ✅ All tests asserting on empty string (TenantIdNormalizationTests, etc.)
4. ✅ Database migration (convert `""` → `"default"`)

### Should Update (Quality/Clarity):
1. 🔄 Remove `NormalizeTenantId()` - no longer needed
2. 🔄 ActivityRegistry filtering (still works but could simplify)
3. 🔄 All `NormalizeTenantId()` call sites (7+ locations)
4. 🔄 Composite key examples in documentation
5. 🔄 API responses that include TenantId

### Already Compatible:
- ✅ `WorkflowIdentity` - already has `string? TenantId = null`
- ✅ `ActivityDescriptor` - already has `string? TenantId`
- ✅ `Entity.TenantId` - already nullable
- ✅ EF Core query filters - logic unchanged
- ✅ Comments - would just update to explain `"default"` meaning

---

## Performance Implications

### Option A: Current Implementation
- ✅ Zero normalization overhead (done upfront)
- ✅ Dictionary lookups are fast (O(1))
- ✅ Composite key lookups are optimal
- ✅ No additional string allocations

### Option B: Proposed Implementation  
- ✅ **Same performance** - no normalization step
- ✅ Removes one string comparison per normalization
- ✅ Slightly faster dictionary creation (no null→"" conversion)
- ✅ Negligible real-world difference

**Verdict:** No significant performance difference either way.

---

## Testing Considerations

### Current Tests (Option A)
**File:** `test/unit/Elsa.Common.UnitTests/Multitenancy/TenantIdNormalizationTests.cs`

```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
public void NormalizeTenantId_WithNullOrEmpty_ReturnsDefaultTenantId(string? tenantId)
{
    var result = tenantId.NormalizeTenantId();
    Assert.Equal(Tenant.DefaultTenantId, result);
    Assert.Equal(string.Empty, result);
}
```

### Tests to Update (Option B)
```csharp
[Fact]
public void NormalizeTenantId_WithNull_ReturnsNull()
{
    var result = ((string?)null).NormalizeTenantId();
    Assert.Null(result);
}

[Fact]
public void NormalizeTenantId_WithEmpty_ReturnsNull()
{
    var result = ((string?)string.Empty).NormalizeTenantId();
    Assert.Null(result);  // Now treated as agnostic
}

[Fact]
public void DefaultTenantId_IsExplicitString()
{
    Assert.Equal(Tenant.DefaultTenantId, "default");
}
```

### New Tests Needed (Option B)
- Ensure migrations handle both empty and null correctly
- Test composite key lookups with "default" string
- Test dictionary creation without normalization
- Integration tests with real database migration

---

## Operational Concerns

### Downtime Requirements

**Option A (Current):** 0 minutes - no migration needed

**Option B (Proposed):** 
- **Small database (<100MB):** 5-15 minutes
- **Medium database (1-10GB):** 30 min - 2 hours
- **Large database (>10GB):** 2-8+ hours
- **Very large database (>100GB):** Potential for days of incremental migration

### Zero-Downtime Alternative (Option B)
Could be achieved with:
1. Dual-write phase (write both to old and new systems)
2. Background reader on new column value
3. Complete switchover after validation
4. Complex and error-prone

### Rollback Strategy

**Option A:** N/A - no migration

**Option B:**
- ❌ Unsafe to rollback via database (loses information)
- ✅ Must rollback via code deployment
- Requires keeping old code path working during transition

---

## Multitenancy Edge Cases

### Scenario 1: System Activity (Built-in)
- **Option A:** `TenantId = null` ✅ Perfect fit
- **Option B:** `TenantId = null` ✅ Perfect fit
- **Behavior:** Visible to all tenants, no differences

### Scenario 2: Default Tenant Data
- **Option A:** `TenantId = ""` ✅ Explicitly default
- **Option B:** `TenantId = "default"` ✅ More explicit
- **Behavior:** Visible only to default tenant (when `tenantAccessor.TenantId == currentValue`)

### Scenario 3: Specific Tenant Data
- **Option A:** `TenantId = "acme-corp"` ✅ Clear
- **Option B:** `TenantId = "acme-corp"` ✅ Clear
- **Behavior:** Visible only when in that tenant context

### Scenario 4: Legacy System Migration
- **Option A:** All legacy `NULL` → `""` (safe, one-way) ✅
- **Option B:** Must convert `""` → `"default"` (risky, not reversible) ❌

---

## Recommendation Matrix

| Situation | Recommendation | Reasoning |
|-----------|---|---|
| Existing production system | **Option A** | Zero migration risk |
| Running system < 100 records | **Option B** | Minimal impact, better clarity |
| Running system 100K-1M records | **Option A** | Migration risk outweighs benefit |
| Running system >1M records | **Option A** | Unacceptable downtime/risk |
| Greenfield project | **Option B** | Clean start, best clarity |
| New branch/environment | **Option A** | Consistency with main |

---

## Summary Table

| Aspect | Option A (Current) | Option B (Explicit "default") |
|--------|---|---|
| **Implementation Status** | ✅ Complete | ❌ Not started |
| **Migration Needed** | ❌ No | ✅ Yes (large) |
| **Downtime Required** | 0 min | 30 min - 8+ hours |
| **Code Changes** | None | ~20 locations |
| **Risk Level** | 🟢 None | 🔴 Critical |
| **Rollback Path** | N/A | ⚠️ Unsafe (code-only) |
| **Clarity** | 🟡 Good | 🟢 Excellent |
| **Industry Alignment** | 🟡 Unusual | 🟢 Standard |
| **Breaking Changes** | None | API contracts, clients |
| **Best For** | Existing systems | New systems only |

