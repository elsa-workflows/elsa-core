import json
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
    STABLE_310_PATTERN,
    STUDIO_CONTENT,
    apply_exclude_ids,
    exclusion_dependency_errors,
    filename_package_id,
    id_set_errors,
    is_stable_310,
    load_expected_ids,
    packed_ids,
    parse_exclude_ids,
    resolve_exclude_ids,
    studio_content_errors,
)

SCRIPT = Path(__file__).with_name("validate_packages_id_set.py")
sys.path.insert(0, str(Path(__file__).resolve().parent / "integration-program"))
import consolidated_candidate_input as candidate_input


class Stable310Tests(unittest.TestCase):
    def test_stable_310_versions_match_the_canonical_pattern(self):
        for version in ("3.10.0", "3.10.1", "3.10.12"):
            with self.subTest(version=version):
                self.assertTrue(is_stable_310(version))

    def test_previews_other_lines_and_noncanonical_patches_are_not_stable_310(self):
        for version in ("3.10.0-preview.1", "3.10.0-rc1", "3.10.1-preview.2", "3.9.0", "3.11.0", "3.10", "3.10.01", ""):
            with self.subTest(version=version):
                self.assertFalse(is_stable_310(version))

    def test_python_stable_310_pattern_is_the_single_source(self):
        from validate_packages_publication_selection import CANONICAL_310

        self.assertEqual(STABLE_310_PATTERN, r"3\.10\.(0|[1-9][0-9]*)")
        self.assertEqual(CANONICAL_310.pattern, STABLE_310_PATTERN + r"(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?")
        targets = Path(__file__).resolve().parents[1] / "build/ConsolidatedPackageProof.targets"
        self.assertIn(STABLE_310_PATTERN, targets.read_text(encoding="utf-8"))


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
            entries = STUDIO_CONTENT.get(package_id)
            if entries:
                with zipfile.ZipFile(path, "w") as archive:
                    for entry in entries:
                        archive.writestr(entry, b"bundle")
            else:
                path.write_bytes(b"nupkg")
            (directory / f"{package_id}.{version}.snupkg").write_bytes(b"snupkg")

    def test_packed_ids_ignore_symbol_packages_and_read_the_id(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            self.write_packages(directory, ["Elsa", "Elsa.Workflows.Core"], version="3.10.0-preview.1")
            self.assertEqual(["Elsa", "Elsa.Workflows.Core"], packed_ids(directory))

    def test_packed_ids_reject_mixed_versions(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            (directory / "Elsa.3.10.0.nupkg").write_bytes(b"nupkg")
            (directory / "Elsa.Workflows.Core.3.10.1.nupkg").write_bytes(b"nupkg")
            with self.assertRaises(SystemExit) as raised:
                packed_ids(directory)
            self.assertIn("mix versions", str(raised.exception))

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
            self.assertIn("exclusions: none", accepted.stdout)
            self.assertIn("231 expected IDs", accepted.stdout)
            self.assertIn("Studio package content matches 8 required entries.", accepted.stdout)

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
            missing = Path(temporary) / "missing-ids.txt"
            with self.assertRaises(SystemExit) as ctx:
                load_expected_ids(missing)
            self.assertIn("Expected ID list not found", str(ctx.exception))

    def test_empty_expected_list_fails_clearly(self):
        with tempfile.TemporaryDirectory() as temporary:
            empty = Path(temporary) / "empty-ids.txt"
            empty.write_text("", encoding="utf-8")
            with self.assertRaises(SystemExit) as ctx:
                load_expected_ids(empty)
            self.assertIn("lists no package IDs", str(ctx.exception))

    def test_script_has_no_skip_bypass_or_override(self):
        source = SCRIPT.read_text(encoding="utf-8")
        self.assertNotIn("EXPECTED_IDS", source)
        lowered = source.lower()
        for marker in ("skip_id_set", "skip_guard", "bypass_id_set", "bypass_guard", "--skip", "--bypass"):
            self.assertNotIn(marker, lowered)


class ExcludeIdsTests(unittest.TestCase):
    def setUp(self):
        self.expected = load_expected_ids(DEFAULT_EXPECTED)
        self.assertEqual(231, len(self.expected))

    def write_packages(self, directory, ids, version="3.10.0", dependencies=None):
        dependencies = dependencies or {}
        for package_id in ids:
            path = directory / f"{package_id}.{version}.nupkg"
            dep_xml = "".join(
                f'<dependency id="{dep}" version="{version}" />'
                for dep in dependencies.get(package_id, ())
            )
            nuspec = (
                '<?xml version="1.0"?>'
                '<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">'
                "<metadata>"
                f"<id>{package_id}</id>"
                f"<version>{version}</version>"
                "<authors>Elsa</authors>"
                "<description>test</description>"
                f"<dependencies>{dep_xml}</dependencies>"
                "</metadata>"
                "</package>"
            )
            with zipfile.ZipFile(path, "w") as archive:
                archive.writestr(f"{package_id}.nuspec", nuspec)
                for entry in STUDIO_CONTENT.get(package_id, ()):
                    archive.writestr(entry, b"bundle")
            (directory / f"{package_id}.{version}.snupkg").write_bytes(b"snupkg")

    def test_empty_exclude_ids_leaves_the_full_set(self):
        for raw in (None, "", "  "):
            with self.subTest(raw=raw):
                self.assertEqual([], parse_exclude_ids(raw))
                self.assertEqual([], resolve_exclude_ids(raw, self.expected))
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            self.write_packages(directory, self.expected)
            remaining, excluded = apply_exclude_ids(directory, list(self.expected), "")
            self.assertEqual([], excluded)
            self.assertEqual(self.expected, remaining)
            self.assertEqual(231, len(packed_ids(directory)))
            self.assertEqual(self.expected, packed_ids(directory))

    def test_valid_exclusion_adjusts_the_expected_count(self):
        skipped = self.expected[0]
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            self.write_packages(directory, self.expected)
            remaining, excluded = apply_exclude_ids(directory, list(self.expected), f"  {skipped.lower()}  ")
            self.assertEqual([skipped], excluded)
            self.assertEqual(230, len(remaining))
            self.assertNotIn(skipped, remaining)
            self.assertEqual(230, len(packed_ids(directory)))
            self.assertNotIn(skipped, packed_ids(directory))
            self.assertFalse(any(filename_package_id(path.name) == skipped for path in directory.iterdir() if path.is_file()))
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            self.write_packages(directory, self.expected)
            accepted = subprocess.run(
                [sys.executable, str(SCRIPT)],
                env={**os.environ, "PACKAGES_DIR": str(directory), "EXCLUDE_IDS": skipped},
                capture_output=True,
                text=True,
            )
            self.assertEqual(0, accepted.returncode, accepted.stderr)
            self.assertIn(f"exclusions: {skipped}", accepted.stdout)
            self.assertIn("230 expected IDs", accepted.stdout)
            self.assertNotIn(skipped, packed_ids(directory))

    def test_unknown_id_fails(self):
        with self.assertRaises(SystemExit) as raised:
            resolve_exclude_ids("NotAPackageId", self.expected)
        self.assertIn("not in the expected package set", str(raised.exception))
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            self.write_packages(directory, self.expected)
            rejected = subprocess.run(
                [sys.executable, str(SCRIPT)],
                env={**os.environ, "PACKAGES_DIR": str(directory), "EXCLUDE_IDS": "NotAPackageId"},
                capture_output=True,
                text=True,
            )
            self.assertEqual(1, rejected.returncode)
            self.assertIn("not in the expected package set", rejected.stderr)
            self.assertEqual(231, len(packed_ids(directory)))

    def test_duplicate_id_fails(self):
        package_id = self.expected[0]
        for raw in (f"{package_id},{package_id}", f"{package_id}, {package_id.lower()}"):
            with self.subTest(raw=raw), self.assertRaises(SystemExit) as raised:
                parse_exclude_ids(raw)
            self.assertIn("more than once", str(raised.exception))
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            self.write_packages(directory, self.expected)
            rejected = subprocess.run(
                [sys.executable, str(SCRIPT)],
                env={**os.environ, "PACKAGES_DIR": str(directory), "EXCLUDE_IDS": f"{package_id},{package_id}"},
                capture_output=True,
                text=True,
            )
            self.assertEqual(1, rejected.returncode)
            self.assertIn("more than once", rejected.stderr)
            self.assertEqual(231, len(packed_ids(directory)))

    def test_empty_entry_fails(self):
        package_id = self.expected[0]
        for raw in (f"{package_id},", f",{package_id}", f"{package_id},,{self.expected[1]}"):
            with self.subTest(raw=raw), self.assertRaises(SystemExit) as raised:
                parse_exclude_ids(raw)
            self.assertIn("empty entry", str(raised.exception))

    def test_exclusion_fails_when_a_remaining_package_depends_on_it(self):
        skipped = self.expected[0]
        dependent = self.expected[1]
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            self.write_packages(directory, self.expected, dependencies={dependent: (skipped,)})
            errors = exclusion_dependency_errors(directory, {skipped})
            self.assertTrue(errors)
            self.assertIn("Remaining packages depend on an excluded ID:", errors)
            self.assertIn(f"{dependent} depends on {skipped}", errors)
            rejected = subprocess.run(
                [sys.executable, str(SCRIPT)],
                env={**os.environ, "PACKAGES_DIR": str(directory), "EXCLUDE_IDS": skipped},
                capture_output=True,
                text=True,
            )
            self.assertEqual(1, rejected.returncode)
            self.assertIn("Remaining packages depend on an excluded ID:", rejected.stderr)
            self.assertIn(f"{dependent} depends on {skipped}", rejected.stderr)
            self.assertNotIn(skipped, packed_ids(directory))

    def test_exclusion_dependency_match_is_case_insensitive(self):
        skipped = self.expected[0]
        dependent = self.expected[1]
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            self.write_packages(directory, self.expected, dependencies={dependent: (skipped.lower(),)})
            errors = exclusion_dependency_errors(directory, {skipped})
            self.assertIn(f"{dependent} depends on {skipped}", errors)

    def test_exclusion_cannot_clear_an_unexpected_packed_id(self):
        stray = "Elsa.SamplePackage"
        self.assertNotIn(stray, self.expected)
        with self.assertRaises(SystemExit) as raised:
            resolve_exclude_ids(stray, self.expected)
        self.assertIn("not in the expected package set", str(raised.exception))


class StudioContentTests(unittest.TestCase):
    def test_required_assets_match_clientlib_assets(self):
        sys.path.insert(0, str(Path(__file__).resolve().parent / "integration-program"))
        from prove_consolidated_packages import CLIENTLIB_ASSETS

        expected = {
            package_id: tuple(f"staticwebassets/{name}" for name in files)
            for package_id, (_project, files) in CLIENTLIB_ASSETS.items()
        }
        self.assertEqual(expected, STUDIO_CONTENT)
        self.assertEqual(3, len(STUDIO_CONTENT["Elsa.Studio.Workflows.Designer"]))
        self.assertEqual(3, len(STUDIO_CONTENT["Elsa.Studio.DomInterop"]))
        self.assertEqual(2, len(STUDIO_CONTENT["Elsa.Studio.Bpmn.BpmnJs"]))

    def test_required_bundle_entries_are_accepted(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            for package_id, entries in STUDIO_CONTENT.items():
                with zipfile.ZipFile(directory / f"{package_id}.3.10.0.nupkg", "w") as archive:
                    for entry in entries:
                        archive.writestr(entry, b"bundle")
            self.assertEqual([], studio_content_errors(directory))

    def test_missing_bundle_entry_fails(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            package_id, entries = next(iter(STUDIO_CONTENT.items()))
            with zipfile.ZipFile(directory / f"{package_id}.3.10.0.nupkg", "w") as archive:
                archive.writestr("lib/net10.0/placeholder.dll", b"dll")
                for entry in entries[1:]:
                    archive.writestr(entry, b"bundle")
            errors = studio_content_errors(directory)
            self.assertEqual([f"Missing required content {entries[0]} in {package_id}.3.10.0.nupkg."], errors)

    def test_script_rejects_a_studio_nupkg_with_the_bundle_removed(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            expected = load_expected_ids(DEFAULT_EXPECTED)
            for package_id in expected:
                path = directory / f"{package_id}.3.10.0.nupkg"
                entries = STUDIO_CONTENT.get(package_id)
                with zipfile.ZipFile(path, "w") as archive:
                    if entries:
                        for entry in entries:
                            archive.writestr(entry, b"bundle")
                    else:
                        archive.writestr("lib/net10.0/placeholder.dll", b"dll")
            package_id, entries = next(iter(STUDIO_CONTENT.items()))
            entry = entries[0]
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


# Oct 6 pin (artifact 11412848210): 231 minus these seven, plus Elsa.SamplePackage = 225.
STALE_OCT6_MISSING = (
    "Elsa.Connections",
    "Elsa.Connections.Credentials.Persistence.EFCore",
    "Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql",
    "Elsa.Connections.Credentials.Workflows",
    "Elsa.Workflows.Admission",
    "Elsa.Workflows.Admission.Persistence.EFCore",
    "Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql",
)
GUARD_COMMAND = "python3 scripts/validate_packages_id_set.py"


class SharedPublisherGuardTests(unittest.TestCase):
    def test_pin_inventory_ids_are_tied_to_the_guard_list(self):
        pin = json.loads(candidate_input.PIN_INVENTORY.read_text(encoding="utf-8"))
        pin_ids = {row["id"] for row in pin["packages"]}
        guard_ids = set(load_expected_ids(DEFAULT_EXPECTED))
        self.assertEqual(pin_ids, guard_ids)
        self.assertEqual(231, len(pin_ids))
        self.assertEqual(candidate_input.PIN_PACKAGE_COUNT, len(pin_ids))

    def test_stale_225_set_can_never_be_published_as_310(self):
        expected = load_expected_ids(DEFAULT_EXPECTED)
        for package_id in STALE_OCT6_MISSING:
            self.assertIn(package_id, expected)
        stale = [package_id for package_id in expected if package_id not in STALE_OCT6_MISSING]
        stale.append("Elsa.SamplePackage")
        self.assertEqual(225, len(stale))
        errors = id_set_errors(stale, expected)
        text = "\n".join(errors)
        self.assertTrue(errors)
        self.assertIn("Missing (7):", text)
        for package_id in STALE_OCT6_MISSING:
            self.assertIn(package_id, text)
        self.assertIn("Unexpected (1):", text)
        self.assertIn("Elsa.SamplePackage", text)

    def test_preflight_and_upgrade_workflows_pin_the_same_artifact(self):
        root = Path(__file__).resolve().parents[1]
        for relative in (
            ".github/workflows/consolidated-package-recovery-preflight.yml",
            ".github/workflows/stable-persisted-workflow-upgrade-proof.yml",
        ):
            source = (root / relative).read_text(encoding="utf-8")
            for value in (
                str(candidate_input.ARTIFACT),
                str(candidate_input.RUN),
                candidate_input.SOURCE,
                candidate_input.ARCHIVE_SHA256,
                str(candidate_input.ARCHIVE_SIZE),
                candidate_input.EXPIRES_AT,
            ):
                self.assertIn(value, source)
            self.assertNotIn("11412848210", source)
            self.assertNotIn("37456860080", source)
            self.assertNotIn("ba5b348aa2414fdf7c19d9d8806e87b7c91a4205", source)

    def test_both_publishers_run_the_same_id_set_guard_before_push(self):
        root = Path(__file__).resolve().parents[1]
        packages = (root / ".github/workflows/packages.yml").read_text(encoding="utf-8")
        candidate = (root / ".github/workflows/publish-consolidated-original-candidate.yml").read_text(encoding="utf-8")
        self.assertIn(GUARD_COMMAND, packages)
        self.assertIn(GUARD_COMMAND, candidate)
        self.assertLess(packages.find(GUARD_COMMAND), packages.find("NuGet login (OIDC)"))
        self.assertLess(
            candidate.find(GUARD_COMMAND),
            candidate.find("consolidated_package_executor.py publish"),
        )
        for source in (packages, candidate):
            lowered = source.lower()
            for marker in ("skip_id_set", "skip_guard", "bypass_id_set", "bypass_guard"):
                self.assertNotIn(marker, lowered)
            self.assertNotIn("EXPECTED_IDS", source)


class WorkflowConditionTests(unittest.TestCase):
    def test_packages_workflow_promotes_stable_310_instead_of_packing(self):
        workflow = Path(__file__).resolve().parents[1] / ".github/workflows/packages.yml"
        source = workflow.read_text(encoding="utf-8")
        self.assertNotIn("CONSOLIDATED_RELEASE_CANDIDATE", source)
        self.assertNotIn("ConsolidatedReleaseCandidate", source)
        self.assertNotIn("build_studio_clientlibs.sh", source)
        self.assertIn("python3 scripts/stage_consolidated_candidate_nupkgs.py retrieve", source)
        self.assertIn("python3 scripts/stage_consolidated_candidate_nupkgs.py stage", source)
        self.assertIn("python3 scripts/validate_packages_id_set.py", source)
        self.assertIn("exclude_ids:", source)
        self.assertIn("EXCLUDE_IDS: ${{ inputs.exclude_ids }}", source)
        candidate = Path(__file__).resolve().parents[1] / ".github/workflows/publish-consolidated-original-candidate.yml"
        self.assertNotIn("exclude_ids", candidate.read_text(encoding="utf-8"))
        self.assertNotIn("EXCLUDE_IDS", candidate.read_text(encoding="utf-8"))
        compile_if = (
            "if: ${{ !(github.event_name == 'workflow_dispatch' && inputs.publish_nuget"
            " && startsWith(github.ref, 'refs/tags/3.10.') && !contains(github.ref_name, '-')) }}"
        )
        self.assertIn(compile_if, source)


if __name__ == "__main__":
    unittest.main()
