#!/usr/bin/env python3
"""Verify the reviewed Studio source fix and latest Core-main merge in the draft import."""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
RECEIPT = ROOT / "doc/integration-program/consolidation/source-tip-refresh-2026-09-25-r3.json"
EXPECTED_SOURCE_PATHS = {
    "samples/react/workflow-definition-editor-sample/workflow-definition-editor-app/src/App.js":
        "samples/studio/react/workflow-definition-editor-sample/workflow-definition-editor-app/src/App.js",
    "src/modules/Elsa.Studio.Diagnostics.OpenTelemetry.Tests/SignalROpenTelemetryObserverTests.cs":
        "src/studio/modules/Elsa.Studio.Diagnostics.OpenTelemetry.Tests/SignalROpenTelemetryObserverTests.cs",
}
PUBLISHER_BLOBS = {
    ".github/workflows/packages.yml": "8cd229e2d5f6307826f144ee0b1de37f86f93ed8",
    ".github/workflows/update-wiki.yml": "65a8494fdb4cedcbe78b065ca23d4d16e6fb6b32",
}


def git(*args: str, root: Path = ROOT) -> str:
    return subprocess.check_output(["git", *args], cwd=root, text=True).strip()


def entry(commit: str, path: str, root: Path = ROOT) -> dict[str, str]:
    line = git("ls-tree", commit, "--", path, root=root)
    if not line:
        raise ValueError(f"Missing Git blob: {commit}:{path}")
    mode, kind, blob, found_path = line.split(None, 3)
    if kind != "blob" or found_path != path:
        raise ValueError(f"Unexpected Git entry: {commit}:{path}")
    return {"blob": blob, "mode": mode}


def parents(commit: str, root: Path = ROOT) -> list[str]:
    return git("rev-list", "--parents", "-n", "1", commit, root=root).split()


def verify(receipt: dict[str, Any], root: Path = ROOT) -> None:
    if receipt.get("schemaVersion") != 1 or receipt.get("issue") != 8286:
        raise ValueError("Studio source-tip receipt schema or issue changed")
    base = receipt["baseImportHead"]
    mapped = receipt["mappedDeltaCommit"]
    join = receipt["historyJoinCommit"]
    core_merge = receipt["coreMainMergeCommit"]
    old_studio = receipt["oldStudioCommit"]
    new_studio = receipt["newStudioCommit"]
    core_main = receipt["coreMainCommit"]

    if parents(mapped, root) != [mapped, base]:
        raise ValueError("Studio mapped delta has unexpected parent")
    if parents(join, root) != [join, mapped, new_studio]:
        raise ValueError("Studio history join does not retain the reviewed source tip")
    if parents(core_merge, root) != [core_merge, join, core_main]:
        raise ValueError("Core-main merge has unexpected parents")
    for commit in (base, mapped, join, core_merge, old_studio, new_studio, core_main):
        subprocess.run(["git", "merge-base", "--is-ancestor", commit, "HEAD"], cwd=root, check=True)
    subprocess.run(["git", "merge-base", "--is-ancestor", old_studio, new_studio], cwd=root, check=True)

    source_changes = git("diff", "--name-status", "--no-renames", old_studio, new_studio, root=root).splitlines()
    if source_changes != [f"M\t{path}" for path in sorted(EXPECTED_SOURCE_PATHS)]:
        raise ValueError(f"Studio upstream delta changed: {source_changes}")
    rows = receipt["mappedChanges"]
    if not isinstance(rows, list) or len(rows) != 2 or {row["source"] for row in rows} != set(EXPECTED_SOURCE_PATHS):
        raise ValueError("Studio mapped delta must name both reviewed source paths exactly once")
    if set(git("diff", "--name-only", base, mapped, root=root).splitlines()) != set(EXPECTED_SOURCE_PATHS.values()):
        raise ValueError("Studio mapped commit changed unrelated paths")
    if git("diff", "--name-only", mapped, join, root=root):
        raise ValueError("Studio source-history join changed the mapped tree")
    if set(git("diff", "--name-only", join, core_merge, root=root).splitlines()) != {
        ".gitignore", "doc/integration-program/consolidation/extensions-generated-ignore-policy.md"
    }:
        raise ValueError("Core-main merge changed unexpected paths")

    for row in rows:
        source = row["source"]
        target = EXPECTED_SOURCE_PATHS[source]
        if row["status"] != "M" or row["mappedPath"] != target:
            raise ValueError(f"Studio source relocation changed: {source}")
        for field, commit, path in (
            ("oldSource", old_studio, source),
            ("oldMapped", base, target),
            ("newSource", new_studio, source),
            ("mappedAtDelta", mapped, target),
            ("finalMapped", "HEAD", target),
        ):
            if row[field] != entry(commit, path, root):
                raise ValueError(f"Studio {field} blob changed: {source}")
        if row["oldSource"] != row["oldMapped"] or row["newSource"] != row["mappedAtDelta"] or row["newSource"] != row["finalMapped"]:
            raise ValueError(f"Unreviewed Studio source transform: {source}")

    for path, blob in PUBLISHER_BLOBS.items():
        for commit in (base, join, core_merge, "HEAD"):
            if entry(commit, path, root) != {"blob": blob, "mode": "100644"}:
                raise ValueError(f"Active Core publisher changed at {commit}: {path}")


def main() -> int:
    try:
        verify(json.loads(RECEIPT.read_text(encoding="utf-8")))
    except (OSError, subprocess.CalledProcessError, ValueError, KeyError, TypeError) as error:
        print(f"Invalid Studio source-tip refresh: {error}", file=sys.stderr)
        return 1
    print("Verified two exact Studio source mappings, preserved ancestry and unchanged Core publishers")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
