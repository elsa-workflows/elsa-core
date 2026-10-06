import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET
import zipfile

import prove_consolidated_packages as proof

ROOT = Path(__file__).resolve().parents[2]
COMMIT = "a" * 40
VERSION = "3.10.0-proof.1.1"


class PackageProofTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)
        self.row = {
            "id": "Elsa.Example", "project": "src/studio/example/Example.csproj",
            "frameworks": ["net8.0"], "assembly_name": "Example", "include_build_output": True,
            "include_symbols": True, "is_tool": False,
            "nupkg": f"Elsa.Example.{VERSION}.nupkg", "snupkg": f"Elsa.Example.{VERSION}.snupkg",
            "framework_properties": {"net8.0": {"assembly_name": "Example", "include_build_output": True}},
        }
        self.manifest = {"version": VERSION, "source_commit": COMMIT, "packages": [self.row],
                         "exclusions": [{"id": "Elsa.Secrets.Models", "project": "retired.csproj"}],
                         "external_package_exceptions": proof.EXTERNAL_PACKAGES, "icon_sha256": hashlib.sha256(b"icon").hexdigest()}

    def nuspec(self, *, dependency=None, version=VERSION, repository=proof.CORE_URL):
        data = ET.Element("metadata")
        for name, value in (("id", self.row["id"]), ("version", version), ("projectUrl", proof.CORE_URL), ("icon", "icon.png")):
            ET.SubElement(data, name).text = value
        ET.SubElement(data, "repository", {"type": "git", "url": repository, "commit": COMMIT})
        dependencies = ET.SubElement(data, "dependencies")
        group = ET.SubElement(dependencies, "group", {"targetFramework": "net8.0"})
        if dependency:
            ET.SubElement(group, "dependency", {"id": dependency[0], "version": dependency[1]})
        return data

    def artifacts(self, data=None):
        metadata = data if data is not None else self.nuspec()
        document = ET.Element("package")
        document.append(metadata)
        for kind, entries in (("nupkg", {"lib/net8.0/Example.dll": b"assembly", "icon.png": b"icon"}),
                              ("snupkg", {"lib/net8.0/Example.pdb": b"symbols"})):
            with zipfile.ZipFile(self.directory / self.row[kind], "w") as archive:
                archive.writestr("Example.nuspec", ET.tostring(document))
                for path, value in entries.items():
                    archive.writestr(path, value)

    def test_complete_inventory_hashes_assets_and_symbols(self):
        self.artifacts()
        result = proof.verify_artifacts(self.directory, self.manifest)
        self.assertEqual(64, len(result["packages"][0]["nupkg_sha256"]))
        self.assertEqual(128, len(result["packages"][0]["nupkg_sha512"]))
        self.assertEqual("lib/net8.0/Example.pdb", result["packages"][0]["assemblies"][0]["pdb"])

    def test_missing_unexpected_and_retired_artifacts_fail(self):
        self.artifacts()
        (self.directory / self.row["snupkg"]).unlink()
        with self.assertRaisesRegex(ValueError, "inventory mismatch"):
            proof.verify_artifacts(self.directory, self.manifest)
        self.artifacts()
        (self.directory / f"Elsa.Secrets.Models.{VERSION}.nupkg").write_bytes(b"retired")
        with self.assertRaisesRegex(ValueError, "unexpected"):
            proof.verify_artifacts(self.directory, self.manifest)

    def test_wrong_identity_version_or_repository_fails(self):
        for metadata in (self.nuspec(version="3.10.0"), self.nuspec(repository="https://github.com/elsa-workflows/elsa-extensions")):
            with self.subTest(metadata=ET.tostring(metadata)):
                self.artifacts(metadata)
                with self.assertRaises(ValueError):
                    proof.verify_artifacts(self.directory, self.manifest)

    def test_dependency_closure_rejects_stale_excluded_and_unknown(self):
        for dependency in (("Elsa.Example", "3.8.4"), ("Elsa.Secrets.Models", VERSION), ("Elsa.Missing", VERSION)):
            with self.subTest(dependency=dependency), self.assertRaises(ValueError):
                proof.verify_metadata(self.nuspec(dependency=dependency), self.row, self.manifest)
        for dependency in (("Elsa.Example", VERSION), ("Elsa.Platform.PackageManifest", "0.0.1-preview.53"), ("Dapper", "2.1.66")):
            proof.verify_metadata(self.nuspec(dependency=dependency), self.row, self.manifest)

    def test_duplicate_zip_entry_and_missing_framework_symbols_fail(self):
        self.artifacts()
        with zipfile.ZipFile(self.directory / self.row["snupkg"], "a") as archive:
            archive.writestr("lib/net9.0/Example.pdb", b"wrong framework")
        with self.assertRaisesRegex(ValueError, "Symbol framework"):
            proof.verify_artifacts(self.directory, self.manifest)
        with zipfile.ZipFile(self.directory / "duplicate.zip", "w") as archive:
            archive.writestr("same", b"one")
            archive.writestr("same", b"two")
        with zipfile.ZipFile(self.directory / "duplicate.zip") as archive, self.assertRaisesRegex(ValueError, "Duplicate"):
            proof.archive_names(archive)

    def inspection(self, relative, *, embedded=None, source=b"source"):
        return {"source_link": {"documents": {"/_/*": f"{proof.RAW_URL}{COMMIT}/*"}},
                "documents": [{"path": f"/_/{relative}", "algorithm": "sha256",
                               "checksum": hashlib.sha256(source).hexdigest(), "embedded_checksum": embedded}]}

    def test_tracked_documents_are_checked_against_exact_blob(self):
        inspection = self.inspection("src/studio/example/Example.cs")
        with patch.object(proof.subprocess, "run", return_value=subprocess.CompletedProcess([], 0, b"source")) as command:
            result = proof.verify_documents(ROOT, self.row, "net8.0", inspection, COMMIT, False)
        self.assertEqual(1, result["tracked_documents"])
        self.assertEqual(0, result["remote_documents"])
        self.assertEqual(f"{COMMIT}:src/studio/example/Example.cs", command.call_args.args[0][-1])
        with patch.object(proof.subprocess, "run", return_value=subprocess.CompletedProcess([], 0, b"wrong")), self.assertRaisesRegex(ValueError, "exact Git blob"):
            proof.verify_documents(ROOT, self.row, "net8.0", inspection, COMMIT, False)

    def test_embedded_sources_do_not_bypass_checksum_or_generated_policy(self):
        tracked = self.inspection("src/studio/example/Example.cs")
        checksum = hashlib.sha256(b"source").hexdigest()
        generated = self.inspection("src/studio/example/obj/Release/net8.0/Example.AssemblyInfo.cs", embedded=checksum)
        inspection = {**tracked, "documents": tracked["documents"] + generated["documents"]}
        def blob(command, **kwargs):
            tracked = command[-1].endswith(":src/studio/example/Example.cs")
            return subprocess.CompletedProcess(command, 0 if tracked else 1, b"source" if tracked else b"")
        with patch.object(proof.subprocess, "run", side_effect=blob):
            result = proof.verify_documents(ROOT, self.row, "net8.0", inspection, COMMIT, False)
            self.assertEqual(1, result["embedded_generated_documents"])
            for path in ("src/studio/example/untracked.cs", "src/studio/other/obj/Release/net8.0/Example.AssemblyInfo.cs"):
                inspection["documents"][1] = self.inspection(path, embedded=checksum)["documents"][0]
                with self.subTest(path=path), self.assertRaises(ValueError):
                    proof.verify_documents(ROOT, self.row, "net8.0", inspection, COMMIT, False)
        tracked["documents"][0]["embedded_checksum"] = "bad"
        with self.assertRaisesRegex(ValueError, "Embedded source checksum"):
            proof.verify_documents(ROOT, self.row, "net8.0", tracked, COMMIT, False)

    def test_unmapped_and_wrong_repository_documents_fail(self):
        inspection = self.inspection("src/studio/example/Example.cs")
        inspection["source_link"]["documents"]["/_/*"] = f"https://raw.githubusercontent.com/elsa-workflows/elsa-studio/{COMMIT}/*"
        with self.assertRaisesRegex(ValueError, "exact Core head"):
            proof.verify_documents(ROOT, self.row, "net8.0", inspection, COMMIT, False)

    def test_proof_version_cannot_be_stable_or_preview(self):
        for version in ("3.10.0", "3.10.0-preview.1", "0.0.0-proof.1.1", "3.10.0-proof.0.1", "3.10.0-proof.1.1-extra"):
            self.assertIsNone(proof.PROOF_VERSION.fullmatch(version))
        self.assertIsNotNone(proof.PROOF_VERSION.fullmatch(VERSION))

    def test_import_guards_are_default_off_and_preserve_explicit_exclusions(self):
        for subtree in ("extensions", "studio"):
            for suffix in ("props", "targets"):
                document = ET.parse(ROOT / f"src/{subtree}/Directory.Build.{suffix}")
                guards = document.findall("./PropertyGroup/IsPackable")
                self.assertEqual(1, len(guards))
                self.assertEqual("'$(ConsolidatedPackageProof)' != 'true'", guards[0].get("Condition"))
                self.assertEqual("false", guards[0].text)
        for path in (ROOT / "src/extensions/secrets").rglob("*.csproj"):
            self.assertEqual("false", ET.parse(path).findtext("./PropertyGroup/IsPackable"))
        for path in (ROOT / "src/modules").glob("Elsa.Connections*/**/*.csproj"):
            if "Credentials" in str(path):
                self.assertEqual("false", ET.parse(path).findtext("./PropertyGroup/IsPackable"))
        self.assertNotIn("ConsolidatedPackageProof", (ROOT / ".github/workflows/packages.yml").read_text())


if __name__ == "__main__":
    unittest.main()
