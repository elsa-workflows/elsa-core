"""Strict optional-feature observations; valid defect evidence is not acceptance.

Expectations follow candidate ba5b348: SecretPicker catches descriptor failures;
WorkflowContextsEditor does not. Parent-owned HTTP/process observations are
separate inputs, never inferred from direct inventory reads or child booleans.
"""
from __future__ import annotations

from dataclasses import asdict, is_dataclass
import hashlib
import re

HOSTS = ("server", "wasm", "hosted-wasm", "custom-elements")
FRAMEWORKS = ("net8.0", "net9.0", "net10.0")
SCENARIOS = ("without-secrets", "without-workflow-contexts", "deny-secrets", "deny-workflow-contexts", "disconnect")
CHECKS = {"authenticated", "native_workflow_created", "native_action", "observation_completed"}
UI_FLAGS = {"secret_syntax_visible", "secret_navigation_visible", "context_heading_visible", "synthetic_checkbox_visible",
            "picker_empty", "inline_create_visible", "authorization_guidance_visible", "error_visible", "editor_visible",
            "server_circuit_closed"}
FIELDS = {"schema", "cell", "scenario", "backend_features", "permission_profile", "checks", "hashes", "ui", "requests",
          "disconnect", "failure_category"}
ENDPOINT_METHODS = {"secrets-descriptors": "GET", "secrets-picker": "POST", "workflow-context-descriptors": "GET"}
REQUEST_FIELDS = {"endpoint", "method", "source", "status", "transport_failed", "after_action", "after_disconnect_ack", "body"}
DISCONNECT_CHECKS = {"child_ready_observed", "owned_backend_stopped", "studio_alive_after_stop", "browser_alive_after_stop",
                     "cleanup_verified"}
EMPTY_BODY_SHA256 = hashlib.sha256(b"").hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def _sha(value):
    require(isinstance(value, str) and re.fullmatch(r"[0-9a-f]{64}", value) is not None, "Unsafe optional feature hash")


def optional_feature_profile(scenario):
    require(isinstance(scenario, str) and scenario in SCENARIOS, "Unknown optional feature scenario")
    return {"backend_features": ["workflow-contexts"] if scenario == "without-secrets" else
            ["secrets"] if scenario == "without-workflow-contexts" else ["workflow-contexts", "secrets"],
            "permission_profile": scenario if scenario in {"deny-secrets", "deny-workflow-contexts"} else "full"}


def validate_optional_feature_profile(request, scenario):
    request = asdict(request) if is_dataclass(request) else request
    require(isinstance(request, dict), "Invalid optional feature request")
    cell = {name: request.get(name) for name in ("version", "framework", "host")}
    require(cell["version"] == "3.10.0" and cell["framework"] in FRAMEWORKS and cell["host"] in HOSTS,
            "Unsupported optional feature cell")
    profile = optional_feature_profile(scenario)
    features = request.get("backend_features")
    require(type(features) in (list, tuple) and list(features) == profile["backend_features"] and
            request.get("permission_profile") == profile["permission_profile"], "Optional feature profile differs")
    require(request.get("designer_mode", "x6") == "x6", "Optional feature probe cannot replace the React phase")
    prefix = request.get("route_prefix", "")
    require(prefix == "" or cell["host"] == "hosted-wasm" and isinstance(prefix, str) and
            re.fullmatch(r"[a-z][a-z0-9-]{0,31}", prefix) is not None, "Unsafe optional feature route")
    require("permission_grants" not in request, "Optional feature grants must come from the named fixture profile")
    return cell, profile


