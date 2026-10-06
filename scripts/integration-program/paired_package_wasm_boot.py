"""Selected standalone bootstrap closure observed in SDK 10.0.300/net10.0 builds.

Platform authority is the owned static manifest, restored platform source copies
and generated dotnet.js. It is not sealed Elsa package archive provenance. The
existing managed mapper retains that separate authority. This closure excludes
ICU/globalization and other platform assemblies; unknown formats fail closed.
"""
from __future__ import annotations

import base64
import json
from pathlib import Path
import re

import paired_package_provenance as provenance
import paired_package_wasm_resources as managed_resources
import verify_browser_package_resources as resources

POLICY_PATH = resources.CONVERTER_POLICY.with_name("wasm-boot-policy.json")
POLICY = json.loads(POLICY_PATH.read_text(encoding="utf-8"))
CHECKS = frozenset(POLICY["checks"])
require = resources.require
MANAGED_PATH = re.compile(r"/_framework/(Elsa\.[A-Za-z0-9_.-]+)\.([a-z0-9]{10})\.wasm")


def _read(path: Path, limit: int) -> bytes:
    path = provenance.regular_file(path.absolute())
    require(0 < path.stat().st_size <= limit, "Bootstrap input exceeds its byte bound")
    with path.open("rb") as stream:
        body = stream.read(limit + 1)
    require(0 < len(body) <= limit, "Bootstrap input exceeds its byte bound")
    return body


def _pairs(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, "Duplicate bootstrap JSON key")
        result[key] = value
    return result


def _json(raw: bytes):
    def invalid_constant(_):
        raise ValueError("Non-finite bootstrap JSON value")
    return json.loads(raw.decode("utf-8"), object_pairs_hook=_pairs, parse_constant=invalid_constant)


def _sri(digest: str) -> str:
    require(isinstance(digest, str) and re.fullmatch(r"[a-f0-9]{64}", digest), "Invalid bootstrap binding hash")
    return "sha256-" + base64.b64encode(bytes.fromhex(digest)).decode("ascii")


def platform_role(path: str) -> str | None:
    for role in POLICY["platform"]:
        pattern = re.escape("/_framework/" + role["served"]).replace(re.escape("{fingerprint}"), "[a-z0-9]{10}")
        if re.fullmatch(pattern, path):
            return role["role"]
    return None


