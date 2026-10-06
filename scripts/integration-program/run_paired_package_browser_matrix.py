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
BPMN_INPUT_PATH = JOURNEY.with_name("paired-browser.bpmn")
BPMN_INPUT_SHA256 = "b450b6fc6bee2a693b5dd6a4af60f02a220f1b89c9d5f2d35bf7fecc1289a1da"
BPMN_INPUT_BYTES = 679
BPMN_SEMANTIC_IDENTITY = (
    "Definitions_paired_browser|paired-process|paired-start|paired-end|paired-flow|paired-start>paired-end"
)
BPMN_SEMANTIC_SHA256 = hashlib.sha256(BPMN_SEMANTIC_IDENTITY.encode("utf-8")).hexdigest()
BPMN_ID_SHA256 = {
    name: hashlib.sha256(value.encode("utf-8")).hexdigest()
    for name, value in {
        "process_id_sha256": "paired-process",
        "start_id_sha256": "paired-start",
        "end_id_sha256": "paired-end",
        "flow_id_sha256": "paired-flow",
    }.items()
}
BPMN_CHECKS = {"imported", "rendered", "selection_callback", "exported", "reimported", "semantic_preserved"}


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
    key = identity(record)
    validate_browser_receipt(record, key)
    assertions = record.get("assertions", [])
    names = [item["name"] for item in assertions]
    require(len(names) == len(set(names)) and set(names) == required_assertions(record), "Missing, extra or duplicated browser assertion")
    require(all(item.get("passed") is True for item in assertions), "Required browser assertion failed")
    require(record.get("result") == "passed", "Browser cell did not pass")
    if key[0] == "3.10.0":
        _require_reopen_completion(record, key)


def check_matrix(ledger: dict) -> None:
    cells = ledger.get("cells", [])
    keys = [identity(cell) for cell in cells]
    require(len(keys) == 36 and len(set(keys)) == 36 and set(keys) == MATRIX, "All 36 unique package browser cells are required")
    for cell in cells:
        check_cell(cell)


def _require_sha256(value: object, message: str = "Unsafe browser proof hash") -> None:
    require(isinstance(value, str) and re.fullmatch(r"[0-9a-f]{64}", value) is not None, message)


def _validate_bpmn_source() -> None:
    try:
        info = BPMN_INPUT_PATH.lstat()
        require(not BPMN_INPUT_PATH.is_symlink() and info.st_mode & 0o170000 == 0o100000,
                "Tracked BPMN input is not a regular file")
        raw = BPMN_INPUT_PATH.read_bytes()
    except OSError:
        raise ValueError("Tracked BPMN input is unavailable") from None
    require(len(raw) == BPMN_INPUT_BYTES and hashlib.sha256(raw).hexdigest() == BPMN_INPUT_SHA256,
            "Tracked BPMN input differs from the reviewed fixture")


