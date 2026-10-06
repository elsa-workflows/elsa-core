#!/usr/bin/env python3
"""Complete coverage ledger and private process transport for real package browsers."""
from __future__ import annotations

from dataclasses import asdict, is_dataclass
import hashlib
import json
import os
import re
from pathlib import Path
import subprocess
from typing import Callable

JOURNEY = Path(__file__).resolve().parents[2] / "test/studio/browser/PackageCompatibility/journey.ts"
POLICY = json.loads(JOURNEY.with_name("coverage-policy.json").read_text(encoding="utf-8"))
HOSTS, FRAMEWORKS, VERSIONS = (tuple(POLICY[key]) for key in ("hosts", "frameworks", "versions"))
MATRIX = {(version, framework, host) for version in VERSIONS for framework in FRAMEWORKS for host in HOSTS}
BASELINE_ASSERTIONS = set(POLICY["baseline"])
CANDIDATE_ASSERTIONS = BASELINE_ASSERTIONS | set(POLICY["candidate"])
HOST_ASSERTIONS = {host: set(names) for host, names in POLICY["host_assertions"].items()}


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def identity(record: dict) -> tuple[str, str, str]:
    key = (record["version"], record["framework"], record["host"])
    require(key in MATRIX, "Unknown package browser cell")
    return key


def required_assertions(record: dict) -> set[str]:
    return (CANDIDATE_ASSERTIONS if record["version"] == "3.10.0" else BASELINE_ASSERTIONS) | HOST_ASSERTIONS[record["host"]]


def new_ledger() -> dict:
    return {"schema": 1, "complete_matrix": False, "passed": False, "cells": [
        {"version": version, "framework": framework, "host": host, "result": "not_run", "assertions": []}
        for version, framework, host in sorted(MATRIX)]}


def check_cell(record: dict) -> None:
    identity(record)
    assertions = record.get("assertions", [])
    names = [item["name"] for item in assertions]
    require(len(names) == len(set(names)) and set(names) == required_assertions(record), "Missing, extra or duplicated browser assertion")
    require(all(item.get("passed") is True for item in assertions), "Required browser assertion failed")
    require(record.get("result") == "passed", "Browser cell did not pass")


def check_matrix(ledger: dict) -> None:
    cells = ledger.get("cells", [])
    keys = [identity(cell) for cell in cells]
    require(len(keys) == 36 and len(set(keys)) == 36 and set(keys) == MATRIX, "All 36 unique package browser cells are required")
    for cell in cells:
        check_cell(cell)


