# Elsa 3 integration program: initial execution evidence

Program [#8194](https://github.com/elsa-workflows/elsa-core/issues/8194), audited 2026-09-23. This phase establishes evidence and proposed architecture. It does not deliver the repository migration, independent publishing, or production connectors.

## Backlog and delivery state

The existing program, 10 epics, 40 features, 6 stories, and 12 tasks were read rather than recreated. All 68 native parent links and 26 explicit blocking dependencies are configured and verified. See [reconciliation](reconciliation.md), [snapshot](hierarchy.json), and the read-only validator. Issue acceptance remains open while evidence PRs are reviewed.

## Reviewable deliverables

| Evidence | Pull request | Existing tasks |
|---|---|---|
| Native hierarchy, separate dependency graph and read-only verifier | [#8264](https://github.com/elsa-workflows/elsa-core/pull/8264) | #8205 |
| Complete project/module inventory and publishing ownership | [#8266](https://github.com/elsa-workflows/elsa-core/pull/8266) | #8251, #8252 |
| Secrets evidence matrix, proposed lifecycle ADR and validation scenarios | [#8265](https://github.com/elsa-workflows/elsa-core/pull/8265) | #8253, #8254, #8261 |
| Catalog schema, broad survey, deep assessments and conditional priorities | Review evidence linked from [#8255](https://github.com/elsa-workflows/elsa-core/issues/8255) | #8255, #8256, #8257, #8258 |

These PRs are independent evidence changes. Their paths may not exist in a checkout until the corresponding PR is integrated; follow the PR links for review. Open PRs do not complete the issues' acceptance criteria.

## Source baselines

| Repository | Inspected commit | Projects |
|---|---|---:|
| Core | `610790ec57ae9d5c334181d50c1e65f99613fd86` | 171 |
| Extensions | `33fa0bfd28c7585240e3d4f665058c067b17e287` | 101 |
| Studio | `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822` | 71 |

These are source snapshots, not release tags. GitHub currently advertises 3.8.4 as the latest release for each repository; neither this label nor a static project declaration proves the contents or compatibility of an individual published package.

## Architecture recommendations

Keep repository organization and release policy separate. Co-locate Core, all Extensions categories, and Studio incrementally, preserving existing package IDs, serialized activity identities, source history, tests and assets. Retain coordinated engine/platform releases until compatibility evidence permits separation. Each connector or tightly coupled backend/Studio pair gets an explicit release unit; unrelated connectors and the Studio shell must not be bumped or published as a side effect.

Treat source debugging and package compatibility as different proofs. A focused solution/filter should debug backend and Blazor modules together with project references. A separate clean consumer must restore the exact built packages against declared released Core/Studio versions. The proof must inspect actual package dependency metadata, SourceLink/source commit and hashes; success from project references alone is insufficient.

Keep three responsibilities separate: connection metadata and authorization, secret persistence, and provider authentication lifecycle. Prefer reusing Secrets protection and provider seams, but do not treat versioned secret values as a compare-and-swap credential protocol. OAuth refresh needs durable coordination, stale-writer protection, provider-specific recovery and explicit handling of the crash between provider success and local persistence. A lease cannot make that cross-system boundary atomic.

Provider research determines the supported operation and lifecycle contract. Catalog presence is not implementation, demand, or shared-cloud certification. OneDrive, Moneybird and Slack are candidate pilot slices until their documented readiness gates, operator ownership and test access are satisfied. Elsa 4 portability should preserve useful provider logic without imposing a universal adapter abstraction on Elsa 3.

## Findings that determine the next batch

The inventory identifies five cross-repository package-ID collisions: four `Elsa.Secrets.Persistence.EFCore` family IDs and `Elsa.Studio.Secrets`. Choose the canonical implementation and sole publisher before import. The source inventory and selected MSBuild evaluation are planning evidence, not proof that every project is publishable or compatible. Full runtime activity descriptor export remains a migration prerequisite.

The current Secrets implementation encrypts stored `Value` using configured AES-GCM protection, while metadata remains outside that encrypted payload. It does not implement the key-ring behavior described in the earlier spec, distributed refresh coordination, or runtime connection-use authorization. Version rotation uses aggregate read/update/save rather than a compare-and-swap lifecycle protocol. The proposed ADR therefore reuses Secrets behind a separate connection lifecycle service with durable operation state and generation references. Generic secret mutation paths need an explicit guard for lifecycle-owned generations; unique names alone do not enforce immutability.

The current Slack module is a useful packaging candidate because a public 3.8.4 artifact and dedicated test project exist. Six declared Watch activities throw `NotImplementedException`, and the module accepts a raw token input; neither working triggers nor Secrets integration is established. The package proof should retain an aligned released baseline while testing current source separately. Slack has no natural paired Studio module: it cannot alone prove backend/Blazor co-debugging.

Existing Secrets and inbound External Authentication suites passed 120, 205 and 34 filtered tests respectively on .NET 10; PR #8265 retains the execution record. Those results do not validate future distributed outbound OAuth behavior. Hierarchy validation checks the full native graph against the recorded edges. No wholesale solution build or future package-consumer proof is claimed by this audit.

## Decisions and execution boundaries

Architecture recommendations in this phase are proposed for review. Release ownership for duplicate package IDs must be resolved before import; named operational maintainers and provider accounts must be established before live certification. No account purchases, real token handling, package publication, repository archival, PR merge or production changes are part of this phase.

The catalog distinguishes a broad candidate survey from deep assessments, and official API feasibility from workflow hypotheses and demand evidence. Unknown demand stays unknown rather than receiving a zero or an invented popularity score. Pilot commitments remain gated by a concrete sponsor/use case, named maintainer, test access and provider-specific auth/event constraints.

Use the existing downstream tasks rather than adding a second queue. #8259 selects and designs the independent release proof after reviewing #8251/#8252. #8260 implements its bounded local-artifact proof only after that design is accepted. #8262 turns the credential ADR into a synthetic-credential validation slice before production credential lifecycle implementation. Broad migration and full provider coverage remain later work.
