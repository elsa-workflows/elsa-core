#!/usr/bin/env python3
"""Verify the #8293 Dapper atomic-update delta on the draft import against the reviewed #8297 patch."""

from __future__ import annotations

import hashlib
import json
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Any

import verify_import_source_tip_refresh as r1
import verify_import_source_tip_refresh_r5 as r5
from verify_import_source_tip_refresh_r2 import RECEIPT as R2_RECEIPT, blob_and_mode, git, git_bytes


ROOT = Path(__file__).resolve().parents[2]
RECEIPT = ROOT / "doc/integration-program/consolidation/source-tip-refresh-2026-09-27-r6.json"
BASE_IMPORT_HEAD = "1976dd2d4781bad44f13384881e3f00605ba44f9"
DELTA_COMMIT = "2d5d7d1e08773f20edb89c6ccad25b8c81a2e743"
REVIEWED_EXTENSIONS_BASE = "33fa0bfd28c7585240e3d4f665058c067b17e287"
REVIEWED_PATCH = {
    "path": "scripts/integration-program/consolidated-build/dapper-atomic-updates.patch",
    "sha256": "0b73ba77d301b4288de1929bd22cf2d5ffc70947eccc2437a1d8f00392e6aa21",
    "pullRequest": "https://github.com/elsa-workflows/elsa-core/pull/8297",
    "extensionsBaseCommit": REVIEWED_EXTENSIONS_BASE,
    "evidence": "doc/integration-program/consolidation/dapper-atomic-updates-evidence.json",
}
DAPPER = "src/extensions/persistence/Elsa.Persistence.Dapper/"
DAPPER_TESTS = "test/extensions/modules/persistence/Elsa.Persistence.Dapper.UnitTests/"
DIALECT = DAPPER + "Dialects/PostgreSqlDialect.cs"
QUERY = DAPPER + "Extensions/ParameterizedQueryBuilderExtensions.cs"
STORE = DAPPER + "Modules/Management/Stores/DapperWorkflowDefinitionStore.cs"
UPSTREAM_TESTS = DAPPER_TESTS + "DapperWorkflowDefinitionStoreCompareAndSwapTests.cs"
REVIEWED_TESTS = DAPPER_TESTS + "DapperWorkflowDefinitionStoreTests.cs"
TRANSFORMS = {
    DIALECT: "reviewed-patch",
    QUERY: "reviewed-patch",
    STORE: "reviewed-patch-with-superseded-version-probe",
    UPSTREAM_TESTS: "store-constructor-arguments",
    REVIEWED_TESTS: "reviewed-patch-with-superseded-version-case",
}
R1_PATH = str(r1.RECEIPT.relative_to(r1.ROOT))
R2_PATH = str(R2_RECEIPT.relative_to(ROOT))
R5_PATH = str(r5.RECEIPT.relative_to(ROOT))
# Prior receipt -> (mapped paths whose current bytes this receipt now owns, field that pinned them there).
SUPERSEDED = {
    R1_PATH: (frozenset({UPSTREAM_TESTS}), "new"),
    R2_PATH: (frozenset({QUERY}), "finalMapped"),
    R5_PATH: (frozenset(), None),
}
PUBLISHERS = (".github/workflows/packages.yml", ".github/workflows/update-wiki.yml")
PERSISTENCE_TEST_SOLUTION_ENTRY = (
    b'"Elsa.Persistence.Dapper.UnitTests", '
    b'"test\\extensions\\modules\\persistence\\Elsa.Persistence.Dapper.UnitTests\\Elsa.Persistence.Dapper.UnitTests.csproj"'
)

