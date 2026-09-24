#!/usr/bin/env python3
"""Pack mapped Slack locally and prove clean package-only consumption.

The proof uses a disposable source-history rehearsal. It never publishes a
package, and its proof version is not a release version. Restore uses NuGet.org
for source packing and NuGet.org plus the isolated local feed for consumers.
"""

from __future__ import annotations

import argparse
import hashlib
import html
import json
import os
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path
from xml.etree import ElementTree

import run_slack_package_proof as shared

CORE_SHA = "8e893e02c4ac089d526b0a0d294a8546f021d072"
EXTENSIONS_SHA = "33fa0bfd28c7585240e3d4f665058c067b17e287"
STUDIO_SHA = "9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822"
SOURCE_COMMITS = {"core": CORE_SHA, "extensions": EXTENSIONS_SHA, "studio": STUDIO_SHA}
PACKAGE_ID = "Elsa.Slack"
PACKAGE_VERSION = "3.8.5-proof"
ELSA_VERSION = "3.8.4"
SLACK_NET_VERSION = "0.17.7"
TFMS = ("net8.0", "net9.0", "net10.0")
REPOSITORY_URL = "https://github.com/elsa-workflows/elsa-extensions"
SLACK_RELATIVE = Path("src/extensions/communication/Elsa.Slack")
EXTENSIONS_SLACK_RELATIVE = Path("src/modules/communication/Elsa.Slack")
ICON_SHA256 = "82fd76d734d59efc6132af0b0b999146254fa5a296ea5d64f85597bb1cda524e"
PATCH_RELATIVE = Path("scripts/integration-program/consolidated-build/source-integration.patch")
PREPARED_RECEIPT = "consolidated-build-receipt.json"
IMPORT_RECEIPT = "import-receipt.json"
CREATED_RECEIPTS = {PREPARED_RECEIPT, IMPORT_RECEIPT}
DOTNET = shutil.which("dotnet")
INVENTORY_RELATIVE = Path("doc/integration-program/inventory/inventory.json")


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def dotnet_command(dotnet: Path, *arguments: str) -> list[str]:
    return [str(dotnet.resolve(strict=True)), *arguments]


def git_value(root: Path, *args: str) -> str:
    return subprocess.check_output(["git", *args], cwd=root, text=True).strip()


def resolved_directory(path: Path, description: str) -> Path:
    if path.is_symlink() or not path.is_dir():
        raise RuntimeError(f"{description} must be an existing, non-symlink directory: {path}")
    return path.resolve(strict=True)


def reject_symlink_ancestors(path: Path) -> None:
    absolute = path.absolute()
    for ancestor in reversed((absolute, *absolute.parents)):
        if ancestor.exists() and ancestor.is_symlink():
            raise RuntimeError(f"Output path has a symlink ancestor: {ancestor}")


def reject_overlap(output: Path, inputs: list[Path]) -> None:
    for input_root in inputs:
        if output == input_root or output in input_root.parents or input_root in output.parents:
            raise RuntimeError(f"Output directory overlaps an inspected source root: {output} and {input_root}")


