# Configuration

Everything the studio needs at runtime lives in one document. It is resolved **on the server for every
request**, which means a deployed container can be reconfigured by replacing a file or an environment
variable, with no rebuild. A browser reload is enough to pick up a change.

The full schema, with defaults, is defined in [`src/config/schema.ts`](../src/config/schema.ts).

> Nothing in this document is secret. It is sent to the browser.

## Where configuration comes from

Sources are merged in this order, highest priority first:

1. **`STUDIO_CONFIG`** — a complete JSON document in an environment variable.
2. **`STUDIO_CONFIG_PATH`** — a path to a JSON file. Defaults to `./studio.config.json`.
3. **`STUDIO_*` variables** — individual overrides layered on top of whichever document was loaded.

## Adding a server

The `servers` array is the list of Elsa backends the studio can talk to. There is no UI for this; the
list is configuration only.

```jsonc
{
  "servers": [
    { "name": "Local", "url": "http://localhost:5080/elsa/api" },
    { "name": "Staging", "url": "https://staging.example.com/elsa/api", "tenantId": "tenant-1" },
    { "name": "Production", "url": "https://elsa.example.com/elsa/api" }
  ]
}
```

| Field | Required | Meaning |
|---|---|---|
| `url` | Yes | Base URL of the Elsa API, including the route prefix (usually `/elsa/api`). |
| `name` | No | Label shown in the environment picker. Defaults to `Default`. |
| `tenantId` | No | Sent as the `X-Tenant-Id` header on every request. |

With two or more servers configured, an environment picker appears in the header, provided the
`environments` module is enabled. The first entry is active by default.

### Four things worth knowing

- **Append rather than insert.** The active server is remembered per browser as an *index* into this
  array, under `elsa-studio:active-server` in local storage. Adding an entry at the top shifts the
  others and silently changes which backend a returning user is pointed at.
- **Each server must allow your origin.** The studio calls the API from the browser, so every backend
  in the list needs CORS headers permitting the studio's origin. See
  [Troubleshooting](troubleshooting.md#cors-requests-are-blocked).
- **Authentication is global.** There is a single `auth` block shared by every server. Backends that
  need different providers or different API keys cannot be mixed into one list.
- **Environment variables cannot add a server.** `STUDIO_SERVER_URL` and friends only overwrite the
  *first* entry. To deploy with several backends, pass the whole document through `STUDIO_CONFIG` or
  mount a file and set `STUDIO_CONFIG_PATH`.

The studio also lists environments that the active server advertises through its own environments
endpoint. Those appear in the picker for information, but only servers present in this file can be
selected.

## Authentication

```jsonc
{
  "auth": {
    "provider": "elsa-identity",
    "apiKey": { "key": "" },
    "oidc": { "authority": "https://login.example.com", "clientId": "elsa-studio", "scope": "openid profile email" },
    "elsaExternal": { "clientId": "elsa-studio" }
  }
}
```

| Provider | How it works |
|---|---|
| `none` | No credentials. Use when the Elsa server runs with security disabled. |
| `api-key` | Sends `Authorization: ApiKey <key>` on every request. |
| `elsa-identity` | Username and password against `/identity/login`; JWT access token with silent refresh; logout revokes the session. |
| `oidc` | OpenID Connect authorization-code flow with PKCE in the browser. Callback route is `/auth/callback`. |
| `elsa-external` | Elsa's external authentication broker: lists the server's login methods, runs the PKCE code flow and exchanges the code at `/external-authentication/token`. Callback route is `/auth/callback`. |

### Permissions

The UI reads the `permissions` claim from the token, in `resource:verb` form, with `*` wildcards on
either side and `path/*` matching descendants. Actions the user cannot perform are hidden.

A token **without** a permissions claim is treated as unknown rather than empty, and the UI shows
everything. The server always remains the authority: hiding a button is a convenience, not a
security boundary.

## Feature flags

Every module is keyed by its id. Core modules ignore the flag and are always on; every other module is
**off unless explicitly enabled**.

```jsonc
{
  "features": {
    "labels": { "enabled": true },
    "secrets": { "enabled": true, "options": {} }
  }
}
```

A module can also be hidden because the backend lacks the server feature it needs, even when enabled
here. [Modules](modules.md) lists the requirement for each one.

`options` is a free-form object handed to the module. The call-context view reads its limits from
`features["workflow-definitions"].options.callContext`, for example.

## Appearance

```jsonc
{
  "ui": {
    "defaultTheme": "system",
    "theme": "classic",
    "pollIntervalMs": 5000,
    "defaultPageSize": 25
  }
}
```

| Setting | Values | Meaning |
|---|---|---|
| `defaultTheme` | `light`, `dark`, `system` | Starting appearance. Users can override it from the header. |
| `theme` | `classic`, `workflow-constellation`, `workflow-aurora`, `execution-timeline`, `human-automation` | Elsa colour palette. |
| `pollIntervalMs` | milliseconds | Refresh interval for live views such as the instances list and the instance viewer. |
| `defaultPageSize` | number | Default rows per page in lists. |

Users can change both the palette and the appearance from the header, or from the Appearance page
under Settings. Their choice is remembered per browser and overrides these defaults.

## Environment variables

| Variable | Effect |
|---|---|
| `STUDIO_CONFIG` | A complete JSON configuration document. Highest priority. |
| `STUDIO_CONFIG_PATH` | Path to a JSON file. Defaults to `./studio.config.json`. |
| `STUDIO_TITLE` | Application title in the sidebar and browser tab. |
| `STUDIO_SERVER_URL` | Overwrites the URL of the **first** server. |
| `STUDIO_SERVER_NAME` | Overwrites the name of the first server. |
| `STUDIO_TENANT_ID` | Overwrites the tenant id of the first server. |
| `STUDIO_AUTH_PROVIDER` | `none`, `api-key`, `elsa-identity`, `oidc` or `elsa-external`. |
| `STUDIO_API_KEY` | API key for the `api-key` provider. |
| `STUDIO_OIDC_AUTHORITY`, `STUDIO_OIDC_CLIENT_ID`, `STUDIO_OIDC_SCOPE` | OpenID Connect settings. |
| `STUDIO_FEATURES` | Comma-separated module ids to enable, for example `labels,secrets,security`. |

## Validation

The document is validated with zod on load. An invalid configuration fails fast with a message naming
the offending path, rather than starting a studio that misbehaves later.
