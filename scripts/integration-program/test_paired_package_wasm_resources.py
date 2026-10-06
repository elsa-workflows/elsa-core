"""Synthetic binaries exercise mapping contracts, never production approvals."""
import base64
import copy
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
from zipfile import ZipFile

import paired_package_wasm_resources as wasm
import paired_package_provenance as provenance
import verify_browser_package_resources as resources
from test_verify_browser_package_resources import binary_fixture


class WasmInventoryContracts(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.pe, self.body, self.converter, self.policy = binary_fixture(official_wrapper=True)
        self.prepare()

    def prepare(self, framework="net10.0"):
        self.project = self.root / framework / "projects/wasm/Elsa.Studio.Host.Wasm.csproj"
        self.project.parent.mkdir(parents=True, exist_ok=True)
        self.project.write_text('<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly"><PropertyGroup>'
                                f'<TargetFramework>{framework}</TargetFramework><IsPackable>false</IsPackable>'
                                '</PropertyGroup></Project>')
        self.layout = SimpleNamespace(request=SimpleNamespace(framework=framework, version="3.10.0", route_prefix=""),
                                      project_paths={"wasm": self.project}, packages_root=self.root / "cache")
        self.owned, target, libraries, self.archives = {}, {}, {}, {}
        self.rows = []
        for name in sorted(wasm.MANDATORY_ASSEMBLIES | {"Elsa.Studio.Optional"}):
            package = {"id": name, "version": "3.10.0"}
            member = f"lib/{framework}/{name}.dll"
            key = name + "/3.10.0"
            target[key] = {"type": "package", "runtime": {member: {}}}
            libraries[key] = {"type": "package", "path": name.lower() + "/3.10.0"}
            self.owned[(name.casefold(), "3.10.0")] = package
            archive = self.root / framework / (name + ".nupkg")
            with ZipFile(archive, "w") as zipped:
                zipped.writestr(member, self.pe)
            self.archives[name] = archive
            cached = self.layout.packages_root / name.lower() / "3.10.0" / member
            cached.parent.mkdir(parents=True, exist_ok=True)
            cached.write_bytes(self.pe)
            self.rows.append(self.row(name, framework))
        self.rows.append(self.row(self.project.stem, framework))
        fixture = self.project.parent / "bin/Release" / framework / (self.project.stem + ".dll")
        fixture.write_bytes(self.pe)
        self.asset_file = self.project.parent / "obj/project.assets.json"
        self.assets = {"targets": {framework: copy.deepcopy(target), framework + "/browser-wasm": target}, "libraries": libraries}
        self.write_assets()
        self.manifest = self.project.parent / "obj/Release" / framework / "staticwebassets.build.json"
        self.build = {"Version": 1, "Source": self.project.stem, "Mode": "Root", "ManifestType": "Build", "BasePath": "/", "Assets": self.rows}
        self.write_manifest()

    def row(self, name, framework):
        fingerprint = "0hfatihs5g"
        served = self.project.parent / "bin/Release" / framework / "wwwroot/_framework" / f"{name}.{fingerprint}.wasm"
        generated = self.project.parent / "obj/Release" / framework / "webcil" / f"{name}.wasm"
        for path in (served, generated):
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(self.body)
        return {"RelativePath": f"_framework/{name}#[.{{fingerprint}}]!.wasm", "Fingerprint": fingerprint,
                "Identity": str(served), "OriginalItemSpec": str(generated), "ContentRoot": str(served.parent.parent),
                "SourceId": self.project.stem, "SourceType": "Computed", "BasePath": "/", "AssetKind": "Build",
                "AssetMode": "All", "AssetRole": "Primary", "AssetTraitName": "WasmResource", "AssetTraitValue": "runtime",
                "RelatedAsset": "", "FileLength": len(self.body),
                "Integrity": base64.b64encode(bytes.fromhex(resources.sha256(self.body))).decode()}

    def write_assets(self):
        self.asset_file.parent.mkdir(parents=True, exist_ok=True)
        self.asset_file.write_text(json.dumps(self.assets))
        self.receipt = {"project_assets_sha256": provenance.sha256(self.asset_file)}

    def write_manifest(self):
        self.manifest.write_text(json.dumps(self.build))

    def derive(self, *, policy=True):
        return wasm._derive(self.layout, self.project, self.manifest, self.owned, self.receipt,
                            lambda package: (self.archives[package["id"]], provenance.sha256(self.archives[package["id"]])),
                            converter=self.converter, route_prefix=self.layout.request.route_prefix,
                            _test_policy=self.policy if policy else None)

    def test_package_and_fixture_bijection_all_frameworks_and_browser_contract(self):
        for framework in ("net8.0", "net9.0", "net10.0"):
            with self.subTest(framework=framework):
                if framework != "net10.0":
                    self.prepare(framework)
                self.layout.request.route_prefix = "/compat"
                result = self.derive()
                self.assertEqual(4, result["package_runtime_count"])
                self.assertEqual(5, len(result["assets"]))
                self.assertEqual({"fixture", "package"}, {x["owner"] for x in result["assets"]})
                self.assertNotIn(str(self.root), json.dumps(result))
                fetched = [{**x, "status": 200, "requested": True} for x in result["assets"]]
                self.assertEqual(5, resources.verify_browser_resources(result["assets"], fetched, route_prefix="/compat")["requested"])
                self.assertTrue(all(x["original_pe_sha256"] == resources.sha256(self.pe) for x in result["conversions"]))

    def test_nonmandatory_runtime_is_bound_and_thirdparty_scope_is_explicit(self):
        self.rows.append(self.row("System.SomeRuntime", "net10.0"))
        self.write_manifest()
        result = self.derive()
        self.assertEqual(["System.SomeRuntime"], result["outside_package_fixture_scope"])
        self.assertTrue(any(x["assembly"] == "Elsa.Studio.Optional" for x in result["conversions"]))
        self.assertFalse(next(x for x in result["assets"] if "/Elsa.Studio.Optional." in x["path"])["required"])
        mandatory = [x for x in result["assets"] if x["required"]]
        fetched = [{**x, "status": 200, "requested": True} for x in mandatory]
        self.assertEqual(4, resources.verify_browser_resources(result["assets"], fetched, require_all=False)["requested"])
        self.build["Assets"] = [x for x in self.rows if "Elsa.Studio.Optional" not in x["RelativePath"]]
        self.write_manifest()
        with self.assertRaisesRegex(ValueError, "Missing package/fixture"):
            self.derive()

    def test_case_ambiguity_and_manifest_schema_changes_fail_closed(self):
        original = copy.deepcopy(self.build)
        for change in ("case", "duplicate_case", "version", "mode"):
            with self.subTest(change=change):
                self.build = copy.deepcopy(original)
                if change in ("case", "duplicate_case"):
                    row = copy.deepcopy(self.build["Assets"][0])
                    row["RelativePath"] = row["RelativePath"].lower()
                    if change == "case":
                        self.build["Assets"][0] = row
                    else:
                        self.build["Assets"].append(row)
                else:
                    self.build["Version" if change == "version" else "Mode"] = 2 if change == "version" else "Reference"
                self.write_manifest()
                with self.assertRaises(ValueError):
                    self.derive()

    def test_production_converter_policy_stays_fail_closed(self):
        with self.assertRaisesRegex(ValueError, "Unreviewed"):
            self.derive(policy=False)

    def test_each_manifest_mapping_field_is_bound(self):
        original = copy.deepcopy(self.rows[0])
        changes = {"SourceId": "other", "SourceType": "Package", "BasePath": "_content/other", "AssetMode": "Reference",
                   "Fingerprint": "../../oops", "Integrity": "a" * 44, "FileLength": True,
                   "RelativePath": "_framework/other.wasm", "Identity": self.rows[1]["Identity"],
                   "OriginalItemSpec": self.rows[1]["OriginalItemSpec"], "RelatedAsset": "other", "ContentRoot": str(self.root)}
        for field, value in changes.items():
            with self.subTest(field=field):
                self.rows[0] = {**original, field: value}
                self.write_manifest()
                with self.assertRaises((ValueError, RuntimeError)):
                    self.derive()
        self.rows[0] = original

    def test_missing_duplicate_and_unknown_elsa_resources_reject(self):
        original = copy.deepcopy(self.rows)
        for rows in (original[1:], original + [original[0]], original + [self.row("Elsa.Unknown", "net10.0")]):
            self.build["Assets"] = rows
            self.write_manifest()
            with self.assertRaises(ValueError):
                self.derive()

    def test_unselected_or_ambiguous_runtime_maps_reject(self):
        original = copy.deepcopy(self.assets)
        key = next(iter(self.assets["targets"]["net10.0/browser-wasm"]))
        for case in ("rid", "collision", "missing", "unsafe", "source", "library_path"):
            with self.subTest(case=case):
                self.assets = copy.deepcopy(original)
                target = self.assets["targets"]["net10.0/browser-wasm"]
                if case == "rid":
                    target[key]["runtimeTargets"] = {"runtimes/browser-wasm/lib/net10.0/other.dll": {}}
                elif case == "collision":
                    target["ThirdParty/1.0.0"] = copy.deepcopy(target[key])
                elif case == "missing":
                    del target[key]
                elif case == "unsafe":
                    target[key]["runtime"] = {"../private.dll": {}}
                elif case == "source":
                    target[key]["type"] = "project"
                else:
                    self.assets["libraries"][key]["path"] = "../private"
                self.write_assets()
                with self.assertRaises((ValueError, RuntimeError)):
                    self.derive()

    def test_archive_cache_generated_served_and_fixture_pe_mutations_reject(self):
        package = next(iter(self.owned.values()))
        member = f'lib/net10.0/{package["id"]}.dll'
        cached = self.layout.packages_root / package["id"].lower() / "3.10.0" / member
        fixture = self.project.parent / "bin/Release/net10.0" / (self.project.stem + ".dll")
        for path in (cached, Path(self.rows[0]["OriginalItemSpec"]), Path(self.rows[0]["Identity"]), fixture):
            with self.subTest(path=path.name):
                before = path.read_bytes()
                path.write_bytes(before[:-1] + bytes([before[-1] ^ 1]))
                with self.assertRaises(ValueError):
                    self.derive()
                path.write_bytes(before)
        archive = self.archives[package["id"]]
        digest = provenance.sha256(archive)
        archive.write_bytes(b"changed")
        with self.assertRaisesRegex(ValueError, "archive changed"):
            wasm._derive(self.layout, self.project, self.manifest, self.owned, self.receipt,
                         lambda entry: (self.archives[entry["id"]], digest if entry == package else provenance.sha256(self.archives[entry["id"]])),
                         converter=self.converter, route_prefix="", _test_policy=self.policy)

    def test_symlinked_output_cache_and_manifest_reject(self):
        package = next(iter(self.owned.values()))
        cached = self.layout.packages_root / package["id"].lower() / "3.10.0/lib/net10.0" / (package["id"] + ".dll")
        for path in (cached, Path(self.rows[0]["Identity"]), self.manifest):
            with self.subTest(path=path.name):
                body = path.read_bytes()
                other = path.with_name(path.name + ".real")
                path.rename(other)
                path.symlink_to(other)
                with self.assertRaisesRegex(RuntimeError, "Symlink"):
                    self.derive()
                path.unlink()
                other.rename(path)
                self.assertEqual(body, path.read_bytes())

    def test_changed_assets_and_wrong_release_manifest_reject(self):
        self.asset_file.write_text(self.asset_file.read_text() + " ")
        with self.assertRaisesRegex(ValueError, "assets changed"):
            self.derive()
        self.write_assets()
        debug = self.manifest.with_name("other.json")
        debug.write_bytes(self.manifest.read_bytes())
        with self.assertRaisesRegex(ValueError, "exact Release"):
            wasm._derive(self.layout, self.project, debug, self.owned, self.receipt,
                         lambda entry: (self.archives[entry["id"]], provenance.sha256(self.archives[entry["id"]])),
                         converter=self.converter, route_prefix="", _test_policy=self.policy)

    def test_public_baseline_wrapper_validates_original_authority_before_mapping(self):
        with patch.object(wasm.baseline, "_read_policy", return_value={"schema": 1}), \
             patch.object(wasm.baseline, "validate_baseline_project", side_effect=RuntimeError("unapproved source")) as validator, \
             patch.object(wasm, "_derive") as mapper:
            with self.assertRaisesRegex(RuntimeError, "unapproved source"):
                wasm.derive_baseline_wasm_resources(self.layout, self.project, self.manifest, converter=self.converter)
            validator.assert_called_once()
            mapper.assert_not_called()

    def test_candidate_manifest_mismatch_rejects_before_project_validation(self):
        verified = self.root / "verified"
        verified.mkdir()
        (verified / "verified-artifacts.json").write_text('{}')
        with patch.object(wasm.provenance, "candidate_project_validator") as validator:
            with self.assertRaisesRegex(ValueError, "manifest changed"):
                wasm.derive_candidate_wasm_resources(self.layout, self.project, self.manifest, verified,
                                                     verified_manifest_sha256="a" * 64, converter=self.converter)
            validator.assert_not_called()


if __name__ == "__main__":
    unittest.main()
