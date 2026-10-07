"""React phase protocol contracts; browser/process calls are mocked."""
import copy
from dataclasses import asdict, replace
import hashlib
import json
import os
from types import SimpleNamespace
import unittest
from unittest.mock import patch

import materialize_paired_package_hosts as hosts
import paired_package_embedding as embedding
import paired_package_react_phase as react
import run_paired_package_browser_matrix as browser


def hashed(value):
    return hashlib.sha256(value.encode()).hexdigest()


def phase_fixture(host="server", framework="net10.0"):
    request = hosts.CellRequest(host, framework, "3.10.0", designer_mode="react-flow")
    cell = {"version": request.version, "framework": framework, "host": host}
    passed = {"authentication", "x6_edit_save_reload", "identity_preserved", "cleanup"}
    original = dict(cell, result="incomplete", browser_version="149.0.7827.55", failure_category=None, resources=[],
                    proof={"definition_id_sha256": hashed("abc"), "root_id_sha256": hashed("def"),
                           "activity_id_sha256": hashed("123"), "value_sha256": hashed("synthetic-browser-value")},
                    assertions=[{"name": name, "passed": name in passed, "reason_category": None if name in passed else "not_implemented"}
                                for name in sorted(browser.required_assertions(cell))])
    if host == "custom-elements":
        original["proof"]["instance_id_sha256"] = hashed("456")
        original["proof"]["embedding"] = {
            "checks": dict.fromkeys(embedding.CHECKS, True),
            **{name: original["proof"][name] for name in embedding.PARENT_BINDINGS},
            "version_id_sha256": hashed("789"),
        }
        for assertion in original["assertions"]:
            if assertion["name"] in {"native_callbacks", "instance_list_viewer"}:
                assertion.update(passed=True, reason_category=None)
    raw = b"sealed-react-bundle"
    assets = [{"path": react.REACT_PATH, "sha256": hashlib.sha256(raw).hexdigest(), "bytes": len(raw),
               "content_type": "text/javascript", "owner": "package", "required": True}]
    record = {"schema": 1, "cell": cell, "mode": "react-flow", "result": "passed", "browser_version": "149.0.7827.55",
              "source_browser_sha256": react.browser_receipt_sha256(original), "checks": dict.fromkeys(sorted(react.CHECKS), True),
              "hashes": {name: original["proof"][parent] for name, parent in react.BEFORE_HASHES.items()} | {"after_value_sha256": react.AFTER_VALUE_SHA256},
              "resources": [{key: value for key, value in assets[0].items() if key != "required"} | {"status": 200, "requested": True}], "failure_category": None}
    return request, original, assets, record