def parse_boot_configuration(body: bytes, platform: list[dict], managed: list[dict]) -> dict:
    require(0 < len(body) <= POLICY["maximum_boot_bytes"], "Bootstrap script exceeds its byte bound")
    start, end = b"/*json-start*/", b"/*json-end*/"
    require(body.count(start) == body.count(end) == 1, "Unreviewed embedded bootstrap format")
    left, right = body.index(start), body.index(end)
    require(left < right and re.search(rb"\.withConfig\(\s*$", body[:left]) and
            body[right + len(end):].startswith(b");"), "Unreviewed embedded bootstrap boundary")
    raw = body[left + len(start):right]
    config = _json(raw)
    require(isinstance(config, dict) and set(config) == set(POLICY["configuration_fields"]) and
            config["mainAssemblyName"] == POLICY["main_assembly"], "Unreviewed bootstrap configuration")
    listed = config["resources"]
    require(isinstance(listed, dict) and set(listed) == set(POLICY["resource_groups"]),
            "Unreviewed bootstrap resource schema")
    by_role = {platform_role(row["path"]): row for row in platform}
    require(len(platform) == len(by_role) == len(POLICY["platform"]) and None not in by_role,
            "Missing or duplicate platform bootstrap bindings")
    for role in POLICY["platform"]:
        group = role.get("group")
        if not group:
            continue
        rows = listed[group]
        expected = by_role[role["role"]]
        require(isinstance(rows, list) and len(rows) == 1 and isinstance(rows[0], dict), "Unreviewed native bootstrap group")
        row = rows[0]
        fields = {"name", "hash", "cache"} if group == "wasmNative" else {"name"}
        require(set(row) == fields and row["name"] == expected["path"].removeprefix("/_framework/"),
                "Native bootstrap name differs from manifest binding")
        if group == "wasmNative":
            require(row["hash"] == _sri(expected["sha256"]) and row["cache"] == "force-cache",
                    "Native bootstrap integrity differs from manifest binding")
    indexed, virtual_paths = {}, set()
    for group in ("assembly", "coreAssembly"):
        rows = listed[group]
        require(isinstance(rows, list) and 0 < len(rows) <= 1024, "Unreviewed managed bootstrap group")
        for row in rows:
            require(isinstance(row, dict) and set(row) == {"virtualPath", "name", "hash", "cache"} and
                    isinstance(row["name"], str) and re.fullmatch(r"[A-Za-z0-9_.-]+\.wasm", row["name"]) and
                    isinstance(row["virtualPath"], str) and re.fullmatch(r"[A-Za-z0-9_.-]+\.wasm", row["virtualPath"]) and
                    row["name"].casefold() not in indexed and row["virtualPath"].casefold() not in virtual_paths and
                    row["cache"] == "force-cache",
                    "Invalid or duplicate managed bootstrap resource")
            indexed[row["name"].casefold()] = row
            virtual_paths.add(row["virtualPath"].casefold())
    require(0 < len(managed) <= POLICY["maximum_managed_resources"], "Missing managed bootstrap bindings")
    expected_names, assemblies = set(), set()
    for asset in managed:
        match = MANAGED_PATH.fullmatch(asset["path"])
        require(match and asset["owner"] in ("package", "fixture"), "Invalid managed bootstrap binding")
        assembly = match[1]
        require((asset["owner"] == "fixture") == (assembly == POLICY["main_assembly"]), "Managed bootstrap owner differs")
        name = asset["path"].removeprefix("/_framework/")
        require(name.casefold() not in expected_names and name.casefold() in indexed, "Missing managed bootstrap resource")
        row = indexed[name.casefold()]
        require(row["name"] == name and row["virtualPath"] == assembly + ".wasm" and row["hash"] == _sri(asset["sha256"]),
                "Managed bootstrap differs from original package/fixture binding")
        expected_names.add(name.casefold())
        assemblies.add(assembly)
    require(set(POLICY["mandatory_managed"]) | {POLICY["main_assembly"]} <= assemblies,
            "Missing mandatory managed bootstrap subset")
    require({name for name in indexed if name.startswith("elsa.")} == expected_names,
            "Unbound Elsa assembly in bootstrap configuration")
    return {"format": POLICY["format"], "configuration_sha256": resources.sha256(raw), "managed_count": len(managed)}


def _source(role: dict, assets: dict, layout, project: Path) -> tuple[Path, dict]:
    package_id = role.get("source_package")
    if not package_id:
        return project.parent / role["source_member"], {"kind": "generated", "member": role["source_member"]}
    if package_id == "Microsoft.AspNetCore.Components.WebAssembly":
        rows = [(key, row) for key, row in assets["libraries"].items() if key.startswith(package_id + "/")]
        require(len(rows) == 1, "Missing restored bootstrap platform package")
        key, row = rows[0]
        version = key.removeprefix(package_id + "/")
        require(row.get("type") == "package" and row.get("path") == package_id.lower() + "/" + version,
                "Restored bootstrap platform package path differs")
    else:
        rows = [row for row in assets["project"]["frameworks"][POLICY["framework"]]["downloadDependencies"]
                if row.get("name") == package_id]
        require(len(rows) == 1, "Missing selected bootstrap runtime pack")
        version_range = re.fullmatch(r"\[(10\.0\.[0-9]{1,6}), \1\]", rows[0].get("version", ""))
        require(version_range is not None, "Bootstrap runtime pack version is not exact")
        version = version_range[1]
    require(re.fullmatch(r"10\.0\.[0-9]{1,6}", version), "Unreviewed platform bootstrap version")
    path = layout.packages_root / package_id.lower() / version / role["source_member"]
    return path, {"kind": "platform-cache", "package_id": package_id, "version": version, "member": role["source_member"]}


