"""Synthetic bootstrap inventory/receipt contracts, not browser acceptance proof."""
import base64
import copy
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

import materialize_paired_package_hosts as hosts
import paired_package_wasm_boot as boot
import verify_browser_package_resources as resources


def sri(digest):
    return "sha256-" + base64.b64encode(bytes.fromhex(digest)).decode("ascii")


def fixture_resources(framework="net10.0"):
    policy = boot.boot_policy(framework)
    platform = [{"path": "/_framework/" + role["served"].replace("{fingerprint}", "a" * 10),
                 "sha256": resources.sha256(role["role"].encode()), "bytes": 8,
                 "content_type": role["content_type"], "owner": "platform", "required": True}
                for role in policy["platform"]]
    managed = [{"path": "/_framework/" + name + ("" if framework == "net8.0" else "." + "b" * 10) + ".wasm", "sha256": resources.sha256(name.encode()),
                "bytes": 8, "content_type": "application/wasm", "owner": "fixture" if name == policy["main_assembly"] else "package",
                "required": name != "Elsa.Studio.Optional"}
               for name in policy["mandatory_managed"] + [policy["main_assembly"], "Elsa.Studio.Optional"]]
    return platform, managed


def fixture_configuration(platform, managed, framework="net10.0"):
    policy = boot.boot_policy(framework)
    config = dict.fromkeys(policy["configuration_fields"])
    config["mainAssemblyName"] = policy["main_assembly"]
    listed = dict.fromkeys(policy["resource_groups"])
    config["resources"] = listed
    listed["assembly"] = [{"name": row["path"].removeprefix("/_framework/"),
                           "virtualPath": boot.managed_path(framework).fullmatch(row["path"])[1] + ".wasm",
                           "hash": sri(row["sha256"]), "cache": "force-cache"} for row in managed]
    listed["coreAssembly"] = [{"name": "System.Core.aaaaaaaaaa.wasm", "virtualPath": "System.Core.wasm",
                               "hash": sri("c" * 64), "cache": "force-cache"}]
    for role in policy["platform"]:
        if "group" in role:
            resource = next(row for row in platform if boot.platform_role(row["path"], framework) == role["role"])
            row = {"name": resource["path"].removeprefix("/_framework/")}
            if role["group"] == "wasmNative":
                row.update(hash=sri(resource["sha256"]), cache="force-cache")
            listed[role["group"]] = [row]
    if framework != "net10.0":
        config.update(debugLevel=0, globalizationMode="sharded")
        listed["hash"] = sri("d" * 64)
        listed["assembly"] = {row["name"]: row["hash"] for row in listed["assembly"]}
        listed["coreAssembly"] = {} if framework == "net8.0" else {row["name"]: row["hash"] for row in listed["coreAssembly"]}
        for role in policy["platform"]:
            if "group" in role:
                resource = next(row for row in platform if boot.platform_role(row["path"], framework) == role["role"])
                listed[role["group"]] = {resource["path"].removeprefix("/_framework/"): sri(resource["sha256"])}
        if framework == "net9.0":
            listed["fingerprinting"] = {row["path"].removeprefix("/_framework/"):
                boot.managed_path(framework).fullmatch(row["path"])[1] + ".wasm" for row in managed}
            listed["fingerprinting"].update({row["path"].removeprefix("/_framework/"): role["virtual"]
                for role in policy["platform"] if "group" in role for row in platform
                if boot.platform_role(row["path"], framework) == role["role"]})
            listed["fingerprinting"]["System.Core.aaaaaaaaaa.wasm"] = "System.Core.wasm"
    return config


def script(config, framework="net10.0"):
    if framework != "net10.0":
        return json.dumps(config).encode()
    # Literal JS outside the JSON is intentionally never evaluated by either parser.
    return b'globalThis.__bootstrapMustNotExecute = true;runtime.withConfig(/*json-start*/' + json.dumps(config).encode() + b'/*json-end*/);'


