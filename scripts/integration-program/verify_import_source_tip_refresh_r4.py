#!/usr/bin/env python3
"""Verify the reviewed Studio timer-test fix and its preserved import ancestry."""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path
from typing import Any

from verify_import_source_tip_refresh_r3 import PUBLISHER_BLOBS, ROOT, entry, git, parents


RECEIPT = ROOT / "doc/integration-program/consolidation/source-tip-refresh-2026-09-25-r4.json"
SOURCE = "src/modules/Elsa.Studio.Workflows.Tests/WorkflowInstanceDesignerDisconnectRefreshTests.cs"
MAPPED = "src/studio/modules/Elsa.Studio.Workflows.Tests/WorkflowInstanceDesignerDisconnectRefreshTests.cs"
OLD_STUDIO = "099402226daba80e473a306bbd1243b8994465b8"
NEW_STUDIO = "5b34ec327caffd132e18bfd88bfc9e862dc35c43"


def verify(receipt: dict[str, Any], root: Path = ROOT) -> None:
    if receipt.get("schemaVersion") != 1 or receipt.get("issue") != 8286:
        raise ValueError("Studio timer source-tip receipt schema or issue changed")
    if (receipt.get("oldStudioCommit"), receipt.get("newStudioCommit")) != (OLD_STUDIO, NEW_STUDIO):
        raise ValueError("Reviewed Studio source commits changed")
    if receipt.get("studioPullRequest") != "https://github.com/elsa-workflows/elsa-studio/pull/1069":
        raise ValueError("Reviewed Studio pull request changed")

    base = receipt["baseImportHead"]
    mapped = receipt["mappedDeltaCommit"]
    join = receipt["historyJoinCommit"]
    if parents(mapped, root) != [mapped, base]:
        raise ValueError("Studio timer mapped delta has unexpected parent")
    if parents(join, root) != [join, mapped, NEW_STUDIO]:
        raise ValueError("Studio timer history join lost the reviewed source parent")
    for commit in (base, mapped, join, OLD_STUDIO, NEW_STUDIO):
        subprocess.run(["git", "merge-base", "--is-ancestor", commit, "HEAD"], cwd=root, check=True)
    subprocess.run(["git", "merge-base", "--is-ancestor", OLD_STUDIO, NEW_STUDIO], cwd=root, check=True)

    if git("diff", "--name-status", "--no-renames", OLD_STUDIO, NEW_STUDIO, root=root).splitlines() != [f"M\t{SOURCE}"]:
        raise ValueError("Studio upstream timer-test delta changed")
    if git("diff", "--name-only", base, mapped, root=root).splitlines() != [MAPPED]:
        raise ValueError("Studio mapped timer-test delta changed unrelated paths")
    if git("diff", "--name-only", mapped, join, root=root):
        raise ValueError("Studio timer source-history join changed the mapped tree")

    rows = receipt.get("mappedChanges")
    if not isinstance(rows, list) or len(rows) != 1:
        raise ValueError("Studio timer source-tip receipt must have one mapped change")
    row = rows[0]
    if (row.get("status"), row.get("source"), row.get("mappedPath")) != ("M", SOURCE, MAPPED):
        raise ValueError("Studio timer source relocation changed")
    for field, commit, path in (
        ("oldSource", OLD_STUDIO, SOURCE),
        ("oldMapped", base, MAPPED),
        ("newSource", NEW_STUDIO, SOURCE),
        ("mappedAtDelta", mapped, MAPPED),
        ("finalMapped", "HEAD", MAPPED),
    ):
        if row.get(field) != entry(commit, path, root):
            raise ValueError(f"Studio timer {field} blob changed")
    if row["oldSource"] != row["oldMapped"] or row["newSource"] != row["mappedAtDelta"] or row["newSource"] != row["finalMapped"]:
        raise ValueError("Unreviewed Studio timer source transformation")

    for path, blob in PUBLISHER_BLOBS.items():
        for commit in (base, join, "HEAD"):
            if entry(commit, path, root) != {"blob": blob, "mode": "100644"}:
                raise ValueError(f"Active Core publisher changed at {commit}: {path}")


def main() -> int:
    try:
        verify(json.loads(RECEIPT.read_text(encoding="utf-8")))
    except (OSError, subprocess.CalledProcessError, ValueError, KeyError, TypeError) as error:
        print(f"Invalid Studio timer source-tip refresh: {error}", file=sys.stderr)
        return 1
    print("Verified exact Studio timer-test mapping, preserved ancestry and unchanged Core publishers")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