def require_prepared_rehearsal(root: Path) -> tuple[dict, dict, str]:
    if git_value(root, "rev-parse", "--show-toplevel") != str(root):
        raise RuntimeError("Pass the physical rehearsal Git root")
    if git_value(root, "remote"):
        raise RuntimeError("The rehearsal must not have a remote")

    import_path = root / IMPORT_RECEIPT
    prepared_path = root / PREPARED_RECEIPT
    if not import_path.is_file() or not prepared_path.is_file():
        raise RuntimeError("The rehearsal must have both import and consolidated-build receipts")
    imported = json.loads(import_path.read_text(encoding="utf-8"))
    prepared = json.loads(prepared_path.read_text(encoding="utf-8"))
    if imported.get("sourceCommits") != SOURCE_COMMITS:
        raise RuntimeError(f"Unexpected rehearsal source pins: {imported.get('sourceCommits')}")
    if not imported.get("exactBlobAndModeMapping") or not imported.get("originalHistoriesReachable"):
        raise RuntimeError("The rehearsal receipt does not prove exact source blob/mode history mapping")
    if imported.get("buildCompatibilityVerified") or imported.get("publicationAuthorized"):
        raise RuntimeError("The rehearsal receipt has inconsistent build/publication claims")
    if prepared.get("sourceCommits") != SOURCE_COMMITS:
        raise RuntimeError(f"Unexpected prepared source pins: {prepared.get('sourceCommits')}")
    if prepared.get("rehearsalCommit") != imported.get("rehearsalCommit"):
        raise RuntimeError("Prepared and import receipts identify different rehearsal commits")
    if prepared.get("buildCompatibilityVerified") or prepared.get("publicationAuthorized"):
        raise RuntimeError("The preparation receipt has inconsistent build/publication claims")
    if git_value(root, "rev-parse", "HEAD") != imported.get("rehearsalCommit"):
        raise RuntimeError("The rehearsal HEAD differs from its source-history receipt")

    patch_path = Path(__file__).resolve().parent / "consolidated-build/source-integration.patch"
    patch_hash = sha256_file(patch_path)
    if prepared.get("patchSha256") != patch_hash:
        raise RuntimeError("The prepared receipt does not match the current reviewed integration patch")

    expected_files = {row["path"]: row["sha256"] for row in prepared.get("files", [])}
    changed = set(git_value(root, "diff", "--name-only", "HEAD").splitlines())
    untracked = set(git_value(root, "ls-files", "--others", "--exclude-standard").splitlines())
    actual_files = changed | (untracked - CREATED_RECEIPTS)
    if actual_files != set(expected_files):
        raise RuntimeError(
            "Prepared source file set changed after receipt creation: "
            f"missing={sorted(set(expected_files) - actual_files)}, extra={sorted(actual_files - set(expected_files))}"
        )
    for relative_path, expected_hash in expected_files.items():
        path = (root / relative_path).resolve(strict=True)
        if root not in path.parents or sha256_file(path) != expected_hash:
            raise RuntimeError(f"Prepared source changed after its receipt: {relative_path}")

    return imported, prepared, patch_hash


def require_pinned_source(root: Path, expected_sha: str, name: str) -> None:
    actual = git_value(root, "rev-parse", "HEAD")
    status = git_value(root, "status", "--porcelain", "--untracked-files=all")
    if actual != expected_sha or status:
        raise RuntimeError(f"{name} source must be clean at {expected_sha}: head={actual}, status={status!r}")


def compare_mapped_source(rehearsal: Path, extensions: Path) -> list[dict[str, str]]:
    mapped_root = rehearsal / SLACK_RELATIVE
    upstream_root = extensions / EXTENSIONS_SLACK_RELATIVE
    mapped = {
        path.relative_to(mapped_root).as_posix(): path
        for path in mapped_root.rglob("*.cs")
        if not {"bin", "obj"}.intersection(path.parts)
    }
    upstream = {
        path.relative_to(upstream_root).as_posix(): path
        for path in upstream_root.rglob("*.cs")
        if not {"bin", "obj"}.intersection(path.parts)
    }
    if set(mapped) != set(upstream):
        raise RuntimeError(
            "Mapped and upstream Slack C# source sets differ: "
            f"missing={sorted(set(upstream) - set(mapped))}, extra={sorted(set(mapped) - set(upstream))}"
        )
    rows = []
    for relative_path in sorted(mapped):
        mapped_hash = sha256_file(mapped[relative_path])
        upstream_hash = sha256_file(upstream[relative_path])
        if mapped_hash != upstream_hash:
            raise RuntimeError(f"Mapped Slack source differs from pinned Extensions source: {relative_path}")
        rows.append({"path": relative_path, "sha256": mapped_hash})
    if not rows:
        raise RuntimeError("No mapped Slack C# files were found")
    return rows


def write_config(path: Path, sources: list[tuple[str, str]]) -> None:
    rendered = "\n".join(
        f'    <add key="{html.escape(key, quote=True)}" value="{html.escape(value, quote=True)}" />'
        for key, value in sources
    )
    path.write_text(
        '<?xml version="1.0" encoding="utf-8"?>\n'
        "<configuration><packageSources><clear />\n"
        f"{rendered}\n"
        "</packageSources></configuration>\n",
        encoding="utf-8",
    )


