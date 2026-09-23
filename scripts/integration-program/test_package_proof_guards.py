import sys
import tempfile
import unittest
import hashlib
import subprocess
import zipfile
from pathlib import Path
from subprocess import CompletedProcess
from unittest.mock import patch
from xml.etree import ElementTree

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

    def test_sourcelink_invokes_verified_installed_payload_not_replaceable_shim(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            tool, assembly, payload_hash = self.create_sourcelink_install(Path(temporary_directory))
            tool.write_text("#!/bin/sh\nexit 99\n", encoding="utf-8")
            tool.chmod(0o755)
            listing = "Package Id      Version      Commands\n---------------------------------------\nsourcelink      3.1.1        sourcelink\n"
            with patch(
                "run_slack_package_proof.subprocess.run",
                return_value=CompletedProcess([], 0, listing, ""),
            ):
                verified_assembly, version, verified_hash = proof.require_sourcelink_tool(tool)

            self.assertEqual(assembly.resolve(), verified_assembly)
            self.assertNotEqual(tool, verified_assembly)
            self.assertEqual("3.1.1", version)
            self.assertEqual(payload_hash, verified_hash)

    def test_sourcelink_rejects_modified_installed_payload_even_when_shim_version_matches(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            tool, assembly, _ = self.create_sourcelink_install(Path(temporary_directory))
            assembly.write_bytes(b"replacement payload")
            listing = "Package Id      Version      Commands\n---------------------------------------\nsourcelink      3.1.1        sourcelink\n"
            with patch(
                "run_slack_package_proof.subprocess.run",
                return_value=CompletedProcess([], 0, listing, ""),
            ):
                with self.assertRaisesRegex(RuntimeError, "does not match the pinned 3.1.1 package"):
                    proof.require_sourcelink_tool(tool)

    def test_focused_test_receipt_requires_exact_skipped_baseline_across_all_trx_files(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            results = Path(temporary_directory)
            self.write_trx(results / "first.trx", [(proof.EXPECTED_SKIPPED_TEST, "NotExecuted", proof.EXPECTED_SKIP_MESSAGE)])

            receipt = proof.read_focused_test_receipt(results)

            self.assertEqual({"total": 1, "executed": 0, "passed": 0, "failed": 0, "error": 0, "timeout": 0,
                              "aborted": 0, "inconclusive": 0, "passedButRunAborted": 0, "notRunnable": 0,
                              "notExecuted": 0, "disconnected": 0, "warning": 0, "completed": 0,
                              "inProgress": 0, "pending": 0, "skipped": 1}, receipt["result"])
            self.assertEqual(["first.trx"], [item["file"] for item in receipt["trx_files"]])

    def test_focused_test_receipt_rejects_failure_in_a_later_trx_file(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            results = Path(temporary_directory)
            self.write_trx(results / "first.trx", [(proof.EXPECTED_SKIPPED_TEST, "NotExecuted", proof.EXPECTED_SKIP_MESSAGE)])
            self.write_trx(results / "second.trx", [("Unexpected.Test", "Failed", "assertion failed")])

            with self.assertRaisesRegex(RuntimeError, "exactly one Slack test result"):
                proof.read_focused_test_receipt(results)

    def test_focused_test_receipt_rejects_summary_failure_counters(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            results = Path(temporary_directory)
            self.write_trx(
                results / "result.trx",
                [(proof.EXPECTED_SKIPPED_TEST, "NotExecuted", proof.EXPECTED_SKIP_MESSAGE)],
                counter_overrides={"failed": 1},
            )

            with self.assertRaisesRegex(RuntimeError, "Unexpected focused Slack test counters"):
                proof.read_focused_test_receipt(results)

    def test_focused_test_receipt_rejects_error_run_info(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            results = Path(temporary_directory)
            self.write_trx(
                results / "result.trx",
                [(proof.EXPECTED_SKIPPED_TEST, "NotExecuted", proof.EXPECTED_SKIP_MESSAGE)],
                run_info_outcomes=["Error"],
            )

            with self.assertRaisesRegex(RuntimeError, "failed or aborted run information"):
                proof.read_focused_test_receipt(results)

    def test_final_source_guard_rejects_core_or_extensions_mutations(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory)
            core, core_sha = self.create_git_source(root / "core")
            extensions, extensions_sha = self.create_git_source(root / "extensions")
            with patch.object(proof, "CORE_SHA", core_sha), patch.object(proof, "EXTENSIONS_SHA", extensions_sha):
                proof.require_sources_unchanged(core, extensions)

                (core / "untracked.txt").write_text("concurrent Core output", encoding="utf-8")
                with self.assertRaisesRegex(RuntimeError, "Elsa Core source must be clean"):
                    proof.require_sources_unchanged(core, extensions)

                (core / "untracked.txt").unlink()
                (extensions / "untracked.txt").write_text("concurrent Extensions output", encoding="utf-8")
                with self.assertRaisesRegex(RuntimeError, "Elsa Extensions source must be clean"):
                    proof.require_sources_unchanged(core, extensions)

                (extensions / "untracked.txt").unlink()
                (extensions / "source.txt").write_text("changed source", encoding="utf-8")
                subprocess.run(["git", "add", "source.txt"], cwd=extensions, check=True)
                subprocess.run(["git", "-c", "commit.gpgsign=false", "commit", "-m", "concurrent source update"], cwd=extensions, check=True, capture_output=True)
                with self.assertRaisesRegex(RuntimeError, "Elsa Extensions source must be clean"):
                    proof.require_sources_unchanged(core, extensions)

                (core / "source.txt").write_text("changed source", encoding="utf-8")
                subprocess.run(["git", "add", "source.txt"], cwd=core, check=True)
                subprocess.run(["git", "-c", "commit.gpgsign=false", "commit", "-m", "concurrent Core update"], cwd=core, check=True, capture_output=True)
                with self.assertRaisesRegex(RuntimeError, "Elsa Core source must be clean"):
                    proof.require_sources_unchanged(core, extensions)

    @staticmethod
    def create_sourcelink_install(root: Path) -> tuple[Path, Path, str]:
        tool_dir = root / "tool"
        tool_dir.mkdir()
        tool = tool_dir / "sourcelink"
        tool.write_text("#!/bin/sh\nexit 0\n", encoding="utf-8")
        tool.chmod(0o755)
        package_root = tool_dir / ".store/sourcelink/3.1.1/sourcelink/3.1.1"
        any_dir = package_root / "tools/netcoreapp2.1/any"
        any_dir.mkdir(parents=True)
        nuspec = (
            '<package><metadata><id>sourcelink</id><version>3.1.1</version></metadata></package>'
        )
        settings = (
            '<DotNetCliTool Version="1"><Commands><Command Name="sourcelink" '
            'EntryPoint="sourcelink.dll" Runner="dotnet" /></Commands></DotNetCliTool>'
        )
        payload = b"verified SourceLink assembly payload"
        (package_root / "sourcelink.nuspec").write_text(nuspec, encoding="utf-8")
        (any_dir / "DotnetToolSettings.xml").write_text(settings, encoding="utf-8")
        assembly = any_dir / "sourcelink.dll"
        assembly.write_bytes(payload)
        with zipfile.ZipFile(package_root / "sourcelink.3.1.1.nupkg", "w") as archive:
            archive.writestr("sourcelink.nuspec", nuspec)
            archive.writestr("tools/netcoreapp2.1/any/sourcelink.dll", payload)
        return tool, assembly, hashlib.sha256(payload).hexdigest()

    @staticmethod
    def write_trx(
        path: Path,
        tests: list[tuple[str, str, str]],
        counter_overrides: dict[str, int] | None = None,
        run_info_outcomes: list[str] | None = None,
    ) -> None:
        counters = {name: 0 for name in proof.TRX_COUNTERS}
        counters["total"] = len(tests)
        counters["executed"] = sum(outcome != "NotExecuted" for _, outcome, _ in tests)
        counters["passed"] = sum(outcome == "Passed" for _, outcome, _ in tests)
        counters["failed"] = sum(outcome == "Failed" for _, outcome, _ in tests)
        counters.update(counter_overrides or {})
        root = ElementTree.Element("TestRun")
        results = ElementTree.SubElement(root, "Results")
        for name, outcome, message in tests:
            result = ElementTree.SubElement(results, "UnitTestResult", {"testName": name, "outcome": outcome})
            if message:
                output = ElementTree.SubElement(result, "Output")
                error_info = ElementTree.SubElement(output, "ErrorInfo")
                ElementTree.SubElement(error_info, "Message").text = message
        summary = ElementTree.SubElement(root, "ResultSummary", {"outcome": "Completed"})
        ElementTree.SubElement(summary, "Counters", {name: str(value) for name, value in counters.items()})
        for outcome in run_info_outcomes or []:
            ElementTree.SubElement(ElementTree.SubElement(summary, "RunInfos"), "RunInfo", {"outcome": outcome})
        path.write_bytes(ElementTree.tostring(root, encoding="utf-8", xml_declaration=True))

    @staticmethod
    def create_git_source(root: Path) -> tuple[Path, str]:
        root.mkdir()
        subprocess.run(["git", "init", "-q"], cwd=root, check=True)
        subprocess.run(["git", "config", "user.name", "Proof Test"], cwd=root, check=True)
        subprocess.run(["git", "config", "user.email", "proof@example.invalid"], cwd=root, check=True)
        (root / "source.txt").write_text("pinned source", encoding="utf-8")
        subprocess.run(["git", "add", "source.txt"], cwd=root, check=True)
        subprocess.run(["git", "-c", "commit.gpgsign=false", "commit", "-m", "pinned source"], cwd=root, check=True, capture_output=True)
        commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
        return root, commit


if __name__ == "__main__":
    unittest.main()
