"""Sanitized same-run evidence; no native approval or publication."""
import copy
from datetime import datetime, timedelta, timezone
import io
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import consolidated_executor_approval_summary as summary
import test_consolidated_package_executor as fixtures

executor = summary.executor


class SummaryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        fixtures.FullInventoryTests.setUpClass()
        cls.addClassCleanup(fixtures.FullInventoryTests.doClassCleanups)
        fixture = fixtures.FullInventoryTests()
        fixture.setUp()
        cls.original = fixture.run_executor(mode="verify")

    def setUp(self):
        self.context = {"GITHUB_ACTIONS": "true", "GITHUB_REPOSITORY": executor.REPOSITORY,
                        "GITHUB_REF": "refs/heads/main", "GITHUB_EVENT_NAME": "workflow_dispatch",
                        "GITHUB_SHA": "a"*40, "GITHUB_RUN_ID": "99", "GITHUB_RUN_ATTEMPT": "1"}
        context = patch.dict(os.environ, self.context, clear=True)
        context.start()
        self.addCleanup(context.stop)
        self.verification = copy.deepcopy(self.original)
        current = datetime.now(timezone.utc)
        stamp = lambda seconds: (current-timedelta(seconds=seconds)).isoformat()
        planner = {"source_commit": self.context["GITHUB_SHA"], "run_id": 99, "run_attempt": 1}
        provenance = self.verification["original_provenance"]
        provenance.update(archive_sha256=executor.candidate_input.ARCHIVE_SHA256,
                          original_envelope_sha256=executor.candidate_input.ENVELOPE_SHA256,
                          preupload_manifest_sha256=executor.candidate_input.MANIFEST_SHA256,
                          observed_at=stamp(5), planner=planner)
        self.verification["before"]["packages"]["original_provenance"] = copy.deepcopy(provenance)
        self.verification.update(scope="production", started_at=stamp(4), finished_at=stamp(3),
            inspector_source_sha256=executor.sha(Path(executor.__file__).with_name("VerifyPackageSymbolPair").joinpath("Program.cs").read_bytes()))
        # Give the receipt the actual candidate's association count without
        # creating additional archives or pretending this fixture is that proof.
        for index, row in enumerate(self.verification["associations"]):
            for framework in ("net9.0",) if index < 7 else ("net9.0", "net10.0"):
                item = copy.deepcopy(row["associations"][0])
                item["framework"] = framework
                digest = executor.sha((row["id"]+framework).encode())
                item.update(key=f"synthetic.pdb/{digest[:32]}FFFFFFFF/synthetic.pdb", pdb_sha256=digest)
                row["associations"].append(item)
                self.verification["before"]["symbols"].append({"key": item["key"], "classification": "missing", "complete": True,
                    "status": 404, "failure_category": None, "pdb_sha256": None})
        self.admission = {"schema": 1, "result": "eligible_for_native_approval", "failure_category": None,
            "publication_performed": False, "publication_ready": False, "admission": {**planner,
            "status": "eligible_for_native_approval", "repository": executor.REPOSITORY, "ref": "refs/heads/main",
            "executor_source": planner["source_commit"], "implementation_sha256": self.verification["implementation_sha256"],
            "environment_name": executor.ENVIRONMENT, "environment_id": 1, "reviewer_ids": [2],
            "branch_policies": [{"id": 3, "type": "branch", "name": "main"}], "policy_sha256": "a"*64,
            "operational_packet_sha256": "b"*64, "observed_at": stamp(2), "native_approval_verified": False,
            "credential_provenance_verified": False, "credential_isolation": "pending_authenticated_execution_check",
            "candidate": {**executor.candidate_input.PRODUCER, "archive_sha256": executor.candidate_input.ARCHIVE_SHA256,
                "preupload_manifest_sha256": executor.candidate_input.MANIFEST_SHA256, "expires_at": executor.EXPIRY,
                "package_count": 225, "exclusion_count": 124, "excluded_id_count": 123}}}

    def render(self):
        return summary.render_summary(self.admission, self.verification, artifact_id=1234, artifact_digest="sha256:"+"c"*64,
                                      admission_hash="d"*64, verification_hash="e"*64)

    def test_complete_missing_observation_reports_actual_668_and_immutable_bindings(self):
        text = self.render()
        for value in (self.context["GITHUB_SHA"], executor.candidate_input.SOURCE, executor.candidate_input.ARCHIVE_SHA256,
                      executor.candidate_input.MANIFEST_SHA256, executor.candidate_input.ENVELOPE_SHA256,
                      self.verification["original_provenance"]["inventory_sha256"], "668 DLL/PDB associations", "668 distinct PDB keys",
                      "225 package pairs / 450 original archives", "0 matching, 225 missing", "Native approval is **pending**",
                      "Publication readiness remains **false**", "not release acceptance", "does not independently reconstruct",
                      "/actions/runs/99/artifacts/1234"):
            self.assertIn(value, text)

    def test_all_matching_still_does_not_imply_release_acceptance(self):
        proof = self.verification["before"]["packages"]
        for row in proof["packages"]:
            row.update(classification="matching", comparison={key: True for key in ("identity", "source", "dependencies", "payload")})
            row["remote"].update(status=200, archive_sha256="a"*64, payload_sha256=row["local_payload_sha256"])
        pdbs = {item["key"]: item["pdb_sha256"] for row in self.verification["associations"] for item in row["associations"]}
        for row in self.verification["before"]["symbols"]:
            row.update(classification="matching", status=200, pdb_sha256=pdbs[row["key"]])
        proof.update(counts=summary.counts(proof["packages"]), content_converged=True)
        self.verification["before"]["content_verified"] = self.verification["content_verified"] = True
        self.assertIn("225 matching, 0 missing", self.render())
        self.assertIn("Publication readiness remains **false**", self.render())

    def test_runtime_and_receipt_identity_mismatch_block(self):
        for key, value in (("GITHUB_SHA", "b"*40), ("GITHUB_RUN_ID", "100"), ("GITHUB_RUN_ATTEMPT", "2"),
                           ("GITHUB_REPOSITORY", "fork/repo"), ("GITHUB_REF", "refs/tags/3.10.0"), ("GITHUB_EVENT_NAME", "push")):
            with self.subTest(field=key), patch.dict(os.environ, {key: value}), self.assertRaises(summary.SummaryError):
                self.render()
        self.verification["original_provenance"]["planner"]["run_id"] = 100
        with self.assertRaisesRegex(summary.SummaryError, "original_identity_invalid"):
            self.render()

    def test_native_or_publication_receipts_cannot_be_summary_authority(self):
        for location, changes in (("schedule", {"result": "native_approval_verified"}),
                                  ("admission", {"native_approval_verified": True}),
                                  ("verification", {"mode": "publish"}), ("verification", {"scope": "injected_simulation"}),
                                  ("verification", {"upload_attempted": True}), ("verification", {"publication_performed": True}),
                                  ("verification", {"result": "failed"}), ("verification", {"publication_ready": True})):
            target = self.admission if location == "schedule" else self.admission["admission"] if location == "admission" else self.verification
            original = copy.deepcopy(target)
            target.update(changes)
            with self.subTest(changes=changes), self.assertRaises(summary.SummaryError):
                self.render()
            target.clear()
            target.update(original)

    def test_original_and_implementation_pins_must_match(self):
        for key in ("archive_sha256", "original_envelope_sha256", "preupload_manifest_sha256", "inventory_sha256"):
            old = self.verification["original_provenance"][key]
            self.verification["original_provenance"][key] = "0"*64
            with self.subTest(pin=key), self.assertRaises(summary.SummaryError):
                self.render()
            self.verification["original_provenance"][key] = old
        self.admission["admission"]["implementation_sha256"] = "0"*64
        with self.assertRaisesRegex(summary.SummaryError, "implementation_identity_invalid"):
            self.render()

    def test_incomplete_conflicting_or_duplicate_package_inventory_blocks(self):
        proof = self.verification["before"]["packages"]
        for mutation in (lambda: proof["packages"].pop(), lambda: proof["packages"].__setitem__(0, proof["packages"][1]),
                         lambda: proof["packages"][0].update(classification="conflicting"),
                         lambda: proof["packages"][0]["remote"].update(complete=False),
                         lambda: proof.update(counts={}), lambda: proof.update(classification_complete=False)):
            original = copy.deepcopy(proof)
            mutation()
            with self.assertRaises(summary.SummaryError):
                self.render()
            proof.clear()
            proof.update(original)

    def test_every_original_operation_and_association_is_accounted_for(self):
        for field, mutation in (("operations", lambda rows: rows.pop()),
                                ("operations", lambda rows: rows[0].update(state="accepted_pending_readback")),
                                ("operations", lambda rows: rows[0].update(archive_sha256="0"*64)),
                                ("associations", lambda rows: rows.pop()),
                                ("associations", lambda rows: rows[0]["associations"].pop()),
                                ("associations", lambda rows: rows[0]["associations"][0].update(source_evidence_preserved=False))):
            original = copy.deepcopy(self.verification[field])
            mutation(self.verification[field])
            with self.subTest(field=field), self.assertRaises(summary.SummaryError):
                self.render()
            self.verification[field] = original
        self.verification["before"]["symbols"][0].update(classification="unverifiable", status=403)
        with self.assertRaisesRegex(summary.SummaryError, "symbol_observation_invalid"):
            self.render()

    def test_completed_ordered_timestamps_and_pending_gates_required(self):
        self.verification["finished_at"] = datetime.now(timezone.utc).isoformat()
        with self.assertRaisesRegex(summary.SummaryError, "receipt_timing_invalid"):
            self.render()
        self.verification["finished_at"] = self.verification["started_at"]
        self.verification["pending_gates"] = []
        with self.assertRaisesRegex(summary.SummaryError, "pending_gates_invalid"):
            self.render()

    def test_cli_hashes_exact_inputs_and_emits_no_untrusted_fields(self):
        self.verification["unrelated_secret_name"] = "PRIVATE_UNRELATED_NAME"
        self.admission["unrelated_error"] = "private credential raw response <script>"
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            admission, verification = root/"admission.json", root/"verification.json"
            admission.write_text(json.dumps(self.admission))
            verification.write_text(json.dumps(self.verification))
            args = ["--admission", str(admission), "--verification", str(verification), "--artifact-id", "1234", "--artifact-digest", "sha256:"+"c"*64]
            output, errors = io.StringIO(), io.StringIO()
            with patch("sys.stdout", output), patch("sys.stderr", errors):
                self.assertEqual(summary.main(args), 0)
            self.assertEqual(errors.getvalue(), "")
            self.assertIn(executor.sha(verification.read_bytes()), output.getvalue())
            self.assertIn(executor.sha(admission.read_bytes()), output.getvalue())
            self.assertNotIn("PRIVATE_UNRELATED_NAME", output.getvalue())
            self.assertNotIn("private credential", output.getvalue())
            for invalid in ('{"schema":1,"schema":1,"private":"never echo"}', "invalid-private-response"):
                verification.write_text(invalid)
                output, errors = io.StringIO(), io.StringIO()
                with patch("sys.stdout", output), patch("sys.stderr", errors):
                    self.assertEqual(summary.main(args), 1)
                self.assertEqual(output.getvalue(), "")
                self.assertEqual(errors.getvalue(), "summary_evidence_invalid\n")
            verification.unlink()
            verification.symlink_to(admission)
            with patch("sys.stdout", io.StringIO()), patch("sys.stderr", io.StringIO()):
                self.assertEqual(summary.main(args), 1)

    def test_cli_size_bound_rejects_before_reading_or_rendering(self):
        with tempfile.TemporaryDirectory() as directory:
            receipt = Path(directory)/"oversized.json"
            with receipt.open("wb") as stream:
                stream.truncate(32*1024*1024+1)
            output, errors = io.StringIO(), io.StringIO()
            with patch("sys.stdout", output), patch("sys.stderr", errors):
                self.assertEqual(summary.main(["--admission", str(receipt), "--verification", str(receipt),
                    "--artifact-id", "1234", "--artifact-digest", "sha256:"+"a"*64]), 1)
            self.assertEqual(output.getvalue(), "")
            self.assertEqual(errors.getvalue(), "summary_evidence_invalid\n")

    def test_argument_errors_are_fixed_categories_without_raw_input(self):
        output, errors = io.StringIO(), io.StringIO()
        with patch("sys.stdout", output), patch("sys.stderr", errors):
            self.assertEqual(summary.main(["--admission", "unused", "--verification", "unused", "--artifact-id",
                "private-invalid-value", "--artifact-digest", "private-invalid-digest"]), 1)
        self.assertEqual(output.getvalue(), "")
        self.assertEqual(errors.getvalue(), "summary_arguments_invalid\n")

    def test_artifact_fields_reject_markdown_or_unknown_identity(self):
        for artifact_id, digest in ((0, "sha256:"+"c"*64), (1234, "c"*64), (1234, "sha256:<script>")):
            with self.assertRaisesRegex(summary.SummaryError, "artifact_identity_invalid"):
                summary.render_summary(self.admission, self.verification, artifact_id=artifact_id, artifact_digest=digest,
                                       admission_hash="a"*64, verification_hash="b"*64)


if __name__ == "__main__":
    unittest.main()
