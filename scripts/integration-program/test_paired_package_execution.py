import copy
from contextlib import contextmanager
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

import paired_package_execution as execution


class ExecutionContracts(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.key = ("3.10.0", "net10.0", "server")
        self.events = []
        self.record = {"version": self.key[0], "framework": self.key[1], "host": self.key[2],
                       "result": "passed", "resources": [], "proof": {}, "browser_version": "149.0.7827.55",
                       "assertions": [{"name": name, "passed": True, "reason_category": None}
                                      for name in execution.browser.required_assertions(dict(zip(("version", "framework", "host"), self.key)))]}
        self.layout = SimpleNamespace(request=execution.hosts.CellRequest("server", "net10.0", "3.10.0"),
                                      project_paths={"backend": self.root / "backend.csproj", "server": self.root / "server.csproj"})

    def patch(self, owner, name, **kwargs):
        context = patch.object(owner, name, **kwargs)
        self.addCleanup(context.stop)
        return context.start()

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
                yield SimpleNamespace(password="PRIVATE-MUST-NOT-BE-RETAINED")
            finally:
                self.events.append(("stop",))
        self.patch(execution, "evidence_gaps", return_value=[])
        self.patch(execution.packages, "_validated_manifest", return_value=("3.10.0", "b" * 40, {"elsa": {"id": "Elsa"}}, {}, []))
        self.patch(execution.packages, "render_nuget_config", return_value="<configuration />")
        self.patch(execution.hosts, "materialize", side_effect=materialize)
        self.patch(execution.hosts, "build", side_effect=lambda _: self.events.append(("build",)) or [])
        self.patch(execution, "_project_validator", return_value=validate)
        self.patch(execution, "_resource_inventory", side_effect=lambda *_: self.events.append(("resources",)) or {"assets": []})
        self.patch(execution.hosts, "start_pair", side_effect=pair)
        def run_browser(_handle, request, _resources):
            self.events.append(("browser",))
            record = copy.deepcopy(self.record)
            record.update(version=request.version, framework=request.framework, host=request.host)
            return record
        browser = self.patch(execution.browser, "run_browser", side_effect=run_browser)
        self.patch(execution, "_observe_loaded", side_effect=lambda *_: self.events.append(("observe",)) or {})
        self.patch(execution, "_verify_loaded", side_effect=lambda *_: self.events.append(("loaded",)) or {})
        self.patch(execution.resources, "verify_browser_resources", side_effect=lambda *_args, **_kw: self.events.append(("resource_check",)) or {})
        return browser

    def execute(self, key=None):
        return execution.execute_cell(key or self.key, private=self.root / "private", retained=self.root / "retained",
                  verified_root=self.root / "candidate", manifest={}, manifest_hash="a" * 64, sdk="10.0.300")

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

    def test_valid_pipeline_verifies_before_start_and_retains_no_private_handle(self):
        self.pipeline()
        self.assertEqual("passed", self.execute()["result"])
        stages = [event[0] for event in self.events]
        self.assertLess(stages.index("validate"), stages.index("start"))
        self.assertLess(stages.index("loaded"), stages.index("stop"))
        self.assertLess(stages.index("stop"), stages.index("resource_check"))
        self.assertTrue(self.receipt()["owned_process_cleanup"])
        for path in (self.root / "retained").rglob("*.json"):
            self.assertNotIn("PRIVATE", path.read_text())

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
            return {"verified_artifacts_sha256": "a" * 64}
        def execute(key, **_kwargs):
            record = copy.deepcopy(self.record)
            record.update(zip(("version", "framework", "host"), key))
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
