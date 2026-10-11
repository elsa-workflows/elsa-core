"""Acceptance checks must fail closed, including when Python optimization is enabled."""

import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
VERIFIER = Path("scripts/integration-program/verify_apps_source_import.py")
MANIFEST = Path("docs/integration-program/apps-source-import.json")


class SourceImportRejectionTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.fixture = Path(temporary.name)
        for path in (VERIFIER, MANIFEST):
            target = self.fixture / path
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / path, target)
        self.manifest = json.loads((self.fixture / MANIFEST).read_text())
        self.environment = dict(os.environ)
        self.environment.update(
            GIT_DIR=subprocess.check_output(
                ["git", "rev-parse", "--absolute-git-dir"], cwd=ROOT, text=True
            ).strip(),
            GIT_WORK_TREE=str(self.fixture),
            PYTHONDONTWRITEBYTECODE="1",
        )

    def assert_rejected(self, reason):
        (self.fixture / MANIFEST).write_text(json.dumps(self.manifest))
        for optimization in ([], ["-O"]):
            with self.subTest(optimization=optimization):
                result = subprocess.run(
                    [sys.executable, *optimization, str(self.fixture / VERIFIER)],
                    cwd=self.fixture, env=self.environment, text=True, capture_output=True,
                )
                self.assertNotEqual(result.returncode, 0)
                self.assertIn(reason, result.stderr)
                self.assertNotIn("Apps import verified", result.stdout)

    def test_corrupted_mapping_is_rejected(self):
        self.manifest["assets"][0]["destinationPath"] = "outside-apps/file"
        self.assert_rejected("Unexpected mapping")

    def test_mapped_symlink_is_rejected(self):
        destination = self.fixture / self.manifest["assets"][0]["destinationPath"]
        destination.parent.mkdir(parents=True, exist_ok=True)
        target = self.fixture / "regular-file"
        target.write_text("Symlinks must fail before blob verification.")
        destination.symlink_to(target)
        self.assert_rejected("Mapped asset traverses a symlink")

    def test_mapped_parent_symlink_is_rejected(self):
        external = tempfile.TemporaryDirectory()
        self.addCleanup(external.cleanup)
        asset = self.manifest["assets"][0]
        destination = self.fixture / asset["destinationPath"]
        original = subprocess.check_output(
            ["git", "show", f"{self.manifest['sourceCommit']}:{asset['sourcePath']}"], cwd=ROOT
        )
        for target in (self.fixture / "linked-assets", Path(external.name)):
            with self.subTest(target_inside_checkout=target.is_relative_to(self.fixture)):
                target.mkdir(parents=True, exist_ok=True)
                (target / destination.name).write_bytes(original)
                destination.parent.symlink_to(target, target_is_directory=True)
                try:
                    self.assert_rejected("Mapped asset traverses a symlink")
                finally:
                    destination.parent.unlink()


if __name__ == "__main__":
    unittest.main()
