# Isolated Workbench and Studio Secrets runtime exercise

The receipt records a disposable, synthetic Workbench API and server-side Studio
exercise at the selected Core, Extensions and Studio source pins. It links the
reviewed Workbench opt-in patch, the fresh host build, two separately built
Studio ClientLib bundles and browser/API observations. The private fixture
configuration, credentials, database and raw logs are excluded from this
repository.

The follow-up [host build provenance](host-build-provenance.json) addresses
the original host-only build receipt: the isolated net10.0 build now restores
and rebuilds the full project-reference graph without incremental compilation.
The receipt records matching SHA-256 values for all six Secrets project outputs
and their Workbench host copies. It also verifies that the distinct pinned old
Workbench patch can apply to the mapped baseline, reverse cleanly, and yield
the same mapped source after the current patch is applied.

The browser completed list, detail, create, metadata update, rotate, revoke,
delete, active/revoked test and workflow picker steps. It saved a named secret
reference in an unpublished draft without executing it. The draft was removed;
the owned synthetic secrets are `Deleted` tombstones. The 20-case HTTP matrix
covered a denied user and a view-only user in the default empty tenant. Exact
synthetic value markers were absent from the scanned workflow payload,
database and fixture logs. This is a bounded scan, not a universal leakage
claim.

The UI initially failed because DomInterop and Designer static bundles were
absent. Building both pinned ClientLibs and rebuilding Studio's static-assets
manifest enabled the picker. The mapped BPMN generator needed a temporary
path adjustment for that local build. The durable imported-source build/CI
integration remains open under #8287. Secrets navigation also remained absent
because Studio queried a legacy feature name; #8332 merged the isolated
compatibility fix, which the two-tenant follow-up below applied.
An unauthorized user could still see an enabled Create Secret control on the
direct page, while the server rejected its requests. Existing Studio PR #999
adds separate permission-aware navigation behavior and needs reconciliation.

The follow-up [two-tenant receipt](two-tenant-receipt.json) records a fresh
synthetic run of the opt-in `--two-tenant` fixture, with the reviewed
Workbench multitenancy and Studio menu patches. The mapped preparation
evaluated 167 projects and 2,586 properties; both Workbench and Studio hosts
built and started against a fresh shared SQLite database. Two tenant logins
returned matching tenant claims. Tenant A listed its own two secrets, tenant B
listed only its own one, tenant B received 404 for tenant A's private secret,
and a roleless tenant A user received 403. The actual Studio secret pickers
showed the corresponding tenant-scoped records, and each tenant saved an
unpublished, unexecuted draft selecting its own secret. The bounded scan
found no synthetic plaintext markers in the runtime logs or shared database.
The receipt corrects a two-character transcription error in the private
matrix-report hash against the existing source-profile ledger. It records Git
blob IDs for the complete tracked patch set, including the BPMN layout patch;
these identify exact patch content without embedding the private fixture.

This run used separate origins in the same Codex in-app browser because the
locked Mac could not create separate browser profiles. The source was a
disposable mapped rehearsal, not the history-bearing import. The matrix is
preparation evidence, not a build or test result. The browser did not publish
or execute either draft, and no provider account was contacted. The fixture
and processes were removed after the run. Repeat the relevant host and browser
smoke after the actual import before closing #8326 or #8275; the durable
frontend build and permission-aware Studio navigation remain separate gates.
The fixture used different technical names for each tenant, so same-name
storage isolation was not proven. It also did not record a switch back to
tenant A after using B or inspect the saved `{name,typeName}` reference shape.
Those checks remain open for the imported-source browser repeat.

## Default-host route probe

The optional `workbench-secrets-route-probe.patch` is a fixture-only overlay. It
adds a guarded loopback endpoint at `/__fixture/secrets/routes`; the endpoint is
registered only when the private launch override
`--Features:Secrets:RouteProbe=true` is supplied. It reports the active
`/secrets`, `/actions/secrets/`, `/bulk-actions/secrets/`, and
`/queries/secrets/` route metadata and loaded assemblies so the mapped Workbench
host can be checked after startup; it normalizes the configured API route
prefix before evaluating the route families. It reads the FastEndpoints
`EndpointDefinition.EndpointType` metadata and requires every retained route to
be owned by `Elsa.Secrets.Endpoints.Secrets.*`. It does not add a production
route or enable the legacy API.

Prepare the route probe only after applying the reviewed overlay to the mapped
canonical Workbench source:

```text
python3 scripts/integration-program/prepare_workbench_secrets_runtime.py \
  --rehearsal-root /private/owned/mapped-workbench \
  --core-sha <core-sha> --extensions-sha <extensions-sha> --studio-sha <studio-sha> \
  --route-probe
```

The sanitized receipt must be produced by
`validate_route_probe_payload(...)`. It retains only the ten canonical Core
routes, route count, canonical assembly ownership result, and the absence of
legacy routes and `Elsa.Secrets.Api`, `Elsa.Secrets.Management`, and
`Elsa.Secrets.Scripting` assemblies. Do not commit the raw endpoint metadata,
private host configuration, credentials, or process logs.
