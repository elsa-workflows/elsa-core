#!/usr/bin/env python3
"""Build a hash-pinned project/TFM impact receipt from a prepared canonical solution."""

from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import re
import shutil
import subprocess
import tempfile
from collections import Counter
from pathlib import Path, PurePosixPath
from typing import Any

import prepare_consolidated_build as preparation
from package_closure import CHANGED_PROJECT, build_plan
from package_impact import InventoryGraph
from release_unit_manifest import (
    DEFAULT_UNIT_ID,
    get_unit,
    load_manifest,
    map_source_project_path,
)

REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_EVIDENCE = REPOSITORY_ROOT / "doc/integration-program/consolidation/canonical-95a-nuke-test-evidence.json"
DEFAULT_INVENTORY = REPOSITORY_ROOT / "doc/integration-program/inventory/inventory.json"
DEFAULT_MANIFEST = REPOSITORY_ROOT / "doc/integration-program/release-units.json"
SOLUTION_PROJECT = re.compile(r'^Project\("\{[^\n]+?"\) = "([^"]+)", "([^"]+\.csproj)"', re.MULTILINE)
BUILD_PROPS = (
    "Directory.Build.props",
    "Directory.Build.targets",
    "Directory.Packages.props",
    "NuGet.Config",
    "src/Directory.Build.props",
    "build/Build.cs",
)
BUILD_CONFIG_NAMES = {
    "directory.build.props",
    "directory.build.targets",
    "directory.packages.props",
    "directory.solution.props",
    "directory.solution.targets",
    "directory.build.rsp",
    "msbuild.rsp",
    "nuget.config",
    "global.json",
}
SUPPLEMENTAL_PATCH_PATHS = {
    "Dapper atomic updates": "scripts/integration-program/consolidated-build/dapper-atomic-updates.patch",
    "Mongo atomic updates": "scripts/integration-program/consolidated-build/mongo-atomic-updates.patch",
    "Workbench canonical Secrets sample": "scripts/integration-program/consolidated-build/workbench-canonical-secrets.patch",
    "Studio test layout": "scripts/integration-program/consolidated-build/studio-test-layout.patch",
}
CURRENT_TIP_PATCH_PATHS = {
    "workbench-canonical-secrets.patch": "scripts/integration-program/consolidated-build/workbench-canonical-secrets.patch",
    "studio-secrets-menu.patch": "scripts/integration-program/consolidated-build/studio-secrets-menu.patch",
    "studio-bpmn-generator-layout.patch": "scripts/integration-program/consolidated-build/studio-bpmn-generator-layout.patch",
    "workbench-two-tenant-multitenancy.patch": "scripts/integration-program/consolidated-build/workbench-two-tenant-multitenancy.patch",
    "workbench-secrets-route-probe.patch": "scripts/integration-program/consolidated-build/workbench-secrets-route-probe.patch",
    "studio-test-layout-current-tip.patch": "scripts/integration-program/consolidated-build/studio-test-layout-current-tip.patch",
}
CURRENT_TIP_IMPORT_RECEIPT_SHA256 = "dee44fd3d8027765e2cc8107a3b6e9970797cc0ec74cd484c21d7985f931cea8"
CURRENT_TIP_PREPARATION_RECEIPT_SHA256 = "80396b7354bf42761e31853d683887c5336898db5aca9696a6c5613576d4bd26"
CURRENT_TIP_SOURCE_COMMITS = {
    "core": "c4b3ce150160e3c9062b57f7b158fd6b968e1631",
    "extensions": "ba8b71d91c15ffe5be4b2c539cf9f712e74af775",
    "studio": "20ceaeeed7e671f0c9662003e82063026f2216de",
}
E96_SOURCE_COMMITS = {
    "core": "e96fd36f4f9a838c6289fd48cf07669ea3229a5e",
    "extensions": "ba8b71d91c15ffe5be4b2c539cf9f712e74af775",
    "studio": "20ceaeeed7e671f0c9662003e82063026f2216de",
}
CURRENT_TIP_PROFILES = {
    "current-tip-c4b3": {
        "sourceCommits": CURRENT_TIP_SOURCE_COMMITS,
        "evidenceDirectory": "current-tip-c4b3-evidence",
        "importReceiptSha256": CURRENT_TIP_IMPORT_RECEIPT_SHA256,
        "preparationReceiptSha256": CURRENT_TIP_PREPARATION_RECEIPT_SHA256,
    },
    "current-tip-e96": {
        "sourceCommits": E96_SOURCE_COMMITS,
        "evidenceDirectory": "current-tip-e96-evidence",
        "importReceiptSha256": "06cd198a338d5c6d49fa6b0183bbda6b602252f39f622f18084e60342880bb75",
        "preparationReceiptSha256": "219fcafe45959bb8f9295d8b9137f8ae0807489f0370b5112204f228fc7bd7bc",
    },
}


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read_json(path: Path) -> dict[str, Any]:
    payload = path.read_bytes()
    if path.suffix == ".gz":
        payload = gzip.decompress(payload)
    value = json.loads(payload)
    if not isinstance(value, dict):
        raise ValueError(f"Expected a JSON object in {path}")
    return value


def ancestor_build_configs(root: Path, project_paths: set[str]) -> list[str]:
    """Hash repository build/restore config files inherited by solution projects."""
    root = root.resolve()
    discovered: set[str] = set()
    for project_path in project_paths:
        current = (root / project_path).parent.resolve()
        while current == root or root in current.parents:
            for candidate in current.iterdir():
                if candidate.is_file() and candidate.name.casefold() in BUILD_CONFIG_NAMES:
                    discovered.add(candidate.relative_to(root).as_posix())
            if current == root:
                break
            current = current.parent
    return sorted(discovered)


def git_output(path: Path, *args: str) -> str:
    result = subprocess.run(
        ["git", "-C", str(path), *args], capture_output=True, text=True, check=False,
    )
    if result.returncode:
        raise ValueError(f"git {' '.join(args)} failed in {path}: {result.stderr.strip()}")
    return result.stdout.strip()


def _relative_path(root: Path, absolute_path: str | Path) -> str:
    candidate = Path(absolute_path).resolve()
    try:
        relative = candidate.relative_to(root.resolve()).as_posix()
    except ValueError as error:
        raise ValueError(f"Project reference escapes the prepared source tree: {absolute_path}") from error
    if PurePosixPath(relative).is_absolute() or ".." in PurePosixPath(relative).parts:
        raise ValueError(f"Project path is not normalized: {relative}")
    return relative


def parse_solution(solution_path: Path) -> list[tuple[str, str]]:
    entries: list[tuple[str, str]] = []
    paths: set[str] = set()
    test_names: set[str] = set()
    for name, raw_path in SOLUTION_PROJECT.findall(solution_path.read_text(encoding="utf-8")):
        path = PurePosixPath(raw_path.replace("\\", "/")).as_posix()
        if path in paths:
            raise ValueError(f"Duplicate project path in solution: {path}")
        if name.endswith("Tests") and name in test_names:
            raise ValueError(f"Duplicate selected test project name in solution: {name}")
        if name.endswith("Tests"):
            test_names.add(name)
        entries.append((name, path))
        paths.add(path)
    if not entries:
        raise ValueError(f"No C# projects were found in {solution_path}")
    return entries


def graph_from_edges(
    project_name_by_path: dict[str, str],
    edges_by_path: dict[str, list[str]],
    test_project_paths: set[str] | None = None,
) -> InventoryGraph:
    """Use the shared impact-closure implementation over one restored TFM graph."""
    known_paths = set(project_name_by_path)
    unknown = sorted({edge for edges in edges_by_path.values() for edge in edges} - known_paths)
    if unknown:
        raise ValueError(f"Restored project references target projects absent from Elsa.sln: {unknown}")
    rows = []
    for path, name in project_name_by_path.items():
        project_path = path.rsplit("@@", 1)[0] if "@@" in path else path
        is_test_project = project_path in test_project_paths if test_project_paths is not None else name.endswith("Tests")
        rows.append({
            "path": path,
            "is_test_project": is_test_project,
            "project_references": [{"target_project": reference} for reference in edges_by_path.get(path, [])],
            "package_references": [],
        })
    return InventoryGraph({
        "repositories": {"elsa-core": {"slug": "elsa-core"}},
        "project_inventory": {"elsa-core": rows},
    })


