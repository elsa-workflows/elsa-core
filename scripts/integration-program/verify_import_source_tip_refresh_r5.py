#!/usr/bin/env python3
"""Verify the later Extensions Dapper tip without rewriting the prior receipt."""

from __future__ import annotations

import hashlib
import json
import subprocess
import sys
from pathlib import Path
from typing import Any

from verify_import_source_tip_refresh_r2 import (
    DAPPER_TEST_SOLUTION_ENTRY,
    RECEIPT as R2_RECEIPT,
    blob_and_mode,
    git,
    git_bytes,
    mapped_path,
    verify as verify_r2,
)


ROOT = Path(__file__).resolve().parents[2]
RECEIPT = ROOT / "doc/integration-program/consolidation/source-tip-refresh-2026-09-26-r5.json"
DECISION_PATH = "doc/integration-program/consolidation/extensions-807-source-tip.md"
SOURCE_PATHS = {
    "src/modules/persistence/Elsa.Persistence.Dapper/Abstractions/SqlDialectBase.cs",
    "src/modules/persistence/Elsa.Persistence.Dapper/Contracts/ISqlDialect.cs",
    "test/modules/persistence/Elsa.Dapper.UnitTests/SqlDialectDefaultUpdateTests.cs",
}


def verify(receipt: dict[str, Any], root: Path = ROOT) -> None:
    if receipt.get("schemaVersion") != 5 or receipt.get("issue") != 8286:
        raise ValueError("Fifth source-tip refresh schema or issue changed")
    if receipt.get("sourcePullRequest") != "https://github.com/elsa-workflows/elsa-core/pull/8499":
        raise ValueError("Reviewed source pull request changed")
    if receipt.get("publicationPerformed") is not False:
        raise ValueError("Source-tip receipt must not claim a package publication")
    historical = (root / R2_RECEIPT.relative_to(ROOT)).read_bytes()
    if hashlib.sha256(historical).hexdigest() != receipt["historicalReceiptSha256"]:
        raise ValueError("Prior reviewed source-tip receipt changed")
    verify_r2(json.loads(historical), root)

    base, delta, join, accepted = (
        receipt[key] for key in ("baseImportHead", "mappedDeltaCommit", "historyJoinCommit", "acceptedMergeCommit")
    )
    old, new = receipt["oldExtensionsCommit"], receipt["newExtensionsCommit"]
    if (base, delta, join, accepted, old, new) != (
        "8f85a3db84af3fac12df5f4364f285b70c761451",
        "55f56cef1d7ebf4f5beeea94c7576c3dd2d40d27",
        "7419e3ac201e1adba4f1284f74b3852a6b48668a",
        "1d8a567c53dba79a47d5503dda7b387411548aaf",
        "9361c80e2ea56ccf71118fb53d6fd8f3d0fbbbf0",
        "807cd89328c01e76bf1cababf525aff3c29bec93",
    ):
        raise ValueError("Reviewed Extensions source-tip commits changed")
    if git("rev-list", "--parents", "-n", "1", delta, root=root).split() != [delta, base]:
        raise ValueError("Mapped Dapper delta is not a child of the reviewed import head")
    if git("rev-list", "--parents", "-n", "1", join, root=root).split() != [join, delta, new]:
        raise ValueError("Current Extensions tip was not preserved as a merge parent")
    if git("rev-list", "--parents", "-n", "1", accepted, root=root).split() != [accepted, base, join]:
        raise ValueError("GitHub integration did not preserve the source-history merge")
    for commit in (base, delta, join, accepted, old, new):
        subprocess.run(["git", "merge-base", "--is-ancestor", commit, "HEAD"], cwd=root, check=True)

    upstream = {}
    for line in git("diff", "--name-status", old, new, root=root).splitlines():
        status, source = line.split("\t", 1)
        upstream[source] = status
    if set(upstream) != SOURCE_PATHS or set(upstream.values()) != {"M", "A"}:
        raise ValueError("Current Extensions delta differs from the reviewed three files")
    rows = receipt["mappedChanges"]
    if not isinstance(rows, list) or len(rows) != 3 or {row["source"] for row in rows} != SOURCE_PATHS:
        raise ValueError("Fifth source-tip receipt must contain three unique mapped files")
    if {row["source"]: row["status"] for row in rows} != upstream:
        raise ValueError("Fifth source-tip statuses differ from upstream")
    if set(git("diff", "--name-only", base, delta, root=root).splitlines()) != (
        {mapped_path(source) for source in SOURCE_PATHS} | {DECISION_PATH}
    ):
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
        upstream_bytes = git_bytes("show", f"{new}:{source}", root=root)
        active_bytes = git_bytes("show", f"HEAD:{mapped}", root=root)
        if row["status"] == "A":
            if row["oldSource"] is not None or row["oldMapped"] is not None or active_bytes != upstream_bytes:
                raise ValueError(f"New regression test differs from upstream: {mapped}")
        elif active_bytes != upstream_bytes + b"\n":
            raise ValueError(f"Reviewed Dapper source transform changed: {mapped}")

    superseded = {mapped_path(source) for source in SOURCE_PATHS}
    prior = json.loads(historical)
    for row in prior["mappedChanges"]:
        if row["mappedPath"] not in superseded and row["finalMapped"] != blob_and_mode("HEAD", row["mappedPath"], root):
            raise ValueError(f"Earlier reviewed mapping changed without another receipt: {row['mappedPath']}")
    for workflow in (".github/workflows/packages.yml", ".github/workflows/update-wiki.yml"):
        if blob_and_mode(base, workflow, root) != blob_and_mode(accepted, workflow, root):
            raise ValueError(f"Source refresh changed active publisher workflow: {workflow}")
    if git_bytes("show", "HEAD:Elsa.sln", root=root).count(DAPPER_TEST_SOLUTION_ENTRY) != 1:
        raise ValueError("Current Elsa.sln does not select the mapped Dapper tests exactly once")


def main() -> int:
    try:
        verify(json.loads(RECEIPT.read_text(encoding="utf-8")))
    except (OSError, subprocess.CalledProcessError, ValueError, KeyError, TypeError) as error:
        print(f"Invalid fifth source-tip refresh: {error}", file=sys.stderr)
        return 1
    print("Verified current Extensions history, three mapped Dapper files and prior asset scope")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