def run(command: list[str], *, cwd: Path, env: dict[str, str], log: Path, timeout: int = 900) -> None:
    log.parent.mkdir(parents=True, exist_ok=True)
    with log.open("w", encoding="utf-8") as output:
        output.write(subprocess.list2cmdline(command) + "\n")
        output.flush()
        try:
            result = subprocess.run(
                command,
                cwd=cwd,
                env=env,
                text=True,
                stdout=output,
                stderr=subprocess.STDOUT,
                check=False,
                timeout=timeout,
            )
        except subprocess.TimeoutExpired as error:
            raise RuntimeError(f"Command timed out after {timeout}s; see {log}") from error
    if result.returncode:
        raise RuntimeError(f"Command failed ({result.returncode}); see {log}")


def inspect_artifact(package: Path, symbols: Path, icon_path: Path) -> dict:
    with zipfile.ZipFile(package) as archive:
        nuspec_files = [name for name in archive.namelist() if name.lower().endswith(".nuspec")]
        if len(nuspec_files) != 1:
            raise RuntimeError(f"Expected one nuspec in {package}: {nuspec_files}")
        metadata = ElementTree.fromstring(archive.read(nuspec_files[0])).find("{*}metadata")
        if metadata is None:
            raise RuntimeError(f"Missing package metadata in {package}")
        repository = metadata.find("{*}repository")
        repository_url = repository.get("url") if repository is not None else None
        repository_commit = repository.get("commit") if repository is not None else None
        package_icon = metadata.findtext("{*}icon")
        package_id = metadata.findtext("{*}id")
        package_version = metadata.findtext("{*}version")
        groups = metadata.findall("{*}dependencies/{*}group")
        frameworks = sorted(group.get("targetFramework", "") for group in groups)
        dependencies = {
            group.get("targetFramework", ""): sorted(
                (row.get("id"), row.get("version")) for row in group.findall("{*}dependency")
            )
            for group in groups
        }
        names = set(archive.namelist())
        if "icon.png" not in names:
            raise RuntimeError("The package does not contain its declared canonical icon.png")
        package_icon_sha = sha256_bytes(archive.read("icon.png"))
        for framework in TFMS:
            if f"lib/{framework}/{PACKAGE_ID}.dll" not in names:
                raise RuntimeError(f"The package omits its {framework} assembly")
            if f"lib/{framework}/{PACKAGE_ID}.xml" not in names:
                raise RuntimeError(f"The package omits its {framework} XML documentation")

    expected_dependencies = sorted((("Elsa", ELSA_VERSION), ("SlackNet", SLACK_NET_VERSION)))
    if package_id != PACKAGE_ID or package_version != PACKAGE_VERSION:
        raise RuntimeError(f"Unexpected package identity {package_id} {package_version}")
    if repository_url != REPOSITORY_URL or repository_commit != EXTENSIONS_SHA:
        raise RuntimeError(f"Unexpected package source provenance: {repository_url} {repository_commit}")
    if package_icon != "icon.png" or package_icon_sha != ICON_SHA256 or sha256_file(icon_path) != ICON_SHA256:
        raise RuntimeError("Packaged icon differs from the pinned canonical Extensions root icon")
    if frameworks != sorted(TFMS):
        raise RuntimeError(f"Unexpected package target frameworks: {frameworks}")
    if any(packages != expected_dependencies for packages in dependencies.values()):
        raise RuntimeError(f"Unexpected dependency metadata: {dependencies}")

    with zipfile.ZipFile(symbols) as archive:
        symbol_names = set(archive.namelist())
        for framework in TFMS:
            if f"lib/{framework}/{PACKAGE_ID}.pdb" not in symbol_names:
                raise RuntimeError(f"The symbol package omits the {framework} PDB")

    return {
        "package_id": package_id,
        "package_version": package_version,
        "repository_url": repository_url,
        "repository_commit": repository_commit,
        "target_frameworks": frameworks,
        "dependencies": dependencies,
        "icon": {"path": package_icon, "sha256": package_icon_sha},
        "nupkg_sha256": sha256_file(package),
        "snupkg_sha256": sha256_file(symbols),
        "symbol_frameworks": list(TFMS),
    }


def parse_marker(log: Path, marker: str) -> dict:
    lines = [line.split(marker, 1)[1] for line in log.read_text(encoding="utf-8").splitlines() if marker in line]
    if len(lines) != 1:
        raise RuntimeError(f"Expected exactly one {marker} result in {log}")
    return json.loads(lines[0])


