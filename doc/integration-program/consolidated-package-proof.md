# Consolidated package proof

[Issue #8626](https://github.com/elsa-workflows/elsa-core/issues/8626) prepares the first consolidated 3.10 package set. It follows the [accepted lockstep and publisher-cutover decision](../adr/2026-09-28-lockstep-consolidated-release-and-publisher-cutover.md). This is a nonpublishing proof; package-feed publication and live source-publisher cutover require their separate manual approval.

Automatic proof runs cover tooling and package/build configuration changes. Source-changing release candidates require an explicit exact-head proof run before release; the expensive rehearsal is not a runtime behavior test for every source edit.

Normal builds retain imported Extensions and Studio `IsPackable=false`. The explicit `ConsolidatedPackageProof=true` MSBuild property skips only those subtree-wide assignments. Project-specific exclusions remain active, including the five retired Extensions Secrets projects and the unfinished Connections lifecycle packages. It never sets `IsPackable=true` globally. The proof validates a `3.10.0-proof.<positive run>.<positive attempt>` version before packing; stable and preview release versions are rejected.

Run from a clean, committed, isolated Core checkout with the required SDKs, runtimes and Node 22 installed:

```sh
python3 -m unittest discover -s scripts/integration-program -p 'test_prove_consolidated*.py'
python3 scripts/integration-program/prove_consolidated_packages.py \
  --version 3.10.0-proof.1.1 --output /tmp/elsa-consolidated-proof-1
```

The output directory must be new and outside the checkout. The canonical `packages/` directory must be empty before the run. The script refuses stale artifacts rather than deleting them. `--inventory-only` evaluates the solution without compilation or packing. Add `--remote-sources` only after the exact commit is available at the Core GitHub remote; the read-only [artifact workflow](../../.github/workflows/prove-consolidated-packages.yml) performs this stronger check against the exact PR head.

The script evaluates each canonical solution project with normal and proof settings and records each framework's package version and assembly name. It builds the Studio ClientLibs using the reviewed lockfiles, records inputs and six required browser outputs, then uses the existing NUKE `Compile+Pack` target in Release with identical global Version/PackageVersion values for restore and compile. Packed static-web-asset bytes must match the browser outputs. It verifies the exact package and symbol inventory. Missing, duplicate, unexpected, retired or unsupported framework artifacts fail. The SDK `_GetRestoreProjectStyle;GenerateNuspec` sequence initializes package style and writes fresh main/symbol nuspecs with `NoBuild=true` and `ContinuePackingAfterGeneratingNuspec=false`; any package output from this metadata-only stage fails. Fresh SDK nuspec output under the same source/configuration defines the complete expected dependency groups, ranges and asset semantics; archived nuspecs must match. This proves SDK metadata/archive fidelity, not independent correctness of NuGet resolution. Restore-assets and staged-nuspec hashes are retained. Nuspec identity, internal dependency closure, repository URL and SHA, project URL, Core icon bytes, framework assemblies and file hashes are checked. External `Elsa.Platform.PackageManifest` tooling is explicitly distinguished from solution-produced packages; other unavailable Elsa dependencies fail.

Packed generated `elsa-package.json` identity, version and framework lists are checked where manifest inclusion is enabled, and assembly informational versions must identify the proof version and exact commit. Every external Portable PDB must actually match its packaged assembly. Every tracked document checksum is compared with its exact Git blob. Remote mode also fetches and checks each exact-head SourceLink URL, caching bytes within the run while checking each PDB document separately. Embedded generated documents have a narrow, explicit policy, checksum verification and separate coverage accounting; embedded data never substitutes for a claimed remote fetch. Local mode reports that remote URL availability remains unverified.

Fresh-cache package-only consumers exercise representative Core, Extensions and Studio surfaces and the real WorkflowContexts API/client descriptor handshake. They use exact proof artifacts, reject source-project fallback and verify restored internal package provenance. This establishes representative consumer coverage and package closure, not behavioral certification of every package.

`inventory.json`, `verified-artifacts.json`, `source-provenance.json`, command logs, consumer evidence and `receipt.json` retain the result. A receipt is written only after all checks succeed and the source is still the same clean commit. No step has publisher credentials, pushes a package, deploys a host, disables a source publisher or changes package ownership. The normal production Packages workflow never opts in. npm package proof and live Slack credential certification remain separate work.

The earlier bounded Slack proof [#8260](https://github.com/elsa-workflows/elsa-core/issues/8260) and Studio Core provenance proof [#8443](https://github.com/elsa-workflows/elsa-core/issues/8443) are completed prerequisites. Their individual scripts and historical package-scoped handoff records remain evidence for those bounded snapshots; the accepted lockstep ADR governs the full 3.10 publisher cutover.

## Pinned external compiler content sources

The real `Elsa.Common` ShellFeatures smoke artifact emitted seven embedded executable `Generator.Hints` documents from the manifest generator cache. These are external compiler content sources, not Core Git documents. The verifier requires their actual restored package identity, an exact pinned archive hash, the audited nine `contentFiles/cs/any/Elsa.Platform.PackageManifest.Generator.Hints/*.cs` payloads, and matching PDB and embedded checksums. It reports separate external coverage and never claims these files were fetched from Core SourceLink URLs. Other untracked/cache sources remain unsupported and fail.

Both archives were independently downloaded on 2026-10-06 from the recorded [official Elsa feed](https://f.feedz.io/elsa-workflows/elsa-3/nuget/index.json) and matched the restore cache byte for byte:

| Generator version | Official archive SHA-256 |
| --- | --- |
| [0.0.1-preview.50](https://f.feedz.io/elsa-workflows/elsa-3/nuget/v3/packages/elsa.platform.packagemanifest.generator/0.0.1-preview.50/elsa.platform.packagemanifest.generator.0.0.1-preview.50.nupkg) | `56310f3c6606c793bce875f0dee5746dc5f42721d0cbbfde5fa3c4b61e6f15aa` |
| [0.0.1-preview.53](https://f.feedz.io/elsa-workflows/elsa-3/nuget/v3/packages/elsa.platform.packagemanifest.generator/0.0.1-preview.53/elsa.platform.packagemanifest.generator.0.0.1-preview.53.nupkg) | `ba9b6c28e11eec6f6c595ebf328da1b925dbee9ca2590aa5b6fa1ec4c2052780` |

Changing these pins requires another archive/source audit. This allowance does not enable Feedz fallback for clean runtime consumers; compiler tooling is private and the consumer lane continues to use only the proof feed and NuGet.org.
