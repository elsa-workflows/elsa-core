#!/usr/bin/env python3
"""Plan or execute the inventory-pinned Core -> Slack dependency test closure."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Any
from uuid import uuid4

from package_impact import InventoryGraph, ProjectKey
from release_unit_manifest import (
    DEFAULT_UNIT_ID,
    MANIFEST_PATH,
    get_unit,
    load_manifest,
    source_project_key,
    source_test_project_keys,
    validate_against_inventory,
)
from source_bindings import verify_overlay

CHANGED_PROJECT: ProjectKey = ("elsa-core", "src/modules/Elsa/Elsa.csproj")
RELEASE_UNIT_ID = DEFAULT_UNIT_ID
KNOWN_BASELINE_SKIPS = {
    "elsa-extensions:test/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj": {
        "Elsa.Slack.Tests.Activities.Channels.CreateChannelTests.ExecuteAsync": "Not implemented yet.",
    },
    "elsa-core:test/component/Elsa.Workflows.ComponentTests/Elsa.Workflows.ComponentTests.csproj": {
        "Elsa.Workflows.ComponentTests.Scenarios.WorkflowActivities.DeleteWorkflowTests.DeleteWorkflow_Clustered":
            "Clustered tests are interfering with other event driven tests",
        "Elsa.Workflows.ComponentTests.Scenarios.ClusteredHosting.ActivityRegistrySyncTests.ImportWorkflowActivity_ShouldUpdateOtherPods":
            "Not yet implemented",
    },
    "elsa-extensions:test/modules/servicebus/Elsa.ServiceBus.AzureServiceBus.ComponentTests/Elsa.ServiceBus.AzureServiceBus.ComponentTests.csproj": {
        "Elsa.ServiceBus.AzureServiceBus.ComponentTests.AzureServiceBusTests.WorkflowReceivesMessage_WhenSendingMessageToTopic": "TODO",
    },
}
DEFAULT_COMMAND_TIMEOUT_SECONDS = 1800


def classify_project(repository: str, project: dict[str, Any]) -> tuple[str, str]:
    """Classify by recorded project inputs, not by folder name alone."""
    path = project["path"]
    packages = {reference["id"] for reference in project.get("package_references", [])}

    if path.startswith("test/performance/"):
        return "performance", "Benchmark project; kept outside the correctness test lane."
    if "Microsoft.NET.Test.Sdk" not in packages:
        return "build-only", "No Microsoft.NET.Test.Sdk reference; this is a host/build input."
    containers = sorted(package for package in packages if package.startswith("Testcontainers."))
    if containers:
        return "docker-service", "Declares container-backed test dependencies: " + ", ".join(containers)
    return "local-test", "No Testcontainers dependency is declared; runtime tests may still expose other requirements."


def git_head(path: Path) -> str:
    result = subprocess.run(
        ["git", "-C", str(path), "rev-parse", "HEAD"],
        text=True,
        capture_output=True,
        check=False,
    )
    if result.returncode:
        raise ValueError(f"Cannot read git HEAD for source {path}: {result.stderr.strip()}")
    return result.stdout.strip()


def verify_source_pin(repository: str, expected: str, actual: str) -> None:
    if actual != expected:
        raise ValueError(f"Source pin mismatch for {repository}: inventory={expected}, checkout={actual}")


def verify_source_clean(repository: str, status: str) -> None:
    if status.strip():
        raise ValueError(f"Source checkout for {repository} is not clean: {status.strip()}")


def verify_project_reference_paths(
    project_file: Path,
    core_source: Path,
    extensions_source: Path,
    references: list[Path],
    requires_core_elsa: bool = False,
) -> None:
    core_source = core_source.resolve()
    extensions_source = extensions_source.resolve()
    missing = [reference for reference in references if not reference.is_file()]
    if missing:
        raise ValueError(f"Project references are missing from {project_file}: {missing}")

    def within(path: Path, root: Path) -> bool:
        path = path.resolve()
        return path == root or root in path.parents

    outside_sources = [
        reference for reference in references
        if not within(reference, core_source) and not within(reference, extensions_source)
    ]
    if outside_sources:
        raise ValueError(
            f"Project references resolve outside the pinned Core and Extensions checkouts for {project_file}: "
            f"{outside_sources}"
        )

    expected_core_elsa = core_source / "src/modules/Elsa/Elsa.csproj"
    if requires_core_elsa and expected_core_elsa.resolve() not in {reference.resolve() for reference in references}:
        raise ValueError(
            f"Slack does not resolve its Elsa project reference to the supplied Core checkout: {expected_core_elsa}"
        )


def resolved_project_inputs(project_file: Path, timeout_seconds: int, use_project_references: bool) -> tuple[list[Path], list[str]]:
    """Evaluate every declared framework; outer-build items alone can miss conditional references."""
    def evaluate(framework: str | None = None) -> dict[str, Any]:
        command = [
            "dotnet", "msbuild", str(project_file), "-nologo",
            "-getItem:ProjectReference,PackageReference",
            "-getProperty:TargetFramework,TargetFrameworks",
            f"-property:UseProjectReferences={str(use_project_references).lower()}",
        ]
        if framework:
            command.append(f"-property:TargetFramework={framework}")
        try:
            result = subprocess.run(command, text=True, capture_output=True, check=False, timeout=timeout_seconds)
        except subprocess.TimeoutExpired as error:
            raise ValueError(f"Project input evaluation timed out for {project_file} after {timeout_seconds}s") from error
        if result.returncode:
            raise ValueError(f"Cannot evaluate project inputs for {project_file}: {result.stdout.strip()} {result.stderr.strip()}")
        try:
            value = json.loads(result.stdout)
            if not isinstance(value["Items"]["ProjectReference"], list) or not isinstance(value["Items"]["PackageReference"], list):
                raise ValueError("Expected item arrays")
            return value
        except (json.JSONDecodeError, KeyError, TypeError, ValueError) as error:
            raise ValueError(f"MSBuild returned invalid project-input JSON for {project_file}: {error}") from error

    outer = evaluate()
    properties = outer.get("Properties", {})
    frameworks = set(filter(None, properties.get("TargetFrameworks", "").split(";")))
    if properties.get("TargetFramework"):
        frameworks.add(properties["TargetFramework"])
    evaluations = [outer] + [evaluate(framework) for framework in sorted(frameworks)]
    references = {Path(item["FullPath"]).resolve() for value in evaluations for item in value["Items"]["ProjectReference"]}
    packages = {item["Identity"] for value in evaluations for item in value["Items"]["PackageReference"]}
    return sorted(references), sorted(packages)


def verify_resolved_project_graph(plan: dict[str, Any], sources: dict[str, Path], timeout_seconds: int) -> None:
    core_source = sources["elsa-core"].resolve()
    extensions_source = sources["elsa-extensions"].resolve()
    release_repository, release_project_path = plan["module_change_scenario"]["changed_project"].split(":", 1)
    if release_repository != "elsa-extensions":
        raise ValueError(f"Source-binding preflight does not support the release-unit repository: {release_repository}")
    slack_project = (extensions_source / release_project_path).resolve()
    source_package_ids = {package.casefold() for package in plan["source_package_ids"]}
    # A Core root uses package references for external infrastructure. An Extensions root passes
    # UseProjectReferences=true to its whole build graph, including referenced Core projects.
    pending = [(slack_project, True)] + [
        (Path(entry["project_file"]).resolve(), entry["key"].startswith("elsa-extensions:"))
        for entry in sorted(plan["projects"], key=lambda row: not row["key"].startswith("elsa-extensions:")) if entry["lane"] != "performance"
    ]
    visited: set[tuple[Path, bool]] = set()
    receipt: dict[str, Any] = {"status": "checking", "framework_scope": "all declared frameworks", "projects": []}
    plan["source_binding_preflight"] = receipt
    try:
        unresolved = plan.get("unresolved_source_package_ids", [])
        receipt["unresolved_source_package_ids"] = unresolved
        if unresolved:
            raise ValueError(
                f"Source package identity is missing for packable or unclassified projects: {unresolved}. "
                "Evaluate and record their package identities before claiming source closure."
            )
        while pending:
            project_file, use_project_references = pending.pop(0)
            identity = (project_file, use_project_references)
            if identity in visited:
                continue
            visited.add(identity)
            references, packages = resolved_project_inputs(project_file, timeout_seconds, use_project_references)
            remaining_source_packages = [package for package in packages if package.casefold() in source_package_ids]
            receipt["projects"].append({
                "project": str(project_file), "use_project_references": use_project_references,
                "project_references": [str(path) for path in references],
                "remaining_source_package_references": remaining_source_packages,
            })
            if remaining_source_packages:
                raise ValueError(
                    f"Pinned-source proof still resolves source-owned packages from a feed/cache in {project_file}: "
                    f"{remaining_source_packages}. Use reviewed source bindings before claiming source closure."
                )
            verify_project_reference_paths(project_file, core_source, extensions_source, references,
                                           requires_core_elsa=project_file == slack_project and use_project_references)
            pending.extend((reference, use_project_references) for reference in references)
    except Exception:
        receipt["status"] = "failed"
        raise
    receipt["status"] = "passed"


def deferred_execution_projects(projects: list[dict[str, Any]], execution: list[dict[str, Any]]) -> list[str]:
    required = {entry["key"] for entry in projects if entry["lane"] in {"local-test", "docker-service", "build-only"}}
    executed = {receipt["key"] for receipt in execution}
    return sorted(required - executed)


def git_status(path: Path) -> str:
    result = subprocess.run(
        ["git", "-C", str(path), "status", "--porcelain", "--untracked-files=normal"],
        text=True,
        capture_output=True,
        check=False,
    )
    if result.returncode:
        raise ValueError(f"Cannot read git status for source {path}: {result.stderr.strip()}")
    return result.stdout


def parse_sources(values: list[str]) -> dict[str, Path]:
    sources: dict[str, Path] = {}
    for value in values:
        repository, separator, raw_path = value.partition("=")
        if not separator or not repository or not raw_path:
            raise ValueError(f"Source must be REPOSITORY=PATH, got {value!r}")
        if repository in sources:
            raise ValueError(f"Duplicate source mapping for {repository}")
        sources[repository] = Path(raw_path).resolve()
    return sources


def build_plan(
    inventory_path: Path,
    sources: dict[str, Path],
    release_manifest_path: Path = MANIFEST_PATH,
    source_binding: dict[str, Any] | None = None,
) -> dict[str, Any]:
    inventory = json.loads(inventory_path.read_text(encoding="utf-8"))
    if source_binding is not None and set(sources) != {"elsa-core", "elsa-extensions"}:
        raise ValueError("Source-bound proof requires both pinned Core and Extensions mappings")
    manifest = load_manifest(release_manifest_path)
    unit = get_unit(manifest, RELEASE_UNIT_ID)
    validate_against_inventory(unit, inventory)
    graph = InventoryGraph(inventory)
    impacted = sorted(graph.affected_tests([CHANGED_PROJECT]))
    release_unit_key = source_project_key(unit)
    declared_unit_tests = source_test_project_keys(unit)
    module_impacted = sorted(graph.affected_tests([release_unit_key]))
    packages = sorted(graph.package_ids([release_unit_key]))
    if packages != [unit["package_id"]]:
        raise ValueError(f"Expected only {unit['package_id']} in the release unit; selector returned {packages}")
    if module_impacted != sorted(declared_unit_tests):
        raise ValueError(
            f"Expected the manifest's release-unit tests {sorted(declared_unit_tests)} "
            f"for a module-only change; selector returned {module_impacted}"
        )

    source_receipts: dict[str, dict[str, str]] = {}
    for repository, source_path in sources.items():
        expected = inventory["repositories"][repository]["commit"]
        if not source_path.is_dir():
            raise ValueError(f"Source checkout does not exist: {source_path}")
        actual = git_head(source_path)
        verify_source_pin(repository, expected, actual)
        if repository == "elsa-extensions" and source_binding is not None:
            verify_overlay(inventory, sources["elsa-core"],
                           Path(source_binding["pristine_extensions"]), source_path, source_binding)
            initial_status = "reviewed source-bound overlay"
        else:
            initial_status = git_status(source_path)
            verify_source_clean(repository, initial_status)
        source_receipts[repository] = {
            "expected_commit": expected,
            "actual_commit": actual,
            "initial_working_tree": initial_status if source_binding is not None and repository == "elsa-extensions" else "clean",
        }

    entries = []
    for repository, relative_path in impacted:
        project = graph.projects[(repository, relative_path)]
        if repository not in ("elsa-core", "elsa-extensions"):
            raise ValueError(f"Selected path belongs to an unsupported source repository: {repository}:{relative_path}")
        lane, reason = classify_project(repository, project)
        frameworks = project.get("target_frameworks", [])
        entry: dict[str, Any] = {
            "key": f"{repository}:{relative_path}",
            "lane": lane,
            "reason": reason,
            "declared_target_frameworks": frameworks,
            "untested_target_frameworks": [framework for framework in frameworks if framework != "net10.0"],
        }
        if sources:
            if repository not in sources:
                raise ValueError(f"No source checkout supplied for selected repository {repository}")
            source_project = sources[repository] / relative_path
            if not source_project.is_file():
                raise ValueError(f"Selected project is missing from pinned checkout: {source_project}")
            entry["project_file"] = str(source_project)
        entries.append(entry)

    lane_counts: dict[str, int] = {}
    for entry in entries:
        lane_counts[entry["lane"]] = lane_counts.get(entry["lane"], 0) + 1

    return {
        "scenario": {
            "changed_project": f"{CHANGED_PROJECT[0]}:{CHANGED_PROJECT[1]}",
            "release_unit_projects": [f"{release_unit_key[0]}:{release_unit_key[1]}"],
            "packages_to_pack": packages,
            "affected_test_project_count": len(impacted),
        },
        "module_change_scenario": {
            "changed_project": f"{release_unit_key[0]}:{release_unit_key[1]}",
            "affected_test_projects": [f"{repository}:{path}" for repository, path in module_impacted],
            "affected_test_project_count": len(module_impacted),
            "release_unit_projects": [f"{release_unit_key[0]}:{release_unit_key[1]}"],
            "packages_to_pack": packages,
        },
        "release_unit_manifest": {
            "path": "doc/integration-program/release-units.json"
            if release_manifest_path.resolve() == MANIFEST_PATH.resolve()
            else release_manifest_path.name,
            "sha256": hashlib.sha256(release_manifest_path.read_bytes()).hexdigest(),
            "unit_id": unit["id"],
            "package_id": unit["package_id"],
            "package_target_frameworks": unit["target_frameworks"],
            "tested_artifact_dependencies": unit["tested_artifact_dependencies"],
            "current_publisher": unit["publisher"]["repository"],
            "local_proof_version": unit["versioning"]["local_proof_version"],
            "local_proof_publishable": unit["versioning"]["local_proof_may_publish"],
        },
        "inventory_snapshot_date": inventory["snapshot_date"],
        "source_pins": {
            repository: inventory["repositories"][repository]["commit"]
            for repository in ("elsa-core", "elsa-extensions")
        },
        "source_checkouts": source_receipts,
        "source_binding": source_binding,
        "source_package_ids": sorted({
            project["package_id"] for repository in ("elsa-core", "elsa-extensions")
            for project in inventory["project_inventory"][repository] if project.get("package_id")
        }),
        "unresolved_source_package_ids": sorted(
            f"{repository}:{project['path']}"
            for repository in ("elsa-core", "elsa-extensions")
            for project in inventory["project_inventory"][repository]
            if project.get("is_packable") is not False and not project.get("package_id")
        ),
        "affected_test_project_count": len(entries),
        "ambiguous_package_edges_resolved_conservatively": len(graph.ambiguous_package_edges),
        "lane_counts": lane_counts,
        "projects": entries,
        "execution": [],
        "correctness_closure_complete": False,
        "limitations": [
            "The inventory graph selects project-level test inputs; it does not prove all possible runtime service requirements.",
            "Performance projects are classified separately and are not executed by this correctness gate.",
            "The pinned source baseline contains four exact skipped test results across Slack and the two component projects; each is reported and keeps the closure incomplete.",
            "The shared Core source-change scenario expands the selected test set; the Slack module-only scenario is also checked and selects one test project.",
        ],
    }


def parse_trx(path: Path) -> tuple[dict[str, int], list[dict[str, str]]]:
    root = ET.parse(path).getroot()
    counters = next((element for element in root.iter() if element.tag.rsplit("}", 1)[-1] == "Counters"), None)
    if counters is None:
        raise ValueError(f"TRX receipt has no Counters element: {path}")
    names = ("total", "executed", "passed", "failed", "error", "timeout", "aborted", "inconclusive", "notRunnable", "notExecuted")
    counts = {name: int(counters.get(name, "0")) for name in names}
    results = []
    for element in root.iter():
        if element.tag.rsplit("}", 1)[-1] != "UnitTestResult":
            continue
        result = {"name": element.get("testName", ""), "outcome": element.get("outcome", "")}
        if result["outcome"] == "NotExecuted":
            for child in element.iter():
                if child.tag.rsplit("}", 1)[-1] == "Message" and child.text and child.text.strip():
                    result["skip_reason"] = child.text.strip()
                    break
        results.append(result)
    if counts["total"] != len(results):
        raise ValueError(f"TRX result count does not match Counters.total: {path}")
    return counts, results


def classify_trx_result(
    key: str,
    exit_code: int,
    counters: dict[str, int],
    results: list[dict[str, str]],
) -> str:
    if exit_code != 0 or any(counters[name] for name in ("failed", "error", "timeout", "aborted")):
        return "failed"
    if any(result["outcome"] in {"Failed", "Error", "Timeout", "Aborted"} for result in results):
        return "failed"
    if (
        counters["executed"] > 0
        and counters["passed"] == counters["executed"]
        and counters["total"] == counters["executed"]
        and all(result["outcome"] == "Passed" for result in results)
    ):
        return "passed"
    expected_skips = KNOWN_BASELINE_SKIPS.get(key)
    observed_skips = {
        result["name"]: result.get("skip_reason", "")
        for result in results
        if result["outcome"] == "NotExecuted"
    }
    if (
        expected_skips
        and observed_skips == expected_skips
        and all(result["outcome"] in {"Passed", "NotExecuted"} for result in results)
        and counters["executed"] == counters["passed"] == sum(result["outcome"] == "Passed" for result in results)
    ):
        return "known-baseline-skip"
    return "incomplete"


def _safe_name(key: str) -> str:
    return re.sub(r"[^A-Za-z0-9_.-]+", "_", key)


def execute_plan(
    plan: dict[str, Any],
    sources: dict[str, Path],
    output_dir: Path,
    include_docker: bool,
    only_projects: set[str] | None = None,
    command_timeout_seconds: int = DEFAULT_COMMAND_TIMEOUT_SECONDS,
    source_binding: dict[str, Any] | None = None,
    inventory: dict[str, Any] | None = None,
) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    lane_by_name = {"local-test", "build-only"}
    if include_docker:
        lane_by_name.add("docker-service")
    selected = [entry for entry in plan["projects"] if entry["lane"] in lane_by_name]
    if only_projects is not None:
        selected_keys = {entry["key"] for entry in selected}
        invalid = only_projects - selected_keys
        if invalid:
            raise ValueError(f"--only includes projects outside the enabled test lanes: {sorted(invalid)}")
        selected = [entry for entry in selected if entry["key"] in only_projects]
    failed = False
    expected_trx_paths: set[Path] = set()

    for index, entry in enumerate(selected, start=1):
        key = entry["key"]
        project_path = Path(entry["project_file"])
        log_path = output_dir / f"{index:02d}-{_safe_name(key)}.log"
        trx_path = output_dir / "TestResults" / f"{index:02d}-{_safe_name(key)}.trx"
        trx_path.parent.mkdir(parents=True, exist_ok=True)
        is_extension = key.startswith("elsa-extensions:")
        if entry["lane"] == "build-only":
            command = ["dotnet", "build", str(project_path), "--framework", "net10.0", "-m:1"]
        else:
            expected_trx_paths.add(trx_path.resolve())
            command = [
                "dotnet", "test", str(project_path), "--framework", "net10.0", "-m:1",
                "--logger", f"trx;LogFileName={trx_path.name}",
                "--results-directory", str(trx_path.parent),
            ]
        command.append(f"-p:UseProjectReferences={str(is_extension).lower()}")

        print(f"[{index}/{len(selected)}] {entry['lane']} {key}", flush=True)
        try:
            result = subprocess.run(
                command,
                text=True,
                capture_output=True,
                check=False,
                timeout=command_timeout_seconds,
            )
        except subprocess.TimeoutExpired as error:
            stdout = error.stdout.decode(errors="replace") if isinstance(error.stdout, bytes) else (error.stdout or "")
            stderr = error.stderr.decode(errors="replace") if isinstance(error.stderr, bytes) else (error.stderr or "")
            message = f"Command timed out after {command_timeout_seconds}s: {' '.join(command)}"
            log_path.write_text(stdout + stderr + "\n" + message + "\n", encoding="utf-8")
            plan["execution"].append({
                "key": key,
                "lane": entry["lane"],
                "exit_code": 124,
                "log": str(log_path),
                "status": "failed",
                "timeout_seconds": command_timeout_seconds,
                "error": message,
            })
            failed = True
            print(message, file=sys.stderr)
            continue
        log_path.write_text(result.stdout + result.stderr, encoding="utf-8")
        receipt: dict[str, Any] = {
            "key": key,
            "lane": entry["lane"],
            "exit_code": result.returncode,
            "log": str(log_path),
        }

        if entry["lane"] == "build-only":
            receipt["status"] = "passed" if result.returncode == 0 else "failed"
        else:
            try:
                counters, test_results = parse_trx(trx_path)
                receipt["trx"] = str(trx_path)
                receipt["counters"] = counters
                receipt["test_results"] = test_results
                receipt["status"] = classify_trx_result(key, result.returncode, counters, test_results)
            except (OSError, ET.ParseError, ValueError) as error:
                receipt["status"] = "failed"
                receipt["receipt_error"] = str(error)

        if receipt["status"] == "failed":
            failed = True
            tail = "\n".join((result.stdout + result.stderr).splitlines()[-20:])
            print(tail, file=sys.stderr)
        plan["execution"].append(receipt)

    deferred = deferred_execution_projects(plan["projects"], plan["execution"])
    partial = any(receipt["status"] in {"known-baseline-skip", "incomplete"} for receipt in plan["execution"])
    untested_frameworks = {
        entry["key"]: entry["untested_target_frameworks"]
        for entry in plan["projects"]
        if entry["untested_target_frameworks"] and entry["lane"] not in {"performance"}
    }
    if untested_frameworks:
        plan["untested_target_frameworks"] = untested_frameworks
        partial = True
    if deferred:
        plan["deferred_projects"] = deferred
        partial = True
    actual_trx_paths = {path.resolve() for path in (output_dir / "TestResults").glob("*.trx")}
    if actual_trx_paths != expected_trx_paths:
        missing = sorted(str(path) for path in expected_trx_paths - actual_trx_paths)
        unexpected = sorted(str(path) for path in actual_trx_paths - expected_trx_paths)
        plan["trx_receipt_error"] = {"missing": missing, "unexpected": unexpected}
        failed = True
    plan["correctness_closure_complete"] = not failed and not partial
    plan["execution_summary"] = {
        "executed_project_count": len(plan["execution"]),
        "failed_project_count": sum(receipt["status"] == "failed" for receipt in plan["execution"]),
        "known_skip_project_count": sum(receipt["status"] == "known-baseline-skip" for receipt in plan["execution"]),
        "included_docker_service_lane": include_docker,
        "performance_projects_executed": False,
        "tested_framework": "net10.0",
        "projects_with_untested_frameworks": len(untested_frameworks),
        "command_timeout_seconds": command_timeout_seconds,
    }
    final_sources: dict[str, dict[str, str]] = {}
    for repository, source_path in sources.items():
        expected = plan["source_pins"][repository]
        actual = git_head(source_path)
        verify_source_pin(repository, expected, actual)
        if repository == "elsa-extensions" and source_binding is not None:
            if inventory is None:
                raise ValueError("Source-bound final check requires the inventory")
            verify_overlay(inventory, sources["elsa-core"],
                           Path(source_binding["pristine_extensions"]), source_path, source_binding)
            final_sources[repository] = {"final_commit": actual, "working_tree": "reviewed source-bound overlay"}
        else:
            status = git_status(source_path)
            verify_source_clean(repository, status)
            final_sources[repository] = {"final_commit": actual, "working_tree": "clean"}
    plan["final_source_checkouts"] = final_sources


def append_step_summary(summary_path: str, content: str) -> None:
    """Write optional CI presentation output without changing the proof result."""
    try:
        with open(summary_path, "a", encoding="utf-8") as stream:
            stream.write(content)
    except OSError as error:
        print(f"Could not append GitHub step summary {summary_path}: {error}", file=sys.stderr)


def write_output(plan: dict[str, Any], path: Path | None) -> None:
    content = json.dumps(plan, indent=2) + "\n"
    if path:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")
        print(
            f"Wrote {path}: {plan['affected_test_project_count']} selected projects, "
            f"packages={','.join(plan['scenario']['packages_to_pack'])}, "
            f"correctness_complete={plan.get('correctness_closure_complete', False)}"
        )
    else:
        print(content, end="")
    summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary_path:
        status = "complete" if plan.get("correctness_closure_complete") else "partial / not complete"
        summary = [
            "## Core → Slack dependency closure",
            "",
            f"Gate status: **{status}**",
            f"Selected projects: {plan['affected_test_project_count']}",
            f"Test scenarios: Core change reaches {plan['scenario']['affected_test_project_count']} projects; Slack-only change reaches {plan['module_change_scenario']['affected_test_project_count']} project.",
            f"Package selection: {', '.join(plan['scenario']['packages_to_pack'])}",
            f"Lane counts: `{json.dumps(plan['lane_counts'], sort_keys=True)}`",
        ]
        if plan.get("execution_summary"):
            summary.append(f"Execution: `{json.dumps(plan['execution_summary'], sort_keys=True)}`")
        skips = [
            f"- `{receipt['key']}`: {result['name']}: {result.get('skip_reason', 'no skip reason recorded')}"
            for receipt in plan.get("execution", [])
            for result in receipt.get("test_results", [])
            if result["outcome"] == "NotExecuted"
        ]
        if skips:
            summary.extend(["", "Skipped test results:", *skips])
        if plan.get("limitations"):
            summary.extend(["", "Limitations:", *[f"- {limitation}" for limitation in plan["limitations"]]])
        append_step_summary(summary_path, "\n".join(summary) + "\n")


def write_failure_output(
    path: Path,
    *,
    phase: str,
    error: Exception,
    args: argparse.Namespace,
    partial_plan: dict[str, Any] | None,
) -> None:
    receipt: dict[str, Any] = {
        "schema_version": 1,
        "result": "failed",
        "failure": {
            "phase": phase,
            "type": type(error).__name__,
            "message": str(error),
        },
        "requested": {
            "inventory": str(args.inventory),
            "sources": args.source,
            "source_binding_receipt": str(args.source_binding_receipt) if args.source_binding_receipt else None,
            "run": args.run,
            "preflight_only": args.preflight_only,
            "include_docker": args.include_docker,
            "only": args.only,
            "command_timeout_seconds": args.command_timeout_seconds,
        },
    }
    if partial_plan is not None:
        receipt["partial_plan"] = partial_plan

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote failure receipt {path} (phase={phase})", file=sys.stderr)
    summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary_path:
        append_step_summary(
            summary_path,
            "## Core → Slack dependency closure\n\n"
            f"Gate status: **failed during {phase}**\n\n"
            f"Error: `{type(error).__name__}: {error}`\n",
        )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--inventory", type=Path, required=True)
    parser.add_argument("--release-manifest", type=Path, default=MANIFEST_PATH)
    parser.add_argument("--source", action="append", default=[], help="Pinned checkout mapping REPOSITORY=PATH")
    parser.add_argument("--source-binding-receipt", type=Path,
                        help="Reviewed disposable Extensions source-binding receipt; original checkouts stay clean")
    parser.add_argument("--run", action="store_true", help="Run local tests and the build-only host")
    parser.add_argument("--preflight-only", action="store_true", help="Evaluate the complete source graph without claiming test execution")
    parser.add_argument("--include-docker", action="store_true", help="Also run projects declaring Testcontainers dependencies")
    parser.add_argument("--only", action="append", default=[], help="Run only this selected repository:path entry (repeatable)")
    parser.add_argument(
        "--command-timeout-seconds",
        type=int,
        default=DEFAULT_COMMAND_TIMEOUT_SECONDS,
        help=f"Per-project build/test and MSBuild evaluation timeout (default: {DEFAULT_COMMAND_TIMEOUT_SECONDS})",
    )
    parser.add_argument("--output", type=Path, help="Write the JSON receipt to this path")
    parser.add_argument("--github-output", type=Path, help="Append source checkout pins as GitHub Actions outputs")
    args = parser.parse_args()

    phase = "source parsing"
    plan: dict[str, Any] | None = None
    try:
        sources = parse_sources(args.source)
        source_binding = json.loads(args.source_binding_receipt.read_text(encoding="utf-8")) if args.source_binding_receipt else None
        phase = "plan construction and source preflight"
        plan = build_plan(args.inventory, sources, args.release_manifest, source_binding)
        if args.command_timeout_seconds <= 0:
            raise ValueError("--command-timeout-seconds must be greater than zero")
        if args.github_output:
            phase = "GitHub output generation"
            inventory = json.loads(args.inventory.read_text(encoding="utf-8"))
            with args.github_output.open("a", encoding="utf-8") as output:
                for repository, output_name in (("elsa-core", "core"), ("elsa-extensions", "extensions")):
                    output.write(f"{output_name}={inventory['repositories'][repository]['commit']}\n")
        if args.run and args.preflight_only:
            raise ValueError("--run and --preflight-only cannot be combined")
        if args.run or args.preflight_only:
            phase = "execution preflight"
            if set(sources) != {"elsa-core", "elsa-extensions"}:
                raise ValueError("Execution requires pinned --source mappings for elsa-core and elsa-extensions")
            artifact_root = args.output.parent / "artifacts" if args.output else Path("artifacts/package-closure")
            run_artifact_dir = artifact_root / f"run-{uuid4().hex[:12]}"
            plan["artifact_directory"] = str(run_artifact_dir.resolve())
            verify_resolved_project_graph(plan, sources, args.command_timeout_seconds)
            if args.run:
                phase = "project test and host execution"
                execute_plan(
                    plan,
                    sources,
                    run_artifact_dir,
                    args.include_docker,
                    set(args.only) if args.only else None,
                    args.command_timeout_seconds,
                    source_binding,
                    json.loads(args.inventory.read_text(encoding="utf-8")) if source_binding else None,
                )
        phase = "success receipt writing"
        write_output(plan, args.output)
        if any(result.get("status") == "failed" for result in plan["execution"]):
            return 1
        if args.run and not plan["correctness_closure_complete"]:
            return 3
        return 0
    except Exception as error:
        print(f"package_closure: {error}", file=sys.stderr)
        if args.output:
            try:
                write_failure_output(
                    args.output,
                    phase=phase,
                    error=error,
                    args=args,
                    partial_plan=plan,
                )
            except OSError as receipt_error:
                print(f"Could not write failure receipt {args.output}: {receipt_error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
