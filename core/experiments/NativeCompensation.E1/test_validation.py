"""Cheap validation/controller-cleanup tests; synthetic children only, no fixture participant or database."""
import copy
import hashlib
import json
import os
import queue
from pathlib import Path
import subprocess
import sys
import tempfile
import threading
from types import SimpleNamespace
import unittest
from unittest.mock import patch

import controls
import run
import summarize
from validation import INSPECTION_CONTROL_CASES, KEY, load_evidence, validate_control, validate_matrix

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

    def check_retained_files(self, matrix, controls):
        self.assertEqual(16, len(load_evidence(matrix, "matrix")))
        self.assertEqual(8, len(load_evidence(controls, "controls")))
        summary = json.loads((Path(__file__).parent / "evidence-summary.json").read_text())
        for category, root in (("matrix", matrix), ("controls", controls)):
            for item in summary[category]:
                file = root / item["case"] / "evidence.json"
                self.assertEqual(item["evidenceSha256"], hashlib.sha256(file.read_bytes()).hexdigest())

    def test_all_retained_cases_and_original_raw_hashes(self):
        self.check_retained_files(MATRIX, CONTROLS)

    def test_relocated_records_validate_without_reading_original_paths(self):
        with tempfile.TemporaryDirectory() as directory:
            relocated = [Path(directory) / "matrix", Path(directory) / "controls"]
            for source, target in zip((MATRIX, CONTROLS), relocated):
                for file in source.glob("*/evidence.json"):
                    destination = target / file.parent.name / file.name
                    destination.parent.mkdir(parents=True, exist_ok=True)
                    destination.write_bytes(file.read_bytes())
            original_open = Path.open
            def guarded_open(path, *args, **kwargs):
                self.assertFalse(any(path.resolve().is_relative_to(root.resolve()) for root in (MATRIX, CONTROLS)), "Original evidence path read")
                return original_open(path, *args, **kwargs)
            with patch.object(Path, "open", guarded_open):
                self.check_retained_files(*relocated)

    def test_optimized_python_fails_before_controller_execution(self):
        here = Path(__file__).parent.resolve()
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "must-not-be-created"
            commands = [[sys.executable, "-O", "-c", "import validation"],
                        [sys.executable, "-O", str(here / "run.py"), str(output), "--case", "baseline"]]
            for command in commands:
                with self.subTest(command=command):
                    result = subprocess.run(command, cwd=here, capture_output=True, text=True)
                    self.assertNotEqual(0, result.returncode)
                    self.assertIn("requires Python assertions", result.stderr)
                    self.assertFalse(output.exists())

    def test_missing_work_wrong_native_outcome_is_rejected(self):
        self.rejected("missing-work", lambda r: r["after"]["elsa"]["rows"][0]["snapshot"].update(subStatus="Finished"), True)

    def test_missing_work_retained_invalid_reference_is_rejected(self):
        self.rejected("missing-work", lambda r: r["after"]["elsa"]["rows"][0]["snapshot"]["scheduledActivities"]["$values"].append({"activityNodeId": "required-work-node-removed"}), True)

    def test_missing_work_slot_one_effect_is_rejected(self):
        self.rejected("missing-work", lambda r: r["after"]["participant"]["operations"].append({"slot": 1, "closed": 0, "undo_effects": 0}), True)

    def test_callback_settled_obligation_is_rejected(self):
        self.rejected("callback-owner", lambda r: r["after"]["participant"]["operations"][0].update(closed=1), True)

    def test_nonzero_inspection_exit_is_rejected_for_every_inspection_control(self):
        for name in sorted(INSPECTION_CONTROL_CASES):
            with self.subTest(name=name):
                candidate = copy.deepcopy(record(name, control=True))
                candidate["inspection"]["exit"] = 7
                with self.assertRaisesRegex(AssertionError, "Inspection process did not exit successfully"):
                    validate_control(candidate)

    def test_bad_native_status_is_rejected(self):
        candidate = copy.deepcopy(record("baseline"))
        candidate["observed_result"]["status"] = "Running"
        final = next(event["value"] for event in candidate["first"]["events"] if event["kind"] == "result")
        final["status"] = "Running"
        candidate["final"]["elsa"]["rows"][0]["snapshot"]["status"] = "Running"
        with self.assertRaisesRegex(AssertionError, "Unexpected native status/substatus"):
            validate_matrix(candidate)

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

    def test_participant_startup_failures_are_bounded_reaped_and_diagnostic(self):
        children = {
            "premature-exit": (
                "import sys; sys.stderr.write('premature participant stderr\\n'); "
                "sys.stderr.flush(); sys.exit(7)"
            ),
            "malformed-port": (
                "import sys,time; sys.stderr.write('malformed participant stderr\\n'); "
                "sys.stderr.flush(); print('{\\\"port\\\":\\\"invalid\\\"}', flush=True); time.sleep(30)"
            ),
            "stalled-handshake": (
                "import sys,time; sys.stderr.write('stalled participant stderr\\n'); "
                "sys.stderr.flush(); time.sleep(30)"
            ),
        }
        for name, child in children.items():
            with self.subTest(name=name):
                result = {"participant_pid": None}
                with self.assertRaises(run.ParticipantFailure):
                    with run.ParticipantProcess(result, [sys.executable, "-u", "-c", child], timeout_seconds=1):
                        self.fail("Invalid participant startup unexpectedly succeeded")
                diagnostics = result["participant"]
                self.assertIsNotNone(diagnostics["pid"])
                self.assertTrue(diagnostics["reaped"])
                self.assertTrue(diagnostics["stdout_reader_completed"])
                self.assertTrue(diagnostics["stderr_reader_completed"])
                self.assertTrue(diagnostics["controller_error"])
                self.assertIsNotNone(diagnostics["exit"])
                if name == "premature-exit":
                    self.assertEqual(7, diagnostics["exit"])
                    self.assertIn("closed stdout", diagnostics["controller_error"])
                    self.assertIn("participant stderr", diagnostics["stderr"])
                elif name == "malformed-port":
                    self.assertIn("invalid port", diagnostics["controller_error"])
                    self.assertIn("participant stderr", diagnostics["stderr"])
                    self.assertTrue(diagnostics["cleanup_terminated"])
                else:
                    self.assertIn("TimeoutError", diagnostics["controller_error"])
                    if diagnostics["stderr"]:
                        self.assertIn("participant stderr", diagnostics["stderr"])
                    self.assertTrue(diagnostics["cleanup_terminated"])
                if os.name == "posix":
                    with self.assertRaises(ProcessLookupError):
                        os.kill(diagnostics["pid"], 0)

    def test_participant_valid_port_enters_body_and_reaps_on_context_exit(self):
        child = "import json,time; print(json.dumps({'port':43210}), flush=True); time.sleep(30)"
        result = {"participant_pid": None}
        participant = run.ParticipantProcess(result, [sys.executable, "-u", "-c", child], timeout_seconds=1)
        with participant as url:
            self.assertEqual("http://127.0.0.1:43210/", url)
            self.assertIsNotNone(participant.process)
            self.assertIsNone(participant.process.poll())
            self.assertEqual(43210, result["participant"]["port"])
        diagnostics = result["participant"]
        self.assertTrue(diagnostics["cleanup_terminated"])
        self.assertTrue(diagnostics["reaped"])
        self.assertIsNotNone(diagnostics["exit"])
        self.assertIsNone(diagnostics["controller_error"])
        self.assertIsNone(diagnostics["cleanup_error"])
        self.assertTrue(diagnostics["stdout_reader_completed"])
        self.assertTrue(diagnostics["stderr_reader_completed"])
        if os.name == "posix":
            with self.assertRaises(ProcessLookupError):
                os.kill(diagnostics["pid"], 0)

    def test_matrix_and_control_startup_failures_are_saved_without_orphans(self):
        malformed = (
            "import time; print('{\\\"port\\\":null}', flush=True); time.sleep(30)"
        )
        stalled = "import sys,time; sys.stderr.write('control startup stderr\\n'); sys.stderr.flush(); time.sleep(30)"
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            matrix_output = root / "matrix"
            matrix_output.mkdir()
            participant_factory = run.ParticipantProcess
            with patch.object(run, "args", SimpleNamespace(output=matrix_output), create=True), \
                 patch.object(run, "participant_command", return_value=[sys.executable, "-u", "-c", malformed]), \
                 patch.object(run, "ParticipantProcess", side_effect=lambda result, command:
                              participant_factory(result, command, timeout_seconds=1)):
                self.assertFalse(run.run_case("startup-failure"))
            matrix_evidence = json.loads((matrix_output / "startup-failure" / "evidence.json").read_text())
            self.assertIn("assertion_failure", matrix_evidence)
            self.assertIn("invalid port", matrix_evidence["participant"]["controller_error"])
            self.assertTrue(matrix_evidence["participant"]["reaped"])

            control_output = root / "controls"
            control_output.mkdir()
            with patch.object(controls.native, "participant_command", return_value=[sys.executable, "-u", "-c", stalled]), \
                 patch.object(controls.native, "ParticipantProcess", side_effect=lambda result, command:
                              participant_factory(result, command, timeout_seconds=1)):
                self.assertFalse(controls.run_control("startup-failure", control_output))
            control_evidence = json.loads((control_output / "startup-failure" / "evidence.json").read_text())
            self.assertIn("control_error", control_evidence)
            self.assertIn("TimeoutError", control_evidence["participant"]["controller_error"])
            self.assertIn("control startup stderr", control_evidence["participant"]["stderr"])
            self.assertTrue(control_evidence["participant"]["reaped"])
            for evidence in (matrix_evidence, control_evidence):
                if os.name == "posix":
                    with self.assertRaises(ProcessLookupError):
                        os.kill(evidence["participant"]["pid"], 0)

    def test_backend_malformed_stdout_reaps_child_and_keeps_prior_diagnostics(self):
        child = (
            "import json,sys,time; "
            "sys.stderr.write('synthetic backend stderr\\n'); sys.stderr.flush(); "
            "print(json.dumps({'kind':'probe','value':{'seen':True}}), flush=True); "
            "print('not json', flush=True); time.sleep(30)"
        )
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaises(run.BackendFailure) as raised:
                run.backend(Path(directory), "unused", command_override=[sys.executable, "-u", "-c", child])
        result = raised.exception.result
        self.assertIn("JSONDecodeError", result["controller_error"])
        self.assertEqual([{"kind": "probe", "value": {"seen": True}}], result["events"])
        self.assertIn("synthetic backend stderr", result["stderr"])
        self.assertTrue(result["cleanup_terminated"])
        self.assertTrue(result["reaped"])
        self.assertTrue(result["stdout_reader_completed"])
        self.assertTrue(result["stderr_reader_completed"])
        if os.name == "posix":
            with self.assertRaises(ProcessLookupError):
                os.kill(result["pid"], 0)

    def test_backend_timeout_reaps_child_and_keeps_stderr(self):
        child = (
            "import json,sys,time; "
            "sys.stderr.write('timeout stderr\\n'); sys.stderr.flush(); "
            "print(json.dumps({'kind':'probe','value':{'started':True}}), flush=True); "
            "time.sleep(30)"
        )
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaises(run.BackendFailure) as raised:
                run.backend(Path(directory), "unused", timeout_seconds=1,
                            command_override=[sys.executable, "-u", "-c", child])
        result = raised.exception.result
        self.assertIn("TimeoutError", result["controller_error"])
        self.assertIn("timeout stderr", result["stderr"])
        self.assertEqual([{"kind": "probe", "value": {"started": True}}], result["events"])
        self.assertTrue(result["cleanup_terminated"])
        self.assertTrue(result["reaped"])
        self.assertTrue(result["stdout_reader_completed"])
        self.assertTrue(result["stderr_reader_completed"])

    def test_backend_waits_for_child_exit_after_stdout_eof(self):
        child = (
            "import json,os,sys,time; "
            "print(json.dumps({'kind':'probe','value':{'complete':True}}), flush=True); "
            "os.close(sys.stdout.fileno()); time.sleep(0.25)"
        )
        with tempfile.TemporaryDirectory() as directory:
            result = run.backend(Path(directory), "unused", timeout_seconds=2,
                                 command_override=[sys.executable, "-u", "-c", child])
        self.assertEqual([{"kind": "probe", "value": {"complete": True}}], result["events"])
        self.assertEqual(0, result["exit"])
        self.assertTrue(result["reaped"])
        self.assertFalse(result["cleanup_terminated"])

    def test_stdout_reader_error_after_cut_barrier_fails_the_phase(self):
        class BarrierThenReaderErrorQueue(queue.Queue):
            def __init__(self):
                super().__init__()
                self.reader_error_queued = threading.Event()

            def put(self, item, block=True, timeout=None):
                super().put(item, block, timeout)
                if item[0] == "reader_error":
                    self.reader_error_queued.set()

            def get(self, block=True, timeout=None):
                item = super().get(block, timeout)
                if item[0] == "line":
                    event = json.loads(item[1])
                    if event.get("kind") == "barrier" and event["value"].get("name") == "cut":
                        if not self.reader_error_queued.wait(5):
                            raise AssertionError("Child did not produce the synchronized stdout reader error")
                return item

        child = (
            "import os,time; "
            "os.write(1, b'{\"kind\":\"barrier\",\"value\":{\"name\":\"cut\"}}\\n'); "
            "time.sleep(0.1); os.write(1, b'\\xff\\n'); "
            "time.sleep(30)"
        )
        with tempfile.TemporaryDirectory() as directory, patch.object(run.queue, "Queue", BarrierThenReaderErrorQueue):
            with self.assertRaises(run.BackendFailure) as raised:
                run.backend(Path(directory), "unused", kill_at="cut", timeout_seconds=2,
                            command_override=[sys.executable, "-u", "-c", child])
        result = raised.exception.result
        self.assertIn("UnicodeDecodeError", result["controller_error"])
        self.assertIn("UnicodeDecodeError", result["stdout_reader_error"])
        self.assertEqual([{"kind": "barrier", "value": {"name": "cut"}}], result["events"])
        self.assertTrue(result["cleanup_terminated"])
        self.assertTrue(result["reaped"])
        self.assertTrue(result["stdout_reader_completed"])

    def test_failed_backend_phase_is_preserved_by_shared_helper(self):
        diagnostics = {"pid": 42, "events": [{"kind": "before-error"}], "stderr": "diagnostic",
                       "controller_error": "ValueError: invalid JSON"}
        with patch.object(run, "backend", side_effect=run.BackendFailure("invalid JSON", diagnostics)):
            recorded = {}
            with self.assertRaises(run.BackendFailure):
                run.run_backend_phase(recorded, "control", Path("."), "unused")
        self.assertIs(recorded["control"], diagnostics)

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