def derive_boot_resources(layout, project: Path, build_manifest: Path, managed: dict) -> dict:
    require(layout.request.host == "wasm" and layout.request.framework == POLICY["framework"] and
            layout.sdk == POLICY["sdk_version"] and not layout.request.route_prefix,
            "Unreviewed standalone WASM bootstrap framework/SDK/host")
    project = managed_resources._owned_project(layout, project)
    require(project.stem == POLICY["main_assembly"], "Unexpected standalone WASM entry assembly")
    expected_manifest = project.parent / "obj/Release" / POLICY["framework"] / "staticwebassets.build.json"
    require(provenance.regular_file(build_manifest.absolute()) == expected_manifest.resolve(), "Bootstrap requires the exact Release manifest")
    raw = _read(build_manifest, 16 * 1024 * 1024)
    manifest_hash = resources.sha256(raw)
    require(manifest_hash == managed["static_asset_manifest_sha256"], "Bootstrap manifest changed after managed verification")
    asset_file = project.parent / "obj/project.assets.json"
    asset_raw = _read(asset_file, 16 * 1024 * 1024)
    require(resources.sha256(asset_raw) == managed["project_assets_sha256"], "Bootstrap project assets changed after managed verification")
    assets, build = _json(asset_raw), _json(raw)
    require(type(build.get("Version")) is int and build["Version"] == 1 and build.get("Source") == project.stem and
            build.get("Mode") == "Root" and build.get("ManifestType") == "Build" and build.get("BasePath") == "/" and
            isinstance(build.get("Assets"), list) and isinstance(build.get("Endpoints"), list), "Unreviewed bootstrap build manifest")
    output_root = project.parent / "bin/Release" / POLICY["framework"] / "wwwroot"
    bindings, platform, manifest_body = [], [], None
    for role in POLICY["platform"]:
        rows = [row for row in build["Assets"] if row.get("RelativePath") == role["relative"] and row.get("AssetRole") == "Primary"]
        require(len(rows) == 1, "Missing or duplicate bootstrap platform asset")
        row = rows[0]
        require(row.get("SourceId") == project.stem and row.get("SourceType") == "Computed" and row.get("BasePath") == "/" and
                row.get("AssetKind") == role["kind"] and row.get("AssetMode") == "All" and not row.get("RelatedAsset") and
                row.get("AssetTraitName") == "WasmResource" and row.get("AssetTraitValue") == role["trait"],
                "Bootstrap platform asset metadata differs")
        fingerprint = row.get("Fingerprint")
        require(isinstance(fingerprint, str) and re.fullmatch(r"[a-z0-9]{10}", fingerprint), "Unsafe bootstrap fingerprint")
        filename = role["served"].replace("{fingerprint}", fingerprint)
        output = output_root / "_framework" / filename
        require(Path(row["Identity"]).is_absolute() and ".." not in Path(row["Identity"]).parts and
                provenance.regular_file(Path(row["Identity"])) == provenance.regular_file(output) and
                Path(row["ContentRoot"]).is_absolute() and ".." not in Path(row["ContentRoot"]).parts and
                provenance.regular_file(Path(row["ContentRoot"]) / "_framework" / filename) == output.resolve(),
                "Bootstrap platform output escaped the owned build")
        body = _read(output, POLICY["maximum_boot_bytes"] if role["role"] == "manifest" else 32 * 1024 * 1024)
        digest = resources.sha256(body)
        require(type(row.get("FileLength")) is int and row["FileLength"] == len(body) and row.get("Integrity") == _sri(digest)[7:],
                "Bootstrap platform length/integrity differs")
        source, source_binding = _source(role, assets, layout, project)
        spec = Path(row["OriginalItemSpec"])
        require(".." not in spec.parts and (spec.is_absolute() or spec.as_posix() == role["source_member"]) and
                provenance.regular_file(spec if spec.is_absolute() else project.parent / spec) == provenance.regular_file(source) and
                _read(source, 32 * 1024 * 1024) == body, "Bootstrap platform source copy differs")
        endpoints = [endpoint for endpoint in build["Endpoints"] if endpoint.get("Route") == "_framework/" + filename and
                     endpoint.get("Selectors") == []]
        require(len(endpoints) == 1 and provenance.regular_file(Path(endpoints[0]["AssetFile"])) == output.resolve(),
                "Missing exact bootstrap delivery endpoint")
        headers = _pairs((header["Name"].lower(), header["Value"]) for header in endpoints[0]["ResponseHeaders"])
        properties = _pairs((item["Name"], item["Value"]) for item in endpoints[0]["EndpointProperties"])
        require(headers.get("content-type") == role["content_type"] and headers.get("content-length") == str(len(body)) and
                properties.get("integrity") == _sri(digest), "Bootstrap endpoint content metadata differs")
        resource = {"path": "/_framework/" + filename, "sha256": digest, "bytes": len(body),
                    "content_type": headers["content-type"], "owner": "platform", "required": True}
        platform.append(resource)
        bindings.append({"role": role["role"], **resource, "source": {**source_binding, "sha256": digest}})
        if role["role"] == "manifest":
            manifest_body = body
    configuration = parse_boot_configuration(manifest_body, platform, managed["assets"])
    next(asset for asset in platform if platform_role(asset["path"]) == "manifest")["boot_configuration_sha256"] = configuration["configuration_sha256"]
    require(provenance.sha256(build_manifest) == manifest_hash and provenance.sha256(asset_file) == resources.sha256(asset_raw),
            "Bootstrap manifests changed during derivation")
    return {"assets": platform, "bindings": bindings, **configuration, "sdk_version": layout.sdk,
            "static_asset_manifest_sha256": manifest_hash, "project_assets_sha256": resources.sha256(asset_raw),
            "policy_sha256": provenance.sha256(POLICY_PATH)}


