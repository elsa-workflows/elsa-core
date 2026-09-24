"""Coverage and receipt-comparison tests for the legacy asset ledger."""

from __future__ import annotations

import copy
import hashlib
import json
import unittest

from validate_legacy_asset_dispositions import (
    DEFAULT_LEDGER,
    DEFAULT_RECEIPT,
    EXPECTED_RECEIPT_FIXTURE_SHA256,
    EXPECTED_SOURCE_RECEIPT_SHA256,
    validate_ledger,
)


class LegacyAssetDispositionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.ledger = json.loads(DEFAULT_LEDGER.read_text(encoding="utf-8"))
        cls.receipt = json.loads(DEFAULT_RECEIPT.read_text(encoding="utf-8"))

    def test_committed_ledger_has_complete_asset_rows_and_pins(self) -> None:
        self.assertEqual([], validate_ledger(self.ledger))
        self.assertEqual(163, len(self.ledger["assets"]))
        self.assertEqual({"extensions": 80, "studio": 83, "total": 163}, self.ledger["asset_counts"])

    def test_frozen_real_receipt_projection_matches_and_is_hash_pinned(self) -> None:
        fixture_hash = hashlib.sha256(DEFAULT_RECEIPT.read_bytes()).hexdigest()
        self.assertEqual(EXPECTED_RECEIPT_FIXTURE_SHA256, fixture_hash)
        self.assertEqual(EXPECTED_SOURCE_RECEIPT_SHA256, self.receipt["provenance"]["sourceReceiptSha256"])
        self.assertEqual(163, len(self.receipt["mapping"]))
        self.assertEqual([], validate_ledger(self.ledger, self.receipt))

    def test_receipt_mutations_fail_closed(self) -> None:
        mutations = (
            ("source", "/tmp/asset.txt"),
            ("source", "../escape.md"),
            ("destination", "/tmp/asset.source"),
            ("destination", "doc/integration-program/legacy/extensions/../escape.source"),
            ("mode", "000000"),
            ("mode", "777777"),
            ("mode", "120000"),
            ("blob", "0" * 40),
        )
        for field, value in mutations:
            with self.subTest(field=field):
                receipt = copy.deepcopy(self.receipt)
                receipt["mapping"][0][field] = value
                errors = validate_ledger(self.ledger, receipt)
                self.assertTrue(errors, errors)

    def test_completion_statuses_are_rejected_without_completion_proof(self) -> None:
        for status in ("implemented", "retired", "complete"):
            with self.subTest(status=status):
                changed = copy.deepcopy(self.ledger)
                changed["assets"][0]["status"] = status
                self.assertTrue(any("unsupported non-pending status" in error for error in validate_ledger(changed)))

    def test_ledger_paths_must_be_normalized_relative_git_paths(self) -> None:
        for field, value in (
            ("original_path", "/tmp/outside"),
            ("original_path", "../outside"),
            ("mapped_path", "/tmp/outside.source"),
            ("mapped_path", "doc/integration-program/legacy/extensions/../outside.source"),
        ):
            with self.subTest(field=field):
                changed = copy.deepcopy(self.ledger)
                changed["assets"][0][field] = value
                errors = validate_ledger(changed)
                self.assertTrue(any("normalized relative Git path" in error or "unexpected mapped path" in error for error in errors))

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