def boot_receipt_fixture(framework="net10.0"):
    """Reusable format-selected fake for receipt-shape tests only; never runtime evidence."""
    policy = boot.boot_policy(framework)
    platform, managed = fixture_resources(framework)
    body = script(fixture_configuration(platform, managed, framework), framework)
    manifest = next(row for row in platform if boot.platform_role(row["path"], framework) == "manifest")
    manifest.update(sha256=resources.sha256(body), bytes=len(body))
    configuration = boot.parse_boot_configuration(body, platform, managed, framework)
    manifest["boot_configuration_sha256"] = configuration["configuration_sha256"]
    proof = {"format": policy["format"], "checks": dict.fromkeys(boot.CHECKS, True),
             "platform_bindings": [{"role": boot.platform_role(row["path"], framework), "path": row["path"], "sha256": row["sha256"]} for row in platform],
             "managed_bindings": [{key: row[key] for key in ("path", "sha256", "owner")} for row in managed],
             "bootstrap_sha256": manifest["sha256"], "configuration_sha256": configuration["configuration_sha256"]}
    expected = platform + managed
    observed = [{key: row[key] for key in ("path", "sha256", "bytes", "content_type", "owner")} | {"status": 200, "requested": True}
                for row in expected]
    return proof, expected, observed


class BootstrapFixture(unittest.TestCase):
    framework = "net10.0"
    def setUp(self):
        self.prepare(self.framework)

    def prepare(self, framework):
        policy = boot.boot_policy(framework)
        major = framework.removeprefix("net").split(".")[0]
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.project = self.root / "projects/wasm/Elsa.Studio.Host.Wasm.csproj"
        self.project.parent.mkdir(parents=True)
        self.project.write_text('<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly" />')
        self.layout = SimpleNamespace(request=hosts.CellRequest("wasm", framework, "3.10.0"),
                                      sdk=policy["sdk_version"], project_paths={"wasm": self.project}, packages_root=self.root / "packages")
        self.asset_file = self.project.parent / "obj/project.assets.json"
        self.asset_file.parent.mkdir()
        self.assets = {"libraries": {f"Microsoft.AspNetCore.Components.WebAssembly/{major}.0.3":
                       {"type": "package", "path": f"microsoft.aspnetcore.components.webassembly/{major}.0.3"}},
                       "project": {"frameworks": {framework: {"downloadDependencies": [
                       {"name": "Microsoft.NETCore.App.Runtime.Mono.browser-wasm", "version": f"[{major}.0.8, {major}.0.8]"}]}}}}
        if framework != "net10.0":
            self.assets["libraries"]["Microsoft.NET.Sdk.WebAssembly.Pack/10.0.8"] = {"type": "package", "path": "microsoft.net.sdk.webassembly.pack/10.0.8"}
        self.asset_file.write_text(json.dumps(self.assets))
        self.platform, self.managed_assets = fixture_resources(framework)
        self.config = fixture_configuration(self.platform, self.managed_assets, framework)
        self.rows, self.endpoints = [], []
        self.manifest = self.project.parent / f"obj/Release/{framework}/staticwebassets.build.json"
        self.manifest.parent.mkdir(parents=True)
        for role in policy["platform"]:
            resource = next(row for row in self.platform if boot.platform_role(row["path"], framework) == role["role"])
            body = script(self.config, framework) if role["role"] == "manifest" else role["role"].encode()
            output = self.project.parent / f"bin/Release/{framework}/wwwroot" / resource["path"].removeprefix("/")
            output.parent.mkdir(parents=True, exist_ok=True)
            output.write_bytes(body)
            resource.update(sha256=resources.sha256(body), bytes=len(body))
        # Native integrity in the embedded JSON refers to its actual source-copy bytes.
        self.config = fixture_configuration(self.platform, self.managed_assets, framework)
        for role in policy["platform"]:
            resource = next(row for row in self.platform if boot.platform_role(row["path"], framework) == role["role"])
            output = self.project.parent / f"bin/Release/{framework}/wwwroot" / resource["path"].removeprefix("/")
            if role["role"] == "manifest":
                output.write_bytes(script(self.config, framework))
            source, _ = boot._source(role, self.assets, self.layout, self.project, policy)
            source.parent.mkdir(parents=True, exist_ok=True)
            source.write_bytes(output.read_bytes())
            digest = resources.sha256(output.read_bytes())
            self.rows.append({"RelativePath": role["relative"], "SourceId": self.project.stem, "SourceType": "Computed", "BasePath": "/",
                              "AssetKind": role["kind"], "AssetMode": "All", "AssetRole": "Primary", "AssetTraitName": "WasmResource",
                              "AssetTraitValue": role["trait"], "Fingerprint": "a" * 10, "Identity": str(output),
                              "OriginalItemSpec": str(source) if "source_package" in role else role["source_member"],
                              "ContentRoot": str(output.parent.parent) + "/", "FileLength": output.stat().st_size,
                              "Integrity": sri(digest)[7:], "RelatedAsset": ""})
            self.endpoints.append({"Route": resource["path"].removeprefix("/"), "AssetFile": str(output), "Selectors": [],
                                   "ResponseHeaders": [{"Name": "Content-Type", "Value": role["content_type"]},
                                                       {"Name": "Content-Length", "Value": str(output.stat().st_size)}],
                                   "EndpointProperties": [{"Name": "integrity", "Value": sri(digest)}]})
        self.managed = {"assets": self.managed_assets, "project_assets_sha256": resources.sha256(self.asset_file.read_bytes())}
        self.write_manifest()

    def write_manifest(self):
        self.manifest.write_text(json.dumps({"Version": 1, "Source": self.project.stem, "BasePath": "/", "Mode": "Root",
                                             "ManifestType": "Build", "Assets": self.rows, "Endpoints": self.endpoints}))
        self.managed["static_asset_manifest_sha256"] = resources.sha256(self.manifest.read_bytes())

    def derive(self):
        return boot.derive_boot_resources(self.layout, self.project, self.manifest, self.managed)


