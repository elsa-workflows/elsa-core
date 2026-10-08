#!/usr/bin/env python3
"""Nonpublishing, exact-head package proof of the canonical Elsa.sln.

The opt-in property lifts only imported product guards. It never overrides
IsPackable globally. The inventory comes from evaluated MSBuild properties.
"""
from __future__ import annotations

import argparse
import base64
from concurrent.futures import ThreadPoolExecutor, as_completed
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import signal
import subprocess
import tempfile
import time
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

CORE_URL = "https://github.com/elsa-workflows/elsa-core"
RAW_URL = "https://raw.githubusercontent.com/elsa-workflows/elsa-core/"
PROOF_VERSION = re.compile(r"3\.10\.0-proof\.[1-9][0-9]*\.[1-9][0-9]*\Z")
EXTERNAL_PACKAGES = {
    "Elsa.Platform.PackageManifest": "External manifest tooling, not produced by Elsa.sln",
    "Elsa.Platform.PackageManifest.Generator": "External compiler tooling, not produced by Elsa.sln",
}
PROPERTIES = (
    "Version", "Configuration", "IsPackable", "PackageId", "PackageVersion", "AssemblyName", "TargetFrameworks",
    "TargetFramework", "IncludeBuildOutput", "IncludeSymbols", "SymbolPackageFormat",
    "IsTestProject", "IsTool", "BuildOutputTargetFolder", "GeneratePackageOnBuild",
    "RepositoryUrl", "PackageProjectUrl", "ProjectAssetsFile", "GenerateElsaPackageManifest", "ElsaPackageManifestIncludeInPackage", "ElsaPackageManifestPackagePath",
    "NETCoreSdkVersion", "MSBuildToolsPath", "NetCoreRoot", "RuntimeIdentifier",
)


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def run(command: list[str], cwd: Path, *, timeout: int = 300, log: Path | None = None,
        env: dict | None = None) -> str:
    def execute(stream=None):
        process = subprocess.Popen(command, cwd=cwd, env=env, text=True,
                                   stdout=stream if stream else subprocess.PIPE,
                                   stderr=subprocess.STDOUT if stream else subprocess.PIPE,
                                   start_new_session=os.name == "posix")
        try:
            stdout, stderr = process.communicate(timeout=timeout)
        except BaseException:
            if process.poll() is None:
                if os.name == "posix":
                    try:
                        os.killpg(process.pid, signal.SIGKILL)
                    except ProcessLookupError:
                        pass
                else:
                    process.kill()
                process.communicate()
            raise
        require(process.returncode == 0,
                f"Command failed ({process.returncode}): {command!r}\n" +
                (f"See {log}" if stream else f"{stdout}\n{stderr}"))
        return stdout or ""

    if log is None:
        return execute()
    with log.open("w") as stream:
        stream.write(json.dumps(command) + "\n")
        stream.flush()
        execute(stream)
    return log.read_text()


def solution_projects(root: Path) -> list[Path]:
    paths = re.findall(r'^Project\("[^"\n]+"\) = "[^"\n]+", "([^"\n]+\.csproj)"',
                       (root / "Elsa.sln").read_text(), re.MULTILINE)
    require(bool(paths), "Elsa.sln contains no C# projects")
    projects = [(root / path.replace("\\", "/")).resolve() for path in paths]
    require(len(set(projects)) == len(projects), "Duplicate solution project")
    for project in projects:
        require(project.is_relative_to(root) and project.is_file(), f"Invalid solution project: {project}")
    return projects


def validate_mode(version: str, mode: str) -> None:
    require(mode in ("proof", "candidate"), "Unknown consolidated package mode")
    if mode == "proof":
        require(bool(PROOF_VERSION.fullmatch(version)), "Version must be 3.10.0-proof.<positive run>.<positive attempt>")
    else:
        require(version == "3.10.0", "Candidate requires exactly stable 3.10.0")


def mode_properties(mode: str, enabled: bool = True) -> list[str]:
    return [f"-p:ConsolidatedPackageProof={str(enabled and mode == 'proof').lower()}",
            f"-p:ConsolidatedReleaseCandidate={str(enabled and mode == 'candidate').lower()}"]


def evaluate(root: Path, project: Path, version: str, proof: bool,
             framework: str | None = None, *, resolved: bool = False, mode: str = "proof") -> dict:
    command = ["dotnet", "msbuild", str(project), "-nologo", "-p:Configuration=Release",
               f"-p:Version={version}", f"-p:PackageVersion={version}", *mode_properties(mode, proof),
               "-getProperty:" + ",".join(PROPERTIES)]
    if framework:
        command.append(f"-p:TargetFramework={framework}")
    if resolved:
        sdk_names = ET.parse(project).getroot().get("Sdk", "").split(";")
        targets = "ResolveReferences"
        if "Microsoft.NET.Sdk.Razor" in sdk_names:
            targets += ";_PrepareRazorSourceGenerators"
        command.extend([f"-target:{targets}", "-p:BuildProjectReferences=false",
                        "-getItem:Analyzer,ResolvedFrameworkReference,Compile"])
    raw = run(command, root)
    try:
        data = json.loads(raw)
        return data if resolved else data["Properties"]
    except (ValueError, KeyError) as error:
        raise ValueError(f"Non-JSON MSBuild evaluation for {project}: {raw}") from error


def package_row(project: str, properties: dict) -> dict:
    return {
        "id": properties["PackageId"], "project": project,
        "assembly_name": properties["AssemblyName"],
        "frameworks": (properties["TargetFrameworks"] or properties["TargetFramework"]).split(";"),
        "include_build_output": properties["IncludeBuildOutput"].lower() != "false",
        "include_symbols": properties["IncludeSymbols"].lower() == "true",
        "is_tool": properties["IsTool"].lower() == "true",
        "symbol_format": properties["SymbolPackageFormat"],
    }


def inventory(root: Path, version: str, commit: str, *, workers: int = 4, mode: str = "proof") -> dict:
    validate_mode(version, mode)
    projects = solution_projects(root)

    def inspect(project: Path) -> tuple[dict | None, dict | None, dict]:
        path = project.relative_to(root).as_posix()
        normal = evaluate(root, project, version, False, mode=mode)
        proof = evaluate(root, project, version, True, mode=mode)
        imported = path.startswith(('extensions/src/', 'studio/src/'))
        require(not imported or normal["IsPackable"].lower() == "false",
                f"Imported project is packable by default: {path}")
        require(imported or normal["IsPackable"] == proof["IsPackable"],
                f"Proof changes a non-imported packability setting: {path}")
        record = {"project": path, "normal": normal, mode: proof}
        if proof["IsPackable"].lower() != "true":
            return None, {"id": proof["PackageId"], "project": path,
                          "reason": "Project or inherited nonpackable setting remains active"}, record
        require(proof["IsTestProject"].lower() != "true", f"Test project is packable: {path}")
        require(proof["PackageVersion"] == version and proof["Version"] == version and proof["Configuration"] == "Release", f"Wrong evaluated package version: {path}")
        require(proof["RepositoryUrl"].rstrip("/") == CORE_URL and
                proof["PackageProjectUrl"].rstrip("/") == CORE_URL,
                f"Noncanonical package metadata: {path}")
        row = package_row(path, proof)
        require(all(row["frameworks"]), f"No evaluated frameworks: {path}")
        row["framework_properties"] = {}
        for framework in row["frameworks"]:
            inner = evaluate(root, project, version, True, framework, mode=mode)
            require(inner["PackageId"] == row["id"] and inner["PackageVersion"] == version,
                    f"Framework changes package identity/version: {path}/{framework}")
            row["framework_properties"][framework] = {
                "assembly_name": inner["AssemblyName"], "package_version": inner["PackageVersion"],
                "include_build_output": inner["IncludeBuildOutput"].lower() != "false",
                "assets_file": inner["ProjectAssetsFile"],
                "manifest_required": inner["GenerateElsaPackageManifest"].lower() == "true" and inner["ElsaPackageManifestIncludeInPackage"].lower() == "true",
                "manifest_path": inner["ElsaPackageManifestPackagePath"],
            }
        row["nupkg"] = f"{row['id']}.{version}.nupkg"
        row["snupkg"] = f"{row['id']}.{version}.snupkg" if row["include_symbols"] and row["include_build_output"] else None
        require(not row["snupkg"] or row["symbol_format"] == "snupkg", f"Unsupported symbol format: {path}")
        return row, None, record

    started = time.monotonic()
    results = []
    with ThreadPoolExecutor(max_workers=workers) as pool:
        futures = [pool.submit(inspect, project) for project in projects]
        for future in as_completed(futures):
            results.append(future.result())
            if len(results) % 20 == 0 or len(results) == len(projects):
                print(f"Evaluated {len(results)}/{len(projects)} projects in {time.monotonic()-started:.1f}s", flush=True)
    results.sort(key=lambda result: result[2]["project"])
    packages = sorted((row for row, _, _ in results if row), key=lambda row: row["id"].casefold())
    require(len({row["id"].casefold() for row in packages}) == len(packages), "Duplicate evaluated PackageId")
    return {"mode": mode, "configuration": "Release", "version": version, "source_commit": commit, "repository_url": CORE_URL,
            "published": False, "packages": packages,
            "exclusions": sorted((row for _, row, _ in results if row), key=lambda row: row["project"]),
            "evaluations": [record for _, _, record in results], "external_package_exceptions": EXTERNAL_PACKAGES,
            "icon_sha256": hashlib.sha256((root / "icon.png").read_bytes()).hexdigest()}


