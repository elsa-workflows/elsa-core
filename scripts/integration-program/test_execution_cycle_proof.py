import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

import run_execution_cycle_proof as proof


class ExecutionCycleProofTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)

    def write_trx(self, path, outcome="Passed", count=1, method="CheckpointRetainsHandle"):
        root = ET.Element("TestRun")
        summary = ET.SubElement(root, "ResultSummary")
        ET.SubElement(summary, "Counters", total=str(count), executed=str(count if outcome == "Passed" else 0),
                      passed=str(count if outcome == "Passed" else 0),
                      notExecuted=str(count if outcome == "NotExecuted" else 0))
        definitions = ET.SubElement(root, "TestDefinitions")
        results = ET.SubElement(root, "Results")
        for index in range(count):
            test = ET.SubElement(definitions, "UnitTest", id=str(index))
            ET.SubElement(test, "TestMethod", className="Elsa.Tests.CycleTests", name=method)
            ET.SubElement(results, "UnitTestResult", testId=str(index), outcome=outcome,
                          testName=f"{method}(value: private-fixture-{index})")
        ET.ElementTree(root).write(path)

    def test_result_summary_retains_methods_without_parameter_values_or_raw_output(self):
        trx = self.root / "result.trx"
        self.write_trx(trx, count=2)
        result = proof.test_summary(trx, 0)
        self.assertEqual("passed", result["status"])
        self.assertEqual(2, len(result["cases"]))
        self.assertNotEqual(result["cases"][0]["caseSha256"], result["cases"][1]["caseSha256"])
        self.assertNotIn("private-fixture", json.dumps(result))
        self.assertEqual("Elsa.Tests.CycleTests.CheckpointRetainsHandle", result["cases"][0]["method"])

    def test_skips_zero_results_and_process_failure_cannot_pass(self):
        trx = self.root / "result.trx"
        for outcome, count, code in (("NotExecuted", 1, 0), ("Passed", 0, 0), ("Passed", 1, 1)):
            with self.subTest(outcome=outcome, count=count, code=code):
                self.write_trx(trx, outcome, count)
                self.assertNotEqual("passed", proof.test_summary(trx, code)["status"])

    def test_invalid_or_missing_method_identity_rejects_receipt(self):
        trx = self.root / "result.trx"
        self.write_trx(trx, method="secret user content")
        with self.assertRaises(ValueError):
            proof.test_summary(trx, 0)
        self.write_trx(trx)
        tree = ET.parse(trx)
        tree.getroot().find("Results/UnitTestResult").set("testId", "missing")
        tree.write(trx)
        with self.assertRaises(ValueError):
            proof.test_summary(trx, 0)

    def test_symlink_file_and_ancestor_rejected_before_git_lookup(self):
        real = self.root / "real"
        real.mkdir()
        (real / "a.csproj").write_text("<Project />")
        (self.root / "linked").symlink_to(real, target_is_directory=True)
        (self.root / "file.csproj").symlink_to(real / "a.csproj")
        for path in ("linked/a.csproj", "file.csproj", "../escape.csproj"):
            with self.subTest(path=path), self.assertRaises(ValueError):
                proof.tracked_input(self.root, path)

    def test_full_closure_requires_all_nine_builds_and_four_test_runs(self):
        checkout = self.root / "repo"
        checkout.mkdir()
        input_file = self.root / "input"
        input_file.write_text("tracked input")
        commands = []

        def execute(command, root, log):
            commands.append(command)
            if command[1] == "test":
                name = command[command.index("--logger") + 1].split("LogFileName=")[1]
                self.write_trx(log.parent / name)
            return {"exitCode": 0, "status": "passed"}

        with patch.object(proof, "assert_clean_source") as clean, \
                patch.object(proof, "tracked_input", return_value=input_file), \
                patch.object(proof, "execute", side_effect=execute):
            result = proof.run(checkout, self.root / "evidence", "exact-head")
        self.assertTrue(result["verificationComplete"])
        self.assertFalse(result["publicationPerformed"])
        self.assertEqual(4, len(result["testBuilds"]))
        self.assertEqual(9, len(result["builds"]))
        self.assertEqual(4, len(result["tests"]))
        self.assertEqual(2, clean.call_count)
        self.assertEqual(1, sum("--filter" in command for command in commands))
        self.assertTrue(all("--no-build" in command and "--no-restore" in command
                            for command in commands if command[1] == "test"))

    def test_all_fixture_errors_collected_before_any_execution_or_broader_builds(self):
        checkout = self.root / "repo"
        checkout.mkdir()
        input_file = self.root / "input"
        input_file.write_text("tracked input")
        with patch.object(proof, "assert_clean_source"), \
                patch.object(proof, "tracked_input", return_value=input_file), \
                patch.object(proof, "execute", return_value={"exitCode": 1, "status": "failed"}) as execute:
            result = proof.run(checkout, self.root / "evidence", "exact-head")
        self.assertFalse(result["verificationComplete"])
        self.assertEqual(4, len(result["testBuilds"]))
        self.assertEqual([], result["builds"])
        self.assertEqual([], result["tests"])
        self.assertEqual(4, execute.call_count)

    def test_failed_test_stops_before_later_tests_and_broader_builds(self):
        checkout = self.root / "repo"
        checkout.mkdir()
        input_file = self.root / "input"
        input_file.write_text("tracked input")
        with patch.object(proof, "assert_clean_source"), \
                patch.object(proof, "tracked_input", return_value=input_file), \
                patch.object(proof, "execute", side_effect=lambda command, root, log:
                             {"exitCode": 1, "status": "failed"} if command[1] == "test"
                             else {"exitCode": 0, "status": "passed"}) as execute:
            result = proof.run(checkout, self.root / "evidence", "exact-head")
        self.assertFalse(result["verificationComplete"])
        self.assertEqual([], result["builds"])
        self.assertEqual(1, len(result["tests"]))
        self.assertEqual(5, execute.call_count)


if __name__ == "__main__":
    unittest.main()
