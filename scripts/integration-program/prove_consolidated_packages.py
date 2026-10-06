#!/usr/bin/env python3
"""Nonpublishing, exact-head package proof of the canonical Elsa.sln.

The opt-in property lifts only imported product guards. It never overrides
IsPackable globally. The inventory comes from evaluated MSBuild properties.
"""
from __future__ import annotations

import argparse
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import tempfile
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

CORE_URL = "https://github.com/elsa-workflows/elsa-core"
RAW_URL = "https://raw.githubusercontent.com/elsa-workflows/elsa-core/"
PROOF_VERSION = re.compile(r"3\.10\.0-proof\.[1-9][0-9]*\.[1-9][0-9]*\Z")
EXTERNAL_PACKAGES = {
    "Elsa.Platform.PackageManifest": "External manifest tooling, not produced by Elsa.sln",
    "Elsa.Platform.PackageManifest.Generator": "External compiler tooling, not produced by Elsa.sln",
}
PROPERTIES = (
    "IsPackable", "PackageId", "PackageVersion", "AssemblyName", "TargetFrameworks",
    "TargetFramework", "IncludeBuildOutput", "IncludeSymbols", "SymbolPackageFormat",
    "IsTestProject", "IsTool", "BuildOutputTargetFolder", "GeneratePackageOnBuild",
    "RepositoryUrl", "PackageProjectUrl",
)


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def run(command: list[str], cwd: Path, *, timeout: int = 300, log: Path | None = None,
        env: dict | None = None) -> str:
    if log is None:
        result = subprocess.run(command, cwd=cwd, env=env, text=True,
                                capture_output=True, timeout=timeout)
        output = result.stdout
        require(result.returncode == 0, f"Command failed: {command!r}\n{output}\n{result.stderr}")
        return output
    with log.open("w") as stream:
        stream.write(json.dumps(command) + "\n")
        stream.flush()
        result = subprocess.run(command, cwd=cwd, env=env, text=True,
                                stdout=stream, stderr=subprocess.STDOUT, timeout=timeout)
    require(result.returncode == 0, f"Command failed ({result.returncode}); see {log}")
    return log.read_text()


def solution_projects(root: Path) -> list[Path]:
    paths = re.findall(r'^Project\("[^"\n]+"\) = "[^"\n]+", "([^"\n]+\.csproj)"',
                       (root / "Elsa.sln").read_text(), re.MULTILINE)
    require(bool(paths), "Elsa.sln contains no C# projects")
    projects = [(root / path.replace("\\", "/")).resolve() for path in paths]
    require(len(set(projects)) == len(projects), "Duplicate solution project")
    for project in projects:
        require(project.is_relative_to(root) and project.is_file(), f"Invalid solution project: {project}")
    return projects


def evaluate(root: Path, project: Path, version: str, proof: bool,
             framework: str | None = None) -> dict[str, str]:
    command = ["dotnet", "msbuild", str(project), "-nologo", "-p:Configuration=Release",
               f"-p:Version={version}", f"-p:ConsolidatedPackageProof={str(proof).lower()}",
               "-getProperty:" + ",".join(PROPERTIES)]
    if framework:
        command.append(f"-p:TargetFramework={framework}")
    raw = run(command, root)
    try:
        return json.loads(raw)["Properties"]
    except (ValueError, KeyError) as error:
        raise ValueError(f"Non-JSON MSBuild evaluation for {project}: {raw}") from error


def package_row(project: str, properties: dict) -> dict:
    return {
        "id": properties["PackageId"], "project": project,
        "assembly_name": properties["AssemblyName"],
        "frameworks": (properties["TargetFrameworks"] or properties["TargetFramework"]).split(";"),
        "include_build_output": properties["IncludeBuildOutput"].lower() != "false",
        "include_symbols": properties["IncludeSymbols"].lower() == "true",
        "is_tool": properties["IsTool"].lower() == "true",
        "symbol_format": properties["SymbolPackageFormat"],
    }