def dependency_groups(data: ET.Element) -> list[dict]:
    require(not data.findall("dependencies/dependency"), "Ungrouped dependencies are unsupported")
    groups = []
    for group in data.findall("dependencies/group"):
        dependencies = [{"id": item.get("id", ""), "version": item.get("version", ""),
                         "include": item.get("include", ""), "exclude": item.get("exclude", "")}
                        for item in group.findall("dependency")]
        require(len({item["id"].casefold() for item in dependencies}) == len(dependencies), "Duplicate package dependency")
        groups.append({"framework": group.get("targetFramework", ""),
                       "dependencies": sorted(dependencies, key=lambda item: item["id"].casefold())})
    require(len({group["framework"] for group in groups}) == len(groups), "Duplicate dependency framework")
    return sorted(groups, key=lambda group: group["framework"])


def framework_reference_groups(data: ET.Element) -> list[dict]:
    require(not data.findall("frameworkReferences/frameworkReference"), "Ungrouped framework references are unsupported")
    groups = []
    containers = data.findall("frameworkReferences")
    require(len(containers) <= 1 and all(child.tag == "group" for container in containers for child in container),
            "Unsupported framework reference structure")
    for group in data.findall("frameworkReferences/group"):
        require(set(group.attrib) == {"targetFramework"} and all(item.tag == "frameworkReference" for item in group),
                "Unsupported framework reference structure")
        references = [item.get("name", "") for item in group.findall("frameworkReference")]
        require(bool(group.get("targetFramework")) and all(references), "Empty framework reference identity")
        require(len({name.casefold() for name in references}) == len(references), "Duplicate framework reference")
        require(all(set(item.attrib) == {"name"} for item in group), "Unsupported framework reference attributes")
        groups.append({"framework": group.get("targetFramework"), "references": sorted(references)})
    require(len({group["framework"] for group in groups}) == len(groups), "Duplicate framework reference group")
    return sorted(groups, key=lambda group: group["framework"])


SDK_BUILD_FOLDERS = {"build", "buildTransitive", "buildMultiTargeting"}


def sdk_asset_path(path: str, row: dict) -> bool:
    manifests = {properties["manifest_path"] for properties in row["framework_properties"].values()
                 if properties["manifest_required"]}
    parts = PurePosixPath(path).parts
    return bool(parts and parts[0].casefold() in {name.casefold() for name in SDK_BUILD_FOLDERS}) or path in manifests or path == "elsa-package.json"


def capture_sdk_assets(root: Path, row: dict, data: bytes) -> list[dict]:
    """Hash concrete SDK nuspec mappings; never expand arbitrary globs or infer files."""
    document = parse_document(data)
    assets = []
    root = root.resolve(strict=True)
    for item in document.findall("files/file"):
        target = item.get("target", "").lstrip("/")
        if not sdk_asset_path(target, row):
            continue
        path = PurePosixPath(target)
        require(target and not path.is_absolute() and ".." not in path.parts and "\\" not in target,
                "Unsafe SDK build/manifest target")
        require(set(item.attrib) == {"src", "target"}, "Unsupported SDK build/manifest mapping")
        source = Path(item.get("src", ""))
        require(source.is_absolute() and source.is_file() and not source.is_symlink(), "Missing or non-concrete SDK build/manifest source")
        resolved = source.resolve(strict=True)
        require(source.is_relative_to(root) and resolved.is_relative_to(root) and not any(parent.is_symlink() for parent in source.parents if parent != root and parent.is_relative_to(root)),
                "SDK build/manifest source escaped worktree")
        assets.append({"path": target, "source_path": resolved.relative_to(root).as_posix(),
                       "sha256": hashlib.sha256(source.read_bytes()).hexdigest()})
    require(len({asset["path"].casefold() for asset in assets}) == len(assets), "Duplicate SDK build/manifest target")
    return sorted(assets, key=lambda asset: asset["path"])


def verify_sdk_assets(archive: zipfile.ZipFile, row: dict, *, required: bool = False) -> None:
    if not required and "expected_sdk_assets" not in row:
        return  # Immutable historical receipts predate this explicit capability.
    require("expected_sdk_assets" in row, "Missing SDK build/manifest asset evidence")
    expected = row["expected_sdk_assets"]
    require(type(expected) is list and all(type(asset) is dict and set(asset) == {"path", "source_path", "sha256"}
            and type(asset["path"]) is str and sdk_asset_path(asset["path"], row)
            and PurePosixPath(asset["path"]).as_posix() == asset["path"]
            and not PurePosixPath(asset["path"]).is_absolute() and ".." not in PurePosixPath(asset["path"]).parts
            and type(asset["source_path"]) is str and bool(asset["source_path"])
            and not PurePosixPath(asset["source_path"]).is_absolute() and ".." not in PurePosixPath(asset["source_path"]).parts
            and "\\" not in asset["source_path"]
            and type(asset["sha256"]) is str and re.fullmatch(r"[0-9a-f]{64}", asset["sha256"]) for asset in expected),
            "Invalid SDK build/manifest asset evidence")
    paths = {asset["path"] for asset in expected}
    require(len({path.casefold() for path in paths}) == len(expected), "Duplicate SDK build/manifest asset evidence")
    actual = {name for name in archive_names(archive) if sdk_asset_path(name, row) and not name.endswith("/")}
    require(actual == paths, "SDK build/manifest asset inventory mismatch")
    for asset in expected:
        require(hashlib.sha256(archive.read(asset["path"])).hexdigest() == asset["sha256"],
                "SDK build/manifest asset differs from generated output")


def read_staged_nuspecs(destination: Path, row: dict) -> None:
    main = row["nupkg"].removesuffix(".nupkg") + ".nuspec"
    symbols = row["snupkg"].removesuffix(".snupkg") + ".symbols.nuspec" if row["snupkg"] else None
    expected = {main} | ({symbols} if symbols else set())
    actual = {path.name for path in destination.glob("*.nuspec")}
    require(actual == expected, f"SDK staged nuspec pair mismatch: {row['id']}; expected={sorted(expected)}, actual={sorted(actual)}")
    require(not any(destination.rglob("*.nupkg")) and not any(destination.rglob("*.snupkg")),
            f"Metadata-only SDK stage produced package output: {row['id']}")
    for name, groups_key, hash_key in (
        (main, "expected_dependency_groups", "sdk_nuspec_sha256"),
        (symbols, "expected_symbol_dependency_groups", "sdk_symbol_nuspec_sha256"),
    ):
        if name is None:
            continue
        path = destination / name
        require(path.is_file() and not path.is_symlink(), f"Invalid staged nuspec: {path}")
        data = path.read_bytes()
        metadata = parse_metadata(data)
        row[groups_key] = dependency_groups(metadata)
        references_key = "expected_symbol_framework_reference_groups" if name == symbols else "expected_framework_reference_groups"
        row[references_key] = framework_reference_groups(metadata)
        row[hash_key] = hashlib.sha256(data).hexdigest()


def stage_nuspecs(root: Path, row: dict, version: str, destination: Path, *, mode: str = "proof") -> None:
    destination.mkdir()
    # GenerateNuspec's condition is evaluated before its dependencies. Derive
    # the style through the SDK restore target before entering it; Pack itself
    # enables package creation even when ContinuePacking... was passed false.
    command = ["dotnet", "msbuild", str(root / row["project"]), "-nologo",
               "-target:_GetRestoreProjectStyle;GenerateNuspec",
               "-p:Configuration=Release", f"-p:Version={version}", f"-p:PackageVersion={version}",
               *mode_properties(mode), "-p:ContinuousIntegrationBuild=true", "-p:NoBuild=true",
               "-p:ContinuePackingAfterGeneratingNuspec=false", f"-p:NuspecOutputPath={destination}",
               f"-p:PackageOutputPath={destination / 'forbidden-packages'}"]
    run(command, root, log=destination / "generate-nuspec.log")
    read_staged_nuspecs(destination, row)


