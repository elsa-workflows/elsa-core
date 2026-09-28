#!/usr/bin/env python3
"""Verify the Extensions Dapper V3_7 migration tip (3cf50295) preserved in the import."""

from __future__ import annotations

import json
import re
import subprocess
import sys
from pathlib import Path
from typing import Any

from verify_import_source_tip_refresh_r2 import blob_and_mode, git, git_bytes, mapped_path
import verify_import_source_tip_refresh_r7 as publisher_receipt


ROOT = Path(__file__).resolve().parents[2]
RECEIPT = ROOT / "doc/integration-program/consolidation/source-tip-refresh-2026-09-28-r10.json"
REVIEWED_COMMITS = (
    "1827914bcecee4c11054f61fd02d105b720d4ad6",
    "6c8f59c3f8666b15bd0760c7e7c2bd0c74b798df",
    "e72f4b2df4ecb654b23c3aab77171046c3475f67",
    "13b946634585ae851016f679e506b2768ae470ba",
    "c2618de7b92b22ae7b23d872b58a20866219f69b",
    "3cf502955791cbd20375022ef4597f0e195faf8a",
)
DAPPER_MIGRATIONS = "src/modules/persistence/Elsa.Persistence.Dapper.Migrations/"
DAPPER_TESTS = "test/modules/persistence/Elsa.Persistence.Dapper.UnitTests/"
DAPPER_TEST_SOLUTION_ENTRY = (
    b"test\\extensions\\modules\\persistence\\Elsa.Persistence.Dapper.UnitTests\\Elsa.Persistence.Dapper.UnitTests.csproj")
# The import rewrote this project file (no BOM, mapped ProjectReference path); upstream's line changes apply on top.
LINE_DELTA_SOURCES = frozenset({DAPPER_TESTS + "Elsa.Persistence.Dapper.UnitTests.csproj"})


def changed_lines(before: str, after: str, path: str, root: Path) -> tuple[list[str], list[str]]:
    diff = git("diff", "--unified=0", before, after, "--", path, root=root).splitlines()
    added = [line[1:].lstrip("\ufeff") for line in diff if line.startswith("+") and not line.startswith("+++")]
    removed = [line[1:].lstrip("\ufeff") for line in diff if line.startswith("-") and not line.startswith("---")]
    return added, removed


def placements(content: bytes, added: list[str]) -> list[tuple[str, str]]:
    """The lines that follow each added line, which pin where it was inserted."""
    lines = [line.lstrip("\ufeff").rstrip("\r") for line in content.decode("utf-8-sig").split("\n")]
    return [(line, lines[index + 1] if index + 1 < len(lines) else "")
            for index, line in enumerate(lines) if line in added]


