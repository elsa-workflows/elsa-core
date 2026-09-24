#!/usr/bin/env python3
"""Load and validate the bounded, nonpublishing package release-unit manifest."""

from __future__ import annotations

import json
import re
from pathlib import Path, PurePosixPath
from typing import Any

MANIFEST_PATH = Path(__file__).resolve().parents[2] / "doc/integration-program/release-units.json"
DEFAULT_UNIT_ID = "elsa-slack"
PACKAGE_ID_PATTERN = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._-]*$")
SEMVER_PATTERN = re.compile(
    r"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)"
    r"(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?"
    r"(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$"
)


def _require_keys(value: Any, required: set[str], path: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise ValueError(f"{path} must be an object")
    missing = required - value.keys()
    extra = value.keys() - required
    if missing or extra:
        raise ValueError(f"{path} keys differ: missing={sorted(missing)}, unknown={sorted(extra)}")
    return value


def _relative_project_path(value: Any, path: str) -> str:
    if not isinstance(value, str) or not value or "\\" in value:
        raise ValueError(f"{path} must be a nonempty POSIX project path")
    parsed = PurePosixPath(value)
    if parsed.is_absolute() or ".." in parsed.parts or "." in parsed.parts or parsed.suffix != ".csproj":
        raise ValueError(f"{path} must be a safe relative .csproj path: {value!r}")
    if parsed.as_posix() != value:
        raise ValueError(f"{path} must use a normalized POSIX path: {value!r}")
    return value


def _frameworks(value: Any, path: str) -> list[str]:
    if not isinstance(value, list) or not value or any(not isinstance(item, str) or not item for item in value):
        raise ValueError(f"{path} must be a nonempty list of target framework names")
    if any(not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]*", item) for item in value):
        raise ValueError(f"{path} contains an invalid target framework name")
    if len(set(value)) != len(value):
        raise ValueError(f"{path} contains duplicate target frameworks")
    return value


def _is_semver2(value: Any) -> bool:
    if not isinstance(value, str):
        return False
    match = SEMVER_PATTERN.fullmatch(value)
    if match is None:
        return False
    prerelease = match.group(4)
    if prerelease is None:
        return True
    return not any(
        identifier.isdigit() and len(identifier) > 1 and identifier.startswith("0")
        for identifier in prerelease.split(".")
    )


def _validate_unit(unit: Any, index: int) -> dict[str, Any]:
    prefix = f"release_units[{index}]"
    unit = _require_keys(
        unit,
        {
            "id", "package_id", "source", "mapped", "target_frameworks",
            "tested_artifact_dependencies", "publisher", "versioning",
        },
        prefix,
    )
    for key in ("id", "package_id"):
        if not isinstance(unit[key], str) or not unit[key].strip():
            raise ValueError(f"{prefix}.{key} must be a nonempty string")
    if not PACKAGE_ID_PATTERN.fullmatch(unit["package_id"]):
        raise ValueError(f"{prefix}.package_id is not a valid package identity")

    source = _require_keys(unit["source"], {"repository", "project_path", "test_projects"}, f"{prefix}.source")
    if not isinstance(source["repository"], str) or not source["repository"].strip():
        raise ValueError(f"{prefix}.source.repository must be a nonempty string")
    _relative_project_path(source["project_path"], f"{prefix}.source.project_path")
    target_frameworks = _frameworks(unit["target_frameworks"], f"{prefix}.target_frameworks")
    if not isinstance(source["test_projects"], list) or not source["test_projects"]:
        raise ValueError(f"{prefix}.source.test_projects must be a nonempty list")
    source_test_paths = set()
    for test_index, test in enumerate(source["test_projects"]):
        test_prefix = f"{prefix}.source.test_projects[{test_index}]"
        test = _require_keys(test, {"project_path", "target_frameworks"}, test_prefix)
        test_path = _relative_project_path(test["project_path"], f"{test_prefix}.project_path")
        if test_path in source_test_paths:
            raise ValueError(f"{prefix} declares a duplicate test project: {test_path}")
        source_test_paths.add(test_path)
        test_frameworks = _frameworks(test["target_frameworks"], f"{test_prefix}.target_frameworks")
        if not set(test_frameworks).issubset(target_frameworks):
            raise ValueError(f"{test_prefix} uses a framework outside the package matrix")

    mapped = _require_keys(unit["mapped"], {"repository", "project_path", "test_projects"}, f"{prefix}.mapped")
    if not isinstance(mapped["repository"], str) or not mapped["repository"].strip():
        raise ValueError(f"{prefix}.mapped.repository must be a nonempty string")
    _relative_project_path(mapped["project_path"], f"{prefix}.mapped.project_path")
    if not isinstance(mapped["test_projects"], list):
        raise ValueError(f"{prefix}.mapped.test_projects must be a list")
    mapped_tests: dict[str, str] = {}
    for test_index, test in enumerate(mapped["test_projects"]):
        test_prefix = f"{prefix}.mapped.test_projects[{test_index}]"
        test = _require_keys(test, {"source_project_path", "project_path"}, test_prefix)
        source_path = _relative_project_path(test["source_project_path"], f"{test_prefix}.source_project_path")
        mapped_path = _relative_project_path(test["project_path"], f"{test_prefix}.project_path")
        if source_path in mapped_tests:
            raise ValueError(f"{prefix}.mapped.test_projects has a duplicate source path: {source_path}")
        mapped_tests[source_path] = mapped_path
    if set(mapped_tests) != source_test_paths:
        raise ValueError(f"{prefix}.mapped test projects must map every declared source test exactly once")
    if len(set(mapped_tests.values())) != len(mapped_tests):
        raise ValueError(f"{prefix}.mapped.test_projects maps multiple tests to one destination")

    dependencies = unit["tested_artifact_dependencies"]
    if not isinstance(dependencies, list) or not dependencies:
        raise ValueError(f"{prefix}.tested_artifact_dependencies must be a nonempty list")
    dependency_ids: set[str] = set()
    for dependency_index, dependency in enumerate(dependencies):
        dependency_prefix = f"{prefix}.tested_artifact_dependencies[{dependency_index}]"
        dependency = _require_keys(dependency, {"package_id", "version", "scope"}, dependency_prefix)
        package_id = dependency["package_id"]
        version = dependency["version"]
        if not isinstance(package_id, str) or not PACKAGE_ID_PATTERN.fullmatch(package_id):
            raise ValueError(f"{dependency_prefix}.package_id is not a valid package identity")
        if package_id.casefold() in dependency_ids or package_id.casefold() == unit["package_id"].casefold():
            raise ValueError(f"{prefix} has a duplicate or self dependency: {package_id}")
        dependency_ids.add(package_id.casefold())
        if not _is_semver2(version):
            raise ValueError(f"{dependency_prefix}.version must be a SemVer 2 version")
        scope = dependency["scope"]
        if not isinstance(scope, str) or scope not in {"internal-compatibility-baseline", "external"}:
            raise ValueError(f"{dependency_prefix}.scope is not supported")

    publisher = _require_keys(
        unit["publisher"],
        {"repository", "status", "cutover_requires_review"},
        f"{prefix}.publisher",
    )
    if publisher["repository"] != source["repository"]:
        raise ValueError(f"{prefix} publisher must match the current source repository")
    if publisher["status"] != "sole-current-publisher" or publisher["cutover_requires_review"] is not True:
        raise ValueError(f"{prefix} must identify the sole current publisher and require reviewed cutover")

    versioning = _require_keys(
        unit["versioning"],
        {
            "scheme", "stable_authority", "stable_must_increase", "published_versions_are_never_reused",
            "bump_rules", "preview_format", "local_proof_version", "local_proof_is_release_allocation",
            "local_proof_may_publish",
        },
        f"{prefix}.versioning",
    )
    if versioning["scheme"] != "independent-per-package-semver2":
        raise ValueError(f"{prefix}.versioning.scheme must be per-package SemVer 2")
    if versioning["stable_authority"] != "current-publisher-and-package-feed-history":
        raise ValueError(f"{prefix}.versioning must use the current publisher and package history as stable authority")
    if versioning["stable_must_increase"] is not True or versioning["published_versions_are_never_reused"] is not True:
        raise ValueError(f"{prefix}.versioning must require monotonic, non-reused stable versions")
    expected_bump_rules = {
        "compatible_fix": "patch",
        "compatible_feature": "minor",
        "breaking_public_or_serialized_identity_change": "major",
    }
    if versioning["bump_rules"] != expected_bump_rules:
        raise ValueError(f"{prefix}.versioning.bump_rules differ from the accepted per-package policy")
    if versioning["preview_format"] != "{next_stable}-preview.{run_number}.{run_attempt}.{source_sha}":
        raise ValueError(f"{prefix}.versioning.preview_format is not the accepted immutable preview form")
    proof_version = versioning["local_proof_version"]
    if not _is_semver2(proof_version) or "-proof" not in proof_version:
        raise ValueError(f"{prefix}.versioning.local_proof_version must be a SemVer proof prerelease")
    if (
        versioning["local_proof_is_release_allocation"] is not False
        or versioning["local_proof_may_publish"] is not False
    ):
        raise ValueError(f"{prefix} local proof version must never allocate or publish a release version")
    return unit


def load_manifest(path: Path = MANIFEST_PATH) -> dict[str, Any]:
    document = json.loads(path.read_text(encoding="utf-8"))
    document = _require_keys(document, {"schema_version", "release_units"}, "manifest")
    if type(document["schema_version"]) is not int or document["schema_version"] != 1:
        raise ValueError(f"Unsupported release-unit manifest schema: {document['schema_version']!r}")
    units = document["release_units"]
    if not isinstance(units, list) or not units:
        raise ValueError("manifest.release_units must be a nonempty list")
    ids: set[str] = set()
    package_owners: dict[str, str] = {}
    for index, raw_unit in enumerate(units):
        unit = _validate_unit(raw_unit, index)
        unit_id = unit["id"].casefold()
        if unit_id in ids:
            raise ValueError(f"Duplicate release-unit id: {unit['id']}")
        ids.add(unit_id)
        package_id = unit["package_id"].casefold()
        previous = package_owners.get(package_id)
        if previous is not None:
            raise ValueError(f"Package {unit['package_id']} has multiple release-unit owners: {previous}, {unit['id']}")
        package_owners[package_id] = unit["id"]
    return document


def get_unit(document: dict[str, Any], unit_id: str = DEFAULT_UNIT_ID) -> dict[str, Any]:
    for unit in document["release_units"]:
        if unit["id"].casefold() == unit_id.casefold():
            return unit
    raise ValueError(f"Unknown release unit: {unit_id}")


def source_project_key(unit: dict[str, Any]) -> tuple[str, str]:
    return unit["source"]["repository"], unit["source"]["project_path"]


def source_test_project_keys(unit: dict[str, Any]) -> list[tuple[str, str]]:
    repository = unit["source"]["repository"]
    return [(repository, row["project_path"]) for row in unit["source"]["test_projects"]]


def validate_against_inventory(unit: dict[str, Any], inventory: dict[str, Any]) -> None:
    repository = unit["source"]["repository"]
    rows = inventory["project_inventory"].get(repository)
    if rows is None:
        raise ValueError(f"Release unit source repository is missing from inventory: {repository}")
    projects = {row["path"]: row for row in rows}
    package_project = projects.get(unit["source"]["project_path"])
    if package_project is None:
        raise ValueError(f"Release unit source project is missing from inventory: {unit['source']['project_path']}")
    if package_project.get("package_id") != unit["package_id"] or package_project.get("is_packable") is not True:
        raise ValueError(f"Release unit package identity or packability differs from inventory: {package_project}")
    if package_project.get("target_frameworks") != unit["target_frameworks"]:
        raise ValueError(
            "Release unit target frameworks differ from inventory: "
            f"{package_project.get('target_frameworks')}"
        )

    package_references = {row.get("id"): row for row in package_project.get("package_references", [])}
    for dependency in unit["tested_artifact_dependencies"]:
        source_reference = package_references.get(dependency["package_id"])
        if source_reference is None:
            raise ValueError(
                "Artifact dependency is missing from the inventory source graph: "
                f"{dependency['package_id']}"
            )
        if dependency["scope"] == "external" and source_reference.get("version") != dependency["version"]:
            raise ValueError(f"External dependency version differs from inventory: {source_reference}")

    for test in unit["source"]["test_projects"]:
        test_project = projects.get(test["project_path"])
        if test_project is None or test_project.get("is_test_project") is not True:
            raise ValueError(f"Release unit test project is missing or not marked as a test: {test['project_path']}")
        if test_project.get("target_frameworks") != test["target_frameworks"]:
            raise ValueError(
                "Release unit test frameworks differ from inventory: "
                f"{test_project.get('target_frameworks')}"
            )


def map_source_project_path(repository: str, path: str, import_mapping: list[dict[str, str]]) -> str:
    """Map an upstream project path through the actual import receipt."""
    product_by_repository = {
        "elsa-core": "core",
        "elsa-extensions": "extensions",
        "elsa-studio": "studio",
    }
    product = product_by_repository.get(repository)
    if product is None:
        raise ValueError(f"No import mapping exists for repository: {repository}")
    if product == "core":
        return path
    matches = [row for row in import_mapping if row.get("repository") == product and row.get("source") == path]
    if len(matches) != 1:
        raise ValueError(f"Expected exactly one import mapping for {repository}:{path}; found {len(matches)}")
    return matches[0]["destination"]
