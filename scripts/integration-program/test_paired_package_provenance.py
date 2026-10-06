import json
import base64
import hashlib
from pathlib import Path
import tempfile
import unittest

import paired_package_provenance as provenance


class PackageGraphTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.project = self.write_project("server", "Elsa.Studio.Host.Server", "Microsoft.NET.Sdk.Web")
        self.assets = {"targets": {"net10.0": {"Elsa.Studio.Core/3.10.0": {"type": "package"}}}, "libraries": {}}

    def write_project(self, directory, name, sdk, reference=""):
        path = self.root / "projects" / directory / (name + ".csproj")
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(f'<Project Sdk="{sdk}"><PropertyGroup><TargetFramework>net10.0</TargetFramework><IsPackable>false</IsPackable></PropertyGroup>{reference}</Project>')
        return path

    def read(self, **kwargs):
        path = self.project.parent / "obj" / "project.assets.json"
        path.parent.mkdir(exist_ok=True)
        path.write_text(json.dumps(self.assets))
        return provenance.read_package_assets(self.project, "net10.0", **kwargs)

    def hosted(self):
        client = self.write_project("wasm", "Elsa.Studio.Host.Wasm", "Microsoft.NET.Sdk.BlazorWebAssembly")
        self.project = self.write_project("hosted-wasm", "Elsa.Studio.Host.HostedWasm", "Microsoft.NET.Sdk.Web",
            '<ItemGroup><ProjectReference Include="../wasm/Elsa.Studio.Host.Wasm.csproj" /></ItemGroup>')
        self.assets["targets"]["net10.0"]["Elsa.Studio.Host.Wasm/1.0.0"] = {"type": "project"}
        self.assets["libraries"]["Elsa.Studio.Host.Wasm/1.0.0"] = {"type": "project", "msbuildProject": "../wasm/Elsa.Studio.Host.Wasm.csproj"}
        return client

    def test_package_graph_is_unchanged(self):
        assets, edges = self.read()
        self.assertEqual(self.assets, assets)
        self.assertEqual([], edges)

    def test_only_explicit_owned_host_edge_is_projected(self):
        client = self.hosted()
        assets, edges = self.read(fixture_project=client)
        self.assertEqual({"Elsa.Studio.Core/3.10.0"}, set(assets["targets"]["net10.0"]))
        self.assertEqual(1, len(edges))
        self.assertEqual("Elsa.Studio.Host.Wasm/1.0.0", edges[0]["identity"])
        self.assertEqual(provenance.sha256(client), edges[0]["project_sha256"])
        self.assertIn("Elsa.Studio.Host.Wasm/1.0.0", self.assets["targets"]["net10.0"])

    def test_project_fallback_without_explicit_fixture_fails(self):
        self.hosted()
        with self.assertRaisesRegex(RuntimeError, "fixture"):
            self.read()

    def test_unexpected_library_or_path_or_packable_fixture_fails(self):
        client = self.hosted()
        original = json.loads(json.dumps(self.assets))
        for change in ("library", "path", "packable", "extra"):
            with self.subTest(change=change):
                self.assets = json.loads(json.dumps(original))
                if change == "library":
                    self.assets["targets"]["net10.0"]["Elsa.Core/3.10.0"] = {"type": "project"}
                elif change == "path":
                    self.assets["libraries"]["Elsa.Studio.Host.Wasm/1.0.0"]["msbuildProject"] = "../other.csproj"
                elif change == "packable":
                    client.write_text(client.read_text().replace("false", "true"))
                else:
                    self.project.write_text(self.project.read_text().replace("</ItemGroup>", '<ProjectReference Include="../other.csproj" /></ItemGroup>'))
                with self.assertRaises(RuntimeError):
                    self.read(fixture_project=client)
                client.write_text(client.read_text().replace("true", "false"))

    def test_rid_graph_only_for_actual_wasm_project(self):
        self.assets["targets"]["net10.0/browser-wasm"] = self.assets["targets"]["net10.0"].copy()
        with self.assertRaisesRegex(RuntimeError, "targets"):
            self.read()
        self.project = self.write_project("wasm", "Elsa.Studio.Host.Wasm", "Microsoft.NET.Sdk.BlazorWebAssembly")
        assets, _ = self.read()
        self.assertEqual(set(self.assets["targets"]), set(assets["targets"]))
        self.assets["targets"]["net10.0/linux-x64"] = {}
        with self.assertRaisesRegex(RuntimeError, "targets"):
            self.read()

    def test_symlinked_assets_and_wrong_framework_fail(self):
        self.read()
        path = self.project.parent / "obj" / "project.assets.json"
        real = path.with_suffix(".real")
        path.rename(real)
        path.symlink_to(real)
        with self.assertRaisesRegex(RuntimeError, "Symlink"):
            provenance.read_package_assets(self.project, "net10.0")
        path.unlink()
        real.rename(path)
        with self.assertRaisesRegex(RuntimeError, "framework"):
            provenance.read_package_assets(self.project, "net9.0")

    def test_public_archive_binds_actual_bytes_sidecar_and_origin(self):
        directory = self.root / "example" / "1.0.0"
        directory.mkdir(parents=True)
        archive = directory / "example.1.0.0.nupkg"
        archive.write_bytes(b"synthetic archive")
        sidecar = directory / "example.1.0.0.nupkg.sha512"
        sidecar.write_text(base64.b64encode(hashlib.sha512(archive.read_bytes()).digest()).decode())
        metadata = directory / ".nupkg.metadata"
        metadata.write_text(json.dumps({"source": provenance.packages.NUGET_ORG}))
        receipt = provenance.public_archive_evidence("Example", "1.0.0", self.root)
        self.assertEqual(provenance.sha256(archive), receipt["sha256"])
        archive.write_bytes(b"changed bytes")
        with self.assertRaisesRegex(RuntimeError, "sidecar"):
            provenance.public_archive_evidence("Example", "1.0.0", self.root)
        metadata.write_text(json.dumps({"source": "https://unreviewed.example/feed"}))
        with self.assertRaisesRegex(RuntimeError, "nuget.org"):
            provenance.public_archive_evidence("Example", "1.0.0", self.root)

    def test_public_four_part_nuget_version_is_valid_but_paths_are_not(self):
        directory = self.root / "sourcegear.sqlite3" / "3.50.4.5"
        directory.mkdir(parents=True)
        content = b"synthetic four-part NuGet archive"
        (directory / "sourcegear.sqlite3.3.50.4.5.nupkg").write_bytes(content)
        (directory / "sourcegear.sqlite3.3.50.4.5.nupkg.sha512").write_text(base64.b64encode(hashlib.sha512(content).digest()).decode())
        (directory / ".nupkg.metadata").write_text(json.dumps({"source": provenance.packages.NUGET_ORG}))
        self.assertEqual(hashlib.sha256(content).hexdigest(), provenance.public_archive_evidence("SourceGear.sqlite3", "3.50.4.5", self.root)["sha256"])
        for invalid in ("../3.50.4.5", "3.50.4.5/extra", "*", ""):
            with self.subTest(version=invalid), self.assertRaisesRegex(RuntimeError, "Unsafe"):
                provenance.public_archive_evidence("SourceGear.sqlite3", invalid, self.root)


if __name__ == "__main__":
    unittest.main()