class BootstrapInventoryContracts(BootstrapFixture):
    def test_selected_platform_sources_preserve_separate_managed_authority(self):
        original = copy.deepcopy(self.managed)
        result = self.derive()
        self.assertEqual(original, self.managed)
        self.assertEqual(5, len(result["assets"]))
        self.assertTrue(all(row["owner"] == "platform" and row["required"] for row in result["assets"]))
        self.assertEqual(len(self.managed_assets), result["managed_count"])
        self.assertEqual({"generated", "platform-cache"}, {row["source"]["kind"] for row in result["bindings"]})
        self.assertNotIn(str(self.root), json.dumps(result))

    def test_only_selected_framework_sdk_and_standalone_composition_are_accepted(self):
        for framework in ("net7.0", "net11.0"):
            self.layout.request = SimpleNamespace(host="wasm", framework=framework, route_prefix="")
            with self.subTest(framework=framework), self.assertRaisesRegex(ValueError, "Unreviewed"):
                self.derive()
        self.layout.request = hosts.CellRequest("wasm", "net10.0", "3.10.0")
        self.layout.sdk = "10.0.999"
        with self.assertRaisesRegex(ValueError, "Unreviewed"):
            self.derive()

    def test_manifest_source_metadata_and_delivery_mutations_reject(self):
        mutations = [lambda: self.rows[0].update(SourceType="Package"), lambda: self.rows[0].update(AssetKind="Build"),
                     lambda: self.rows[0].update(Integrity="changed"), lambda: self.rows[0].update(FileLength=True),
                     lambda: self.rows[0].update(Fingerprint="../escape"), lambda: self.rows[0].update(OriginalItemSpec="../private"),
                     lambda: self.rows[0].update(ContentRoot=str(self.root) + "/"), lambda: self.rows.append(copy.deepcopy(self.rows[0])),
                     lambda: self.rows.pop(), lambda: self.endpoints[0]["ResponseHeaders"][0].update(Value="text/html"),
                     lambda: self.endpoints[0]["EndpointProperties"][0].update(Value=sri("0" * 64)), lambda: self.endpoints.pop()]
        original_rows, original_endpoints = copy.deepcopy(self.rows), copy.deepcopy(self.endpoints)
        for index, mutate in enumerate(mutations):
            with self.subTest(mutation=index), self.assertRaises((ValueError, RuntimeError)):
                mutate(); self.write_manifest(); self.derive()
            self.rows, self.endpoints = copy.deepcopy(original_rows), copy.deepcopy(original_endpoints)
            self.write_manifest()

    def test_changed_inputs_source_copies_and_symlinked_outputs_reject(self):
        for path in (self.manifest, self.asset_file, Path(self.rows[0]["OriginalItemSpec"]), Path(self.rows[0]["Identity"])):
            raw = path.read_bytes()
            path.write_bytes(raw + b"changed")
            with self.subTest(path=path.name), self.assertRaises((ValueError, RuntimeError)):
                self.derive()
            path.write_bytes(raw)
        output = Path(self.rows[0]["Identity"])
        other = output.with_suffix(".source")
        output.rename(other); output.symlink_to(other)
        with self.assertRaisesRegex(RuntimeError, "Symlink"):
            self.derive()

    def test_resource_inventory_appends_bootstrap_without_replacing_original_bindings(self):
        import paired_package_execution as execution
        static = {"assets": [{"path": path, "owner": "package", "required": True} for path in execution.browser.POLICY["required_browser_assets"]]}
        with patch.object(execution.resources, "derive_candidate_resources", return_value=static), \
             patch.object(execution.wasm_resources, "derive_candidate_wasm_resources", return_value=self.managed):
            result = execution._resource_inventory(self.layout, Path("/verified"), "a" * 64, converter={"synthetic": True})
        self.assertEqual(self.managed_assets, [row for row in result["assets"] if row["path"].startswith("/_framework/Elsa.")])
        self.assertEqual(5, len([row for row in result["assets"] if row["owner"] == "platform"]))
        self.assertEqual(self.managed["static_asset_manifest_sha256"], result["bootstrap_resources"]["static_asset_manifest_sha256"])


