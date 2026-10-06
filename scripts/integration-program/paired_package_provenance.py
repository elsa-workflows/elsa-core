"""Package graph checks for disposable hosts, with one explicit fixture-only edge."""
from __future__ import annotations

import copy
import hashlib
import json
from pathlib import Path
from xml.etree import ElementTree as ET


def require(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def regular_file(path: Path) -> Path:
    # macOS exposes the system temporary roots through these fixed aliases.
    aliases = {Path("/tmp"): Path("/private/tmp"), Path("/var"): Path("/private/var")}
    for part in (path, *path.parents):
        require(not part.is_symlink() or aliases.get(part) == part.resolve(), "Symlink in package proof input")
    require(path.is_file(), "Missing package proof input")
    return path.resolve()


def read_package_assets(project: Path, tfm: str, *, fixture_project: Path | None = None) -> tuple[dict, list[dict]]:
    """Validate every source edge before projecting only the owned WASM host glue.

    The caller retains the hash of the original project.assets.json. This returned
    copy lets the unchanged package-only validator classify every real library.
    """
    project = regular_file(project.absolute())
    xml = ET.parse(project).getroot()
    require(xml.findtext("PropertyGroup/TargetFramework") == tfm, "Materialized framework mismatch")
    require(xml.findtext("PropertyGroup/IsPackable") == "false", "Fixture must be nonpackable")
    assets_path = regular_file(project.parent / "obj" / "project.assets.json")
    assets = json.loads(assets_path.read_text())
    expected_targets = {tfm}
    if xml.get("Sdk") == "Microsoft.NET.Sdk.BlazorWebAssembly":
        expected_targets.add(tfm + "/browser-wasm")
    require(isinstance(assets.get("targets"), dict) and set(assets["targets"]) == expected_targets,
            "Unexpected restored fixture targets")
    references = xml.findall(".//ProjectReference")
    edges = []
    fixture_key = None
    if fixture_project is None:
        require(not references, "Unexpected source reference without explicit fixture")
    else:
        fixture_project = regular_file(fixture_project.absolute())
        require(project.name == "Elsa.Studio.Host.HostedWasm.csproj" and project.parent.name == "hosted-wasm"
                and project.parent.parent.name == "projects" and xml.get("Sdk") == "Microsoft.NET.Sdk.Web",
                "Only the owned HostedWasm fixture may reference client glue")
        expected = project.parent.parent / "wasm" / "Elsa.Studio.Host.Wasm.csproj"
        require(fixture_project == expected and len(references) == 1
                and (project.parent / references[0].get("Include", "")).resolve() == expected,
                "Unexpected fixture source edge")
        child = ET.parse(fixture_project).getroot()
        require(child.get("Sdk") == "Microsoft.NET.Sdk.BlazorWebAssembly"
                and child.findtext("PropertyGroup/TargetFramework") == tfm
                and child.findtext("PropertyGroup/IsPackable") == "false"
                and not child.findall(".//ProjectReference"), "Invalid referenced fixture glue")
    projected = copy.deepcopy(assets)
    for framework, libraries in assets["targets"].items():
        require(isinstance(libraries, dict), "Malformed restored target")
        source_edges = [key for key, value in libraries.items() if value.get("type") != "package"]
        require(len(source_edges) == (1 if fixture_project else 0), "Unexpected project/source fallback")
        if fixture_project:
            key = source_edges[0]
            require(libraries[key].get("type") == "project" and key.partition("/")[0] == fixture_project.stem,
                    "Source fallback is not the owned fixture")
            record = assets.get("libraries", {}).get(key, {})
            require(record.get("type") == "project" and isinstance(record.get("msbuildProject"), str)
                    and (project.parent / record["msbuildProject"]).resolve() == fixture_project,
                    "Restored fixture path differs from owned source edge")
            require(fixture_key is None or fixture_key == key, "Inconsistent fixture identity")
            fixture_key = key
            del projected["targets"][framework][key]
    # Reject hidden project records as well as target entries.
    for key, record in assets.get("libraries", {}).items():
        require(record.get("type") == "package" or key == fixture_key, "Unclassified source library record")
    if fixture_key:
        del projected["libraries"][fixture_key]
        edges.append({"identity": fixture_key, "project": "projects/wasm/Elsa.Studio.Host.Wasm.csproj",
                      "project_sha256": sha256(fixture_project), "kind": "nonpackable_fixture_only"})
    return projected, edges