def _assets_for_project(root: Path, project_path: str) -> tuple[Path, dict[str, Any]]:
    project_file = root / project_path
    if not project_file.is_file():
        raise ValueError(f"Solution project is missing from prepared source: {project_path}")
    assets_path = project_file.parent / "obj/project.assets.json"
    if not assets_path.is_file():
        raise ValueError(f"Prepared restore is missing project.assets.json: {project_path}")
    document = json.loads(assets_path.read_text(encoding="utf-8"))
    restored_project = Path(document.get("project", {}).get("restore", {}).get("projectPath", "")).resolve()
    if restored_project != project_file.resolve():
        raise ValueError(f"Restore project path does not match the solution entry: {project_path}")
    frameworks = document.get("project", {}).get("restore", {}).get("frameworks")
    if not isinstance(frameworks, dict) or not frameworks:
        raise ValueError(f"Restore graph has no target frameworks: {project_path}")
    return assets_path, document


def _tfm_moniker(target_framework: str) -> str:
    match = re.fullmatch(r"net(\d+)\.(\d+)", target_framework)
    if match is None:
        raise ValueError(f"Unsupported restored .NET target framework alias: {target_framework}")
    return f".NETCoreApp,Version=v{match.group(1)}.{match.group(2)}"


def _validated_framework_summary(evidence: dict[str, Any]) -> dict[str, Any]:
    """Recompute global totals and reject a stale summary header."""
    summary = evidence["observedTestResults"]["frameworkConsoleSummaries"]
    runs = summary.get("runs")
    if not isinstance(runs, list) or not runs:
        raise ValueError("Framework console evidence has no per-framework run rows")

    totals = Counter({"passed": 0, "failed": 0, "skipped": 0, "total": 0})
    for run in runs:
        if run.get("status") != "Passed!":
            raise ValueError(f"Framework console run did not pass: {run.get('assembly')} ({run.get('framework')})")
        counts = {}
        for field in ("passed", "failed", "skipped", "total"):
            value = run.get(field)
            if isinstance(value, bool) or not isinstance(value, int) or value < 0:
                raise ValueError(f"Framework console run has invalid {field} count: {run.get('assembly')} ({run.get('framework')})")
            counts[field] = value
        if counts["failed"] != 0:
            raise ValueError(f"Framework console run reports failures: {run.get('assembly')} ({run.get('framework')})")
        if counts["total"] != counts["passed"] + counts["failed"] + counts["skipped"]:
            raise ValueError(f"Framework console run totals do not reconcile: {run.get('assembly')} ({run.get('framework')})")
        totals.update(counts)

    expected_totals = dict(totals)
    if summary.get("assemblyFrameworkRuns") != len(runs):
        raise ValueError("Framework summary run count differs from the detailed run rows")
    if summary.get("totalsFromPassedSummaries") != expected_totals:
        raise ValueError("Framework summary header totals differ from recomputed per-framework run rows")
    return {"totals": expected_totals, "runCount": len(runs)}


def _validated_retained_trx_summary(evidence: dict[str, Any]) -> dict[str, int]:
    """Recompute retained TRX totals and reject a stale aggregate header."""
    retained = evidence["observedTestResults"]["retainedTrx"]
    rows = retained.get("rows")
    if not isinstance(rows, list):
        raise ValueError("Retained TRX evidence has no row list")

    totals = Counter({"passed": 0, "failed": 0, "executed": 0, "totalIncludingSkipped": 0})
    seen_files: set[str] = set()
    for row in rows:
        file_name = row.get("file")
        if not isinstance(file_name, str) or not file_name or file_name in seen_files:
            raise ValueError(f"Retained TRX evidence has a missing or duplicate file name: {file_name}")
        seen_files.add(file_name)
        counters = row.get("counters")
        if not isinstance(counters, dict):
            raise ValueError(f"Retained TRX evidence has no counters: {file_name}")
        values = {}
        for field in ("total", "executed", "passed", "failed", "error", "timeout", "aborted"):
            value = counters.get(field)
            if isinstance(value, bool) or not isinstance(value, int) or value < 0:
                raise ValueError(f"Retained TRX row has invalid {field} count: {file_name}")
            values[field] = value
        if values["executed"] > values["total"]:
            raise ValueError(f"Retained TRX executed count exceeds total: {file_name}")
        completed = sum(values[field] for field in ("passed", "failed", "error", "timeout", "aborted"))
        if completed != values["executed"]:
            raise ValueError(f"Retained TRX execution counters do not reconcile: {file_name}")
        if any(values[field] for field in ("failed", "error", "timeout", "aborted")):
            raise ValueError(f"Retained TRX row reports a failed or incomplete test: {file_name}")
        totals.update({
            "passed": values["passed"],
            "failed": values["failed"],
            "executed": values["executed"],
            "totalIncludingSkipped": values["total"],
        })

    expected = {
        "files": len(rows),
        "passed": totals["passed"],
        "failed": totals["failed"],
        "skipped": totals["totalIncludingSkipped"] - totals["executed"],
        "executed": totals["executed"],
        "totalIncludingSkipped": totals["totalIncludingSkipped"],
    }
    if {field: retained.get(field) for field in expected} != expected:
        raise ValueError("Retained TRX summary header totals differ from recomputed row counters")
    return expected


def _node_key(project_path: str, framework: str) -> str:
    return f"{project_path}@@{framework}"


def _framework_graph(
    root: Path,
    project_name_by_path: dict[str, str],
    asset_documents: dict[str, dict[str, Any]],
    test_project_paths: set[str] | None = None,
) -> tuple[InventoryGraph, dict[str, tuple[str, str]], dict[tuple[str, str], list[tuple[str, str]]]]:
    """Create a graph whose edges retain the target framework selected by restore."""
    node_to_project_framework: dict[str, tuple[str, str]] = {}
    graph_names: dict[str, str] = {}
    edges: dict[str, list[str]] = {}
    edge_rows: dict[tuple[str, str], list[tuple[str, str]]] = {}

    for project_path, document in asset_documents.items():
        frameworks = document["project"]["restore"]["frameworks"]
        for source_framework in frameworks:
            node = _node_key(project_path, source_framework)
            node_to_project_framework[node] = (project_path, source_framework)
            graph_names[node] = project_name_by_path[project_path]
            edges[node] = []
            edge_rows[(project_path, source_framework)] = []

    for project_path, document in asset_documents.items():
        restore_frameworks = document["project"]["restore"]["frameworks"]
        for source_framework, restored in restore_frameworks.items():
            references = restored.get("projectReferences", {})
            if not isinstance(references, dict):
                raise ValueError(f"Restore ProjectReferences are not an object for {project_path} ({source_framework})")
            target_assets = document.get("targets", {}).get(source_framework)
            libraries = document.get("libraries", {})
            if not isinstance(target_assets, dict) or not isinstance(libraries, dict):
                raise ValueError(f"Restore target libraries are missing for {project_path} ({source_framework})")
            for reference_path, reference in references.items():
                if not isinstance(reference, dict):
                    raise ValueError(f"Invalid project reference in restore graph for {project_path} ({source_framework})")
                target_path_value = reference.get("projectPath", reference_path)
                if not isinstance(target_path_value, str) or not target_path_value:
                    raise ValueError(f"Restore graph has an empty ProjectReference for {project_path} ({source_framework})")
                target_path = _relative_path(root, target_path_value)
                target_project_assets = asset_documents.get(target_path)
                if target_project_assets is None:
                    raise ValueError(f"Restored project reference is absent from the canonical solution: {target_path}")

                matching_target_libraries = []
                source_directory = (root / project_path).parent
                for library_key, library in libraries.items():
                    target_entry = target_assets.get(library_key)
                    if library.get("type") != "project" or not isinstance(target_entry, dict) or target_entry.get("type") != "project":
                        continue
                    library_path = library.get("msbuildProject") or library.get("path")
                    if not isinstance(library_path, str) or not library_path:
                        continue
                    resolved_library_path = _relative_path(root, source_directory / library_path)
                    if resolved_library_path == target_path:
                        matching_target_libraries.append(target_entry)
                if len(matching_target_libraries) != 1:
                    raise ValueError(
                        f"Expected one restored library target for {project_path} -> {target_path} ({source_framework}); "
                        f"found {len(matching_target_libraries)}"
                    )
                target_moniker = matching_target_libraries[0].get("framework")
                if not isinstance(target_moniker, str) or not target_moniker:
                    raise ValueError(f"Restored project target has no framework metadata: {project_path} -> {target_path}")
                candidates = [
                    framework for framework in target_project_assets["project"]["restore"]["frameworks"]
                    if _tfm_moniker(framework) == target_moniker
                ]
                if len(candidates) != 1:
                    raise ValueError(
                        f"Cannot uniquely map restored target framework {target_moniker!r} to {target_path}: {candidates}"
                    )
                target_framework = candidates[0]
                target_node = _node_key(target_path, target_framework)
                edges[_node_key(project_path, source_framework)].append(target_node)
                edge_rows[(project_path, source_framework)].append((target_path, target_framework))

    return graph_from_edges(graph_names, edges, test_project_paths), node_to_project_framework, edge_rows


