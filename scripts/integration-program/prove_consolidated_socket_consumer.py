#!/usr/bin/env python3
"""Run the first package-only Socket vertical (classic/net10), never full acceptance.

The caller supplies already verified original candidate archives. All raw logs,
credentials, caches and machine paths stay in an owned private temporary tree.
"""
from __future__ import annotations

import hashlib
import json
import os
from pathlib import Path
import re
import secrets
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

import prove_consolidated_admission_consumers as lifecycle
import prove_consolidated_package_consumers as consumers
from run_admission_proof import read_json, regular
import verify_socket_package_consumer as reports

ROOT = Path(__file__).resolve().parents[2]
FIXTURE = Path(__file__).with_name("socket-package-consumer")
TFM, FEATURE = "net10.0", "classic"
CONTRACT = read_json(FIXTURE / "report-contract.json")
REQUIRED_PACKAGES = tuple(CONTRACT["requiredLoadedAssemblies"])
SECRET_MARKERS = tuple(CONTRACT["syntheticSecretMarkers"])
ProofError = lifecycle.ProofError
require = lifecycle.require
sha256 = lifecycle.sha256
CATEGORIES = frozenset({
    "linux_required", "source_head", "source_dirty", "manifest_sdk", "artifacts_directory",
    "retained_output_exists", "private_output_boundary", "archive_name", "archive_manifest_hash",
    "archive_bytes", "archive_inventory", "container_identity", "container_image", "container_loopback",
    "postgres_start_timeout", "service_command", "container_cleanup", "service_cleanup", "postgres_version",
    "fixture_project", "isolation_inputs", "fresh_cache", "restore_command", "build_command",
    "required_restored_packages", "assets_changed_during_build", "database_identity", "runtime_exit",
    "runtime_child_leak", "runtime_timeout", "runtime_process", "runtime_cleanup", "process_start_identity",
    "runtime_evidence_inventory", "cache_changed_during_execution", "consumer_outputs_changed",
    "consumer_inputs_changed", "cell_validation", "private_log_secret_marker", "report_secret_marker",
    "source_changed", "archives_changed", "loaded_logical_path", "retained_secret_marker",
    "retained_machine_path", "private_cleanup", "receipt_exists", "receipt_write", "preflight",
})


def _diagnostic_source() -> tuple[set[str], set[str]]:
    names = {path.name for path in FIXTURE.iterdir() if path.suffix in (".cs", ".csproj")}
    codes = set()
    for path in FIXTURE.glob("*.cs"):
        codes.update(re.findall(r'"([a-z][a-z0-9-]{0,95})"', regular(path).read_text()))
    codes.difference_update(SECRET_MARKERS)
    return names, codes


def failure_diagnostics(log: Path, root: Path) -> dict:
    """Only fixed fixture categories, source assertion identifiers and coordinates."""
    result = {"compilerDiagnostics": [], "fixtureFailure": None, "runtimeDiagnostic": None}
    names, codes = _diagnostic_source()
    if not log.is_file():
        return result
    with regular(log).open("rb") as stream:
        size = stream.seek(0, 2)
        stream.seek(max(0, size - 1024 * 1024))
        text = stream.read(1024 * 1024).decode("utf-8", errors="replace")
    for line in text.splitlines():
        match = re.match(r"^(.*)\(([1-9][0-9]{0,6}),([1-9][0-9]{0,6})\): error ((?:CS|NU|MSB|NETSDK)[0-9]{1,6}):", line)
        if match and Path(match[1]).parent == root and Path(match[1]).name in names:
            row = {"file": Path(match[1]).name, "line": int(match[2]), "column": int(match[3]), "code": match[4]}
            if row not in result["compilerDiagnostics"] and len(result["compilerDiagnostics"]) < 128:
                result["compilerDiagnostics"].append(row)
        match = re.fullmatch(r"SOCKET_PACKAGE_CONSUMER_FAIL:(configuration|socket-vertical|report):(timeout|operation|assertion:([a-z][a-z0-9-]{0,95}))", line)
        if match and (match[3] is None or match[3] in codes):
            result["fixtureFailure"] = {"stage": match[1], "category": match[2]}
        match = re.fullmatch(r"SOCKET_PACKAGE_CONSUMER_DIAGNOSTIC:(postgres|db-update|argument|invalid-operation|not-supported|invalid-cast|null-reference|timeout|cancelled|io|other):(none|[0-9A-Z]{5}):([A-Za-z]+\.cs|none):([0-9]{1,7})", line)
        if match and ((match[3] in names and int(match[4]) > 0) or (match[3] == "none" and match[4] == "0")):
            result["runtimeDiagnostic"] = {"category": match[1], "sqlState": None if match[2] == "none" else match[2],
                                           "file": None if match[3] == "none" else match[3], "line": int(match[4])}
    return result


