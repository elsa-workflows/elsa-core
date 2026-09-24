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
path adjustment for that local build. The durable build/CI integration remains
open under #8287. Secrets navigation also remained absent because Studio
queried a legacy feature name; #8332 prepares the isolated compatibility fix.
An unauthorized user could still see an enabled Create Secret control on the
direct page, while the server rejected its requests. Existing Studio PR #999
adds separate permission-aware navigation behavior and needs reconciliation.

The recorded browser receipt does not establish actual Workbench tenant
separation, use of real provider accounts, published static assets, a
history-bearing source import or production readiness. The preparer now has an
opt-in `--two-tenant` mode for a separate synthetic host/browser run. That
mode requires the hashed, fixture-only Workbench multitenancy activation patch
and the merged Studio Secrets menu patch to be applied in a fresh mapped clone;
it has not been launched or browser-verified. See the runbook for its exact
patch, build and cleanup steps. Repeat the smoke after the history import and
reconcile those gates before closing #8326 or #8275.
