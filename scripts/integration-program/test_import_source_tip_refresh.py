"""Fail-closed checks for the history-preserving upstream source-tip refresh."""

from __future__ import annotations

import copy
import json
import unittest

from verify_import_source_tip_refresh import RECEIPT, verify


class SourceTipRefreshTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.receipt = json.loads(RECEIPT.read_text(encoding="utf-8"))

    def test_refreshed_upstream_histories_and_six_exact_mappings_verify(self) -> None:
        verify(self.receipt)

    def test_changed_source_blob_or_relocation_is_rejected(self) -> None:
        for change in ("blob", "mappedPath"):
            with self.subTest(change=change):
                receipt = copy.deepcopy(self.receipt)
                row = next(item for item in receipt["mappedChanges"] if item["repository"] == "studio"
                           and item["source"].endswith("Secrets.razor"))
                if change == "blob":
                    row["new"]["blob"] = "0" * 40
                else:
                    row["mappedPath"] = "src/studio/elsewhere/Secrets.razor"
                with self.assertRaisesRegex(ValueError, "Refreshed source mapping|Mapped changes differ"):
                    verify(receipt)

    def test_dropping_new_upstream_parent_is_rejected(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["newUpstreamCommits"]["studio"] = receipt["oldUpstreamCommits"]["studio"]
        with self.assertRaisesRegex(ValueError, "preserves both upstream histories"):
            verify(receipt)


if __name__ == "__main__":
    unittest.main()