def _safe_diagnostics(data: dict) -> dict:
    """Revalidate the failure boundary too; arbitrary helper exceptions cannot export data."""
    names, codes = _diagnostic_source()
    result = {"compilerDiagnostics": [], "fixtureFailure": None, "runtimeDiagnostic": None}
    if type(data) is not dict:
        return result
    rows = data.get("compilerDiagnostics", [])
    for row in rows[:128] if type(rows) is list else []:
        if (type(row) is dict and set(row) == {"file", "line", "column", "code"}
                and type(row["file"]) is str and row["file"] in names and type(row["code"]) is str
                and re.fullmatch(r"(?:CS|NU|MSB|NETSDK)[0-9]{1,6}", row["code"])
                and all(type(row[key]) is int and 1 <= row[key] <= 9999999 for key in ("line", "column"))):
            result["compilerDiagnostics"].append(row)
    row = data.get("fixtureFailure")
    if (type(row) is dict and set(row) == {"stage", "category"}
            and row["stage"] in ("configuration", "socket-vertical", "report")
            and type(row["category"]) is str
            and (row["category"] in ("timeout", "operation") or row["category"] in {"assertion:" + code for code in codes})):
        result["fixtureFailure"] = row
    row = data.get("runtimeDiagnostic")
    if (type(row) is dict and set(row) == {"category", "sqlState", "file", "line"}
            and row["category"] in ("postgres", "db-update", "argument", "invalid-operation", "not-supported",
                                    "invalid-cast", "null-reference", "timeout", "cancelled", "io", "other")
            and (row["sqlState"] is None or type(row["sqlState"]) is str and re.fullmatch(r"[0-9A-Z]{5}", row["sqlState"]))
            and type(row["line"]) is int
            and ((type(row["file"]) is str and row["file"] in names and 1 <= row["line"] <= 9999999)
                 or (row["file"] is None and row["line"] == 0))):
        result["runtimeDiagnostic"] = row
    if data.get("cleanupFailure") == "service_cleanup":
        result["cleanupFailure"] = "service_cleanup"
    return result


def _fixture_files() -> list[Path]:
    files = sorted(path for path in FIXTURE.iterdir() if path.suffix in (".cs", ".csproj"))
    projects = [path for path in files if path.suffix == ".csproj"]
    require(len(projects) == 1 and projects[0].name == "SocketPackageConsumer.csproj", "fixture_project")
    project = ET.fromstring(regular(projects[0]).read_bytes())
    references = project.findall(".//PackageReference")
    require(project.attrib.get("Sdk") == "Microsoft.NET.Sdk" and bool(references)
            and not project.findall(".//ProjectReference") and not project.findall(".//FrameworkReference")
            and not project.findall(".//Reference")
            and all(row.attrib.get("Version") == "$(CandidateVersion)" for row in references), "fixture_project")
    return files


def _run_runtime(command: list[str], root: Path, environment: dict, log: Path) -> dict:
    # Shared helper owns the actual Linux birth observation, wait, kill and reap.
    # Its Admission diagnostics are replaced; no module globals are changed.
    try:
        return lifecycle._run_runtime(command, root, environment, log)
    except ProofError as error:
        raise ProofError(str(error), failure_diagnostics(log, root)) from None


