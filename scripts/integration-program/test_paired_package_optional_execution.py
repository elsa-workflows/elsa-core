"""Synthetic orchestration contracts; no backend, Node or browser is launched."""
import copy
from contextlib import contextmanager
from pathlib import Path
import tempfile
import threading
from types import SimpleNamespace
import unittest
from unittest.mock import patch

import materialize_paired_package_hosts as hosts
import paired_package_execution as matrix
import paired_package_optional_execution as execution
import paired_package_optional_features as features
from paired_package_optional_endpoints import derive_backend_native_rows
from test_paired_package_optional_endpoints import assembly_inventory, observation, snapshot
from test_paired_package_optional_features import fixture, sha


def readiness(request):
    """Synthetic readiness shaped by the real profile validator, without HTTP."""
    installed = {"Elsa.Identity", "Elsa.DefaultAuthentication", "Elsa.WorkflowManagement", "Elsa.WorkflowRuntime",
                 "Elsa.WorkflowsApi", "Elsa.EFCoreWorkflowDefinitionPersistence", "Elsa.EFCoreWorkflowInstancePersistence",
                 "Elsa.EFCoreWorkflowRuntimePersistence", "Elsa.Bpmn", "Elsa.BpmnInterchange"}
    if "workflow-contexts" in request.backend_features:
        installed.add("Elsa.WorkflowContexts")
    if "secrets" in request.backend_features:
        installed.update({"Elsa.Secrets", "Elsa.EFCoreSecretsPersistence"})
    return {"schema": 1, "framework": ".NETCoreApp,Version=v" + request.framework.removeprefix("net"),
            "runtime": ".NET " + request.framework.removeprefix("net") + ".1", "auth_mode": "ElsaIdentity",
            "permission_profile": request.permission_profile, "permission_grants": list(hosts.permission_grants(request)),
            "workflow_contexts_enabled": "workflow-contexts" in request.backend_features,
            "secrets_enabled": "secrets" in request.backend_features, "features": sorted(installed)}


def complete_record(scenario, host="wasm", framework="net8.0", canonicalhash=sha("canonical"), *,
                    hypothetical_context_recovery=False):
    """Reusable synthetic retained record, including readiness and canonical owners.

    The default preserves the source-supported denied-context editor defect.
    Recovery is an explicit hypothetical case for testing a successful retention
    gate; it is never represented as actual released UI/browser evidence.
    """
    probe, _, options = fixture(scenario, host, framework)
    if hypothetical_context_recovery:
        if scenario != "deny-workflow-contexts":
            raise ValueError("Hypothetical recovery applies only to denied WorkflowContexts")
        probe["ui"]["editor_visible"] = True
    request = execution.profile_request(hosts.CellRequest(host, framework, "3.10.0"), scenario)
    owners = execution.endpoint_assemblies({"backend": {"package_assemblies": assembly_inventory()}}, request)
    child = {"schema": 1, "phase": "optional-feature-probe", "cell": probe["cell"], "probe": probe,
             "browser_version": "140.0.0.1", "cleanup_verified": True,
             "browser_alive_after_stop": True if scenario == "disconnect" else None, "failure_category": None}
    record = {"schema": 1, "cell": probe["cell"], "scenario": scenario,
              "profile": features.optional_feature_profile(scenario), "stage": "complete", "result": "failed",
              "owned_process_cleanup": True, "failure_category": "optional_acceptance_failed",
              "runtime_readiness": readiness(request), "endpoint_assemblies": owners, "browser": child}
    parent_rows = []
    if host == "server":
        rows = [observation(index, row["endpoint"], status=row["status"], body_bytes=0 if row["status"] == 403 else 2)
                for index, row in enumerate(options["parent_observations"], 1)]
        prior = [row for row, generic in zip(rows, options["parent_observations"]) if not generic["after_action"]]
        record["endpoint_boundary"], record["endpoint_final"] = snapshot(*prior), snapshot(*rows)
        parent_rows = derive_backend_native_rows(record["endpoint_boundary"], record["endpoint_final"], owners,
                                                scenario=scenario)
    disconnect = options["parent_disconnect"]
    if disconnect is not None:
        record["parent_disconnect"] = disconnect
    record["assessment"] = features.validate_optional_feature_receipt(probe, request, scenario,
        parent_observations=parent_rows, parent_disconnect=disconnect,
        canonical_definition_id_sha256=canonicalhash)
    if record["assessment"]["acceptance"]:
        record.update(result="passed", failure_category=None)
    return record


