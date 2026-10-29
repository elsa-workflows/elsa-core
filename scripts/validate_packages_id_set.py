#!/usr/bin/env python3
"""Compare packed NuGet IDs with the expected consolidated 3.10 set.

Both publishers run this same script before any 3.10.0 push: packages.yml
(nuget.org promote) and publish-consolidated-original-candidate.yml (Feedz).
The package directory arrives as:

    PACKAGES_DIR   directory containing the produced .nupkg files
    EXCLUDE_IDS    optional comma-separated package IDs to skip (default empty)

The expected ID list is always scripts/validate_packages_id_set.txt. Empty
EXCLUDE_IDS keeps that full set. A non-empty list is a reviewed, count-adjusting
skip: it must name distinct IDs from the expected list, the remaining packages
must not depend on an excluded ID, and the expected count is then 231 minus
that many. Feedz never sets EXCLUDE_IDS. The publication-selection check rejects
a non-empty list except on a publish_nuget dispatch for a stable 3.10 tag.
"""

from __future__ import annotations

import os
import re
import sys
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

# Same release-version shape as validate_packages_publication_selection.RELEASE_TAG.
PACKAGE_FILE = re.compile(r"(.+)\.[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?\.nupkg")
PACKAGE_VERSION = re.compile(r"\.([0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?)\.nupkg$")
# Canonical stable 3.10.x patch. Python callers import this; MSBuild keeps the same
# source string in ConsolidatedPackageProof.targets. Rejects 3.10.01.
STABLE_310_PATTERN = r"3\.10\.(0|[1-9][0-9]*)"
STABLE_310 = re.compile(STABLE_310_PATTERN)
DEFAULT_EXPECTED = Path(__file__).with_name("validate_packages_id_set.txt")
# Same packed entries per package as prove_consolidated_packages.CLIENTLIB_ASSETS.
STUDIO_CONTENT = {
    "Elsa.Studio.Workflows.Designer": (
        "staticwebassets/designer.entry.js",
        "staticwebassets/react-designer.entry.js",
        "staticwebassets/designer.css",
    ),
    "Elsa.Studio.DomInterop": (
        "staticwebassets/dom.entry.js",
        "staticwebassets/clipboard.entry.js",
        "staticwebassets/files.entry.js",
    ),
    "Elsa.Studio.Bpmn.BpmnJs": (
        "staticwebassets/bpmn-js-designer.js",
        "staticwebassets/bpmn-js-designer.css",
    ),
}


def is_stable_310(version: str) -> bool:
    """True for 3.10.0 / 3.10.1; false for previews, 3.10.01, and other lines."""
    return bool(STABLE_310.fullmatch(version))


def load_expected_ids(path: Path) -> list[str]:
    if not path.is_file():
        raise SystemExit(f"Expected ID list not found: {path}")
    ids = [line.strip() for line in path.read_text(encoding="utf-8").splitlines() if line.strip() and not line.startswith("#")]
    if not ids:
        raise SystemExit(f"{path} lists no package IDs.")
    if len(ids) != len(set(ids)):
        raise SystemExit(f"{path} lists a package ID more than once.")
    return ids


def packed_ids(directory: Path) -> list[str]:
    """Return package IDs from *.nupkg names, ignoring symbol packages."""
    ids = []
    versions = set()
    for nupkg in sorted(directory.glob("*.nupkg")):
        match = PACKAGE_FILE.fullmatch(nupkg.name)
        if match is None:
            raise SystemExit(f"Cannot parse package ID from {nupkg.name!r}.")
        version = PACKAGE_VERSION.search(nupkg.name)
        if version is None:
            raise SystemExit(f"Cannot parse package version from {nupkg.name!r}.")
        ids.append(match.group(1))
        versions.add(version.group(1))
    if len(ids) != len(set(ids)):
        raise SystemExit("Produced packages list a package ID more than once.")
    if len(versions) > 1:
        raise SystemExit(f"Produced packages mix versions: {', '.join(sorted(versions))}.")
    return ids


def id_set_errors(produced: list[str], expected: list[str]) -> list[str]:
    """Return why the produced set must not be published; empty means it matches."""
    produced_set, expected_set = set(produced), set(expected)
    if produced_set == expected_set:
        return []
    missing = sorted(expected_set - produced_set)
    extra = sorted(produced_set - expected_set)
    lines = ["Packed package IDs do not match the expected consolidated set."]
    if missing:
        lines.extend([f"Missing ({len(missing)}):", *missing])
    if extra:
        lines.extend([f"Unexpected ({len(extra)}):", *extra])
    lines.extend([
        f"Expected ({len(expected)}):",
        *expected,
        f"Produced ({len(produced)}):",
        *produced,
    ])
    return lines


def parse_exclude_ids(raw: str | None) -> list[str]:
    """Split a comma-separated exclude list. Empty input is none; empty entries fail."""
    if raw is None or not raw.strip():
        return []
    parts = [part.strip() for part in raw.split(",")]
    if any(not part for part in parts):
        raise SystemExit("exclude_ids contains an empty entry.")
    folded = [part.casefold() for part in parts]
    if len(folded) != len(set(folded)):
        raise SystemExit("exclude_ids lists a package ID more than once.")
    return parts


