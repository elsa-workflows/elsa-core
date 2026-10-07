"""Private React browser transport and independently revalidatable phase receipts.

A phase does not create a matrix cell or certify package provenance. The caller
owns the shared backend/Studio lifecycle and independently verifies all resources.
"""
from __future__ import annotations

from dataclasses import asdict, is_dataclass
import hashlib
import json
import os
import re
import subprocess

import run_paired_package_browser_matrix as browser
import verify_browser_package_resources as resources

AFTER_VALUE = "synthetic-browser-react-value"
AFTER_VALUE_SHA256 = hashlib.sha256(AFTER_VALUE.encode()).hexdigest()
CHECKS = {"authentication", "source_binding", "react_mount", "selection_callback", "property_edit",
          "saved", "reloaded", "identity_preserved", "react_bundle", "cleanup"}
BEFORE_HASHES = {"definition_id_sha256": "definition_id_sha256", "root_id_sha256": "root_id_sha256",
                 "activity_id_sha256": "activity_id_sha256", "before_value_sha256": "value_sha256"}
HASHES = set(BEFORE_HASHES) | {"after_value_sha256"}
FIELDS = {"schema", "cell", "mode", "result", "browser_version", "source_browser_sha256", "checks",
          "hashes", "resources", "failure_category"}
SUMMARY_FIELDS = {"source_browser_sha256", "phase_receipt_sha256", "checks", "hashes"}
FAILURE = "browser_execution_or_validation_failed"
REACT_PATH = "/_content/Elsa.Studio.Workflows.Designer/react-designer.entry.js"


def require(condition, message):
    if not condition:
        raise ValueError(message)


def _sha(value):
    require(isinstance(value, str) and re.fullmatch(r"[0-9a-f]{64}", value) is not None, "Unsafe React phase hash")


def browser_receipt_sha256(record: dict) -> str:
    """Exact serialization used for retained unchanged browser.json and phase files."""
    raw = (json.dumps(record, indent=2, sort_keys=True, allow_nan=False) + "\n").encode("utf-8")
    return hashlib.sha256(raw).hexdigest()


def _cell(request):
    request = asdict(request) if is_dataclass(request) else dict(request)
    key = browser.identity(request)
    require(key in browser.MATRIX and key[0] == "3.10.0" and key[2] in {"server", "wasm", "hosted-wasm"}
            and request.get("designer_mode", "x6") in {"x6", "react-flow"}, "Unsupported React phase request")
    prefix = request.get("route_prefix", "")
    require(prefix == "" or key[2] == "hosted-wasm" and isinstance(prefix, str) and
            re.fullmatch(r"[a-z][a-z0-9-]{0,31}", prefix), "Unsafe React phase route")
    # The caller may hold the original X6 request; only the designer mode changes.
    return dict(request, designer_mode="react-flow"), key, {"version": key[0], "framework": key[1], "host": key[2]}


def _source(original, key):
    browser.validate_browser_receipt(original, key)
    assertions = {item["name"]: item["passed"] for item in original["assertions"]}
    require(original["result"] in {"passed", "incomplete"} and all(assertions.get(name) is True for name in
            ("authentication", "x6_edit_save_reload", "identity_preserved", "cleanup")), "Original X6 phase is not eligible")
    proof = original.get("proof", {})
    require(not assertions.get("reactflow_edit_save", False) and "reactflow" not in proof,
            "Original child already claims a React phase")
    for name in BEFORE_HASHES.values():
        _sha(proof.get(name))
    require(proof["value_sha256"] != AFTER_VALUE_SHA256, "React phase literal must change")
    return {name: proof[parent] for name, parent in BEFORE_HASHES.items()}



def source_bindings(request, original_browser):
    """Validate the unchanged first child before starting any React listener."""
    _, key, _ = _cell(request)
    return {"source_browser_sha256": browser_receipt_sha256(original_browser),
            "expected_hashes": _source(original_browser, key)}

def _checks_and_hashes(checks, hashes, expected):
    require(isinstance(checks, dict) and set(checks) == CHECKS and all(type(flag) is bool for flag in checks.values()),
            "Invalid React phase checks")
    require(isinstance(hashes, dict) and set(hashes) <= HASHES, "Unsafe React phase hash fields")
    for value in hashes.values():
        _sha(value)
    for name, previous in (("source_binding", "authentication"), ("react_mount", "source_binding"),
                           ("selection_callback", "react_mount"), ("property_edit", "selection_callback"),
                           ("saved", "property_edit"), ("reloaded", "saved"), ("identity_preserved", "reloaded")):
        require(not checks[name] or checks[previous], "React phase checks skipped a required stage")
    source_hash_names = set(hashes) & set(BEFORE_HASHES)
    require(source_hash_names == (set(BEFORE_HASHES) if checks["source_binding"] else set()),
            "Partial React source identity")
    if checks["source_binding"]:
        require(all(hashes[name] == expected[name] for name in BEFORE_HASHES), "React phase changed source identity or value")
    require(("after_value_sha256" in hashes) is checks["property_edit"], "React value hash lacks native edit")
    if checks["property_edit"]:
        require(hashes["after_value_sha256"] == AFTER_VALUE_SHA256 and
                hashes["after_value_sha256"] != hashes["before_value_sha256"], "React phase literal is unchanged or unapproved")


def _expected_resources(expected, request):
    require(isinstance(expected, list) and 0 < len(expected) <= 4096, "Missing or excessive React resource inventory")
    prefix = "/" + request["route_prefix"] if request.get("route_prefix") else ""
    # Reuse the native inventory validator without requiring first-phase assets in this phase.
    resources.verify_browser_resources([dict(asset, required=False) for asset in expected], [], require_all=False, route_prefix=prefix)
    matches = [asset for asset in expected if asset["path"] == prefix + REACT_PATH]
    require(len(matches) == 1 and matches[0]["owner"] == "package" and
            matches[0]["content_type"] in {"text/javascript", "application/javascript"}, "Missing sealed React bundle")
    return {asset["path"]: asset for asset in expected}, matches[0]


