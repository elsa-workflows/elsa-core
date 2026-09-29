#!/usr/bin/env python3
"""Verify the Studio 3.10 host-branding tip (24337549) preserved in the import."""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path
from typing import Any

from verify_import_source_tip_refresh_r2 import LANDED_IMPORT, blob_and_mode, git, git_bytes
import verify_import_source_tip_refresh_r7 as publisher_receipt


ROOT = Path(__file__).resolve().parents[2]
RECEIPT = ROOT / "doc/integration-program/consolidation/source-tip-refresh-2026-09-28-r9.json"
REVIEWED_COMMITS = (
    "ab109c6286999ddb56f8f24172191c69d0f6fcc0",
    "1043d3d9a6be3f4670016d82eab21ad4c2fbed94",
    "0653505a7b0640350703c29d1b2312dcba847460",
    "b42d93879dfb8e2026084271259669b8aa679d60",
    "5b34ec327caffd132e18bfd88bfc9e862dc35c43",
    "24337549a28be797b639708a87d5fcb9b8c693ee",
)
STUDIO_SOURCES = ("src/framework/Elsa.Studio.Core/", "src/hosts/Elsa.Studio.Host.")


def mapped_path(source: str) -> str:
    if not source.startswith("src/"):
        raise ValueError(f"Unexpected source path in refreshed Studio delta: {source}")
    return "src/studio/" + source.removeprefix("src/")


def verify(receipt: dict[str, Any], root: Path = ROOT) -> None:
    if receipt.get("schemaVersion") != 9 or receipt.get("issue") != 8286:
        raise ValueError("Ninth source-tip refresh schema or issue changed")
    if receipt.get("sourcePullRequest") != "https://github.com/elsa-workflows/elsa-core/pull/8530":
        raise ValueError("Reviewed source pull request changed")
    if receipt.get("publicationPerformed") is not False:
        raise ValueError("Source-tip receipt must not claim a package publication")

    keys = ("baseImportHead", "mappedDeltaCommit", "historyJoinCommit", "acceptedMergeCommit",
            "oldStudioCommit", "newStudioCommit")
    if tuple(receipt[key] for key in keys) != REVIEWED_COMMITS:
        raise ValueError("Reviewed Studio source-tip commits changed")
    base, delta, join, accepted, old, new = REVIEWED_COMMITS
    if git("rev-list", "--parents", "-n", "1", delta, root=root).split() != [delta, base]:
        raise ValueError("Mapped Studio delta is not a child of the reviewed import head")
    if git("rev-list", "--parents", "-n", "1", join, root=root).split() != [join, delta, new]:
        raise ValueError("Current Studio tip was not preserved as a merge parent")
    if git("rev-list", "--parents", "-n", "1", accepted, root=root).split() != [accepted, base, join]:
        raise ValueError("GitHub integration did not preserve the source-history merge")
    for commit in REVIEWED_COMMITS:
        subprocess.run(["git", "merge-base", "--is-ancestor", commit, "HEAD"], cwd=root, check=True)

    upstream = {}
    for line in git("diff", "--name-status", old, new, root=root).splitlines():
        status, source = line.split("\t", 1)
        upstream[source] = status
    if not upstream or not all(source.startswith(STUDIO_SOURCES) for source in upstream):
        raise ValueError("Current Studio delta reaches beyond the reviewed host-branding files")
    rows = receipt["mappedChanges"]
    if not isinstance(rows, list) or {row["source"]: row["status"] for row in rows} != upstream or len(rows) != len(upstream):
        raise ValueError("Ninth source-tip receipt does not match the upstream delta")
    if set(git("diff", "--name-only", base, delta, root=root).splitlines()) != {mapped_path(source) for source in upstream}:
        raise ValueError("Mapped Studio delta changed unreviewed paths")
    if git("diff", "--name-only", delta, join, root=root):
        raise ValueError("Studio history join changed the mapped source tree")
    if git("diff", "--name-only", join, accepted, root=root):
        raise ValueError("GitHub merge changed the reviewed mapped source tree")

    for row in rows:
        source = row["source"]
        mapped = mapped_path(source)
        if row["mappedPath"] != mapped:
            raise ValueError(f"Wrong mapped path for {source}")
        for key, commit, path in (
            ("oldSource", old, source), ("oldMapped", base, mapped),
            ("newSource", new, source), ("finalMapped", LANDED_IMPORT, mapped),
        ):
            if row[key] != blob_and_mode(commit, path, root):
                raise ValueError(f"Changed {key} for {mapped}")
        if row["status"] == "D":
            if row["newSource"] is not None or row["finalMapped"] is not None:
                raise ValueError(f"Deleted upstream file is still mapped: {mapped}")
        elif git_bytes("show", f"{LANDED_IMPORT}:{mapped}", root=root) != git_bytes("show", f"{new}:{source}", root=root):
            raise ValueError(f"Mapped Studio file differs from upstream: {mapped}")

    for workflow in publisher_receipt.PUBLISHERS:
        if blob_and_mode(base, workflow, root) != blob_and_mode(accepted, workflow, root):
            raise ValueError(f"Source refresh changed active publisher workflow: {workflow}")
    # The seventh receipt owns the reviewed publisher-workflow bytes at HEAD.
    publisher_receipt.verify(json.loads((root / publisher_receipt.RECEIPT.relative_to(ROOT)).read_bytes()), root)


def main() -> int:
    try:
        verify(json.loads(RECEIPT.read_text(encoding="utf-8")))
    except (OSError, subprocess.CalledProcessError, ValueError, KeyError, TypeError) as error:
        print(f"Invalid ninth source-tip refresh: {error}", file=sys.stderr)
        return 1
    print("Verified the Studio 24337549 history join, the mapped host-branding files and the landed import")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
