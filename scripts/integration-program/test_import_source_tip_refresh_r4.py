"""Fail-closed checks for the reviewed Studio timer-test source refresh."""

from __future__ import annotations

import copy
import json
import unittest
from unittest.mock import patch

from verify_import_source_tip_refresh_r4 import PUBLISHER_BLOBS, RECEIPT, verify


class SourceTipRefreshR4Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.receipt = json.loads(RECEIPT.read_text(encoding="utf-8"))

    def test_reviewed_source_and_import_ancestry_verify(self) -> None:
        verify(self.receipt)

    def test_changed_source_blob_and_relocation_are_rejected(self) -> None:
        for field, value, message in (
            ("newSource", {"blob": "0" * 40, "mode": "100644"}, "Studio timer newSource blob changed"),
            ("mappedPath", "src/studio/elsewhere.cs", "Studio timer source relocation changed"),
        ):
            with self.subTest(field=field):
                receipt = copy.deepcopy(self.receipt)
                receipt["mappedChanges"][0][field] = value
                with self.assertRaisesRegex(ValueError, message):
                    verify(receipt)

    def test_unreviewed_history_parent_is_rejected(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["historyJoinCommit"] = receipt["mappedDeltaCommit"]
        with self.assertRaisesRegex(ValueError, "Studio timer history join lost"):
            verify(receipt)

    def test_publisher_blob_change_is_rejected(self) -> None:
        with patch.dict(PUBLISHER_BLOBS, {".github/workflows/packages.yml": "0" * 40}):
            with self.assertRaisesRegex(ValueError, "Active Core publisher changed"):
                verify(self.receipt)


if __name__ == "__main__":
    unittest.main()
