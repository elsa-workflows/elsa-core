# Workflow Designer Performance Optimizations

## Summary

This document describes the performance optimizations implemented to address the significant performance degradation reported in issue [Performance] Elsa Studio Performance Tests (develop 3.6).

## Performance Issue

Users reported that Elsa Studio 3.6 (develop) had significantly degraded performance compared to 3.3.4:

### Reported Performance Metrics (WASM 3.6 vs 3.3.4)

| Component | Workflow | Time 3.3.4 | Time 3.6 | Degradation |
|-----------|----------|------------|----------|-------------|
| Designer  | ChonkyWF | 12.1s      | 44.3s    | 3.7x slower |
| Designer  | SetsAlot | 15.1s      | 65.2s    | 4.3x slower |
| Tracker   | ChonkyWF | 18.0s      | 94.5s    | 5.2x slower |
| Tracker   | SetsAlot | 37.8s      | 140.8s   | 3.7x slower |

### Key Observation

The "Scripting" time in browser profiler increased dramatically:
- **3.3.4**: 2-5 seconds
- **3.6 (first load)**: 57-91 seconds
- **3.6 (cached)**: 5-7 seconds

This indicated a JavaScript performance bottleneck that was being cached after first load.

## Root Cause Analysis

### Issue #1: Expensive Activity Size Calculation

**Location**: `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/designer/api/calculate-activity-size.ts`

**Problem**:
For each activity being loaded:
1. Created a temporary DOM element
2. Appended it to document body
3. Used `requestAnimationFrame` polling loop to wait for custom element rendering
4. Measured the element
5. Removed the element

**Impact with 108 activities (SetsAlot workflow)**:
- 108 temporary DOM insertions/removals
- 108+ `requestAnimationFrame` callbacks (potentially hundreds if elements took multiple frames to render)
- Excessive DOM manipulation causing layout thrashing and reflows
- Sequential processing meant total time = sum of all individual calculations

### Issue #2: No Size Caching

**Problem**: Activity sizes were calculated fresh every time, even though many activities of the same type have identical sizes.

**Impact**:
- Workflows commonly have multiple instances of the same activity type
- Each instance recalculated size independently
- Cache would have been cleared between page loads anyway

### Issue #3: Unnecessary Size Updates

**Location**: `src/modules/Elsa.Studio.Workflows.Designer/Components/ActivityWrappers/ActivityWrapperBase.cs`

**Problem**:
- `UpdateSizeAsync()` was called in `OnParametersSetAsync` for every activity
- Called on every parameter change, even if size wasn't affected
- No tracking of what changed between renders

**Impact**:
- Redundant JSInterop calls during re-renders
- Wasted CPU cycles on measurements that wouldn't change

## Solutions Implemented

### Optimization #1: Batched Activity Size Calculation

**File**: `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/designer/api/calculate-activity-size.ts`

**Implementation**:
```typescript
// Queue for batching size calculations
let calculationQueue: Array<{activity: Activity, resolve, reject}> = [];
let batchTimer: number | null = null;

function processBatch() {
    // Create a single container for all measurements
    const container = document.createElement('div');
    container.style.position = 'absolute';
    container.style.visibility = 'hidden';
    
    // Create all elements at once
    for (const item of batch) {
        const wrapper = document.createElement('div');
        wrapper.appendChild(createActivityElement(item.activity, true));
        container.appendChild(wrapper);
    }
    
    document.body.appendChild(container);
    
    // Single requestAnimationFrame loop for all elements
    const checkAllSizes = () => {
        if (!allElementsReady()) {
            window.requestAnimationFrame(checkAllSizes);
            return;
        }
        
        // Measure all at once
        for (const measurement of measurements) {
            const size = measure(measurement.wrapper);
            measurement.resolve(size);
        }
        
        container.remove();
    };
    
    checkAllSizes();
}
```

**Benefits**:
- 1 temporary container instead of N individual DOM insertions
- 1 `requestAnimationFrame` loop instead of N loops
- All measurements happen together, minimizing reflows
- Batching delay allows multiple activities to accumulate before processing

### Optimization #2: Activity Size Caching

**File**: `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/designer/api/calculate-activity-size.ts`

**Implementation**:
```typescript
const sizeCache = new Map<string, Size>();

function getCacheKey(activity: Activity): string {
    const type = activity.type || 'unknown';
    const hasDescription = !!activity.metadata?.description;
    const showDescription = activity.metadata?.showDescription === true;
    const hasIcon = !!activity.metadata?.icon;
    return `${type}:${hasDescription && showDescription}:${hasIcon}`;
}

export function calculateActivitySize(activity: Activity): Promise<Size> {
    const cacheKey = getCacheKey(activity);
    const cachedSize = sizeCache.get(cacheKey);
    
    if (cachedSize) {
        return Promise.resolve(cachedSize);
    }
    
    // Calculate and cache...
}
```

