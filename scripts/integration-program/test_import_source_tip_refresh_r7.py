"""Fail-closed checks for the reviewed #8497 publisher-gate merge and the receipts it supersedes."""

from __future__ import annotations

import copy
import json
import subprocess
import sys
import unittest
from unittest.mock import patch

from verify_import_source_tip_refresh_r7 import (
    MERGE_COMMIT,
    RECEIPT,
    blob_and_mode,
    verify,
)

CHANGED = {"blob": "0" * 40, "mode": "100644"}


def changed_at(path: str, commit: str):
    """Patch the r7 module so a specific commit reports different bytes for one path."""

    def blob(candidate_commit: str, candidate_path: str, *root: object):
        return (
            CHANGED
            if (candidate_commit, candidate_path) == (commit, path)
            else blob_and_mode(candidate_commit, candidate_path, *root)
        )

    return patch("verify_import_source_tip_refresh_r7.blob_and_mode", side_effect=blob)


class SourceTipRefreshR7Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.receipt = json.loads(RECEIPT.read_text(encoding="utf-8"))

    def rejects(self, message: str, receipt: dict | None = None) -> None:
        with self.assertRaisesRegex(ValueError, message):
            verify(receipt if receipt is not None else self.receipt)

    def test_reviewed_publisher_gate_merge_and_superseded_receipts_verify(self) -> None:
        verify(self.receipt)

    def test_replaced_merge_commits_are_rejected(self) -> None:
        for field in ("baseImportHead", "mergedMainCommit", "reviewedGateCommit", "mergeCommit"):
            with self.subTest(field=field):
                receipt = copy.deepcopy(self.receipt)
                receipt[field] = "0" * 40
                self.rejects("Reviewed publisher-gate merge commits changed", receipt)

    def test_publication_or_pull_request_change_is_rejected(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["publicationPerformed"] = True
        self.rejects("must not claim a package publication", receipt)
        receipt = copy.deepcopy(self.receipt)
        receipt["sourcePullRequest"] = "https://github.com/elsa-workflows/elsa-core/pull/1"
        self.rejects("Reviewed publisher-gate pull request changed", receipt)

    def test_changed_superseded_receipt_digest_or_scope_is_rejected(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["supersededPublisherReceipts"][0]["sha256"] = "0" * 64
        self.rejects("Superseded publisher receipt changed", receipt)
        receipt = copy.deepcopy(self.receipt)
        del receipt["supersededPublisherReceipts"][0]
        self.rejects("Superseded publisher receipts changed", receipt)

    def test_before_bytes_must_match_the_reviewed_import_head(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["publisherWorkflows"]["before"][".github/workflows/packages.yml"] = CHANGED
        self.rejects("Recorded before-bytes differ from the reviewed import head", receipt)

    def test_after_bytes_must_hold_through_every_reviewed_commit_and_head(self) -> None:
        for commit in (
            "79d6c41e09069927abb7825f16b5ba0a168cea38",
            "d0ea5b039c2525774b14592d3322078a645b993c",
            MERGE_COMMIT,
            "HEAD",
        ):
            with self.subTest(commit=commit), changed_at(".github/workflows/packages.yml", commit):
                self.rejects(f"changed active publisher workflow at {commit}")

    def test_recorded_bytes_must_reflect_an_actual_packages_yml_change(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["publisherWorkflows"]["after"][".github/workflows/packages.yml"] = receipt["publisherWorkflows"][
            "before"
        ][".github/workflows/packages.yml"]
        self.rejects("did not change the root packages.yml", receipt)

    def test_recorded_bytes_must_not_claim_an_update_wiki_change(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["publisherWorkflows"]["after"][".github/workflows/update-wiki.yml"] = CHANGED
        self.rejects("unexpectedly changed update-wiki.yml", receipt)

    def test_gate_validator_path_change_is_rejected(self) -> None:
        receipt = copy.deepcopy(self.receipt)
        receipt["gateValidator"] = "scripts/some-other-validator.py"
        self.rejects("Recorded packages gate validator path changed", receipt)

    def test_failing_gate_validator_is_rejected(self) -> None:
        real_run = subprocess.run

        def fake_run(command: list[str], **kwargs: object) -> subprocess.CompletedProcess:
            if command[:1] == [sys.executable]:
                return subprocess.CompletedProcess(
                    command, 1, stdout="", stderr="packages.yml: a publish job is reachable from pull_request"
                )
            return real_run(command, **kwargs)

        with patch("verify_import_source_tip_refresh_r7.subprocess.run", side_effect=fake_run):
            self.rejects("Packages gate validator failed")


if __name__ == "__main__":
    unittest.main()
