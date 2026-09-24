#!/usr/bin/env python3
"""Build a hash-pinned project/TFM impact receipt from a prepared canonical solution."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
from collections import Counter
from pathlib import Path, PurePosixPath
from typing import Any

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


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


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
) -> InventoryGraph:
    """Use the shared impact-closure implementation over one restored TFM graph."""
    known_paths = set(project_name_by_path)
    unknown = sorted({edge for edges in edges_by_path.values() for edge in edges} - known_paths)
    if unknown:
        raise ValueError(f"Restored project references target projects absent from Elsa.sln: {unknown}")
    rows = []
    for path, name in project_name_by_path.items():
        rows.append({
            "path": path,
            "is_test_project": name.endswith("Tests"),
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

    return graph_from_edges(graph_names, edges), node_to_project_framework, edge_rows


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
            absolute = Path(code_base).resolve()
            relative = _relative_path(root, absolute)
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
        if counters.get("total") == 1 and "explicitly skipped" in explanation:
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


def refresh_receipt(rehearsal: Path, evidence_path: Path, inventory_path: Path, manifest_path: Path) -> dict[str, Any]:
    root = rehearsal.resolve()
    evidence_path = evidence_path.resolve()
    inventory_path = inventory_path.resolve()
    manifest_path = manifest_path.resolve()
    evidence = json.loads(evidence_path.read_text(encoding="utf-8"))
    prep_receipt_path = root / "consolidated-build-receipt.json"
    import_receipt_path = root / "import-receipt.json"
    prep_receipt = json.loads(prep_receipt_path.read_text(encoding="utf-8"))
    import_receipt = json.loads(import_receipt_path.read_text(encoding="utf-8"))
    profile_ledger_path = REPOSITORY_ROOT / "doc/integration-program/consolidation/canonical-source-profile-ledger.json"
    profile_ledger = json.loads(profile_ledger_path.read_text(encoding="utf-8"))
    accepted_preparation = profile_ledger["canonicalPreparationEvidence"]

    pins = evidence["profile"]
    if pins.get("canonicalSolution") != "Elsa.sln" or prep_receipt.get("canonicalSolution") != "Elsa.sln":
        raise ValueError("Canonical evidence does not select Elsa.sln")
    if pins.get("rawRehearsalCommit") != prep_receipt.get("rehearsalCommit"):
        raise ValueError("Canonical run evidence raw rehearsal commit differs from preparation receipt")
    if pins.get("core") != prep_receipt.get("sourceCommits", {}).get("core"):
        raise ValueError("Canonical Core source pin differs from preparation receipt")
    if pins.get("extensions") != prep_receipt.get("sourceCommits", {}).get("extensions"):
        raise ValueError("Canonical Extensions source pin differs from preparation receipt")
    if pins.get("studio") != prep_receipt.get("sourceCommits", {}).get("studio"):
        raise ValueError("Canonical Studio source pin differs from preparation receipt")
    if import_receipt.get("sourceCommits") != prep_receipt.get("sourceCommits") or import_receipt.get("exactBlobAndModeMapping") is not True or import_receipt.get("originalHistoriesReachable") is not True:
        raise ValueError("Import receipt does not match source pins or fail-closed ancestry/blob checks")
    if git_output(root, "rev-parse", "HEAD") != pins["rawRehearsalCommit"]:
        raise ValueError("Prepared source checkout HEAD differs from recorded raw rehearsal commit")
    if accepted_preparation.get("rehearsalCommit") != pins["rawRehearsalCommit"]:
        raise ValueError("Source-profile ledger does not accept this raw rehearsal commit")
    expected_prep_receipt_hash = accepted_preparation.get("preparationReceiptSha256")
    actual_prep_receipt_hash = sha256(prep_receipt_path)
    if actual_prep_receipt_hash != expected_prep_receipt_hash:
        raise ValueError("Prepared-source receipt hash differs from the accepted canonical profile ledger")
    source_integration_patch = REPOSITORY_ROOT / "scripts/integration-program/consolidated-build/source-integration.patch"
    accepted_source_patch_hash = accepted_preparation.get("sourceIntegrationPatchSha256")
    if sha256(source_integration_patch) != accepted_source_patch_hash or prep_receipt.get("patchSha256") != accepted_source_patch_hash:
        raise ValueError("Tooling source-integration patch and accepted preparation receipt hash differ")

    for patch in pins.get("supplementalPatches", []):
        patch_path = SUPPLEMENTAL_PATCH_PATHS.get(patch.get("name"))
        if patch_path is None:
            raise ValueError(f"Unrecognized supplemental patch in canonical evidence: {patch.get('name')}")
        actual_hash = sha256(root / patch_path)
        if actual_hash != patch.get("sha256"):
            raise ValueError(f"Supplemental patch hash differs for {patch['name']}")

    solution_path = root / "Elsa.sln"
    projects = parse_solution(solution_path)
    project_name_by_path = {path: name for name, path in projects}
    project_paths = set(project_name_by_path)
    if any(not (root / path).is_file() for path in project_paths):
        raise ValueError("Elsa.sln contains a missing project file")
    run_evidence = _parse_test_run_evidence(evidence, root, projects)
    asset_documents: dict[str, dict[str, Any]] = {}
    asset_paths: dict[str, Path] = {}
    input_hashes = []
    for path in sorted(project_paths):
        assets_path, document = _assets_for_project(root, path)
        asset_paths[path] = assets_path
        asset_documents[path] = document
        input_hashes.append({"project": path, "projectSha256": sha256(root / path), "assets": _relative_path(root, assets_path), "assetsSha256": sha256(assets_path)})

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
    build_config_paths = ancestor_build_configs(root, project_paths | {build_csproj})
    external_nuget_config_count = sum(
        1
        for document in asset_documents.values()
        for config_path in document.get("project", {}).get("restore", {}).get("configFilePaths", [])
        if not (Path(config_path).resolve() == root or root in Path(config_path).resolve().parents)
    )
    name_by_path = project_name_by_path
    graph, node_to_project_framework, edge_rows = _framework_graph(root, name_by_path, asset_documents)
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
        restored_count = sum(framework in document["project"]["restore"]["frameworks"] for document in asset_documents.values())
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
            result_rows.append(_classify_test_observation(path, target_framework, run_evidence, dependencies))
        core_scenario[framework] = {
            "restoredProjectCount": restored_count,
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
        "doc/integration-program/consolidation/canonical-95a-nuke-test-evidence.json": evidence_path,
        "doc/integration-program/consolidation/canonical-source-profile-ledger.json": profile_ledger_path,
    }
    source_file_hashes = {
        path: sha256(root / path)
        for path in sorted({"Elsa.sln", *BUILD_PROPS, *build_config_paths, *build_tool_paths, *sorted(project_paths)})
    }
    source_file_hashes.update({
        _relative_path(root, asset_paths[path]): sha256(asset_paths[path])
        for path in sorted(asset_paths)
    })
    manifest_units = [unit["id"] for unit in manifest["release_units"]]
    return {
        "schemaVersion": 1,
        "scope": "Prepared canonical Elsa.sln project-impact graph and exact 95a run reconciliation; no source writes or package actions.",
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
            "sourceFileSha256": source_file_hashes,
            "ancestorBuildConfigPaths": build_config_paths,
            "canonicalNukeBuildInputs": build_tool_paths,
            "externalNuGetConfigReferenceCountAcrossProjectAssets": external_nuget_config_count,
            "supplementalPatches": pins["supplementalPatches"],
            "preparationReceiptSha256": sha256(prep_receipt_path),
            "acceptedPreparationReceiptSha256": expected_prep_receipt_hash,
            "sourceIntegrationPatchProvenance": {
                "reviewedToolingCheckoutPath": "scripts/integration-program/consolidated-build/source-integration.patch",
                "reviewedToolingPatchSha256": accepted_source_patch_hash,
                "preparationReceiptPatchSha256": prep_receipt.get("patchSha256"),
                "inertCopyRetainedInRawSourceSha256": sha256(root / "scripts/integration-program/consolidated-build/source-integration.patch"),
                "note": "The tool patch used for canonical preparation is pinned separately; the imported raw source tree retains an inert historical copy with different bytes.",
            },
            "importReceiptSha256": sha256(import_receipt_path),
            "canonicalRunEvidenceSha256": sha256(evidence_path),
            "canonicalRunLogSha256": evidence["invocation"]["logSha256"],
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
            "frameworkSummaryTotals": run_evidence["frameworkSummaryTotals"],
            "frameworkSummaryCount": run_evidence["frameworkSummaryCount"],
            "retainedTrxSummary": run_evidence["retainedTrxSummary"],
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
            "supplementalPatchSha256": next(row["sha256"] for row in pins["supplementalPatches"] if row["name"] == "Workbench canonical Secrets sample"),
            "frameworkEvaluations": workbench_evaluations,
            "limitation": "Only the post-patch Workbench effective properties were reevaluated; the 2,586-project matrix predates supplemental patches. This does not prove package-mode restore or publication behavior.",
        },
        "limitations": [
            "The dependency edges come from source-mode Restore assets. Package-mode restore graph behavior was not tested.",
            "Only Elsa.Slack is declared as a release unit; other release ownership remains unspecified.",
            "The historical 51-project inventory closure is a planned baseline, not an executed set. The canonical NUKE run selected 95 project paths and used per-framework outcomes in this receipt.",
            "Canonical test evidence is for the 95a658/33fa0bfd/9afd3e36 profile plus four supplemental patches, not current Core main or a history-bearing import.",
            "No Pack, Push, Publish, deployment, or package-migration action ran.",
        ],
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--rehearsal", type=Path, required=True, help="Prepared canonical source checkout; it is read-only")
    parser.add_argument("--evidence", type=Path, default=DEFAULT_EVIDENCE, help="Run receipt from this tooling checkout")
    parser.add_argument("--inventory", type=Path, default=DEFAULT_INVENTORY)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    receipt = refresh_receipt(args.rehearsal, args.evidence, args.inventory, args.manifest)
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
