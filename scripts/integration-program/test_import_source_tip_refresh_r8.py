"""Fail-closed checks for the accepted Extensions MongoDB source tip."""

from __future__ import annotations

import copy
import json
import unittest
from unittest.mock import patch

from verify_import_source_tip_refresh_r8 import RECEIPT, git_bytes, verify


class SourceTipRefreshR8Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.receipt = json.loads(RECEIPT.read_text(encoding="utf-8"))

    def test_reviewed_mongodb_tip_verifies(self) -> None:
        verify(self.receipt)

    def test_changed_blob_relocation_or_missing_row_is_rejected(self) -> None:
        for mutate, message in (
            (lambda rows: rows[0].update(finalMapped={"blob": "0" * 40, "mode": "100644"}), "Changed finalMapped"),
            (lambda rows: rows[0].update(mappedPath="src/extensions/elsewhere.cs"), "Wrong mapped path"),
            (lambda rows: rows.pop(), "does not match the upstream delta"),
        ):
            with self.subTest(message=message):
                receipt = copy.deepcopy(self.receipt)
                mutate(receipt["mappedChanges"])
                with self.assertRaisesRegex(ValueError, message):
                    verify(receipt)

    def test_replaced_history_parent_is_rejected(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["mappedDeltaCommit"] = receipt["baseImportHead"]
        with self.assertRaisesRegex(ValueError, "Reviewed Extensions source-tip commits changed"):
            verify(receipt)

    def test_publication_claim_is_rejected(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["publicationPerformed"] = True
        with self.assertRaisesRegex(ValueError, "must not claim a package publication"):
            verify(receipt)

    def test_mapped_bytes_must_match_upstream(self) -> None:
        def edited_mapping(*args: str, **kwargs: object) -> bytes:
            content = git_bytes(*args, **kwargs)
            return content + b"\n" if args[1].startswith("HEAD:src/extensions/") else content

        with patch("verify_import_source_tip_refresh_r8.git_bytes", side_effect=edited_mapping):
            with self.assertRaisesRegex(ValueError, "differs from upstream"):
                verify(self.receipt)

    def test_current_solution_must_select_mongodb_tests(self) -> None:
        def without_current_test(*args: str, **kwargs: object) -> bytes:
            return b"" if args == ("show", "HEAD:Elsa.sln") else git_bytes(*args, **kwargs)

        with patch("verify_import_source_tip_refresh_r8.git_bytes", side_effect=without_current_test):
            with self.assertRaisesRegex(ValueError, "Current Elsa.sln does not select"):
                verify(self.receipt)


if __name__ == "__main__":
    unittest.main()
