"""Lightweight process contracts; these never launch npm, a browser or a host."""
import json
import os
from pathlib import Path
import signal
import subprocess
import sys
import tempfile
import time
from types import SimpleNamespace
import unittest
from unittest.mock import Mock, patch

import run_paired_package_browser_matrix as matrix


@unittest.skipUnless(sys.platform == "darwin" or sys.platform.startswith("linux"), "Requires process creation identities")
class BrowserProcessContracts(unittest.TestCase):
    def test_normal_process_keeps_private_input_and_output_transport(self):
        command = [sys.executable, "-c", "import sys; print(sys.stdin.read()); print('private stderr', file=sys.stderr)"]
        result = matrix._run_browser_process(command, cwd=Path(__file__).parent, input="private credentials", env=os.environ.copy(), timeout=5)
        self.assertEqual("private credentials\n", result.stdout)
        self.assertEqual("private stderr\n", result.stderr)
        self.assertEqual(0, result.returncode)
        self.assertNotIn("private credentials", result.args)

    def test_timeout_kills_descendants_in_separate_sessions_and_preserves_unrelated_process(self):
        with tempfile.TemporaryDirectory() as directory:
            receipt = Path(directory) / "pids.json"
            # Both descendants change session, and ignore TERM to require the bounded KILL fallback.
            grandchild = "import signal,time; signal.signal(signal.SIGTERM, signal.SIG_IGN); print('ready', flush=True); time.sleep(60)"
            child = ("import subprocess,sys,signal,time,json,os; import run_paired_package_browser_matrix as m; "
                     "signal.signal(signal.SIGTERM, signal.SIG_IGN); "
                     f"p=subprocess.Popen([sys.executable,'-c',{grandchild!r}], start_new_session=True, stdout=subprocess.PIPE, text=True); "
                     "p.stdout.readline(); print(json.dumps([[pid,m._browser_process_info(pid)[1]] for pid in [os.getpid(),p.pid]]), flush=True); time.sleep(60)")
            parent = ("import subprocess,sys,json,time; from pathlib import Path; "
                      f"p=subprocess.Popen([sys.executable,'-c',{child!r}], start_new_session=True, stdout=subprocess.PIPE, text=True); "
                      f"Path({str(receipt)!r}).write_text(p.stdout.readline()); time.sleep(60)")
            unrelated = subprocess.Popen([sys.executable, "-c", "import time; time.sleep(60)"], start_new_session=True)
            self.addCleanup(self.stop_child, unrelated)
            started = time.monotonic()
            try:
                with self.assertRaises(subprocess.TimeoutExpired):
                    matrix._run_browser_process([sys.executable, "-c", parent], cwd=Path(__file__).parent, input="private credentials",
                                                env=os.environ.copy(), timeout=15)
                self.assertTrue(receipt.is_file(), "Fake descendants did not start within the timeout")
                for pid, started_identity in json.loads(receipt.read_text()):
                    stamp = tuple(started_identity) if isinstance(started_identity, list) else started_identity
                    info = matrix._browser_process_info(pid)
                    self.assertTrue(info is None or info[1] != stamp or info[2], f"Owned descendant {pid} is still running")
                self.assertIsNone(unrelated.poll(), "Cleanup terminated an unrelated process")
                self.assertLess(time.monotonic() - started, 35)
            finally:
                if receipt.is_file() and receipt.read_text():
                    for pid, started_identity in json.loads(receipt.read_text()):
                        stamp = tuple(started_identity) if isinstance(started_identity, list) else started_identity
                        matrix._signal_browser_process(pid, stamp, signal.SIGKILL)

    @staticmethod
    def stop_child(process):
        if process.poll() is None:
            process.kill()
        process.wait(timeout=2)

    def test_exited_root_with_pipe_holding_descendant_is_explicitly_unverified(self):
        with tempfile.TemporaryDirectory() as directory:
            receipt = Path(directory) / "orphan.json"
            child = "import time; time.sleep(60)"
            parent = ("import subprocess,sys,json; from pathlib import Path; import run_paired_package_browser_matrix as m; "
                      f"p=subprocess.Popen([sys.executable,'-c',{child!r}], start_new_session=True); "
                      f"Path({str(receipt)!r}).write_text(json.dumps([p.pid,m._browser_process_info(p.pid)[1]]))")
            try:
                with self.assertRaisesRegex(matrix.BrowserCleanupUnverified, "cleanup could not be verified"):
                    matrix._run_browser_process([sys.executable, "-c", parent], cwd=Path(__file__).parent,
                                                input="", env=os.environ.copy(), timeout=5)
                self.assertTrue(receipt.is_file(), "Fake parent did not exit within the timeout")
                pid, started = json.loads(receipt.read_text())
                info = matrix._browser_process_info(pid)
                self.assertIsNotNone(info, "Cleanup guessed ownership of a reparented process")
                self.assertFalse(info[2])
            finally:
                if receipt.is_file():
                    pid, started = json.loads(receipt.read_text())
                    stamp = tuple(started) if isinstance(started, list) else started
                    matrix._signal_browser_process(pid, stamp, signal.SIGKILL)

    def test_unverified_cleanup_remains_sanitized_at_public_boundary(self):
        handle = SimpleNamespace(studio_url="http://localhost:1", backend_url="http://localhost:2",
                                 username="private username", password="private password", safe_ids={})
        error = matrix.BrowserCleanupUnverified("Browser process cleanup could not be verified")
        with patch.object(matrix, "_run_browser_process", side_effect=error):
            with self.assertRaises(matrix.BrowserCleanupUnverified) as caught:
                matrix.run_browser(handle, {"version": "3.8.4", "framework": "net10.0", "host": "server"}, [])
        self.assertEqual("Browser process cleanup could not be verified", str(caught.exception))

    def test_linux_identity_parser_handles_names_and_stopped_state(self):
        stat = "123 (fake process (name)) " + " ".join(["T", "9", *(["0"] * 17), "456"])
        with patch.object(matrix.sys, "platform", "linux"), patch.object(matrix.Path, "read_text", return_value=stat):
            self.assertEqual((9, 456, False, True), matrix._browser_process_info(123))
        with patch.object(matrix.sys, "platform", "linux"), patch.object(matrix.Path, "read_text", side_effect=FileNotFoundError):
            self.assertIsNone(matrix._browser_process_info(123))

    def test_reused_pid_is_never_signalled(self):
        with patch.object(matrix, "_browser_process_info", return_value=(1, "new-identity", False)), patch.object(matrix.os, "kill") as kill:
            self.assertFalse(matrix._signal_browser_process(12345, "original-identity", signal.SIGKILL))
        kill.assert_not_called()

    def test_missing_root_ownership_is_explicitly_unverified(self):
        process = Mock(pid=12345)
        with patch.object(matrix, "_browser_process_info", return_value=None), patch.object(matrix.os, "kill") as kill:
            with self.assertRaisesRegex(ValueError, "cleanup could not be verified"):
                matrix._cleanup_browser_process(process, "original-identity")
        kill.assert_not_called()

    def test_failed_inventory_remains_unverified_after_root_termination(self):
        process = subprocess.Popen([sys.executable, "-c", "import time; time.sleep(60)"], start_new_session=True)
        self.addCleanup(self.stop_child, process)
        info = matrix._browser_process_info(process.pid)
        with patch.object(matrix.subprocess, "run", side_effect=subprocess.TimeoutExpired("ps", 2)):
            with self.assertRaisesRegex(ValueError, "cleanup could not be verified"):
                matrix._cleanup_browser_process(process, info[1])
        self.assertIsNotNone(process.returncode)


if __name__ == "__main__":
    unittest.main()
