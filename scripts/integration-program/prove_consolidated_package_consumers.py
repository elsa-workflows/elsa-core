#!/usr/bin/env python3
"""Prove representative consolidated NuGet packages through fresh package-only consumers."""

from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import signal
import subprocess
import tempfile
import zipfile
from typing import Any
from xml.etree import ElementTree


FRAMEWORKS = ("net8.0", "net9.0", "net10.0")
NUGET_ORG = "https://api.nuget.org/v3/index.json"
REQUIRED_PACKAGES = (
    "Elsa",
    "Elsa.Slack",
    "Elsa.WorkflowContexts",
    "Elsa.Studio.WorkflowContexts",
    "Elsa.Studio.Core",
)
AUDITED_EXTERNAL_ELSA_IDS = {
    "elsa.platform.packagemanifest",
    "elsa.platform.packagemanifest.generator",
}
PACKAGE_ID_PATTERN = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._-]*$")
VERSION_PATTERN = re.compile(r"^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$")
COMMAND_TIMEOUT_SECONDS = 1200
RUN_TIMEOUT_SECONDS = 300
RESTORE_LOG = "consolidated-consumer-restore.log"
BUILD_LOG = "consolidated-consumer-build.log"
RUN_LOG = "consolidated-consumer-run-{}.log"
FIXTURE = Path(__file__).resolve().parent / "consolidated-package-consumer"


def render_nuget_config(
    artifacts: Path,
    internal_package_ids: list[str],
    external_package_exceptions: dict[str, str],
) -> str:
    """Route produced IDs to the local feed and every other ID to nuget.org."""
    internal_ids = sorted(internal_package_ids, key=str.casefold)
    if not internal_ids or len({package_id.casefold() for package_id in internal_ids}) != len(internal_ids):
        raise ValueError("Internal package IDs must be nonempty and unique")

    exceptions_by_id = {package_id.casefold(): package_id for package_id in external_package_exceptions}
    if set(exceptions_by_id) - AUDITED_EXTERNAL_ELSA_IDS:
        raise ValueError("External Elsa package exceptions must use the exact audited package IDs")
    if set(exceptions_by_id) & {package_id.casefold() for package_id in internal_ids}:
        raise ValueError("A package cannot be both a produced package and an external exception")
    if any(not isinstance(reason, str) or not reason.strip() for reason in external_package_exceptions.values()):
        raise ValueError("Every external package exception requires a nonempty review reason")

    root = ElementTree.Element("configuration")
    sources = ElementTree.SubElement(root, "packageSources")
    ElementTree.SubElement(sources, "clear")
    ElementTree.SubElement(sources, "add", {"key": "local-proof", "value": str(artifacts.resolve())})
    ElementTree.SubElement(sources, "add", {"key": "nuget.org", "value": NUGET_ORG})

    mapping = ElementTree.SubElement(root, "packageSourceMapping")
    local = ElementTree.SubElement(mapping, "packageSource", {"key": "local-proof"})
    for package_id in internal_ids:
        ElementTree.SubElement(local, "package", {"pattern": package_id})

    public = ElementTree.SubElement(mapping, "packageSource", {"key": "nuget.org"})
    for package_id in sorted(external_package_exceptions, key=str.casefold):
        ElementTree.SubElement(public, "package", {"pattern": package_id})
    # Exact local patterns win over this wildcard. Anything not produced by
    # this manifest can only resolve from the public NuGet source.
    ElementTree.SubElement(public, "package", {"pattern": "*"})
    return ElementTree.tostring(root, encoding="unicode")