def stage_sdk_metadata(root: Path, manifest: dict, output: Path, inspector: Path) -> None:
    staging = output / "sdk-metadata"
    staging.mkdir()
    cache = {"archive_inspector": inspector, "source_commit": manifest["source_commit"]}
    for index, row in enumerate(manifest["packages"]):
        destination = staging / f"{index:03}-{row['id']}"
        stage_nuspecs(root, row, manifest["version"], destination, mode=manifest.get("mode", "proof"))
        row["restore_assets"] = []
        for framework, properties in row["framework_properties"].items():
            resolved = evaluate(root, root / row["project"], manifest["version"], True, framework, resolved=True, mode=manifest.get("mode", "proof"))
            (destination / f"resolved.{framework}.json").write_text(json.dumps(resolved, indent=2) + "\n")
            after_restore = resolved["Properties"]
            require(after_restore["AssemblyName"] == properties["assembly_name"] and after_restore["PackageVersion"] == manifest["version"],
                    f"Restore changed package identity/version: {row['id']}/{framework}")
            properties["manifest_required"] = after_restore["GenerateElsaPackageManifest"].lower() == "true" and after_restore["ElsaPackageManifestIncludeInPackage"].lower() == "true"
            properties["manifest_path"] = after_restore["ElsaPackageManifestPackagePath"]
            assets = Path(after_restore["ProjectAssetsFile"])
            require(assets.is_relative_to(root) and assets.is_file(), f"Missing canonical restore assets: {assets}")
            row["restore_assets"].append({"framework": framework, "path": assets.relative_to(root).as_posix(),
                                          "sha256": hashlib.sha256(assets.read_bytes()).hexdigest()})
            snapshot = destination / f"restore.{framework}.assets.json"
            snapshot.write_bytes(assets.read_bytes())
            row["restore_assets"][-1]["retained_path"] = snapshot.relative_to(output).as_posix()
            properties["compiler_evidence"] = capture_compiler_evidence(root, row, framework, resolved, cache)
        staged_main = destination / (row["nupkg"].removesuffix(".nupkg") + ".nuspec")
        row["expected_sdk_assets"] = capture_sdk_assets(root, row, staged_main.read_bytes())
        if (index + 1) % 20 == 0 or index + 1 == len(manifest["packages"]):
            print(f"Staged SDK metadata for {index+1}/{len(manifest['packages'])} packages", flush=True)


CLIENTLIB_ASSETS = {
    "Elsa.Studio.Workflows.Designer": (
        'studio/src/modules/Elsa.Studio.Workflows.Designer',
        ("designer.entry.js", "react-designer.entry.js", "designer.css")),
    "Elsa.Studio.DomInterop": (
        'studio/src/framework/Elsa.Studio.DomInterop',
        ("dom.entry.js", "clipboard.entry.js", "files.entry.js")),
}