def _validate_bpmn_roundtrip(value: object, assertion_passed: bool) -> None:
    required = {
        "input_xml_sha256", "input_xml_bytes", "semantic_sha256", "process_id_sha256",
        "start_id_sha256", "end_id_sha256", "flow_id_sha256", "checks",
    }
    optional = {"first_definition_id_sha256", "export_xml_sha256", "export_xml_bytes", "second_definition_id_sha256"}
    require(isinstance(value, dict) and required <= set(value) <= required | optional,
            "Unsafe BPMN roundtrip proof fields")
    _validate_bpmn_source()
    require(value["input_xml_sha256"] == BPMN_INPUT_SHA256 and value["input_xml_bytes"] == BPMN_INPUT_BYTES,
            "BPMN proof is not bound to the tracked input")
    require(type(value["input_xml_bytes"]) is int and 0 < value["input_xml_bytes"] <= 1024 * 1024,
            "Invalid BPMN input size")
    require(value["semantic_sha256"] == BPMN_SEMANTIC_SHA256, "BPMN semantic identity differs from the reviewed model")
    for name, expected in BPMN_ID_SHA256.items():
        require(value[name] == expected, "BPMN model identity differs from the reviewed model")
    for name in ("input_xml_sha256", "semantic_sha256", *BPMN_ID_SHA256):
        _require_sha256(value[name])

    checks = value["checks"]
    require(isinstance(checks, dict) and set(checks) == BPMN_CHECKS and
            all(type(flag) is bool for flag in checks.values()), "Invalid BPMN roundtrip checks")
    require(not checks["rendered"] or checks["imported"], "BPMN render proof precedes import")
    require(not checks["selection_callback"] or checks["rendered"], "BPMN selection proof precedes render")
    require(not checks["exported"] or checks["selection_callback"], "BPMN export proof precedes native selection")
    require(not checks["reimported"] or checks["exported"], "BPMN reimport proof precedes export")
    require(not checks["semantic_preserved"] or all(checks[name] for name in BPMN_CHECKS - {"semantic_preserved"}),
            "BPMN semantic proof precedes a completed roundtrip")

    has_first = "first_definition_id_sha256" in value
    require(has_first == checks["imported"], "BPMN first definition identity does not match import observation")
    if has_first:
        _require_sha256(value["first_definition_id_sha256"])
    has_export_hash = "export_xml_sha256" in value
    has_export_size = "export_xml_bytes" in value
    require(has_export_hash == checks["exported"] and has_export_size == checks["exported"],
            "BPMN export evidence does not match the export observation")
    if checks["exported"]:
        _require_sha256(value["export_xml_sha256"])
        require(type(value["export_xml_bytes"]) is int and 0 < value["export_xml_bytes"] <= 1024 * 1024,
                "Invalid BPMN export size")
    has_second = "second_definition_id_sha256" in value
    require(has_second == checks["reimported"], "BPMN second definition identity does not match reimport observation")
    if has_second:
        _require_sha256(value["second_definition_id_sha256"])
        require(value["second_definition_id_sha256"] != value["first_definition_id_sha256"],
                "BPMN reimport reused the original definition identity")

    complete = all(checks.values()) and has_first and has_export_hash and has_export_size and has_second
    require(assertion_passed is complete, "BPMN assertion does not match the complete roundtrip proof")


