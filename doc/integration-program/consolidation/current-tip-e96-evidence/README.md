# Core e96fd36 consolidated source rehearsal

Program #8194; story #8286. This is a disposable, no-remote rehearsal of the source import. It is not the canonical import, package release, or publisher cutover.

The source pins are Core `e96fd36f4f9a838c6289fd48cf07669ea3229a5e`, Extensions `ba8b71d91c15ffe5be4b2c539cf9f712e74af775`, and Studio `20ceaeeed7e671f0c9662003e82063026f2216de`. The [import receipt](import-receipt.json.gz) records synthetic commit `d5409c2cb2338166e1bccaf5df07212e5d7bf159`, with 6,439 Core files and 3,771 byte-and-mode-preserved Extensions/Studio files. All three original source commits remain reachable. The uncompressed receipt SHA-256 is `06cd198a338d5c6d49fa6b0183bbda6b602252f39f622f18084e60342880bb75`.

The [upstream refresh](upstream-pr-refresh.json) confirms that the selected commits matched each repository's `main` on 2026-09-25 at 05:51 UTC. Extensions had 18 open PRs and Studio had 15. No PR was added, removed, or changed head relative to the [previous review ledger](../current-tip-c4b3-evidence/upstream-pr-refresh.json). Refresh those sources and PRs again before opening the history-bearing import.

The reviewed source-integration patch remains SHA-256 `b091576a7f8d51c8b469645eea4b868556ebfadb084d7afbc4287ea36155433d`. The 39 Core paths changed since the [previous c4b3 rehearsal](../current-tip-c4b3-evidence/README.md) do not overlap its 185 patch targets. The [preparation receipt](consolidated-build-receipt.json.gz), uncompressed SHA-256 `219fcafe45959bb8f9295d8b9137f8ae0807489f0370b5112204f228fc7bd7bc`, records the patch application and the prepared `Elsa.sln` with 343 projects.

The [packability report](canonical-packability-report.json.gz), uncompressed SHA-256 `5b720229d7f58da91a4c62aaf19670acea6212638c013dfcc676713fba6b5d18`, evaluates 167 imported/test projects in Debug and Release, across their declared frameworks and default/source/package-reference modes. All 2,586 evaluated cases have `IsPackable=false` and `GeneratePackageOnBuild=false`. The preparation receipt deliberately retains `buildCompatibilityVerified=false`, `canonicalBuildAndTestsVerified=false`, and `publicationAuthorized=false`. The three compressed receipts were scanned for absolute local path values before inclusion.

The six [previously reviewed fixture overlays](reviewed-overlays-six.json) matched their recorded SHA-256 values and applied cleanly to this disposable tree. The receipt updates only the Core source pin from the earlier run; the six patch hashes are unchanged. They are separate from the raw history-preserving relocation and still need review for the actual import. Preparation and patch applicability alone do not prove compilation, final package dependency closure, browser behavior, SourceLink provenance, or supported Secrets upgrades.

## Canonical build, tests, and dependency closure

A clean `env -u NUKE_ENTERPRISE_TOKEN ./build.sh --host Terminal --target Test` run completed Restore, Compile, and Test in 13 minutes 32 seconds on 2026-09-25. The NUKE and retained TRX result was **6,140 passed, 147 skipped, zero failed**, across 95 selected projects and 94 retained TRX files. The [extracted test evidence](full-suite-nuke-test-evidence.json.gz), uncompressed SHA-256 `be4926862ba5457d6ee6fdb91d9611f4aa4e99045e7a654c39ad76fa2361e20a`, verifies the test selection, run window, exact source/overlay receipts, successful targets, counters, and absence of Pack, Push or Publish. The raw private log SHA-256 is `65d3c581bb885697fb4355fa50a99a9be22c5e3ad9c06ae30158e7e4556d9e28`. Compile reported zero errors and 1,570 warnings, including inherited analyzers, dependency advisories, and missing SourceLink because the synthetic commit has no remote. The public test receipt contains disposable local test paths needed to map TRX assemblies; it contains no customer evidence.

The last Slack TRX finished 0.118 seconds after NUKE's displayed `08:11:47` success timestamp. The evidence extractor now treats that displayed timestamp as a whole-second interval and still rejects a TRX finishing in the next second. Its two boundary tests passed, alongside the rest of the extractor suite. This is a timestamp-precision correction, not a test-result override.

The [tested dependency closure](canonical-dependency-closure-tested.json.gz), uncompressed SHA-256 `263692b07bff0d2851e11e762bb628b3593359caa9c8ecac9c42b3d14eda4f89`, rechecks original source ancestry, exact imported blobs and modes, prepared file hashes, all six overlays, the 343-project restored graph, and the matching test receipt. A Core dependency change reaches 53 selected `net10.0` projects: 50 passed, Slack and Azure Service Bus were selected but executed zero test cases, and the performance project was built without a Test SDK/TRX. No affected `net8.0` or `net9.0` test project was selected by the canonical Test target. The 94 per-framework console summaries total 6,306 passed and 145 skipped because the multi-framework Designer summaries differ from the latest retained per-project TRX; the NUKE/retained TRX result above is authoritative. The graph identifies Slack as the only declared independent release unit and records its test as skipped.

Integration-program tooling tests passed 271/271 in both normal and optimized Python modes. After adding a committed-receipt integrity assertion, the focused graph verifier passed 16/16 in both modes. `git fsck --full --no-dangling` also passed on the disposable three-parent rehearsal.

This is a passing **pinned rehearsal**, not the history-bearing import at the later Core `main`. Final remote SourceLink, package-mode dependency closure, one-publisher handoff, imported-host behavior, and supported historical Secrets upgrades remain open. No packages were packed or published, and no production change was made.

To repeat from this tooling checkout with full-history source repositories at the exact commits:

```text
python3 scripts/integration-program/rehearse-import.py --core <core-checkout> --extensions <extensions-checkout> --studio <studio-checkout> --output <new-disposable-directory> --source-profile current-tip
python3 scripts/integration-program/prepare_consolidated_build.py --rehearsal <new-disposable-directory>
python3 -m unittest discover -s scripts/integration-program -p test_prepare_consolidated_build.py
```

Never push the synthetic rehearsal commit or publish packages from it.

To recheck dependency closure from this tooling checkout and this exact disposable source tree, decompress `full-suite-nuke-test-evidence.json.gz` and pass it with `--source-profile current-tip-e96`, `--rehearsal`, and `--overlay-receipt reviewed-overlays-six.json` to `scripts/integration-program/refresh_canonical_dependency_graph.py`. The restored assets and source files must still match the recorded hashes.
