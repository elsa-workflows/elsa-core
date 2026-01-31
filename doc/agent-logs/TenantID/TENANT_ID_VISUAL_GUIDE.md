# Tenant ID Convention: Visual Guide and Examples

## Flow Diagrams

### Option A: Current Implementation (null for agnostic, "" for default)

```
┌─────────────────────────────────────────────────────────────────────┐
│                     TENANT ID VALUE SPACE (OPTION A)                 │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  null ──────────────────► TENANT-AGNOSTIC (visible to all)          │
│                           • System activities                        │
│                           • Global configurations                    │
│                           • Shared resources                        │
│                                                                       │
│  "" (empty string) ─────► DEFAULT TENANT                            │
│                           • Only visible to default tenant context  │
│                           • Legacy data from before agnostic feature│
│                           • Default tenant-specific entities        │
│                                                                       │
│  "acme-corp" ───────────► SPECIFIC TENANT (ACME Corp)               │
│  "customer-a" ──────────► SPECIFIC TENANT (Customer A)              │
│  "customer-b" ──────────► SPECIFIC TENANT (Customer B)              │
│                           • Each visible only in their context      │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

### Option B: Proposed Implementation ("default" for default, null for agnostic)

```
┌─────────────────────────────────────────────────────────────────────┐
│                     TENANT ID VALUE SPACE (OPTION B)                 │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  null ──────────────────► TENANT-AGNOSTIC (visible to all)          │
│                           • System activities                        │
│                           • Global configurations                    │
│                           • Shared resources                        │
│                                                                       │
│  "default" ─────────────► DEFAULT TENANT                            │
│                           • Only visible to default tenant context  │
│                           • Explicitly marked as default             │
│                           • All data converted from "" in migration │
│                                                                       │
│  "acme-corp" ───────────► SPECIFIC TENANT (ACME Corp)               │
│  "customer-a" ──────────► SPECIFIC TENANT (Customer A)              │
│  "customer-b" ──────────► SPECIFIC TENANT (Customer B)              │
│                           • Each visible only in their context      │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Query Filter Logic

### EF Core Global Query Filter

Both options use identical EF Core filtering:

```csharp
// Applied automatically to all Entity subclasses
[Query Filter Logic]
WHERE TenantId = @currentTenantId OR TenantId IS NULL
```

**Result:**

```
┌────────────────────────────────────────────────────┐
│         Records Visible to "acme-corp" Context      │
├────────────────────────────────────────────────────┤
│                                                    │
│  WHERE TenantId = "acme-corp"                      │
│     OR TenantId IS NULL (agnostic)                │
│                                                    │
│  ✓ TenantId = "acme-corp"          VISIBLE        │
│  ✓ TenantId = null                 VISIBLE        │
│  ✗ TenantId = ""                   NOT VISIBLE    │
│  ✗ TenantId = "customer-a"         NOT VISIBLE    │
│                                                    │
└────────────────────────────────────────────────────┘
```

---

## Practical Code Examples

### Example 1: Registering a Built-in Activity (Tenant-Agnostic)

**Current Code (Option A):**
```csharp
var activityDescriptor = new ActivityDescriptor
{
    TypeName = "Elsa.Workflows.WriteLine",
    Name = "Write Line",
    Category = "Logging",
    Version = 1,
    TenantId = null,  // Explicitly null = visible to all tenants
    // ... other properties
};

registry.Register(activityDescriptor);
```

**Behavior:**
- Stored in database with `TenantId = NULL`
- Visible when querying from any tenant context
- System activities are not isolated per tenant

**With Option B (identical code):**
```csharp
var activityDescriptor = new ActivityDescriptor
{
    TypeName = "Elsa.Workflows.WriteLine",
    Name = "Write Line",
    Category = "Logging",
    Version = 1,
    TenantId = null,  // Still explicitly null = visible to all tenants
    // ... other properties
};

registry.Register(activityDescriptor);
```

**Behavior:** Identical to Option A

---

### Example 2: Creating Default Tenant Entity

