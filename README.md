<div align="center">
  <img src="src/app/icon.png" alt="Elsa" width="88" height="88">
  <h1>Elsa Studio</h1>
  <p><strong>A modern, component-based web studio for <a href="https://github.com/elsa-workflows/elsa-core">Elsa Workflows 3.x</a>.</strong></p>
  <p>Design, run and operate workflows from the browser: a visual designer, live instance inspection, human tasks, secrets, diagnostics and more.</p>
  <p>
    <img src="https://img.shields.io/badge/Next.js-16.4-000000?logo=nextdotjs&logoColor=white" alt="Next.js 16.4">
    <img src="https://img.shields.io/badge/React-19.3-087EA4?logo=react&logoColor=white" alt="React 19.3">
    <img src="https://img.shields.io/badge/TypeScript-5-3178C6?logo=typescript&logoColor=white" alt="TypeScript 5">
    <img src="https://img.shields.io/badge/Tailwind-4-06B6D4?logo=tailwindcss&logoColor=white" alt="Tailwind CSS 4">
    <img src="https://img.shields.io/badge/pnpm-10-F69220?logo=pnpm&logoColor=white" alt="pnpm 10">
  </p>
</div>

![The workflow designer](docs/images/designer.png)

## Overview

Elsa Studio is a standalone front end for the Elsa Workflows engine. It is a client-rendered
application that talks directly to the Elsa REST API, so it can be deployed next to any Elsa server
without changing the engine.

Three modules are always available: the **dashboard**, **workflow definitions** and **workflow
instances**. Everything else is optional and switched on per deployment through configuration, and is
hidden automatically when the connected backend does not have the matching feature installed.

| | |
|---|---|
| **Visual designer** | Flowchart, sequence and state machine containers, a searchable quick-add palette, auto layout, live validation and keyboard-driven editing. |
| **Operations** | Instance viewer with the execution journal, activity state, incidents and variables, plus bulk cancel and retry through alterations. |
| **Human tasks** | A task inbox with assignment, dynamic forms, protected fields and completion outcomes. |
| **Governance** | Users, roles and permissions, labels, and write-only secrets that workflows reference by name. |
| **Insight** | A call-context view of how workflows invoke each other, data-flow overlays, console and structured logs. |

## Quick start

**Prerequisites:** Node.js 22 or newer, pnpm 10, and a running Elsa server with CORS open to the
studio's origin.

```bash
pnpm install
pnpm dev                 # http://localhost:3000
```

Point `servers[0].url` in `studio.config.json` at your Elsa API, which is typically something like
`https://localhost:5001/elsa/api`, then sign in. The default authentication provider is Elsa Identity,
so a username and password is all you need.

New to the project? Start with the [Getting started guide](docs/getting-started.md).

## Documentation

| Guide | What it covers |
|---|---|
| [Getting started](docs/getting-started.md) | Install, run, connect to a server, first sign-in. |
| [Configuration](docs/configuration.md) | **Adding servers**, authentication providers, feature flags, theming, environment variables. |
| [Modules](docs/modules.md) | What every module does, how to enable it, and how to build a new one. |
| [Designer](docs/designer.md) | Containers, the quick-add palette, data flow, call context, keyboard shortcuts. |
| [Deployment](docs/deployment.md) | Docker image, standalone output, runtime configuration. |
| [Troubleshooting](docs/troubleshooting.md) | CORS, hidden modules, sign-in failures, workflows from older Elsa versions. |
| [Architecture](docs/ARCHITECTURE.md) | Module contract, data access, designer internals, conventions. |

## Screenshots

### Dashboard

Throughput, status breakdown and per-workflow activity for the connected server.

![Dashboard](docs/images/dashboard.png)

### Workflow definitions

Published and draft versions, labels, filtering and a column picker.

![Workflow definitions](docs/images/workflow-definitions.png)

### Call context

See where a workflow sits in the hierarchy: every workflow that calls it on the left, everything it
calls on the right, with the call type on each edge, cycles highlighted, and unresolved or missing
targets called out. Drill into any node to open that workflow.

![Call context](docs/images/call-context.png)

### Instance viewer

Replay a run against the diagram, with the journal, activity state, variables and incidents.

![Instance viewer](docs/images/instance-viewer.png)

### Human tasks

Task detail with instructions, form fields, protected values and the recorded outcome.

![User task](docs/images/user-task.png)

Completion renders the task's pinned form, validates required fields and submits the response back to
the workflow.

![Completing a task](docs/images/user-task-form.png)

### Secrets

Credentials workflows reference by name. Values are write-only and never displayed.

![Secrets](docs/images/secrets.png)

### Themes

Five palettes and light, dark or system appearance, selectable per user from the header.

![Designer in dark mode](docs/images/designer-dark.png)

## Configuration at a glance

Configuration is resolved on the server for every request, so a container can be reconfigured without
a rebuild. See [Configuration](docs/configuration.md) for the full reference.

```jsonc
{
  "title": "Elsa Studio",
  "servers": [
    { "name": "Local", "url": "https://localhost:5001/elsa/api" },
    { "name": "Staging", "url": "https://staging.example.com/elsa/api", "tenantId": "tenant-1" }
  ],
  "auth": { "provider": "elsa-identity" },
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

Nothing in this document is secret: it is shipped to the browser.

## Project layout

```
src/
  app/                      Next.js routes; the catch-all delegates to the module registry
  config/                   runtime configuration: zod schema, server loader, React provider
  lib/
    api/                    HTTP client, typed endpoint groups, TanStack Query provider
    auth/                   AuthStrategy contract and the five providers
    modules/                StudioModule contract, registry, route matcher
    permissions/            resource:verb permission set mirroring the server
  components/
    ui/                     shadcn components (generated)
    common/                 shared building blocks (DataTable, PageHeader, ConfirmProvider, …)
    shell/                  app shell, sidebar, header, login
  modules/                  one folder per feature module
```

## Scripts

| Command | Purpose |
|---|---|
| `pnpm dev` | Development server on port 3000. |
| `pnpm build` / `pnpm start` | Production build (standalone output) and server. |
| `pnpm test` | Unit tests (Vitest). |
| `pnpm lint` | ESLint. |
| `npx tsc --noEmit` | Type check. |

## Docker

```bash
docker build -t elsa-studio-react .
docker run -p 3000:3000 \
  -e STUDIO_SERVER_URL=https://elsa.example.com/elsa/api \
  -e STUDIO_FEATURES=labels,secrets,security \
  elsa-studio-react
```

Mount a `studio.config.json` at `/app/studio.config.json`, or pass the whole document in
`STUDIO_CONFIG`, for the full configuration. See [Deployment](docs/deployment.md).
