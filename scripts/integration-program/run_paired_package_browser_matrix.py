#!/usr/bin/env python3
"""Complete coverage ledger and private process transport for real package browsers."""
from __future__ import annotations

from dataclasses import asdict, is_dataclass
import ctypes
import errno
import hashlib
import json
import os
import re
import signal
from pathlib import Path
import subprocess
import sys
import time
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
JSON_ROUNDTRIP_CHECKS = {
    "exported", "import_chooser_observed", "imported", "saved", "reloaded",
    "semantic_preserved", "published", "terminal", "output", "studio_terminal",
}
DOM_INTEROP_CHECKS = {"import_menu_clicked", "filechooser_observed", "import_succeeded", "save_callback_observed"}
REACT_PHASE_HOSTS = {"server", "wasm", "hosted-wasm", "custom-elements"}
NATIVE_JSON_HOSTS = set(REACT_PHASE_HOSTS)


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


def _validate_json_roundtrip(value: object, assertion_passed: bool, proof: dict, key: tuple[str, str, str]) -> None:
    required = {
        "definition_id_sha256", "root_id_sha256", "activity_id_sha256", "expected_value_sha256",
        "source_semantic_sha256", "checks",
    }
    optional = {
        "export_document_sha256", "export_document_bytes", "export_semantic_sha256",
        "uploaded_document_sha256", "imported_document_sha256", "imported_semantic_sha256",
        "saved_semantic_sha256", "reloaded_semantic_sha256", "instance_id_sha256", "actual_output_sha256",
    }
    require(isinstance(value, dict) and required <= set(value) <= required | optional,
            "Unsafe native JSON roundtrip proof fields")
    require(key[0] == "3.10.0" and key[2] in NATIVE_JSON_HOSTS, "Unexpected native JSON roundtrip host")
    checks = value["checks"]
    require(isinstance(checks, dict) and set(checks) == JSON_ROUNDTRIP_CHECKS and
            all(type(flag) is bool for flag in checks.values()), "Invalid native JSON roundtrip checks")

    hash_fields = (required - {"checks"}) | ((set(value) & optional) - {"export_document_bytes"})
    for name in hash_fields:
        _require_sha256(value[name], "Invalid native JSON roundtrip hash")
    require(type(value.get("export_document_bytes", 0)) is int and
            0 <= value.get("export_document_bytes", 0) <= 1024 * 1024,
            "Invalid native JSON export size")

    parent_bindings = {
        "definition_id_sha256": "definition_id_sha256",
        "root_id_sha256": "root_id_sha256",
        "activity_id_sha256": "activity_id_sha256",
        "expected_value_sha256": "value_sha256",
    }
    require(all(parent in proof for parent in parent_bindings.values()),
            "Native JSON proof is missing candidate execution bindings")
    for name, parent in parent_bindings.items():
        require(value[name] == proof[parent], "Native JSON proof is bound to a different candidate workflow")

    has_export_hash = "export_document_sha256" in value
    has_export_size = "export_document_bytes" in value
    has_export_semantic = "export_semantic_sha256" in value
    require(has_export_hash == has_export_size == has_export_semantic == checks["exported"],
            "Native JSON export fields differ from the observed download")
    if checks["exported"]:
        require(0 < value["export_document_bytes"] <= 1024 * 1024, "Invalid native JSON download size")

    for name in ("uploaded_document_sha256", "imported_document_sha256"):
        if name in value:
            require(checks["import_chooser_observed"], "Native JSON upload hash has no native chooser observation")
            require(has_export_hash and value[name] == value["export_document_sha256"],
                    "Native JSON import did not use the observed export bytes")
    if checks["import_chooser_observed"]:
        require("uploaded_document_sha256" in value, "Native JSON chooser observation has no uploaded bytes binding")
    if checks["imported"]:
        require(checks["import_chooser_observed"] and "imported_document_sha256" in value and
                "imported_semantic_sha256" in value, "Native JSON import is missing native UI or semantic evidence")
    else:
        require("imported_semantic_sha256" not in value, "Unobserved native JSON import has semantic evidence")

    for stage, previous, field in (
        ("saved", "imported", "saved_semantic_sha256"),
        ("reloaded", "saved", "reloaded_semantic_sha256"),
    ):
        if checks[stage]:
            require(checks[previous] and field in value, f"Native JSON {stage} is missing its prior stage or semantic hash")
        else:
            require(field not in value, f"Unobserved native JSON {stage} has semantic evidence")

    semantic_stages = ["source_semantic_sha256", "export_semantic_sha256", "imported_semantic_sha256",
                       "saved_semantic_sha256", "reloaded_semantic_sha256"]
    semantic_complete = all(name in value for name in semantic_stages)
    semantic_equal = semantic_complete and len({value[name] for name in semantic_stages}) == 1
    require(not checks["semantic_preserved"] or all(checks[name] for name in ("exported", "imported", "saved", "reloaded")) and semantic_equal,
            "Native JSON semantic preservation is not supported by matching roundtrip hashes")
    if checks["published"]:
        require(checks["semantic_preserved"], "Native JSON candidate was published before semantic preservation")
    if checks["terminal"]:
        require(checks["published"] and "instance_id_sha256" in value,
                "Native JSON terminal proof is missing its published run")
    require(("instance_id_sha256" in value) is checks["terminal"],
            "Native JSON instance identity does not match terminal observation")
    if "instance_id_sha256" in value:
        _require_sha256(value["instance_id_sha256"])
        require(proof.get("instance_id_sha256") == value["instance_id_sha256"],
                "Native JSON proof belongs to a different candidate run")
    if checks["output"]:
        require(checks["terminal"] and "actual_output_sha256" in value and
                value["actual_output_sha256"] == value["expected_value_sha256"],
                "Native JSON output differs from the bound candidate value")
    if "actual_output_sha256" in value:
        require(checks["terminal"], "Native JSON output hash has no terminal run")
    if checks["studio_terminal"]:
        require(checks["output"], "Native JSON Studio terminal view preceded the verified output")

    complete = all(checks.values()) and semantic_equal and has_export_hash and "instance_id_sha256" in value and \
        value.get("actual_output_sha256") == value["expected_value_sha256"]
    require(assertion_passed is complete, "JSON roundtrip assertion does not match its complete native proof")


