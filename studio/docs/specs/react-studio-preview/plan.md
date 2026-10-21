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
without publishing. Local image build and non-root container sign-in, definitions navigation and reload
pass after configuring the build network’s trusted CA bundle. The subsequent SDK,
module and hosting work is recorded below. Parity work, publication and final source
cutover remain open. See the
[port verification checklist](../../../react/docs/vite-port-verification.md).

## SDK extraction

Four unpublished pnpm packages now own the Preview module contracts, shared UI,
API client and authentication. The host and imported feature sources consume their
public exports. Registration tests cover stable ordering, incompatible major
versions, rollback, replacement, pending activation and cleanup; an external sample
compiles and renders against four packed artifacts without host-source aliases.
The generated TypeDoc reference is retained by frontend CI. The imported modules
use a declarative adapter to register their slots; new designer/editor slot renderers
remain part of subsequent Preview parity work.

The next split preparation moves the common components, configuration/navigation,
API/query context and designer model/store into the public packages. Designer and
ActivityEditors provide typed component slots, removing the source import cycle.
A boundary guard checks every feature file for host aliases and sibling source
imports. Unit, packed-consumer and host-browser checks pass, and real workflow
creation, designer render, saved reload and cleanup pass against the reference
backend.

## Production feature packages and hosting

Sixteen RCL features now own their client sources and separate ESM/CSS assets. The
host bootstrap uses the enabled server manifest and shared import map; there is no
compiled-in frontend module list. The 20 production projects join the root solution
and `Elsa.ReactStudio.slnf`; the four independent spike projects retain their explicit
sample disposition. The dependency census passes with 97 Studio projects, 197 across
Extensions/Studio, without changing the updater selectors.

The production host checks incompatible majors before import, rolls back failed
registration and isolates component render failures. The shared UI and first-party
utility classes compile into one stylesheet to preserve responsive rule ordering.
NUKE builds the frontend before solution compile/test/pack. CI packs all 18 libraries,
and verifies supported frameworks, embedded entry/CSS, consistent package/assembly
versions and source notices. Package license metadata retains the donor's observed
status instead of inheriting an unsupported MIT grant.

Core hosting is opt-in, with `/studio-preview` beside `/elsa/api` and a same-origin
server default. Preview resolution preserves existing host endpoints and their DI
scopes. CShells 0.0.31 supplies repeat-safe host shutdown; 0.0.28 could drain an already
disposed provider during framework test teardown. The independent spike remains on
its receipted dependency version. Full parity, third-party production template,
container/release publication and source cutover remain separate acceptance gates.

## Specialized activity editors

ActivityEditors now registers the four missing UI hints through the public slot.
Date/time controls convert local time to the stored instant; dynamic outcomes use
Object JSON and include fixed designer ports; HTTP code cases preserve embedded
activities while wrapped integer-array inputs retain their numeric format; input
selection preserves the complete definition. Unit fixtures cover each editor and
extension registration. A browser fixture loads the actual ESM entry and exercises
editing, local-time conversion, read-only behavior and an extension email hint.
The current frontend suite has 110 unit cases and seven browser cases.


## Activity settings and retries

ActivityEditors now includes commit strategy, log persistence, resilience, task and
workflow-activity version tabs. WorkflowInstances includes paged retry history per
activity execution, with details and permission checks. The [Blazor parity checklist](../../../react/docs/activity-settings.md)
records the wire contracts, visibility and verification boundaries. Browser fixtures
exercise the actual production modules; a local reference-backend check verifies
save/reload of each setting and a published reusable activity's paired version IDs.
This does not close the broader partial-parity or release acceptance gates.


## Partial parity verification

The [evidence checklist](../../../react/docs/parity.md) covers every #8851 item.
Workflow metadata/settings, variable defaults, outcomes, versions and keyboard
history/clipboard now have mounted editor browser checks, with deeper designer-store
checks for IDs, nested clones and edge preservation. Confirmed gaps are tracked in
#8940 (workflow log persistence), #8941 (variable test values), #8942 (Applications/API
keys), #8943 (HTTP ports/embedded branches) and #8944 (batch paste names). The retained
sanitized observations identify the audited source head and real API boundaries.

## Third-party module authoring

The sample generator creates a standalone RCL and host using package references
only. Its widget and activity tab use the shared public UI/auth/API contexts,
scoped CSS, immutable custom-property updates and read-only editor context.
The [authoring guide](../../../react/docs/module-authoring.md) covers generation,
slot ordering, compatibility majors, feature enablement, packaging and cache-safe
versioning. Template source files carry a `.template` suffix and are project data,
outside the root solution and release inventory.

