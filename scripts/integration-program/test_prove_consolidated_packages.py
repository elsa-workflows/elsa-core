import hashlib
import base64
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
            "framework_properties": {"net8.0": {"assembly_name": "Example", "include_build_output": True, "assets_file": "unused.json", "manifest_required": False, "manifest_path": ""}},
        }
        self.row["expected_dependency_groups"] = [{"framework": "net8.0", "dependencies": []}]
        self.row["expected_symbol_dependency_groups"] = [{"framework": "net8.0", "dependencies": []}]
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
                data = self.nuspec(dependency=dependency)
                self.row["expected_dependency_groups"] = proof.dependency_groups(data)
                proof.verify_metadata(data, self.row, self.manifest)
        for dependency in (("Elsa.Example", VERSION), ("Elsa.Platform.PackageManifest", "0.0.1-preview.53"), ("Dapper", "2.1.66")):
            data = self.nuspec(dependency=dependency)
            self.row["expected_dependency_groups"] = proof.dependency_groups(data)
            proof.verify_metadata(data, self.row, self.manifest)

    def test_missing_dependency_group_dependency_and_extra_assembly_fail(self):
        no_groups = self.nuspec()
        no_groups.remove(no_groups.find("dependencies"))
        with self.assertRaisesRegex(ValueError, "SDK dependency metadata/archive mismatch"):
            proof.verify_metadata(no_groups, self.row, self.manifest)
        wrong_group = self.nuspec()
        wrong_group.find("dependencies/group").set("targetFramework", "net7.0")
        with self.assertRaisesRegex(ValueError, "SDK dependency metadata/archive mismatch"):
            proof.verify_metadata(wrong_group, self.row, self.manifest)
        data = self.nuspec(dependency=("Dapper", "2.1.66"))
        self.row["expected_dependency_groups"] = proof.dependency_groups(data)
        with self.assertRaisesRegex(ValueError, "SDK dependency metadata/archive mismatch"):
            proof.verify_metadata(self.nuspec(), self.row, self.manifest)
        data = self.nuspec(dependency=("Dapper", "2.1.66"))
        proof.verify_metadata(data, self.row, self.manifest)
        self.artifacts(data)
        with zipfile.ZipFile(self.directory / self.row["nupkg"], "a") as archive:
            archive.writestr("lib/net7.0/Example.dll", b"unexpected framework")
        with self.assertRaisesRegex(ValueError, "Assembly framework coverage"):
            proof.verify_artifacts(self.directory, self.manifest)

    def test_sdk_dependency_comparison_includes_asset_semantics(self):
        data = self.nuspec(dependency=("Dapper", "2.1.66"))
        data.find("dependencies/group/dependency").set("exclude", "Build,Analyzers")
        self.row["expected_dependency_groups"] = proof.dependency_groups(data)
        proof.verify_metadata(data, self.row, self.manifest)
        data.find("dependencies/group/dependency").set("exclude", "Compile")
        with self.assertRaisesRegex(ValueError, "SDK dependency metadata/archive mismatch"):
            proof.verify_metadata(data, self.row, self.manifest)

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

    def staged_nuspecs(self, destination):
        document = ET.Element("package")
        document.append(self.nuspec())
        for suffix in (".nuspec", ".symbols.nuspec"):
            (destination / (self.row["nupkg"].removesuffix(".nupkg") + suffix)).write_bytes(ET.tostring(document))

    def test_sdk_metadata_stage_initializes_style_and_forbids_packages(self):
        def sdk(command, cwd, **kwargs):
            self.assertIn("-target:_GetRestoreProjectStyle;GenerateNuspec", command)
            self.assertIn("-p:NoBuild=true", command)
            self.assertIn("-p:ContinuePackingAfterGeneratingNuspec=false", command)
            self.assertIn(f"-p:PackageVersion={VERSION}", command)
            self.staged_nuspecs(Path(kwargs["log"]).parent)
            return ""
        destination = self.directory / "metadata"
        with patch.object(proof, "run", side_effect=sdk):
            proof.stage_nuspecs(ROOT, self.row, VERSION, destination)
        self.assertIn("sdk_symbol_nuspec_sha256", self.row)
        self.assertEqual(self.row["expected_dependency_groups"], self.row["expected_symbol_dependency_groups"])
        (destination / self.row["snupkg"]).write_bytes(b"unexpected package")
        with self.assertRaisesRegex(ValueError, "produced package output"):
            proof.read_staged_nuspecs(destination, self.row)

    def test_sdk_metadata_stage_rejects_missing_or_unexpected_pair(self):
        self.staged_nuspecs(self.directory)
        symbol = self.directory / (self.row["nupkg"].removesuffix(".nupkg") + ".symbols.nuspec")
        symbol.unlink()
        with self.assertRaisesRegex(ValueError, "nuspec pair mismatch"):
            proof.read_staged_nuspecs(self.directory, self.row)
        self.staged_nuspecs(self.directory)
        (self.directory / "other.nuspec").write_bytes(b"unexpected")
        with self.assertRaisesRegex(ValueError, "nuspec pair mismatch"):
            proof.read_staged_nuspecs(self.directory, self.row)

    def test_built_browser_assets_must_be_packaged_with_exact_bytes(self):
        self.artifacts()
        asset = {"id": self.row["id"], "package_path": "staticwebassets/example.js", "sha256": hashlib.sha256(b"built").hexdigest()}
        with zipfile.ZipFile(self.directory / self.row["nupkg"]) as archive, self.assertRaisesRegex(ValueError, "Missing packaged browser"):
            proof.verify_browser_assets(archive, self.row, [asset])
        with zipfile.ZipFile(self.directory / self.row["nupkg"], "a") as archive:
            archive.writestr(asset["package_path"], b"stale")
        with zipfile.ZipFile(self.directory / self.row["nupkg"]) as archive, self.assertRaisesRegex(ValueError, "differs from built output"):
            proof.verify_browser_assets(archive, self.row, [asset])

    def test_generated_package_manifest_must_match_proof_id_version_and_frameworks(self):
        self.artifacts()
        self.row["framework_properties"]["net8.0"].update(manifest_required=True, manifest_path="elsa-package.json")
        with zipfile.ZipFile(self.directory / self.row["nupkg"]) as archive, self.assertRaisesRegex(ValueError, "Missing generated"):
            proof.verify_package_manifest(archive, self.row, VERSION)
        for version, frameworks in (("1.0.0", ["net8.0"]), (VERSION, ["net7.0"])):
            self.artifacts()
            data = {"package": {"id": self.row["id"], "version": version},
                    "extensions": {"targetFrameworks": frameworks, "repositoryUrl": proof.CORE_URL}}
            with zipfile.ZipFile(self.directory / self.row["nupkg"], "a") as archive:
                archive.writestr("elsa-package.json", __import__("json").dumps(data))
            with zipfile.ZipFile(self.directory / self.row["nupkg"]) as archive, self.assertRaises(ValueError):
                proof.verify_package_manifest(archive, self.row, VERSION)

    def inspection(self, relative, *, embedded=None, source=b"source"):
        return {"source_link": {"documents": {"/_/*": f"{proof.RAW_URL}{COMMIT}/*"}},
                "documents": [{"path": f"/_/{relative}", "algorithm": "sha256",
                               "checksum": hashlib.sha256(source).hexdigest(), "embedded_checksum": embedded}]}

    def compiler_fixture(self):
        sdk = self.directory / "sdk/10.0.300"
        compiler = sdk / "Roslyn/bincore/csc.dll"
        compiler.parent.mkdir(parents=True, exist_ok=True)
        compiler.write_bytes(b"compiler")
        self.assets_fixture({"libraries": {}, "packageFolders": {str(self.directory / "nuget"): {}}})
        evidence = {"sdk_version": sdk.name, "sdk_root": str(sdk),
                    "compiler_sha256": hashlib.sha256(b"compiler").hexdigest(),
                    "frameworks": [{"Identity": "Microsoft.NETCore.App", "TargetingPackName": "Microsoft.NETCore.App.Ref",
                                    "TargetingPackVersion": "8.0.27", "TargetingPackPath": str(self.directory / "packs/Microsoft.NETCore.App.Ref/8.0.27")}],
                    "tools": {}, "compile_inputs": []}
        self.row["framework_properties"]["net8.0"]["compiler_evidence"] = evidence
        return evidence

    def assets_fixture(self, assets):
        data = __import__("json").dumps(assets).encode()
        path = self.directory / "project.assets.json"
        path.write_bytes(data)
        self.row["restore_assets"] = [{"framework": "net8.0", "path": str(path), "sha256": hashlib.sha256(data).hexdigest()}]

    def refit_fixture(self):
        evidence = self.compiler_fixture()
        folder = self.directory / "nuget/refit/9.0.2"
        folder.mkdir(parents=True, exist_ok=True)
        entry = "analyzers/dotnet/cs/InterfaceStubGeneratorV2.dll"
        tool = folder / entry
        tool.parent.mkdir(parents=True, exist_ok=True)
        tool.write_bytes(b"generator")
        archive = folder / "refit.9.0.2.nupkg"
        with zipfile.ZipFile(archive, "w") as contents:
            contents.writestr(entry, b"generator")
        assets = {"libraries": {"Refit/9.0.2": {"type": "package", "path": "refit/9.0.2",
                    "sha512": base64.b64encode(hashlib.sha512(archive.read_bytes()).digest()).decode()}},
                  "packageFolders": {str(self.directory / "nuget"): {}}}
        self.assets_fixture(assets)
        evidence["tools"]["refit"] = proof.package_tool(assets, "Refit", "9.0.2", entry)
        return archive, tool

    def framework_logging_fixture(self):
        evidence = self.compiler_fixture()
        pack = {"Identity": "Microsoft.AspNetCore.App", "TargetingPackName": "Microsoft.AspNetCore.App.Ref",
                "TargetingPackVersion": "8.0.27", "TargetingPackPath": str(self.directory / "packs/Microsoft.AspNetCore.App.Ref/8.0.27")}
        tool = Path(pack["TargetingPackPath"]) / "analyzers/dotnet/roslyn4.4/cs/Microsoft.Extensions.Logging.Generators.dll"
        tool.parent.mkdir(parents=True)
        tool.write_bytes(b"framework generator")
        return {"Properties": {"MSBuildToolsPath": evidence["sdk_root"], "NETCoreSdkVersion": evidence["sdk_version"],
                               "NetCoreRoot": str(self.directory), "RuntimeIdentifier": ""},
                "Items": {"ResolvedFrameworkReference": [pack], "Analyzer": [{"Identity": str(tool)}], "Compile": []}}

    def test_framework_logging_requires_exact_resolved_pack_identity_and_path(self):
        resolved = self.framework_logging_fixture()
        cache = {("compile_input_paths", "HEAD"): set()}
        for metadata in ({}, {"NuGetPackageId": "Microsoft.AspNetCore.App.Ref", "NuGetPackageVersion": "8.0.27"}):
            resolved["Items"]["Analyzer"][0].update(metadata)
            evidence = proof.capture_compiler_evidence(self.directory, self.row, "net8.0", resolved, cache)
            self.row["framework_properties"]["net8.0"]["compiler_evidence"] = evidence
            self.assertEqual("Microsoft.AspNetCore.App.Ref", evidence["tools"]["logging"]["package_id"])
            proof.verify_generator_identity(self.directory, self.row, "net8.0", "logging", {})
        analyzer = resolved["Items"]["Analyzer"][0]
        for changes in ({"NuGetPackageId": "Wrong.App.Ref"}, {"NuGetPackageVersion": "8.0.99"},
                        {"NuGetPackageId": "Microsoft.Extensions.Logging.Abstractions"},
                        {"Identity": str(self.directory / "outside/Microsoft.Extensions.Logging.Generators.dll")},
                        {"Identity": str(Path(resolved["Items"]["ResolvedFrameworkReference"][0]["TargetingPackPath"]) / "ref/Microsoft.Extensions.Logging.Generators.dll")}):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                proof.capture_compiler_evidence(self.directory, self.row, "net8.0",
                    {**resolved, "Items": {**resolved["Items"], "Analyzer": [{**analyzer, **changes}]}}, cache)
        pack = resolved["Items"]["ResolvedFrameworkReference"][0]
        for changes in ({"TargetingPackName": "Microsoft.NETCore.App.Ref"}, {"TargetingPackVersion": "8.0.99"}):
            with self.subTest(pack=changes), self.assertRaisesRegex(ValueError, "differs from resolved targeting pack"):
                proof.capture_compiler_evidence(self.directory, self.row, "net8.0",
                    {**resolved, "Items": {**resolved["Items"], "ResolvedFrameworkReference": [{**pack, **changes}]}}, cache)

    def test_failed_generator_capture_retains_resolved_msbuild_response(self):
        self.compiler_fixture()
        output = self.directory / "evidence"
        output.mkdir()
        resolved = {"Properties": {"AssemblyName": "Example", "PackageVersion": VERSION,
                    "GenerateElsaPackageManifest": "false", "ElsaPackageManifestIncludeInPackage": "false",
                    "ElsaPackageManifestPackagePath": "", "ProjectAssetsFile": str(self.directory / "project.assets.json")}}
        with patch.object(proof, "stage_nuspecs", side_effect=lambda root, row, version, destination, **kwargs: destination.mkdir()), \
                patch.object(proof, "evaluate", return_value=resolved), \
                patch.object(proof, "capture_compiler_evidence", side_effect=ValueError("rejected generator")), \
                self.assertRaisesRegex(ValueError, "rejected generator"):
            proof.stage_sdk_metadata(self.directory, self.manifest, output, self.directory / "helper.dll")
        retained = output / "sdk-metadata/000-Elsa.Example/resolved.net8.0.json"
        self.assertEqual(resolved, __import__("json").loads(retained.read_text()))
        self.assertTrue(retained.with_name("restore.net8.0.assets.json").is_file())

    def test_physical_generator_uses_exact_framework_and_runtime_target(self):
        resolved = self.framework_logging_fixture()
        resolved["Items"]["Analyzer"] = []
        self.row.update(id="Elsa.Api.Common", project="src/common/Elsa.Api.Common/Elsa.Api.Common.csproj")
        identifier = "FastEndpoints.Swagger"
        entry = proof.PHYSICAL_GENERATORS["swagger"][1]
        libraries = {}
        for version in ("7.1.1", "8.2.0"):
            folder = self.directory / "nuget" / identifier.lower() / version
            tool = folder / entry
            tool.parent.mkdir(parents=True)
            tool.write_bytes(version.encode())
            archive = folder / f"{identifier.lower()}.{version}.nupkg"
            with zipfile.ZipFile(archive, "w") as contents:
                contents.writestr(entry, version.encode())
            libraries[f"{identifier}/{version}"] = {"type": "package", "sha512": base64.b64encode(hashlib.sha512(archive.read_bytes()).digest()).decode()}
        assets = {"libraries": libraries, "packageFolders": {str(self.directory / "nuget"): {}},
                  "targets": {"net8.0": {f"{identifier}/7.1.1": {"type": "package"}},
                              "net10.0": {f"{identifier}/8.2.0": {"type": "package"}},
                              "net8.0/linux-x64": {f"{identifier}/8.2.0": {"type": "package"}}}}
        self.assets_fixture(assets)
        self.row["restore_assets"].append({**self.row["restore_assets"][0], "framework": "net10.0"})
        cache = {("compile_input_paths", "HEAD"): set()}
        for framework, rid, version in (("net8.0", "", "7.1.1"), ("net10.0", "", "8.2.0"), ("net8.0", "linux-x64", "8.2.0")):
            resolved["Properties"]["RuntimeIdentifier"] = rid
            result = proof.capture_compiler_evidence(self.directory, self.row, framework, resolved, cache)
            self.assertEqual(version, result["tools"]["swagger"]["package_version"])
        resolved["Properties"]["RuntimeIdentifier"] = "unrestored-rid"
        with self.assertRaisesRegex(ValueError, "Missing exact restored generator target"):
            proof.capture_compiler_evidence(self.directory, self.row, "net8.0", resolved, cache)
        resolved["Properties"]["RuntimeIdentifier"] = ""
        assets["targets"]["net8.0"][f"{identifier}/8.2.0"] = {"type": "package"}
        self.assets_fixture(assets)
        with self.assertRaisesRegex(ValueError, "ambiguous physical generator"):
            proof.capture_compiler_evidence(self.directory, self.row, "net8.0", resolved, cache)
        assets["targets"]["net8.0"] = {f"{identifier}/7.1.1": {"type": "project"}}
        self.assets_fixture(assets)
        with self.assertRaisesRegex(ValueError, "not a restored package"):
            proof.capture_compiler_evidence(self.directory, self.row, "net8.0", resolved, cache)
        assets["targets"]["net8.0"] = {f"{identifier}/9.9.9": {"type": "package"}}
        self.assets_fixture(assets)
        with self.assertRaisesRegex(ValueError, "not an actual versioned restored dependency"):
            proof.capture_compiler_evidence(self.directory, self.row, "net8.0", resolved, cache)
        del resolved["Properties"]["RuntimeIdentifier"]
        with self.assertRaisesRegex(ValueError, "Missing resolved RuntimeIdentifier"):
            proof.capture_compiler_evidence(self.directory, self.row, "net8.0", resolved, cache)

    def test_staging_failure_keeps_package_bytes_without_acceptance_receipt(self):
        root = self.directory / "checkout"
        packages = root / "packages"
        packages.mkdir(parents=True)
        output = self.directory / "evidence"
        produced = {self.row["nupkg"]: b"package", self.row["snupkg"]: b"symbols"}

        def pack(*args, **kwargs):
            if args[0][0] in ("dotnet", "node"):
                return "10.0.300"
            for name, data in produced.items():
                (packages / name).write_bytes(data)

        with patch.object(proof, "__file__", str(root / "scripts/integration-program/prove.py")), \
                patch("sys.argv", ["prove", "--version", VERSION, "--output", str(output)]), \
                patch.object(proof, "clean_head", return_value=COMMIT), \
                patch.object(proof, "inventory", return_value=self.manifest), \
                patch.object(proof, "build_clientlibs", return_value={"assets": []}), \
                patch.object(proof, "run", side_effect=pack), \
                patch.object(proof, "build_symbol_verifier", return_value=self.directory / "helper.dll"), \
                patch.object(proof, "stage_sdk_metadata", side_effect=ValueError("staging failed")), \
                patch.object(proof, "verify_artifacts") as verify, \
                self.assertRaisesRegex(ValueError, "staging failed"):
            proof.main()
        for name, data in produced.items():
            self.assertEqual(data, (output / "artifacts" / name).read_bytes())
        self.assertFalse((output / "receipt.json").exists())
        verify.assert_not_called()

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
        self.compiler_fixture()
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

    def test_generated_families_are_owned_and_narrow(self):
        prefix = "src/studio/example/obj/Release/net8.0/"
        name = "InterfaceStubGeneratorV2/Refit.Generator.InterfaceStubGeneratorV2/Generated.g.cs"
        self.assertEqual("refit", proof.generated_family(self.row, "net8.0", prefix + name))
        self.assertEqual("refit", proof.generated_family(self.row, "net8.0", prefix + name.replace("Generated", "PreserveAttribute")))
        for name in ("EmbeddedAttribute.cs", "ValidatableTypeAttribute.cs"):
            self.assertEqual("razor", proof.generated_family(self.row, "net10.0", prefix.replace("net8.0", "net10.0") + name))
            self.assertIsNone(proof.generated_family(self.row, "net8.0", prefix + name))
        name = "InterfaceStubGeneratorV2/Refit.Generator.InterfaceStubGeneratorV2/Generated.g.cs"
        for relative in (prefix + "UnknownGenerator/Generated.g.cs", prefix + name.replace("Generated.g.cs", "Arbitrary.cs"),
                         prefix.replace("example/", "other/") + name, prefix.replace("net8.0", "net9.0") + name,
                         prefix + "./" + name, prefix + name.replace("/", "//", 1), prefix + "../" + name):
            with self.subTest(relative=relative):
                self.assertIsNone(proof.generated_family(self.row, "net8.0", relative))

    def test_generated_document_requires_embedding_and_actual_tool_identity(self):
        self.refit_fixture()
        checksum = hashlib.sha256(b"source").hexdigest()
        tracked = self.inspection("src/studio/example/Example.cs")
        generated = self.inspection("src/studio/example/obj/Release/net8.0/InterfaceStubGeneratorV2/Refit.Generator.InterfaceStubGeneratorV2/Generated.g.cs", embedded=checksum)
        inspection = {**tracked, "documents": tracked["documents"] + generated["documents"]}
        def blob(command, **kwargs):
            tracked = command[-1].endswith(":src/studio/example/Example.cs")
            return subprocess.CompletedProcess(command, 0 if tracked else 1, b"source" if tracked else b"")
        with patch.object(proof.subprocess, "run", side_effect=blob):
            result = proof.verify_documents(ROOT, self.row, "net8.0", inspection, COMMIT, False)
            self.assertEqual("Refit", result["documents"][1]["generator_identity"]["package_id"])
            self.assertFalse(result["documents"][1]["remote_fetched"])
            inspection["documents"][1]["embedded_checksum"] = None
            with self.assertRaisesRegex(ValueError, "audited generated-source policy"):
                proof.verify_documents(ROOT, self.row, "net8.0", inspection, COMMIT, False)

    def test_generator_identity_rejects_changed_restore_archive_tool_or_version(self):
        for changed in ("assets", "archive", "tool", "version"):
            with self.subTest(changed=changed):
                archive, tool = self.refit_fixture()
                if changed == "assets":
                    (self.directory / "project.assets.json").write_bytes(b"changed")
                elif changed == "archive":
                    archive.write_bytes(b"changed")
                elif changed == "tool":
                    tool.write_bytes(b"changed")
                else:
                    self.row["framework_properties"]["net8.0"]["compiler_evidence"]["tools"]["refit"]["package_version"] = "0.0.0"
                with self.assertRaises(ValueError):
                    proof.verify_generator_identity(self.directory, self.row, "net8.0", "refit", {})

    def test_signed_archive_requires_sdk_integrity_and_content_hash(self):
        archive, _ = self.refit_fixture()
        with zipfile.ZipFile(archive, "a") as contents:
            contents.writestr(".signature.p7s", b"signature fixture")
        assets = proof.restored_assets(self.directory, self.row, "net8.0")
        declared = assets["libraries"]["Refit/9.0.2"]["sha512"]
        result = {"signed": True, "content_hash": declared, "archive_sha256": hashlib.sha256(archive.read_bytes()).hexdigest()}
        inspector = self.directory / "inspector.dll"
        with patch.object(proof, "run", return_value=__import__("json").dumps(result)) as command:
            _, record = proof.restored_archive(assets, "Refit", "9.0.2", cache={"archive_inspector": inspector})
            self.assertEqual(declared, record["nuget_content_hash"])
            self.assertIn("--inspect-archive", command.call_args.args[0])
        for changed in ({**result, "signed": False}, {**result, "content_hash": "wrong"},
                        {**result, "archive_sha256": "wrong"}):
            with self.subTest(result=changed), patch.object(proof, "run", return_value=__import__("json").dumps(changed)):
                with self.assertRaises(ValueError):
                    proof.restored_archive(assets, "Refit", "9.0.2", cache={"archive_inspector": inspector})
        with patch.object(proof, "run", side_effect=ValueError("NU3008")), self.assertRaisesRegex(ValueError, "NU3008"):
            proof.restored_archive(assets, "Refit", "9.0.2", cache={"archive_inspector": inspector})
        with self.assertRaisesRegex(ValueError, "requires SDK NuGet"):
            proof.restored_archive(assets, "Refit", "9.0.2")

    def test_sdk_generator_rejects_changed_compiler(self):
        evidence = self.compiler_fixture()
        proof.verify_generator_identity(self.directory, self.row, "net8.0", "sdk", {})
        (Path(evidence["sdk_root"]) / "Roslyn/bincore/csc.dll").write_bytes(b"changed")
        with self.assertRaisesRegex(ValueError, "Pinned SDK compiler"):
            proof.verify_generator_identity(self.directory, self.row, "net8.0", "sdk", {})

    def test_empty_pdb_is_only_allowed_for_verified_interface_only_dropins(self):
        inspection = {"source_link": {"documents": {"/_/*": f"{proof.RAW_URL}{COMMIT}/*"}}, "documents": [],
                      "executable_method_bodies": 0, "nonabstract_methods_without_body": 0, "native_or_external_methods": 0, "nonmodule_types": 0}
        with self.assertRaisesRegex(ValueError, "no source documents"):
            proof.verify_documents(ROOT, self.row, "net8.0", inspection, COMMIT, True)
        self.row.update(id="Elsa.DropIns.Core", project="src/extensions/dropins/Elsa.DropIns.Core/Elsa.DropIns.Core.csproj")
        result = proof.verify_documents(ROOT, self.row, "net8.0", inspection, COMMIT, True)
        self.assertEqual("not_applicable_interface_only", result["document_coverage"])
        self.assertEqual(0, result["tracked_documents"])
        self.assertEqual(0, result["remote_documents"])
        for key in ("executable_method_bodies", "nonabstract_methods_without_body", "native_or_external_methods"):
            for count in (1, None, False):
                with self.subTest(key=key, count=count), self.assertRaisesRegex(ValueError, "Interface-only source coverage"):
                    proof.verify_documents(ROOT, self.row, "net8.0", {**inspection, key: count}, COMMIT, True)
        inspection["source_link"]["documents"]["/_/*"] = f"{proof.RAW_URL}{'b' * 40}/*"
        with self.assertRaisesRegex(ValueError, "exact Core head"):
            proof.verify_documents(ROOT, self.row, "net8.0", inspection, COMMIT, False)

    def test_metadata_bundle_requires_exact_sdk_documents_and_no_implemented_methods(self):
        self.refit_fixture()
        self.row.update(id="Elsa.Studio", project="src/studio/bundles/Elsa.Studio/Elsa.Studio.csproj")
        checksum = hashlib.sha256(b"source").hexdigest()
        inspection = {"source_link": {"documents": {"/_/*": f"{proof.RAW_URL}{COMMIT}/*"}},
                      "documents": [self.inspection(path, embedded=checksum)["documents"][0]
                                    for path in sorted(proof.sdk_document_paths(self.row, "net8.0"))],
                      "executable_method_bodies": 0, "nonabstract_methods_without_body": 0, "native_or_external_methods": 0, "nonmodule_types": 0}
        with patch.object(proof.subprocess, "run", return_value=subprocess.CompletedProcess([], 1, b"")):
            result = proof.verify_documents(ROOT, self.row, "net8.0", inspection, COMMIT, True)
            self.assertEqual("not_applicable_authored_code_metadata_bundle", result["document_coverage"])
            self.assertEqual(3, result["embedded_generated_documents"])
            self.assertEqual(0, result["tracked_documents"])
            self.assertEqual(0, result["remote_documents"])
            with self.assertRaisesRegex(ValueError, "No tracked source"):
                proof.verify_documents(ROOT, {**self.row, "id": "Elsa.Other"}, "net8.0", inspection, COMMIT, False)
            wrong_project = {**self.row, "project": "src/studio/other/Elsa.Studio.csproj"}
            with self.assertRaisesRegex(ValueError, "Untracked source"):
                proof.verify_documents(ROOT, wrong_project, "net8.0", inspection, COMMIT, False)
            for key in ("executable_method_bodies", "nonabstract_methods_without_body", "native_or_external_methods"):
                with self.subTest(key=key), self.assertRaisesRegex(ValueError, "Metadata-only Studio bundle"):
                    proof.verify_documents(ROOT, self.row, "net8.0", {**inspection, key: 1}, COMMIT, False)
            with self.assertRaisesRegex(ValueError, "exactly its three"):
                proof.verify_documents(ROOT, self.row, "net8.0", {**inspection, "documents": inspection["documents"][:-1]}, COMMIT, False)
            extra = self.inspection("src/studio/bundles/Elsa.Studio/obj/Release/net8.0/InterfaceStubGeneratorV2/Refit.Generator.InterfaceStubGeneratorV2/Generated.g.cs", embedded=checksum)
            with self.assertRaisesRegex(ValueError, "exactly its three"):
                proof.verify_documents(ROOT, self.row, "net8.0", {**inspection, "documents": inspection["documents"] + extra["documents"]}, COMMIT, False)
            with self.assertRaisesRegex(ValueError, "no declared types"):
                proof.verify_documents(ROOT, self.row, "net8.0", {**inspection, "nonmodule_types": 1}, COMMIT, False)
            for path in ("src/studio/bundles/Elsa.Studio/Constants.cs", "src/common/Shared/LinkedConstants.cs"):
                self.row["framework_properties"]["net8.0"]["compiler_evidence"]["compile_inputs"] = [{"path": path}]
                with self.subTest(path=path), self.assertRaisesRegex(ValueError, "authored or unaudited Compile inputs"):
                    proof.verify_documents(ROOT, self.row, "net8.0", inspection, COMMIT, False)

    def test_byte_cache_keeps_each_document_checksum_check(self):
        inspection = self.inspection("src/studio/example/Example.cs")
        cache = {}
        with patch.object(proof.subprocess, "run", return_value=subprocess.CompletedProcess([], 0, b"source")) as command:
            for _ in range(2):
                proof.verify_documents(ROOT, self.row, "net8.0", inspection, COMMIT, False, cache)
            self.assertEqual(1, command.call_count)
            inspection["documents"][0]["checksum"] = hashlib.sha256(b"different").hexdigest()
            with self.assertRaisesRegex(ValueError, "exact Git blob"):
                proof.verify_documents(ROOT, self.row, "net8.0", inspection, COMMIT, False, cache)

    def external_source_fixture(self, version, entries=None):
        identifier = "elsa.platform.packagemanifest.generator"
        package = self.directory / "packages" / identifier / version
        package.mkdir(parents=True, exist_ok=True)
        archive = package / f"{identifier}.{version}.nupkg"
        with zipfile.ZipFile(archive, "w") as contents:
            for entry in sorted(entries if entries is not None else proof.GENERATOR_SOURCE_ENTRIES[version]):
                contents.writestr(entry, b"external")
        pinned = hashlib.sha256(archive.read_bytes()).hexdigest()
        assets = {"libraries": {f"Elsa.Platform.PackageManifest.Generator/{version}": {"type": "package"}},
                  "packageFolders": {str(self.directory / "packages"): {}}}
        encoded = __import__("json").dumps(assets).encode()
        (self.directory / "project.assets.json").write_bytes(encoded)
        self.row["restore_assets"] = [{"framework": "net8.0", "path": "project.assets.json", "sha256": hashlib.sha256(encoded).hexdigest()}]
        checksum = hashlib.sha256(b"external").hexdigest()
        document = {"path": f"/_1/{identifier}/{version}/{proof.GENERATOR_HINTS_PREFIX}ManifestSettingAttribute.cs",
                    "algorithm": "sha256", "checksum": checksum, "embedded_checksum": checksum}
        return archive, pinned, document

    def test_external_embedded_sources_require_pinned_archive_and_restore_identity(self):
        for version in proof.GENERATOR_SOURCE_ENTRIES:
            with self.subTest(version=version):
                archive, pinned, document = self.external_source_fixture(version)
                with patch.dict(proof.GENERATOR_SOURCE_PINS, {version: pinned}):
                    result = proof.verify_external_document(self.directory, self.row, "net8.0", document, {})
                    self.assertEqual(pinned, result["archive_sha256"])
                    self.assertFalse(result["remote_fetched"])
                    document["embedded_checksum"] = None
                    with self.assertRaisesRegex(ValueError, "embedded bytes"):
                        proof.verify_external_document(self.directory, self.row, "net8.0", document, {})
                    document["embedded_checksum"] = document["checksum"]
                    archive.write_bytes(b"tampered cache")
                    with self.assertRaisesRegex(ValueError, "official feed pin"):
                        proof.verify_external_document(self.directory, self.row, "net8.0", document, {})
                document["path"] = document["path"].replace(version, "0.0.1-preview.79")
                with self.assertRaisesRegex(ValueError, "Unreviewed external"):
                    proof.verify_external_document(self.directory, self.row, "net8.0", document, {})

    def test_external_archives_require_exact_version_specific_entries(self):
        for version, expected in proof.GENERATOR_SOURCE_ENTRIES.items():
            removed = sorted(expected)[0]
            unexpected = proof.GENERATOR_HINTS_PREFIX + "UnexpectedAttribute.cs"
            for entries in (expected - {removed}, expected | {unexpected}, expected - {removed} | {unexpected}):
                with self.subTest(version=version, entries=sorted(entries)):
                    _, pinned, document = self.external_source_fixture(version, entries)
                    with patch.dict(proof.GENERATOR_SOURCE_PINS, {version: pinned}):
                        with self.assertRaisesRegex(ValueError, "exact audited source entries"):
                            proof.verify_external_document(self.directory, self.row, "net8.0", document, {})

    def test_generator_audit_distinguishes_version_specific_sources(self):
        common = {"ElsaRuntimeKinds.cs", "ManifestExtensionAttribute.cs", "ManifestIgnoreAttribute.cs",
                  "ManifestInfrastructureAttribute.cs", "ManifestRuntimeKindAttribute.cs", "ManifestSettingAttribute.cs",
                  "ManifestUIOptionAttribute.cs", "ManifestUIOptionsProviderAttribute.cs"}
        for version, names in (("0.0.1-preview.50", common),
                               ("0.0.1-preview.53", common | {"ManifestFeatureCategoryAttribute.cs"})):
            with self.subTest(version=version):
                self.assertEqual({proof.GENERATOR_HINTS_PREFIX + name for name in names},
                                 proof.GENERATOR_SOURCE_ENTRIES[version])

    def test_unmapped_and_wrong_repository_documents_fail(self):
        inspection = self.inspection("src/studio/example/Example.cs")
        inspection["source_link"]["documents"]["/_/*"] = f"https://raw.githubusercontent.com/elsa-workflows/elsa-studio/{COMMIT}/*"
        with self.assertRaisesRegex(ValueError, "exact Core head"):
            proof.verify_documents(ROOT, self.row, "net8.0", inspection, COMMIT, False)

    def test_proof_version_cannot_be_stable_or_preview(self):
        for version in ("3.10.0", "3.10.0-preview.1", "0.0.0-proof.1.1", "3.10.0-proof.0.1", "3.10.0-proof.1.1-extra"):
            self.assertIsNone(proof.PROOF_VERSION.fullmatch(version))
        self.assertIsNotNone(proof.PROOF_VERSION.fullmatch(VERSION))

    def test_package_output_allows_only_absent_or_real_empty_directory(self):
        for kind in ("absent", "empty", "nonempty", "file", "symlink", "dangling_symlink"):
            with self.subTest(kind=kind):
                packages = self.directory / kind
                if kind in ("empty", "nonempty"):
                    packages.mkdir()
                    if kind == "nonempty":
                        (packages / "existing.nupkg").write_bytes(b"existing")
                elif kind == "file":
                    packages.write_bytes(b"file")
                elif kind in ("symlink", "dangling_symlink"):
                    target = self.directory / f"{kind}-target"
                    if kind == "symlink":
                        target.mkdir()
                    packages.symlink_to(target, target_is_directory=True)
                if kind in ("absent", "empty"):
                    proof.require_empty_package_output(packages)
                else:
                    with self.assertRaisesRegex(ValueError, "Canonical packages output"):
                        proof.require_empty_package_output(packages)

    def test_import_guards_are_default_off_and_preserve_explicit_exclusions(self):
        for subtree in ("extensions", "studio"):
            for suffix in ("props", "targets"):
                document = ET.parse(ROOT / f"src/{subtree}/Directory.Build.{suffix}")
                guards = document.findall("./PropertyGroup/IsPackable")
                self.assertEqual(1, len(guards))
                self.assertEqual("'$(ConsolidatedPackageProof)' != 'true' and '$(ConsolidatedReleaseCandidate)' != 'true'", guards[0].get("Condition"))
                self.assertEqual("false", guards[0].text)
        for path in (ROOT / "src/extensions/secrets").rglob("*.csproj"):
            self.assertEqual("false", ET.parse(path).findtext("./PropertyGroup/IsPackable"))
        for path in (ROOT / "src/modules").glob("Elsa.Connections*/**/*.csproj"):
            if "Credentials" in str(path):
                self.assertEqual("false", ET.parse(path).findtext("./PropertyGroup/IsPackable"))
        self.assertNotIn("ConsolidatedPackageProof", (ROOT / ".github/workflows/packages.yml").read_text())


if __name__ == "__main__":
    unittest.main()
