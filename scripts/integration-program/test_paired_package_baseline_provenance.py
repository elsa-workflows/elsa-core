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

    def write_archive(self, package_id: str, version: str, repository: dict | None,
                      runtime_files: dict[str, bytes] | None = None, *,
                      metadata_content_hash: str | None = None,
                      signature: bytes | None = None) -> bytes:
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
            for name, content in (runtime_files or {}).items():
                package.writestr(name, content)
            if signature is not None:
                package.writestr(".signature.p7s", signature)
        content = archive_path.read_bytes()
        digest = base64.b64encode(hashlib.sha512(content).digest()).decode("ascii")
        content_hash = metadata_content_hash or digest
        archive_path.with_suffix(archive_path.suffix + ".sha512").write_text(digest, encoding="utf-8")
        (cache_entry / ".nupkg.metadata").write_text(
            json.dumps({"version": 2, "contentHash": content_hash, "source": baseline.NUGET_ORG}),
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
        resolved_libraries = copy.deepcopy(libraries or {})
        for key, library in target.items():
            if library.get("type") != "package":
                continue
            package_id, separator, version = key.partition("/")
            if not separator:
                continue
            metadata_path = self.cache / package_id.lower() / version / ".nupkg.metadata"
            if metadata_path.is_file():
                resolved_libraries.setdefault(key, {"type": "package"}).setdefault(
                    "sha512", json.loads(metadata_path.read_text(encoding="utf-8")).get("contentHash")
                )
        assets = {
            "targets": {"net8.0": target},
            "libraries": resolved_libraries or {key: {"type": "package"} for key in target},
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

    def prepare_loaded_baseline(self):
        specifications = (
            ("Elsa", "elsa-core", "Elsa"),
            ("Elsa.WorkflowContexts", "elsa-extensions", "Elsa.WorkflowContexts"),
            ("Elsa.Studio", "elsa-studio", "Elsa.Studio"),
        )
        records, target, loaded = [], {}, []
        for package_id, owner, assembly_name in specifications:
            repository = {
                "type": "git",
                "url": baseline.REPOSITORIES[owner],
                "commit": baseline.RELEASE_COMMITS[owner]["3.8.4"],
            }
            asset = f"lib/net8.0/{assembly_name}.dll"
            content = f"binary bytes for {assembly_name}".encode()
            record = {
                "id": package_id,
                "version": "3.8.4",
                "owner": owner,
                "repository": repository,
                "archive_sha256": "0" * 64,
            }
            archive = self.write_archive(package_id, "3.8.4", repository, {asset: content})
            record["archive_sha256"] = hashlib.sha256(archive).hexdigest()
            records.append(record)
            target[f"{package_id}/3.8.4"] = {"type": "package", "runtime": {asset: {}}}

            cached = self.cache / package_id.lower() / "3.8.4" / asset
            cached.parent.mkdir(parents=True, exist_ok=True)
            cached.write_bytes(content)
            location = self.project.parent / "bin" / "Release" / "net8.0" / f"{assembly_name}.dll"
            location.parent.mkdir(parents=True, exist_ok=True)
            location.write_bytes(content)
            loaded.append({
                "name": assembly_name,
                "version": "3.8.4.0",
                "fullName": f"{assembly_name}, Version=3.8.4.0, Culture=neutral, PublicKeyToken=null",
                "informationalVersion": "3.8.4+" + repository["commit"],
                "location": str(location),
                "sha256": hashlib.sha256(content).hexdigest(),
            })
        self.policy["packages"] = records
        self.write_restore(target)
        return loaded, tuple(item[2] for item in specifications)

    def test_loaded_assemblies_are_bound_to_exact_release_owned_archives(self):
        loaded, required = self.prepare_loaded_baseline()

        receipt = baseline.verify_baseline_loaded_assemblies(
            loaded, self.project, self.cache, "net8.0", "3.8.4", required_assemblies=required,
            policy=self.policy,
        )

        self.assertEqual(set(required), {item["name"] for item in receipt})
        self.assertEqual(
            {"elsa-core", "elsa-extensions", "elsa-studio"},
            {item["package_owner"] for item in receipt},
        )
        for item in receipt:
            self.assertEqual(item["repository_commit"], baseline.RELEASE_COMMITS[item["package_owner"]]["3.8.4"])
            self.assertEqual(item["archive_member_sha256"], item["sha256"])
            self.assertEqual(item["cached_sha256"], item["built_sha256"])
            self.assertEqual("https://api.nuget.org/v3/index.json", item["package_source"])

    def test_loaded_assembly_verifier_rejects_missing_duplicate_and_unmatched_names(self):
        loaded, required = self.prepare_loaded_baseline()
        verify = lambda rows, names=required: baseline.verify_baseline_loaded_assemblies(
            rows, self.project, self.cache, "net8.0", "3.8.4", required_assemblies=names,
            policy=self.policy,
        )

        with self.assertRaisesRegex(RuntimeError, "missing"):
            verify(loaded[:-1])
        with self.assertRaisesRegex(RuntimeError, "(?i)duplicate"):
            verify(loaded + [loaded[0]])
        host_executable = dict(loaded[0], name="Elsa.Studio.Host", fullName="Elsa.Studio.Host, Version=1.0.0.0,")
        with self.assertRaisesRegex(RuntimeError, "no unique restored package runtime asset"):
            verify(loaded + [host_executable])

    def test_loaded_assembly_verifier_rejects_cache_and_built_byte_mismatches(self):
        loaded, required = self.prepare_loaded_baseline()
        verify = lambda: baseline.verify_baseline_loaded_assemblies(
            loaded, self.project, self.cache, "net8.0", "3.8.4", required_assemblies=required,
            policy=self.policy,
        )

        cached = self.cache / "elsa" / "3.8.4" / "lib/net8.0/Elsa.dll"
        cached.write_bytes(b"changed cache")
        with self.assertRaisesRegex(RuntimeError, "cache runtime DLL differs"):
            verify()

        cached.write_bytes(b"binary bytes for Elsa")
        output = Path(loaded[0]["location"])
        output.write_bytes(b"changed output")
        with self.assertRaisesRegex(RuntimeError, "(?i)built assembly differs"):
            verify()

    def test_loaded_assembly_verifier_rejects_release_version_and_source_mismatches(self):
        loaded, required = self.prepare_loaded_baseline()
        verify = lambda rows: baseline.verify_baseline_loaded_assemblies(
            rows, self.project, self.cache, "net8.0", "3.8.4", required_assemblies=required,
            policy=self.policy,
        )

        wrong_assembly_version = json.loads(json.dumps(loaded))
        wrong_assembly_version[0]["version"] = "3.9.0.0"
        wrong_assembly_version[0]["fullName"] = wrong_assembly_version[0]["fullName"].replace(
            "Version=3.8.4.0", "Version=3.9.0.0"
        )
        with self.assertRaisesRegex(RuntimeError, "release/source identity mismatch"):
            verify(wrong_assembly_version)

        wrong_source = json.loads(json.dumps(loaded))
        wrong_source[0]["informationalVersion"] = "3.8.4+" + "0" * 40
        with self.assertRaisesRegex(RuntimeError, "release/source identity mismatch"):
            verify(wrong_source)

    def test_loaded_assembly_verifier_rejects_unsafe_assets_and_symlinked_outputs(self):
        loaded, required = self.prepare_loaded_baseline()
        assets_path = self.project.parent / "obj" / "project.assets.json"
        assets = json.loads(assets_path.read_text(encoding="utf-8"))
        assets["targets"]["net8.0"]["Elsa/3.8.4"]["runtime"] = {"../Elsa.dll": {}}
        assets_path.write_text(json.dumps(assets), encoding="utf-8")
        with self.assertRaisesRegex(RuntimeError, "Unsafe runtime asset path"):
            baseline.verify_baseline_loaded_assemblies(
                loaded, self.project, self.cache, "net8.0", "3.8.4", required_assemblies=required,
                policy=self.policy,
            )

        self.write_restore({
            "Elsa/3.8.4": {"type": "package", "runtime": {"lib/net8.0/Elsa.dll": {}}},
            "Elsa.WorkflowContexts/3.8.4": {"type": "package", "runtime": {"lib/net8.0/Elsa.WorkflowContexts.dll": {}}},
            "Elsa.Studio/3.8.4": {"type": "package", "runtime": {"lib/net8.0/Elsa.Studio.dll": {}}},
        })
        output = Path(loaded[0]["location"])
        external = self.root / "outside.dll"
        external.write_bytes(output.read_bytes())
        output.unlink()
        output.symlink_to(external)
        with self.assertRaisesRegex(RuntimeError, "Symlink"):
            baseline.verify_baseline_loaded_assemblies(
                loaded, self.project, self.cache, "net8.0", "3.8.4", required_assemblies=required,
                policy=self.policy,
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
        self.assertEqual(150, len(tracked["packages"]))
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

    def test_signed_package_uses_raw_archive_sidecar_and_unsigned_restore_content_hash(self):
        unsigned_content_hash = base64.b64encode(hashlib.sha512(b"NuGet unsigned package content").digest()).decode("ascii")
        self.elsa_bytes = self.write_archive(
            "Elsa", "3.8.4", self.elsa_nuspec_repository,
            metadata_content_hash=unsigned_content_hash, signature=b"fixture signature bytes",
        )
        raw_archive_sha512 = base64.b64encode(hashlib.sha512(self.elsa_bytes).digest()).decode("ascii")
        self.record["archive_sha256"] = hashlib.sha256(self.elsa_bytes).hexdigest()
        self.write_restore({
            "Elsa/3.8.4": {"type": "package"},
            "Newtonsoft.Json/13.0.3": {"type": "package"},
        })

        receipt = self.verify()
        elsa = next(package for package in receipt["packages"] if package["id"] == "Elsa")
        self.assertNotEqual(raw_archive_sha512, unsigned_content_hash)
        self.assertEqual(hashlib.sha512(self.elsa_bytes).hexdigest(), elsa["archive_sha512"])
        self.assertEqual(unsigned_content_hash, elsa["nuget_content_hash"])
        self.assertEqual(unsigned_content_hash, elsa["restore_library_sha512"])

    def test_rejects_malformed_or_restore_mismatched_unsigned_content_hash(self):
        metadata = self.cache / "elsa" / "3.8.4" / ".nupkg.metadata"
        value = json.loads(metadata.read_text(encoding="utf-8"))
        value["contentHash"] = "not-base64"
        metadata.write_text(json.dumps(value), encoding="utf-8")
        with self.assertRaisesRegex(RuntimeError, "canonical SHA-512 base64"):
            self.verify()

        self.write_archive("Elsa", "3.8.4", self.elsa_nuspec_repository)
        self.record["archive_sha256"] = hashlib.sha256(
            (self.cache / "elsa" / "3.8.4" / "elsa.3.8.4.nupkg").read_bytes()
        ).hexdigest()
        assets_path = self.write_restore({
            "Elsa/3.8.4": {"type": "package"},
            "Newtonsoft.Json/13.0.3": {"type": "package"},
        })
        assets = json.loads(assets_path.read_text(encoding="utf-8"))
        assets["libraries"]["Elsa/3.8.4"]["sha512"] = base64.b64encode(b"x" * 64).decode("ascii")
        assets_path.write_text(json.dumps(assets), encoding="utf-8")
        with self.assertRaisesRegex(RuntimeError, "differs from the restored library SHA-512"):
            self.verify()

    def test_requires_a_restore_library_sha512_for_each_cached_package(self):
        assets_path = self.write_restore({
            "Elsa/3.8.4": {"type": "package"},
            "Newtonsoft.Json/13.0.3": {"type": "package"},
        })
        assets = json.loads(assets_path.read_text(encoding="utf-8"))
        del assets["libraries"]["Elsa/3.8.4"]["sha512"]
        assets_path.write_text(json.dumps(assets), encoding="utf-8")

        with self.assertRaisesRegex(RuntimeError, "restore library is missing its SHA-512"):
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
        self.write_restore({
            "Elsa/3.8.4": {"type": "package"},
            "Newtonsoft.Json/13.0.3": {"type": "package"},
        })
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
