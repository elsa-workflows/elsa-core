#!/usr/bin/env python3
"""Real package-only SQLite suspension/resumption across released Elsa versions."""
from __future__ import annotations

import argparse
from contextlib import closing
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import sqlite3
import stat
from typing import Any
from urllib.parse import quote
from xml.etree import ElementTree as ET
import zipfile

import prove_consolidated_package_consumers as packages

BASELINES = {
    "3.8.4": "33181ae3048f628f591a0155b5665a8e4d1bcea2",
    "3.9.0": "6436609a1a3874d3fea7ccf690897792f5a1f702",
}
PROOF_VERSION = "3.10.0-proof.37410240989.1"
PROOF_SOURCE = "44083dc4f47d56aa2d5c215b0c7479c0b7290cb0"
PROOF_RUN = 37410240989
PROOF_ARTIFACT = 11390805430
PROOF_ARCHIVE_SHA256 = "8317e5766591f9da2d1f83cd6a85274bd41efbfbd0039d6a1bcfaf0c9bf81636"
REQUIRED = ("Elsa", "Elsa.Persistence.EFCore.Sqlite")
FIXTURE = Path(__file__).resolve().parent / "persisted-workflow-upgrade" / "Program.cs"
MATRIX = {(baseline, tfm) for baseline in BASELINES for tfm in packages.FRAMEWORKS}


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_json(path: Path, value: Any) -> None:
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)


def safe_members(package: zipfile.ZipFile) -> list[zipfile.ZipInfo]:
    members = package.infolist()
    names = set()
    for member in members:
        path = Path(member.filename)
        require(not path.is_absolute() and ".." not in path.parts and "\\" not in member.filename and
                not stat.S_ISLNK(member.external_attr >> 16), "Unsafe proof archive entry")
        require(member.filename not in names, "Duplicate proof archive entry")
        names.add(member.filename)
    return members


def validate_artifact_metadata(metadata: dict) -> None:
    workflow = metadata.get("workflow_run", {})
    require(metadata.get("id") == PROOF_ARTIFACT and metadata.get("name") == f"consolidated-proof-{PROOF_RUN}-1" and
            metadata.get("expired") is False and workflow.get("id") == PROOF_RUN and workflow.get("head_sha") == PROOF_SOURCE,
            "Artifact metadata does not identify the original available proof")


def extract_archive(archive: Path, destination: Path) -> None:
    require(sha256(archive) == PROOF_ARCHIVE_SHA256, "Original proof archive digest mismatch")
    require(not destination.exists(), "Refusing to overwrite extracted proof")
    with zipfile.ZipFile(archive) as package:
        members = safe_members(package)
        destination.mkdir(parents=True)
        for member in members:
            path = destination / member.filename
            if member.is_dir():
                path.mkdir(parents=True, exist_ok=True)
            else:
                path.parent.mkdir(parents=True, exist_ok=True)
                with package.open(member) as source, path.open("xb") as target:
                    shutil.copyfileobj(source, target)


def verify_archive(root: Path, archive: Path) -> None:
    require(sha256(archive) == PROOF_ARCHIVE_SHA256, "Original proof archive digest mismatch")
    with zipfile.ZipFile(archive) as package:
        expected = set()
        for member in safe_members(package):
            if member.is_dir():
                continue
            require(member.filename not in expected, "Duplicate proof archive entry")
            expected.add(member.filename)
            extracted = root / member.filename
            require(extracted.is_file() and not extracted.is_symlink() and extracted.resolve().is_relative_to(root.resolve())
                    and extracted.read_bytes() == package.read(member), "Extracted evidence differs from original archive")
        require({str(path.relative_to(root)) for path in root.rglob("*") if path.is_file()} == expected, "Unexpected extracted evidence files")