# The only unreviewed store change: a superseded version matching the filter is Conflict (as in the memory, EF Core
# and MongoDB stores), found by a second tenant-scoped read that runs only when the reviewed latest-only read is empty.
REVIEWED_SELECTION = b"""\
            var query = connectionProvider.CreateQuery().From(store.TableName);
            ApplyFilter(query, filter);
            query.Is(nameof(WorkflowDefinitionRecord.IsLatest), true);
            if (!filter.TenantAgnostic)
            {
                query.Is(nameof(WorkflowDefinitionRecord.TenantId), (object?)tenantAccessor?.Tenant?.Id ?? DBNull.Value);
            }

            var record = await connection.QueryFirstOrDefaultAsync<WorkflowDefinitionRecord>(new CommandDefinition(
                query.Sql.ToString(), query.Parameters, transaction, cancellationToken: cancellationToken));
            if (record == null)
            {
                return WorkflowDefinitionUpdateResult.NotFound();
            }
"""
PROBED_SELECTION = b"""\
            var record = await FindInScopeAsync(latestOnly: true);
            if (record == null)
            {
                // A filter can name a version that has since been superseded (for example by Id). As in the
                // memory, EF Core and MongoDB stores, that lost race is a Conflict; only a missing row is NotFound.
                return await FindInScopeAsync(latestOnly: false) == null
                    ? WorkflowDefinitionUpdateResult.NotFound()
                    : WorkflowDefinitionUpdateResult.Conflict();
            }
"""
REVIEWED_COMMIT = b"""\
            return WorkflowDefinitionUpdateResult.Updated(next);
        }
"""
SCOPED_READ = b"""\
            return WorkflowDefinitionUpdateResult.Updated(next);

            Task<WorkflowDefinitionRecord?> FindInScopeAsync(bool latestOnly)
            {
                var query = connectionProvider.CreateQuery().From(store.TableName);
                ApplyFilter(query, filter);
                if (latestOnly)
                {
                    query.Is(nameof(WorkflowDefinitionRecord.IsLatest), true);
                }
                if (!filter.TenantAgnostic)
                {
                    query.Is(nameof(WorkflowDefinitionRecord.TenantId), (object?)tenantAccessor?.Tenant?.Id ?? DBNull.Value);
                }

                return connection.QueryFirstOrDefaultAsync<WorkflowDefinitionRecord?>(new CommandDefinition(
                    query.Sql.ToString(), query.Parameters, transaction, cancellationToken: cancellationToken));
            }
        }
"""
UPSTREAM_CONSTRUCTION = b"        _store = new DapperWorkflowDefinitionStore(store, new JsonPayloadSerializer());\n"
ATOMIC_CONSTRUCTION = (
    b"        _store = new DapperWorkflowDefinitionStore(store, new JsonPayloadSerializer(), connectionProvider, _tenantAccessor);\n"
)
ADDED_CASE_START = b"    [Fact]\n    public async Task SupersededVersionIsConflictOnlyInsideTenantScope()\n"
ADDED_CASE_END = b"    private WorkflowDefinition Edited("
ADDED_CASE_SHA256 = "3c94917470530d8cee7e183f6c953e5e695e41e1223abd249fa754e660f3ff80"


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def replace_once(content: bytes, old: bytes, new: bytes, path: str) -> bytes:
    if content.count(old) != 1:
        raise ValueError(f"Reviewed anchor is not unique: {path}")
    return content.replace(old, new)


def apply_patch(patch: bytes, files: dict[str, bytes], paths: tuple[str, ...]) -> dict[str, bytes]:
    with tempfile.TemporaryDirectory() as directory:
        work = Path(directory)
        subprocess.run(["git", "init", "-q"], cwd=work, check=True, capture_output=True)
        for path, content in files.items():
            (work / path).parent.mkdir(parents=True, exist_ok=True)
            (work / path).write_bytes(content)
        (work / "reviewed.patch").write_bytes(patch)
        subprocess.run(
            ["git", "-c", "core.autocrlf=false", "-c", "apply.whitespace=nowarn", "apply",
             *(f"--include={path}" for path in paths), "reviewed.patch"],
            cwd=work, check=True, capture_output=True,
        )
        return {path: (work / path).read_bytes() for path in paths}