class JsonBootstrapInventoryContracts(BootstrapFixture):
    def test_synthetic_json_inventories_preserve_source_manifest_and_managed_bindings(self):
        for framework in ("net8.0", "net9.0"):
            self.prepare(framework)
            with self.subTest(framework=framework):
                original = copy.deepcopy(self.managed)
                result = self.derive()
                self.assertEqual(original, self.managed)
                self.assertEqual(boot.boot_policy(framework)["format"], result["format"])
                self.assertEqual(6, len(result["assets"]))
                self.assertTrue(all(row["owner"] == "platform" for row in result["assets"]))
                manifest = next(row for row in result["assets"] if boot.platform_role(row["path"], framework) == "manifest")
                self.assertEqual("application/json", manifest["content_type"])
                self.assertEqual(manifest["sha256"], manifest["boot_configuration_sha256"])
                self.assertEqual({"generated", "platform-cache"}, {row["source"]["kind"] for row in result["bindings"]})
                self.assertTrue(all(row["source"].get("version", "").startswith(framework[3:-2] + ".0.")
                                    for row in result["bindings"] if row["source"]["kind"] == "platform-cache"))
                source = Path(self.rows[2]["OriginalItemSpec"])
                raw = source.read_bytes(); source.write_bytes(raw + b"changed")
                with self.assertRaises(ValueError):
                    self.derive()
                source.write_bytes(raw)
                original_assets = copy.deepcopy(self.assets)
                self.assets["libraries"]["Microsoft.NET.Sdk.WebAssembly.Pack/10.0.8"]["path"] = "other/10.0.8"
                self.asset_file.write_text(json.dumps(self.assets))
                self.managed["project_assets_sha256"] = resources.sha256(self.asset_file.read_bytes())
                with self.assertRaisesRegex(ValueError, "SDK task package/source"):
                    self.derive()
                self.assets = original_assets
                self.asset_file.write_text(json.dumps(self.assets))
                self.managed["project_assets_sha256"] = resources.sha256(self.asset_file.read_bytes())
                self.endpoints[1]["ResponseHeaders"][0]["Value"] = "text/javascript"
                self.write_manifest()
                with self.assertRaises(ValueError):
                    self.derive()


