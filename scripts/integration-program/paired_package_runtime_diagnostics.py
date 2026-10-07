"""Closed startup diagnostics; process logs and exception text remain private."""
from __future__ import annotations

HOSTS = frozenset({"server", "wasm", "hosted-wasm", "custom-elements"})
MESSAGE_CODES = {
    "Missing project verifier or invalid startup bound": "startup_bound_invalid",
    "Invalid owned process lifetime/platform": "startup_lifetime_invalid",
    "Materialized input changed before launch": "startup_input_changed",
    "Unreviewed host settings entered fixture": "startup_settings_unreviewed",
    "Unexpected source ProjectReference": "startup_source_reference_invalid",
    "Source edge is not fixture-only WASM glue": "startup_source_edge_invalid",
    "Missing restored asset targets": "startup_restored_targets_missing",
    "Restored library uses source/project fallback": "startup_source_fallback",
    "Restored project edge escapes fixture glue": "startup_source_edge_escape",
    "Restored library is not a package": "startup_library_unclassified",
    "Project provenance not bound to restored assets": "startup_project_unbound",
    "Backend and Studio origins collided": "startup_origins_collided",
    "Symlinked client runtime config": "startup_config_symlink",
    "Owned host exited before readiness; inspect private process log": "startup_host_exited",
    "Owned host readiness timed out": "startup_readiness_timeout",
}
CODES = frozenset(MESSAGE_CODES.values()) | {"unknown"}
READINESS_CODES = frozenset({"startup_host_exited", "startup_readiness_timeout"})
CONFIGURATION_CODES = frozenset({"startup_origins_collided", "startup_config_symlink"})
VALIDATION_CODES = CODES - READINESS_CODES - CONFIGURATION_CODES - {"unknown"}


def validate_operation(value, host) -> None:
    if not isinstance(value, dict) or set(value) not in (
            {"component", "phase"}, {"component", "phase", "http_status"}):
        raise ValueError("Invalid retained runtime startup operation")
    if not isinstance(host, str) or host not in HOSTS:
        raise ValueError("Invalid retained runtime startup host")
    component, phase = value.get("component"), value.get("phase")
    allowed = {("pair", "validation"), ("pair", "configuration"),
               ("backend", "launch"), ("backend", "readiness"),
               (host, "launch"), (host, "readiness")}
    if not isinstance(component, str) or not isinstance(phase, str) or (component, phase) not in allowed:
        raise ValueError("Invalid retained runtime startup boundary")
    if "http_status" in value and (phase != "readiness" or type(value["http_status"]) is not int
                                   or not 100 <= value["http_status"] <= 599):
        raise ValueError("Invalid retained runtime readiness status")


def report_operation(evidence, host, component, phase) -> None:
    operation = {"component": component, "phase": phase}
    validate_operation(operation, host)
    evidence["last_startup_operation"] = operation


def report_status(evidence, host, component, status) -> None:
    operation = dict(evidence.get("last_startup_operation", {}))
    if operation.get("component") != component or operation.get("phase") != "readiness":
        raise ValueError("Runtime readiness status is outside its boundary")
    operation["http_status"] = status
    validate_operation(operation, host)
    evidence["last_startup_operation"] = operation


def failure_receipt(error: BaseException) -> dict[str, str]:
    code = "unknown"
    if type(error) in (ValueError, RuntimeError) and len(error.args) == 1 and isinstance(error.args[0], str):
        code = MESSAGE_CODES.get(error.args[0], "unknown")
    return {"code": code}


def validate_evidence(evidence) -> None:
    operation = evidence.get("last_startup_operation")
    failure = evidence.get("runtime_startup_failure")
    if "last_startup_operation" not in evidence and "runtime_startup_failure" not in evidence:
        return
    validate_operation(operation, evidence.get("host"))
    if not isinstance(evidence.get("stage"), str) or evidence["stage"] not in {
            "owned_runtime", "project_provenance", "runtime_readiness", "browser_execution",
            "loaded_assemblies", "secrets_endpoint_ownership", "react_source_binding", "react_runtime",
            "react_browser_execution", "react_loaded_assemblies", "browser_resources", "released_document",
            "browser_contract", "optional_feature_probes", "complete"}:
        raise ValueError("Runtime startup diagnostic is outside execution")
    if "runtime_startup_failure" not in evidence:
        return
    if (not isinstance(failure, dict) or set(failure) != {"code"}
            or not isinstance(failure.get("code"), str) or failure["code"] not in CODES
            or evidence.get("result") != "failed"
            or evidence.get("failure_category") != "execution_or_evidence_failed"
            or evidence.get("stage") not in {"owned_runtime", "project_provenance"}
            or (evidence["stage"] == "project_provenance" and operation["phase"] != "validation")):
        raise ValueError("Invalid retained runtime startup failure")
    code, phase = failure["code"], operation["phase"]
    if ((code in READINESS_CODES and phase != "readiness")
            or (code in CONFIGURATION_CODES and phase != "configuration")
            or (code in VALIDATION_CODES and phase != "validation")):
        raise ValueError("Runtime startup failure is outside its boundary")