def validate_boot_receipt(value: object, assertion_passed: bool, observed: list[dict], managed_callback: bool) -> None:
    required = {"format", "checks", "platform_bindings", "managed_bindings"}
    optional = {"bootstrap_sha256", "configuration_sha256"}
    require(isinstance(value, dict) and required <= set(value) <= required | optional and
            value["format"] == POLICY["format"], "Unsafe WASM boot proof fields/format")
    checks = value["checks"]
    require(isinstance(checks, dict) and set(checks) == CHECKS and all(type(flag) is bool for flag in checks.values()),
            "Invalid WASM boot checks")
    platform, managed = value["platform_bindings"], value["managed_bindings"]
    require(isinstance(platform, list) and len(platform) == len(POLICY["platform"]) and
            isinstance(managed, list) and 0 < len(managed) <= POLICY["maximum_managed_resources"],
            "Missing or oversized WASM boot bindings")
    roles, paths, assemblies = {}, set(), set()
    for binding in platform:
        require(isinstance(binding, dict) and set(binding) == {"role", "path", "sha256"} and
                isinstance(binding["path"], str) and platform_role(binding["path"]) == binding["role"] and
                binding["role"] not in roles, "Invalid or duplicated platform bootstrap binding")
        _sri(binding["sha256"])
        roles[binding["role"]] = binding
    require(set(roles) == {role["role"] for role in POLICY["platform"]}, "Missing platform bootstrap role")
    for binding in managed:
        require(isinstance(binding, dict) and set(binding) == {"path", "sha256", "owner"} and
                isinstance(binding["path"], str), "Unsafe managed bootstrap binding")
        match = MANAGED_PATH.fullmatch(binding["path"])
        require(match and binding["owner"] in ("package", "fixture") and
                (binding["owner"] == "fixture") == (match[1] == POLICY["main_assembly"]) and
                binding["path"].casefold() not in paths and match[1] not in assemblies,
                "Invalid or duplicated managed bootstrap binding")
        _sri(binding["sha256"])
        paths.add(binding["path"].casefold())
        assemblies.add(match[1])
    require(set(POLICY["mandatory_managed"]) | {POLICY["main_assembly"]} <= assemblies,
            "Missing original managed/fixture bootstrap subset")
    for field in optional & set(value):
        _sri(value[field])

    def matched(binding, owner, content_type):
        return any(row.get("path") == binding["path"] and row.get("sha256") == binding["sha256"] and
                   row.get("owner") == owner and row.get("status") == 200 and row.get("requested") is True and
                   row.get("content_type") == content_type for row in observed)

    platform_matched = all(matched(roles[role["role"]], "platform", role["content_type"]) for role in POLICY["platform"])
    managed_matched = all(matched(binding, binding["owner"], "application/wasm") for binding in managed)
    require(checks["platform_resources"] is platform_matched, "WASM boot platform assertion differs from observed resources")
    require(not checks["configuration"] or optional <= set(value) and
            value["bootstrap_sha256"] == roles["manifest"]["sha256"] and
            matched(roles["manifest"], "platform", "text/javascript"), "Missing observed bootstrap configuration binding")
    require(checks["managed_resources"] is (managed_matched and checks["configuration"]),
            "WASM boot managed assertion differs from observed original bindings")
    require(checks["managed_callback"] is managed_callback, "WASM boot callback differs from executed native form validation")
    require(assertion_passed is all(checks.values()), "WASM boot assertion differs from complete observed proof")


