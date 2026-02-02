# Release Notes: Elsa 3.6.0-rc3

**Compare**: `3.6.0-rc2...3.6.0-rc3`

This release candidate includes significant multitenancy improvements, bug fixes, and enhanced test coverage.

---

## ⚠️ Breaking changes / upgrade notes

### Multitenancy Tenant ID Convention Changes

**Affected users**: Applications using multitenancy features, especially those with existing tenant data.

**What changed**:
- **Default tenant ID convention**: The default tenant now uses an empty string (`""`) instead of `null` (ADR-0008). (b09a56481) (#7217)
- **Tenant-agnostic entity convention**: Tenant-agnostic entities (visible to all tenants) now use asterisk (`"*"`) as the sentinel value instead of `null` (ADR-0009). (7bc9035f5) (#7226)
- **Database schema impact**: Database indexes on the `Triggers` table now include `TenantId` in unique constraints across all EF Core providers. (b09a56481) (#7217)

**Migration required**:
- **SQL Server users**: Database migrations are included to automatically convert existing `null` `TenantId` values to empty strings for default tenant data in `WorkflowDefinitions`, `WorkflowInstances`, and runtime entities.
- **Other database providers** (PostgreSQL, MySQL, SQLite, Oracle): Similar migrations will be needed but are not yet included in this release.
- Review ADR-0008 (Default Tenant ID) and ADR-0009 (Tenant-Agnostic Entities) in `doc/adr/` for detailed rationale and migration guidance.

**Query filter changes**: EF Core query filters have been updated to handle the new tenant ID conventions, ensuring tenant isolation is properly maintained. (7bc9035f5) (#7226)

---

## ✨ New features

### Multitenancy enhancements

- **Configuration-based multitenancy**: Introduced configuration-based tenant provider to streamline tenant initialization and customization. (b09a56481) (#7217)
- **Tenant-agnostic workflow support**: Workflows and activities can now be marked as tenant-agnostic (using `"*"` as tenant ID) to make them accessible across all tenants. (7bc9035f5) (#7226)
- **Selective lock mocking in tests**: Added `SelectiveMockLockProvider` for precise lock mocking in tests without affecting unrelated background operations. (b09a56481) (#7217)

---

## 🔧 Improvements

### Multitenancy

- **Tenant ID normalization**: Introduced `NormalizeTenantId` method for consistent tenant ID handling throughout the codebase. (b09a56481) (#7217)
- **Tenant filtering in workflow store populator**: Added tenant-specific filtering in `DefaultWorkflowDefinitionStorePopulator` to ensure workflows are only loaded for the current tenant. (b09a56481) (#7217)
- **Tenant isolation in activity provider**: Enforced tenant isolation in `WorkflowDefinitionActivityProvider` and included tenant ID in activity type names for better separation. (558902bb7)
- **Optimized activity registry**: Improved `ActivityRegistry.Find` to prefer tenant-specific descriptors over tenant-agnostic ones with single-pass iteration for better performance. (7bc9035f5) (#7226)
- **Tenant headers support**: Added tenant headers support to `BackgroundWorkflowCancellationDispatcher` for proper tenant context propagation during workflow cancellation. (bc70beff1) (#7040)

### API improvements

- **Labels endpoints migration**: Updated Labels endpoints to use `ElsaEndpoint` base class with standardized permission configuration. (72569702f) (#7205)

### Error handling

- **Graceful `ParentInstanceId` handling**: Enhanced `ResumeBulkDispatchWorkflowActivity` to gracefully handle missing `ParentInstanceId` values. (641dd664d)
- **Improved async handling**: Enhanced async handling in `CommandHandlerInvokerMiddleware` to properly await tasks without blocking. (7bc9035f5) (#7226)
- **Enhanced logging for recurring tasks**: Added error handling and logger support to prevent crashes in scheduled timers. (7bc9035f5) (#7226)

---

## 🐛 Fixes

- **Synthetic inputs exception handling**: Fixed uncaught exception thrown in `ReadSyntheticInputs` when workflow inputs have reserved names like 'Metadata'. Now collects exceptions and returns them to the caller with a valid `IActivity` instance instead of breaking the publishing loop. (4cd2c4cda, b8228351a) (#7199)
- **Memory leak in compression**: Fixed memory leak by properly disposing `IronCompressResult` in Zstd codec implementation. (05d40e3a2) (#7193)
- **Default tenant data visibility**: Fixed data visibility leak by removing `NullIfEmpty` conversion to properly distinguish between default tenant (`""`) and tenant-agnostic (`"*"`) entities. (7bc9035f5) (#7226)

---

## 🧪 Tests

- **Reserved keywords documentation**: Added unit tests to document and verify reserved keywords in workflow inputs (e.g., `CustomProperties`, `Metadata`). (ffd4327d2) (#7180)
- **Tenant ID normalization tests**: Added comprehensive unit tests for tenant ID normalization covering null, empty, and valid ID scenarios. (b09a56481) (#7217)
- **Multitenancy pipeline tests**: Added tests for multitenancy pipeline invoker covering various tenant resolution scenarios. (b09a56481) (#7217)
- **Activity registry tests**: Added extensive tests for `ActivityRegistry` to verify tenant-specific and tenant-agnostic descriptor handling. (7bc9035f5) (#7226)
- **Compression codec tests**: Added tests for Zstd codec compression and decompression to prevent memory leaks. (05d40e3a2) (#7193)
- **Workflow deletion tests**: Enhanced `DeleteWorkflowTests` with signal-based waiting and improved registry refresh verification. (641dd664d, 25bf37705, cc3a2c2c0, afe33baf0)
- **Activity construction tests**: Improved test coverage for `ActivityConstructionResult` and exception handling scenarios. (b09a56481) (#7217)
- **Workflow store populator tests**: Expanded test coverage for `DefaultWorkflowDefinitionStorePopulator` with tenant-specific scenarios. (b09a56481) (#7217)

---

## 🔁 CI / Build

- **Component test configuration**: Disabled multitenancy in component tests to simplify test setup and improve reliability. (afe33baf0)
- **Test project configuration**: Adjusted threshold settings and updated GitHub Actions versions in test projects. (afe33baf0)

---

## 📦 Dependencies

- **Deprecated `CommonPersistenceFeature`**: Removed deprecated `CommonPersistenceFeature` in favor of modular persistence feature extension. (b09a56481) (#7217)

---

## 📖 Documentation

- **Architecture Decision Records**: Added multiple ADRs documenting multitenancy design decisions:
  - ADR-0005: Token-centric flowchart execution model
  - ADR-0006: Tenant Deleted event
  - ADR-0007: Explicit merge modes for flowchart joins
  - ADR-0008: Empty string as default tenant ID (⚠️ breaking)
  - ADR-0009: Asterisk as tenant-agnostic sentinel value (⚠️ breaking)
  - (b09a56481, 7bc9035f5) (#7217, #7226)

---

## 📦 Full changelog

**Commits not covered above:**

- Remove duplicate reference to `tenant-deleted-event` ADR in solution file. (af1e4e835)
- Remove unused variables in synthetic input handling. (a69d44794)
- Apply code review suggestions from @sfmskywalker. (b0b2080a3)
- Refactor obsolete class handling to avoid contract changes. (b3d155de6)

---

## 🙏 Contributors

Special thanks to all contributors to this release:
- @sfmskywalker
- @Copilot
- @copilot-swe-agent
- Community contributors who reported issues and provided feedback

---

**Note**: This is a release candidate. Please report any issues on [GitHub Issues](https://github.com/elsa-workflows/elsa-core/issues).
