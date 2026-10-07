import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

import run_mongo_atomic_proof as proof


class MongoAtomicProofTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name).resolve()

    def write_trx(self, path, methods, outcome="Passed", markers=None):
        root = ET.Element("TestRun")
        summary = ET.SubElement(root, "ResultSummary")
        total = sum(methods.values())
        ET.SubElement(summary, "Counters", total=str(total), executed=str(total),
                      passed=str(total if outcome == "Passed" else 0),
                      failed=str(total if outcome == "Failed" else 0),
                      notExecuted=str(total if outcome == "NotExecuted" else 0))
        definitions = ET.SubElement(root, "TestDefinitions")
        results = ET.SubElement(root, "Results")
        index = 0
        for method, count in methods.items():
            for case in range(count):
                class_name, name = method.rsplit(".", 1)
                test = ET.SubElement(definitions, "UnitTest", id=str(index))
                ET.SubElement(test, "TestMethod", className=class_name, name=name)
                result = ET.SubElement(results, "UnitTestResult", testId=str(index), outcome=outcome,
                                       testName=f"{method}(private argument {case})")
                if outcome == "Failed":
                    output = ET.SubElement(result, "Output")
                    error = ET.SubElement(output, "ErrorInfo")
                    ET.SubElement(error, "Message").text = (markers or {}).get(method, "Docker unavailable")
                index += 1
        ET.ElementTree(root).write(path)

    def test_baseline_requires_both_intended_behavior_assertions_and_normal_failure(self):
        trx = self.directory / "baseline.trx"
        methods = dict.fromkeys(proof.REGRESSIONS, 1)
        self.write_trx(trx, methods, "Failed", proof.REGRESSIONS)
        result = proof.baseline_summary(trx, 1)
        self.assertTrue(result["expectedRegressionFailures"])
        self.assertNotIn("private argument", json.dumps(result))
        for code in (0, 124):
            with self.subTest(code=code), self.assertRaises(ValueError):
                proof.baseline_summary(trx, code)
        for outcome, markers in (("Passed", None), ("NotExecuted", None), ("Failed", None),
                                 ("Failed", dict(zip(methods, reversed(list(proof.REGRESSIONS.values())))))):
            with self.subTest(outcome=outcome, markers=markers), self.assertRaises(ValueError):
                self.write_trx(trx, methods, outcome, markers)
                proof.baseline_summary(trx, 1)

    def test_missing_or_duplicate_candidate_cases_cannot_pass(self):
        cases = [{"method": proof.PREFIX + name, "outcome": "Passed", "caseSha256": str(index)}
                 for name, count in proof.MONGO_CASES.items() for index in range(count)]
        proof.require_case_coverage({"cases": cases}, proof.PREFIX, proof.MONGO_CASES)
        for invalid in (cases[:-1], cases + [cases[0]]):
            with self.assertRaises(ValueError):
                proof.require_case_coverage({"cases": invalid}, proof.PREFIX, proof.MONGO_CASES)

    def identity(self, label, modes, image_id="sha256:" + "a" * 64):
        directory = self.directory / "service-identity" / label
        directory.mkdir(parents=True, exist_ok=True)
        for index, mode in enumerate(modes):
            (directory / f"{index}.json").write_text(json.dumps({
                "imageId": image_id, "mode": mode,
                "replicaSet": "rs1" if mode == "replica-set" else None}))

    def test_service_identity_requires_observed_topology_and_same_immutable_image(self):
        self.identity("baseline-probes", ["replica-set"])
        self.identity("candidate-tests-0", ["replica-set", "standalone"])
        with patch.object(proof, "image_identity", return_value={"verified": True}) as inspect:
            result = proof.service_identity(self.directory)
            self.assertTrue(result["image"]["verified"])
            inspect.assert_called_once_with("sha256:" + "a" * 64)
        self.identity("candidate-tests-0", ["replica-set", "standalone"], "sha256:" + "b" * 64)
        with self.assertRaises(ValueError):
            proof.service_identity(self.directory)
        self.identity("candidate-tests-0", ["replica-set", "standalone"])
        (self.directory / "service-identity/candidate-tests-0/1.json").unlink()
        with self.assertRaises(ValueError):
            proof.service_identity(self.directory)

    def test_image_metadata_must_match_tested_container_id(self):
        image_id = "sha256:" + "a" * 64
        data = {"Id": image_id, "RepoDigests": ["mongo@sha256:" + "b" * 64],
                "Os": "linux", "Architecture": "amd64", "private": "not retained"}
        with patch.object(proof.subprocess, "check_output", return_value=json.dumps([data])):
            self.assertNotIn("private", proof.image_identity(image_id))
            with self.assertRaises(ValueError):
                proof.image_identity("sha256:" + "c" * 64)

    def test_full_orchestration_requires_negative_control_and_all_candidate_phases(self):
        checkout = self.directory / "repo"
        checkout.mkdir()
        subprocess.run(["git", "init", "-q", str(checkout)], check=True)
        proof.git(checkout, "config", "user.email", "proof@example.invalid")
        proof.git(checkout, "config", "user.name", "Proof fixture")
        for name in proof.INPUTS:
            path = checkout / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("baseline input")
        proof.git(checkout, "add", ".")
        proof.git(checkout, "commit", "-qm", "baseline")
        baseline = proof.git(checkout, "rev-parse", "HEAD")
        (checkout / proof.FIXTURE).write_text("candidate regression fixture")
        proof.git(checkout, "add", ".")
        proof.git(checkout, "commit", "-qm", "candidate")
        head = proof.git(checkout, "rev-parse", "HEAD")
        output = self.directory / "evidence"
        commands = []

        def execute(command, root, log):
            commands.append(command)
            if command[1] != "test":
                return {"exitCode": 0, "status": "passed"}
            baseline_run = root != checkout
            mongo = command[2] == proof.MONGO_TEST
            methods = (dict.fromkeys(proof.REGRESSIONS, 1) if baseline_run else
                       {proof.PREFIX + key: count for key, count in proof.MONGO_CASES.items()} if mongo else
                       {proof.BPMN_PREFIX + key: count for key, count in proof.BPMN_CASES.items()})
            trx_name = command[command.index("--logger") + 1].split("LogFileName=")[1]
            self.write_trx(log.parent / trx_name, methods, "Failed" if baseline_run else "Passed", proof.REGRESSIONS)
            return {"exitCode": 1 if baseline_run else 0, "status": "failed" if baseline_run else "passed"}

        with patch.object(proof, "BASELINE", baseline), patch.object(proof, "execute", side_effect=execute), \
                patch.object(proof, "service_identity", return_value={"tested": True}):
            result = proof.run(checkout, output, head)
        self.assertTrue(result["verificationComplete"])
        self.assertFalse(result["publicationPerformed"])
        self.assertEqual(3, len(result["testBuilds"]))
        self.assertEqual(2, len(result["tests"]))
        self.assertEqual(3, len(result["builds"]))
        self.assertTrue(result["baseline"]["expectedRegressionFailures"])
        self.assertEqual(9, len(commands))
        self.assertFalse((output / "baseline-source").exists())
        self.assertEqual("", proof.git(checkout, "status", "--porcelain"))
        self.assertEqual(hashlib.sha256((checkout / proof.FIXTURE).read_bytes()).hexdigest(), result["inputSha256"][proof.FIXTURE])


if __name__ == "__main__":
    unittest.main()
