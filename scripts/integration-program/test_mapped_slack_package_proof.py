import copy
import base64
import hashlib
import json
import os
import subprocess
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest.mock import patch
from xml.etree import ElementTree

SCRIPTS = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPTS))

from package_impact import InventoryGraph
import run_mapped_slack_package_proof as proof  # noqa: E402
import verify_mapped_slack_consumer_provenance as provenance_recheck  # noqa: E402


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

    def test_retained_consumer_provenance_matches_the_exact_local_package(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            fixture = self.consumption_fixture(Path(temporary_directory))

            receipt = proof.verify_local_package_consumption(**fixture["arguments"])

            self.assertTrue(receipt["package_sha512_matches_nupkg"])
            self.assertTrue(receipt["target_framework_assets_match"])
            self.assertTrue(receipt["package_cache_isolated"])
            self.assertEqual("local-proof-feed", receipt["restore_source"])
            self.assertTrue(receipt["metadata_source_matches_local_feed"])
            self.assertTrue(receipt["consumer_assembly_matches_package"])

    def test_retained_consumer_provenance_rejects_wrong_feed_hash_or_version(self):
        mutations = (
            (
                "feed",
                "unexpected feed",
                lambda fixture: self.update_metadata(fixture, source="https://api.nuget.org/v3/index.json"),
            ),
            ("hash", "different .* archive", lambda fixture: self.update_asset_hash(fixture, "wrong-hash")),
            (
                "cached archive",
                "Cached .* archive differs",
                lambda fixture: self.update_cached_archive(fixture, b"different archive"),
            ),
            ("version", "do not select exactly", lambda fixture: self.update_asset_identity(fixture, "3.8.4")),
            ("framework", "compile and runtime assets", self.update_target_framework),
        )
        for _name, message, mutate in mutations:
            with self.subTest(case=_name), tempfile.TemporaryDirectory() as temporary_directory:
                fixture = self.consumption_fixture(Path(temporary_directory))
                mutate(fixture)

                with self.assertRaisesRegex(RuntimeError, message):
                    proof.verify_local_package_consumption(**fixture["arguments"])

    def test_inventory_closure_maps_all_51_paths_without_claiming_execution(self):
        graph = InventoryGraph(proof.INVENTORY_DOCUMENT)
        affected = graph.affected_tests([("elsa-core", "src/modules/Elsa/Elsa.csproj")])
        mapping = []
        with tempfile.TemporaryDirectory() as temporary_directory:
            rehearsal = Path(temporary_directory)
            for repository, source_path in sorted(affected):
                if repository == "elsa-extensions":
                    destination = "test/extensions/" + source_path.removeprefix("test/")
                    mapping.append({"repository": "extensions", "source": source_path, "destination": destination})
                else:
                    destination = source_path
                destination_path = rehearsal / destination
                destination_path.parent.mkdir(parents=True, exist_ok=True)
                destination_path.touch()

            imported = {
                "mapping": mapping,
                "sourceCommits": {
                    "core": proof.CORE_SHA,
                    "extensions": proof.EXTENSIONS_SHA,
                    "studio": proof.STUDIO_SHA,
                },
            }
            selection = {"affected_test_projects": [f"{repo}:{path}" for repo, path in sorted(affected)]}

            receipt = proof.map_impact_selection(selection, rehearsal, imported)

        self.assertEqual("path-and-framework-plan-only", receipt["status"])
        self.assertEqual(51, receipt["selected_project_count"])
        self.assertFalse(receipt["source_compatibility_verified"])
        self.assertFalse(receipt["test_execution_performed"])
        self.assertTrue(all(row["mapped_project_exists"] for row in receipt["selected_projects"]))
        self.assertEqual(
            {row["inventory_project"] for row in receipt["selected_projects"]},
            set(selection["affected_test_projects"]),
        )

    def test_declared_test_projects_map_by_source_and_run_every_framework(self):
        unit = copy.deepcopy(proof.RELEASE_UNIT)
        unit["source"]["test_projects"] = [
            {"project_path": "test/one/One.Tests.csproj", "target_frameworks": ["net8.0", "net10.0"]},
            {"project_path": "test/two/Two.Tests.csproj", "target_frameworks": ["net9.0"]},
        ]
        unit["mapped"]["test_projects"] = [
            {"source_project_path": "test/two/Two.Tests.csproj", "project_path": "test/extensions/two/Two.Tests.csproj"},
            {"source_project_path": "test/one/One.Tests.csproj", "project_path": "test/extensions/one/One.Tests.csproj"},
        ]
        mapping = [
            {"repository": "extensions", "source": unit["source"]["project_path"], "destination": unit["mapped"]["project_path"]},
            {"repository": "extensions", "source": "test/one/One.Tests.csproj", "destination": "test/extensions/one/One.Tests.csproj"},
            {"repository": "extensions", "source": "test/two/Two.Tests.csproj", "destination": "test/extensions/two/Two.Tests.csproj"},
        ]
        imported = {"mapping": mapping}

        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory)
            rehearsal = root / "rehearsal"
            for mapped_test in unit["mapped"]["test_projects"]:
                test_path = rehearsal / mapped_test["project_path"]
                test_path.parent.mkdir(parents=True, exist_ok=True)
                test_path.touch()

            commands = []

            def fake_run(command, *, cwd, env, log):
                commands.append(command)
                log.parent.mkdir(parents=True, exist_ok=True)
                log.write_text("fixture command\n", encoding="utf-8")
                if command[1] == "test":
                    results_dir = Path(command[command.index("--results-directory") + 1])
                    results_dir.mkdir(parents=True, exist_ok=True)
                    result = ElementTree.Element("TestRun")
                    results = ElementTree.SubElement(result, "Results")
                    ElementTree.SubElement(results, "UnitTestResult", testName=f"{command[command.index('--framework') + 1]}.Passes", outcome="Passed")
                    summary = ElementTree.SubElement(result, "ResultSummary", outcome="Completed")
                    counters = {name: "0" for name in proof.shared.TRX_COUNTERS}
                    counters.update(total="1", executed="1", passed="1")
                    ElementTree.SubElement(summary, "Counters", counters)
                    ElementTree.ElementTree(result).write(results_dir / "result.trx", encoding="utf-8", xml_declaration=True)

            with patch.object(proof, "RELEASE_UNIT", unit), patch.object(proof, "run", side_effect=fake_run):
                verified_mapping = proof.verify_release_unit_mapping(imported)
                receipt = proof.verify_upstream_test_baseline(
                    root / "output", rehearsal, {}, root / "cache", root / "NuGet.Config", Path("/dotnet"), imported
                )

        self.assertEqual("test/extensions/one/One.Tests.csproj", verified_mapping["test/one/One.Tests.csproj"])
        self.assertEqual("test/extensions/two/Two.Tests.csproj", verified_mapping["test/two/Two.Tests.csproj"])
        self.assertEqual(2, receipt["manifest_declared_test_project_count"])
        self.assertEqual(3, receipt["manifest_declared_framework_count"])
        self.assertTrue(receipt["all_declared_project_frameworks_ran"])
        self.assertEqual(
            {("test/one/One.Tests.csproj", "net8.0"), ("test/one/One.Tests.csproj", "net10.0"), ("test/two/Two.Tests.csproj", "net9.0")},
            {(row["source_project_path"], row["framework"]) for row in receipt["runs"]},
        )
        test_commands = [command for command in commands if command[1] == "test"]
        restore_commands = [command for command in commands if command[1] == "restore"]
        self.assertEqual(3, len(test_commands))
        self.assertEqual(2, len(restore_commands))
        self.assertEqual(
            {"test/extensions/one/One.Tests.csproj", "test/extensions/two/Two.Tests.csproj"},
            {Path(command[2]).relative_to(rehearsal).as_posix() for command in test_commands},
        )

    def test_retained_provenance_recheck_rejects_dotdot_output_alias(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory).resolve()
            proof_root = root / "proof"
            proof_root.mkdir()
            evidence = proof_root / "evidence.json"
            evidence.write_text("{}", encoding="utf-8")
            output = proof_root / ".." / "proof" / "recheck.json"

            with patch.object(sys, "argv", [
                "verify_mapped_slack_consumer_provenance.py",
                "--evidence", str(evidence),
                "--output", str(output),
            ]):
                with self.assertRaisesRegex(ValueError, "outside the retained proof directory"):
                    provenance_recheck.main()

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
    def consumption_fixture(root: Path) -> dict:
        framework = "net10.0"
        package_version = proof.PACKAGE_VERSION
        package = root / f"{proof.PACKAGE_ID}.{package_version}.nupkg"
        assembly = b"pinned-package-assembly"
        with zipfile.ZipFile(package, "w") as archive:
            archive.writestr(f"lib/{framework}/{proof.PACKAGE_ID}.dll", assembly)

        feed = root / "local-feed"
        feed.mkdir()
        package_cache = root / "isolated-cache"
        package_cache.mkdir()
        consumer = root / "consumer"
        (consumer / "obj").mkdir(parents=True)
        output = consumer / "bin/Debug/net10.0"
        output.mkdir(parents=True)
        (output / f"{proof.PACKAGE_ID}.dll").write_bytes(assembly)

        digest = base64.b64encode(hashlib.sha512(package.read_bytes()).digest()).decode("ascii")
        relative_path = Path(proof.PACKAGE_ID.casefold()) / package_version.casefold()
        cached_package = package_cache / relative_path
        cached_package.mkdir(parents=True)
        (cached_package / ".nupkg.metadata").write_text(
            json.dumps({"source": str(feed.resolve()), "contentHash": digest}),
            encoding="utf-8",
        )
        (cached_package / f"{proof.PACKAGE_ID.casefold()}.{package_version.casefold()}.nupkg.sha512").write_text(
            digest,
            encoding="utf-8",
        )
        assets_path = consumer / "obj/project.assets.json"
        assets_path.write_text(json.dumps({
            "targets": {
                framework: {
                    f"{proof.PACKAGE_ID}/{package_version}": {
                        "type": "package",
                        "compile": {f"lib/{framework}/{proof.PACKAGE_ID}.dll": {"related": ".xml"}},
                        "runtime": {f"lib/{framework}/{proof.PACKAGE_ID}.dll": {"related": ".xml"}},
                    },
                },
            },
            "libraries": {
                f"{proof.PACKAGE_ID}/{package_version}": {
                    "type": "package",
                    "path": relative_path.as_posix(),
                    "sha512": digest,
                },
            },
            "packageFolders": {str(package_cache.resolve()) + os.sep: {}},
        }), encoding="utf-8")
        cached_archive = cached_package / f"{proof.PACKAGE_ID.casefold()}.{package_version.casefold()}.nupkg"
        cached_archive.write_bytes(package.read_bytes())

        return {
            "arguments": {
                "consumer_dir": consumer,
                "framework": framework,
                "package": package,
                "package_cache": package_cache,
                "local_feed": feed,
            },
            "assets_path": assets_path,
            "metadata_path": cached_package / ".nupkg.metadata",
        }

    @staticmethod
    def update_metadata(fixture: dict, *, source: str) -> None:
        metadata_path = fixture["metadata_path"]
        metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
        metadata["source"] = source
        metadata_path.write_text(json.dumps(metadata), encoding="utf-8")

    @staticmethod
    def update_asset_hash(fixture: dict, value: str) -> None:
        assets = json.loads(fixture["assets_path"].read_text(encoding="utf-8"))
        library = next(iter(assets["libraries"].values()))
        library["sha512"] = value
        fixture["assets_path"].write_text(json.dumps(assets), encoding="utf-8")

    @staticmethod
    def update_asset_identity(fixture: dict, version: str) -> None:
        assets = json.loads(fixture["assets_path"].read_text(encoding="utf-8"))
        library = assets["libraries"].pop(next(iter(assets["libraries"])))
        library["path"] = f"{proof.PACKAGE_ID.casefold()}/{version.casefold()}"
        assets["libraries"][f"{proof.PACKAGE_ID}/{version}"] = library
        fixture["assets_path"].write_text(json.dumps(assets), encoding="utf-8")

    @staticmethod
    def update_cached_archive(fixture: dict, content: bytes) -> None:
        package_version = proof.PACKAGE_VERSION.casefold()
        cached_archive = (
            fixture["arguments"]["package_cache"]
            / proof.PACKAGE_ID.casefold()
            / package_version
            / f"{proof.PACKAGE_ID.casefold()}.{package_version}.nupkg"
        )
        cached_archive.write_bytes(content)

    @staticmethod
    def update_target_framework(fixture: dict) -> None:
        assets = json.loads(fixture["assets_path"].read_text(encoding="utf-8"))
        target_package = next(iter(assets["targets"]["net10.0"].values()))
        target_package["runtime"] = {"lib/net9.0/Elsa.Slack.dll": {}}
        fixture["assets_path"].write_text(json.dumps(assets), encoding="utf-8")

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