def validate_project_assets(
    assets: dict[str, Any],
    frameworks: list[str] | tuple[str, ...],
    internal_package_ids: set[str],
    proof_version: str,
    external_exception_ids: set[str],
    excluded_package_ids: set[str] | None = None,
) -> dict[str, Any]:
    """Reject source/project fallback and classify every restored Elsa package."""
    targets = assets.get("targets")
    if not isinstance(targets, dict) or set(targets) != set(frameworks):
        actual = sorted(targets) if isinstance(targets, dict) else []
        raise RuntimeError(f"Unexpected project.assets.json target frameworks: {actual}")

    internal_ids = {package_id.casefold() for package_id in internal_package_ids}
    external_ids = {package_id.casefold() for package_id in external_exception_ids}
    excluded_ids = {package_id.casefold() for package_id in (excluded_package_ids or set())} - external_ids
    library_records: dict[tuple[str, str], str] = {}
    framework_records: dict[str, list[dict[str, str]]] = {}
    library_frameworks: dict[tuple[str, str], set[str]] = {}

    for framework, target_libraries in targets.items():
        if not isinstance(target_libraries, dict):
            raise RuntimeError(f"Unexpected target library list for {framework}")
        framework_internal: list[dict[str, str]] = []
        for library_key, library in target_libraries.items():
            package_id, separator, version = library_key.partition("/")
            if not separator or not package_id or not version:
                raise RuntimeError(f"Unrecognized asset library identity: {library_key}")
            if not isinstance(library, dict) or library.get("type") != "package":
                raise RuntimeError(f"{library_key} must resolve as a NuGet package, not a project")

            folded_id = package_id.casefold()
            if folded_id in excluded_ids:
                raise RuntimeError(f"Restored explicitly excluded package {library_key}")
            elif folded_id in internal_ids:
                if version != proof_version:
                    raise RuntimeError(f"Internal package {library_key} is not the proof version {proof_version}")
                category = "internal"
                framework_internal.append({"id": package_id, "version": version})
            elif folded_id in external_ids:
                category = "external"
            elif folded_id.startswith("elsa"):
                raise RuntimeError(f"Restored unclassified Elsa package {library_key}")
            else:
                category = "other"

            key = (package_id, version)
            prior = library_records.get(key)
            if prior is not None and prior != category:
                raise RuntimeError(f"Inconsistent package classification for {library_key}")
            library_records[key] = category
            library_frameworks.setdefault(key, set()).add(framework)
        framework_records[framework] = sorted(framework_internal, key=lambda item: item["id"].casefold())

    restored_internal = sorted(
        ({"id": package_id, "version": version} for (package_id, version), category in library_records.items()
         if category == "internal"),
        key=lambda item: item["id"].casefold(),
    )
    restored_external = sorted(
        ({"id": package_id, "version": version} for (package_id, version), category in library_records.items()
         if category == "external"),
        key=lambda item: item["id"].casefold(),
    )
    restored_other = sorted(
        ({"id": package_id, "version": version} for (package_id, version), category in library_records.items()
         if category == "other"),
        key=lambda item: item["id"].casefold(),
    )
    package_frameworks = [
        {"id": package_id, "version": version, "frameworks": sorted(selected_frameworks)}
        for (package_id, version), selected_frameworks in sorted(
            library_frameworks.items(), key=lambda item: (item[0][0].casefold(), item[0][1])
        )
    ]
    return {
        "internal": restored_internal,
        "external": restored_external,
        "other": restored_other,
        "by_framework": framework_records,
        "package_frameworks": package_frameworks,
    }


def base64_sha512(content: bytes) -> str:
    return base64.b64encode(hashlib.sha512(content).digest()).decode("ascii")


