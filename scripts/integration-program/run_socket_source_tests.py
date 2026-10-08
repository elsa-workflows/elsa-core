#!/usr/bin/env python3
"""Run allowlisted Socket source-test cells; retain aggregate metadata, never raw logs.

A PostgreSQL source cell exercises the real fixture selected by its filter. This is not
package-only or full Socket acceptance, and this runner does not attest fixture cleanup.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess

from run_admission_proof import diagnostic_codes, regular
from run_current_import_affected_tests import assert_clean_source
from run_execution_cycle_proof import digest, execute, test_summary, tracked_input

ROOT = Path(__file__).resolve().parents[2]
RUNNER = "scripts/integration-program/run_socket_source_tests.py"
WORKFLOW = ".github/workflows/socket-mode-source-tests.yml"
SLACK = "test/extensions/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj"
POSTGRES = ("test/integration/Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests/"
            "Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests.csproj")
SUITES = {
    "slack-unit-loopback": (SLACK, None, ("net8.0", "net9.0", "net10.0"), "Elsa.Slack.Tests."),
    "postgres-socket": (POSTGRES, "FullyQualifiedName~Socket", ("net10.0",),
                        "Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests."),
}
HEAD = re.compile(r"[0-9a-f]{40}\Z")
NUMBER = re.compile(r"[1-9][0-9]{0,19}\Z")
CATEGORIES = {"source_validation_failed", "restore_failed", "restored_framework_mismatch",
              "test_failed", "trx_invalid", "source_postcheck_failed", "source_test_setup_failed", "receipt_write_failed"}


class SourceTestError(ValueError):
    pass


def require(condition: bool, category: str) -> None:
    if not condition:
        raise SourceTestError(category)


def selection(suite: str, framework: str) -> tuple[str, str | None, str]:
    require(suite in SUITES, "source_test_setup_failed")
    project, test_filter, frameworks, namespace = SUITES[suite]
    require(framework in frameworks, "source_test_setup_failed")
    return project, test_filter, namespace


def results(trx: Path, suite: str, namespace: str) -> dict:
    regular(trx)
    summary = test_summary(trx, 0)
    require(summary["status"] == "passed" and bool(summary["cases"]), "trx_invalid")
    require(summary["counters"].get("notExecuted", 0) == 0, "trx_invalid")
    identities = {(row["method"], row["caseSha256"]) for row in summary["cases"]}
    require(len(identities) == len(summary["cases"]), "trx_invalid")
    require(all(method.startswith(namespace) and (suite != "postgres-socket" or "Socket" in method)
                for method, _ in identities), "trx_invalid")
    # Names may contain private synthetic data. Retain their digest, never display names/messages.
    identity_hash = hashlib.sha256(json.dumps(sorted(identities), separators=(",", ":")).encode()).hexdigest()
    return {"passed": len(identities), "failed": 0, "skipped": 0,
            "privateTrxSha256": summary["trxSha256"], "privateTrxBytes": trx.stat().st_size,
            "caseIdentitiesSha256": identity_hash}


def retain(private: Path, retained: Path, name: str, data: dict) -> None:
    # Both paths are in one fresh evidence tree. A closed sanitized private file plus
    # no-overwrite link is the commit point; no fallible cleanup follows publication.
    pending = private / (name + ".pending")
    with pending.open("x", encoding="utf-8") as stream:
        stream.write(json.dumps(data, indent=2) + "\n")
    os.link(pending, retained / name)


def run(root: Path, output: Path, head: str, suite: str, framework: str,
        run_id: str, run_attempt: str) -> bool:
    # Validate user-selected identities before any child/network-capable command.
    project, test_filter, namespace = selection(suite, framework)
    require(type(head) is str and HEAD.fullmatch(head) is not None and
            all(type(value) is str and NUMBER.fullmatch(value) is not None for value in (run_id, run_attempt)),
            "source_test_setup_failed")
    require(not any(part.is_symlink() for part in (output, *output.parents)), "source_test_setup_failed")
    root, output = root.resolve(), output.resolve()
    require(output != root and root not in output.parents and
            (not output.exists() or output.is_dir() and not any(output.iterdir())), "source_test_setup_failed")
    output.mkdir(parents=True, exist_ok=True)
    private, retained = output / "private", output / "retained"
    private.mkdir(mode=0o700)
    retained.mkdir()
    identity = {"schemaVersion": 1, "sourceRevision": head, "suite": suite, "project": project,
                "filter": test_filter, "framework": framework, "runId": run_id, "runAttempt": run_attempt}
    stage = "source_validation_failed"
    exit_code = None
    log = None
    try:
        assert_clean_source(root, head)
        source_tree = subprocess.check_output(["git", "-C", str(root), "rev-parse", "HEAD^{tree}"],
                                              text=True, stderr=subprocess.DEVNULL).strip()
        require(HEAD.fullmatch(source_tree) is not None, "source_validation_failed")
        hashes = {path: digest(tracked_input(root, path)) for path in (project, RUNNER, WORKFLOW)}
        # Pin BOTH properties during restore and test; inherited test props otherwise select net10.
        properties = [f"-p:TargetFramework={framework}", f"-p:TargetFrameworks={framework}",
                      "-m:1", "-p:UseProjectReferences=true", "-p:IsPackable=false",
                      "-p:GeneratePackageOnBuild=false", "-p:CollectCoverage=false"]
        commands = [("restore", ["dotnet", "restore", project, *properties]),
                    ("test", ["dotnet", "test", project, "--no-restore", "--configuration", "Release",
                              "--framework", framework, *properties, "--logger", "trx;LogFileName=socket-source.trx",
                              "--results-directory", str(private), *(["--filter", test_filter] if test_filter else [])])]
        command_receipts = []
        for name, command in commands:
            stage, log = f"{name}_failed", private / f"{name}.log"
            execution = execute(command, root, log, timeout=1200)
            exit_code = execution["exitCode"]
            require(exit_code == 0, stage)
            command_receipts.append({"stage": name, "exitCode": 0, "logSha256": execution["logSha256"]})
            if name == "restore":
                stage = "restored_framework_mismatch"
                assets = regular(root / project).with_name("obj") / "project.assets.json"
                regular(assets)
                data = json.loads(assets.read_text(encoding="utf-8"))
                require(set(data.get("targets", {})) == {framework} and
                        set(data.get("project", {}).get("frameworks", {})) == {framework}, stage)
        stage, log, exit_code = "trx_invalid", None, None
        require({path.name for path in private.glob("*.trx")} == {"socket-source.trx"}, stage)
        counts = results(private / "socket-source.trx", suite, namespace)
        stage = "source_postcheck_failed"
        assert_clean_source(root, head)
        require(hashes == {path: digest(tracked_input(root, path)) for path in hashes}, stage)
        receipt = {**identity, "kind": "socket-source-test-aggregate", "sourceTree": source_tree,
                   "inputSha256": hashes, "workingTreeCleanBefore": True, "workingTreeCleanAfter": True,
                   "commands": command_receipts, **counts, "packageOnlyProof": False,
                   "postgresqlProof": False, "postgresqlSourceTests": suite == "postgres-socket",
                   "fixtureCleanupVerified": False, "fullSocketAcceptance": False}
        stage = "receipt_write_failed"
        retain(private, retained, "receipt.json", receipt)
        return True
    except Exception:
        # Exception text, test names and raw diagnostics may contain private credentials.
        category = stage if stage in CATEGORIES else "source_test_setup_failed"
        failure = {**identity, "kind": "socket-source-test-failure", "category": category,
                   "exitCode": exit_code if type(exit_code) is int else None,
                   "diagnosticCodes": diagnostic_codes(log) if log is not None else []}
        retain(private, retained, "failure.json", failure)
        return False


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--suite", choices=tuple(SUITES), required=True)
    parser.add_argument("--framework", choices=("net8.0", "net9.0", "net10.0"), required=True)
    parser.add_argument("--expected-head", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--run-id", required=True)
    parser.add_argument("--run-attempt", required=True)
    args = parser.parse_args()
    try:
        passed = run(ROOT, args.output, args.expected_head, args.suite, args.framework, args.run_id, args.run_attempt)
    except Exception:
        print("Socket source-test setup failed; private diagnostics are not published.")
        return 1
    print("Socket source-test aggregate passed." if passed else "Socket source-test stage failed; raw diagnostics remain private.")
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
