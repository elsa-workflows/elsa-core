"""Pure contract checks for the backend's sanitized Secrets endpoint evidence."""
from __future__ import annotations

import copy
import re
import unittest


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


def assembly_inventory() -> list[dict]:
    return [
        {
            "name": "Elsa.Secrets",
            "fullName": "Elsa.Secrets, Version=3.10.0.0, Culture=neutral, PublicKeyToken=null",
            "version": "3.10.0.0",
            "informationalVersion": "3.10.0",
            "location": "package/Elsa.Secrets.dll",
            "sha256": "a" * 64,
        }
    ]


def valid_observations() -> list[dict]:
    inventory = assembly_inventory()[0]
    return [
        {
            "route": route,
            "verb": verb,
            "handler_type": handler,
            "handler_assembly_name": inventory["name"],
            "handler_assembly_full_name": inventory["fullName"],
            "handler_assembly_sha256": inventory["sha256"],
            "status_code": 200,
            "failure_category": None,
        }
        for (verb, route), handler in EXPECTED.items()
    ]


def validate_secrets_endpoint_evidence(document: dict, assemblies: list[dict]) -> None:
    if not isinstance(document, dict) or set(document) != {"schema", "truncated", "observations"}:
        raise ValueError("Unexpected Secrets endpoint evidence envelope")
    if type(document["schema"]) is not int or document["schema"] != 1 or document["truncated"] is not False:
        raise ValueError("Incomplete Secrets endpoint evidence")
    observations = document["observations"]
    if not isinstance(observations, list):
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
            not isinstance(assembly_name, str)
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


class SecretsEndpointEvidenceContracts(unittest.TestCase):
    def test_both_native_routes_bind_to_loaded_handler_assembly_and_success_status(self):
        validate_secrets_endpoint_evidence(
            {"schema": 1, "truncated": False, "observations": valid_observations()},
            assembly_inventory(),
        )

    def test_rejects_missing_or_wrong_handler_route_and_verb(self):
        mutations = [
            lambda item: item.update(handler_type="Fixture.FakeEndpoint"),
            lambda item: item.update(route="/elsa/api/secrets/other"),
            lambda item: item.update(verb="DELETE"),
        ]
        for mutate in mutations:
            document = {"schema": 1, "truncated": False, "observations": valid_observations()}
            mutate(document["observations"][0])
            with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                validate_secrets_endpoint_evidence(document, assembly_inventory())

        document = {"schema": 1, "truncated": False, "observations": valid_observations()[:1]}
        with self.assertRaisesRegex(ValueError, "Missing required"):
            validate_secrets_endpoint_evidence(document, assembly_inventory())

    def test_rejects_unbound_hash_failed_status_truncation_and_raw_fields(self):
        mutations = [
            lambda item: item.update(handler_assembly_sha256="b" * 64),
            lambda item: item.update(status_code=403),
            lambda item: item.update(failure_category="endpoint_definition_missing"),
            lambda item: item.update(raw_body="private"),
        ]
        for mutate in mutations:
            document = {"schema": 1, "truncated": False, "observations": valid_observations()}
            mutate(document["observations"][0])
            with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                validate_secrets_endpoint_evidence(document, assembly_inventory())

        truncated = {"schema": 1, "truncated": True, "observations": valid_observations()}
        with self.assertRaisesRegex(ValueError, "Incomplete"):
            validate_secrets_endpoint_evidence(truncated, assembly_inventory())

        wrong_inventory = copy.deepcopy(assembly_inventory())
        wrong_inventory[0]["sha256"] = "b" * 64
        with self.assertRaisesRegex(ValueError, "not bound"):
            validate_secrets_endpoint_evidence(
                {"schema": 1, "truncated": False, "observations": valid_observations()},
                wrong_inventory,
            )


if __name__ == "__main__":
    unittest.main()
