import json
import sys
import tempfile
import unittest
from pathlib import Path
from subprocess import TimeoutExpired
from unittest.mock import patch
from xml.etree import ElementTree

SCRIPTS = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPTS))

import prove_consolidated_package_consumers as proof  # noqa: E402


class ConsolidatedPackageConsumerTests(unittest.TestCase):
    def test_source_mapping_routes_only_manifest_ids_to_the_local_feed(self):
        config = ElementTree.fromstring(
            proof.render_nuget_config(
                Path("/tmp/proof-feed"),
                ["Elsa", "Elsa.Slack", "Elsa.WorkflowContexts"],
                {
                    "Elsa.Platform.PackageManifest": "Reviewed external tool package",
                    "Elsa.Platform.PackageManifest.Generator": "Reviewed external generator",
                },
            )
        )
        sources = config.find("packageSources")
        self.assertEqual(
            [(source.attrib["key"], source.attrib["value"]) for source in sources if source.tag == "add"],
            [("local-proof", str(Path("/tmp/proof-feed").resolve())), ("nuget.org", proof.NUGET_ORG)],
        )
        mapping = config.find("packageSourceMapping")
        local_patterns = [node.attrib["pattern"] for node in mapping.find("packageSource[@key='local-proof']")]
        public_patterns = [node.attrib["pattern"] for node in mapping.find("packageSource[@key='nuget.org']")]
        self.assertEqual(local_patterns, ["Elsa", "Elsa.Slack", "Elsa.WorkflowContexts"])
        self.assertIn("Elsa.Platform.PackageManifest", public_patterns)
        self.assertIn("Elsa.Platform.PackageManifest.Generator", public_patterns)
        self.assertEqual(public_patterns[-1], "*")
        self.assertFalse(any(pattern.lower().startswith("elsa.") and pattern.endswith("*") for pattern in local_patterns))

    def test_project_assets_require_package_libraries_and_proof_versions(self):
        assets = self.assets(
            {
                "Elsa/4.0.0-proof.12.1": {"type": "package"},
                "Elsa.Slack/4.0.0-proof.12.1": {"type": "package"},
                "Elsa.Platform.PackageManifest.Generator/0.0.1-preview.53": {"type": "package"},
                "Newtonsoft.Json/13.0.3": {"type": "package"},
            }
        )

        restored = proof.validate_project_assets(
            assets,
            ["net8.0", "net9.0", "net10.0"],
            {"elsa", "elsa.slack", "elsa.workflowcontexts"},
            "4.0.0-proof.12.1",
            {"elsa.platform.packagemanifest", "elsa.platform.packagemanifest.generator"},
        )

        self.assertEqual(
            restored["internal"],
            [
                {"id": "Elsa", "version": "4.0.0-proof.12.1"},
                {"id": "Elsa.Slack", "version": "4.0.0-proof.12.1"},
            ],
        )
        self.assertEqual(
            restored["external"],
            [{"id": "Elsa.Platform.PackageManifest.Generator", "version": "0.0.1-preview.53"}],
        )

    def test_project_assets_reject_unproduced_elsa_dependency(self):
        assets = self.assets({"Elsa.RetiredModule/3.9.0": {"type": "package"}})
        with self.assertRaisesRegex(RuntimeError, "unclassified Elsa package"):
            proof.validate_project_assets(
                assets,
                ["net8.0", "net9.0", "net10.0"],
                {"elsa"},
                "4.0.0-proof.12.1",
                {"elsa.platform.packagemanifest", "elsa.platform.packagemanifest.generator"},
            )

    def test_project_assets_reject_manifest_exclusion_even_without_elsa_prefix(self):
        assets = self.assets({"Retired.Internal.Module/1.2.3": {"type": "package"}})
        with self.assertRaisesRegex(RuntimeError, "explicitly excluded package"):
            proof.validate_project_assets(
                assets,
                ["net8.0", "net9.0", "net10.0"],
                {"elsa"},
                "4.0.0-proof.12.1",
                set(),
                {"retired.internal.module"},
            )

    def test_project_assets_reject_wrong_version_project_reference_and_missing_tfm(self):
        wrong_version = self.assets({"Elsa/3.9.0": {"type": "package"}})
        with self.assertRaisesRegex(RuntimeError, "not the proof version"):
            proof.validate_project_assets(
                wrong_version,
                ["net8.0", "net9.0", "net10.0"],
                {"elsa"},
                "4.0.0-proof.12.1",
                set(),
            )

        project_reference = self.assets({"Elsa/4.0.0-proof.12.1": {"type": "project"}})
        with self.assertRaisesRegex(RuntimeError, "must resolve as a NuGet package"):
            proof.validate_project_assets(
                project_reference,
                ["net8.0", "net9.0", "net10.0"],
                {"elsa"},
                "4.0.0-proof.12.1",
                set(),
            )

        missing_framework = self.assets(
            {"Elsa/4.0.0-proof.12.1": {"type": "package"}}, frameworks=["net8.0", "net10.0"]
        )
        with self.assertRaisesRegex(RuntimeError, "target frameworks"):
            proof.validate_project_assets(
                missing_framework,
                ["net8.0", "net9.0", "net10.0"],
                {"elsa"},
                "4.0.0-proof.12.1",
                set(),
            )

    def test_project_assets_report_each_framework_direct_package_selection(self):
        package = {"Elsa/4.0.0-proof.12.1": {"type": "package"}}
        assets = {
            "targets": {
                "net8.0": package,
                "net9.0": {},
                "net10.0": package,
            },
            "libraries": package,
        }
        restored = proof.validate_project_assets(
            assets,
            ["net8.0", "net9.0", "net10.0"],
            {"elsa"},
            "4.0.0-proof.12.1",
            set(),
        )
        self.assertEqual(restored["by_framework"]["net9.0"], [])

    def test_cached_package_requires_local_metadata_exact_bytes_and_nuget_sha512(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            artifacts = root / "artifacts"
            cache = root / "packages"
            artifacts.mkdir()
            package = artifacts / "Elsa.4.0.0-proof.12.1.nupkg"
            package.write_bytes(b"verified package bytes")
            cache_entry = cache / "elsa" / "4.0.0-proof.12.1"
            cache_entry.mkdir(parents=True)
            cached = cache_entry / package.name.lower()
            cached.write_bytes(package.read_bytes())
            expected = proof.base64_sha512(package.read_bytes())
            cached.with_suffix(cached.suffix + ".sha512").write_text(expected, encoding="utf-8")
            (cache_entry / ".nupkg.metadata").write_text(
                json.dumps({"source": str(artifacts.resolve())}), encoding="utf-8"
            )

            proof.verify_cached_package("Elsa", "4.0.0-proof.12.1", package, cache, artifacts)

            cached.write_bytes(b"different bytes")
            with self.assertRaisesRegex(RuntimeError, "exact verified nupkg"):
                proof.verify_cached_package("Elsa", "4.0.0-proof.12.1", package, cache, artifacts)

    def test_cached_package_rejects_nonlocal_source_and_wrong_hash(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            artifacts = root / "artifacts"
            cache = root / "packages"
            artifacts.mkdir()
            package = artifacts / "Elsa.4.0.0-proof.12.1.nupkg"
            package.write_bytes(b"verified package bytes")
            cache_entry = cache / "elsa" / "4.0.0-proof.12.1"
            cache_entry.mkdir(parents=True)
            cached = cache_entry / package.name.lower()
            cached.write_bytes(package.read_bytes())
            cached.with_suffix(cached.suffix + ".sha512").write_text("wrong", encoding="utf-8")
            metadata_path = cache_entry / ".nupkg.metadata"
            metadata_path.write_text(json.dumps({"source": proof.NUGET_ORG}), encoding="utf-8")

            with self.assertRaisesRegex(RuntimeError, "Unexpected package source"):
                proof.verify_cached_package("Elsa", "4.0.0-proof.12.1", package, cache, artifacts)

            metadata_path.write_text(json.dumps({"source": str(artifacts.resolve())}), encoding="utf-8")
            with self.assertRaisesRegex(RuntimeError, "NuGet SHA-512"):
                proof.verify_cached_package("Elsa", "4.0.0-proof.12.1", package, cache, artifacts)

    def test_reviewed_external_exception_must_come_from_nuget_org(self):
        with tempfile.TemporaryDirectory() as temporary:
            packages = Path(temporary)
            metadata = packages / "elsa.platform.packagemanifest.generator" / "0.0.1-preview.53" / ".nupkg.metadata"
            metadata.parent.mkdir(parents=True)
            metadata.write_text(json.dumps({"source": "https://f.feedz.io/example/index.json"}), encoding="utf-8")

            with self.assertRaisesRegex(RuntimeError, "did not resolve from nuget.org"):
                proof.verify_external_cache_source(
                    "Elsa.Platform.PackageManifest.Generator", "0.0.1-preview.53", packages
                )

            metadata.write_text(json.dumps({"source": proof.NUGET_ORG}), encoding="utf-8")
            evidence = proof.verify_external_cache_source(
                "Elsa.Platform.PackageManifest.Generator", "0.0.1-preview.53", packages
            )
            self.assertEqual(evidence["assets_type"], "package")
            self.assertEqual(evidence["source"], proof.NUGET_ORG)

    def test_timed_out_command_kills_only_its_own_process_group(self):
        class FakeProcess:
            pid = 12345

            def __init__(self):
                self.waits = 0

            def wait(self, timeout=None):
                self.waits += 1
                if self.waits == 1:
                    raise TimeoutExpired("dotnet restore", timeout)
                return -9

            def poll(self):
                return None

        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            process = FakeProcess()
            with patch.object(proof.subprocess, "Popen", return_value=process), patch.object(
                proof.os, "killpg"
            ) as kill_group:
                with self.assertRaisesRegex(RuntimeError, "exceeded 1s"):
                    proof._run_command(
                        ["dotnet", "restore"], root, {}, root / "restore.log", timeout_seconds=1
                    )
            kill_group.assert_called_once_with(process.pid, proof.signal.SIGKILL)
            self.assertEqual(process.waits, 2)

    def test_generated_consumer_is_package_reference_only_for_all_frameworks(self):
        with tempfile.TemporaryDirectory() as temporary:
            project = Path(temporary) / "Consumer.csproj"
            proof._create_project(project, "4.0.0-proof.12.1")
            text = project.read_text(encoding="utf-8")
            self.assertIn("<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>", text)
            self.assertEqual(text.count("<PackageReference "), len(proof.REQUIRED_PACKAGES))
            self.assertNotIn("ProjectReference", text)
            for package_id in proof.REQUIRED_PACKAGES:
                self.assertIn(f'Include="{package_id}" Version="4.0.0-proof.12.1"', text)

    def test_manifest_rejects_omitted_reviewed_exceptions_and_wrong_nupkg_identity(self):
        manifest = self.valid_manifest()
        manifest.pop("external_package_exceptions")
        with self.assertRaisesRegex(ValueError, "external_package_exceptions"):
            proof._validated_manifest(manifest)

        manifest = self.valid_manifest()
        manifest["packages"][0]["nupkg"] = "Elsa.Retired.4.0.0-proof.12.1.nupkg"
        with self.assertRaisesRegex(ValueError, "does not match package ID and version"):
            proof._validated_manifest(manifest)

    @staticmethod
    def valid_manifest():
        version = "4.0.0-proof.12.1"
        return {
            "version": version,
            "source_commit": "a" * 40,
            "packages": [
                {
                    "id": package_id,
                    "nupkg": f"{package_id}.{version}.nupkg",
                    "frameworks": list(proof.FRAMEWORKS),
                }
                for package_id in proof.REQUIRED_PACKAGES
            ],
            "exclusions": [],
            "external_package_exceptions": {
                "Elsa.Platform.PackageManifest": "Reviewed external manifest tool",
                "Elsa.Platform.PackageManifest.Generator": "Reviewed external generator",
            },
        }

    @staticmethod
    def assets(libraries, frameworks=("net8.0", "net9.0", "net10.0")):
        targets = {framework: dict(libraries) for framework in frameworks}
        return {"targets": targets, "libraries": libraries}


if __name__ == "__main__":
    unittest.main()
