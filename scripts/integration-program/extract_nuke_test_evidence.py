#!/usr/bin/env python3
"""Extract fail-closed canonical NUKE Test evidence from a completed run."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path, PurePosixPath
from typing import Any

import prepare_consolidated_build as preparation
from refresh_canonical_dependency_graph import parse_solution


SUMMARY_RE = re.compile(
    r"(?P<status>Passed!|Skipped!|Failed!)\s+- Failed:\s*(?P<failed>\d+),"
    r"\s*Passed:\s*(?P<passed>\d+),\s*Skipped:\s*(?P<skipped>\d+),"
    r"\s*Total:\s*(?P<total>\d+),.*?-\s*(?P<assembly>[^ ]+\.dll)\s+\((?P<framework>[^)]+)\)"
)
COMMAND_RE = re.compile(r"dotnet test\s+(?P<project>[^\s]+\.csproj)\s+--configuration\s+(?P<configuration>\S+)\s+--no-build\s+--results-directory\s+(?P<results>\S+)\s+--logger\s+trx;LogFileName=(?P<trx>[^\s]+)")
NUKE_TEST_RE = re.compile(r"^\s*Test\s+Succeeded\s+(?P<duration>\S+).*?Passed:\s*(?P<passed>[\d,]+),\s*Skipped:\s*(?P<skipped>[\d,]+)")
TARGET_RE = re.compile(r"^\s*(?P<target>Restore|Compile|Test)\s+Succeeded\s+(?P<duration>\S+)")
NAMESPACE = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def json_file(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"Expected a JSON object in {path}")
    return value


def _pin_profile(root: Path, prep_path: Path, import_path: Path, patch_path: Path,
                 profile_template: Path | None) -> dict[str, Any]:
    prep = json_file(prep_path)
    imported = json_file(import_path)
    commits = prep.get("sourceCommits")
    if not isinstance(commits, dict) or any(not isinstance(commits.get(key), str) or not re.fullmatch(r"[0-9a-f]{40}", commits[key]) for key in ("core", "extensions", "studio")):
        raise ValueError("Preparation receipt has incomplete source commit pins")
    if prep.get("canonicalSolution") != "Elsa.sln" or imported.get("canonicalSolution", "Elsa.sln") != "Elsa.sln":
        raise ValueError("Source receipts do not identify canonical Elsa.sln")
    rehearsal_commit = prep.get("rehearsalCommit")
    if not isinstance(rehearsal_commit, str) or not re.fullmatch(r"[0-9a-f]{40}", rehearsal_commit):
        raise ValueError("Preparation receipt has no full rehearsal commit")
    if imported.get("rehearsalCommit") != rehearsal_commit or imported.get("sourceCommits") != commits:
        raise ValueError("Import receipt source pins differ from preparation receipt")
    if imported.get("exactBlobAndModeMapping") is not True or imported.get("originalHistoriesReachable") is not True:
        raise ValueError("Import receipt does not prove exact mapping and source ancestry")
    if sha256(patch_path) != prep.get("patchSha256"):
        raise ValueError("Source integration patch hash differs from preparation receipt")
    try:
        current_commit = _git(root, "rev-parse", "HEAD")
    except Exception as error:  # pragma: no cover - exercised through CLI failures.
        raise ValueError(f"Cannot verify prepared source checkout HEAD: {error}") from error
    if current_commit != rehearsal_commit:
        raise ValueError("Prepared source checkout HEAD differs from preparation receipt")
    preparation.verify_import_lineage(root, imported)

    profile: dict[str, Any] = {
        "core": commits["core"],
        "extensions": commits["extensions"],
        "studio": commits["studio"],
        "rawRehearsalCommit": rehearsal_commit,
        "canonicalSolution": "Elsa.sln",
    }
    if profile_template:
        document = json_file(profile_template)
        overlay_receipt = "reviewedOverlayReceipt" in document
        template = document if overlay_receipt else document.get("profile", {})
        if not isinstance(template, dict):
            raise ValueError("Profile template must contain an object")
        if overlay_receipt and template.get("sourcePins") != commits:
            raise ValueError("Overlay receipt source pins differ from preparation receipt")
        expected_pins = {
            "core": commits["core"],
            "extensions": commits["extensions"],
            "studio": commits["studio"],
            "rawRehearsalCommit": rehearsal_commit,
            "canonicalSolution": "Elsa.sln",
        }
        for key, value in expected_pins.items():
            if key in template and template[key] != value:
                raise ValueError(f"Profile template {key} differs from source receipts")
        supplemental = template.get("reviewedOverlayReceipt" if overlay_receipt else "supplementalPatches", [])
        if not isinstance(supplemental, list):
            raise ValueError("Profile template supplementalPatches must be a list")
        names = set()
        for item in supplemental:
            if not isinstance(item, dict) or not isinstance(item.get("name"), str) or not re.fullmatch(r"[0-9a-f]{64}", str(item.get("sha256", ""))):
                raise ValueError("Profile template contains an invalid supplemental patch receipt")
            if item["name"] in names:
                raise ValueError(f"Profile template repeats supplemental patch: {item['name']}")
            names.add(item["name"])
            if overlay_receipt:
                if Path(item["name"]).name != item["name"] or not item["name"].endswith(".patch"):
                    raise ValueError(f"Overlay receipt has an unsafe patch name: {item['name']}")
                candidate = patch_path.parent / item["name"]
                if not candidate.is_file() or sha256(candidate) != item["sha256"]:
                    raise ValueError(f"Overlay patch bytes differ from receipt: {item['name']}")
        profile["supplementalPatches"] = supplemental
        if overlay_receipt:
            profile["overlayReceiptSha256"] = sha256(profile_template)
    profile["sourceReceipts"] = {
        "preparationReceiptSha256": sha256(prep_path),
        "importReceiptSha256": sha256(import_path),
        "sourceIntegrationPatchSha256": sha256(patch_path),
    }
    return profile


def _git(root: Path, *args: str) -> str:
    import subprocess
    result = subprocess.run(["git", "-C", str(root), *args], check=True, text=True, capture_output=True)
    return result.stdout.strip()


def _counter_values(root: ET.Element) -> dict[str, int]:
    counters = root.find(f"{NAMESPACE}ResultSummary/{NAMESPACE}Counters")
    if counters is None:
        raise ValueError("TRX has no result counters")
    fields = ("total", "executed", "passed", "failed", "error", "timeout", "aborted")
    try:
        return {field: int(counters.attrib[field]) for field in fields}
    except (KeyError, ValueError) as error:
        raise ValueError("TRX has incomplete or invalid result counters") from error


def _trx_rows(results_dir: Path) -> list[dict[str, Any]]:
    rows = []
    for path in sorted(results_dir.glob("*.trx"), key=lambda candidate: candidate.name):
        try:
            document = ET.parse(path).getroot()
        except ET.ParseError as error:
            raise ValueError(f"Malformed retained TRX {path.name}: {error}") from error
        code_bases = sorted({node.attrib["codeBase"] for node in document.iter(f"{NAMESPACE}TestMethod") if node.attrib.get("codeBase")})
        if not code_bases:
            raise ValueError(f"Retained TRX has no TestMethod codeBase: {path.name}")
        counters = _counter_values(document)
        if counters["executed"] > counters["total"]:
            raise ValueError(f"TRX executed count exceeds total: {path.name}")
        if sum(counters[key] for key in ("passed", "failed", "error", "timeout", "aborted")) != counters["executed"]:
            raise ValueError(f"TRX counters do not reconcile: {path.name}")
        if any(counters[key] for key in ("failed", "error", "timeout", "aborted")):
            raise ValueError(f"TRX reports failing or incomplete tests: {path.name}")
        rows.append({"file": path.name, "counters": counters, "codeBases": code_bases})
    return rows


def _duration_seconds(value: str) -> int:
    parts = [int(part) for part in value.split(":")]
    if len(parts) == 2:
        return parts[0] * 60 + parts[1]
    if len(parts) == 3:
        return parts[0] * 3600 + parts[1] * 60 + parts[2]
    raise ValueError(f"Unsupported NUKE duration format: {value}")


def extract(log_path: Path, root: Path, prep_path: Path, import_path: Path, patch_path: Path,
            profile_template: Path | None, command: str) -> dict[str, Any]:
    if not re.search(r"(?:^|\s)--target\s+Test(?:\s|$)", command):
        raise ValueError("Recorded invocation must select the NUKE Test target")
    if re.search(r"(?:^|\s)--target\s+(?:Pack|Push|Publish)(?:\s|$)", command, re.IGNORECASE):
        raise ValueError("Recorded invocation selects a publication target")
    log_text = log_path.read_text(encoding="utf-8", errors="replace")
    log_lines = log_text.splitlines()
    root = root.resolve()
    solution = root / "Elsa.sln"
    if not solution.is_file():
        raise ValueError(f"Prepared rehearsal is missing Elsa.sln: {root}")
    project_entries = parse_solution(solution)
    selected = {path: name for name, path in project_entries if name.endswith("Tests")}
    if not selected:
        raise ValueError("Canonical Elsa.sln has no selected projects ending in Tests")

    profile = _pin_profile(root, prep_path, import_path, patch_path, profile_template)
    commands: dict[str, dict[str, str]] = {}
    for line in log_lines:
        match = COMMAND_RE.search(line)
        if not match:
            continue
        project_path = Path(match["project"])
        try:
            relative = project_path.resolve().relative_to(root).as_posix()
        except ValueError as error:
            raise ValueError(f"NUKE test command project escapes prepared rehearsal: {project_path}") from error
        if relative not in selected:
            raise ValueError(f"NUKE invoked a test project outside canonical selection: {relative}")
        results = Path(match["results"])
        if results.resolve() != (root / "testresults").resolve():
            raise ValueError(f"NUKE test command results directory differs from prepared testresults: {results}")
        if match["configuration"] != "Debug":
            raise ValueError(f"Unexpected NUKE Test configuration: {match['configuration']}")
        if match["trx"] != Path(relative).stem + ".trx":
            raise ValueError(f"NUKE Test command TRX file does not match project: {relative}")
        if relative in commands:
            raise ValueError(f"NUKE issued duplicate Test commands for {relative}")
        commands[relative] = match.groupdict()
    if set(commands) != set(selected):
        missing = sorted(set(selected) - set(commands))
        raise ValueError(f"NUKE Test command inventory differs from canonical selection; missing={missing}")

    console_runs = []
    observed_status: dict[str, str] = {}
    for line in log_lines:
        match = SUMMARY_RE.search(line)
        if not match:
            continue
        assembly = match["assembly"]
        status = match["status"]
        observed_status[assembly] = status
        if status != "Passed!":
            continue
        console_runs.append({
            "assembly": assembly,
            "framework": match["framework"],
            "status": status,
            "passed": int(match["passed"]),
            "failed": int(match["failed"]),
            "skipped": int(match["skipped"]),
            "total": int(match["total"]),
        })
    if not console_runs:
        raise ValueError("NUKE log has no passing per-framework test summaries")
    test_names = set(selected.values())
    for run in console_runs:
        if Path(run["assembly"]).stem not in test_names:
            raise ValueError(f"Framework summary assembly is outside canonical test selection: {run['assembly']}")
        if run["failed"] or run["total"] != run["passed"] + run["failed"] + run["skipped"]:
            raise ValueError(f"Framework summary counters do not reconcile: {run['assembly']} ({run['framework']})")
    run_keys = [(run["assembly"], run["framework"]) for run in console_runs]
    if len(run_keys) != len(set(run_keys)):
        raise ValueError("NUKE log contains duplicate assembly/framework summaries")

    results_dir = root / "testresults"
    if not results_dir.is_dir():
        raise ValueError(f"Prepared rehearsal has no testresults directory: {results_dir}")
    trx_rows = _trx_rows(results_dir)
    expected_trx_names = {Path(path).stem + ".trx" for path in selected}
    actual_trx_names = {row["file"] for row in trx_rows}
    if actual_trx_names - expected_trx_names:
        raise ValueError(f"Retained TRX includes unselected project files: {sorted(actual_trx_names - expected_trx_names)}")
    for row in trx_rows:
        for code_base in row["codeBases"]:
            assembly_path = Path(code_base)
            try:
                relative = assembly_path.resolve().relative_to(root).as_posix()
            except ValueError as error:
                raise ValueError(f"TRX codeBase escapes prepared rehearsal: {code_base}") from error
            parts = PurePosixPath(relative).parts
            if "bin" not in parts or not parts[-1].endswith(".dll"):
                raise ValueError(f"TRX codeBase is not a compiled test assembly: {code_base}")
            project_name = Path(parts[-1]).stem
            if project_name not in test_names or project_name != Path(row["file"]).stem:
                raise ValueError(f"TRX codeBase does not match selected project {row['file']}: {code_base}")

    totals = Counter({"passed": 0, "failed": 0, "skipped": 0, "total": 0})
    for row in console_runs:
        totals.update({key: row[key] for key in totals})
    trx_totals = Counter({"passed": 0, "failed": 0, "executed": 0, "totalIncludingSkipped": 0})
    for row in trx_rows:
        counters = row["counters"]
        trx_totals.update({"passed": counters["passed"], "failed": counters["failed"], "executed": counters["executed"], "totalIncludingSkipped": counters["total"]})
    retained = {
        "files": len(trx_rows), "passed": trx_totals["passed"], "failed": trx_totals["failed"],
        "skipped": trx_totals["totalIncludingSkipped"] - trx_totals["executed"],
        "executed": trx_totals["executed"], "totalIncludingSkipped": trx_totals["totalIncludingSkipped"],
        "rows": trx_rows,
    }

    no_pass = []
    trx_by_file = {row["file"]: row for row in trx_rows}
    for relative, name in sorted(selected.items()):
        file_name = name + ".trx"
        if any(run["assembly"] == name + ".dll" for run in console_runs):
            continue
        row = trx_by_file.get(file_name)
        if row is None:
            status = observed_status.get(name + ".dll")
            if status:
                outcome = f"Selected; NUKE emitted a {status} framework summary, but no retained TRX was produced."
            else:
                outcome = "Selected by NUKE and built, but no test run or TRX was produced."
        elif row["counters"]["executed"] == 0:
            outcome = f"Selected; retained TRX records {row['counters']['total']} case(s) with zero executed tests; NUKE summary was {observed_status.get(name + '.dll', 'not emitted')}."
        else:
            raise ValueError(f"Selected project has executed TRX cases but no passing console summary: {name}")
        no_pass.append({"path": relative, "outcome": outcome})

    target_durations: dict[str, str] = {}
    for line in log_lines:
        match = TARGET_RE.match(line)
        if match:
            if match["target"] in target_durations:
                raise ValueError(f"NUKE log repeats successful target summary: {match['target']}")
            target_durations[match["target"]] = match["duration"]
    nuke_line = next((NUKE_TEST_RE.match(line) for line in log_lines if NUKE_TEST_RE.match(line)), None)
    build_complete = any(re.match(r"^\s*Build succeeded on ", line) for line in log_lines)
    build_failed = any(re.match(r"^\s*Build failed on ", line) for line in log_lines)
    if build_failed or not build_complete or set(target_durations) != {"Restore", "Compile", "Test"} or nuke_line is None:
        raise ValueError("NUKE run is incomplete or failed; successful Restore, Compile, and Test summaries are required")
    nuke_passed = int(nuke_line["passed"].replace(",", ""))
    nuke_skipped = int(nuke_line["skipped"].replace(",", ""))
    if any(status == "Failed!" for status in observed_status.values()) or any(row["failed"] for row in console_runs) or retained["failed"]:
        raise ValueError("NUKE run contains test failures")
    if (nuke_passed, nuke_skipped) != (retained["passed"], retained["skipped"]):
        raise ValueError("NUKE summary totals differ from retained TRX counters")
    warning_summaries = [int(match.group(1)) for line in log_lines
                         if (match := re.search(r"\[DBG\]\s+(\d+) Warning\(s\)$", line))]
    if len(warning_summaries) > 1:
        raise ValueError("NUKE log repeats the compile warning summary")
    compile_warnings = warning_summaries[0] if warning_summaries else sum(
        1 for line in log_lines if "[WRN] Compile:" in line
    )
    compile_errors = sum(1 for line in log_lines if "[ERR] Compile:" in line)
    if compile_errors:
        raise ValueError(f"NUKE compile emitted {compile_errors} error line(s)")
    forbidden_targets = {name: False for name in ("pack", "push", "publish")}
    if any(re.match(r"^║\s*(?:Pack|Push|Publish)\s*$", line, re.IGNORECASE) or
           re.search(r"\b(?:Pack|Push|Publish)\s+(?:Succeeded|Failed)\b", line) or
           re.search(r"\[INF\] > .*\bdotnet\s+(?:pack|nuget\s+push)\b", line, re.IGNORECASE)
           for line in log_lines):
        raise ValueError("NUKE log shows a package or publication target ran")
    total_duration = sum(_duration_seconds(value) for value in target_durations.values())
    duration = lambda seconds: f"{seconds // 60:02d}:{seconds % 60:02d}"

    return {
        "schemaVersion": 1,
        "profile": profile,
        "invocation": {
            "command": command,
            "workingDirectory": str(root),
            "logPath": str(log_path.resolve()),
            "logSha256": sha256(log_path),
            "exitCode": 0,
            "targets": {"restore": "succeeded", "compile": "succeeded", "test": "succeeded", **forbidden_targets},
            "duration": {"restore": target_durations["Restore"], "compile": target_durations["Compile"], "test": target_durations["Test"], "total": duration(total_duration)},
            "compile": {"errors": compile_errors, "warnings": compile_warnings},
            "nukeSummary": {"passed": nuke_passed, "skipped": nuke_skipped, "failed": 0},
        },
        "selection": {
            "canonicalSolutionProjectEntries": len(project_entries),
            "nukeSelectedTestProjects": len(selected),
            "commandsObserved": len(commands),
            "uniqueCommandProjectPaths": len(set(commands)),
            "projects": [{"name": selected[path], "path": path} for path in commands],
        },
        "observedTestResults": {
            "retainedTrx": retained,
            "frameworkConsoleSummaries": {
                "assemblyFrameworkRuns": len(console_runs),
                "totalsFromPassedSummaries": dict(totals),
                "runs": console_runs,
            },
            "selectedProjectsWithoutPassingTestCases": no_pass,
        },
    }


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--log", type=Path, required=True, help="Completed NUKE build log")
    parser.add_argument("--rehearsal", type=Path, required=True, help="Prepared rehearsal containing Elsa.sln and testresults")
    parser.add_argument("--preparation-receipt", type=Path, required=True)
    parser.add_argument("--import-receipt", type=Path, required=True)
    parser.add_argument("--source-integration-patch", type=Path, required=True)
    parser.add_argument("--profile-template", type=Path, help="Optional accepted evidence JSON supplying supplemental patch pins")
    parser.add_argument("--command", required=True, help="Exact build invocation recorded in the evidence")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args(argv)
    try:
        evidence = extract(args.log, args.rehearsal, args.preparation_receipt, args.import_receipt,
                           args.source_integration_patch, args.profile_template, args.command)
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
    except (OSError, ValueError, KeyError, ET.ParseError) as error:
        print(f"error: {error}", file=sys.stderr)
        return 2
    print(f"Wrote canonical NUKE Test evidence to {args.output}")
    print(f"Selected projects: {evidence['selection']['nukeSelectedTestProjects']}; retained TRX: {evidence['observedTestResults']['retainedTrx']['files']}; framework summaries: {evidence['observedTestResults']['frameworkConsoleSummaries']['assemblyFrameworkRuns']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
