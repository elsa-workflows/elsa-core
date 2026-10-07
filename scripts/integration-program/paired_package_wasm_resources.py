"""Bind package runtime PEs to actual Release WebCIL build/served bytes.

The mandatory subset is not a complete loaded-library claim. Every owned package
runtime DLL is mapped; actual browser requests must still be verified separately.
SDK 10.0.300 defaults to plain net8 filenames and required fingerprints for
net9/10 (WasmFingerprintAssets in Microsoft.NET.Sdk.WebAssembly.Browser.targets).
These source-grounded selectors do not certify actual cross-TFM builds. Fingerprint
mapping is checked, not the SDK's fingerprint generation algorithm. Production converter approval
remains exclusively in verify_browser_package_resources' tracked policy.
"""
from __future__ import annotations

import base64
import json
import re
from pathlib import Path, PurePosixPath
from zipfile import ZipFile

import consolidated_candidate_input as candidate
import paired_package_baseline_provenance as baseline
import paired_package_provenance as provenance
import prove_consolidated_package_consumers as packages
import verify_browser_package_resources as resources
from run_persisted_workflow_upgrade_fixture import safe_members

MANDATORY_ASSEMBLIES = frozenset(("Elsa.Studio.Core", "Elsa.Studio.Core.BlazorWasm", "Elsa.Studio.Workflows"))
require = resources.require


def _owned_project(layout, project: Path) -> Path:
    project = provenance.regular_file(Path(project).absolute())
    clients = {path.resolve() for name, path in layout.project_paths.items() if name in ("wasm", "custom-elements")}
    require(project in clients and re.fullmatch(r"net(?:8|9|10)\.0", layout.request.framework),
            "WASM project is not an owned client/framework")
    return project


def _member_path(member: str) -> PurePosixPath:
    require(isinstance(member, str) and member and "\\" not in member, "Unsafe runtime member")
    path = PurePosixPath(member)
    require(not path.is_absolute() and all(part not in (".", "..") for part in path.parts)
            and path.as_posix() == member and path.suffix == ".dll", "Unsafe runtime member")
    require(re.fullmatch(r"[A-Za-z0-9_.-]+", path.stem), "Unsafe runtime assembly name")
    return path