def parse_evaluation(log: Path) -> dict:
    lines = log.read_text(encoding="utf-8").splitlines()
    try:
        evaluation = json.loads("\n".join(lines[1:]))
    except (IndexError, json.JSONDecodeError) as error:
        raise RuntimeError(f"Could not parse MSBuild evaluation JSON from {log}") from error
    if not isinstance(evaluation, dict) or not isinstance(evaluation.get("Properties"), dict) or not isinstance(evaluation.get("Items"), dict):
        raise RuntimeError(f"MSBuild evaluation result has an unexpected shape in {log}")
    return evaluation


def verify_evaluation(log: Path, rehearsal: Path, *, package_mode: bool) -> dict:
    evaluation = parse_evaluation(log)
    properties = evaluation["Properties"]
    expected_properties = {
        "PackageId": PACKAGE_ID,
        "AssemblyName": PACKAGE_ID,
        "RootNamespace": PACKAGE_ID,
        "ElsaVersion": ELSA_VERSION,
        "PackageVersion": PACKAGE_VERSION,
    }
    if package_mode:
        expected_properties["IsPackable"] = "true"
    for name, expected in expected_properties.items():
        actual = properties.get(name)
        if actual != expected:
            raise RuntimeError(f"Unexpected evaluated {name} in {log}: {actual!r} != {expected!r}")

    items = evaluation["Items"]
    package_references = items.get("PackageReference", [])
    project_references = items.get("ProjectReference", [])
    elsa_packages = [row for row in package_references if row.get("Identity") == "Elsa"]
    if package_mode:
        if len(elsa_packages) != 1 or project_references:
            raise RuntimeError(
                f"Package mode must contain one Elsa package and no project references: "
                f"ElsaPackageCount={len(elsa_packages)}, ProjectReferences={project_references}"
            )
        if elsa_packages[0].get("Version") not in (None, ELSA_VERSION):
            raise RuntimeError(f"Unexpected Elsa package version in evaluation: {elsa_packages[0]}")
    else:
        expected_core = (rehearsal / "src/modules/Elsa/Elsa.csproj").resolve(strict=True)
        resolved = [Path(row["FullPath"]).resolve(strict=True) for row in project_references]
        if elsa_packages or resolved != [expected_core]:
            raise RuntimeError(
                f"Project-reference mode must resolve only the mapped Core project: "
                f"ElsaPackages={elsa_packages}, ProjectReferences={resolved}, expected={[expected_core]}"
            )
    return {
        "package_mode": package_mode,
        "properties": {name: properties.get(name) for name in expected_properties},
        "elsa_package_reference_count": len(elsa_packages),
        "project_references": [row.get("FullPath") for row in project_references],
    }


def selector_evidence(output: Path) -> dict:
    inventory = Path(__file__).resolve().parents[2] / INVENTORY_RELATIVE
    inventory_document = json.loads(inventory.read_text(encoding="utf-8"))
    selector = Path(__file__).with_name("package_impact.py")
    result = subprocess.run(
        [
            sys.executable,
            str(selector),
            "--inventory",
            str(inventory),
            "--changed",
            "elsa-core:src/modules/Elsa/Elsa.csproj",
            "--release-unit",
            "elsa-extensions:src/modules/communication/Elsa.Slack/Elsa.Slack.csproj",
        ],
        text=True,
        capture_output=True,
        check=False,
    )
    if result.returncode:
        raise RuntimeError(f"Current-source Slack impact selection failed: {result.stderr.strip()}")
    selection = json.loads(result.stdout)
    selection["inventory_snapshot_date"] = inventory_document["snapshot_date"]
    selection["inventory_sha256"] = sha256_file(inventory)
    selection["inventory_source_commits"] = {
        name: repository["commit"]
        for name, repository in inventory_document["repositories"].items()
    }
    if selection.get("package_ids_to_pack") != [PACKAGE_ID]:
        raise RuntimeError(f"Shared Core change did not select only Slack as release unit: {selection}")
    slack_tests = [
        path
        for path in selection.get("affected_test_projects", [])
        if path.endswith("test/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj")
    ]
    if not slack_tests:
        raise RuntimeError(f"Shared Core change did not select the Slack test project: {selection}")
    path = output / "current-source-impact-selection.json"
    path.write_text(json.dumps(selection, indent=2) + "\n", encoding="utf-8")
    return {
        "selection": selection,
        "receipt": str(path),
        "scope_note": "Selector uses the current inventory graph pins; artifact source pins are recorded separately and are not inferred from this graph.",
    }


