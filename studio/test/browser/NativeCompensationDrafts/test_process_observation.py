#!/usr/bin/env python3
"""Tests for reconciling CDP process observations with the local process table."""
import importlib.util
import json
import os
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


SCRIPT = Path(__file__).with_name("run-proof-skeleton.py")
SPEC = importlib.util.spec_from_file_location("run_proof_skeleton", SCRIPT)
PROOF = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PROOF)

RUNNER_PID = 101
OWNER_PID = 102
BROWSER_PID = 103
RUNNER_BIRTH = "1:000000"
CAPTURED_AT_MS = 2_000


def process(pid, ppid, pgid, sid, birth=RUNNER_BIRTH, birth_unix_micros=1_000_000):
    return {
        "pid": pid,
        "ppid": ppid,
        "pgid": pgid,
        "sid": sid,
        "birth": birth,
        "birthUnixMicros": birth_unix_micros,
    }


class BrowserProcessObservationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.capture_path = self.root / "capture.json"
        self.ownership_path = self.root / "ownership.json"
        self.capture_path.write_text(json.dumps({
            "schemaVersion": 1,
            "source": "Chromium-CDP-SystemInfo.getProcessInfo",
            "snapshots": [{
                "capturedAtUnixMs": CAPTURED_AT_MS,
                "ownerPid": OWNER_PID,
                "ownerParentPid": RUNNER_PID,
                "processes": [{"id": BROWSER_PID, "type": "browser"}],
            }],
        }), encoding="utf-8")
        os.chmod(self.capture_path, 0o600)
        self.table = {
            RUNNER_PID: process(RUNNER_PID, 1, RUNNER_PID, RUNNER_PID),
            OWNER_PID: process(OWNER_PID, RUNNER_PID, RUNNER_PID, RUNNER_PID),
        }

    def refresh(self, fresh_read, direct_read=None):
        with patch.object(PROOF, "process_table", return_value=dict(self.table)), \
             patch.object(PROOF, "read_owned_process_info", side_effect=fresh_read) as read, \
             patch.object(PROOF, "process_info", return_value=direct_read):
            ownership = PROOF.refresh_browser_ownership(
                self.capture_path, self.ownership_path, RUNNER_PID, RUNNER_BIRTH
            )
        return ownership, read

    def records_for_browser_pid(self, ownership):
        return [item for item in ownership["processes"] if item["pid"] == BROWSER_PID]

    def test_missing_table_pid_uses_fresh_owned_identity(self):
        fresh = process(BROWSER_PID, OWNER_PID, RUNNER_PID, RUNNER_PID)
        ownership, read = self.refresh(lambda pid: fresh if pid == BROWSER_PID else None, direct_read=fresh)

        read.assert_called_once_with(BROWSER_PID)
        browser_records = self.records_for_browser_pid(ownership)
        self.assertEqual(len(browser_records), 1)
        self.assertEqual(browser_records[0]["source"], "cdp")

    def test_fresh_identity_with_reused_birth_is_not_added(self):
        reused = process(BROWSER_PID, RUNNER_PID, 999, 999, birth="3:000000", birth_unix_micros=3_000_000)
        ownership, read = self.refresh(lambda pid: reused if pid == BROWSER_PID else None, direct_read=reused)

        read.assert_called_once_with(BROWSER_PID)
        self.assertEqual(self.records_for_browser_pid(ownership), [])

    def test_fresh_unrelated_group_is_rejected(self):
        unrelated = process(BROWSER_PID, 999, 999, 999)
        with self.assertRaisesRegex(PROOF.StopProof, "browser-process-ancestry-unverified"):
            self.refresh(lambda pid: unrelated if pid == BROWSER_PID else None, direct_read=unrelated)

    def test_persistent_unreadability_remains_fail_closed(self):
        def unreadable(_pid):
            raise PROOF.StopProof("browser-process-identity-unreadable")

        with patch.object(PROOF, "process_table", return_value=dict(self.table)), \
             patch.object(PROOF, "read_owned_process_info", side_effect=unreadable) as read, \
             patch.object(PROOF, "process_info", return_value=None) as direct_read:
            with self.assertRaisesRegex(PROOF.StopProof, "browser-process-identity-unreadable"):
                PROOF.refresh_browser_ownership(
                    self.capture_path, self.ownership_path, RUNNER_PID, RUNNER_BIRTH
                )

        read.assert_called_once_with(BROWSER_PID)
        direct_read.assert_not_called()


if __name__ == "__main__":
    unittest.main()
