# Selected Studio maintenance NuGet consumers

Task #8693's selected consumer adapter accepts one exact reviewed eligible plan, one successful matching artifact receipt with its reviewed digest, the original retained NuGet archives, and original hash-bound planning assets. The implemented control is Studio 3.8. It has no independent product, line, version, source, framework or feed selector. Other products and original Extensions assembly policies remain separate adapter work.

The private planning snapshot uses the existing `receipt.private.json` schema with mode `private-original-planning-assets-snapshot`, exact plan/controller/full-source identity and selected `id/project/frameworks/sha256/bytes/file` entries. Raw assets bytes must match the plan's original `restore_assets_sha256`; derived `resolved_targets` are ignored. Missing, changed, stale, symbolic or mismatched inputs fail before consumer work. Preserve these original snapshots before product commands overwrite source obj files.

```sh
PYTHONDONTWRITEBYTECODE=1 python3 scripts/integration-program/prove_product_release_consumers.py \
  --plan /absolute/reviewed/plan.json --plan-sha256 EXACT_REVIEWED_PLAN_HASH \
  --artifact-receipt /absolute/retained/receipt.json --artifact-receipt-sha256 EXACT_REVIEWED_RECEIPT_HASH \
  --artifacts /absolute/retained/nuget \
  --planning-assets /absolute/private-original-planning-assets-snapshot \
  --output /absolute/new/consumer-control
```

Each selected package's original applicable TFM receives a separate fresh PackageReference restore and compile cell. A managed package uses an `extern alias` compile witness; output/content-only policies receive explicit restore/build accounting. A complete successful coverage ledger requires every selected package/TFM exactly once. Representative runtime behavior is additional proof: the source-bound Studio Core backend-options accessor must preserve its configured URI, and loaded Elsa bytes must join the restored archive/cache/output bytes and original Studio version/source policy. This is no browser, deployment or full-package behavioral certification.

The dependency closure follows actual retained selected nuspec edges and original hash-bound external restore edges. Compiler-only private source dependencies are not added to consumer scope. A lock document pins original external versions/SHA512 and exact selected archive SHA512 with one direct package reference, avoiding extra direct dependencies that could conceal a missing transitive declaration. Native NuGet semantics selects original applicable dependency groups and original feed mapping. Every selected ID maps only to the exact local archive source. Every external ID maps only to its original configured applicable feeds; no wildcard remains in the isolated consumer mapping. Restore closure, versions, hashes, config, caches and fallback folders are verified after restore. Cache archives and extracted compile/runtime/content/build assets are joined to original bytes before compilation.

Each cell has new HOME, NuGet global-package/HTTP/plugin caches, explicit config and empty central MSBuild files. Product source and ProjectReference fallback, selected registry resolution, unintended external versions/feeds, altered archives/cache payloads and private excluded package admission fail. Local UUID/UTC execution is explicit; hosted claims are rejected until a real workflow/provider binding exists. Raw source snapshots, inputs and logs remain under `private/`; only the allowlisted `retained/receipt.json` is intended for review.

Cheap contracts:

```sh
PYTHONDONTWRITEBYTECODE=1 PYTHONPATH=scripts/integration-program \
  python3 -m unittest test_prove_product_release_consumers
PYTHONDONTWRITEBYTECODE=1 PYTHONPATH=scripts/integration-program \
  python3 -O -m unittest test_prove_product_release_consumers
```

This implementation has not yet executed a live product restore, compile or runtime consumer. The first authorized Studio 3.8 consumer control must validate the original archives, manually constructed NuGet lock interface and framework-pack source behavior before broader execution. A genuine restore/compile/runtime failure stays a failure with a private diagnostic and closed public stage. A source defect requires separately reviewed maintenance continuation; no source helper, dependency or version substitution is permitted.
