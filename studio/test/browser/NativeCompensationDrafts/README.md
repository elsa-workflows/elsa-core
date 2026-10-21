# Native compensation draft browser and API proof

This fixture tests the effect-free draft-authoring prerequisite in #8956. It never executes a workflow. The fixture and preparation checks are not acceptance evidence; the owning issue records actual runs against their exact source heads.

Program [#8884](https://github.com/elsa-workflows/elsa-core/issues/8884) owns this temporary proof machinery. It can be removed when its checks are replaced by the maintained Studio compensation qualification suite and its accepted evidence is retained. The hosting-startup project lives under `scripts/native-compensation-draft-proof`, outside the broad Studio updater root and deliberately outside `Elsa.sln`: it is built explicitly for this isolated local proof, is not a product dependency, and is not packaged.

The Playwright journey signs in through the normal ElsaIdentity UI in a fresh context, visually authors two paired rows, edits forward/handler inputs, saves, renames/reorders, reopens, and compares stable IDs, exact pins, values and all stored definition rows through an independent read-only SQLite verifier. It then attempts Studio Save + Publish and requires a visible refusal and unchanged rows/flags. A separate Python caller signs in through the Core login API and tests the actual stored-definition Publish route, targeted diagnostics and unchanged rows. Its bearer stays in memory and is never supplied to the browser.

## Toolchain and isolation

Use macOS with Python 3.11+, Node, the lockfile-pinned Playwright Chromium, and the repository-compatible .NET SDK. Process ownership verification uses macOS libproc; this launcher does not claim Linux/Windows cleanup support. Build these projects from one clean, frozen source head, serially through the configured .NET build wrapper:

- `core/src/apps/Elsa.ModularServer.Web/Elsa.ModularServer.Web.csproj` for `net10.0`.
- `studio/src/hosts/Elsa.Studio.Host.Server/Elsa.Studio.Host.Server.csproj` for `net10.0`.
- `scripts/native-compensation-draft-proof/StudioProofDataProtectionStartup/StudioProofDataProtectionStartup.csproj`.

Provision `StudioDraftProof` dependencies with `npm ci --ignore-scripts --no-audit --no-fund`, using isolated npm configuration/cache and a private browser cache. Run its `typecheck` script before launching. Do not copy local credentials or settings into the fixture. The startup library is non-packable and configures an isolated temporary Data Protection key ring through the standard hosting-startup seam; it adds no product setting.

`run-proof-skeleton.py --help` lists explicit absolute executable, compiled-output, browser, source, manifest, scratch and receipt paths. Both loopback ports must be unused. Supply the exact full source HEAD, every proof-relevant repository-relative source/test path, and all required UI assertion IDs from `StudioDraftProof/README.md`. The source must remain clean and unchanged through the run. The external build-manifest contract is documented in that README; its hashes must cover all copied host runtime outputs and the startup DLL/deps, not only the entry DLLs. Root is the trusted builder; this manifest is not cryptographic attestation.

Provide fresh disposable local fixture username/password through `ELSA_COMP_PROOF_ADMIN_USERNAME` and `ELSA_COMP_PROOF_ADMIN_PASSWORD` from a private launch process, never command arguments or shell history. The fixture creates an isolated admin role/user to exercise the normal UI and API login flows. It uses private SQLite databases, random signing/encryption/broker secrets, a private key ring and HOME/temp directories. Environment-specific appsettings files are excluded from copied host outputs; Core receives a private shell overlay, its sample external-authentication client is disabled, and Nuplane package loading/reconciliation/watchers are disabled. Telemetry SDK export is disabled for both hosts.

## Evidence and cleanup

Tracing, screenshots, video, browser-state files, request bodies and host/browser output are disabled or discarded. Private names, IDs, expected literals, credentials, database contents and raw process ownership metadata stay in disposable scratch. Durable mode-0600 receipts contain fixed assertion IDs/status/failure codes and hashes. The supervisor verifies owned process termination before deleting temporary data; uncertain ownership retains private scratch and reports cleanup unconfirmed. Never remove retained scratch until process absence is independently confirmed.

For a browser-only cleanup preflight after dependency provisioning, run `run-browser-cleanup-smoke.py` with explicit `--node`, `--browser-cache` and an existing external `--scratch-parent`. It uses a blank page to test normal close and forced-deadline teardown. A pass establishes fixture cleanup only; it is not an Elsa backend SIGKILL/restart demonstration.

The supervisor always emits `UNQUALIFIED` and does not independently evaluate assertion records. The integration lead must inspect every required UI/API assertion, source/build/harness hashes, process cleanup and retained failure history before publishing a bounded acceptance report. Record actual commands/results and exact source HEAD. A successful exit alone is insufficient.

This one Server Studio journey does not establish WebAssembly browser behavior. Both host-compatible modules still require builds/tests. The program's later effect-capable Studio/backend/persistence abrupt-kill and restart proof remains mandatory; this authoring fixture does not satisfy it.