**Current Code (Option A):**
```csharp
var workflow = new WorkflowDefinition
{
    Id = "my-workflow-1",
    Name = "My Workflow",
    TenantId = "",  // Empty string = default tenant only
    // ... other properties
};

await store.SaveAsync(workflow);
```

**With Option B:**
```csharp
var workflow = new WorkflowDefinition
{
    Id = "my-workflow-1",
    Name = "My Workflow",
    TenantId = "default",  // Explicit "default" constant
    // ... other properties
};

await store.SaveAsync(workflow);
```

**Difference:**
- Option A: Requires comment explaining empty string
- Option B: Self-documenting with explicit string

---

### Example 3: Creating Tenant-Specific Entity

**Both Options (identical):**
```csharp
var tenantAccessor = serviceProvider.GetRequiredService<ITenantAccessor>();
var tenantId = tenantAccessor.TenantId;  // e.g., "acme-corp"

var workflow = new WorkflowDefinition
{
    Id = "tenant-workflow-1",
    Name = "Tenant-Specific Workflow",
    TenantId = tenantId,  // "acme-corp"
    // ... other properties
};

await store.SaveAsync(workflow);
```

**Behavior:** Identical for both options - stored with specific tenant ID

---

### Example 4: ActivityRegistry Lookup with Fallback

**Current Code (Option A):**
```csharp
public ActivityDescriptor? Find(string type, int version)
{
    var tenantId = _tenantAccessor.TenantId;  // Current context, e.g., "acme-corp"
    
    // First try tenant-specific
    if (_activityDescriptors.TryGetValue((tenantId, type, version), out var descriptor))
        return descriptor;
    
    // Fall back to tenant-agnostic
    if (_activityDescriptors.TryGetValue((null, type, version), out var agnosticDescriptor))
        return agnosticDescriptor;
    
    return null;
}
```

**Example Scenario:**
```
Looking for: Type="WriteLine", Version=1
Current Context: TenantId="acme-corp"

Step 1: Check ("acme-corp", "WriteLine", 1) → NOT FOUND
Step 2: Check (null, "WriteLine", 1) → FOUND ✓
Return: System-level WriteLine activity (tenant-agnostic)
```

**With Option B (identical logic):**
```csharp
public ActivityDescriptor? Find(string type, int version)
{
    var tenantId = _tenantAccessor.TenantId;  // Current context, e.g., "acme-corp"
    
    // First try tenant-specific
    if (_activityDescriptors.TryGetValue((tenantId, type, version), out var descriptor))
        return descriptor;
    
    // Fall back to tenant-agnostic
    if (_activityDescriptors.TryGetValue((null, type, version), out var agnosticDescriptor))
        return agnosticDescriptor;
    
    return null;
}

// Identical behavior!
```

---

### Example 5: Multi-Tenant Activity Discovery

**Scenario:** Custom activity for "acme-corp" tenant

**Setup:**
```csharp
// Register system activity (all tenants can use)
registry.Register(new ActivityDescriptor 
{
    TypeName = "Elsa.Workflows.If",
    TenantId = null  // Agnostic
});

// Register custom activity for acme-corp tenant
using (tenantAccessor.PushContext(new Tenant { Id = "acme-corp" }))
{
    registry.Register(new ActivityDescriptor
    {
        TypeName = "Custom.AcmeActivity",
        TenantId = "acme-corp"  // Specific to acme-corp
    });
}

// Register another activity for default tenant
registry.Register(new ActivityDescriptor
{
    TypeName = "Elsa.Workflows.While",
    TenantId = ""  // Default tenant only (Option A)
    // or TenantId = "default" (Option B)
});
```

**Query Results:**

