# Core a13ac consolidated preparation receipt

Program #8194; story #8286; task #8287. This is a disposable, no-remote source-history rehearsal. It does not import the repositories into `main`, build the combined solution, pack a package, or authorize publication.

The source commits are Core `a13ac7a412e037280d7a568fd9dd87b06ff8b724`, Extensions `ba8b71d91c15ffe5be4b2c539cf9f712e74af775`, and Studio `20ceaeeed7e671f0c9662003e82063026f2216de`. The synthetic rehearsal commit is `372ed7ed3973c90fa581bf6003040900137bc093`. The [import receipt](import-receipt.json.gz) reports 6,361 Core files and 3,771 mapped Extensions/Studio files (10,132 total), exact blob/mode mapping, and reachable original histories. Its uncompressed SHA-256 is `dd31bc0073b897d5d34e30edef59780b3825ada9b68b3e3ad9580ff680ad6f72`.

The [build-preparation receipt](consolidated-build-receipt.json.gz) records the reviewed source integration patch and every prepared file hash (uncompressed SHA-256 `992b68ec247ab007cf554d50e215fcef73cda4b19d1192a5fed43f07703814e1`). The [packability report](canonical-packability-report.json.gz) records SDK 10.0.300 evaluations of 167 imported/test projects across Debug, Release, declared target frameworks and three reference modes. All 2,586 evaluations report `IsPackable=false` and `GeneratePackageOnBuild=false` (uncompressed SHA-256 `5b720229d7f58da91a4c62aaf19670acea6212638c013dfcc676713fba6b5d18`). All three compressed JSON files were checked for absolute path values before inclusion; none were present.

Repeat with full-history source checkouts at the commits above:

```text
python3 scripts/integration-program/rehearse-import.py --core <core-checkout> --extensions <extensions-checkout> --studio <studio-checkout> --output <new-disposable-directory> --source-profile current-tip
python3 scripts/integration-program/prepare_consolidated_build.py --rehearsal <new-disposable-directory>
python3 -m unittest discover -s scripts/integration-program -p test_prepare_consolidated_build.py
python3 -O -m unittest discover -s scripts/integration-program -p test_prepare_consolidated_build.py
```

The preparation suite passed 23/23 in each Python mode on 2026-09-24. The receipts explicitly retain `buildCompatibilityVerified=false`, `canonicalBuildAndTestsVerified=false`, and `publicationAuthorized=false`. Workbench/Studio fixture overlays applied **after** these receipts are separately verified by `prepare_workbench_secrets_runtime.py`; this receipt makes no runtime-host claim. The synthetic commit must never be pushed or used as a release commit.
