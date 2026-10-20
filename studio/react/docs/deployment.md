# Deployment

The studio is a Next.js application built with `output: "standalone"`. It serves the shell and
resolves configuration per request; all workflow data is fetched from the Elsa API by the browser.

## Docker

```bash
docker build -t elsa-studio-react .
docker run -p 3000:3000 \
  -e STUDIO_SERVER_URL=https://elsa.example.com/elsa/api \
  -e STUDIO_SERVER_NAME=Production \
  -e STUDIO_FEATURES=labels,secrets,security,user-tasks \
  elsa-studio-react
```

The image is multi-stage on `node:22-alpine`, installs with a frozen lockfile, builds, and runs the
standalone server on port 3000 as a non-root user.

### Configuring a container

`STUDIO_SERVER_URL` and its siblings only overwrite the **first** server. For anything more, supply
the whole document.

Mount a file:

```bash
docker run -p 3000:3000 \
  -v $(pwd)/studio.config.json:/app/studio.config.json:ro \
  elsa-studio-react
```

Or pass it inline, which suits Kubernetes ConfigMaps:

```bash
docker run -p 3000:3000 \
  -e STUDIO_CONFIG='{"servers":[{"name":"Prod","url":"https://elsa.example.com/elsa/api"}],"auth":{"provider":"oidc"}}' \
  elsa-studio-react
```

Because configuration is read per request, replacing a mounted file reconfigures a running container
without a restart. See [Configuration](configuration.md) for the full reference.

## Running without Docker

```bash
pnpm install
pnpm build
pnpm start
```

The standalone output lands in `.next/standalone` with static assets in `.next/static`, which is what
the Dockerfile copies.

## Requirements on the Elsa server

- **CORS.** The API must allow the studio's origin, with the `Authorization` and `Content-Type`
  headers and the methods the studio uses. A single fixed allowed origin is a common cause of a studio
  that cannot sign in; see [Troubleshooting](troubleshooting.md#cors-requests-are-blocked).
- **Features.** Optional modules stay hidden unless the server reports the matching feature. See
  [Modules](modules.md).
- **Authentication.** The provider configured in the studio must match what the server expects.

## Reverse proxies

The studio is a plain HTTP application and sits behind any reverse proxy. Two points matter:

- The browser talks to the Elsa API **directly**, using the `url` from the configuration, so that URL
  must be reachable from the user's browser, not only from the studio container.
- If you terminate TLS at the proxy, make sure the configured API URL uses `https`, otherwise the
  browser blocks the calls as mixed content.

## Health and operations

- The studio holds no state of its own. Everything lives in the Elsa server.
- Per-user preferences such as the chosen environment, palette and column visibility are kept in the
  browser's local storage and are safe to lose.
- Scaling out needs no sticky sessions.