class SyntheticRuntime:
    def __init__(self, events):
        self.events, self.closed, self.disconnected = events, False, False
        self.handle = SimpleNamespace(synthetic=True)

    def check_live_studio(self):
        if self.closed:
            raise RuntimeError("Synthetic owner is closed")

    def disconnect_backend(self):
        self.check_live_studio()
        if self.disconnected:
            raise RuntimeError("Synthetic stop is one-shot")
        self.events.append("stop")
        self.disconnected = True
        return {"owned_backend_stopped": True, "studio_alive_after_stop": True}


class OptionalExecutionContracts(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.events, self.layouts, self.runtimes = [], [], []
        self.cleanup_failure = False

    def layout(self, host="server", framework="net8.0"):
        group = self.root / host / framework
        return hosts.CellLayout(hosts.CellRequest(host, framework, "3.10.0"), group, {},
                                group / "canonical-runtime", group / "packages", "synthetic-sdk", {})

    @contextmanager
    def owner(self, layout, *, validate_project):
        self.assertTrue(layout.runtime_root.is_dir())
        self.assertEqual([], list(layout.runtime_root.iterdir()))
        self.layouts.append(layout)
        runtime = SyntheticRuntime(self.events)
        self.runtimes.append(runtime)
        self.events.append("enter")
        try:
            yield runtime
        finally:
            runtime.closed = True
            self.events.append("cleanup")
            if self.cleanup_failure:
                raise RuntimeError("Synthetic cleanup failure")

    def run_profile(self, scenario, host="server", framework="net8.0", *, child=None, browser=None, assemblies=None,
                    ready=None):
        expected = complete_record(scenario, host, framework)
        child = copy.deepcopy(expected["browser"] if child is None else child)
        snapshots = iter([expected.get("endpoint_boundary"), expected.get("endpoint_final")])

        def loaded(_handle, _layout):
            self.events.append("assemblies")
            return {"backend": {"package_assemblies": assembly_inventory()}}

        def endpoints(_handle):
            self.assertFalse(self.runtimes[-1].disconnected, "Post-stop HTTP observation was attempted")
            self.events.append("snapshot")
            return copy.deepcopy(next(snapshots))

        def native_browser(_handle, request, actual_scenario, *, on_event):
            self.assertEqual(scenario, actual_scenario)
            self.assertEqual(features.optional_feature_profile(scenario)["permission_profile"], request.permission_profile)
            self.events.append("browser")
            if scenario == "disconnect":
                on_event("disconnect-ready")
                self.events.append("disconnect-ack")
            on_event("begin-native-action")
            self.events.append("action-ack")
            self.events.append("synthetic-native-action")
            return child

        with patch.object(execution.hosts, "start_optional_feature_probe", side_effect=self.owner), \
                patch.object(execution.transport, "run_optional_browser", side_effect=browser or native_browser):
            return execution.run_optional_profile(self.layout(host, framework), scenario,
                validate_project=lambda _path: {}, observe_ready=ready or (lambda _handle, request: readiness(request)),
                observe_assemblies=assemblies or loaded, observe_endpoints=endpoints,
                canonical_definition_id_sha256=sha("canonical"))

    def validate(self, record):
        request = hosts.CellRequest(record["cell"]["host"], record["cell"]["framework"], "3.10.0")
        return execution.validate_optional_execution(record, request, record["scenario"],
            canonical_definition_id_sha256=sha("canonical"), canonical_assemblies=assembly_inventory(),
            validate_ready=matrix.verify_runtime_readiness)

    def test_all_sixty_profiles_use_fresh_owned_state_and_preserve_context_defects(self):
        for scenario in features.SCENARIOS:
            for host in features.HOSTS:
                for framework in features.FRAMEWORKS:
                    with self.subTest(scenario=scenario, host=host, framework=framework):
                        record = self.run_profile(scenario, host, framework)
                        self.assertEqual("complete", record["stage"])
                        self.assertEqual("failed" if scenario == "deny-workflow-contexts" else "passed", record["result"])
                        self.assertTrue(record["owned_process_cleanup"])
                        self.assertEqual(record, self.validate(record))
                        actual = self.layouts[-1]
                        self.assertEqual(features.optional_feature_profile(scenario)["backend_features"], list(actual.request.backend_features))
                        self.assertEqual(self.layout(host, framework).project_paths, actual.project_paths)
                        self.assertNotEqual(self.layout(host, framework).runtime_root, actual.runtime_root)
        self.assertEqual(60, len({layout.runtime_root for layout in self.layouts}))
        self.assertTrue(all(runtime.closed for runtime in self.runtimes))

    def test_normal_server_snapshots_surround_ack_and_native_action(self):
        record = self.run_profile("deny-secrets")
        self.assertEqual(["enter", "browser", "assemblies", "snapshot", "action-ack", "synthetic-native-action",
                          "assemblies", "snapshot", "cleanup"], self.events)
        self.assertEqual(1, record["endpoint_boundary"]["cursor"])
        self.assertEqual(2, record["endpoint_final"]["cursor"])
        rows = derive_backend_native_rows(record["endpoint_boundary"], record["endpoint_final"],
                                         record["endpoint_assemblies"], scenario="deny-secrets")
        self.assertEqual([False, True], [row["after_action"] for row in rows])
        self.assertEqual([200, 403], [row["status"] for row in rows])

    def test_disconnect_captures_ownership_and_snapshot_before_stop_and_never_reads_after(self):
        record = self.run_profile("disconnect")
        self.assertEqual(["enter", "browser", "assemblies", "snapshot", "stop", "disconnect-ack", "action-ack",
                          "synthetic-native-action", "cleanup"], self.events)
        self.assertEqual(record["endpoint_boundary"], record["endpoint_final"])
        self.assertTrue(all(record["parent_disconnect"].values()))
        self.assertFalse(record["assessment"]["native_transport_observed"])
        self.validate(record)

    def test_complete_defect_is_not_acceptance_and_recovery_fixture_is_explicitly_hypothetical(self):
        observed = self.run_profile("deny-workflow-contexts")
        self.assertTrue(observed["assessment"]["observation_complete"])
        self.assertEqual(["editor_unavailable"], observed["assessment"]["defects"])
        self.assertEqual("optional_acceptance_failed", observed["failure_category"])
        hypothetical = complete_record("deny-workflow-contexts", "server", hypothetical_context_recovery=True)
        self.assertEqual("passed", hypothetical["result"])
        self.validate(hypothetical)
        self.assertFalse(fixture("deny-workflow-contexts", "server")[0]["ui"]["editor_visible"])

    def test_transport_failure_never_claims_cleanup_or_retains_raw_error(self):
        def failed(*_args, **_kwargs):
            raise RuntimeError("PRIVATE synthetic transport error")
        record = self.run_profile("deny-secrets", browser=failed)
        self.assertEqual("failed", record["result"])
        self.assertFalse(record["owned_process_cleanup"])
        self.assertNotIn("browser", record)
        self.assertNotIn("PRIVATE", repr(record))
        self.assertTrue(self.runtimes[-1].closed)
        self.validate(record)

    def test_unverified_metadata_cannot_acknowledge_action_or_enter_retained_facts(self):
        record = self.run_profile("deny-secrets", assemblies=lambda _handle, _layout:
                                  {"backend": {"package_assemblies": []}})
        self.assertEqual("failed", record["result"])
        self.assertFalse(record["owned_process_cleanup"])
        self.assertNotIn("endpoint_assemblies", record)
        self.assertNotIn("endpoint_boundary", record)
        self.assertNotIn("action-ack", self.events)
        self.assertNotIn("synthetic-native-action", self.events)
        self.assertTrue(self.runtimes[-1].closed)
        self.validate(record)

    def test_cleanup_failure_preserves_validated_browser_facts_without_success(self):
        self.cleanup_failure = True
        record = self.run_profile("disconnect")
        self.assertEqual("owned_cleanup", record["stage"])
        self.assertEqual("failed", record["result"])
        self.assertFalse(record["owned_process_cleanup"])
        self.assertFalse(record["parent_disconnect"]["cleanup_verified"])
        self.assertIn("browser", record)
        self.assertNotIn("assessment", record)
        self.validate(record)

    def test_child_cleanup_failure_is_a_complete_failed_transport_assessment(self):
        child = complete_record("deny-secrets", "wasm")["browser"]
        child.update(cleanup_verified=False, failure_category="optional_probe_execution_failed")
        record = self.run_profile("deny-secrets", "wasm", child=child)
        self.assertEqual("complete", record["stage"])
        self.assertEqual("failed", record["result"])
        self.assertEqual("optional_acceptance_failed", record["failure_category"])
        self.validate(record)

    def test_failed_readiness_and_reused_runtime_cannot_reach_browser_success(self):
        def failed_ready(_handle, _request):
            raise ValueError("Synthetic readiness mismatch")
        record = self.run_profile("without-secrets", "wasm", ready=failed_ready)
        self.assertEqual("runtime_readiness", record["stage"])
        self.assertNotIn("runtime_readiness", record)
        self.assertNotIn("browser", record)
        self.validate(record)
        record = self.run_profile("without-secrets", "wasm")
        self.assertEqual("fresh_runtime", record["stage"])
        self.assertEqual(1, len(self.layouts))
        self.validate(record)

    def test_retention_recomputes_assessment_cleanup_and_canonical_owner_binding(self):
        original = complete_record("deny-secrets", "server")
        mutations = (lambda row: row.update(owned_process_cleanup=False),
                     lambda row: row["assessment"].update(acceptance=False),
                     lambda row: row["endpoint_assemblies"][0].update(sha256="f" * 64),
                     lambda row: row["endpoint_final"].update(pending=1),
                     lambda row: row.update(raw_error="private"))
        for mutate in mutations:
            with self.subTest(mutation=mutate):
                changed = copy.deepcopy(original)
                mutate(changed)
                with self.assertRaises(ValueError):
                    self.validate(changed)
        disconnect = complete_record("disconnect", "server")
        disconnect["parent_disconnect"]["browser_alive_after_stop"] = False
        with self.assertRaises(ValueError):
            self.validate(disconnect)

    def test_delayed_metadata_after_failed_transport_cannot_mutate_receipt_or_stop_closed_owner(self):
        for scenario, event in (("deny-secrets", "begin-native-action"), ("disconnect", "disconnect-ready")):
            with self.subTest(scenario=scenario):
                entered, release = threading.Event(), threading.Event()
                failures, workers = [], []

                def delayed_loaded(_handle, _layout):
                    entered.set()
                    if not release.wait(3):
                        raise TimeoutError("Synthetic callback deadline")
                    return {"backend": {"package_assemblies": assembly_inventory()}}

                def failed_browser(_handle, _request, _scenario, *, on_event):
                    def callback():
                        try:
                            on_event(event)
                        except Exception as error:
                            failures.append(type(error))
                    worker = threading.Thread(target=callback, daemon=True)
                    workers.append(worker)
                    worker.start()
                    if not entered.wait(3):
                        raise TimeoutError("Synthetic callback did not start")
                    raise RuntimeError("Synthetic transport failed before callback returned")

                try:
                    record = self.run_profile(scenario, browser=failed_browser, assemblies=delayed_loaded)
                    frozen = copy.deepcopy(record)
                    self.assertTrue(self.runtimes[-1].closed)
                    self.assertEqual("failed", record["result"])
                finally:
                    release.set()
                    for worker in workers:
                        worker.join(3)
                        self.assertFalse(worker.is_alive())
                self.assertEqual(frozen, record)
                self.assertNotIn("endpoint_assemblies", record)
                self.assertNotIn("parent_disconnect", record)
                self.assertNotIn("stop", self.events)
                self.assertEqual([RuntimeError], failures)
                self.validate(record)


if __name__ == "__main__":
    unittest.main()
