"""Hash-only native Secrets picker/reference receipts, not execution acceptance."""
from __future__ import annotations

import hashlib
import re

CHECKS = ("backend_readonly_inventory", "native_created", "native_picker", "native_create_dialog",
          "native_secret_created", "native_selected", "saved", "reloaded")
STAGE_FIELDS = {
    "backend_readonly_inventory": {"backend_readonly_inventory_sha256", "type_count", "store_count",
                                   "descriptor_type_sha256", "descriptor_store_sha256"},
    "native_created": {"probe_definition_id_sha256", "secret_name_sha256", "reference_type_sha256",
                       "reference_scope_sha256", "expected_reference_sha256", "expected_selected_label_sha256"},
    "native_secret_created": {"backend_readonly_created_metadata_sha256"},
    "native_selected": {"selected_label_sha256"},
    "saved": {"saved_definition_id_sha256", "root_id_sha256", "activity_id_sha256", "saved_reference_sha256"},
    "reloaded": {"reloaded_definition_id_sha256", "reloaded_root_id_sha256", "reloaded_activity_id_sha256",
                 "reloaded_reference_sha256", "reloaded_selected_label_sha256"},
}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def validate_secrets(value, assertion_passed, proof, key):
    require(type(assertion_passed) is bool, "Invalid Secrets assertion")
    if value is None:
        require(not assertion_passed, "Passed Secrets assertion lacks native proof")
        return
    require(key[0] == "3.10.0" and key[2] in {"server", "wasm", "hosted-wasm", "custom-elements"},
            "Unexpected Secrets proof cell")
    allowed = {"checks"} | set().union(*STAGE_FIELDS.values())
    require(isinstance(value, dict) and {"checks"} <= set(value) <= allowed, "Unsafe Secrets proof fields")
    checks = value["checks"]
    require(isinstance(checks, dict) and set(checks) == set(CHECKS) and
            all(type(flag) is bool for flag in checks.values()), "Invalid Secrets checks")
    for index, name in enumerate(CHECKS):
        require(not checks[name] or all(checks[previous] for previous in CHECKS[:index]),
                "Secrets observation lacks predecessors")
    for check, fields in STAGE_FIELDS.items():
        for field in fields:
            require((field in value) == checks[check], "Secrets evidence differs from observed stage")
            if field not in value:
                continue
            if field in {"type_count", "store_count"}:
                require(type(value[field]) is int and 1 <= value[field] <= 100, "Invalid Secrets descriptor count")
            else:
                require(isinstance(value[field], str) and re.fullmatch(r"[0-9a-f]{64}", value[field]) is not None,
                        "Unsafe Secrets hash")
    if checks["backend_readonly_inventory"]:
        for field, expected in (("descriptor_type_sha256", "text"), ("descriptor_store_sha256", "encrypted")):
            require(value[field] == hashlib.sha256(expected.encode()).hexdigest(),
                    "Secrets inventory is not bound to a writable text/encrypted descriptor")
    if checks["native_created"]:
        require(value["reference_type_sha256"] == value["descriptor_type_sha256"], "Secrets reference type changed")
        require(value["reference_scope_sha256"] == hashlib.sha256(b"package-browser-probe").hexdigest(),
                "Secrets probe scope changed")
        for other in (proof.get("definition_id_sha256"),
                      (proof.get("workflow_contexts") or {}).get("probe_definition_id_sha256")):
            require(other is None or value["probe_definition_id_sha256"] != other,
                    "Secrets probe reused another workflow")
    for stage in ("saved", "reloaded"):
        if checks[stage]:
            require(value[stage + "_definition_id_sha256"] == value["probe_definition_id_sha256"],
                    "Secrets workflow identity changed")
            require(value[stage + "_reference_sha256"] == value["expected_reference_sha256"],
                    "Secrets reference identity changed")
    for stage in ("native_selected", "reloaded"):
        if checks[stage]:
            field = "selected_label_sha256" if stage == "native_selected" else "reloaded_selected_label_sha256"
            require(value[field] == value["expected_selected_label_sha256"], "Secrets selected label changed")
    if checks["reloaded"]:
        for field in ("root_id_sha256", "activity_id_sha256"):
            require(value["reloaded_" + field] == value[field], "Secrets graph identity changed on reload")
    require(assertion_passed is all(checks.values()),
            "Secrets assertion differs from complete native reference proof")
