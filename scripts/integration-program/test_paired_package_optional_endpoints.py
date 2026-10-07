"""Pure parent contracts for bounded optional Server endpoint snapshots."""
from __future__ import annotations

import copy
import hashlib
import unittest

import paired_package_optional_endpoints as endpoints
import paired_package_optional_features as optional_features


def assembly_inventory() -> list[dict]:
    return [
        {"name": "Elsa.Secrets", "fullName": "Elsa.Secrets, Version=3.10.0.0, Culture=neutral, PublicKeyToken=null",
         "version": "3.10.0.0", "informationalVersion": "3.10.0", "location": "package/Elsa.Secrets.dll",
         "sha256": "a" * 64},
        {"name": "Elsa.WorkflowContexts", "fullName": "Elsa.WorkflowContexts, Version=3.10.0.0, Culture=neutral, PublicKeyToken=null",
         "version": "3.10.0.0", "informationalVersion": "3.10.0", "location": "package/Elsa.WorkflowContexts.dll",
         "sha256": "b" * 64},
    ]


def observation(sequence: int, endpoint_id: str, *, status: int = 200, body_bytes: int = 0,
                complete: bool = True) -> dict:
    verb, route = endpoints.ENDPOINT_BY_ID[endpoint_id]
    _, handler_type, assembly_name = endpoints.EXPECTED_ENDPOINTS[(verb, route)]
    inventory = next(item for item in assembly_inventory() if item["name"] == assembly_name)
    body_hash = hashlib.sha256(b"").hexdigest() if body_bytes == 0 else "c" * 64
    return {
        "sequence": sequence,
        "route": route,
        "verb": verb,
        "handler_type": handler_type,
        "handler_assembly_name": assembly_name,
        "handler_assembly_full_name": inventory["fullName"],
        "handler_assembly_sha256": inventory["sha256"],
        "status_code": status,
        "failure_category": None if complete else "response_body_unobserved",
        "body": {"bytes": body_bytes, "sha256": body_hash if complete else None,
                 "complete": complete,
                 "sensitive_items_present": (False if complete and body_bytes == 0 else None)},
    }


def snapshot(*rows: dict, pending: int = 0, truncated: bool = False) -> dict:
    return {"schema": 1, "cursor": len(rows) + pending, "truncated": truncated,
            "pending": pending, "observations": copy.deepcopy(list(rows))}


