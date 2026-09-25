#!/usr/bin/env python3
"""Select Slack and shared-Core tests from the restored imported source graph."""

from __future__ import annotations

import argparse
import hashlib
import json
import subprocess
import sys
from pathlib import Path
from typing import Any

import refresh_canonical_dependency_graph as graph_reader
from release_unit_manifest import DEFAULT_UNIT_ID, get_unit, load_manifest


ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "doc/integration-program/release-units.json"
CORE_PROJECT = "src/modules/Elsa/Elsa.csproj"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def git_output(root: Path, *arguments: str) -> str:
    result = subprocess.run(
        ["git", "-C", str(root), *arguments], text=True, capture_output=True, check=False
    )
    if result.returncode:
        raise ValueError(f"Cannot inspect source Git state: {result.stderr.strip()}")
    return result.stdout.strip()


def restored_test_projects(asset_documents: dict[str, dict[str, Any]]) -> set[str]:
    tests = {
        path for path, document in asset_documents.items()
        if any(key.startswith("Microsoft.NET.Test.Sdk/") for key in document.get("libraries", {}))
    }
    if not tests or any(not path.endswith(".csproj") for path in tests):
        raise ValueError("Restored graph has no valid test projects")
    return tests


def selected_tests(
    graph: Any, asset_documents: dict[str, dict[str, Any]], project_path: str
) -> list[dict[str, str]]:
    if project_path not in asset_documents:
        raise ValueError(f"Release-unit project is absent from restored solution: {project_path}")
    frameworks = asset_documents[project_path]["project"]["restore"]["frameworks"]
    if not frameworks:
        raise ValueError(f"Release-unit project has no restored frameworks: {project_path}")
    roots = [("elsa-core", f"{project_path}@@{framework}") for framework in frameworks]
    selected = graph.affected_tests(roots)
    return [
        {"project": path, "framework": framework}
        for _, node in sorted(selected)
        for path, framework in [node.rsplit("@@", 1)]
    ]


def select_scenarios(
    graph: Any, asset_documents: dict[str, dict[str, Any]], unit: dict[str, Any]
) -> dict[str, Any]:
    if unit["package_id"] != "Elsa.Slack" or unit["mapped"]["repository"] != "elsa-core":
        raise ValueError("This bounded selector accepts only the mapped Elsa.Slack release unit")
    slack_project = unit["mapped"]["project_path"]
    slack = selected_tests(graph, asset_documents, slack_project)
    shared = selected_tests(graph, asset_documents, CORE_PROJECT)
    mapped_tests = unit["mapped"]["test_projects"]
    source_test_rows = unit["source"]["test_projects"]
    source_paths = [row["project_path"] for row in source_test_rows]
    mapped_source_paths = [row["source_project_path"] for row in mapped_tests]
    mapped_paths = [row["project_path"] for row in mapped_tests]
    if (
        len(source_paths) != len(set(source_paths))
        or len(mapped_source_paths) != len(set(mapped_source_paths))
        or len(mapped_paths) != len(set(mapped_paths))
        or set(source_paths) != set(mapped_source_paths)
    ):
        raise ValueError("Mapped Slack tests must map each source test exactly once")
    source_tests = {row["project_path"]: row["target_frameworks"] for row in source_test_rows}
    expected_slack = set()
    for row in mapped_tests:
        source_path = row["source_project_path"]
        frameworks = source_tests.get(source_path)
        if not isinstance(frameworks, list) or not frameworks:
            raise ValueError(f"Mapped Slack test has no source framework declaration: {source_path}")
        expected_slack.update((row["project_path"], framework) for framework in frameworks)
    actual_slack = {(row["project"], row["framework"]) for row in slack}
    if actual_slack != expected_slack or len(slack) != len(expected_slack):
        raise ValueError(f"Current Slack test closure differs from the release-unit manifest: {slack}")
    if not set((row["project"], row["framework"]) for row in slack).issubset(
        (row["project"], row["framework"]) for row in shared
    ):
        raise ValueError("Shared-Core test closure omits the Slack release-unit test")
    if len(shared) <= len(slack):
        raise ValueError("Shared-Core change did not expand the affected-test closure")
    return {
        "releaseUnit": unit["id"],
        "packageIdsToPack": [unit["package_id"]],
        "unchangedMqttSelectedForPack": False,
        "slackOnly": {"changedProject": slack_project, "tests": slack},
        "sharedCore": {"changedProject": CORE_PROJECT, "tests": shared},
        "testExecutionPerformed": False,
        "publicationPerformed": False,
    }


