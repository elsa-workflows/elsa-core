"""Checks for the refreshed, history-import legacy asset audit."""

from __future__ import annotations

import copy
import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from audit_current_tip_legacy_assets import EVIDENCE, compare_assets, load_pinned_receipt, verify_mapped_files
from validate_legacy_asset_dispositions import DEFAULT_LEDGER


class CurrentTipLegacyAssetsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.ledger = json.loads(DEFAULT_LEDGER.read_text(encoding="utf-8"))
        cls.receipt = load_pinned_receipt()
        cls.pins = json.loads((EVIDENCE / "reviewed-overlays-six.json").read_text(encoding="utf-8"))["sourcePins"]

    def test_exact_e96_receipt_keeps_all_assets_and_identifies_four_changed_blobs(self) -> None:
        errors, changed = compare_assets(self.ledger, self.receipt, self.pins)
        self.assertEqual([], errors)
        self.assertEqual(163, len([row for row in self.receipt["mapping"] if row["destination"].endswith(".source")]))
        self.assertEqual(4, len(changed))
        self.assertEqual({
            ".github/workflows/packages.yml",
            "Directory.Build.props",
            "Directory.Packages.props",
            "src/modules/secrets/Elsa.Studio.Secrets/Elsa.Studio.Secrets.csproj",
        }, {row["path"] for row in changed})

    def test_relocation_and_source_drift_fail_closed(self) -> None:
        mutations = (
            lambda receipt: receipt["mapping"].append(copy.deepcopy(next(row for row in receipt["mapping"] if row["destination"].endswith(".source")))),
            lambda receipt: receipt["mapping"].remove(next(row for row in receipt["mapping"] if row["destination"].endswith(".source"))),
            lambda receipt: next(row for row in receipt["mapping"] if row["destination"].endswith(".source")).update(mode="100755"),
        )
        for mutate in mutations:
            with self.subTest(mutate=mutate):
                receipt = copy.deepcopy(self.receipt)
                mutate(receipt)
                errors, _ = compare_assets(self.ledger, receipt, self.pins)
                self.assertTrue(errors)
        receipt = copy.deepcopy(self.receipt)
        receipt["sourceCommits"]["extensions"] = "0" * 40
        errors, _ = compare_assets(self.ledger, receipt, self.pins)
        self.assertTrue(any("source commits" in error for error in errors))

    def test_materialized_asset_bytes_and_mode_must_match_receipt(self) -> None:
        content = b"source asset\n"
        destination = "doc/integration-program/legacy/extensions/sample.source"
        blob = hashlib.sha1(f"blob {len(content)}\0".encode() + content).hexdigest()
        receipt = {"mapping": [{"destination": destination, "blob": blob, "mode": "100644"}]}
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / destination
            path.parent.mkdir(parents=True)
            path.write_bytes(content)
            self.assertEqual([], verify_mapped_files(root, receipt))
            path.chmod(0o755)
            self.assertTrue(verify_mapped_files(root, receipt))
            receipt["mapping"][0]["mode"] = "100755"
            self.assertEqual([], verify_mapped_files(root, receipt))
            path.write_bytes(b"changed\n")
            self.assertTrue(verify_mapped_files(root, receipt))
            path.unlink()
            self.assertTrue(verify_mapped_files(root, receipt))


if __name__ == "__main__":
    unittest.main()
