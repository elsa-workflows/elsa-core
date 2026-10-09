# Selected Studio maintenance NuGet consumers

Task #8693's selected consumer adapter accepts one exact reviewed eligible plan, one successful matching artifact receipt with its reviewed digest, the original retained NuGet archives, and original hash-bound planning assets. The implemented control is Studio 3.8. It has no independent product, line, version, source, framework or feed selector. Other products and original Extensions assembly policies remain separate adapter work.

The producer receipt must carry the exact reviewed planner controller and an artifact controller whose commit/tree resolves to immutable local Git objects. Both controllers must contain the exact plan-bound planner inputs. The consumer controller may differ; the retained receipt carries all three identities separately. Loaded assembly locations remain in private runtime logs after archive/cache/output byte verification. Retained runtime rows contain only allowlisted assembly/package identities and hashes, and selected cache origins use the logical local-archive source name.

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

The original source assets define an allowed external package ID/version/native-content-hash catalogue. Their target graphs are context-specific and do not define the clean consumer closure. Before native inspection or nuspec parsing, the adapter freezes original external archive bytes from one unambiguous original package-folder candidate. Original project/TFM/hash contexts remain in the private catalogue; conflicting hashes for the same ID/version fail closed. Signed archives use the existing SDK signature-integrity and NuGet content-hash inspector; raw archive SHA256/SHA512 remain separate from the NuGet content hash. The native runtime assemblies match the plan's exact SDK identities. Signature integrity does not certify the signer or original feed provenance. Original applicable feed names map to private finite mirrors for discovery only; the separate cold restore must prove the original HTTPS feed mapping. Unknown, missing, changed, ambiguous or unverifiable original archives fail closed.

Each cell first performs an offline native NuGet restore against that finite original archive pool and the genuine exact selected archives. This provisional restore generates its own native lock; it is not consumer build proof. Validation joins every package/version/hash to the admitted catalogue, checks full applicable actual nuspec edges and native ranges, requires one exact direct selected root with only reachable transitive packages, and freezes the generated lock bytes. No extra direct dependency or guessed highest version is added. Private compiler packages can be available in the original pool but enter the graph only if actual published nuspec edges require them.

A second restore uses a new empty cache and HOME, the exact frozen lock, the original configured external HTTPS feed mapping and selected IDs exclusively mapped to local same-run archives. No wildcard remains in the isolated mapping. The graph, lock, cache archives, original external raw archive hashes and native content hashes, local selected bytes and extracted compile/runtime/content/build assets must all agree before compilation. Discovery mirror/cache bytes are not proof caches.

Each cell has new HOME, NuGet global-package/HTTP/plugin caches, explicit config and empty central MSBuild files. Product source and ProjectReference fallback, selected registry resolution, unintended external versions/feeds, altered archives/cache payloads and private excluded package admission fail. Local UUID/UTC execution is explicit; hosted claims are rejected until a real workflow/provider binding exists. Raw source snapshots, inputs and logs remain under `private/`; only the allowlisted `retained/receipt.json` is intended for review.

Cheap contracts:

```sh
PYTHONDONTWRITEBYTECODE=1 PYTHONPATH=scripts/integration-program \
  python3 -m unittest test_prove_product_release_consumers
PYTHONDONTWRITEBYTECODE=1 PYTHONPATH=scripts/integration-program \
  python3 -O -m unittest test_prove_product_release_consumers
```

This implementation has not yet executed a live consumer restore, compile or runtime check. The first authorized Studio3.8 control must validate native lock representation, original archive availability and framework-pack/pruning behavior before broader execution. Missing full nuspec edges or unexpected framework pruning fail closed for review. A genuine restore/compile/runtime failure stays a failure with private diagnostics and a closed public stage. A source defect requires separately reviewed maintenance continuation; no source helper, dependency or version substitution is permitted.

The original plan freshness gate is unchanged. A successful long-running producer receipt does not silently extend an expired plan or authorize backdating a consumer check; any stage-binding continuation needs separate review.
