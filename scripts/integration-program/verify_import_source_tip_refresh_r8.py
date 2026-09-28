#!/usr/bin/env python3
"""Verify the Extensions MongoDB tenant-isolation tip (c2618de7) preserved in the import."""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path
from typing import Any

from verify_import_source_tip_refresh_r2 import blob_and_mode, git, git_bytes, mapped_path


ROOT = Path(__file__).resolve().parents[2]
RECEIPT = ROOT / "doc/integration-program/consolidation/source-tip-refresh-2026-09-28-r8.json"
REVIEWED_COMMITS = (
    "9ed2e1fa4f72d4238241aea6c44412458d68ef49",
    "c405a11bf3a39700b8afaaf534f4421ac83346c2",
    "f8ad6661f2f38506dd28c9c22f86414cedb65a51",
    "3ae6568e7a1afdb259518cb3b4736930512373bc",
    "807cd89328c01e76bf1cababf525aff3c29bec93",
    "c2618de7b92b22ae7b23d872b58a20866219f69b",
)
MONGO_SOURCE = "src/modules/persistence/Elsa.Persistence.MongoDb/"
MONGO_TESTS = "test/modules/persistence/Elsa.MongoDb.UnitTests/"
MONGO_TEST_SOLUTION_ENTRY = b"test\\extensions\\modules\\persistence\\Elsa.MongoDb.UnitTests\\Elsa.MongoDb.UnitTests.csproj"


def verify(receipt: dict[str, Any], root: Path = ROOT) -> None:
    if receipt.get("schemaVersion") != 8 or receipt.get("issue") != 8286:
        raise ValueError("Eighth source-tip refresh schema or issue changed")
    if receipt.get("sourcePullRequest") != "https://github.com/elsa-workflows/elsa-core/pull/8526":
        raise ValueError("Reviewed source pull request changed")
    if receipt.get("publicationPerformed") is not False:
        raise ValueError("Source-tip receipt must not claim a package publication")

    keys = ("baseImportHead", "mappedDeltaCommit", "historyJoinCommit", "acceptedMergeCommit",
            "oldExtensionsCommit", "newExtensionsCommit")
    if tuple(receipt[key] for key in keys) != REVIEWED_COMMITS:
        raise ValueError("Reviewed Extensions source-tip commits changed")
    base, delta, join, accepted, old, new = REVIEWED_COMMITS
    if git("rev-list", "--parents", "-n", "1", delta, root=root).split() != [delta, base]:
        raise ValueError("Mapped MongoDB delta is not a child of the reviewed import head")
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
    if not upstream or not all(source.startswith((MONGO_SOURCE, MONGO_TESTS)) for source in upstream):
        raise ValueError("Current Extensions delta reaches beyond the reviewed MongoDB files")
    rows = receipt["mappedChanges"]
    if not isinstance(rows, list) or {row["source"]: row["status"] for row in rows} != upstream or len(rows) != len(upstream):
        raise ValueError("Eighth source-tip receipt does not match the upstream delta")
    if set(git("diff", "--name-only", base, delta, root=root).splitlines()) != {mapped_path(source) for source in upstream}:
        raise ValueError("Mapped MongoDB delta changed unreviewed paths")
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
        if git_bytes("show", f"HEAD:{mapped}", root=root) != git_bytes("show", f"{new}:{source}", root=root):
            raise ValueError(f"Mapped MongoDB file differs from upstream: {mapped}")

    for workflow in (".github/workflows/packages.yml", ".github/workflows/update-wiki.yml"):
        if blob_and_mode(base, workflow, root) != blob_and_mode(accepted, workflow, root):
            raise ValueError(f"Source refresh changed active publisher workflow: {workflow}")
    if git_bytes("show", "HEAD:Elsa.sln", root=root).count(MONGO_TEST_SOLUTION_ENTRY) != 1:
        raise ValueError("Current Elsa.sln does not select the mapped MongoDB tests exactly once")


def main() -> int:
    try:
        verify(json.loads(RECEIPT.read_text(encoding="utf-8")))
    except (OSError, subprocess.CalledProcessError, ValueError, KeyError, TypeError) as error:
        print(f"Invalid eighth source-tip refresh: {error}", file=sys.stderr)
        return 1
    print("Verified the Extensions c2618de7 history join, the mapped MongoDB files and current HEAD")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