def resolve_exclude_ids(raw: str | None, available: list[str]) -> list[str]:
    """Map requested IDs onto the artifact set. Unknown IDs fail closed."""
    requested = parse_exclude_ids(raw)
    if not requested:
        return []
    index = {package_id.casefold(): package_id for package_id in available}
    resolved: list[str] = []
    unknown: list[str] = []
    for item in requested:
        match = index.get(item.casefold())
        if match is None:
            unknown.append(item)
        else:
            resolved.append(match)
    if unknown:
        raise SystemExit("exclude_ids names IDs that are not in the expected package set: " + ", ".join(unknown))
    return resolved


def filename_package_id(name: str) -> str | None:
    nupkg_name = name[:-7] + ".nupkg" if name.endswith(".snupkg") else name
    match = PACKAGE_FILE.fullmatch(nupkg_name)
    return None if match is None else match.group(1)


def remove_excluded_packages(directory: Path, excluded: set[str]) -> None:
    for path in sorted(directory.iterdir()):
        if not path.is_file() or path.suffix not in {".nupkg", ".snupkg"}:
            continue
        package_id = filename_package_id(path.name)
        if package_id in excluded:
            path.unlink()


def apply_exclude_ids(directory: Path, expected: list[str], raw: str | None) -> tuple[list[str], list[str]]:
    """Drop excluded packages from directory and return the remaining expected IDs."""
    excluded = resolve_exclude_ids(raw, expected)
    if not excluded:
        return expected, []
    excluded_set = set(excluded)
    remove_excluded_packages(directory, excluded_set)
    return [package_id for package_id in expected if package_id not in excluded_set], excluded


def _xml_local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def nupkg_dependency_ids(nupkg: Path) -> set[str]:
    """Return direct dependency IDs from a package's .nuspec."""
    try:
        with zipfile.ZipFile(nupkg) as archive:
            names = [name for name in archive.namelist() if name.endswith(".nuspec")]
            if len(names) != 1:
                raise SystemExit(f"Cannot read {nupkg.name} as a NuGet package.")
            root = ET.fromstring(archive.read(names[0]))
    except zipfile.BadZipFile:
        raise SystemExit(f"Cannot read {nupkg.name} as a NuGet package.") from None
    except ET.ParseError:
        raise SystemExit(f"Cannot read {nupkg.name} as a NuGet package.") from None
    return {
        package_id
        for element in root.iter()
        if _xml_local_name(element.tag) == "dependency" and (package_id := element.get("id"))
    }


def exclusion_dependency_errors(directory: Path, excluded: set[str]) -> list[str]:
    """Fail when a remaining package depends on an excluded ID."""
    dependents: list[str] = []
    for nupkg in sorted(directory.glob("*.nupkg")):
        package_id = filename_package_id(nupkg.name)
        if package_id is None or package_id in excluded:
            continue
        excluded_index = {package_id.casefold(): package_id for package_id in excluded}
        hits = sorted({
            excluded_index[dep.casefold()]
            for dep in nupkg_dependency_ids(nupkg)
            if dep.casefold() in excluded_index
        })
        if hits:
            dependents.append(f"{package_id} depends on {', '.join(hits)}")
    if not dependents:
        return []
    return ["Remaining packages depend on an excluded ID:", *dependents]


def studio_content_errors(directory: Path) -> list[str]:
    """Fail when a packed Studio ClientLib package is missing a required bundle entry."""
    errors = []
    for nupkg in sorted(directory.glob("*.nupkg")):
        match = PACKAGE_FILE.fullmatch(nupkg.name)
        if match is None:
            continue
        entries = STUDIO_CONTENT.get(match.group(1))
        if not entries:
            continue
        try:
            with zipfile.ZipFile(nupkg) as archive:
                names = archive.namelist()
        except zipfile.BadZipFile:
            errors.append(f"Cannot read {nupkg.name} as a NuGet package.")
            continue
        for entry in entries:
            if entry not in names:
                errors.append(f"Missing required content {entry} in {nupkg.name}.")
    return errors


def main() -> int:
    packages_dir = os.environ.get("PACKAGES_DIR")
    if not packages_dir:
        raise SystemExit("PACKAGES_DIR must be set to a directory of packed .nupkg files.")
    packages = Path(packages_dir)
    if not packages.is_dir():
        raise SystemExit("PACKAGES_DIR must be a directory of packed .nupkg files.")
    expected = load_expected_ids(DEFAULT_EXPECTED)
    expected, excluded = apply_exclude_ids(packages, expected, os.environ.get("EXCLUDE_IDS"))
    produced = packed_ids(packages)
    errors = id_set_errors(produced, expected)
    if excluded:
        errors.extend(exclusion_dependency_errors(packages, set(excluded)))
    errors.extend(studio_content_errors(packages))
    for error in errors:
        print(error, file=sys.stderr)
    if errors:
        return 1
    print("exclusions: none" if not excluded else "exclusions: " + ", ".join(excluded))
    print(f"Package ID set matches {len(expected)} expected IDs.")
    print(f"Studio package content matches {sum(len(entries) for entries in STUDIO_CONTENT.values())} required entries.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
