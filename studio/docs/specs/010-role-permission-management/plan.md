# Implementation plan — Elsa Studio Role Permission Management

## Repository responsibility

- `elsa-studio`: canonical program artifacts, module activation, internal API clients and DTOs, permission context, navigation, role UI, deletion UI, component tests, and browser validation.
- `elsa-core`: authoritative role, catalog, reach, authorization, deletion-impact, and remediation behavior. During exact-host validation, additive selective-remediation work moved to elsa-core#8028/PR #8029, in-memory role tenant isolation to #8012, and an unrelated .NET 10 compile unblocker to #8030/PR #8031. These remain separately reviewed Core changes rather than hidden Studio scope.

## Architecture

- Preserve the existing `Elsa.Studio.Security` module and `AddSecurityModule()` entry point.
- Register the module in Server and WebAssembly hosts and reference it from the Studio bundle.
- Use internal Refit-style clients resolved through the existing backend client provider.
- Use remote shell-feature discovery and `/identity/me/permissions` for fail-closed rendering.
- Keep server authorization authoritative for every mutation.
- Use one page container per route and focused presentational components for list, permission catalog, advanced-grant reach, repair, and deletion states.

## Approved mockups

- [Responsive role list](mockups/roles-list-responsive.png)
- [Exact permissions tab](mockups/role-editor-exact-permissions.png)
- [Advanced grants and per-grant reach](mockups/role-editor-advanced-grants.png)
- [Safe and configuration-blocked deletion](mockups/role-deletion-safe-and-blocked.png)
- [Editable remediation and conflict outcomes](mockups/role-deletion-remediation.png)

## Pull request sequence

1. PRD, decision log, approved mockup references, validation plan, and executable backlog.
2. Security module activation, API contracts, permission context, navigation gating, and contract tests.
3. Approved role list/editor, permission catalog, advanced grants, repair UI, and component tests.
4. Shared deletion/remediation modal and focused tests.
5. Exact-head integration fixes and recorded Server/WebAssembly end-to-end evidence.

Core prerequisites discovered on that path land independently before the final host matrix is accepted.

## Critical path

1. Verify live Core contracts and Studio extension seams.
2. Land module activation and internal clients.
3. Land browse/create/edit vertical journey.
4. Land deletion/remediation.
5. Run real-host validation and resolve exact-head findings.

## Compatibility and failure boundaries

- Relative client routes begin with `/identity/...`; host configuration owns the backend prefix.
- The complete experience requires a current-main-compatible Core backend; the current 3.8 release contract does not expose catalog, reach, or caller-permission endpoints.
- Missing feature discovery or effective permission data fails closed.
- Ordinary updates remain last-write-wins.
- Stale deletion impact returns to review; it is never retried automatically.
- A discovery that requires Core changes becomes a separate issue or spike.
- This program is separate from the organization Project **Scoped role-based access**, whose future Foundation contract deliberately differs on wildcards and role concurrency.
