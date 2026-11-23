# Nonpublishing Studio npm pair

Task [#8679](https://github.com/elsa-workflows/elsa-core/issues/8679) prepares the
existing `@elsa-workflows/elsa-studio-wasm` and
`@elsa-workflows/elsa-studio-wasm-react` archives from one exact Core source.
It provides no npm upload, production version allocation, publisher authority,
maintenance ref activation, archival or browser certification.

The [workflow](../../.github/workflows/studio-npm-pair.yml) first ran on the reviewed
`codex/elsa-integration-studio-npm-8679` proof branch. That branch is gone, so its push
trigger was removed and the workflow now runs only by manual dispatch on main, which
requires its exact reviewed SHA. The only accepted
version is `0.0.0-proof.RUN.ATTEMPT`, bound to the workflow run and attempt.
Checkout credentials are disabled, permissions are read-only, and there is no
OIDC, protected environment, registry token or publisher action. Source build
processes receive an explicit environment allowlist and isolated npm config,
home and cache; repository/npm credentials and Actions authority handles are
not inherited. Historical source workflows are not invoked.

With Node 22, SDK 10.0.300 and Python available, run from a clean reviewed Core
checkout, selecting a new output directory outside it:

```sh
python3 scripts/integration-program/prove_studio_npm_pair.py \
  --commit FULL_REVIEWED_CORE_SHA --version 0.0.0-proof.12345.1 \
  --run-id 12345 --run-attempt 1 --output /absolute/new/proof-directory
```

The controller verifies the exact source SHA/tree and clean checkout before
work and again in failure/success finalization. It locally fetches that exact
commit into the private output source tree. ClientLib builds, generated files,
SDK restore/build/publish outputs and staged package/lock metadata stay there.
The existing ClientLib recipe builds both JavaScript bundles and checks the
Designer generated BPMN types/tests. The real CustomElements host is published
in Release for net10.0, then its entire emitted `wwwroot` is staged and packed.
The archive must match the full staged file inventory, actual host references,
runtime loader and fingerprinted native/managed WASM assets. There is no assumed
legacy `blazor.boot.json` filename or substitute synthetic runtime.

Both archives embed `elsa-proof.json` with the exact Core commit/tree, shared
proof version, run/attempt and package identity; package metadata names Core and
the exact source commit. The wrapper's embedded manifest also binds the WASM
archive's SHA-512 integrity. Before its build, the original wrapper workspace
lock is staged to install that exact verified local WASM archive. Every
third-party lock entry is retained unchanged. The actual installed WASM file
inventory is checked before building the existing Vite wrapper. Its packed
dependency is the exact WASM proof version; every main/module/exports target and
the lifecycle helper must exist in the original archive.

The clean consumer uses the original local pair and already installed producer
third-party lock/cache. Workspace links are removed and the
two Elsa entries are replaced with exact local archive identities/integrities;
unregistered, nested, missing or foreign-registry Elsa entries fail. No registry
metadata resolution selects newer transitive dependencies. npm's offline
lock-only operation selects the reachable consumer closure, discarding unused
producer tools; every surviving third-party entry is restored byte-for-byte
from the producer lock after its identity/dependencies are checked unchanged.
The actual offline install must leave a producer-only sentinel absent in the
focused fixture. The consumer's
`npm ci --offline --ignore-scripts=false` requires ordinary, unforced peer
resolution and actual normal lifecycle execution. Wrapper workspace installation
is also unforced. The separate existing ClientLib recipe retains its own flags.
Cache absence fails; it does not fall back to online Elsa resolution. Consumer
React/ReactDOM remain on the declared React 19 line, and actual React, ReactDOM,
Vite and uuid versions must equal the producer lock.

The wrapper lifecycle now resolves WASM relative to the installed package. For
normal nested installation it copies `_framework`, `_content`, appsettings and
the host stylesheet into the invoking consumer's `public/` via npm's `INIT_CWD`.
Source-workspace invocation retains wrapper-local `public/` for Vite/Storybook.
The package-specific `public/.elsa-studio-wasm-assets.json` ledger records only
safe relative asset-file paths and their SHA-256 hashes. Before changing any
asset, the helper reads the full incoming payload and checks the ledger, target
path types, every existing owned file and all incoming destinations. Malformed
ownership, links on a target path, modified owned files and unmanaged collisions
fail before writes. Existing files without a ledger are not adopted, even when
their bytes match; reconcile those conflicts before retrying. Unmanaged sibling
files and shared directories are preserved. Only previously owned, unchanged
files can be replaced or removed as stale; a missing owned file can be restored.
The ledger is replaced atomically after a successful serial refresh. The whole
copy is not a filesystem transaction: interruption or I/O failure may require
reconciling owned files with the previous ledger before retrying. No automatic
rollback, concurrent-writer guarantee or forced overwrite is claimed.
Consumer checks compare every copied asset byte and the ownership ledger with
the archive, exercise removal of a fixture explicitly recorded as previously
owned, and verify unmanaged files in both shared asset folders survive. The actual
host's asset references are carried into a small React/Vite consumer with root
public URLs. All four existing exports must import as callable ESM and CommonJS
values, and Vite must build while retaining the exact public asset payload.
This proves package resolution and static assets, not browser startup, backend
access or complete custom-element behavior.

Only `retained/` is uploaded: the two original `.tgz` files as they become
available, byte-original consumer package/lock inputs and a structured
`receipt.json`. Consumer inputs contain relative local archive locators; their
hashes are retained and checked unchanged after installation and consumer proof.
The receipt binds source/run/version,
toolchain, original archive SHA-512 values, embedded manifests, complete file
hash inventories, producer input/consumer lock hashes, installed versions and
consumer outcomes. Success is assigned only after every gate; failed receipts
retain the closed failure code/stage and final source status. Raw command logs,
source trees, package caches and restore/build metadata stay in `private/` and
are not uploaded or printed into hosted logs. Unknown exceptions remain closed
`unexpected-failure`, requiring private-log inspection rather than an unsafe
public traceback.

Focused contracts:

```sh
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s scripts/integration-program -p test_prove_studio_npm_pair.py
PYTHONDONTWRITEBYTECODE=1 python3 -O -m unittest discover -s scripts/integration-program -p test_prove_studio_npm_pair.py
actionlint .github/workflows/studio-npm-pair.yml
```

Synthetic contracts and the tiny actual offline npm lock/lifecycle fixture do
not prove the real net10 host. Acceptance still needs an exact-head hosted pair,
original-provider ZIP/tarball readback, independent current-head review, required
CI/Greptile gates and actual merged-tree verification. Earlier readiness reports
are not artifact proof. Dependabot #8672's broad Nx/Lerna/transitive changes and
#8659's old paths are not adopted by this slice; no dependency upgrade is needed
merely to stage the local pair. Later product version/release policy (#8217),
artifact-only publishers/recovery (#8220), retained maintenance npm paths and
concrete authority/archival handoff remain separate.

## Verify a published classic Studio npm pair

For a Core release built with SBOM support, the tagged Packages workflow generates a
CycloneDX 1.6 SBOM for each tarball and validates it with the pinned CycloneDX CLI.
Before either Feedz upload, the publisher checks producer SHA-256 checksums, both
SBOMs and the embedded pair identity. It then reads both packages back from the
registry and compares the downloaded bytes and npm SHA-512 integrity. GitHub then
signs a build-provenance statement and the corresponding SBOM for each tarball.
The signed SBOM is retrievable by the tarball digest after the temporary Actions
artifacts expire. If the attestation job fails, the registry may already contain
the packages. Inspect the publisher's readback and retry the failed jobs using the
same original build artifacts. SLSA provenance records the original build attempt even
when a later signing attempt succeeds.

With GitHub CLI and jq installed, set VERSION to the release, COMMIT to the
independently trusted commit for that tag, and download both original tarballs.
Run this in their directory:

```bash
set -euo pipefail
repo=elsa-workflows/elsa-core
policy=(-R "$repo" --signer-workflow "$repo/.github/workflows/packages.yml"
        --source-ref "refs/tags/$VERSION" --source-digest "$COMMIT" --deny-self-hosted-runners)
for leaf in elsa-studio-wasm elsa-studio-wasm-react; do
  tgz="elsa-workflows-${leaf}-${VERSION}.tgz"
  gh attestation verify "$tgz" "${policy[@]}"
  gh attestation verify "$tgz" "${policy[@]}" --predicate-type https://cyclonedx.org/bom --format json |
    jq -e '.[0].verificationResult.statement.predicate' > "$tgz.cdx.json"
done
```

The root component identifies the packed library by name, version, SHA-256 and
npm SHA-512 integrity. Each regular archive member has a SHA-256 entry.
The SBOM identifies managed assemblies and static assets associated with NuGet
restore and publish records, and npm libraries observed in first-party webpack
source maps, esbuild module metadata and Vite output. Prebundled third-party
assets retain file hashes and their enclosing NuGet package where available. Monaco
Editor exposes its version in shipped language bundles. The authentication service
includes an identifiable OIDC client and a patched CryptoJS source fragment. Their embedded dependency inventories can be incomplete.
CycloneDX dependency edges describe the direct npm declarations. Their properties
record the declared dependency and peer ranges. Both inventories mark runtime
dependency graph coverage as incomplete.
The React SBOM also names the exact WASM version and integrity selected at build
time. Consumers should generate a separate deployment SBOM from their own resolved
application, including installed peers, runtime, base image and configuration.

Third-party files sometimes contain libraries whose package manifests were stripped
upstream. The [reviewed vendor notices](../../scripts/integration-program/studio-vendor-notices.cdx.json)
record identifiable components from those shipped bytes using CycloneDX. Their
hashes and paths select the exact embedded file in each tarball. For an updated
vendor, inspect its license banners, upstream notices and bundler metadata. A
Retire.js catalog scan can corroborate identities such as DOMPurify. Add a
version only when the packed code or its matching upstream build establishes it.
Use CycloneDX pedigree for modified or partial copies. A different file digest
still gets a file inventory entry and any available enclosing NuGet attribution.
Review the embedded-component evidence before reusing it for that update.

Historical npmjs 3.9.0 packages carry npm provenance from
`elsa-workflows/elsa-studio/.github/workflows/packages.yml`. Use
`npm audit signatures` in an isolated project installed from the exact original
npmjs versions for that producer. The separate
[original-pair custody procedure](studio-npm-original-publisher.md) covers
recovery of previously built artifacts. For new release candidates, the proof workflow
retains validated unsigned SBOMs in a companion artifact keyed by source, run and
attempt. Each root package digest identifies its original tarball. See [#9316](https://github.com/elsa-workflows/elsa-core/issues/9316).
