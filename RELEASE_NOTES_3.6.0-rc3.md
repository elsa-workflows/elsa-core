# Release Notes: 3.6.0-rc3

**Compare**: `3.6.0-rc2...3.6.0-rc3`

This release candidate includes important bug fixes, memory leak resolution, and multi-tenancy improvements for Elsa Workflows 3.6.0.

## 🐛 Fixes

### Workflow Publishing & Serialization

- **Elsa.Workflows.Core**: Fixed uncaught exception in ReadSyntheticInputs preventing workflow publishing when workflow inputs use reserved names like 'Metadata'. The fix collects exceptions gracefully without breaking the publish loop for other workflows. (b8228351ae) (#7199)
  - Root cause: Workflows used as activities with input names matching activity property names (e.g., `Metadata`, `CustomProperties`)
  - Solution: ActivityJsonConverter now logs exceptions instead of throwing, allowing other workflows to publish successfully

### Multi-Tenancy

- **Elsa.Workflows.Runtime**: Added tenant headers support to BackgroundWorkflowCancellationDispatcher. This ensures tenant context is properly passed when canceling suspended workflows in multi-tenant environments. (bc70beff12) (#7040)

### Memory Management

- **Elsa.Common**: Fixed memory leak by properly disposing IronCompressResult in Zstd codec. The codec now returns ArrayPool buffers correctly in both compress and decompress operations. (05d40e3a2a) (#7193)

## 🔧 Improvements

### API Permissions

- **Elsa.Labels**: Updated Labels endpoints to use ElsaEndpoint base class with ConfigurePermissions, bringing them in line with other API endpoints. Removed unused constants from endpoint classes. (72569702fe) (#7205)

## 🧪 Tests

- **Elsa.Workflows.Core**: Added unit tests to document and verify reserved keywords for workflow inputs when used as activities. Tests cover input names that clash with activity properties. (ffd4327d2a) (#7180)
- **Elsa.Common**: Added comprehensive unit tests for Zstd codec compression and decompression operations using xUnit Theory and InlineData. (05d40e3a2a) (#7193)

## 📦 Full changelog

All changes included above represent the complete changelog for this release candidate.

---

**Contributors**: @j03y-nxxbz, @RalfvandenBurg, @Sverre-W, @sfmskywalker, @Copilot

**Affected Packages**: 
- Elsa.Workflows.Core
- Elsa.Workflows.Runtime
- Elsa.Workflows.Management
- Elsa.Common
- Elsa.Labels
- Elsa.Dsl.ElsaScript
