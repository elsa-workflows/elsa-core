# React Studio preview delivery

Delivery goal: complete [#8859](https://github.com/elsa-workflows/elsa-core/issues/8859)
and its twenty children. The React Studio ships with **3.11.0**, labelled **Preview**,
as an opt-in server feature and a standalone Docker image. The Blazor Studio remains
the default through 3.x. The epic's current milestone supersedes older child titles
that still say 3.10.

This is a working delivery record, not a completion or publication receipt. Keep it
while #8859 is in progress; move durable decisions to the ADR and remove this plan
when the epic closes. Do not copy private strategy documents into the public repository.

## Acceptance gates

- Every child is closed with its implementation and verification evidence.
- The preview runs from packed NuGet packages in Elsa.Server.Web and from its Docker image.
- A third-party module loads from its own package without a host project reference.
- Package metadata, UI and release notes say Preview; the Blazor default is preserved.
- Release publishing follows the repository's independent review and release gates.

## Work breakdown

| Issue | Delivery | Dependency | Evidence required |
| --- | --- | --- | --- |
| [#8839](https://github.com/elsa-workflows/elsa-core/issues/8839) | Modularity spike | None | Packed modules, one React runtime, feature/version gates, versioned assets, nonce CSP, proxied HMR and Playwright; ADR outcome |
| [#8842](https://github.com/elsa-workflows/elsa-core/issues/8842) | License and dependency compatibility | Maintainer/contributor | Record actual donor license status; compatible dependency scan; employer-clearance requirement removed by task owner |
| [#8840](https://github.com/elsa-workflows/elsa-core/issues/8840) | History-preserving import | Spike | Unchanged donor tree at `studio/react/`, original commit ancestry, import receipt/verifier/tests/workflow, source-tip preservation and reviewed cutover |
| [#8841](https://github.com/elsa-workflows/elsa-core/issues/8841) | Architecture decision | Spike; lands with import | Dated ADR, package IDs, 3.11 Preview policy, opt-in delivery, Blazor default and generated TOC |
| [#8843](https://github.com/elsa-workflows/elsa-core/issues/8843) | Next.js to Vite SPA | Spike and import | No Next dependency/imports; deep links; host-resolved runtime configuration; Vitest and feature checklist |
| [#8844](https://github.com/elsa-workflows/elsa-core/issues/8844) | SDK v1, UI kit, API and auth packages | Spike | Public typed slots, deterministic ordering, compatibility diagnostics, generated Preview API documentation |
| [#8845](https://github.com/elsa-workflows/elsa-core/issues/8845) | Module packages | Port and SDK | Razor class libraries with ESM/CSS, public SDK-only dependencies, feature gating, solution/filter membership |
| [#8846](https://github.com/elsa-workflows/elsa-core/issues/8846) | Opt-in server and standalone hosts | Modules | Off by default; configurable mount path; same/cross-origin authentication; enablement documentation |
| [#8847](https://github.com/elsa-workflows/elsa-core/issues/8847) | Workspace CI | Import | Frozen install, lint/types/tests, Vite and .NET builds/pack, asset checks, browser smoke, license scan, Dependabot |
| [#8848](https://github.com/elsa-workflows/elsa-core/issues/8848) | 3.11 packaging and Preview labeling | Modules and CI | JS-before-pack integration, metadata/UI labels, release notes and try-it guide |
| [#8849](https://github.com/elsa-workflows/elsa-core/issues/8849) | Missing UI hints | Modules | Date/time, dynamic outcomes, status codes and input-picker slot editors with behavioral tests |
| [#8850](https://github.com/elsa-workflows/elsa-core/issues/8850) | Activity tabs and retries | Modules | Server round trips for commit/log/resilience/task/version settings; instance retry attempts |
| [#8851](https://github.com/elsa-workflows/elsa-core/issues/8851) | Partial-parity verification | Modules | Evidence for settings, variables, outcomes, versions, API keys, undo/redo, clipboard and ports; linked gap issues |
| [#8852](https://github.com/elsa-workflows/elsa-core/issues/8852) | Third-party authoring guide/template | SDK and modules | Generated sample builds, packs and loads; slots, versioning, CSS and packaging instructions |
| [#8853](https://github.com/elsa-workflows/elsa-core/issues/8853) | BPMN import/export | Modules | Findings/refusal UI and tested round trips against server contracts |
| [#8854](https://github.com/elsa-workflows/elsa-core/issues/8854) | C# expressions and completion | Modules | C# syntax support; server-driven variables/inputs/outputs in supported languages; large-workflow performance |
| [#8855](https://github.com/elsa-workflows/elsa-core/issues/8855) | Localization | Modules | Externalized UI strings, persistent culture picker, package-contributed resources, English and an existing Blazor language |
| [#8856](https://github.com/elsa-workflows/elsa-core/issues/8856) | Tenant switcher | Modules | Claims/selection behavior and invalidation of every module's tenant-specific state; tests |
| [#8857](https://github.com/elsa-workflows/elsa-core/issues/8857) | Docker preview | Hosts | CI-built standalone image, STUDIO configuration, paired compose example and Preview labeling |
| [#8858](https://github.com/elsa-workflows/elsa-core/issues/8858) | npm publishing | SDK and CI | Release-owned provenance workflow, preview tags before 3.11.0 and template install/type-check proof |

## Current evidence and blockers

The independent [modularity sample](../../../samples/ReactStudio.Modularity/README.md)
builds its own assets and packs Runtime, Labels and third-party Weather before
restoring a host with package references only. Its browser proof exercises the
spike's acceptance behaviors; the [proposed ADR](../../../../docs/adr/2026-10-20-modular-react-studio-preview.md)
records the outcome and limits. This does not import donor code, implement SDK v1,
or close the epic.

The task owner removed the employer-clearance requirement on 2026-10-20 and
explicitly authorized the import and adaptation. The donor contains no license file;
the import receipt records this without inventing a donor MIT grant or changing
copyright attribution. The original donor tree and all 14 commits are retained in
the import merge. The remote main tip matched the planned 2026-10-19 09:00
Europe/Amsterdam snapshot; no post-snapshot commits existed at import time.
Source archival and the redirect follow independent import review and merge.

GitHub API access was restored on 2026-10-20. The epic and spike are assigned to
the connected account, `sfmskywalker`, and the initial implementation is under review
in [#8923](https://github.com/elsa-workflows/elsa-core/pull/8923). Required Greptile
and automatic CodeRabbit reviews were requested/started. The source import is recorded in the [receipt](../../../../docs/integration-program/studio-react-source-import.md);
the epic and child issues are not closed by this record.

Feedz and Google Fonts access also recovered. The unchanged reference server builds,
its workflow-core suite passes 363 tests, both health checks pass, and its sample HTTP
workflow executes. The unchanged donor production build and browser sign-in/dashboard
check pass. These establish a development baseline; they do not establish the Vite
port, production module package integration, full feature parity or a preview release.

## Imported application adaptation

The receipted history-preserving import is under review in
[#8924](https://github.com/elsa-workflows/elsa-core/pull/8924). The subsequent #8843
port replaces Next.js with Vite and an ASP.NET host while retaining the 74 existing
unit cases. Host configuration and HTTP behavior have 19 passing .NET cases; four
browser cases cover mounted deep links, History navigation, assets and OIDC callback
URLs. The real reference backend also passes sign-in, dashboard and list reload.
The host builds on net8/net9/net10. The build scans 113 production dependencies and
retains their notices; the full npm audit reports zero known vulnerabilities.

The source-validation workflow also builds and starts the standalone Preview image
without publishing. Local image validation is pending access to the Microsoft base
image CDN. Production module extraction, SDK v1, opt-in CShells/server integration,
parity work, publication and final source cutover remain open. See the
[port verification checklist](../../../react/docs/vite-port-verification.md).