def verify_artifact(root: Path, archive: Path) -> tuple[dict, dict, dict, set]:
    verify_archive(root, archive)
    manifest = json.loads((root / "verified-artifacts.json").read_text())
    version, source, by_id, exceptions, exclusions = packages._validated_manifest(manifest)
    require(version == PROOF_VERSION and source == PROOF_SOURCE and manifest.get("published") is False,
            "Candidate must be the pinned unpublished verified proof")
    require(len(by_id) == 225 and len(manifest["exclusions"]) == 124, "Pinned proof inventory is incomplete")
    for package_id in REQUIRED:
        require(package_id.casefold() in by_id, f"Missing required artifact {package_id}")
    for entry in by_id.values():
        for filename_key, digest_key in (("nupkg", "nupkg_sha256"), ("snupkg", "snupkg_sha256")):
            name = entry.get(filename_key)
            require(isinstance(name, str) and Path(name).name == name, "Unsafe artifact filename")
            artifact = root / "artifacts" / name
            require(artifact.is_file() and sha256(artifact) == entry.get(digest_key), f"Missing/tampered verified artifact {name}")
    proof = json.loads((root / "receipt.json").read_text())
    require(proof.get("result") == "passed" and proof.get("published") is False and
            proof.get("remote_sources_verified") is True and proof.get("version") == version and proof.get("source_commit") == source,
            "Original package proof did not pass for the pinned source/version")
    return manifest, by_id, exceptions, exclusions


def spec_for(baseline: str, tfm: str, cell: Path) -> dict:
    identity = f"upgrade-{baseline}-{tfm}"
    return {
        "definition_id": identity + "-definition", "definition_version_id": identity + "-v1",
        "instance_id": identity + "-instance", "correlation_id": identity + "-correlation",
        "sentinel_id": "durable-sentinel", "event_name": identity + "-event", "text": identity,
        "expected": {"text": identity, "number": 37, "flag": True},
        "management": str(cell / "management.db"), "runtime": str(cell / "runtime.db"),
        "baseline_receipt": str(cell / "suspend.json"),
    }


def check_state(state: dict, spec: dict, finished: bool, original: dict | None = None) -> None:
    for key in ("definition_id", "definition_version_id", "instance_id", "correlation_id"):
        require(state.get(key) == spec[key], f"State identity mismatch: {key}")
    require(state.get("definition_version") == 1, "Definition version mismatch")
    # JSON equality alone considers True == 1; check the exact primitive types as well.
    for field in ("input", "output") if finished else ("input",):
        check_typed_values(state.get(field, {}), spec["expected"], f"Typed {field}")
    require(state.get("sentinel") == spec["text"], "Sentinel mismatch")
    require(state.get("status") == ("Finished" if finished else "Running") and
            state.get("sub_status") == ("Finished" if finished else "Suspended"), "Unexpected durable status")
    require(state.get("state_bookmark_count") == (0 if finished else 1) and
            state.get("stored_bookmark_count") == (0 if finished else 1), "Unexpected durable bookmarks")
    if not finished:
        bookmark = state.get("bookmark", {})
        stored = state.get("stored_bookmark", {})
        require(bool(bookmark.get("Id")) and bookmark.get("ActivityId") == "upgrade-event" and
                bookmark.get("Hash") == stored.get("Hash") and bookmark.get("Id") == stored.get("Id"), "Bookmark mismatch")
        if original:
            require(bookmark == original.get("bookmark") and stored == original.get("stored_bookmark"), "Original bookmark changed")
    if original:
        for key in ("definition_json_sha256", "input", "sentinel", "input_metadata", "output_metadata", "activity_graph"):
            require(state.get(key) == original.get(key), f"Original durable field changed: {key}")


def check_typed_values(actual: dict, expected: dict, label: str = "Raw typed") -> None:
    require(set(actual) == set(expected), f"{label} values missing")
    for key, value in expected.items():
        require(type(actual[key]) is type(value) and actual[key] == value, f"{label} value mismatch: {key}")


def check_phase(receipt: dict, spec: dict, phase: str, original: dict | None = None) -> None:
    require(receipt.get("passed") is True and receipt.get("phase") == phase, f"Phase failed: {phase}")
    require(receipt.get("tenant_id") == "", "Normal default tenant required")
    check_state(receipt["state"], spec, phase != "suspend", original)
    if phase == "resume":
        check_state(receipt["before"], spec, False, original)
    for context in ("management", "runtime"):
        before = receipt["migrations_before"][context]
        after = receipt["migrations_after"][context]
        require(after["pending"] == [] and set(before["applied"]).issubset(after["applied"]), "Normal migrations did not finish")
        require(after["data_source"] == spec[context], "Migration context/file mismatch")


def readonly(path: Path) -> sqlite3.Connection:
    connection = sqlite3.connect(f"file:{quote(str(path.resolve()), safe='/')}?mode=ro", uri=True)
    connection.row_factory = sqlite3.Row
    connection.execute("PRAGMA query_only=ON")
    return connection


