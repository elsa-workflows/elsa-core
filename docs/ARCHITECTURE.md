# Elsa Studio (Next.js) – architecture and module guide

> Part of the [documentation set](../README.md#documentation): [Getting started](getting-started.md) ·
> [Configuration](configuration.md) · [Modules](modules.md) · [Designer](designer.md) ·
> [Deployment](deployment.md) · [Troubleshooting](troubleshooting.md)

A standalone React front end for Elsa Workflows 3.x. Built with Next.js (App Router), React 19,
TypeScript, Tailwind v4, shadcn/ui (Base UI primitives) and React Flow for the diagrams. The app
is a client-rendered SPA that talks straight to the Elsa REST API; Next.js only serves the shell
and resolves the runtime configuration per request.

```
src/
  app/                      Next.js routes (thin): layout loads config; [[...slug]] is a catch-all
  config/                   runtime configuration: zod schema, server-side loader, React provider
  lib/
    api/                    HTTP client, typed endpoint groups, TS models, TanStack Query provider
    auth/                   AuthStrategy contract + none / api-key / elsa-identity / oidc / elsa-external
    environments/           active backend selection (multi-server)
    features/               server feature catalog (GET /features/installed)
    modules/                StudioModule contract, registry, route matcher
    permissions/            resource:verb permission set (mirrors the server; unknown = allow)
    utils/                  download helpers, URL state
  components/
    ui/                     shadcn components (generated, do not hand-edit)
    common/                 shared building blocks (DataTable, PageHeader, SearchInput, SimpleSelect,
                            ConfirmProvider/useConfirm, StatusBadge, Timestamp, CodeEditor, JsonViewer…)
    shell/                  AppShell, sidebar, header, login, auth callback, status pages
  modules/
    index.ts                the list of compiled-in modules (order = route resolution order)
    dashboard/              core
    workflow-definitions/   core (list + editor)
    workflow-instances/     core (list + viewer)
    designer/               core shared: React Flow canvas, store, toolbox, port rules
    activity-editors/       core shared: UI-hint editors, expression editor, properties panel
    <optional>/             labels, secrets, security, environments, alterations, user-tasks, settings,
                            diagnostics…
```

## Runtime configuration

`studio.config.json` (or `STUDIO_CONFIG` JSON env, or `STUDIO_*` env overrides) is read on the server
for every request, validated with `src/config/schema.ts`, and provided to the client with
`useStudioConfig()`. Nothing in it is secret: it ends up in the browser.

```jsonc
{
  "title": "Elsa Studio",
  "servers": [{ "name": "Local", "url": "https://localhost:5001/elsa/api", "tenantId": null }],
  "auth": { "provider": "elsa-identity" },           // none | api-key | elsa-identity | oidc | elsa-external
  "features": { "labels": { "enabled": true, "options": {} } },
  "ui": { "defaultTheme": "system", "pollIntervalMs": 5000, "defaultPageSize": 25 }
}
```

`useFeature("labels")` returns `{ enabled, options }`.

## Modules

A module is a plain object created with `defineModule()` (`src/lib/modules/types.ts`):

```ts
export const labelsModule = defineModule({
  id: "labels",                      // feature flag key in config.features
  name: "Labels",
  requiresServerFeatures: ["Elsa.Labels"],   // optional: hidden when the backend lacks it
  routes: [{ path: "/labels", component: LabelsPage, title: "Labels", permissions: [{ resource: "labels", verb: "view" }] }],
  nav: [{ id: "labels", label: "Labels", href: "/labels", icon: Tag, group: NavGroups.administration.id, order: 200 }],
  extensions: { workflowPropertiesTabs: [...], instanceRowActions: [...], dashboardWidgets: [...] },
});
```

Rules:

- Core modules set `core: true` and ignore the flag; everything else is **off unless enabled** in config.
- Routes are matched by the catch-all page through `resolveRoute()`; patterns use `:param` segments.
  Never add files under `src/app/` for a module.
- Each module owns `src/modules/<id>/` with `index.tsx` (the `defineModule` export), `api.ts`
  (endpoint group built from `useElsaClient().http`), `hooks.ts` (TanStack Query hooks, keys prefixed
  with `useQueryScope()`), `pages/` and `components/`.
- Register the module in `src/modules/index.ts`.
- Cross-module UI goes through `extensions` and `useExtensions(kind)`; never import another module's
  pages. Shared code belongs in `src/components/common` or `src/lib`.
- Permissions: use `usePermission(resource, verb)` / `usePermissions()` and the `Resources`/`Verbs`
  constants. The set is *unknown* (allow everything) when the token has no `permissions` claim.
- A module that is enabled in config but needs a server feature the backend lacks (checked against
  `GET /features/installed`) is hidden entirely: no nav item, no routes. Check that endpoint first when
  a module "does not show".

### Settings sections

The `settings` module owns the `/settings` landing page but stores nothing itself (same split as the
Blazor `Elsa.Studio.Settings` module). The "Settings" sidebar item only appears once a module contributes
a section (`appearance` is the built-in example). A module adds a destination with two entries, using
the helpers in `src/lib/modules/settings.ts`:

```ts
const section: SettingsSection = { id: "webhooks", label: "Webhooks", description: "…", href: "/settings/webhooks", icon: Webhook, order: 10, permissions: [...] };
export const webhooksModule = defineModule({
  id: "webhooks",
  routes: [{ path: "/settings/webhooks", component: WebhookSettingsPage }],
  nav: [settingsSectionNav(section)],            // "Settings > Overview, Webhooks" (parents merge by id)
  extensions: { settingsSections: [section] },  // card on the /settings landing page
});
```

`useSettingsSections()` filters by permission, drops duplicate ids and orders by `order`, label, id.

## Data access

`useElsaClient()` returns the typed client; `client.http` is the raw fetch wrapper for endpoints the
core doesn't cover. Query keys always start with `useQueryScope()` (the server URL) so switching
environments never mixes caches. Errors are `ApiError` with a `problem` payload; show them with
`<ErrorAlert error>` or `toast.error(describeError(e))`.

## Designer

Containers are modelled after elsa-core: `Flowchart`/`Sequence` keep children in `activities[]`,
`StateMachine` keeps them in `states[i].entry|exit` and `transitions[i].trigger|action`, and any
other activity holds one child per Embedded port. `model/containers.ts` addresses children with
*slot paths* (`then`, `states.0.entry`, `transitions.2.action`); the store's `path` is a list of
`(activityId, slot)` steps and `DesignerSurface` picks the canvas (flowchart, sequence list, state
machine) for the container at the end of it. State machine selection uses `state:<name>` /
`transition:<index>` ids next to activity ids.

`src/modules/designer` renders a Flowchart with React Flow. The zustand store
(`createDesignerStore(root, readOnly)`) is the single source of truth: nodes/edges are derived from
the current container and every interaction mutates the flowchart JSON (so undo/redo and save are
trivial). Use `DesignerProvider` + `DesignerCanvas`; pass `stats` (per node id) to colour execution
state in read-only mode. Activity port rules live in `model/ports.ts`.

## Activity inputs: syntaxes and encodings

`src/modules/activity-editors/expression.ts` is the only place that knows how a wrapped input is
encoded. Three cases matter, and all three appear in workflows authored by older Elsa versions:

- **Literal / Object / Json** are edited by the UI-hint editor for the input.
- **Variable / Input** bind to a workflow member rather than to text. The stored value is the
  `Variable` or `InputDefinition` object itself (resolved by id and by name respectively), so they are
  edited with `BindingPicker`, never as code. This mirrors the Blazor studio, which swaps in the
  variable / input picker for those syntaxes.
- **Everything else** (JavaScript, Liquid, C#, Python) is edited as text in the code editor.

List-valued inputs are stored by Elsa under the `Object` syntax as a JSON *string*
(`"[\"GET\",\"POST\"]"`), and the server normalises an array back into that string on save. Read
them with `stringListOf`, which accepts both shapes and falls back to the descriptor default only when
the input was never set.

## Call context (workflow hierarchy)

The editor's View menu has a **Call context** toggle (`?context=1` in the URL carries it across
drill-ins). It draws the current workflow inside a grouping box with its transitive callers on the
left and callees on the right, using only existing endpoints. Code lives in
`src/modules/workflow-definitions/call-graph/`:

- `edge-extractor.ts` walks one definition's activity tree (iteratively, with a visited set) and
  returns `(caller -> callee, call type, activity id)` edges for Dispatch/Execute Workflow activities.
  Expression targets yield an edge without a target ("unresolved"); they cannot be matched as callers.
- `call-graph-service.ts` builds and caches the session graph per server: paged summaries
  (`GET workflow-definitions`, published versions), full models in bulk (`GET many-by-id`), one
  `by-definition-id` fallback per referenced-but-unlisted id. Bounded concurrency, in-flight dedupe,
  missing nodes, cancellation, and hard limits (requests, nodes, depth, time) that stop with a warning.
  The editor keeps it current with `setLiveEdges` (unsaved state), `updateDefinition` (save) and
  `revertLive` (discard).
- `subgraph.ts` derives the incoming/outgoing subgraph with iterative BFS, per-side visited sets,
  back edges for cycles, and truncation placeholders. `layout.ts` layers the external nodes by distance.
- `components/call-context/use-call-context.ts` turns that into canvas **decorations**; the designer
  (`decorations` prop, `contextBox` / `contextWorkflow` nodes, `contextCall` edges) renders them as
  view-only elements that are never part of the definition.

Limits and the inspected version are configurable under
`features["workflow-definitions"].options.callContext` (see `call-graph/config.ts`).

## Shell header override

A page can replace the app header and drop the content padding with `useShellHeader(node, { fullBleed })`
from `src/components/shell/shell-chrome.ts` (the workflow editor does this to show a breadcrumb and its
actions in one row). The node is rendered by the shell, outside the page tree, so wrap it in any context
providers it needs.

## Theming

Five palettes (`ELSA_THEMES` in `src/lib/theme/elsa-theme.ts`) are expressed as CSS variable sets in
`src/app/globals.css`, selected by the `data-elsa-theme` attribute on `<html>` (set server-side from
config and client-side from the persisted store). Light/dark comes from next-themes (`.dark` class).
Use the semantic tokens (`bg-card`, `text-muted-foreground`, `bg-status-success`, `text-brand`,
`bg-canvas`) and the helpers `node-card`, `accent-rail` (with `--rail-color`), `eyebrow`,
`diagram-grid` / `diagram-grid-faint` and `port-dot`. Category hues for activities come from
`activityAccentColor()` in the designer module.

## UI conventions

- shadcn here is built on Base UI: use the `render={<Link href="…" />}` prop instead of `asChild`.
- Prefer the shared `SimpleSelect`, `DataTable`, `Field`, `SearchInput`, `useConfirm()` and
  `useUrlState()` over ad-hoc markup so list pages look and behave alike.
- Toasts: `import { toast } from "sonner"`.
- No `any`; keep components small; no business logic in `src/app`.
