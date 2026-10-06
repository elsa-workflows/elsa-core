"""Validate the exact NuGet provenance of a released Elsa package baseline."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path
import re
from typing import Any, Mapping
from xml.etree import ElementTree as ET
import zipfile

import paired_package_provenance as package_graph
import prove_consolidated_package_consumers as package_consumer
from run_persisted_workflow_upgrade_fixture import safe_members


NUGET_ORG = package_consumer.NUGET_ORG
POLICY_PATH = Path(__file__).resolve().parent / "paired-package-browser" / "baseline-source-policy.json"
RELEASE_COMMITS = {
    "elsa-core": {
        "3.8.4": "33181ae3048f628f591a0155b5665a8e4d1bcea2",
        "3.9.0": "6436609a1a3874d3fea7ccf690897792f5a1f702",
    },
    "elsa-studio": {
        "3.8.4": "9bff3f785fd13bd80a3a7ecf88fec4aec8eef7ae",
        "3.9.0": "a30ed7c997dfb3cff1d5d095a4ee19ff03c7fe42",
    },
    "elsa-extensions": {
        "3.8.4": "154ba15fb4da85b4bebecfbe43639579cbda1d0d",
        "3.9.0": "89d4eb9b739ae135604aac2a1de18653a289bdb0",
    },
}
REPOSITORIES = {
    "elsa-core": "https://github.com/elsa-workflows/elsa-core",
    "elsa-studio": "https://github.com/elsa-workflows/elsa-studio",
    "elsa-extensions": "https://github.com/elsa-workflows/elsa-extensions",
}
PLATFORM_IDS = {
    "Elsa.Platform.PackageManifest",
    "Elsa.Platform.PackageManifest.Generator",
}
LEGACY_EXCLUDED_IDS = {
    "Elsa.Secrets.Api",
    "Elsa.Secrets.Core",
    "Elsa.Secrets.Management",
    "Elsa.Secrets.Models",
    "Elsa.Secrets.Scripting",
}
HASH_PATTERN = re.compile(r"^[0-9a-f]{64}$")


def _read_policy(policy: Path | Mapping[str, Any]) -> dict[str, Any]:
    if isinstance(policy, Path):
        value = json.loads(policy.read_text(encoding="utf-8"))
    elif isinstance(policy, Mapping):
        value = dict(policy)
    else:
        raise TypeError("Baseline policy must be a JSON path or mapping")
    if not isinstance(value, dict) or value.get("schema_version") != 1:
        raise ValueError("Unsupported baseline source policy")
    if value.get("release_commits") != RELEASE_COMMITS:
        raise ValueError("Baseline source policy release commits differ from the reviewed 3.8.4/3.9.0 pins")
    if value.get("repositories") != REPOSITORIES:
        raise ValueError("Baseline source policy repository ownership map is invalid")

    records = value.get("packages")
    if not isinstance(records, list) or not records:
        raise ValueError("Baseline policy requires owned package archive records")
    seen: set[tuple[str, str]] = set()
    for record in records:
        if not isinstance(record, dict):
            raise ValueError("Baseline archive records must be objects")
        package_id, version = record.get("id"), record.get("version")
        owner, repository, digest = record.get("owner"), record.get("repository"), record.get("archive_sha256")
        if not isinstance(package_id, str) or not package_consumer.PACKAGE_ID_PATTERN.fullmatch(package_id):
            raise ValueError("Baseline archive record has an invalid exact package ID")
        if not package_id.startswith("Elsa"):
            raise ValueError(f"Baseline source record is not an exact Elsa package ID: {package_id}")
        if not isinstance(version, str) or version not in RELEASE_COMMITS["elsa-core"]:
            raise ValueError(f"Baseline archive record has an unsupported version: {version!r}")
        key = (package_id.casefold(), version)
        if key in seen:
            raise ValueError(f"Duplicate baseline archive tuple: {package_id}/{version}")
        seen.add(key)
        if owner not in RELEASE_COMMITS or not isinstance(repository, dict):
            raise ValueError(f"Baseline archive record has an unknown source owner: {package_id}/{version}")
        expected_repository = {
            "type": "git",
            "url": REPOSITORIES[owner],
            "commit": RELEASE_COMMITS[owner][version],
        }
        if repository != expected_repository:
            raise ValueError(f"Baseline source owner/commit does not match the exact package tuple: {package_id}/{version}")
        if not isinstance(digest, str) or not HASH_PATTERN.fullmatch(digest):
            raise ValueError(f"Baseline archive record has an invalid SHA-256: {package_id}/{version}")

    missing_records = value.get("missing_provenance")
    if not isinstance(missing_records, list):
        raise ValueError("Baseline policy must list unresolved tuples as missing_provenance")
    missing_seen: set[tuple[str, str]] = set()
    for record in missing_records:
        if not isinstance(record, dict) or record.get("status") != "missing_provenance":
            raise ValueError("Unresolved package tuples must remain explicitly marked missing_provenance")
        package_id, version = record.get("id"), record.get("version")
        if not isinstance(package_id, str) or not package_consumer.PACKAGE_ID_PATTERN.fullmatch(package_id):
            raise ValueError("Missing provenance record has an invalid package ID")
        if not package_id.startswith("Elsa"):
            raise ValueError(f"Missing provenance record is not an exact Elsa package ID: {package_id}")
        if not isinstance(version, str) or version not in RELEASE_COMMITS["elsa-core"]:
            raise ValueError(f"Missing provenance record has an unsupported version: {version!r}")
        key = (package_id.casefold(), version)
        if key in missing_seen or key in seen:
            raise ValueError(f"Duplicate or approved/missing baseline tuple: {package_id}/{version}")
        missing_seen.add(key)

    platform_records = value.get("platform_exceptions")
    if not isinstance(platform_records, list):
        raise ValueError("Baseline policy must classify Platform package exceptions separately")
    platform_by_id: dict[str, dict[str, Any]] = {}
    for record in platform_records:
        if not isinstance(record, dict) or not isinstance(record.get("id"), str):
            raise ValueError("Platform exception records must identify an exact package ID")
        package_id = record["id"]
        folded_id = package_id.casefold()
        if folded_id in platform_by_id:
            raise ValueError(f"Duplicate Platform exception record: {package_id}")
        if record.get("approved_tuples") != []:
            raise ValueError(f"Platform exception tuples require separate archive review: {package_id}")
        platform_by_id[folded_id] = record
    if set(platform_by_id) != {package_id.casefold() for package_id in PLATFORM_IDS}:
        raise ValueError("Baseline policy must list only the two exact unapproved Platform package IDs")

    exclusions = value.get("excluded_elsa_ids")
    if (not isinstance(exclusions, list) or not all(isinstance(item, str) for item in exclusions)
            or len(exclusions) != len(set(exclusions))):
        raise ValueError("Baseline policy must explicitly and uniquely list excluded Elsa package IDs")
    if set(exclusions) != LEGACY_EXCLUDED_IDS:
        raise ValueError("Baseline policy legacy Secrets exclusions differ from the reviewed exact IDs")
    return value


def _regular_cache_file(path: Path, root: Path, what: str) -> Path:
    resolved_root = root.resolve(strict=True)
    if root.is_symlink() or path.is_symlink() or not path.is_file():
        raise RuntimeError(f"Missing or symlinked {what}")
    for parent in path.parents:
        if parent == root:
            break
        if parent.is_symlink():
            raise RuntimeError(f"Symlink in {what} path")
    resolved = path.resolve(strict=True)
    if not resolved.is_relative_to(resolved_root):
        raise RuntimeError(f"{what} escaped the isolated package cache")
    return resolved


def _validate_restore_isolation(project: Path, cache: Path, assets: dict[str, Any]) -> None:
    project_root = project.parent.resolve(strict=True)
    cache_root = cache.resolve(strict=True)
    shared_cache = (Path.home() / ".nuget" / "packages").resolve()
    if cache.is_symlink() or cache_root == shared_cache or cache_root.is_relative_to(shared_cache):
        raise RuntimeError("Baseline package cache must be isolated from the shared user cache")

    config_path = project_root / "NuGet.Config"
    config_path = package_graph.regular_file(config_path)
    root = ET.parse(config_path).getroot()
    sources = root.findall("packageSources")
    if root.tag != "configuration" or len(sources) != 1 or len(root) != 1:
        raise RuntimeError("Baseline NuGet.Config must contain only one isolated packageSources section")
    children = list(sources[0])
    if len(children) != 2 or children[0].tag != "clear" or children[1].tag != "add":
        raise RuntimeError("Baseline NuGet.Config must clear inherited sources and add only nuget.org")
    if children[1].attrib != {"key": "nuget.org", "value": NUGET_ORG}:
        raise RuntimeError("Baseline NuGet.Config contains a non-NuGet.org package source")

    restore = assets.get("project", {}).get("restore", {})
    effective_sources = restore.get("sources") if isinstance(restore, dict) else None
    if not isinstance(effective_sources, dict) or set(effective_sources) != {NUGET_ORG}:
        raise RuntimeError("Baseline restore must use NuGet.org as its only effective source")
    if restore.get("configFilePaths") != [str(config_path)]:
        raise RuntimeError("Baseline restore did not use only its project-local NuGet.Config")
    packages_path = restore.get("packagesPath")
    if not isinstance(packages_path, str) or Path(packages_path).resolve() != cache_root:
        raise RuntimeError("Baseline restore did not use the requested isolated package cache")
    if restore.get("fallbackFolders"):
        raise RuntimeError("Baseline restore must not use fallback package folders")
    package_folders = assets.get("packageFolders")
    if not isinstance(package_folders, dict) or {Path(folder).resolve() for folder in package_folders} != {cache_root}:
        raise RuntimeError("Baseline assets contain an unexpected package or fallback folder")


def _read_package_nuspec(archive: Path, package_id: str, version: str) -> dict[str, str]:
    try:
        with zipfile.ZipFile(archive) as package:
            members = safe_members(package)
            nuspec_names = [member.filename for member in members if member.filename.casefold().endswith(".nuspec")]
            if len(nuspec_names) != 1:
                raise RuntimeError(f"Package archive must contain exactly one nuspec: {package_id}/{version}")
            content = package.read(nuspec_names[0])
    except (OSError, zipfile.BadZipFile, KeyError) as error:
        raise RuntimeError(f"Invalid package archive for {package_id}/{version}") from error

    try:
        root = ET.fromstring(content)
    except ET.ParseError as error:
        raise RuntimeError(f"Invalid nuspec XML for {package_id}/{version}") from error
    metadata_records = root.findall("{*}metadata")
    metadata = metadata_records[0] if len(metadata_records) == 1 else None
    if root.tag.split("}")[-1] != "package" or metadata is None:
        raise RuntimeError(f"Package nuspec is missing metadata: {package_id}/{version}")
    ids, versions = metadata.findall("{*}id"), metadata.findall("{*}version")
    if len(ids) != 1 or len(versions) != 1:
        raise RuntimeError(f"Package nuspec has ambiguous identity metadata: {package_id}/{version}")
    actual_id = ids[0].text
    actual_version = versions[0].text
    if actual_id != package_id or actual_version != version:
        raise RuntimeError(f"Package nuspec identity mismatch for {package_id}/{version}")
    repositories = metadata.findall("{*}repository")
    repository = repositories[0] if len(repositories) == 1 else None
    actual_repository = {
        "id": actual_id or "",
        "version": actual_version or "",
        "type": repository.get("type", "") if repository is not None else "",
        "url": repository.get("url", "") if repository is not None else "",
        "commit": repository.get("commit", "") if repository is not None else "",
    }
    return actual_repository


def _cache_package(cache: Path, package_id: str, version: str) -> tuple[Path, str, str]:
    cache_entry = cache / package_id.lower() / version
    archive_path = _regular_cache_file(
        cache_entry / f"{package_id.lower()}.{version}.nupkg", cache, "cached package archive"
    )
    metadata_path = _regular_cache_file(cache_entry / ".nupkg.metadata", cache, "NuGet cache metadata")
    sidecar_path = _regular_cache_file(archive_path.with_suffix(archive_path.suffix + ".sha512"), cache, "NuGet SHA-512 sidecar")
    content = archive_path.read_bytes()
    sha256 = hashlib.sha256(content).hexdigest()
    sha512 = hashlib.sha512(content).hexdigest()
    expected_sidecar = package_consumer.base64_sha512(content)
    if sidecar_path.read_text(encoding="utf-8").strip() != expected_sidecar:
        raise RuntimeError(f"NuGet SHA-512 sidecar does not match the cached archive: {package_id}/{version}")
    package_consumer.verify_external_cache_source(package_id, version, cache)
    metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
    if not isinstance(metadata, dict) or metadata.get("contentHash") != expected_sidecar:
        raise RuntimeError(f"Cached package content hash is not bound to its NuGet SHA-512 sidecar: {package_id}/{version}")
    return archive_path, sha256, sha512


def validate_baseline_project(
    project: Path,
    cache: Path,
    tfm: str,
    version: str,
    *,
    policy: Path | Mapping[str, Any] = POLICY_PATH,
    fixture_project: Path | None = None,
) -> dict[str, Any]:
    """Verify one package-only restore against an exact, source-pinned Elsa baseline.

    A hosted-WASM fixture may have one client ProjectReference only when the
    caller supplies that owned fixture path. The shared package graph adapter
    validates and projects that single edge; the package-only validator below
    remains unchanged. Host-required root package IDs belong to the caller.
    """
    selected_policy = _read_policy(policy)
    if version not in RELEASE_COMMITS["elsa-core"]:
        raise ValueError(f"Unsupported Elsa baseline release: {version}")
    if not isinstance(tfm, str) or not re.fullmatch(r"net[0-9]+\.[0-9]+", tfm):
        raise ValueError(f"Invalid target framework: {tfm!r}")
    project = package_graph.regular_file(Path(project).absolute())
    cache = Path(cache)
    assets_path = package_graph.regular_file(project.parent / "obj" / "project.assets.json")
    raw_assets_hash = package_graph.sha256(assets_path)
    package_assets, fixture_edges = package_graph.read_package_assets(
        project, tfm, fixture_project=fixture_project
    )
    if package_graph.sha256(assets_path) != raw_assets_hash:
        raise RuntimeError("Raw project.assets.json changed while package provenance was being read")
    _validate_restore_isolation(project, cache, package_assets)

    targets = package_assets.get("targets", {})
    policy_records = {
        (record["id"].casefold(), record["version"]): record
        for record in selected_policy["packages"]
    }
    missing_records = {
        (record["id"].casefold(), record["version"]): record
        for record in selected_policy["missing_provenance"]
    }
    internal_ids = {
        record["id"] for record in selected_policy["packages"]
    } | {
        record["id"] for record in selected_policy["missing_provenance"]
    }
    external_ids = {record["id"] for record in selected_policy["platform_exceptions"]}
    external_ids_folded = {package_id.casefold() for package_id in external_ids}
    exclusions = set(selected_policy["excluded_elsa_ids"])
    graph = package_consumer.validate_project_assets(
        package_assets, list(targets), internal_ids, version, external_ids, exclusions
    )

    restored = []
    for target_libraries in targets.values():
        for key in target_libraries:
            package_id, separator, package_version = key.partition("/")
            if not separator or not package_id or not package_version:
                raise RuntimeError(f"Unrecognized package asset identity: {key}")
            pair = (package_id.casefold(), package_version)
            if pair in missing_records:
                raise RuntimeError(f"Baseline package lacks approved source archive provenance: {package_id}/{package_version}")
            if package_id.casefold() in external_ids_folded:
                raise RuntimeError(f"Platform package tuple requires separate selected-archive review: {package_id}/{package_version}")
            if pair not in {(item["id"].casefold(), item["version"]) for item in restored}:
                restored.append({"id": package_id, "version": package_version})

    package_frameworks: dict[tuple[str, str], set[str]] = {}
    for framework, target_libraries in targets.items():
        for key in target_libraries:
            package_id, _, package_version = key.partition("/")
            package_frameworks.setdefault((package_id.casefold(), package_version), set()).add(framework)

    ledger = []
    for item in sorted(restored, key=lambda record: (record["id"].casefold(), record["version"])):
        package_id, package_version = item["id"], item["version"]
        pair = (package_id.casefold(), package_version)
        record = policy_records.get(pair)
        if record is not None and package_id != record["id"]:
            raise RuntimeError(f"Restored package ID casing differs from exact policy ID: {package_id}/{package_version}")
        archive_path, sha256, sha512 = _cache_package(cache, package_id, package_version)
        nuspec = _read_package_nuspec(archive_path, package_id, package_version)
        entry: dict[str, Any] = {
            "id": package_id,
            "version": package_version,
            "frameworks": sorted(package_frameworks[pair]),
            "classification": "owned_elsa" if record is not None else "nuget_dependency",
            "source": NUGET_ORG,
            "archive_sha256": sha256,
            "archive_sha512": sha512,
        }
        if record is not None:
            if sha256 != record["archive_sha256"]:
                raise RuntimeError(f"Cached Elsa archive differs from the exact source-approved digest: {package_id}/{package_version}")
            if nuspec != {
                "id": record["id"],
                "version": record["version"],
                "type": record["repository"]["type"],
                "url": record["repository"]["url"],
                "commit": record["repository"]["commit"],
            }:
                raise RuntimeError(f"Elsa nuspec source ownership differs from exact policy tuple: {package_id}/{package_version}")
            entry.update(
                owner=record["owner"],
                repository_url=record["repository"]["url"],
                repository_commit=record["repository"]["commit"],
            )
        ledger.append(entry)

    return {
        "version": version,
        "tfm": tfm,
        "package_only": True,
        "project_assets_sha256": raw_assets_hash,
        "package_graph": graph,
        "packages": ledger,
        "fixture_edge_receipts": fixture_edges,
    }
