import unittest

import paired_package_provenance_diagnostics as diagnostics


class ResourceProvenanceDiagnosticsTests(unittest.TestCase):
    def test_exact_known_messages_map_to_stage_scoped_codes(self):
        self.assertEqual(
            {"stage": "static", "code": "static_archive_digest_mismatch"},
            diagnostics.resource_failure_receipt(
                "static", RuntimeError("Baseline browser archive differs from approved source digest")),
        )
        self.assertEqual(
            {"stage": "managed", "code": "managed_runtime_subset_missing"},
            diagnostics.resource_failure_receipt(
                "managed", ValueError("Missing mandatory package runtime subset")),
        )

    def test_unknown_errors_and_stages_retain_no_exception_text(self):
        private_message = "Missing asset at /private/cache/package/3.8.4"
        receipt = diagnostics.resource_failure_receipt("other", RuntimeError(private_message))
        self.assertEqual({"stage": "unknown", "code": "unknown"}, receipt)
        self.assertNotIn(private_message, repr(receipt))
        self.assertEqual({"stage": "bootstrap", "code": "unknown"}, diagnostics.resource_failure_receipt(
            "bootstrap", OSError("private path")))

    def test_boundary_preserves_original_exception_and_inner_stage(self):
        evidence = {"stage": "resource_provenance"}
        expected = RuntimeError("Baseline browser archive differs from approved source digest")

        def fail():
            raise expected

        with self.assertRaises(RuntimeError) as raised:
            diagnostics.run_resource_boundary(
                "managed",
                lambda: diagnostics.run_resource_boundary("static", fail, evidence),
                evidence,
            )

        self.assertIs(raised.exception, expected)
        self.assertEqual({"stage": "static", "code": "static_archive_digest_mismatch"},
                         evidence["resource_provenance_failure"])
        self.assertEqual({"stage", "code"}, set(evidence["resource_provenance_failure"]))

    def test_failed_receipt_validator_accepts_only_bound_closed_values(self):
        value = {"stage": "host_policy", "code": "host_policy_invalid"}

        def validate(record, **overrides):
            fields = {
                "host": "hosted-wasm",
                "stage": "resource_provenance",
                "result": "failed",
                "failure_category": "execution_or_evidence_failed",
            }
            fields.update(overrides)
            diagnostics.validate_resource_failure_evidence(record, **fields)

        validate(value)
        invalid = [
            ({**value, "message": "private path"}, {}),
            ({**value, "code": "private path"}, {}),
            ({**value, "stage": "private path"}, {}),
            (value, {"stage": "complete"}),
            (value, {"result": "passed"}),
            (value, {"host": "backend"}),
        ]
        for record, overrides in invalid:
            with self.subTest(record=record, overrides=overrides), self.assertRaises(ValueError):
                validate(record, **overrides)


if __name__ == "__main__":
    unittest.main()