def verify(receipt: dict[str, Any], root: Path = ROOT) -> None:
    if receipt.get("schemaVersion") != 10 or receipt.get("issue") != 8286:
        raise ValueError("Tenth source-tip refresh schema or issue changed")
    if receipt.get("sourcePullRequest") != "https://github.com/elsa-workflows/elsa-core/pull/8535":
        raise ValueError("Reviewed source pull request changed")
    if receipt.get("publicationPerformed") is not False:
        raise ValueError("Source-tip receipt must not claim a package publication")

    keys = ("baseImportHead", "mappedDeltaCommit", "historyJoinCommit", "acceptedMergeCommit",
            "oldExtensionsCommit", "newExtensionsCommit")
    if tuple(receipt[key] for key in keys) != REVIEWED_COMMITS:
        raise ValueError("Reviewed Extensions source-tip commits changed")
    base, delta, join, accepted, old, new = REVIEWED_COMMITS
    if git("rev-list", "--parents", "-n", "1", delta, root=root).split() != [delta, base]:
        raise ValueError("Mapped Dapper delta is not a child of the reviewed import head")
    if git("rev-list", "--parents", "-n", "1", join, root=root).split() != [join, delta, new]:
        raise ValueError("Current Extensions tip was not preserved as a merge parent")
    if git("rev-list", "--parents", "-n", "1", accepted, root=root).split() != [accepted, base, join]:
        raise ValueError("GitHub integration did not preserve the source-history merge")
    for commit in REVIEWED_COMMITS:
        subprocess.run(["git", "merge-base", "--is-ancestor", commit, "HEAD"], cwd=root, check=True)

    upstream = {}
    for line in git("diff", "--name-status", old, new, root=root).splitlines():
        status, source = line.split("\t", 1)
        upstream[source] = status
    if not upstream or not all(source.startswith((DAPPER_MIGRATIONS, DAPPER_TESTS)) for source in upstream):
        raise ValueError("Current Extensions delta reaches beyond the reviewed Dapper migration files")
    rows = receipt["mappedChanges"]
    if not isinstance(rows, list) or {row["source"]: row["status"] for row in rows} != upstream or len(rows) != len(upstream):
        raise ValueError("Tenth source-tip receipt does not match the upstream delta")
    if set(git("diff", "--name-only", base, delta, root=root).splitlines()) != {mapped_path(source) for source in upstream}:
        raise ValueError("Mapped Dapper delta changed unreviewed paths")
    if git("diff", "--name-only", delta, join, root=root):
        raise ValueError("Extensions history join changed the mapped source tree")
    if git("diff", "--name-only", join, accepted, root=root):
        raise ValueError("GitHub merge changed the reviewed mapped source tree")

    for row in rows:
        source = row["source"]
        mapped = mapped_path(source)
        if row["mappedPath"] != mapped:
            raise ValueError(f"Wrong mapped path for {source}")
        for key, commit, path in (
            ("oldSource", old, source), ("oldMapped", base, mapped),
            ("newSource", new, source), ("finalMapped", "HEAD", mapped),
        ):
            if row[key] != blob_and_mode(commit, path, root):
                raise ValueError(f"Changed {key} for {mapped}")
        expected = "upstream-line-delta" if source in LINE_DELTA_SOURCES else "identical"
        if row.get("transform") != expected:
            raise ValueError(f"Reviewed transform changed for {mapped}")
        if expected == "identical":
            if git_bytes("show", f"HEAD:{mapped}", root=root) != git_bytes("show", f"{new}:{source}", root=root):
                raise ValueError(f"Mapped Dapper file differs from upstream: {mapped}")
        else:
            upstream_lines = changed_lines(old, new, source, root)
            mapped_lines = changed_lines(base, "HEAD", mapped, root)
            if not upstream_lines[0] or mapped_lines != upstream_lines:
                raise ValueError(f"Mapped project file does not carry exactly the upstream line changes: {mapped}")
            if placements(git_bytes("show", f"{new}:{source}", root=root), upstream_lines[0]) != placements(
                    git_bytes("show", f"HEAD:{mapped}", root=root), upstream_lines[0]):
                raise ValueError(f"Mapped project file places the upstream line changes elsewhere: {mapped}")

    for workflow in publisher_receipt.PUBLISHERS:
        if blob_and_mode(base, workflow, root) != blob_and_mode(accepted, workflow, root):
            raise ValueError(f"Source refresh changed active publisher workflow: {workflow}")
    # The seventh receipt owns the reviewed publisher-workflow bytes at HEAD; defer to it rather than pin them twice.
    publisher_receipt.verify(json.loads((root / publisher_receipt.RECEIPT.relative_to(ROOT)).read_bytes()), root)

    solution = git_bytes("show", "HEAD:Elsa.sln", root=root)
    entries = re.findall(rb'^Project\([^)]*\) = "[^"]*", "' + re.escape(DAPPER_TEST_SOLUTION_ENTRY) + rb'", "(\{[^}]+\})"',
                         solution, re.MULTILINE)
    if len(entries) != 1:
        raise ValueError("Current Elsa.sln does not select the mapped Dapper tests exactly once")
    if not re.search(rb'^\s*' + re.escape(entries[0]) + rb'\.[^=]+\.Build\.0 = ', solution, re.MULTILINE | re.IGNORECASE):
        raise ValueError("Current Elsa.sln declares the mapped Dapper tests but builds them in no configuration")


def main() -> int:
    try:
        verify(json.loads(RECEIPT.read_text(encoding="utf-8")))
    except (OSError, subprocess.CalledProcessError, ValueError, KeyError, TypeError) as error:
        print(f"Invalid tenth source-tip refresh: {error}", file=sys.stderr)
        return 1
    print("Verified the Extensions 3cf50295 history join, the mapped Dapper files and current HEAD")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