```
┌──────────────────────────────────────────────────────────────┐
│ When querying from "acme-corp" context:                      │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│ Find("Elsa.Workflows.If", 1)                                │
│   → ✓ Returns system activity (TenantId=null)               │
│                                                              │
│ Find("Custom.AcmeActivity", 1)                              │
│   → ✓ Returns acme-corp activity (TenantId="acme-corp")     │
│                                                              │
│ Find("Elsa.Workflows.While", 1)                             │
│   → ✗ NOT FOUND (Option A: requires TenantId="")            │
│   → ✗ NOT FOUND (Option B: requires TenantId="default")     │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

### Example 6: EF Core Query Example

**Current Code (Option A):**
```csharp
var tenantId = tenantAccessor.TenantId;  // "acme-corp"

var bookmarks = await context.Bookmarks
    .Where(x => x.WorkflowInstanceId == instanceId)
    .ToListAsync();

// EF Core automatically applies the global filter:
// WHERE (TenantId = 'acme-corp' OR TenantId IS NULL)
```

**What gets queried:**
```sql
SELECT * FROM Bookmarks
WHERE WorkflowInstanceId = @instanceId
  AND (TenantId = 'acme-corp' OR TenantId IS NULL)
```

**Results:**
- ✓ Bookmarks with `TenantId = 'acme-corp'` (specific)
- ✓ Bookmarks with `TenantId = NULL` (agnostic)
- ✗ Bookmarks with `TenantId = ''` (default tenant, not visible)
- ✗ Bookmarks with `TenantId = 'customer-a'` (different tenant)

**With Option B:** Same SQL and behavior (filtering logic unchanged)

---

## Migration Example (Option B)

### Before Migration
```sql
-- Current database state (Option A):
SELECT TenantId, Count(*) as Count FROM WorkflowDefinitions GROUP BY TenantId;

Results:
TenantId        Count
─────────────   ──────
NULL            142    -- Tenant-agnostic workflows
(empty)         8,432  -- Default tenant workflows  
acme-corp       1,250  -- Acme Corp workflows
customer-a      892    -- Customer A workflows
customer-b      567    -- Customer B workflows
```

### Migration SQL
```sql
-- Step 1: Convert empty strings to "default"
UPDATE WorkflowDefinitions 
SET TenantId = 'default' 
WHERE TenantId = '';

UPDATE WorkflowInstances 
SET TenantId = 'default' 
WHERE TenantId = '';

-- ... repeat for all tables with TenantId ...
```

### After Migration
```sql
-- After migration (Option B):
SELECT TenantId, Count(*) as Count FROM WorkflowDefinitions GROUP BY TenantId;

Results:
TenantId        Count
─────────────   ──────
NULL            142    -- Tenant-agnostic workflows (unchanged)
default         8,432  -- Default tenant workflows (converted from "")
acme-corp       1,250  -- Acme Corp workflows (unchanged)
customer-a      892    -- Customer A workflows (unchanged)
customer-b      567    -- Customer B workflows (unchanged)
```

**Time to execute:** Depends on table sizes
- 10M records: ~2-5 minutes
- 100M+ records: 15-45 minutes

---

## Data Model Relationships

### Composite Key Structure (Both Options)

```
ActivityDescriptor Composite Key: (TenantId, TypeName, Version)

Examples:
┌──────────────────┬──────────────────┬─────────┐
│ TenantId         │ TypeName         │ Version │
├──────────────────┼──────────────────┼─────────┤
│ null             │ If               │ 1       │ ← System activity
│ null             │ While            │ 1       │ ← System activity
│ ""               │ CustomWorkflow   │ 1       │ ← Default tenant (Option A)
│ "default"        │ CustomWorkflow   │ 1       │ ← Default tenant (Option B)
│ "acme-corp"      │ AcmeActivity     │ 1       │ ← Tenant-specific
│ "acme-corp"      │ AcmeActivity     │ 2       │ ← Updated version
│ "customer-a"     │ If               │ 1       │ ← Same as system (will use null)
└──────────────────┴──────────────────┴─────────┘
```

**Key Insight:** 
- Multiple entries with same TypeName/Version but different TenantId are allowed
- Lookup algorithm chooses most specific match first
- Falls back to agnostic (null) if specific not found

---

## Ambiguity Examples

### The Empty String Ambiguity Problem (Option A)

```
┌─ User sees TenantId = "" in database
│
├─ What does it mean?
│  a) Default tenant (intended in ADR-0008)
│  b) Tenant-agnostic (old code pre-ADR-0009)
│  c) Database migration error (forgot to convert)
│  d) Unknown
│
└─ Requires developer to check migration history
   and code comments to understand intent
