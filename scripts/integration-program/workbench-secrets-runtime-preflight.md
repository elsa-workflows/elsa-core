# Workbench Secrets runtime preflight

This preparation creates an isolated host fixture; it does not start Workbench or Studio. The runtime proof must launch two separate processes: the Workbench API host first, then the mapped server-side Studio host. Backend login/API checks alone do not prove the Studio Secrets screens.

The source baseline inspected for this plan is Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287`, at `src/workbench/Elsa.Server.Web/Program.cs` and `src/workbench/Elsa.Server.Web/Elsa.Server.Web.csproj`. The project uses `Microsoft.NET.Sdk.Web` and leaves SDK Compile globs enabled. The pinned tree has 12 C# files and no `IWorkflow` or `WorkflowBase` declarations. The fixture-preparation script re-evaluates the mapped project with `dotnet msbuild -getItem:Compile`, requires the evaluated C# inputs to match the source inventory, and records the list. `AddWorkflowsFrom<Program>()` scans exported `IWorkflow` types from the Workbench host assembly, as implemented by Core's `WorkflowTypeScanner`; it does not scan referenced assemblies. A fresh SQLite database therefore has no restored workflow instances for the recurring restart task to resume.

The pinned 95a rehearsal contains the earlier canonical Workbench Secrets patch (SHA-256 `5dd4667190c538279f3b89cea80b81681402775555a89d89248001c2e22aa446`). A newer rehearsal may instead carry the byte-exact current patch (SHA-256 `eb792ede2d40eaae72f8b6174ba920c8e1ee02e71e2506c4905737245d1c313c`). Prepare the **disposable mapped source** so the current patch is applied exactly once: reverse the older patch first if that older overlay is present, apply the current patch if it is absent, and leave the source unchanged if the current patch is already applied. Check the applicable state with `git apply --check` or `git apply --reverse --check` before changing it. Never apply a patch to the canonical source repository. The runtime validator accepts only the reviewed artifact hashes, reverses the complete mapped patch chain to verify the source baseline, and replays the applicable chain in a separate temporary ledger. The generated `patchTransition` describes that validator replay; it is **not** a command to reapply a patch to an already-current checkout. The reviewed runtime delta makes Secrets opt-in through `Features:Secrets:Enabled`, whose default remains `false`. The source receipt hash refers to the external preparer's integration patch, not the older copy of that tool stored inside the mapped rehearsal.

The runtime validator records the source pins, the mapped and current Workbench patch hashes, whether an old-to-current transition occurred, the supplemental Dapper/MongoDB/Studio patch chain, prepared source receipt hashes, evaluated Workbench Compile items, and the fresh host DLL hash. The canonical rehearsal's existing `obj/project.assets.json` files embed its original absolute paths. Restore the Workbench project inside the clone to regenerate clone-local assets without compiling dependencies, then build the host with `BuildProjectReferences=false` against the copied, already-built dependency outputs. Before running those commands, verify the clone's Git root and commit, confirm it has no remotes or alternates, compare the source-tree sentinel inodes/link counts, and review the restore/build logs and generated asset paths for references to the original rehearsal. This bounded Workbench clone restore/build passed on 2026-09-24; the Studio host needs a separate reference-enabled incremental build after its static assets are generated, as described below. The dated receipt records the actual DLL hashes and validation limits.

The Workbench source derives two paths from the process working directory: distributed locks use `App_Data/locks` and drop-ins use `App_Data/DropIns`. The fixture sets its private temporary root as the process working directory and creates both directories there. Its separate content root contains a fresh `App_Data/workbench.sqlite`; the mapped source's `App_Data`, database, WAL/SHM files, and lock files are never copied into or used by the host. The fixture sets `Http:BasePath` to `/workflows`, preserving the source default so the workflow HTTP middleware does not intercept the health and API routes. The source enables EF migrations and Quartz, uses an in-memory MassTransit broker, and disables Hangfire, Kafka, SignalR, and multitenancy. The fixture empties `Webhooks:Sinks`, binds SQLite to its private database, leaves SMTP empty, and points MQTT at `127.0.0.1:1`. The source registers MQTT, LDAP, SQL, email, webhooks, and drop-ins; the fixture has no workflow definitions or instances to invoke these activities. The dated receipt includes the observed network and persistence checks.

The synthetic user and role use the empty tenant ID. This follows Core's `Tenant.DefaultTenantId` and `DefaultTenantAccessor` behavior; it does not claim multitenant coverage. The script creates a random password, signing key, and 32-byte Secrets key inside the private fixture, and writes credentials/configuration with mode `0600`. A real Workbench login verified the configuration-backed `Identity.Users` entry; the bootstrap diagnostic only checks persisted `IUserStore` records and is not evidence that the configuration user was ignored.

Prepare only from the reviewed mapped rehearsal root and exact three source pins. On this macOS host, `tempfile.gettempdir()` follows `TMPDIR`; set it to `/private/tmp` so the fixture and its failure logs remain in the approved private temporary area:

```sh
TMPDIR=/private/tmp python3 scripts/integration-program/prepare_workbench_secrets_runtime.py \
  --rehearsal-root /path/to/mapped-rehearsal \
  --core-sha <40-character-core-sha> \
  --extensions-sha <40-character-extensions-sha> \
  --studio-sha <40-character-studio-sha> \
  --temp-parent /private/tmp
