# Extension activity compatibility before source import

Program #8194, task #8313. This is an offline compatibility probe against the recorded consolidated source rehearsal. It does not execute connector operations, select pilot integrations, publish packages, or complete the actual source import.

The probe covers 18 Extensions assemblies containing static activity classes. Sixteen have an exact NuGet 3.8.4 baseline. The public NuGet flat-container endpoints for `Elsa.Ldap` and `Elsa.Mqtt` returned 404 when checked on 2026-09-24; their nine current descriptors have no released-package comparison in this run. The [matrix](../../../scripts/integration-program/activity-compatibility/matrix.json) records every project/package pair. NuGet restore uses only nuget.org and the local package cache; this is not a fresh-cache or feed-signature attestation.

## Results and boundaries

The reviewed run covers `net8.0`, `net9.0` and `net10.0`. All six hosts build with zero errors; each released host has zero warnings and each source host has 92 warnings. Every framework verifies unchanged hashes for 7,694 source/build input files and rejects all five negative probes. Each overall runner exits 1 because of the four baseline failures described below. Build warnings and unexecuted provider behavior are not a runtime or security approval.

The baseline exposes 118 concrete descriptors; the mapped source exposes 127. No released descriptor is missing or has a changed activity type/version, kind, input/output contract or port contract. Raw CLR generic type strings retain assembly build versions in the receipts; the comparison key omits only assembly version and retains assembly name, culture and public-key token. The unversioned development build is not a final public package-version proof.

For each host, the probe constructs activities without executing them, configures string, integer, boolean and list-of-string inputs with synthetic values, and verifies serialization, actual deserialized CLR type, ID, activity type/version and typed literal values. It retains all failures. Thirteen abstract base classes are reported as exclusions. Constructors marked `JsonConstructor` can be used when all parameters have defaults; no arbitrary service-required constructor is guessed.

The released workflow contains 114 successfully round-tripped activities; the source workflow contains 123. Both the release's first-write workflow JSON and its canonical serialized form are imported into the current host. The resulting actual activity contracts and typed inputs are compared with the released contracts, and canonical JSON is compared field-for-field. No unknown-activity fallback can count as a pass solely because it preserved JSON.

Four GitHub activities fail the same-host identity round trip in both hosts: `Comments.DeleteComment`, `Comments.GetComment`, `Comments.UpdateComment` and `Gists.GetGist`. Their `Input<T> Id` member hides the workflow's `IActivity.Id`, causing the serialized `id` property to represent a provider input instead of preserving the workflow ID. These activities remain in descriptor and failure evidence but cannot enter the successful workflow fixture. **Both host probes and the overall runner return a nonzero exit code.** There is no baseline-failure allowlist and no whole-module compatibility pass.

The Telnyx `LookupNumber.Types` literal illustrates a representation-only difference: the serializer converts a list JSON value to a JSON-encoded string. The actual Literal expression evaluator returns the same typed list before and after. The probe normalizes only known Literal expression values after typed evaluation; expression kinds, declared input types, metadata and surrounding JSON stay in the comparison. It evaluates no non-literal expression.

Configuration-generated identities from Agents, MassTransit, Orchard content types and Telnyx webhook providers are not established merely by describing their static template classes. Their configured descriptors and workflow instances require additional fixtures before #8313 is complete. Unconfigured input types, provider behavior, external permissions, webhooks, durable execution and a production workflow corpus are also outside this fixture.

## Repeat the probe

Prepare the mapped source using [the current-Core rehearsal](current-core-build.md) and its recorded compatibility artifacts. This run additionally contains the test-only [Studio path fix](studio-test-layout.md). The source baseline is Core `076f022cc174d497af26fc8e26414970e61a79b1`, Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287`, Studio `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`; it is not a claim about later Core commits or the eventual import branch.

```sh
python3 scripts/integration-program/activity-compatibility/run.py \
  --source-tree /path/to/prepared-rehearsal \
  --output /tmp/activity-proof-net10-new --framework net10.0
python3 scripts/integration-program/activity-compatibility/verify_probe.py \
  --evidence /tmp/activity-proof-net10-new \
  --output /tmp/activity-negative-net10-new
python3 -m unittest discover \
  -s scripts/integration-program/activity-compatibility -p 'test_*.py'
```

Output paths must be new and outside the source tree. Run one source build at a time; different proof hosts still share referenced-project output directories. SDK 10.0.300 is pinned. Use `--framework net8.0` and `--framework net9.0` for the other target frameworks. Source inputs are hashed before and after each run, builds disable packaging, and package dependency versions/content hashes are recorded from restore assets. A changed source file fails the runner.

The negative probes mutate an activity type, ID, version, literal input and first-write-only ID. Every case must fail the historical import check, while the unmodified fixture must pass that check first. Seven Python comparison tests cover assembly build-version normalization, retained strong-name/contract changes, retained activity identities/ports, duplicate CLR-name rejection, and rejection of added descriptors outside the matrix's explicitly source-only assemblies; normal and optimized Python execution both pass.

The [evidence directory](activity-compatibility-evidence/) retains sanitized host results, source input hashes, build/probe logs and a summary with original and sanitized file hashes. Sanitization replaces machine-local paths only. Failed exploratory fixtures are not substituted for the reviewed run, and known baseline failures are not relabeled as passes. Final imported-source CI and runtime verification remain required.

The added-descriptor guard was tightened after review. [The comparison replay](activity-compatibility-evidence/added-descriptor-review.json) verifies the new Python guard against the unchanged, hash-checked C# evidence for all three frameworks: only the nine descriptors in explicitly source-only LDAP/MQTT assemblies are allowed additions. New descriptors in released or unlisted assemblies fail the gate. The original run receipts retain their original harness hashes; no .NET rerun is claimed for this comparison-only change.
