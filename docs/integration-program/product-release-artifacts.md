# Selected maintenance artifact controls

Task [#8693](https://github.com/elsa-workflows/elsa-core/issues/8693) consumes the exact bytes of an eligible [product release plan](product-release-plans.md). This is a nonpublishing adapter, with no independent product, line, source or version selector. The first implemented producer control is Studio 3.8. Extensions, Core, the full six-cell matrix, complete selected-package NuGet consumer coverage and hosted archive sealing remain pending.

Generate a fresh plan at the reviewed artifact controller, inspect its complete eligible receipt and record its SHA-256. A plan older than one hour, unavailable prerequisites/history, malformed selection or the wrong reviewed hash fails before product work. The planner and artifact controller identities remain separate. Admission rebinds the registered source and original ownership policy; setup compares original solution/workflow/npm intents and refreshes prerequisite/history eligibility using GET-only requests and the original feed mapping. A changed prerequisite metadata hash requires a new reviewed plan.

```sh
python3 scripts/integration-program/prove_product_release_artifacts.py \
  --plan /absolute/reviewed/plan.json --plan-sha256 EXACT_REVIEWED_SHA256 \
  --run-id ACTUAL_RUN --run-attempt ACTUAL_ATTEMPT \
  --output /absolute/new/control
```

`--setup-only` stops after source setup and fresh prerequisite checks, before product commands. It returns `setup_complete: true` and `artifact_proof: false`; it never reports a successful artifact control. The same command without this flag runs the original Studio build/test/pack recipe privately, verifies its original SDK metadata, symbols, source documents and satellites, and retains only the selected package archives. Every retained NuGet dependency group must equal the reviewed plan's group. Full original recipes can produce excluded packages privately; the receipt accounts for those archives explicitly. Publication selection does not determine the source test closure.

The npm control publishes the historical CustomElements host privately with its original net10.0 recipe, creates the original WASM package, and installs that exact local same-run archive into the original wrapper workspace. It preserves the inline copy lifecycle, Vite build and dist-only package selection. Only package versions and the local producer dependency locator change. Complete tarball inventories join the original WASM bytes to the wrapper's dist assets. A downstream consumer uses exact local archive locators, denies scoped Elsa registry resolution, installs from a fresh npm cache with normal lifecycle scripts, then checks installed bytes, ESM/CommonJS imports and Vite compilation. It retains original third-party lock selections. Historical scripts do not acquire the current-main helper or ownership ledger.

A genuine original lifecycle/package failure stays a failure. Source corrections require a separately reviewed registered maintenance continuation. The runner does not transplant helpers, disable lifecycle scripts, substitute registry Elsa packages, execute historical workflows or claim browser/deployed certification. Existing current/main npm proof is historical tooling evidence only.

The output's `private/` directory contains source checkouts, original recipe outputs, caches and raw diagnostic logs. Share only allowlisted receipts and reviewed archives from `retained/`; producer receipts/raw logs remain private. Failed stage receipts do not disclose arbitrary exception or command output. Preserve failures when iterating with a new output directory.

Cheap contracts run without compiling products:

```sh
PYTHONDONTWRITEBYTECODE=1 PYTHONPATH=scripts/integration-program \
  python3 -m unittest test_prove_product_release_artifacts
PYTHONDONTWRITEBYTECODE=1 PYTHONPATH=scripts/integration-program \
  python3 -O -m unittest test_prove_product_release_artifacts
```

The planner PR path filter covers its consumed runtime/tests/native helpers, release archive verifier and all governed integration-program documents, including containment/register/ownership inputs. Publisher/recovery must later consume these same verified original bytes without rebuilding. This control grants no registry upload, credentials, OIDC, protected approval, permanent ref/tag/version allocation, deployment, authority retirement or archival permission.
