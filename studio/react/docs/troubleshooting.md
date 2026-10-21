# Troubleshooting

## CORS: requests are blocked

**Symptoms.** Sign-in fails, lists stay empty, and the browser's network tab shows the request with a
referrer policy but **no status code** and no response body. The console mentions a cross-origin
request being blocked.

**Why.** The studio runs in the browser and calls the Elsa API directly, so the API must return CORS
headers that permit the studio's origin. A deployment that allows only its own bundled designer will
reject a studio running anywhere else, including `http://localhost:3000`.

**Confirm it** by sending the same preflight the browser sends:

```bash
curl -i -X OPTIONS https://your-elsa-host/elsa/api/identity/login \
  -H "Origin: http://localhost:3000" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: content-type"
```

Look at `access-control-allow-origin` in the response. If it names a different host, or is missing,
that is your answer. Note that `Access-Control-Allow-Origin` carries **one** origin or `*`; a
comma-separated list is invalid and browsers reject it.

**Fix it** in one of three ways:

1. Allow the studio's origin on the Elsa server. With Envoy Gateway, use a `SecurityPolicy` with
   `cors.allowOrigins`, which accepts a list, rather than setting the header by hand in a
   `ClientTrafficPolicy`.
2. Serve the studio from an origin the API already allows.
3. Proxy the API through the studio's own server so the browser sees a same-origin request.

A healthy but protected API answers an unauthenticated call with `401` and `WWW-Authenticate: Bearer`.
If you get that from `curl`, the server is fine and the problem is purely CORS.

## A module is missing from the sidebar

An optional module appears only when **both** conditions hold:

1. `features.<id>.enabled` is `true` in the configuration.
2. The connected server reports the feature the module requires.

Check the second by querying the installed-feature catalogue:

```bash
curl -s https://your-elsa-host/elsa/api/features/installed \
  -H "Authorization: Bearer $TOKEN"
```

If the feature is absent, the server does not have that module installed and the studio is correctly
hiding it. [Modules](modules.md) lists the requirement for each one.

Permissions are the other possibility: navigation entries are filtered by the `permissions` claim in
your token, so a user without `secrets:view` will not see Secrets.

## Sign-in fails

- **No status code in the network tab** is CORS, not credentials. See above.
- **401 from `/identity/login`** means the credentials were rejected, or the configured provider does
  not match the server. A development Elsa server usually accepts `admin` / `password`.
- **404 from `/identity/login`** means the server does not have the Elsa Identity module, so pick a
  different provider in [configuration](configuration.md#authentication).
- **Mixed content** errors appear when the studio is served over `https` but the configured API URL
  uses `http`. Browsers block that.

## A workflow fails to open with a 500

The studio shows "Could not load the workflow" when the API returns `500` for
`workflow-definitions/by-definition-id/...`. The error is server-side: the definition was found, but
the server could not turn the stored row into a model. Common causes are a stored payload that cannot
be deserialised, or a definition row carrying no payload at all, which happens with partially migrated
data.

The endpoint distinguishes other cases clearly, which narrows it down: a missing definition returns
`404`, and an unavailable materializer returns `422` with a message naming it.

Open another workflow to tell the two apart. If everything else loads, that one row is the problem
rather than the deployment. The server's log for that request holds the exception.

## Workflows from older Elsa versions look wrong

Definitions authored by earlier 3.x versions encode some inputs differently. The studio reads both the
old and the current shapes, including:

- **Lists** such as an HTTP endpoint's supported methods, stored as a JSON string rather than an array.
- **Variable bindings**, stored as a `Variable` expression rather than as a literal.

If a field still looks wrong, check the workflow's `toolVersion` in the Info panel and open an issue
with that value and the field in question.

## Changes to `studio.config.json` are not picked up

Configuration is read per request, so a browser reload is normally enough. If it is not:

- Confirm the file you edited is the one being read. `STUDIO_CONFIG` takes priority over any file, and
  `STUDIO_CONFIG_PATH` may point elsewhere.
- Check the server log for a validation error. An invalid document fails fast with a message naming
  the offending path.

## The environment picker is missing

It appears only when the `environments` module is enabled **and** two or more servers are configured.
A single server leaves nothing to switch between.

## Reporting a problem

Useful details to include:

- The studio version and the Elsa server version.
- The failing request: method, URL, status code, and the response body if there is one.
- Whether other workflows or pages behave the same way.