def _validate_dom_interop(value: object, assertion_passed: bool, proof: dict, key: tuple[str, str, str]) -> None:
    required = {
        "definition_id_sha256", "root_id_sha256", "activity_id_sha256", "expected_value_sha256", "checks",
    }
    optional = {"export_document_sha256", "uploaded_document_sha256"}
    require(isinstance(value, dict) and required <= set(value) <= required | optional,
            "Unsafe native DOM interop proof fields")
    require(key[0] == "3.10.0" and key[2] in NATIVE_JSON_HOSTS, "Unexpected native DOM interop host")
    checks = value["checks"]
    require(isinstance(checks, dict) and set(checks) == DOM_INTEROP_CHECKS and
            all(type(flag) is bool for flag in checks.values()), "Invalid native DOM interop checks")
    for name in required - {"checks"} | optional & set(value):
        _require_sha256(value[name], "Invalid native DOM interop hash")

    json_proof = proof.get("json_roundtrip")
    require(isinstance(json_proof, dict), "DOM interop proof is missing its candidate JSON journey")
    for field, parent in (("definition_id_sha256", "definition_id_sha256"),
                          ("root_id_sha256", "root_id_sha256"),
                          ("activity_id_sha256", "activity_id_sha256"),
                          ("expected_value_sha256", "value_sha256")):
        require(value[field] == proof.get(parent) == json_proof.get(field),
                "DOM interop proof is bound to a different candidate workflow")

    if "export_document_sha256" in value:
        require(value["export_document_sha256"] == json_proof.get("export_document_sha256"),
                "DOM import action is bound to a different native export")
    if checks["import_menu_clicked"]:
        require(checks["filechooser_observed"] and "export_document_sha256" in value,
                "DOM import click has no native chooser observation")
    if checks["filechooser_observed"]:
        require(checks["import_menu_clicked"] and "uploaded_document_sha256" in value and
                value.get("uploaded_document_sha256") == value.get("export_document_sha256"),
                "DOM file chooser does not carry the exact exported document")
    else:
        require("uploaded_document_sha256" not in value, "DOM upload hash has no native file chooser")
    if checks["import_succeeded"]:
        require(checks["filechooser_observed"] and json_proof["checks"]["import_chooser_observed"] and
                json_proof["checks"]["imported"] and
                value.get("uploaded_document_sha256") == json_proof.get("imported_document_sha256"),
                "DOM import success has no completed native workflow import")
    if checks["save_callback_observed"]:
        require(checks["import_succeeded"] and json_proof["checks"]["saved"],
                "DOM save callback has no completed native import and backend save")

    complete = all(checks.values())
    require(assertion_passed is complete, "DOM interop assertion does not match its native callback proof")