def _requests(rows, source, scenario):
    require(type(rows) is list and len(rows) <= 64, "Invalid optional feature request inventory")
    for row in rows:
        require(isinstance(row, dict) and set(row) == REQUEST_FIELDS, "Unsafe optional feature request fields")
        require(isinstance(row["endpoint"], str) and row["endpoint"] in ENDPOINT_METHODS and
                row["method"] == ENDPOINT_METHODS[row["endpoint"]] and row["source"] == source,
                "Optional feature request source or route differs")
        for flag in ("transport_failed", "after_action", "after_disconnect_ack"):
            require(type(row[flag]) is bool, "Invalid optional feature request flag")
        require(row["after_disconnect_ack"] is False or scenario == "disconnect" and row["after_action"],
                "Optional feature request precedes the disconnect action")
        require(row["status"] is None if row["transport_failed"] else
                type(row["status"]) is int and 100 <= row["status"] <= 599, "Invalid optional feature HTTP status")
        require(source != "backend-native" or not row["transport_failed"], "Incoming backend endpoint cannot observe a transport failure")
        body = row["body"]
        require(not row["transport_failed"] or body is None, "Transport failure cannot contain an HTTP body")
        if body is not None:
            require(isinstance(body, dict) and set(body) == {"bytes", "sha256", "sensitive_items_present"},
                    "Unsafe optional feature body fields")
            require(type(body["bytes"]) is int and 0 <= body["bytes"] <= 65536, "Invalid optional feature body size")
            _sha(body["sha256"])
            require(body["sensitive_items_present"] is None or type(body["sensitive_items_present"]) is bool,
                    "Invalid optional feature payload classification")
            require((body["bytes"] == 0) is (body["sha256"] == EMPTY_BODY_SHA256), "Optional empty body hash differs")
            require(body["bytes"] != 0 or body["sensitive_items_present"] is False, "Empty body classification differs")
    return rows


