"""Prove the unpublished Slack package and its external symbols are paired."""

from __future__ import annotations

import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
from zipfile import ZipFile

import verify_slack_source_link as proof


VERSION = "3.8.5-proof.123.1"
COMMIT = "a" * 40


class SlackSourceLinkArtifactTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        directory = tempfile.TemporaryDirectory(prefix="elsa-slack-symbol-pair-test-")
        cls.addClassCleanup(directory.cleanup)
        cls.root = Path(directory.name)
        cls.binaries = {}
        for variant, value in (("first", 1), ("second", 2)):
            project = cls.root / variant
            project.mkdir()
            (project / "Elsa.Slack.csproj").write_text(
                '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework>'
                '<DebugType>portable</DebugType></PropertyGroup></Project>\n', encoding="utf-8"
            )
            (project / "Marker.cs").write_text(
                f"public static class Marker {{ public static int Value => {value}; }}\n", encoding="utf-8"
            )
            output = project / "compiled"
            subprocess.run(
                ["dotnet", "build", str(project / "Elsa.Slack.csproj"), "--configuration", "Release",
                 "--output", str(output), "--nologo", "--verbosity", "quiet"],
                check=True,
                capture_output=True,
                text=True,
            )
            cls.binaries[variant] = (output / "Elsa.Slack.dll", output / "Elsa.Slack.pdb")

    def package_pair(self, *, mismatched_framework: str | None = None) -> Path:
        artifacts = self.root / f"artifacts-{self._testMethodName}"
        artifacts.mkdir()
        assembly, symbols = self.binaries["first"]
        with ZipFile(artifacts / f"Elsa.Slack.{VERSION}.nupkg", "w") as package:
            for framework in proof.TFMS:
                package.write(assembly, f"lib/{framework}/Elsa.Slack.dll")
        with ZipFile(artifacts / f"Elsa.Slack.{VERSION}.snupkg", "w") as symbol_package:
            for framework in proof.TFMS:
                selected = self.binaries["second"][1] if framework == mismatched_framework else symbols
                symbol_package.write(selected, f"lib/{framework}/Elsa.Slack.pdb")
        return artifacts

    def call_verifier(self, artifacts: Path) -> dict[str, object]:
        frameworks = [{"framework": framework, "sourceDocumentCount": 1} for framework in proof.TFMS]
        with patch.object(proof, "require_sourcelink_tool", return_value=(Path("source-link.dll"), "3.1.1", "a" * 64)):
            with patch.object(proof, "verify_imported_source_link", return_value=frameworks) as source_check:
                result = proof.verify(artifacts, VERSION, COMMIT, Path("sourcelink"))
        source_check.assert_called_once()
        return result

    def test_matching_package_and_symbols_are_verified_for_each_framework(self) -> None:
        result = self.call_verifier(self.package_pair())
        self.assertEqual([row["assemblySymbolPair"] for row in result["frameworks"]], ["passed"] * len(proof.TFMS))
        self.assertFalse(result["published"])

    def test_mixed_package_and_symbols_are_rejected_before_source_link_check(self) -> None:
        artifacts = self.package_pair(mismatched_framework="net9.0")
        with patch.object(proof, "require_sourcelink_tool", return_value=(Path("source-link.dll"), "3.1.1", "a" * 64)):
            with patch.object(proof, "verify_imported_source_link") as source_check:
                with self.assertRaisesRegex(ValueError, "external symbols do not match for net9.0"):
                    proof.verify(artifacts, VERSION, COMMIT, Path("sourcelink"))
        source_check.assert_not_called()

    def test_extra_artifact_is_rejected(self) -> None:
        artifacts = self.package_pair()
        (artifacts / "unrelated.nupkg").write_bytes(b"")
        with self.assertRaisesRegex(ValueError, "Expected only the Elsa.Slack package and symbols"):
            proof.verify(artifacts, VERSION, COMMIT, Path("sourcelink"))


if __name__ == "__main__":
    unittest.main()