def _validate_clipboard(value: object, assertion_passed: bool, proof: dict) -> None:
    required = {"instance_id_sha256", "expected_value_sha256", "native_copy_observed"}
    allowed = required | {"actual_value_sha256"}
    require(isinstance(value, dict) and required <= set(value) <= allowed, "Unsafe clipboard proof fields")
    _require_sha256(value["instance_id_sha256"])
    _require_sha256(value["expected_value_sha256"])
    require(type(value["native_copy_observed"]) is bool, "Invalid clipboard observation flag")
    if "actual_value_sha256" in value:
        _require_sha256(value["actual_value_sha256"])
    require({"instance_id_sha256", "value_sha256"} <= set(proof),
            "Clipboard proof is missing its candidate execution binding")
    require(value["instance_id_sha256"] == proof["instance_id_sha256"],
            "Clipboard proof belongs to a different workflow instance")
    require(value["expected_value_sha256"] == proof["value_sha256"],
            "Clipboard proof uses a different synthetic value")
    observed_match = value.get("native_copy_observed") is True and value.get("actual_value_sha256") == value["expected_value_sha256"]
    require(not value["native_copy_observed"] or observed_match,
            "Clipboard success is missing matching observed bytes")
    require(assertion_passed is observed_match, "Clipboard assertion does not match the native copy proof")


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
    require(isinstance(proof, dict), "Invalid browser proof container")
    assertions_by_name = {item["name"]: item["passed"] for item in assertions}
    stages = {"backend_authenticated", "login_navigation", "login_form", "login_submitted", "workflow_list", "create_dialog_opened", "create_name_filled", "create_submitted", "workflow_created", "output_tab_opened", "output_dialog_opened", "output_type_selected", "output_declared", "activity_registry", "activity_inserted", "property_saved", "edit_reloaded", "workflow_published", "workflow_run", "released_export_menu_opened", "released_export_dialog_opened", "released_document_exported", "baseline_imported", "baseline_reloaded", "baseline_run", "bpmn_input_validated", "bpmn_imported", "bpmn_rendered", "bpmn_selected", "bpmn_exported", "bpmn_reimported", "clipboard_copied"}
    hashes = {"definition_id_sha256", "activity_id_sha256", "value_sha256", "synthetic_document_sha256", "instance_id_sha256", "released_document_sha256"}
    flags = {"login_failure_visible", "login_form_visible", "server_circuit_observed", "server_render_frames_observed", "elsa_identity_ui_visible", "expected_auth_provider_observed", "interactive_validation_observed", "private_input_values_retained", "initial_list_navigation_completed", "editor_ready_observed"}
    counts = {"create_name_label_count", "create_name_textbox_count"}
    require(set(proof) <= hashes | flags | counts | {"last_completed_stage", "baseline_reopens", "bpmn_roundtrip", "clipboard"}, "Unsafe browser proof field")
    require("bpmn_roundtrip" not in proof or "bpmn_roundtrip" in assertions_by_name and record["version"] == "3.10.0",
            "Unexpected BPMN roundtrip proof")
    require("clipboard" not in proof or "clipboard" in assertions_by_name and record["version"] == "3.10.0",
            "Unexpected clipboard proof")
    require(not assertions_by_name.get("bpmn_roundtrip", False) or "bpmn_roundtrip" in proof,
            "Missing BPMN roundtrip proof for passed assertion")
    require(not assertions_by_name.get("clipboard", False) or "clipboard" in proof,
            "Missing clipboard proof for passed assertion")
    for name, value in proof.items():
        if name == "baseline_reopens":
            _validate_reopens(value, key)
            continue
        if name == "bpmn_roundtrip":
            _validate_bpmn_roundtrip(value, assertions_by_name["bpmn_roundtrip"])
            continue
        if name == "clipboard":
            _validate_clipboard(value, assertions_by_name["clipboard"], proof)
            continue
        if name in counts:
            valid = type(value) is int and 0 <= value <= 100
        elif name in flags:
            valid = type(value) is bool
        elif name == "last_completed_stage":
            valid = value in stages
        else:
            valid = isinstance(value, str) and re.fullmatch("[0-9a-f]{64}", value)
        require(valid, "Unsafe browser proof value")
    if any(item["name"] == "baseline_reopen" and item["passed"] for item in assertions):
        _require_reopen_completion(record, key)
    for resource in record.get("resources", []):
        require(set(resource) == {"path", "status", "content_type", "sha256", "bytes", "owner", "requested"}, "Unsafe resource receipt fields")
        require(isinstance(resource["path"], str) and re.fullmatch(r"/[A-Za-z0-9_./-]+", resource["path"]) and ".." not in resource["path"].split("/"), "Unsafe resource receipt path")
        require(resource["owner"] in ("package", "fixture", "platform") and resource["requested"] is True and type(resource["status"]) is int and 100 <= resource["status"] <= 599 and type(resource["bytes"]) is int and 0 <= resource["bytes"] <= 32 * 1024 * 1024, "Unsafe resource receipt metadata")
        require(isinstance(resource["sha256"], str) and re.fullmatch("[0-9a-f]{64}", resource["sha256"]) and resource["content_type"] in set(POLICY["resource_content_types"].values()) | {"application/javascript"}, "Unsafe resource receipt content metadata")
    return record


REOPEN_CHECKS = {"imported", "visible", "saved", "reloaded", "published", "studio_terminal", "backend_output"}


def _require_reopen_completion(record, key):
    rows = record.get("proof", {}).get("baseline_reopens", [])
    _validate_reopens(rows, key)
    require(len(rows) == 2 and all(all(row["checks"].values()) and "instance_id_sha256" in row for row in rows),
            "Both complete released reopen journeys are required")