def inspect_database(path: Path, snapshot: Path | None = None) -> dict:
    """Fresh read-only connection; SQLite backup incorporates committed WAL pages."""
    with closing(readonly(path)) as connection:
        require(connection.execute("PRAGMA integrity_check").fetchone()[0] == "ok", f"SQLite integrity failed: {path.name}")
        if snapshot:
            require(not snapshot.exists(), "Refusing to overwrite immutable baseline snapshot")
            with closing(sqlite3.connect(snapshot)) as destination:
                connection.backup(destination)
        schema = [dict(row) for row in connection.execute("SELECT name, sql FROM sqlite_master WHERE type='table' ORDER BY name")]
        tables = {row["name"] for row in schema}
        require("__EFMigrationsHistory" in tables, "Migration history missing")
        history = [dict(row) for row in connection.execute('SELECT * FROM "__EFMigrationsHistory" ORDER BY MigrationId')]
        rows = {}
        for name in ("WorkflowDefinitions", "WorkflowInstances", "Bookmarks"):
            if name in tables:
                rows[name] = [dict(row) for row in connection.execute(f'SELECT * FROM "{name}" ORDER BY Id')]
        # The fixture only creates built-in synthetic data. BinaryData is normally NULL.
        for table_rows in rows.values():
            for row in table_rows:
                for key, value in list(row.items()):
                    if isinstance(value, bytes):
                        row[key] = {"blob_sha256": hashlib.sha256(value).hexdigest(), "length": len(value)}
        wal = Path(str(path) + "-wal")
        return {"file": str(path), "sha256": sha256(path), "wal_sha256": sha256(wal) if wal.is_file() else None,
                "journal_mode": connection.execute("PRAGMA journal_mode").fetchone()[0], "schema": schema, "history": history,
                "rows": rows, "integrity": "ok", "snapshot_sha256": sha256(snapshot) if snapshot else None}


def normalize_state(value: Any) -> Any:
    """Resolve the provider's reference-preserving JSON without changing its evidence."""
    references = {}
    def collect(node):
        if isinstance(node, dict):
            if "$id" in node:
                references[node["$id"]] = node
            for child in node.values():
                collect(child)
        elif isinstance(node, list):
            for child in node:
                collect(child)
    collect(value)
    def convert(node, visited=frozenset()):
        if isinstance(node, dict):
            if "$ref" in node:
                require(node["$ref"] not in visited, "Cyclic state reference")
                return convert(references[node["$ref"]], visited | {node["$ref"]})
            if "$values" in node:
                return convert(node["$values"], visited)
            return {key: convert(child, visited) for key, child in node.items() if key not in ("$id", "_type")}
        if isinstance(node, list):
            return [convert(child, visited) for child in node]
        return node
    return convert(value)


def corroborate(databases: dict, state: dict, spec: dict, finished: bool) -> None:
    management = databases["management"]["rows"]
    definitions = management.get("WorkflowDefinitions", [])
    instances = management.get("WorkflowInstances", [])
    require(len(definitions) == 1 and len(instances) == 1, "Unexpected durable definition/instance row count")
    definition, instance = definitions[0], instances[0]
    for row, expected in ((definition, spec["definition_version_id"]), (instance, spec["instance_id"])):
        require(row["Id"] == expected and row["DefinitionId"] == spec["definition_id"], "SQLite identity mismatch")
        require(row.get("TenantId") in (None, ""), "SQLite default tenant mismatch")
    require(definition.get("OriginalSource") is None, "Unexpected persisted OriginalSource")
    require(instance["DefinitionVersionId"] == spec["definition_version_id"] and instance["CorrelationId"] == spec["correlation_id"], "SQLite instance identity mismatch")
    require(instance.get("DataCompressionAlgorithm") == "None", "Unexpected state compression; cannot inspect as plain JSON")
    persisted = normalize_state(json.loads(instance["Data"]))
    for key, expected in (("id", spec["instance_id"]), ("definitionId", spec["definition_id"]),
                          ("definitionVersionId", spec["definition_version_id"]), ("input", spec["expected"])):
        require(persisted.get(key) == expected, f"Raw serialized state mismatch: {key}")
    check_typed_values(persisted["input"], spec["expected"])
    if not finished:
        roots = [context for context in persisted["activityExecutionContexts"] if context.get("parentContextId") is None]
        require(len(roots) == 1 and roots[0].get("properties", {}).get("Variables", {}).get(spec["sentinel_id"]) == spec["text"], "Raw sentinel mismatch")
    bookmarks = databases["runtime"]["rows"].get("Bookmarks", [])
    require(len(bookmarks) == (0 if finished else 1) and len(persisted.get("bookmarks", [])) == len(bookmarks), "Raw bookmark count mismatch")
    if not finished:
        saved, stored = state["bookmark"], bookmarks[0]
        for key in ("Id", "Hash", "ActivityInstanceId", "Name"):
            require(stored[key] == saved[key] and persisted["bookmarks"][0][key[0].lower() + key[1:]] == saved[key], "Raw bookmark identity mismatch")
        require(persisted["bookmarks"][0]["activityId"] == saved["ActivityId"] == "upgrade-event" and
                persisted["bookmarks"][0]["activityNodeId"] == saved["ActivityNodeId"], "Raw activity identity mismatch")
        require(stored["WorkflowInstanceId"] == spec["instance_id"] and stored["CorrelationId"] == spec["correlation_id"] and
                normalize_state(json.loads(stored["SerializedPayload"])) == {"eventName": spec["event_name"]} and
                saved["Payload"] == {"EventName": spec["event_name"]}, "Raw bookmark payload mismatch")
    else:
        check_typed_values(persisted.get("output", {}), spec["expected"])