An integration check generates outside the repository, installs four packed npm
packages, builds all three RCL frameworks, packs it, restores a host from local
NuGet feeds and exercises both contributions in Chromium. It verifies saved data,
read-only behavior, immutable asset caching and feature removal. CI runs the check
after packing the Preview libraries. This is local package-consumption evidence;
registry publication and the final Preview release remain separate work.

## Release packaging preparation

The root package workflow now installs Node 22 for NUKE's asset build. The existing
3.10 release base remains in place; evaluated MSBuild policy excludes React
packages below 3.11.0 and includes 3.11 previews/plain releases. Package and assembly
versions must match. All 18 plain 3.11.0 packages pass the artifact verifier with
Preview descriptions/tags, notices, all three frameworks and embedded assets;
an actual 3.10.0 pack produces no React packages. Existing publication-selection
and workflow-gate checks still pass. The try guide and release preparation notes
are under Studio docs. This prepares packages without publishing them.

## BPMN interchange

Workflow definitions now imports BPMN through the existing server interchange API,
shows Info/Degraded/Dropped findings and requires process selection for multi-process
documents. Structured capability and expression-condition refusals retain server
messages and offending IDs. Row actions export after a binding/expression warning,
with absent/unknown-version/stale-source refusals. Browser tests cover the flow,
cancellation and download/reimport. A disposable real Core check verifies process
and flow preservation, stable canonical XML after reimport, structured condition
refusal and cleanup. Server export can normalize XML and add missing layout, so
original byte equality is not asserted. Editable BPMN canvas work remains #8929.

BPMN review also verifies the native `Elsa.BpmnProcess` route. It opens a dedicated
read-only view outside the generic designer, preventing normalization and accidental
autosave before #8929 provides a native canvas. An actual Core-imported process
retains its exact root after opening and advancing beyond the autosave interval,
and still exports. Browser cases abort pending analysis/export on cancellation
and reject late navigation/download effects.

## Expression completion

C# now has a syntax mode. Expression and script inputs combine current workflow
members with cached server declaration metadata, providing typed members and
runtime-correct input/output accessors for C#, JavaScript and Python. Liquid's
existing suggestions remain available. Requests are shared across edits, cancelable
and invalidated after save; server failures preserve local suggestions.

Unit cases, three browser cases and an actual Core declaration document verify the
behavior. The reproducible benchmark covers 3,000 variables, 3,000 inputs and 10,000
activities. With localized descriptions, the measured completion-generation medians
are 95.8 ms for C#, 79.6 ms for JavaScript and 63.8 ms for Python on the development
environment. The expression guide states the one-level member scope and distinction
from language-server diagnostics and full-diagram performance.

Review regressions preserve the runtime's exact input-wrapper case, keep completion
replacement inside surrounding calls/arrays, exclude saved wrappers that no longer
match current workflow members, and parse generic CLR declaration names. The guide
documents the conservative treatment of unknown cached get/set wrappers.

## Tenant context

Each server now selects fixed, authenticated-claim or configured-choice tenancy.
Tenant selection reloads the application to clear first- and third-party module
state and pending browser work; query scopes include server, subject and tenant.
Unsaved workflows can cancel before a preference changes, with one discard prompt.
The empty default-tenant ID is distinct from an omitted header. Choices persist
per server/subject and claims mode rejects missing/ambiguous claims before module
API requests. The tenancy guide records the reference Core host's account-bound
resolver and the separate backend authorization needed for selection mode.

Review regressions coordinate a single pending context reload, preserve queries
during React StrictMode's effect replay while disposing true unmounts, and retain
the last authenticated scope across an intermediate missing-claim error. The
hosted A/missing/B case verifies that module-level dialog state cannot revive in
another tenant. Dirty server switches retain one discard prompt.

## Localization

The shared SDK now supplies i18next-backed hooks and English/Dutch catalogs for
1,201 built-in messages. Nonempty matching Blazor Dutch resources were reused;
React-specific and blank source entries were translated. A catalog check rejects
empty/missing messages, mismatched interpolation and raw JSX display text.

The header language preference is keyed by API URL and subject. Culture changes
update shell/module labels, tables, controls, diagnostics and completion descriptions
without remounting editors or rewriting workflow names/expressions. The host updates
document language/direction and culture-aware formatting. Modules contribute owned,
transactional resource slots and use their namespace with English fallback. The
authoring template ships both languages and verifies a state-preserving switch.

Hosted browser cases cover account preferences, dialogs, unsaved workflow state,
independent module resources, namespace isolation and fallback. A real development
React StrictMode unit case verifies query-cache retention through effect replay
and disposal on true unmount; production browser bundles do not replay effects.
