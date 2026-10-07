"""Synthetic receipt contracts only; these fixtures are not native browser evidence."""
from __future__ import annotations

import copy
import hashlib
import unittest

import paired_package_optional_features as probes


def sha(value):
    return hashlib.sha256(value.encode()).hexdigest()


def native_request(endpoint, status=200, *, source="browser-native", after_action=True, after_ack=False, items=False):
    return {"endpoint": endpoint, "method": probes.ENDPOINT_METHODS[endpoint], "source": source, "status": status,
            "transport_failed": status is None, "after_action": after_action, "after_disconnect_ack": after_ack,
            "body": None if status is None else {"bytes": 0 if status == 403 else 2,
                "sha256": sha("") if status == 403 else sha("{}"), "sensitive_items_present": items}}


def fixture(scenario, host="wasm", framework="net8.0"):
    profile = probes.optional_feature_profile(scenario)
    cell = {"version": "3.10.0", "framework": framework, "host": host}
    request = dict(cell, **copy.deepcopy(profile), designer_mode="x6", route_prefix="")
    ui = dict.fromkeys(probes.UI_FLAGS, None)
    ui.update(secret_navigation_visible=None if host == "custom-elements" else scenario not in {"without-secrets", "deny-secrets"},
              context_heading_visible=scenario not in {"without-workflow-contexts", "deny-workflow-contexts"},
              synthetic_checkbox_visible=scenario not in {"without-workflow-contexts", "deny-workflow-contexts"},
              authorization_guidance_visible=scenario.startswith("deny-"),
              error_visible=scenario.startswith("deny-") or scenario == "disconnect",
              editor_visible=scenario != "deny-workflow-contexts", server_circuit_closed=False if host == "server" else None,
              page_closed=False, page_error_count=0)
    rows = []
    if scenario == "deny-workflow-contexts":
        rows.append(native_request("workflow-context-descriptors", 403))
    else:
        ui["secret_syntax_visible"] = scenario != "without-secrets"
        if scenario != "without-workflow-contexts":
            rows.append(native_request("workflow-context-descriptors", items=True, after_action=False))
        if scenario != "without-secrets":
            ui.update(picker_empty=True, inline_create_visible=scenario == "without-workflow-contexts")
            if scenario == "deny-secrets":
                rows.append(native_request("secrets-descriptors", 403))
            elif scenario == "without-workflow-contexts":
                rows.extend([native_request("secrets-descriptors", items=True), native_request("secrets-picker")])
            elif host != "server":
                rows.append(native_request("secrets-descriptors", None, after_ack=True))
    for row in rows:
        row["source"] = "backend-native" if host == "server" else "browser-native"
    receipt = {"schema": 1, "cell": cell, "scenario": scenario, **copy.deepcopy(profile),
               "checks": dict.fromkeys(probes.CHECKS, True), "hashes": {"probe_definition_id_sha256": sha("separate probe")},
               "ui": ui, "requests": [] if host == "server" else rows,
               "disconnect": dict.fromkeys(("child_ready", "parent_acknowledged", "native_action_after_ack"), True)
                             if scenario == "disconnect" else None,
               "failure_category": None}
    options = {"parent_observations": rows if host == "server" else [],
               "parent_disconnect": dict.fromkeys(probes.DISCONNECT_CHECKS, True) if scenario == "disconnect" else None,
               "canonical_definition_id_sha256": sha("canonical")}
    return receipt, request, options


