"""Bind optional Server endpoint snapshots to loaded package assemblies.

The fixture publishes only bounded response lengths and hashes. This module
never accepts or returns response bytes, headers, paths, or query data.
"""
from __future__ import annotations

import hashlib
import re

MAX_OBSERVATIONS = 64
MAX_RESPONSE_BYTES = 65536
MAX_RECORDED_RESPONSE_BYTES = (1 << 63) - 1
SNAPSHOT_PATH = "/_fixture/optional-endpoints"
SNAPSHOT_FIELDS = {"schema", "cursor", "truncated", "pending", "observations"}
OBSERVATION_FIELDS = {
    "sequence",
    "endpoint",
    "route",
    "verb",
    "handler_type",
    "handler_assembly_name",
    "handler_assembly_full_name",
    "handler_assembly_sha256",
    "status_code",
    "failure_category",
    "body",
}
BODY_FIELDS = {"bytes", "sha256", "complete", "sensitive_items_present"}
EMPTY_BODY_SHA256 = hashlib.sha256(b"").hexdigest()

# Fixed candidate endpoint metadata from the canonical package source. These
# identities are checked against the backend's already-verified package DLLs.
EXPECTED_ENDPOINTS = {
    ("GET", "/elsa/api/secrets/descriptors"): (
        "secrets-descriptors",
        "Elsa.Secrets.Endpoints.Secrets.Descriptors.Endpoint",
        "Elsa.Secrets",
    ),
    ("POST", "/elsa/api/secrets/picker"): (
        "secrets-picker",
        "Elsa.Secrets.Endpoints.Secrets.Picker.Endpoint",
        "Elsa.Secrets",
    ),
    ("GET", "/elsa/api/workflow-contexts/provider-descriptors"): (
        "workflow-context-descriptors",
        "Elsa.WorkflowContexts.Endpoints.ProviderTypes.List.List",
        "Elsa.WorkflowContexts",
    ),
}
ENDPOINT_BY_ID = {identity[0]: (verb, route) for (verb, route), identity in EXPECTED_ENDPOINTS.items()}


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def _sha256(value: object, *, nullable: bool = False) -> bool:
    if nullable and value is None:
        return True
    return isinstance(value, str) and re.fullmatch(r"[0-9a-f]{64}", value) is not None


def _package_assembly_matches(assemblies: object, name: str, full_name: str, digest: str) -> bool:
    require(type(assemblies) is list and 0 < len(assemblies) <= 512,
            "Invalid verified backend package assembly inventory")
    matches = [
        row for row in assemblies
        if isinstance(row, dict)
        and row.get("name") == name
        and row.get("fullName") == full_name
        and row.get("sha256") == digest
    ]
    return len(matches) == 1