def validate_browser_receipt(record: dict, key: tuple[str, str, str]) -> dict:
    """Allow only bounded, sanitized fields from the private child process."""
    allowed = {"host", "framework", "version", "result", "assertions", "resources", "proof", "browser_version", "failure_category"}
    require(isinstance(record, dict) and set(record) <= allowed and all(name in record for name in ("host", "framework", "version")) and identity(record) == key, "Invalid or unsafe browser receipt")
    require(record.get("result") in {"passed", "failed", "incomplete"}, "Invalid browser result")
    require(record.get("failure_category") in (None, "browser_execution_or_validation_failed"), "Unsafe browser failure category")
    require(re.fullmatch(r"[0-9]+(?:\.[0-9]+){1,3}", record.get("browser_version", "")), "Invalid browser version")
    assertions = record.get("assertions", [])
    names = [item.get("name") for item in assertions]
    require(len(names) == len(set(names)) and set(names) == required_assertions(record), "Missing or duplicated browser assertions")
    for item in assertions:
        require(set(item) <= {"name", "passed", "reason_category"} and type(item.get("passed")) is bool and item.get("reason_category") in (None, "not_implemented"), "Unsafe browser assertion")
    proof = record.get("proof", {})
    stages = {"backend_authenticated", "login_navigation", "login_form", "login_submitted", "workflow_list", "create_dialog_opened", "create_name_filled", "create_submitted", "workflow_created", "output_declared", "activity_registry", "activity_inserted", "property_saved", "edit_reloaded", "workflow_run"}
    hashes = {"definition_id_sha256", "activity_id_sha256", "value_sha256", "synthetic_document_sha256", "instance_id_sha256"}
    flags = {"login_failure_visible", "login_form_visible", "server_circuit_observed", "server_render_frames_observed", "elsa_identity_ui_visible", "expected_auth_provider_observed"}
    counts = {"create_name_label_count", "create_name_textbox_count"}
    require(set(proof) <= hashes | flags | counts | {"last_completed_stage"}, "Unsafe browser proof field")
    for name, value in proof.items():
        if name in counts:
            valid = type(value) is int and 0 <= value <= 100
        elif name in flags:
            valid = type(value) is bool
        elif name == "last_completed_stage":
            valid = value in stages
        else:
            valid = isinstance(value, str) and re.fullmatch("[0-9a-f]{64}", value)
        require(valid, "Unsafe browser proof value")
    for resource in record.get("resources", []):
        require(set(resource) == {"path", "status", "content_type", "sha256", "bytes", "owner", "requested"}, "Unsafe resource receipt fields")
        require(isinstance(resource["path"], str) and re.fullmatch(r"/[A-Za-z0-9_./-]+", resource["path"]) and ".." not in resource["path"].split("/"), "Unsafe resource receipt path")
        require(resource["owner"] in ("package", "fixture", "platform") and resource["requested"] is True and type(resource["status"]) is int and 100 <= resource["status"] <= 599 and type(resource["bytes"]) is int and 0 <= resource["bytes"] <= 32 * 1024 * 1024, "Unsafe resource receipt metadata")
        require(isinstance(resource["sha256"], str) and re.fullmatch("[0-9a-f]{64}", resource["sha256"]) and resource["content_type"] in set(POLICY["resource_content_types"].values()) | {"application/javascript"}, "Unsafe resource receipt content metadata")
    return record


