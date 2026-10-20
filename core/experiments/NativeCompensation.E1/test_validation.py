"""Cheap assertion mutation tests; no .NET process, participant, or database execution."""
import copy
import hashlib
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import controls
import run
import summarize
from validation import KEY, load_evidence, validate_control, validate_matrix

MATRIX = Path(os.environ.get("E1_MATRIX_EVIDENCE", "/tmp/elsa-e1-r5"))
CONTROLS = Path(os.environ.get("E1_CONTROL_EVIDENCE", "/tmp/elsa-e1-controls-r2"))


def record(name, control=False):
    return json.loads(((CONTROLS if control else MATRIX) / name / "evidence.json").read_text())


class ValidationTests(unittest.TestCase):
    def rejected(self, name, change, control=False):
        candidate = copy.deepcopy(record(name, control))
        change(candidate)
        with self.assertRaises(AssertionError):
            (validate_control if control else validate_matrix)(candidate)

    def test_all_retained_cases_and_original_raw_hashes(self):
        self.assertEqual(16, len(load_evidence(MATRIX, "matrix")))
        self.assertEqual(8, len(load_evidence(CONTROLS, "controls")))
        summary = json.loads((Path(__file__).parent / "evidence-summary.json").read_text())
        for item in summary["matrix"] + summary["controls"]:
            self.assertEqual(item["evidenceSha256"], hashlib.sha256(Path(item["evidenceFile"]).read_bytes()).hexdigest())

    def test_missing_work_wrong_native_outcome_is_rejected(self):
        self.rejected("missing-work", lambda r: r["after"]["elsa"]["rows"][0]["snapshot"].update(subStatus="Finished"), True)

    def test_missing_work_retained_invalid_reference_is_rejected(self):
        self.rejected("missing-work", lambda r: r["after"]["elsa"]["rows"][0]["snapshot"]["scheduledActivities"]["$values"].append({"activityNodeId": "required-work-node-removed"}), True)

    def test_missing_work_slot_one_effect_is_rejected(self):
        self.rejected("missing-work", lambda r: r["after"]["participant"]["operations"].append({"slot": 1, "closed": 0, "undo_effects": 0}), True)

    def test_callback_settled_obligation_is_rejected(self):
        self.rejected("callback-owner", lambda r: r["after"]["participant"]["operations"][0].update(closed=1), True)

    def test_bad_native_status_is_rejected(self):
        self.rejected("baseline", lambda r: r["observed_result"].update(status="Running"))

    def test_wrong_exit_kill_pid_and_preintent_effect_are_rejected(self):
        mutations = [lambda r: r["restart"].update(exit=2), lambda r: r["first"].update(exit=0),
                     lambda r: r["restart"].update(pid=r["first"]["pid"]),
                     lambda r: r["after_first"]["participant"]["calls"].append({"direction": "reserve"})]
        for change in mutations:
            with self.subTest(change=change):
                self.rejected("after-intent", change)

    def test_fault_owned_missing_undo_and_failure_missing_bookmark_rejected(self):
        self.rejected("fault-owned", lambda r: r["final"]["participant"]["operations"][0].update(undo_effects=0))
        self.rejected("undo-failure", lambda r: r["final"]["elsa"]["rows"][0]["snapshot"]["bookmarks"].update({"$values": []}))

    def test_case_mismatch_duplicate_and_missing_case_are_rejected(self):
        source = MATRIX / "baseline" / "evidence.json"
        with patch.object(Path, "glob", return_value=[source, source]):
            with self.assertRaisesRegex(AssertionError, "Duplicate"):
                load_evidence(MATRIX, "matrix")
        with patch.object(Path, "glob", return_value=[source]):
            with self.assertRaisesRegex(AssertionError, "case set"):
                load_evidence(MATRIX, "matrix")
        with tempfile.TemporaryDirectory() as directory:
            mismatch = Path(directory) / "wrong-directory"
            mismatch.mkdir()
            (mismatch / "evidence.json").write_text(source.read_text())
            with self.assertRaisesRegex(AssertionError, "Directory/case"):
                load_evidence(Path(directory), "matrix")

    def test_recorded_empty_failure_is_not_success(self):
        self.rejected("baseline", lambda r: r.update(assertion_failure=""))

    def test_controller_failure_exit_and_unknown_case(self):
        with tempfile.TemporaryDirectory() as directory:
            with patch.object(run, "run_case", return_value=False):
                self.assertEqual(1, run.main([directory, "--case", "baseline"]))
            with patch.object(controls, "run_control", return_value=False):
                self.assertEqual(1, controls.main([directory]))
            with self.assertRaises(SystemExit) as error:
                run.main([directory, "--case", "not-a-case"])
            self.assertEqual(2, error.exception.code)

    def test_explicit_dotnet_host_override(self):
        with patch.dict(os.environ, {"E1_DOTNET": "explicit-host"}), patch.object(run.shutil, "which", return_value="/chosen/dotnet") as which:
            self.assertEqual("/chosen/dotnet", run.dotnet_host())
            which.assert_called_once_with("explicit-host")
        with patch.dict(os.environ, {}, clear=True), patch.object(run.shutil, "which", return_value="/path/dotnet") as which:
            self.assertEqual("/path/dotnet", run.dotnet_host())
            which.assert_called_once_with("dotnet")

    def test_summary_hashes_protected_capture_and_drops_payload(self):
        journal = copy.deepcopy(record("baseline")["observed_result"]["journal"])
        sentinel = "CfDJ-DO-NOT-EXPORT-PROTECTED-CAPTURE"
        journal["Entries"][0]["ProtectedRequest"] = sentinel
        journal["ProtectedOriginalCause"] = sentinel
        exported = summarize.journal_summary(journal)
        self.assertNotIn(sentinel, json.dumps(exported))
        self.assertEqual(hashlib.sha256(sentinel.encode()).hexdigest(), exported["entries"][0]["protectedCaptureHashes"]["ProtectedRequest"])
        retained_summary = (Path(__file__).parent / "evidence-summary.json").read_text()
        self.assertNotIn("CfDJ", retained_summary)
        self.assertNotIn("synthetic-resource", retained_summary)


if __name__ == "__main__":
    unittest.main()
