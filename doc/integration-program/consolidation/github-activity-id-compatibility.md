# GitHub activity provider IDs and Elsa activity IDs

Program #8194, task #8325. This proof preserves the four released GitHub activity contracts and introduces explicit version 2 classes whose provider IDs no longer occupy Elsa's activity `id` field. The history-preserving source import and release remain separate open gates.

The source patch is [extensions-v2.patch](../../../scripts/integration-program/github-activity-id-compatibility/extensions-v2.patch). It applies to Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287`; the focused build references Core `6f493809eae0e1652ca185b901a982984f4ef799`. Existing version-1 classes are untouched. Each V2 class explicitly declares the same logical activity type name with version 2; comment activities rename their provider input to `CommentId`, and the gist activity uses `GistId`. Core's activity-name helper, descriptor registry, and version dispatch are unchanged.

The mapping tool is deliberately narrow. It accepts one exported workflow `Root` or `root` activity and recursively follows only `Elsa.Sequence.activities`. It preserves that input casing and all unrelated JSON values. It refuses ambiguous duplicate root properties, a top-level activities list, unsupported containers or topology properties, duplicate JSON keys, unknown target versions, malformed old input wrappers, and missing, unused, duplicate, or colliding caller-supplied activity IDs. A mapping includes the exact original-file SHA-256 and a workflow label; the output is atomically created at a new path and the source is not overwritten. Decimal JSON tokens are retained without a float conversion. Arbitrary business-data objects named `type` or `id` are not traversed.

`Elsa.For` and `Elsa.StateMachine` are explicitly unsupported containers. When either appears inside a supported Sequence, the tool rejects the entire workflow before writing output, including any earlier top-level activity that it could otherwise migrate. An unrelated custom leaf with the same short name, such as `Acme.For`, remains supported if it has no nested topology. A custom container holding a serialized legacy GitHub activity in an untraversed property is also rejected; a business classification that only uses the same `type` string is not an activity. Opaque `customProperties` business data is not treated as workflow topology. An operator must use a reviewed migration for rejected container topology; a partial success receipt is not acceptable.

For a real workflow, create a mapping file such as:

```json
{
  "workflow_key": "orders-v17",
  "workflow_sha256": "<sha256 of the exact original workflow file bytes>",
  "activity_ids": {
    "/Root/activities/0": "approved-new-activity-id"
  }
}
```

Then run the migration into an output path that does not already exist:

```sh
python3 scripts/integration-program/github_activity_id_migration.py \
  --input /path/to/original-workflow.json \
  --mapping /path/to/activity-id-map.json \
  --workflow-key orders-v17 \
  --output /path/to/migrated-workflow.json
```

Use `/root/...` pointers when the source file uses a lowercase root. The operator must inspect and approve every new ID against the target workflow and any external references. The utility does not generate IDs, follow references, rewrite topology, or import a migrated workflow into a running Elsa instance. Flowchart connections and containers other than `Sequence` are rejected. This is not a general persisted-workflow migration framework.

The [fixture](../../../scripts/integration-program/github-activity-id-compatibility/fixtures/released-3.8.4-v1.json) binds its four activity objects to the released compatibility receipt by hash and verifies each serialized object against the receipt. Its WorkflowDefinitionModel root/Sequence shape is captured by the test; top-level casing follows the observed Studio CodeView representation (`Root`, `DefinitionId`, and `Name`), while activity fields stay camelCase. The test also exercises the v2 provider-input round trips with the real `IActivitySerializer`, both serializer entry points, descriptor population through `AddActivitiesFrom`, descriptor construction, and the exported root/Sequence representation.

## Reproduce the focused source proof

Use clean source checkouts at the pins in the evidence receipt. The runner creates a disposable Extensions source archive, applies the reviewed patch, and redirects the sibling Core project reference only in that disposable copy. The supplied Core checkout must have clean build inputs under `src/` and the package/build props. Builds write ignored `bin/obj` outputs into the supplied Core checkout.

The pinned Extensions project also requires `Elsa.Platform.PackageManifest.Generator` `0.0.1-preview.50`. A run with an empty NuGet cache could not restore that package from the configured feeds in this environment, although the earlier proof used an existing package cache. Supply an extracted cache folder for this one package using `--generator-cache`; the runner requires the exact nupkg SHA-256 `56310f3c6606c793bce875f0dee5746dc5f42721d0cbbfde5fa3c4b61e6f15aa`, then extracts only that verified nupkg into its own cache. It does not trust the previously extracted files in the supplied folder. Other .NET and NuGet cache locations are newly created beneath the output directory. This pins the local artifact bytes but does not independently verify its feed provenance or guarantee that every developer can download it; resolve its release provenance in #8260 before claiming a fully portable build.

```sh
python3 scripts/integration-program/run_github_activity_id_compatibility.py \
  --extensions-source /path/to/clean/elsa-extensions \
  --core-source /path/to/pinned/elsa-core \
  --generator-cache /path/to/elsa.platform.packagemanifest.generator/0.0.1-preview.50 \
  --output-dir /tmp/github-activity-id-proof-run

PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover \
  -s scripts/integration-program -p test_github_activity_id_migration.py
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover \
  -s scripts/integration-program -p test_run_github_activity_id_compatibility.py
```

The reviewed replay passed 6/6 focused .NET tests on net10.0 with isolated caches and the exact seeded generator artifact; the offline utilities passed 23 Python tests in standard and optimized modes. The receipt and raw test log/TRX are in [the evidence directory](github-activity-id-compatibility-evidence/); the receipt hashes both result files and the applied patch. The build reported the existing v1 hidden-member warnings, a Fody configuration warning, and empty SourceLink data from the Git-archive staging tree. This was a test-only source replay; no `.nupkg` was packed, no feed was published, and no automatic publisher was invoked. The warnings and absent SourceLink data are not treated as release provenance evidence.

This proof does not establish net8.0/net9.0 results, all-upstream solution tests, actual history-preserving import, SourceLink against imported Git history, behavior against production workflow documents, or provider side effects. Explicit-version v1 payloads and versionless latest-dispatch rejection are covered by focused tests; mixed legacy `WorkflowDefinitionModel` serialization, the original historical workflow corpus, and unsupported graph topologies require later imported-source validation. Do not close #8325 or claim release readiness from this mapped-source proof alone.
