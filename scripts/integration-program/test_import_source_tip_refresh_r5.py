"""Fail-closed checks for the accepted Extensions Dapper source tip."""

from __future__ import annotations

import copy
import json
import unittest
from unittest.mock import patch

from verify_import_source_tip_refresh_r5 import RECEIPT, git_bytes, verify


class SourceTipRefreshR5Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.receipt = json.loads(RECEIPT.read_text(encoding="utf-8"))

    def test_reviewed_dapper_tip_and_prior_receipt_verify(self) -> None:
        verify(self.receipt)

    def test_changed_current_blob_and_relocation_are_rejected(self) -> None:
        for field, value, message in (
            ("finalMapped", {"blob": "0" * 40, "mode": "100644"}, "Changed finalMapped"),
            ("mappedPath", "src/extensions/elsewhere.cs", "Wrong mapped path"),
        ):
            with self.subTest(field=field):
                receipt = copy.deepcopy(self.receipt)
                receipt["mappedChanges"][0][field] = value
                with self.assertRaisesRegex(ValueError, message):
                    verify(receipt)

    def test_changed_prior_receipt_digest_is_rejected(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["historicalReceiptSha256"] = "0" * 64
        with self.assertRaisesRegex(ValueError, "Prior reviewed source-tip receipt changed"):
            verify(receipt)

    def test_replaced_history_parent_is_rejected(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["mappedDeltaCommit"] = receipt["baseImportHead"]
        with self.assertRaisesRegex(ValueError, "Reviewed Extensions source-tip commits changed"):
            verify(receipt)

    def test_current_solution_must_select_dapper_tests(self) -> None:
        def without_current_test(*args: str, **kwargs: object) -> bytes:
            if args == ("show", "HEAD:Elsa.sln"):
                return b""
            return git_bytes(*args, **kwargs)

        with patch("verify_import_source_tip_refresh_r5.git_bytes", side_effect=without_current_test):
            with self.assertRaisesRegex(ValueError, "Current Elsa.sln does not select"):
                verify(self.receipt)


if __name__ == "__main__":
    unittest.main()
