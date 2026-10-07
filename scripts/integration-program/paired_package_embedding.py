"""Strict portable guards for native CustomElements observations, not runtime acceptance."""
from __future__ import annotations

import re

CHECKS = (
    "backend_configured", "native_authentication", "list_callback", "activity_callback",
    "version_callback", "execution_callback", "instance_list_callback", "instance_viewer",
)
HASH_CHECKS = {
    "definition_id_sha256": "list_callback", "activity_id_sha256": "activity_callback",
    "version_id_sha256": "version_callback", "instance_id_sha256": "execution_callback",
}
PARENT_BINDINGS = ("definition_id_sha256", "activity_id_sha256", "instance_id_sha256")
ASSERTIONS = ("authentication", "native_callbacks", "instance_list_viewer")


def require(condition, message):
    if not condition:
        raise ValueError(message)


def validate_embedding(value, assertions_by_name, proof, key):
    """Bind exact observations; early failures may precede the parent identity hashes."""
    require(isinstance(assertions_by_name, dict) and isinstance(proof, dict), "Invalid embedding parent evidence")
    require(type(key) is tuple and len(key) == 3 and all(isinstance(part, str) for part in key),
            "Invalid embedding cell identity")
    if key[2] != "custom-elements":
        require(value is None, "Unexpected embedding proof for non-CustomElements host")
        require(not assertions_by_name.get("native_callbacks", False) and
                not assertions_by_name.get("instance_list_viewer", False), "Unexpected embedding assertion")
        return
    require(all(type(assertions_by_name.get(name)) is bool for name in ASSERTIONS), "Invalid embedding assertions")
    if value is None:
        require(not any(assertions_by_name[name] for name in ASSERTIONS), "Missing embedding proof for claimed assertion")
        return
    require(isinstance(value, dict) and {"checks"} <= set(value) <= {"checks"} | set(HASH_CHECKS),
            "Unsafe embedding proof fields")
    checks = value["checks"]
    require(isinstance(checks, dict) and set(checks) == set(CHECKS) and
            all(type(flag) is bool for flag in checks.values()), "Invalid embedding checks")
    for index, name in enumerate(CHECKS):
        require(not checks[name] or all(checks[previous] for previous in CHECKS[:index]),
                "Embedding observation has missing predecessors")
    for name, check in HASH_CHECKS.items():
        require((name in value) == checks[check], "Embedding identity differs from observed callback")
        if name in value:
            require(isinstance(value[name], str) and re.fullmatch(r"[0-9a-f]{64}", value[name]) is not None,
                    "Unsafe embedding identity hash")
            if name in PARENT_BINDINGS and name in proof:
                require(value[name] == proof[name], "Embedding identity differs from parent browser proof")
    callbacks = all(checks[name] for name in CHECKS[:6])
    instances = all(checks.values())
    require(assertions_by_name["native_callbacks"] == callbacks,
            "Native callbacks assertion differs from observed embedding proof")
    require(assertions_by_name["instance_list_viewer"] == instances,
            "Instance list/viewer assertion differs from observed embedding proof")
    if assertions_by_name["authentication"]:
        require(checks["backend_configured"] and checks["native_authentication"],
                "Embedding authentication lacks native authenticated requests")
    if callbacks:
        require(all(name in proof for name in PARENT_BINDINGS), "Passed embedding callbacks lack parent identity bindings")