def _include_restored_project_references(
    root: Path,
    project_name_by_path: dict[str, str],
    asset_documents: dict[str, dict[str, Any]],
) -> None:
    """Load transitive restored ProjectReferences even when they are outside Elsa.sln."""
    pending = list(project_name_by_path)
    while pending:
        source_path = pending.pop()
        document = asset_documents[source_path]
        restore_frameworks = document.get("project", {}).get("restore", {}).get("frameworks", {})
        if not isinstance(restore_frameworks, dict) or not restore_frameworks:
            raise ValueError(f"Restore graph has no target frameworks: {source_path}")
        for source_framework, restored in restore_frameworks.items():
            references = restored.get("projectReferences", {})
            if not isinstance(references, dict):
                raise ValueError(f"Restore ProjectReferences are not an object for {source_path} ({source_framework})")
            for reference_path, reference in references.items():
                if not isinstance(reference, dict):
                    raise ValueError(f"Invalid project reference in restore graph for {source_path} ({source_framework})")
                target_path_value = reference.get("projectPath", reference_path)
                if not isinstance(target_path_value, str) or not target_path_value:
                    raise ValueError(f"Restore graph has an empty ProjectReference for {source_path} ({source_framework})")
                target_path = _relative_path(root, target_path_value)
                if target_path in asset_documents:
                    continue
                _, target_assets = _assets_for_project(root, target_path)
                project_name_by_path[target_path] = Path(target_path).stem
                asset_documents[target_path] = target_assets
                pending.append(target_path)


def _parse_test_run_evidence(evidence: dict[str, Any], root: Path, projects: list[tuple[str, str]]) -> dict[str, Any]:
    framework_summary = _validated_framework_summary(evidence)
    retained_trx_summary = _validated_retained_trx_summary(evidence)
    selection = evidence["selection"]
    expected_entries = sorted(
        ({"name": name, "path": path} for name, path in projects if name.endswith("Tests")),
        key=lambda row: row["path"],
    )
    actual_entries = sorted(selection.get("projects", []), key=lambda row: row.get("path", ""))
    if actual_entries != expected_entries:
        raise ValueError("NUKE selected project path/name receipt differs from canonical Elsa.sln suffix selection")
    if selection.get("canonicalSolutionProjectEntries") != len(projects):
        raise ValueError("Run evidence solution project count differs from prepared Elsa.sln")
    if selection.get("nukeSelectedTestProjects") != len(expected_entries):
        raise ValueError("Run evidence test project count differs from canonical NUKE selection")
    if selection.get("commandsObserved") != len(expected_entries) or selection.get("uniqueCommandProjectPaths") != len(expected_entries):
        raise ValueError("Run evidence does not contain one unique Test command for every selected project")

    invocation = evidence["invocation"]
    recorded_root_text = invocation.get("workingDirectory", str(root))
    recorded_root = Path(recorded_root_text)
    if not recorded_root.is_absolute() or ".." in recorded_root.parts:
        raise ValueError("Canonical Test evidence working directory is not a clean absolute path")
    recorded_roots = [recorded_root]
    # The retained macOS run used /tmp for its working directory while VSTest
    # recorded the same volume under /private/tmp. Compare these recorded
    # paths lexically so a Linux recheck does not depend on the host's aliases.
    if recorded_root.parts[:2] == ("/", "tmp"):
        recorded_roots.append(Path("/private") / recorded_root.relative_to("/"))
    elif recorded_root.parts[:3] == ("/", "private", "tmp"):
        recorded_roots.append(Path("/") / recorded_root.relative_to("/private"))
    targets = invocation.get("targets", {})
    if invocation.get("exitCode") != 0 or any(targets.get(name) != "succeeded" for name in ("restore", "compile", "test")):
        raise ValueError("Canonical Restore/Compile/Test evidence is not successful")
    if any(targets.get(name) for name in ("pack", "push", "publish")):
        raise ValueError("Canonical Test evidence unexpectedly records a package or publication target")

    names_by_path = {path: name for name, path in projects}
    test_by_directory = {str(Path(path).parent): path for name, path in projects if name.endswith("Tests")}
    test_path_by_name = {name: path for name, path in projects if name.endswith("Tests")}
    trx_by_path_framework: dict[tuple[str, str], dict[str, Any]] = {}
    assembly_to_path: dict[str, str] = {}
    for row in evidence["observedTestResults"]["retainedTrx"].get("rows", []):
        code_bases = row.get("codeBases", [])
        if not code_bases:
            raise ValueError(f"Retained TRX has no codeBase path: {row.get('file')}")
        resolved_records = set()
        for code_base in code_bases:
            recorded_assembly = Path(code_base)
            if not recorded_assembly.is_absolute() or ".." in recorded_assembly.parts:
                raise ValueError(f"Retained TRX codeBase is not a clean absolute path: {code_base}")
            source_relative = None
            for candidate in recorded_roots:
                try:
                    source_relative = recorded_assembly.relative_to(candidate)
                    break
                except ValueError:
                    continue
            if source_relative is None:
                raise ValueError(f"Retained TRX codeBase escapes recorded rehearsal: {code_base}")
            relative = _relative_path(root, root / source_relative)
            parts = PurePosixPath(relative).parts
            if "bin" not in parts:
                raise ValueError(f"TRX codeBase is outside a project bin directory: {code_base}")
            bin_index = parts.index("bin")
            project_directory = PurePosixPath(*parts[:bin_index]).as_posix()
            path = test_by_directory.get(project_directory)
            if path is None:
                raise ValueError(f"TRX assembly does not map to a selected test project: {code_base}")
            framework = parts[bin_index + 2] if len(parts) > bin_index + 2 else ""
            assembly = PurePosixPath(parts[-1]).stem
            expected_name = names_by_path[path]
            if assembly != expected_name:
                raise ValueError(f"TRX assembly name differs from Elsa.sln entry for {path}: {assembly}")
            resolved_records.add((path, framework, assembly))
        if len(resolved_records) != 1:
            raise ValueError(f"Retained TRX codeBases do not identify one project/TFM: {row.get('file')}")
        path, framework, assembly = next(iter(resolved_records))
        key = (path, framework)
        if key in trx_by_path_framework:
            raise ValueError(f"Duplicate retained TRX for {path} ({framework})")
        trx_by_path_framework[key] = row["counters"]
        previous = assembly_to_path.setdefault(assembly, path)
        if previous != path:
            raise ValueError(f"Test assembly name maps to multiple project paths: {assembly}")

    run_by_path_framework: dict[tuple[str, str], dict[str, Any]] = {}
    for run in evidence["observedTestResults"]["frameworkConsoleSummaries"].get("runs", []):
        assembly = Path(run.get("assembly", "")).stem
        path = test_path_by_name.get(assembly)
        if path is None or not assembly.endswith("Tests"):
            raise ValueError(f"Framework run summary does not map to a selected NUKE test project: {assembly}")
        key = (path, run.get("framework", ""))
        if key in run_by_path_framework:
            raise ValueError(f"Duplicate framework run summary for {path} ({key[1]})")
        run_by_path_framework[key] = run

    no_pass_rows = {row["path"]: row["outcome"] for row in evidence["observedTestResults"].get("selectedProjectsWithoutPassingTestCases", [])}
    return {
        "runByPathFramework": run_by_path_framework,
        "trxByPathFramework": trx_by_path_framework,
        "selectedWithoutPass": no_pass_rows,
        "frameworkSummaryTotals": framework_summary["totals"],
        "frameworkSummaryCount": framework_summary["runCount"],
        "retainedTrxSummary": retained_trx_summary,
    }