def reviewed_result(patch: bytes, root: Path) -> dict[str, bytes]:
    evidence = json.loads((root / REVIEWED_PATCH["evidence"]).read_text(encoding="utf-8"))
    if evidence.get("patchSha256") != REVIEWED_PATCH["sha256"] or evidence["sourceCommits"]["extensions"] != REVIEWED_EXTENSIONS_BASE:
        raise ValueError("Reviewed Dapper patch no longer matches its recorded evidence")
    recorded = {item["path"]: item["sha256"] for item in evidence["files"]}
    if set(recorded) != {DIALECT, QUERY, STORE, REVIEWED_TESTS}:
        raise ValueError("Reviewed Dapper patch evidence names different files")
    sources = {
        path: git_bytes("show", f"{REVIEWED_EXTENSIONS_BASE}:src/modules/{path.removeprefix('src/extensions/')}", root=root)
        for path in (DIALECT, QUERY, STORE)
    }
    reviewed = apply_patch(patch, sources, tuple(recorded))
    for path, digest in recorded.items():
        if sha256(reviewed[path]) != digest:
            raise ValueError(f"Reviewed Dapper patch does not reproduce its recorded result: {path}")
    return reviewed


def expected_final(path: str, old: bytes | None, final: bytes, patch: bytes, reviewed: dict[str, bytes]) -> bytes:
    if path in (DIALECT, QUERY):
        return apply_patch(patch, {path: old or b""}, (path,))[path]
    if path == STORE:
        probed = replace_once(reviewed[STORE], REVIEWED_SELECTION, PROBED_SELECTION, path)
        return replace_once(probed, REVIEWED_COMMIT, SCOPED_READ, path)
    if path == UPSTREAM_TESTS:
        return replace_once(old or b"", UPSTREAM_CONSTRUCTION, ATOMIC_CONSTRUCTION, path)
    start = final.find(ADDED_CASE_START)
    end = final.find(ADDED_CASE_END, start)
    if start < 0 or end < 0 or final.count(ADDED_CASE_START) != 1 or sha256(final[start:end]) != ADDED_CASE_SHA256:
        raise ValueError(f"Added superseded-version case changed: {path}")
    return reviewed[REVIEWED_TESTS][:start] + final[start:end] + reviewed[REVIEWED_TESTS][start:]


