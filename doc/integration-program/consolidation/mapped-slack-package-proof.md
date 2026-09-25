# Mapped-source Elsa.Slack package proof

This is a local-only artifact and consumer proof for the source-mapped consolidation rehearsal. It does not publish a package or prove final import readiness. It is distinct from the released 3.8.4 package compatibility proof in [Slack package proof](../slack-package-proof.md).

## Exact release unit and source boundary

- Release unit: `Elsa.Slack` only, described by the machine-validated [`release-units.json`](../release-units.json) manifest.
- Package project: `src/extensions/communication/Elsa.Slack/Elsa.Slack.csproj` in the prepared rehearsal.
- Source project: `src/modules/communication/Elsa.Slack/Elsa.Slack.csproj` in Extensions.
- Core, Extensions and Studio source pins: `8e893e02c4ac089d526b0a0d294a8546f021d072`, `33fa0bfd28c7585240e3d4f665058c067b17e287` and `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`.
- Rehearsal commit: `0feffeea6ca994c78bbaeb02db203e3a62078ec5`.
- Preparation patch SHA-256: `b091576a7f8d51c8b469645eea4b868556ebfadb084d7afbc4287ea36155433d`.
- Local package identity: `Elsa.Slack` `3.8.5-proof`; the Elsa dependency remains `3.8.4` and SlackNet is `0.17.7`.

The package's Elsa 3.8.4 dependency is the tested artifact compatibility baseline. The separate current-source inventory graph records Elsa `3.8.0-preview.5557` references at different pins. The mapped proof overrides the release project's dependency to 3.8.4 explicitly and does not infer or allocate an Extensions-wide release version.

Package mode evaluates exactly one `PackageReference` to Elsa and no project references. The source-debug mode evaluates a single project reference to the mapped Core `src/modules/Elsa/Elsa.csproj` and no Elsa package reference. These modes are separately evaluated; only package mode is packed and consumed by this proof.

## Retained local result

### History-bearing draft import, 2026-09-25

