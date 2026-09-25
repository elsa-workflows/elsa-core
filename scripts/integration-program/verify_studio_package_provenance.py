#!/usr/bin/env python3
"""Verify the imported Studio Core package without publishing it."""

from __future__ import annotations

import argparse
import hashlib
import json
import subprocess
import tempfile
from pathlib import Path
from xml.etree import ElementTree
from zipfile import ZipFile

from run_slack_package_proof import require_sourcelink_tool


ROOT = Path(__file__).resolve().parents[2]
PACKAGE_ID = "Elsa.Studio.Core"
REPOSITORY_URL = "https://github.com/elsa-workflows/elsa-core"
ICON_SHA256 = "82fd76d734d59efc6132af0b0b999146254fa5a296ea5d64f85597bb1cda524e"
FRAMEWORKS = ("net8.0", "net9.0", "net10.0")


def sha256(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def metadata_value(metadata: ElementTree.Element, name: str) -> str | None:
    element = metadata.find("{*}" + name)
    return None if element is None else element.text


def verify(package_dir: Path, version: str, commit: str, sourcelink_tool: Path | None) -> dict[str, object]:
    if not version.startswith("0.0.0-proof.") or not version.replace(".", "").replace("-", "").isalnum():
        raise ValueError("Studio provenance proof requires a 0.0.0-proof version")
    if len(commit) != 40 or any(character not in "0123456789abcdef" for character in commit):
        raise ValueError("Expected commit must be a full lowercase Git SHA")
    if not package_dir.is_dir() or package_dir.is_symlink():
        raise ValueError("Package directory must exist and must not be a symlink")
    expected = {
        f"{PACKAGE_ID}.{version}.nupkg",
        f"{PACKAGE_ID}.{version}.snupkg",
    }
    actual = {path.name for path in package_dir.iterdir() if path.is_file()}
    if actual != expected:
        raise ValueError(f"Expected only the Studio Core proof pair; got {sorted(actual)}")
    nupkg = package_dir / f"{PACKAGE_ID}.{version}.nupkg"
    snupkg = package_dir / f"{PACKAGE_ID}.{version}.snupkg"
    icon = (ROOT / "icon.png").read_bytes()
    if sha256(icon) != ICON_SHA256:
        raise ValueError("Core root icon changed from the reviewed shared icon")

    with ZipFile(nupkg) as archive:
        names = set(archive.namelist())
        nuspecs = [name for name in names if name.endswith(".nuspec")]
        if len(nuspecs) != 1:
            raise ValueError("Studio package must contain exactly one nuspec")
        metadata = ElementTree.fromstring(archive.read(nuspecs[0])).find("{*}metadata")
        if metadata is None:
            raise ValueError("Studio package has no nuspec metadata")
        repository = metadata.find("{*}repository")
        if (metadata_value(metadata, "id") != PACKAGE_ID
                or metadata_value(metadata, "version") != version
                or metadata_value(metadata, "projectUrl") != REPOSITORY_URL
                or metadata_value(metadata, "icon") != "icon.png"
                or repository is None
                or repository.attrib.get("type") != "git"
                or repository.attrib.get("url") != REPOSITORY_URL
                or repository.attrib.get("commit") != commit):
            raise ValueError("Studio nuspec identity, icon, or Core repository provenance changed")
        if names.intersection({"icon.png"}) != {"icon.png"} or archive.read("icon.png") != icon:
            raise ValueError("Studio package does not contain the reviewed shared icon bytes")
        for framework in FRAMEWORKS:
            if f"lib/{framework}/{PACKAGE_ID}.dll" not in names:
                raise ValueError(f"Studio package omits {framework} assembly")

    with ZipFile(snupkg) as archive:
        names = set(archive.namelist())
        pdbs = {framework: f"lib/{framework}/{PACKAGE_ID}.pdb" for framework in FRAMEWORKS}
        if any(path not in names for path in pdbs.values()):
            raise ValueError("Studio symbol package omits a target-framework PDB")
        if sourcelink_tool is not None:
            assembly, _, _ = require_sourcelink_tool(sourcelink_tool)
            with tempfile.TemporaryDirectory() as directory:
                for framework, member in pdbs.items():
                    pdb = Path(directory) / f"{PACKAGE_ID}.{framework}.pdb"
                    pdb.write_bytes(archive.read(member))
                    result = subprocess.run(["dotnet", str(assembly), "test", str(pdb)],
                                            capture_output=True, text=True, check=False)
                    if result.returncode or "sourcelink test passed" not in result.stdout:
                        raise ValueError(f"Studio {framework} SourceLink URL/content check failed: {result.stderr.strip()}")

    return {
        "packageId": PACKAGE_ID,
        "version": version,
        "repository": REPOSITORY_URL,
        "commit": commit,
        "frameworks": list(FRAMEWORKS),
        "iconSha256": ICON_SHA256,
        "nupkgSha256": sha256(nupkg.read_bytes()),
        "snupkgSha256": sha256(snupkg.read_bytes()),
        "sourceLinkVerified": sourcelink_tool is not None,
        "published": False,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--package-dir", type=Path, required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--commit", required=True)
    parser.add_argument("--sourcelink-tool", type=Path)
    args = parser.parse_args()
    print(json.dumps(verify(args.package_dir, args.version, args.commit, args.sourcelink_tool), indent=2))


if __name__ == "__main__":
    main()
