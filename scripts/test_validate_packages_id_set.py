import os
import subprocess
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from validate_packages_id_set import (
    DEFAULT_EXPECTED,
    STUDIO_CONTENT,
    id_set_errors,
    is_stable_310,
    load_expected_ids,
    packed_ids,
    studio_content_errors,
)

SCRIPT = Path(__file__).with_name("validate_packages_id_set.py")


class Stable310Tests(unittest.TestCase):
    def test_stable_310_tags_enable_the_consolidated_pack(self):
        for version in ("3.10.0", "3.10.1", "3.10.12"):
            with self.subTest(version=version):
                self.assertTrue(is_stable_310(version))

    def test_previews_other_lines_and_noncanonical_patches_keep_the_default_pack(self):
        for version in ("3.10.0-preview.1", "3.10.0-rc1", "3.10.1-preview.2", "3.9.0", "3.11.0", "3.10", "3.10.01", ""):
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
            path = directory / f"{package_id}.{version}.nupkg"
            entry = STUDIO_CONTENT.get(package_id)
            if entry:
                with zipfile.ZipFile(path, "w") as archive:
                    archive.writestr(entry, b"bundle")
            else:
                path.write_bytes(b"nupkg")
            (directory / f"{package_id}.{version}.snupkg").write_bytes(b"snupkg")

    def test_packed_ids_ignore_symbol_packages_and_read_the_id(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            self.write_packages(directory, ["Elsa", "Elsa.Workflows.Core"], version="3.10.0-preview.1")
            self.assertEqual(["Elsa", "Elsa.Workflows.Core"], packed_ids(directory))

    def test_matching_set_is_accepted(self):
        self.assertEqual([], id_set_errors(list(self.expected), list(self.expected)))

    def test_mismatch_prints_missing_and_unexpected_ids_first(self):
        missing_case = [package_id for package_id in self.expected if package_id != "Elsa"]
        extra_case = [*self.expected, "Elsa.SamplePackage"]
        for produced in (missing_case, extra_case):
            with self.subTest(produced_count=len(produced)):
                errors = id_set_errors(produced, self.expected)
                text = "\n".join(errors)
                expected_index = text.find("Expected (231):")
                self.assertGreater(expected_index, 0)
                self.assertTrue("Missing (1):\nElsa" in text or "Unexpected (1):\nElsa.SamplePackage" in text)
                if "Missing (1):" in text:
                    self.assertLess(text.find("Missing (1):"), expected_index)
                if "Unexpected (1):" in text:
                    self.assertLess(text.find("Unexpected (1):"), expected_index)

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
            self.assertIn("Studio package content matches 2 required entries.", accepted.stdout)

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
            self.assertLess(missing.stderr.find("Missing (1):"), missing.stderr.find("Expected (231):"))

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
            self.assertLess(extra.stderr.find("Unexpected (1):"), extra.stderr.find("Expected (231):"))

    def test_empty_or_unset_packages_dir_fails_clearly(self):
        for value in (None, ""):
            with self.subTest(value=value):
                env = {key: item for key, item in os.environ.items() if key != "PACKAGES_DIR"}
                if value is not None:
                    env["PACKAGES_DIR"] = value
                result = subprocess.run(
                    [sys.executable, str(SCRIPT)],
                    env=env,
                    capture_output=True,
                    text=True,
                )
                self.assertEqual(1, result.returncode)
                self.assertIn("PACKAGES_DIR must be set", result.stderr)

    def test_missing_expected_list_fails_clearly(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            missing = directory / "missing-ids.txt"
            result = subprocess.run(
                [sys.executable, str(SCRIPT)],
                env={**os.environ, "PACKAGES_DIR": str(directory), "EXPECTED_IDS": str(missing)},
                capture_output=True,
                text=True,
            )
            self.assertEqual(1, result.returncode)
            self.assertIn("Expected ID list not found", result.stderr)
            self.assertNotIn("Traceback", result.stderr)

    def test_empty_expected_list_fails_clearly(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            empty = directory / "empty-ids.txt"
            empty.write_text("", encoding="utf-8")
            result = subprocess.run(
                [sys.executable, str(SCRIPT)],
                env={**os.environ, "PACKAGES_DIR": str(directory), "EXPECTED_IDS": str(empty)},
                capture_output=True,
                text=True,
            )
            self.assertEqual(1, result.returncode)
            self.assertIn("lists no package IDs", result.stderr)


class StudioContentTests(unittest.TestCase):
    def test_required_bundle_entry_is_accepted(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            for package_id, entry in STUDIO_CONTENT.items():
                with zipfile.ZipFile(directory / f"{package_id}.3.10.0.nupkg", "w") as archive:
                    archive.writestr(entry, b"bundle")
            self.assertEqual([], studio_content_errors(directory))

    def test_missing_bundle_entry_fails(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            package_id, entry = next(iter(STUDIO_CONTENT.items()))
            with zipfile.ZipFile(directory / f"{package_id}.3.10.0.nupkg", "w") as archive:
                archive.writestr("lib/net10.0/placeholder.dll", b"dll")
            errors = studio_content_errors(directory)
            self.assertEqual([f"Missing required content {entry} in {package_id}.3.10.0.nupkg."], errors)

    def test_script_rejects_a_studio_nupkg_with_the_bundle_removed(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            expected = load_expected_ids(DEFAULT_EXPECTED)
            for package_id in expected:
                path = directory / f"{package_id}.3.10.0.nupkg"
                entry = STUDIO_CONTENT.get(package_id)
                with zipfile.ZipFile(path, "w") as archive:
                    if entry:
                        archive.writestr(entry, b"bundle")
                    else:
                        archive.writestr("lib/net10.0/placeholder.dll", b"dll")
            package_id, entry = next(iter(STUDIO_CONTENT.items()))
            mutated = directory / f"{package_id}.3.10.0.nupkg"
            with zipfile.ZipFile(mutated) as archive:
                others = [name for name in archive.namelist() if name != entry]
                contents = {name: archive.read(name) for name in others}
            mutated.unlink()
            with zipfile.ZipFile(mutated, "w") as archive:
                for name, data in contents.items():
                    archive.writestr(name, data)
            result = subprocess.run(
                [sys.executable, str(SCRIPT)],
                env={**os.environ, "PACKAGES_DIR": str(directory)},
                capture_output=True,
                text=True,
            )
            self.assertEqual(1, result.returncode)
            self.assertIn(f"Missing required content {entry}", result.stderr)


class WorkflowConditionTests(unittest.TestCase):
    def test_packages_workflow_enables_crc_only_for_stable_310(self):
        workflow = Path(__file__).resolve().parents[1] / ".github/workflows/packages.yml"
        source = workflow.read_text(encoding="utf-8")
        self.assertIn('if [[ "${VERSION}" =~ ^3\\.10\\.(0|[1-9][0-9]*)$ ]]; then', source)
        self.assertIn("CONSOLIDATED_RELEASE_CANDIDATE=true", source)
        self.assertIn("python3 scripts/validate_packages_id_set.py", source)
        self.assertIn("scripts/integration-program/build_studio_clientlibs.sh", source)
        self.assertLess(
            source.find("scripts/integration-program/build_studio_clientlibs.sh"),
            source.find("name: Compile+Pack"),
        )
        self.assertIn("ConsolidatedReleaseCandidate: ${{ env.CONSOLIDATED_RELEASE_CANDIDATE }}", source)
        self.assertIn("if: ${{ env.CONSOLIDATED_RELEASE_CANDIDATE == 'true' }}", source)


if __name__ == "__main__":
    unittest.main()