The `imported` runner profile completed against a fresh GitHub checkout of [draft import PR #8409](https://github.com/elsa-workflows/elsa-core/pull/8409) at `7abe24b76295c6f64fbb6c878962bf0367ebd8fe`. The runner checked that the imported head preserves Core `e96fd36f4f9a838c6289fd48cf07669ea3229a5e`, Extensions `ba8b71d91c15ffe5be4b2c539cf9f712e74af775`, and Studio `20ceaeeed7e671f0c9662003e82063026f2216de` ancestry. Its E96 import and preparation receipts matched their reviewed SHA-256 values. All 41 mapped Slack C# files matched the pinned Extensions checkout byte for byte before and after the proof.

The runner now requires that exact reviewed import HEAD before packing. A clean descendant may change project files or other build inputs without changing the 41 C# files, so it requires a new review and a new proof pin before it can support a release decision. The receipt below proves the recorded HEAD only.

Only `Elsa.Slack` `3.8.5-proof` was packed into an isolated local feed. The `.nupkg` SHA-256 is `311c98e26e6821cc9947f618772aa1cd6f70a2a70895b5d01bc3e329f73f682b`; its `.snupkg` is `f55b03e00acdf7849ee0b51ae6acf9855a771a77680193dd36cc4192087bbd94`. Its nuspec names repository `elsa-core` at that exact imported commit, Elsa `3.8.4` and SlackNet `0.17.7` on net8.0, net9.0 and net10.0. Package mode evaluated one Elsa package reference and no project references; source-debug mode evaluated one mapped Core project reference and no Elsa package reference. The pinned SourceLink 3.1.1 tool checked each packaged PDB's `elsa-core/7abe24b...` URL and source checksum on all three frameworks; all 41 C# files were also byte-verified from each symbol PDB against the original Extensions source.

Three isolated package-only consumers restored from this exact local archive and started on net8.0, net9.0 and net10.0. The offline fake-client `CreateChannel` smoke passed. The declared upstream Slack test still has one `Not implemented yet` skip and zero executed assertions; the fake-client smoke is separate narrow behavior evidence. The runner's inventory selector maps 51 affected projects but its selector receipt is a plan, not proof that those tests executed. The separate [E96 canonical test evidence](current-tip-e96-evidence/README.md) covers the older synthetic rehearsal; the exact history-import head's full test run is tracked on PR #8409. The local proof version is not a release allocation, no feed received a package, and one-publisher/cutover approval remains open.

The full local receipt is `/private/tmp/elsa-8260-imported-package-proof-result-7abe/evidence.json` (SHA-256 `8bbc744b8ce2a9d1937c5f47a8741c989fc8b88a39dec51dcb34bd516aab4c20`). To rerun, use a fresh full-history import checkout with GitHub `origin` at the exact head, clean full-history upstream checkouts at the three commits above, and a new absent output directory:

```sh
python3 scripts/integration-program/run_mapped_slack_package_proof.py \
  --rehearsal /path/to/imported-elsa-core --core-source /path/to/core-at-e96 \
  --extensions-source /path/to/extensions-at-ba8b --studio-source /path/to/studio-at-20cea \
  --source-profile imported --sourcelink-tool /path/to/pinned/sourcelink \
  --output-dir /path/to/new-local-proof-output
```

The `--sourcelink-tool` must be the installed, payload-verified 3.1.1 CLI described in [Slack package proof](../slack-package-proof.md). Rollback is removing the disposable import and output checkouts; the upstream sources, public feeds and release versions were unchanged.

### Current imported-source profile, 2026-09-25

The runner also completed against the later reviewed import profile: Core `c4b3ce150160e3c9062b57f7b158fd6b968e1631`, Extensions `ba8b71d91c15ffe5be4b2c539cf9f712e74af775`, and Studio `20ceaeeed7e671f0c9662003e82063026f2216de`. The disposable import commit was `668a10dda46000c90a87bc7d35758420179b6c0c`; its preparer receipt records the same source pins and patch SHA-256 `b091576a7f8d51c8b469645eea4b868556ebfadb084d7afbc4287ea36155433d`. The runner's `--source-profile prepared` option accepts only source maps already reviewed by `prepare_consolidated_build.py` and checks both receipts and all three clean source checkouts before and after the proof. The default `manifest` profile retains the original pins above for existing automation.

The local receipt is `/private/tmp/elsa-8260-current-tip-local-proof-3/evidence.json`; a separate retained-file provenance recheck is `/private/tmp/elsa-8260-current-tip-local-proof-3-provenance-recheck.json`. This final run includes the committed source-ancestry and relocated-tree guard. The local feed contains only `Elsa.Slack` `3.8.5-proof` and its symbols. Their SHA-256 values are `52abf97d8521c67452860e5f33d5935ab96f932a6e7a2540981f1e2a9dab5e20` and `0fe898f26ca3be684d9983ffd437099c5db436116d079313e5d224ef3ad0dd7d`. The nuspec retains Elsa `3.8.4` and SlackNet `0.17.7` dependencies on net8.0, net9.0, and net10.0. Package mode had one Elsa package reference and no project references; source-debug mode had one mapped Core project reference and no Elsa package reference. Three isolated package-only consumers passed, as did the offline fake-client CreateChannel smoke. All 41 Slack C# files embedded in each framework's symbol PDB matched the pinned Extensions source bytes. The independent provenance replay passed for all four consumers.

The declared upstream `CreateChannelTests.ExecuteAsync` still has one known `NotExecuted` skip and zero passing tests; the fake-client smoke provides separate narrow behavior evidence. The synthetic import has no remote, so final Core SourceLink URLs remain unverified. The selector's 51 affected inputs are a plan, not an executed dependency closure. This local-only proof did not publish a package (`publication_authorized: false`) or allocate a real `3.8.5` release.

To repeat this profile from clean full-history checkouts at those three commits, use fresh disposable rehearsal and output directories:

```sh
python3 scripts/integration-program/rehearse-import.py \
  --core /path/to/elsa-core --extensions /path/to/elsa-extensions \
  --studio /path/to/elsa-studio --source-profile current-tip \
  --output /tmp/elsa-slack-current-rehearsal
git -C /tmp/elsa-slack-current-rehearsal switch --quiet --detach rehearsal
git -C /tmp/elsa-slack-current-rehearsal reset --hard --quiet HEAD
python3 scripts/integration-program/prepare_consolidated_build.py \
  --rehearsal /tmp/elsa-slack-current-rehearsal
python3 scripts/integration-program/run_mapped_slack_package_proof.py \
  --rehearsal /tmp/elsa-slack-current-rehearsal \
  --core-source /path/to/elsa-core --extensions-source /path/to/elsa-extensions \
  --studio-source /path/to/elsa-studio --source-profile prepared \
  --output-dir /tmp/elsa-slack-current-proof
```

The CLI derives the exact source pins from the import receipt only after checking they belong to a preparer-reviewed profile, then verifies the rehearsal commit's source parents and relocated tree. Rollback is removal of these disposable rehearsal/output directories and use of the unchanged default `manifest` profile; no release state or remote feed was changed.

### Original manifest profile

The completed receipt and logs are at `/private/tmp/elsa-8260-slack-package-final-proof-3/evidence.json` and the adjacent `logs/` directory. Earlier attempts and their failure logs are retained in its `attempts/` directory. The completed run records `publication_authorized: false`.

The final `.nupkg` SHA-256 is `3303615cbb228d4819be937708b45eff3fb0caecaff6b272308c8b47d1ec9baf`; the `.snupkg` SHA-256 is `7f8984f2b9b78ce15c0f245679f8817b0cd1f4d606f05ff7ab3f293194c7fda5`. An earlier successful proof attempt with the same source pins is retained separately under `attempts/` and has a different package hash; the final receipt and artifact hashes above are authoritative. The package contains its canonical root icon (`icon.png`, SHA-256 `82fd76d734d59efc6132af0b0b999146254fa5a296ea5d64f85597bb1cda524e`) and `lib` assemblies/XML documentation for net8.0, net9.0 and net10.0. The `.nuspec` names only Elsa 3.8.4 and SlackNet 0.17.7 on all three target frameworks and records the pinned Extensions repository and source commit.

Three consumer projects restored using NuGet.org and the isolated local feed, then started on net8.0, net9.0 and net10.0. Each registered `Elsa.Slack.Channels.CreateChannel`, version 1, through the Elsa activity registry. A separate net10.0 consumer invoked `CreateChannel` through a deterministic fake Slack client. It asserted the channel name, private flag and team ID in the request, the returned channel ID/name, and exactly one `Conversations.Create` call.

For each consumer and the offline smoke, the runner checks that `project.assets.json` selected exactly this package ID/version, NuGet used only that consumer's private package cache, `.nupkg.metadata` identifies the proof's local feed, and NuGet's SHA-512 agrees with the exact local `.nupkg`. It also byte-compares the consumer's output assembly with the assembly inside that package. The manual job replays these checks from retained files and uploads a separate recheck receipt; it does not restore or rebuild during that replay.

The source test project also ran on net10.0. Its retained TRX records exactly one `NotExecuted` result, `CreateChannelTests.ExecuteAsync`, skipped as `Not implemented yet.`, with zero executed or passed tests and no failure counters. This is an incomplete upstream test gate. The separate fake-client smoke is narrow behavior evidence and does not turn the skipped test into a pass.

For each TFM, all 41 embedded Slack C# files in the symbol package were decompressed and byte-compared with the pinned Extensions project. The synthetic rehearsal has no remote, so its PDBs contain no SourceLink URLs. This embedded-source check does not prove final Core-repository SourceLink URLs; that remains a history-import gate.

The selector receipt uses the separate 2026-09-23 inventory graph, whose recorded Core/Extensions/Studio commits are `610790ec57ae9d5c334181d50c1e65f99613fd86`, `33fa0bfd28c7585240e3d4f665058c067b17e287` and `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`; the inventory SHA-256 is `297569201d879f4ef14943a3d30631912d4803d1e24e69327c6beb6e30fa7842`. These are the impact-selection graph pins. The packed source rehearsal uses its separately recorded Core `8e893e02c4ac089d526b0a0d294a8546f021d072`, Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287` and Studio `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822` pins. The selector chooses `Elsa.Slack` as the sole package for the requested release unit and records 51 affected test/build inputs for a Core `Elsa.csproj` change, including the Slack test project. The updated runner adds their exact import-mapped paths, frameworks, and pin comparisons as a plan-only receipt; those 51 inputs were not executed by this artifact proof. Dependency-aware closure execution remains incomplete as documented in [package closure](../package-closure.md).

## Reproduce

Use clean full-history checkouts at the three exact commits above and a new disposable rehearsal and output directory:

```sh
python3 scripts/integration-program/rehearse-import.py \
  --core /path/to/elsa-core \
  --extensions /path/to/elsa-extensions \
  --studio /path/to/elsa-studio \
  --output /tmp/elsa-slack-mapped-rehearsal
git -C /tmp/elsa-slack-mapped-rehearsal switch --quiet --detach rehearsal
git -C /tmp/elsa-slack-mapped-rehearsal reset --hard --quiet HEAD
python3 scripts/integration-program/prepare_consolidated_build.py \
  --rehearsal /tmp/elsa-slack-mapped-rehearsal
python3 scripts/integration-program/run_mapped_slack_package_proof.py \
  --rehearsal /tmp/elsa-slack-mapped-rehearsal \
  --core-source /path/to/elsa-core \
  --extensions-source /path/to/elsa-extensions \
  --studio-source /path/to/elsa-studio \
  --output-dir /tmp/elsa-slack-mapped-proof
python3 scripts/integration-program/verify_mapped_slack_consumer_provenance.py \
  --evidence /tmp/elsa-slack-mapped-proof/evidence.json \
  --output /private/tmp/elsa-slack-consumer-provenance-recheck.json
python3 scripts/integration-program/create_mapped_slack_portable_bundle.py \
  --evidence /tmp/elsa-slack-mapped-proof/evidence.json \
  --output /tmp/elsa-slack-portable-proof.tar.gz
```

The runner rejects wrong or dirty source pins, mismatched rehearsal receipts or patch hashes, changed prepared inputs, output aliases into inspected trees, an unrelated package in the local feed, an unexpected evaluated reference mode, a mismatched `.nuspec`, a changed icon or embedded source, an invalid activity descriptor, or a changed test baseline. Its builds write ignored `bin/` and `obj/` files only inside the disposable rehearsal; proof logs, caches, consumers, local packages and receipts stay in the new output directory. The provenance recheck reads the retained evidence, private caches and consumer outputs only. Its new receipt must be outside the retained proof directory; it does not restore or rebuild.

The `Package impact closure rehearsal` runs its `mapped-source-artifact-proof` job on relevant pull requests and on manual dispatch. For a pull request, its primary checkout is the exact PR head SHA. The job checks out the pinned proof sources with full history, creates a disposable preparation, runs this proof, replays the consumer provenance checks from retained files, then extracts and rechecks a portable provenance bundle before retaining it for 14 days. It uploads the bundle only after that recheck passes. The bundle contains only the exact local `.nupkg`, four consumer restore assets and output assemblies, their four isolated `Elsa.Slack` cache records, and the evidence receipt. It excludes shared caches and unrelated build outputs. After extracting it into any new directory, rerun `verify_mapped_slack_consumer_provenance.py --evidence <new-directory>/evidence.json --output <separate-output>.json`. The verifier maps the original runner path recorded in `proof_root` to the extracted location while checking the original NuGet cache/feed paths and the exact package and assembly bytes. Older receipts without `proof_root` remain checkable only in place; the bundle creator rejects them. The workflow grants read-only repository permission and configures no publish credential or push step.

On 2026-09-24, [manual run 35984242622](https://github.com/elsa-workflows/elsa-core/actions/runs/35984242622) passed its mapped-source artifact job and uploaded the bundle. An independent macOS download and extraction rechecked all four consumers against its local nupkg SHA-256 `836f3fb63a26348ee118119d9f26135c532c4cfd0c92cfabe77834d5347156c0`. That cross-machine recheck exposed a path-normalization defect: resolving the absent Linux runner root on macOS changed the expected feed path. The verifier now compares the recorded absolute path lexically while resolving only files that exist in the extracted bundle. The same manual run's separate pinned-source closure job failed its source-binding preflight; this artifact result does not complete the 51-project dependency closure.

The PR selector receipt separately proves Slack-only package selection and shared-Core test-impact expansion. The package artifact still comes from a disposable mapped rehearsal, not the final canonical imported repository history; final remote SourceLink validation and the four known baseline skips remain open.