DIRECT_BACKEND_CHECKS = {"distinct_origins", "login_request", "login_cors", "login_authenticated",
                         "descriptor_request", "descriptor_cors", "bearer_matches_login",
                         "login_precedes_descriptor", "activity_identity"}


def _validate_direct_backend(value: object, assertion_passed: bool) -> None:
    optional = {"login_status", "descriptor_status", "descriptor_count", "descriptor_body_sha256"}
    require(isinstance(value, dict) and {"checks"} <= set(value) <= {"checks"} | optional,
            "Unsafe direct backend proof fields")
    checks = value["checks"]
    require(isinstance(checks, dict) and set(checks) == DIRECT_BACKEND_CHECKS and
            all(type(flag) is bool for flag in checks.values()), "Invalid direct backend checks")
    for name in ("login_status", "descriptor_status"):
        require(name not in value or type(value[name]) is int and 100 <= value[name] <= 599,
                "Invalid direct backend status")
    require("descriptor_count" not in value or type(value["descriptor_count"]) is int and
            0 < value["descriptor_count"] <= 10_000, "Invalid direct backend descriptor count")
    if "descriptor_body_sha256" in value:
        _require_sha256(value["descriptor_body_sha256"], "Invalid direct backend descriptor hash")
        require("descriptor_status" in value, "Descriptor hash has no observed response")
    require("descriptor_count" not in value or value.get("descriptor_status") == 200 and
            "descriptor_body_sha256" in value, "Descriptor count has no successful response body")
    require(not any(checks[name] for name in ("login_request", "login_cors", "login_authenticated")) or
            "login_status" in value, "Missing observed browser login status")
    require(not checks["login_authenticated"] or value.get("login_status") == 200,
            "Browser login did not authenticate")
    require(not any(checks[name] for name in ("descriptor_request", "descriptor_cors", "bearer_matches_login",
                                           "login_precedes_descriptor", "activity_identity")) or
            {"login_status", "descriptor_status"} <= set(value), "Missing observed browser request statuses")
    require(not checks["bearer_matches_login"] or checks["login_authenticated"],
            "Browser bearer is not bound to authenticated login")
    require(not checks["activity_identity"] or value.get("descriptor_status") == 200 and
            {"descriptor_count", "descriptor_body_sha256"} <= set(value) and value["descriptor_count"] >= 2,
            "Activity identity is missing its successful descriptor response")
    require(assertion_passed is all(checks.values()), "Direct backend assertion differs from observed proof")


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
    from paired_package_workflow_contexts import validate_workflow_contexts
    validate_workflow_contexts(proof.get("workflow_contexts"), assertions_by_name.get("workflow_contexts", False), proof, key)
    from paired_package_secrets import validate_secrets
    validate_secrets(proof.get("secrets"), assertions_by_name.get("secrets", False), proof, key)
    stages = {"backend_authenticated", "login_navigation", "login_form", "login_submitted", "workflow_list", "create_dialog_opened", "create_name_filled", "create_submitted", "workflow_created", "output_tab_opened", "output_dialog_opened", "output_type_selected", "output_declared", "activity_registry", "activity_inserted", "property_saved", "edit_reloaded", "workflow_published", "workflow_run", "released_export_menu_opened", "released_export_dialog_opened", "released_document_exported", "candidate_export_menu_opened", "candidate_export_dialog_opened", "candidate_json_exported", "candidate_import_menu_opened", "candidate_import_chooser_observed", "candidate_json_imported", "candidate_json_saved", "candidate_json_reloaded", "baseline_imported", "baseline_reloaded", "baseline_run", "bpmn_input_validated", "bpmn_imported", "bpmn_rendered", "bpmn_selected", "bpmn_exported", "bpmn_reimported", "clipboard_copied"}
    hashes = {"definition_id_sha256", "root_id_sha256", "activity_id_sha256", "value_sha256", "synthetic_document_sha256", "instance_id_sha256", "released_document_sha256"}
    flags = {"login_failure_visible", "login_form_visible", "server_circuit_observed", "server_render_frames_observed", "elsa_identity_ui_visible", "expected_auth_provider_observed", "interactive_validation_observed", "private_input_values_retained", "initial_list_navigation_completed", "editor_ready_observed"}
    counts = {"create_name_label_count", "create_name_textbox_count"}
    stages.add("workflow_contexts_reloaded")
    stages.add("secrets_reloaded")
    require(set(proof) <= hashes | flags | counts | {"last_completed_stage", "baseline_reopens", "bpmn_roundtrip", "clipboard", "direct_backend", "wasm_boot", "json_roundtrip", "dom_interop", "reactflow", "embedding", "workflow_contexts", "secrets", "resource_failures"}, "Unsafe browser proof field")
    from paired_package_embedding import validate_embedding
    validate_embedding(proof.get("embedding"), assertions_by_name, proof, key)
    require("reactflow" not in proof or record["version"] == "3.10.0" and record["host"] in REACT_PHASE_HOSTS,
            "Unexpected React phase proof")
    require(not assertions_by_name.get("reactflow_edit_save", False) or "reactflow" in proof,
            "Missing independent React phase proof for passed assertion")
    require("wasm_boot" not in proof or record["host"] == "wasm" and record["framework"] in FRAMEWORKS,
            "Unexpected standalone WASM boot proof")
    require(not assertions_by_name.get("wasm_boot", False) or record["host"] != "wasm" or "wasm_boot" in proof,
            "Missing standalone WASM boot proof for passed assertion")
    require("direct_backend" not in proof or record["host"] == "wasm", "Unexpected direct backend proof")
    require(not assertions_by_name.get("direct_backend", False) or "direct_backend" in proof,
            "Missing direct backend proof for passed assertion")
    require("bpmn_roundtrip" not in proof or "bpmn_roundtrip" in assertions_by_name and record["version"] == "3.10.0",
            "Unexpected BPMN roundtrip proof")
    require("clipboard" not in proof or "clipboard" in assertions_by_name and record["version"] == "3.10.0",
            "Unexpected clipboard proof")
    require("json_roundtrip" not in proof or record["version"] == "3.10.0" and record["host"] in NATIVE_JSON_HOSTS,
            "Unexpected native JSON roundtrip proof")
    require("dom_interop" not in proof or record["version"] == "3.10.0" and record["host"] in NATIVE_JSON_HOSTS,
            "Unexpected native DOM interop proof")
    require(not assertions_by_name.get("bpmn_roundtrip", False) or "bpmn_roundtrip" in proof,
            "Missing BPMN roundtrip proof for passed assertion")
    require(not assertions_by_name.get("clipboard", False) or "clipboard" in proof,
            "Missing clipboard proof for passed assertion")
    require(not assertions_by_name.get("json_roundtrip", False) or "json_roundtrip" in proof,
            "Missing native JSON proof for passed assertion")
    require(not assertions_by_name.get("dom_interop", False) or "dom_interop" in proof,
            "Missing native DOM proof for passed assertion")
    for name, value in proof.items():
        if name == "resource_failures":
            require(record["result"] == "failed" and isinstance(value, list) and 1 <= len(value) <= 32,
                    "Resource failure diagnostics require a failed bounded receipt")
            for failure in value:
                require(isinstance(failure, dict) and set(failure) == {"path_sha256", "status", "phase", "reason"},
                        "Unsafe resource failure fields")
                _require_sha256(failure["path_sha256"])
                require(failure["status"] is None or type(failure["status"]) is int and 100 <= failure["status"] <= 599,
                        "Invalid failed resource response status")
                require((failure["phase"] == "body" and failure["reason"] in {
                            "resource_body_limit", "resource_body_size", "response_read_failed"}) or
                        (failure["phase"] == "observation" and failure["reason"] == "resource_observation_failed"),
                        "Invalid resource failure category")
            continue
        if name in {"embedding", "workflow_contexts", "secrets"}:
            continue  # Validated with its host assertions and parent identity above.
        if name == "reactflow":
            from paired_package_react_phase import validate_react_phase_summary
            validate_react_phase_summary(value, assertions_by_name["reactflow_edit_save"], proof, key)
            continue
        if name == "wasm_boot":
            from paired_package_wasm_boot import validate_boot_receipt
            validate_boot_receipt(value, assertions_by_name["wasm_boot"], record.get("resources", []),
                                  proof.get("interactive_validation_observed") is True, record["framework"])
            continue
        if name == "direct_backend":
            _validate_direct_backend(value, assertions_by_name["direct_backend"])
            continue
        if name == "baseline_reopens":
            _validate_reopens(value, key)
            continue
        if name == "bpmn_roundtrip":
            _validate_bpmn_roundtrip(value, assertions_by_name["bpmn_roundtrip"])
            continue
        if name == "clipboard":
            _validate_clipboard(value, assertions_by_name["clipboard"], proof)
            continue
        if name == "json_roundtrip":
            _validate_json_roundtrip(value, assertions_by_name["json_roundtrip"], proof, key)
            continue
        if name == "dom_interop":
            _validate_dom_interop(value, assertions_by_name["dom_interop"], proof, key)
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


