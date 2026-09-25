# Shared root assets and empty Extensions placeholder

Program #8194; story #8286. This decision covers five rows in the frozen
[163-asset ledger](legacy-asset-dispositions.md) without changing its original
source pins. The current upstream `main` tips checked on 2026-09-25 were
Extensions `1c134e00f50a7ac36ea61e6c442dc3c5148db049` and Studio
`f0eeb3c7428443b09512049fe890635fa4f7b427`; the active Core files were
inspected at `ed1338c201b66078341a8a788942e982da007828`.

| Retained source asset | Source Git blob and mode | Active disposition |
| --- | --- | --- |
| Extensions `CONTRIBUTING.md` | `77c8a2ffee6660fbf8622210eb30439dcc5fb0ea`, `100644` | Represented by Core-root [`CONTRIBUTING.md`](../../../CONTRIBUTING.md). |
| Studio `CONTRIBUTING.md` | `77c8a2ffee6660fbf8622210eb30439dcc5fb0ea`, `100644` | Same active Core guide. |
| Extensions `icon.png` | `47e1cadea43af0b7d1a5c489e0d46cf72ecf87eb`, `100644` | Represented byte-for-byte by Core-root [`icon.png`](../../../icon.png). |
| Studio `icon.png` | `47e1cadea43af0b7d1a5c489e0d46cf72ecf87eb`, `100644` | Same active Core icon. |
| Extensions `SHELL_FEATURES_MIGRATION.md` | `e69de29bb2d1d6434b8b29ae775ad8c2e48c5391`, `100644` | Explicitly retired as a zero-byte placeholder; no active document is installed. |

The two upstream contributor guides are identical at the checked tips. Core's
current guide contains all 101 source lines in order and adds 65 lines for
Core's ADR and feature-request process; no source guidance was deleted or
rewritten. The active icon has exactly the upstream blob and mode. The
Extensions placeholder is zero bytes, and a search of its tracked source tip
found no reference to that path outside the placeholder itself. Its inert
`.source` copy in the draft import remains historical provenance, not current
migration guidance.

The current Core icon's source identity is settled here. Actual icon inclusion
in every future package remains part of each release unit's artifact checks;
the bounded Slack package proof has its own icon receipt. These five decisions
do not activate historical build or publication files, settle the 47 Secrets
collision sources, or authorize the history merge. Recheck these current-tip
comparisons on the final import head before marking #8286 complete.

Reproduce the local active-file checks with `git hash-object icon.png
CONTRIBUTING.md` in Core. At the pinned Core head they return
`47e1cadea43af0b7d1a5c489e0d46cf72ecf87eb` and
`68ab718a55ed06ec7b38a140c7c4e973e4762df4`, respectively. The upstream
commits and paths above provide immutable source bytes for the corresponding
comparison.
