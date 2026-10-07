"""Hash-only native workflow context metadata receipts, not runtime acceptance."""
from __future__ import annotations

import hashlib
import re

CHECKS = ("backend_readonly_inventory", "native_created", "native_unchecked", "native_checked", "saved", "reloaded")
STAGE_FIELDS = {
    "backend_readonly_inventory": {
        "backend_readonly_inventory_sha256", "descriptor_count", "descriptor_name_sha256",
        "descriptor_type_sha256", "custom_property_key_sha256",
    },
    "native_created": {"probe_definition_id_sha256"},
    "saved": {"saved_definition_id_sha256", "saved_provider_type_sha256"},
    "reloaded": {"reloaded_definition_id_sha256", "reloaded_provider_type_sha256"},
}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def validate_workflow_contexts(value, assertion_passed, proof, key):
    require(type(assertion_passed) is bool, "Invalid workflow contexts assertion")
    if value is None:
        require(not assertion_passed, "Passed workflow contexts assertion lacks native proof")
        return
    require(key[0] == "3.10.0" and key[2] in {"server", "wasm", "hosted-wasm", "custom-elements"},
            "Unexpected workflow contexts proof cell")
    allowed = {"checks"} | set().union(*STAGE_FIELDS.values())
    require(isinstance(value, dict) and {"checks"} <= set(value) <= allowed,
            "Unsafe workflow contexts proof fields")
    checks = value["checks"]
    require(isinstance(checks, dict) and set(checks) == set(CHECKS) and
            all(type(flag) is bool for flag in checks.values()), "Invalid workflow contexts checks")
    for index, name in enumerate(CHECKS):
        require(not checks[name] or all(checks[previous] for previous in CHECKS[:index]),
                "Workflow contexts observation lacks predecessors")
    for check, fields in STAGE_FIELDS.items():
        for field in fields:
            require((field in value) == checks[check], "Workflow contexts evidence differs from observed stage")
            if field not in value:
                continue
            if field == "descriptor_count":
                require(type(value[field]) is int and 1 <= value[field] <= 100,
                        "Invalid workflow contexts descriptor count")
            else:
                require(isinstance(value[field], str) and re.fullmatch(r"[0-9a-f]{64}", value[field]) is not None,
                        "Unsafe workflow contexts hash")
    if checks["backend_readonly_inventory"]:
        for field, expected in (("descriptor_name_sha256", "Synthetic"),
                                ("custom_property_key_sha256", "Elsa:WorkflowContextProviderTypes")):
            require(value[field] == hashlib.sha256(expected.encode()).hexdigest(),
                    "Workflow contexts inventory is not bound to the native Synthetic property")
    for stage in ("saved", "reloaded"):
        if checks[stage]:
            require(value[stage + "_definition_id_sha256"] == value["probe_definition_id_sha256"],
                    "Workflow contexts workflow identity changed")
            require(value[stage + "_provider_type_sha256"] == value["descriptor_type_sha256"],
                    "Workflow contexts metadata differs from the inventoried descriptor type")
    if checks["native_created"] and "definition_id_sha256" in proof:
        require(value["probe_definition_id_sha256"] != proof["definition_id_sha256"],
                "Workflow contexts probe reused the canonical workflow")
    require(assertion_passed is all(checks.values()),
            "Workflow contexts assertion differs from complete native metadata proof")
