"""Portable protocol negatives; these synthetic fixtures are not browser proof."""
from __future__ import annotations

import copy
import hashlib
import unittest

import run_paired_package_browser_matrix as matrix
from paired_package_workflow_contexts import CHECKS, STAGE_FIELDS
from test_paired_package_browser_native_interop import assertion, receipt


def sha(value):
    return hashlib.sha256(value.encode()).hexdigest()


def contexts_proof():
    identity, provider = sha("separate native context probe"), sha("synthetic descriptor type")
    return {
        "checks": dict.fromkeys(CHECKS, True),
        "backend_readonly_inventory_sha256": sha("readonly descriptor inventory"),
        "descriptor_count": 1,
        "descriptor_name_sha256": sha("Synthetic"),
        "descriptor_type_sha256": provider,
        "custom_property_key_sha256": sha("Elsa:WorkflowContextProviderTypes"),
        "probe_definition_id_sha256": identity,
        "saved_definition_id_sha256": identity,
        "saved_provider_type_sha256": provider,
        "reloaded_definition_id_sha256": identity,
        "reloaded_provider_type_sha256": provider,
    }


class WorkflowContextsContracts(unittest.TestCase):
    def record(self, host="server"):
        value = receipt(host)
        value["proof"].update(workflow_contexts=contexts_proof(), definition_id_sha256=sha("canonical workflow"))
        assertion(value, "workflow_contexts", True)
        return value

    def validate(self, value):
        return matrix.validate_browser_receipt(value, (value["version"], value["framework"], value["host"]))

    def test_complete_native_probe_is_supported_on_all_candidate_hosts(self):
        for host in matrix.HOSTS:
            for framework in matrix.FRAMEWORKS:
                with self.subTest(host=host, framework=framework):
                    value = self.record(host)
                    value["framework"] = framework
                    self.validate(value)

    def test_each_prefix_is_valid_only_as_unfinished_evidence(self):
        for completed in range(len(CHECKS)):
            with self.subTest(completed=completed):
                value = self.record()
                proof = value["proof"]["workflow_contexts"]
                for stage in CHECKS[completed:]:
                    proof["checks"][stage] = False
                    for field in STAGE_FIELDS.get(stage, set()):
                        proof.pop(field)
                assertion(value, "workflow_contexts", False)
                self.validate(value)
                assertion(value, "workflow_contexts", True)
                with self.assertRaises(ValueError):
                    self.validate(value)

    def test_missing_native_receipt_cannot_pass(self):
        value = self.record()
        value["proof"].pop("workflow_contexts")
        with self.assertRaises(ValueError):
            self.validate(value)
        assertion(value, "workflow_contexts", False)
        self.validate(value)

    def test_raw_fields_identity_type_and_stage_forgery_are_rejected(self):
        original = self.record()
        mutations = [
            lambda p: p.update(descriptor_type="private raw type"),
            lambda p: p.update(backend_readonly_inventory={"items": []}),
            lambda p: p.update(descriptor_count=True),
            lambda p: p.update(descriptor_count=101),
            lambda p: p.update(descriptor_name_sha256=sha("Other")),
            lambda p: p.update(custom_property_key_sha256=sha("Wrong key")),
            lambda p: p.update(saved_definition_id_sha256=sha("other workflow")),
            lambda p: p.update(reloaded_definition_id_sha256=sha("other workflow")),
            lambda p: p.update(saved_provider_type_sha256=sha("other type")),
            lambda p: p.update(reloaded_provider_type_sha256=sha("other type")),
            lambda p: p.update(probe_definition_id_sha256=sha("canonical workflow")),
            lambda p: p.update(**{field: sha("canonical workflow") for field in
                                  ("probe_definition_id_sha256", "saved_definition_id_sha256", "reloaded_definition_id_sha256")}),
            lambda p: p.pop("reloaded_provider_type_sha256"),
            lambda p: p["checks"].update(native_unchecked=False),
            lambda p: p["checks"].update(native_checked=1),
            lambda p: p["checks"].update(server_ui_request_observed=True),
        ]
        for index, mutation in enumerate(mutations):
            with self.subTest(mutation=index):
                value = copy.deepcopy(original)
                mutation(value["proof"]["workflow_contexts"])
                with self.assertRaises(ValueError):
                    self.validate(value)

    def test_baseline_cannot_supply_candidate_context_proof(self):
        value = self.record()
        value["version"] = "3.9.0"
        value["assertions"] = [item for item in value["assertions"] if item["name"] in matrix.required_assertions(value)]
        with self.assertRaises(ValueError):
            self.validate(value)


if __name__ == "__main__":
    unittest.main()
