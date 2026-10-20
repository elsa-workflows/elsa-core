# Deliver the React Studio preview as modular NuGet packages

- Status: Proposed
- Date: 2026-10-20
- Related: [#8859](https://github.com/elsa-workflows/elsa-core/issues/8859), [#8839](https://github.com/elsa-workflows/elsa-core/issues/8839), [#8841](https://github.com/elsa-workflows/elsa-core/issues/8841)

## Context

The React Studio preview needs third-party UI modules that deploy with server
features. A single compiled-in frontend cannot support that scenario. The preview
must coexist with the Blazor Studio without changing its default or its package IDs.

The release target is 3.11.0, as recorded by the current epic. Older 3.10 references
in child issue titles and the original decision predate the release-train change.

## Proposed decision

Import the React Studio, with history and attribution preserved, under `studio/react/`
after the license and employer-clearance requirements of #8842 are met. Use a pnpm
workspace for the Vite SPA, host, SDK, UI kit, API client, authentication and modules.

Use distinct NuGet IDs `Elsa.ReactStudio.*` and npm IDs
`@elsa-workflows/react-studio-*`, including `-sdk`, `-ui`, `-api-client` and `-auth`.
UI modules are Razor class libraries whose built ESM entry and CSS travel inside
their NuGet packages. CShells feature activation contributes their manifests.
The host serves an import map with one React/React DOM runtime and shared SDK
contexts, and imports compatible module entries. Check both the host and SDK major;
show a diagnostic for a rejected module while keeping the shell usable. Asset URLs
include their package version. The public SDK must define deterministic slot ordering
and documented Preview contracts before third-party production use.

Ship plain 3.11.0 packages, labelled **Preview** in metadata, documentation, release
notes and UI. Delivery is an off-by-default `ReactStudioPreview` feature mounted at
`/studio-preview` in Elsa.Server.Web, plus a standalone host and Docker image. The
Blazor Studio remains unchanged and the default through 3.x. This decision authorizes
no package publication or source-repository archival; those keep their review gates.

## Spike outcome

**Go for implementing the module architecture**, within the proof's stated limits.
The independently authored [#8839 sample](../../studio/samples/ReactStudio.Modularity/README.md)
builds and packs three local NuGet libraries before building a consumer host with
no module project references. Its Playwright tests check:

- Labels and third-party Weather execute from the packed assemblies' embedded JS/CSS.
- They share React hooks, query-client state and theme context across route navigation.
- Disabled CShells features contribute no manifest entries or production asset responses.
- Host/SDK incompatibility is visible, and compatible module routes remain usable.
- An enabled module that throws while rendering shows its own diagnostic; navigation
  and other modules remain usable.
- Module asset URLs include the version; deep links, History API navigation and UI-hint slots work.
- A nonce CSP permits the import map; Vite assets and WebSocket updates pass through
  the ASP.NET host, and a hot update preserves query state.

The sample intentionally remains outside product packing and `Elsa.sln`, consumes
only official dependencies and locally packed sample modules, and publishes nothing.
It proves ESM loading from embedded resources. Production Razor static-web-asset
integration, authenticated APIs, configurable mounts, tenant isolation, package reload,
CSS isolation, all SDK v1 slots and release provenance remain subsequent work, not
claims established by this spike. No donor code is imported by the sample.

## Consequences

The Vite port and public SDK can proceed against an exercised loading model. Shared
dependencies must stay external in module builds and be pinned by the host, so
independent module bundles do not create competing React or SDK contexts.

The import cannot merge before license/employer clearance. Its receipt must verify
the exact donor tree and ancestry, and preserve post-snapshot commits. Finalizing
this ADR with the import PR must retain the distinction between this bounded proof
and production package/host verification.
