"""Synthetic manifest contracts; these do not run MSBuild or a browser."""
import base64
import copy
from dataclasses import replace
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from zipfile import ZipFile

import consolidated_candidate_input as candidate
import materialize_paired_package_hosts as hosts
import paired_package_baseline_provenance as baseline
import paired_package_hosted_assets as inherited
from paired_package_baseline_resources import derive_baseline_resources
import prove_consolidated_package_consumers as packages
import verify_browser_package_resources as resources


class HostedFixture:
    def __init__(self, root, version="3.10.0", framework="net10.0"):
        self.root, self.version = root.resolve(), version
        group = self.root / "group"
        self.layout = hosts.materialize(hosts.CellRequest("hosted-wasm", framework, version), group,
            nuget_config="<configuration />", packages_root=group / "packages", sdk="10.0.300")
        self.client = self.layout.project_paths["wasm"].parent
        self.build = self.layout.project_paths["hosted-wasm"].parent / "obj/Release" / framework / "staticwebassets.build.json"
        self.client_build = self.client / "obj/Release" / framework / "staticwebassets.build.json"
        self.client_build.parent.mkdir(parents=True)
        self.build.parent.mkdir(parents=True)
        output = self.client / "bin/Release" / framework
        generated = output / "wwwroot/_framework/dotnet.js"
        generated.parent.mkdir(parents=True)
        generated.write_bytes(b"synthetic completed build output")
        bundle = self.client_build.parent / "scopedcss/bundle" / (inherited.CLIENT_SOURCE + ".styles.css")
        bundle.parent.mkdir(parents=True)
        bundle.write_bytes(b"synthetic generated CSS bundle")
        self.own = [self.row(self.client / "wwwroot/index.html", "Discovered", self.client / "wwwroot", "index.html"),
                    self.row(generated, "Computed", output / "wwwroot", "_framework/dotnet.js"),
                    self.row(bundle, "Computed", bundle.parent, inherited.CLIENT_SOURCE + ".styles.css")]
        self.inherited = [{**row, "SourceType": "Project", "OriginalItemSpec": str(Path(row["Identity"]))}
                          for row in self.own]
        self.restored = self.client / "obj/project.assets.json"
        self.restored.write_text("{}")
        self.stamp = self.client / "build-reuse.json"
        self.stamp.write_text(json.dumps({"schema": 1,
            "inputs": {name: value for name, value in self.layout.input_hashes.items() if name.startswith("projects/wasm/")},
            "assets": resources.sha256(self.restored.read_bytes()),
            "generated_static_assets": hosts._generated_static_assets(self.layout, "wasm", self.layout.project_paths["wasm"]),
            "outputs": {path.relative_to(output).as_posix(): resources.sha256(path.read_bytes())
                        for path in output.rglob("*") if path.is_file()}}))
        self.required = json.loads(resources.CONVERTER_POLICY.with_name("coverage-policy.json").read_text())["required_browser_assets"]
        self.package_rows = []
        self.policy = {**json.loads(baseline.POLICY_PATH.read_text()), "packages": []}
        self.manifest = {"version": version, "source_commit": candidate.SOURCE, "packages": [],
            "external_package_exceptions": {name: "reviewed" for name in packages.AUDITED_EXTERNAL_ELSA_IDS}}
        artifacts = self.root / "artifacts"
        artifacts.mkdir()
        ids = {path.split("/")[2] for path in self.required}
        if version == "3.10.0":
            ids |= set(packages.REQUIRED_PACKAGES)
        for package_id in sorted(ids):
            package_root = self.layout.packages_root / package_id.lower() / version
            package_root.mkdir(parents=True)
            archive = package_root / f"{package_id.lower()}.{version}.nupkg"
            pins = []
            repository = {"type": "git", "url": baseline.REPOSITORIES["elsa-studio"],
                          "commit": baseline.RELEASE_COMMITS["elsa-studio"].get(version, "a" * 40)}
            with ZipFile(archive, "w") as zipped:
                attrs = " ".join(f'{key}="{value}"' for key, value in repository.items())
                zipped.writestr(package_id + ".nuspec", f"<package><metadata><id>{package_id}</id><version>{version}</version><repository {attrs}/></metadata></package>")
                for path in self.required:
                    if path.split("/")[2] != package_id:
                        continue
                    relative = path.rsplit("/", 1)[1]
                    member, body = "staticwebassets/" + relative, relative.encode()
                    actual = package_root / member
                    actual.parent.mkdir(exist_ok=True)
                    actual.write_bytes(body)
                    zipped.writestr(member, body)
                    pins.append({"package_path": member, "sha256": resources.sha256(body)})
                    self.package_rows.append({"SourceId": package_id, "SourceType": "Package", "BasePath": "_content/" + package_id,
                        "RelativePath": relative, "Identity": str(actual), "AssetRole": "Primary", "FileLength": len(body)})
            content = archive.read_bytes()
            original = artifacts / archive.name
            original.write_bytes(content)
            self.manifest["packages"].append({"id": package_id, "frameworks": list(packages.FRAMEWORKS),
                "nupkg": archive.name, "nupkg_sha256": resources.sha256(content), "browser_assets": pins})
            sha512 = baseline.package_consumer.base64_sha512(content)
            archive.with_suffix(".nupkg.sha512").write_text(sha512)
            (package_root / ".nupkg.metadata").write_text(json.dumps({"source": baseline.NUGET_ORG, "contentHash": sha512}))
            self.policy["packages"].append({"id": package_id, "version": version, "owner": "elsa-studio",
                "repository": repository, "archive_sha256": resources.sha256(content)})
        self.verified = self.root / "verified-artifacts.json"
        self.verified.write_text(json.dumps(self.manifest))
        self.write()

    def row(self, path, source_type, content_root, relative):
        body = path.read_bytes()
        return {"Identity": str(path), "SourceId": inherited.CLIENT_SOURCE, "SourceType": source_type,
            "ContentRoot": str(content_root) + "/", "OriginalItemSpec": path.relative_to(self.client).as_posix(),
            "RelativePath": relative, "BasePath": "/", "AssetKind": "All", "AssetMode": "All", "AssetRole": "Primary",
            "AssetTraitName": "", "AssetTraitValue": "", "RelatedAsset": "", "Fingerprint": "synthetic",
            "Integrity": base64.b64encode(hashlib.sha256(body).digest()).decode(), "FileLength": len(body),
            "CopyToOutputDirectory": "PreserveNewest", "CopyToPublishDirectory": "Never"}

    def write(self):
        self.client_build.write_text(json.dumps({"Version": 1, "Source": inherited.CLIENT_SOURCE, "Mode": "Root",
            "ManifestType": "Build", "BasePath": "/", "Assets": self.own}))
        self.build.write_text(json.dumps({"Version": 1, "Source": inherited.WRAPPER_SOURCE, "ManifestType": "Build",
            "Assets": self.package_rows + self.inherited}))

    def verify(self, **kwargs):
        options = {"hosted_layout": self.layout, **kwargs}
        if self.version == "3.10.0":
            return resources.derive_candidate_resources(self.build, self.root, self.layout.packages_root,
                verified_manifest_sha256=resources.sha256(self.verified.read_bytes()), **options)
        return derive_baseline_resources(self.build, self.layout.packages_root, self.version, policy=self.policy, **options)


