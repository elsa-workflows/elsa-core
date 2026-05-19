# Data Model: Secrets Module

## Secret

Logical named secret referenced by workflows and modules.

- `TechnicalName`: immutable unique name used by references and exports.
- `DisplayName`: optional user-facing label.
- `Description`: optional safe metadata.
- `Type`: secret type identifier.
- `Scope`: optional classification used by pickers and consumers.
- `Tags`: optional safe labels.
- `StoreName`: selected store identifier.
- `Status`: active, retired, expired, revoked, or deleted.
- `TenantId`, `Owner`, `CreatedAt`, `UpdatedAt`: operational metadata.
- `LatestVersionNumber`: current active version number.

Validation:

- `TechnicalName` is required, trimmed, case-normalized for uniqueness, and immutable after creation.
- Metadata must not contain raw secret values.
- Only one latest active version may exist for a technical name.

## SecretVersion

Versioned value or external-reference payload for a secret.

- `SecretTechnicalName`: parent technical name.
- `Version`: monotonically increasing version number.
- `Status`: active, retired, expired, or revoked.
- `ProtectedPayload`: provider-private encrypted value or lookup metadata.
- `PayloadContentType`: store/type-specific payload descriptor.
- `ExpiresAt`: optional expiration time.
- `CreatedAt`, `UpdatedAt`, `RetiredAt`, `RevokedAt`: lifecycle timestamps.

State transitions:

- Create: no version -> version 1 active latest.
- Rotate/update: current active latest -> retired, new version -> active latest.
- Expire: active -> expired.
- Revoke: active or retired -> revoked.
- Delete: logical secret becomes unavailable; provider decides whether versions are hard-deleted or tombstoned.

## SecretReference

Serializable value stored in workflow definitions and module settings.

- `TechnicalName`: immutable secret technical name.
- `RequiredType`: optional expected secret type.
- `RequiredScope`: optional expected scope.

Resolution:

- Resolve by `TechnicalName`.
- Return the latest active version only.
- Fail for missing, expired, revoked, deleted, incompatible type, incompatible scope, or unavailable store.

## SecretTypeDescriptor

Registered metadata for a secret type.

- `Type`: stable type name.
- `DisplayName`: safe user-facing label.
- `Description`: safe help text.
- `SupportedStoreCapabilities`: required read/write/list/export/test capabilities.
- `EditorContract`: Studio editor descriptor and validation metadata.
- `CanGenerate`: whether the type can generate payloads server-side.
- `CanExportEncrypted`: whether encrypted export is supported.

Built-in v1 types:

- `Text`: standard string value.
- `RsaKey`: generated or provided RSA key material.
- `X509CertificateReference`: certificate reference metadata, not private certificate material.

## SecretStoreDescriptor

Registered metadata for a store implementation.

- `Name`: stable store identifier.
- `DisplayName`: safe user-facing label.
- `Description`: safe help text.
- `Capabilities`: read, write, list, delete, rotate, test, encrypted export.
- `SupportedTypes`: secret types the store can handle.
- `IsDefault`: whether new secrets use this store when no explicit store is selected.

V1 stores:

- `ElsaEncrypted`: writable Elsa-managed encrypted store.
- `Configuration`: read-only deployment configuration store.

## SecretPayload

Store-owned payload attached to a version.

- `StoreName`: store that owns the payload.
- `Type`: secret type.
- `ProtectedData`: encrypted value or provider-private lookup metadata.
- `Metadata`: safe store/type metadata for diagnostics and pickers.

Rules:

- General metadata APIs never return `ProtectedData`.
- Provider-private metadata is not exposed outside store-specific boundaries.
- Cleartext is not persisted in workflow definitions, module settings, logs, audit events, or general API responses.

## SecretImportItem

Safe import/export package representation.

- `TechnicalName`: target secret name.
- `Type`, `Scope`, `Description`: safe metadata.
- `StoreName`: requested target store.
- `ReferenceOnly`: whether no value is included.
- `EncryptedPayload`: optional payload encrypted for the import target.
- `ConflictBehavior`: create-new, update/rotate, skip, or unspecified.

Rules:

- Same-name conflicts fail when `ConflictBehavior` is unspecified.
- Encrypted payload import requires matching decryption material.
- Reference-only import creates or validates metadata without importing a value.

## SecretAuditEvent

Security event emitted for privileged operations.

- `Action`: create, update, rotate, revoke, delete, test, runtime-use, encrypted-export, import, failed-privileged-operation.
- `TechnicalName`: affected secret.
- `Actor`: user or system identity.
- `Timestamp`: event time.
- `Reason`: optional user-provided reason.
- `Result`: success or failure.

Rules:

- Audit events never include raw secret values, encrypted payloads, or provider-private metadata.
