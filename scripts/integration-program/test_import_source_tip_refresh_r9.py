"""Fail-closed checks for the accepted Studio host-branding source tip."""

from __future__ import annotations

import copy
import json
import unittest
from unittest.mock import patch

from verify_import_source_tip_refresh_r9 import RECEIPT, git, git_bytes, verify


class SourceTipRefreshR9Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.receipt = json.loads(RECEIPT.read_text(encoding="utf-8"))

    def test_reviewed_studio_tip_verifies(self) -> None:
        verify(self.receipt)

    def test_changed_blob_relocation_or_missing_row_is_rejected(self) -> None:
        kept = next(index for index, row in enumerate(self.receipt["mappedChanges"]) if row["status"] == "M")
        for mutate, message in (
            (lambda rows: rows[kept].update(finalMapped={"blob": "0" * 40, "mode": "100644"}), "Changed finalMapped"),
            (lambda rows: rows[kept].update(mappedPath="src/studio/elsewhere.cs"), "Wrong mapped path"),
            (lambda rows: rows.pop(), "does not match the upstream delta"),
        ):
            with self.subTest(message=message):
                receipt = copy.deepcopy(self.receipt)
                mutate(receipt["mappedChanges"])
                with self.assertRaisesRegex(ValueError, message):
                    verify(receipt)

    def test_deleted_upstream_file_must_stay_deleted(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        deleted = next(row for row in receipt["mappedChanges"] if row["status"] == "D")
        deleted["finalMapped"] = {"blob": "0" * 40, "mode": "100644"}
        with self.assertRaisesRegex(ValueError, "Changed finalMapped"):
            verify(receipt)

    def test_replaced_history_parent_is_rejected(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["mappedDeltaCommit"] = receipt["baseImportHead"]
        with self.assertRaisesRegex(ValueError, "Reviewed Studio source-tip commits changed"):
            verify(receipt)

    def test_publication_claim_is_rejected(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["publicationPerformed"] = True
        with self.assertRaisesRegex(ValueError, "must not claim a package publication"):
            verify(receipt)

    def test_mapped_bytes_must_match_upstream(self) -> None:
        def edited_mapping(*args: str, **kwargs: object) -> bytes:
            content = git_bytes(*args, **kwargs)
            return content + b"\n" if args[1].startswith("HEAD:src/studio/") else content

        with patch("verify_import_source_tip_refresh_r9.git_bytes", side_effect=edited_mapping):
            with self.assertRaisesRegex(ValueError, "differs from upstream"):
                verify(self.receipt)

    def test_current_publisher_workflow_drift_is_rejected(self) -> None:
        import verify_import_source_tip_refresh_r7 as r7

        real = r7.blob_and_mode

        def drifted(commit: str, path: str, root: object = r7.ROOT) -> dict[str, str] | None:
            if commit == "HEAD" and path == ".github/workflows/packages.yml":
                return {"blob": "0" * 40, "mode": "100644"}
            return real(commit, path, root)

        with patch("verify_import_source_tip_refresh_r7.blob_and_mode", side_effect=drifted):
            with self.assertRaisesRegex(ValueError, "changed active publisher workflow at HEAD"):
                verify(self.receipt)

    def test_extra_upstream_or_mapped_diff_path_is_rejected(self) -> None:
        base, delta = self.receipt["baseImportHead"], self.receipt["mappedDeltaCommit"]
        old = self.receipt["mappedChanges"][0]["source"]
        for scope, extra, message in (
            ("upstream", "M\tsrc/elsewhere/Unreviewed.cs", "reaches beyond the reviewed host-branding files"),
            ("mapped", "src/studio/framework/Elsa.Studio.Core/Unreviewed.cs", "changed unreviewed paths"),
        ):
            def with_extra(*args: str, **kwargs: object) -> str:
                output = git(*args, **kwargs)
                if scope == "upstream" and args[:2] == ("diff", "--name-status"):
                    return output + "\n" + extra
                if scope == "mapped" and args == ("diff", "--name-only", base, delta):
                    return output + "\n" + extra
                return output

            with self.subTest(scope=scope), \
                    patch("verify_import_source_tip_refresh_r9.git", side_effect=with_extra):
                with self.assertRaisesRegex(ValueError, message):
                    verify(self.receipt)


if __name__ == "__main__":
    unittest.main()
