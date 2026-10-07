"""Validate positive canonical Secrets route ownership against loaded assemblies."""
from __future__ import annotations

import re

EXPECTED = {
    ("GET", "/elsa/api/secrets/descriptors"): "Elsa.Secrets.Endpoints.Secrets.Descriptors.Endpoint",
    ("POST", "/elsa/api/secrets/picker"): "Elsa.Secrets.Endpoints.Secrets.Picker.Endpoint",
}
OBSERVATION_FIELDS = {
    "route",
    "verb",
    "handler_type",
    "handler_assembly_name",
    "handler_assembly_full_name",
    "handler_assembly_sha256",
    "status_code",
    "failure_category",
}


def validate_secrets_endpoint_evidence(document: dict, assemblies: list[dict]) -> None:
    if not isinstance(document, dict) or set(document) != {"schema", "truncated", "observations"}:
        raise ValueError("Unexpected Secrets endpoint evidence envelope")
    if type(document["schema"]) is not int or document["schema"] != 1 or document["truncated"] is not False:
        raise ValueError("Incomplete Secrets endpoint evidence")
    observations = document["observations"]
    if not isinstance(observations, list) or not 1 <= len(observations) <= 64:
        raise ValueError("Invalid Secrets endpoint observations")

    seen: set[tuple[str, str]] = set()
    for item in observations:
        if not isinstance(item, dict) or set(item) != OBSERVATION_FIELDS:
            raise ValueError("Unexpected Secrets endpoint observation fields")
        if item["failure_category"] is not None:
            raise ValueError("Secrets endpoint metadata could not be bound")
        if not isinstance(item["verb"], str) or not isinstance(item["route"], str):
            raise ValueError("Invalid Secrets endpoint route identity")
        route_key = (item["verb"], item["route"])
        expected_handler = EXPECTED.get(route_key)
        if expected_handler is None or item["handler_type"] != expected_handler:
            raise ValueError("Unexpected Secrets endpoint owner")
        if type(item["status_code"]) is not int or not 200 <= item["status_code"] < 300:
            raise ValueError("Secrets endpoint did not complete successfully")
        assembly_name = item["handler_assembly_name"]
        assembly_full_name = item["handler_assembly_full_name"]
        assembly_sha256 = item["handler_assembly_sha256"]
        if (
            assembly_name != "Elsa.Secrets"
            or not isinstance(assembly_full_name, str)
            or not isinstance(assembly_sha256, str)
            or not re.fullmatch(r"[0-9a-f]{64}", assembly_sha256)
        ):
            raise ValueError("Invalid Secrets handler assembly hash")
        matches = [
            assembly for assembly in assemblies
            if assembly.get("name") == assembly_name
            and assembly.get("fullName") == assembly_full_name
            and assembly.get("sha256") == assembly_sha256
        ]
        if len(matches) != 1:
            raise ValueError("Secrets handler assembly is not bound to loaded assembly inventory")
        seen.add(route_key)

    if seen != set(EXPECTED):
        raise ValueError("Missing required Secrets endpoint observation")
