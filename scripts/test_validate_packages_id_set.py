import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from validate_packages_id_set import (
    DEFAULT_EXPECTED,
    id_set_errors,
    is_stable_310,
    load_expected_ids,
    packed_ids,
)

SCRIPT = Path(__file__).with_name("validate_packages_id_set.py")


class Stable310Tests(unittest.TestCase):
    def test_stable_310_tags_enable_the_consolidated_pack(self):
        for version in ("3.10.0", "3.10.1", "3.10.12"):
            with self.subTest(version=version):
                self.assertTrue(is_stable_310(version))

    def test_previews_and_other_lines_keep_the_default_pack(self):
        for version in ("3.10.0-preview.1", "3.10.0-rc1", "3.10.1-preview.2", "3.9.0", "3.11.0", "3.10", ""):
            with self.subTest(version=version):
                self.assertFalse(is_stable_310(version))


class IdSetTests(unittest.TestCase):
    def setUp(self):
        self.expected = load_expected_ids(DEFAULT_EXPECTED)
        self.assertEqual(231, len(self.expected))
        self.assertNotIn("Elsa.SamplePackage", self.expected)
        self.assertNotIn("Elsa.Testing.Extensions", self.expected)
        self.assertNotIn("Elsa.Templates", self.expected)
        for secret in ("Elsa.Secrets.Api", "Elsa.Secrets.Core", "Elsa.Secrets.Management",
                       "Elsa.Secrets.Models", "Elsa.Secrets.Scripting"):
            self.assertNotIn(secret, self.expected)

    def write_packages(self, directory, ids, version="3.10.0"):
        for package_id in ids:
            (directory / f"{package_id}.{version}.nupkg").write_bytes(b"nupkg")
            (directory / f"{package_id}.{version}.snupkg").write_bytes(b"snupkg")

    def test_packed_ids_ignore_symbol_packages_and_read_the_id(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            self.write_packages(directory, ["Elsa", "Elsa.Workflows.Core"], version="3.10.0-preview.1")
            self.assertEqual(["Elsa", "Elsa.Workflows.Core"], packed_ids(directory))

    def test_matching_set_is_accepted(self):
        self.assertEqual([], id_set_errors(list(self.expected), list(self.expected)))

    def test_missing_or_extra_id_prints_both_lists(self):
        missing_case = [package_id for package_id in self.expected if package_id != "Elsa"]
        extra_case = [*self.expected, "Elsa.SamplePackage"]
        for produced in (missing_case, extra_case):
            with self.subTest(produced_count=len(produced)):
                errors = id_set_errors(produced, self.expected)
                text = "\n".join(errors)
                self.assertIn("Expected (231):", text)
                self.assertIn("Produced (", text)
                self.assertTrue("Missing (1):\nElsa" in text or "Unexpected (1):\nElsa.SamplePackage" in text)

    def test_script_accepts_the_expected_set_and_rejects_mutations(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            self.write_packages(directory, self.expected)
            accepted = subprocess.run(
                [sys.executable, str(SCRIPT)],
                env={**os.environ, "PACKAGES_DIR": str(directory)},
                capture_output=True,
                text=True,
            )
            self.assertEqual(0, accepted.returncode, accepted.stderr)
            self.assertIn("231 expected IDs", accepted.stdout)

            (directory / "Elsa.3.10.0.nupkg").unlink()
            missing = subprocess.run(
                [sys.executable, str(SCRIPT)],
                env={**os.environ, "PACKAGES_DIR": str(directory)},
                capture_output=True,
                text=True,
            )
            self.assertEqual(1, missing.returncode)
            self.assertIn("Missing (1):", missing.stderr)
            self.assertIn("Elsa\n", missing.stderr)

            (directory / "Elsa.3.10.0.nupkg").write_bytes(b"nupkg")
            (directory / "Elsa.SamplePackage.3.10.0.nupkg").write_bytes(b"nupkg")
            extra = subprocess.run(
                [sys.executable, str(SCRIPT)],
                env={**os.environ, "PACKAGES_DIR": str(directory)},
                capture_output=True,
                text=True,
            )
            self.assertEqual(1, extra.returncode)
            self.assertIn("Unexpected (1):", extra.stderr)
            self.assertIn("Elsa.SamplePackage", extra.stderr)


class WorkflowConditionTests(unittest.TestCase):
    def test_packages_workflow_enables_crc_only_for_stable_310(self):
        workflow = Path(__file__).resolve().parents[1] / ".github/workflows/packages.yml"
        source = workflow.read_text(encoding="utf-8")
        self.assertIn('if [[ "${VERSION}" =~ ^3\\.10\\.[0-9]+$ ]]; then', source)
        self.assertIn("CONSOLIDATED_RELEASE_CANDIDATE=true", source)
        self.assertIn("python3 scripts/validate_packages_id_set.py", source)
        self.assertIn("ConsolidatedReleaseCandidate: ${{ env.CONSOLIDATED_RELEASE_CANDIDATE }}", source)
        self.assertIn("if: ${{ env.CONSOLIDATED_RELEASE_CANDIDATE == 'true' }}", source)


if __name__ == "__main__":
    unittest.main()
