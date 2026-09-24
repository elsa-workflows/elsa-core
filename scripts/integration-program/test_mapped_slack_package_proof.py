import json
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

SCRIPTS = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPTS))

import run_mapped_slack_package_proof as proof  # noqa: E402


class MappedSlackPackageProofTests(unittest.TestCase):
    def test_impact_selection_receipt_records_the_inventory_graph_pins(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            output = Path(temporary_directory)

            receipt = proof.selector_evidence(output)
            selection = receipt["selection"]

            self.assertEqual("2026-09-23", selection["inventory_snapshot_date"])
            self.assertEqual(64, len(selection["inventory_sha256"]))
            self.assertEqual(proof.PACKAGE_ID, selection["package_ids_to_pack"][0])
            self.assertEqual(
                {"elsa-core", "elsa-extensions", "elsa-studio"},
                set(selection["inventory_source_commits"]),
            )
            self.assertEqual(selection, json.loads(Path(receipt["receipt"]).read_text(encoding="utf-8")))

    def test_pinned_source_guard_rejects_wrong_commit_and_dirty_worktree(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            source = Path(temporary_directory) / "source"
            source.mkdir()
            subprocess.run(["git", "init", "-q"], cwd=source, check=True)
            subprocess.run(["git", "config", "user.name", "Proof Test"], cwd=source, check=True)
            subprocess.run(["git", "config", "user.email", "proof@example.invalid"], cwd=source, check=True)
            source_file = source / "source.txt"
            source_file.write_text("pinned", encoding="utf-8")
            subprocess.run(["git", "add", "source.txt"], cwd=source, check=True)
            subprocess.run(["git", "-c", "commit.gpgsign=false", "commit", "-m", "source"], cwd=source, check=True, capture_output=True)

            commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=source, text=True).strip()
            proof.require_pinned_source(source, commit, "fixture")
            with self.assertRaisesRegex(RuntimeError, "must be clean"):
                proof.require_pinned_source(source, "0" * 40, "fixture")

            source_file.write_text("dirty", encoding="utf-8")
            with self.assertRaisesRegex(RuntimeError, "must be clean"):
                proof.require_pinned_source(source, commit, "fixture")

    def test_package_mode_evaluation_requires_single_elsa_package_reference(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory)
            log = root / "package.log"
            properties = self.properties()
            items = {
                "PackageReference": [{"Identity": "Elsa", "Version": proof.ELSA_VERSION}],
                "ProjectReference": [],
            }
            self.write_evaluation(log, properties, items)

            receipt = proof.verify_evaluation(log, root, package_mode=True)

            self.assertEqual(1, receipt["elsa_package_reference_count"])
            self.assertEqual([], receipt["project_references"])

            items["PackageReference"].append({"Identity": "Elsa", "Version": proof.ELSA_VERSION})
            self.write_evaluation(log, properties, items)
            with self.assertRaisesRegex(RuntimeError, "one Elsa package"):
                proof.verify_evaluation(log, root, package_mode=True)

    def test_project_reference_mode_requires_exact_mapped_core_path(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory)
            core_project = root / "src/modules/Elsa/Elsa.csproj"
            core_project.parent.mkdir(parents=True)
            core_project.write_text("<Project />", encoding="utf-8")
            log = root / "project.log"
            properties = self.properties()
            items = {
                "PackageReference": [],
                "ProjectReference": [{"Identity": "../../../modules/Elsa/Elsa.csproj", "FullPath": str(core_project)}],
            }
            self.write_evaluation(log, properties, items)

            receipt = proof.verify_evaluation(log, root, package_mode=False)

            self.assertEqual([str(core_project)], receipt["project_references"])

            wrong_project = root / "other-core/src/modules/Elsa/Elsa.csproj"
            wrong_project.parent.mkdir(parents=True)
            wrong_project.write_text("<Project />", encoding="utf-8")
            items["ProjectReference"][0]["FullPath"] = str(wrong_project)
            self.write_evaluation(log, properties, items)
            with self.assertRaisesRegex(RuntimeError, "mapped Core project"):
                proof.verify_evaluation(log, root, package_mode=False)

    def test_relative_dotnet_path_remains_runnable_from_a_changed_working_directory(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory)
            executable = root / "tools" / "dotnet"
            executable.parent.mkdir()
            executable.write_text("#!/bin/sh\nprintf 'fake-dotnet-cwd=%s\\n' \"$PWD\"\n", encoding="utf-8")
            executable.chmod(0o755)
            rehearsal = root / "rehearsal"
            rehearsal.mkdir()

            relative_executable = Path(os.path.relpath(executable, Path.cwd()))
            command = proof.dotnet_command(relative_executable, "pack")
            self.assertEqual(executable.resolve(), Path(command[0]))

            log = root / "dotnet.log"
            proof.run(command, cwd=rehearsal, env=os.environ.copy(), log=log)

            self.assertIn(f"fake-dotnet-cwd={rehearsal.resolve()}", log.read_text(encoding="utf-8"))

    @staticmethod
    def properties():
        return {
            "PackageId": proof.PACKAGE_ID,
            "AssemblyName": proof.PACKAGE_ID,
            "RootNamespace": proof.PACKAGE_ID,
            "ElsaVersion": proof.ELSA_VERSION,
            "PackageVersion": proof.PACKAGE_VERSION,
            "IsPackable": "true",
        }

    @staticmethod
    def write_evaluation(path: Path, properties: dict, items: dict) -> None:
        path.write_text(
            "$ dotnet msbuild ...\n" + json.dumps({"Properties": properties, "Items": items}, indent=2) + "\n",
            encoding="utf-8",
        )


if __name__ == "__main__":
    unittest.main()