def build_clientlibs(root: Path, output: Path) -> dict:
    script = root / "scripts/integration-program/build_studio_clientlibs.sh"
    inputs = {script.relative_to(root).as_posix(), "Directory.Packages.props", 'studio/src/Directory.Packages.props'}
    for project, _ in CLIENTLIB_ASSETS.values():
        tracked = run(["git", "ls-files", "--", f"{project}/ClientLib"], root).splitlines()
        inputs.update(tracked)
    inputs.update(run(["git", "ls-files", "--", "scripts/integration-program/consolidated-build/studio-clientlib-lockfiles"], root).splitlines())
    input_hashes = {path: hashlib.sha256((root / path).read_bytes()).hexdigest() for path in sorted(inputs)}
    run(["bash", str(script), str(root)], root, timeout=1200, log=output / "studio-clientlibs.log")
    assets = []
    for identifier, (project, files) in CLIENTLIB_ASSETS.items():
        for name in files:
            path = root / project / "wwwroot" / name
            require(path.is_file() and not path.is_symlink() and path.stat().st_size > 0, f"Missing built browser asset: {path}")
            assets.append({"id": identifier, "source_path": path.relative_to(root).as_posix(),
                           "package_path": f"staticwebassets/{name}", "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    return {"inputs": input_hashes, "assets": assets, "published": False}


def verify_browser_assets(archive: zipfile.ZipFile, row: dict, assets: list[dict]) -> list[dict]:
    expected = [asset for asset in assets if asset["id"] == row["id"]]
    names = archive_names(archive)
    for asset in expected:
        require(asset["package_path"] in names, f"Missing packaged browser bundle: {row['id']} {asset['package_path']}")
        require(hashlib.sha256(archive.read(asset["package_path"])).hexdigest() == asset["sha256"],
                f"Packaged browser bundle differs from built output: {asset['package_path']}")
    return expected


# Source-bound catalog support for the seven newly promoted #8661 packages.
# Concrete Shell declarations live in the named packages' ShellFeatures directories;
# the EFCore package contains only an abstract persistence base, never a selection.
ADMISSION_SHELL_FEATURES = {
    "Elsa.Workflows.Admission": {"Elsa.Workflows.Admission.ShellFeatures.AdmissionFeature"},
    "Elsa.Workflows.Admission.Persistence.EFCore": set(),
    "Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql": {
        "Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql.ShellFeatures.PostgreSqlAdmissionPersistenceShellFeature"},
}


def verify_admission_catalog(data: dict, row: dict) -> dict:
    require(data.get("schemaVersion") == "1.0", "Admission package manifest schema mismatch")
    compatibility = data.get("compatibility")
    require(type(compatibility) is dict and compatibility.get("runtimeKinds") == ["elsa.server"],
            "Admission package manifest must target Server runtime")
    features = data.get("features")
    require(type(features) is list and all(type(feature) is dict for feature in features),
            "Admission package manifest feature inventory is missing")
    identities = [feature.get("id") for feature in features]
    types = [feature.get("typeName") for feature in features]
    require(all(type(identity) is str and identity.startswith(row["id"] + ".") and identity != row["id"] + "."
                for identity in identities) and len(set(identities)) == len(identities),
            "Admission package manifest feature identity mismatch")
    require(all(type(name) is str for name in types) and len(set(types)) == len(types)
            and set(types) == ADMISSION_SHELL_FEATURES[row["id"]],
            "Admission package manifest selectable feature mismatch")
    for feature in features:
        compatibility = feature.get("compatibility")
        require(compatibility is None or (type(compatibility) is dict and compatibility.get("runtimeKinds") == ["elsa.server"]),
                "Admission selectable feature narrows away from Server runtime")
    return {"selectable_features": [{"id": feature["id"], "type_name": feature["typeName"]} for feature in features],
            "runtime_kinds": data["compatibility"]["runtimeKinds"]}


def verify_package_manifest(archive: zipfile.ZipFile, row: dict, version: str, *, require_sdk_metadata: bool = False) -> dict | None:
    required = any(properties["manifest_required"] for properties in row["framework_properties"].values())
    paths = {properties["manifest_path"] for properties in row["framework_properties"].values() if properties["manifest_required"]}
    catalog_required = require_sdk_metadata and row["id"] in ADMISSION_SHELL_FEATURES
    if catalog_required and ADMISSION_SHELL_FEATURES[row["id"]]:
        require(required, "Selectable Admission package manifest is required")
    require(len(paths) <= 1, f"Frameworks disagree on package manifest location: {row['id']}")
    if not required:
        require("elsa-package.json" not in archive_names(archive), f"Unexpected package manifest: {row['id']}")
        return None
    path = next(iter(paths))
    require(path in archive_names(archive), f"Missing generated package manifest: {row['id']}")
    data = json.loads(archive.read(path))
    require(data.get("package", {}).get("id") == row["id"] and data.get("package", {}).get("version") == version,
            f"Generated package manifest identity/version mismatch: {row['id']}")
    require(set(data.get("extensions", {}).get("targetFrameworks", [])) == set(row["frameworks"]),
            f"Generated package manifest framework mismatch: {row['id']}")
    require(data.get("extensions", {}).get("repositoryUrl", "").rstrip("/") == CORE_URL,
            f"Generated package manifest repository mismatch: {row['id']}")
    catalog = verify_admission_catalog(data, row) if catalog_required else {}
    return {"path": path, "sha256": hashlib.sha256(archive.read(path)).hexdigest(),
            "id": row["id"], "version": version, "frameworks": row["frameworks"], **catalog}


def archive_names(archive: zipfile.ZipFile) -> set[str]:
    names = archive.namelist()
    require(len({name.casefold() for name in names}) == len(names), "Duplicate ZIP entries")
    for name in names:
        path = PurePosixPath(name)
        require(not path.is_absolute() and ".." not in path.parts and "\\" not in name,
                f"Unsafe ZIP entry: {name}")
    return set(names)


def metadata(archive: zipfile.ZipFile) -> ET.Element:
    files = [name for name in archive_names(archive) if name.casefold().endswith(".nuspec")]
    require(len(files) == 1, "Artifact must contain exactly one nuspec")
    return parse_metadata(archive.read(files[0]))


def parse_document(data: bytes) -> ET.Element:
    document = ET.fromstring(data)
    for element in document.iter():
        element.tag = element.tag.rsplit("}", 1)[-1]
    return document


def parse_metadata(data: bytes) -> ET.Element:
    result = parse_document(data).find("metadata")
    require(result is not None, "Nuspec metadata is missing")
    return result


def verify_metadata(data: ET.Element, row: dict, manifest: dict, *, symbols: bool = False, require_sdk_metadata: bool = False) -> list[dict]:
    require(data.findtext("id") == row["id"] and data.findtext("version") == manifest["version"],
            f"Artifact identity/version mismatch: {row['id']}")
    repository = data.find("repository")
    require(repository is not None and repository.get("type") == "git" and
            repository.get("url", "").rstrip("/") == CORE_URL and
            repository.get("commit") == manifest["source_commit"],
            f"Artifact repository metadata mismatch: {row['id']}")
    produced = {package["id"].casefold() for package in manifest["packages"]}
    exceptions = {name.casefold() for name in manifest["external_package_exceptions"]}
    groups = dependency_groups(data)
    groups_key = "expected_symbol_dependency_groups" if symbols else "expected_dependency_groups"
    require(groups_key in row and groups == row[groups_key],
            f"SDK dependency metadata/archive mismatch: {row['id']}")
    references_key = "expected_symbol_framework_reference_groups" if symbols else "expected_framework_reference_groups"
    if require_sdk_metadata or references_key in row:
        require(references_key in row and framework_reference_groups(data) == row[references_key],
                f"SDK framework reference metadata/archive mismatch: {row['id']}")
        require(all(group["framework"] in row["framework_properties"] for group in row[references_key]),
                f"Unsupported framework reference framework: {row['id']}")
    for group in groups:
        require(group["framework"] in row["framework_properties"], f"Unsupported dependency framework: {group['framework']}")
        for dependency in group["dependencies"]:
            identifier, version = dependency["id"], dependency["version"]
            require(bool(identifier) and bool(version), "Empty artifact dependency")
            if identifier.casefold() in produced:
                require(version in (manifest["version"], f"[{manifest['version']}]", f"[{manifest['version']}, )",
                                    f"[{manifest['version']},)"),
                        f"Internal dependency is not the proof version: {row['id']} -> {identifier} {version}")
            elif identifier.casefold().startswith("elsa"):
                require(identifier.casefold() in exceptions,
                        f"Unavailable or excluded internal dependency: {row['id']} -> {identifier}")
    return groups


def verify_artifacts(artifacts: Path, manifest: dict, *, require_sdk_metadata: bool = False) -> dict:
    """Fresh runs require SDK asset/reference evidence; immutable legacy replay cannot invent it."""
    require(artifacts.is_dir() and not artifacts.is_symlink(), "Artifact directory is missing or symlinked")
    expected = {row[key] for row in manifest["packages"] for key in ("nupkg", "snupkg") if row[key]}
    actual = {path.name for path in artifacts.iterdir()}
    require(actual == expected, f"Artifact inventory mismatch; missing={sorted(expected-actual)}, unexpected={sorted(actual-expected)}")
    for row in manifest["packages"]:
        for key in ("nupkg", "snupkg"):
            if row[key]:
                path = artifacts / row[key]
                require(path.is_file() and not path.is_symlink(), f"Not a regular artifact: {path}")
        path = artifacts / row["nupkg"]
        data = path.read_bytes()
        row["nupkg_sha256"] = hashlib.sha256(data).hexdigest()
        row["nupkg_sha512"] = hashlib.sha512(data).hexdigest()
        with zipfile.ZipFile(path) as archive:
            names = archive_names(archive)
            nuspec = metadata(archive)
            row["dependency_groups"] = verify_metadata(nuspec, row, manifest, require_sdk_metadata=require_sdk_metadata)
            verify_sdk_assets(archive, row, required=require_sdk_metadata)
            row["browser_assets"] = verify_browser_assets(archive, row, manifest.get("browser_assets", []))
            row["package_manifest"] = verify_package_manifest(archive, row, manifest["version"], require_sdk_metadata=require_sdk_metadata)
            require(nuspec.findtext("projectUrl", "").rstrip("/") == CORE_URL,
                    f"Noncanonical project URL: {row['id']}")
            icon = nuspec.findtext("icon")
            require(icon is not None and icon in names, f"Missing declared package icon: {row['id']}")
            require(hashlib.sha256(archive.read(icon)).hexdigest() == manifest["icon_sha256"],
                    f"Package icon differs from canonical Core icon: {row['id']}")
            row["assemblies"] = []
            for framework, properties in row["framework_properties"].items():
                if not properties["include_build_output"]:
                    continue
                base = f"tools/{framework}/any" if row["is_tool"] else f"lib/{framework}"
                assembly = f"{base}/{properties['assembly_name']}.dll"
                require(assembly in names, f"Missing framework assembly: {row['id']} {assembly}")
                row["assemblies"].append({"framework": framework, "assembly": assembly,
                                          "pdb": f"{base}/{properties['assembly_name']}.pdb"})
            expected_dlls = {assembly["assembly"] for assembly in row["assemblies"]}
            own_names = {properties["assembly_name"] + ".dll" for properties in row["framework_properties"].values()}
            actual_dlls = {name for name in names if PurePosixPath(name).name in own_names}
            require(actual_dlls == expected_dlls, f"Assembly framework coverage mismatch: {row['id']}")
            if row["snupkg"]:
                symbol_path = artifacts / row["snupkg"]
                row["snupkg_sha256"] = hashlib.sha256(symbol_path.read_bytes()).hexdigest()
                with zipfile.ZipFile(symbol_path) as symbols:
                    symbol_names = archive_names(symbols)
                    verify_metadata(metadata(symbols), row, manifest, symbols=True, require_sdk_metadata=require_sdk_metadata)
                    expected_pdbs = {assembly["pdb"] for assembly in row["assemblies"]}
                    require({name for name in symbol_names if name.endswith(".pdb")} == expected_pdbs,
                            f"Symbol framework coverage mismatch: {row['id']}")
            else:
                require(not row["assemblies"], f"Assembly package has no external symbol proof: {row['id']}")
    return manifest


def source_url(path: str, maps: dict[str, str]) -> str | None:
    matches = []
    for pattern, target in maps.items():
        if "*" not in pattern:
            if path == pattern:
                matches.append((len(pattern), target))
            continue
        require(pattern.count("*") == 1 and target.count("*") == 1, "Unsupported SourceLink wildcard map")
        before, after = pattern.split("*")
        if path.startswith(before) and path.endswith(after):
            replacement = path[len(before):len(path)-len(after) if after else None]
            matches.append((len(before)+len(after), target.replace("*", replacement)))
    return max(matches, default=(0, None))[1]


# Independently downloaded from the recorded official Feedz source on 2026-10-06.
# These pins authorize only the version-specific Generator.Hints sources, not a
# general exemption for untracked files or arbitrary NuGet compiler packages.
GENERATOR_SOURCE_PINS = {
    "0.0.1-preview.50": "56310f3c6606c793bce875f0dee5746dc5f42721d0cbbfde5fa3c4b61e6f15aa",
    "0.0.1-preview.53": "ba9b6c28e11eec6f6c595ebf328da1b925dbee9ca2590aa5b6fa1ec4c2052780",
}
GENERATOR_HINTS_PREFIX = "contentFiles/cs/any/Elsa.Platform.PackageManifest.Generator.Hints/"
GENERATOR_HINTS_50 = frozenset({
    "ElsaRuntimeKinds.cs", "ManifestExtensionAttribute.cs", "ManifestIgnoreAttribute.cs",
    "ManifestInfrastructureAttribute.cs", "ManifestRuntimeKindAttribute.cs", "ManifestSettingAttribute.cs",
    "ManifestUIOptionAttribute.cs", "ManifestUIOptionsProviderAttribute.cs",
})
GENERATOR_SOURCE_ENTRIES = {
    "0.0.1-preview.50": frozenset(GENERATOR_HINTS_PREFIX + name for name in GENERATOR_HINTS_50),
    "0.0.1-preview.53": frozenset(GENERATOR_HINTS_PREFIX + name
                                for name in GENERATOR_HINTS_50 | {"ManifestFeatureCategoryAttribute.cs"}),
}
GENERATOR_FEED = "https://f.feedz.io/elsa-workflows/elsa-3/nuget/index.json"
GENERATOR_SOURCE = re.compile(
    r"^/_[0-9]+/elsa\.platform\.packagemanifest\.generator/([^/]+)/"
    r"(contentFiles/cs/any/Elsa\.Platform\.PackageManifest\.Generator\.Hints/[A-Za-z]+\.cs)$")


GENERATOR_TOOLS = {
    "refit": ("InterfaceStubGeneratorV2.dll", {"Refit"}),
    "logging": ("Microsoft.Extensions.Logging.Generators.dll", {"Microsoft.Extensions.Logging.Abstractions", "Microsoft.AspNetCore.App.Ref"}),
    "regex": ("System.Text.RegularExpressions.Generator.dll", {"Microsoft.NETCore.App.Ref"}),
    "json": ("System.Text.Json.SourceGeneration.dll", {"System.Text.Json", "Microsoft.NETCore.App.Ref"}),
    "razor": ("Microsoft.CodeAnalysis.Razor.Compiler.dll", set()),
    "resx": ("Microsoft.CodeAnalysis.ResxSourceGenerator.CSharp.dll", {"Microsoft.CodeAnalysis.ResxSourceGenerator"}),
    "polysharp": ("PolySharp.SourceGenerators.dll", {"PolySharp"}),
}
GENERATED_FAMILIES = (
    (r"InterfaceStubGeneratorV2/Refit\.Generator\.InterfaceStubGeneratorV2/(Generated|PreserveAttribute|I[A-Za-z0-9_]+)\.g\.cs", "refit"),
    (r"Microsoft\.Extensions\.Logging\.Generators/Microsoft\.Extensions\.Logging\.Generators\.LoggerMessageGenerator/LoggerMessage\.g\.cs", "logging"),
    (r"System\.Text\.RegularExpressions\.Generator/System\.Text\.RegularExpressions\.Generator\.RegexGenerator/RegexGenerator\.g\.cs", "regex"),
    (r"System\.Text\.Json\.SourceGeneration/System\.Text\.Json\.SourceGeneration\.JsonSourceGenerator/[A-Za-z_][A-Za-z0-9_.]*\.g\.cs", "json"),
    (r"Microsoft\.CodeAnalysis\.Razor\.Compiler/Microsoft\.NET\.Sdk\.Razor\.SourceGenerators\.RazorSourceGenerator/[A-Za-z0-9_/-]+_razor\.g\.cs", "razor"),
    (r"Microsoft\.CodeAnalysis\.ResxSourceGenerator\.CSharp/Microsoft\.CodeAnalysis\.ResxSourceGenerator\.CSharp\.CSharpResxGenerator/Translations\.Designer\.cs", "resx"),
    (r"PolySharp\.SourceGenerators/PolySharp\.SourceGenerators\.PolyfillsGenerator/System\.Runtime\.CompilerServices\.OverloadResolutionPriorityAttribute\.g\.cs", "polysharp"),
)
PHYSICAL_GENERATORS = {
    "grpc": ("Grpc.Tools", "build/_protobuf/netstandard1.3/Protobuf.MSBuild.dll"),
    "protograin": ("Proto.Cluster.CodeGen", "tasks/net8.0/Proto.Cluster.CodeGen.dll"),
    "swagger": ("FastEndpoints.Swagger", "build/FastEndpoints.Swagger.targets"),
}
PHYSICAL_GENERATED_PROJECTS = {
    ("Elsa.Caching.Distributed.ProtoActor", 'extensions/src/caching/Elsa.Caching.Distributed.ProtoActor/Elsa.Caching.Distributed.ProtoActor.csproj'):
        ((r"Proto/LocalCacheMessages\.cs", "grpc"),),
    ("Elsa.Workflows.Runtime.ProtoActor", 'extensions/src/runtimes/Elsa.Workflows.Runtime.ProtoActor/Elsa.Workflows.Runtime.ProtoActor.csproj'):
        ((r"Proto/(Shared|WorkflowInstanceMessages)\.cs", "grpc"),
         (r"protopotato/WorkflowInstance-[0-9A-F]{32}\.cs", "protograin")),
    ("Elsa.Api.Common", 'core/src/common/Elsa.Api.Common/Elsa.Api.Common.csproj'):
        ((r"SwaggerExportPathInitializer\.g\.cs", "swagger"),),
}


def restored_assets(root: Path, row: dict, framework: str) -> dict:
    record = next((item for item in row["restore_assets"] if item["framework"] == framework), None)
    require(record is not None, f"Missing restored dependency evidence: {row['id']}/{framework}")
    data = (root / record["path"]).read_bytes()
    require(hashlib.sha256(data).hexdigest() == record["sha256"], "Restored dependency evidence changed")
    return json.loads(data)


def restored_archive(assets: dict, identifier: str, version: str, *, targeting_pack: bool = False,
                     cache: dict | None = None) -> tuple[Path, dict]:
    key = f"{identifier}/{version}"
    library = assets.get("libraries", {}).get(key, {})
    if targeting_pack:
        downloads = [item for frame in assets.get("project", {}).get("frameworks", {}).values()
                     for item in frame.get("downloadDependencies", [])]
        require(library.get("type") == "package" or any(item["name"] == identifier and
                item["version"].replace(" ", "") == f"[{version},{version}]" for item in downloads),
                f"Generator targeting pack is not restored at its resolved version: {key}")
    else:
        require(library.get("type") == "package" and bool(library.get("sha512")),
                f"Generator is not an actual versioned restored dependency: {key}")
    relative = library.get("path", f"{identifier.lower()}/{version.lower()}")
    candidates = [Path(folder) / relative / f"{identifier.lower()}.{version.lower()}.nupkg"
                  for folder in assets.get("packageFolders", {})]
    candidates = [path for path in candidates if path.is_file() and not path.is_symlink()]
    require(bool(candidates), f"Missing restored generator archive: {key}")
    archive = candidates[0]
    data = archive.read_bytes()
    archive_hash = hashlib.sha256(data).hexdigest()
    require(zipfile.is_zipfile(archive), f"Malformed restored generator archive: {key}")
    with zipfile.ZipFile(archive) as contents:
        signed = ".signature.p7s" in archive_names(contents)
    content_hash = base64.b64encode(hashlib.sha512(data).digest()).decode()
    if signed:
        cache = cache if cache is not None else {}
        hash_key = ("nuget_content_hash", str(archive), archive_hash)
        if hash_key not in cache:
            require("archive_inspector" in cache, "Signed generator archive requires SDK NuGet integrity inspector")
            inspected = json.loads(run(["dotnet", str(cache["archive_inspector"]), "--inspect-archive", str(archive)], archive.parent))
            require(inspected["signed"] is True and inspected["archive_sha256"] == archive_hash,
                    "SDK archive inspection did not identify unchanged signed bytes")
            cache[hash_key] = inspected["content_hash"]
        content_hash = cache[hash_key]
    expected = library.get("sha512")
    if expected:
        require(content_hash == expected,
                f"Generator archive differs from restored dependency hash: {key}")
    else:
        # Framework download dependencies carry their hash beside the nupkg,
        # while the immutable assets snapshot records the exact pack version.
        expected = archive.with_suffix(archive.suffix + ".sha512").read_text().strip()
        require(base64.b64encode(hashlib.sha512(data).digest()).decode() == expected,
                f"Generator targeting archive hash differs: {key}")
    return archive, {"package_id": identifier, "package_version": version,
                     "archive_path": str(archive), "archive_sha256": archive_hash,
                     "restore_sha512": expected, "nuget_content_hash": content_hash, "signed": signed}


def package_tool(assets: dict, identifier: str, version: str, entry: str, *, targeting_pack: bool = False,
                 cache: dict | None = None) -> dict:
    archive, record = restored_archive(assets, identifier, version, targeting_pack=targeting_pack, cache=cache)
    with zipfile.ZipFile(archive) as contents:
        names = archive_names(contents)
        require(entry in names, f"Missing audited generator content: {identifier}/{entry}")
        data = contents.read(entry)
        # Check the generator's supplied binaries/tasks, including the native
        # protoc executables selected by Grpc.Tools on different platforms.
        prefixes = ("analyzers/",) if entry.startswith("analyzers/") else (str(PurePosixPath(entry).parent) + "/",)
        if identifier == "Grpc.Tools":
            prefixes += ("tools/",)
        checked = {}
        for name in names:
            if name.startswith(prefixes) and (name.endswith((".dll", ".exe", ".targets", ".props")) or PurePosixPath(name).name == "protoc"):
                payload = contents.read(name)
                extracted = archive.parent / name
                require(extracted.is_file() and not extracted.is_symlink() and extracted.read_bytes() == payload,
                        f"Extracted generator content differs from restored archive: {identifier}/{name}")
                checked[name] = hashlib.sha256(payload).hexdigest()
    tool = archive.parent / entry
    require(tool.is_file() and not tool.is_symlink() and tool.read_bytes() == data,
            f"Extracted generator content differs from restored archive: {identifier}/{entry}")
    return {**record, "kind": "nuget", "archive_entry": entry, "tool_path": str(tool),
            "content_sha256": hashlib.sha256(data).hexdigest(), "targeting_pack": targeting_pack,
            "checked_archive_contents": checked}


def capture_compiler_evidence(root: Path, row: dict, framework: str, resolved: dict, cache: dict | None = None) -> dict:
    cache = cache if cache is not None else {}
    properties = resolved["Properties"]
    sdk = Path(properties["MSBuildToolsPath"]).resolve()
    require(sdk.name == properties["NETCoreSdkVersion"], "SDK root does not match resolved SDK version")
    assets = restored_assets(root, row, framework)
    evidence = {"sdk_version": properties["NETCoreSdkVersion"], "sdk_root": str(sdk),
                "frameworks": [{key: item[key] for key in ("Identity", "TargetingPackName", "TargetingPackVersion", "TargetingPackPath")}
                               for item in resolved["Items"]["ResolvedFrameworkReference"]], "tools": {}}
    compiler = sdk / "Roslyn/bincore/csc.dll"
    require(compiler.is_file(), "Missing compiler in resolved SDK")
    evidence["compiler_sha256"] = hashlib.sha256(compiler.read_bytes()).hexdigest()
    evidence["compile_inputs"] = []
    tracked_key = ("compile_input_paths", cache.get("source_commit", "HEAD"))
    if tracked_key not in cache:
        cache[tracked_key] = set(run(["git", "ls-tree", "-r", "--name-only", tracked_key[1]], root).splitlines())
    for item in resolved["Items"]["Compile"]:
        path = Path(item["FullPath"]).resolve()
        relative = path.relative_to(root).as_posix() if path.is_relative_to(root) else str(path)
        evidence["compile_inputs"].append({"path": relative, "tracked": relative in cache[tracked_key]})
    for family, (filename, packages) in GENERATOR_TOOLS.items():
        matches = [item for item in resolved["Items"]["Analyzer"] if Path(item["Identity"]).name == filename]
        require(len(matches) <= 1, f"Ambiguous resolved generator: {row['id']}/{framework}/{family}")
        if not matches:
            continue
        item = matches[0]
        path = Path(item["Identity"]).resolve()
        if family == "razor":
            require(path.is_relative_to(sdk) and path.is_file(), "Razor generator is outside resolved SDK")
            record = {"kind": "sdk", "tool_path": str(path), "sdk_version": evidence["sdk_version"],
                      "content_sha256": hashlib.sha256(path.read_bytes()).hexdigest()}
            targets = sdk / "Sdks/Microsoft.NET.Sdk.Razor/targets/Sdk.Razor.CurrentVersion.targets"
            record["source_emitting_targets_path"] = str(targets)
            record["source_emitting_targets_sha256"] = hashlib.sha256(targets.read_bytes()).hexdigest()
        else:
            identifier, version = item.get("NuGetPackageId"), item.get("NuGetPackageVersion")
            # Installed SDK targeting packs need not be NuGet restore libraries.
            pack = next((pack for pack in evidence["frameworks"]
                         if path.is_relative_to(Path(pack["TargetingPackPath"]).resolve())), None)
            if pack and not identifier:
                identifier, version = pack["TargetingPackName"], pack["TargetingPackVersion"]
            require(identifier in packages and bool(version), f"Unexpected resolved generator identity: {family}/{identifier}")
            if pack is not None or identifier in {"Microsoft.NETCore.App.Ref", "Microsoft.AspNetCore.App.Ref"}:
                require(pack is not None and identifier == pack["TargetingPackName"] and version == pack["TargetingPackVersion"],
                        f"Generator identity differs from resolved targeting pack: {family}/{identifier}/{version}")
                relative = path.relative_to(Path(pack["TargetingPackPath"]).resolve()).as_posix()
                require(re.fullmatch(r"analyzers/dotnet/(?:roslyn[0-9]+\.[0-9]+/)?cs/" + re.escape(filename), relative) is not None,
                        f"Generator is outside resolved targeting pack analyzer path: {family}/{relative}")
            package_root = next((Path(folder) / identifier.lower() / version.lower() for folder in assets.get("packageFolders", {})
                                 if path.is_relative_to(Path(folder).resolve())), None)
            if package_root:
                record = package_tool(assets, identifier, version, path.relative_to(package_root).as_posix(), targeting_pack=pack is not None, cache=cache)
            else:
                require(pack is not None and path.is_relative_to(Path(properties["NetCoreRoot"]).resolve() / "packs"),
                        f"Generator is outside resolved SDK/targeting pack: {family}")
                record = {"kind": "framework", "package_id": identifier, "package_version": version,
                          "tool_path": str(path), "content_sha256": hashlib.sha256(path.read_bytes()).hexdigest()}
        evidence["tools"][family] = record
    for _, family in PHYSICAL_GENERATED_PROJECTS.get((row["id"], row["project"]), ()):
        identifier, entry = PHYSICAL_GENERATORS[family]
        require("RuntimeIdentifier" in properties, "Missing resolved RuntimeIdentifier for physical generator target selection")
        target_key = framework + (f"/{properties['RuntimeIdentifier']}" if properties["RuntimeIdentifier"] else "")
        target = assets.get("targets", {}).get(target_key)
        require(isinstance(target, dict), f"Missing exact restored generator target: {target_key}")
        selected = {key: item for key, item in target.items() if key.split("/", 1)[0] == identifier}
        require(all(item.get("type") == "package" for item in selected.values()), f"Selected generator is not a restored package: {identifier}/{target_key}")
        versions = [key.split("/", 1)[1] for key in selected]
        # Swagger's target exists only in its newer package train.
        if family == "swagger" and not versions:
            continue
        require(len(versions) == 1, f"Missing/ambiguous physical generator dependency: {identifier}")
        archive, _ = restored_archive(assets, identifier, versions[0], cache=cache)
        with zipfile.ZipFile(archive) as contents:
            if family == "swagger" and entry not in contents.namelist():
                continue
        evidence["tools"][family] = package_tool(assets, identifier, versions[0], entry, cache=cache)
    return evidence


def verify_generator_identity(root: Path, row: dict, framework: str, family: str, cache: dict) -> dict:
    key = ("generator", row["project"], framework, family)
    if key in cache:
        return cache[key]
    evidence = row["framework_properties"][framework].get("compiler_evidence", {})
    require(bool(evidence.get("sdk_version")) and bool(evidence.get("frameworks")), "Missing resolved SDK/framework evidence")
    if family == "sdk":
        sdk = Path(evidence["sdk_root"])
        require(sdk.name == evidence["sdk_version"] and hashlib.sha256((sdk / "Roslyn/bincore/csc.dll").read_bytes()).hexdigest() == evidence["compiler_sha256"],
                "Pinned SDK compiler content changed")
        restored_assets(root, row, framework)
        result = {"kind": "sdk", "sdk_version": evidence["sdk_version"], "compiler_sha256": evidence["compiler_sha256"],
                  "frameworks": evidence["frameworks"]}
    else:
        record = evidence.get("tools", {}).get(family)
        require(record is not None, f"Missing actual resolved generator evidence: {row['id']}/{framework}/{family}")
        path = Path(record["tool_path"])
        require(path.is_file() and not path.is_symlink() and hashlib.sha256(path.read_bytes()).hexdigest() == record["content_sha256"],
                f"Resolved generator content changed: {family}")
        if record["kind"] == "nuget":
            assets = restored_assets(root, row, framework)
            current = package_tool(assets, record["package_id"], record["package_version"], record["archive_entry"], targeting_pack=record["targeting_pack"], cache=cache)
            require(current == record, f"Resolved generator archive/identity changed: {family}")
        elif record["kind"] == "sdk":
            require(record["sdk_version"] == evidence["sdk_version"] and path.is_relative_to(Path(evidence["sdk_root"])),
                    "Generator does not belong to pinned SDK")
            targets = Path(record["source_emitting_targets_path"])
            require(targets.is_relative_to(Path(evidence["sdk_root"])) and hashlib.sha256(targets.read_bytes()).hexdigest() == record["source_emitting_targets_sha256"],
                    "SDK source-emitting targets changed")
        else:
            require(record["kind"] == "framework" and any(pack["TargetingPackName"] == record["package_id"] and
                    pack["TargetingPackVersion"] == record["package_version"] and path.is_relative_to(Path(pack["TargetingPackPath"]).resolve())
                    for pack in evidence["frameworks"]), "Generator does not belong to resolved framework")
        result = {key: value for key, value in record.items() if key != "tool_path"}
    cache[key] = result
    return result


def sdk_document_paths(row: dict, framework: str) -> set[str]:
    project = PurePosixPath(row["project"])
    prefix = f"{project.parent}/obj/Release/{framework}/"
    return {prefix + name for name in (f"{project.stem}.AssemblyInfo.cs", f"{project.stem}.GlobalUsings.g.cs",
                                      f".NETCoreApp,Version=v{framework.removeprefix('net')}.AssemblyAttributes.cs")}


def only_abstract_methods(inspection: dict) -> bool:
    counts = [inspection.get(name) for name in
              ("executable_method_bodies", "nonabstract_methods_without_body", "native_or_external_methods")]
    return all(type(count) is int and count == 0 for count in counts)


def generated_family(row: dict, framework: str, relative: str) -> str | None:
    project = PurePosixPath(row["project"])
    prefix = f"{project.parent}/obj/Release/{framework}/"
    if not relative.startswith(prefix):
        return None
    name = relative[len(prefix):]
    if any(part in ("", ".", "..") for part in name.split("/")):
        return None
    if relative in sdk_document_paths(row, framework):
        return "sdk"
    if framework == "net10.0" and name in ("EmbeddedAttribute.cs", "ValidatableTypeAttribute.cs"):
        return "razor"
    for pattern, family in GENERATED_FAMILIES + PHYSICAL_GENERATED_PROJECTS.get((row["id"], row["project"]), ()):
        if re.fullmatch(pattern, name):
            return family
    return None


def verify_external_document(root: Path, row: dict, framework: str, document: dict, cache: dict) -> dict | None:
    match = GENERATOR_SOURCE.fullmatch(document["path"])
    if match is None:
        return None
    version, entry = match.groups()
    require(version in GENERATOR_SOURCE_PINS, f"Unreviewed external source package version: {version}")
    record = next((item for item in row["restore_assets"] if item["framework"] == framework), None)
    require(record is not None, f"Missing restored dependency evidence for external source: {row['id']}")
    assets_file = root / record["path"]
    assets_bytes = assets_file.read_bytes()
    require(hashlib.sha256(assets_bytes).hexdigest() == record["sha256"], "Restored dependency evidence changed")
    assets = json.loads(assets_bytes)
    key = f"Elsa.Platform.PackageManifest.Generator/{version}"
    require(assets.get("libraries", {}).get(key, {}).get("type") == "package",
            f"External PDB source package is not an actual restored dependency: {key}")
    cache_key = ("external", version)
    if cache_key not in cache:
        candidates = [Path(folder) / "elsa.platform.packagemanifest.generator" / version /
                      f"elsa.platform.packagemanifest.generator.{version}.nupkg"
                      for folder in assets.get("packageFolders", {})]
        candidates = [path for path in candidates if path.is_file() and not path.is_symlink()]
        require(bool(candidates), f"Missing pinned external archive: {key}")
        data = candidates[0].read_bytes()
        require(hashlib.sha256(data).hexdigest() == GENERATOR_SOURCE_PINS[version],
                f"External source archive differs from official feed pin: {key}")
        with zipfile.ZipFile(candidates[0]) as archive:
            names = archive_names(archive)
            sources = {name: archive.read(name) for name in names if name.startswith("contentFiles/") and name.endswith(".cs")}
            require(sources.keys() == GENERATOR_SOURCE_ENTRIES[version],
                    f"Pinned generator archive does not contain the exact audited source entries: {key}")
        cache[cache_key] = sources
    require(entry in cache[cache_key], f"External document is absent from pinned content sources: {entry}")
    checksum = hashlib.new(document["algorithm"], cache[cache_key][entry]).hexdigest()
    require(checksum == document["checksum"] and document.get("embedded_checksum") == checksum,
            f"External source document/embedded bytes differ from pinned archive: {entry}")
    return {"path": document["path"], "algorithm": document["algorithm"], "checksum": checksum,
            "external_package": key, "archive_entry": entry, "archive_sha256": GENERATOR_SOURCE_PINS[version],
            "feed": GENERATOR_FEED, "embedded": True, "remote_fetched": False}


def verify_documents(root: Path, row: dict, framework: str, inspection: dict,
                     commit: str, remote: bool, cache: dict | None = None) -> dict:
    cache = cache if cache is not None else {}
    maps = inspection.get("source_link", {}).get("documents", {})
    require(bool(maps), f"No SourceLink map: {row['id']}/{framework}")
    prefix = f"{RAW_URL}{commit}/"
    require(all(isinstance(url, str) and url.startswith(prefix) for url in maps.values()),
            f"SourceLink map does not resolve to exact Core head: {row['id']}/{framework}")
    documents = inspection.get("documents", [])
    seen = set()
    counts = {"tracked_documents": 0, "remote_documents": 0, "embedded_tracked_documents": 0,
              "embedded_generated_documents": 0, "embedded_external_documents": 0}
    if not documents:
        require((row["id"], row["project"]) == ("Elsa.DropIns.Core", 'extensions/src/dropins/Elsa.DropIns.Core/Elsa.DropIns.Core.csproj'),
                f"Portable PDB has no source documents: {row['id']}/{framework}")
        require(only_abstract_methods(inspection),
                "Interface-only source coverage exception requires zero executable, native or bodyless implemented methods")
        return {"framework": framework, **counts, "document_coverage": "not_applicable_interface_only",
                "documents": [], "executable_method_bodies": 0, "nonabstract_methods_without_body": 0, "native_or_external_methods": 0}
    records = []
    for document in documents:
        path = document["path"]
        require(path not in seen, f"Duplicate PDB document: {path}")
        seen.add(path)
        algorithm = document["algorithm"]
        checksum = document["checksum"]
        require(algorithm in ("sha1", "sha256") and bool(re.fullmatch(r"[0-9a-f]+", checksum)), "Invalid document checksum")
        embedded = document.get("embedded_checksum")
        require(embedded is None or embedded == checksum, f"Embedded source checksum mismatch: {path}")
        external = verify_external_document(root, row, framework, document, cache)
        if external is not None:
            counts["embedded_external_documents"] += 1
            records.append(external)
            continue
        url = source_url(path, maps)
        require(url is not None and url.startswith(prefix), f"Unmapped source document: {path}")
        relative = url[len(prefix):]
        require(not PurePosixPath(relative).is_absolute() and ".." not in PurePosixPath(relative).parts and "\\" not in relative,
                f"Unsafe source document path: {relative}")
        blob_key = ("blob", commit, relative)
        if blob_key not in cache:
            blob = subprocess.run(["git", "show", f"{commit}:{relative}"], cwd=root, capture_output=True)
            cache[blob_key] = blob.stdout if blob.returncode == 0 else None
        blob_bytes = cache[blob_key]
        if blob_bytes is not None:
            require(hashlib.new(algorithm, blob_bytes).hexdigest() == checksum, f"Tracked source differs from exact Git blob: {relative}")
            counts["tracked_documents"] += 1
            if embedded is not None:
                counts["embedded_tracked_documents"] += 1
            if remote:
                remote_key = ("remote", url)
                if remote_key not in cache:
                    with urllib.request.urlopen(url, timeout=45) as response:
                        cache[remote_key] = response.read()
                require(hashlib.new(algorithm, cache[remote_key]).hexdigest() == checksum, f"Remote source checksum mismatch: {url}")
                counts["remote_documents"] += 1
            records.append({"path": relative, "checksum": checksum, "algorithm": algorithm,
                            "embedded": embedded is not None, "remote_fetched": remote})
        else:
            family = generated_family(row, framework, relative)
            require(family is not None and embedded is not None,
                    f"Untracked source document has no audited generated-source policy: {relative}")
            identity = verify_generator_identity(root, row, framework, family, cache)
            counts["embedded_generated_documents"] += 1
            records.append({"path": relative, "checksum": checksum, "algorithm": algorithm,
                            "embedded": True, "generated": True, "remote_fetched": False,
                            "generator_family": family, "generator_identity": identity,
                            "evidence": "Embedded compiled bytes and declared generator identity; not independently regenerated"})
    if counts["tracked_documents"] == 0 and (row["id"], row["project"]) == ("Elsa.Studio", 'studio/src/bundles/Elsa.Studio/Elsa.Studio.csproj'):
        compile_inputs = row["framework_properties"][framework].get("compiler_evidence", {}).get("compile_inputs")
        require(isinstance(compile_inputs, list) and all(item["path"] in sdk_document_paths(row, framework) and item.get("tracked") is False for item in compile_inputs),
                "Metadata-only Studio bundle has authored or unaudited Compile inputs")
        require(only_abstract_methods(inspection) and {record["path"] for record in records} == sdk_document_paths(row, framework)
                and type(inspection.get("nonmodule_types")) is int and inspection["nonmodule_types"] == 0
                and all(record.get("generator_family") == "sdk" and record["embedded"] for record in records),
                "Metadata-only Studio bundle requires no declared types or implemented methods and exactly its three embedded SDK documents")
        return {"framework": framework, **counts, "document_coverage": "not_applicable_authored_code_metadata_bundle",
                "documents": records, "executable_method_bodies": 0, "nonabstract_methods_without_body": 0, "native_or_external_methods": 0}
    require(counts["tracked_documents"] > 0, f"No tracked source covered: {row['id']}/{framework}")
    return {"framework": framework, **counts, "documents": records}


def build_symbol_verifier(root: Path, output: Path) -> Path:
    helper = root / "scripts/integration-program/VerifyPackageSymbolPair/VerifyPackageSymbolPair.csproj"
    helper_out = output / "symbol-verifier"
    run(["dotnet", "build", str(helper), "--configuration", "Release", "--output", str(helper_out)],
        root, timeout=600, log=output / "symbol-verifier.log")
    return helper_out / "VerifyPackageSymbolPair.dll"


def provenance(root: Path, artifacts: Path, manifest: dict, output: Path, *, remote: bool,
               inspector: Path) -> list[dict]:
    executable = inspector
    results = []
    cache = {"archive_inspector": inspector}
    with tempfile.TemporaryDirectory(prefix="elsa-symbol-proof-") as temporary:
        temporary_path = Path(temporary)
        for row in manifest["packages"]:
            frames = []
            if not row["assemblies"]:
                results.append({"id": row["id"], "assembly_free": True, "frameworks": []})
                continue
            with zipfile.ZipFile(artifacts / row["nupkg"]) as package, zipfile.ZipFile(artifacts / row["snupkg"]) as symbols:
                for assembly in row["assemblies"]:
                    dll = temporary_path / Path(assembly["assembly"]).name
                    pdb = temporary_path / Path(assembly["pdb"]).name
                    dll.write_bytes(package.read(assembly["assembly"]))
                    pdb.write_bytes(symbols.read(assembly["pdb"]))
                    inspection = json.loads(run(["dotnet", str(executable), str(dll), str(pdb), "--inspect-documents"], root))
                    require(inspection["assembly_name"] == row["framework_properties"][assembly["framework"]]["assembly_name"], f"Packaged assembly identity mismatch: {row['id']}")
                    require(inspection["assembly_version"] == "3.10.0.0", f"Packaged assembly was not compiled with common release identity: {row['id']}")
                    require(inspection["informational_version"] == f"{manifest['version']}+{manifest['source_commit']}", f"Assembly informational version does not identify exact source head: {row['id']}")
                    frame = verify_documents(root, row, assembly["framework"], inspection, manifest["source_commit"], remote, cache)
                    frame.update({key: inspection[key] for key in ("assembly_name", "assembly_version", "informational_version")})
                    frames.append(frame)
            results.append({"id": row["id"], "frameworks": frames})
    return results


def clean_head(root: Path) -> str:
    require(not run(["git", "status", "--porcelain"], root).strip(), "Proof requires a clean committed worktree")
    commit = run(["git", "rev-parse", "HEAD"], root).strip()
    require(bool(re.fullmatch(r"[0-9a-f]{40}", commit)), "Proof requires a full Git commit")
    return commit


def write_json(path: Path, data: dict | list) -> None:
    path.write_text(json.dumps(data, indent=2, sort_keys=True) + "\n")


def require_empty_package_output(packages: Path) -> None:
    require(not packages.is_symlink(), "Canonical packages output must not be a symlink")
    require(not packages.exists() or (packages.is_dir() and not any(packages.iterdir())),
            "Canonical packages output must be absent or an empty directory before proof; use an isolated worktree")


def source_input_hashes(root: Path) -> dict[str, str]:
    from run_admission_proof import source_hashes, regular
    result = source_hashes(root)
    extra_paths = subprocess.check_output([
        "git", "-C", str(root), "ls-files", "-z", "--", "build.sh", "build.cmd", "build.ps1",
        ".nuke", ".github/actions", ".github/workflows/prove-consolidated-packages.yml", ".github/workflows/pr.yml", "icon.png",
    ], text=True).split("\0")
    require("build.sh" in extra_paths and ".github/workflows/prove-consolidated-packages.yml" in extra_paths,
            "Package proof source inputs are incomplete")
    result.update({path: hashlib.sha256(regular(root / path).read_bytes()).hexdigest()
                   for path in extra_paths if path})
    return dict(sorted(result.items()))


def main(*, mode: str = "proof") -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", required=True)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--inventory-only", action="store_true")
    parser.add_argument("--remote-sources", action="store_true", help="Fetch every tracked document from the exact pushed Core commit")
    if mode == "candidate":
        parser.add_argument("--baseline", type=Path, default=Path(__file__).with_name("consolidated-candidate-inventory-baseline.json"))
    args = parser.parse_args()
    validate_mode(args.version, mode)
    root = Path(__file__).resolve().parents[2]
    for name in ("IsPackable", "PackageVersion", "GeneratePackageOnBuild", "TargetFramework", "TargetFrameworks", "Version", "Configuration", "ConsolidatedPackageProof", "ConsolidatedReleaseCandidate"):
        require(name not in os.environ, f"Proof refuses an inherited MSBuild override: {name}")
    output = args.output.resolve()
    require(not output.exists() and not output.is_relative_to(root), "Output must be a new directory outside the worktree")
    output.mkdir(parents=True)
    commit = clean_head(root)
    initial_sources = source_input_hashes(root)
    write_json(output / "source-inputs.json", initial_sources)
    manifest = inventory(root, args.version, commit, mode=mode)
    write_json(output / "inventory.json", manifest)
    if mode == "candidate":
        from prepare_consolidated_release_candidate import compare_inventory
        compare_inventory(manifest, json.loads(args.baseline.read_text()), output / "inventory-diff.json")
    print(f"Evaluated {len(manifest['packages'])} packages and {len(manifest['exclusions'])} exclusions", flush=True)
    if args.inventory_only:
        require(clean_head(root) == commit and source_input_hashes(root) == initial_sources, "Source changed during inventory evaluation")
        return
    packages = root / "packages"
    require_empty_package_output(packages)
    clientlibs = build_clientlibs(root, output)
    manifest["browser_assets"] = clientlibs["assets"]
    write_json(output / "studio-clientlibs.json", clientlibs)
    environment = dict(os.environ, ConsolidatedPackageProof=str(mode == "proof").lower(),
                       ConsolidatedReleaseCandidate=str(mode == "candidate").lower(), ContinuousIntegrationBuild="true")
    inputs = {"sdk": run(["dotnet", "--version"], root).strip(),
              "node": run(["node", "--version"], root).strip(), "configuration": "Release",
              "Version": args.version, "PackageVersion": args.version, "mode": mode}
    manifest["build_inputs"] = inputs
    write_json(output / "build-inputs.json", inputs)
    run([str(root / "build.sh"), "Compile+Pack", "--configuration", "Release", "--version", args.version], root,
        timeout=7200, log=output / "compile-pack.log", env=environment)
    artifacts = output / "artifacts"
    shutil.copytree(packages, artifacts)
    inspector = build_symbol_verifier(root, output)
    stage_sdk_metadata(root, manifest, output, inspector)
    manifest = verify_artifacts(artifacts, manifest, require_sdk_metadata=True)
    write_json(output / "verified-artifacts.json", manifest)
    sources = provenance(root, artifacts, manifest, output, remote=args.remote_sources, inspector=inspector)
    write_json(output / "source-provenance.json", sources)
    from prove_consolidated_package_consumers import prove
    consumers = prove(artifacts, manifest, output / "consumers") if mode == "proof" else {"status": "required_downstream_exact_archive"}
    from prove_consolidated_admission_consumers import prove as prove_admission
    admission_consumers = (prove_admission(artifacts, manifest, output / "admission-consumers")
                           if mode == "proof" else {"status": "required_downstream_exact_archive"})
    require(clean_head(root) == commit and source_input_hashes(root) == initial_sources, "Source changed during package proof")
    receipt = {"result": "passed", "mode": mode, "build_inputs": inputs, "published": False, "source_commit": commit, "version": args.version,
               "package_count": len(manifest["packages"]), "exclusion_count": len(manifest["exclusions"]),
               "remote_sources_verified": args.remote_sources, "sdk_metadata_verified": True,
               "provenance": sources, "consumers": consumers, "admission_consumers": admission_consumers,
               "source_inputs_sha256": hashlib.sha256((output / "source-inputs.json").read_bytes()).hexdigest(), "source_inputs_unchanged": True,
               "limits": ["Consumers are representative; this is not behavioral certification of every package.",
                          "No publication, publisher cutover, npm artifact proof or live Slack certification."]}
    if not args.remote_sources:
        receipt["limits"].append("Tracked sources checked against exact local Git blobs; remote SourceLink fetch remains unverified.")
    write_json(output / "receipt.json", receipt)
    if mode == "candidate":
        from prepare_consolidated_release_candidate import seal
        seal(output, commit)
    print(f"Package {mode} passed: {output / 'receipt.json'}", flush=True)


if __name__ == "__main__":
    main()