def receipt(root: Path, manifest_path: Path, expected_head: str | None) -> dict[str, Any]:
    root = root.resolve()
    head = git_output(root, "rev-parse", "HEAD")
    if expected_head and head != expected_head:
        raise ValueError(f"Expected source commit {expected_head}, checked out {head}")
    initial_status = git_output(root, "status", "--porcelain", "--untracked-files=normal")
    restore = subprocess.run(
        ["dotnet", "restore", str(root / "Elsa.sln"), "-p:UseProjectReferences=true"],
        cwd=root, text=True, capture_output=True, check=False, timeout=1200,
    )
    if restore.returncode:
        raise ValueError(f"Current-source restore failed: {restore.stdout[-2000:]} {restore.stderr[-2000:]}")

    entries = graph_reader.parse_solution(root / "Elsa.sln")
    separately_restored = []
    for _, path in entries:
        if ((root / path).parent / "obj/project.assets.json").is_file():
            continue
        project_restore = subprocess.run(
            ["dotnet", "restore", str(root / path), "-p:UseProjectReferences=true"],
            cwd=root, text=True, capture_output=True, check=False, timeout=1200,
        )
        if project_restore.returncode:
            raise ValueError(
                f"Solution member {path} did not restore: "
                f"{project_restore.stdout[-2000:]} {project_restore.stderr[-2000:]}"
            )
        separately_restored.append(path)
    project_names = {path: name for name, path in entries}
    assets = {path: graph_reader._assets_for_project(root, path)[1] for path in project_names}
    graph_reader._include_restored_project_references(root, project_names, assets)
    tests = restored_test_projects(assets)
    graph, _, _ = graph_reader._framework_graph(root, project_names, assets, tests)
    unit = get_unit(load_manifest(manifest_path), DEFAULT_UNIT_ID)
    scenarios = select_scenarios(graph, assets, unit)

    final_status = git_output(root, "status", "--porcelain", "--untracked-files=normal")
    if git_output(root, "rev-parse", "HEAD") != head or initial_status != final_status:
        raise ValueError("Source revision or working-tree state changed during graph evaluation")
    asset_hashes = {
        path: sha256((root / path).parent / "obj/project.assets.json")
        for path in sorted(assets)
    }
    project_hashes = {path: sha256(root / path) for path in sorted(assets)}
    build_configs = graph_reader.ancestor_build_configs(root, set(assets))
    return {
        "schemaVersion": 1,
        "sourceRevision": head,
        "workingTreeClean": not initial_status,
        "acceptanceEligible": not initial_status,
        "solutionSha256": sha256(root / "Elsa.sln"),
        "manifestSha256": sha256(manifest_path),
        "solutionProjectCount": len(entries),
        "restoredProjectCount": len(assets),
        "restoredTestProjectCount": len(tests),
        "restore": {
            "performed": True,
            "exitCode": restore.returncode,
            "separatelyRestoredSolutionMembers": separately_restored,
        },
        "projectFileSha256": project_hashes,
        "projectAssetsSha256": asset_hashes,
        "buildConfigSha256": {path: sha256(root / path) for path in build_configs},
        "scenarios": scenarios,
        "limitations": [
            "Project-reference impact is evaluated from this checkout's restored assets; runtime service effects are not inferred.",
            "This receipt selects tests but does not execute them or publish packages.",
            "The release-unit manifest selects one Slack package; it is not a Core release package plan.",
        ],
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--manifest", type=Path, default=MANIFEST)
    parser.add_argument("--expected-head")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        result = receipt(args.root, args.manifest, args.expected_head)
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
        print(
            f"Selected {len(result['scenarios']['slackOnly']['tests'])} Slack and "
            f"{len(result['scenarios']['sharedCore']['tests'])} shared-Core test/TFM nodes "
            f"at {result['sourceRevision']}; acceptanceEligible={result['acceptanceEligible']}"
        )
        return 0 if result["acceptanceEligible"] else 2
    except (OSError, ValueError, KeyError, subprocess.TimeoutExpired) as error:
        print(f"Current imported-source impact selection failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
