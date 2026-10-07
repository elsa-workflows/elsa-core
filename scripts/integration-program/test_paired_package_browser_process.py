"""Lightweight process contracts; these never launch npm, a browser or a host."""
import ctypes
import errno
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


class DarwinInventoryContracts(unittest.TestCase):
    def query(self, callback):
        query = Mock(side_effect=callback)
        library = SimpleNamespace(proc_listchildpids=query)
        context = patch.object(matrix.ctypes, "CDLL", return_value=library)
        self.addCleanup(context.stop)
        context.start()
        return query

    def test_count_is_pids_not_bytes_and_errno_is_cleared(self):
        def query(parent, values, size):
            self.assertEqual(12345, parent)
            self.assertEqual(64 * ctypes.sizeof(ctypes.c_int), size)
            self.assertEqual(0, ctypes.get_errno())
            values[0], values[1] = 12, 34
            return 2
        self.query(query)
        ctypes.set_errno(errno.EPERM)
        self.assertEqual([12, 34], matrix._darwin_child_pids(12345))

    def test_full_buffer_is_retried_and_never_treated_as_complete(self):
        calls = []
        def query(_parent, values, size):
            capacity = size // ctypes.sizeof(ctypes.c_int)
            calls.append(capacity)
            for index in range(capacity):
                values[index] = index + 1
            return capacity
        self.query(query)
        with self.assertRaisesRegex(OSError, "truncated"):
            matrix._darwin_child_pids(12345)
        self.assertEqual([64, 256, 1024, 4096], calls)

    def test_full_first_snapshot_followed_by_smaller_snapshot_is_accepted(self):
        def query(_parent, values, size):
            capacity = size // ctypes.sizeof(ctypes.c_int)
            if capacity == 64:
                return capacity
            values[0] = 42
            return 1
        self.query(query)
        self.assertEqual([42], matrix._darwin_child_pids(12345))

    def test_empty_result_differs_from_error_and_invalid_inventory_is_rejected(self):
        query = self.query(lambda *_: 0)
        self.assertEqual([], matrix._darwin_child_pids(12345))
        for count, values, error in ((0, [], errno.EPERM), (-1, [], 0), (65, [], 0),
                                     (1, [0], 0), (1, [12345], 0), (2, [42, 42], 0)):
            def malformed(_parent, buffer, _size):
                ctypes.set_errno(error)
                for index, value in enumerate(values):
                    buffer[index] = value
                return count
            query.side_effect = malformed
            with self.subTest(count=count, values=values, error=error), self.assertRaises(OSError):
                matrix._darwin_child_pids(12345)

    def test_missing_native_symbol_is_sanitized(self):
        with patch.object(matrix.ctypes, "CDLL", return_value=SimpleNamespace()):
            with self.assertRaisesRegex(OSError, "Native process inventory unavailable"):
                matrix._darwin_child_pids(12345)

    def test_missing_identity_symbol_is_sanitized(self):
        with patch.object(matrix.sys, "platform", "darwin"), \
                patch.object(matrix.ctypes, "CDLL", return_value=SimpleNamespace()):
            with self.assertRaisesRegex(OSError, "Process identity unavailable"):
                matrix._browser_process_info(12345)

    def test_missing_identity_symbol_still_attempts_reap(self):
        process = Mock(pid=12345)
        with patch.object(matrix.sys, "platform", "darwin"), \
                patch.object(matrix.ctypes, "CDLL", return_value=SimpleNamespace()):
            with self.assertRaises(matrix.BrowserCleanupUnverified) as caught:
                matrix._cleanup_browser_process(process, "original-identity")
        self.assertEqual(("exit", "root_identity", "signal"), caught.exception.categories)
        process.wait.assert_called_once_with(timeout=2)

    def test_native_discovery_claims_nested_owned_children_without_launching_ps(self):
        current = {pid: (parent, str(pid), False, False) for pid, parent in ((10, 1), (20, 10), (30, 20), (40, 1))}
        def send(pid, stamp, sig):
            info = current.get(pid)
            if info is None or info[1] != stamp:
                return False
            current[pid] = (info[0], stamp, False, True) if sig == signal.SIGSTOP else None
            return True
        process = Mock(pid=10)
        with patch.object(matrix.sys, "platform", "darwin"), \
                patch.object(matrix, "_browser_process_info", side_effect=current.get), \
                patch.object(matrix, "_signal_browser_process", side_effect=send), \
                patch.object(matrix, "_darwin_child_pids", side_effect=lambda pid: {10: [20], 20: [30], 30: []}[pid]), \
                patch.object(matrix.subprocess, "run") as ps:
            matrix._cleanup_browser_process(process, "10")
        self.assertTrue(all(current[pid] is None for pid in (10, 20, 30)))
        self.assertEqual((1, "40", False, False), current[40])
        ps.assert_not_called()
        process.wait.assert_called_once_with(timeout=2)

    def test_parent_disappearing_during_empty_native_snapshot_cannot_prove_cleanup(self):
        current = {10: (1, "10", False, False), 20: (10, "20", False, False)}
        reads = {20: 0}
        def send(pid, stamp, sig):
            info = current.get(pid)
            if info is None or info[1] != stamp:
                return False
            current[pid] = (info[0], stamp, False, True) if sig == signal.SIGSTOP else None
            return True
        def children(pid):
            self.assertTrue(current[pid][3], "Parent was traversed before its stop was confirmed")
            if pid == 10:
                return [20]
            reads[20] += 1
            if reads[20] == 2:
                current[20] = None
            return []
        with patch.object(matrix.sys, "platform", "darwin"), \
                patch.object(matrix, "_browser_process_info", side_effect=current.get), \
                patch.object(matrix, "_signal_browser_process", side_effect=send), \
                patch.object(matrix, "_darwin_child_pids", side_effect=children):
            with self.assertRaises(matrix.BrowserCleanupUnverified) as caught:
                matrix._cleanup_browser_process(Mock(pid=10), "10")
        self.assertEqual(("inventory",), caught.exception.categories)
        self.assertEqual(2, reads[20])

    def test_missing_native_symbol_still_attempts_teardown_and_reap(self):
        current = {10: (1, "10", False, False)}
        def send(pid, stamp, sig):
            info = current.get(pid)
            if info is None or info[1] != stamp:
                return False
            if sig == signal.SIGSTOP:
                current[pid] = (info[0], stamp, False, True)
            elif sig == signal.SIGTERM:
                current[pid] = None
            return True
        process = Mock(pid=10)
        with patch.object(matrix.sys, "platform", "darwin"), \
                patch.object(matrix, "_browser_process_info", side_effect=current.get), \
                patch.object(matrix, "_signal_browser_process", side_effect=send), \
                patch.object(matrix.ctypes, "CDLL", return_value=SimpleNamespace()):
            with self.assertRaises(matrix.BrowserCleanupUnverified) as caught:
                matrix._cleanup_browser_process(process, "10")
        self.assertEqual(("inventory",), caught.exception.categories)
        self.assertIsNone(current[10])
        process.wait.assert_called_once_with(timeout=2)

    def test_root_stop_failure_is_attributed_to_root_stop(self):
        current = {10: (1, "10", False, False)}
        def send(pid, _stamp, sig):
            if sig == signal.SIGSTOP:
                return False
            current[pid] = None
            return True
        process = Mock(pid=10)
        with patch.object(matrix.sys, "platform", "darwin"), \
                patch.object(matrix, "_browser_process_info", side_effect=current.get), \
                patch.object(matrix, "_signal_browser_process", side_effect=send):
            with self.assertRaises(matrix.BrowserCleanupUnverified) as caught:
                matrix._cleanup_browser_process(process, "10")
        self.assertEqual(("root_stop",), caught.exception.categories)
        process.wait.assert_called_once_with(timeout=2)

    def test_descendant_stop_failure_is_attributed_to_descendant_stop(self):
        current = {10: (1, "10", False, False), 20: (10, "20", False, False)}
        def send(pid, stamp, sig):
            info = current.get(pid)
            if info is None or info[1] != stamp:
                return False
            if sig == signal.SIGSTOP and pid == 20:
                return False
            if sig == signal.SIGSTOP:
                current[pid] = (info[0], stamp, False, True)
            elif sig == signal.SIGTERM:
                current[pid] = None
            return True
        process = Mock(pid=10)
        with patch.object(matrix.sys, "platform", "darwin"), \
                patch.object(matrix, "_browser_process_info", side_effect=current.get), \
                patch.object(matrix, "_signal_browser_process", side_effect=send), \
                patch.object(matrix, "_darwin_child_pids", side_effect=lambda pid: [20] if pid == 10 else []):
            with self.assertRaises(matrix.BrowserCleanupUnverified) as caught:
                matrix._cleanup_browser_process(process, "10")
        self.assertEqual(("descendant_stop",), caught.exception.categories)
        process.wait.assert_called_once_with(timeout=2)

    def test_owned_process_cap_is_checked_before_claim(self):
        current = {10: (1, "10", False, False), 20: (10, "20", False, False), 30: (20, "30", False, False)}
        stopped = []
        def send(pid, stamp, sig):
            info = current.get(pid)
            if info is None or info[1] != stamp:
                return False
            if sig == signal.SIGSTOP:
                stopped.append(pid)
                current[pid] = (info[0], stamp, False, True)
            elif sig == signal.SIGTERM:
                current[pid] = None
            return True
        process = Mock(pid=10)
        def children(pid):
            self.assertTrue(current[pid][3])
            return {10: [20], 20: [30], 30: []}[pid]
        with patch.object(matrix.sys, "platform", "darwin"), \
                patch.object(matrix, "_MAX_OWNED_BROWSER_PROCESSES", 2), \
                patch.object(matrix, "_browser_process_info", side_effect=current.get), \
                patch.object(matrix, "_signal_browser_process", side_effect=send), \
                patch.object(matrix, "_darwin_child_pids", side_effect=children):
            with self.assertRaises(matrix.BrowserCleanupUnverified) as caught:
                matrix._cleanup_browser_process(process, "10")
        self.assertEqual(("inventory",), caught.exception.categories)
        self.assertEqual([10, 20], stopped)
        self.assertIsNotNone(current[30])
        process.wait.assert_called_once_with(timeout=2)

    def test_native_inventory_time_budget_is_enforced(self):
        current = {10: (1, "10", False, False)}
        clock = {"now": 0.0}
        def send(pid, stamp, sig):
            info = current.get(pid)
            if info is None or info[1] != stamp:
                return False
            if sig == signal.SIGSTOP:
                current[pid] = (info[0], stamp, False, True)
            elif sig == signal.SIGTERM:
                current[pid] = None
            return True
        def children(_pid):
            clock["now"] = 11.0
            return []
        process = Mock(pid=10)
        with patch.object(matrix.sys, "platform", "darwin"), \
                patch.object(matrix.time, "monotonic", side_effect=lambda: clock["now"]), \
                patch.object(matrix, "_BROWSER_PROCESS_INVENTORY_TIMEOUT_SECONDS", 10), \
                patch.object(matrix, "_browser_process_info", side_effect=current.get), \
                patch.object(matrix, "_signal_browser_process", side_effect=send), \
                patch.object(matrix, "_darwin_child_pids", side_effect=children):
            with self.assertRaises(matrix.BrowserCleanupUnverified) as caught:
                matrix._cleanup_browser_process(process, "10")
        self.assertEqual(("inventory",), caught.exception.categories)
        process.wait.assert_called_once_with(timeout=2)


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
            with self.assertRaisesRegex(matrix.BrowserCleanupUnverified, "cleanup could not be verified") as caught:
                matrix._cleanup_browser_process(process, "original-identity")
        self.assertEqual(("root_identity",), caught.exception.categories)
        kill.assert_not_called()

    def test_cleanup_diagnostics_are_bounded_categories_without_raw_errors(self):
        error = matrix.BrowserCleanupUnverified(categories={"inventory", "exit", "inventory"})
        self.assertEqual(("exit", "inventory"), error.categories)
        self.assertEqual("Browser process cleanup could not be verified", str(error))
        self.assertEqual(("unknown",), matrix.BrowserCleanupUnverified().categories)
        for categories in (("private path",), ("TimeoutExpired: private argv",), "inventory"):
            with self.subTest(categories=categories), self.assertRaises(ValueError):
                matrix.BrowserCleanupUnverified(categories=categories)

    def test_failed_inventory_remains_unverified_after_root_termination(self):
        process = subprocess.Popen([sys.executable, "-c", "import time; time.sleep(60)"], start_new_session=True)
        self.addCleanup(self.stop_child, process)
        info = matrix._browser_process_info(process.pid)
        target = "_darwin_child_pids" if sys.platform == "darwin" else "subprocess.run"
        with patch("run_paired_package_browser_matrix." + target, side_effect=subprocess.TimeoutExpired("inventory", 2)):
            with self.assertRaisesRegex(matrix.BrowserCleanupUnverified, "cleanup could not be verified") as caught:
                matrix._cleanup_browser_process(process, info[1])
        self.assertIn("inventory", caught.exception.categories)
        self.assertIsNotNone(process.returncode)


if __name__ == "__main__":
    unittest.main()
