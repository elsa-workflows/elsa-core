# Elsa Studio (Next.js)

A component-based web studio for [Elsa Workflows 3.x](https://github.com/elsa-workflows/elsa-core),
built from scratch with Next.js, React, TypeScript, Tailwind CSS, shadcn/ui and React Flow.

Core modules (always on): **Dashboard**, **Workflow definitions** (list + visual editor) and
**Workflow instances** (list + viewer). Every other module is enabled per deployment through
configuration and is hidden automatically when the backend does not have the matching feature:
environments, labels, secrets, security (users & roles), alterations, user tasks, settings, appearance, console logs,
structured logs.

## Quick start

```bash
pnpm install
cp studio.config.json studio.config.local.json   # optional: keep your own copy
pnpm dev                                         # http://localhost:3000
```

Point `servers[0].url` in `studio.config.json` at your Elsa API (default `…/elsa/api`). The Elsa
server must allow CORS from the studio origin (the reference host in elsa-core already does).

## Editing workflows

Three designers, one per container type that elsa-core executes:

- **Flowchart** (`Elsa.Flowchart`): free graph of steps joined by outcome ports.
- **Sequence** (`Elsa.Sequence`): an ordered list; steps run top to bottom, a terminal step ends it
  early. Reorder by dragging, insert with the + buttons. No manual connections exist, by design.
- **State machine** (`Elsa.StateMachine`): states as nodes, transitions as edges. Each state can have
  an entry and an exit activity; each transition a trigger (armed while the source state is current),
  a condition and an action. Transitions without a trigger are automatic and evaluated in declaration
  order; the first state becomes the initial state. State positions are stored in the activity's
  designer metadata, everything else is exactly the server's model.

Pick the designer when creating a workflow. Containers nest: an If's branch, a state's entry or a
transition's action can hold a flowchart, a sequence or another state machine, opened from the
breadcrumb.

The designer follows the interaction patterns of modern flow builders:

- **Quick-add palette**: press `A`, double-click the canvas, click **+** on a step's port, or **+** on a
  connection to search and place an activity; it is wired in automatically and recently used
  activities are listed first.
- **Context toolbar** on the selected step: add next, rename (`F2` or double-click), duplicate, mark a
  trigger as the starting point, delete.
- **Live checks**: disconnected steps and dangling connections are flagged on the canvas and listed in
  the header; clicking a finding selects the step. Server-side validation still runs on save/publish.
- **Keyboard**: undo/redo, copy/paste, select-all, save; the keyboard icon in the editor lists them.
- **One toolbar**: the editor takes over the app header (breadcrumb with the editable workflow name,
  Designer/Code switch, a View menu for panels and mini-map, save state, Publish, Run); the
  properties pane shows the workflow's settings, or the selected step's inputs.
- **Canvas controls**: zoom in/out/fit and the mini-map toggle sit bottom-left; editing actions
  (Add, undo/redo, auto-layout) top-right. Steps with several outcomes let you pick which outcome to
  append to or connect from; "Connect to…" wires a step to an existing one with a click.
- **Straight connections and snapping**: a step's in-port sits on the same row as its out-port, so
  aligned steps connect with a straight line; while dragging, a step snaps to neighbouring ports and
  edges (within 6 px) and dashed helper lines show the alignment.
- **Data flow**: the Variables and I/O tabs show how many steps write (↑) and read (↓) each item;
  hovering highlights those steps on the canvas and clicking lists them. The data-flow overlay (View
  menu or the database icon) draws dotted associations from writers to readers and tags each step with
  the data it uses. Expression editors autocomplete `variables.X`, `getX()`, `getInput('X')` and
  `getOutputFrom('Step', 'Output')`.
- Auto-save is on by default (toggle it from the … menu) and leaving with unsaved changes asks for
  confirmation. Lists offer quick filter chips, a column picker and a clear-filters shortcut.

## Configuration

Configuration is resolved on the server for every request, so a container can be reconfigured
without rebuilding. Sources, highest precedence first:

1. `STUDIO_CONFIG` – a JSON document in an environment variable.
2. `STUDIO_CONFIG_PATH` – path to a JSON file (default `./studio.config.json`).
3. `STUDIO_*` overrides: `STUDIO_TITLE`, `STUDIO_SERVER_URL`, `STUDIO_SERVER_NAME`, `STUDIO_TENANT_ID`,
   `STUDIO_AUTH_PROVIDER`, `STUDIO_API_KEY`, `STUDIO_OIDC_AUTHORITY`, `STUDIO_OIDC_CLIENT_ID`,
   `STUDIO_OIDC_SCOPE`, `STUDIO_FEATURES` (comma-separated module ids to enable).

```jsonc
{
  "title": "Elsa Studio",
  "servers": [
    { "name": "Local", "url": "https://localhost:5001/elsa/api" },
    { "name": "Staging", "url": "https://staging.example.com/elsa/api", "tenantId": "tenant-1" }
  ],
  "auth": {
    "provider": "elsa-identity",                 // none | api-key | elsa-identity | oidc | elsa-external
    "apiKey": { "key": "" },
    "oidc": { "authority": "https://login.example.com", "clientId": "elsa-studio", "scope": "openid profile email" },
    "elsaExternal": { "clientId": "elsa-studio" }
  },
  "features": {
    "environments": { "enabled": true },
    "labels": { "enabled": true },
    "secrets": { "enabled": true },
    "security": { "enabled": true },
    "alterations": { "enabled": true },
    "user-tasks": { "enabled": true },
    "settings": { "enabled": true },
    "appearance": { "enabled": true },
    "diagnostics-console-logs": { "enabled": true },
    "diagnostics-structured-logs": { "enabled": true }
  },
  "ui": { "defaultTheme": "system", "theme": "classic", "pollIntervalMs": 5000, "defaultPageSize": 25 }
}
```

### Themes

`ui.theme` selects the default Elsa palette: `classic`, `workflow-constellation`, `workflow-aurora`,
`execution-timeline` or `human-automation` (the same packs as the Blazor studio). `ui.defaultTheme`
selects the appearance (`light`, `dark`, `system`). Users can override both from the palette menu in
the header; the choice is remembered per browser. All colours are CSS variables defined per palette in
`src/app/globals.css`, so components never hard-code hues: status colours come from
`--status-*`, the canvas and React Flow from `--elsa-canvas*` and the `--xy-*` overrides.

The full schema (with defaults) lives in `src/config/schema.ts`. Nothing in the configuration is
secret: it is shipped to the browser.

### Authentication providers

| provider        | How it works                                                                                  |
|-----------------|-----------------------------------------------------------------------------------------------|
| `none`          | No credentials; use when the Elsa server runs with security disabled.                          |
| `api-key`       | Sends `Authorization: ApiKey <key>` on every request (Elsa's default authentication feature).  |
| `elsa-identity` | Username/password login against `/identity/login`; JWT access token with silent refresh via `/identity/refresh-token`; logout revokes the session. |
| `oidc`          | OpenID Connect authorization-code flow with PKCE in the browser (`oidc-client-ts`); the access token (or id token) is sent as bearer. Callback route: `/auth/callback`. |
| `elsa-external` | Elsa's External Authentication broker: lists the server's login methods (local and external identity providers), runs the PKCE code flow against the Elsa server and exchanges the code at `/external-authentication/token`. Callback route: `/auth/callback`. |

Permissions in the token's `permissions` claim drive what the UI shows (`resource:verb`, with
wildcards). Tokens without the claim are treated as "unknown" and the UI shows everything; the
server remains the authority.

## Scripts

- `pnpm dev` – development server
- `pnpm build` / `pnpm start` – production build (`output: "standalone"`) and server
- `pnpm lint` – ESLint
- `npx tsc --noEmit` – type-check

## Docker

```bash
docker build -t elsa-studio-next .
docker run -p 3000:3000 -e STUDIO_SERVER_URL=https://elsa.example.com/elsa/api -e STUDIO_FEATURES=labels,security elsa-studio-next
```

Mount a `studio.config.json` at `/app/studio.config.json` (or set `STUDIO_CONFIG`) for the full
configuration.

## Architecture

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the module contract, data access, designer
internals and conventions for adding a module.
