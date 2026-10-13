# Getting started

This guide takes you from a clone to a signed-in studio connected to an Elsa server.

## Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| Node.js | 22 or newer | The Docker image is built on `node:22-alpine`. |
| pnpm | 10 | The repository pins `pnpm@10.33.0` through `packageManager`. |
| Elsa server | 3.x | Must expose the REST API and allow the studio's origin through CORS. |

## Install and run

```bash
pnpm install
pnpm dev
```

The studio starts on <http://localhost:3000>. The development server reads `studio.config.json` on
every request, so configuration changes need only a browser reload, not a restart.

## Point it at a server

Open `studio.config.json` and set the first entry's `url` to your Elsa API. The base URL ends with the
API route prefix, which is `/elsa/api` by default:

```jsonc
{
  "servers": [
    { "name": "Local", "url": "https://localhost:5001/elsa/api" }
  ],
  "auth": { "provider": "elsa-identity" }
}
```

To work against several backends at once, see
[adding a server](configuration.md#adding-a-server) in the configuration guide.

### The server must allow your origin

The studio runs in the browser and calls the Elsa API directly, so the API has to return CORS headers
permitting `http://localhost:3000`. The reference host in `elsa-core` already allows any origin in
development. If sign-in fails with no status code in the network tab, this is almost always the cause,
and [Troubleshooting](troubleshooting.md#cors-requests-are-blocked) explains how to confirm it.

## Sign in

![Sign in](images/login.png)

The default provider is Elsa Identity, which signs in with a username and password against
`/identity/login` and keeps the session alive with a silent refresh. A development Elsa server
typically starts with `admin` / `password`.

Four other providers are supported, including API key, OpenID Connect and Elsa's external
authentication broker. See [authentication](configuration.md#authentication).

## First steps

Once signed in:

1. **Dashboard** shows throughput and status for the connected server.
2. **Workflows → Definitions** lists what exists. Use **New workflow** in the sidebar to create one,
   choosing a flowchart, sequence or state machine.
3. Build the workflow in the [designer](designer.md), then **Publish** it.
4. **Run** it from the editor header, and follow the run under **Workflows → Instances**.

## What you see depends on the backend

Optional modules are hidden unless two things are true: the module is enabled in configuration, and
the connected server reports the feature it needs. A studio pointed at a minimal Elsa server will
legitimately show fewer sidebar entries than one pointed at a full installation.

[Modules](modules.md) lists every module with the server feature it requires.

## Verify your changes

```bash
pnpm test          # unit tests
pnpm lint          # ESLint
npx tsc --noEmit   # type check
```
