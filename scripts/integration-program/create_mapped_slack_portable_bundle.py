#!/usr/bin/env python3
"""Retain exactly the files needed to recheck a mapped Slack package proof offline."""

import argparse
import hashlib
import json
from pathlib import Path
import tarfile

from run_mapped_slack_package_proof import PACKAGE_ID, PACKAGE_VERSION, TFMS


def required_paths():
    package_name = f"{PACKAGE_ID}.{PACKAGE_VERSION}.nupkg"
    relative_cache = Path(PACKAGE_ID.casefold()) / PACKAGE_VERSION.casefold()
    paths = [Path("evidence.json"), Path("local-feed") / package_name]
    consumers = [Path("consumers") / framework for framework in TFMS]
    consumers.append(Path("offline-activity-smoke"))
    for directory in consumers:
        framework = directory.name if directory.name != "offline-activity-smoke" else "net10.0"
        cache = Path("package-caches") / directory / relative_cache
        paths.extend([
            directory / "obj" / "project.assets.json",
            directory / "bin" / "Debug" / framework / f"{PACKAGE_ID}.dll",
            cache / ".nupkg.metadata",
            cache / f"{PACKAGE_ID.casefold()}.{PACKAGE_VERSION.casefold()}.nupkg.sha512",
            cache / f"{PACKAGE_ID.casefold()}.{PACKAGE_VERSION.casefold()}.nupkg",
        ])
    return paths


def create_bundle(evidence_path: Path, output_path: Path):
    evidence_path = evidence_path.resolve(strict=True)
    root = evidence_path.parent
    evidence = json.loads(evidence_path.read_text(encoding="utf-8"))
    if evidence.get("result") != "passed" or evidence.get("publication_authorized") is not False:
        raise ValueError("Only a passed nonpublishing Slack proof can be bundled")
    recorded_root = evidence.get("proof_root")
    if not isinstance(recorded_root, str) or not Path(recorded_root).is_absolute() or ".." in Path(recorded_root).parts:
        raise ValueError("Portable Slack bundle requires a clean absolute recorded proof root")
    output_path = output_path.expanduser().resolve(strict=False)
    if output_path == root or root in output_path.parents:
        raise ValueError("Portable bundle output must be outside the proof root")
    paths = required_paths()
    if len(paths) != len(set(paths)):
        raise ValueError("Portable bundle path list contains duplicates")
    for relative in paths:
        path = root / relative
        if not path.is_file() or path.is_symlink() or any(parent.is_symlink() for parent in path.parents if parent != root):
            raise ValueError(f"Missing or unsafe portable proof input: {relative}")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    with output_path.open("xb") as output:
        with tarfile.open(fileobj=output, mode="w:gz") as archive:
            for relative in paths:
                archive.add(root / relative, arcname=relative.as_posix(), recursive=False)
    return {"fileCount": len(paths), "sha256": hashlib.sha256(output_path.read_bytes()).hexdigest()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evidence", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(create_bundle(args.evidence, args.output), indent=2))


if __name__ == "__main__":
    main()