def _classify_test_observation(
    path: str,
    framework: str,
    run_evidence: dict[str, Any],
    framework_dependencies: list[str],
) -> dict[str, Any]:
    run = run_evidence["runByPathFramework"].get((path, framework))
    if run is not None:
        if run.get("status") != "Passed!" or run.get("failed", 0) != 0:
            raise ValueError(f"Affected project/TFM did not pass: {path} ({framework})")
        return {"path": path, "framework": framework, "status": "passed", "passed": run["passed"], "skipped": run["skipped"], "source": "framework-console-summary"}

    counters = run_evidence["trxByPathFramework"].get((path, framework))
    if counters is not None and counters.get("executed") == 0:
        explanation = run_evidence["selectedWithoutPass"].get(path, "")
        current_skip = (
            f"Selected; retained TRX records {counters.get('total')} case(s) with zero executed tests; "
            "NUKE summary was Skipped!."
        )
        if (counters.get("total") == 1 and counters.get("passed") == 0 and counters.get("failed") == 0
                and (explanation == current_skip or "explicitly skipped" in explanation)):
            return {"path": path, "framework": framework, "status": "selected-but-skipped", "passed": 0, "skipped": counters["total"], "source": "retained-trx-and-run-evidence", "explanation": explanation}

    if "Microsoft.NET.Test.Sdk" not in framework_dependencies:
        explanation = run_evidence["selectedWithoutPass"].get(path, "")
        if "no test run or TRX" in explanation:
            return {"path": path, "framework": framework, "status": "selected-built-no-test-sdk", "passed": 0, "skipped": 0, "source": "run-evidence-and-restored-package-dependencies", "explanation": explanation}

    raise ValueError(f"Affected project/TFM has no matching passing summary or explicit skip evidence: {path} ({framework})")


def evaluate_msbuild_properties(project_file: Path, framework: str, timeout_seconds: int = 120) -> dict[str, Any]:
    """Evaluate the patched Workbench project without restoring or building it."""
    records = []
    for configuration in ("Debug", "Release"):
        for mode, global_property in (("default", None), ("source", "true"), ("package", "false")):
            command = [
                "dotnet", "msbuild", str(project_file), "-nologo",
                "-getProperty:PackageId,AssemblyName,TargetFramework,IsPackable,GeneratePackageOnBuild,UseProjectReferences",
                "-getItem:ProjectReference,PackageReference",
                f"-property:Configuration={configuration}", f"-property:TargetFramework={framework}",
            ]
            if global_property is not None:
                command.append(f"-property:UseProjectReferences={global_property}")
            try:
                result = subprocess.run(command, cwd=project_file.parent, capture_output=True, text=True, check=False, timeout=timeout_seconds)
            except subprocess.TimeoutExpired as error:
                raise ValueError(f"Workbench property evaluation timed out in {configuration}/{mode}") from error
            if result.returncode:
                raise ValueError(f"Workbench property evaluation failed in {configuration}/{mode}: {result.stderr.strip()}")
            try:
                output = json.loads(result.stdout)
                properties = output["Properties"]
                items = output["Items"]
                project_references = sorted(item["FullPath"] for item in items["ProjectReference"])
                package_references = sorted(item["Identity"] for item in items["PackageReference"])
            except (json.JSONDecodeError, KeyError, TypeError) as error:
                raise ValueError(f"Workbench property evaluation returned invalid JSON in {configuration}/{mode}") from error
            if not properties.get("PackageId", "").strip():
                raise ValueError(f"Workbench PackageId is empty in {configuration}/{mode}")
            if properties.get("IsPackable", "").casefold() != "false" or properties.get("GeneratePackageOnBuild", "").casefold() != "false":
                raise ValueError(f"Workbench sample became packageable in {configuration}/{mode}")
            if properties.get("TargetFramework") != framework:
                raise ValueError(f"Workbench evaluated an unexpected target framework in {configuration}/{mode}")
            records.append({
                "configuration": configuration,
                "referenceMode": mode,
                "properties": properties,
                "projectReferenceCount": len(project_references),
                "projectReferencesSha256": hashlib.sha256("\n".join(project_references).encode()).hexdigest(),
                "packageReferenceCount": len(package_references),
                "packageReferencesSha256": hashlib.sha256("\n".join(package_references).encode()).hexdigest(),
            })
    package_ids = {row["properties"]["PackageId"] for row in records}
    assembly_names = {row["properties"]["AssemblyName"] for row in records}
    if len(package_ids) != 1 or len(assembly_names) != 1 or len(records) != 6:
        raise ValueError("Workbench identity or evaluation coverage is unstable across the six property cases")
    reference_mode_interpretation = (
        "All six evaluations retained the same explicit project/package reference item sets. Setting UseProjectReferences=false changes the evaluated property but does not convert this sample's explicit ProjectReference items; this is not package-mode restore evidence."
        if len({row["projectReferencesSha256"] for row in records}) == 1 and len({row["packageReferencesSha256"] for row in records}) == 1
        else "Reference item sets vary by configuration or mode and are recorded per case."
    )
    return {
        "framework": framework,
        "evaluationCount": len(records),
        "properties": records,
        "referenceModeInterpretation": reference_mode_interpretation,
        "scope": "MSBuild evaluation only; no build, restore, Pack, or package-mode restore was run.",
    }


def _validate_overlay_receipt(path: Path, source_commits: dict[str, str] = CURRENT_TIP_SOURCE_COMMITS) -> list[dict[str, str]]:
    receipt = read_json(path)
    if receipt.get("sourcePins") != source_commits:
        raise ValueError("Reviewed overlay receipt does not pin the selected source profile")
    if receipt.get("publicationAuthorized") is not False:
        raise ValueError("Reviewed overlay receipt must keep package publication unauthorized")
    rows = receipt.get("reviewedOverlayReceipt")
    if not isinstance(rows, list) or not rows:
        raise ValueError("Reviewed overlay receipt has no patches")
    names: set[str] = set()
    normalized = []
    for row in rows:
        if not isinstance(row, dict):
            raise ValueError("Reviewed overlay receipt contains an invalid patch row")
        name, digest = row.get("name"), row.get("sha256")
        if not isinstance(name, str) or name not in CURRENT_TIP_PATCH_PATHS or name in names:
            raise ValueError(f"Reviewed overlay receipt has an unknown or duplicate patch: {name}")
        if not isinstance(digest, str) or re.fullmatch(r"[0-9a-f]{64}", digest) is None:
            raise ValueError(f"Reviewed overlay receipt has an invalid patch hash: {name}")
        patch_path = REPOSITORY_ROOT / CURRENT_TIP_PATCH_PATHS[name]
        if not patch_path.is_file() or sha256(patch_path) != digest:
            raise ValueError(f"Reviewed overlay patch hash differs from tooling checkout: {name}")
        names.add(name)
        normalized.append({"name": name, "sha256": digest})
    if [row["name"] for row in normalized] != list(CURRENT_TIP_PATCH_PATHS):
        raise ValueError("Reviewed overlay receipt must list the complete current-tip patch set in order")
    return normalized


def _validate_overlay_build_receipt(path: Path, overlay_patches: list[dict[str, str]], rehearsal_commit: str,
                                    source_commits: dict[str, str] = CURRENT_TIP_SOURCE_COMMITS) -> dict[str, Any]:
    receipt = read_json(path)
    if receipt.get("sourcePins") != source_commits:
        raise ValueError("Overlay build receipt does not pin the selected source profile")
    if receipt.get("syntheticRehearsalCommit") != rehearsal_commit:
        raise ValueError("Overlay build receipt rehearsal commit differs from prepared source")
    if receipt.get("canonicalImportBuilt") is not False or receipt.get("fullCombinedTestSuiteVerified") is not False:
        raise ValueError("Accepted overlay build receipt has unexpected canonical-import or full-suite claims")
    overlaid = receipt.get("builds", {}).get("overlaid", {})
    if overlaid.get("exitCode") != 0 or overlaid.get("errorCount") != 0:
        raise ValueError("Accepted overlay build receipt does not record a passing overlaid build")
    applied = overlaid.get("appliedOverlays")
    if applied != overlay_patches:
        raise ValueError("Applied overlay list differs from the reviewed overlay receipt")
    return receipt


def _validate_test_profile_pins(
    evidence: dict[str, Any],
    source_commits: dict[str, str],
    rehearsal_commit: str,
    source_receipts: dict[str, str],
    overlay_patches: list[dict[str, str]],
    overlay_receipt_sha256: str,
) -> dict[str, str]:
    profile = evidence.get("profile", {})
    expected_profile = {
        **source_commits,
        "rawRehearsalCommit": rehearsal_commit,
        "canonicalSolution": "Elsa.sln",
    }
    if any(profile.get(key) != value for key, value in expected_profile.items()):
        raise ValueError("Canonical NUKE run evidence differs from the current prepared source profile")
    if profile.get("sourceReceipts") != source_receipts:
        raise ValueError("Canonical NUKE run evidence does not hash the current source receipts")
    if profile.get("supplementalPatches") != overlay_patches:
        raise ValueError("Canonical NUKE run evidence supplemental patches differ from the reviewed overlay receipt")
    if profile.get("overlayReceiptSha256") != overlay_receipt_sha256:
        raise ValueError("Canonical NUKE run evidence does not hash the reviewed overlay receipt")
    return source_receipts