def verify_cached_package(
    package_id: str,
    version: str,
    package: Path,
    packages_root: Path,
    artifacts: Path,
) -> dict[str, str]:
    package_bytes = package.read_bytes()
    cache_entry = packages_root / package_id.lower() / version
    cached_package = cache_entry / package.name.lower()
    metadata_path = cache_entry / ".nupkg.metadata"
    if not cached_package.is_file() or cached_package.read_bytes() != package_bytes:
        raise RuntimeError(f"NuGet cache does not contain the exact verified nupkg for {package_id}/{version}")
    if not metadata_path.is_file():
        raise RuntimeError(f"NuGet cache is missing .nupkg.metadata for {package_id}/{version}")
    metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
    expected_source = str(artifacts.resolve())
    if metadata.get("source") != expected_source:
        raise RuntimeError(f"Unexpected package source for {package_id}/{version}: {metadata.get('source')}")
    sha512_path = cached_package.with_suffix(cached_package.suffix + ".sha512")
    if not sha512_path.is_file() or sha512_path.read_text(encoding="utf-8").strip() != base64_sha512(package_bytes):
        raise RuntimeError(f"NuGet SHA-512 cache entry does not match the verified nupkg for {package_id}/{version}")
    return {
        "id": package_id,
        "version": version,
        "assets_type": "package",
        "source": expected_source,
        "sha256": hashlib.sha256(package_bytes).hexdigest(),
        "sha512": hashlib.sha512(package_bytes).hexdigest(),
    }


def verify_external_cache_source(
    package_id: str,
    version: str,
    packages_root: Path,
) -> dict[str, str]:
    metadata_path = packages_root / package_id.lower() / version / ".nupkg.metadata"
    if not metadata_path.is_file():
        raise RuntimeError(f"NuGet cache is missing .nupkg.metadata for reviewed external {package_id}/{version}")
    metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
    if metadata.get("source") != NUGET_ORG:
        raise RuntimeError(f"Reviewed external package {package_id}/{version} did not resolve from nuget.org")
    return {"id": package_id, "version": version, "assets_type": "package", "source": NUGET_ORG}


def _validated_manifest(
    manifest: dict[str, Any],
    required_packages: tuple[str, ...] = REQUIRED_PACKAGES,
) -> tuple[str, str, dict[str, dict[str, Any]], dict[str, str], set[str]]:
    version = manifest.get("version")
    source_commit = manifest.get("source_commit")
    if not isinstance(version, str) or not version.strip():
        raise ValueError("Manifest requires a package proof version")
    if not VERSION_PATTERN.fullmatch(version):
        raise ValueError("Manifest package proof version is not a safe SemVer value")
    if not isinstance(source_commit, str) or not source_commit.strip():
        raise ValueError("Manifest requires a pinned source_commit")

    packages = manifest.get("packages")
    if not isinstance(packages, list) or not packages:
        raise ValueError("Manifest requires a nonempty packages list")
    package_by_id: dict[str, dict[str, Any]] = {}
    for package in packages:
        if not isinstance(package, dict):
            raise ValueError("Every manifest package must be an object")
        package_id = package.get("id")
        frameworks = package.get("frameworks")
        filename = package.get("nupkg")
        if not isinstance(package_id, str) or not package_id.strip():
            raise ValueError("Every manifest package requires an id")
        if not PACKAGE_ID_PATTERN.fullmatch(package_id):
            raise ValueError(f"Manifest package ID is not a safe NuGet identity: {package_id!r}")
        if not isinstance(frameworks, list) or not all(isinstance(item, str) for item in frameworks):
            raise ValueError(f"Manifest package {package_id} requires a frameworks list")
        if not isinstance(filename, str) or not filename or Path(filename).name != filename or not filename.endswith(".nupkg"):
            raise ValueError(f"Manifest package {package_id} requires a safe nupkg filename")
        if filename.casefold() != f"{package_id}.{manifest.get('version', '')}.nupkg".casefold():
            raise ValueError(f"Manifest nupkg filename does not match package ID and version: {filename}")
        folded_id = package_id.casefold()
        if folded_id in package_by_id:
            raise ValueError(f"Duplicate manifest package id {package_id}")
        package_by_id[folded_id] = package

    missing = [package_id for package_id in required_packages if package_id.casefold() not in package_by_id]
    if missing:
        raise ValueError(f"Manifest is missing required representative packages: {missing}")

    exclusions = manifest.get("exclusions", [])
    if not isinstance(exclusions, list):
        raise ValueError("Manifest exclusions must be a list")
    excluded_ids: set[str] = set()
    for item in exclusions:
        if not isinstance(item, dict) or not isinstance(item.get("id"), str) or not item["id"].strip():
            raise ValueError("Every manifest exclusion requires an id")
        excluded_ids.add(item["id"].casefold())
    if excluded_ids & {package_id.casefold() for package_id in required_packages}:
        raise ValueError("A required representative package is declared excluded")

    external_exceptions = manifest.get("external_package_exceptions")
    if not isinstance(external_exceptions, dict):
        raise ValueError("Manifest requires the reviewed external_package_exceptions map")
    exception_ids = {package_id.casefold() for package_id in external_exceptions}
    if exception_ids != AUDITED_EXTERNAL_ELSA_IDS:
        raise ValueError("Manifest external package exceptions must exactly name the audited Platform.PackageManifest IDs")
    if any(not isinstance(reason, str) or not reason.strip() for reason in external_exceptions.values()):
        raise ValueError("Every external package exception requires a review reason")
    if exception_ids & set(package_by_id):
        raise ValueError("A reviewed external package ID cannot also be listed as produced")
    if excluded_ids & set(package_by_id):
        raise ValueError("A manifest package cannot also be declared excluded")

    for package_id in required_packages:
        package = package_by_id[package_id.casefold()]
        if not set(FRAMEWORKS).issubset(package["frameworks"]):
            raise ValueError(f"Required representative package {package_id} does not support all {FRAMEWORKS}")
    return version, source_commit, package_by_id, external_exceptions, excluded_ids