class BrowserCleanupUnverified(ValueError):
    """Sanitized failure: the caller must not claim that owned cleanup passed."""

    CATEGORIES = frozenset({"root_identity", "root_stop", "inventory", "descendant_stop",
                            "ancestry_stability", "signal", "exit", "reap", "unknown"})

    def __init__(self, message: str = "Browser process cleanup could not be verified", *, categories=()):
        super().__init__(message)
        require(set(categories) <= self.CATEGORIES, "Unsafe cleanup failure category")
        self.categories = tuple(sorted(set(categories) or {"unknown"}))


def _browser_process_info(pid: int) -> tuple[int, object, bool, bool] | None:
    """Read parent, birth identity, zombie/stopped state without command lines/environment."""
    if sys.platform == "darwin":
        # sys/proc_info.h: PROC_PIDTBSDINFO (3), including microsecond birth time.
        class BsdInfo(ctypes.Structure):
            _fields_ = [("header", ctypes.c_uint32 * 12), ("names", ctypes.c_char * 48),
                        ("tail", ctypes.c_uint32 * 6), ("started", ctypes.c_uint64 * 2)]
        try:
            library = ctypes.CDLL("/usr/lib/libproc.dylib", use_errno=True)
            query = library.proc_pidinfo
            query.argtypes = [ctypes.c_int, ctypes.c_int, ctypes.c_uint64, ctypes.c_void_p, ctypes.c_int]
            query.restype = ctypes.c_int
            info = BsdInfo()
            ctypes.set_errno(0)
            size = query(pid, 3, 0, ctypes.byref(info), ctypes.sizeof(info))
            error = ctypes.get_errno()
        except Exception:
            raise OSError("Process identity unavailable") from None
        if size == 0 and error == errno.ESRCH:
            return None
        if size != ctypes.sizeof(info):
            raise OSError("Process identity unavailable")
        return info.header[4], tuple(info.started), info.header[1] == 5, info.header[1] == 4  # SZOMB/SSTOP
    if sys.platform.startswith("linux"):
        try:
            raw = Path(f"/proc/{pid}/stat").read_text()
        except FileNotFoundError:
            return None
        # comm may contain spaces/parentheses; fields after its closing ')' begin at field 3.
        try:
            fields = raw[raw.rfind(")") + 2:].split()
            return int(fields[1]), int(fields[19]), fields[0] == "Z", fields[0] in {"T", "t"}
        except (IndexError, ValueError):
            raise OSError("Process identity unavailable") from None
    raise OSError("Browser cleanup requires macOS or Linux process identities")