def inventory(root: Path, version: str, commit: str, *, workers: int = 4) -> dict:
    require(bool(PROOF_VERSION.fullmatch(version)), "Version must be 3.10.0-proof.<positive run>.<positive attempt>")
    projects = solution_projects(root)

    def inspect(project: Path) -> tuple[dict | None, dict | None, dict]:
        path = project.relative_to(root).as_posix()
        normal = evaluate(root, project, version, False)
        proof = evaluate(root, project, version, True)
        imported = path.startswith(("src/extensions/", "src/studio/"))
        require(not imported or normal["IsPackable"].lower() == "false",
                f"Imported project is packable by default: {path}")
        require(imported or normal["IsPackable"] == proof["IsPackable"],
                f"Proof changes a non-imported packability setting: {path}")
        record = {"project": path, "normal": normal, "proof": proof}
        if proof["IsPackable"].lower() != "true":
            return None, {"id": proof["PackageId"], "project": path,
                          "reason": "Project or inherited nonpackable setting remains active"}, record
        require(proof["IsTestProject"].lower() != "true", f"Test project is packable: {path}")
        require(proof["PackageVersion"] == version, f"Wrong evaluated package version: {path}")
        require(proof["RepositoryUrl"].rstrip("/") == CORE_URL and
                proof["PackageProjectUrl"].rstrip("/") == CORE_URL,
                f"Noncanonical package metadata: {path}")
        row = package_row(path, proof)
        require(all(row["frameworks"]), f"No evaluated frameworks: {path}")
        row["framework_properties"] = {}
        for framework in row["frameworks"]:
            inner = evaluate(root, project, version, True, framework)
            require(inner["PackageId"] == row["id"] and inner["PackageVersion"] == version,
                    f"Framework changes package identity/version: {path}/{framework}")
            row["framework_properties"][framework] = {
                "assembly_name": inner["AssemblyName"], "package_version": inner["PackageVersion"],
                "include_build_output": inner["IncludeBuildOutput"].lower() != "false",
            }
        row["nupkg"] = f"{row['id']}.{version}.nupkg"
        row["snupkg"] = f"{row['id']}.{version}.snupkg" if row["include_symbols"] and row["include_build_output"] else None
        require(not row["snupkg"] or row["symbol_format"] == "snupkg", f"Unsupported symbol format: {path}")
        return row, None, record

    with ThreadPoolExecutor(max_workers=workers) as pool:
        results = list(pool.map(inspect, projects))
    packages = sorted((row for row, _, _ in results if row), key=lambda row: row["id"].casefold())
    require(len({row["id"].casefold() for row in packages}) == len(packages), "Duplicate evaluated PackageId")
    return {"version": version, "source_commit": commit, "repository_url": CORE_URL,
            "published": False, "packages": packages,
            "exclusions": sorted((row for _, row, _ in results if row), key=lambda row: row["project"]),
            "evaluations": [record for _, _, record in results], "external_package_exceptions": EXTERNAL_PACKAGES,
            "icon_sha256": hashlib.sha256((root / "icon.png").read_bytes()).hexdigest()}


def archive_names(archive: zipfile.ZipFile) -> set[str]:
    names = archive.namelist()
    require(len(set(names)) == len(names), "Duplicate ZIP entries")
    for name in names:
        path = PurePosixPath(name)
        require(not path.is_absolute() and ".." not in path.parts and "\\" not in name,
                f"Unsafe ZIP entry: {name}")
    return set(names)


def metadata(archive: zipfile.ZipFile) -> ET.Element:
    files = [name for name in archive_names(archive) if name.endswith(".nuspec")]
    require(len(files) == 1, "Artifact must contain exactly one nuspec")
    document = ET.fromstring(archive.read(files[0]))
    for element in document.iter():
        element.tag = element.tag.rsplit("}", 1)[-1]
    result = document.find("metadata")
    require(result is not None, "Nuspec metadata is missing")
    return result