def package_provenance(project: Path, cache: Path, tfm: str, version: str, root: Path, by_id: dict, exceptions: dict, exclusions: set) -> dict:
    assets_path = project / "obj" / "project.assets.json"
    assets = json.loads(assets_path.read_text())
    restore = assets["project"]["restore"]
    expected_sources = {packages.NUGET_ORG} | ({str((root / "artifacts").resolve())} if version == PROOF_VERSION else set())
    require(set(restore["sources"]) == expected_sources, "Effective restore sources leaked")
    require({str(Path(path).resolve()) for path in assets["packageFolders"]} == {str(cache.resolve())}, "Effective package cache/fallback folders leaked")
    require(restore.get("configFilePaths") == [str(project / "NuGet.Config")], "Parent NuGet config leaked")
    if version == PROOF_VERSION:
        restored = packages.validate_project_assets(assets, [tfm], set(by_id), version, set(exceptions), exclusions)
        evidence = packages._package_evidence(root / "artifacts", cache, by_id, restored, exceptions, version)
        for item in evidence:
            if item["id"].casefold() not in by_id:
                packages.verify_external_cache_source(item["id"], item["version"], cache)
                archive = cache / item["id"].lower() / item["version"] / f'{item["id"].lower()}.{item["version"]}.nupkg'
                require(archive.is_file(), "Public dependency archive missing")
                item["sha256"] = sha256(archive)
                item["sha512"] = hashlib.sha512(archive.read_bytes()).hexdigest()
            else:
                item["repository_commit"] = PROOF_SOURCE
    else:
        target = assets.get("targets", {}).get(tfm)
        require(target is not None and set(assets["targets"]) == {tfm}, "Unexpected baseline targets")
        baseline_ids = {key.split("/")[0] for key in target if key.split("/")[0].casefold().startswith("elsa")}
        require(not ({x.casefold() for x in baseline_ids} & set(exceptions)), "Unexpected baseline external Elsa exception")
        restored = packages.validate_project_assets(assets, [tfm], baseline_ids, version, set())
        evidence = []
        for key, library in target.items():
            package_id, package_version = key.split("/")
            item = packages.verify_external_cache_source(package_id, package_version, cache)
            archive = cache / package_id.lower() / package_version / f"{package_id.lower()}.{package_version}.nupkg"
            require(archive.is_file(), "Baseline cache archive missing")
            item["sha256"] = sha256(archive)
            item["sha512"] = hashlib.sha512(archive.read_bytes()).hexdigest()
            if package_id in baseline_ids:
                with zipfile.ZipFile(archive) as package:
                    nuspec = ET.fromstring(package.read(next(name for name in package.namelist() if name.endswith(".nuspec"))))
                repository = nuspec.find(".//{*}repository")
                require(repository is not None and repository.get("commit") == BASELINES[version], f"Baseline nuspec source mismatch: {key}")
                item["repository_commit"] = repository.get("commit")
                item["repository_url"] = repository.get("url")
            evidence.append(item)
    require({x.casefold() for x in REQUIRED}.issubset({x["id"].casefold() for x in restored["internal"]}), "Required fixture packages missing from graph")
    isolation = {name: sha256(project / name) for name in ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "global.json", "NuGet.Config", "Consumer.csproj")}
    return {"packages": evidence, "assets_sha256": sha256(assets_path), "lock_sha256": sha256(project / "packages.lock.json"), "graph": restored,
            "isolation": isolation, "effective_restore_sources": sorted(expected_sources), "package_only": True}


