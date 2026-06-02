# Persistence vNext Tasks

## S1 - Core Manifest And Planner Foundations

- [x] T001 Add provider-neutral storage descriptors.
- [x] T002 Add relational planner and SQLite renderer.
- [x] T003 Add SQL Server renderer.
- [x] T004 Add document planner.
- [x] T005 Add SQLite schema history/version runner.
- [x] T006 Add Secrets sample manifest.
- [x] T007 Add focused tests for relational, document, and version runner behavior.
- [ ] T008 Open/update PR for S1.
- [x] T009 Run self-review loop for S1.
- [ ] T010 Merge S1 PR when no big issues remain.
- [ ] T011 Close/complete associated S1 issue after merge.

## S2 - Portable SQLite Document Store MVP

- [x] T012 Define document envelope and store contracts.
- [x] T013 Add SQLite document table materialization.
- [x] T014 Implement save/load/delete.
- [x] T015 Implement generic field index storage.
- [x] T016 Implement index maintenance in the same transaction.
- [x] T017 Implement query by declared indexes.
- [x] T018 Add optimistic concurrency.
- [x] T019 Add unindexed-query validation.
- [x] T020 Add SQLite document store tests.
- [ ] T021 Open/review/merge S2 PR and close associated issue.

## S3 - SQL Server And PostgreSQL Relational Document Providers

- [x] T022 Implement SQL Server document provider.
- [x] T023 Implement PostgreSQL document provider.
- [x] T024 Add startup locking/materialization strategy.
- [ ] T025 Add relational provider contract tests.
- [ ] T026 Open/review/merge S3 PR and close associated issue.

## S4 - MongoDB Document Provider

- [x] T027 Implement MongoDB document provider.
- [x] T028 Materialize native collections and indexes.
- [x] T029 Implement indexed query translation.
- [x] T030 Add optimistic concurrency.
- [ ] T031 Add MongoDB provider contract tests.
- [ ] T032 Open/review/merge S4 PR and close associated issue.

## S5 - Elsa Integration And First Module Migration

- [x] T033 Add Elsa vNext persistence integration package.
- [x] T034 Add shell feature/options/startup materialization.
- [x] T035 Add diagnostics for applied manifests and provider status.
- [x] T036 Implement Secrets vNext store.
- [ ] T037 Add migration/import path from existing EF Core store if required.
- [ ] T038 Open/review/merge S5 PR and close associated issue.

## S6 - Runtime-Defined Entities

- [ ] T039 Define runtime entity definition model.
- [ ] T040 Add draft/published/retired lifecycle.
- [ ] T041 Add validation and capability checks.
- [ ] T042 Add CRUD/API integration.
- [ ] T043 Add audit trail.
- [ ] T044 Open/review/merge S6 PR and close associated issue.

## S7 - Physicalization And Performance

- [ ] T045 Add storage policy model.
- [ ] T046 Implement optimized relational indexes for one provider.
- [ ] T047 Implement native document-provider optimization for one provider.
- [ ] T048 Add benchmarks.
- [ ] T049 Open/review/merge S7 PR and close associated issue.

## S8 - Workflow Runtime Evaluation And Hardening

- [ ] T050 Benchmark runtime persistence candidates.
- [ ] T051 Validate concurrency, locking, and retry behavior.
- [ ] T052 Add provider diagnostics and recovery guidance.
- [ ] T053 Document go/no-go decision for workflow runtime stores.
- [ ] T054 Open/review/merge S8 PR and close associated issue.
