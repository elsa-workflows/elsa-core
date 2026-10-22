#!/usr/bin/env python3
"""Compare packed NuGet IDs with the expected consolidated 3.10 set.

The build job in .github/workflows/packages.yml runs this after Compile+Pack when
ConsolidatedReleaseCandidate is on (stable 3.10.x tags). The package directory
arrives as an environment variable, never as script text:

    PACKAGES_DIR   directory containing the produced .nupkg files
    EXPECTED_IDS   optional path to the ID list; defaults to validate_packages_id_set.txt
"""

from __future__ import annotations

import os
import re
import sys
from pathlib import Path

# Same release-version shape as validate_packages_publication_selection.RELEASE_TAG.
PACKAGE_FILE = re.compile(r"(.+)\.[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?\.nupkg")
STABLE_310 = re.compile(r"3\.10\.[0-9]+")
DEFAULT_EXPECTED = Path(__file__).with_name("validate_packages_id_set.txt")


def is_stable_310(version: str) -> bool:
    """True for 3.10.0 / 3.10.1; false for previews and other lines."""
    return bool(STABLE_310.fullmatch(version))


def load_expected_ids(path: Path) -> list[str]:
    ids = [line.strip() for line in path.read_text(encoding="utf-8").splitlines() if line.strip() and not line.startswith("#")]
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
    lines = [
        "Packed package IDs do not match the expected consolidated set.",
        f"Expected ({len(expected)}):",
        *expected,
        f"Produced ({len(produced)}):",
        *produced,
    ]
    if missing:
        lines.extend([f"Missing ({len(missing)}):", *missing])
    if extra:
        lines.extend([f"Unexpected ({len(extra)}):", *extra])
    return lines


def main() -> int:
    packages = Path(os.environ.get("PACKAGES_DIR", ""))
    if not packages.is_dir():
        raise SystemExit("PACKAGES_DIR must be a directory of packed .nupkg files.")
    expected_path = Path(os.environ.get("EXPECTED_IDS") or DEFAULT_EXPECTED)
    expected = load_expected_ids(expected_path)
    produced = packed_ids(packages)
    errors = id_set_errors(produced, expected)
    for error in errors:
        print(error, file=sys.stderr)
    if errors:
        return 1
    print(f"Package ID set matches {len(expected)} expected IDs.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
