# Tasks: Secrets Module

**Input**: Design documents from `/specs/007-secrets-module/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included because the feature is security-sensitive and the specification requires automated safety checks.

## Phase 1: Setup

- [x] T001 Create `src/modules/Elsa.Secrets` and `test/unit/Elsa.Secrets.UnitTests` project structure.
- [x] T002 Add the Secrets projects to `Elsa.sln`.
- [x] T003 Add paired Studio module structure in `/Users/sipke/.codex/worktrees/ae40/elsa-studio/src/modules/Elsa.Studio.Secrets`.

## Phase 2: Foundational

- [x] T004 Implement secret models, status, references, descriptors, and request/response DTOs in `src/modules/Elsa.Secrets/Models`.
- [x] T005 Implement runtime and management contracts in `src/modules/Elsa.Secrets/Contracts`.
- [x] T006 Implement secret value protection, name validation, type registry, and store registry services in `src/modules/Elsa.Secrets/Services`.
- [x] T007 Implement the Elsa-managed encrypted store and configuration-backed read-only store in `src/modules/Elsa.Secrets/Stores`.
- [x] T008 Register module and shell features in `src/modules/Elsa.Secrets/Features`, `src/modules/Elsa.Secrets/ShellFeatures`, and `src/modules/Elsa.Secrets/Extensions`.

## Phase 3: User Story 1 - Manage Named Secrets (P1)

**Goal**: Operators can create, inspect safe metadata, rotate, revoke, and delete secrets without cleartext reveal.
**Independent Test**: Create a text secret, rotate it, verify the old version is retired, revoke it, and verify resolution fails.

- [x] T009 [US1] Add unit tests for name immutability, rotation, revocation, and no-reveal metadata in `test/unit/Elsa.Secrets.UnitTests/SecretManagerTests.cs`.
- [x] T010 [US1] Implement secret manager lifecycle operations in `src/modules/Elsa.Secrets/Services/DefaultSecretManager.cs`.
- [x] T011 [US1] Implement list/get/create/rotate/revoke/delete/test endpoints in `src/modules/Elsa.Secrets/Endpoints/Secrets`.

## Phase 4: User Story 2 - Use Secrets From Workflows And Modules (P1)

**Goal**: Workflows and modules can resolve immutable secret references to latest active values.
**Independent Test**: Resolve a reference before and after rotation and verify the latest active value is returned.

- [x] T012 [US2] Add unit tests for latest-active reference resolution in `test/unit/Elsa.Secrets.UnitTests/SecretResolverTests.cs`.
- [x] T013 [US2] Implement `ISecretResolver` and legacy `ISecretProvider` adapter in `src/modules/Elsa.Secrets/Services`.
- [x] T014 [US2] Add Studio picker contract DTOs and endpoint in `src/modules/Elsa.Secrets/Endpoints/Secrets/Picker`.

## Phase 5: User Story 3 - Choose Secret Types And Stores (P2)

**Goal**: Operators can pick compatible types and stores, including read-only configuration-backed references.
**Independent Test**: Resolve one encrypted text secret and one configuration-backed secret through the same resolver.

- [x] T015 [US3] Add unit tests for type descriptors, store descriptors, and configuration store resolution in `test/unit/Elsa.Secrets.UnitTests/SecretStoreTests.cs`.
- [x] T016 [US3] Implement text, RSA key, and X.509 reference descriptors in `src/modules/Elsa.Secrets/Types`.
- [x] T017 [US3] Implement descriptors endpoint in `src/modules/Elsa.Secrets/Endpoints/Secrets/Descriptors`.

## Phase 6: Studio UX

**Goal**: Elsa Studio provides a Security > Secrets area plus reusable picker/create UX.
**Independent Test**: Build the Studio module and inspect the management pages/components compile against the server API contract.

- [x] T018 Add Studio API client and models in `/Users/sipke/.codex/worktrees/ae40/elsa-studio/src/modules/Elsa.Studio.Secrets`.
- [x] T019 Add Studio menu, service registration, list/detail/create dialogs, and picker component in `/Users/sipke/.codex/worktrees/ae40/elsa-studio/src/modules/Elsa.Studio.Secrets`.
- [x] T020 Add Studio module project to `/Users/sipke/.codex/worktrees/ae40/elsa-studio/Elsa.Studio.sln` and host/bundle project references.

## Phase 7: Polish

- [x] T021 Update quickstart/docs for server and Studio configuration.
- [x] T022 Run targeted server and Studio builds/tests and fix compile errors.

## Dependencies

- Phase 1 before all implementation.
- Phase 2 blocks all user stories.
- US1 and US2 are the MVP and must pass before Studio UX is considered complete.
- Studio UX depends on the REST contract from US1-US3.

## Implementation Strategy

Implement the smallest secure end-to-end slice first: in-memory metadata, encrypted Elsa-managed values, configuration-backed references, safe management endpoints, runtime resolver, Studio list/detail/create/rotate/revoke UX, and picker component. EF persistence and additional providers can follow once the API and UX shape is validated.