def _create_project(path: Path, version: str) -> None:
    package_references = "\n".join(
        f'    <PackageReference Include="{package_id}" Version="{version}" />'
        for package_id in REQUIRED_PACKAGES
    )
    project = f"""<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworks>{';'.join(FRAMEWORKS)}</TargetFrameworks>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <DisableImplicitLibraryPacksFolder>true</DisableImplicitLibraryPacksFolder>
    <DisableImplicitNuGetFallbackFolder>true</DisableImplicitNuGetFallbackFolder>
  </PropertyGroup>
  <ItemGroup>
{package_references}
  </ItemGroup>
</Project>
"""
    if "ProjectReference" in project or "PackageReference" not in project:
        raise RuntimeError("Generated consumer must use PackageReference only")
    path.write_text(project, encoding="utf-8")


def _run_command(
    command: list[str],
    cwd: Path,
    environment: dict[str, str],
    log_path: Path,
    timeout_seconds: int,
) -> dict[str, Any]:
    log_path.parent.mkdir(parents=True, exist_ok=True)
    try:
        with log_path.open("wb") as log:
            process = subprocess.Popen(
                command,
                cwd=cwd,
                env=environment,
                stdout=log,
                stderr=subprocess.STDOUT,
                start_new_session=(os.name == "posix"),
            )
            try:
                return_code = process.wait(timeout=timeout_seconds)
            except BaseException:
                if process.poll() is None:
                    if os.name == "posix":
                        try:
                            os.killpg(process.pid, signal.SIGKILL)
                        except ProcessLookupError:
                            pass
                    else:
                        process.kill()
                process.wait()
                raise
    except subprocess.TimeoutExpired as error:
        raise RuntimeError(
            f"Command exceeded {timeout_seconds}s: {' '.join(command)}; inspect {log_path}"
        ) from error

    evidence = {"command": command, "exit_code": return_code, "log": str(log_path.resolve())}
    if return_code != 0:
        raise RuntimeError(f"Command failed ({return_code}): {' '.join(command)}; inspect {log_path}")
    return evidence


