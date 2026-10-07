"""Portable protocol negatives; synthetic fixtures are not native browser proof."""
from __future__ import annotations

import copy
import hashlib
import unittest

import run_paired_package_browser_matrix as matrix
from paired_package_secrets import CHECKS, STAGE_FIELDS
from test_paired_package_browser_native_interop import assertion, receipt


def sha(value):
    return hashlib.sha256(value.encode()).hexdigest()


def secrets_proof():
    identity, reference, label = (sha(value) for value in ("separate native Secrets probe", "safe reference", "selected label"))
    return {
        "checks": dict.fromkeys(CHECKS, True),
        "backend_readonly_inventory_sha256": sha("readonly descriptors"),
        "type_count": 1, "store_count": 1,
        "descriptor_type_sha256": sha("text"), "descriptor_store_sha256": sha("encrypted"),
        "probe_definition_id_sha256": identity, "secret_name_sha256": sha("synthetic technical name"),
        "reference_type_sha256": sha("text"), "reference_scope_sha256": sha("package-browser-probe"),
        "expected_reference_sha256": reference, "expected_selected_label_sha256": label,
        "backend_readonly_created_metadata_sha256": sha("readonly created metadata"),
        "selected_label_sha256": label,
        "saved_definition_id_sha256": identity, "root_id_sha256": sha("root"), "activity_id_sha256": sha("activity"),
        "saved_reference_sha256": reference, "reloaded_definition_id_sha256": identity,
        "reloaded_root_id_sha256": sha("root"), "reloaded_activity_id_sha256": sha("activity"),
        "reloaded_reference_sha256": reference, "reloaded_selected_label_sha256": label,
    }


class SecretsProofContracts(unittest.TestCase):
    def record(self, host="server"):
        value = receipt(host)
        value["proof"].update(secrets=secrets_proof(), definition_id_sha256=sha("canonical workflow"))
        assertion(value, "secrets", True)
        return value

    def validate(self, value):
        return matrix.validate_browser_receipt(value, matrix.identity(value))

    def test_complete_native_reference_probe_supports_all_candidate_hosts_and_frameworks(self):
        for host in matrix.HOSTS:
            for framework in matrix.FRAMEWORKS:
                with self.subTest(host=host, framework=framework):
                    value = self.record(host)
                    value["framework"] = framework
                    self.validate(value)

    def test_each_prefix_is_unfinished_and_cannot_pass(self):
        for completed in range(len(CHECKS)):
            with self.subTest(completed=completed):
                value = self.record()
                proof = value["proof"]["secrets"]
                for stage in CHECKS[completed:]:
                    proof["checks"][stage] = False
                    for field in STAGE_FIELDS.get(stage, set()):
                        proof.pop(field)
                assertion(value, "secrets", False)
                self.validate(value)
                assertion(value, "secrets", True)
                with self.assertRaises(ValueError):
                    self.validate(value)

    def test_feature_absence_denial_and_disconnect_cannot_pass_without_native_reference_proof(self):
        for reason in ("absent", "denied", "disconnected"):
            with self.subTest(reason=reason):
                value = self.record()
                value["proof"].pop("secrets")
                assertion(value, "secrets", False)
                self.validate(value)
                assertion(value, "secrets", True)
                with self.assertRaises(ValueError):
                    self.validate(value)

    def test_unsafe_fields_forged_stages_and_reference_identity_changes_are_rejected(self):
        base = self.record()
        mutations = [
            lambda p: p.update(secret_value="private"),
            lambda p: p.update(secret_value_sha256=sha("private")),
            lambda p: p.update(reference={"name": "raw", "typeName": "text", "scope": "raw"}),
            lambda p: p.update(raw_workflow={}),
            lambda p: p.update(type_count=True),
            lambda p: p.update(store_count=101),
            lambda p: p.update(descriptor_type_sha256=sha("other")),
            lambda p: p.update(descriptor_store_sha256=sha("configuration")),
            lambda p: p.update(reference_type_sha256=sha("other")),
            lambda p: p.update(reference_scope_sha256=sha("other")),
            lambda p: p.update(saved_definition_id_sha256=sha("other")),
            lambda p: p.update(reloaded_definition_id_sha256=sha("other")),
            lambda p: p.update(reloaded_root_id_sha256=sha("other")),
            lambda p: p.update(reloaded_activity_id_sha256=sha("other")),
            lambda p: p.update(saved_reference_sha256=sha("literal payload")),
            lambda p: p.update(reloaded_reference_sha256=sha("other reference")),
            lambda p: p.update(selected_label_sha256=sha("other")),
            lambda p: p.update(reloaded_selected_label_sha256=sha("other")),
            lambda p: p.pop("backend_readonly_created_metadata_sha256"),
            lambda p: p["checks"].update(native_create_dialog=False),
            lambda p: p["checks"].update(native_selected=1),
            lambda p: p["checks"].update(server_ui_request_observed=True),
        ]
        for index, mutate in enumerate(mutations):
            with self.subTest(mutation=index):
                value = copy.deepcopy(base)
                mutate(value["proof"]["secrets"])
                with self.assertRaises(ValueError):
                    self.validate(value)

    def test_probe_cannot_reuse_canonical_or_context_workflow(self):
        for other in ("canonical", "contexts"):
            with self.subTest(other=other):
                value = self.record()
                identity = value["proof"]["secrets"]["probe_definition_id_sha256"]
                if other == "canonical":
                    value["proof"]["definition_id_sha256"] = identity
                else:
                    from test_paired_package_workflow_contexts import contexts_proof
                    contexts = contexts_proof()
                    for field in ("probe_definition_id_sha256", "saved_definition_id_sha256", "reloaded_definition_id_sha256"):
                        contexts[field] = identity
                    value["proof"]["workflow_contexts"] = contexts
                    assertion(value, "workflow_contexts", True)
                with self.assertRaises(ValueError):
                    self.validate(value)

    def test_completed_receipt_cannot_be_downgraded_to_failed_assertion(self):
        value = self.record()
        assertion(value, "secrets", False)
        with self.assertRaises(ValueError):
            self.validate(value)

    def test_baseline_cannot_supply_candidate_secrets_proof(self):
        value = self.record()
        value["version"] = "3.9.0"
        value["assertions"] = [item for item in value["assertions"] if item["name"] in matrix.required_assertions(value)]
        with self.assertRaises(ValueError):
            self.validate(value)


if __name__ == "__main__":
    unittest.main()
