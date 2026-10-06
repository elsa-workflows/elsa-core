"""Reject files outside the browser proof's portable receipt inventory."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import stat

from paired_package_provenance import regular_file
from run_paired_package_browser_matrix import MATRIX


def verify_retained_inventory(root: Path) -> list[str]:
    # Validate ancestors before traversal; never follow an evidence-root link.
    root = root.absolute()
    regular_file(root / "matrix.json")
    allowed = {"matrix.json", "inputs/original-envelope.json", "inputs/live-retrieval.json", "inputs/provenance.json"}
    for key in MATRIX:
        allowed.update(f"cells/{'-'.join(key)}/{name}.json" for name in ("execution", "browser"))
    directories = {str(parent) for name in allowed for parent in Path(name).parents if str(parent) != "."}
    found = []
    for path in root.rglob("*"):
        name = path.relative_to(root).as_posix()
        mode = path.lstat().st_mode
        if stat.S_ISLNK(mode):
            raise ValueError("Linked browser evidence cannot be retained")
        if stat.S_ISDIR(mode):
            if name not in directories:
                raise ValueError("Unexpected browser evidence directory")
            continue
        if not stat.S_ISREG(mode) or name not in allowed:
            raise ValueError("Unexpected browser evidence file")
        regular_file(path)
        if not 0 < path.stat().st_size <= 8 * 1024 * 1024:
            raise ValueError("Unbounded or empty browser evidence file")
        if not isinstance(json.loads(path.read_text(encoding="utf-8")), dict):
            raise ValueError("Browser evidence must be a JSON object")
        found.append(name)
    return sorted(found)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("root", type=Path)
    arguments = parser.parse_args()
    try:
        inventory = verify_retained_inventory(arguments.root)
    except (OSError, ValueError, RuntimeError):
        raise SystemExit("Browser evidence inventory rejected") from None
    print(f"Validated {len(inventory)} portable browser receipt files")
