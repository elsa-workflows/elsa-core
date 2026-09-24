# Isolated Workbench and Studio Secrets runtime exercise

The receipt records a disposable, synthetic Workbench API and server-side Studio
exercise at the selected Core, Extensions and Studio source pins. It links the
reviewed Workbench opt-in patch, the fresh host build, two separately built
Studio ClientLib bundles and browser/API observations. The private fixture
configuration, credentials, database and raw logs are excluded from this
repository.

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

The proof does not establish actual Workbench tenant separation, use of real
provider accounts, published static assets, a history-bearing source import or
production readiness. Repeat the backend/Studio smoke after the import and
reconcile those gates before closing #8326 or #8275.
