# Root editor policy and Studio pull request template

Program #8194; Story #8286. This decision covers two retained repository-root
assets from the frozen history-import ledger. The checked source files are the
inert `.source` copies in draft import #8409 at `530d9489e49a45c66e6922d6fcf597dade0aa5cd`.
The active Core files were checked on `main` at
`ffb9662efbc269f626a44f670003c7a6b01290e4`.

| Retained source | Active Core representation | Decision |
| --- | --- | --- |
| Extensions `.editorconfig`, blob `31a97b0835a6371d8c3e591bebe56bd5698dfb0c`, `100644` | [Root `.editorconfig`](../../../.editorconfig), blob `ae542588fe3d66b843a5416a3ba6bc0ba2c848c8`, `100644` | Keep Core's root policy. A direct diff shows the only behavior difference is Core's three `csharp_style_var_* = true` preferences versus Extensions' `false`; a comment moved across `root = true`. The repository's `AGENTS.md` already says to prefer `var`. |
| Studio `.github/pull_request_template.md`, blob `e52b47382d92aed7b56faf67c7f8417c75b43d3b`, `100644` | [Active PR template](../../../.github/pull_request_template.md), same blob and mode | One byte-identical template serves the combined repository; no second template path is needed. |

The Extensions editor file remains historical provenance at its inert `.source`
path. Installing it at the root would reverse Core's established C# style for
every imported module. The active root configuration applies to the imported
tree unless a nested configuration overrides it; this decision does not
change any nested file. The Studio template remains available at the exact
active Git blob. Neither decision alters compilation, package identities,
publisher workflows, or release behavior.

Reproduce the source comparison in the checked-out import draft with
`git ls-tree HEAD doc/integration-program/legacy/extensions/.editorconfig.source
doc/integration-program/legacy/studio/.github/pull_request_template.md.source`,
`diff -u doc/integration-program/legacy/extensions/.editorconfig.source
.editorconfig`, and `cmp
doc/integration-program/legacy/studio/.github/pull_request_template.md.source
.github/pull_request_template.md`. Compare the active file identities on Core
`main` with `git ls-tree HEAD .editorconfig .github/pull_request_template.md`.
