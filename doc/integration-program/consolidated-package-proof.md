# Consolidated package proof

[Issue #8626](https://github.com/elsa-workflows/elsa-core/issues/8626) prepares the first consolidated 3.10 package set. It follows the [accepted lockstep and publisher-cutover decision](../adr/2026-09-28-lockstep-consolidated-release-and-publisher-cutover.md). This is a nonpublishing proof; package-feed publication and live source-publisher cutover require their separate manual approval.

Normal builds retain imported Extensions and Studio `IsPackable=false`. The explicit `ConsolidatedPackageProof=true` MSBuild property skips only those subtree-wide assignments. Project-specific exclusions remain active, including the five retired Extensions Secrets projects and the unfinished Connections lifecycle packages. It never sets `IsPackable=true` globally. The proof validates a `3.10.0-proof.<positive run>.<positive attempt>` version before packing; stable and preview release versions are rejected.

Run from a clean, committed, isolated Core checkout with the required SDKs and runtimes installed:

```sh
python3 -m unittest discover -s scripts/integration-program -p 'test_prove_consolidated*.py'
python3 scripts/integration-program/prove_consolidated_packages.py \
  --version 3.10.0-proof.1.1 --output /tmp/elsa-consolidated-proof-1
```

The output directory must be new and outside the checkout. The canonical `packages/` directory must be empty before the run. The script refuses stale artifacts rather than deleting them. `--inventory-only` evaluates the solution without compilation or packing. Add `--remote-sources` only after the exact commit is available at the Core GitHub remote; the read-only [artifact workflow](../../.github/workflows/prove-consolidated-packages.yml) performs this stronger check against the exact PR head.

The script evaluates each canonical solution project with normal and proof settings and records each framework's package version and assembly name. It then uses the existing NUKE `Compile+Pack` target with one proof version and verifies the exact package and symbol inventory. Missing, duplicate, unexpected, retired or unsupported framework artifacts fail. Nuspec identity, internal dependency closure, repository URL and SHA, project URL, Core icon bytes, framework assemblies and file hashes are checked. External `Elsa.Platform.PackageManifest` tooling is explicitly distinguished from solution-produced packages; other unavailable Elsa dependencies fail.

Every external Portable PDB must actually match its packaged assembly. Every tracked document checksum is compared with its exact Git blob. Remote mode also fetches and checks each exact-head SourceLink URL, caching bytes within the run while checking each PDB document separately. Embedded generated documents have a narrow, explicit policy, checksum verification and separate coverage accounting; embedded data never substitutes for a claimed remote fetch. Local mode reports that remote URL availability remains unverified.

Fresh-cache package-only consumers exercise representative Core, Extensions and Studio surfaces and the real WorkflowContexts API/client descriptor handshake. They use exact proof artifacts, reject source-project fallback and verify restored internal package provenance. This establishes representative consumer coverage and package closure, not behavioral certification of every package.

`inventory.json`, `verified-artifacts.json`, `source-provenance.json`, command logs, consumer evidence and `receipt.json` retain the result. A receipt is written only after all checks succeed and the source is still the same clean commit. No step has publisher credentials, pushes a package, deploys a host, disables a source publisher or changes package ownership. The normal production Packages workflow never opts in. npm package proof and live Slack credential certification remain separate work.

The earlier bounded Slack proof [#8260](https://github.com/elsa-workflows/elsa-core/issues/8260) and Studio Core provenance proof [#8443](https://github.com/elsa-workflows/elsa-core/issues/8443) are completed prerequisites. Their individual scripts and historical package-scoped handoff records remain evidence for those bounded snapshots; the accepted lockstep ADR governs the full 3.10 publisher cutover.
