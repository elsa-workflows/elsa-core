#!/usr/bin/env python3
"""Run the execution-cycle correctness closure; publish metadata, never raw diagnostics."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import signal
import subprocess
import time
import xml.etree.ElementTree as ET

from package_closure import parse_trx
from run_current_import_affected_tests import assert_clean_source, classify


ROOT = Path(__file__).resolve().parents[2]
FRAMEWORKS = ("net8.0", "net9.0", "net10.0")
BUILD_PROJECTS = tuple(f"core/src/modules/{name}/{name}.csproj" for name in (
    "Elsa.Workflows.Core", "Elsa.Workflows.Runtime", "Elsa.Alterations")) + (
    'extensions/src/runtimes/Elsa.Workflows.Runtime.ProtoActor/Elsa.Workflows.Runtime.ProtoActor.csproj',
)
TEST_PROJECTS = (
    ('core/test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj', None),
    ('core/test/unit/Elsa.Workflows.Runtime.UnitTests/Elsa.Workflows.Runtime.UnitTests.csproj', None),
    ('core/test/integration/Elsa.Workflows.IntegrationTests/Elsa.Workflows.IntegrationTests.csproj',
     "FullyQualifiedName~GracefulShutdown|FullyQualifiedName~DefaultActivityCommitStrategy|FullyQualifiedName~DefaultWorkflowCommitStrategy"),
    ('core/test/integration/Elsa.Alterations.IntegrationTests/Elsa.Alterations.IntegrationTests.csproj', None),
    ('extensions/test/modules/runtimes/Elsa.Workflows.Runtime.ProtoActor.UnitTests/Elsa.Workflows.Runtime.ProtoActor.UnitTests.csproj', None),
)
IDENTIFIER = re.compile(r"[A-Za-z_][A-Za-z0-9_.+`]*\Z")


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def tracked_input(root: Path, relative: str) -> Path:
    path = root / relative
    if Path(relative).is_absolute() or ".." in Path(relative).parts:
        raise ValueError("Input must be a repository-relative path")
    if any(item.is_symlink() for item in (path, *path.parents)) or not path.is_file():
        raise ValueError("Input must be a regular file without symlink ancestors")
    subprocess.run(["git", "-C", str(root), "ls-files", "--error-unmatch", "--", relative],
                   check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    return path


def test_summary(trx: Path, exit_code: int) -> dict:
    counters, results = parse_trx(trx)
    root = ET.parse(trx).getroot()
    methods = {}
    for test in root.findall(".//{*}UnitTest"):
        method = test.find("{*}TestMethod")
        if method is None:
            raise ValueError("Missing test method metadata")
        name = f"{method.get('className', '')}.{method.get('name', '')}"
        if len(name) > 512 or not IDENTIFIER.fullmatch(name):
            raise ValueError("Invalid test method metadata")
        test_id = test.get("id")
        if not test_id or test_id in methods:
            raise ValueError("Missing or duplicate test definition")
        methods[test_id] = name
    cases = []
    for result in root.findall(".//{*}UnitTestResult"):
        method = methods.get(result.get("testId"))
        if method is None:
            raise ValueError("Result has no test definition")
        cases.append({"method": method, "outcome": result.get("outcome"),
                      "caseSha256": hashlib.sha256(result.get("testName", "").encode()).hexdigest()})
    if len(cases) != len(results):
        raise ValueError("Result metadata is incomplete")
    return {"status": classify(exit_code, counters, results), "counters": counters,
            "cases": cases, "trxSha256": digest(trx)}


def execute(command: list[str], root: Path, log: Path, timeout: int = 900) -> dict:
    started = time.monotonic()
    with log.open("wb") as output:
        process = subprocess.Popen(command, cwd=root, stdout=output, stderr=subprocess.STDOUT,
                                   start_new_session=True)
        try:
            exit_code = process.wait(timeout=timeout)
        except subprocess.TimeoutExpired:
            os.killpg(process.pid, signal.SIGKILL)
            process.wait()
            exit_code = 124
    # Compiler diagnostics retain only a repository-relative location and error code.
    diagnostics = []
    pattern = re.compile(r"^(.*?)(\(\d+,\d+\)): error ([A-Z]+\d+):")
    for line in log.read_text(errors="replace").splitlines():
        match = pattern.match(line)
        if match:
            try:
                relative = Path(match[1]).relative_to(root).as_posix()
            except ValueError:
                continue
            if re.fullmatch(r"[A-Za-z0-9_./-]+", relative):
                diagnostic = {"file": relative, "location": match[2], "code": match[3]}
                if diagnostic not in diagnostics:
                    diagnostics.append(diagnostic)
    return {"exitCode": exit_code, "durationSeconds": round(time.monotonic() - started, 2),
            "logSha256": digest(log), "compilerDiagnostics": diagnostics,
            "status": "passed" if exit_code == 0 else "failed"}


def run(root: Path, output: Path, head: str) -> dict:
    root, output = root.resolve(), output.resolve()
    if output == root or root in output.parents or (output.exists() and any(output.iterdir())):
        raise ValueError("Evidence requires a fresh directory outside the checkout")
    assert_clean_source(root, head)
    inputs = [*BUILD_PROJECTS, *(project for project, _ in TEST_PROJECTS),
              "scripts/integration-program/run_execution_cycle_proof.py",
              "scripts/integration-program/run_current_import_affected_tests.py",
              "scripts/integration-program/package_closure.py"]
    hashes = {path: digest(tracked_input(root, path)) for path in inputs}
    output.mkdir(parents=True, exist_ok=True)
    receipt = {"schemaVersion": 1, "sourceRevision": head, "inputSha256": hashes,
               "publicationPerformed": False, "verificationComplete": False,
               "testBuilds": [], "builds": [], "tests": []}

    def save() -> None:
        (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")

    save()
    properties = ["-m:1", "-p:UseProjectReferences=true", "-p:IsPackable=false",
                  "-p:GeneratePackageOnBuild=false", "-p:CollectCoverage=false"]
    # Compile each affected fixture once before execution. Collect all fixture errors in one pass;
    # none of the tests or broader framework builds may run if a fixture does not compile.
    for index, (project, _) in enumerate(TEST_PROJECTS):
        row = {"project": project, "framework": "net10.0"}
        row.update(execute(["dotnet", "build", project, "-c", "Release", "-f", "net10.0",
                            *properties], root, output / f"test-build-{index}.log"))
        receipt["testBuilds"].append(row)
        save()
        print(f"Compile fixture {project}: {row['status']}", flush=True)
    if any(row["status"] != "passed" for row in receipt["testBuilds"]):
        return receipt
    for index, (project, test_filter) in enumerate(TEST_PROJECTS):
        trx = output / f"tests-{index}.trx"
        command = ["dotnet", "test", project, "-c", "Release", "-f", "net10.0", *properties,
                   "--no-build", "--no-restore",
                   "--logger", f"trx;LogFileName={trx.name}", "--results-directory", str(output)]
        if test_filter:
            command.extend(["--filter", test_filter])
        row = {"project": project, "framework": "net10.0", "filter": test_filter}
        row.update(execute(command, root, output / f"tests-{index}.log"))
        try:
            row.update(test_summary(trx, row["exitCode"]))
        except (OSError, ValueError, KeyError, ET.ParseError):
            row.update(status="failed", evidenceError="Missing or invalid test result metadata")
        receipt["tests"].append(row)
        save()
        print(f"Test {project}: {row['status']}", flush=True)
        if row["status"] != "passed":
            return receipt
    # Serial on one hosted runner: shared output directories never race.
    for framework in FRAMEWORKS:
        for project in BUILD_PROJECTS:
            row = {"project": project, "framework": framework}
            row.update(execute(["dotnet", "build", project, "-c", "Release", "-f", framework,
                                *properties], root, output / f"build-{len(receipt['builds'])}.log"))
            receipt["builds"].append(row)
            save()
            print(f"Build {project} {framework}: {row['status']}", flush=True)
            if row["status"] != "passed":
                return receipt
    assert_clean_source(root, head)
    if hashes != {path: digest(tracked_input(root, path)) for path in inputs}:
        raise ValueError("Verification inputs changed")
    receipt["verificationComplete"] = True
    save()
    return receipt


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--expected-head", required=True)
    args = parser.parse_args()
    try:
        result = run(args.root, args.output, args.expected_head)
        raise SystemExit(0 if result["verificationComplete"] else 1)
    except (OSError, ValueError, subprocess.SubprocessError):
        print("Execution-cycle verification failed during preflight or evidence validation.")
        raise SystemExit(1)
