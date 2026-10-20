"""Provenance regression cases use real Git graphs, including invalid imports."""

import contextlib
import io
from pathlib import Path
import subprocess
import tempfile
import unittest

from verify_studio_react_source_import import git, verify


class SourceImportTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.run_git("init", "-q")
        self.run_git("config", "user.name", "Fixture")
        self.run_git("config", "user.email", "fixture@example.invalid")
        (self.root / "host").write_text("donor")
        self.run_git("add", ".")
        self.run_git("commit", "-qm", "Core")
        base = git(self.root, "rev-parse", "HEAD")
        self.run_git("checkout", "--orphan", "donor")
        self.run_git("rm", "-rf", ".")
        (self.root / "source").write_text("donor")
        self.run_git("add", ".")
        self.run_git("commit", "-qm", "Donor")
        source = git(self.root, "rev-parse", "HEAD")
        self.run_git("checkout", "--detach", base)
        self.run_git("merge", "-s", "ours", "--no-commit", "--allow-unrelated-histories", source)
        self.run_git("read-tree", "--prefix=studio/react/", "-u", source)
        self.run_git("commit", "-qm", "Import")
        self.receipt = dict(coreBase=base, sourceCommit=source, snapshotCommit=source,
                            importCommit=git(self.root, "rev-parse", "HEAD"),
                            destination="studio/react", sourceTree=git(self.root, "rev-parse", source + "^{tree}"),
                            sourceHistoryCommits=1, postSnapshotCommits=[],
                            sourceRefs=[dict(ref="refs/heads/main", commit=source)])

    def run_git(self, *args):
        subprocess.run(["git", *args], cwd=self.root, check=True, capture_output=True)

    def check_receipt(self):
        with contextlib.redirect_stdout(io.StringIO()):
            verify(self.root, self.receipt)

    def test_exact_import_remains_valid_after_adaptation(self):
        (self.root / "studio/react/source").write_text("adapted")
        self.run_git("commit", "-qam", "Adapt")
        self.check_receipt()

    def test_changed_source_tree_is_rejected(self):
        self.receipt["sourceTree"] = self.receipt["coreBase"]
        with self.assertRaisesRegex(ValueError, "tree differs"):
            self.check_receipt()

    def test_squashed_import_is_rejected(self):
        self.receipt["importCommit"] = self.receipt["coreBase"]
        with self.assertRaisesRegex(ValueError, "two parents"):
            self.check_receipt()

    def test_changed_imported_file_is_rejected(self):
        (self.root / "studio/react/source").write_text("changed during import")
        self.run_git("commit", "-qam", "Changed import", "--amend")
        self.receipt["importCommit"] = git(self.root, "rev-parse", "HEAD")
        with self.assertRaisesRegex(ValueError, "tree differs"):
            self.check_receipt()

    def test_rename_detection_cannot_hide_an_outside_deletion(self):
        self.run_git("config", "diff.renames", "true")
        self.run_git("rm", "host")
        self.run_git("commit", "--amend", "--no-edit", "-q")
        self.receipt["importCommit"] = git(self.root, "rev-parse", "HEAD")
        self.assertIn("R100", git(self.root, "diff", "--name-status", self.receipt["coreBase"], "HEAD"))
        with self.assertRaisesRegex(ValueError, "outside the destination"):
            self.check_receipt()

    def test_import_must_be_in_current_history(self):
        self.run_git("checkout", "--detach", self.receipt["coreBase"])
        with self.assertRaises(subprocess.CalledProcessError):
            self.check_receipt()

    def test_missing_post_snapshot_commits_are_rejected(self):
        self.receipt["postSnapshotCommits"] = [self.receipt["sourceCommit"]]
        with self.assertRaisesRegex(ValueError, "Post-snapshot"):
            self.check_receipt()

    def test_unpreserved_source_ref_is_rejected(self):
        self.receipt["sourceRefs"][0]["commit"] = "0" * 40
        with self.assertRaises(subprocess.CalledProcessError):
            self.check_receipt()


if __name__ == "__main__":
    unittest.main()