```

For the **committed history-import repeat** in #8326, use a separate clean,
isolated checkout of the selected import PR head. Do not generate rehearsal
receipts or apply the mapped-source patches to that checkout. The preparer
requires the exact checkout SHA, the original three-parent import commit, and
the three original source commits; it verifies the parent order and ancestry,
committed Workbench and Studio files, reviewed fixture patch artifacts and
their applied changes, and no tracked or unignored untracked changes before
building. Git's status check does not inventory ignored inputs. The resulting plan says
`sourceMode: history-import`, records the exact source revision and import
parents, and leaves rehearsal receipt fields empty. The subsequent Workbench
build still writes ignored `bin`/`obj` outputs in this isolated checkout, so
do not point this mode at a shared working tree.

```sh
TMPDIR=/private/tmp python3 scripts/integration-program/prepare_workbench_secrets_runtime.py \
  --imported-root /path/to/isolated/import-checkout \
  --import-commit <three-parent-import-commit> \
  --imported-sha <exact-reviewed-checkout-head> \
  --core-sha <40-character-core-sha> \
  --extensions-sha <40-character-extensions-sha> \
  --studio-sha <40-character-studio-sha> \
  --temp-parent /private/tmp --two-tenant --route-probe
```

Build both Studio ClientLibs and launch the Workbench and Studio processes
using the same private loopback, database, browser-profile and cleanup steps
below. Record raw HTTP response-body checks in the private evidence, then
retain only sanitized status, route, hash and UI observations in the public
receipt. A successful preparation is not a browser result; #8326 stays open
until the actual imported-source host and browser exercise passes.

Before any launch, review the generated source receipt, `launch-plan.json`, `host-build.log`, and `launch-command.txt`; confirm the Workbench DLL hash and all configured data, lock, and drop-in paths are under the expected private roots. After the process exits, remove only the marked fixture:

```sh
TMPDIR=/private/tmp python3 scripts/integration-program/prepare_workbench_secrets_runtime.py \
  --cleanup /private/tmp/elsa-workbench-secrets-<fixture-id> \
  --host-stopped
```

After reviewing the backend fixture plan and build receipts, launch Workbench on its generated loopback URL. Verify process startup, SQLite migration, health, synthetic login, and Core Secrets API separately. Do not configure provider accounts, external webhook destinations, cloud services, or non-loopback listeners.

For the browser proof, use the distinct mapped server-side Studio project at `src/studio/hosts/Elsa.Studio.Host.Server`. Its `Program.cs` registers `AddSecretsModule(backendApiConfig)` and loads `appsettings.Local.json` after the tracked defaults. Put the private content root and its `appsettings.Local.json` under the fixture root, copy the tracked default and Development settings there, and write only these overrides with mode `0600`, substituting the two loopback ports from the reviewed launch plan:

```json
{
  "AllowedHosts": "127.0.0.1;localhost",
  "Backend": { "Url": "http://127.0.0.1:<workbench-port>/elsa/api" },
  "Authentication": { "Provider": "ElsaIdentity" }
}
```

The tracked Studio defaults select `ElsaIdentity` but point at `https://localhost:7294/elsa/api`; the private override changes only the backend URL and host allow-list. Use `ASPNETCORE_ENVIRONMENT=Development` with a private `HOME`, `DOTNET_CLI_HOME`, `TMPDIR`, and XDG directories. Studio's unchanged host calls `UseStaticFiles` and relies on the Development static-web-assets manifest; this proves the developer-host path, not published Production asset deployment.