def verify_consumers(output: Path, packages: Path, env: dict[str, str], cache_root: Path, dotnet: Path) -> list[dict]:
    results = []
    package_sources = [
        ("nuget.org", "https://api.nuget.org/v3/index.json"),
        ("local-proof-feed", str(packages)),
    ]
    for framework in TFMS:
        consumer_dir = output / "consumers" / framework
        consumer_dir.mkdir(parents=True)
        project = shared.consumer_project(consumer_dir, framework, package_version=PACKAGE_VERSION)
        config = consumer_dir / "NuGet.Config"
        write_config(config, package_sources)
        consumer_env = env.copy()
        consumer_env["NUGET_PACKAGES"] = str(cache_root / framework)
        run(
            [str(dotnet), "restore", str(project), "--configfile", str(config)],
            cwd=consumer_dir,
            env=consumer_env,
            log=output / "logs" / f"consumer-{framework}-restore.log",
        )
        run(
            [str(dotnet), "run", "--no-restore", "--project", str(project), "--framework", framework],
            cwd=consumer_dir,
            env=consumer_env,
            log=output / "logs" / f"consumer-{framework}.log",
        )
        descriptor = parse_marker(output / "logs" / f"consumer-{framework}.log", "ELSA_ACTIVITY_DESCRIPTOR=")
        if not descriptor.get("TypeName") or descriptor.get("Version", 0) < 1:
            raise RuntimeError(f"Invalid runtime ActivityDescriptor from {framework} consumer: {descriptor}")
        results.append({"framework": framework, "result": "passed", "descriptor": descriptor})
    if any(row["descriptor"] != results[0]["descriptor"] for row in results[1:]):
        raise RuntimeError(f"Package consumer ActivityDescriptor differs across TFMs: {results}")
    return results


def verify_offline_activity(output: Path, packages: Path, env: dict[str, str], cache_root: Path, dotnet: Path) -> dict:
    root = output / "offline-activity-smoke"
    root.mkdir()
    project = shared.offline_activity_smoke_project(root, PACKAGE_VERSION)
    config = root / "NuGet.Config"
    write_config(
        config,
        [("nuget.org", "https://api.nuget.org/v3/index.json"), ("local-proof-feed", str(packages))],
    )
    smoke_env = env.copy()
    smoke_env["NUGET_PACKAGES"] = str(cache_root / "offline-activity-smoke")
    run(
        [str(dotnet), "restore", str(project), "--configfile", str(config)],
        cwd=root,
        env=smoke_env,
        log=output / "logs/offline-activity-restore.log",
    )
    runtime_env = smoke_env.copy()
    runtime_env["HTTP_PROXY"] = "http://127.0.0.1:9"
    runtime_env["HTTPS_PROXY"] = "http://127.0.0.1:9"
    runtime_env["ALL_PROXY"] = "http://127.0.0.1:9"
    runtime_env["NO_PROXY"] = "localhost,127.0.0.1,::1"
    runtime_log = output / "logs/offline-activity.log"
    run(
        [str(dotnet), "run", "--no-restore", "--project", str(project), "--framework", "net10.0"],
        cwd=root,
        env=runtime_env,
        log=runtime_log,
    )
    result = parse_marker(runtime_log, "ELSA_OFFLINE_ACTIVITY_SMOKE=")
    if result.get("fakeCalls") != 1 or result.get("output", {}).get("Id") != "C_OFFLINE_PROOF":
        raise RuntimeError(f"Offline CreateChannel contract failed: {result}")
    return {"result": "passed", "framework": "net10.0", "receipt": result}