```

### The Explicit String Advantage (Option B)

```
┌─ User sees TenantId = "default" in database
│
├─ What does it mean?
│  a) DEFAULT TENANT (100% clear)
│  b) No ambiguity
│
└─ Self-documenting, requires no investigation
```

### Agnostic (Both Options)

```
TenantId = null (in database)
  → Unambiguous in both options
  → Clear system activity/shared resource
```

---

## Developer Experience Comparison

### Scenario: Code Review - Empty String Encountered

**Option A Review:**
```csharp
var result = entity.TenantId == "";  // ← What does this mean?

Reviewer: "Is this checking for default tenant or agnostic?"
Author: "Default tenant, per ADR-0008"
Reviewer: "Why not use null? ADR-0009 says null is agnostic"
Author: "We use empty string for default, null for agnostic"
Reviewer: *reads ADRs* "Ah, okay, but this is confusing"
```

**Option B Review:**
```csharp
var result = entity.TenantId == Tenant.DefaultTenantId;  // ← Clear

Reviewer: "Makes sense, checking for default tenant"
Author: "Yep, using the constant for clarity"
Reviewer: ✓ Approved
```

---

## Performance Test Case

### Scenario: Finding Activity with 10K Activities

**Data Structure:**
```
Total Activities: 10,000
  - Null (agnostic): 500
  - Default tenant ("" or "default"): 3,000
  - Acme Corp: 2,500
  - Other tenants: 4,000

Query: Find("WriteLine", 1) from "acme-corp" context
```

**Option A Flow:**
```
1. Try lookup: ("acme-corp", "WriteLine", 1) → HIT ✓
   Time: ~microseconds (dictionary O(1) lookup)
   
Fallback (if not found):
2. Try lookup: (null, "WriteLine", 1) → HIT ✓
   Time: ~microseconds (dictionary O(1) lookup)
```

**Option B Flow:**
```
1. Try lookup: ("acme-corp", "WriteLine", 1) → HIT ✓
   Time: ~microseconds (dictionary O(1) lookup)
   
Fallback (if not found):
2. Try lookup: (null, "WriteLine", 1) → HIT ✓
   Time: ~microseconds (dictionary O(1) lookup)
```

**Result:** Identical performance, no difference

---

## Backward Compatibility Matrix

| Scenario | Option A | Option B | Notes |
|----------|----------|----------|-------|
| **Existing DB** | ✅ Works | ⚠️ Migration needed | Option B requires conversion |
| **New DB** | ✅ Works | ✅ Works | Both fine for new systems |
| **Legacy Code** | ✅ Compatible | ⚠️ Updates needed | Option B breaks empty string checks |
| **Client API** | ✅ Returns "" | ⚠️ Returns "default" | Breaking change for API consumers |
| **Test Data** | ✅ Uses "" | ⚠️ Uses "default" | Fixtures and mocks need updates |
| **Documentation** | ✅ Matches | ⚠️ Extensive updates | Docs, examples, guides all change |

---

## Conclusion: Visual Decision Tree

```
START: Should we change tenant ID convention?
│
├─ Are we in a production environment?
│  │
│  ├─ YES: How many tenant records? 
│  │  │
│  │  ├─ < 100K: Consider Option B (manageable migration)
│  │  ├─ 100K - 1M: Use Option A (risk not worth it)
│  │  └─ > 1M: MUST use Option A (unacceptable downtime)
│  │
│  └─ NO: Greenfield project?
│     │
│     ├─ YES: Use Option B (better clarity)
│     └─ NO: Use Option A (consistency with main)
│
└─ FINAL DECISION:
   ├─ 85% of cases → Option A (recommended)
   └─ 15% of cases → Option B (only greenfield/small systems)
```

