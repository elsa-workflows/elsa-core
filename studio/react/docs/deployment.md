# Deploy the React Studio Preview

The Vite SPA is served by ASP.NET Core and calls the Elsa API from the browser.
This opt-in frontend targets 3.11.0 Preview; the Blazor Studio remains the default.

## Standalone host

From `studio/react/`:

```sh
pnpm install --frozen-lockfile
pnpm build
dotnet publish dotnet/Elsa.ReactStudio.Server -c Release -o artifacts/server
dotnet artifacts/server/Elsa.ReactStudio.Server.dll --urls http://0.0.0.0:8080
```

The default mount is `/studio-preview`. Set `ReactStudio:Path` in host configuration
or pass `--ReactStudio:Path=/` for a dedicated site. The host writes the HTML base
per request, so the same frontend build supports either mount and deep-link reloads.
The published configuration file is loaded on every configuration request. Set
`STUDIO_CONFIG_PATH` to an external file to replace it without publishing again.

## Docker

Build from the repository root:

```sh
docker build -f studio/react/Dockerfile -t elsa-react-studio:preview .
docker run --rm -p 8080:8080 \
  -e STUDIO_SERVER_URL=https://elsa.example.com/elsa/api \
  -e STUDIO_FEATURES=labels,secrets,security,user-tasks \
  elsa-react-studio:preview
```

The image builds the frontend with a frozen lockfile, embeds its assets in the host,
and runs the ASP.NET runtime on port 8080 as its supplied non-root app user. Node.js
is needed only in the build stage. To supply a complete configuration:

```sh
docker run --rm -p 8080:8080 \
  -v "$PWD/studio.config.json:/app/studio.config.json:ro" \
  elsa-react-studio:preview
```

`STUDIO_CONFIG` and the existing `STUDIO_*` overrides are also supported. Every
configuration value is browser-visible. See [Configuration](configuration.md).

## Backend and reverse proxy requirements

The configured API URL must be reachable from the user's browser. For separate
origins, allow the Studio origin, required methods, `Authorization`, `Content-Type`
and `X-Tenant-Id` in the backend's CORS policy. Use HTTPS for the API when the Studio
uses HTTPS. Match the Studio authentication provider to the backend.

For OIDC and Elsa External Authentication, default callback and logout URLs include
the Studio mount. Explicit configured redirect URIs take precedence. Register the
complete callback URI with the identity provider.

Forward the entire mount to ASP.NET, including deep links and `studio-config`, and
forward `/_content/Elsa.ReactStudio.Host/` for versioned JS/CSS. A proxy that preserves
a site prefix must configure ASP.NET's `PathBase` before mapping the endpoints.
Do not cache HTML or runtime configuration; versioned assets are immutable.

Workflow data lives in the Elsa server; browser preferences live in local storage.
Optional module visibility still depends on configuration and backend capabilities.
