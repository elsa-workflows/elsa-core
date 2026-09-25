# Relocated Studio generated-output ignore rules

Program #8194; Story #8286. Studio's retained `.gitignore` at
`9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822` is preserved under
`doc/integration-program/legacy/studio/.gitignore.source`. The
[static-source decision](studio-ignore-policy.md) fixed the active root rule
that hid new hand-authored `wwwroot` files. This follow-up maps only the
standalone rules that still protect generated or local Studio paths:

| Standalone output | Consolidated output |
| --- | --- |
| CustomElements `wasm/` | `src/studio/hosts/Elsa.Studio.Host.CustomElements/wasm/` |
| React sample `public/` | `samples/studio/react/workflow-definition-editor-sample/workflow-definition-editor-app/public/` |
| Designer and DomInterop ClientLib lockfiles | Corresponding `src/studio` ClientLib paths |
| Browser `test-results/` | `test/studio/browser/**/test-results/` |
| Wrappers `.npmrc` | `src/studio/wrappers/.npmrc` |

The React sample's `postinstall` copies WASM assets into `public/`. The
[ClientLib build script](../../../scripts/integration-program/build_studio_clientlibs.sh)
copies reviewed lockfiles into the two source directories before running
`npm ci`; those copies are build inputs for the local proof, not changes to
commit. Existing checked-in React and wrappers lockfiles remain tracked and
visible. A developer-local wrappers `.npmrc` can contain registry settings,
so it remains untracked.

Core already tracks `.specify/feature.json`; importing Studio's unanchored
ignore for that path would not remove the tracked file and would hide a newly
created replacement if it were ever removed. The remaining broad Visual
Studio, package, publish, release, and documentation patterns are not copied
into Core without evidence that they are generated at the relocated paths.
The retained Studio `.dockerignore` has a separate container-context gate.

`git check-ignore --no-index` verifies the six relocated ignore cases and
controls for hand-authored Studio static assets, a checked-in React lockfile,
and an unrelated Core source path. The changes do not alter a Docker build,
package, workflow, or feed publication.
