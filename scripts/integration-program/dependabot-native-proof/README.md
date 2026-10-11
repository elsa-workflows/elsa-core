# Temporary #8636 native discovery preflight

This proof-only branch adds `native_discovery` to the existing Integration program
tools workflow. Dispatching it as true runs only the diagnostic job. Ordinary PR
checks and a dispatch with false retain their existing behavior. Independent review
is required before the lead pushes or dispatches this branch. Delete these helpers
and the opt-in job after #8636 native inventory evidence is accepted and retained.

There are no source, SDK, target or experiment overrides. The target is exactly
`c0f470853bfa8fbf9c3266df75da48cb859f9799`, tree
`51aa6e0938cd24732d8a285f701dc6b614b63a69`. The runner creates a Git archive and
records its SHA-256 and all initial content hashes as one aggregate, then joins the
actual 379-project census, 355 solution entries and one retained testing project.
Helpers are outside that archive and do not enter its census. Changed tracked
snapshot files are reported after discovery.

The upstream worker is dependabot/dependabot-core at
`9c06d60057ba9e7e79210e6618f932a28cf6a158`, with NuGet.Client
`626dc64e95d6f5bc366cbbe191a15751ccbfb097` and dotnet/core
`843c037496b2941d8bc42d0eceaabf8d15a9deff`. The isolated SDK must resolve to
10.0.400 exactly. The official CLI is built unchanged, then the thin caller is built
under the official native helper's existing MSBuild/central-package configuration.
The template suffix prevents adding a project to the Elsa tracked-project census.
The caller uses the public DiscoveryWorker output overload twice, serially, with
separate instances for `/` and `/extensions/src/Elsa.Testing.Extensions`.

This is a credential-free diagnostic. Both native experiments use false defaults;
no actual hosted job dictionary is supplied, so this does not establish hosted
updater equivalence, authenticated Feedz resolution, proposals or package delivery.
Cold official builds and 356 native evaluations can take minutes or more. The
45-minute job limit bounds runner cost; a timeout is a failed/incomplete diagnostic,
never successful coverage. No local build, restore or SDK download is needed to
review this candidate.

Native raw logs, errors and complete public JSON remain ephemeral in RUNNER_TEMP.
Only summary.json is uploaded for seven days: fixed pins, stage exit codes, hashes,
known contained relative paths, validated TFMs, centrally allowlisted dependency
IDs/version strings, statuses and stable allowlisted error types. No raw messages,
URLs, environment, configuration/credential values or user data are uploaded.
Versions are joined to the project's own central source file. A literal source
version match does not evaluate its XML condition; mismatches need review.

Every selected row receives an explicit disposition. Empty project results,
missing output, missing central imports/TFMs/dependencies, duplicate or escaping
records and workspace/project failures cannot certify completeness. The public
pinned worker itself filters failed projects from workspace Projects and retains
only its first workspace error; omissions remain visible in the full inventory join.
Additional tracked projects are listed separately. The diagnostic exits nonzero
for incomplete evidence while retaining its safe summary. Acceptance stays false
even when diagnostic metadata is complete. If setup or the job timeout prevents
summary creation, the failed job and missing artifact are themselves missing proof.

Cheap checks from repository root:

```sh
python3 -m unittest discover -s scripts/integration-program -p test_dependabot_native_proof.py
python3 -O -m unittest discover -s scripts/integration-program -p test_dependabot_native_proof.py
actionlint .github/workflows/integration-program-tools.yml
```

The tests use synthetic native records, not native execution. Root owns exact-head
independent review and any subsequent hosted execution or delivery decision.