def validate_optional_endpoint_snapshot(document: object, backend_package_assemblies: object) -> dict:
    """Validate a complete, quiescent full snapshot and return safe fields only.

    Snapshots are taken at action boundaries. Any pending matching request makes
    the boundary ambiguous, so it is rejected rather than attributed to either
    adjacent action. A non-truncated snapshot contains every sequence through
    its cursor, with no query- or body-bearing fields.
    """
    require(isinstance(document, dict) and set(document) == SNAPSHOT_FIELDS,
            "Unexpected optional endpoint snapshot fields")
    require(type(document["schema"]) is int and document["schema"] == 1,
            "Unsupported optional endpoint snapshot schema")
    require(type(document["truncated"]) is bool and document["truncated"] is False,
            "Incomplete optional endpoint snapshot")
    cursor, pending = document["cursor"], document["pending"]
    require(type(cursor) is int and 0 <= cursor <= MAX_OBSERVATIONS,
            "Invalid optional endpoint snapshot cursor")
    require(type(pending) is int and 0 <= pending <= MAX_OBSERVATIONS,
            "Invalid optional endpoint pending count")
    require(pending == 0, "Optional endpoint action boundary has pending requests")
    observations = document["observations"]
    require(type(observations) is list and len(observations) <= MAX_OBSERVATIONS,
            "Invalid optional endpoint observation inventory")
    require(len(observations) == cursor, "Optional endpoint snapshot cursor differs from observations")

    safe_observations = []
    for expected_sequence, item in enumerate(observations, start=1):
        require(isinstance(item, dict) and set(item) == OBSERVATION_FIELDS,
                "Unsafe optional endpoint observation fields")
        require(type(item["sequence"]) is int and item["sequence"] == expected_sequence,
                "Optional endpoint observation sequence is incomplete")
        require(isinstance(item["verb"], str) and isinstance(item["route"], str),
                "Invalid optional endpoint route identity")
        route_key = (item["verb"], item["route"])
        expected = EXPECTED_ENDPOINTS.get(route_key)
        require(expected is not None, "Unexpected optional endpoint route")
        endpoint, handler_type, assembly_name = expected
        require(item["endpoint"] == endpoint and item["handler_type"] == handler_type and
                item["handler_assembly_name"] == assembly_name,
                "Optional endpoint handler identity differs")
        full_name, digest = item["handler_assembly_full_name"], item["handler_assembly_sha256"]
        require(isinstance(full_name, str) and 0 < len(full_name) <= 512 and _sha256(digest),
                "Invalid optional endpoint assembly identity")
        require(_package_assembly_matches(backend_package_assemblies, assembly_name, full_name, digest),
                "Optional endpoint assembly is not bound to verified backend package bytes")
        status = item["status_code"]
        require(type(status) is int and 100 <= status <= 599, "Invalid optional endpoint response status")

        failure = item["failure_category"]
        require(failure in (None, "response_body_unobserved"),
                "Optional endpoint metadata or response observation failed")
        body = item["body"]
        require(isinstance(body, dict) and set(body) == BODY_FIELDS,
                "Unsafe optional endpoint response body metadata")
        size, body_hash, complete, sensitive = (
            body["bytes"], body["sha256"], body["complete"], body["sensitive_items_present"])
        require(type(size) is int and 0 <= size <= MAX_RECORDED_RESPONSE_BYTES,
                "Invalid optional endpoint response byte count")
        require(type(complete) is bool and (sensitive is None or type(sensitive) is bool),
                "Invalid optional endpoint response coverage")
        if complete:
            require(_sha256(body_hash) and failure is None,
                    "Complete optional endpoint response has an invalid hash")
            if size == 0:
                require(body_hash == EMPTY_BODY_SHA256 and sensitive is False,
                        "Empty optional endpoint response proof differs")
            else:
                require(body_hash != EMPTY_BODY_SHA256 and sensitive is None,
                        "Non-empty optional endpoint response claimed empty content")
        else:
            require(body_hash is None and sensitive is None and failure == "response_body_unobserved",
                    "Incomplete optional endpoint response lacks its fixed failure category")

        safe_observations.append({
            "sequence": expected_sequence,
            "endpoint": endpoint,
            "route": route_key[1],
            "verb": route_key[0],
            "handler_type": handler_type,
            "handler_assembly_name": assembly_name,
            "handler_assembly_full_name": full_name,
            "handler_assembly_sha256": digest,
            "status_code": status,
            "failure_category": failure,
            "body": {"bytes": size, "sha256": body_hash, "complete": complete,
                     "sensitive_items_present": sensitive},
        })

    return {"schema": 1, "cursor": cursor, "truncated": False, "pending": 0,
            "observations": safe_observations}


def derive_backend_native_rows(before_snapshot: object, after_snapshot: object,
                               backend_package_assemblies: object, *, scenario: str,
                               after_disconnect_ack: bool = False) -> list[dict]:
    """Return the after-cut request ledger, marking entries after the before cursor."""
    from paired_package_optional_features import SCENARIOS

    require(isinstance(scenario, str) and scenario in SCENARIOS,
            "Unknown optional endpoint action scenario")
    require(type(after_disconnect_ack) is bool and
            (not after_disconnect_ack or scenario == "disconnect"),
            "Optional endpoint disconnect boundary differs")
    before = validate_optional_endpoint_snapshot(before_snapshot, backend_package_assemblies)
    after = validate_optional_endpoint_snapshot(after_snapshot, backend_package_assemblies)
    require(after["cursor"] >= before["cursor"], "Optional endpoint cursor moved backwards")
    old_count = before["cursor"]
    require(after["observations"][:old_count] == before["observations"],
            "Optional endpoint snapshot changed prior observations")

    return [
        {
            "endpoint": row["endpoint"],
            "method": row["verb"],
            "source": "backend-native",
            "status": row["status_code"],
            "transport_failed": False,
            "after_action": row["sequence"] > before["cursor"],
            "after_disconnect_ack": row["sequence"] > before["cursor"] and after_disconnect_ack,
            "body": ({"bytes": row["body"]["bytes"], "sha256": row["body"]["sha256"],
                      "sensitive_items_present": row["body"]["sensitive_items_present"]}
                     if row["body"]["complete"] and row["body"]["bytes"] <= MAX_RESPONSE_BYTES else None),
        }
        for row in after["observations"]
    ]
