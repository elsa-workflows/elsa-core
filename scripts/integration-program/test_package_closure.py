import io
import json
import os
import tempfile
import unittest
from contextlib import contextmanager, redirect_stderr
from pathlib import Path
from unittest.mock import patch

from package_closure import (
    KNOWN_BASELINE_SKIPS,
    build_plan,
    classify_project,
    classify_trx_result,
    deferred_execution_projects,
    main,
    parse_sources,
    parse_trx,
    verify_project_reference_paths,
    verify_resolved_project_graph,
    resolved_project_inputs,
    verify_source_clean,
    verify_source_pin,
)


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
INVENTORY = REPOSITORY_ROOT / "doc/integration-program/inventory/inventory.json"


class PackageClosureTests(unittest.TestCase):
    def test_core_change_selects_slack_release_unit_at_inventory_graph(self):
        plan = build_plan(INVENTORY, {})

        self.assertEqual(plan["affected_test_project_count"], 51)
        self.assertEqual(plan["scenario"]["packages_to_pack"], ["Elsa.Slack"])
        self.assertEqual(plan["scenario"]["affected_test_project_count"], 51)
        self.assertEqual(plan["module_change_scenario"]["affected_test_project_count"], 1)
        self.assertEqual(
            plan["module_change_scenario"]["affected_test_projects"],
            ["elsa-extensions:test/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj"],
        )
        self.assertEqual(plan["module_change_scenario"]["packages_to_pack"], ["Elsa.Slack"])
        self.assertEqual(plan["ambiguous_package_edges_resolved_conservatively"], 0)
        self.assertEqual(
            plan["lane_counts"],
            {"local-test": 47, "docker-service": 2, "performance": 1, "build-only": 1},
        )
        self.assertEqual(
            {framework for project in plan["projects"] for framework in project["declared_target_frameworks"]},
            {"net10.0"},
        )
        self.assertTrue(all(not project["untested_target_frameworks"] for project in plan["projects"]))
        self.assertEqual(plan["source_pins"], {
            "elsa-core": "610790ec57ae9d5c334181d50c1e65f99613fd86",
            "elsa-extensions": "33fa0bfd28c7585240e3d4f665058c067b17e287",
        })

    def test_missing_packable_identity_blocks_source_preflight(self):
        self.assertIn("TlsSmoke", build_plan(INVENTORY, {})["source_package_ids"])
        inventory = json.loads(INVENTORY.read_text())
        smoke = next(row for row in inventory["project_inventory"]["elsa-core"] if row["path"] == "test/TlsSmoke/TlsSmoke.csproj")
        smoke["package_id"] = None
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "inventory.json"
            path.write_text(json.dumps(inventory))
            recorded = build_plan(path, {})
        self.assertEqual(recorded["unresolved_source_package_ids"], ["elsa-core:test/TlsSmoke/TlsSmoke.csproj"])
        with self.source_graph() as (plan, sources, *_):
            plan["unresolved_source_package_ids"] = recorded["unresolved_source_package_ids"]
            with patch("package_closure.resolved_project_inputs") as evaluate:
                with self.assertRaisesRegex(ValueError, "identity is missing"):
                    verify_resolved_project_graph(plan, sources, 30)
            evaluate.assert_not_called()
            self.assertEqual(plan["source_binding_preflight"]["status"], "failed")
            self.assertEqual(plan["source_binding_preflight"]["projects"], [])

    def test_declared_test_inputs_drive_service_and_host_classification(self):
        docker_lane, _ = classify_project("elsa-core", {
            "path": "test/component/Example/Example.csproj",
            "package_references": [
                {"id": "Microsoft.NET.Test.Sdk"},
                {"id": "Testcontainers.PostgreSql"},
            ],
        })
        host_lane, _ = classify_project("elsa-extensions", {
            "path": "test/workbench/Example/Example.csproj",
            "package_references": [{"id": "Testcontainers.PostgreSql"}],
        })
        performance_lane, _ = classify_project("elsa-core", {
            "path": "test/performance/Example/Example.csproj",
            "package_references": [{"id": "Microsoft.NET.Test.Sdk"}],
        })

        self.assertEqual(docker_lane, "docker-service")
        self.assertEqual(host_lane, "build-only")
        self.assertEqual(performance_lane, "performance")

    def test_source_pin_mismatch_fails_closed(self):
        with self.assertRaisesRegex(ValueError, "Source pin mismatch"):
            verify_source_pin("elsa-core", "expected-sha", "different-sha")

    def test_source_dirty_before_or_after_execution_fails_closed(self):
        with self.assertRaisesRegex(ValueError, "not clean"):
            verify_source_clean("elsa-core", "?? stray.txt")

    def test_extension_project_references_must_resolve_to_supplied_sources(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            core = root / "elsa-core"
            extensions = root / "extensions"
            project = extensions / "src/modules/communication/Elsa.Slack/Elsa.Slack.csproj"
            expected_core_elsa = core / "src/modules/Elsa/Elsa.csproj"
            project.parent.mkdir(parents=True)
            expected_core_elsa.parent.mkdir(parents=True)
            project.touch()
            expected_core_elsa.touch()

            verify_project_reference_paths(project, core, extensions, [expected_core_elsa], requires_core_elsa=True)

            decoy_core_elsa = root / "other-checkout/src/modules/Elsa/Elsa.csproj"
            decoy_core_elsa.parent.mkdir(parents=True)
            decoy_core_elsa.touch()
            with self.assertRaisesRegex(ValueError, "outside the pinned Core and Extensions"):
                verify_project_reference_paths(project, core, extensions, [decoy_core_elsa], requires_core_elsa=True)

    @contextmanager
    def source_graph(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            core, extensions = root / "core", root / "extensions"
            slack = extensions / "src/modules/communication/Elsa.Slack/Elsa.Slack.csproj"
            elsa = core / "src/modules/Elsa/Elsa.csproj"
            helper = core / "src/Helper/Helper.csproj"
            for path in (slack, elsa, helper):
                path.parent.mkdir(parents=True, exist_ok=True)
                path.touch()
            plan = {"source_package_ids": ["Elsa"], "projects": []}
            yield plan, {"elsa-core": core, "elsa-extensions": extensions}, slack, elsa, helper

    def test_transitive_cached_source_package_is_rejected_before_execution(self):
        with self.source_graph() as (plan, sources, slack, elsa, helper):
            inputs = {slack: ([elsa], []), elsa: ([helper], []), helper: ([], ["eLsA", "ThirdParty"])}
            with patch("package_closure.resolved_project_inputs", side_effect=lambda path, *_: inputs[path]):
                with self.assertRaisesRegex(ValueError, "feed/cache"):
                    verify_resolved_project_graph(plan, sources, 30)
            receipt = plan["source_binding_preflight"]
            self.assertEqual(receipt["status"], "failed")
            self.assertEqual(receipt["projects"][-1]["remaining_source_package_references"], ["eLsA"])
            self.assertEqual(len(receipt["projects"]), 3)

    def test_transitive_outside_reference_is_rejected_and_cycle_is_bounded(self):
        with self.source_graph() as (plan, sources, slack, elsa, helper):
            outside = sources["elsa-core"].parent / "ambient.csproj"
            outside.touch()
            inputs = {slack: ([elsa], []), elsa: ([helper], []), helper: ([elsa, outside], [])}
            with patch("package_closure.resolved_project_inputs", side_effect=lambda path, *_: inputs[path]):
                with self.assertRaisesRegex(ValueError, "outside the pinned"):
                    verify_resolved_project_graph(plan, sources, 30)
                inputs[helper] = ([elsa], ["ThirdParty"])
                verify_resolved_project_graph(plan, sources, 30)
            self.assertEqual(plan["source_binding_preflight"]["status"], "passed")
            self.assertEqual(len(plan["source_binding_preflight"]["projects"]), 3)

    def test_evaluation_includes_framework_conditional_package_references(self):
        from types import SimpleNamespace
        def response(packages):
            return SimpleNamespace(returncode=0, stderr="", stdout=json.dumps({
                "Properties": {"TargetFramework": "", "TargetFrameworks": "net8.0;net10.0"},
                "Items": {"ProjectReference": [], "PackageReference": [{"Identity": value} for value in packages]},
            }))
        with patch("package_closure.subprocess.run", side_effect=[response([]), response(["Elsa"]), response(["ThirdParty"])]) as run:
            references, packages = resolved_project_inputs(Path("Test.csproj"), 30, True)
        self.assertEqual(references, [])
        self.assertEqual(packages, ["Elsa", "ThirdParty"])
        self.assertEqual(run.call_count, 3)
        self.assertTrue(all("-property:UseProjectReferences=true" in call.args[0] for call in run.call_args_list))

    def test_omitted_build_host_and_docker_projects_remain_deferred(self):
        projects = [
            {"key": "local", "lane": "local-test"},
            {"key": "docker", "lane": "docker-service"},
            {"key": "host", "lane": "build-only"},
            {"key": "benchmark", "lane": "performance"},
        ]
        execution = [{"key": "local", "status": "passed"}]

        self.assertEqual(deferred_execution_projects(projects, execution), ["docker", "host"])

    def test_source_mapping_rejects_duplicates_and_malformed_values(self):
        with self.assertRaisesRegex(ValueError, "Duplicate source mapping"):
            parse_sources(["elsa-core=one", "elsa-core=two"])
        with self.assertRaisesRegex(ValueError, "REPOSITORY=PATH"):
            parse_sources(["elsa-core"])

    def test_trx_receipt_accepts_passes_and_marks_only_the_exact_known_skip(self):
        passed = {"total": 2, "executed": 2, "passed": 2, "failed": 0, "error": 0, "timeout": 0, "aborted": 0}
        self.assertEqual(classify_trx_result("repo:test/Test.csproj", 0, passed, [
            {"name": "A", "outcome": "Passed"},
            {"name": "B", "outcome": "Passed"},
        ]), "passed")

        skipped = {"total": 1, "executed": 0, "passed": 0, "failed": 0, "error": 0, "timeout": 0, "aborted": 0}
        slack_skip_key, slack_skip_reasons = next(iter(KNOWN_BASELINE_SKIPS.items()))
        slack_skip_name, slack_skip_reason = next(iter(slack_skip_reasons.items()))
        self.assertEqual(classify_trx_result(slack_skip_key, 0, skipped, [
            {"name": slack_skip_name, "outcome": "NotExecuted", "skip_reason": slack_skip_reason},
        ]), "known-baseline-skip")
        self.assertEqual(classify_trx_result(slack_skip_key, 0, skipped, [
            {"name": slack_skip_name, "outcome": "NotExecuted", "skip_reason": "Changed skip reason"},
        ]), "incomplete")
        self.assertEqual(classify_trx_result("repo:test/Test.csproj", 0, skipped, [
            {"name": "Unrelated.Skip", "outcome": "NotExecuted"},
        ]), "incomplete")

        core_component_key = "elsa-core:test/component/Elsa.Workflows.ComponentTests/Elsa.Workflows.ComponentTests.csproj"
        core_component_skip_reasons = KNOWN_BASELINE_SKIPS[core_component_key]
        mixed = {"total": 3, "executed": 1, "passed": 1, "failed": 0, "error": 0, "timeout": 0, "aborted": 0}
        core_component_results = [
            {"name": "A", "outcome": "Passed"},
            *(
                {"name": name, "outcome": "NotExecuted", "skip_reason": core_component_skip_reasons[name]}
                for name in sorted(core_component_skip_reasons)
            ),
        ]
        self.assertEqual(classify_trx_result(core_component_key, 0, mixed, core_component_results), "known-baseline-skip")
        self.assertEqual(classify_trx_result("repo:test/Test.csproj", 0, passed, [
            {"name": "A", "outcome": "Failed"},
            {"name": "B", "outcome": "Passed"},
        ]), "failed")

    def test_trx_parser_requires_every_counted_result_to_be_present(self):
        trx = """<TestRun><Results><UnitTestResult testName="A" outcome="Passed" /></Results>
          <ResultSummary><Counters total="1" executed="1" passed="1" failed="0" />
          </ResultSummary></TestRun>"""
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "receipt.trx"
            path.write_text(trx, encoding="utf-8")
            counters, results = parse_trx(path)
            self.assertEqual(counters["passed"], 1)
            self.assertEqual(results, [{"name": "A", "outcome": "Passed"}])

            path.write_text(trx.replace('total="1"', 'total="2"'), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "does not match"):
                parse_trx(path)

            skipped_trx = """<TestRun><Results><UnitTestResult testName="A" outcome="NotExecuted">
              <Output><ErrorInfo><Message>Not implemented yet.</Message></ErrorInfo></Output>
              </UnitTestResult></Results><ResultSummary>
              <Counters total="1" executed="0" passed="0" failed="0" /></ResultSummary></TestRun>"""
            path.write_text(skipped_trx, encoding="utf-8")
            counters, results = parse_trx(path)
            self.assertEqual(counters["executed"], 0)
            self.assertEqual(results[0]["skip_reason"], "Not implemented yet.")

    def test_missing_release_unit_package_fails_closed(self):
        data = json.loads(INVENTORY.read_text(encoding="utf-8"))
        for project in data["project_inventory"]["elsa-extensions"]:
            if project["path"] == "src/modules/communication/Elsa.Slack/Elsa.Slack.csproj":
                project["package_id"] = "Elsa.Slack.Renamed"

        with tempfile.TemporaryDirectory() as directory:
            temporary = Path(directory) / "inventory.json"
            temporary.write_text(json.dumps(data), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "Expected only Elsa.Slack"):
                build_plan(temporary, {})

    def test_requested_receipt_path_gets_a_failure_receipt_with_partial_plan(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "nested" / "failure.json"
            with patch("package_closure.sys.argv", [
                "package_closure.py",
                "--inventory", str(INVENTORY),
                "--command-timeout-seconds", "0",
                "--output", str(output),
            ]):
                result = main()

            receipt = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(result, 2)
            self.assertEqual(receipt["result"], "failed")
            self.assertEqual(receipt["failure"]["phase"], "plan construction and source preflight")
            self.assertIn("greater than zero", receipt["failure"]["message"])
            self.assertEqual(receipt["requested"]["inventory"], str(INVENTORY))
            self.assertEqual(receipt["partial_plan"]["affected_test_project_count"], 51)

    def test_step_summary_failure_does_not_replace_success_receipt(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            output = root / "plan.json"
            summary_directory = root / "summary-directory"
            summary_directory.mkdir()
            stderr = io.StringIO()
            with patch.dict(os.environ, {"GITHUB_STEP_SUMMARY": str(summary_directory)}), \
                    patch("package_closure.sys.argv", [
                        "package_closure.py",
                        "--inventory", str(INVENTORY),
                        "--output", str(output),
                    ]), redirect_stderr(stderr):
                result = main()

            receipt = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(result, 0)
            self.assertEqual(receipt["affected_test_project_count"], 51)
            self.assertEqual(receipt["scenario"]["packages_to_pack"], ["Elsa.Slack"])
            self.assertNotIn("failure", receipt)
            self.assertIn("Could not append GitHub step summary", stderr.getvalue())

    def test_step_summary_failure_preserves_primary_failure_receipt(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            output = root / "failure.json"
            summary_directory = root / "summary-directory"
            summary_directory.mkdir()
            stderr = io.StringIO()
            with patch.dict(os.environ, {"GITHUB_STEP_SUMMARY": str(summary_directory)}), \
                    patch("package_closure.sys.argv", [
                        "package_closure.py",
                        "--inventory", str(INVENTORY),
                        "--command-timeout-seconds", "0",
                        "--output", str(output),
                    ]), redirect_stderr(stderr):
                result = main()

            receipt = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(result, 2)
            self.assertEqual(receipt["result"], "failed")
            self.assertEqual(receipt["failure"]["phase"], "plan construction and source preflight")
            self.assertIn("Could not append GitHub step summary", stderr.getvalue())

    def test_source_pin_preflight_failure_still_writes_requested_receipt(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "failed-pin.json"
            with patch("package_closure.sys.argv", [
                "package_closure.py",
                "--inventory", str(INVENTORY),
                "--source", "elsa-core=/missing/pinned-source",
                "--output", str(output),
            ]):
                result = main()

            receipt = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(result, 2)
            self.assertEqual(receipt["result"], "failed")
            self.assertIn("Source checkout does not exist", receipt["failure"]["message"])
            self.assertEqual(receipt["requested"]["sources"], ["elsa-core=/missing/pinned-source"])


if __name__ == "__main__":
    unittest.main()
