# Feature Specification: Secrets Module

**Feature Branch**: `007-secrets-module`  
**Created**: 2026-05-19  
**Status**: Draft  
**Input**: User description: "Design a revamped Secrets module for Elsa Workflows and Elsa Studio inspired by Orchard Core secrets: named secrets, pluggable stores, extensible secret types and editors, secret picker UX, permissions, import/export encryption support, and migration from existing sensitive fields."

## Product Context

Elsa needs a first-class secrets capability for workflow authors, module developers, and operators. The goal is to replace ad hoc secret handling with named secret references that can be resolved consistently at runtime, managed safely in Elsa Studio, and stored in the right backend for each deployment.

The existing `elsa-extensions` implementation provides a useful baseline:

- Core resolution through a simple secret provider.
- Management services for create, update, versioning, expiration, revocation, and name validation.
- In-memory and EF Core persistence.
- API endpoints and a basic Elsa Studio management screen.
- JavaScript expression helpers.

The revamped module should preserve the useful concepts, but expand the model toward the Orchard Core pattern from the transcript: secret values are named, typed, store-backed, extensible, selectable from Studio pickers, and usable by other modules without those modules knowing how or where the secret is stored.

This feature's delivery boundary is the Elsa Core/server module plus the paired Elsa Studio implementation in the sibling `elsa-studio` repository.

## Clarifications

### Session 2026-05-19

- Q: How should workflow definitions and module settings persist secret references? → A: Secret technical names are immutable; users create a new secret when a different technical name is needed.
- Q: Should Studio/API support revealing current cleartext secret values? → A: No cleartext reveal after creation; users can only replace, rotate, use, test, or encrypted-export secrets.
- Q: Which secret version should references resolve? → A: References always resolve the latest active version for the immutable technical name.
- Q: How should import handle existing secret technical-name conflicts? → A: Existing-name conflicts are errors unless the import request explicitly chooses create-new, update/rotate, or skip.
- Q: Which secret stores are in v1 scope? → A: V1 includes an Elsa-managed encrypted store and configuration-backed read-only store; other stores are optional later packages.
- Q: Is Studio implementation part of this feature's delivery boundary? → A: Studio implementation is included in the paired `elsa-studio` repository work.

### Existing Module Gaps To Address

- The current model treats a secret primarily as one encrypted text value with optional scope; it does not distinguish logical secret metadata, typed payload metadata, store metadata, and versioned value material cleanly enough for external stores.
- Store selection is a module-level persistence choice rather than a per-secret authoring decision.
- The current implementation encrypts Elsa-owned values with Data Protection, which is useful for local persistence but not sufficient for moving encrypted secret payloads across isolated environments.
- General API models include encrypted value payloads, and the edit flow can retrieve cleartext through an input model endpoint; the revamped API should use explicit metadata, update, use, test, and encrypted-export boundaries without cleartext reveal.
- The existing extension Studio package provides basic list/create/edit pages, but not a reusable picker for sensitive workflow inputs or module settings; this spec defines the required Studio contracts and UX, while the concrete implementation belongs in `elsa-studio`.
- Secret scopes are hardcoded UI choices rather than a typed, extensible classification system.
- Shell feature classes in the extension are placeholders in several packages and need complete feature registration before promotion.
- JavaScript helpers are generated from secret names, which is convenient but brittle for arbitrary names and should be stabilized.
- There is no import/export contract for references, encrypted value movement, conflict handling, or external store behavior.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manage Named Secrets (Priority: P1)

An operator can create, view, update, rotate, retire, revoke, and delete named secrets in Elsa Studio without exposing secret values by default. Each secret has metadata that helps users understand what it is for and whether it is usable.

**Why this priority**: This is the core product value. Without a trusted management experience, workflow authors and module developers keep storing sensitive values in workflow definitions, settings, or app configuration.

**Independent Test**: Enable the secrets module, open Studio, create a secret, update it, verify a new active version is available, revoke it, and verify runtime resolution no longer returns a usable value.

**Acceptance Scenarios**:

1. **Given** an authorized operator opens Studio, **When** they create a text secret with a name, value, type, store, and description, **Then** the secret appears in the secrets list without showing its value.
2. **Given** an existing active secret, **When** the operator updates its value, **Then** the module creates a new active version and marks the previous active version as retired.
3. **Given** a revoked or expired secret, **When** runtime code attempts to resolve it, **Then** resolution fails with a clear non-secret error.
4. **Given** a user without read permission opens Studio, **When** they navigate to the secrets section, **Then** they cannot list secret metadata.
5. **Given** a user with metadata read permission, **When** they inspect a secret, **Then** they can see metadata but cannot reveal the current cleartext value.

---

### User Story 2 - Use Secrets From Workflows And Modules (Priority: P1)