def _validate_reopens(rows, key):
    require(key[0] == "3.10.0" and isinstance(rows, list) and 0 < len(rows) <= 2, "Invalid released reopen proof")
    seen = set()
    required = {"source_cell", "source_binding_sha256", "document_sha256", "definition_id_sha256", "root_id_sha256",
                "activity_id_sha256", "value_sha256", "tool_version", "checks"}
    for row in rows:
        require(isinstance(row, dict) and required <= set(row) <= required | {"instance_id_sha256"}, "Unsafe reopen fields")
        source = row["source_cell"]
        require(isinstance(source, dict) and set(source) == {"version", "framework", "host"}, "Invalid reopen source cell")
        version = source["version"]
        require(version in {"3.8.4", "3.9.0"} and version not in seen and
                (source["framework"], source["host"]) == key[1:], "Mismatched or duplicate reopen source")
        seen.add(version)
        require(row["tool_version"] == {"3.8.4": "3.8.0.0", "3.9.0": "3.9.0.0"}[version], "Wrong released tool marker")
        require(isinstance(row["checks"], dict) and set(row["checks"]) == REOPEN_CHECKS and
                all(type(value) is bool for value in row["checks"].values()), "Invalid reopen checks")
        for name in set(row) - {"source_cell", "tool_version", "checks"}:
            require(isinstance(row[name], str) and re.fullmatch(r"[0-9a-f]{64}", row[name]), "Unsafe reopen hash")


def _released_inputs(key, inputs):
    """Root verifies source provenance; transport independently rechecks native bytes."""
    from paired_package_provenance import regular_file
    from paired_package_released_documents import TOOL_VERSIONS, canonical_sha256, _bounded, _constant, _pairs
    require(key[0] == "3.10.0" and isinstance(inputs, list) and len(inputs) == 2, "Candidate requires exactly two released inputs")
    result, seen = [], set()
    for item in inputs:
        require(isinstance(item, dict) and set(item) == {"private_path", "binding"}, "Invalid private released input")
        binding = item["binding"]
        require(isinstance(binding, dict) and set(binding) == {"document", "fixture_identity_sha256", "source_evidence_sha256", "browser_receipt_sha256"}, "Invalid released source binding")
        for field in set(binding) - {"document"}:
            require(isinstance(binding[field], str) and re.fullmatch(r"[0-9a-f]{64}", binding[field]), "Invalid released source hash")
        document = binding["document"]
        require(isinstance(document, dict) and set(document) == {"schema", "source_cell", "document_sha256", "bytes", "tool_version", "definition_id_sha256", "activity_id_sha256", "value_sha256", "binding_sha256"}, "Invalid document binding")
        source = document["source_cell"]
        require(isinstance(source, dict) and set(source) == {"version", "framework", "host"}, "Invalid released source cell")
        version = source["version"]
        require(version in TOOL_VERSIONS and version not in seen and (source["framework"], source["host"]) == key[1:], "Released input host/framework/version differs")
        seen.add(version)
        require(type(document["schema"]) is int and document["schema"] == 1 and
                type(document["bytes"]) is int and 0 < document["bytes"] <= 1024 * 1024 and
                document["tool_version"] == TOOL_VERSIONS[version], "Invalid released document metadata")
        for name in ("document_sha256", "definition_id_sha256", "activity_id_sha256", "value_sha256", "binding_sha256"):
            require(isinstance(document[name], str) and re.fullmatch(r"[0-9a-f]{64}", document[name]), "Invalid released document hash")
        require(canonical_sha256({name: value for name, value in document.items() if name != "binding_sha256"}) == document["binding_sha256"], "Released document binding differs")
        path = regular_file(_external_path(Path(item["private_path"])))
        require(path.stat().st_mode & 0o777 == 0o600, "Released input must remain private")
        with path.open("rb") as stream:
            raw = stream.read(1024 * 1024 + 1)
        require(len(raw) == document["bytes"] and hashlib.sha256(raw).hexdigest() == document["document_sha256"], "Released input bytes differ")
        try:
            native = json.loads(raw.decode("utf-8"), object_pairs_hook=_pairs, parse_constant=_constant)
            _bounded(native)
            root_id = native["root"]["id"]
            actual = {"definition_id_sha256": native["definitionId"], "activity_id_sha256": native["root"]["activities"][0]["id"],
                      "value_sha256": native["root"]["activities"][0]["outputValue"]["expression"]["value"]}
            require(isinstance(root_id, str) and re.fullmatch(r"[0-9a-f]{1,16}", root_id), "Invalid released root identity")
            require(native["toolVersion"] == document["tool_version"] and all(isinstance(value, str) and
                    hashlib.sha256(value.encode()).hexdigest() == document[name] for name, value in actual.items()),
                    "Released input identity/value differs")
        except (KeyError, IndexError, TypeError, UnicodeError, RecursionError):
            raise ValueError("Invalid released input structure") from None
        result.append({"private_path": str(path), "binding": json.loads(json.dumps(binding)), "source_binding_sha256": canonical_sha256(binding),
                       "root_id_sha256": hashlib.sha256(root_id.encode()).hexdigest()})
    return sorted(result, key=lambda item: item["binding"]["document"]["source_cell"]["version"])


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


