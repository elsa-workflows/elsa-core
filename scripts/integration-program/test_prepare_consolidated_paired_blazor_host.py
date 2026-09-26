"""Focused preparation guards for the consolidated paired Blazor probe."""

from __future__ import annotations

import json
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from prepare_consolidated_paired_blazor_host import CONTRACT, FIXTURE, REFERENCES, ROOT, SOURCE_FILES, build_host, materialize


class ConsolidatedPairedBlazorHostTests(unittest.TestCase):
    def test_focused_solution_filter_contains_imported_pair_and_core(self) -> None:
        solution_filter = json.loads((ROOT / "Elsa.WorkflowContexts.Debug.slnf").read_text(encoding="utf-8"))
        self.assertEqual(solution_filter["solution"]["path"], "Elsa.sln")
        projects = solution_filter["solution"]["projects"]
        self.assertEqual(len(projects), 3)
        self.assertEqual(set(projects), {
            "src/modules/Elsa/Elsa.csproj",
            "src/extensions/workflows/Elsa.WorkflowContexts/Elsa.WorkflowContexts.csproj",
            "src/extensions/workflows/Elsa.Studio.WorkflowContexts/Elsa.Studio.WorkflowContexts.csproj",
        })
        self.assertTrue(all((ROOT / project).is_file() for project in projects))

    def test_materializes_reviewed_fixture_with_imported_source_references(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "new-probe"
            receipt = materialize(output)

            project = (output / "UiProbe/UiProbe.csproj").read_text(encoding="utf-8")
            for original, mapped in REFERENCES.items():
                self.assertNotIn(original, project)
                self.assertIn(str(ROOT / mapped), project)
            for name in SOURCE_FILES:
                self.assertEqual((FIXTURE / name).read_bytes(), (output / "UiProbe" / name).read_bytes())
            self.assertEqual(CONTRACT.read_bytes(), (output / "ContractProbe/HttpProbe.cs").read_bytes())
            self.assertFalse(receipt["published"])
            self.assertFalse(receipt["browserVerified"])
            self.assertFalse(receipt["debuggerVerified"])

    def test_refuses_existing_or_source_tree_output(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaises(FileExistsError):
                materialize(Path(directory))
        with self.assertRaises(ValueError):
            materialize(ROOT / "unsafe-probe")

    def test_refuses_ambient_parent_configuration_before_writing(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            parent = Path(directory)
            (parent / "Global.Json").write_text('{"sdk":{"version":"0.0.0"}}', encoding="utf-8")
            output = parent / "new-probe"
            with self.assertRaisesRegex(ValueError, "Ambient build configuration"):
                materialize(output)
            self.assertFalse(output.exists())

    def test_accepts_nested_new_output_without_ambient_configuration(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "new-parent" / "new-probe"
            receipt = materialize(output)
            self.assertEqual(str(output.resolve() / "UiProbe/UiProbe.csproj"), receipt["hostProject"])

    def test_build_launch_errors_keep_a_failure_receipt(self) -> None:
        for error in (subprocess.TimeoutExpired(["dotnet"], 900), FileNotFoundError("dotnet")):
            with self.subTest(error=type(error).__name__), tempfile.TemporaryDirectory() as directory:
                output = Path(directory) / "new-probe"
                receipt = materialize(output)
                with patch("prepare_consolidated_paired_blazor_host.subprocess.run", side_effect=error):
                    with self.assertRaisesRegex(RuntimeError, "Host build did not finish"):
                        build_host(output, receipt)
                saved = json.loads((output / "evidence.json").read_text(encoding="utf-8"))
                self.assertIsNone(saved["buildExitCode"])
                self.assertEqual(type(error).__name__, saved["buildFailure"])
                self.assertFalse(saved["published"])
                self.assertFalse(saved["browserVerified"])
                self.assertFalse(saved["debuggerVerified"])
                self.assertTrue((output / "build.log").is_file())


if __name__ == "__main__":
    unittest.main()