class BootstrapParserContracts(unittest.TestCase):
    def test_source_selected_json_formats_bind_every_original_resource(self):
        for framework in ("net8.0", "net9.0"):
            with self.subTest(framework=framework):
                platform, managed = fixture_resources(framework)
                config = fixture_configuration(platform, managed, framework)
                raw = script(config, framework)
                result = boot.parse_boot_configuration(raw, platform, managed, framework)
                self.assertEqual(boot.boot_policy(framework)["format"], result["format"])
                self.assertEqual(resources.sha256(raw), result["configuration_sha256"])
                self.assertEqual(len(managed), result["managed_count"])
                mutations = [lambda c: c.update(extra=True), lambda c: c.update(mainAssemblyName="Other.Host"),
                             lambda c: c.update(debugLevel=True), lambda c: c["resources"].update(unknown={}),
                             lambda c: c["resources"]["assembly"].pop(next(iter(c["resources"]["assembly"]))),
                             lambda c: c["resources"]["assembly"].update({"Elsa.Unknown.wasm": sri("0" * 64)}),
                             lambda c: c["resources"]["wasmNative"].update({next(iter(c["resources"]["wasmNative"])): sri("0" * 64)}),
                             lambda c: c["resources"].update(assembly=[])]
                if framework == "net9.0":
                    mutations += [lambda c: c["resources"].pop("fingerprinting"),
                                  lambda c: c["resources"]["fingerprinting"].update({next(iter(c["resources"]["assembly"])): "Other.wasm"}),
                                  lambda c: c["resources"]["fingerprinting"].update({next(iter(c["resources"]["wasmNative"])): "other.wasm"})]
                else:
                    mutations += [lambda c: c["resources"].update(fingerprinting={})]
                for index, mutate in enumerate(mutations):
                    changed = copy.deepcopy(config); mutate(changed)
                    with self.subTest(mutation=index), self.assertRaises(ValueError):
                        boot.parse_boot_configuration(script(changed, framework), platform, managed, framework)
                for wrong in ("net10.0", "net9.0" if framework == "net8.0" else "net8.0", "net11.0"):
                    with self.subTest(wrong_framework=wrong), self.assertRaises(ValueError):
                        boot.parse_boot_configuration(raw, platform, managed, wrong)
                with self.assertRaises(ValueError):
                    boot.parse_boot_configuration(raw.replace(b'"mainAssemblyName":', b'"mainAssemblyName": null, "mainAssemblyName":'), platform, managed, framework)

    def test_parse_only_known_embedded_json_and_original_resource_bindings(self):
        platform, managed = fixture_resources()
        config = fixture_configuration(platform, managed)
        self.assertEqual(len(managed), boot.parse_boot_configuration(script(config), platform, managed)["managed_count"])
        mutations = [lambda c: c.update(mainAssemblyName="Other.Host"), lambda c: c.update(extra=True),
                     lambda c: c["resources"].update(unknown=[]), lambda c: c["resources"]["wasmNative"][0].update(hash=sri("0" * 64)),
                     lambda c: c["resources"]["jsModuleNative"][0].update(name="../escape.js"),
                     lambda c: c["resources"]["assembly"][0].update(hash=sri("0" * 64)),
                     lambda c: c["resources"]["coreAssembly"][0].update(virtualPath=c["resources"]["assembly"][0]["virtualPath"]),
                     lambda c: c["resources"]["assembly"].pop(), lambda c: c["resources"]["assembly"].append(copy.deepcopy(c["resources"]["assembly"][0]))]
        for index, mutate in enumerate(mutations):
            changed = copy.deepcopy(config); mutate(changed)
            with self.subTest(mutation=index), self.assertRaises(ValueError):
                boot.parse_boot_configuration(script(changed), platform, managed)
        for raw in (b"{}", b"runtime.withConfig(/*json-start*/{}/*json-end*/);", script(config) + b"/*json-start*/",
                    b"x" * (boot.POLICY["maximum_boot_bytes"] + 1), script(config).replace(b'"mainAssemblyName":', b'"mainAssemblyName": null, "mainAssemblyName":')):
            with self.assertRaises(ValueError):
                boot.parse_boot_configuration(raw, platform, managed)


