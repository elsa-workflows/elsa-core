#!/usr/bin/env python3
"""Verify the second, history-preserving Extensions source-tip refresh."""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
RECEIPT = ROOT / "doc/integration-program/consolidation/source-tip-refresh-2026-09-25-r2.json"
TEST_FRIEND = b'        <InternalsVisibleTo Include="Elsa.Dapper.UnitTests" />\n'
EXISTING_FRIEND = b'        <InternalsVisibleTo Include="Elsa.Persistence.Dapper.UnitTests" />'
OLD_TEST_REFERENCE = (
    b'..\\..\\..\\..\\src\\modules\\persistence\\Elsa.Persistence.Dapper\\Elsa.Persistence.Dapper.csproj'
)
NEW_TEST_REFERENCE = b'../../../../../src/extensions/persistence/Elsa.Persistence.Dapper/Elsa.Persistence.Dapper.csproj'


def git(*args: str, root: Path = ROOT) -> str:
    return subprocess.check_output(["git", *args], cwd=root, text=True).strip()


def git_bytes(*args: str, root: Path = ROOT) -> bytes:
    return subprocess.check_output(["git", *args], cwd=root)


def blob_and_mode(commit: str, path: str, root: Path = ROOT) -> dict[str, str] | None:
    entry = git("ls-tree", commit, "--", path, root=root)
    if not entry:
        return None
    mode, kind, blob, found_path = entry.split(None, 3)
    if kind != "blob" or found_path != path:
        raise ValueError(f"Unexpected Git tree entry: {commit}:{path}")
    return {"blob": blob, "mode": mode}


def mapped_path(source: str) -> str:
    if source.startswith("src/modules/"):
        return "src/extensions/" + source.removeprefix("src/modules/")
    if source.startswith("test/"):
        return "test/extensions/" + source.removeprefix("test/")
    if "/" not in source:
        return f"doc/integration-program/legacy/extensions/{source}.source"
    raise ValueError(f"Unexpected source path in refreshed delta: {source}")


def with_new_test_friend(content: bytes) -> bytes:
    if content.count(EXISTING_FRIEND) != 1 or TEST_FRIEND in content:
        raise ValueError("Dapper test-friend insertion point changed")
    return content.replace(EXISTING_FRIEND, TEST_FRIEND + EXISTING_FRIEND)