class OptionalFeatureContracts(unittest.TestCase):
    def validate(self, data):
        receipt, request, options = data
        return probes.validate_optional_feature_receipt(receipt, request, receipt["scenario"], **options)

    def test_all_sixty_candidate_probes_preserve_observed_context_defect(self):
        for scenario in probes.SCENARIOS:
            for host in probes.HOSTS:
                for framework in probes.FRAMEWORKS:
                    with self.subTest(scenario=scenario, host=host, framework=framework):
                        result = self.validate(fixture(scenario, host, framework))
                        self.assertTrue(result["observation_complete"])
                        self.assertEqual(result["acceptance"], scenario != "deny-workflow-contexts")
                        self.assertEqual(result["defects"], ["editor_unavailable"] if scenario == "deny-workflow-contexts" else [])
                        self.assertEqual(result["native_transport_observed"], scenario == "disconnect" and host != "server")

    def test_complete_coverage_cannot_drop_duplicate_or_replace_a_cell(self):
        keys = sorted(probes.expected_optional_feature_keys())
        probes.validate_optional_feature_coverage(keys)
        for invalid in (keys[:-1], keys + keys[:1], [*keys[:-1], ("3.9.0", *keys[-1][1:])], [list(key) for key in keys]):
            with self.subTest(keys=len(invalid)), self.assertRaises(ValueError):
                probes.validate_optional_feature_coverage(invalid)

    def test_named_profiles_are_exact_and_reject_arbitrary_grants(self):
        for scenario in probes.SCENARIOS:
            receipt, request, options = fixture(scenario)
            for field, value in (("backend_features", []), ("backend_features", ["secrets", "workflow-contexts"]),
                                 ("permission_profile", "denied"), ("permission_grants", ["*"]),
                                 ("version", "3.9.0"), ("designer_mode", "react-flow"), ("route_prefix", "unsafe/route")):
                with self.subTest(scenario=scenario, field=field), self.assertRaises(ValueError):
                    self.validate((receipt, dict(request, **{field: value}), options))

    def test_hosted_route_and_tuple_features_are_supported(self):
        data = fixture("without-secrets", "hosted-wasm")
        data[1].update(route_prefix="studio-prefix", backend_features=tuple(data[1]["backend_features"]))
        self.assertTrue(self.validate(data)["acceptance"])

    def test_denied_context_failure_can_precede_return_of_created_identity(self):
        data = fixture("deny-workflow-contexts")
        data[0]["checks"]["native_workflow_created"] = False
        data[0]["hashes"] = {}
        result = self.validate(data)
        self.assertTrue(result["observation_complete"])
        self.assertFalse(result["acceptance"])

    def test_403_alone_does_not_claim_graceful_denial_or_payload_confidentiality(self):
        for scenario in ("deny-secrets", "deny-workflow-contexts"):
            for body in (None, {"bytes": 2, "sha256": sha("{}"), "sensitive_items_present": None}):
                data = fixture(scenario)
                data[0]["requests"][-1]["body"] = body
                result = self.validate(data)
                self.assertFalse(result["observation_complete"])
                self.assertFalse(result["acceptance"])
                self.assertIn("descriptor_payload_unverified_or_exposed", result["defects"])

    def test_payload_exposure_wrong_status_and_illegal_picker_post_are_retained_as_defects(self):
        mutations = [
            (lambda r: r["requests"][-1]["body"].update(bytes=2, sha256=sha("[]"), sensitive_items_present=True), "descriptor_payload_unverified_or_exposed"),
            (lambda r: r["requests"][-1].update(status=200), "secret_denial_not_observed"),
            (lambda r: r["requests"].append(native_request("secrets-picker", 403)), "picker_post_after_descriptor_denial"),
            (lambda r: r["ui"].update(inline_create_visible=True), "unexpected_inline_create"),
            (lambda r: r["ui"].update(secret_navigation_visible=True), "unexpected_secret_navigation"),
        ]
        for mutate, defect in mutations:
            with self.subTest(defect=defect):
                data = fixture("deny-secrets")
                mutate(data[0])
                result = self.validate(data)
                self.assertFalse(result["acceptance"])
                self.assertIn(defect, result["defects"])

    def test_observed_page_circuit_and_editor_failures_never_pass(self):
        for host, ui in (("wasm", {"page_error_count": 1}), ("server", {"server_circuit_closed": True}),
                         ("custom-elements", {"page_closed": True}), ("wasm", {"editor_visible": False})):
            with self.subTest(host=host, ui=ui):
                data = fixture("deny-secrets", host)
                data[0]["ui"].update(ui)
                self.assertFalse(self.validate(data)["acceptance"])

    def test_direct_reads_and_wrong_host_surfaces_cannot_supply_native_http_evidence(self):
        data = fixture("deny-secrets")
        data[0]["requests"][-1]["source"] = "backend-readonly"
        with self.assertRaises(ValueError):
            self.validate(data)
        data = fixture("deny-secrets", "server")
        data[0]["requests"] = [native_request("secrets-descriptors", 403)]
        with self.assertRaises(ValueError):
            self.validate(data)
        data = fixture("deny-secrets")
        data[2]["parent_observations"] = [native_request("secrets-descriptors", 403, source="backend-native")]
        with self.assertRaises(ValueError):
            self.validate(data)

    def test_disconnect_requires_parent_owned_stop_live_studio_and_ordered_ack(self):
        for field in probes.DISCONNECT_CHECKS:
            with self.subTest(parent=field):
                data = fixture("disconnect")
                data[2]["parent_disconnect"][field] = False
                if field == "child_ready_observed":
                    with self.assertRaises(ValueError):
                        self.validate(data)
                else:
                    self.assertFalse(self.validate(data)["acceptance"])
        data = fixture("disconnect")
        data[2]["parent_disconnect"] = None
        self.assertFalse(self.validate(data)["observation_complete"])
        for field in ("child_ready", "parent_acknowledged", "native_action_after_ack"):
            data = fixture("disconnect")
            data[0]["disconnect"][field] = False
            with self.subTest(child=field), self.assertRaises(ValueError):
                self.validate(data)

    def test_disconnect_http_status_is_null_and_no_server_transport_is_invented(self):
        data = fixture("disconnect")
        data[0]["requests"][-1]["status"] = 503
        with self.assertRaises(ValueError):
            self.validate(data)
        data = fixture("disconnect", "server")
        result = self.validate(data)
        self.assertTrue(result["acceptance"])
        self.assertFalse(result["native_transport_observed"])
        data[2]["parent_observations"].append(native_request("secrets-descriptors", 200, source="backend-native", after_ack=True))
        self.assertFalse(self.validate(data)["acceptance"])
        data = fixture("disconnect", "server")
        data[2]["parent_observations"].append(native_request("secrets-descriptors", None, source="backend-native", after_ack=True))
        with self.assertRaises(ValueError):
            self.validate(data)

    def test_unsafe_fields_raw_errors_secret_data_and_forged_pass_claims_are_rejected(self):
        mutations = [lambda r: r.update(result="passed"), lambda r: r.update(error="raw exception"),
                     lambda r: r["hashes"].update(secret_value_sha256=sha("private")),
                     lambda r: r["ui"].update(error_text="raw"), lambda r: r["requests"][-1].update(url="http://private"),
                     lambda r: r["requests"][-1]["body"].update(raw_body="private"),
                     lambda r: r["requests"][-1].update(status=True),
                     lambda r: r["checks"].update(native_action=1), lambda r: r["ui"].update(page_error_count=True),
                     lambda r: r["hashes"].update(probe_definition_id_sha256="raw id")]
        for index, mutate in enumerate(mutations):
            with self.subTest(mutation=index):
                data = fixture("deny-secrets")
                mutate(data[0])
                with self.assertRaises(ValueError):
                    self.validate(data)

    def test_missing_observation_identity_and_canonical_reuse_cannot_pass(self):
        data = fixture("deny-secrets")
        data[0]["checks"]["observation_completed"] = False
        data[0]["failure_category"] = "probe_execution_failed"
        self.assertFalse(self.validate(data)["acceptance"])
        data = fixture("deny-secrets")
        data[0]["hashes"]["probe_definition_id_sha256"] = data[2]["canonical_definition_id_sha256"]
        with self.assertRaises(ValueError):
            self.validate(data)
        data = fixture("deny-secrets")
        data[0]["requests"] = []
        self.assertFalse(self.validate(data)["observation_complete"])

    def test_empty_body_hash_size_and_classification_are_bound(self):
        for update in ({"bytes": 1}, {"sha256": sha("nonempty")}, {"sensitive_items_present": None},
                       {"sensitive_items_present": True}):
            data = fixture("deny-secrets")
            data[0]["requests"][-1]["body"].update(update)
            with self.subTest(update=update), self.assertRaises(ValueError):
                self.validate(data)

    def test_empty_workflow_identity_hash_is_rejected(self):
        data = fixture("deny-secrets")
        data[0]["hashes"]["probe_definition_id_sha256"] = sha("")
        with self.assertRaises(ValueError):
            self.validate(data)


if __name__ == "__main__":
    unittest.main()