def _derive(layout, project: Path, build_manifest: Path, owned: dict, package_receipt: dict,
            archive_for, *, converter: dict, route_prefix: str, _test_policy=None) -> dict:
    """Internal mapper; archive authority is established by the public wrappers."""
    project = _owned_project(layout, project)
    framework = layout.request.framework
    expected_prefix = "/" + layout.request.route_prefix if layout.request.route_prefix else ""
    require(route_prefix == expected_prefix and
            (not route_prefix or re.fullmatch(r"/[A-Za-z0-9_-]+(?:/[A-Za-z0-9_-]+)*", route_prefix)),
            "WASM resource prefix differs from owned request")
    assets_path = provenance.regular_file(project.parent / "obj" / "project.assets.json")
    assets_hash = provenance.sha256(assets_path)
    require(assets_hash == package_receipt["project_assets_sha256"], "Project assets changed after package validation")
    assets, edges = provenance.read_package_assets(project, framework)
    require(not edges and framework + "/browser-wasm" in assets["targets"], "Missing selected browser-wasm runtime target")
    runtime_map, all_names = {}, set()
    for identity, library in assets["targets"][framework + "/browser-wasm"].items():
        package_id, _, version = identity.partition("/")
        pair = (package_id.casefold(), version)
        runtime = library.get("runtime", {})
        require(isinstance(runtime, dict), "Malformed WASM runtime map")
        require(not library.get("runtimeTargets"), "Unreviewed browser-wasm runtimeTargets map")
        for member in runtime:
            if not isinstance(member, str) or not member.endswith(".dll"):
                require(member == "_._", "Unreviewed WASM runtime member type")
                continue
            path = _member_path(member)
            name = path.stem
            require(name.casefold() not in all_names, "Ambiguous runtime assembly basename")
            all_names.add(name.casefold())
            if pair not in owned:
                continue
            package = owned[pair]
            require(package_id == package["id"] and assets["libraries"][identity].get("path") ==
                    package_id.lower() + "/" + version, "Runtime package identity/path differs from verified ledger")
            runtime_map[name] = (package, member)
    require(MANDATORY_ASSEMBLIES <= runtime_map.keys(), "Missing mandatory package runtime subset")
    require(project.stem.casefold() not in all_names, "Fixture conflicts with package runtime identity")
    build_manifest = provenance.regular_file(Path(build_manifest).absolute())
    require(build_manifest == (project.parent / "obj" / "Release" / framework / "staticwebassets.build.json").resolve(),
            "WASM build manifest is not the exact Release path")
    build_hash = provenance.sha256(build_manifest)
    build = json.loads(build_manifest.read_text(encoding="utf-8"))
    require(type(build.get("Version")) is int and build["Version"] == 1 and build.get("Source") == project.stem
            and build.get("Mode") == "Root" and build.get("ManifestType") == "Build"
            and build.get("BasePath") == "/" and isinstance(build.get("Assets"), list), "Unreviewed WASM build manifest schema")
    names = set(runtime_map) | {project.stem}
    mapped = {}
    mapped_folded = set()
    outside = []
    for row in build["Assets"]:
        require(isinstance(row, dict), "Malformed WASM build asset")
        if row.get("AssetRole") != "Primary" or row.get("AssetTraitName") != "WasmResource" or row.get("AssetTraitValue") != "runtime":
            continue
        relative = row.get("RelativePath")
        require(isinstance(relative, str), "Missing runtime resource path")
        pattern = (r"_framework/([A-Za-z0-9_.-]+)\.wasm" if framework == "net8.0" else
                   r"_framework/([A-Za-z0-9_.-]+)#\[\.\{fingerprint\}\]!\.wasm")
        match = re.fullmatch(pattern, relative)
        require(match is not None, "Unreviewed runtime resource filename schema")
        name = match[1]
        require(name.casefold() not in mapped_folded, "Duplicate runtime WASM manifest record")
        mapped_folded.add(name.casefold())
        require(name in names or name.casefold() not in {item.casefold() for item in names},
                "Runtime WASM assembly casing differs from selected asset")
        mapped[name] = row
        if name not in names:
            require(not name.casefold().startswith("elsa."), "Unknown Elsa runtime resource outside verified package/fixture map")
            outside.append(name)
    require(names <= mapped.keys(), "Missing package/fixture runtime WASM resource")
    records, conversions = [], []
    for name in sorted(names):
        row = mapped[name]
        require(row.get("SourceId") == project.stem and row.get("SourceType") == "Computed"
                and row.get("BasePath") == "/" and row.get("AssetKind") == "Build" and row.get("AssetMode") == "All"
                and not row.get("RelatedAsset"), "WASM resource source/runtime mapping differs")
        fingerprint = row.get("Fingerprint")
        require(isinstance(fingerprint, str) and re.fullmatch(r"[a-z0-9]{10}", fingerprint), "Unsafe WASM fingerprint")
        filename = f"{name}.wasm" if framework == "net8.0" else f"{name}.{fingerprint}.wasm"
        output = project.parent / "bin" / "Release" / framework / "wwwroot" / "_framework" / filename
        generated = project.parent / "obj" / "Release" / framework / "webcil" / f"{name}.wasm"
        for field, expected in (("Identity", output), ("OriginalItemSpec", generated)):
            value = row.get(field)
            require(isinstance(value, str) and Path(value).is_absolute() and ".." not in Path(value).parts,
                    "Unsafe WASM materialized path")
            require(provenance.regular_file(Path(value)) == expected.resolve(), "WASM resource escaped exact generated/served path")
        require(provenance.regular_file(output) == output.resolve() and provenance.regular_file(generated) == generated.resolve(),
                "Missing WASM generated/served copies")
        require(isinstance(row.get("ContentRoot"), str) and Path(row["ContentRoot"]).is_absolute()
                and ".." not in Path(row["ContentRoot"]).parts
                and provenance.regular_file(Path(row["ContentRoot"]) / "_framework" / output.name) == output.resolve(),
                "WASM content root differs from served directory")
        body = output.read_bytes()
        require(body == generated.read_bytes() and type(row.get("FileLength")) is int and len(body) == row["FileLength"]
                and 0 < len(body) <= 32 * 1024 * 1024, "Generated/served WASM bytes or length differ")
        require(row.get("Integrity") == base64.b64encode(bytes.fromhex(resources.sha256(body))).decode("ascii"),
                "WASM manifest integrity differs from served bytes")
        if name == project.stem:
            pe = provenance.regular_file(project.parent / "bin" / "Release" / framework / f"{name}.dll").read_bytes()
            owner, binding = "fixture", {"fixture_project_sha256": provenance.sha256(project)}
        else:
            package, member = runtime_map[name]
            archive, digest = archive_for(package)
            archive = provenance.regular_file(archive)
            require(provenance.sha256(archive) == digest, "Verified original archive changed")
            with ZipFile(archive) as zipped:
                matches = [entry for entry in safe_members(zipped) if entry.filename == member]
                require(len(matches) == 1, "Missing or ambiguous original package runtime member")
                pe = zipped.read(matches[0])
            require(provenance.sha256(archive) == digest, "Verified original archive changed during member read")
            cached = provenance.regular_file(layout.packages_root / package["id"].lower() / package["version"] / member)
            require(pe == cached.read_bytes(), "Cached runtime PE differs from original package member")
            owner, binding = "package", {"package_id": package["id"], "version": package["version"],
                                         "archive_sha256": digest, "package_member": member}
        conversion = resources.verify_webcil(pe, body, converter, _test_policy=_test_policy)
        path = route_prefix + "/_framework/" + filename
        records.append({"path": path, "sha256": resources.sha256(body), "bytes": len(body),
                        "content_type": "application/wasm", "owner": owner,
                        "required": name in MANDATORY_ASSEMBLIES or owner == "fixture"})
        conversions.append({"assembly": name, "path": path, "owner": owner, **binding, **conversion})
    require(provenance.sha256(assets_path) == assets_hash and provenance.sha256(build_manifest) == build_hash,
            "WASM manifests changed during derivation")
    return {"assets": records, "conversions": conversions, "package_runtime_count": len(runtime_map),
            "mandatory_package_subset": sorted(MANDATORY_ASSEMBLIES),
            "outside_package_fixture_scope": sorted(outside),
            "project_assets_sha256": assets_hash, "static_asset_manifest_sha256": build_hash}