def _signal_browser_process(pid: int, started: object, sig: int) -> bool:
    current = _browser_process_info(pid)
    if current is None or current[1] != started or current[2]:
        return False
    try:
        os.kill(pid, sig)
    except ProcessLookupError:
        return False
    return True


_MAX_OWNED_BROWSER_PROCESSES = 4096
_BROWSER_PROCESS_INVENTORY_TIMEOUT_SECONDS = 10
_BROWSER_PROCESS_STOP_TIMEOUT_SECONDS = 2


def _darwin_child_pids(parent: int) -> list[int]:
    """Bounded direct-child snapshots; exact-fit arrays may be truncated.

    Apple's libproc proc_listchildpids returns a PID count, not bytes, and
    maps syscall errors to zero. Clear errno to distinguish an empty result.
    No null-buffer probe: its size estimate includes the entire process list.
    See apple-oss-distributions/xnu, libsyscall/wrappers/libproc/libproc.c.
    """
    try:
        library = ctypes.CDLL("/usr/lib/libproc.dylib", use_errno=True)
        query = library.proc_listchildpids
        query.argtypes = [ctypes.c_int, ctypes.c_void_p, ctypes.c_int]
        query.restype = ctypes.c_int
    except Exception:
        raise OSError("Native process inventory unavailable") from None
    for capacity in (64, 256, 1024, 4096):
        values = (ctypes.c_int * capacity)()
        try:
            ctypes.set_errno(0)
            count = query(parent, values, ctypes.sizeof(values))
            error = ctypes.get_errno()
        except Exception:
            raise OSError("Native process inventory unavailable") from None
        if count < 0 or count > capacity or error:
            raise OSError("Native process inventory unavailable")
        if count == capacity:
            continue
        children = list(values[:count])
        if any(pid <= 0 or pid == parent for pid in children) or len(set(children)) != len(children):
            raise OSError("Invalid native process inventory")
        return children
    raise OSError("Native process inventory may be truncated")