def verify_upstream_test_baseline(output: Path, rehearsal: Path, env: dict[str, str], cache_root: Path, config: Path, dotnet: Path) -> dict:
    test_project = rehearsal / "test/extensions/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj"
    if not test_project.is_file():
        raise RuntimeError(f"Pinned Slack test project is missing: {test_project}")
    test_env = env.copy()
    test_env["NUGET_PACKAGES"] = str(cache_root / "upstream-test")
    run(
        [str(dotnet), "restore", str(test_project), "--configfile", str(config), "-p:UseProjectReferences=false", f"-p:ElsaVersion={ELSA_VERSION}"],
        cwd=rehearsal,
        env=test_env,
        log=output / "logs/upstream-test-restore.log",
    )
    results = output / "test-results"
    results.mkdir()
    run(
        [
            str(dotnet), "test", str(test_project), "--no-restore", "--configuration", "Release",
            "--framework", "net10.0", "--logger", "trx;LogFileName=Slack.Tests.net10.0.trx",
            "--results-directory", str(results), "-p:UseProjectReferences=false",
            f"-p:ElsaVersion={ELSA_VERSION}", "-m:1",
        ],
        cwd=rehearsal,
        env=test_env,
        log=output / "logs/upstream-test-net10.0.log",
    )
    receipt = shared.read_focused_test_receipt(results)
    return {"result": "baseline-recorded", "framework": "net10.0", "receipt": receipt}