def _verify_overlays_applied(root: Path, overlays: list[dict[str, str]], prepared_files: list[dict[str, str]]) -> set[str]:
    """Reverse reviewed overlays and compare every touched file with the prepared source."""
    patch_paths = [REPOSITORY_ROOT / CURRENT_TIP_PATCH_PATHS[row["name"]] for row in overlays]
    touched_paths: set[str] = set()
    for patch_path in patch_paths:
        for raw_path in re.findall(r"^diff --git a/(.+?) b/", patch_path.read_text(encoding="utf-8"), re.MULTILINE):
            normalized = PurePosixPath(raw_path)
            if normalized.is_absolute() or ".." in normalized.parts:
                raise ValueError(f"Reviewed overlay contains an unsafe path: {raw_path}")
            touched_paths.add(normalized.as_posix())
    if not touched_paths:
        raise ValueError("Reviewed overlays contain no file paths")
    prepared_hashes = {row["path"]: row["sha256"] for row in prepared_files}

    with tempfile.TemporaryDirectory(prefix="elsa-overlay-check-") as temp:
        temporary_root = Path(temp)
        for relative in touched_paths:
            source = (root / relative).resolve()
            try:
                source.relative_to(root.resolve())
            except ValueError as error:
                raise ValueError(f"Reviewed overlay path escapes the prepared source tree: {relative}") from error
            if not source.is_file():
                raise ValueError(f"Prepared source is missing an overlaid file: {relative}")
            target = temporary_root / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, target)
        subprocess.run(["git", "-C", str(temporary_root), "init", "-q"], check=True, capture_output=True, text=True)
        for patch_path in reversed(patch_paths):
            result = subprocess.run(
                ["git", "-C", str(temporary_root), "apply", "--reverse", str(patch_path)],
                capture_output=True,
                text=True,
                check=False,
            )
            if result.returncode:
                raise ValueError(f"Prepared source does not contain the reviewed overlay series: {result.stderr.strip()}")
        for relative in touched_paths:
            reverted = temporary_root / relative
            if relative in prepared_hashes:
                if not reverted.is_file() or sha256(reverted) != prepared_hashes[relative]:
                    raise ValueError(f"Reversed overlay differs from the prepared file receipt: {relative}")
                continue
            baseline = subprocess.run(
                ["git", "-C", str(root), "show", f"HEAD:{relative}"],
                capture_output=True,
                check=False,
            )
            if baseline.returncode:
                if reverted.exists():
                    raise ValueError(f"Reversed overlay left a file absent from the imported source: {relative}")
            elif not reverted.is_file() or hashlib.sha256(reverted.read_bytes()).digest() != hashlib.sha256(baseline.stdout).digest():
                raise ValueError(f"Reversed overlay differs from the imported source: {relative}")
    return touched_paths


def _verify_prepared_source_files(root: Path, prepared_files: list[dict[str, str]], overlay_paths: set[str]) -> None:
    """Reject changes outside the exact prepared source and reviewed overlay series."""
    prepared_hashes = {row["path"]: row["sha256"] for row in prepared_files}
    if len(prepared_hashes) != len(prepared_files):
        raise ValueError("Prepared source receipt repeats a file path")
    for relative, expected_hash in prepared_hashes.items():
        if relative in overlay_paths:
            continue  # Reversing every overlay already checks this file against its prepared hash.
        source = root / relative
        if not source.is_file() or source.is_symlink() or sha256(source) != expected_hash:
            raise ValueError(f"Prepared source file differs from its accepted receipt: {relative}")

    permitted = set(prepared_hashes) | overlay_paths
    changed = set(git_output(root, "diff", "HEAD", "--name-only").splitlines())
    unexpected_changes = changed - permitted
    if unexpected_changes:
        raise ValueError(f"Imported source differs outside reviewed preparation and overlays: {sorted(unexpected_changes)}")
    untracked = set(git_output(root, "ls-files", "--others", "--exclude-standard").splitlines())
    unexpected_untracked = untracked - permitted - {"import-receipt.json", "consolidated-build-receipt.json"}
    if unexpected_untracked:
        raise ValueError(f"Unreviewed files were added to the prepared source: {sorted(unexpected_untracked)}")


def _validate_current_tip_profile(
    root: Path,
    evidence: dict[str, Any] | None,
    overlay_receipt_path: Path,
    overlay_build_receipt_path: Path | None,
    source_profile: str = "current-tip-c4b3",
) -> dict[str, Any]:
    """Pin a reviewed source preparation/import and optional full NUKE evidence."""
    profile = CURRENT_TIP_PROFILES[source_profile]
    prep_receipt_path = root / "consolidated-build-receipt.json"
    import_receipt_path = root / "import-receipt.json"
    prep_receipt = read_json(prep_receipt_path)
    import_receipt = read_json(import_receipt_path)
    source_integration_patch = REPOSITORY_ROOT / "scripts/integration-program/consolidated-build/source-integration.patch"
    source_commits = prep_receipt.get("sourceCommits")
    rehearsal_commit = prep_receipt.get("rehearsalCommit")
    if source_commits != profile["sourceCommits"]:
        raise ValueError("Prepared source does not match the selected source profile")
    if prep_receipt.get("canonicalSolution") != "Elsa.sln" or import_receipt.get("canonicalSolution", "Elsa.sln") != "Elsa.sln":
        raise ValueError("Current source receipts do not identify canonical Elsa.sln")
    if import_receipt.get("sourceCommits") != source_commits or import_receipt.get("rehearsalCommit") != rehearsal_commit:
        raise ValueError("Current import receipt differs from the preparation source pins")
    if import_receipt.get("exactBlobAndModeMapping") is not True or import_receipt.get("originalHistoriesReachable") is not True:
        raise ValueError("Current import receipt lacks exact mapping or source ancestry proof")
    if git_output(root, "rev-parse", "HEAD") != rehearsal_commit:
        raise ValueError("Prepared source checkout HEAD differs from current preparation receipt")
    if sha256(source_integration_patch) != prep_receipt.get("patchSha256"):
        raise ValueError("Current preparation receipt does not match the reviewed source-integration patch")

    accepted_dir = REPOSITORY_ROOT / "doc/integration-program/consolidation" / profile["evidenceDirectory"]
    # Compare the exact uncompressed public receipts, not merely their summary fields.
    accepted_import_bytes = gzip.decompress((accepted_dir / "import-receipt.json.gz").read_bytes())
    accepted_prep_bytes = gzip.decompress((accepted_dir / "consolidated-build-receipt.json.gz").read_bytes())
    if hashlib.sha256(accepted_import_bytes).hexdigest() != profile["importReceiptSha256"]:
        raise ValueError("Committed current import receipt no longer matches its accepted digest")
    if hashlib.sha256(accepted_prep_bytes).hexdigest() != profile["preparationReceiptSha256"]:
        raise ValueError("Committed current preparation receipt no longer matches its accepted digest")
    accepted_import = json.loads(accepted_import_bytes)
    accepted_prep = json.loads(accepted_prep_bytes)
    if accepted_prep.get("sourceCommits") != source_commits or accepted_import.get("sourceCommits") != source_commits:
        raise ValueError("Committed current-tip receipts do not retain the selected source pins")
    if accepted_prep.get("patchSha256") != prep_receipt.get("patchSha256"):
        raise ValueError("Prepared source uses a different reviewed source-integration patch")
    accepted_prep.pop("rehearsalCommit", None)
    accepted_import.pop("rehearsalCommit", None)
    comparable_prep = dict(prep_receipt)
    comparable_import = dict(import_receipt)
    comparable_prep.pop("rehearsalCommit", None)
    comparable_import.pop("rehearsalCommit", None)
    if comparable_prep != accepted_prep or comparable_import != accepted_import:
        raise ValueError("Current preparation/import receipts differ from accepted evidence beyond the rehearsal commit")

    preparation.verify_import_lineage(root, import_receipt)
    patches = _validate_overlay_receipt(overlay_receipt_path, source_commits)
    overlay_paths = _verify_overlays_applied(root, patches, prep_receipt["files"])
    _verify_prepared_source_files(root, prep_receipt["files"], overlay_paths)
    build_receipt = None
    if overlay_build_receipt_path is not None:
        build_receipt = _validate_overlay_build_receipt(overlay_build_receipt_path, patches, rehearsal_commit, source_commits)
    evidence_hash = None
    if evidence is not None:
        expected_receipts = {
            "preparationReceiptSha256": sha256(prep_receipt_path),
            "importReceiptSha256": sha256(import_receipt_path),
            "sourceIntegrationPatchSha256": sha256(source_integration_patch),
        }
        evidence_hash = _validate_test_profile_pins(
            evidence,
            source_commits,
            rehearsal_commit,
            expected_receipts,
            patches,
            sha256(overlay_receipt_path),
        )
    return {
        "sourceCommits": source_commits,
        "rawRehearsalCommit": rehearsal_commit,
        "preparationReceiptSha256": sha256(prep_receipt_path),
        "importReceiptSha256": sha256(import_receipt_path),
        "sourceIntegrationPatchSha256": sha256(source_integration_patch),
        "reviewedOverlayReceiptSha256": sha256(overlay_receipt_path),
        "reviewedOverlays": patches,
        "overlayBuildReceiptSha256": sha256(overlay_build_receipt_path) if overlay_build_receipt_path else None,
        "overlayBuildVerified": build_receipt is not None,
        "testEvidenceReceiptHashes": evidence_hash,
    }


