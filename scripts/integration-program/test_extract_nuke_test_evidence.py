from __future__ import annotations

import json
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from extract_nuke_test_evidence import extract


class ExtractNukeTestEvidenceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name) / "rehearsal"
        self.root.mkdir()
        (self.root / "test/unit/Alpha.Tests/bin/Debug/net10.0").mkdir(parents=True)
        (self.root / "test/unit/Beta.Tests/bin/Debug/net10.0").mkdir(parents=True)
        (self.root / "testresults").mkdir()
        (self.root / "test/unit/Alpha.Tests/Alpha.Tests.csproj").write_text("<Project />\n")
        (self.root / "test/unit/Beta.Tests/Beta.Tests.csproj").write_text("<Project />\n")
        (self.root / "Elsa.sln").write_text(
            'Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Alpha.Tests", "test/unit/Alpha.Tests/Alpha.Tests.csproj", "{11111111-1111-1111-1111-111111111111}"\n'
            'Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Beta.Tests", "test/unit/Beta.Tests/Beta.Tests.csproj", "{22222222-2222-2222-2222-222222222222}"\n'
        )
        subprocess.run(["git", "init", "-q", str(self.root)], check=True)
        subprocess.run(["git", "-C", str(self.root), "config", "user.email", "test@example.invalid"], check=True)
        subprocess.run(["git", "-C", str(self.root), "config", "user.name", "Evidence Test"], check=True)
        subprocess.run(["git", "-C", str(self.root), "add", "."], check=True)
        subprocess.run(["git", "-C", str(self.root), "commit", "-qm", "fixture"], check=True)
        self.prep_path = self.root / "consolidated-build-receipt.json"
        self.import_path = self.root / "import-receipt.json"
        self.patch_path = self.root / "source-integration.patch"
        self.patch_path.write_text("fixture patch\n")
        commit = subprocess.run(["git", "-C", str(self.root), "rev-parse", "HEAD"], check=True, text=True, capture_output=True).stdout.strip()
        self.prep_path.write_text(json.dumps({
            "sourceCommits": {"core": "a" * 40, "extensions": "b" * 40, "studio": "c" * 40},
            "rehearsalCommit": commit,
            "patchSha256": self._hash(self.patch_path),
            "canonicalSolution": "Elsa.sln",
        }))
        self.import_path.write_text(json.dumps({
            "sourceCommits": {"core": "a" * 40, "extensions": "b" * 40, "studio": "c" * 40},
            "rehearsalCommit": commit,
            "exactBlobAndModeMapping": True,
            "originalHistoriesReachable": True,
        }))
        self.log_path = self.root.parent / "nuke.log"
        self._write_log()
        self._write_trx()

    @staticmethod
    def _hash(path: Path) -> str:
        import hashlib
        return hashlib.sha256(path.read_bytes()).hexdigest()

    def _write_log(self, test_summary: str = "  Test               Succeeded       0:02   // Passed: 1, Skipped: 0\n") -> None:
        lines = [
            "12:00:00 [INF] BUILD SETUP:",
            "╔ Restore",
            "  Restore            Succeeded       0:01",
            "╔ Compile",
            "  Compile            Succeeded       0:03",
            "╔ Test",
            f"> dotnet test {self.root}/test/unit/Alpha.Tests/Alpha.Tests.csproj --configuration Debug --no-build --results-directory {self.root}/testresults --logger trx;LogFileName=Alpha.Tests.trx",
            f"> dotnet test {self.root}/test/unit/Beta.Tests/Beta.Tests.csproj --configuration Debug --no-build --results-directory {self.root}/testresults --logger trx;LogFileName=Beta.Tests.trx",
            "Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 10 ms - Alpha.Tests.dll (.NETCoreApp,Version=v10.0)",
            "Skipped! - Failed: 0, Passed: 0, Skipped: 1, Total: 1, Duration: 1 ms - Beta.Tests.dll (.NETCoreApp,Version=v10.0)",
            test_summary,
            "Build succeeded on 25/09/2026 12:00:10.",
        ]
        self.log_path.write_text("\n".join(lines) + "\n")

    def _write_trx(self) -> None:
        code_base = self.root / "test/unit/Alpha.Tests/bin/Debug/net10.0/Alpha.Tests.dll"
        xml = f'''<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Times start="2026-09-25T12:00:05+02:00" finish="2026-09-25T12:00:06+02:00" />
  <TestDefinitions><UnitTest><TestMethod codeBase="{code_base}" /></UnitTest></TestDefinitions>
  <ResultSummary><Counters total="1" executed="1" passed="1" failed="0" error="0" timeout="0" aborted="0" /></ResultSummary>
</TestRun>'''
        (self.root / "testresults/Alpha.Tests.trx").write_text(xml)

    def _extract(self, profile_template: Path | None = None) -> dict:
        with patch("extract_nuke_test_evidence.preparation.verify_import_lineage"):
            return extract(self.log_path, self.root, self.prep_path, self.import_path, self.patch_path,
                           profile_template, "./build.sh --target Test")

    def test_extracts_selection_passed_summary_and_retained_trx(self) -> None:
        evidence = self._extract()
        self.assertEqual(2, evidence["selection"]["nukeSelectedTestProjects"])
        self.assertEqual(2, evidence["selection"]["commandsObserved"])
        self.assertEqual(1, evidence["observedTestResults"]["frameworkConsoleSummaries"]["assemblyFrameworkRuns"])
        self.assertEqual({"passed": 1, "failed": 0, "skipped": 0, "total": 1}, evidence["observedTestResults"]["frameworkConsoleSummaries"]["totalsFromPassedSummaries"])
        retained = evidence["observedTestResults"]["retainedTrx"]
        self.assertEqual({"files": 1, "passed": 1, "failed": 0, "skipped": 0, "executed": 1, "totalIncludingSkipped": 1}, {key: retained[key] for key in ("files", "passed", "failed", "skipped", "executed", "totalIncludingSkipped")})
        self.assertEqual(str(self.root / "test/unit/Alpha.Tests/bin/Debug/net10.0/Alpha.Tests.dll"), retained["rows"][0]["codeBases"][0])
        self.assertEqual(self._hash(self.log_path), evidence["invocation"]["logSha256"])
        self.assertEqual({"startLocal": "2026-09-25T12:00:00", "finishLocal": "2026-09-25T12:00:10"},
                         evidence["invocation"]["runWindow"])
        self.assertEqual("succeeded", evidence["invocation"]["targets"]["test"])
        self.assertIn("Skipped!", evidence["observedTestResults"]["selectedProjectsWithoutPassingTestCases"][0]["outcome"])

    def test_rejects_incomplete_nuke_target_summary(self) -> None:
        self._write_log(test_summary="")
        with self.assertRaisesRegex(ValueError, "incomplete or failed"):
            self._extract()

    def test_rejects_retained_trx_from_an_earlier_run(self) -> None:
        log = self.log_path.read_text()
        self.log_path.write_text(log.replace("12:00:00 [INF] BUILD SETUP:", "13:00:00 [INF] BUILD SETUP:")
                                 .replace("25/09/2026 12:00:10", "25/09/2026 13:00:10"))
        with self.assertRaisesRegex(ValueError, "outside recorded NUKE run"):
            self._extract()

    def test_rejects_import_receipt_that_does_not_pin_preparation(self) -> None:
        imported = json.loads(self.import_path.read_text())
        imported["sourceCommits"]["core"] = "d" * 40
        self.import_path.write_text(json.dumps(imported))
        with self.assertRaisesRegex(ValueError, "source pins differ"):
            self._extract()

    def test_uses_dotnet_compile_warning_summary(self) -> None:
        log = self.log_path.read_text()
        self.log_path.write_text(log.replace("╔ Test", "[WRN] Compile: one displayed warning\n[DBG]     7 Warning(s)\n╔ Test"))
        self.assertEqual(7, self._extract()["invocation"]["compile"]["warnings"])

    def test_rejects_publication_target_in_nuke_log(self) -> None:
        self.log_path.write_text(self.log_path.read_text().replace("╔ Test", "║ Publish\n╔ Test"))
        with self.assertRaisesRegex(ValueError, "publication target ran"):
            self._extract()

    def test_rejects_failed_nuke_run_before_parsing_repeated_failure_output(self) -> None:
        self.log_path.write_text(self.log_path.read_text().replace(
            "Build succeeded on", "Build failed on"
        ))
        with self.assertRaisesRegex(ValueError, "NUKE run failed"):
            self._extract()

    def test_pins_current_tip_overlay_patch_bytes(self) -> None:
        overlay = self.root / "workbench.patch"
        overlay.write_text("reviewed overlay\n")
        receipt = self.root / "overlay-receipt.json"
        receipt.write_text(json.dumps({
            "sourcePins": json.loads(self.prep_path.read_text())["sourceCommits"],
            "reviewedOverlayReceipt": [{"name": overlay.name, "sha256": self._hash(overlay)}],
        }))
        evidence = self._extract(receipt)
        self.assertEqual(self._hash(receipt), evidence["profile"]["overlayReceiptSha256"])
        overlay.write_text("changed overlay\n")
        with self.assertRaisesRegex(ValueError, "Overlay patch bytes differ"):
            self._extract(receipt)

    def test_rejects_failed_source_lineage(self) -> None:
        with patch("extract_nuke_test_evidence.preparation.verify_import_lineage", side_effect=ValueError("Rehearsal parent set changed")):
            with self.assertRaisesRegex(ValueError, "Rehearsal parent set changed"):
                extract(self.log_path, self.root, self.prep_path, self.import_path,
                        self.patch_path, None, "./build.sh --target Test")


if __name__ == "__main__":
    unittest.main()