def verify_metadata(data: ET.Element, row: dict, manifest: dict) -> list[dict]:
    require(data.findtext("id") == row["id"] and data.findtext("version") == manifest["version"],
            f"Artifact identity/version mismatch: {row['id']}")
    repository = data.find("repository")
    require(repository is not None and repository.get("type") == "git" and
            repository.get("url", "").rstrip("/") == CORE_URL and
            repository.get("commit") == manifest["source_commit"],
            f"Artifact repository metadata mismatch: {row['id']}")
    produced = {package["id"].casefold() for package in manifest["packages"]}
    exceptions = {name.casefold() for name in manifest["external_package_exceptions"]}
    groups = []
    for group in data.findall("dependencies/group"):
        dependencies = []
        for dependency in group.findall("dependency"):
            identifier, version = dependency.get("id", ""), dependency.get("version", "")
            require(bool(identifier) and bool(version), "Empty artifact dependency")
            if identifier.casefold() in produced:
                require(version in (manifest["version"], f"[{manifest['version']}]", f"[{manifest['version']}, )",
                                    f"[{manifest['version']},)"),
                        f"Internal dependency is not the proof version: {row['id']} -> {identifier} {version}")
            elif identifier.casefold().startswith("elsa"):
                require(identifier.casefold() in exceptions,
                        f"Unavailable or excluded internal dependency: {row['id']} -> {identifier}")
            dependencies.append({"id": identifier, "version": version})
        groups.append({"framework": group.get("targetFramework", ""), "dependencies": dependencies})
    require(not data.findall("dependencies/dependency"), "Ungrouped dependencies require explicit supported handling")
    return groups


def verify_artifacts(artifacts: Path, manifest: dict) -> dict:
    require(artifacts.is_dir() and not artifacts.is_symlink(), "Artifact directory is missing or symlinked")
    expected = {row[key] for row in manifest["packages"] for key in ("nupkg", "snupkg") if row[key]}
    actual = {path.name for path in artifacts.iterdir()}
    require(actual == expected, f"Artifact inventory mismatch; missing={sorted(expected-actual)}, unexpected={sorted(actual-expected)}")
    for row in manifest["packages"]:
        for key in ("nupkg", "snupkg"):
            if row[key]:
                path = artifacts / row[key]
                require(path.is_file() and not path.is_symlink(), f"Not a regular artifact: {path}")
        path = artifacts / row["nupkg"]
        data = path.read_bytes()
        row["nupkg_sha256"] = hashlib.sha256(data).hexdigest()
        row["nupkg_sha512"] = hashlib.sha512(data).hexdigest()
        with zipfile.ZipFile(path) as archive:
            names = archive_names(archive)
            nuspec = metadata(archive)
            row["dependency_groups"] = verify_metadata(nuspec, row, manifest)
            require(nuspec.findtext("projectUrl", "").rstrip("/") == CORE_URL,
                    f"Noncanonical project URL: {row['id']}")
            icon = nuspec.findtext("icon")
            require(icon is not None and icon in names, f"Missing declared package icon: {row['id']}")
            require(hashlib.sha256(archive.read(icon)).hexdigest() == manifest["icon_sha256"],
                    f"Package icon differs from canonical Core icon: {row['id']}")
            row["assemblies"] = []
            for framework, properties in row["framework_properties"].items():
                if not properties["include_build_output"]:
                    continue
                base = f"tools/{framework}/any" if row["is_tool"] else f"lib/{framework}"
                assembly = f"{base}/{properties['assembly_name']}.dll"
                require(assembly in names, f"Missing framework assembly: {row['id']} {assembly}")
                row["assemblies"].append({"framework": framework, "assembly": assembly,
                                          "pdb": f"{base}/{properties['assembly_name']}.pdb"})
            if row["snupkg"]:
                symbol_path = artifacts / row["snupkg"]
                row["snupkg_sha256"] = hashlib.sha256(symbol_path.read_bytes()).hexdigest()
                with zipfile.ZipFile(symbol_path) as symbols:
                    symbol_names = archive_names(symbols)
                    verify_metadata(metadata(symbols), row, manifest)
                    expected_pdbs = {assembly["pdb"] for assembly in row["assemblies"]}
                    require({name for name in symbol_names if name.endswith(".pdb")} == expected_pdbs,
                            f"Symbol framework coverage mismatch: {row['id']}")
            else:
                require(not row["assemblies"], f"Assembly package has no external symbol proof: {row['id']}")
    return manifest


def source_url(path: str, maps: dict[str, str]) -> str | None:
    matches = []
    for pattern, target in maps.items():
        if "*" not in pattern:
            if path == pattern:
                matches.append((len(pattern), target))
            continue
        require(pattern.count("*") == 1 and target.count("*") == 1, "Unsupported SourceLink wildcard map")
        before, after = pattern.split("*")
        if path.startswith(before) and path.endswith(after):
            replacement = path[len(before):len(path)-len(after) if after else None]
            matches.append((len(before)+len(after), target.replace("*", replacement)))
    return max(matches, default=(0, None))[1]