def run_browser(handle, request, resources: list[dict], *, timeout: int = 240,
                released_document_output: Path | None = None, released_document_inputs: list[dict] | None = None) -> dict:
    """Credentials only enter the child through stdin; raw process errors are discarded."""
    cell = asdict(request) if is_dataclass(request) else dict(request)
    identity(cell)
    payload = {"request": cell, "studio_url": handle.studio_url, "backend_url": handle.backend_url,
               "username": handle.username, "password": handle.password,
               "safe_ids": handle.safe_ids, "resources": resources}
    if released_document_inputs is not None:
        payload["released_document_inputs"] = _released_inputs(identity(cell), released_document_inputs)
    if released_document_output is not None:
        require(cell["version"] in {"3.8.4", "3.9.0"}, "Only released cells may author released documents")
        released_document_output = _external_path(released_document_output)
        require(released_document_output.parent.is_dir() and not released_document_output.exists(), "Released document requires a fresh private output in an existing directory")
        payload["released_document_output"] = str(released_document_output)
    try:
        completed = subprocess.run(["npm", "exec", "--no", "--", "tsx", str(JOURNEY)],
                                   cwd=JOURNEY.parent, input=json.dumps(payload), capture_output=True, text=True, timeout=timeout,
                                   env={key: value for key, value in os.environ.items() if key in {"PATH", "HOME", "TMPDIR", "SystemRoot"}})
        record = json.loads(completed.stdout)
    except (OSError, subprocess.TimeoutExpired, json.JSONDecodeError):
        raise ValueError("Browser process failed to return a safe receipt") from None
    require(isinstance(record, dict) and all(key in record for key in ("host", "framework", "version")) and identity(record) == identity(cell), "Browser process returned an invalid cell")
    record = validate_browser_receipt(record, identity(cell))
    reopens = record.get("proof", {}).get("baseline_reopens", [])
    require(not reopens or released_document_inputs is not None, "Unrequested released reopen proof")
    expected_sources = {item["binding"]["document"]["source_cell"]["version"]: item for item in payload.get("released_document_inputs", [])}
    for row in reopens:
        expected_source = expected_sources[row["source_cell"]["version"]]
        expected_document = expected_source["binding"]["document"]
        require(row["source_binding_sha256"] == expected_source["source_binding_sha256"] and
                row["root_id_sha256"] == expected_source["root_id_sha256"] and
                all(row[name] == expected_document[name] for name in
                    ("document_sha256", "definition_id_sha256", "activity_id_sha256", "value_sha256")),
                "Released reopen proof differs from supplied bytes/source")
    require(completed.returncode == (1 if record["result"] == "failed" else 0), "Inconsistent browser exit status")
    document_hash = record.get("proof", {}).get("released_document_sha256")
    require(released_document_output is None or not released_document_output.exists() or document_hash is not None,
            "Released download is missing its browser hash")
    if document_hash is not None:
        from paired_package_provenance import regular_file
        require(released_document_output is not None, "Unrequested released document")
        document = regular_file(released_document_output)
        require(0 < document.stat().st_size <= 1024 * 1024 and document.stat().st_mode & 0o777 == 0o600,
                "Invalid private released document size or permissions")
        require(hashlib.sha256(document.read_bytes()).hexdigest() == document_hash, "Released document bytes differ from browser download")
    return record


def main(argv=None):
    from paired_package_execution import main as execute_main
    return execute_main(argv)


if __name__ == "__main__":
    raise SystemExit(main())
