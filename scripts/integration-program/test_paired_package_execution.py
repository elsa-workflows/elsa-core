import copy
from contextlib import contextmanager
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

import paired_package_execution as execution
from test_paired_package_released_documents import fixture_identity, write_released_fixture
from test_paired_package_browser_matrix import reopen_row


class ReleasedInputsContracts(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.key = ("3.10.0", "net10.0", "server")
        self.identity = fixture_identity()
        for version in ("3.8.4", "3.9.0"):
            cell = (version, *self.key[1:])
            directory = self.root / "cells" / "-".join(cell)
            path = directory / "released-document.json"
            _, child = write_released_fixture(path, cell)
            evidence = dict(zip(("version", "framework", "host"), cell))
            evidence.update(result="passed", stage="complete", owned_process_cleanup=True,
                projects={"backend": {}}, resource_inventory={"assets": []}, runtime_readiness={"runtime": ".NET 10.0.8"},
                loaded_assemblies={"backend": []}, browser_resources={"requested": []})
            evidence["released_document"] = execution.documents.bind_released_document(path, cell, child, evidence, self.identity)
            (directory / "execution.json").write_text(json.dumps(evidence))
            (directory / "browser.json").write_text(json.dumps(child))

    def test_returns_only_matching_pair_with_exact_source_bindings(self):
        inputs = execution.released_document_inputs(self.key, self.root, self.identity)
        self.assertEqual(["3.8.4", "3.9.0"], [item["binding"]["document"]["source_cell"]["version"] for item in inputs])
        self.assertTrue(all(Path(item["private_path"]).is_file() for item in inputs))
        for key in (("3.10.0", "net9.0", "server"), ("3.10.0", "net10.0", "wasm"), ("3.9.0", "net10.0", "server")):
            with self.subTest(key=key), self.assertRaises((ValueError, RuntimeError, OSError)):
                execution.released_document_inputs(key, self.root, self.identity)

    def test_changed_fixture_child_source_or_missing_document_rejects(self):
        with self.assertRaises(ValueError):
            execution.released_document_inputs(self.key, self.root, {**self.identity, "fixture_source_commit": "c" * 40})
        directory = self.root / "cells/3.8.4-net10.0-server"
        for name, field, value in (("execution", "result", "failed"), ("execution", "projects", {"changed": []}),
                                   ("browser", "browser_version", "150.0")):
            path = directory / (name + ".json")
            raw = path.read_bytes()
            changed = json.loads(raw)
            changed[field] = value
            path.write_text(json.dumps(changed))
            with self.subTest(name=name, field=field), self.assertRaises(ValueError):
                execution.released_document_inputs(self.key, self.root, self.identity)
            path.write_bytes(raw)
        (directory / "released-document.json").unlink()
        with self.assertRaises((ValueError, RuntimeError, OSError)):
            execution.released_document_inputs(self.key, self.root, self.identity)


class ExecutionContracts(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.key = ("3.10.0", "net10.0", "server")
        self.events = []
        self.record = {"version": self.key[0], "framework": self.key[1], "host": self.key[2],
                       "result": "passed", "resources": [],
                       "proof": {"baseline_reopens": [reopen_row(version) for version in execution.documents.TOOL_VERSIONS]},
                       "browser_version": "149.0.7827.55",
                       "assertions": [{"name": name, "passed": True, "reason_category": None}
                                      for name in execution.browser.required_assertions(dict(zip(("version", "framework", "host"), self.key)))]}
        self.layout = SimpleNamespace(request=execution.hosts.CellRequest("server", "net10.0", "3.10.0"),
                                      project_paths={"backend": self.root / "backend.csproj", "server": self.root / "server.csproj"})

    def patch(self, owner, name, **kwargs):
        context = patch.object(owner, name, **kwargs)
        self.addCleanup(context.stop)
        return context.start()

    def readiness(self, request=None):
        request = request or self.layout.request
        features = ["Elsa.Identity", "Elsa.DefaultAuthentication", "Elsa.WorkflowManagement", "Elsa.WorkflowRuntime",
                    "Elsa.WorkflowsApi", "Elsa.EFCoreWorkflowDefinitionPersistence", "Elsa.EFCoreWorkflowInstancePersistence",
                    "Elsa.EFCoreWorkflowRuntimePersistence",
                    "Elsa.JavaScript"]
        if request.version != "3.8.4":
            features.extend(["Elsa.Bpmn", "Elsa.BpmnInterchange"])
        if "workflow-contexts" in request.backend_features:
            features.append("Elsa.WorkflowContexts")
        if "secrets" in request.backend_features:
            features.extend(["Elsa.Secrets", "Elsa.EFCoreSecretsPersistence"])
        return {"schema": 1, "framework": ".NETCoreApp,Version=v" + request.framework.removeprefix("net"),
                "runtime": ".NET " + request.framework.removeprefix("net") + ".8", "auth_mode": "ElsaIdentity",
                "permission_profile": request.permission_profile, "permission_grants": list(execution.hosts.permission_grants(request)),
                "workflow_contexts_enabled": "workflow-contexts" in request.backend_features,
                "secrets_enabled": "secrets" in request.backend_features, "features": features}

    def pipeline(self):
        def materialize(request, group, **kwargs):
            self.events.append(("materialize", group, kwargs["packages_root"]))
            self.layout.request = request
            return self.layout
        def validate(project):
            self.events.append(("validate", project))
            return {"project_assets_sha256": "a" * 64}
        @contextmanager
        def pair(layout, *, validate_project):
            self.events.append(("start",))
            for project in layout.project_paths.values():
                validate_project(project)
            try:
                yield SimpleNamespace(password="PRIVATE-MUST-NOT-BE-RETAINED", backend_url="http://127.0.0.1:4000/elsa/api")
            finally:
                self.events.append(("stop",))
        self.patch(execution, "evidence_gaps", return_value=[])
        self.patch(execution, "released_document_inputs", return_value=[])
        self.patch(execution.packages, "_validated_manifest", return_value=("3.10.0", "b" * 40, {"elsa": {"id": "Elsa"}}, {}, []))
        self.patch(execution.packages, "render_nuget_config", return_value="<configuration />")
        self.patch(execution.hosts, "materialize", side_effect=materialize)
        self.patch(execution.hosts, "build", side_effect=lambda _: self.events.append(("build",)) or [])
        self.patch(execution, "_project_validator", return_value=validate)
        self.patch(execution, "_resource_inventory", side_effect=lambda *_: self.events.append(("resources",)) or {"assets": []})
        self.patch(execution.hosts, "start_pair", side_effect=pair)
        self.patch(execution, "_json_request", side_effect=lambda *_args, **_kwargs:
                   self.events.append(("ready",)) or self.readiness())
        def run_browser(_handle, request, _resources, **_options):
            self.events.append(("browser",))
            record = copy.deepcopy(self.record)
            record.update(version=request.version, framework=request.framework, host=request.host)
            for row in record["proof"]["baseline_reopens"]:
                row["source_cell"].update(framework=request.framework, host=request.host)
            return record
        browser = self.patch(execution.browser, "run_browser", side_effect=run_browser)
        self.patch(execution, "_observe_loaded", side_effect=lambda *_: self.events.append(("observe",)) or {})
        self.patch(execution, "_verify_loaded", side_effect=lambda *_: self.events.append(("loaded",)) or {})
        self.patch(execution.resources, "verify_browser_resources", side_effect=lambda *_args, **_kw: self.events.append(("resource_check",)) or {})
        return browser

    def execute(self, key=None):
        return execution.execute_cell(key or self.key, private=self.root / "private", retained=self.root / "retained",
                  verified_root=self.root / "candidate", manifest={}, manifest_hash="a" * 64, sdk="10.0.300",
                  fixture_identity=fixture_identity())

    def released_pipeline(self):
        self.pipeline()
        def download(_handle, request, _resources, *, released_document_output):
            self.download, self.child = write_released_fixture(released_document_output,
                (request.version, request.framework, request.host))
            return self.child
        self.patch(execution.browser, "run_browser", side_effect=download)
        self.patch(execution, "_verify_loaded", return_value={"backend": {"package_assemblies": []}})
        self.patch(execution.resources, "verify_browser_resources", return_value={"verified_assets": []})

    def test_released_export_retains_exact_download_after_all_source_checks(self):
        self.released_pipeline()
        for version in ("3.8.4", "3.9.0"):
            key = (version, "net10.0", "server")
            self.assertEqual("passed", self.execute(key)["result"])
            root = self.root / "retained/cells" / "-".join(key)
            path = root / "released-document.json"
            self.assertEqual(self.download, path.read_bytes())
            self.assertEqual(0o600, path.stat().st_mode & 0o777)
            self.assertEqual(self.child, json.loads((root / "browser.json").read_text()))
            evidence = json.loads((root / "execution.json").read_text())
            self.assertEqual(evidence["released_document"], execution.documents.bind_released_document(
                path, key, self.child, evidence, fixture_identity()))

    def test_failed_source_check_never_retains_export(self):
        self.released_pipeline()
        self.patch(execution.resources, "verify_browser_resources", side_effect=ValueError("rejected"))
        with self.assertRaises(ValueError):
            self.execute(("3.9.0", "net10.0", "server"))
        self.assertFalse(list((self.root / "retained").rglob("released-document.json")))
        self.assertTrue(list((self.root / "private").rglob("released-document.json")))

    def test_missing_fixture_identity_never_promotes_export(self):
        self.released_pipeline()
        with self.assertRaises(ValueError):
            execution.execute_cell(("3.9.0", "net10.0", "server"), private=self.root / "private",
                retained=self.root / "retained", verified_root=self.root / "candidate", manifest={},
                manifest_hash="a" * 64, sdk="10.0.300")
        self.assertFalse(list((self.root / "retained").rglob("released-document.json")))

    def test_missing_released_inputs_fail_before_candidate_build_or_runtime(self):
        run_browser = self.pipeline()
        self.patch(execution, "released_document_inputs", side_effect=ValueError("Missing source evidence"))
        with self.assertRaises(ValueError):
            self.execute()
        self.assertEqual([], self.events)
        run_browser.assert_not_called()
        self.assertEqual("released_inputs", self.receipt()["stage"])
        self.assertEqual("failed", self.receipt()["result"])

    def receipt(self):
        return json.loads((self.root / "retained/cells/3.10.0-net10.0-server/execution.json").read_text())

    def test_full_selection_has_36_and_four_hosts_share_only_version_framework(self):
        selected = execution.selected_cells(None)
        self.assertEqual(execution.browser.MATRIX, set(selected))
        self.assertEqual(36, len(selected))
        self.assertEqual(4, len([k for k in selected if k[:2] == selected[0][:2]]))
        self.assertEqual([self.key], execution.selected_cells(",".join(self.key)))
        self.assertEqual([self.key], execution.selected_cells("/".join(self.key)))
        for invalid in ("", "3.10.0,net10.0,server,extra", "3.10.0,net10.0,unknown"):
            with self.subTest(invalid=invalid), self.assertRaises(ValueError):
                execution.selected_cells(invalid)

    def test_production_feature_policy_keeps_secrets_mandatory_for_all_candidate_cells(self):
        candidates, baselines = [], []
        for key in execution.selected_cells(None):
            request = execution.cell_request(key)
            self.assertEqual(key, (request.version, request.framework, request.host))
            self.assertEqual("full", request.permission_profile)
            if request.version == "3.10.0":
                candidates.append(key)
                self.assertEqual(("workflow-contexts", "secrets"), request.backend_features)
            else:
                baselines.append(key)
                self.assertEqual(("workflow-contexts",), request.backend_features)
        self.assertEqual(12, len(candidates))
        self.assertEqual(24, len(baselines))
        with self.assertRaisesRegex(ValueError, "Invalid package browser cell"):
            execution.cell_request(("unreviewed", "net10.0", "server"))

    def test_requested_features_are_retained_even_when_evidence_preflight_fails(self):
        self.patch(execution, "evidence_gaps", return_value=["unavailable_evidence"])
        for version in execution.browser.VERSIONS:
            with self.subTest(version=version), self.assertRaises(ValueError):
                self.execute((version, "net10.0", "server"))
            receipt = json.loads((self.root / "retained" / "cells" / f"{version}-net10.0-server" / "execution.json").read_text())
            expected = ["workflow-contexts", "secrets"] if version == "3.10.0" else ["workflow-contexts"]
            self.assertEqual(expected, receipt["requested_backend_features"])
            self.assertEqual("full", receipt["permission_profile"])
            self.assertEqual("candidate_representative_features" if version == "3.10.0" else
                             "released_shell_editor_export_contexts_only", receipt["feature_policy"])
            self.assertEqual("failed", receipt["result"])

    def test_valid_pipeline_verifies_before_start_and_retains_no_private_handle(self):
        self.pipeline()
        self.assertEqual("passed", self.execute()["result"])
        stages = [event[0] for event in self.events]
        self.assertLess(stages.index("validate"), stages.index("start"))
        self.assertLess(stages.index("start"), stages.index("ready"))
        self.assertLess(stages.index("ready"), stages.index("browser"))
        self.assertLess(stages.index("loaded"), stages.index("stop"))
        self.assertLess(stages.index("stop"), stages.index("resource_check"))
        self.assertTrue(self.receipt()["owned_process_cleanup"])
        self.assertEqual(sorted(self.readiness()["features"]), self.receipt()["runtime_readiness"]["features"])
        for path in (self.root / "retained").rglob("*.json"):
            self.assertNotIn("PRIVATE", path.read_text())

    def test_readiness_validates_profiles_features_and_frameworks_without_pinning_entire_catalog(self):
        for version in execution.browser.VERSIONS:
            for framework in execution.browser.FRAMEWORKS:
                for profile in execution.hosts.PERMISSION_PROFILES:
                    with self.subTest(version=version, framework=framework, profile=profile):
                        request = execution.hosts.CellRequest("server", framework, version,
                            backend_features=execution.cell_request((version, framework, "server")).backend_features,
                            permission_profile=profile)
                        value = self.readiness(request)
                        value["features"].append("Elsa.OtherInstalledFeature")
                        actual = execution.verify_runtime_readiness(value, request)
                        self.assertEqual(sorted(value["features"]), actual["features"])
                        self.assertEqual(value["permission_grants"], actual["permission_grants"])

    def test_versioned_bpmn_registration_must_match_available_backend_packages(self):
        bpmn = {"Elsa.Bpmn", "Elsa.BpmnInterchange"}
        for version in execution.browser.VERSIONS:
            request = execution.cell_request((version, "net10.0", "server"))
            original = self.readiness(request)
            for name in sorted(bpmn):
                changed = original | {"features": original["features"] + [name] if version == "3.8.4" else
                                      [feature for feature in original["features"] if feature != name]}
                with self.subTest(version=version, name=name), self.assertRaisesRegex(ValueError, "BPMN registration"):
                    execution.verify_runtime_readiness(changed, request)

    def test_observed_39_readiness_shape_accepts_specific_definition_and_instance_persistence_features(self):
        # Sanitized actual fixture 117a68 metadata; original observation hash:
        # 25ca09d6715e1f4ce6ad8c8c57926fea1542b433809cfb9cdb9e385265182d5b.
        observed = {"schema": 1, "framework": ".NETCoreApp,Version=v10.0", "runtime": ".NET 10.0.8",
                    "auth_mode": "ElsaIdentity", "permission_profile": "full", "permission_grants": ["*"],
                    "workflow_contexts_enabled": True, "secrets_enabled": False,
                    "features": ["Elsa." + name for name in (
                        "Mediator SystemClock Expressions DefaultFormatters Multitenancy CommitStrategies Workflows Flowchart "
                        "WorkflowRuntime DefaultWorkflowRuntime StringCompression MemoryCache WorkflowDefinitions WorkflowInstances "
                        "WorkflowManagement Elsa App Identity DefaultAuthentication WorkflowManagementPersistence "
                        "EFCoreWorkflowDefinitionPersistence EFCoreWorkflowInstancePersistence EFCoreWorkflowRuntimePersistence "
                        "KeyValue WorkflowsApi SasTokens JavaScript BpmnInterchange Bpmn WorkflowContexts WorkflowContextsJavaScript").split()]}
        request = execution.cell_request(("3.9.0", "net10.0", "server"))
        actual = execution.verify_runtime_readiness(observed, request)
        self.assertEqual(sorted(observed["features"]), actual["features"])
        self.assertFalse(actual["secrets_enabled"])
        self.assertNotIn("Elsa.Secrets", actual["features"])
        self.assertNotIn("Elsa.EFCoreWorkflowManagementPersistence", actual["features"])

    def test_runtime_mismatches_fail_before_browser_and_still_clean_up_without_retaining_raw_response(self):
        child = self.pipeline()
        original = self.readiness()
        mutations = [
            {"permission_profile": "denied"}, {"permission_grants": ["*"] + ["PRIVATE-GRANT"]},
            {"permission_grants": []}, {"workflow_contexts_enabled": False}, {"secrets_enabled": False},
            {"framework": ".NETCoreApp,Version=v9.0"}, {"runtime": "PRIVATE-RUNTIME"}, {"schema": True},
            {"auth_mode": "Unauthenticated"}, {"features": [name for name in original["features"] if name != "Elsa.Secrets"]},
            {"features": [name for name in original["features"] if name != "Elsa.EFCoreSecretsPersistence"]},
            {"features": [name for name in original["features"] if name != "Elsa.WorkflowsApi"]},
            {"features": [name for name in original["features"] if name != "Elsa.Bpmn"]},
            {"features": [name for name in original["features"] if name != "Elsa.BpmnInterchange"]},
            {"features": original["features"] + ["/PRIVATE/PATH"]}, {"encryption_key": "PRIVATE-KEY"}]
        for index, mutation in enumerate(mutations):
            with self.subTest(mutation=list(mutation)):
                self.patch(execution, "_json_request", return_value=original | mutation)
                retained = self.root / f"failure-{index}"
                with self.assertRaises(ValueError):
                    execution.execute_cell(self.key, private=self.root / "private", retained=retained,
                        verified_root=self.root / "candidate", manifest={}, manifest_hash="a" * 64, sdk="10.0.300")
                receipt = json.loads((retained / "cells/3.10.0-net10.0-server/execution.json").read_text())
                self.assertEqual("runtime_readiness", receipt["stage"])
                self.assertTrue(receipt["owned_process_cleanup"])
                self.assertNotIn("runtime_readiness", receipt)
                self.assertNotIn("PRIVATE", json.dumps(receipt))
        child.assert_not_called()

    def test_unrequested_optional_features_and_absent_registration_are_rejected(self):
        baseline = execution.cell_request(("3.9.0", "net10.0", "server"))
        original = self.readiness(baseline)
        for mutation in ({"secrets_enabled": True}, {"secrets_enabled": 0},
                         {"features": original["features"] + ["Elsa.Secrets"]},
                         {"features": original["features"] + ["Elsa.EFCoreSecretsPersistence"]},
                         {"features": [name for name in original["features"] if name != "Elsa.WorkflowContexts"]}):
            with self.subTest(mutation=list(mutation)), self.assertRaises(ValueError):
                execution.verify_runtime_readiness(original | mutation, baseline)

    def test_browser_exception_stops_pair_and_does_not_retain_raw_error(self):
        child = self.pipeline()
        child.side_effect = RuntimeError("PRIVATE-BROWSER-ERROR")
        with self.assertRaisesRegex(ValueError, "execution or evidence"):
            self.execute()
        self.assertEqual("stop", self.events[-1][0])
        self.assertTrue(self.receipt()["owned_process_cleanup"])
        self.assertFalse((self.root / "retained/cells/3.10.0-net10.0-server/browser.json").exists())
        self.assertNotIn("PRIVATE", json.dumps(self.receipt()))

    def test_incomplete_browser_assertions_remain_failed_with_safe_receipt(self):
        self.pipeline()
        self.record["result"] = "incomplete"
        next(item for item in self.record["assertions"] if item["name"] == "x6_edit_save_reload").update(passed=False, reason_category="not_implemented")
        with self.assertRaises(ValueError):
            self.execute()
        record = json.loads((self.root / "retained/cells/3.10.0-net10.0-server/browser.json").read_text())
        self.assertEqual("incomplete", record["result"])
        self.assertFalse(next(item for item in record["assertions"] if item["name"] == "x6_edit_save_reload")["passed"])
        self.assertEqual("browser_contract", self.receipt()["stage"])
        self.assertEqual("failed", self.receipt()["result"])

    def test_verified_python_assertions_complete_cell_without_mutating_child_receipt(self):
        self.pipeline()
        self.record["result"] = "incomplete"
        for assertion in self.record["assertions"]:
            if assertion["name"] in {"package_provenance", "browser_resources"}:
                assertion.update(passed=False, reason_category="not_implemented")
        original = copy.deepcopy(self.record)
        combined = self.execute()
        self.assertEqual("passed", combined["result"])
        execution.browser.check_cell(combined)
        retained = json.loads((self.root / "retained/cells/3.10.0-net10.0-server/browser.json").read_text())
        self.assertEqual(original, retained)
        self.assertEqual(original, self.record)
        self.assertTrue(self.receipt()["owned_process_cleanup"])

    def test_failed_child_cannot_be_promoted_even_with_all_true_assertions(self):
        self.pipeline()
        self.record["result"] = "failed"
        self.record["failure_category"] = "browser_execution_or_validation_failed"
        with self.assertRaises(ValueError):
            self.execute()
        self.assertEqual("failed", self.receipt()["result"])

    def test_loaded_evidence_failure_still_stops_owned_pair(self):
        self.pipeline()
        self.patch(execution, "_verify_loaded", side_effect=RuntimeError("PRIVATE-PATH"))
        with self.assertRaises(ValueError):
            self.execute()
        self.assertEqual("stop", self.events[-1][0])
        self.assertEqual("loaded_assemblies", self.receipt()["stage"])
        self.assertTrue(self.receipt()["owned_process_cleanup"])
        self.assertNotIn("PRIVATE", json.dumps(self.receipt()))

    def test_cleanup_failure_never_claims_successful_owned_cleanup(self):
        self.pipeline()
        @contextmanager
        def broken_cleanup(_layout, **_kwargs):
            yield SimpleNamespace()
            raise RuntimeError("PRIVATE-CLEANUP-ERROR")
        self.patch(execution.hosts, "start_pair", side_effect=broken_cleanup)
        with self.assertRaises(ValueError):
            self.execute()
        self.assertNotIn("owned_process_cleanup", self.receipt())
        self.assertNotIn("PRIVATE", json.dumps(self.receipt()))

    def test_resource_failure_retains_safe_browser_without_granting_assertions(self):
        self.pipeline()
        next(item for item in self.record["assertions"] if item["name"] == "browser_resources").update(passed=False, reason_category="not_implemented")
        self.patch(execution.resources, "verify_browser_resources", side_effect=RuntimeError("PRIVATE-RESOURCE-PATH"))
        with self.assertRaises(ValueError):
            self.execute()
        record = json.loads((self.root / "retained/cells/3.10.0-net10.0-server/browser.json").read_text())
        self.assertFalse(next(item for item in record["assertions"] if item["name"] == "browser_resources")["passed"])
        self.assertEqual("browser_resources", self.receipt()["stage"])

    def test_unavailable_evidence_fails_before_materialize_or_build(self):
        with patch.object(execution.hosts, "materialize") as materialize:
            with self.assertRaises(ValueError):
                self.execute(("3.10.0", "net10.0", "wasm"))
            materialize.assert_not_called()
        receipt = json.loads((self.root / "retained/cells/3.10.0-net10.0-wasm/execution.json").read_text())
        self.assertEqual(["wasm_runtime_and_converter"], receipt["missing_evidence"])

    def test_group_cache_is_distinct_across_frameworks(self):
        self.pipeline()
        self.execute()
        self.execute(("3.10.0", "net9.0", "server"))
        materializations = [event for event in self.events if event[0] == "materialize"]
        self.assertNotEqual(materializations[0][1:], materializations[1][1:])
        self.assertTrue(all(cache == group / "packages" for _, group, cache in materializations))

    def test_invalid_child_private_fields_are_never_retained(self):
        self.pipeline()
        self.record["password"] = "PRIVATE-CHILD-DATA"
        with self.assertRaises(ValueError):
            self.execute()
        self.assertEqual("stop", self.events[-1][0])
        self.assertFalse((self.root / "retained/cells/3.10.0-net10.0-server/browser.json").exists())
        self.assertNotIn("PRIVATE", json.dumps(self.receipt()))

    def test_run_invokes_all_36_and_development_selection_never_accepts(self):
        def prepare(_inputs, destination, _retained, **_identity):
            destination.mkdir()
            (destination / "verified-artifacts.json").write_text("{}")
            return {"verified_artifacts_sha256": "a" * 64, "browser_execution": {}}
        def execute(key, **_kwargs):
            record = copy.deepcopy(self.record)
            record.update(zip(("version", "framework", "host"), key))
            record["proof"] = ({"baseline_reopens": [reopen_row(version, key[1], key[2])
                                for version in execution.documents.TOOL_VERSIONS]} if key[0] == "3.10.0" else {})
            record["assertions"] = [{"name": name, "passed": True, "reason_category": None}
                                     for name in execution.browser.required_assertions(record)]
            return record
        self.patch(execution.browser, "prepare_candidate", side_effect=prepare)
        self.patch(execution.subprocess, "check_output", return_value="10.0.300\n")
        cells = self.patch(execution, "execute_cell", side_effect=execute)
        for suffix, selected in (("full", None), ("development", ",".join(self.key))):
            ledger = execution.run(self.root / "inputs", self.root / ("candidate-" + suffix), self.root / suffix,
                                    fixture_source="a" * 40, cell=selected)
            self.assertEqual(selected is None, ledger["passed"])
            self.assertEqual(selected is None, ledger["complete_matrix"])
            self.assertEqual(36, len(ledger["cells"]))
        self.assertEqual(37, cells.call_count)

    def test_setup_failure_retains_36_not_run_and_never_builds(self):
        self.patch(execution.browser, "prepare_candidate", side_effect=RuntimeError("PRIVATE-INPUT-ERROR"))
        build = self.patch(execution.hosts, "build")
        with self.assertRaises(ValueError):
            execution.run(self.root / "inputs", self.root / "candidate", self.root / "output", fixture_source="a" * 40)
        build.assert_not_called()
        receipt = (self.root / "output/retained-evidence/matrix.json").read_text()
        ledger = json.loads(receipt)
        self.assertEqual(36, sum(cell["result"] == "not_run" for cell in ledger["cells"]))
        self.assertFalse(ledger["passed"])
        self.assertNotIn("PRIVATE", receipt)

    def test_command_logs_are_hashes_without_commands_or_absolute_paths(self):
        group = self.root / "group"
        log = group / "logs/restore.log"
        log.parent.mkdir(parents=True)
        log.write_text("PRIVATE-LOG-CONTENT")
        receipt = execution._command_receipts([{"command": ["PRIVATE-ARGV"], "exit_code": 0, "log": str(log)}], group)
        self.assertNotIn("PRIVATE", json.dumps(receipt))
        self.assertNotIn(str(self.root), json.dumps(receipt))
        reuse = {"stage": "reuse_verified_build", "project": "server", "project_assets_sha256": "a" * 64}
        self.assertEqual([reuse], execution._command_receipts([reuse], group))
        for mutation in ({"project": str(self.root / "server.csproj")}, {"project_assets_sha256": "PRIVATE"}):
            with self.subTest(mutation=mutation), self.assertRaises(ValueError):
                execution._command_receipts([{**reuse, **mutation}], group)


if __name__ == "__main__":
    unittest.main()
