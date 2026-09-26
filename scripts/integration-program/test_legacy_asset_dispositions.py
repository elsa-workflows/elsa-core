"""Coverage and receipt-comparison tests for the legacy asset ledger."""

from __future__ import annotations

import copy
import hashlib
import json
import subprocess
import tempfile
import unittest
from pathlib import Path

from validate_legacy_asset_dispositions import (
    DEFAULT_LEDGER,
    DEFAULT_RECEIPT,
    EXPECTED_RECEIPT_FIXTURE_SHA256,
    EXPECTED_SOURCE_RECEIPT_SHA256,
    _active_git_blob_and_mode,
    validate_ledger,
)


class LegacyAssetDispositionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.ledger = json.loads(DEFAULT_LEDGER.read_text(encoding="utf-8"))
        cls.receipt = json.loads(DEFAULT_RECEIPT.read_text(encoding="utf-8"))

    def test_committed_ledger_has_complete_asset_rows_and_pins(self) -> None:
        self.assertEqual([], validate_ledger(self.ledger))
        self.assertEqual(2, self.ledger["schema_version"])
        self.assertEqual(163, len(self.ledger["assets"]))
        self.assertEqual({"extensions": 80, "studio": 83, "total": 163}, self.ledger["asset_counts"])
        self.assertEqual(97, sum(row["status"] == "represented_in_core" for row in self.ledger["assets"]))
        self.assertEqual(3, sum(row["status"] == "retired_from_active_tree" for row in self.ledger["assets"]))
        license_rows = [row for row in self.ledger["assets"] if row["category"] == "license_notice"]
        self.assertEqual(2, len(license_rows))
        self.assertTrue(all(row["status"] == "represented_in_core" and row["completion"]["active_path"] == "LICENSE"
                            and row["completion"]["representation"] == "expanded" for row in license_rows))
        studio_tooling = [row for row in self.ledger["assets"]
                          if row["category"] == "studio_agent_specification_tooling"]
        self.assertEqual(50, len(studio_tooling))
        self.assertTrue(all(row["status"] == "represented_in_core" for row in studio_tooling))
        self.assertEqual({"identical": 42, "expanded": 8}, {
            representation: sum(row["completion"]["representation"] == representation for row in studio_tooling)
            for representation in ("identical", "expanded")
        })

    def test_frozen_real_receipt_projection_matches_and_is_hash_pinned(self) -> None:
        fixture_hash = hashlib.sha256(DEFAULT_RECEIPT.read_bytes()).hexdigest()
        self.assertEqual(EXPECTED_RECEIPT_FIXTURE_SHA256, fixture_hash)
        self.assertEqual(EXPECTED_SOURCE_RECEIPT_SHA256, self.receipt["provenance"]["sourceReceiptSha256"])
        self.assertEqual(163, len(self.receipt["mapping"]))
        self.assertEqual([], validate_ledger(self.ledger, self.receipt))

    def test_studio_readme_and_dockerignore_use_consolidated_paths(self) -> None:
        rows = {row["original_path"]: row for row in self.ledger["assets"]
                if row["original_repository"] == "studio"}
        self.assertEqual("doc/studio/README.md", rows["README.md"]["completion"]["active_path"])
        self.assertEqual(".dockerignore", rows[".dockerignore"]["completion"]["active_path"])
        self.assertEqual("represented_in_core", rows["README.md"]["status"])
        self.assertEqual("represented_in_core", rows[".dockerignore"]["status"])

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
        for status in ("represented_in_core", "retired_from_active_tree"):
            with self.subTest(status=status):
                changed = copy.deepcopy(self.ledger)
                changed["assets"][0]["status"] = status
                self.assertTrue(any("lacks structured evidence" in error for error in validate_ledger(changed)))

    def test_completion_evidence_rejects_missing_review_or_changed_active_file(self) -> None:
        represented = next(index for index, row in enumerate(self.ledger["assets"])
                           if row["original_path"] == ".interface-design/system.md")
        for field, value, message in (
            ("decision_path", "doc/missing.md", "invalid decision path"),
            ("decision_path", "doc/integration-program/consolidation/missing.md", "decision file is missing"),
            ("pr_url", "https://example.com/pull/8427", "invalid PR evidence"),
            ("merge_commit", "not-a-commit", "invalid merge commit"),
            ("active_path", "README.md", "active blob or mode changed"),
            ("active_blob", "0" * 40, "active blob or mode changed"),
            ("representation", "unknown", "representation is invalid"),
        ):
            with self.subTest(field=field):
                changed = copy.deepcopy(self.ledger)
                changed["assets"][represented]["completion"][field] = value
                errors = validate_ledger(changed)
                self.assertTrue(any(message in error for error in errors), errors)
        changed = copy.deepcopy(self.ledger)
        changed["assets"][represented]["completion"]["active_blob"] = "0" * 40
        changed["assets"][represented]["completion"]["representation"] = "identical"
        self.assertTrue(any("not identical" in error for error in validate_ledger(changed)))

    def test_active_git_blob_accepts_clean_crlf_checkout_but_rejects_changes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            subprocess.run(["git", "init", "-q"], cwd=root, check=True)
            subprocess.run(["git", "config", "core.autocrlf", "true"], cwd=root, check=True)
            active = root / "asset.md"
            active.write_bytes(b"recorded\n")
            subprocess.run(["git", "add", "asset.md"], cwd=root, check=True)
            blob = subprocess.check_output(["git", "rev-parse", ":asset.md"], cwd=root, text=True).strip()

            active.write_bytes(b"recorded\r\n")
            self.assertEqual((blob, "100644"), _active_git_blob_and_mode(root, "asset.md"))

            active.write_bytes(b"changed\r\n")
            self.assertIsNone(_active_git_blob_and_mode(root, "asset.md"))

    def test_pending_assets_cannot_claim_completion_evidence(self) -> None:
        changed = copy.deepcopy(self.ledger)
        changed["assets"][0]["completion"] = {"pr_url": "https://github.com/elsa-workflows/elsa-core/pull/8428"}
        self.assertTrue(any("pending asset has completion evidence" in error for error in validate_ledger(changed)))

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
