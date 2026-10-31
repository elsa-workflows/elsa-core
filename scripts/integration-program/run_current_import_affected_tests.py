#!/usr/bin/env python3
"""Execute the shared-Core test closure selected from one exact imported source revision."""

from __future__ import annotations

import argparse
import hashlib
import json
import subprocess
import sys
import time
from pathlib import Path
from typing import Any

sys.dont_write_bytecode = True
from package_closure import parse_trx


ROOT = Path(__file__).resolve().parents[2]


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def git_output(root: Path, *arguments: str) -> str:
    result = subprocess.run(
        ["git", "-C", str(root), *arguments], text=True, capture_output=True, check=False
    )
    if result.returncode:
        raise ValueError(f"Cannot inspect source Git state: {result.stderr.strip()}")
    return result.stdout.strip()


def assert_clean_source(root: Path, expected_head: str) -> None:
    if git_output(root, "rev-parse", "HEAD") != expected_head:
        raise ValueError("Source revision changed from the expected commit")
    if git_output(root, "status", "--porcelain", "--untracked-files=normal"):
        raise ValueError("Source working tree is not clean")


def selected_nodes(selection: dict[str, Any], root: Path, head: str) -> list[dict[str, str]]:
    root = root.resolve()
    if selection.get("sourceRevision") != head or selection.get("acceptanceEligible") is not True:
        raise ValueError("Selection is not eligible for this exact source revision")
    if selection.get("scenarios", {}).get("testExecutionPerformed") is not False:
        raise ValueError("Expected a selection-only receipt")
    nodes = selection["scenarios"]["sharedCore"]["tests"]
    if not isinstance(nodes, list) or not nodes:
        raise ValueError("Selection has no shared-Core tests")
    seen: set[tuple[str, str]] = set()
    for node in nodes:
        project, framework = node["project"], node["framework"]
        if not isinstance(project, str) or not isinstance(framework, str):
            raise ValueError("Selected test path and framework must be strings")
        path = (root / project).resolve()
        if not path.is_relative_to(root) or not path.is_file() or path.suffix != ".csproj":
            raise ValueError(f"Selected project is missing or outside the source tree: {project}")
        if not framework.startswith("net") or (project, framework) in seen:
            raise ValueError(f"Invalid or duplicate selected test/TFM: {project} {framework}")
        seen.add((project, framework))
    return nodes


def classify(exit_code: int, counters: dict[str, int] | None, results: list[dict[str, str]]) -> str:
    if counters is None:
        return "failed"
    failure_counters = ("failed", "error", "timeout", "aborted", "inconclusive", "notRunnable")
    if exit_code or any(value < 0 for value in counters.values()) or any(
            counters.get(key, 0) for key in failure_counters):
        return "failed"
    outcomes = [result["outcome"] for result in results]
    if any(outcome not in {"Passed", "NotExecuted"} for outcome in outcomes):
        return "failed"
    if counters.get("notExecuted", 0) > outcomes.count("NotExecuted"):
        return "failed"
    passed = outcomes.count("Passed")
    if (counters["total"] != len(results) or counters["executed"] != passed
            or counters["passed"] != passed):
        return "failed"
    if not results or "NotExecuted" in outcomes:
        return "incomplete"
    return "passed"


def run_tests(root: Path, selection_path: Path, output: Path, expected_head: str, timeout: int) -> dict[str, Any]:
    root = root.resolve()
    output = output.resolve()
    if output == root or root in output.parents:
        raise ValueError("Evidence output must be outside the source tree")
    if output.exists() and any(output.iterdir()):
        raise ValueError("Evidence output directory must start empty")
    assert_clean_source(root, expected_head)
    head = expected_head
    selection = json.loads(selection_path.read_text(encoding="utf-8"))
    nodes = selected_nodes(selection, root, head)
    output.mkdir(parents=True, exist_ok=True)
    receipt: dict[str, Any] = {
        "schemaVersion": 1,
        "sourceRevision": head,
        "selectionSha256": sha256(selection_path),
        "runnerSha256": sha256(Path(__file__)),
        "configuration": "Debug",
        "projectCount": len(nodes),
        "completedCount": 0,
        "testExecutionPerformed": True,
        "publicationPerformed": False,
        "workingTreeCleanBefore": True,
        "execution": [],
    }

    for index, node in enumerate(nodes, start=1):
        project, framework = node["project"], node["framework"]
        log = output / f"{index:02d}.log"
        trx = output / f"{index:02d}.trx"
        command = [
            "dotnet", "test", str(root / project), "--configuration", "Debug", "--framework", framework,
            "-m:1", "--no-restore", "-p:UseProjectReferences=true", "-p:IsPackable=false",
            "-p:GeneratePackageOnBuild=false", "--logger", f"trx;LogFileName={trx.name}",
            "--results-directory", str(output),
        ]
        started = time.monotonic()
        try:
            result = subprocess.run(command, cwd=root, text=True, capture_output=True, check=False, timeout=timeout)
            log.write_text(result.stdout + result.stderr, encoding="utf-8")
            exit_code = result.returncode
        except subprocess.TimeoutExpired as error:
            stdout = error.stdout.decode(errors="replace") if isinstance(error.stdout, bytes) else error.stdout or ""
            stderr = error.stderr.decode(errors="replace") if isinstance(error.stderr, bytes) else error.stderr or ""
            log.write_text(stdout + stderr + "\nTIMEOUT\n", encoding="utf-8")
            exit_code = 124
        row: dict[str, Any] = {
            "project": project,
            "framework": framework,
            "exitCode": exit_code,
            "durationSeconds": round(time.monotonic() - started, 2),
            "logSha256": sha256(log),
        }
        if trx.exists():
            try:
                counters, results = parse_trx(trx)
                skips = [
                    {"name": item["name"], "reason": item.get("skip_reason", "")}
                    for item in results if item["outcome"] == "NotExecuted"
                ]
                row.update(counters=counters, skips=skips, trxSha256=sha256(trx))
                row["status"] = classify(exit_code, counters, results)
            except (OSError, ValueError, KeyError) as error:
                row.update(status="failed", error=str(error))
        else:
            row["status"] = "failed"
            row["error"] = "No TRX was produced"
        receipt["execution"].append(row)
        receipt["completedCount"] = index
        (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
        print(f"[{index}/{len(nodes)}] {project} ({framework}): {row['status']}", flush=True)

    assert_clean_source(root, head)
    if sha256(Path(__file__)) != receipt["runnerSha256"]:
        raise ValueError("Test runner changed during execution")
    if sha256(selection_path) != receipt["selectionSha256"]:
        raise ValueError("Selection receipt changed during execution")
    receipt["workingTreeCleanAfter"] = True
    receipt["correctnessClosureComplete"] = all(row["status"] == "passed" for row in receipt["execution"])
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    return receipt


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--selection", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--expected-head", required=True)
    parser.add_argument("--timeout-seconds", type=int, default=900)
    args = parser.parse_args()
    try:
        if args.timeout_seconds < 1:
            raise ValueError("Test timeout must be positive")
        receipt = run_tests(args.root, args.selection, args.output_dir, args.expected_head, args.timeout_seconds)
        if any(row["status"] == "failed" for row in receipt["execution"]):
            return 1
        return 0 if receipt["correctnessClosureComplete"] else 3
    except (OSError, ValueError, KeyError, TypeError, json.JSONDecodeError) as error:
        print(f"Current imported-source test execution failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