def _cell(private: Path, artifacts: Path, identity: tuple, sdk: str,
          service: dict, connection: str, service_log: Path) -> dict:
    version, source, by_id, exceptions, excluded = identity
    root = private / (TFM + "-" + FEATURE)
    root.mkdir()
    isolation = consumers.prepare_isolation(root, sdk)
    for path in _fixture_files():
        shutil.copy2(regular(path), root / path.name)
    config = root / "NuGet.Config"
    config.write_text(consumers.render_nuget_config(artifacts, [row["id"] for row in by_id.values()], exceptions))
    inputs = {path.name: sha256(path) for path in root.iterdir() if path.is_file()}
    require(all(inputs[name] == value for name, value in isolation.items()), "isolation_inputs")
    cache, home = root / "fresh-packages", root / "home"
    require(not cache.exists(), "fresh_cache")
    home.mkdir()
    environment = {key: value for key, value in os.environ.items()
                   if not key.upper().startswith(("NUGET_", "ELSA_", "MSBUILD", "DOTNET_"))
                   and key not in ("IsPackable", "Version", "PackageVersion", "Configuration", "TargetFramework", "TargetFrameworks")}
    environment.update({"HOME": str(home), "DOTNET_CLI_HOME": str(home), "NUGET_PACKAGES": str(cache),
                        "NUGET_HTTP_CACHE_PATH": str(root / "http-cache"), "NUGET_PLUGINS_CACHE_PATH": str(root / "plugin-cache"),
                        "DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER": "1", "MSBUILDDISABLENODEREUSE": "1",
                        "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1"})
    project = root / "SocketPackageConsumer.csproj"
    properties = ["-p:CandidateVersion=" + version, "-p:TargetFrameworks=" + TFM]
    commands = []
    for phase, command in (
        ("restore", ["dotnet", "restore", str(project), "--configfile", str(config), "--packages", str(cache),
                     "--force-evaluate", "--no-cache", "--nologo", "--verbosity", "minimal", *properties]),
        ("build", ["dotnet", "build", str(project), "--configuration", "Release", "--framework", TFM,
                   "--no-restore", "--disable-build-servers", "--nologo", "--verbosity", "minimal", *properties]),
    ):
        log = root / (phase + ".log")
        try:
            consumers._run_command(command, root, environment, log, lifecycle.COMMAND_TIMEOUT)
        except Exception:
            raise ProofError(phase + "_command", failure_diagnostics(log, root)) from None
        commands.append({"phase": phase, "exitCode": 0, "logSha256": sha256(log)})
        if phase == "restore":
            assets_path = root / "obj" / "project.assets.json"
            assets = read_json(assets_path, maximum=32 * 1024 * 1024)
            consumers.verify_restore_isolation(assets, root, cache, artifacts)
            restored = consumers.validate_project_assets(assets, (TFM,), {row["id"] for row in by_id.values()},
                                                        version, set(exceptions), excluded)
            require({name.casefold() for name in REQUIRED_PACKAGES} <=
                    {row["id"].casefold() for row in restored["internal"]}, "required_restored_packages")
            packages = consumers._package_evidence(artifacts, cache, by_id, restored, exceptions, version)
            assets_hash = sha256(assets_path)
    require(sha256(assets_path) == assets_hash, "assets_changed_during_build")
    database = "socket_consumer_" + secrets.token_hex(12)
    lifecycle._sql(service["containerId"], "postgres", f'CREATE DATABASE "{database}"', service_log)
    actual = lifecycle._sql(service["containerId"], database, "SELECT current_database()", service_log)
    require(actual == database, "database_identity")
    database_hash = hashlib.sha256(actual.encode()).hexdigest()
    selected_connection = connection.replace("Database=postgres;", "Database=" + database + ";")
    environment.update({"ELSA_SOCKET_PACKAGE_CONNECTION_STRING": selected_connection,
                        "ELSA_SOCKET_PACKAGE_SOURCE_REVISION": source, "ELSA_SOCKET_PACKAGE_CANDIDATE_VERSION": version})
    evidence = root / "runtime-evidence"
    evidence.mkdir()
    report_path = evidence / "cell-report.json"
    executable = root / "bin" / "Release" / TFM / "Elsa.Slack.Tests.dll"
    executable_hash = sha256(executable)
    process = _run_runtime(["dotnet", str(executable), "--feature", FEATURE, "--tfm", TFM,
                            "--output", str(report_path)], root, environment, root / "runtime.log")
    require({path.name for path in evidence.iterdir()} == {"cell-report.json"}, "runtime_evidence_inventory")
    report = reports.validate_cell(read_json(report_path), expected_head=source, version=version, tfm=TFM,
                                   feature=FEATURE, process_id=process["pid"], database_hash=database_hash,
                                   server_version=service["serverVersion"])
    loaded = consumers.verify_loaded_assemblies(report, assets, TFM, root, cache, artifacts, by_id, version, source,
                                                required_packages=REQUIRED_PACKAGES)
    require(consumers._package_evidence(artifacts, cache, by_id, restored, exceptions, version) == packages,
            "cache_changed_during_execution")
    require(sha256(executable) == executable_hash and sha256(assets_path) == assets_hash, "consumer_outputs_changed")
    require(all(sha256(root / name) == value for name, value in inputs.items()), "consumer_inputs_changed")
    lifecycle._private_scan(private, (*SECRET_MARKERS, connection, selected_connection))
    process["fixtureReportedStartIdentitySha256"] = report["process"]["startIdentitySha256"]
    return {"tfm": TFM, "feature": FEATURE, "report": report, "process": process, "commands": commands,
            "consumerInputs": inputs, "assetsSha256": assets_hash, "fixtureAssemblySha256": executable_hash,
            "restoredPackages": packages, "loadedAssemblies": loaded}


