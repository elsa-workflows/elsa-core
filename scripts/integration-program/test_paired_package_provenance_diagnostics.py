from pathlib import Path
import unittest

import paired_package_provenance_diagnostics as diagnostics


class ProjectProvenanceDiagnosticsTests(unittest.TestCase):
    def test_exact_allowlisted_messages_map_to_fixed_codes(self):
        self.assertEqual(
            {"project": "hosted-wasm", "code": "fixture_reference_path_mismatch"},
            diagnostics.failure_receipt(
                "hosted-wasm", RuntimeError("Restored fixture path differs from owned source edge")),
        )

    def test_dynamic_or_changed_messages_fall_back_without_retaining_text(self):
        private_message = "Restored unclassified Elsa.Studio.Host.Wasm/3.8.4"
        result = diagnostics.failure_receipt("wasm", RuntimeError(private_message))
        self.assertEqual({"project": "wasm", "code": "unknown"}, result)
        self.assertNotIn(private_message, repr(result))
        self.assertEqual({"project": "wasm", "code": "unknown"}, diagnostics.failure_receipt(
            "wasm", OSError("Materialized framework mismatch")))

    def test_boundary_records_only_project_and_code_then_reraises(self):
        evidence = {"stage": "owned_runtime"}
        expected = RuntimeError("Unexpected restored fixture targets")

        def fail(_project):
            raise expected

        with self.assertRaisesRegex(RuntimeError, "Unexpected restored fixture targets") as raised:
            diagnostics.validate_project_with_diagnostic("hosted-wasm", Path("private.csproj"), fail, evidence)

        self.assertIs(raised.exception, expected)
        self.assertEqual({
            "stage": "owned_runtime",
            "project_provenance_failure": {"project": "hosted-wasm", "code": "restored_target_mismatch"},
        }, evidence)

    def test_wrapper_binds_paths_to_enums_and_stage(self):
        backend = Path("/fixture/backend.csproj")
        client = Path("/fixture/wasm.csproj")
        evidence = {"stage": "project_provenance"}
        projects = {"backend": backend, "wasm": client}

        def validate(project):
            if project == client:
                raise RuntimeError("Unexpected project/source fallback")
            return {"ok": True}

        wrapped = diagnostics.wrap_project_validator(projects, validate, evidence)
        self.assertEqual({"ok": True}, wrapped(backend))
        self.assertEqual("project_provenance", evidence["stage"])
        with self.assertRaisesRegex(RuntimeError, "Unexpected project/source fallback"):
            wrapped(client)
        self.assertEqual("project_provenance", evidence["stage"])
        self.assertEqual({"project": "wasm", "code": "unapproved_project_fallback"},
                         evidence["project_provenance_failure"])

    def test_failed_receipt_validator_rejects_unsafe_or_unbound_values(self):
        base = {
            "project": "wasm",
            "code": "unapproved_project_fallback",
        }

        def validate(value, **overrides):
            fields = {
                "host": "hosted-wasm",
                "stage": "project_provenance",
                "result": "failed",
                "failure_category": "execution_or_evidence_failed",
            }
            fields.update(overrides)
            diagnostics.validate_failure_evidence(value, **fields)

        validate(base)
        invalid = [
            ({**base, "message": "private path"}, {}),
            ({**base, "project": "secrets"}, {}),
            ({**base, "code": "private path"}, {}),
            (base, {"stage": "complete"}),
            (base, {"result": "passed"}),
            (base, {"host": "custom-elements"}),
        ]
        for value, overrides in invalid:
            with self.subTest(value=value, overrides=overrides), self.assertRaises(ValueError):
                validate(value, **overrides)


if __name__ == "__main__":
    unittest.main()