def verify(receipt: dict[str, Any], root: Path = ROOT) -> None:
    if receipt.get("schemaVersion") != 2 or receipt.get("issue") != 8286:
        raise ValueError("Second source-tip refresh schema or issue changed")
    base, delta, join, overlay, test_integration = (
        receipt[key] for key in (
            "baseImportHead", "mappedDeltaCommit", "historyJoinCommit", "overlayCommit", "testIntegrationCommit"
        )
    )
    old, new = receipt["oldUpstreamCommits"], receipt["newUpstreamCommits"]
    if set(old) != {"extensions", "studio"} or set(new) != set(old):
        raise ValueError("Second source-tip refresh repository set changed")
    if old["studio"] != new["studio"]:
        raise ValueError("Unexpected Studio change in Extensions-only source refresh")
    if git("rev-list", "--parents", "-n", "1", delta, root=root).split() != [delta, base]:
        raise ValueError("Mapped delta is not a child of the reviewed import head")
    if git("rev-list", "--parents", "-n", "1", join, root=root).split() != [
        join, delta, new["extensions"], new["studio"]
    ]:
        raise ValueError("History join does not preserve both upstream histories")
    if git("rev-list", "--parents", "-n", "1", overlay, root=root).split() != [overlay, join]:
        raise ValueError("Reviewed project transform is not a child of the history join")
    if git("rev-list", "--parents", "-n", "1", test_integration, root=root).split() != [test_integration, overlay]:
        raise ValueError("Test project integration is not a child of the reviewed project transform")
    for commit in (base, delta, join, overlay, test_integration, *old.values(), *new.values()):
        subprocess.run(["git", "merge-base", "--is-ancestor", commit, "HEAD"], cwd=root, check=True)

    expected: dict[str, tuple[str, str]] = {}
    for line in git("diff", "--name-status", old["extensions"], new["extensions"], root=root).splitlines():
        status, source = line.split("\t", 1)
        if status not in {"M", "A"}:
            raise ValueError(f"Unsupported upstream source change: {line}")
        expected[source] = (status, mapped_path(source))
    rows = receipt["mappedChanges"]
    if not isinstance(rows, list) or len(rows) != 14 or len({row["source"] for row in rows}) != 14:
        raise ValueError("Second source-tip refresh must contain fourteen unique mapped changes")
    if {row["source"]: (row["status"], row["mappedPath"]) for row in rows} != expected:
        raise ValueError("Mapped changes differ from the exact upstream source delta")

    transform = receipt["reviewedProjectTransform"]
    transformed_source = "src/modules/persistence/Elsa.Persistence.Dapper/Elsa.Persistence.Dapper.csproj"
    transformed_path = mapped_path(transformed_source)
    test_source = "test/modules/persistence/Elsa.Dapper.UnitTests/Elsa.Dapper.UnitTests.csproj"
    test_path = mapped_path(test_source)
    if transform != {
        "source": transformed_source,
        "mappedPath": transformed_path,
        "reason": "Retain reviewed consolidated project references while adding new upstream test assembly friend access.",
    }:
        raise ValueError("Reviewed Dapper project transform changed")
    if receipt["reviewedTestProjectTransform"] != {
        "source": test_source,
        "mappedPath": test_path,
        "reason": "Point the newly imported Dapper test project at the consolidated Dapper source and include it in canonical Elsa.sln.",
    }:
        raise ValueError("Reviewed Dapper test project transform changed")

    for row in rows:
        source, mapped = row["source"], row["mappedPath"]
        if row["repository"] != "extensions":
            raise ValueError("Unexpected repository in Extensions-only source refresh")
        if row["oldSource"] != blob_and_mode(old["extensions"], source, root):
            raise ValueError(f"Old upstream source changed: {source}")
        if row["oldMapped"] != blob_and_mode(base, mapped, root):
            raise ValueError(f"Old mapped source changed: {mapped}")
        if row["newSource"] != blob_and_mode(new["extensions"], source, root):
            raise ValueError(f"New upstream source changed: {source}")
        if row["newSource"] != blob_and_mode(join, mapped, root):
            raise ValueError(f"History join is not an exact source mapping: {mapped}")
        if row["finalMapped"] != blob_and_mode(test_integration, mapped, root):
            raise ValueError(f"Refreshed mapped file changed at reviewed integration commit: {mapped}")
        if source not in {transformed_source, test_source}:
            if row["oldSource"] != row["oldMapped"] or row["newSource"] != row["finalMapped"]:
                raise ValueError(f"Unreviewed source transform: {mapped}")

    old_source = git_bytes("show", f'{old["extensions"]}:{transformed_source}', root=root)
    new_source = git_bytes("show", f'{new["extensions"]}:{transformed_source}', root=root)
    old_mapped = git_bytes("show", f"{base}:{transformed_path}", root=root)
    final_mapped = git_bytes("show", f"{test_integration}:{transformed_path}", root=root)
    if new_source != with_new_test_friend(old_source) or final_mapped != with_new_test_friend(old_mapped):
        raise ValueError("Dapper project transform differs from the reviewed one-line upstream addition")
    upstream_test = git_bytes("show", f'{new["extensions"]}:{test_source}', root=root)
    active_test = git_bytes("show", f"{test_integration}:{test_path}", root=root)
    if upstream_test.count(OLD_TEST_REFERENCE) != 1 or NEW_TEST_REFERENCE in upstream_test:
        raise ValueError("Upstream Dapper test project reference changed")
    if active_test != upstream_test.replace(OLD_TEST_REFERENCE, NEW_TEST_REFERENCE):
        raise ValueError("Dapper test project has an unreviewed source transform")
    solution = git_bytes("show", f"{test_integration}:Elsa.sln", root=root)
    if solution.count(b'"Elsa.Dapper.UnitTests", "test\\extensions\\modules\\persistence\\Elsa.Dapper.UnitTests\\Elsa.Dapper.UnitTests.csproj"') != 1:
        raise ValueError("New Dapper tests are not in canonical Elsa.sln")

    mapped_paths = {row["mappedPath"] for row in rows}
    if set(git("diff", "--name-only", base, delta, root=root).splitlines()) != mapped_paths:
        raise ValueError("Mapped delta changed paths outside the exact upstream source delta")
    if git("diff", "--name-only", delta, join, root=root):
        raise ValueError("History join changed the mapped source tree")
    if git("diff", "--name-only", join, overlay, root=root).splitlines() != transformed_path.splitlines():
        raise ValueError("Reviewed transform changed more than the Dapper project")
    if set(git("diff", "--name-only", overlay, test_integration, root=root).splitlines()) != {
        test_path, "Elsa.sln"
    }:
        raise ValueError("Test integration changed paths outside the reviewed test project and solution")
    for workflow in (".github/workflows/packages.yml", ".github/workflows/update-wiki.yml"):
        if blob_and_mode(base, workflow, root) != blob_and_mode(join, workflow, root):
            raise ValueError(f"Source-tip refresh changed an active Core publishing workflow: {workflow}")


def main() -> int:
    try:
        receipt = json.loads(RECEIPT.read_text(encoding="utf-8"))
        verify(receipt)
    except (OSError, subprocess.CalledProcessError, ValueError, KeyError, TypeError) as error:
        print(f"Invalid second source-tip refresh: {error}", file=sys.stderr)
        return 1
    print(f"Verified {len(receipt['mappedChanges'])} exact source deltas and the reviewed Dapper project transform")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
