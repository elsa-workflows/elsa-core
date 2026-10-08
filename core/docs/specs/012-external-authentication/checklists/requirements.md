# Specification Quality Checklist: External Authentication

**Purpose**: Validate specification completeness and quality before proceeding to planning

**Created**: 2026-07-24

**Revalidated**: 2026-07-24 approved revision

**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details dominate the product requirements
- [x] Focused on user value and business needs
- [x] Written for product and technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No `[NEEDS CLARIFICATION]` markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria describe observable outcomes
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] Core/server and paired Studio delivery boundaries are explicit

## Approved Revision Consistency

- [x] Settings is defined as UI composition/navigation only
- [x] Connections are host-wide in the connected environment without a Deployment Target entity/field
- [x] Record ID versus durable Connection Key responsibilities and full-shadow lifecycle are unambiguous
- [x] OIDC `discoveryUrl`, gated Advanced trust overrides, immutable deployment-derived callback/confidential client/S256 PKCE/validation, and basic/post authentication are explicit
- [x] Managed and External Secret ownership is explicit
- [x] Archive/restore, Test, and Preview behavior is explicit
- [x] Preferred method never causes automatic redirect
- [x] Per-connection policy, single External User Matcher, ephemeral claims, fallback behavior, and static create-user `defaultRoleIds` are explicit
- [x] Role deletion is guarded by all database/configuration JIT-policy references, with immutable configuration diagnostics and atomic-or-safe-best-effort editable remediation
- [x] Authentication.UI shell, Settings Connections, and separate Security Links/Sessions ownership are explicit
- [x] Direct OIDC compatibility and staged deprecation are explicit
- [x] Minimal upstream token retention and Elsa-initiated login/logout are explicit
- [x] Claim-permission mapping UI is explicitly outside v1
- [x] Open implementation delta is represented by unchecked T117–T135

## Implementation Readiness

- [ ] T117–T132 implementation and migration work complete
- [ ] T133–T135 verification gates pass

## Notes

- The PRD interview resolved the product-level questions, so no clarification markers remain.
- Protocol and security terms such as OpenID Connect and PKCE are behavioral constraints, not implementation prescriptions.
- Exact routes, schemas, storage models, package names, and framework integration belong in the implementation plan.
- The OIDC validation contract, host-wide discovery shape, WebAssembly session behavior, normalized claim/role projection, and shared latest-test observation are explicit and testable.
- Checked historical tasks T001–T116 do not override the open approved-revision tasks.