def verify(receipt: dict[str, Any], root: Path = ROOT) -> None:
    if receipt.get("schemaVersion") != 6 or receipt.get("issue") != 8293 or receipt.get("story") != 8286:
        raise ValueError("Sixth source-tip receipt schema or issue changed")
    if receipt.get("publicationPerformed") is not False:
        raise ValueError("Source-tip receipt must not claim a package publication")
    if (receipt["baseImportHead"], receipt["deltaCommit"]) != (BASE_IMPORT_HEAD, DELTA_COMMIT):
        raise ValueError("Reviewed #8293 delta commits changed")
    if receipt["reviewedPatch"] != REVIEWED_PATCH:
        raise ValueError("Reviewed Dapper patch artifact changed")
    if git("rev-list", "--parents", "-n", "1", DELTA_COMMIT, root=root).split() != [DELTA_COMMIT, BASE_IMPORT_HEAD]:
        raise ValueError("#8293 delta is not a child of the reviewed import head")
    for commit in (BASE_IMPORT_HEAD, DELTA_COMMIT, REVIEWED_EXTENSIONS_BASE):
        subprocess.run(["git", "merge-base", "--is-ancestor", commit, "HEAD"], cwd=root, check=True)

    priors = receipt["priorReceipts"]
    if not isinstance(priors, list) or len(priors) != len(SUPERSEDED) or {
        prior["path"]: frozenset(prior["supersededMappedPaths"]) for prior in priors
    } != {path: paths for path, (paths, _) in SUPERSEDED.items()}:
        raise ValueError("Superseded prior receipts changed")
    if (r1.PATHS_SUPERSEDED_BY_R6, r5.R2_PATHS_SUPERSEDED_BY_R6) != (SUPERSEDED[R1_PATH][0], SUPERSEDED[R2_PATH][0]):
        raise ValueError("Older verifier deferral differs from the sixth receipt")
    prior_receipts = {}
    for prior in priors:
        content = (root / prior["path"]).read_bytes()
        if sha256(content) != prior["sha256"]:
            raise ValueError(f"Prior reviewed source-tip receipt changed: {prior['path']}")
        prior_receipts[prior["path"]] = json.loads(content)

    rows = receipt["mappedChanges"]
    if not isinstance(rows, list) or len(rows) != len(TRANSFORMS) or {
        row["path"]: row["transform"] for row in rows
    } != TRANSFORMS:
        raise ValueError("Sixth source-tip receipt must name the five reviewed #8293 files and transforms")
    delta = {}
    for line in git("diff", "--name-status", "--no-renames", BASE_IMPORT_HEAD, DELTA_COMMIT, root=root).splitlines():
        status, path = line.split("\t", 1)
        delta[path] = status
    if delta != {row["path"]: row["status"] for row in rows}:
        raise ValueError("#8293 delta changed unreviewed paths")

    patch = (root / REVIEWED_PATCH["path"]).read_bytes()
    if sha256(patch) != REVIEWED_PATCH["sha256"]:
        raise ValueError("Reviewed Dapper patch bytes changed")
    reviewed = reviewed_result(patch, root)
    for row in rows:
        path = row["path"]
        for key, commit in (("oldMapped", BASE_IMPORT_HEAD), ("finalMapped", DELTA_COMMIT), ("finalMapped", "HEAD")):
            if row[key] != blob_and_mode(commit, path, root):
                raise ValueError(f"Changed {key} at {commit} for {path}")
        old = git_bytes("show", f"{BASE_IMPORT_HEAD}:{path}", root=root) if row["oldMapped"] else None
        final = git_bytes("show", f"{DELTA_COMMIT}:{path}", root=root)
        if final != expected_final(path, old, final, patch, reviewed):
            raise ValueError(f"Unreviewed #8293 transform: {path}")
    for prior_path, (paths, field) in SUPERSEDED.items():
        for path in paths:
            pinned = [item[field] for item in prior_receipts[prior_path]["mappedChanges"] if item["mappedPath"] == path]
            if pinned != [next(row["oldMapped"] for row in rows if row["path"] == path)]:
                raise ValueError(f"Superseded bytes differ from the prior receipt: {path}")

    if receipt["publisherWorkflows"] != {path: blob_and_mode(BASE_IMPORT_HEAD, path, root) for path in PUBLISHERS}:
        raise ValueError("Recorded publisher workflows differ from the reviewed import head")
    # The seventh receipt owns the live HEAD comparison for these two paths; this only checks them
    # through the #8293 delta commit that this receipt reviews.
    for path in PUBLISHERS:
        if blob_and_mode(DELTA_COMMIT, path, root) != receipt["publisherWorkflows"][path]:
            raise ValueError(f"#8293 delta changed active publisher workflow at {DELTA_COMMIT}: {path}")
    if git_bytes("show", "HEAD:Elsa.sln", root=root).count(PERSISTENCE_TEST_SOLUTION_ENTRY) != 1:
        raise ValueError("Current Elsa.sln does not select the reviewed Dapper tests exactly once")
    # Each older receipt still verifies its reviewed commits and every path it did not hand over to this receipt.
    r1.verify(prior_receipts[R1_PATH], root)
    r5.verify(prior_receipts[R5_PATH], root)


def main() -> int:
    try:
        verify(json.loads(RECEIPT.read_text(encoding="utf-8")))
        from verify_import_source_tip_refresh_r7 import RECEIPT as R7_RECEIPT, verify as verify_r7

        verify_r7(json.loads(R7_RECEIPT.read_text(encoding="utf-8")))
    except (OSError, subprocess.CalledProcessError, ValueError, KeyError, TypeError) as error:
        print(f"Invalid sixth source-tip refresh: {error}", file=sys.stderr)
        return 1
    print("Verified the reviewed #8293 Dapper delta, five mapped files, superseded prior pins and current HEAD")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
