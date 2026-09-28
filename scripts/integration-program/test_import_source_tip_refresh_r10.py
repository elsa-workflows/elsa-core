"""Fail-closed checks for the accepted Extensions Dapper source tip."""

from __future__ import annotations

import copy
import json
import unittest
from unittest.mock import patch

from verify_import_source_tip_refresh_r10 import RECEIPT, git, git_bytes, verify


class SourceTipRefreshR10Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.receipt = json.loads(RECEIPT.read_text(encoding="utf-8"))

    def test_reviewed_dapper_tip_verifies(self) -> None:
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

        with patch("verify_import_source_tip_refresh_r10.git_bytes", side_effect=edited_mapping):
            with self.assertRaisesRegex(ValueError, "differs from upstream"):
                verify(self.receipt)

    def test_current_solution_must_select_dapper_tests(self) -> None:
        def without_current_test(*args: str, **kwargs: object) -> bytes:
            return b"" if args == ("show", "HEAD:Elsa.sln") else git_bytes(*args, **kwargs)

        with patch("verify_import_source_tip_refresh_r10.git_bytes", side_effect=without_current_test):
            with self.assertRaisesRegex(ValueError, "Current Elsa.sln does not select"):
                verify(self.receipt)

    def test_solution_must_build_the_dapper_tests(self) -> None:
        def without_build_rows(*args: str, **kwargs: object) -> bytes:
            content = git_bytes(*args, **kwargs)
            if args != ("show", "HEAD:Elsa.sln"):
                return content
            return b"".join(line for line in content.splitlines(keepends=True)
                            if not (b"90C6FC77-1B4E-5457-9019-A860AA0BA5CB" in line and b".Build.0" in line))

        with patch("verify_import_source_tip_refresh_r10.git_bytes", side_effect=without_build_rows):
            with self.assertRaisesRegex(ValueError, "builds them in no configuration"):
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
        for scope, extra, message in (
            ("upstream", "M\tsrc/elsewhere/Unreviewed.cs", "reaches beyond the reviewed Dapper migration files"),
            ("mapped", "src/extensions/persistence/Elsa.Persistence.Dapper.Migrations/Unreviewed.cs", "changed unreviewed paths"),
        ):
            def with_extra(*args: str, **kwargs: object) -> str:
                output = git(*args, **kwargs)
                if scope == "upstream" and args[:2] == ("diff", "--name-status"):
                    return output + "\n" + extra
                if scope == "mapped" and args == ("diff", "--name-only", base, delta):
                    return output + "\n" + extra
                return output

            with self.subTest(scope=scope), \
                    patch("verify_import_source_tip_refresh_r10.git", side_effect=with_extra):
                with self.assertRaisesRegex(ValueError, message):
                    verify(self.receipt)

    def test_reviewed_project_transform_cannot_be_dropped(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        next(row for row in receipt["mappedChanges"] if row["source"].endswith(".csproj"))["transform"] = "identical"
        with self.assertRaisesRegex(ValueError, "Reviewed transform changed"):
            verify(receipt)

    def test_project_file_must_carry_exactly_the_upstream_line_changes(self) -> None:
        project = next(row["mappedPath"] for row in self.receipt["mappedChanges"] if row["source"].endswith(".csproj"))

        def extra_line(*args: str, **kwargs: object) -> str:
            output = git(*args, **kwargs)
            if args[:2] == ("diff", "--unified=0") and args[-1] == project:
                return output + "\n+        <PackageReference Include=\"Unreviewed\" />"
            return output

        with patch("verify_import_source_tip_refresh_r10.git", side_effect=extra_line):
            with self.assertRaisesRegex(ValueError, "does not carry exactly the upstream line changes"):
                verify(self.receipt)


if __name__ == "__main__":
    unittest.main()
