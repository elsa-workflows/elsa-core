#!/usr/bin/env python3
"""Focused tests for the copied runtime-artifact manifest contract."""
import importlib.util
import json
import os
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).with_name("run-proof-skeleton.py")
SPEC = importlib.util.spec_from_file_location("run_proof_skeleton", SCRIPT)
PROOF = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PROOF)

SOURCE_HEAD = "a" * 40
BUILDS = [
    {"component": "core", "command": "dotnet build core", "exitCode": 0},
    {"component": "server-studio", "command": "dotnet publish studio", "exitCode": 0},
    {"component": "startup", "command": "dotnet build startup", "exitCode": 0},
]


class RuntimeArtifactProvenanceTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.base = Path(self.temporary.name)
        self.core = self.base / "core-host"
        self.studio = self.base / "studio-host"
        self.wwwroot = self.studio / "wwwroot"
        self.core.mkdir()
        (self.wwwroot / "_content" / "Elsa.Studio").mkdir(parents=True)
        (self.wwwroot / "assets").mkdir()

        self.write(self.core / "Elsa.ModularServer.Web.dll", b"core dll")
        self.write(self.core / "Elsa.ModularServer.Web.deps.json", b"{}")
        self.write(self.core / "appsettings.Production.json", b"excluded host config")
        self.write(self.studio / "Elsa.Studio.Host.Server.dll", b"studio dll")
        self.write(self.studio / "Elsa.Studio.Host.Server.runtimeconfig.json", b"{}")
        self.write(self.studio / "Elsa.NativeCompensation.StudioProofStartup.dll", b"startup dll")
        self.write(self.studio / "Elsa.NativeCompensation.StudioProofStartup.deps.json", b"{}")
        self.write(self.studio / "other-settings.json", b"excluded unrelated config")
        self.write(self.wwwroot / "index.html", b"<html></html>")
        self.write(self.wwwroot / "_content" / "Elsa.Studio" / "studio.css", b"body { color: black; }")
        self.write(self.wwwroot / "assets" / "app.js", b"const proof = 1;")
        self.write(self.wwwroot / "assets" / "asset-map.json", b"{}")

        self.manifest = self.base / "build-manifest.json"
        self.write_manifest(PROOF.binary_snapshot(self.core, self.studio))

    @staticmethod
    def write(path, content):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(content)

    def write_manifest(self, hashes):
        self.manifest.write_text(json.dumps({
            "schemaVersion": 1,
            "sourceHead": SOURCE_HEAD,
            "sourceTreeClean": True,
            "builds": BUILDS,
            "binarySha256": hashes,
        }), encoding="utf-8")
        os.chmod(self.manifest, 0o600)

    def validate_current_outputs(self):
        return PROOF.validate_build_manifest(
            self.manifest, SOURCE_HEAD, PROOF.binary_snapshot(self.core, self.studio)
        )

    def test_same_length_javascript_tamper_rejects_manifest(self):
        self.validate_current_outputs()
        asset = self.wwwroot / "assets" / "app.js"
        original = asset.read_bytes()
        changed = b"const proof = 2;"
        self.assertEqual(len(original), len(changed))
        asset.write_bytes(changed)

        with self.assertRaisesRegex(PROOF.StopProof, "build-manifest-binary-hashes-mismatch"):
            self.validate_current_outputs()

    def test_missing_static_asset_rejects_manifest(self):
        (self.wwwroot / "index.html").unlink()

        with self.assertRaisesRegex(PROOF.StopProof, "build-manifest-binary-hashes-mismatch"):
            self.validate_current_outputs()

    def test_extra_static_asset_rejects_manifest(self):
        self.write(self.wwwroot / "assets" / "unexpected.js", b"extra")

        with self.assertRaisesRegex(PROOF.StopProof, "build-manifest-binary-hashes-mismatch"):
            self.validate_current_outputs()

    def test_snapshot_counts_runtime_binaries_and_wwwroot_assets_only(self):
        self.write(self.studio / "outside-assets.js", b"not under wwwroot")
        hashes = PROOF.binary_snapshot(self.core, self.studio)

        self.assertIn("core-host/Elsa.ModularServer.Web.dll", hashes)
        self.assertIn("core-host/Elsa.ModularServer.Web.deps.json", hashes)
        self.assertIn("studio-host/Elsa.Studio.Host.Server.runtimeconfig.json", hashes)
        self.assertIn("studio-host/Elsa.NativeCompensation.StudioProofStartup.dll", hashes)
        self.assertIn("studio-host/wwwroot/index.html", hashes)
        self.assertIn("studio-host/wwwroot/_content/Elsa.Studio/studio.css", hashes)
        self.assertIn("studio-host/wwwroot/assets/app.js", hashes)
        self.assertIn("studio-host/wwwroot/assets/asset-map.json", hashes)
        self.assertNotIn("core-host/appsettings.Production.json", hashes)
        self.assertNotIn("studio-host/other-settings.json", hashes)
        self.assertNotIn("studio-host/outside-assets.js", hashes)


if __name__ == "__main__":
    unittest.main()