def check_assemblies(receipt: dict, project: Path, cache: Path, tfm: str) -> None:
    assets = json.loads((project / "obj" / "project.assets.json").read_text())
    expected = {}
    for key, library in assets["targets"][tfm].items():
        package_id, version = key.split("/")
        for entry in library.get("runtime", {}):
            if entry.endswith(".dll") and Path(entry).name.startswith("Elsa"):
                expected[Path(entry).stem] = sha256(cache / package_id.lower() / version / entry)
    assemblies = receipt.get("assemblies", [])
    require(bool(assemblies), "No loaded Elsa assembly provenance")
    for assembly in assemblies:
        require(assembly["name"] in expected and assembly["sha256"] == expected[assembly["name"]], "Loaded Elsa assembly differs from restored package")
        require(Path(assembly["location"]).is_relative_to(project / "bin"), "Loaded assembly escaped standalone consumer")


def record_command(command: list[str], cwd: Path, environment: dict, log: Path, timeout: int, records: list[dict]) -> None:
    record = {"command": command, "log": str(log), "exit_code": None, "timed_out": False}
    records.append(record)
    try:
        record.update(packages._run_command(command, cwd, environment, log, timeout))
    except RuntimeError as error:
        match = re.match(r"Command failed \((-?\d+)\):", str(error))
        if match:
            record["exit_code"] = int(match.group(1))
        record["timed_out"] = isinstance(error.__cause__, packages.subprocess.TimeoutExpired)
        raise


def prepare_project(project: Path, cache: Path, version: str, tfm: str, config: str, environment: dict, sdk: str, commands: list[dict]) -> None:
    project.mkdir()
    cache.mkdir()
    for name in ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props"):
        (project / name).write_text("<Project />\n")
    write_json(project / "global.json", {"sdk": {"version": sdk, "rollForward": "disable"}})
    references = "\n".join(f'    <PackageReference Include="{name}" Version="{version}" />' for name in REQUIRED)
    (project / "Consumer.csproj").write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>{tfm}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><LangVersion>latest</LangVersion>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile><IsPackable>false</IsPackable>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <RestoreFallbackFolders></RestoreFallbackFolders>
    <DisableImplicitNuGetFallbackFolder>true</DisableImplicitNuGetFallbackFolder>
    <DisableImplicitLibraryPacksFolder>true</DisableImplicitLibraryPacksFolder>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
{references}
  </ItemGroup>