def run_matrix(execute: Callable[[tuple[str, str, str]], dict], selected: list[tuple[str, str, str]], output: Path) -> dict:
    """Caller owns verified materialization and start_pair/stop_pair context lifetime."""
    output = _external_path(output)
    require(not output.exists(), "Refusing to overwrite browser evidence")
    require(len(selected) == len(set(selected)) and set(selected) <= MATRIX, "Invalid selected matrix cells")
    ledger = new_ledger()
    ledger["development_only"] = set(selected) != MATRIX
    records = {identity(cell): cell for cell in ledger["cells"]}
    try:
        for key in selected:
            try:
                record = execute(key)
                record = validate_browser_receipt(record, key)
                records[key].update(record)
                if record["result"] == "passed":
                    check_cell(records[key])
                require(record["result"] != "failed", "Browser cell failed")
            except Exception:
                records[key]["result"] = "failed"
                records[key]["failure_category"] = "execution_or_validation_failed"
                raise ValueError("Browser cell execution or validation failed") from None
        if set(selected) == MATRIX:
            check_matrix(ledger)
            ledger.update(complete_matrix=True, passed=True)
    finally:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(json.dumps(ledger, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return ledger


def _external_path(path: Path) -> Path:
    raw = path.absolute()
    require(".." not in raw.parts and not raw.is_symlink(), "Symlinked or escaping browser evidence root")
    # macOS system aliases are fixed filesystem roots; arbitrary ancestor links are rejected.
    aliases = {Path("/tmp"): Path("/private/tmp"), Path("/var"): Path("/private/var")}
    for component in raw.parents:
        require(not component.is_symlink() or aliases.get(component) == component.resolve(), "Symlinked browser evidence ancestor")
    result = raw.resolve()
    require(not result.is_relative_to(JOURNEY.parents[4].resolve()), "Browser evidence must be outside the source tree")
    return result


def prepare_candidate(inputs: Path, destination: Path, retained: Path, *, fixture_source: str,
                      fixture_run: int | None = None, fixture_attempt: int | None = None) -> dict:
    """Bind original immutable producer bytes separately from this browser execution."""
    import consolidated_candidate_input as candidate
    import prove_consolidated_package_consumers as packages
    require(re.fullmatch("[0-9a-f]{40}", fixture_source), "Invalid browser execution source")
    require((fixture_run is None and fixture_attempt is None) or
            (type(fixture_run) is int and fixture_run > 0 and type(fixture_attempt) is int and fixture_attempt > 0), "Invalid browser execution run")
    checkout = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=JOURNEY.parents[4], text=True).strip()
    require(fixture_source == checkout, "Browser execution source does not match checkout")
    require(not subprocess.check_output(["git", "status", "--porcelain", "--untracked-files=all"], cwd=JOURNEY.parents[4], text=True).strip(), "Acceptance requires a clean tracked fixture checkout")
    candidate.validate_transport(inputs)  # Reject raw symlink/layout before resolving macOS aliases.
    inputs, destination, retained = _external_path(inputs), _external_path(destination), _external_path(retained)
    require(all(not left.is_relative_to(right) and not right.is_relative_to(left) for left, right in ((inputs, destination), (inputs, retained), (destination, retained))), "Browser input/evidence roots overlap")
    require(not retained.exists(), "Refusing to overwrite retained browser inputs")
    provenance = candidate.verify_candidate_inputs(inputs / "candidate.zip", inputs / "artifact.json",
                                                   inputs / "producer-run.json", inputs / "live-retrieval.json", destination)
    manifest = json.loads((destination / "verified-artifacts.json").read_text(encoding="utf-8"))
    version, source, _, _, _ = packages._validated_manifest(manifest)
    require(version == candidate.PRODUCER["version"] and source == candidate.SOURCE, "Browser candidate manifest identity differs")
    # Capture all tracked fixture/helper inputs, including host glue added by the host owner.
    tracked = subprocess.check_output(["git", "ls-files", "--", "scripts/integration-program", "test/studio/browser/PackageCompatibility"], cwd=JOURNEY.parents[4], text=True).splitlines()
    provenance["verified_artifacts_sha256"] = hashlib.sha256((destination / "verified-artifacts.json").read_bytes()).hexdigest()
    files = [JOURNEY.parents[4] / name for name in tracked]
    require(all(path.is_file() and not path.is_symlink() and path.resolve().is_relative_to(JOURNEY.parents[4].resolve()) for path in files), "Untracked or symlinked fixture input")
    require(JOURNEY in files and Path(__file__).resolve() in files, "Browser entrypoints must be tracked")
    provenance["browser_execution"] = {"fixture_source_commit": fixture_source, "run_id": fixture_run,
                                       "run_attempt": fixture_attempt, "fixture_files_sha256": {
        str(path.relative_to(JOURNEY.parents[4])): hashlib.sha256(path.read_bytes()).hexdigest() for path in files}}
    retained.mkdir(parents=True, mode=0o700)
    for name, original in (("original-envelope.json", candidate.ORIGINAL_ENVELOPE), ("live-retrieval.json", inputs / "live-retrieval.json")):
        (retained / name).write_bytes(original.read_bytes())
    (retained / "provenance.json").write_text(json.dumps(provenance, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return provenance


def run_browser(handle, request, resources: list[dict], *, timeout: int = 240) -> dict:
    """Credentials only enter the child through stdin; raw process errors are discarded."""
    cell = asdict(request) if is_dataclass(request) else dict(request)
    identity(cell)
    payload = {"request": cell, "studio_url": handle.studio_url, "backend_url": handle.backend_url,
               "username": handle.username, "password": handle.password,
               "safe_ids": handle.safe_ids, "resources": resources}
    try:
        completed = subprocess.run(["npm", "exec", "--no", "--", "tsx", str(JOURNEY)],
                                   cwd=JOURNEY.parent, input=json.dumps(payload), capture_output=True, text=True, timeout=timeout,
                                   env={key: value for key, value in os.environ.items() if key in {"PATH", "HOME", "TMPDIR", "SystemRoot"}})
        record = json.loads(completed.stdout)
    except (OSError, subprocess.TimeoutExpired, json.JSONDecodeError):
        raise ValueError("Browser process failed to return a safe receipt") from None
    require(isinstance(record, dict) and all(key in record for key in ("host", "framework", "version")) and identity(record) == identity(cell), "Browser process returned an invalid cell")
    record = validate_browser_receipt(record, identity(cell))
    require(completed.returncode == (1 if record["result"] == "failed" else 0), "Inconsistent browser exit status")
    return record
