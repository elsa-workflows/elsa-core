# Modules

The studio is a shell plus a set of modules. A module contributes routes, navigation, dashboard
widgets and other extension points, and is switched on per deployment.

Two conditions must both hold for an optional module to appear:

1. It is enabled in configuration: `features.<id>.enabled` is `true`.
2. The connected server reports the feature it requires, read from the installed-feature catalogue.

A module that fails the second test is hidden entirely, with no navigation entry and no routes, even
when enabled. This is why a studio pointed at a minimal Elsa server shows fewer entries.

## What ships

### Core modules

Always enabled; the configuration flag is ignored.

| Module | Id | Routes | Purpose |
|---|---|---|---|
| Dashboard | `dashboard` | `/` | Throughput, status breakdown, per-workflow activity. |
| Workflow definitions | `workflow-definitions` | `/workflows/definitions`, `…/:definitionId/edit` | List and the visual editor. |
| Workflow instances | `workflow-instances` | `/workflows/instances`, `…/:id/view` | List and the run viewer. |

### Optional modules

| Module | Id | Requires server feature | Routes |
|---|---|---|---|
| Environments | `environments` | none | Header picker only |
| Labels | `labels` | `Elsa.Labels` | `/labels` |
| Secrets | `secrets` | `Elsa.Secrets` | `/security/secrets`, `…/:name` |
| Security | `security` | `Elsa.Identity` | `/security/users`, `/security/roles` |
| Alterations | `alterations` | `Elsa.Alterations` | `/alterations/plans`, `…/:id` |
| User tasks | `user-tasks` | `Elsa.UserTasks` | `/workflows/user-tasks`, `…/:taskId` |
| Settings | `settings` | none | `/settings` |
| Appearance | `appearance` | none | `/settings/appearance` |
| Console logs | `diagnostics-console-logs` | `Elsa.Diagnostics.ConsoleLogs.ShellFeatures.ConsoleLogs` | `/diagnostics/console` |
| Structured logs | `diagnostics-structured-logs` | `Elsa.Diagnostics.StructuredLogs.ShellFeatures.StructuredLogs` | `/diagnostics/structured-logs` |

## Notes on individual modules

**Secrets** manages credentials that workflows reference by name. Values are write-only: they are
submitted on create or rotate and never read back. The module also contributes a secret picker to
activity inputs, so a workflow refers to a secret rather than embedding one.

**User tasks** is the human-task inbox: claim, assign, release, complete and cancel, with dynamic
forms resolved by the server. Masked fields are hidden until explicitly revealed, including in the raw
payload views.

**Alterations** applies changes to running instances, such as migrating to a newer version, modifying
a variable, scheduling or cancelling an activity. It contributes row actions to the instances list and
the instance viewer, and can bulk-retry faulted instances.

**Settings** owns the `/settings` landing page but stores nothing itself. Other modules contribute
destinations to it; Appearance is the built-in example. The Settings entry only appears once at least
one module contributes a section.

## Adding a module

A module is a plain object created with `defineModule()`. Create `src/modules/<id>/` with this layout:

```
src/modules/<id>/
  index.tsx     the defineModule export
  routes.ts     route patterns, kept separate so components can import them without a cycle
  api.ts        endpoint group built from the shared HTTP client
  hooks.ts      TanStack Query hooks
  pages/        route components
  components/   module-local components
```

```ts
export const labelsModule = defineModule({
  id: "labels",                                   // the key in config.features
  name: "Labels",
  requiresServerFeatures: ["Elsa.Labels"],        // hidden when the backend lacks it
  routes: [{ path: "/labels", component: LabelsPage, title: "Labels", permissions: [{ resource: "labels", verb: "view" }] }],
  nav: [{ id: "labels", label: "Labels", href: "/labels", icon: Tag, group: NavGroups.administration.id, order: 200 }],
  extensions: { workflowPropertiesTabs: [...], instanceRowActions: [...], dashboardWidgets: [...] },
});
```

Then:

1. Register it in [`src/modules/index.ts`](../src/modules/index.ts). Order matters: it decides route
   resolution order.
2. Add `"<id>": { "enabled": true }` to `studio.config.json`.
3. If the module introduces a new permission resource, add it to `Resources` in `src/lib/permissions`.

### Rules

- Never add files under `src/app/` for a module. Routes are matched by the catch-all page through the
  module registry, so enabling or disabling a module is enough to add or remove its routes.
- Cross-module UI goes through `extensions` and `useExtensions(kind)`. Never import another module's
  pages. Shared code belongs in `src/components/common` or `src/lib`.
- Query keys start with `useQueryScope()`, the active server URL, so switching environments never
  mixes caches.
- Build pages from the shared building blocks (`PageHeader`, `DataTable`, `SearchInput`,
  `SimpleSelect`, `useConfirm`, `Timestamp`) so lists look and behave alike.

### Extension points

A module can contribute to other parts of the studio without either side importing the other:

| Extension | Where it appears |
|---|---|
| `dashboardWidgets` | Dashboard zones. |
| `workflowPropertiesTabs` | The workflow definition properties pane. |
| `activityTabs` | The activity properties panel. |
| `definitionRowActions`, `instanceRowActions`, `instanceViewerActions` | List rows and the instance viewer header. |
| `headerItems` | The app header, like the environment picker. |
| `uiHints` | Activity input editors keyed by UI hint, like the secret picker. |
| `settingsSections` | Cards on the Settings landing page. |
| `Provider` | A context provider wrapped around the whole app. |

See [Architecture](ARCHITECTURE.md) for the full contract.
