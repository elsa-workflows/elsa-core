"""Coverage and receipt-comparison tests for the legacy asset ledger."""

from __future__ import annotations

import copy
import json
import unittest

from validate_legacy_asset_dispositions import DEFAULT_LEDGER, validate_ledger


class LegacyAssetDispositionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.ledger = json.loads(DEFAULT_LEDGER.read_text(encoding="utf-8"))

    def test_committed_ledger_has_complete_asset_rows_and_pins(self) -> None:
        self.assertEqual([], validate_ledger(self.ledger))
        self.assertEqual(163, len(self.ledger["assets"]))
        self.assertEqual({"extensions": 80, "studio": 83, "total": 163}, self.ledger["asset_counts"])

    def test_exact_receipt_mapping_is_accepted_and_metadata_drift_is_rejected(self) -> None:
        receipt = {
            "sourceCommits": self.ledger["recorded_source_commits"],
            "mapping": [
                {
                    "repository": row["original_repository"],
                    "source": row["original_path"],
                    "destination": row["mapped_path"],
                    "blob": row["blob"],
                    "mode": row["mode"],
                }
                for row in self.ledger["assets"]
            ],
        }
        self.assertEqual([], validate_ledger(self.ledger, receipt))

        changed_receipt = copy.deepcopy(receipt)
        changed_receipt["mapping"][0]["blob"] = "0" * 40
        self.assertTrue(any("metadata mismatch" in error for error in validate_ledger(self.ledger, changed_receipt)))

    def test_missing_or_duplicate_assets_fail_closed(self) -> None:
        missing_asset_ledger = copy.deepcopy(self.ledger)
        missing_asset_ledger["assets"].pop()
        self.assertTrue(any("expected 163" in error for error in validate_ledger(missing_asset_ledger)))

        duplicate_asset_ledger = copy.deepcopy(self.ledger)
        duplicate_asset_ledger["assets"].append(copy.deepcopy(duplicate_asset_ledger["assets"][0]))
        errors = validate_ledger(duplicate_asset_ledger)
        self.assertTrue(any("duplicate original asset" in error for error in errors))
        self.assertTrue(any("duplicate mapped path" in error for error in errors))

    def test_each_row_requires_an_owner_disposition_gate_and_status(self) -> None:
        incomplete_ledger = copy.deepcopy(self.ledger)
        incomplete_ledger["assets"][0]["evidence_or_gate"] = ""
        errors = validate_ledger(incomplete_ledger)
        self.assertTrue(any("evidence_or_gate" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