class ReactPhaseContracts(unittest.TestCase):
    def setUp(self):
        self.request, self.original, self.assets, self.record = phase_fixture()
        self.handle = SimpleNamespace(request=self.request, studio_url="http://127.0.0.1:1", backend_url="http://127.0.0.1:2/elsa/api",
                                      username="private-user", password="private-password", safe_ids={"definition_name": "paired-browser-012345abcdef", "activity_value": "synthetic-browser-value"})

    def validate(self, record=None, **options):
        return react.validate_react_phase_receipt(record if record is not None else self.record,
            options.get("request", self.request), options.get("original", self.original), options.get("assets", self.assets))

    def test_typescript_semantic_binding_contract(self):
        executable = browser.JOURNEY.parent / "node_modules/.bin/tsx"
        completed = browser._run_browser_process([str(executable), str(browser.JOURNEY.with_name("react-phase.contract.ts"))],
            cwd=browser.JOURNEY.parent, input="", timeout=60,
            env={key: value for key, value in os.environ.items() if key in {"PATH", "HOME", "TMPDIR", "SystemRoot"}})
        self.assertEqual(0, completed.returncode, completed.stderr)
        self.assertIn("React phase contracts passed", completed.stdout)

    def test_all_supported_cells_bind_original_and_exact_bundle(self):
        for host in ("server", "wasm", "hosted-wasm", "custom-elements"):
            for framework in browser.FRAMEWORKS:
                with self.subTest(host=host, framework=framework):
                    request, original, assets, record = phase_fixture(host, framework)
                    self.assertEqual(record, self.validate(record, request=request, original=original, assets=assets))
                    self.assertEqual(record["source_browser_sha256"], react.source_bindings(request, original)["source_browser_sha256"])

    def test_canonical_source_hash_includes_original_child_without_mutation(self):
        before = copy.deepcopy(self.original)
        exact = (json.dumps(self.original, indent=2, sort_keys=True) + "\n").encode()
        self.assertEqual(hashlib.sha256(exact).hexdigest(), react.browser_receipt_sha256(self.original))
        self.validate()
        self.assertEqual(before, self.original)

    def test_original_failure_or_missing_x6_identity_cleanup_prevents_preflight(self):
        for name in ("authentication", "x6_edit_save_reload", "identity_preserved", "cleanup"):
            original = copy.deepcopy(self.original)
            next(item for item in original["assertions"] if item["name"] == name)["passed"] = False
            with self.subTest(name=name), self.assertRaises(ValueError):
                react.source_bindings(self.request, original)
        for name, value in (("result", "failed"), ("root_id_sha256", None), ("value_sha256", react.AFTER_VALUE_SHA256)):
            original = copy.deepcopy(self.original)
            if name == "result":
                original[name] = value
            elif value is None:
                original["proof"].pop(name)
            else:
                original["proof"][name] = value
            with self.subTest(name=name), self.assertRaises(ValueError):
                react.source_bindings(self.request, original)

    def test_wrong_cell_mode_and_baseline_are_rejected(self):
        for request in (dict(asdict(self.request), designer_mode="unknown"), replace(self.request, version="3.9.0", designer_mode="x6")):
            with self.subTest(request=request), self.assertRaises(ValueError):
                react.source_bindings(request, self.original)
        record = copy.deepcopy(self.record)
        record["cell"]["host"] = "wasm"
        with self.assertRaises(ValueError):
            self.validate(record)

    def test_custom_embedding_requires_complete_original_native_callbacks_and_instance_viewer(self):
        request, original, assets, record = phase_fixture("custom-elements")
        self.validate(record, request=request, original=original, assets=assets)
        for prefix in (2, 5, 6, 7):
            with self.subTest(prefix=prefix):
                partial = copy.deepcopy(original)
                native = partial["proof"]["embedding"]
                native["checks"] = {name: index < prefix for index, name in enumerate(embedding.CHECKS)}
                for name, check in embedding.HASH_CHECKS.items():
                    if not native["checks"][check]:
                        native.pop(name, None)
                for assertion in partial["assertions"]:
                    if assertion["name"] == "native_callbacks":
                        assertion["passed"] = prefix >= 6
                    if assertion["name"] == "instance_list_viewer":
                        assertion["passed"] = False
                with self.assertRaisesRegex(ValueError, "lacks complete native embedding"):
                    react.source_bindings(request, partial)

    def test_custom_phase_revalidates_exact_source_bundle_and_identity_guards(self):
        request, original, assets, record = phase_fixture("custom-elements")
        before = copy.deepcopy(original)
        for mutate in (
            lambda value: value.update(source_browser_sha256="0" * 64),
            lambda value: value["hashes"].update(definition_id_sha256=hashed("wrong")),
            lambda value: value["hashes"].update(activity_id_sha256=hashed("wrong")),
            lambda value: value["hashes"].update(root_id_sha256=hashed("wrong")),
            lambda value: value["hashes"].update(after_value_sha256=hashed("wrong")),
            lambda value: value["resources"][0].update(owner="fixture"),
            lambda value: value["resources"][0].update(requested=False),
        ):
            with self.subTest(mutation=mutate):
                changed = copy.deepcopy(record)
                mutate(changed)
                with self.assertRaises(ValueError):
                    self.validate(changed, request=request, original=original, assets=assets)
        self.assertEqual(before, original)

    def test_custom_summary_requires_complete_crossbound_original_embedding(self):
        request, original, assets, record = phase_fixture("custom-elements")
        summary = react.summarize_react_phase(self.validate(record, request=request, original=original, assets=assets))
        key = browser.identity(asdict(request))
        react.validate_react_phase_summary(summary, True, original["proof"], key)
        for name in ("embedding", "definition_id_sha256", "activity_id_sha256", "instance_id_sha256"):
            with self.subTest(field=name):
                proof = copy.deepcopy(original["proof"])
                proof.pop(name)
                with self.assertRaises(ValueError):
                    react.validate_react_phase_summary(summary, True, proof, key)

    def test_custom_private_transport_accepts_original_x6_request_and_fresh_react_handle(self):
        request, original, assets, record = phase_fixture("custom-elements")
        handle = SimpleNamespace(**vars(self.handle))
        handle.request = request
        original_request = replace(request, designer_mode="x6")
        before = copy.deepcopy(original)
        with patch.object(browser, "_run_browser_process", return_value=SimpleNamespace(stdout=json.dumps(record), returncode=0)) as child:
            self.assertEqual(record, react.run_react_phase(handle, original_request, assets, original))
        payload = json.loads(child.call_args.kwargs["input"])
        self.assertEqual("custom-elements", payload["request"]["host"])
        self.assertEqual("react-flow", payload["request"]["designer_mode"])
        self.assertEqual(react.browser_receipt_sha256(original), payload["react_phase"]["source_browser_sha256"])
        self.assertEqual(before, original)

    def test_failed_partial_and_unlaunched_receipts_are_retained_without_pass(self):
        record = copy.deepcopy(self.record)
        record.update(result="failed", failure_category=react.FAILURE, hashes={}, resources=[], checks=dict.fromkeys(react.CHECKS, False))
        record["checks"]["cleanup"] = True
        self.validate(record)
        record["checks"].update(authentication=True, source_binding=True)
        record["hashes"] = {name: self.record["hashes"][name] for name in react.BEFORE_HASHES}
        self.validate(record)
        record.update(browser_version=None, hashes={}, checks=dict.fromkeys(react.CHECKS, False))
        self.validate(record)
        record["checks"]["cleanup"] = True
        with self.assertRaises(ValueError):
            self.validate(record)

    def test_exact_keys_and_boolean_checks_reject_unsafe_or_missing_fields(self):
        for name, value in (("password", "private"), ("schema", True), ("browser_version", "raw-error")):
            record = copy.deepcopy(self.record)
            record[name] = value
            with self.subTest(name=name), self.assertRaises(ValueError):
                self.validate(record)
        for name in react.CHECKS:
            record = copy.deepcopy(self.record)
            record["checks"].pop(name)
            with self.subTest(name=name), self.assertRaises(ValueError):
                self.validate(record)
        record = copy.deepcopy(self.record)
        record["checks"]["saved"] = 1
        with self.assertRaises(ValueError):
            self.validate(record)

    def test_stage_dependencies_and_complete_claim_fail_closed(self):
        for name in react.CHECKS:
            record = copy.deepcopy(self.record)
            record["checks"][name] = False
            with self.subTest(name=name), self.assertRaises(ValueError):
                self.validate(record)

    def test_source_before_after_hashes_must_match_exact_expectations(self):
        for name in ("source_browser_sha256", *react.HASHES):
            record = copy.deepcopy(self.record)
            if name == "source_browser_sha256":
                record[name] = "f" * 64
            else:
                record["hashes"][name] = "f" * 64
            with self.subTest(name=name), self.assertRaises(ValueError):
                self.validate(record)
        record = copy.deepcopy(self.record)
        record["hashes"]["after_value_sha256"] = record["hashes"]["before_value_sha256"]
        with self.assertRaises(ValueError):
            self.validate(record)

    def test_bundle_claim_requires_exact_owned_requested_successful_bytes_and_metadata(self):
        for name, value in (("owner", "fixture"), ("requested", False), ("status", 404), ("sha256", "f" * 64), ("bytes", 1),
                            ("content_type", "application/javascript"), ("path", "/_content/other.js")):
            record = copy.deepcopy(self.record)
            record["resources"][0][name] = value
            with self.subTest(name=name), self.assertRaises(ValueError):
                self.validate(record)
        for assets in ([], self.assets * 2, [dict(self.assets[0], owner="fixture")], [dict(self.assets[0], path="/tmp/private")]):
            with self.subTest(assets=assets), self.assertRaises((ValueError, RuntimeError)):
                self.validate(assets=assets)

    def test_failed_resource_observation_cannot_be_promoted(self):
        record = copy.deepcopy(self.record)
        record.update(result="failed", failure_category=react.FAILURE)
        record["resources"][0]["status"] = 404
        record["checks"]["react_bundle"] = False
        self.validate(record)
        record.update(result="passed", failure_category=None)
        with self.assertRaises(ValueError):
            self.validate(record)

    def test_summary_binds_exact_phase_hash_and_parent_proof(self):
        summary = react.summarize_react_phase(self.validate())
        self.assertEqual(react.browser_receipt_sha256(self.record), summary["phase_receipt_sha256"])
        key = browser.identity(asdict(self.request))
        react.validate_react_phase_summary(summary, True, self.original["proof"], key)
        for name in react.BEFORE_HASHES:
            changed = copy.deepcopy(summary)
            changed["hashes"][name] = "f" * 64
            with self.subTest(name=name), self.assertRaises(ValueError):
                react.validate_react_phase_summary(changed, True, self.original["proof"], key)
        summary["checks"]["cleanup"] = False
        with self.assertRaises(ValueError):
            react.validate_react_phase_summary(summary, True, self.original["proof"], key)
        react.validate_react_phase_summary(summary, False, self.original["proof"], key)

    def test_private_stdin_only_transport_preserves_original_and_exit_contract(self):
        before = copy.deepcopy(self.original)
        completed = SimpleNamespace(stdout=json.dumps(self.record), returncode=0)
        with patch.object(browser, "_run_browser_process", return_value=completed) as child:
            self.assertEqual(self.record, react.run_react_phase(self.handle, self.request, self.assets, self.original))
        call = child.call_args
        payload = json.loads(call.kwargs["input"])
        self.assertEqual("private-password", payload["password"])
        self.assertEqual("react-flow", payload["phase"])
        self.assertEqual(react.source_bindings(self.request, self.original), payload["react_phase"])
        self.assertNotIn("private-password", str(call.args))
        self.assertEqual(240, call.kwargs["timeout"])
        self.assertEqual(before, self.original)
        completed.returncode = 1
        with patch.object(browser, "_run_browser_process", return_value=completed), self.assertRaises(ValueError):
            react.run_react_phase(self.handle, self.request, self.assets, self.original)

    def test_original_x6_request_normalizes_only_mode_and_requires_react_handle(self):
        original_request = replace(self.request, designer_mode="x6")
        self.assertEqual(react.source_bindings(self.request, self.original), react.source_bindings(original_request, self.original))
        completed = SimpleNamespace(stdout=json.dumps(self.record), returncode=0)
        with patch.object(browser, "_run_browser_process", return_value=completed) as child:
            react.run_react_phase(self.handle, original_request, self.assets, self.original)
        self.assertEqual("react-flow", json.loads(child.call_args.kwargs["input"])["request"]["designer_mode"])
        self.handle.request = original_request
        with patch.object(browser, "_run_browser_process") as child, self.assertRaises(ValueError):
            react.run_react_phase(self.handle, original_request, self.assets, self.original)
        child.assert_not_called()

    def test_failed_child_returns_sanitized_partial_and_cleanup_uncertainty_propagates(self):
        partial = copy.deepcopy(self.record)
        partial.update(result="failed", failure_category=react.FAILURE, checks=dict.fromkeys(react.CHECKS, False), hashes={}, resources=[])
        with patch.object(browser, "_run_browser_process", return_value=SimpleNamespace(stdout=json.dumps(partial), returncode=1)):
            self.assertEqual(partial, react.run_react_phase(self.handle, self.request, self.assets, self.original))
        with patch.object(browser, "_run_browser_process", side_effect=browser.BrowserCleanupUnverified("safe")):
            with self.assertRaises(browser.BrowserCleanupUnverified):
                react.run_react_phase(self.handle, self.request, self.assets, self.original)

    def test_malformed_child_and_mismatched_handle_never_run_as_success(self):
        for stdout in ("not-json", json.dumps({"password": "private"}), '{"schema":1,"schema":1}'):
            with patch.object(browser, "_run_browser_process", return_value=SimpleNamespace(stdout=stdout, returncode=0)), self.assertRaises(ValueError):
                react.run_react_phase(self.handle, self.request, self.assets, self.original)
        self.handle.request = replace(self.request, framework="net9.0")
        with patch.object(browser, "_run_browser_process") as child, self.assertRaises(ValueError):
            react.run_react_phase(self.handle, self.request, self.assets, self.original)
        child.assert_not_called()


if __name__ == "__main__":
    unittest.main()
