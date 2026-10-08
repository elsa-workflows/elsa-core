# Designer edge-hover source browser proof

This isolated Chromium smoke imports the production `registerEdgeHoverTools` helper from the mapped Studio ClientLib and bundles the ClientLib's restored X6. It uses native pointer movement and a real X6 remove-button click. The page exposes fresh-graph setup, observations and an explicitly selected supplemental notification adapter; it does not synthesize graph events or remove edges through the model. The nine primary cases use unfiltered production subscriptions.

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

The receipt has exactly nine primary assertion records, all required to pass:

1. `real-x6-rendered`: two real rendered edges and matching X6 model cells.
2. `repeated-edge-hover`: five native entries on A, exactly one button and one vertex tool in both rendered DOM and model on each entry.
3. `edge-to-edge-transfer`: A's button clears and B has exactly one; A's vertex tool remains.
4. `node-clears-hover`: native node entry clears the button.
5. `blank-clears-hover`: native blank hover clears the button without a held mouse button.
6. `outside-clears-hover`: B extends to the graph boundary; moving directly along B to outside clears its button, retains its vertex tool and produces a trusted native root `pointerleave`. Independently require an actual 800x440 container, CSS `:hover` false, and the last trusted pointer position outside both its rectangle and DOM hit subtree. X6 edge/graph leave counters are retained separately because its mouseout-based graph-leave emulation can preserve the edge target and emit only an edge leave on this path.
7. `remove-tool-entry`: pointer reaches the real button hit target along A; its button survives.
8. `remove-tool-click`: native mouse down/up removes A from the model and DOM, retaining B.
9. `noninteractive-no-tools`: a fresh disabled graph receives repeated native entries on A/B, retains both edges and creates no model or rendered tools. A subsequent boundary exit increments its native leave counter exactly once, guarding listener cleanup across graph reset.

The stale-A-leave-after-B regression is a separate synthetic source unit test. This browser proof does not replay that event sequence.

The natural edge-to-edge and edge-to-node paths can clear A through edge leave or intervening blank hover before the destination callback runs. Those primary cases establish the visible outcome, not independent coverage of each fallback handler. Four additional `supplemental_assertions` isolate destination handling and are also mandatory:

| Supplemental assertion | Destination callback | Required old-A clear predicate |
| --- | --- | --- |
| `isolated-edge-target-clears-old-button` | B entry delivered | True; B also has its normal button/vertex tools |
| `isolated-node-target-clears-old-button` | Node entry delivered | True; no remove buttons remain |
| `omitted-edge-target-rejected` | Only B entry callback omitted | False, with A's model/DOM button still present |
| `omitted-node-target-rejected` | Only node entry callback omitted | False, with A's model/DOM button still present |

These are explicitly labeled `dropped-notification-fault-injection`, not unmodified natural paths. Each uses a fresh real X6 graph, genuine A entry and real browser movement to B or the node. An adapter wraps only the production helper's subscriptions: it drops earlier edge-leave and blank-hover callbacks, and the negative controls additionally omit only the destination callback. Other delivered callbacks retain their original payload, `this`, context and return value. Independent X6 observers still receive the actual notifications. A bounded event sequence must show A entry, earlier leave and blank hover, then destination entry; both earlier callback types must have been suppressed before that entry. Immediately before the destination callback, A must still have exactly one model/DOM button and vertex tool. Positives clear that old button; negatives must reject that exact clear predicate with A still at one. Missing target events, setup errors, timeouts and absent B tools cannot count as a successful negative control. The same production helper bytes and bundle serve all cases; no source mutation or fabricated graph event is used.

## Receipt and lifecycle

`--expected-head` and `--output` are required; the output parent directory must already exist. Exit zero requires all nine primary and four supplemental assertions, zero browser errors and zero external browser requests. Every attempted assertion captures its current fixed subcheck, model/rendered tool counts, cell-existence and pointer-boundary booleans, actual container width/height, and native/X6 event counts before teardown, including on failure. Supplemental observations also capture the bounded fixed-event sequence, suppressed callback counts, before/after destination tool counts and the old-A clear predicate. Unattempted assertions retain a null observation. Failure records contain no exception text, console messages, URLs, pointer coordinates, screenshots or browser traces. Native enter/leave counters accept only trusted mouse PointerEvents whose target is the root container; capture-phase listeners exclude descendant notifications and run independently of X6's mouse-event handlers. Pointer-inside state comes from actual CSS `:hover`, not callback state. The last trusted mouse pointer move is observed on the document in capture phase, with only its geometric/hit-test booleans retained. Native listeners are aborted before each graph reset and on page disposal. The page is served from an ephemeral loopback port and external browser requests are blocked. Browser/context/server cleanup runs in `finally`, and signal handlers close native resources. Playwright actions/assertions have finite timeouts; the hosted wrapper must also bound the complete process.

The JSON binds the Git source commit, production helper/create-graph/unit-test/Vitest configuration, reviewed and installed ClientLib locks, ClientLib package manifest, source workflow, all fixture source files, and every actual esbuild input to SHA-256 and byte length. It records the exact installed X6/Playwright/esbuild and launched Chromium versions, the JavaScript bundle digest, and native event counts for interactive/noninteractive graphs. The runner requires the expected Git HEAD and clean tracked inputs initially, then rechecks HEAD, tracked cleanliness and every input hash after browser teardown before accepting the result. Eight pure Node guard regressions cover unchanged inputs, mismatched/missing expected head, pre/post tracked edits, restored dependency/lock changes, changed HEAD and untracked source. The receipt is written atomically with mode 0600. Do not accept merely a prior-head receipt or a command that omitted required assertions.
