"""Package graph checks for disposable hosts, with one explicit fixture-only edge."""
from __future__ import annotations

import copy
import base64
import hashlib
import json
import re
from pathlib import Path
from xml.etree import ElementTree as ET

import consolidated_candidate_input as candidate
import prove_consolidated_package_consumers as packages


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


def public_archive_evidence(package_id: str, version: str, cache: Path) -> dict:
    """Bind the actual selected public dependency bytes, not just its exception ID."""
    require(packages.PACKAGE_ID_PATTERN.fullmatch(package_id) is not None
            and re.fullmatch(r"[0-9]+(?:\.[0-9]+){2,3}(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?", version) is not None,
            "Unsafe dependency identity")
    directory = cache / package_id.lower() / version
    regular_file(directory / ".nupkg.metadata")
    packages.verify_external_cache_source(package_id, version, cache)
    archive = regular_file(directory / f"{package_id.lower()}.{version}.nupkg")
    sidecar = regular_file(directory / f"{package_id.lower()}.{version}.nupkg.sha512")
    content = archive.read_bytes()
    digest = hashlib.sha512(content).digest()
    require(sidecar.read_text().strip() == base64.b64encode(digest).decode("ascii"), "Public archive sidecar differs")
    return {"sha256": hashlib.sha256(content).hexdigest(), "sha512": digest.hex(), "source": packages.NUGET_ORG}


def candidate_project_validator(layout, verified_root: Path, manifest: dict):
    """Create the mandatory prelaunch validator from verified immutable inputs."""
    manifest_path = regular_file(verified_root / "verified-artifacts.json")
    actual_manifest = json.loads(manifest_path.read_text())
    require(actual_manifest == manifest, "Candidate manifest differs from verified input")
    version, source, by_id, exceptions, exclusions = packages._validated_manifest(actual_manifest)
    require(version == candidate.PRODUCER["version"] == layout.request.version and source == candidate.SOURCE,
            "Candidate project producer mismatch")
    manifest_digest = sha256(manifest_path)
    projects = {path.resolve(): host for host, path in layout.project_paths.items()}

    def validate(project: Path) -> dict:
        project = regular_file(project.absolute())
        require(project in projects, "Project is not owned by this cell")
        require(sha256(manifest_path) == manifest_digest, "Verified candidate manifest changed")
        fixture = layout.project_paths.get("wasm") if projects[project] == "hosted-wasm" else None
        assets, edges = read_package_assets(project, layout.request.framework, fixture_project=fixture)
        packages.verify_restore_isolation(assets, project.parent, layout.packages_root, verified_root / "artifacts")
        restored = packages.validate_project_assets(assets, list(assets["targets"]), set(by_id), version, set(exceptions), exclusions)
        xml = ET.parse(project).getroot()
        roots = {item.get("Include", "").casefold() for item in xml.findall(".//PackageReference")
                 if item.get("Include", "").casefold().startswith("elsa")}
        require(roots <= {item["id"].casefold() for item in restored["internal"]}, "Direct Elsa package roots missing")
        for item in restored["internal"]:
            directory = layout.packages_root / item["id"].lower() / item["version"]
            for name in (".nupkg.metadata", f'{item["id"].lower()}.{item["version"]}.nupkg',
                         f'{item["id"].lower()}.{item["version"]}.nupkg.sha512'):
                regular_file(directory / name)
            regular_file(verified_root / "artifacts" / by_id[item["id"].casefold()]["nupkg"])
        evidence = packages._package_evidence(verified_root / "artifacts", layout.packages_root, by_id, restored, exceptions, version)
        for item in evidence:
            if item["id"].casefold() in by_id:
                item.update(source="verified_original_candidate", repository_commit=source)
            else:
                item.update(public_archive_evidence(item["id"], item["version"], layout.packages_root))
        return {"project_assets_sha256": sha256(project.parent / "obj" / "project.assets.json"),
                "lock_sha256": sha256(regular_file(project.parent / "packages.lock.json")),
                "verified_manifest_sha256": manifest_digest, "packages": evidence,
                "package_graph": restored, "fixture_edges": edges, "package_only_libraries": True}

    return validate