A workflow author or module developer can reference a secret by immutable technical name from sensitive inputs, shell settings, and module configuration. The consuming module receives the resolved value at runtime without knowing whether it came from a database, configuration, key vault, certificate store, or another provider.

**Why this priority**: The module exists to remove duplicated password handling from each Elsa module and to keep sensitive workflow inputs out of workflow definition payloads.

**Independent Test**: Configure a workflow activity with a secret reference instead of a literal connection string, execute the workflow, and verify the activity receives the resolved value while the workflow definition stores only the reference.

**Acceptance Scenarios**:

1. **Given** an activity input is marked as sensitive, **When** a workflow author edits the activity in Studio, **Then** Studio offers a secret picker and optional inline secret creation.
2. **Given** a workflow definition contains a secret reference, **When** the workflow runs, **Then** the runtime resolves the latest active version through the secrets service and passes only the resolved value to the consuming activity.
3. **Given** a module setting supports secret references, **When** an operator selects a secret in Studio or shell configuration, **Then** the module stores the immutable technical name and resolves the value only when needed.
4. **Given** a secret value is unavailable because the store is unreachable, **When** runtime resolution is attempted, **Then** the caller receives a clear resolution failure without leaking the reference target's value or backend details.

---

### User Story 3 - Choose Secret Types And Stores (Priority: P2)

An operator can choose what kind of secret they are creating and where it is stored. Different secret types have different editors, validation rules, display metadata, and resolution behavior.

**Why this priority**: Orchard's design is valuable because it separates the logical secret from its storage backend and type-specific authoring experience. Elsa needs the same extension points for cloud stores, certificates, keys, and module-specific secret shapes.

**Independent Test**: Register the v1 stores and secret types, create one secret in the built-in encrypted store and one secret referencing a configuration-backed read-only value, then resolve both through the same runtime service.

**Acceptance Scenarios**:

1. **Given** multiple secret stores are available, **When** an operator creates a secret, **Then** Studio requires them to choose a supported store or accepts a configured default.
2. **Given** a text secret type is selected, **When** the editor is shown, **Then** Studio presents a value editor and validates required text input.
3. **Given** an RSA key type is selected, **When** the editor is shown, **Then** Studio supports generating or entering key material according to the registered type rules.
4. **Given** an X.509 certificate reference type is selected, **When** the editor is shown, **Then** Studio supports selecting or entering certificate identity metadata without copying certificate private material into Elsa-managed storage.
5. **Given** an external store does not support writing values from Studio, **When** an operator creates a secret reference for that store, **Then** Studio only asks for the lookup metadata the store supports.

---

### User Story 4 - Export, Import, And Move Securely (Priority: P2)

An operator can export workflows and configuration that contain secret references without accidentally exporting raw secret values. When secret values must move between environments, the export process encrypts them for an explicit import target or key.

**Why this priority**: The transcript identifies import/export as a key reason for the module. Elsa needs a safe way to move workflow packages between isolated staging and production environments without relying on shared Data Protection keys.

**Independent Test**: Export a workflow package containing a secret-backed setting, inspect the package to verify no raw secret value exists, import into another environment with the expected decryption material, and verify the imported reference resolves.

**Acceptance Scenarios**:

1. **Given** a workflow contains secret references, **When** the workflow is exported without value export enabled, **Then** the export contains only references and metadata safe for transport.
2. **Given** an authorized operator explicitly exports secret values, **When** they choose an export encryption target, **Then** exported values are encrypted for that target and cannot be read as raw payload text.
3. **Given** an import package contains encrypted secret payloads, **When** the target environment has the matching import key or certificate, **Then** the import can create or update the corresponding secrets.
4. **Given** the import target cannot decrypt a secret payload, **When** import runs, **Then** the importer reports which secret could not be imported without revealing the value.
5. **Given** an environment uses external store references, **When** export runs, **Then** the operator can choose whether to export only references or include encrypted values where the store permits reading.
6. **Given** an import package contains a secret whose technical name already exists, **When** the operator has not selected create-new, update/rotate, or skip for that conflict, **Then** import fails that item without changing the existing secret.

---

### User Story 5 - Govern Access And Audit Use (Priority: P3)

Administrators can delegate secret metadata management, value updates, encrypted export, and runtime use independently. The system records security-relevant events so operators can answer who changed, used, tested, exported, or deleted a secret.

**Why this priority**: Secrets are a security boundary. The module should avoid granting broad access just because a user can manage the consuming feature, such as SMTP, SQL, or Service Bus configuration.

**Independent Test**: Assign a user permissions for metadata management but not encrypted export, verify they can rotate a secret without seeing the old value, then verify audit events are emitted for create, update, use/test, export, revoke, and delete.

**Acceptance Scenarios**:

1. **Given** a user can manage a module that consumes a secret, **When** they edit that module's settings, **Then** they can select allowed secrets without gaining permission to update, test, or encrypted-export those secrets.
2. **Given** a user tests or encrypted-exports a secret value, **When** the operation succeeds, **Then** the module records an audit event containing actor, action, secret identity, time, and reason where provided.
3. **Given** a user can update a secret value, **When** they rotate or replace it, **Then** Studio lets them submit a replacement value without showing the current value.

### Edge Cases

- A secret technical name collides with another secret after trimming or case normalization.
- A user wants to change a secret's technical name; the system requires creating a new secret and repointing consumers.
- A secret store supports reading but not writing, listing, deletion, versioning, testing, or encrypted export.
- A workflow references a secret that was deleted, revoked, expired, or moved to an unavailable store.
- A workflow was created before a secret was rotated; the next run resolves the new latest active version automatically.
- Multiple versions exist and more than one is incorrectly marked latest.
- A secret value is too large for the selected store.
- A secret type editor is unavailable in Studio because the related plugin is not installed.
- Import attempts to create or update a secret whose technical name already exists.
- A user can use a secret through a workflow but cannot read its metadata in Studio.
- Logs, validation errors, API responses, export packages, and Studio notifications must not contain raw secret values.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST support named logical secrets that can be referenced by immutable technical name from workflows, module settings, and other Elsa features without embedding raw secret values in those artifacts.
- **FR-002**: The system MUST distinguish secret metadata from secret values so callers can list and inspect safe metadata without retrieving or revealing values.
- **FR-003**: The system MUST support explicit value resolution by immutable technical name through a runtime service available to workflows and modules.
- **FR-004**: The system MUST support secret versioning, including a single active latest version per logical secret and retired previous versions after rotation.
- **FR-005**: The system MUST support secret statuses including active, retired, expired, and revoked.
- **FR-006**: The system MUST prevent expired, revoked, deleted, or otherwise inactive secrets from resolving as usable values.
- **FR-007**: The system MUST support pluggable secret stores behind a common abstraction.
- **FR-008**: The system MUST include a built-in encrypted Elsa-managed store suitable for development and simple production deployments.
- **FR-009**: The system MUST support read-only or externally managed stores where Elsa stores lookup metadata but does not own the underlying value.
- **FR-010**: V1 MUST support configuration-backed read-only secrets so deployments can reference values from existing application configuration.
- **FR-011**: Cloud vault, operating-system certificate stores, and other external stores SHOULD be supported through optional later provider packages after the v1 store abstraction is stable.
- **FR-012**: The system MUST support extensible secret types with type metadata, validation, and Studio editor registration.
- **FR-013**: The first supported secret types MUST include text value, RSA key material, and X.509 certificate reference.
- **FR-014**: The system MUST let secret type providers declare which stores they support and which operations they allow.
- **FR-015**: The system MUST expose management APIs for listing metadata, creating, updating, rotating, revoking, deleting, and validating secret names.
- **FR-016**: The system MUST require explicit permission for encrypted value export, value update, metadata read, metadata write, deletion, runtime use, and test operations.
- **FR-017**: The system MUST not return raw secret values from general list, get, validation, or picker APIs.
- **FR-018**: The system MUST NOT support user-initiated cleartext reveal of current secret values after creation.
- **FR-019**: The system MUST record audit events for create, update, rotate, revoke, delete, runtime use/test where observable, encrypted export, import, and failed privileged operations.
- **FR-020**: The Core/server feature MUST define the Studio contract for a Secrets area under security or settings where authorized users can search, filter, create, edit, rotate, revoke, delete, and inspect metadata.
- **FR-021**: The Core/server feature MUST define the Studio contract for a reusable secret picker component that can be used by workflow activity editors and module settings editors.
- **FR-022**: The secret picker contract MUST support filtering by allowed type, allowed scope, status, store capability, and consuming context.
- **FR-023**: The secret picker contract MUST support creating a new compatible secret inline when the user has permission.
- **FR-024**: Sensitive workflow inputs marked by existing metadata MUST be eligible for secret reference editing in Studio.
- **FR-025**: Workflow definitions MUST persist secret references rather than resolved values when a secret is selected.
- **FR-026**: Runtime workflow execution MUST resolve selected secret references only at the point of use.
- **FR-027**: The system MUST provide import/export behavior that never includes raw secret values unless an authorized operator explicitly chooses an encrypted value export.
- **FR-028**: Encrypted secret export MUST use an explicit import/export key, certificate, or asymmetric target that can work across environments without shared application data-protection keys.
- **FR-029**: Import MUST support creating missing secrets, updating compatible existing secrets, skipping conflicting secrets, and reporting conflicts without revealing values.
- **FR-030**: The system MUST provide a migration path from the existing `elsa-extensions` secrets model to the revamped model.
- **FR-031**: The migration path MUST preserve secret names, descriptions, scopes, expiration, statuses, latest-version semantics, and EF Core persisted values where practical.
- **FR-032**: Existing `ISecretProvider.GetSecretAsync(name)` consumers SHOULD continue to work through an adapter while newer code moves to richer secret reference and resolution APIs.
- **FR-033**: Shell feature registration MUST be complete and usable for Core, Management, API, Scripting, and Studio-facing capabilities.
- **FR-034**: The system MUST avoid logging raw secret values in success paths, failures, validation messages, audit records, and import/export diagnostics.
- **FR-035**: JavaScript expression integration SHOULD expose stable secret access helpers without generating unsafe identifiers from arbitrary secret names.
- **FR-036**: The system MUST not expose encrypted payloads, external-store lookup secrets, or provider-private metadata through general metadata APIs.
- **FR-037**: The system MUST treat the secret technical name as immutable after creation; changing the technical name requires creating a new secret and updating consumers to reference it.
- **FR-038**: The system MUST allow users to replace or rotate a secret value without retrieving the previous cleartext value.
- **FR-039**: The system MUST resolve secret references to the latest active version for the referenced immutable technical name.
- **FR-040**: Import MUST treat same-technical-name conflicts as errors unless the import request explicitly chooses create-new, update/rotate, or skip behavior for the conflict.
- **FR-041**: V1 MUST include exactly two production-oriented store implementations: an Elsa-managed encrypted store and a configuration-backed read-only store.
- **FR-042**: The concrete Studio UI implementation MUST be delivered in the paired `elsa-studio` repository.