def _package_evidence(
    artifacts: Path,
    packages_root: Path,
    package_by_id: dict[str, dict[str, Any]],
    restored: dict[str, list[dict[str, str]]],
    external_exceptions: dict[str, str],
    version: str,
) -> list[dict[str, Any]]:
    manifest_by_folded_id = package_by_id
    frameworks_by_identity = {
        (item["id"].casefold(), item["version"]): item["frameworks"]
        for item in restored["package_frameworks"]
    }
    result: list[dict[str, Any]] = []
    for entry in restored["internal"]:
        package_id = entry["id"]
        package = manifest_by_folded_id.get(package_id.casefold())
        if package is None:
            raise RuntimeError(f"Restored internal package {package_id} is missing from the proof manifest")
        if entry["version"] != version:
            raise RuntimeError(f"Restored internal package {package_id} is not at proof version {version}")
        artifact = artifacts / package["nupkg"]
        if not artifact.is_file():
            raise RuntimeError(f"Verified package artifact is missing: {artifact}")
        package_bytes = artifact.read_bytes()
        expected_sha256 = package.get("nupkg_sha256")
        expected_sha512 = package.get("nupkg_sha512")
        if not isinstance(expected_sha256, str) or hashlib.sha256(package_bytes).hexdigest() != expected_sha256.casefold():
            raise RuntimeError(f"Verified artifact SHA-256 differs from manifest for {package_id}")
        if not isinstance(expected_sha512, str) or hashlib.sha512(package_bytes).hexdigest() != expected_sha512.casefold():
            raise RuntimeError(f"Verified artifact SHA-512 differs from manifest for {package_id}")
        evidence = verify_cached_package(package_id, version, artifact, packages_root, artifacts)
        evidence["frameworks"] = frameworks_by_identity[(package_id.casefold(), version)]
        result.append(evidence)

    external_entries = {package_id.casefold(): package_id for package_id in external_exceptions}
    for entry in restored["external"]:
        folded_id = entry["id"].casefold()
        if folded_id not in external_entries:
            raise RuntimeError(f"Restored external Elsa package {entry['id']} is not reviewed")
        evidence = verify_external_cache_source(entry["id"], entry["version"], packages_root)
        evidence["frameworks"] = frameworks_by_identity[(entry["id"].casefold(), entry["version"])]
        result.append(evidence)

    result.extend(
        {**verify_external_cache_source(entry["id"], entry["version"], packages_root),
            "id": entry["id"],
            "version": entry["version"],
            "assets_type": "package",
            "frameworks": frameworks_by_identity[(entry["id"].casefold(), entry["version"])],
            "source": "nuget.org via explicit package mapping",
        }
        for entry in restored["other"]
    )
    return sorted(result, key=lambda item: (item["id"].casefold(), item["version"]))


def prepare_isolation(root: Path, sdk: str) -> dict[str, str]:
    for name in ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props"):
        (root / name).write_text("<Project />\n")
    (root / "global.json").write_text(json.dumps({"sdk": {"version": sdk, "rollForward": "disable"}}) + "\n")
    return {name: hashlib.sha256((root / name).read_bytes()).hexdigest() for name in
            ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "global.json")}


def verify_restore_isolation(assets: dict, root: Path, cache: Path, artifacts: Path) -> None:
    restore = assets["project"]["restore"]
    if set(restore["sources"]) != {NUGET_ORG, str(artifacts.resolve())}:
        raise RuntimeError("Effective restore sources leaked")
    if {str(Path(path).resolve()) for path in assets["packageFolders"]} != {str(cache.resolve())}:
        raise RuntimeError("Effective package cache/fallback folders leaked")
    if restore.get("configFilePaths") != [str(root / "NuGet.Config")]:
        raise RuntimeError("Parent NuGet config leaked")


