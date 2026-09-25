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

### Current-tip two-tenant browser follow-up (2026-09-25)

The [sanitized browser receipt](current-tip-browser-2026-09-25.json) records a
fresh disposable mapped run at Core `c4b3ce150160e3c9062b57f7b158fd6b968e1631`,
Extensions `ba8b71d91c15ffe5be4b2c539cf9f712e74af775`, and Studio
`20ceaeeed7e671f0c9662003e82063026f2216de`. Its no-remote mapped commit
was `9b7d423cf82593555c9c2f280705f8009437bf79`. Five reviewed overlays
were applied only to that disposable source; their tracked paths and the Core
commit that fixes their exact contents are in the receipt. The prepared
Workbench and two server-side Studio hosts used separate loopback processes,
separate Chrome profiles, two configured tenant host aliases,
and one fresh shared SQLite database. Both ClientLib asset families built, the
hosts compiled for `net10.0`, Workbench health returned 200, and the guarded
route probe found ten Core-owned Secrets routes without legacy routes or
assemblies.

The following generated assets remained in the same mapped worktree used by
the Studio host. Their last modification times were `2026-09-25 01:46–01:47 UTC`,
before the browser exercise. SHA-256 values were measured after the run; the
raw private asset-build log remains local. For this exact disposable commit,
the retained private `import-receipt.json` hashes to
`6f349cb5c76a9ad0ebd086892acc39224ae681d115f7ef314c311fef0b535a4a`
and `consolidated-build-receipt.json` hashes to
`a0d4d60a14b16eddc1001e8c443c5ef3d270c2d57bbf44ce3736e9ad24d6992e`
(SHA-256). The [earlier public same-pin preparation receipt](../../consolidation/current-tip-c4b3-evidence/consolidated-build-receipt.json.gz)
is a separate synthetic commit; it does not replace these run-specific hashes.
The browser fixture's host DLL digest was not retained, so the asset table
does not close that provenance gap.

| Generated asset under `src/studio/` | SHA-256 |
| --- | --- |
| `framework/Elsa.Studio.DomInterop/wwwroot/dom.entry.js` | `2f73757bcf89a8ce980c9b62cbe03d173e852f52d73d4f423dfe38b146146efa` |
| `framework/Elsa.Studio.DomInterop/wwwroot/clipboard.entry.js` | `cd9f895109f2d6e662a68056c0e4bce85b960c6e417416a245932dfc14b9082c` |
| `framework/Elsa.Studio.DomInterop/wwwroot/files.entry.js` | `fd2a7ffa35aa7ce5269daebe6c16e2f2533d26ecac757ac5e461fa0aa908db91` |
| `modules/Elsa.Studio.Workflows.Designer/wwwroot/designer.entry.js` | `2c3731492966b6061dfb3b4da606fa31552c07389775df706ea1c3e581ac59f3` |
| `modules/Elsa.Studio.Workflows.Designer/wwwroot/react-designer.entry.js` | `1e1688338ee657e96187974df01562c6a9056a4a0c373fb811a973c828b7fe88` |
| `modules/Elsa.Studio.Workflows.Designer/wwwroot/designer.css` | `5291afcec6ba61e9e02138c89991eb84f39dcfdf249f85d0fed8f614e578f25b` |

After waiting for each Blazor connection before filling the login form, both
tenant administrators and the roleless tenant-A user signed in through Studio.
Tenant A and B each created `smoke-shared-key` with distinct synthetic metadata
and values. The lists and workflow expression pickers showed only the tenant's
own record; tenant B's direct read of A's separate `smoke-a-only` returned 404.
Switching back to A still showed A's metadata. A's separate secret also passed
UI detail, edit, rotation to version 2, positive resolution test, revocation,
and deletion. The roleless user's list, detail and create attempt returned 403
without disclosing metadata or creating a row. Studio still displayed its
Create control to that user, so permission-aware presentation remains a
separate UI issue; the server denied the mutation.