### Key Entities *(include if feature involves data)*

- **Secret**: A logical named item users and modules reference. It has an immutable technical name, optional display metadata, type, scope/tags, status, owner/tenant metadata, and links to versions.
- **Secret Version**: A specific version of a secret's value or external reference metadata. It has version number, status, creation time, optional expiration, and store-specific payload metadata.
- **Secret Type**: Describes how a secret is authored, validated, displayed, resolved, and optionally exported. Examples: text, RSA key, X.509 certificate reference.
- **Secret Store**: A backend capable of reading or writing secret payloads or references. Examples: Elsa encrypted database store, configuration store, key vault, OS certificate store.
- **Secret Reference**: The serializable value stored in workflow definitions or module settings. It identifies the logical secret by immutable technical name and optionally constrains type or required scope; runtime resolution uses the latest active version.
- **Secret Picker Context**: Metadata supplied by a consuming UI so Studio can filter compatible secrets and inline creation options.
- **Secret Export Package Item**: A safe representation of a secret in an export package, containing references only or values encrypted for an explicit target.
- **Audit Event**: A security record describing privileged secret operations without containing the secret value.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An authorized user can create a text secret in Studio, select it from a sensitive workflow input, run the workflow, and verify the workflow definition contains no raw secret value.
- **SC-002**: Metadata list and detail API responses contain zero raw secret values in automated contract tests.
- **SC-003**: A secret rotation produces exactly one active latest version and preserves at least one retired previous version in tests.
- **SC-004**: A user can manage metadata and replace values without any API or Studio path exposing the current cleartext value.
- **SC-005**: Export tests can scan package content and verify raw secret values are absent unless encrypted export was explicitly requested.
- **SC-006**: Import tests can move an encrypted secret export between two environments without shared Data Protection keys.
- **SC-007**: At least three secret types and the two v1 store implementations can be registered and exercised through the same Studio and runtime resolution surfaces.
- **SC-008**: Existing `elsa-extensions` secret records can be migrated or adapted with no loss of name, latest active value, status, expiration, or description in migration tests.
- **SC-009**: All privileged operations produce audit events without raw values.
- **SC-010**: Runtime resolution of unavailable, revoked, expired, or unauthorized secrets fails deterministically with non-secret error messages.

## Assumptions

- The revamped module will live in Elsa Core or be promoted from `elsa-extensions` into the main Elsa module structure.
- Elsa Studio implementation happens in the `elsa-studio` repository, while server contracts live with the Core module.
- Existing `InputAttribute.CanContainSecrets` metadata is the primary signal for replacing plain editors with a secret-aware editor.
- Data Protection remains acceptable for encrypting values that stay in one deployment environment, but import/export must use explicit portable cryptographic material.
- Database persistence remains optional; in-memory storage is acceptable for tests and development only.
- Secret values are not shown in Studio or API after creation, even to administrators.
- Hashing-only identity credentials remain separate from retrievable secrets unless a future migration explicitly changes that boundary.
- Provider-specific integrations such as Azure Key Vault and operating-system certificate stores are optional packages after the base abstraction is stable.