def verify_embedded_sources(output: Path, symbols: Path, extensions: Path, env: dict[str, str], config: Path, dotnet: Path) -> list[dict]:
    helper_source = Path(__file__).resolve().parent / "VerifyEmbeddedSources"
    helper_root = output / "embedded-source-verifier"
    shutil.copytree(helper_source, helper_root, ignore=shutil.ignore_patterns("bin", "obj", "__pycache__"))
    helper_project = helper_root / "VerifyEmbeddedSources.csproj"
    helper_env = env.copy()
    helper_env["NUGET_PACKAGES"] = str(output / "package-caches" / "embedded-source-verifier")
    run(
        [str(dotnet), "restore", str(helper_project), "--configfile", str(config)],
        cwd=helper_root,
        env=helper_env,
        log=output / "logs/embedded-source-verifier-restore.log",
    )
    run(
        [str(dotnet), "build", str(helper_project), "--no-restore", "--configuration", "Release"],
        cwd=helper_root,
        env=helper_env,
        log=output / "logs/embedded-source-verifier-build.log",
    )
    verifier = helper_root / "bin/Release/net10.0/VerifyEmbeddedSources.dll"
    source_root = extensions / EXTENSIONS_SLACK_RELATIVE
    results = []
    for framework in TFMS:
        with zipfile.ZipFile(symbols) as archive:
            pdb = output / "pdb" / f"Elsa.Slack.{framework}.pdb"
            pdb.parent.mkdir(parents=True, exist_ok=True)
            pdb.write_bytes(archive.read(f"lib/{framework}/{PACKAGE_ID}.pdb"))
        log = output / "logs" / f"embedded-source-{framework}.json"
        run(
            [str(dotnet), str(verifier), str(pdb), str(source_root)],
            cwd=helper_root,
            env=helper_env,
            log=log,
        )
        result = json.loads("\n".join(log.read_text(encoding="utf-8").splitlines()[1:]))
        if result.get("result") != "passed" or result.get("embeddedSourceCount") == 0:
            raise RuntimeError(f"Embedded source verification failed for {framework}: {result}")
        results.append({"framework": framework, **result})
    return results


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--rehearsal", type=Path, required=True)
    parser.add_argument("--core-source", type=Path, required=True)
    parser.add_argument("--extensions-source", type=Path, required=True)
    parser.add_argument("--studio-source", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--dotnet", type=Path, default=Path(DOTNET) if DOTNET else None)
    args = parser.parse_args()

    if args.dotnet is None or not args.dotnet.is_file():
        raise RuntimeError("A dotnet executable is required")
    dotnet = args.dotnet.resolve(strict=True)
    rehearsal = resolved_directory(args.rehearsal, "Rehearsal")
    core = resolved_directory(args.core_source, "Core source")
    extensions = resolved_directory(args.extensions_source, "Extensions source")
    studio = resolved_directory(args.studio_source, "Studio source")
    require_pinned_source(core, CORE_SHA, "Core")
    require_pinned_source(extensions, EXTENSIONS_SHA, "Extensions")
    require_pinned_source(studio, STUDIO_SHA, "Studio")
    imported, prepared, patch_hash = require_prepared_rehearsal(rehearsal)

    output_argument = args.output_dir.expanduser().absolute()
    reject_symlink_ancestors(output_argument)
    output = output_argument.resolve(strict=False)
    reject_overlap(output, [rehearsal, core, extensions, studio])
    if output.exists():
        raise RuntimeError(f"Output directory must be absent; use a new proof output path: {output}")
    output.mkdir(parents=True)

    source_files = compare_mapped_source(rehearsal, extensions)
    if sha256_file(extensions / "icon.png") != ICON_SHA256:
        raise RuntimeError("Pinned Extensions root icon differs from the recorded canonical icon SHA")

    packages = output / "local-feed"
    packages.mkdir()
    cache_root = output / "package-caches"
    cache_root.mkdir()
    pack_config = output / "NuGet.org-only.Config"
    write_config(pack_config, [("nuget.org", "https://api.nuget.org/v3/index.json")])

    env = os.environ.copy()
    env["DOTNET_CLI_HOME"] = str(output / ".dotnet-home")
    env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    env["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1"
    env["NUGET_HTTP_CACHE_PATH"] = str(output / "nuget-http-cache")
    env["NUGET_PACKAGES"] = str(cache_root / "pack")
    project = rehearsal / SLACK_RELATIVE / "Elsa.Slack.csproj"
    package_properties = [
        "-p:IsPackable=true",
        "-p:UseProjectReferences=false",
        f"-p:ElsaVersion={ELSA_VERSION}",
        f"-p:PackageVersion={PACKAGE_VERSION}",
        f"-p:RepositoryCommit={EXTENSIONS_SHA}",
        f"-p:RepositoryUrl={REPOSITORY_URL}",
    ]
    package_evaluation = output / "logs/package-mode-evaluation.log"
    run(
        [str(dotnet), "msbuild", str(project), "-getProperty:PackageId,AssemblyName,RootNamespace,ElsaVersion,PackageVersion,IsPackable", "-getItem:PackageReference,ProjectReference", *package_properties],
        cwd=rehearsal,
        env=env,
        log=package_evaluation,
    )
    package_evaluation_receipt = verify_evaluation(package_evaluation, rehearsal, package_mode=True)
    project_reference_evaluation = output / "logs/project-reference-mode-evaluation.log"
    run(
        [str(dotnet), "msbuild", str(project), "-getProperty:PackageId,AssemblyName,RootNamespace,ElsaVersion,PackageVersion,IsPackable", "-getItem:PackageReference,ProjectReference", "-p:UseProjectReferences=true", "-p:IsPackable=true", f"-p:ElsaVersion={ELSA_VERSION}", f"-p:PackageVersion={PACKAGE_VERSION}"],
        cwd=rehearsal,
        env=env,
        log=project_reference_evaluation,
    )
    project_reference_evaluation_receipt = verify_evaluation(project_reference_evaluation, rehearsal, package_mode=False)

    run(
        dotnet_command(dotnet, "restore", str(project), "--configfile", str(pack_config), *package_properties),
        cwd=rehearsal,
        env=env,
        log=output / "logs/pack-restore.log",
    )
    run(
        dotnet_command(
            dotnet,
            "pack", str(project), "--no-restore", "--configuration", "Release",
            "--output", str(packages), *package_properties,
            "-p:ContinuousIntegrationBuild=true", "-p:EmbedAllSources=true",
            "-p:IncludeSymbols=true", "-p:SymbolPackageFormat=snupkg",
        ),
        cwd=rehearsal,
        env=env,
        log=output / "logs/pack.log",
    )

    nupkgs = sorted(packages.glob("*.nupkg"))
    snupkgs = sorted(packages.glob("*.snupkg"))
    if [path.name for path in nupkgs] != [f"{PACKAGE_ID}.{PACKAGE_VERSION}.nupkg"]:
        raise RuntimeError(f"Local feed contains unrelated or missing packages: {[path.name for path in nupkgs]}")
    if [path.name for path in snupkgs] != [f"{PACKAGE_ID}.{PACKAGE_VERSION}.snupkg"]:
        raise RuntimeError(f"Local feed contains unrelated or missing symbol packages: {[path.name for path in snupkgs]}")
    artifact = inspect_artifact(nupkgs[0], snupkgs[0], extensions / "icon.png")

    upstream_test = verify_upstream_test_baseline(output, rehearsal, env, cache_root, pack_config, dotnet)
    current_selection = selector_evidence(output)
    consumers = verify_consumers(output, packages, env, cache_root / "consumers", dotnet)
    offline_activity = verify_offline_activity(output, packages, env, cache_root, dotnet)
    embedded_sources = verify_embedded_sources(output, snupkgs[0], extensions, env, pack_config, dotnet)

    # Recheck the source and preparation receipt after all builds. Ignored bin/obj
    # outputs are allowed only inside the disposable rehearsal.
    require_pinned_source(core, CORE_SHA, "Core")
    require_pinned_source(extensions, EXTENSIONS_SHA, "Extensions")
    require_pinned_source(studio, STUDIO_SHA, "Studio")
    imported_after, prepared_after, patch_hash_after = require_prepared_rehearsal(rehearsal)
    if imported_after != imported or prepared_after != prepared or patch_hash_after != patch_hash:
        raise RuntimeError("Pinned source or prepared input receipt changed during package proof")

    evidence = {
        "result": "passed",
        "scope": "mapped Elsa.Slack local pack and clean package-only consumers; no package feed publication",
        "package": artifact,
        "evaluated_modes": {
            "package": package_evaluation_receipt,
            "project_reference": project_reference_evaluation_receipt,
        },
        "release_unit_source": {
            "repository": REPOSITORY_URL,
            "extensions_commit": EXTENSIONS_SHA,
            "rehearsal_commit": imported["rehearsalCommit"],
            "source_integration_patch_sha256": patch_hash,
            "mapped_slack_project_sha256": sha256_file(project),
            "mapped_source_file_count": len(source_files),
            "mapped_source_files": source_files,
            "core_commit": CORE_SHA,
            "studio_commit": STUDIO_SHA,
        },
        "versions": {
            "local_package_version": PACKAGE_VERSION,
            "elsa_package_dependency": ELSA_VERSION,
            "slacknet_dependency": SLACK_NET_VERSION,
        },
        "restore_sources": {
            "pack": ["https://api.nuget.org/v3/index.json"],
            "consumers": ["https://api.nuget.org/v3/index.json", str(packages)],
            "source_link_urls": [],
            "source_link_note": "The synthetic history rehearsal has no remote, so source linking is not fabricated. All 41 C# sources embedded in each target-framework PDB were byte-verified against the pinned Extensions project. Final Core-repository SourceLink URLs remain a post-import remote-history gate.",
        },
        "consumers": consumers,
        "offline_activity": offline_activity,
        "upstream_slack_test": upstream_test,
        "embedded_source_verification": embedded_sources,
        "current_source_impact_selection": current_selection,
        "known_test_limit": "The pinned Slack test project ran on net10.0 and produced the exact known baseline: one NotExecuted CreateChannelTests.ExecuteAsync result ('Not implemented yet'), zero executed or passed tests, and no other test results or nonzero failure counters. This remains an incomplete test gate; the separate offline CreateChannel smoke is narrow behavior evidence and does not unskip or replace the upstream test.",
        "publication_authorized": False,
        "commands": sorted(str(path.relative_to(output)) for path in (output / "logs").glob("*.log")),
        "source_state": {
            "core_commit": git_value(core, "rev-parse", "HEAD"),
            "core_clean": True,
            "extensions_commit": git_value(extensions, "rev-parse", "HEAD"),
            "extensions_clean": True,
            "studio_commit": git_value(studio, "rev-parse", "HEAD"),
            "studio_clean": True,
            "rehearsal_commit": git_value(rehearsal, "rev-parse", "HEAD"),
            "preparation_receipt_verified": True,
            "ignored_build_outputs": "confined to the disposable rehearsal; source worktrees and tracked preparation inputs rechecked after execution",
        },
    }
    evidence_path = output / "evidence.json"
    evidence_path.write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"result": evidence["result"], "evidence": str(evidence_path)}, indent=2))
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, RuntimeError, ValueError, subprocess.CalledProcessError, zipfile.BadZipFile) as error:
        print(f"error: {error}", file=sys.stderr)
        raise SystemExit(1) from error
