# Research: Secrets Module

## Decision: Redesign the existing extension module before promotion

**Rationale**: The `elsa-extensions` secrets module already has useful pieces: simple runtime resolution, management services, versioning, expiration, EF Core persistence, API endpoints, Studio pages, and JavaScript helpers. It also exposes clear gaps for the requested Orchard-style model: per-secret store choice, extensible types/editors, picker contracts, import/export, complete shell features, and safer API boundaries. Promoting a redesigned module preserves compatibility while avoiding a leaky first-party API.

**Alternatives considered**:

- Copy the extension module as-is: rejected because it keeps Data Protection portability limits, cleartext edit endpoints, and module-level store selection.
- Start from scratch: rejected because the extension already proves useful lifecycle and persistence concepts.

## Decision: Use immutable technical names as serialized references

**Rationale**: The user clarified that technical names cannot be changed; a user creates a new secret when a different technical name is needed. This makes workflow definitions and module settings readable, export-friendly, and stable without needing rename semantics.

**Alternatives considered**:

- Store database IDs: rejected because exports become less readable and cross-environment mapping becomes harder.
- Store mutable names: rejected because renames would break existing consumers.

## Decision: Resolve latest active version only

**Rationale**: Secret rotation should update consumers automatically. Always resolving the latest active version keeps workflow definitions stable and avoids stale credential pinning.

**Alternatives considered**:

- Version pinning: rejected for v1 because it increases UX and lifecycle complexity and can keep consumers on retired credentials.
- Explicit version policy per reference: rejected as unnecessary for current requirements.

## Decision: Do not support cleartext reveal after creation

**Rationale**: A no-reveal model reduces the highest-risk UI/API surface. Operators can replace, rotate, test, use, and encrypted-export secrets without reading the current cleartext value.

**Alternatives considered**:

- Reveal with permission and audit: rejected because it still creates a cleartext disclosure path.
- Reveal only for Elsa-managed stores: rejected because it complicates user expectations and provider capability rules.

## Decision: V1 includes Elsa-managed encrypted and configuration-backed read-only stores

**Rationale**: These two stores cover the most important first-party scenarios: Elsa-owned values and deployment-owned values. They also exercise both writable and read-only store capability paths without adding cloud/provider dependencies.

**Alternatives considered**:

- Include cloud vault and OS certificate stores in v1: rejected because provider-specific behavior should follow after the stable contract lands.
- Define abstractions only: rejected because the module needs a complete usable default.

## Decision: Keep Data Protection for local-at-rest protection, not portable exports

**Rationale**: Data Protection is suitable for values staying in one deployment environment. The transcript's import/export case requires portable encryption material, so exports use an explicit key/certificate/asymmetric target rather than shared application Data Protection keys.

**Alternatives considered**:

- Use Data Protection for export: rejected because isolated environments often do not share keys.
- Store raw values in export with transport security only: rejected because packages are often stored and transferred outside the original channel.

## Decision: Treat import name conflicts as explicit operator choices

**Rationale**: Same-technical-name conflicts can either overwrite an operational secret or leave workflows pointing to stale credentials. Failing by default until the import request chooses create-new, update/rotate, or skip prevents hidden behavior.

**Alternatives considered**:

- Skip by default: rejected because imports can appear successful while leaving missing values.
- Update by default: rejected because imports can unexpectedly rotate production secrets.

## Decision: Define Studio contracts here and implement UI in `elsa-studio`

**Rationale**: Server/Core contracts must shape picker, metadata, permission, and inline-create behavior. The concrete UX belongs in the Studio repository, so this feature adds Studio source code in the paired `elsa-studio` worktree rather than `elsa-core`.

**Alternatives considered**:

- Implement Studio UI in `elsa-core`: rejected because the repository boundary is wrong.
- Exclude Studio entirely: rejected because picker contracts affect server API shape.
