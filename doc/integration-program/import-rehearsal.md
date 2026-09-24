# Consolidation history rehearsal

The [history ADR](../adr/2026-09-23-preserve-upstream-history-during-consolidation.md) and `scripts/integration-program/rehearse-import.py` implement a disposable tree/history proof for #8213. The rehearsal does not migrate the live source tree or establish build compatibility.

## Run

Use full local clones, including original history. Fetch shallow clones with `git fetch --unshallow origin` first; do not change their working trees. The Extensions and Studio pins are explicit constants in the script. Core uses the chosen checkout's committed HEAD; uncommitted work is not imported.

```sh
python3 scripts/integration-program/rehearse-import.py \
  --core /path/to/elsa-core \
  --extensions /path/to/elsa-extensions \
  --studio /path/to/elsa-studio \
  --output /tmp/elsa-import-rehearsal-new
python3 -m unittest discover -s scripts/integration-program -p 'test_*.py'
python3 -O -m unittest discover -s scripts/integration-program -p 'test_*.py'
```

The output path must not exist and must be outside all source checkouts and Git metadata directories. If a post-creation step fails, the tool removes only its newly created output so the same path can be retried. Raw Git pathname bytes round-trip through UTF-8 `surrogateescape`; the JSON receipt escapes non-UTF-8 bytes rather than discarding or normalizing them. The script fetches objects from the supplied local clones, creates `source-core`, `source-extensions`, `source-studio` and `rehearsal` refs only in that new repository, points `HEAD` at `rehearsal`, materializes the mapped tree, and writes `import-receipt.json`. It does not check out or modify any source repository, build, run imported scripts, push, or configure an external remote. A POSIX surrogateescape path is materialized when the host filesystem accepts its raw bytes; a path the host cannot represent remains exact in the Git tree and index with `skip-worktree`. The receipt covers every imported path, blob and mode, including inert legacy assets. Keep it with the final migration evidence; do not publish a rehearsal commit as the actual import.

## Verified execution: 2026-09-23

| Input | Commit | Tracked files |
|---|---|---:|
| Core | `fa1e36e8890a3ccd35626844772b781c29503342` | 5,833 |
| Extensions | `33fa0bfd28c7585240e3d4f665058c067b17e287` | 1,665 |
| Studio | `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822` | 2,106 |

The disposable tree has **9,604 files**, with exact blob/mode equality and no destination collision. `git merge-base --is-ancestor` passed for all three original tips; `git fsck --full --no-dangling` passed for the result and complete reachable history. The first local evidence commit was `9755bef905be5a9ba7af07c4b25676574e6ab6ca`; it is deliberately not pushed. Repeating the rehearsal changes this synthetic commit ID because its creation time changes. Source commit IDs and mapped blob IDs do not change.

The mapping regressions reject existing Core overwrites, file/directory collisions, two input paths collapsing to one output, and path escapes. They also check inactive workflow/build inputs, the five duplicate-package projects, and complete file/blob/mode retention. Both regular and optimized Python runs passed.

No solution build, runtime descriptor comparison, package publication, backend/Studio debug session, canonical-source compatibility or cutover is claimed. Those gates remain in #8214, #8215 and #8259–#8260. In particular, retained `.source` assets require explicit integration or retirement, and the candidate Secrets source ownership must pass API/storage/UI compatibility review before the real import.

The exact path-by-path proposal for all 163 retained `.source` assets is recorded in the [legacy asset disposition ledger](consolidation/legacy-asset-dispositions.md). It distinguishes candidate behavior in the disposable build patch from changes actually integrated into the history-bearing source tree.