def _prove(artifacts: Path, manifest: dict, output: Path) -> dict:
    require(os.name == "posix" and Path("/proc/sys/kernel/random/boot_id").is_file(), "linux_required")
    identity = consumers._validated_manifest(manifest, required_packages=REQUIRED_PACKAGES)
    version, source, by_id, _, _ = identity
    require(subprocess.check_output(["git", "-C", str(ROOT), "rev-parse", "HEAD"], text=True).strip() == source, "source_head")
    require(not subprocess.check_output(["git", "-C", str(ROOT), "status", "--porcelain", "--untracked-files=no"], text=True), "source_dirty")
    sdk = manifest.get("build_inputs", {}).get("sdk")
    require(type(sdk) is str and re.fullmatch(r"10\.[0-9]+\.[0-9]+", sdk) is not None, "manifest_sdk")
    require(not any(path.is_symlink() for path in (artifacts, *artifacts.parents)), "artifacts_directory")
    artifacts = artifacts.resolve(strict=True)
    require(artifacts.is_dir(), "artifacts_directory")
    require(not any(path.is_symlink() for path in (output, *output.parents)), "retained_output_exists")
    output = output.resolve()
    require(not any(path.is_symlink() for path in (output, *output.parents))
            and not output.exists() and not output.is_relative_to(ROOT)
            and not output.is_relative_to(artifacts), "retained_output_exists")
    before_source = lifecycle.tracked_inputs()
    before_archives = lifecycle.frozen_archives(artifacts, manifest)
    service = {}
    with tempfile.TemporaryDirectory(prefix="elsa-socket-consumer-private-") as temporary:
        private = Path(temporary).resolve()
        require(not private.is_relative_to(ROOT) and not private.is_relative_to(artifacts) and not private.is_relative_to(output)
                and not output.is_relative_to(private), "private_output_boundary")
        name, password = "elsa-socket-consumer-" + secrets.token_hex(12), secrets.token_urlsafe(32)
        log = private / "postgres.log"
        try:
            connection, service = lifecycle._start_postgres(name, password, log, service)
            server = lifecycle._sql(service["containerId"], "postgres", "SELECT current_setting('server_version_num')::integer", log)
            require(server.isdecimal() and 160000 <= int(server) <= 169999, "postgres_version")
            service["serverVersion"] = int(server)
            try:
                cell = _cell(private, artifacts, identity, sdk, service, connection, log)
            except ProofError:
                raise
            except Exception:
                raise ProofError("cell_validation") from None
            lifecycle._private_scan(private, (*SECRET_MARKERS, password, connection))
        finally:
            original = sys.exc_info()[1]
            try:
                cleanup = lifecycle._cleanup_postgres(name, service, log)
                require(cleanup["absenceVerified"] and ("containerId" not in service or cleanup["containerRemoved"]), "service_cleanup")
            except Exception:
                if isinstance(original, ProofError):
                    original.diagnostics["cleanupFailure"] = "service_cleanup"
                    raise original from None
                raise ProofError("service_cleanup") from None
        require(lifecycle.tracked_inputs() == before_source, "source_changed")
        require(lifecycle.frozen_archives(artifacts, manifest) == before_archives, "archives_changed")
        lifecycle.normalize_locations([cell], private, by_id)
        receipt = {"schemaVersion": 1, "status": "passed", "sourceRevision": source, "candidateVersion": version,
                   "sdk": sdk, "scope": "first-vertical-classic-net10.0", "fullSocketAcceptance": False,
                   "publicationPerformed": False, "pathConvention": "logical-consumer-root-after-byte-verification",
                   "cells": [cell], "service": service, "cleanup": cleanup,
                   "sourceInputs": before_source, "archiveHashes": before_archives,
                   "limitations": ["One package-only vertical; remaining Socket matrix and live acceptance are unproven.",
                                   "Linux birth token is independently observed; fixture UTC StartTime hash is self-reported."]}
        encoded = (json.dumps(receipt, indent=2, sort_keys=True) + "\n").encode()
        require(not any(value.encode() in encoded for value in (*SECRET_MARKERS, password, connection)), "retained_secret_marker")
        require(str(private).encode() not in encoded and str(artifacts).encode() not in encoded, "retained_machine_path")
    require(not private.exists(), "private_cleanup")
    _write_receipt(output, encoded)
    return receipt


