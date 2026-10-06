"""Synthetic receipt guards, not actual browser/CORS acceptance evidence."""
import copy
import unittest

import run_paired_package_browser_matrix as matrix


def receipt(version="3.9.0", framework="net10.0", host="wasm"):
    value = {"version": version, "framework": framework, "host": host, "result": "incomplete",
             "browser_version": "149.0.0.0", "failure_category": None, "resources": [], "proof": {}}
    value["assertions"] = [{"name": name, "passed": name == "direct_backend", "reason_category": None}
                           for name in sorted(matrix.required_assertions(value))]
    value["proof"]["direct_backend"] = {
        "checks": dict.fromkeys(matrix.DIRECT_BACKEND_CHECKS, True),
        "login_status": 200, "descriptor_status": 200, "descriptor_count": 2,
        "descriptor_body_sha256": "a" * 64,
    }
    return value


def validate(value):
    return matrix.validate_browser_receipt(value, matrix.identity(value))


def set_passed(value, passed):
    next(row for row in value["assertions"] if row["name"] == "direct_backend")["passed"] = passed


class DirectBackendReceiptContracts(unittest.TestCase):
    def test_all_versions_frameworks_and_later_journey_failure_keep_independent_proof(self):
        for version in matrix.VERSIONS:
            for framework in matrix.FRAMEWORKS:
                with self.subTest(version=version, framework=framework):
                    value = receipt(version, framework)
                    validate(value)
                    value.update(result="failed", failure_category="browser_execution_or_validation_failed")
                    validate(value)

    def test_absent_or_partial_observation_is_allowed_only_with_false_assertion(self):
        value = receipt()
        set_passed(value, False)
        value["proof"]["direct_backend"] = {"checks": dict.fromkeys(matrix.DIRECT_BACKEND_CHECKS, False)}
        validate(value)
        del value["proof"]["direct_backend"]
        validate(value)
        set_passed(value, True)
        with self.assertRaisesRegex(ValueError, "Missing direct backend proof"):
            validate(value)

    def test_each_missing_native_check_rejects_passed_assertion(self):
        for name in matrix.DIRECT_BACKEND_CHECKS:
            with self.subTest(check=name):
                value = receipt()
                value["proof"]["direct_backend"]["checks"][name] = False
                with self.assertRaises(ValueError):
                    validate(value)

    def test_complete_proof_cannot_hide_behind_false_assertion(self):
        value = receipt()
        set_passed(value, False)
        with self.assertRaisesRegex(ValueError, "differs from observed proof"):
            validate(value)

    def test_standalone_scope_rejects_other_hosts(self):
        for host in ("server", "hosted-wasm", "custom-elements"):
            with self.subTest(host=host), self.assertRaisesRegex(ValueError, "Unexpected direct backend proof"):
                validate(receipt(host=host))

    def test_strict_fields_types_bounds_and_response_bindings(self):
        mutations = [
            lambda p: p.update(token="private"), lambda p: p.update(username="private"),
            lambda p: p.update(url="http://127.0.0.1:1"), lambda p: p["checks"].update(extra=True),
            lambda p: p["checks"].pop("login_cors"), lambda p: p["checks"].update(login_cors=1),
            lambda p: p.pop("login_status"), lambda p: p.update(login_status=True),
            lambda p: p.update(login_status=401), lambda p: p.update(descriptor_status=403),
            lambda p: p.update(descriptor_status=600), lambda p: p.pop("descriptor_status"),
            lambda p: p.pop("descriptor_count"), lambda p: p.update(descriptor_count=True),
            lambda p: p.update(descriptor_count=0), lambda p: p.update(descriptor_count=10_001),
            lambda p: p.update(descriptor_count=1),
            lambda p: p.pop("descriptor_body_sha256"), lambda p: p.update(descriptor_body_sha256="not-a-hash"),
        ]
        for index, mutate in enumerate(mutations):
            with self.subTest(mutation=index), self.assertRaises(ValueError):
                value = copy.deepcopy(receipt())
                mutate(value["proof"]["direct_backend"])
                validate(value)


if __name__ == "__main__":
    unittest.main()
