"""Fail-closed checks for the Extensions persistence source-tip refresh."""

from __future__ import annotations

import copy
import json
import unittest

from verify_import_source_tip_refresh_r2 import RECEIPT, verify


class SourceTipRefreshR2Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.receipt = json.loads(RECEIPT.read_text(encoding="utf-8"))

    def test_latest_extensions_history_and_fourteen_mappings_verify(self) -> None:
        verify(self.receipt)

    def test_changed_source_blob_and_relocation_are_rejected(self) -> None:
        for field, replacement, message in (
            ("newSource", {"blob": "0" * 40, "mode": "100644"}, "New upstream source changed"),
            ("mappedPath", "src/extensions/elsewhere.cs", "Mapped changes differ"),
        ):
            with self.subTest(field=field):
                receipt = copy.deepcopy(self.receipt)
                receipt["mappedChanges"][0][field] = replacement
                with self.assertRaisesRegex(ValueError, message):
                    verify(receipt)

    def test_unreviewed_project_transform_is_rejected(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["reviewedProjectTransform"]["reason"] = "unreviewed"
        with self.assertRaisesRegex(ValueError, "Reviewed Dapper project transform changed"):
            verify(receipt)

    def test_unreviewed_test_project_transform_is_rejected(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["reviewedTestProjectTransform"]["mappedPath"] = "test/extensions/elsewhere.csproj"
        with self.assertRaisesRegex(ValueError, "Reviewed Dapper test project transform changed"):
            verify(receipt)


if __name__ == "__main__":
    unittest.main()