def _write_receipt(output: Path, encoded: bytes) -> None:
    """No overwrite; publish sanitized bytes only with an atomic hardlink."""
    require(not any(path.is_symlink() for path in (output, *output.parents)), "receipt_write")
    output.mkdir(parents=True, exist_ok=True)
    destination = output / "socket-consumer-proof.json"
    require(not destination.exists(), "receipt_exists")
    staging = None
    try:
        staging = Path(tempfile.mkdtemp(prefix="elsa-socket-receipt-"))
        require(not staging.resolve().is_relative_to(output.resolve()), "receipt_write")
        pending = staging / "receipt.pending"
        with pending.open("xb") as stream:
            stream.write(encoded)
        os.link(pending, destination)
    except Exception:
        raise ProofError("receipt_write") from None
    finally:
        if staging is not None:
            # This staging holds sanitized bytes only. Runtime private cleanup
            # is mandatory before publication; a post-commit unlink cannot undo it.
            shutil.rmtree(staging, ignore_errors=True)


def prove(artifacts: Path, verifiedmanifest: dict, output: Path) -> dict:
    """Return only verified sanitized evidence; failure never claims cleanup success."""
    try:
        return _prove(artifacts, verifiedmanifest, output)
    except Exception as error:
        category = str(error) if isinstance(error, ProofError) and str(error) in CATEGORIES else "preflight"
        diagnostics = _safe_diagnostics(error.diagnostics) if isinstance(error, ProofError) and category != "preflight" else {}
        if not output.exists():
            _write_receipt(output, (json.dumps({"schemaVersion": 1, "status": "failed", "category": category,
                           "diagnostics": diagnostics, "publicationPerformed": False,
                           "fullSocketAcceptance": False}, sort_keys=True) + "\n").encode())
        raise ProofError(category, diagnostics) from None
