# Release Notes — elsa-workflows/elsa-core — 3.6.0-rc3

**Comparison:** 3.6.0-rc2...3.6.0-rc3  
**Date:** 2026-01-27

## Highlights

- **Memory leak fixed** in Zstd compression codec (`Elsa.Common`) — IronCompressResult now properly disposed
- **Improved workflow publishing robustness** — Workflows with reserved input names (e.g., `Metadata`, `CustomProperties`) no longer block publishing of other workflows
- **Multi-tenant support improved** — Background workflow cancellation now respects tenant context
- **Labels API refactored** to use `ElsaEndpoint` base class for consistency

## Breaking Changes

None.

## New Features

### Multi-tenant Support in Background Workflow Cancellation

**Package:** `Elsa.Workflows.Runtime`

The `BackgroundWorkflowCancellationDispatcher` now includes tenant header support, ensuring workflow cancellation requests properly propagate tenant context to background workers.

**Impact:** Applications using multi-tenancy with background workflow cancellation will now maintain tenant isolation correctly.

**Reference:** [PR #7040](https://github.com/elsa-workflows/elsa-core/pull/7040)

### Activity Construction Exception Handling

**Package:** `Elsa.Workflows.Core`

Introduced `ActivityConstructionResult` type to capture exceptions during activity construction without blocking workflow deserialization. The `ActivityJsonConverter` now logs construction warnings while allowing other workflows to load successfully.

**Impact:** Improves resilience when deserializing workflow definitions — malformed activities are logged but don't prevent publishing other valid workflows.

**Reference:** Commits 4cd2c4cda, b3d155de6

## Improvements

### Labels API Consistency

**Package:** `Elsa.Labels`

All Labels endpoints refactored to inherit from `ElsaEndpoint` base class. Removed duplicate `Constants.cs` files, centralizing permission configuration.

**Impact:** Internal code cleanup — no consumer-facing API changes.

**Reference:** [PR #7205](https://github.com/elsa-workflows/elsa-core/pull/7205)

### Reserved Keywords Documentation

**Package:** `Elsa.Workflows.Core`

Added unit tests documenting reserved input names (`Metadata`, `CustomProperties`, `_Metadata`, `_CustomProperties`) that conflict with activity base properties when using workflows as activities.

**Impact:** Developers now have clear test-based documentation of naming restrictions for workflow inputs when used as activities.

**Reference:** [PR #7180](https://github.com/elsa-workflows/elsa-core/pull/7180)

## Bug Fixes

### Memory Leak in Zstd Compression Codec

**Package:** `Elsa.Common`

Fixed memory leak where `IronCompressResult` was not disposed after compression/decompression operations.

**Symptoms:** Memory accumulation over time when using Zstd codec for workflow instance compression.

**Fix:** Added `using` declarations to ensure proper disposal of `IronCompressResult` objects.

**Reference:** [PR #7193](https://github.com/elsa-workflows/elsa-core/pull/7193)

### Synthetic Input Property Construction Error

**Package:** `Elsa.Workflows.Core`

Fixed exception thrown when deserializing workflows that use workflows-as-activities with reserved input names (e.g., `Metadata`). Previously, this error would prevent all workflows from being published.

**Root Cause:** When a workflow used as an activity defined an input with a reserved name, the `typeName` property was missing from the synthetic input, causing deserialization to fail.

**Solution:**
- Construction exceptions are now collected in `ActivityConstructionResult`
- `ActivityJsonConverter` logs exceptions but continues processing
- Invalid activities are constructed in a degraded state without throwing

**Reference:** Commits 4cd2c4cda, a69d44794

## Dependencies

No dependency updates in this release.

## Upgrade Notes

### For Users of Zstd Compression

If you experienced memory growth when using Zstd compression for workflow instances, upgrading to 3.6.0-rc3 resolves the leak. No code changes required.

### For Multi-Tenant Applications

Background workflow cancellation now correctly propagates tenant context. Verify that your tenant resolution logic works as expected with background cancellation operations.

### For Workflows with Reserved Input Names

If your workflows-as-activities use input names matching reserved properties (`Metadata`, `CustomProperties`), they will be logged as construction warnings but won't block publishing. Consider renaming these inputs to avoid conflicts:

**Reserved names to avoid:**
- `Metadata`
- `_Metadata`
- `CustomProperties`
- `_CustomProperties`

## Known Issues

None.

---

## Traceability

### Pull Requests
- [#7205](https://github.com/elsa-workflows/elsa-core/pull/7205) — Labels endpoints updated to ElsaEndpoint
- [#7193](https://github.com/elsa-workflows/elsa-core/pull/7193) — Fix memory leak: Dispose IronCompressResult in Zstd codec
- [#7180](https://github.com/elsa-workflows/elsa-core/pull/7180) — Add unit tests to document and verify reserved keywords
- [#7040](https://github.com/elsa-workflows/elsa-core/pull/7040) — Add tenant headers support to BackgroundWorkflowCancellationDispatcher

### Commits
- `72569702f` — Updated to Labels-endpoints to ElsaEndpoint (#7205)
- `b0b2080a3` — Apply suggestion from @sfmskywalker
- `a69d44794` — Removing unused variables
- `b3d155de6` — Actually, I should not have handled obsolete classes/interfaces. Handling that as it should without changing the contract
- `4cd2c4cda` — Bugfix: error thrown in ReadSyntheticInputs
- `05d40e3a2` — Fix memory leak: Dispose IronCompressResult in Zstd codec (#7193)
- `bc70beff1` — Add tenant headers support to BackgroundWorkflowCancellationDispatcher (#7040)
- `ffd4327d2` — Add unit tests to document and verify reserved keywords (#7180)

### Affected Packages
- `Elsa.Common` — Memory leak fix in Zstd codec
- `Elsa.Workflows.Core` — Activity construction improvements, reserved keywords documentation
- `Elsa.Workflows.Runtime` — Multi-tenant support for background cancellation
- `Elsa.Labels` — API consistency refactoring
- `Elsa.Workflows.Management` — Activity describer updates
- `Elsa.Dsl.ElsaScript` — Compiler adjustments for new construction result type

---

### Contributors

Thanks to the following contributors for this release:
- @RalfvandenBurg
- @sfmskywalker (Sipke Schoorstra)
- @j03y-nxxbz (Joey Barten)
- @Sverre-W (Sverre Winkelmans)
- @Copilot
