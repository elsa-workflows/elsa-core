#!/usr/bin/env python3
"""Validate the actual Preview package set, frameworks, assets and dependency versions."""

import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET
from zipfile import ZipFile


def verify(directory: Path, version: str) -> None:
    root = Path(__file__).resolve().parents[1]
    modules = {module["packageId"] for module in json.loads((root / "module-catalog.json").read_text())}
    expected = modules | {"Elsa.ReactStudio", "Elsa.ReactStudio.Host"}
    packages = list(directory.glob("*.nupkg"))
    assert {path.name for path in packages} == {f"{name}.{version}.nupkg" for name in expected}, "Unexpected package set"
    for path in packages:
        with ZipFile(path) as package:
            names = package.namelist()
            document = ET.fromstring(package.read(next(name for name in names if name.endswith(".nuspec"))))
            metadata = {element.tag.rsplit("}", 1)[-1]: element for element in document[0]}
            identity = metadata["id"].text
            assert metadata["version"].text == version, identity
            assert "Preview" in metadata["description"].text, identity
            assert metadata["license"].attrib["type"] == "file", identity
            assert metadata["license"].text == "SOURCE-NOTICE.md", identity
            assert "no license file" in package.read("SOURCE-NOTICE.md").decode(), identity
            assert metadata["readme"].text == "README.md" and "README.md" in names, identity
            for framework in ("net8.0", "net9.0", "net10.0"):
                assembly = package.read(f"lib/{framework}/{identity}.dll")
                assert version.encode() in assembly, (identity, framework, "assembly version")
                if identity in modules:
                    for asset in ("entry.js", "module.css"):
                        assert f"ReactStudio.Module/{asset}".encode() in assembly, (identity, framework, asset)
                        assert f"staticwebassets/{asset}" in names, (identity, "static web asset", asset)
                if identity == "Elsa.ReactStudio.Host":
                    for resource in ("index.html", "shared-imports.json", "THIRD-PARTY-NOTICES.txt"):
                        assert f"ReactStudio/{resource}".encode() in assembly, (identity, framework, resource)
            for dependency in document.iter():
                if dependency.tag.rsplit("}", 1)[-1] == "dependency" and dependency.attrib["id"] in expected:
                    assert dependency.attrib["version"] == version, (identity, dependency.attrib)
    print(f"PASS: {len(packages)} Preview packages have all three frameworks, embedded assets, source notices and consistent versions")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    parser.add_argument("version")
    args = parser.parse_args()
    verify(args.directory, args.version)
