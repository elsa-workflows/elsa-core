# Consolidated Copilot and release-note guidance

Program #8194; build integration #8286. This record closes the path-specific
guidance rows in the frozen legacy-asset ledger after the active files were
reviewed and merged into draft import #8409. The original `.source` files and
their Git identities remain preserved for history and audit.

| Pinned source asset | Active Core representation | Reviewed change |
| --- | --- | --- |
| Studio `.github/copilot-instructions.md` | `.github/copilot-instructions.md` | [#8479](https://github.com/elsa-workflows/elsa-core/pull/8479), `e2711ab0def8be84ef8cf1d7c17f82e153cacc36` |
| Extensions `.github/agents/release-notes.agent.md` | `.github/agents/release-notes.agent.md` | [#8480](https://github.com/elsa-workflows/elsa-core/pull/8480), `87a0ac7055134e8532cca130fa15313a35fe8c69` |
| Extensions `.github/copilot-release-notes-playbook.md` | `.github/copilot-release-notes-playbook.md` | #8480, same merge |
| Studio `.github/agents/release-notes.agent.md` | `.github/agents/release-notes.agent.md` | #8480, same merge; updates its earlier identical representation from #8453 |
| Studio `.github/copilot-release-notes-playbook.md` | `.github/copilot-release-notes-playbook.md` | #8480, same merge |

The pinned Studio Copilot instructions still described a separate Studio
repository, older SDK/Node requirements and blanket package-version
workarounds. The active guidance now points to `AGENTS.md`, the consolidated
source paths and solution, Node 22 ClientLib build, the current Studio guide,
and explicit release boundaries. It represents the developer workflow without
copying those stale instructions.

The archived Extensions and Studio release-note prompts selected a whole
repository tag/range. In one source tree, that would mix unrelated connectors
and could misstate what a package release contains. The active agent and
playbook require a named release unit, package IDs, reviewed refs and current
publisher, and changed-path/shared-dependency evidence before drafting. They
define distinct changelog paths for a single-package unit, a Core-wide unit,
and another reviewed multi-package unit. Both original prompts map to this one
active process. The Studio agent row was initially an identical copy; #8480
changed its active blob, so it is now recorded as an expanded representation.

These are developer and release-note instructions. They do not establish
package compatibility, authorize feed publication, or complete publisher
cutover. The ledger pins each active file's reviewed Git blob and mode, while
the original source commit, blob and mode remain unchanged.
