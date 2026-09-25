# Studio design baseline and historical release note

Program #8194; story #8286. This settles two documentation rows in the frozen
[legacy asset ledger](legacy-asset-dispositions.md) without editing its original
source pins or removing its inert `.source` copies. Studio `main` was
`f0eeb3c7428443b09512049fe890635fa4f7b427` when checked on 2026-09-25.
Both source paths are regular files (`100644`) at that commit.

| Studio source path | Active Core path | Git blob | Disposition |
| --- | --- | --- | --- |
| `.interface-design/system.md` | [`doc/studio/design/system.md`](../../studio/design/system.md) | `3a5059c6d654e2ab51a46884638effa3625f1b4c` | Retained verbatim as the approved 2026-09-01 Studio design baseline. |
| `.github/RELEASE_NOTES_3.6.0-rc1.md` | [`doc/studio/releases/3.6.0-rc1.md`](../../studio/releases/3.6.0-rc1.md) | `4b3251bdcbe0fd58aad3ff2b2e1f06fa79f28107` | Archived verbatim as historical 3.6.0-rc1 notes, never current release instructions. |

The active [Studio documentation index](../../studio/README.md) distinguishes
those roles. Verify the copies without requiring a second Studio checkout:

```sh
git hash-object doc/studio/design/system.md
git hash-object doc/studio/releases/3.6.0-rc1.md
```

Both outputs must match the pinned blobs above. The materialized **draft import**
retains Studio Git history and inert `.source` copies; neither that history nor
those copies is in the current Core tree through this documentation PR. The
history-bearing import remains a separate, gated change. The exact copied
blobs and source pin preserve this relocation's provenance. This decision says
nothing about the remaining 161 ledger rows, current release content, package
publisher ownership, or repository retirement.
