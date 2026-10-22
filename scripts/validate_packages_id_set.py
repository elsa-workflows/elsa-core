#!/usr/bin/env python3
"""Compare packed NuGet IDs with the expected consolidated 3.10 set.

Both publishers run this same script before any 3.10.0 push: packages.yml
(nuget.org promote) and publish-consolidated-original-candidate.yml (Feedz).
There is no skip, bypass, or override input. The package directory arrives as:

    PACKAGES_DIR   directory containing the produced .nupkg files

The expected ID list is always scripts/validate_packages_id_set.txt.
"""

from __future__ import annotations

import os
import re
import sys
import zipfile
from pathlib import Path

# Same release-version shape as validate_packages_publication_selection.RELEASE_TAG.
PACKAGE_FILE = re.compile(r"(.+)\.[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?\.nupkg")
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
    for nupkg in sorted(directory.glob("*.nupkg")):
        match = PACKAGE_FILE.fullmatch(nupkg.name)
        if match is None:
            raise SystemExit(f"Cannot parse package ID from {nupkg.name!r}.")
        ids.append(match.group(1))
    if len(ids) != len(set(ids)):
        raise SystemExit("Produced packages list a package ID more than once.")
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
    produced = packed_ids(packages)
    errors = id_set_errors(produced, expected)
    errors.extend(studio_content_errors(packages))
    for error in errors:
        print(error, file=sys.stderr)
    if errors:
        return 1
    print(f"Package ID set matches {len(expected)} expected IDs.")
    print(f"Studio package content matches {sum(len(entries) for entries in STUDIO_CONTENT.values())} required entries.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