def verify_documents(root: Path, row: dict, framework: str, inspection: dict,
                     commit: str, remote: bool, cache: dict | None = None) -> dict:
    cache = cache if cache is not None else {}
    maps = inspection.get("source_link", {}).get("documents", {})
    require(bool(maps), f"No SourceLink map: {row['id']}/{framework}")
    prefix = f"{RAW_URL}{commit}/"
    require(all(isinstance(url, str) and url.startswith(prefix) for url in maps.values()),
            f"SourceLink map does not resolve to exact Core head: {row['id']}/{framework}")
    documents = inspection.get("documents", [])
    require(bool(documents), f"Portable PDB has no source documents: {row['id']}/{framework}")
    seen = set()
    counts = {"tracked_documents": 0, "remote_documents": 0, "embedded_tracked_documents": 0,
              "embedded_generated_documents": 0}
    records = []
    project_dir = PurePosixPath(row["project"]).parent.as_posix()
    for document in documents:
        path = document["path"]
        require(path not in seen, f"Duplicate PDB document: {path}")
        seen.add(path)
        algorithm = document["algorithm"]
        checksum = document["checksum"]
        require(algorithm in ("sha1", "sha256") and bool(re.fullmatch(r"[0-9a-f]+", checksum)), "Invalid document checksum")
        embedded = document.get("embedded_checksum")
        require(embedded is None or embedded == checksum, f"Embedded source checksum mismatch: {path}")
        url = source_url(path, maps)
        require(url is not None and url.startswith(prefix), f"Unmapped source document: {path}")
        relative = url[len(prefix):]
        require(not PurePosixPath(relative).is_absolute() and ".." not in PurePosixPath(relative).parts and "\\" not in relative,
                f"Unsafe source document path: {relative}")
        blob_key = ("blob", commit, relative)
        if blob_key not in cache:
            blob = subprocess.run(["git", "show", f"{commit}:{relative}"], cwd=root, capture_output=True)
            cache[blob_key] = blob.stdout if blob.returncode == 0 else None
        blob_bytes = cache[blob_key]
        if blob_bytes is not None:
            require(hashlib.new(algorithm, blob_bytes).hexdigest() == checksum, f"Tracked source differs from exact Git blob: {relative}")
            counts["tracked_documents"] += 1
            if embedded is not None:
                counts["embedded_tracked_documents"] += 1
            if remote:
                remote_key = ("remote", url)
                if remote_key not in cache:
                    with urllib.request.urlopen(url, timeout=45) as response:
                        cache[remote_key] = response.read()
                require(hashlib.new(algorithm, cache[remote_key]).hexdigest() == checksum, f"Remote source checksum mismatch: {url}")
                counts["remote_documents"] += 1
            records.append({"path": relative, "checksum": checksum, "algorithm": algorithm,
                            "embedded": embedded is not None, "remote_fetched": remote})
        else:
            # Studio explicitly embeds SDK-generated top-level source files. No
            # tracked source, arbitrary untracked file or other project is exempt.
            generated_prefix = f"{project_dir}/obj/Release/{framework}/"
            generated = relative.startswith(generated_prefix) and "/" not in relative[len(generated_prefix):]
            generated = generated and (relative.endswith((".AssemblyInfo.cs", ".AssemblyAttributes.cs", ".GlobalUsings.g.cs")))
            require(row["project"].startswith("src/studio/") and generated and embedded is not None,
                    f"Untracked source document has no audited generated-source policy: {relative}")
            counts["embedded_generated_documents"] += 1
            records.append({"path": relative, "checksum": checksum, "algorithm": algorithm,
                            "embedded": True, "generated": True, "remote_fetched": False})
    require(counts["tracked_documents"] > 0, f"No tracked source covered: {row['id']}/{framework}")
    return {"framework": framework, **counts, "documents": records}