def _cleanup_browser_process(process: subprocess.Popen, started: object) -> None:
    """Freeze and identify descendants before parents exit, including new sessions.

    Check birth identity before each signal; never infer ownership from executable names.
    An already-exited root cannot establish ownership of reparented descendants.
    """
    owned = {process.pid: started}
    failures = set()
    stage = "root_identity"
    inventory_deadline = None

    def check_inventory_deadline() -> None:
        if inventory_deadline is not None and time.monotonic() >= inventory_deadline:
            raise OSError("Browser process inventory exceeded its time bound")

    def wait_until_stopped(pid: int, stamp: object, deadline: float) -> bool:
        while True:
            info = _browser_process_info(pid)
            if info is None or info[1] != stamp or info[2]:
                raise OSError("Browser descendant identity changed before stopping")
            if info[3]:
                return True
            check_inventory_deadline()
            if time.monotonic() >= deadline:
                return False
            time.sleep(0.1)

    def claim(pid: int, parent_pid: int) -> bool:
        nonlocal stage
        parent = _browser_process_info(parent_pid)
        info = _browser_process_info(pid)
        if parent is None or parent[1] != owned[parent_pid] or info is None:
            raise OSError("Browser ancestry disappeared during cleanup")
        if info[0] != parent_pid:
            raise OSError("Browser ancestry changed during cleanup")
        if pid in owned:
            if info[1] != owned[pid]:
                raise OSError("Browser descendant identity changed")
            return False
        owned[pid] = info[1]
        previous_stage = stage
        stage = "descendant_stop"
        if not _signal_browser_process(pid, info[1], signal.SIGSTOP):
            raise OSError("Browser descendant exited before stopping")
        if sys.platform == "darwin" and not wait_until_stopped(
                pid, info[1], time.monotonic() + _BROWSER_PROCESS_STOP_TIMEOUT_SECONDS):
            raise OSError("Browser descendant did not stop")
        stage = previous_stage
        return True

    def discover() -> bool:
        nonlocal inventory_deadline, stage
        if sys.platform == "darwin":
            if inventory_deadline is None:
                inventory_deadline = time.monotonic() + _BROWSER_PROCESS_INVENTORY_TIMEOUT_SECONDS
            added = False
            pending = list(owned)
            for parent_pid in pending:
                check_inventory_deadline()
                parent = _browser_process_info(parent_pid)
                if parent is None or parent[1] != owned[parent_pid] or parent[2]:
                    raise OSError("Browser parent identity unavailable during discovery")
                if not parent[3]:
                    previous_stage = stage
                    stage = "descendant_stop"
                    if not _signal_browser_process(parent_pid, owned[parent_pid], signal.SIGSTOP):
                        raise OSError("Browser parent exited before stopping")
                    if not wait_until_stopped(parent_pid, owned[parent_pid],
                                              time.monotonic() + _BROWSER_PROCESS_STOP_TIMEOUT_SECONDS):
                        raise OSError("Browser parent did not stop")
                    stage = previous_stage
                    parent = _browser_process_info(parent_pid)
                    if parent is None or parent[1] != owned[parent_pid] or parent[2] or not parent[3]:
                        raise OSError("Browser parent identity unavailable during discovery")
                stage = "inventory"
                children = _darwin_child_pids(parent_pid)
                check_inventory_deadline()
                after = _browser_process_info(parent_pid)
                if after is None or after[1] != owned[parent_pid] or after[2] or not after[3]:
                    raise OSError("Browser parent changed during discovery")
                for pid in children:
                    check_inventory_deadline()
                    if pid not in owned and len(owned) >= _MAX_OWNED_BROWSER_PROCESSES:
                        raise OSError("Browser descendants exceeded inventory bound")
                    if claim(pid, parent_pid):
                        pending.append(pid)
                        added = True
                    check_inventory_deadline()
            return added
        table = subprocess.run(["ps", "-axo", "pid=,ppid="], stdout=subprocess.PIPE,
                               stderr=subprocess.DEVNULL, text=True, timeout=2, check=True)
        parents = {int(pid): int(parent) for pid, parent in (line.split() for line in table.stdout.splitlines())}
        added = False
        pending = set(parents) - set(owned)
        while pending:
            children = {pid for pid in pending if parents[pid] in owned}
            if not children:
                break
            for pid in children:
                pending.remove(pid)
                added = claim(pid, parents[pid]) or added
        return added

    def wait_state(seconds: float, *, frozen: bool = False) -> bool:
        deadline = time.monotonic() + seconds
        while True:
            ready = True
            for pid, stamp in owned.items():
                info = _browser_process_info(pid)
                if frozen and (info is None or info[1] != stamp or info[2]):
                    raise OSError("Browser ownership disappeared before inventory completed")
                if info is not None and info[1] == stamp and not info[2] and not (frozen and info[3]):
                    ready = False
            if ready or time.monotonic() >= deadline:
                return ready
            time.sleep(0.1)

    try:
        root_info = _browser_process_info(process.pid)
        if root_info is None or root_info[1] != started or root_info[2]:
            raise OSError("Browser root identity unavailable")
        stage = "root_stop"
        if not _signal_browser_process(process.pid, started, signal.SIGSTOP):
            raise OSError("Browser root exited before descendant ownership was established")
        if not wait_state(2, frozen=True):
            raise OSError("Browser root did not stop")
        # A stable snapshot after every discovered process stops bounds further forks.
        for _ in range(3):
            stage = "inventory"
            added = discover()
            stage = "descendant_stop"
            if not wait_state(2, frozen=True):
                raise OSError("Browser descendants did not stop")
            if not added:
                break
        else:
            stage = "ancestry_stability"
            raise OSError("Browser process ancestry did not stabilize")
    except (OSError, subprocess.SubprocessError, ValueError):
        failures.add(stage)
    finally:
        # Attempt resume even on discovery failure; report signal/identity failures as unverified.
        for pid, stamp in reversed(tuple(owned.items())):
            for sig in (signal.SIGTERM, signal.SIGCONT):
                try:
                    _signal_browser_process(pid, stamp, sig)
                except OSError:
                    failures.add("signal")
    try:
        exited = wait_state(2)
    except OSError:
        failures.add("exit")
        exited = False
    if not exited:
        for pid, stamp in reversed(tuple(owned.items())):
            try:
                _signal_browser_process(pid, stamp, signal.SIGKILL)
            except OSError:
                failures.add("signal")
    try:
        if not wait_state(2):
            failures.add("exit")
    except OSError:
        failures.add("exit")
    try:
        process.wait(timeout=2)
    except (OSError, subprocess.SubprocessError):
        failures.add("reap")
    if failures:
        raise BrowserCleanupUnverified(categories=failures) from None


