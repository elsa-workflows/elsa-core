# Route probe response fixture

`canonical-secrets-route-probe.json` is the response captured from a fresh
mapped Workbench host, not a hand-authored validator example. The host used
`ASPNETCORE_ENVIRONMENT=Production`, a fixture-generated signing key, a fresh
SQLite database, loopback binding, and explicit fixture-only Secrets and route
probe overrides. No provider, publisher, or external credentials were used.

Source provenance:

- Core: `95a658b96107ad4dbb280a13972479af74bc6a30`
- Extensions: `33fa0bfd28c7585240e3d4f665058c067b17e287`
- Studio: `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`
- Mapped rehearsal: `e4607a6c9bd9dcfcc8a648beb50b765d5517fcc8`
- Fresh Workbench host DLL SHA-256: `a5c89ab258a5babefcf494d82e04c72e576a40c80497c7520d547076447db7dd`
- Route probe patch SHA-256: `956992c32399efb9e8a0e122a54100ba54b2037843bb2b62d6e60df84778a0f7`

The route response returned HTTP 200 with ten canonical routes. Every route's
`endpointAssembly` was `Elsa.Secrets`; the loaded assembly list contained the
six expected Secrets assemblies. The fixture retains the emitted endpoint
metadata needed by the validator and contains no credentials, database path,
signing key, loopback port, or process environment. The response was validated
with `validate_route_probe_payload` before it was retained. The checked-in JSON
is pretty-printed without changing its content; the original HTTP response
body had SHA-256 `289bc70332038061cf846aaf6d567c6a0d24a5f46463bc0f25bd93e637cce85c`.

These pins are the baseline profile used for this capture. The earlier
current-tip receipt remains separate and does not establish per-route assembly
ownership for the amended probe.