class OptionalEndpointSnapshotContracts(unittest.TestCase):
    def test_all_three_canonical_routes_bind_to_exact_verified_package_assemblies(self):
        rows = [observation(1, "secrets-descriptors"), observation(2, "secrets-picker"),
                observation(3, "workflow-context-descriptors")]
        validated = endpoints.validate_optional_endpoint_snapshot(snapshot(*rows), assembly_inventory())
        self.assertEqual(3, validated["cursor"])
        self.assertEqual(["secrets-descriptors", "secrets-picker", "workflow-context-descriptors"],
                         [endpoints.EXPECTED_ENDPOINTS[(row["verb"], row["route"])][0]
                          for row in validated["observations"]])
        self.assertEqual("backend-native", endpoints.derive_backend_native_rows(
            snapshot(), snapshot(*rows), assembly_inventory(), scenario="without-workflow-contexts")[0]["source"])

    def test_body_hash_shape_distinguishes_proven_empty_nonempty_and_incomplete(self):
        empty = observation(1, "secrets-descriptors", status=403)
        nonempty = observation(2, "workflow-context-descriptors", body_bytes=12)
        incomplete = observation(3, "secrets-picker", body_bytes=7, complete=False)
        normalized = endpoints.validate_optional_endpoint_snapshot(
            snapshot(empty, nonempty, incomplete), assembly_inventory())
        self.assertEqual({"bytes": 0, "sha256": endpoints.EMPTY_BODY_SHA256, "complete": True,
                          "sensitive_items_present": False},
                         normalized["observations"][0]["body"])
        self.assertIsNone(normalized["observations"][1]["body"]["sensitive_items_present"])
        self.assertFalse(normalized["observations"][2]["body"]["complete"])
        incomplete_single = observation(1, "secrets-picker", body_bytes=7, complete=False)
        self.assertIsNone(endpoints.derive_backend_native_rows(
            snapshot(), snapshot(incomplete_single), assembly_inventory(), scenario="deny-secrets")[0]["body"])

    def test_derived_rows_match_generic_optional_receipt_schema_and_action_cursor(self):
        old = observation(1, "workflow-context-descriptors")
        before = snapshot(old)
        after = snapshot(old, observation(2, "secrets-descriptors", status=403))
        rows = endpoints.derive_backend_native_rows(before, after, assembly_inventory(), scenario="deny-secrets")
        self.assertEqual([{
            "endpoint": "secrets-descriptors", "method": "GET", "source": "backend-native", "status": 403,
            "transport_failed": False, "after_action": True, "after_disconnect_ack": False,
            "body": {"bytes": 0, "sha256": endpoints.EMPTY_BODY_SHA256, "sensitive_items_present": False},
        }], rows)
        self.assertEqual(rows, optional_features._requests(rows, "backend-native", "deny-secrets"))

    def test_snapshots_reject_pending_truncated_gapped_or_unbounded_cursors(self):
        one = observation(1, "secrets-descriptors")
        cases = [
            snapshot(one, pending=1),
            snapshot(one, truncated=True),
            {**snapshot(one), "cursor": True},
            {**snapshot(one), "cursor": 2},
            {**snapshot(one), "cursor": endpoints.MAX_OBSERVATIONS + 1},
            {**snapshot(one), "observations": [dict(one, sequence=2)]},
            {**snapshot(one), "query": "private"},
            {**snapshot(one), "observations": [dict(one, response_body="private")]},
        ]
        for document in cases:
            with self.subTest(document=document), self.assertRaises(ValueError):
                endpoints.validate_optional_endpoint_snapshot(document, assembly_inventory())

    def test_canonical_route_handler_and_loaded_assembly_sha_are_all_required(self):
        mutations = [
            lambda row: row.update(route="/elsa/api/secrets/other"),
            lambda row: row.update(route=["/elsa/api/secrets/descriptors"]),
            lambda row: row.update(verb="DELETE"),
            lambda row: row.update(handler_type="Fixture.FakeEndpoint"),
            lambda row: row.update(handler_assembly_name="Elsa.Unrelated"),
            lambda row: row.update(handler_assembly_full_name="Elsa.Secrets, Version=wrong"),
            lambda row: row.update(handler_assembly_sha256="d" * 64),
            lambda row: row.update(status_code=True),
            lambda row: row.update(failure_category="private exception"),
        ]
        for mutate in mutations:
            row = observation(1, "secrets-descriptors")
            mutate(row)
            with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                endpoints.validate_optional_endpoint_snapshot(snapshot(row), assembly_inventory())
        inventory = assembly_inventory()
        inventory[0]["sha256"] = "d" * 64
        with self.assertRaises(ValueError):
            endpoints.validate_optional_endpoint_snapshot(snapshot(observation(1, "secrets-descriptors")), inventory)
        inventory = assembly_inventory()
        inventory.append(copy.deepcopy(inventory[0]))
        with self.assertRaises(ValueError):
            endpoints.validate_optional_endpoint_snapshot(snapshot(observation(1, "secrets-descriptors")), inventory)

    def test_invalid_or_misclassified_body_metadata_is_rejected(self):
        mutations = [
            lambda body: body.update(bytes=True),
            lambda body: body.update(bytes=endpoints.MAX_RECORDED_RESPONSE_BYTES + 1),
            lambda body: body.update(sha256="ABC"),
            lambda body: body.update(complete=1),
            lambda body: body.update(sensitive_items_present=True),
            lambda body: body.update(raw_body="private"),
        ]
        for mutate in mutations:
            row = observation(1, "secrets-descriptors")
            mutate(row["body"])
            with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                endpoints.validate_optional_endpoint_snapshot(snapshot(row), assembly_inventory())
        nonempty_claim = observation(1, "secrets-descriptors", body_bytes=10)
        nonempty_claim["body"]["sensitive_items_present"] = False
        with self.assertRaises(ValueError):
            endpoints.validate_optional_endpoint_snapshot(snapshot(nonempty_claim), assembly_inventory())
        empty_wrong_hash = observation(1, "secrets-descriptors")
        empty_wrong_hash["body"]["sha256"] = "d" * 64
        with self.assertRaises(ValueError):
            endpoints.validate_optional_endpoint_snapshot(snapshot(empty_wrong_hash), assembly_inventory())
        incomplete_with_hash = observation(1, "secrets-descriptors", complete=False)
        incomplete_with_hash["body"]["sha256"] = "d" * 64
        with self.assertRaises(ValueError):
            endpoints.validate_optional_endpoint_snapshot(snapshot(incomplete_with_hash), assembly_inventory())

    def test_large_complete_response_keeps_bounded_hash_evidence_but_not_generic_body_claim(self):
        large = observation(1, "secrets-descriptors", body_bytes=endpoints.MAX_RESPONSE_BYTES + 1)
        validated = endpoints.validate_optional_endpoint_snapshot(snapshot(large), assembly_inventory())
        self.assertEqual(endpoints.MAX_RESPONSE_BYTES + 1, validated["observations"][0]["body"]["bytes"])
        self.assertEqual("c" * 64, validated["observations"][0]["body"]["sha256"])
        derived = endpoints.derive_backend_native_rows(snapshot(), snapshot(large), assembly_inventory(),
                                                      scenario="deny-secrets")
        self.assertIsNone(derived[0]["body"])
        self.assertEqual(200, derived[0]["status"])

    def test_action_delta_rejects_cursor_rollback_mutated_prefix_and_pending_boundaries(self):
        prior = observation(1, "workflow-context-descriptors")
        before = snapshot(prior)
        valid_after = snapshot(prior, observation(2, "secrets-descriptors"))
        self.assertEqual(1, len(endpoints.derive_backend_native_rows(
            before, valid_after, assembly_inventory(), scenario="deny-secrets")))
        changed_prefix = snapshot(dict(prior, status_code=500), observation(2, "secrets-descriptors"))
        for after in (snapshot(), changed_prefix, snapshot(prior, pending=1)):
            with self.subTest(after=after), self.assertRaises(ValueError):
                endpoints.derive_backend_native_rows(before, after, assembly_inventory(), scenario="deny-secrets")
        with self.assertRaises(ValueError):
            endpoints.derive_backend_native_rows(snapshot(prior, pending=1), valid_after,
                                                  assembly_inventory(), scenario="deny-secrets")

    def test_disconnect_ack_is_explicit_and_never_synthesizes_transport_failure(self):
        after = snapshot(observation(1, "secrets-descriptors", status=403))
        with self.assertRaises(ValueError):
            endpoints.derive_backend_native_rows(snapshot(), after, assembly_inventory(),
                                                 scenario="deny-secrets", after_disconnect_ack=True)
        row = endpoints.derive_backend_native_rows(snapshot(), after, assembly_inventory(),
                                                    scenario="disconnect", after_disconnect_ack=True)[0]
        self.assertTrue(row["after_disconnect_ack"])
        self.assertFalse(row["transport_failed"])
        self.assertEqual(403, row["status"])
        self.assertEqual([], endpoints.derive_backend_native_rows(after, after, assembly_inventory(),
                                                                   scenario="disconnect", after_disconnect_ack=True))


if __name__ == "__main__":
    unittest.main()