def validate_boot_request_binding(record: dict, expected_resources: list[dict], framework: str) -> None:
    """Bind the browser claim to original verified input, not a receipt-selected resource set."""
    proof = record.get("proof", {}).get("wasm_boot")
    if proof is None:
        return
    require(record["host"] == "wasm" and framework == record["framework"] == POLICY["framework"],
            "WASM boot proof differs from requested framework/host")
    platform = [row for row in expected_resources if row["owner"] == "platform"]
    managed = [row for row in expected_resources if row["owner"] != "platform" and row["path"].startswith("/_framework/")]
    expected_platform = [{"role": platform_role(row["path"]), "path": row["path"], "sha256": row["sha256"]} for row in platform]
    expected_managed = [{"path": row["path"], "sha256": row["sha256"], "owner": row["owner"]} for row in managed]
    require(sorted(proof["platform_bindings"], key=lambda row: row["path"]) == sorted(expected_platform, key=lambda row: row["path"]) and
            sorted(proof["managed_bindings"], key=lambda row: row["path"]) == sorted(expected_managed, key=lambda row: row["path"]),
            "WASM boot bindings differ from original requested resources")
    manifests = [row for row in platform if platform_role(row["path"]) == "manifest"]
    require(len(manifests) == 1, "Missing requested bootstrap manifest resource")
    manifest = manifests[0]
    _sri(manifest.get("boot_configuration_sha256"))
    for field, expected in (("bootstrap_sha256", manifest["sha256"]),
                            ("configuration_sha256", manifest["boot_configuration_sha256"])):
        require(field not in proof or proof[field] == expected, "WASM boot configuration differs from original requested bytes")
    by_path = {row["path"]: row for row in platform + managed}
    for kind, bindings in (("platform_resources", proof["platform_bindings"]), ("managed_resources", proof["managed_bindings"])):
        if not proof["checks"][kind]:
            continue
        for binding in bindings:
            expected = by_path[binding["path"]]
            require(any(all(row.get(field) == expected[field] for field in ("path", "sha256", "bytes", "content_type", "owner")) and
                        row.get("status") == 200 and row.get("requested") is True for row in record.get("resources", [])),
                    "WASM boot response differs from original requested resource metadata")