**Benefits**:
- Same activity type only calculated once per page session
- Cache key includes factors that affect size (description, icon)
- Immediate return for cached sizes (no async overhead)

### Optimization #3: Conditional Size Updates

**File**: `src/modules/Elsa.Studio.Workflows.Designer/Components/ActivityWrappers/ActivityWrapperBase.cs`

**Implementation**:
```csharp
private bool _isFirstRender = true;
private bool _previousShowDescription;
private string? _previousDescription;
private int _previousPortCount;

protected override async Task OnParametersSetAsync()
{
    // ... load activity data ...
    
    var currentPortCount = Ports.Count;
    var designerMetadata = activity.GetDesignerMetadata();
    var hasValidSize = designerMetadata.Size?.Width > 0 
                    && designerMetadata.Size?.Height > 0;
    
    // Only update size when necessary
    var needsSizeUpdate = (_isFirstRender && !hasValidSize) ||
                        (!_isFirstRender && (
                            currentShowDescription != _previousShowDescription ||
                            Description != _previousDescription ||
                            currentPortCount != _previousPortCount));
    
    if (needsSizeUpdate)
    {
        await UpdateSizeAsync();
        // Update tracking variables
    }
    
    _isFirstRender = false;
}
```

**Benefits**:
- Skips size calculation for activities with valid existing sizes
- Only recalculates when description visibility, content, or ports change
- Dramatically reduces JSInterop calls for saved workflows

### Optimization #4: Skip Recalculation for Valid Existing Sizes

**Implementation**: Part of Optimization #3

**Logic**:
```csharp
var hasValidSize = designerMetadata.Size?.Width > 0 
                && designerMetadata.Size?.Height > 0;

var needsSizeUpdate = (_isFirstRender && !hasValidSize) || ...
```

**Benefits**:
- Activities loaded from storage with existing sizes skip calculation entirely
- Most workflows are loaded from storage, so this applies to majority of cases
- Particularly beneficial for large workflows with many activities

## Expected Performance Improvements

### Initial Load (New Activities)
- **Before**: N sequential calculations, each with DOM insertion + RAF loop
- **After**: 1 batched calculation for all activities
- **Expected**: 3-5x faster

### Initial Load (Saved Workflows)
- **Before**: N sequential calculations even though sizes exist in metadata
- **After**: Skip calculation for activities with valid sizes
- **Expected**: 10-50x faster (most activities skip calculation)

### Repeated Activity Types
- **Before**: Each instance calculated independently
- **After**: First instance calculates, rest use cache
- **Expected**: 10-100x faster for duplicate types

### Re-renders
- **Before**: Size recalculated on every parameter change
- **After**: Only recalculate when size-affecting properties change
- **Expected**: Minimal overhead (most re-renders skip calculation)

## Testing Recommendations

To verify these optimizations:

1. **Load SetsAlot workflow** (108 SetVariable activities)
   - Measure total scripting time
   - Expected: <10s (vs 87s before)
   - All activities should use cached size after first

2. **Load ChonkyWF workflow** (complex workflow with varied activities)
   - Measure total scripting time
   - Expected: <15s (vs 57s before)
   - Should benefit from batching and caching

3. **Load saved workflow multiple times**
   - First load: Activities with metadata skip calculation
   - Subsequent loads: Same performance as first
   - Expected: Consistent fast load times

4. **Toggle description visibility**
   - Should trigger size recalculation
   - Verify only affected activities update
   - Expected: <1s for reasonable workflow sizes

## Implementation Details

### Files Modified

1. `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/designer/api/calculate-activity-size.ts`
   - Added batching queue and processing
   - Added size caching with intelligent cache keys
   - Optimized DOM manipulation

2. `src/modules/Elsa.Studio.Workflows.Designer/Components/ActivityWrappers/ActivityWrapperBase.cs`
   - Added state tracking for size-affecting properties
   - Added conditional size update logic
   - Added check for valid existing sizes

### Backward Compatibility

✅ All changes are backward compatible:
- No breaking API changes
- Existing workflows continue to work
- Size metadata format unchanged
- Cache is transparent to consumers

### Build Status

✅ All builds successful:
- TypeScript compiled without errors
- .NET solution built successfully (Release configuration)
- No new warnings introduced

## Conclusion

These optimizations directly address the reported performance issues by:
1. Reducing the number of expensive DOM operations
2. Eliminating redundant calculations through caching
3. Skipping unnecessary updates through conditional logic
4. Leveraging existing size metadata when available

The combination of these optimizations should restore or exceed the performance levels of version 3.3.4, with expected improvements of 3-50x depending on the specific scenario.