def _run_browser_process(command: list[str], *, cwd: Path, input: str, env: dict, timeout: int) -> subprocess.CompletedProcess:
    require(os.name == "posix", "Browser process cleanup requires POSIX")
    process = subprocess.Popen(command, cwd=cwd, env=env, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                               stderr=subprocess.PIPE, text=True, start_new_session=True)
    info = None
    try:
        info = _browser_process_info(process.pid)
        if info is None:
            raise OSError("Browser root identity unavailable")
        stdout, stderr = process.communicate(input=input, timeout=timeout)
        return subprocess.CompletedProcess(command, process.returncode, stdout, stderr)
    except BaseException:
        if info is not None:
            _cleanup_browser_process(process, info[1])
        else:
            # Popen still owns this unreaped child; no descendant ownership is established.
            try:
                process.kill()
                process.wait(timeout=2)
            finally:
                raise BrowserCleanupUnverified(categories={"root_identity"}) from None
        raise
    finally:
        for stream in (process.stdin, process.stdout, process.stderr):
            if stream is not None:
                stream.close()


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
        completed = _run_browser_process(["npm", "exec", "--no", "--", "tsx", str(JOURNEY)],
                                   cwd=JOURNEY.parent, input=json.dumps(payload), timeout=timeout,
                                   env={key: value for key, value in os.environ.items() if key in {"PATH", "HOME", "TMPDIR", "SystemRoot"}})
        record = json.loads(completed.stdout)
    except (OSError, subprocess.TimeoutExpired, json.JSONDecodeError):
        raise ValueError("Browser process failed to return a safe receipt") from None
    require(isinstance(record, dict) and all(key in record for key in ("host", "framework", "version")) and identity(record) == identity(cell), "Browser process returned an invalid cell")
    record = validate_browser_receipt(record, identity(cell))
    require("reactflow" not in record.get("proof", {})
            and not any(item["name"] == "reactflow_edit_save" and item["passed"] for item in record["assertions"]),
            "X6 browser child cannot supply the independent React phase proof")
    if cell["host"] == "wasm":
        from paired_package_wasm_boot import validate_boot_request_binding
        validate_boot_request_binding(record, resources, cell["framework"])
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