Build both Studio ClientLib bundles before the .NET host build. The pinned Studio source `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822` documents the order in `.github/workflows/pr.yml` lines 29-61 and `.github/workflows/packages.yml` lines 77-100: Node 22; restore `Bpmn.Model`; install Designer dependencies; run `npm run check:generated` and `npm test`; build Designer; install/build DomInterop; then build and test the .NET solution. In the mapped Core tree, the source paths are under `src/studio`:

```sh
cd src/studio/modules/Elsa.Studio.Workflows.Designer/ClientLib
npm install --force
npm run check:generated
npm test
npm run build

cd ../../../framework/Elsa.Studio.DomInterop/ClientLib
npm install --force
npm run build
```

The Designer build emits `designer.entry.js`, `react-designer.entry.js`, and the designer CSS files to its ignored parent `wwwroot`. DomInterop emits `dom.entry.js`, `clipboard.entry.js`, and `files.entry.js`. Both asset families are runtime dependencies: a DomInterop-only build can leave the designer canvas blank even when the toolbox renders.

The pinned `generate-bpmn-types.js` assumes the standalone Studio layout (`src/modules/...`) when locating `Directory.Packages.props`. In the mapped Core layout (`src/studio/modules/...`), its current five-parent lookup lands at Core's `src/Directory.Packages.props` and fails. For the 2026-09-24 runtime proof only, a one-line temporary adjustment reduced that lookup by one parent so it resolved `src/studio/Directory.Packages.props`; the generated-type check against `Bpmn.Model 0.2.0` passed, the original script was restored byte-for-byte, and no source change was retained in the runtime clone. The unmodified generated check and `npm run build` were observed to fail with this layout error. Do not report them as passing. The consolidation build needs a durable root-discovery fix and must invoke both ClientLib pipelines before packaging mapped Studio projects; Core's current package workflow has no `npm` ClientLib steps. Keep this layout-specific fix separate from the private runtime proof.

Use an isolated npm cache and user configuration, and save each generated lockfile, exact Node/npm versions, package versions, and build logs in the private fixture. The 2026-09-24 local repair used Node 25.8.0/npm 11.11.0 for DomInterop and Node 22.22.1/npm 10.9.4 for Designer; it therefore does not establish a single pinned Node toolchain for the future CI step. After both bundles are built, restore the Studio host and its project-reference graph inside the clone, then run a normal incremental net10 build with project references enabled so every static-assets manifest uses clone-local source roots. The 2026-09-24 receipt records 44 content roots (38 clone-local and 6 NuGet), zero original rehearsal roots, and 408 manifest assets. The Workbench host's build uses `BuildProjectReferences=false`; do not apply that shortcut to Studio because it can leave client assets out of the runtime manifest. Start Studio as a separate process bound only to `http://127.0.0.1:<studio-port>`, with the private environment and no extra credentials. If HTTP redirects or a host attempts an unexpected external connection, stop and capture the logs rather than weakening middleware or broadening network access. In a browser, authenticate through Studio and verify Secrets directly. Record UI evidence separately from the API result; direct routes do not prove the navigation menu is wired.

## Two-tenant Workbench and Studio proof

The default preparation above remains single-tenant. The `--two-tenant` mode is an opt-in fixture-only proof: it writes a shared fresh SQLite path, two synthetic tenant administrators, one synthetic denied user, tenant-specific host mappings, a random signing/encryption key, and private credentials. It does not add a provider or change the production Workbench defaults.

Workbench currently declares `const bool useMultitenancy = false`, so appsettings cannot enable the tenant middleware. In a fresh disposable mapped rehearsal, apply both reviewed fixture patches from the Core checkout. The first is the merged #8332 Studio menu compatibility patch; the second changes only the Workbench compile-time switch into a configuration lookup whose default remains disabled:

```sh
git -C "$MAPPED_REHEARSAL" apply --check "$CORE_CHECKOUT/scripts/integration-program/consolidated-build/studio-secrets-menu.patch"
git -C "$MAPPED_REHEARSAL" apply "$CORE_CHECKOUT/scripts/integration-program/consolidated-build/studio-secrets-menu.patch"
git -C "$MAPPED_REHEARSAL" apply --check "$CORE_CHECKOUT/scripts/integration-program/consolidated-build/workbench-two-tenant-multitenancy.patch"
git -C "$MAPPED_REHEARSAL" apply "$CORE_CHECKOUT/scripts/integration-program/consolidated-build/workbench-two-tenant-multitenancy.patch"
```

