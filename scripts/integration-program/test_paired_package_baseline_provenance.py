from __future__ import annotations

import base64
import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
import zipfile

import paired_package_baseline_provenance as baseline


class BaselinePackageProvenanceTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.project = self.write_project("server", "Elsa.Studio.Host.Server", "Microsoft.NET.Sdk.Web")
        self.cache = self.root / "isolated-cache"
        self.cache.mkdir()
        self.policy = json.loads(baseline.POLICY_PATH.read_text(encoding="utf-8"))
        record = {
            "id": "Elsa",
            "version": "3.8.4",
            "owner": "elsa-core",
            "repository": {
                "type": "git",
                "url": baseline.REPOSITORIES["elsa-core"],
                "commit": baseline.RELEASE_COMMITS["elsa-core"]["3.8.4"],
            },
            "archive_sha256": "0" * 64,
        }
        self.policy["packages"] = [record]
        self.policy["missing_provenance"] = []
        self.record = record
        self.elsa_nuspec_repository = dict(record["repository"])
        self.elsa_bytes = self.write_archive("Elsa", "3.8.4", self.elsa_nuspec_repository)
        self.record["archive_sha256"] = hashlib.sha256(self.elsa_bytes).hexdigest()
        self.write_archive("Newtonsoft.Json", "13.0.3", None)
        self.write_restore(
            {
                "Elsa/3.8.4": {"type": "package"},
                "Newtonsoft.Json/13.0.3": {"type": "package"},
            }
        )

    def write_project(self, directory: str, name: str, sdk: str, extra: str = "") -> Path:
        path = self.root / "projects" / directory / f"{name}.csproj"
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(
            f'<Project Sdk="{sdk}"><PropertyGroup><TargetFramework>net8.0</TargetFramework>'
            f"<IsPackable>false</IsPackable></PropertyGroup>{extra}</Project>",
            encoding="utf-8",
        )
        return path

    def write_archive(self, package_id: str, version: str, repository: dict | None) -> bytes:
        cache_entry = self.cache / package_id.lower() / version
        cache_entry.mkdir(parents=True, exist_ok=True)
        repository_xml = ""
        if repository:
            repository_xml = (
                f'<repository type="{repository["type"]}" url="{repository["url"]}" '
                f'commit="{repository["commit"]}" />'
            )
        nuspec = (
            f'<package><metadata><id>{package_id}</id><version>{version}</version>'
            f"{repository_xml}</metadata></package>"
        ).encode()
        archive_path = cache_entry / f"{package_id.lower()}.{version}.nupkg"
        with zipfile.ZipFile(archive_path, "w", zipfile.ZIP_DEFLATED) as package:
            package.writestr(f"{package_id}.nuspec", nuspec)
        content = archive_path.read_bytes()
        digest = base64.b64encode(hashlib.sha512(content).digest()).decode("ascii")
        archive_path.with_suffix(archive_path.suffix + ".sha512").write_text(digest, encoding="utf-8")
        (cache_entry / ".nupkg.metadata").write_text(
            json.dumps({"version": 2, "contentHash": digest, "source": baseline.NUGET_ORG}),
            encoding="utf-8",
        )
        return content

    def write_restore(self, target: dict, libraries: dict | None = None) -> Path:
        project_root = self.project.parent
        project_root.mkdir(parents=True, exist_ok=True)
        config = project_root / "NuGet.Config"
        config.write_text(
            '<configuration><packageSources><clear /><add key="nuget.org" '
            f'value="{baseline.NUGET_ORG}" /></packageSources></configuration>',
            encoding="utf-8",
        )
        assets_path = project_root / "obj" / "project.assets.json"
        assets_path.parent.mkdir(parents=True, exist_ok=True)
        assets = {
            "targets": {"net8.0": target},
            "libraries": libraries or {key: {"type": "package"} for key in target},
            "project": {
                "restore": {
                    "sources": {baseline.NUGET_ORG: {}},
                    "configFilePaths": [str(config.resolve())],
                    "packagesPath": str(self.cache.resolve()),
                    "fallbackFolders": [],
                }
            },
            "packageFolders": {str(self.cache.resolve()): {}},
        }
        assets_path.write_text(json.dumps(assets), encoding="utf-8")
        return assets_path

    def verify(self, policy: dict | None = None, fixture_project: Path | None = None) -> dict:
        return baseline.validate_baseline_project(
            self.project,
            self.cache,
            "net8.0",
            "3.8.4",
            policy=self.policy if policy is None else policy,
            fixture_project=fixture_project,
        )

    def test_returns_raw_assets_hash_full_package_hash_ledger_and_graph(self):
        assets_path = self.project.parent / "obj" / "project.assets.json"
        receipt = self.verify()
        packages = {item["id"]: item for item in receipt["packages"]}
        self.assertEqual(hashlib.sha256(assets_path.read_bytes()).hexdigest(), receipt["project_assets_sha256"])
        self.assertEqual({"Elsa", "Newtonsoft.Json"}, set(packages))
        self.assertEqual(hashlib.sha256(self.elsa_bytes).hexdigest(), packages["Elsa"]["archive_sha256"])
        self.assertEqual("elsa-core", packages["Elsa"]["owner"])
        self.assertEqual("nuget_dependency", packages["Newtonsoft.Json"]["classification"])
        self.assertEqual(["net8.0"], packages["Elsa"]["frameworks"])
        self.assertTrue(receipt["package_only"])
        self.assertEqual([], receipt["fixture_edge_receipts"])
        self.assertEqual({"Elsa/3.8.4", "Newtonsoft.Json/13.0.3"}, {
            f'{entry["id"]}/{entry["version"]}' for entry in receipt["package_graph"]["package_frameworks"]
        })

    def test_tracked_policy_keeps_verified_sources_and_missing_tuples_separate(self):
        tracked = baseline._read_policy(baseline.POLICY_PATH)
        self.assertEqual(145, len(tracked["packages"]))
        self.assertEqual([], tracked["missing_provenance"])
        self.assertEqual(
            {"elsa-core", "elsa-studio", "elsa-extensions"},
            {record["owner"] for record in tracked["packages"]},
        )
        self.assertTrue(all(record["approved_tuples"] == [] for record in tracked["platform_exceptions"]))

    def test_rejects_a_cached_archive_that_differs_from_the_pinned_digest(self):
        archive = self.cache / "elsa" / "3.8.4" / "elsa.3.8.4.nupkg"
        archive.write_bytes(archive.read_bytes() + b"tamper")
        with self.assertRaisesRegex(RuntimeError, "sidecar"):
            self.verify()

    def test_rejects_a_stale_nuget_sha512_sidecar(self):
        sidecar = self.cache / "elsa" / "3.8.4" / "elsa.3.8.4.nupkg.sha512"
        sidecar.write_text("bad-sidecar", encoding="utf-8")
        with self.assertRaisesRegex(RuntimeError, "SHA-512 sidecar"):
            self.verify()

    def test_rejects_package_cache_that_was_not_actually_restored_from_nuget_org(self):
        metadata = self.cache / "elsa" / "3.8.4" / ".nupkg.metadata"
        value = json.loads(metadata.read_text(encoding="utf-8"))
        value["source"] = "https://feed.example.invalid/index.json"
        metadata.write_text(json.dumps(value), encoding="utf-8")
        with self.assertRaisesRegex(RuntimeError, "resolve from nuget.org"):
            self.verify()

    def test_rejects_elsa_archive_from_wrong_repository_commit(self):
        bad_repository = dict(self.elsa_nuspec_repository, commit="0" * 40)
        self.elsa_bytes = self.write_archive("Elsa", "3.8.4", bad_repository)
        self.record["archive_sha256"] = hashlib.sha256(self.elsa_bytes).hexdigest()
        with self.assertRaisesRegex(RuntimeError, "source ownership"):
            self.verify()

    def test_rejects_wrong_source_owner_even_when_repository_fields_are_self_consistent(self):
        altered = copy.deepcopy(self.policy)
        altered["packages"][0]["owner"] = "elsa-studio"
        with self.assertRaisesRegex(ValueError, "source owner/commit"):
            self.verify(altered)

    def test_rejects_duplicate_exact_policy_tuple(self):
        altered = copy.deepcopy(self.policy)
        altered["packages"].append(copy.deepcopy(altered["packages"][0]))
        with self.assertRaisesRegex(ValueError, "Duplicate baseline archive tuple"):
            self.verify(altered)

    def test_rejects_invalid_policy_archive_hash(self):
        altered = copy.deepcopy(self.policy)
        altered["packages"][0]["archive_sha256"] = "not-a-sha256"
        with self.assertRaisesRegex(ValueError, "invalid SHA-256"):
            self.verify(altered)

    def test_unresolved_dependency_remains_unapproved(self):
        self.policy["missing_provenance"] = [
            {"id": "Elsa.Api.Client", "version": "3.8.4", "status": "missing_provenance"}
        ]
        self.write_restore({"Elsa.Api.Client/3.8.4": {"type": "package"}})
        with self.assertRaisesRegex(RuntimeError, "lacks approved source archive provenance"):
            self.verify()

    def test_unknown_elsa_package_is_not_owned_by_prefix(self):
        self.write_restore({"Elsa.Unlisted.Component/3.8.4": {"type": "package"}})
        with self.assertRaisesRegex(RuntimeError, "unclassified Elsa package"):
            self.verify()

    def test_platform_package_is_classified_separately_but_has_no_approved_tuple(self):
        self.write_restore({"Elsa.Platform.PackageManifest/0.1.0": {"type": "package"}})
        with self.assertRaisesRegex(RuntimeError, "Platform package tuple requires separate"):
            self.verify()

    def test_explicit_legacy_secrets_exclusion_is_rejected(self):
        self.write_restore({"Elsa.Secrets.Api/3.8.4": {"type": "package"}})
        with self.assertRaisesRegex(RuntimeError, "explicitly excluded"):
            self.verify()

    def test_requires_project_local_nuget_org_only_restore(self):
        self.write_restore({"Elsa/3.8.4": {"type": "package"}})
        assets_path = self.project.parent / "obj" / "project.assets.json"
        assets = json.loads(assets_path.read_text())
        assets["project"]["restore"]["sources"]["https://example.invalid/index.json"] = {}
        assets_path.write_text(json.dumps(assets), encoding="utf-8")
        with self.assertRaisesRegex(RuntimeError, "only effective source"):
            self.verify()

    def test_accepts_only_the_shared_adapter_projected_hosted_wasm_edge(self):
        client = self.write_project("wasm", "Elsa.Studio.Host.Wasm", "Microsoft.NET.Sdk.BlazorWebAssembly")
        hosted = self.write_project(
            "hosted-wasm",
            "Elsa.Studio.Host.HostedWasm",
            "Microsoft.NET.Sdk.Web",
            '<ItemGroup><ProjectReference Include="../wasm/Elsa.Studio.Host.Wasm.csproj" /></ItemGroup>',
        )
        self.project = hosted
        self.write_restore(
            {
                "Elsa/3.8.4": {"type": "package"},
                "Elsa.Studio.Host.Wasm/1.0.0": {"type": "project"},
            },
            {
                "Elsa/3.8.4": {"type": "package"},
                "Elsa.Studio.Host.Wasm/1.0.0": {
                    "type": "project",
                    "msbuildProject": "../wasm/Elsa.Studio.Host.Wasm.csproj",
                },
            },
        )
        receipt = self.verify(fixture_project=client)
        self.assertEqual(1, len(receipt["fixture_edge_receipts"]))
        self.assertEqual("nonpackable_fixture_only", receipt["fixture_edge_receipts"][0]["kind"])
        self.assertEqual(["Elsa"], [entry["id"] for entry in receipt["packages"]])

    def test_validates_both_real_wasm_target_frameworks(self):
        self.project = self.write_project("wasm", "Elsa.Studio.Host.Wasm", "Microsoft.NET.Sdk.BlazorWebAssembly")
        assets_path = self.write_restore({"Elsa/3.8.4": {"type": "package"}})
        assets = json.loads(assets_path.read_text(encoding="utf-8"))
        assets["targets"]["net8.0/browser-wasm"] = copy.deepcopy(assets["targets"]["net8.0"])
        assets_path.write_text(json.dumps(assets), encoding="utf-8")

        receipt = self.verify()
        self.assertEqual(["net8.0", "net8.0/browser-wasm"], receipt["packages"][0]["frameworks"])
        self.assertEqual(2, len(receipt["package_graph"]["by_framework"]))

    def test_project_reference_without_the_explicit_fixture_is_rejected(self):
        self.project = self.write_project(
            "hosted-wasm",
            "Elsa.Studio.Host.HostedWasm",
            "Microsoft.NET.Sdk.Web",
            '<ItemGroup><ProjectReference Include="../wasm/Elsa.Studio.Host.Wasm.csproj" /></ItemGroup>',
        )
        self.write_restore({"Elsa/3.8.4": {"type": "package"}})
        with self.assertRaisesRegex(RuntimeError, "source reference without explicit fixture"):
            self.verify()


if __name__ == "__main__":
    unittest.main()