class HostedAssetContracts(unittest.TestCase):
    def fixture(self, **kwargs):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        return HostedFixture(Path(temporary.name), **kwargs)

    def test_candidate_and_both_released_wrappers_preserve_package_authority(self):
        for version in ("3.8.4", "3.9.0", "3.10.0"):
            with self.subTest(version=version):
                fixture = self.fixture(version=version)
                receipt = fixture.verify()
                self.assertEqual(6, len(receipt["assets"]))
                self.assertTrue(all(row["owner"] == "package" for row in receipt["assets"]))
                rows = receipt["nonpackage_build_assets"]
                self.assertEqual(3, len(rows))
                self.assertEqual({inherited.CLIENT_SOURCE}, {row["source_id"] for row in rows})
                self.assertEqual({"Project"}, {row["source_type"] for row in rows})
                self.assertEqual({"Discovered", "Computed"}, {row["origin_source_type"] for row in rows})
                self.assertNotIn(str(fixture.root), json.dumps(receipt))
                for row in rows:
                    self.assertTrue(all(len(value) == 64 and set(value) <= set("0123456789abcdef")
                        for key, value in row.items() if key.endswith("sha256")))

    def test_hosted_proof_is_required_and_not_available_to_standalone_hosts(self):
        fixture = self.fixture()
        with self.assertRaisesRegex(ValueError, "Unowned Elsa"):
            fixture.verify(hosted_layout=None)
        for host in ("server", "wasm", "custom-elements"):
            with self.subTest(host=host), self.assertRaisesRegex(ValueError, "aligned Hosted"):
                fixture.verify(hosted_layout=replace(fixture.layout, request=hosts.CellRequest(host, "net10.0", "3.10.0")))

    def test_unknown_sibling_source_and_client_metadata_are_rejected(self):
        for field, value in (("SourceId", "Elsa.Studio.Host.Sibling"), ("SourceType", "Framework"),
            ("BasePath", "_content/Elsa.Sibling"), ("RelativePath", "../outside.js"), ("FileLength", 1),
            ("Integrity", "forged"), ("AssetMode", "Reference"), ("AssetTraitValue", "unknown")):
            with self.subTest(field=field):
                fixture = self.fixture()
                fixture.inherited[1][field] = value
                fixture.write()
                with self.assertRaises(ValueError):
                    fixture.verify()

    def test_exact_project_edge_and_current_pinned_inputs_are_required(self):
        for mutation in ("edge", "project", "input", "runtime-config", "stamp", "restored", "missing-pin", "extra-pin", "generated-pin"):
            with self.subTest(mutation=mutation):
                fixture = self.fixture()
                if mutation in ("edge", "project"):
                    project = fixture.layout.project_paths["hosted-wasm" if mutation == "edge" else "wasm"]
                    project.write_text(project.read_text().replace("../wasm/", "../sibling/") if mutation == "edge" else project.read_text() + "\n")
                elif mutation in ("input", "runtime-config"):
                    (fixture.client / ("Program.cs" if mutation == "input" else "wwwroot/appsettings.json")).write_bytes(b"changed")
                elif mutation == "stamp":
                    fixture.stamp.write_text("{}")
                elif mutation == "generated-pin":
                    stamp = json.loads(fixture.stamp.read_text())
                    del stamp["generated_static_assets"]
                    fixture.stamp.write_text(json.dumps(stamp))
                elif mutation in ("missing-pin", "extra-pin"):
                    pins = dict(fixture.layout.input_hashes)
                    if mutation == "missing-pin":
                        del pins["projects/wasm/Program.cs"]
                    else:
                        pins["projects/wasm/arbitrary.js"] = "a" * 64
                    fixture.layout = replace(fixture.layout, input_hashes=pins)
                else:
                    fixture.restored.write_text('{"changed":true}')
                with self.assertRaises(ValueError):
                    fixture.verify()

    def test_unmatched_identity_original_path_and_symlink_are_rejected(self):
        for mutation in ("sibling", "original", "symlink", "content-root", "duplicate", "client-duplicate"):
            with self.subTest(mutation=mutation):
                fixture = self.fixture()
                if mutation in ("sibling", "symlink"):
                    path = fixture.root / "outside.js"
                    path.write_bytes(Path(fixture.own[1]["Identity"]).read_bytes())
                    if mutation == "symlink":
                        linked = Path(fixture.own[1]["Identity"])
                        linked.unlink()
                        linked.symlink_to(path)
                    else:
                        fixture.inherited[1]["Identity"] = str(path)
                elif mutation == "original":
                    fixture.inherited[1]["OriginalItemSpec"] = fixture.own[0]["Identity"]
                elif mutation == "content-root":
                    fixture.inherited[1]["ContentRoot"] = str(fixture.root)
                elif mutation == "duplicate":
                    fixture.inherited.append(copy.deepcopy(fixture.inherited[1]))
                else:
                    fixture.own.append(copy.deepcopy(fixture.own[1]))
                fixture.write()
                with self.assertRaises((ValueError, RuntimeError)):
                    fixture.verify()

    def test_changed_bytes_and_rewritten_build_output_hash_are_rejected(self):
        for index, rewritten in ((1, False), (1, True), (2, True)):
            with self.subTest(index=index, rewritten=rewritten):
                fixture = self.fixture()
                path = Path(fixture.own[index]["Identity"])
                path.write_bytes(b"changed output")
                if rewritten:
                    for row in (fixture.own[index], fixture.inherited[index]):
                        row.update(FileLength=path.stat().st_size, Integrity=base64.b64encode(hashlib.sha256(path.read_bytes()).digest()).decode())
                fixture.write()
                with self.assertRaises(ValueError):
                    fixture.verify()

    def test_arbitrary_computed_obj_file_and_unpinned_discovered_file_are_rejected(self):
        for kind in ("Computed", "Discovered"):
            with self.subTest(kind=kind):
                fixture = self.fixture()
                root = fixture.client_build.parent if kind == "Computed" else fixture.client / "wwwroot"
                path = root / "arbitrary.js"
                path.write_bytes(b"unapproved")
                row = fixture.row(path, kind, root, "arbitrary.js")
                fixture.own.append(row)
                fixture.inherited.append({**row, "SourceType": "Project"})
                fixture.write()
                with self.assertRaises(ValueError):
                    fixture.verify()

    def test_inherited_fixture_cannot_replace_mandatory_package_asset(self):
        fixture = self.fixture()
        fixture.package_rows.pop()
        fixture.write()
        with self.assertRaisesRegex(ValueError, "Missing mandatory"):
            fixture.verify()

    def test_wrapper_and_client_manifest_identity_are_bound(self):
        for target, field, value in (("wrapper", "Source", inherited.CLIENT_SOURCE),
            ("wrapper", "ManifestType", "Publish"), ("client", "Source", "Elsa.Studio.Host.Sibling"),
            ("client", "Mode", "Default"), ("client", "BasePath", "_content/client")):
            with self.subTest(target=target, field=field):
                fixture = self.fixture()
                path = fixture.build if target == "wrapper" else fixture.client_build
                manifest = json.loads(path.read_text())
                manifest[field] = value
                path.write_text(json.dumps(manifest))
                with self.assertRaises(ValueError):
                    fixture.verify()

    def test_manifest_hash_binding_cannot_change_during_derivation(self):
        fixture = self.fixture()
        proof = inherited.HostedClientAssets(fixture.layout, fixture.build,
            json.loads(fixture.build.read_text()), fixture.version)
        fixture.client_build.write_text(fixture.client_build.read_text() + "\n")
        with self.assertRaisesRegex(ValueError, "manifests changed"):
            proof.verify_unchanged()


if __name__ == "__main__":
    unittest.main()