def validate_optional_feature_receipt(record, request, scenario, *, parent_observations=None, parent_disconnect=None,
                                      canonical_definition_id_sha256=None):
    """Validate structure, then return independent completeness and acceptance assessments.

    parent_observations must already be bound by the lead to actual native
    backend endpoint observations and canonical package ownership. This module
    validates their safe semantic schema, not their provenance.
    """
    cell, profile = validate_optional_feature_profile(request, scenario)
    require(isinstance(record, dict) and set(record) == FIELDS and type(record["schema"]) is int and record["schema"] == 1
            and record["cell"] == cell and record["scenario"] == scenario and
            all(record[name] == value for name, value in profile.items()), "Unsafe optional feature receipt identity")
    checks, ui, hashes = record["checks"], record["ui"], record["hashes"]
    require(isinstance(checks, dict) and set(checks) == CHECKS and all(type(flag) is bool for flag in checks.values()),
            "Invalid optional feature checks")
    require(not checks["native_workflow_created"] or checks["authenticated"], "Optional workflow precedes authentication")
    require(not checks["native_action"] or checks["authenticated"], "Optional action precedes authentication")
    require(not checks["observation_completed"] or checks["native_action"], "Optional observation lacks a native action")
    require(not checks["native_action"] or scenario == "deny-workflow-contexts" or checks["native_workflow_created"],
            "Optional action lacks its separate workflow")
    require(isinstance(hashes, dict) and set(hashes) == ({"probe_definition_id_sha256"} if checks["native_workflow_created"] else set()),
            "Optional workflow identity differs from its native stage")
    for value in hashes.values():
        _sha(value)
        require(value != EMPTY_BODY_SHA256, "Optional workflow identity is empty")
    if canonical_definition_id_sha256 is not None:
        _sha(canonical_definition_id_sha256)
        require(hashes.get("probe_definition_id_sha256") != canonical_definition_id_sha256, "Optional probe reused canonical workflow")
    require(isinstance(ui, dict) and set(ui) == UI_FLAGS | {"page_closed", "page_error_count"} and
            all(ui[name] is None or type(ui[name]) is bool for name in UI_FLAGS) and type(ui["page_closed"]) is bool and
            type(ui["page_error_count"]) is int and 0 <= ui["page_error_count"] <= 64, "Unsafe optional feature UI observations")
    require(cell["host"] == "server" or ui["server_circuit_closed"] is None, "Nonserver probe claimed a server circuit")
    require(cell["host"] != "custom-elements" or ui["secret_navigation_visible"] is None,
            "Embedded custom elements claimed shell navigation")
    require(record["failure_category"] is None or record["failure_category"] == "probe_execution_failed", "Unsafe optional feature failure category")
    require(record["failure_category"] is None or not checks["observation_completed"], "Failed probe claimed completed observation")
    child_rows = _requests(record["requests"], "browser-native", scenario)
    parent_rows = _requests([] if parent_observations is None else parent_observations, "backend-native", scenario)
    require(not child_rows if cell["host"] == "server" else not parent_rows, "Optional HTTP observation used the wrong host surface")
    rows = parent_rows if cell["host"] == "server" else child_rows
    require(not any(row["after_action"] for row in rows) or checks["native_action"], "HTTP observation lacks its native action")
    disconnect = record["disconnect"]
    if scenario == "disconnect":
        require(isinstance(disconnect, dict) and set(disconnect) == {"child_ready", "parent_acknowledged", "native_action_after_ack"}
                and all(type(flag) is bool for flag in disconnect.values()), "Invalid child disconnect handshake")
        require(not disconnect["parent_acknowledged"] or disconnect["child_ready"], "Disconnect acknowledgement precedes readiness")
        require(not disconnect["native_action_after_ack"] or disconnect["parent_acknowledged"] and checks["native_action"],
                "Disconnect native action precedes acknowledgement")
        require(not any(row["after_disconnect_ack"] for row in rows) or disconnect["native_action_after_ack"],
                "HTTP observation precedes the disconnect handshake")
        if parent_disconnect is not None:
            require(isinstance(parent_disconnect, dict) and set(parent_disconnect) == DISCONNECT_CHECKS and
                    all(type(flag) is bool for flag in parent_disconnect.values()), "Unsafe parent disconnect evidence")
            require(not parent_disconnect["owned_backend_stopped"] or parent_disconnect["child_ready_observed"],
                    "Parent stopped backend before child readiness")
    else:
        require(disconnect is None and parent_disconnect is None, "Unexpected disconnect evidence")
    defects = []

    def need(condition, defect):
        if not condition and defect not in defects:
            defects.append(defect)

    complete = checks["authenticated"] and checks["native_action"] and checks["observation_completed"]
    if scenario != "deny-workflow-contexts":
        complete = complete and checks["native_workflow_created"]
    common_ui = {"authorization_guidance_visible", "error_visible", "editor_visible"}
    if cell["host"] != "custom-elements":
        common_ui.add("secret_navigation_visible")
    if cell["host"] == "server":
        common_ui.add("server_circuit_closed")
    complete = complete and all(ui[name] is not None for name in common_ui)
    need(not ui["page_closed"], "page_closed")
    need(ui["page_error_count"] == 0, "page_error")
    need(ui["server_circuit_closed"] is not True, "server_circuit_closed")
    need(ui["editor_visible"] is True, "editor_unavailable")
    if cell["host"] != "custom-elements":
        need(ui["secret_navigation_visible"] is (scenario not in {"without-secrets", "deny-secrets"}), "unexpected_secret_navigation")
    complete = complete and all(ui[name] is not None for name in ("context_heading_visible", "synthetic_checkbox_visible"))
    if scenario == "deny-workflow-contexts":
        relevant = [row for row in rows if row["endpoint"] == "workflow-context-descriptors" and row["after_action"]]
        complete = complete and bool(relevant)
        need(bool(relevant) and all(row["status"] == 403 for row in relevant), "context_denial_not_observed")
        need(ui["authorization_guidance_visible"] is True, "authorization_guidance_missing")
        need(ui["synthetic_checkbox_visible"] is False, "descriptor_payload_exposed")
    else:
        present = scenario != "without-workflow-contexts"
        need(ui["context_heading_visible"] is present and ui["synthetic_checkbox_visible"] is present, "unexpected_context_controls")
        if present:
            contexts = [row for row in rows if row["endpoint"] == "workflow-context-descriptors"]
            complete = complete and bool(contexts)
            need(bool(contexts) and all(row["status"] == 200 for row in contexts), "present_contexts_not_observed")
        complete = complete and ui["secret_syntax_visible"] is not None
        need(ui["secret_syntax_visible"] is (scenario != "without-secrets"), "unexpected_secret_syntax")
        relevant = [row for row in rows if row["endpoint"] == "secrets-descriptors" and row["after_action"]]
        picker = [row for row in rows if row["endpoint"] == "secrets-picker" and row["after_action"]]
        if scenario == "without-secrets":
            relevant = []
            need(not any(row["endpoint"].startswith("secrets-") for row in rows), "absent_feature_requested")
        elif scenario == "deny-secrets":
            complete = complete and bool(relevant)
            need(bool(relevant) and all(row["status"] == 403 for row in relevant), "secret_denial_not_observed")
            need(not picker, "picker_post_after_descriptor_denial")
            need(ui["authorization_guidance_visible"] is True, "authorization_guidance_missing")
        elif scenario == "without-workflow-contexts":
            complete = complete and bool(relevant) and bool(picker)
            need(bool(relevant) and bool(picker) and all(row["status"] == 200 for row in relevant + picker), "present_secrets_not_observed")
            need(not any(row["endpoint"] == "workflow-context-descriptors" for row in rows), "absent_feature_requested")
        else:
            relevant = [row for row in relevant if row["after_disconnect_ack"]]
            complete = complete and all(disconnect.values()) and parent_disconnect is not None and all(parent_disconnect.values())
            if cell["host"] != "server":
                complete = complete and bool(relevant)
                need(bool(relevant) and all(row["transport_failed"] for row in relevant), "disconnect_transport_not_observed")
            else:
                need(not relevant, "response_after_owned_backend_stop")
            need(not picker, "picker_post_after_disconnect")
            need(ui["error_visible"] is True, "disconnect_error_missing")
        if scenario != "without-secrets":
            complete = complete and ui["picker_empty"] is not None and ui["inline_create_visible"] is not None
            need(ui["inline_create_visible"] is (scenario == "without-workflow-contexts"), "unexpected_inline_create")
            need(ui["picker_empty"] is True, "unexpected_picker_items")
    if scenario in {"deny-secrets", "deny-workflow-contexts"}:
        for row in relevant:
            classified = row["body"] is not None and row["body"]["sensitive_items_present"] is not None
            complete = complete and classified
            need(classified and row["body"]["sensitive_items_present"] is False, "descriptor_payload_unverified_or_exposed")
    if scenario in {"without-secrets", "without-workflow-contexts"}:
        need(ui["error_visible"] is False and ui["authorization_guidance_visible"] is False, "unexpected_optional_feature_error")
    need(record["failure_category"] is None, "probe_execution_failed")
    return {"observation_complete": bool(complete), "acceptance": bool(complete) and not defects, "defects": defects,
            "native_transport_observed": any(row["after_action"] and row["transport_failed"] for row in rows)}


def expected_optional_feature_keys():
    return frozenset(("3.10.0", framework, host, scenario) for framework in FRAMEWORKS for host in HOSTS for scenario in SCENARIOS)


def validate_optional_feature_coverage(keys):
    """Require all sixty separate probes; positive feature proof stays with the main matrix."""
    require(type(keys) is list and all(type(key) is tuple and len(key) == 4 and all(isinstance(part, str) for part in key)
                                     for key in keys), "Invalid optional feature coverage keys")
    require(len(keys) == len(set(keys)) and set(keys) == expected_optional_feature_keys(), "Incomplete or duplicate optional feature coverage")
