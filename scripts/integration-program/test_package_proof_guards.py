import sys
import tempfile
import unittest
from pathlib import Path
from subprocess import CompletedProcess
from unittest.mock import patch

SCRIPTS = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPTS))

import run_slack_package_proof as proof  # noqa: E402


class PackageProofGuardTests(unittest.TestCase):
    def test_core_project_reference_must_match_the_supplied_source_tree(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory)
            extension_project = root / "extensions/src/modules/communication/Elsa.Slack/Elsa.Slack.csproj"
            expected_core_project = root / "different-core/src/modules/Elsa/Elsa.csproj"
            extension_project.parent.mkdir(parents=True)
            expected_core_project.parent.mkdir(parents=True)
            extension_project.write_text(
                '<Project><ItemGroup><ProjectReference Include="../../../../../other-core/src/modules/Elsa/Elsa.csproj" />'
                "</ItemGroup></Project>",
                encoding="utf-8",
            )
            expected_core_project.write_text("<Project />", encoding="utf-8")

            with self.assertRaisesRegex(RuntimeError, "expected exactly the supplied Core project"):
                proof.require_core_project_reference(extension_project, expected_core_project)

    def test_sourcelink_tool_must_be_explicit_and_executable(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            missing_tool = Path(temporary_directory) / "sourcelink"
            with self.assertRaisesRegex(RuntimeError, "executable sourcelink 3.1.1"):
                proof.require_sourcelink_tool(missing_tool)

    def test_sourcelink_tool_must_have_the_pinned_version(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            tool = Path(temporary_directory) / "sourcelink"
            tool.write_text("placeholder", encoding="utf-8")
            tool.chmod(0o755)
            listing = "Package Id      Version      Commands\n---------------------------------------\nsourcelink      3.1.2        sourcelink\n"
            with patch(
                "run_slack_package_proof.subprocess.run",
                return_value=CompletedProcess([], 0, listing, ""),
            ):
                with self.assertRaisesRegex(RuntimeError, "Expected SourceLink CLI 3.1.1"):
                    proof.require_sourcelink_tool(tool)


if __name__ == "__main__":
    unittest.main()