Each tenant saved an unpublished Write Line draft selecting its own same-name
secret. Read-only inspection found `Secret` expressions containing exactly
`{name: "smoke-shared-key", typeName: "text"}` and no synthetic plaintext in
either workflow definition. There were zero workflow instances and execution
log records. Exact-marker scans found none of the four synthetic values in the
database, WAL, SHM, Workbench log, or either Studio log. Observed browser
screens likewise did not display a value. These checks do not prove that every
possible response or future value is redacted. Raw HTTP response bodies were
not captured or scanned in this browser run; that preflight check remains
unverified.

The repeatable private fixture setup and guarded cleanup procedure remain in
[Workbench Secrets runtime preflight](../../../../scripts/integration-program/workbench-secrets-runtime-preflight.md).
All three processes were stopped before the owned fixture was removed. This
run confirms the earlier same-name, switch-back and saved-reference gaps in a
mapped rehearsal. It does not close #8326: the same relevant browser smoke
must run after the history-preserving import. No workflow was published or
executed, and no provider account, package feed, or production system was used.

A separate [cross-host token check](cross-host-token-2026-09-25.json) used a
fresh Workbench-only fixture at the same source pins. The tenant-A JWT could
not read a tenant-B secret, even when sent to B's host alias. A tenant-B JWT
could read its own secret when sent to A's host alias. The current Secrets
access path therefore showed tenant-scoped data, while the tested host alias
did not act as a token or environment binding. This does not establish a
production policy defect; it is an explicit design and verification gap for
credential environment binding. The second host and fixture were stopped and
removed. The browser receipt's separate Studio origins must not be read as
proof that tokens are restricted to those origins.

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

### Verified disposable route run (2026-09-24)

The probe ran against a disposable mapped copy at Core
`1855a2ef2719d536a66181dec604e781bfdd42a9`, Extensions
`ba8b71d91c15ffe5be4b2c539cf9f712e74af775`, and Studio
`20ceaeeed7e671f0c9662003e82063026f2216de` (synthetic rehearsal source commit
`f39c2e0b47c45f959f6cbfea2fb8faf5b9a91077`). The Workbench project
built for `net10.0` with zero errors and 227 warnings. A private, loopback-only
host started with `ASPNETCORE_ENVIRONMENT=Production`, an ephemeral signing key,
an isolated SQLite database, and the two explicit Secrets feature overrides.
The raw probe response was passed through `validate_route_probe_payload`; only
the [sanitized route receipt](route-probe-receipt.json) is retained here. It
records ten canonical Core-owned routes and six canonical Secrets assemblies,
with no legacy Secrets route or assembly. The host was stopped after the probe.

The focused fixture suite passed 17/17 in both normal and optimized Python
modes; root independently reran the normal suite. The mapped host source was a
disposable rehearsal, not the final history import. Development startup on
this mapped tip fails existing Secrets service-lifetime validation, so this
Production-mode route observation does not clear that separate host issue.
The route probe does not establish tenant-membership policy, Studio browser
behavior, published-package upgrade compatibility, or release readiness.

### Newer Core pin and Development startup (2026-09-24)

The [newer-pin receipt](current-core-route-probe-receipt.json) records a second
disposable, no-remote import rehearsal at Core
`a13ac7a412e037280d7a568fd9dd87b06ff8b724`, the same Extensions and Studio
pins above, and synthetic rehearsal commit
`372ed7ed3973c90fa581bf6003040900137bc093`. This mapped source includes the
merged Secrets service-lifetime correction in #8370. The reviewed Workbench,
Studio menu, BPMN layout, two-tenant, and fixture-only route-probe overlays were
applied to the disposable source before building the `net10.0` Workbench host.

The host started in both Production and Development. In each mode, the private
loopback probe response passed `validate_route_probe_payload`: all ten routes
were owned by the canonical Core Secrets endpoint assembly and no legacy route
or assembly was present. The Development run therefore clears the specific
service-lifetime startup failure observed at the older Core pin. The fixture
deliberately had no sign-in credentials, and Elsa emitted its expected identity
bootstrap diagnostic; management authorization was not exercised. Both hosts
were stopped and the private fixture was removed. This is still a mapped-source
host check, not the imported-source Studio browser repeat or consumer upgrade
proof required before closing #8326.
