"""Fail-closed checks for the #8293 Dapper atomic-update delta and the older receipts it supersedes."""

from __future__ import annotations

import copy
import json
import unittest
from unittest.mock import patch

import verify_import_source_tip_refresh as r1
import verify_import_source_tip_refresh_r5 as r5
from verify_import_source_tip_refresh_r6 import (
    DELTA_COMMIT,
    QUERY,
    RECEIPT,
    REVIEWED_TESTS,
    STORE,
    UPSTREAM_TESTS,
    blob_and_mode,
    git_bytes,
    verify,
)

OTHER_R1_PATH = "test/extensions/modules/persistence/Elsa.MongoDb.UnitTests/MongoWorkflowDefinitionStoreCompareAndSwapTests.cs"
OTHER_R2_PATH = "src/extensions/persistence/Elsa.Persistence.Dapper/Services/Store.cs"
CHANGED = {"blob": "0" * 40, "mode": "100644"}


def changed_at_head(module: str, path: str):
    """Patch a verifier module so HEAD reports different bytes for one path."""

    def blob(commit: str, candidate: str, *root: object):
        return CHANGED if (commit, candidate) == ("HEAD", path) else blob_and_mode(commit, candidate, *root)

    return patch(f"{module}.blob_and_mode", side_effect=blob)


class SourceTipRefreshR6Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.receipt = json.loads(RECEIPT.read_text(encoding="utf-8"))

    def rejects(self, message: str, receipt: dict | None = None) -> None:
        with self.assertRaisesRegex(ValueError, message):
            verify(receipt or self.receipt)

    def test_reviewed_delta_and_prior_receipts_verify(self) -> None:
        verify(self.receipt)

    def test_changed_final_blob_and_relocation_are_rejected(self) -> None:
        for field, value, message in (
            ("finalMapped", CHANGED, "Changed finalMapped"),
            ("path", "src/extensions/elsewhere.cs", "must name the five reviewed"),
            ("transform", "reviewed-patch", "must name the five reviewed"),
        ):
            with self.subTest(field=field):
                receipt = copy.deepcopy(self.receipt)
                receipt["mappedChanges"][2][field] = value
                self.rejects(message, receipt)

    def test_changed_prior_receipt_digest_or_scope_is_rejected(self) -> None:
        for index, field, value, message in (
            (0, "sha256", "0" * 64, "Prior reviewed source-tip receipt changed"),
            (0, "supersededMappedPaths", [], "Superseded prior receipts changed"),
            (2, "supersededMappedPaths", [OTHER_R2_PATH], "Superseded prior receipts changed"),
        ):
            with self.subTest(index=index, field=field):
                receipt = copy.deepcopy(self.receipt)
                receipt["priorReceipts"][index][field] = value
                self.rejects(message, receipt)

    def test_replaced_delta_or_patch_is_rejected(self) -> None:
        for field, message in (("deltaCommit", "delta commits changed"), ("reviewedPatch", "patch artifact changed")):
            with self.subTest(field=field):
                receipt = copy.deepcopy(self.receipt)
                if field == "deltaCommit":
                    receipt[field] = receipt["baseImportHead"]
                else:
                    receipt[field]["sha256"] = "0" * 64
                self.rejects(message, receipt)

    def test_publication_or_publisher_change_is_rejected(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["publicationPerformed"] = True
        self.rejects("must not claim a package publication", receipt)
        with changed_at_head("verify_import_source_tip_refresh_r6", ".github/workflows/packages.yml"):
            self.rejects("changed active publisher workflow at HEAD")

    def test_older_verifier_deferral_must_match_this_receipt(self) -> None:
        with patch.object(r1, "PATHS_SUPERSEDED_BY_R6", r1.PATHS_SUPERSEDED_BY_R6 | {OTHER_R1_PATH}):
            self.rejects("Older verifier deferral differs")
        with patch.object(r5, "R2_PATHS_SUPERSEDED_BY_R6", frozenset()):
            self.rejects("Older verifier deferral differs")

    def test_superseded_files_must_match_final_mapped_at_head(self) -> None:
        for path in (UPSTREAM_TESTS, QUERY):
            with self.subTest(path=path), changed_at_head("verify_import_source_tip_refresh_r6", path):
                self.rejects("Changed finalMapped at HEAD")

    def test_unreviewed_transform_is_rejected(self) -> None:
        for path, old, message in (
            (STORE, b"WorkflowDefinitionUpdateResult.Conflict()", "Unreviewed #8293 transform"),
            (UPSTREAM_TESTS, b"connectionProvider, _tenantAccessor", "Unreviewed #8293 transform"),
            (REVIEWED_TESTS, b"WorkflowDefinitionUpdateOutcome.Conflict", "Unreviewed #8293 transform"),
            (REVIEWED_TESTS, b"// Matches the memory", "Added superseded-version case changed"),
        ):
            def tampered(*args: str, **kwargs: object) -> bytes:
                content = git_bytes(*args, **kwargs)
                if args == ("show", f"{DELTA_COMMIT}:{path}"):
                    return content.replace(old, old + b" ", 1)
                return content

            with self.subTest(path=path, old=old), patch("verify_import_source_tip_refresh_r6.git_bytes", side_effect=tampered):
                self.rejects(message)

    def test_older_verifiers_defer_only_the_superseded_paths(self) -> None:
        r1_receipt = json.loads(r1.RECEIPT.read_text(encoding="utf-8"))
        r5_receipt = json.loads(r5.RECEIPT.read_text(encoding="utf-8"))
        with changed_at_head("verify_import_source_tip_refresh", UPSTREAM_TESTS):
            r1.verify(r1_receipt)
        with changed_at_head("verify_import_source_tip_refresh", OTHER_R1_PATH):
            with self.assertRaisesRegex(ValueError, "changed after history join"):
                r1.verify(r1_receipt)
        with changed_at_head("verify_import_source_tip_refresh_r5", QUERY):
            r5.verify(r5_receipt)
        with changed_at_head("verify_import_source_tip_refresh_r5", OTHER_R2_PATH):
            with self.assertRaisesRegex(ValueError, "Earlier reviewed mapping changed without another receipt"):
                r5.verify(r5_receipt)

    def test_current_solution_must_select_reviewed_tests(self) -> None:
        def without_tests(*args: str, **kwargs: object) -> bytes:
            return b"" if args == ("show", "HEAD:Elsa.sln") else git_bytes(*args, **kwargs)

        with patch("verify_import_source_tip_refresh_r6.git_bytes", side_effect=without_tests):
            self.rejects("Current Elsa.sln does not select the reviewed Dapper tests")


if __name__ == "__main__":
    unittest.main()
