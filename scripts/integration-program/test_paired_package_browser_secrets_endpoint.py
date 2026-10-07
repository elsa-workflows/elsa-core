"""Pure contract checks for the backend's sanitized Secrets endpoint evidence."""
from __future__ import annotations

import copy
import unittest


from paired_package_secrets_endpoints import EXPECTED, validate_secrets_endpoint_evidence

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


class SecretsEndpointEvidenceContracts(unittest.TestCase):
    def test_rejects_noncanonical_loaded_assembly_and_unbounded_snapshot(self):
        inventory = assembly_inventory()
        inventory[0]["name"] = "Elsa.Unrelated"
        rows = valid_observations()
        for row in rows:
            row["handler_assembly_name"] = "Elsa.Unrelated"
        with self.assertRaises(ValueError):
            validate_secrets_endpoint_evidence(
                {"schema": 1, "truncated": False, "observations": rows}, inventory)
        with self.assertRaises(ValueError):
            validate_secrets_endpoint_evidence(
                {"schema": 1, "truncated": False, "observations": valid_observations() * 33},
                assembly_inventory())

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
