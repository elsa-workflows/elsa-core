# Designer edge-hover source browser proof

This isolated Chromium smoke imports the production `registerEdgeHoverTools` helper from the mapped Studio ClientLib and bundles the ClientLib's restored X6. It uses native pointer movement and a real X6 remove-button click. The page exposes fresh-graph setup and observations; it does not synthesize graph events or remove edges through the model.

This proves the helper's browser behavior at one exact source head. The ClientLib Vitest/typecheck/production build gate separately proves its integration into `createGraph`. It is not the paused #8643 paired-package/browser matrix, a full Studio host journey, or npm publication evidence.

## Hosted commands

Use Node 22 on the hosted runner. The existing source gate prepares the reviewed Designer lock and dependencies, runs the ten source cases/typecheck and builds the production bundles:

```sh
scripts/integration-program/build_studio_clientlibs.sh .
npm ci --prefix test/studio/browser/DesignerHover
cd test/studio/browser/DesignerHover
npm test
npx --no-install playwright install --with-deps chromium
node run.mjs --expected-head "$EXPECTED_HEAD" --output "$RUNNER_TEMP/designer-hover-browser.json"
```

Set `EXPECTED_HEAD` to the exact candidate SHA selected by the hosted workflow, rather than deriving it from the checkout under test. For a browser-only diagnostic run after the source gate has already passed, reuse its installed Designer dependencies and execute the fixture commands. A standalone dependency setup must copy `scripts/integration-program/consolidated-build/studio-clientlib-lockfiles/designer.package-lock.json` to the mapped `ClientLib/package-lock.json` and run `npm ci --force` there first. The browser runner rejects a different checkout lock or restored X6 version. Do not regenerate the ClientLib lock for this proof.

Playwright 1.61.1 and esbuild 0.28.2 are pinned from the existing paused PackageCompatibility harness lock, with only their dependency closure retained here. That checkout and its artifacts are untouched. X6 is deliberately not installed in this fixture package; bundling resolves it exclusively from the mapped ClientLib. The reviewed lock currently resolves X6 2.19.2.

## Acceptance manifest

The receipt has exactly nine assertion records, all required to pass:

1. `real-x6-rendered`: two real rendered edges and matching X6 model cells.
2. `repeated-edge-hover`: five native entries on A, exactly one button and one vertex tool in both rendered DOM and model on each entry.
3. `edge-to-edge-transfer`: A's button clears and B has exactly one; A's vertex tool remains.
4. `node-clears-hover`: native node entry clears the button.
5. `blank-clears-hover`: native blank hover clears the button without a held mouse button.
6. `outside-clears-hover`: B extends to the graph boundary; moving directly along B to outside clears its button and produces a graph leave.
7. `remove-tool-entry`: pointer reaches the real button hit target along A; its button survives.
8. `remove-tool-click`: native mouse down/up removes A from the model and DOM, retaining B.
9. `noninteractive-no-tools`: a fresh disabled graph receives repeated native entries on A/B, retains both edges and creates no model or rendered tools.

The stale-A-leave-after-B regression is a separate synthetic source unit test. This browser proof does not replay that event sequence.

## Receipt and lifecycle

`--expected-head` and `--output` are required; the output parent directory must already exist. Exit zero requires all nine assertions, zero browser errors and zero external browser requests. Failure records contain only fixed stage/assertion categories, never exception text, console messages, URLs, screenshots or traces. The page is served from an ephemeral loopback port and external browser requests are blocked. Browser/context/server cleanup runs in `finally`, and signal handlers close native resources. Playwright actions/assertions have finite timeouts; the hosted wrapper must also bound the complete process.

The JSON binds the Git source commit, production helper/create-graph/unit-test/Vitest configuration, reviewed and installed ClientLib locks, ClientLib package manifest, source workflow, all fixture source files, and every actual esbuild input to SHA-256 and byte length. It records the exact installed X6/Playwright/esbuild and launched Chromium versions, the JavaScript bundle digest, and native event counts for interactive/noninteractive graphs. The runner requires the expected Git HEAD and clean tracked inputs initially, then rechecks HEAD, tracked cleanliness and every input hash after browser teardown before accepting the result. Eight pure Node guard regressions cover unchanged inputs, mismatched/missing expected head, pre/post tracked edits, restored dependency/lock changes, changed HEAD and untracked source. The receipt is written atomically with mode 0600. Do not accept merely a prior-head receipt or a command that omitted required assertions.