The preparer accepts these changes only when every patch target is present and the exact patch reverses cleanly. It records both SHA-256 values and the replay ledger in `launch-plan.json`. Do not apply these patches to upstream trees or a shared checkout. Run the regular Studio layout patch and ClientLib build from #8334 in this disposable source as well:

```sh
git -C "$MAPPED_REHEARSAL" apply --check "$CORE_CHECKOUT/scripts/integration-program/consolidated-build/studio-bpmn-generator-layout.patch"
git -C "$MAPPED_REHEARSAL" apply "$CORE_CHECKOUT/scripts/integration-program/consolidated-build/studio-bpmn-generator-layout.patch"
```

Use the exact three source pins and fresh selected-Core mapped rehearsal for preparation:

```sh
TMPDIR=/private/tmp python3 "$CORE_CHECKOUT/scripts/integration-program/prepare_workbench_secrets_runtime.py" \
  --rehearsal-root "$MAPPED_REHEARSAL" \
  --core-sha <40-character-core-sha> \
  --extensions-sha <40-character-extensions-sha> \
  --studio-sha <40-character-studio-sha> \
  --temp-parent /private/tmp \
  --two-tenant
```

After the Workbench fixture has been prepared, build both Studio browser bundles with the pinned Node 22 lane. This emits the required Designer and DomInterop assets for the Studio host build:

```sh
bash "$CORE_CHECKOUT/scripts/integration-program/build_studio_clientlibs.sh" "$MAPPED_REHEARSAL"
```

Review the generated plan and private `content-root/appsettings.json` before launch. It enables `Features:Multitenancy:Enabled` only in this fixture. `tenant-a` maps to `127.0.0.1:<workbench-port>` and `tenant-b` maps to `tenant-b.localhost:<workbench-port>`; `AllowedHosts` includes both. The configuration-backed ElsaIdentity users carry matching `TenantId` claims and each administrator role is scoped to its tenant. Host resolution selects a tenant before login and the token claim preserves that selection after login. Both tenants point to the same new SQLite database so success demonstrates tenant scoping rather than separate-database isolation. `synthetic-denied` has no roles and is scoped to tenant A.

Start Workbench with the generated `launch-command.txt`. Build the mapped `Elsa.Studio.Host.Server` with its normal project references after the ClientLib script has emitted all six assets. Create two private Studio content roots and two local override files, on separate ports so each host can use its own backend URL. Point tenant A at `http://127.0.0.1:<workbench-port>/elsa/api` and tenant B at `http://tenant-b.localhost:<workbench-port>/elsa/api`. Bind both Studio processes to loopback. Open the two Studio origins in separate temporary browser profiles and record each profile's identity before the switch-back check. TCP ports alone do not isolate cookies for the same host; tabs in one profile are insufficient proof of independent sessions. Use a third temporary profile for `synthetic-denied` or sign out of A only after the A switch-back check.

In Studio, sign in as tenant A, create a secret using a technical name that will also be used in tenant B, and create one tenant-A-only secret name. Confirm the A list and picker show only A data. In the second Studio origin, sign in as tenant B and create the same technical name with distinct synthetic metadata/value. Confirm that the operation succeeds, the B list and picker show only B data, and a request for the tenant-A-only name returns no metadata. Switch back to A and confirm its metadata is unchanged. Sign in as `synthetic-denied` and record list/detail denial and the Create control state; server authorization remains the security boundary even if the control is visible.

Create one unpublished draft per tenant, select that tenant's same-name secret through the workflow expression picker, save, and inspect the stored references. Confirm each reference has the expected `{name,typeName}` shape, no plaintext appears in responses, workflow payloads, logs, database/WAL/SHM files, and neither workflow is published or executed. Record actual host identity/membership, tenant host mapping, browser actions, permission statuses, exact patch/source/asset hashes, cleanup, and all limitations in the sanitized evidence receipt. Stop all three processes before running the ownership-guarded cleanup command. A browser check with this temporary patch still does not satisfy the eventual history-preserving import rerun.

The #8334 ClientLib command requires Node 22 and normal .NET restore access; the mapped Studio generator fix remains an import-time patch. The fresh schema/startup/scheduler behavior of enabling Workbench tenancy has not yet been run and must be observed in the fixture. Do not claim browser or imported-history validation until those steps actually pass. This fixture proves only synthetic local tenants and does not cover provider accounts, production readiness, package publication, or cutover.
