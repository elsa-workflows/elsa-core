# Studio native-compensation draft proof fixture

This fixture contains a Playwright journey, an independent read-only SQLite verifier, and a separate API probe for the effect-free draft-authoring slice. The fixture source is not implementation evidence; the owning issue records actual host runs against their exact source heads. The route, response, and identity contracts were inspected in the effect-free implementation checkout; before use, root must recheck the exact source HEAD and working-tree state, relevant file hashes, and harness against the selected host outputs.

## Journey

The test creates a fresh Playwright context with empty cookies and origin storage, then signs in through the visible `/login` form (`User name`, `Password`, `Sign in`). Since the Server host prerenders this page, visible fields do not prove that its Blazor circuit can handle events. Before entering credentials, the test keeps both fields empty and retries the visible Sign in click until both inputs report `aria-invalid="true"` from the login component's managed required-field validation. This bounded, harmless validation round-trip proves interactivity without a fixed sleep or an authentication request; only then does the test fill the credentials and complete normal UI sign-in. It then creates a default Flowchart workflow. In the Activities picker it searches through the visible English `Search...` label. If the `Compensation Scope` leaf is not visible, it clicks the visible `Flow` tree-item label to expand that category, then drags the `draggable=true` Compensation Scope tree item onto the designer. It performs a user double-click on the actual `.elsa-activity` matching `.activity-type` and the `Compensation Scope` text. The custom region is expected to appear only after this interaction. The test repeats the visible activity double-click after reopening the workflow.

Inside the region, the test uses the keyboard Add step control twice to create two rows. It selects `Write Line` as both `Forward activity` and `Inline handler` for each row, then selects each child’s properties, opens the `Input` tab, and enters a fixed non-secret `Text` literal through the UI. It saves once and takes an independent SQLite baseline. It then renames the first row and uses its own scoped `Move step down` control, saves again, and verifies the changed name/order against the latest stored definition. The workflow is never run and no compensation activity is executed.

The read-only Python verifier opens SQLite with `mode=ro` and `PRAGMA query_only`. It reads every stored definition row for the workflow ID and hashes every persisted column, including IDs, versions, published/latest flags, and raw definition-data fields. It validates the latest row’s two names/order from the serialized native step `name` field, distinct stable scope/step/child IDs, `Elsa.WriteLine` child types, exact expected child pins, and exact `Text` literal expressions. Its identity digest sorts by stable step ID and includes child IDs, types, pins, and literal digests; its separate layout digest includes names and order. Neither digest reveals names, IDs, or literal contents.

After reopening the saved workflow, the test rechecks the identity, layout, and all-row digests. Immediately before clicking the accessible `Publish workflow` toolbar button, it snapshots the full definition row set; after the alert named `Native compensation publication refusal` appears with the fixed heading `Native compensation drafts cannot be published.`, it requires keyboard focus on that summary and compares all six rendered diagnostic target associations with a separate SQLite-derived digest. It then snapshots again and requires the exact row-set digest and all derived fingerprints to remain unchanged. Server Studio uses Server Blazor, so the browser cannot observe its server-side Refit response. The browser checks the safe feature-owned summary; the separate API caller checks the raw response contract.

After the browser runner exits successfully, the supervisor starts a separate bounded API probe. The probe logs in directly to Core with the generated username/password through `POST /elsa/api/identity/login`; this is an independent API authentication and does not read or reuse browser cookies, storage, or tokens. It then snapshots every definition row read-only, calls the stored-definition route `POST /elsa/api/workflow-definitions/{definitionId}/publish`, and requires HTTP 400, code `NATIVE_COMPENSATION_DRAFT_ONLY`, the exact fixed refusal message, and six diagnostics targeted to the two stored steps and their selected child IDs. A second all-row SQLite snapshot must match immediately after the rejection. Tokens, response bodies, diagnostic IDs, and raw database data stay in memory or private scratch. A separate receipt file contains only fixed assertion IDs/statuses/codes and is hashed by the supervisor. The supervisor still records all assertions as `NOT_EVALUATED` and keeps `proofStatus: UNQUALIFIED` for root evaluation.

## Safe evidence and assertion IDs