def validate_react_phase_receipt(record, request, original_browser, expected_resources):
    request, key, cell = _cell(request)
    expected = _source(original_browser, key)
    materialized, bundle = _expected_resources(expected_resources, request)
    require(isinstance(record, dict) and set(record) == FIELDS and type(record["schema"]) is int and record["schema"] == 1
            and record["cell"] == cell and record["mode"] == "react-flow", "Unsafe React phase receipt identity")
    require(record["result"] in {"passed", "failed"} and
            record["failure_category"] == (None if record["result"] == "passed" else FAILURE), "Invalid React phase result")
    require(record["browser_version"] is None and record["result"] == "failed" or
            isinstance(record["browser_version"], str) and re.fullmatch(r"[0-9]+(?:\.[0-9]+){1,3}", record["browser_version"]),
            "Invalid React browser version")
    require(record["source_browser_sha256"] == browser_receipt_sha256(original_browser), "React phase source browser differs")
    _checks_and_hashes(record["checks"], record["hashes"], expected)
    observed = record["resources"]
    require(isinstance(observed, list) and len(observed) <= 8192, "Invalid React resource observations")
    # The existing child sanitizer enforces exact safe response fields. Its empty
    # proof avoids relating this separate phase to the original WASM boot proof.
    shell = dict(cell, result="failed", browser_version=record["browser_version"] or "0.0",
                 failure_category=FAILURE, proof={}, resources=observed,
                 assertions=[{"name": name, "passed": False, "reason_category": "not_implemented"}
                             for name in sorted(browser.required_assertions(cell))])
    browser.validate_browser_receipt(shell, key)
    require(all(item["path"] in materialized for item in observed), "Unpaired React phase resource")
    matching_bundle = any(item["path"] == bundle["path"] and item["status"] == 200 and item["requested"] is True and
                          all(item[name] == bundle[name] for name in ("sha256", "bytes", "content_type", "owner")) for item in observed)
    require(record["checks"]["react_bundle"] is matching_bundle, "React bundle claim differs from actual byte observation")
    require(record["result"] != "passed" or all(record["checks"].values()), "Incomplete React phase cannot pass")
    require(record["browser_version"] is not None or not any(record["checks"].values()) and not observed and not record["hashes"],
            "Unlaunched React browser claimed observations")
    return record


def summarize_react_phase(receipt):
    require(isinstance(receipt, dict) and set(receipt) == FIELDS, "Invalid React phase for summary")
    return {"source_browser_sha256": receipt["source_browser_sha256"],
            "phase_receipt_sha256": browser_receipt_sha256(receipt),
            "checks": dict(receipt["checks"]), "hashes": dict(receipt["hashes"])}


def validate_react_phase_summary(value, assertion_passed, parent_proof, key):
    require(key in browser.MATRIX and key[0] == "3.10.0" and key[2] in {"server", "wasm", "hosted-wasm"},
            "Unexpected React phase cell")
    require(isinstance(value, dict) and set(value) == SUMMARY_FIELDS and type(assertion_passed) is bool,
            "Unsafe React phase summary")
    _sha(value["source_browser_sha256"])
    _sha(value["phase_receipt_sha256"])
    expected = {}
    for name, parent in BEFORE_HASHES.items():
        _sha(parent_proof.get(parent))
        expected[name] = parent_proof[parent]
    _checks_and_hashes(value["checks"], value["hashes"], expected)
    require(not assertion_passed or all(value["checks"].values()), "React assertion lacks complete native phase")
    return value



def _unique_object(pairs):
    result = {}
    for name, value in pairs:
        require(name not in result, "Duplicate React receipt field")
        result[name] = value
    return result

def run_react_phase(handle, request, expected_resources, original_browser, *, timeout=240):
    """Accept the original cell request; require its exact React-mode runtime handle."""
    request, key, _ = _cell(request)
    source = source_bindings(request, original_browser)
    _expected_resources(expected_resources, request)
    require(type(timeout) is int and 0 < timeout <= 240, "Invalid React browser timeout")
    require(asdict(handle.request) == request if is_dataclass(handle.request) else dict(handle.request) == request,
            "React handle/request differ")
    payload = {"phase": "react-flow", "request": request, "studio_url": handle.studio_url,
               "backend_url": handle.backend_url, "username": handle.username, "password": handle.password,
               "safe_ids": handle.safe_ids, "resources": expected_resources,
               "react_phase": source}
    try:
        completed = browser._run_browser_process(["npm", "exec", "--no", "--", "tsx", str(browser.JOURNEY)],
            cwd=browser.JOURNEY.parent, input=json.dumps(payload), timeout=timeout,
            env={key: value for key, value in os.environ.items() if key in {"PATH", "HOME", "TMPDIR", "SystemRoot"}})
        require(len(completed.stdout.encode("utf-8")) <= 4 * 1024 * 1024, "React child receipt exceeds bound")
        record = json.loads(completed.stdout, object_pairs_hook=_unique_object)
    except browser.BrowserCleanupUnverified:
        raise
    except (OSError, subprocess.TimeoutExpired, json.JSONDecodeError):
        raise ValueError("React browser did not return a safe receipt") from None
    record = validate_react_phase_receipt(record, request, original_browser, expected_resources)
    require(completed.returncode == (0 if record["result"] == "passed" else 1), "React browser exit status differs")
    return record
