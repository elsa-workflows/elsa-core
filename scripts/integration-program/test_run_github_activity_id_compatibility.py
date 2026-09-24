import hashlib
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

from run_github_activity_id_compatibility import (
    CORE_REFERENCE,
    REPOSITORY,
    ProofError,
    core_build_input_paths,
    override_fixture_project_reference,
    read_test_counts,
    resolve_dotnet,
)


class GitHubActivityCompatibilityRunnerTests(unittest.TestCase):
    def test_project_reference_override_requires_exactly_one_expected_reference(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            project = root / "GitHub.csproj"
            project.write_bytes(b"\xef\xbb\xbf<Project>\n  <" + CORE_REFERENCE + b" />\n</Project>")
            core = root / "core & build inputs"
            core_project = core / "src/modules/Elsa/Elsa.csproj"
            core_project.parent.mkdir(parents=True)
            core_project.write_text("<Project />", encoding="utf-8")

            original = project.read_bytes()
            original_sha = override_fixture_project_reference(project, core)

            self.assertEqual(hashlib.sha256(original).hexdigest(), original_sha)
            self.assertIn(b"&amp;", project.read_bytes())
            self.assertTrue(project.read_bytes().startswith(b"\xef\xbb\xbf"))
            xml = ET.fromstring(project.read_bytes())
            reference = xml.find("ProjectReference")
            self.assertIsNotNone(reference)
            self.assertEqual(str(core_project.resolve()), reference.get("Include"))

            project.write_bytes(b"<Project />")
            with self.assertRaisesRegex(ProofError, "found 0"):
                override_fixture_project_reference(project, core)

    def test_core_build_input_guard_includes_root_targets_and_nuget_config(self):
        paths = core_build_input_paths(REPOSITORY)
        self.assertIn("src", paths)
        self.assertIn("Directory.Build.targets", paths)
        self.assertIn("NuGet.Config", paths)

    def test_relative_dotnet_command_is_resolved_to_absolute_executable(self):
        dotnet = resolve_dotnet(sys.executable)
        self.assertTrue(dotnet.is_absolute())
        self.assertTrue(dotnet.is_file())

    def test_trx_receipt_rejects_skips_and_inconsistent_counters(self):
        passed_results = "".join(
            f'<UnitTestResult testName="test-{index}" outcome="Passed" />' for index in range(6)
        )
        counters = (
            'total="6" executed="6" passed="6" failed="0" error="0" timeout="0" '
            'aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" '
            'notExecuted="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0"'
        )
        template = (
            '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">'
            '<Results>{results}</Results><ResultSummary outcome="Completed">'
            '<Counters {counts} /></ResultSummary></TestRun>'
        )
        with tempfile.TemporaryDirectory() as directory:
            trx = Path(directory) / "results.trx"
            trx.write_text(template.format(results=passed_results, counts=counters), encoding="utf-8")
            self.assertTrue(read_test_counts(trx)["passed"])

            skipped_results = passed_results.replace('test-5" outcome="Passed', 'test-5" outcome="NotExecuted')
            skipped_counters = counters.replace('passed="6"', 'passed="5"').replace(
                'notExecuted="0"', 'notExecuted="1"'
            )
            trx.write_text(template.format(results=skipped_results, counts=skipped_counters), encoding="utf-8")
            self.assertFalse(read_test_counts(trx)["passed"])


if __name__ == "__main__":
    unittest.main()