The fixed UI assertion IDs are `NC-DRAFT-LOGIN`, `NC-DRAFT-CREATE`, `NC-DRAFT-SCOPE-OPENED-VIA-UI`, `NC-DRAFT-TWO-STEP-KEYBOARD`, `NC-DRAFT-PAIRED-CHILDREN`, `NC-DRAFT-INPUT-LITERALS`, `NC-DRAFT-SAVE-BASELINE`, `NC-DRAFT-SQLITE-BASELINE`, `NC-DRAFT-RENAME-AND-REORDER`, `NC-DRAFT-SAVE-REORDERED`, `NC-DRAFT-SQLITE-REORDERED`, `NC-DRAFT-REOPEN`, `NC-DRAFT-REOPEN-STABLE-IDENTITY`, `NC-DRAFT-PRIVATE-CAPTURE`, `NC-DRAFT-PUBLISH-UI-REFUSAL`, `NC-DRAFT-ROWSET-UNCHANGED-ON-REFUSAL`, `NC-DRAFT-CONTEXT-CLOSED`, `NC-DRAFT-CHROMIUM-PROCESS-CAPTURE`, and `NC-DRAFT-CHROMIUM-PROCESS-EXIT`. The separate API result uses `NC-API-STORED-PUBLISH-REFUSAL`, `NC-API-STORED-PUBLISH-TARGETED-DIAGNOSTICS`, and `NC-API-STORED-PUBLISH-ROWS-UNCHANGED`. Result files contain only their fixed IDs, `NOT_RUN`/`PASS`/`FAIL`, and fixed failure codes. The receipt interpretation remains `NOT_EVALUATED`; the root agent must independently assess assertion records, cleanup, exact source/build provenance, and the run before making any proof claim.

The random workflow name, definition ID, digest values, raw Chromium PIDs, expected literals, and private fixture database remain under disposable run scratch. The private handoff file also carries the expected two-row values for the independent API probe. Assertion, API-result, and process-evidence files go to the private receipt directory. No credentials, raw definition, entered values, page content, request/error body, URL, screenshot, trace, video, browser storage, test output, or host output is written to durable receipts.

The browser-process capture remains private. Playwright records CDP `SystemInfo.getProcessInfo` snapshots; the supervisor joins those to macOS birth, parent, process-group, and session identities. On timeout it signals only individually revalidated PIDs. Missing capture or conflicting identities leaves cleanup unconfirmed and retains scratch. The supervisor owns the runner and host groups; its process ownership and safe cleanup implementation is unchanged by this harness revision. Blank-page normal-close and forced-deadline cleanup smokes contain no Elsa host, login, database, or feature proof. Retain their actual command/results in the owning issue rather than inferring feature acceptance from them.

## Required inputs and build provenance

The supervisor requires explicit absolute `--dotnet` and `--node` paths, compiled Core/Studio/startup paths, loopback ports, `--scratch-parent`, and `--build-manifest`. The Server Studio directory must be published output containing its static web assets, since the isolated host runs in Production; copying ordinary build output can start the host while its scripts and styles return 404. The scratch parent must be outside this fixture, the Elsa source checkout, and the receipt directory; each invocation creates a private `runs/` child there. The receipt directory stays outside the source checkout and private scratch. Runtime output is copied before launch; source hosts are not built by the supervisor.

The external build manifest is mode `0600` JSON with this compact schema:

```json
{
  "schemaVersion": 1,
  "sourceHead": "<exact lowercase 40-character commit>",
  "sourceTreeClean": true,
  "builds": [
    { "component": "core", "command": "<recorded build command>", "exitCode": 0 },
    { "component": "server-studio", "command": "<recorded build command>", "exitCode": 0 },
    { "component": "startup", "command": "<recorded build command>", "exitCode": 0 }
  ],
  "binarySha256": {
    "core-host/<relative runtime output path>": "<sha256>",
    "studio-host/<relative runtime output path>": "<sha256>"
  }
}
```

`binarySha256` must exactly match every copied `.dll`, `.deps.json`, `.runtimeconfig.json`, `.dylib`, `.so`, and `.exe`, including the startup DLL/deps copied into the Studio output. The supervisor records the manifest hash and source HEAD and compares the copied output map before launching. It does not build these files or cryptographically attest to the builder; the root agent is the trusted builder. The supervisor’s own source and startup `.cs`/`.csproj` hashes are included in the harness hash section of its safe receipt.

The Playwright output directory is disposable and separate from durable receipts. The fixture uses one worker, no retries, line-only reporting, and disabled trace, screenshot, and video; it does not use browser `storageState`. No assertion parser promotes receipt status: the supervisor always records `proofStatus: UNQUALIFIED` and `NOT_EVALUATED` assertion results.

## Remaining limits

Acceptance requires exercising the complete login, scope drill-down, two-row authoring, SQLite schema/data serialization, child selection, input editing, reopen, visible refusal, stored-publish API rejection, and stored-row invariants in a real Elsa host. The test depends on an accessible localized publish button name and on the row controls being exposed by the existing UI. Root must recheck exact source/build provenance and any selector changes before running it. Synthetic SQLite or parser preflights do not establish those host behaviors.