def derive_candidate_wasm_resources(layout, project: Path, build_manifest: Path, verified_root: Path, *,
                                    verified_manifest_sha256: str, converter: dict, route_prefix: str = "",
                                    _test_policy=None) -> dict:
    project = _owned_project(layout, project)
    manifest_path = provenance.regular_file(verified_root / "verified-artifacts.json")
    require(provenance.sha256(manifest_path) == verified_manifest_sha256, "Verified candidate manifest changed")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    receipt = provenance.candidate_project_validator(layout, verified_root, manifest)(project)
    _, _, by_id, _, _ = packages._validated_manifest(manifest)
    owned = {(entry["id"].casefold(), entry["version"]): entry for entry in receipt["packages"]
             if entry.get("source") == "verified_original_candidate"}
    def archive_for(package):
        pin = by_id[package["id"].casefold()]
        return verified_root / "artifacts" / pin["nupkg"], pin["nupkg_sha256"]
    result = _derive(layout, project, build_manifest, owned, receipt, archive_for,
                     converter=converter, route_prefix=route_prefix, _test_policy=_test_policy)
    require(provenance.sha256(manifest_path) == verified_manifest_sha256, "Verified candidate manifest changed during derivation")
    return {**result, "verified_artifacts_sha256": verified_manifest_sha256, "candidate_producer": dict(candidate.PRODUCER)}


def derive_baseline_wasm_resources(layout, project: Path, build_manifest: Path, *, converter: dict,
                                   policy=baseline.POLICY_PATH, route_prefix: str = "", _test_policy=None) -> dict:
    project = _owned_project(layout, project)
    selected_policy = baseline._read_policy(policy)
    receipt = baseline.validate_baseline_project(project, layout.packages_root, layout.request.framework,
                                                layout.request.version, policy=selected_policy)
    owned = {(entry["id"].casefold(), entry["version"]): entry for entry in receipt["packages"]
             if entry.get("classification") == "owned_elsa"}
    def archive_for(package):
        entry = layout.packages_root / package["id"].lower() / package["version"]
        return entry / f'{package["id"].lower()}.{package["version"]}.nupkg', package["archive_sha256"]
    result = _derive(layout, project, build_manifest, owned, receipt, archive_for,
                     converter=converter, route_prefix=route_prefix, _test_policy=_test_policy)
    return {**result, "version": receipt["version"],
            "source_policy_sha256": resources.sha256(json.dumps(selected_policy, sort_keys=True).encode())}