def verify_loaded_assemblies(result: dict, assets: dict, framework: str, root: Path,
                             cache: Path, artifacts: Path, by_id: dict, version: str, source: str,
                             *, required_assemblies: tuple[str, ...] | list[str] = REQUIRED_PACKAGES) -> list[dict]:
    if (not isinstance(required_assemblies, (tuple, list)) or not required_assemblies or
            any(not isinstance(name, str) or not name or name != name.strip() for name in required_assemblies) or
            len(set(required_assemblies)) != len(required_assemblies)):
        raise ValueError("Required assembly names must be nonempty, unique and explicit")
    expected = {}
    for key, library in assets["targets"][framework].items():
        package_id, package_version = key.split("/")
        for entry in library.get("runtime", {}):
            if not entry.endswith(".dll") or not Path(entry).name.startswith("Elsa"):
                continue
            cached = cache / package_id.lower() / package_version / entry
            if not cached.is_file() or cached.is_symlink():
                raise RuntimeError("Runtime asset missing from fresh package cache")
            digest = hashlib.sha256(cached.read_bytes()).hexdigest()
            internal = package_id.casefold() in by_id
            archive = artifacts / by_id[package_id.casefold()]["nupkg"] if internal else cache / package_id.lower() / package_version / f"{package_id.lower()}.{package_version}.nupkg"
            with zipfile.ZipFile(archive) as package:
                if hashlib.sha256(package.read(entry)).hexdigest() != digest:
                    raise RuntimeError("Cached runtime DLL differs from exact package asset")
            name = Path(entry).stem
            if name in expected:
                raise RuntimeError("Ambiguous restored runtime assembly identity")
            expected[name] = {"sha256": digest, "internal": internal, "id": package_id, "version": package_version, "asset": entry}
    loaded = result.get("loadedAssemblies", [])
    if not loaded or len({row["name"] for row in loaded}) != len(loaded):
        raise RuntimeError("Missing or duplicate loaded assembly identities")
    if not set(required_assemblies) <= {row["name"] for row in loaded}:
        raise RuntimeError("Representative loaded assembly missing")
    records = []
    for row in loaded:
        asset = expected.get(row["name"])
        location = Path(row["location"])
        if asset is None or row.get("sha256") != asset["sha256"] or not location.resolve().is_relative_to((root / "bin").resolve()):
            raise RuntimeError("Loaded assembly differs from restored package asset")
        if not row.get("fullName", "").startswith(row["name"] + ", Version=" + str(row.get("version")) + ",") or not row.get("informationalVersion"):
            raise RuntimeError("Incomplete loaded assembly identity")
        if asset["internal"] and (row["version"] != version.split("+", 1)[0].split("-", 1)[0] + ".0" or row["informationalVersion"] != f"{version}+{source}"):
            raise RuntimeError("Loaded internal assembly release/source identity mismatch")
        records.append({**row, "package_id": asset["id"], "package_version": asset["version"], "package_asset": asset["asset"]})
    return records


