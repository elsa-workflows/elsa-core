"""Exercise the workflow's admission receipt consumer and credential boundary."""
import copy
import json
from pathlib import Path
import subprocess
import unittest

import yaml


ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = ROOT / ".github/workflows/publish-consolidated-original-candidate.yml"
SOURCE = "a" * 40


class ExecutorWorkflowTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = yaml.safe_load(WORKFLOW.read_text())
        cls.jobs = cls.workflow["jobs"]
        cls.filter = cls.workflow["env"]["ELSA_ADMISSION_RECEIPT_FILTER"]

    def receipt(self, native=False):
        result = "native_approval_verified" if native else "eligible_for_native_approval"
        return {
            "schema": 1, "result": result, "publication_performed": False,
            "publication_ready": False,
            "admission": {
                "status": result, "repository": "elsa-workflows/elsa-core",
                "ref": "refs/heads/main", "source_commit": SOURCE,
                "run_id": 123, "run_attempt": 1,
                "environment_name": "elsa-3-10-feedz", "environment_id": 456,
                "reviewer_ids": [789],
                "branch_policies": [{"id": 321, "type": "branch", "name": "main"}],
                "operational_packet_sha256": "b" * 64,
                "native_approval_verified": native,
            },
        }

    def accepted(self, receipt, native=False):
        result = "native_approval_verified" if native else "eligible_for_native_approval"
        completed = subprocess.run(
            ["jq", "-e", "--arg", "result", result, "--argjson", "native", json.dumps(native),
             "--arg", "source", SOURCE, "--argjson", "run", "123", self.filter],
            input=json.dumps(receipt), text=True, stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL, timeout=5, check=False,
        )
        return completed.returncode == 0

    def test_positive_receipts_do_not_cross_admission_phases(self):
        for native in (False, True):
            with self.subTest(native=native):
                self.assertTrue(self.accepted(self.receipt(native), native))
                self.assertFalse(self.accepted(self.receipt(native), not native))

    def test_unknown_failed_or_wrong_run_receipts_are_rejected(self):
        bad_values = {
            "repository": "another/repository", "ref": "refs/heads/unreviewed",
            "source_commit": "c" * 40, "run_id": 124, "run_attempt": 2,
            "environment_name": "unprotected", "environment_id": None,
            "reviewer_ids": [], "branch_policies": [{"id": 321, "type": "branch", "name": "*"}],
            "operational_packet_sha256": "unknown", "status": "failed",
            "native_approval_verified": "false",
        }
        for key, value in bad_values.items():
            with self.subTest(key=key):
                receipt = self.receipt()
                receipt["admission"][key] = value
                self.assertFalse(self.accepted(receipt))
        for receipt in ({}, None, {"result": "failed"}, self.receipt() | {"publication_ready": True}):
            with self.subTest(receipt_type=type(receipt).__name__):
                self.assertFalse(self.accepted(receipt))

    def test_missing_identity_fields_fail_closed(self):
        original = self.receipt()
        for key in original["admission"]:
            with self.subTest(missing=key):
                receipt = copy.deepcopy(original)
                del receipt["admission"][key]
                self.assertFalse(self.accepted(receipt))

    def test_receipt_is_asserted_before_scheduling_and_key_exposure(self):
        schedule = next(step for step in self.jobs["admit"]["steps"] if step.get("id") == "admission")
        self.assertIn("--phase schedule", schedule["run"])
        self.assertLess(schedule["run"].index("jq -e"), schedule["run"].index("admitted=true"))
        self.assertIn("ELSA_ADMISSION_RECEIPT_FILTER", schedule["run"])
        steps = self.jobs["publish"]["steps"]
        credential_index = next(i for i, step in enumerate(steps)
                                if "ELSA_CONSOLIDATED_FEEDZ_PUBLISH_KEY" in step.get("env", {}))
        recheck = steps[credential_index - 1]
        self.assertIn("--phase publish", recheck["run"])
        self.assertIn("--arg result native_approval_verified", recheck["run"])
        self.assertIn("ELSA_ADMISSION_RECEIPT_FILTER", recheck["run"])

    def test_nonpublishing_jobs_cannot_receive_publisher_credentials(self):
        self.assertEqual(self.jobs["publish"]["environment"], "elsa-3-10-feedz")
        self.assertIn("github.ref == 'refs/heads/main'", self.jobs["publish"]["if"])
        self.assertIn("needs.admit.outputs.admitted == 'true'", self.jobs["publish"]["if"])
        guard = self.jobs["admit"]["steps"][0]["run"]
        self.assertIn('test "$GITHUB_REF" = \'refs/heads/main\'', guard)
        self.assertIn('test "$GITHUB_RUN_ATTEMPT" = \'1\'', guard)
        for name, job in self.jobs.items():
            if name != "publish":
                with self.subTest(job=name):
                    self.assertNotIn("environment", job)
                    self.assertNotIn("secrets", job)
                    self.assertNotIn("secrets.", json.dumps(job))


if __name__ == "__main__":
    unittest.main()