def provenance(root: Path, artifacts: Path, manifest: dict, output: Path, *, remote: bool) -> list[dict]:
    helper = root / "scripts/integration-program/VerifyPackageSymbolPair/VerifyPackageSymbolPair.csproj"
    helper_out = output / "symbol-verifier"
    run(["dotnet", "build", str(helper), "--configuration", "Release", "--output", str(helper_out)],
        root, timeout=600, log=output / "symbol-verifier.log")
    executable = helper_out / "VerifyPackageSymbolPair.dll"
    results = []
    cache = {}
    with tempfile.TemporaryDirectory(prefix="elsa-symbol-proof-") as temporary:
        temporary_path = Path(temporary)
        for row in manifest["packages"]:
            frames = []
            if not row["assemblies"]:
                results.append({"id": row["id"], "assembly_free": True, "frameworks": []})
                continue
            with zipfile.ZipFile(artifacts / row["nupkg"]) as package, zipfile.ZipFile(artifacts / row["snupkg"]) as symbols:
                for assembly in row["assemblies"]:
                    dll = temporary_path / Path(assembly["assembly"]).name
                    pdb = temporary_path / Path(assembly["pdb"]).name
                    dll.write_bytes(package.read(assembly["assembly"]))
                    pdb.write_bytes(symbols.read(assembly["pdb"]))
                    inspection = json.loads(run(["dotnet", str(executable), str(dll), str(pdb), "--inspect-documents"], root))
                    frames.append(verify_documents(root, row, assembly["framework"], inspection, manifest["source_commit"], remote, cache))
            results.append({"id": row["id"], "frameworks": frames})
    return results


def clean_head(root: Path) -> str:
    require(not run(["git", "status", "--porcelain"], root).strip(), "Proof requires a clean committed worktree")
    commit = run(["git", "rev-parse", "HEAD"], root).strip()
    require(bool(re.fullmatch(r"[0-9a-f]{40}", commit)), "Proof requires a full Git commit")
    return commit


def write_json(path: Path, data: dict | list) -> None:
    path.write_text(json.dumps(data, indent=2, sort_keys=True) + "\n")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", required=True)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--inventory-only", action="store_true")
    parser.add_argument("--remote-sources", action="store_true", help="Fetch every tracked document from the exact pushed Core commit")
    args = parser.parse_args()
    require(bool(PROOF_VERSION.fullmatch(args.version)), "Version must be 3.10.0-proof.<positive run>.<positive attempt>")
    root = Path(__file__).resolve().parents[2]
    for name in ("IsPackable", "PackageVersion", "GeneratePackageOnBuild", "TargetFramework", "TargetFrameworks"):
        require(name not in os.environ, f"Proof refuses an inherited MSBuild override: {name}")
    output = args.output.resolve()
    require(not output.exists() and not output.is_relative_to(root), "Output must be a new directory outside the worktree")
    output.mkdir(parents=True)
    commit = clean_head(root)
    manifest = inventory(root, args.version, commit)
    write_json(output / "inventory.json", manifest)
    print(f"Evaluated {len(manifest['packages'])} packages and {len(manifest['exclusions'])} exclusions", flush=True)
    if args.inventory_only:
        return
    packages = root / "packages"
    require(not packages.exists() or not any(packages.iterdir()), "Canonical packages output must be empty before proof; use an isolated worktree")
    environment = dict(os.environ, ConsolidatedPackageProof="true", ContinuousIntegrationBuild="true")
    run([str(root / "build.sh"), "Compile+Pack", "--version", args.version], root,
        timeout=7200, log=output / "compile-pack.log", env=environment)
    artifacts = output / "artifacts"
    shutil.copytree(packages, artifacts)
    manifest = verify_artifacts(artifacts, manifest)
    write_json(output / "verified-artifacts.json", manifest)
    sources = provenance(root, artifacts, manifest, output, remote=args.remote_sources)
    write_json(output / "source-provenance.json", sources)
    from prove_consolidated_package_consumers import prove
    consumers = prove(artifacts, manifest, output / "consumers")
    require(clean_head(root) == commit, "Source changed during package proof")
    receipt = {"result": "passed", "published": False, "source_commit": commit, "version": args.version,
               "package_count": len(manifest["packages"]), "exclusion_count": len(manifest["exclusions"]),
               "remote_sources_verified": args.remote_sources,
               "provenance": sources, "consumers": consumers,
               "limits": ["Consumers are representative; this is not behavioral certification of every package.",
                          "No publication, publisher cutover, npm artifact proof or live Slack certification."]}
    if not args.remote_sources:
        receipt["limits"].append("Tracked sources checked against exact local Git blobs; remote SourceLink fetch remains unverified.")
    write_json(output / "receipt.json", receipt)
    print(f"Package proof passed: {output / 'receipt.json'}", flush=True)


if __name__ == "__main__":
    main()