class BootstrapReceiptContracts(unittest.TestCase):
    def setUp(self):
        self.proof, self.expected, self.observed = boot_receipt_fixture()

    def validate(self, proof=None, passed=True, callback=True, observed=None):
        boot.validate_boot_receipt(proof if proof is not None else self.proof, passed,
                                   observed if observed is not None else self.observed, callback)

    def test_bound_resources_and_executed_callback_are_both_required(self):
        self.validate()
        self.proof["checks"]["managed_callback"] = False
        self.validate(passed=False, callback=False)
        with self.assertRaises(ValueError):
            self.validate(passed=True, callback=False)
        self.proof["checks"]["platform_resources"] = self.proof["checks"]["managed_resources"] = False
        self.proof["checks"]["configuration"] = False
        self.validate(passed=False, callback=False, observed=[])

    def test_json_receipts_are_selected_by_framework_not_relabelled_net10_proof(self):
        for framework in ("net8.0", "net9.0"):
            proof, expected, observed = boot_receipt_fixture(framework)
            record = {"host": "wasm", "framework": framework, "proof": {"wasm_boot": proof}, "resources": observed}
            with self.subTest(framework=framework):
                boot.validate_boot_receipt(proof, True, observed, True, framework)
                boot.validate_boot_request_binding(record, expected, framework)
                with self.assertRaises(ValueError):
                    boot.validate_boot_receipt(proof, True, observed, False, framework)
                with self.assertRaises(ValueError):
                    boot.validate_boot_receipt(proof, True, observed, True)
                changed = copy.deepcopy(proof); changed["format"] = boot.POLICY["format"]
                with self.assertRaises(ValueError):
                    boot.validate_boot_receipt(changed, True, observed, True, framework)
                changed = copy.deepcopy(record); changed["proof"]["wasm_boot"]["managed_bindings"].pop()
                with self.assertRaises(ValueError):
                    boot.validate_boot_request_binding(changed, expected, framework)
                manifest = next(row for row in expected if boot.platform_role(row["path"], framework) == "manifest")
                manifest["boot_configuration_sha256"] = "0" * 64
                with self.assertRaises(ValueError):
                    boot.validate_boot_request_binding(record, expected, framework)

    def test_proof_fields_roles_owners_hashes_and_assertion_consistency_are_strict(self):
        mutations = [lambda p: p.update(raw_script="private"), lambda p: p.update(format="invented"),
                     lambda p: p["checks"].update(configuration=1), lambda p: p["platform_bindings"].pop(),
                     lambda p: p["platform_bindings"][0].update(sha256="0" * 64),
                     lambda p: p["platform_bindings"][0].update(role="manifest"),
                     lambda p: p["managed_bindings"][0].update(owner="platform"),
                     lambda p: p["managed_bindings"].pop(3), lambda p: p["managed_bindings"].append(copy.deepcopy(p["managed_bindings"][0])),
                     lambda p: p.pop("configuration_sha256"), lambda p: p.update(bootstrap_sha256="0" * 64)]
        for index, mutate in enumerate(mutations):
            changed = copy.deepcopy(self.proof); mutate(changed)
            with self.subTest(mutation=index), self.assertRaises(ValueError):
                self.validate(changed)
        with self.assertRaises(ValueError):
            self.validate(passed=False)

    def test_original_request_binding_rejects_self_consistent_browser_forgeries(self):
        record = {"host": "wasm", "framework": "net10.0", "proof": {"wasm_boot": self.proof}, "resources": self.observed}
        boot.validate_boot_request_binding(record, self.expected, "net10.0")
        for field in ("sha256", "bytes", "content_type", "owner"):
            changed = copy.deepcopy(record)
            if field == "sha256":
                changed["proof"]["wasm_boot"]["managed_bindings"][0][field] = "0" * 64
                changed["resources"][5][field] = "0" * 64
            else:
                changed["resources"][5][field] = {"bytes": 7, "content_type": "text/html", "owner": "platform"}[field]
            with self.subTest(field=field), self.assertRaises(ValueError):
                boot.validate_boot_request_binding(changed, self.expected, "net10.0")
        changed = copy.deepcopy(record)
        changed["proof"]["wasm_boot"]["managed_bindings"].pop()
        changed["resources"].pop()
        boot.validate_boot_receipt(changed["proof"]["wasm_boot"], True, changed["resources"], True)
        with self.assertRaisesRegex(ValueError, "original requested"):
            boot.validate_boot_request_binding(changed, self.expected, "net10.0")
        for framework in ("net8.0", "net9.0"):
            with self.subTest(framework=framework), self.assertRaisesRegex(ValueError, "framework/host"):
                boot.validate_boot_request_binding(record, self.expected, framework)

    def test_production_receipt_requires_standalone_known_format_and_executed_callback(self):
        import run_paired_package_browser_matrix as matrix
        record = {"host": "wasm", "framework": "net10.0", "version": "3.9.0", "result": "incomplete",
                  "browser_version": "149.0", "resources": self.observed,
                  "proof": {"wasm_boot": self.proof, "interactive_validation_observed": True}}
        record["assertions"] = [{"name": name, "passed": name == "wasm_boot"}
                                for name in matrix.required_assertions(record)]
        matrix.validate_browser_receipt(record, matrix.identity(record))
        boot.validate_boot_request_binding(record, self.expected, "net10.0")
        for framework in ("net8.0", "net9.0"):
            changed = copy.deepcopy(record); changed["framework"] = framework
            with self.subTest(framework=framework), self.assertRaises(ValueError):
                matrix.validate_browser_receipt(changed, matrix.identity(changed))
        for mutate in (lambda r: r["proof"].pop("wasm_boot"),
                       lambda r: r["proof"].update(interactive_validation_observed=False)):
            changed = copy.deepcopy(record); mutate(changed)
            with self.assertRaises(ValueError):
                matrix.validate_browser_receipt(changed, matrix.identity(changed))


if __name__ == "__main__":
    unittest.main()
