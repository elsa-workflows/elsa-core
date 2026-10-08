# [Story] F2.2: Resolve Secrets package and upgrade compatibility

Issue: [#8275](https://github.com/elsa-workflows/elsa-core/issues/8275)
Parent: #8214 — Incremental import of extensions into core
Program: #8194

## User story

As an Elsa maintainer consolidating Core, Extensions and Studio, I need one explicit Secrets package/API owner and a supported upgrade path from published consumers so the import does not break secret management APIs or strand persisted credentials.

## Scope

This slice covers the four `Elsa.Secrets.Persistence.EFCore*` package IDs, `Elsa.Studio.Secrets`, the separate published legacy packages `Elsa.Secrets.Api`, `Elsa.Secrets.Core`, `Elsa.Secrets.Management`, `Elsa.Secrets.Models` and `Elsa.Secrets.Scripting`, and the corresponding host/UI activation boundary. It establishes compatibility evidence and an implementation decision. It does not authorize release publisher cutover or invent a credential transformation.

## Acceptance criteria

- [ ] Record the currently published versions and nuspec source provenance for every affected package ID; identify missing provenance, private-feed uncertainty and supported consumer versions.
- [ ] Name the canonical source and sole future publisher for each duplicate package ID, with a reviewed capability map for the separate legacy package graph.
- [ ] Keep the legacy API endpoint package out of the default consolidated host while routes overlap, or prove an explicit route migration with permission and request/response contract tests.
- [ ] Compile representative consumers for net8.0, net9.0 and net10.0 against the selected package graph, including target-framework-conditioned package dependencies.
- [ ] Execute database upgrade and rollback tests from each supported published persistence baseline on SQLite, PostgreSQL and SQL Server. Cover existing secret values, versions, statuses, tenant ownership/default tenant, duplicate names, expiration metadata and encryption-key compatibility.
- [ ] Require security review and an independently verified cryptographic contract before any old-to-new value conversion; the test must show secrets remain decryptable and are not exposed in logs or migration artifacts.
- [ ] Test the selected Studio/backend pair for list/detail/create/update/rotate/revoke/test and workflow picker behavior, plus authorization and tenant isolation.
- [ ] Document supported upgrade steps, unsupported combinations, deprecation timing, recovery/rollback, and one publisher per package. Do not remove or silently republish legacy capabilities.

## Tasks

1. Inventory public artifacts and internal consumers. Record exact package versions, nuspec provenance, installed projects/workbench references, and uncertain `Elsa.Secrets.Models` provenance; distinguish published release sources from current source-tree copies.
2. Freeze the package/host boundary for the first consolidated build. Keep duplicate package copies and the legacy Core/Management/Models/API/Scripting graph out of default project discovery and host activation until the chosen path is implemented.
3. Compare public API surfaces and routes. Add compile-time consumer fixtures for old API/DI usage and route tests proving one handler per verb/path with permission checks.
4. Specify a reviewed persistence transition from supported released baselines. Build old-schema fixtures from published migration artifacts, then run provider-specific upgrade/downgrade tests; stop if encryption, key ownership or tenant semantics cannot be proven.
5. Build and pack the selected package graph under net8.0/net9.0/net10.0, resolve conditional central versions and missing UI dependencies, and verify a clean consumer restore.
6. Run backend/Studio integration and browser tests for current Secrets workflows and any retained legacy capability; document gaps and deprecation/adapter choices.
7. Publish the compatibility ledger, rollback instructions and publisher proposal for review. Keep cutover as a separate approval and execution step.

## Evidence

See [Secrets package lineage and compatibility evidence](../secrets-collision-compatibility.md). The first implementation Task is [#8276](https://github.com/elsa-workflows/elsa-core/issues/8276); broader provider and API tasks remain in the Story backlog.