def refresh_receipt(
    rehearsal: Path,
    evidence_path: Path | None,
    inventory_path: Path,
    manifest_path: Path,
    *,
    source_profile: str = "historical-95a",
    overlay_receipt_path: Path | None = None,
    overlay_build_receipt_path: Path | None = None,
) -> dict[str, Any]:
    root = rehearsal.resolve()
    if evidence_path is not None:
        evidence_path = evidence_path.resolve()
    inventory_path = inventory_path.resolve()
    manifest_path = manifest_path.resolve()
    prep_receipt_path = root / "consolidated-build-receipt.json"
    import_receipt_path = root / "import-receipt.json"
    source_integration_patch = REPOSITORY_ROOT / "scripts/integration-program/consolidated-build/source-integration.patch"
    prep_receipt = read_json(prep_receipt_path)
    import_receipt = read_json(import_receipt_path)
    evidence = read_json(evidence_path) if evidence_path is not None else None
    profile_ledger_path = REPOSITORY_ROOT / "doc/integration-program/consolidation/canonical-source-profile-ledger.json"
    current_profile_receipts = None
    if source_profile == "historical-95a":
        if evidence is None:
            raise ValueError("Historical 95a profile requires canonical Test evidence")
        profile_ledger = read_json(profile_ledger_path)
        accepted_preparation = profile_ledger["canonicalPreparationEvidence"]
        pins = evidence["profile"]
        if pins.get("canonicalSolution") != "Elsa.sln" or prep_receipt.get("canonicalSolution") != "Elsa.sln":
            raise ValueError("Canonical evidence does not select Elsa.sln")
        if pins.get("rawRehearsalCommit") != prep_receipt.get("rehearsalCommit"):
            raise ValueError("Canonical run evidence raw rehearsal commit differs from preparation receipt")
        for source in ("core", "extensions", "studio"):
            if pins.get(source) != prep_receipt.get("sourceCommits", {}).get(source):
                raise ValueError(f"Canonical {source.title()} source pin differs from preparation receipt")
        if import_receipt.get("sourceCommits") != prep_receipt.get("sourceCommits") or import_receipt.get("exactBlobAndModeMapping") is not True or import_receipt.get("originalHistoriesReachable") is not True:
            raise ValueError("Import receipt does not match source pins or fail-closed ancestry/blob checks")
        if git_output(root, "rev-parse", "HEAD") != pins["rawRehearsalCommit"]:
            raise ValueError("Prepared source checkout HEAD differs from recorded raw rehearsal commit")
        if accepted_preparation.get("rehearsalCommit") != pins["rawRehearsalCommit"]:
            raise ValueError("Source-profile ledger does not accept this raw rehearsal commit")
        expected_prep_receipt_hash = accepted_preparation.get("preparationReceiptSha256")
        if sha256(prep_receipt_path) != expected_prep_receipt_hash:
            raise ValueError("Prepared-source receipt hash differs from the accepted canonical profile ledger")
        accepted_source_patch_hash = accepted_preparation.get("sourceIntegrationPatchSha256")
        if sha256(source_integration_patch) != accepted_source_patch_hash or prep_receipt.get("patchSha256") != accepted_source_patch_hash:
            raise ValueError("Tooling source-integration patch and accepted preparation receipt hash differ")
        for patch in pins.get("supplementalPatches", []):
            patch_path = SUPPLEMENTAL_PATCH_PATHS.get(patch.get("name"))
            if patch_path is None:
                raise ValueError(f"Unrecognized supplemental patch in canonical evidence: {patch.get('name')}")
            if sha256(root / patch_path) != patch.get("sha256"):
                raise ValueError(f"Supplemental patch hash differs for {patch['name']}")
    elif source_profile in CURRENT_TIP_PROFILES:
        if overlay_receipt_path is None:
            raise ValueError("Current-tip profile requires the accepted overlay receipt")
        overlay_receipt_path = overlay_receipt_path.resolve()
        if overlay_build_receipt_path is not None:
            overlay_build_receipt_path = overlay_build_receipt_path.resolve()
        current_profile_receipts = _validate_current_tip_profile(
            root, evidence, overlay_receipt_path, overlay_build_receipt_path, source_profile,
        )
        pins = {
            **current_profile_receipts["sourceCommits"],
            "rawRehearsalCommit": current_profile_receipts["rawRehearsalCommit"],
            "canonicalSolution": "Elsa.sln",
            "supplementalPatches": current_profile_receipts["reviewedOverlays"],
        }
    else:
        raise ValueError(f"Unknown canonical source profile: {source_profile}")

    solution_path = root / "Elsa.sln"
    projects = parse_solution(solution_path)
    project_name_by_path = {path: name for name, path in projects}
    project_paths = set(project_name_by_path)
    solution_test_project_paths = {path for name, path in projects if name.endswith("Tests")}
    if any(not (root / path).is_file() for path in project_paths):
        raise ValueError("Elsa.sln contains a missing project file")
    run_evidence = _parse_test_run_evidence(evidence, root, projects) if evidence is not None else None
    asset_documents: dict[str, dict[str, Any]] = {}
    asset_paths: dict[str, Path] = {}
    input_hashes = []
    for path in sorted(project_paths):
        assets_path, document = _assets_for_project(root, path)
        asset_paths[path] = assets_path
        asset_documents[path] = document
        input_hashes.append({"project": path, "projectSha256": sha256(root / path), "assets": _relative_path(root, assets_path), "assetsSha256": sha256(assets_path)})

    _include_restored_project_references(root, project_name_by_path, asset_documents)
    graph_project_paths = set(project_name_by_path)
    for path in sorted(graph_project_paths - project_paths):
        assets_path = root / path
        project_assets = assets_path.parent / "obj/project.assets.json"
        asset_paths[path] = project_assets
        input_hashes.append({"project": path, "projectSha256": sha256(assets_path), "assets": _relative_path(root, project_assets), "assetsSha256": sha256(project_assets)})

    frameworks = sorted({
        framework
        for document in asset_documents.values()
        for framework in document["project"]["restore"]["frameworks"]
    })
    build_csproj = "build/_build.csproj"
    if not (root / build_csproj).is_file():
        raise ValueError("Canonical NUKE build project is missing from the prepared source tree")
    build_tool_paths = sorted({
        build_csproj,
        "build.sh",
        *[path.relative_to(root).as_posix() for path in (root / "build").glob("*.cs")],
        *([".nuke/parameters.json"] if (root / ".nuke/parameters.json").is_file() else []),
    })
    build_config_paths = ancestor_build_configs(root, graph_project_paths | {build_csproj})
    external_nuget_config_count = sum(
        1
        for document in asset_documents.values()
        for config_path in document.get("project", {}).get("restore", {}).get("configFilePaths", [])
        if not (Path(config_path).resolve() == root or root in Path(config_path).resolve().parents)
    )
    name_by_path = project_name_by_path
    graph, node_to_project_framework, edge_rows = _framework_graph(
        root, name_by_path, asset_documents, solution_test_project_paths,
    )
    core_scenario = {}
    core_root_path = CHANGED_PROJECT[1]
    for framework in frameworks:
        root_node = _node_key(core_root_path, framework)
        if ("elsa-core", root_node) not in graph.projects:
            continue
        changed_key = ("elsa-core", root_node)
        affected_nodes = graph.affected_projects([changed_key])
        affected_tests = sorted(
            node_to_project_framework[node]
            for repository, node in graph.affected_tests([changed_key])
        )
        restored_count = sum(
            framework in asset_documents[path]["project"]["restore"]["frameworks"]
            for path in project_paths
        )
        referenced_project_count = sum(
            framework in asset_documents[path]["project"]["restore"]["frameworks"]
            for path in graph_project_paths - project_paths
        )
        source_edge_rows = [
            (source_framework, target_framework)
            for (source_path, source_framework), targets in edge_rows.items()
            if source_framework == framework
            for _, target_framework in targets
        ]
        edge_matrix = dict(sorted(Counter(f"{source}->{target}" for source, target in source_edge_rows).items()))
        result_rows = []
        for path, target_framework in affected_tests:
            dependencies = list(asset_documents[path]["project"]["frameworks"].get(target_framework, {}).get("dependencies", {}))
            if run_evidence is None:
                result_rows.append({
                    "path": path,
                    "framework": target_framework,
                    "status": "not-assessed-no-current-profile-test-evidence",
                    "passed": 0,
                    "skipped": 0,
                    "source": "no-current-profile-test-receipt",
                })
            else:
                result_rows.append(_classify_test_observation(path, target_framework, run_evidence, dependencies))
        core_scenario[framework] = {
            "restoredProjectCount": restored_count,
            "restoredReferencedProjectCount": referenced_project_count,
            "directProjectReferenceEdgeCount": sum(len(targets) for (source_path, source_framework), targets in edge_rows.items() if source_framework == framework),
            "directProjectReferenceTargetFrameworkMatrix": edge_matrix,
            "affectedProjectCount": len(affected_nodes),
            "affectedTestProjectCount": len(affected_tests),
            "affectedTestProjectResults": result_rows,
            "passedTestProjects": sum(row["status"] == "passed" for row in result_rows),
            "skippedTestProjects": [row["path"] for row in result_rows if row["status"] == "selected-but-skipped"],
            "unexecutedOrMissing": [row for row in result_rows if row["status"] != "passed"],
        }

    manifest = load_manifest(manifest_path)
    unit = get_unit(manifest, DEFAULT_UNIT_ID)
    mapped_root = map_source_project_path(unit["source"]["repository"], unit["source"]["project_path"], import_receipt["mapping"])
    mapped_test_paths = {
        map_source_project_path(unit["source"]["repository"], test["project_path"], import_receipt["mapping"]): test
        for test in unit["source"]["test_projects"]
    }
    mapped_test_results = []
    release_root_frameworks = sorted(
        set(unit["target_frameworks"])
        & set(asset_documents.get(mapped_root, {}).get("project", {}).get("restore", {}).get("frameworks", {}))
    )
    if set(release_root_frameworks) != set(unit["target_frameworks"]):
        raise ValueError(f"Mapped release-unit package target frameworks differ from restored source: {release_root_frameworks}")
    slack_affected = set()
    for framework in release_root_frameworks:
        root_node = _node_key(mapped_root, framework)
        slack_affected.update(node_to_project_framework[node] for repository, node in graph.affected_tests([("elsa-core", root_node)]))
    expected_slack_tests = {
        (path, framework)
        for path, test in mapped_test_paths.items()
        for framework in test["target_frameworks"]
    }
    if slack_affected != expected_slack_tests:
        raise ValueError(f"Manifest Slack test closure differs from prepared source graph: {sorted(slack_affected)}")
    for path, framework in sorted(slack_affected):
        dependencies = list(asset_documents[path]["project"]["frameworks"].get(framework, {}).get("dependencies", {}))
        if run_evidence is None:
            mapped_test_results.append({
                "path": path,
                "framework": framework,
                "status": "not-assessed-no-current-profile-test-evidence",
                "passed": 0,
                "skipped": 0,
                "source": "no-current-profile-test-receipt",
            })
        else:
            mapped_test_results.append(_classify_test_observation(path, framework, run_evidence, dependencies))

    # Compare the historical 51-input plan after applying its source paths through the recorded relocation map.
    old_plan = build_plan(inventory_path, {}, manifest_path)
    old_mapped_rows = []
    for row in old_plan["projects"]:
        repository, source_path = row["key"].split(":", 1)
        if repository == "elsa-extensions":
            mapped_path = map_source_project_path(repository, source_path, import_receipt["mapping"])
        elif repository == "elsa-core":
            mapped_path = source_path
        else:
            raise ValueError(f"Historical plan includes an unsupported repository: {repository}")
        old_mapped_rows.append({"oldKey": row["key"], "mappedPath": mapped_path, "lane": row["lane"], "inCanonicalSolution": mapped_path in project_paths})
    current_paths = {row["path"] for row in core_scenario.get("net10.0", {}).get("affectedTestProjectResults", [])}
    old_test_paths = {row["mappedPath"] for row in old_mapped_rows if row["lane"] != "build-only" and row["inCanonicalSolution"]}
    old_plan_comparison = {
        "status": "historical-plan-only; not executed by canonical 95a NUKE run",
        "plannedProjectCount": len(old_mapped_rows),
        "inventorySnapshotDate": old_plan["inventory_snapshot_date"],
        "inventorySourcePins": old_plan["source_pins"],
        "mappedPathsPresentInCanonicalSolution": sum(row["inCanonicalSolution"] for row in old_mapped_rows),
        "plannedBuildOnlyInputs": [row["mappedPath"] for row in old_mapped_rows if row["lane"] == "build-only"],
        "testPathsOverlappingCurrentNet10CoreClosure": sorted(old_test_paths & current_paths),
        "oldTestPathsAbsentFromCurrentNet10CoreClosure": sorted(old_test_paths - current_paths),
        "currentNet10CoreTestsNotInOldPlan": sorted(current_paths - old_test_paths),
        "rows": old_mapped_rows,
    }

    workbench_path = "samples/extensions/workbench/Elsa.Server.Web/Elsa.Server.Web.csproj"
    if workbench_path not in asset_documents:
        raise ValueError("Patched Workbench sample is absent from canonical Elsa.sln")
    workbench_frameworks = sorted(asset_documents[workbench_path]["project"]["restore"]["frameworks"])
    if not workbench_frameworks:
        raise ValueError("Patched Workbench sample has no declared restored target framework")
    workbench_file = root / workbench_path
    workbench_hash_before = sha256(workbench_file)
    workbench_evaluations = [evaluate_msbuild_properties(workbench_file, framework) for framework in workbench_frameworks]
    if sha256(workbench_file) != workbench_hash_before:
        raise ValueError("Workbench MSBuild evaluation modified the prepared source project")

    tool_paths = {
        "scripts/integration-program/refresh_canonical_dependency_graph.py": Path(__file__).resolve(),
        "scripts/integration-program/package_impact.py": REPOSITORY_ROOT / "scripts/integration-program/package_impact.py",
        "scripts/integration-program/package_closure.py": REPOSITORY_ROOT / "scripts/integration-program/package_closure.py",
        "scripts/integration-program/release_unit_manifest.py": REPOSITORY_ROOT / "scripts/integration-program/release_unit_manifest.py",
        "scripts/integration-program/test_refresh_canonical_dependency_graph.py": REPOSITORY_ROOT / "scripts/integration-program/test_refresh_canonical_dependency_graph.py",
        "scripts/integration-program/prepare_consolidated_build.py": REPOSITORY_ROOT / "scripts/integration-program/prepare_consolidated_build.py",
        "scripts/integration-program/test_prepare_consolidated_build.py": REPOSITORY_ROOT / "scripts/integration-program/test_prepare_consolidated_build.py",
        "scripts/integration-program/consolidated-build/source-integration.patch": source_integration_patch,
        "doc/integration-program/inventory/inventory.json": inventory_path,
        "doc/integration-program/release-units.json": manifest_path,
    }
    if evidence_path is not None:
        tool_paths[evidence_path.name] = evidence_path
    if source_profile == "historical-95a":
        tool_paths["doc/integration-program/consolidation/canonical-source-profile-ledger.json"] = profile_ledger_path
    else:
        tool_paths["reviewed-overlay-receipt.json"] = overlay_receipt_path
        if overlay_build_receipt_path is not None:
            tool_paths["mapped-solution-build.json"] = overlay_build_receipt_path
    source_file_hashes = {
        path: sha256(root / path)
        for path in sorted({"Elsa.sln", *BUILD_PROPS, *build_config_paths, *build_tool_paths, *sorted(graph_project_paths)})
    }
    source_file_hashes.update({
        _relative_path(root, asset_paths[path]): sha256(asset_paths[path])
        for path in sorted(asset_paths)
    })
    manifest_units = [unit["id"] for unit in manifest["release_units"]]
    accepted_preparation_receipt_hash = (
        accepted_preparation.get("preparationReceiptSha256")
        if source_profile == "historical-95a"
        else CURRENT_TIP_PROFILES[source_profile]["preparationReceiptSha256"]
    )
    accepted_source_patch_hash = (
        accepted_preparation.get("sourceIntegrationPatchSha256")
        if source_profile == "historical-95a"
        else current_profile_receipts["sourceIntegrationPatchSha256"]
    )
    canonical_run_receipt = {
        "status": "validated-current-profile-test-run" if evidence is not None else "dependency-closure-only-no-test-run-receipt",
        "frameworkSummaryTotals": run_evidence["frameworkSummaryTotals"] if run_evidence is not None else None,
        "frameworkSummaryCount": run_evidence["frameworkSummaryCount"] if run_evidence is not None else None,
        "retainedTrxSummary": run_evidence["retainedTrxSummary"] if run_evidence is not None else None,
    }
    workbench_patch_hash = next(
        row["sha256"] for row in pins["supplementalPatches"]
        if row["name"] in ("Workbench canonical Secrets sample", "workbench-canonical-secrets.patch")
    )
    return {
        "schemaVersion": 1,
        "sourceProfile": source_profile,
        "scope": (
            "Prepared canonical Elsa.sln project-impact graph and exact 95a run reconciliation; no source writes or package actions."
            if source_profile == "historical-95a"
            else f"Prepared {source_profile} canonical Elsa.sln dependency closure with reviewed overlays verified against source; current-profile test outcomes are included only when matching NUKE evidence is supplied. No source writes or package actions."
        ),
        "toolCheckout": {
            "repository": "elsa-workflows/elsa-core",
            "gitHead": git_output(REPOSITORY_ROOT, "rev-parse", "HEAD"),
            "workingTreeFileHashesAreAuthoritative": True,
            "worktreePathKind": "tooling checkout; the CLI and manifest are read from this checkout",
            "files": {path: sha256(file_path) for path, file_path in tool_paths.items()},
        },
        "sourceCheckout": {
            "rawRehearsalCommit": pins["rawRehearsalCommit"],
            "core": pins["core"],
            "extensions": pins["extensions"],
            "studio": pins["studio"],
            "preparedSourcePathKind": "separate disposable source checkout passed with --rehearsal; this checkout supplies Elsa.sln, project files, restored assets, build inputs, and retained receipts",
            "canonicalSolution": "Elsa.sln",
            "canonicalSolutionProjectCount": len(project_paths),
            "restoredReferenceProjectCount": len(graph_project_paths - project_paths),
            "sourceFileSha256": source_file_hashes,
            "ancestorBuildConfigPaths": build_config_paths,
            "canonicalNukeBuildInputs": build_tool_paths,
            "externalNuGetConfigReferenceCountAcrossProjectAssets": external_nuget_config_count,
            "supplementalPatches": pins["supplementalPatches"],
            "preparationReceiptSha256": sha256(prep_receipt_path),
            "acceptedPreparationReceiptSha256": accepted_preparation_receipt_hash,
            "sourceIntegrationPatchProvenance": {
                "reviewedToolingCheckoutPath": "scripts/integration-program/consolidated-build/source-integration.patch",
                "reviewedToolingPatchSha256": accepted_source_patch_hash,
                "preparationReceiptPatchSha256": prep_receipt.get("patchSha256"),
                "inertCopyRetainedInRawSourceSha256": sha256(root / "scripts/integration-program/consolidated-build/source-integration.patch"),
                "note": "The tool patch used for canonical preparation is pinned separately; the imported raw source tree retains an inert historical copy with different bytes.",
            },
            "importReceiptSha256": sha256(import_receipt_path),
            "canonicalRunEvidenceSha256": sha256(evidence_path) if evidence_path is not None else None,
            "canonicalRunLogSha256": evidence["invocation"]["logSha256"] if evidence is not None else None,
            "reviewedOverlayReceipt": current_profile_receipts if current_profile_receipts is not None else None,
            "restoreFrameworkProjectCounts": {
                framework: sum(framework in document["project"]["restore"]["frameworks"] for document in asset_documents.values())
                for framework in frameworks
            },
            "restoreFrameworkProjectReferenceCounts": {
                framework: core_scenario[framework]["directProjectReferenceEdgeCount"]
                for framework in frameworks
            },
            "restoreGraphSource": "project.assets.json project.restore.frameworks[tfm].projectReferences; source-mode Restore outputs only",
            "projectInputs": input_hashes,
        },
        "canonicalRun": {
            "selectedByNukeSuffix": "Project.Name.EndsWith(\"Tests\") from pinned build/Build.cs",
            "selectedProjectCount": len([name for name, _ in projects if name.endswith("Tests")]),
            **canonical_run_receipt,
        },
        "coreSourceChangeImpact": {
            "changedProject": f"elsa-core:{CHANGED_PROJECT[1]}",
            "graphByTargetFramework": core_scenario,
            "selectedButNotExecuted": [
                {"path": row["path"], "status": row["status"], "explanation": row.get("explanation", "")}
                for scenario in core_scenario.values() for row in scenario["affectedTestProjectResults"]
                if row["status"] != "passed"
            ],
        },
        "declaredReleaseUnitImpact": {
            "manifestSha256": sha256(manifest_path),
            "manifestUnitCount": len(manifest_units),
            "declaredUnitIds": manifest_units,
            "unitId": unit["id"],
            "packageId": unit["package_id"],
            "mappedPackageProject": mapped_root,
            "mappedTestProjects": mapped_test_paths,
            "affectedTests": mapped_test_results,
            "otherProjectReleaseOwnership": "Not specified by the one-unit manifest; this graph does not infer that every solution project is an independently published package.",
            "separateProof": "The mapped Slack local-package consumer proof is separate artifact evidence; it is not execution of the selected Slack test, which is explicitly skipped.",
        },
        "historicalInventoryPlanComparison": old_plan_comparison,
        "postSupplementalWorkbenchEvaluation": {
            "project": workbench_path,
            "projectSha256": workbench_hash_before,
            "supplementalPatchSha256": workbench_patch_hash,
            "frameworkEvaluations": workbench_evaluations,
            "limitation": "Only the post-patch Workbench effective properties were reevaluated; the 2,586-project matrix predates supplemental patches. This does not prove package-mode restore or publication behavior.",
        },
        "limitations": [
            "The dependency edges come from source-mode Restore assets. Package-mode restore graph behavior was not tested.",
            "Only Elsa.Slack is declared as a release unit; other release ownership remains unspecified.",
            "The historical 51-project inventory closure is a planned baseline, not an executed set. The canonical NUKE run selected 95 project paths and used per-framework outcomes in this receipt.",
            f"Canonical test evidence is for the {pins['core'][:7]}/{pins['extensions'][:7]}/{pins['studio'][:7]} profile plus {len(pins['supplementalPatches'])} supplemental patches, not current Core main or a history-bearing import.",
            "No Pack, Push, Publish, deployment, or package-migration action ran.",
        ],
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-profile", choices=("historical-95a", *CURRENT_TIP_PROFILES), default="historical-95a",
                        help="Accepted source profile; current-tip mode never falls back to historical test results")
    parser.add_argument("--rehearsal", type=Path, required=True, help="Prepared canonical source checkout; it is read-only")
    parser.add_argument("--evidence", type=Path, help="Matching NUKE Test receipt; omitted for graph-only current-tip closure")
    parser.add_argument("--overlay-receipt", type=Path, help="Accepted current-tip overlay hash receipt")
    parser.add_argument("--overlay-build-receipt", type=Path, help="Optional receipt proving the exact overlays were applied and built")
    parser.add_argument("--inventory", type=Path, default=DEFAULT_INVENTORY)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    evidence_path = args.evidence
    if evidence_path is None and args.source_profile == "historical-95a":
        evidence_path = DEFAULT_EVIDENCE
    receipt = refresh_receipt(
        args.rehearsal,
        evidence_path,
        args.inventory,
        args.manifest,
        source_profile=args.source_profile,
        overlay_receipt_path=args.overlay_receipt,
        overlay_build_receipt_path=args.overlay_build_receipt,
    )
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({
        "output": str(args.output),
        "sourceCommit": receipt["sourceCheckout"]["core"],
        "solutionProjects": receipt["sourceCheckout"]["canonicalSolutionProjectCount"],
        "frameworks": {
            framework: value["affectedTestProjectCount"]
            for framework, value in receipt["coreSourceChangeImpact"]["graphByTargetFramework"].items()
        },
        "slackStatuses": [row["status"] for row in receipt["declaredReleaseUnitImpact"]["affectedTests"]],
    }, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