</Project>
''')
    shutil.copy2(FIXTURE, project / "Program.cs")
    (project / "NuGet.Config").write_text(config)
    for command, log in ((["dotnet", "restore", "--configfile", "NuGet.Config", "--packages", str(cache), "--force-evaluate", "--no-cache", "--nologo"], "restore.log"),
                         (["dotnet", "build", "--no-restore", "--configuration", "Release", "--nologo", "-p:UseSharedCompilation=false"], "build.log")):
        record_command(command, project, environment, project / log, 1200, commands)


def run_cell(baseline: str, tfm: str, cell: Path, root: Path, by_id: dict, exceptions: dict, exclusions: set, sdk: str) -> dict:
    cell.mkdir()
    spec = spec_for(baseline, tfm, cell)
    write_json(cell / "spec.json", spec)
    result = {"baseline": baseline, "framework": tfm, "passed": False, "phases": {}, "provenance": {}, "databases": {}, "commands": []}
    environment = {key: value for key, value in os.environ.items() if key not in ("GH_TOKEN", "GITHUB_TOKEN")}
    environment.update(DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER="1", MSBUILDDISABLENODEREUSE="1")
    public = '<configuration><packageSources><clear/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><packageSource key="nuget.org"><package pattern="*"/></packageSource></packageSourceMapping></configuration>'
    try:
        for label, version, config in (("baseline", baseline, public), ("candidate", PROOF_VERSION, packages.render_nuget_config(root / "artifacts", [x["id"] for x in by_id.values()], exceptions))):
            project, cache = cell / label, cell / f"{label}-packages"
            env = dict(environment, NUGET_PACKAGES=str(cache))
            prepare_project(project, cache, version, tfm, config, env, sdk, result["commands"])
            result["provenance"][label] = package_provenance(project, cache, tfm, version, root, by_id, exceptions, exclusions)
            for phase in (["suspend"] if label == "baseline" else ["resume", "verify"]):
                log = cell / f"{phase}.log"
                record_command(["dotnet", str(project / "bin" / "Release" / tfm / "Consumer.dll"), phase, str(cell / "spec.json"), str(cell / f"{phase}.json")], project, env, log, 240, result["commands"])
                require("Exception while deserializing" not in log.read_text() and "Reverting to default state" not in log.read_text(), "State deserialization fallback was logged")
                receipt = json.loads((cell / f"{phase}.json").read_text())
                original = result["phases"].get("suspend", {}).get("state")
                check_phase(receipt, spec, phase, original)
                check_assemblies(receipt, project, cache, tfm)
                if phase != "suspend":
                    previous = result["phases"]["suspend" if phase == "resume" else "resume"]
                    for context in ("management", "runtime"):
                        require(receipt["migrations_before"][context]["applied"] == previous["migrations_after"][context]["applied"], "Original migration history changed before startup")
                        snapshot = cell / f"baseline-{context}.snapshot.db"
                        require(sha256(snapshot) == result["databases"]["suspend"][context]["snapshot_sha256"], "Immutable baseline snapshot was changed")
                result["phases"][phase] = receipt
                databases = {}
                for context in ("management", "runtime"):
                    snapshot = cell / f"baseline-{context}.snapshot.db" if phase == "suspend" else None
                    databases[context] = inspect_database(Path(spec[context]), snapshot)
                    require([row["MigrationId"] for row in databases[context]["history"]] == receipt["migrations_after"][context]["applied"], "SQLite and EF migration histories differ")
                    if snapshot:
                        require(inspect_database(snapshot)["rows"] == databases[context]["rows"], "WAL-consistent snapshot rows differ")
                corroborate(databases, receipt["state"], spec, phase != "suspend")
                result["databases"][phase] = databases
                write_json(cell / "cell.json", result)
        result["passed"] = True
    except Exception as error:
        result["error"] = str(error)
        raise
    finally:
        write_json(cell / "cell.json", result)
    return result


def check_matrix(cells: list[dict]) -> None:
    identities = [(cell["baseline"], cell["framework"]) for cell in cells]
    require(len(identities) == len(MATRIX) and set(identities) == MATRIX and all(cell.get("passed") is True for cell in cells), "All six unique real matrix cells must pass")


def public_receipt(result: dict) -> dict:
    """Allowlist portable proof evidence: no paths, raw rows, properties or errors."""
    projected = {key: result[key] for key in ("passed", "complete_matrix", "published", "proof_run", "proof_artifact", "candidate_source", "candidate_version", "manifest_sha256", "original_receipt_sha256", "fixture_sha256", "archive_sha256")}
    projected["sdk"] = result.get("sdk")
    projected["cells"] = []
    actual = {(cell["baseline"], cell["framework"]): cell for cell in result["cells"]}
    for baseline, tfm in sorted(MATRIX):
        cell = actual.get((baseline, tfm))
        item = {"baseline": baseline, "baseline_source": BASELINES[baseline], "framework": tfm,
                "result": "not_run" if cell is None else ("passed" if cell["passed"] else "failed")}
        if cell:
            item["provenance"] = {}
            for label, evidence in cell.get("provenance", {}).items():
                item["provenance"][label] = {key: evidence[key] for key in ("assets_sha256", "lock_sha256", "isolation", "package_only")}
                item["provenance"][label]["packages"] = [
                    {key: package[key] for key in ("id", "version", "assets_type", "sha256", "sha512", "repository_commit", "frameworks") if key in package}
                    for package in evidence["packages"]]
            item["phases"] = {}
            for phase, receipt in cell.get("phases", {}).items():
                state = receipt["state"]
                item["phases"][phase] = {
                    "passed": receipt["passed"] and phase in cell.get("databases", {}), "runtime": receipt["runtime"], "tenant_id": receipt["tenant_id"],
                    "status": state["status"], "sub_status": state["sub_status"],
                    "state_bookmark_count": state["state_bookmark_count"], "stored_bookmark_count": state["stored_bookmark_count"],
                    "definition_json_sha256": state["definition_json_sha256"],
                    "input_metadata": state["input_metadata"], "output_metadata": state["output_metadata"],
                    "assemblies": [{key: assembly[key] for key in ("name", "identity", "informational_version", "sha256")} for assembly in receipt["assemblies"]],
                    "migrations": {context: {"before": receipt["migrations_before"][context]["applied"], "after": receipt["migrations_after"][context]["applied"], "pending": receipt["migrations_after"][context]["pending"]} for context in ("management", "runtime")},
                    "databases": {context: {key: database[key] for key in ("sha256", "snapshot_sha256", "wal_sha256", "journal_mode", "integrity")} for context, database in cell.get("databases", {}).get(phase, {}).items()},
                }
            if not cell["passed"]:
                item["failure_category"] = "execution_or_validation_failed"
        projected["cells"].append(item)
    if "error" in result:
        projected["failure_category"] = "execution_or_validation_failed"
    return projected


def stage_evidence(output: Path, result: dict) -> None:
    """Retain original synthetic DB bytes plus explicitly projected execution logs."""
    destination = output / "retained-evidence"
    require(not destination.exists(), "Refusing to overwrite retained evidence")
    destination.mkdir()
    shutil.copy2(output / "public-upgrade-proof.json", destination / "public-upgrade-proof.json")
    manifest = {"complete_matrix": result["complete_matrix"], "passed": result["passed"], "files": [], "cells": []}
    for cell in result["cells"]:
        identity = (cell["baseline"], cell["framework"])
        require(identity in MATRIX, "Unknown cell cannot enter retained evidence")
        relative = f"{identity[0]}-{identity[1]}"
        source_dir = output / relative
        require(not source_dir.is_symlink() and source_dir.resolve().is_relative_to(output.resolve()), "Retained cell escaped evidence directory")
        target_dir = destination / relative
        target_dir.mkdir()
        files = [f"{context}.db{suffix}" for context in ("management", "runtime") for suffix in ("", "-wal")]
        files += [f"baseline-{context}.snapshot.db" for context in ("management", "runtime")]
        for name in files:
            source = source_dir / name
            if not source.exists() and not source.is_symlink():
                continue
            require(source.is_file() and not source.is_symlink() and source.resolve().is_relative_to(source_dir.resolve()), "Retained database escaped allowlist")
            digest = sha256(source)
            target = target_dir / name
            shutil.copyfile(source, target)
            require(sha256(target) == digest and sha256(source) == digest, "Synthetic database changed during retention")
            manifest["files"].append({"file": f"{relative}/{name}", "sha256": digest, "bytes": target.stat().st_size})
        diagnostic = {"baseline": identity[0], "framework": identity[1], "passed": cell["passed"], "commands": [], "phases": []}
        allowed_logs = {source_dir / f"{phase}.log" for phase in ("suspend", "resume", "verify")}
        allowed_logs |= {source_dir / label / f"{name}.log" for label in ("baseline", "candidate") for name in ("restore", "build")}
        for command in cell.get("commands", []):
            argv = command["command"]
            kind = argv[1] if argv[1] in ("restore", "build") else argv[2]
            require(kind in ("restore", "build", "suspend", "resume", "verify"), "Unknown command kind")
            log = Path(command["log"])
            # Build/restore records carry cwd paths in their logs. Permit only their exact two consumer locations.
            require(log in allowed_logs and not log.is_symlink() and log.resolve().is_relative_to(source_dir.resolve()), "Command log escaped allowlist")
            entry = {"kind": kind, "phase": kind if kind in ("suspend", "resume", "verify") else None,
                     "exit_code": command["exit_code"], "timed_out": command["timed_out"],
                     "failure_category": "command_failed" if command["exit_code"] != 0 or command["timed_out"] else None}
            if log.is_file():
                text = log.read_text(errors="replace")
                entry.update(raw_log_sha256=sha256(log), raw_log_bytes=log.stat().st_size,
                             diagnostic_codes=sorted(set(re.findall(r"\b(?:CS|MSB|NU|ASP)\d{3,5}\b", text)))[:30],
                             ef_diagnostic_codes=sorted(set(re.findall(r"^(?:warn|fail): Microsoft\.EntityFrameworkCore\.[A-Za-z0-9.]+\[(\d+)\]", text, re.MULTILINE)))[:30],
                             exception_types=sorted(set(re.findall(r"^(System\.[A-Za-z0-9_.]+Exception)(?::|$)", text, re.MULTILINE)))[:10])
            diagnostic["commands"].append(entry)
        for phase in ("suspend", "resume", "verify"):
            path = source_dir / f"{phase}.json"
            if not path.exists():
                continue
            require(not path.is_symlink() and path.resolve().is_relative_to(source_dir.resolve()), "Phase receipt escaped allowlist")
            try:
                receipt = json.loads(path.read_text())
                valid_receipt = isinstance(receipt, dict) and receipt.get("phase") == phase
                if not isinstance(receipt, dict):
                    receipt = {}
            except json.JSONDecodeError:
                receipt, valid_receipt = {}, False
            diagnostic["phases"].append({"phase": phase, "runner_passed": valid_receipt and receipt.get("passed") is True,
                                         "validation_passed": phase in cell.get("databases", {}),
                                         "failure_category": "receipt_validation_failed" if not valid_receipt else
                                             (None if phase in cell.get("databases", {}) else "execution_or_validation_failed")})
        if not cell["passed"]:
            diagnostic["failure_category"] = "execution_or_validation_failed"
        write_json(target_dir / "execution.json", diagnostic)
        manifest["cells"].append({"baseline": identity[0], "framework": identity[1], "passed": cell["passed"], "execution_log": f"{relative}/execution.json"})
    manifest["retention_complete"] = True
    write_json(destination / "retention-manifest.json", manifest)


def run(root: Path, archive: Path, output: Path, selected: list[tuple[str, str]]) -> dict:
    require(not output.exists(), "Refusing to reuse evidence/cache directory")
    require(not output.resolve().is_relative_to(Path(__file__).resolve().parents[2]), "Consumers must be generated outside checkout")
    # Fail before restoring anything if the original package artifact is incomplete or altered.
    manifest, by_id, exceptions, exclusions = verify_artifact(root, archive)
    output.mkdir(parents=True)
    result = {"passed": False, "complete_matrix": False, "published": False, "proof_run": PROOF_RUN, "proof_artifact": PROOF_ARTIFACT,
              "candidate_source": PROOF_SOURCE, "candidate_version": PROOF_VERSION, "manifest_sha256": sha256(root / "verified-artifacts.json"),
              "original_receipt_sha256": sha256(root / "receipt.json"), "fixture_sha256": sha256(FIXTURE), "cells": []}
    result["archive_sha256"] = sha256(archive)
    try:
        packages._run_command(["dotnet", "--info"], output, dict(os.environ), output / "sdk-info.log", 60)
        packages._run_command(["dotnet", "--list-sdks"], output, dict(os.environ), output / "sdk-list.log", 60)
        sdks = [line.split()[0] for line in (output / "sdk-list.log").read_text().splitlines() if line.startswith("10.") and "-" not in line.split()[0]]
        require(bool(sdks), "A .NET 10 SDK is required to compile all three framework consumers")
        sdk = sorted(sdks, key=lambda value: tuple(int(part) for part in value.split(".")))[-1]
        result["sdk"] = sdk
        for baseline, tfm in selected:
            print(f"Starting {baseline}/{tfm}", flush=True)
            cell_path = output / f"{baseline}-{tfm}"
            try:
                run_cell(baseline, tfm, cell_path, root, by_id, exceptions, exclusions, sdk)
            finally:
                if (cell_path / "cell.json").is_file():
                    result["cells"].append(json.loads((cell_path / "cell.json").read_text()))
        if set(selected) == MATRIX:
            check_matrix(result["cells"])
            result["complete_matrix"] = True
            result["passed"] = True
        else:
            result["development_cell_passed"] = True
    except Exception as error:
        result["error"] = str(error)
        raise
    finally:
        write_json(output / "upgrade-proof.json", result)
        write_json(output / "public-upgrade-proof.json", public_receipt(result))
        stage_evidence(output, result)
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--proof-artifacts", type=Path, required=True)
    parser.add_argument("--proof-archive", type=Path, required=True)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--extract-only", action="store_true")
    parser.add_argument("--artifact-metadata", type=Path)
    parser.add_argument("--cell", choices=[f"{baseline}/{tfm}" for baseline, tfm in sorted(MATRIX)], help="Development smoke only; cannot satisfy full matrix")
    args = parser.parse_args()
    if args.artifact_metadata:
        validate_artifact_metadata(json.loads(args.artifact_metadata.read_text()))
    if args.extract_only:
        extract_archive(args.proof_archive.resolve(strict=True), args.proof_artifacts.resolve())
        return
    if not args.output:
        parser.error("--output is required unless --extract-only is specified")
    selected = [tuple(args.cell.split("/"))] if args.cell else sorted(MATRIX)
    run(args.proof_artifacts.resolve(strict=True), args.proof_archive.resolve(strict=True), args.output.resolve(), selected)


if __name__ == "__main__":
    main()