def prove(artifacts: Path, manifest: dict[str, Any], output: Path) -> dict[str, Any]:
    """Restore and execute a clean PackageReference-only consumer without publishing."""
    if os.name != "posix":
        raise RuntimeError("Consumer process-group cleanup requires POSIX (macOS or Linux)")
    version, source_commit, package_by_id, external_exceptions, excluded_package_ids = _validated_manifest(manifest)
    artifacts = artifacts.resolve(strict=True)
    if not artifacts.is_dir():
        raise ValueError(f"Artifacts path is not a directory: {artifacts}")
    for package_id in REQUIRED_PACKAGES:
        package = package_by_id[package_id.casefold()]
        artifact = artifacts / package["nupkg"]
        if not artifact.is_file():
            raise ValueError(f"Required verified package is missing: {artifact}")

    output = output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    receipt_path = output / "consumer-proof.json"
    if receipt_path.exists():
        raise FileExistsError(f"Refusing to overwrite prior consumer proof: {receipt_path}")
    inputs_path = output / "consumer-inputs"
    if inputs_path.exists():
        raise FileExistsError(f"Refusing to overwrite prior consumer inputs: {inputs_path}")

    package_ids = [package["id"] for package in package_by_id.values()]
    consumers: list[dict[str, Any]] = []
    with tempfile.TemporaryDirectory(prefix="elsa-consolidated-consumer-") as temporary:
        root = Path(temporary).resolve()
        sdk = manifest.get("build_inputs", {}).get("sdk")
        if not sdk:
            installed = subprocess.check_output(["dotnet", "--list-sdks"], text=True)
            sdks = [match.group(1) for line in installed.splitlines() if (match := re.match(r"(10\.[0-9]+\.[0-9]+) ", line))]
            if not sdks:
                raise RuntimeError("Representative consumers require an installed .NET 10 SDK")
            sdk = max(sdks, key=lambda value: tuple(int(part) for part in value.split(".")))
        isolation = prepare_isolation(root, sdk)
        project = root / "ConsolidatedPackageConsumer.csproj"
        _create_project(project, version)
        shutil.copy2(FIXTURE / "Program.cs", root / "Program.cs")
        shutil.copy2(FIXTURE / "HttpProbe.cs", root / "HttpProbe.cs")

        config = root / "NuGet.Config"
        config.write_text(
            render_nuget_config(artifacts, package_ids, external_exceptions),
            encoding="utf-8",
        )
        inputs_path.mkdir()
        for name in isolation:
            shutil.copy2(root / name, inputs_path / name)
        shutil.copy2(project, inputs_path / project.name)
        shutil.copy2(config, inputs_path / config.name)
        shutil.copy2(FIXTURE / "Program.cs", inputs_path / "Program.cs")
        shutil.copy2(FIXTURE / "HttpProbe.cs", inputs_path / "HttpProbe.cs")
        isolation.update({path.name: hashlib.sha256(path.read_bytes()).hexdigest()
                          for path in inputs_path.iterdir() if path.is_file()})

        packages_root = root / "fresh-packages"
        if packages_root.exists() and any(packages_root.iterdir()):
            raise RuntimeError("Fresh NuGet package cache was not empty before restore")
        environment = os.environ.copy()
        environment["NUGET_PACKAGES"] = str(packages_root)
        environment["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1"
        environment["MSBUILDDISABLENODEREUSE"] = "1"
        environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"

        commands: list[dict[str, Any]] = []
        commands.append(_run_command(
            ["dotnet", "restore", str(project), "--configfile", str(config), "--packages", str(packages_root),
             "--force-evaluate", "--no-cache", "--nologo", "--verbosity", "minimal"],
            root, environment, output / RESTORE_LOG, COMMAND_TIMEOUT_SECONDS,
        ))

        assets_path = root / "obj" / "project.assets.json"
        if not assets_path.is_file():
            raise RuntimeError("dotnet restore did not create project.assets.json")
        assets_bytes = assets_path.read_bytes()
        assets = json.loads(assets_bytes)
        verify_restore_isolation(assets, root, packages_root, artifacts)
        restored = validate_project_assets(
            assets,
            FRAMEWORKS,
            set(package_ids),
            version,
            set(external_exceptions),
            excluded_package_ids,
        )
        restored_internal_ids = {entry["id"].casefold() for entry in restored["internal"]}
        required_ids = {package_id.casefold() for package_id in REQUIRED_PACKAGES}
        if not required_ids.issubset(restored_internal_ids):
            raise RuntimeError(
                "A direct representative PackageReference did not resolve from the proof set: "
                f"{sorted(required_ids - restored_internal_ids)}"
            )
        for framework, framework_packages in restored["by_framework"].items():
            selected_ids = {entry["id"].casefold() for entry in framework_packages}
            if not required_ids.issubset(selected_ids):
                raise RuntimeError(
                    f"A direct representative PackageReference did not resolve for {framework}: "
                    f"{sorted(required_ids - selected_ids)}"
                )

        restored_package_evidence = _package_evidence(
            artifacts,
            packages_root,
            package_by_id,
            restored,
            external_exceptions,
            version,
        )
        commands.append(_run_command(
            ["dotnet", "build", str(project), "--configuration", "Release", "--no-restore", "--disable-build-servers", "--nologo", "--verbosity", "minimal"],
            root, environment, output / BUILD_LOG, COMMAND_TIMEOUT_SECONDS,
        ))

        framework_receipts: list[dict[str, Any]] = []
        for framework in FRAMEWORKS:
            run_command = [
                "dotnet", "run", "--project", str(project), "--no-build", "--no-restore",
                "--framework", framework, "--configuration", "Release",
            ]
            command_evidence = _run_command(
                run_command,
                root,
                environment,
                output / RUN_LOG.format(framework),
                RUN_TIMEOUT_SECONDS,
            )
            commands.append(command_evidence)
            proof_lines = [
                line.removeprefix("CONSUMER_PROOF=")
                for line in (output / RUN_LOG.format(framework)).read_text(encoding="utf-8").splitlines()
                if line.startswith("CONSUMER_PROOF=")
            ]
            if len(proof_lines) != 1:
                raise RuntimeError(f"Expected one CONSUMER_PROOF receipt for {framework}")
            result = json.loads(proof_lines[0])
            if result.get("framework") != framework or result.get("featureMatches") is not True:
                raise RuntimeError(f"Package feature handshake failed for {framework}")
            if result.get("httpRoundtrip") is not True or result.get("providerTypeResolves") is not True:
                raise RuntimeError(f"WorkflowContexts loopback descriptor handshake failed for {framework}")
            expected_assemblies = {package_id for package_id in REQUIRED_PACKAGES}
            actual_assemblies = result.get("assemblyChecks", {})
            if set(actual_assemblies) != expected_assemblies or any(
                actual_assemblies.get(package_id) != package_id for package_id in expected_assemblies
            ):
                raise RuntimeError(f"Representative package assembly checks failed for {framework}")
            loaded = verify_loaded_assemblies(result, assets, framework, root, packages_root, artifacts,
                                               package_by_id, version, source_commit)
            framework_receipts.append({"framework": framework, "result": result, "loaded_assemblies": loaded, "command": command_evidence})

        consumers.append({
            "name": "consolidated-package-consumer",
            "project": "ConsolidatedPackageConsumer.csproj",
            "package_reference_only": True,
            "fresh_cache": True,
            "frameworks": framework_receipts,
            "commands": commands,
        })

    receipt = {
        "published": False,
        "version": version,
        "source_commit": source_commit,
        "consumers": consumers,
        "restored_packages": restored_package_evidence,
        "consumer_inputs": str(inputs_path.resolve()),
        "isolation": isolation,
        "sdk": sdk,
        "assets_sha256": hashlib.sha256(assets_bytes).hexdigest(),
        "external_package_exceptions": [
            {
                "id": item["id"],
                "version": item["version"],
                "reason": next(
                    reason for package_id, reason in external_exceptions.items()
                    if package_id.casefold() == item["id"].casefold()
                ),
                "source": item["source"],
            }
            for item in restored_package_evidence
            if item["id"].casefold() in {package_id.casefold() for package_id in external_exceptions}
        ],
        "limitations": [
            "This representative consumer verifies package resolution and selected API contracts; it does not certify every package behavior.",
            "The consumer does not render the Studio designer or prove browser client-library runtime behavior.",
            "WorkflowContexts authorization uses a synthetic loopback identity and does not prove deployed authorization.",
        ],
        "publication_performed": False,
        "evidence_files": [
            str((output / RESTORE_LOG).resolve()),
            str((output / BUILD_LOG).resolve()),
            *(str((output / RUN_LOG.format(framework)).resolve()) for framework in FRAMEWORKS),
        ],
    }
    receipt_path.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    return receipt


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--artifacts", type=Path, required=True)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
    receipt = prove(args.artifacts, manifest, args.output)
    print(json.dumps(receipt, indent=2))


if __name__ == "__main__":
    main()
