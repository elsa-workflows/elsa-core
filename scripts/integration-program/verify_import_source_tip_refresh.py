#!/usr/bin/env python3
"""Verify exact mapped source deltas and ancestry on the draft history import."""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
RECEIPT = ROOT / "doc/integration-program/consolidation/source-tip-refresh-2026-09-25.json"


def git(*args: str, root: Path = ROOT) -> str:
    return subprocess.check_output(["git", *args], cwd=root, text=True).strip()


def blob_and_mode(commit: str, path: str, root: Path = ROOT) -> dict[str, str] | None:
    entry = git("ls-tree", commit, "--", path, root=root)
    if not entry:
        return None
    mode, kind, blob, found_path = entry.split(None, 3)
    if kind != "blob" or found_path != path:
        raise ValueError(f"Unexpected Git tree entry: {commit}:{path}")
    return {"blob": blob, "mode": mode}


def mapped_path(repository: str, source: str) -> str:
    if repository == "extensions":
        if source.startswith("test/"):
            return "test/extensions/" + source.removeprefix("test/")
        return f"doc/integration-program/legacy/extensions/{source}.source"
    if repository == "studio":
        if source.startswith("src/"):
            return "src/studio/" + source.removeprefix("src/")
        return f"doc/integration-program/legacy/studio/{source}.source"
    raise ValueError(f"Unrecognized source repository: {repository}")


def verify(receipt: dict[str, Any], root: Path = ROOT) -> None:
    if receipt.get("schemaVersion") != 1 or receipt.get("issue") != 8286:
        raise ValueError("Source-tip refresh receipt schema or issue changed")
    base, delta, join = (receipt[key] for key in ("baseImportHead", "deltaCommit", "historyJoinCommit"))
    old, new = receipt["oldUpstreamCommits"], receipt["newUpstreamCommits"]
    if set(old) != {"extensions", "studio"} or set(new) != set(old):
        raise ValueError("Source-tip refresh repository set changed")
    if git("rev-list", "--parents", "-n", "1", delta, root=root).split() != [delta, base]:
        raise ValueError("Mapped delta is not a child of the reviewed import head")
    if git("rev-list", "--parents", "-n", "1", join, root=root).split() != [
        join, delta, new["extensions"], new["studio"]
    ]:
        raise ValueError("Source-tip join no longer preserves both upstream histories")
    for commit in (base, delta, join, *old.values(), *new.values()):
        subprocess.run(["git", "merge-base", "--is-ancestor", commit, "HEAD"], cwd=root, check=True)

    expected: dict[tuple[str, str], tuple[str, str]] = {}
    for repository in ("extensions", "studio"):
        for line in git("diff", "--name-status", old[repository], new[repository], root=root).splitlines():
            status, source = line.split("\t", 1)
            expected[(repository, source)] = (status, mapped_path(repository, source))
    rows = receipt["mappedChanges"]
    if not isinstance(rows, list) or len(rows) != 6 or len({(row["repository"], row["source"]) for row in rows}) != 6:
        raise ValueError("Source-tip refresh must contain six unique mapped changes")
    if {(row["repository"], row["source"]): (row["status"], row["mappedPath"]) for row in rows} != expected:
        raise ValueError("Mapped changes differ from the exact upstream source deltas")
    for row in rows:
        repository, source, mapped = row["repository"], row["source"], row["mappedPath"]
        if row["old"] != blob_and_mode(old[repository], source, root) or row["old"] != blob_and_mode(base, mapped, root):
            raise ValueError(f"Old source or mapped import changed: {repository}/{source}")
        if row["new"] != blob_and_mode(new[repository], source, root) or row["new"] != blob_and_mode(join, mapped, root):
            raise ValueError(f"Refreshed source mapping changed: {repository}/{source}")
        if row["new"] != blob_and_mode("HEAD", mapped, root):
            raise ValueError(f"Refreshed mapped file changed after history join: {mapped}")
    actual_paths = set(git("diff", "--name-only", base, join, root=root).splitlines())
    if actual_paths != {row["mappedPath"] for row in rows}:
        raise ValueError("History join changed paths outside the six reviewed source deltas")
    for workflow in (".github/workflows/packages.yml", ".github/workflows/update-wiki.yml"):
        if blob_and_mode(base, workflow, root) != blob_and_mode(join, workflow, root):
            raise ValueError(f"Source-tip refresh changed an active Core publishing workflow: {workflow}")


def main() -> int:
    try:
        receipt = json.loads(RECEIPT.read_text(encoding="utf-8"))
        verify(receipt)
    except (OSError, subprocess.CalledProcessError, ValueError, KeyError, TypeError) as error:
        print(f"Invalid source-tip refresh: {error}", file=sys.stderr)
        return 1
    print(f"Verified {len(receipt['mappedChanges'])} exact source deltas and both refreshed histories")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
