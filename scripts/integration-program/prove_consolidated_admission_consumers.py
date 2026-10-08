#!/usr/bin/env python3
"""Prove the six package-only Admission/managed-secret cells without publication.

Only a complete sanitized receipt is retained. Source, caches, keyrings, connection
strings and command output remain in an independently owned private temporary tree.
The caller supplies the original already-verified package manifest and archives.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import secrets
import shutil
import signal
import subprocess
import sys
import tempfile
import time
import zipfile
from typing import Any

import prove_consolidated_package_consumers as consumers
from run_admission_proof import image_identity, read_json, regular
import verify_admission_package_consumers as reports

ROOT = Path(__file__).resolve().parents[2]
FIXTURE = Path(__file__).with_name("admission-package-consumer")
REQUIRED_PACKAGES = tuple(reports.CONTRACT["requiredAssemblies"])
SECRET_MARKERS = ("synthetic-package-consumer-secret-never-export-8661",)
IMAGE = "postgres:16-alpine"
COMMAND_TIMEOUT = 1200
RUN_TIMEOUT = 300
SHA256 = re.compile(r"[0-9a-f]{64}\Z")


class ProofError(RuntimeError):
    """A fixed category, never a raw command, diagnostic or secret-bearing value."""

    def __init__(self, category: str, diagnostics: dict | None = None):
        super().__init__(category)
        self.diagnostics = diagnostics or {}


def failure_diagnostics(log: Path, root: Path) -> dict:
    """Extract only source-allowlisted identifiers and compiler locations."""
    result: dict = {"compilerDiagnostics": [], "fixtureFailure": None, "runtimeDiagnostic": None}
    if not log.is_file():
        return result
    fixture_names = {p.name for p in FIXTURE.iterdir() if p.suffix in (".cs", ".csproj")}
    codes = set()
    for path in FIXTURE.glob("*.cs"):
        codes.update(re.findall(r'"([a-z][a-z0-9-]{0,95})"', regular(path).read_text()))
    codes.difference_update(SECRET_MARKERS)
    for line in regular(log).read_text(errors="replace").splitlines():
        match = re.match(r"^(.*)\((\d{1,7}),(\d{1,7})\): error ((?:CS|NU|MSB|NETSDK)\d{1,6}):", line)
        if match:
            path = Path(match[1])
            if path.parent == root and path.name in fixture_names:
                row = {"file": path.name, "line": int(match[2]), "column": int(match[3]), "code": match[4]}
                if row not in result["compilerDiagnostics"]:
                    result["compilerDiagnostics"].append(row)
        match = re.fullmatch(r"PACKAGE_CONSUMER_FAIL:(configuration|admission|managed-secret-grant|report):(timeout|operation|assertion:([a-z][a-z0-9-]{0,95}))", line)
        if match and (match[3] is None or match[3] in codes):
            result["fixtureFailure"] = {"stage": match[1], "category": match[2]}
        match = re.fullmatch(r"PACKAGE_CONSUMER_DIAGNOSTIC:(postgres|db-update|argument|invalid-operation|not-supported|invalid-cast|null-reference|timeout|cancelled|io|other):(none|[0-9A-Z]{5}):([A-Za-z]+\.cs|none):([0-9]{1,7})", line)
        if match and ((match[3] in fixture_names and int(match[4]) > 0) or (match[3] == "none" and match[4] == "0")):
            result["runtimeDiagnostic"] = {"category": match[1], "sqlState": None if match[2] == "none" else match[2],
                                           "file": None if match[3] == "none" else match[3], "line": int(match[4])}
    result["compilerDiagnostics"] = result["compilerDiagnostics"][:128]
    return result


def require(condition: bool, category: str) -> None:
    if not condition:
        raise ProofError(category)


def sha256(path: Path) -> str:
    return hashlib.sha256(regular(path).read_bytes()).hexdigest()


def tracked_inputs() -> dict[str, str]:
    from prove_consolidated_packages import source_input_hashes
    return source_input_hashes(ROOT)


def frozen_archives(artifacts: Path, manifest: dict) -> dict[str, str]:
    """Recheck original archive hashes; never replace their trusted manifest hashes."""
    import prove_consolidated_packages as packages
    expected = {}
    for package in manifest["packages"]:
        for kind in ("nupkg", "snupkg"):
            name = package.get(kind)
            if name is None and kind == "snupkg":
                continue
            require(type(name) is str and Path(name).name == name and name.endswith("." + kind), "archive_name")
            digest = package.get(kind + "_sha256")
            require(type(digest) is str and SHA256.fullmatch(digest) is not None, "archive_manifest_hash")
            require(name not in expected and sha256(artifacts / name) == digest, "archive_bytes")
            with zipfile.ZipFile(artifacts / name) as archive:
                packages.verify_metadata(packages.metadata(archive), package, manifest,
                                         symbols=kind == "snupkg", require_sdk_metadata=True)
                if kind == "nupkg":
                    packages.verify_sdk_assets(archive, package, required=True)
            expected[name] = digest
    actual = {path.name for path in artifacts.iterdir()}
    require(actual == set(expected), "archive_inventory")
    return expected


def _capture(command: list[str], log: Path, *, timeout: int = 60) -> str:
    """Private, bounded diagnostics. Nothing from the command is retained verbatim."""
    try:
        result = subprocess.run(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                timeout=timeout, check=False)
        with log.open("ab") as stream:
            stream.write(result.stdout)
        require(result.returncode == 0, "service_command")
        return result.stdout.decode("utf-8", errors="strict").strip()
    except ProofError:
        raise
    except Exception:
        raise ProofError("service_command") from None


def _start_postgres(name: str, password: str, log: Path, owned: dict) -> tuple[str, dict]:
    # This narrow lifecycle uses the existing image validator, but captures Docker
    # output privately (the older upgrade fixture deliberately streams its output).
    container_id = _capture(["docker", "run", "--pull=missing", "--detach", "--name", name,
                             "--env", "POSTGRES_PASSWORD=" + password, "--env", "POSTGRES_DB=postgres",
                             "--publish", "127.0.0.1::5432", IMAGE], log, timeout=180)
    require(SHA256.fullmatch(container_id) is not None, "container_identity")
    owned["containerId"] = container_id
    image_id = _capture(["docker", "inspect", "--type", "container", "--format", "{{.Image}}", container_id], log)
    require(re.fullmatch(r"sha256:[0-9a-f]{64}", image_id) is not None, "container_image")
    image = image_identity(image_id)
    port = _capture(["docker", "port", container_id, "5432/tcp"], log)
    require(re.fullmatch(r"127\.0\.0\.1:[0-9]{1,5}", port) is not None, "container_loopback")
    deadline = time.monotonic() + 60
    while True:
        ready = subprocess.run(["docker", "exec", container_id, "pg_isready", "-U", "postgres", "-d", "postgres"],
                               stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=10)
        if ready.returncode == 0:
            break
        require(time.monotonic() < deadline, "postgres_start_timeout")
        time.sleep(0.5)
    owned["image"] = image
    return (f"Host=127.0.0.1;Port={port.split(':')[1]};Database=postgres;Username=postgres;Password={password}", owned)


def _sql(container: str, database: str, sql: str, log: Path) -> str:
    return _capture(["docker", "exec", container, "psql", "-X", "-q", "-A", "-t", "-v", "ON_ERROR_STOP=1",
                     "-U", "postgres", "-d", database, "-c", sql], log)


def _remove_postgres(name: str, log: Path, container_id: str | None) -> dict:
    _capture(["docker", "rm", "--force", name], log)
    # A successful query is essential: Docker failure is never evidence of absence.
    names = _capture(["docker", "ps", "-a", "--format", "{{.Names}}"], log).splitlines()
    ids = _capture(["docker", "ps", "-aq", "--no-trunc"], log).splitlines()
    require(all(SHA256.fullmatch(value) is not None for value in ids)
            and name not in names and (container_id is None or container_id not in ids), "container_cleanup")
    return {"containerRemoved": True, "absenceVerified": True}


def _cleanup_postgres(name: str, service: dict | None, log: Path) -> dict:
    present = _capture(["docker", "ps", "-a", "--format", "{{.Names}}"], log).splitlines()
    container_id = service.get("containerId") if service is not None else None
    if name in present:
        return _remove_postgres(name, log, container_id)
    ids = _capture(["docker", "ps", "-aq", "--no-trunc"], log).splitlines()
    require(all(SHA256.fullmatch(value) is not None for value in ids)
            and (container_id is None or container_id not in ids), "container_cleanup")
    return {"containerRemoved": container_id is not None, "absenceVerified": True}


def _process_start_token(pid: int) -> str:
    """Observe the live Linux process birth token, not the .NET wall-clock hash.

    /proc stat field22 is a monotonic boot-relative start token. Keep that clock
    distinct from the fixture's UTC StartTime ticks; only their PID is equated.
    """
    stat = Path(f"/proc/{pid}/stat").read_text()
    fields = stat[stat.rfind(")") + 2:].split()
    token = fields[19]
    require(token.isdecimal() and int(token) > 0, "process_start_identity")
    boot_id = Path("/proc/sys/kernel/random/boot_id").read_text().strip()
    require(re.fullmatch(r"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", boot_id) is not None, "process_start_identity")
    return hashlib.sha256(f"{pid}:{boot_id}:{token}".encode()).hexdigest()


def _group_exists(pid: int) -> bool:
    try:
        os.killpg(pid, 0)
        return True
    except ProcessLookupError:
        return False


def _run_runtime(command: list[str], root: Path, environment: dict[str, str], log: Path) -> dict:
    process = None
    try:
        with log.open("wb") as stream:
            process = subprocess.Popen(command, cwd=root, env=environment, stdout=stream,
                                       stderr=subprocess.STDOUT, start_new_session=True)
            token = _process_start_token(process.pid)
            exit_code = process.wait(timeout=RUN_TIMEOUT)
        require(exit_code == 0, "runtime_exit")
        require(not _group_exists(process.pid), "runtime_child_leak")
        return {"pid": process.pid, "observedLinuxStartTokenSha256": token,
                "exitCode": exit_code, "reaped": True, "processGroupAbsent": True,
                "fixtureStartIdentityIndependentlyVerified": False, "logSha256": sha256(log)}
    except ProofError as error:
        error.diagnostics = failure_diagnostics(log, root)
        raise
    except subprocess.TimeoutExpired:
        raise ProofError("runtime_timeout") from None
    except Exception:
        raise ProofError("runtime_process") from None
    finally:
        if process is not None:
            if _group_exists(process.pid):
                try:
                    os.killpg(process.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
            process.wait()
            require(not _group_exists(process.pid), "runtime_cleanup")


def _private_scan(root: Path, sensitive_values: tuple[str, ...]) -> None:
    # Source contains the intentional marker; scan outputs/diagnostics, not source.
    needles = tuple(value.encode() for value in (*SECRET_MARKERS, *sensitive_values))
    for path in root.rglob("*.log"):
        data = regular(path).read_bytes()
        require(not any(value in data for value in needles), "private_log_secret_marker")
    for path in root.rglob("cell-report.json"):
        data = regular(path).read_bytes()
        require(not any(value in data for value in needles), "report_secret_marker")


def _cell(private: Path, artifacts: Path, identity: tuple, sdk: str,
          container: str, connection: str, tfm: str, feature: str, service_log: Path) -> dict:
    version, source, by_id, exceptions, excluded = identity
    root = private / (tfm + "-" + feature)
    root.mkdir()
    isolation = consumers.prepare_isolation(root, sdk)
    files = sorted(path for path in FIXTURE.iterdir() if path.suffix in (".cs", ".csproj"))
    require(len([path for path in files if path.suffix == ".csproj"]) == 1, "fixture_project")
    for path in files:
        shutil.copy2(regular(path), root / path.name)
    config = root / "NuGet.Config"
    config.write_text(consumers.render_nuget_config(artifacts, [row["id"] for row in by_id.values()], exceptions))
    input_hashes = {path.name: sha256(path) for path in root.iterdir() if path.is_file()}
    require(all(input_hashes[name] == value for name, value in isolation.items()), "isolation_inputs")
    cache = root / "fresh-packages"
    require(not cache.exists(), "fresh_cache")
    home = root / "home"
    home.mkdir()
    environment = os.environ.copy()
    # Do not inherit a credential-bearing proof environment or NuGet fallback path.
    for key in list(environment):
        if key.upper().startswith(("NUGET_", "ELSA_PACKAGE_", "MSBUILD")):
            del environment[key]
    environment.update({"HOME": str(home), "DOTNET_CLI_HOME": str(home), "NUGET_PACKAGES": str(cache),
                        "NUGET_HTTP_CACHE_PATH": str(root / "http-cache"), "NUGET_PLUGINS_CACHE_PATH": str(root / "plugin-cache"),
                        "DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER": "1", "MSBUILDDISABLENODEREUSE": "1",
                        "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1"})
    project = root / "AdmissionPackageConsumer.csproj"
    properties = ["-p:CandidateVersion=" + version, "-p:TargetFrameworks=" + tfm]
    commands = []
    for phase, command in (
        ("restore", ["dotnet", "restore", str(project), "--configfile", str(config), "--packages", str(cache),
                     "--force-evaluate", "--no-cache", "--nologo", "--verbosity", "minimal", *properties]),
        ("build", ["dotnet", "build", str(project), "--configuration", "Release", "--framework", tfm,
                   "--no-restore", "--disable-build-servers", "--nologo", "--verbosity", "minimal", *properties]),
    ):
        log = root / (phase + ".log")
        try:
            consumers._run_command(command, root, environment, log, COMMAND_TIMEOUT)
        except Exception:
            raise ProofError(phase + "_command", failure_diagnostics(log, root)) from None
        commands.append({"phase": phase, "exitCode": 0, "logSha256": sha256(log)})
        if phase == "restore":
            assets_path = root / "obj" / "project.assets.json"
            assets = read_json(assets_path, maximum=32 * 1024 * 1024)
            consumers.verify_restore_isolation(assets, root, cache, artifacts)
            restored = consumers.validate_project_assets(assets, (tfm,), {row["id"] for row in by_id.values()},
                                                        version, set(exceptions), excluded)
            require({name.casefold() for name in REQUIRED_PACKAGES} <=
                    {row["id"].casefold() for row in restored["internal"]}, "required_restored_packages")
            packages = consumers._package_evidence(artifacts, cache, by_id, restored, exceptions, version)
            assets_hash = sha256(assets_path)
    require(sha256(assets_path) == assets_hash, "assets_changed_during_build")
    databases = {}
    connections = {}
    for scenario in reports.SCENARIOS:
        database = "admission_consumer_" + secrets.token_hex(12)
        _sql(container, "postgres", f'CREATE DATABASE "{database}"', service_log)
        actual = _sql(container, database, "SELECT current_database()", service_log)
        require(actual == database, "database_identity")
        databases[scenario] = hashlib.sha256(actual.encode()).hexdigest()
        connections[scenario] = connection.replace("Database=postgres;", "Database=" + database + ";")
    require(len(set(databases.values())) == 2, "database_pair")
    environment.update({"ELSA_PACKAGE_ADMISSION_CONNECTION_STRING": connections["admission"],
                        "ELSA_PACKAGE_GRANT_CONNECTION_STRING": connections["managed-secret-grant"],
                        "ELSA_PACKAGE_SOURCE_REVISION": source, "ELSA_PACKAGE_CANDIDATE_VERSION": version})
    evidence = root / "runtime-evidence"
    evidence.mkdir()
    report_path = evidence / "cell-report.json"
    executable = root / "bin" / "Release" / tfm / "AdmissionPackageConsumer.dll"
    executable_hash = sha256(executable)
    process = _run_runtime(["dotnet", str(executable), "--feature", feature, "--tfm", tfm,
                            "--output", str(report_path)], root, environment, root / "runtime.log")
    require({path.name for path in evidence.iterdir()} == {"cell-report.json"}, "runtime_evidence_inventory")
    report = reports.validate_cell(read_json(report_path), expected_head=source, version=version, tfm=tfm,
                                   feature=feature, process_id=process["pid"], database_hashes=databases)
    loaded = consumers.verify_loaded_assemblies(report, assets, tfm, root, cache, artifacts, by_id, version, source,
                                                required_packages=REQUIRED_PACKAGES)
    require(consumers._package_evidence(artifacts, cache, by_id, restored, exceptions, version) == packages,
            "cache_changed_during_execution")
    require(sha256(executable) == executable_hash and sha256(assets_path) == assets_hash, "consumer_outputs_changed")
    require(all(sha256(root / name) == value for name, value in input_hashes.items()), "consumer_inputs_changed")
    process["fixtureReportedStartIdentitySha256"] = report["process"]["startIdentitySha256"]
    return {"tfm": tfm, "feature": feature, "report": report, "process": process, "commands": commands,
            "consumerInputs": input_hashes, "assetsSha256": assets_hash, "fixtureAssemblySha256": executable_hash,
            "restoredPackages": packages, "loadedAssemblies": loaded}


def normalize_locations(cells: list[dict], private: Path, by_id: dict) -> None:
    """Called only after all raw-path byte checks and complete matrix validation."""
    for cell in cells:
        cell_id = cell["tfm"] + "-" + cell["feature"]
        root = private / cell_id
        for rows in (cell["report"]["loadedAssemblies"], cell["loadedAssemblies"]):
            for row in rows:
                relative = Path(row["location"]).relative_to(root)
                require(relative.parts[0] == "bin" and ".." not in relative.parts, "loaded_logical_path")
                row["location"] = "/consumer/" + cell_id + "/" + relative.as_posix()
        for row in cell["restoredPackages"]:
            if row["id"].casefold() in by_id:
                row["source"] = "exact-verified-artifact-feed"


def _prove(artifacts: Path, manifest: dict[str, Any], output: Path) -> dict[str, Any]:
    """Consume the caller's exact archives and return complete sanitized evidence."""
    require(os.name == "posix" and Path("/proc/sys/kernel/random/boot_id").is_file(), "linux_required")
    identity = consumers._validated_manifest(manifest, required_packages=REQUIRED_PACKAGES)
    version, source, _, _, _ = identity
    require(subprocess.check_output(["git", "-C", str(ROOT), "rev-parse", "HEAD"], text=True).strip() == source,
            "source_head")
    require(not subprocess.check_output(["git", "-C", str(ROOT), "status", "--porcelain", "--untracked-files=no"], text=True),
            "source_dirty")
    sdk = manifest.get("build_inputs", {}).get("sdk")
    require(type(sdk) is str and re.fullmatch(r"10\.[0-9]+\.[0-9]+", sdk) is not None, "manifest_sdk")
    artifacts = artifacts.resolve(strict=True)
    require(artifacts.is_dir(), "artifacts_directory")
    output = output.resolve()
    require(not output.exists(), "retained_output_exists")
    before_source = tracked_inputs()
    before_archives = frozen_archives(artifacts, manifest)
    cells = []
    service: dict = {}
    cleanup = None
    with tempfile.TemporaryDirectory(prefix="elsa-admission-consumers-private-") as temporary:
        private = Path(temporary).resolve()
        require(not private.is_relative_to(output) and not output.is_relative_to(private), "private_output_boundary")
        name = "elsa-admission-consumer-" + secrets.token_hex(12)
        password = secrets.token_urlsafe(32)
        log = private / "postgres.log"
        try:
            connection, service = _start_postgres(name, password, log, service)
            server_version = _sql(service["containerId"], "postgres", "SELECT current_setting('server_version_num')::integer", log)
            require(server_version.isdecimal() and 160000 <= int(server_version) <= 169999, "postgres_version")
            service["serverVersion"] = int(server_version)
            for tfm in reports.FRAMEWORKS:
                for feature in reports.FEATURES:
                    try:
                        cells.append(_cell(private, artifacts, identity, sdk, service["containerId"],
                                           connection, tfm, feature, log))
                    except ProofError as error:
                        error.diagnostics["cell"] = {"tfm": tfm, "feature": feature}
                        raise
                    except Exception:
                        raise ProofError("cell_validation", {"cell": {"tfm": tfm, "feature": feature}}) from None
            reports.validate_matrix([cell["report"] for cell in cells])
            require(all(scenario["serverVersion"] == service["serverVersion"]
                        for cell in cells for scenario in cell["report"]["scenarios"]), "server_version_binding")
            require(len({cell["process"]["observedLinuxStartTokenSha256"] for cell in cells}) == 6, "process_identity_reuse")
            _private_scan(private, (password, connection))
        except ProofError:
            raise
        except Exception:
            raise ProofError("consumer_proof") from None
        finally:
            # Name is allocated before docker run, so partial startup is also owned.
            # Missing containers after failed startup are verified rather than trusted.
            original = sys.exc_info()[1]
            try:
                cleanup = _cleanup_postgres(name, service, log)
                require(cleanup["absenceVerified"] and ("containerId" not in service or cleanup["containerRemoved"]), "service_cleanup")
            except Exception:
                if isinstance(original, ProofError):
                    original.diagnostics["cleanupFailure"] = "service_cleanup"
                    raise original from None
                raise ProofError("service_cleanup") from None
        require(bool(service) and len(cells) == 6, "proof_incomplete")
        require(tracked_inputs() == before_source, "source_changed")
        require(frozen_archives(artifacts, manifest) == before_archives, "archives_changed")
        normalize_locations(cells, private, identity[2])
        receipt = {"schemaVersion": 1, "status": "passed", "sourceRevision": source, "candidateVersion": version,
                   "sdk": sdk, "publicationPerformed": False, "pathConvention": "logical-consumer-root-after-byte-verification", "cells": cells, "service": service, "cleanup": cleanup,
                   "sourceInputs": before_source, "archiveHashes": before_archives,
                   "limitations": ["Offline package contracts only; no live provider calls or publication.",
                                   "Linux process start token is independently observed; fixture UTC StartTime hash is self-reported."]}
        encoded = (json.dumps(receipt, indent=2, sort_keys=True) + "\n").encode()
        require(not any(value.encode() in encoded for value in (*SECRET_MARKERS, password, connection)), "retained_secret_marker")
        require(str(private).encode() not in encoded and str(artifacts).encode() not in encoded, "retained_machine_path")
    # TemporaryDirectory cleanup completed before any success evidence is retained.
    require(not private.exists(), "private_cleanup")
    _write_receipt(output, encoded)
    return receipt


def _write_receipt(output: Path, encoded: bytes) -> None:
    output.mkdir(parents=True, exist_ok=True)
    destination = output / "admission-consumer-proof.json"
    pending = output / ".admission-consumer-proof.pending"
    require(not destination.exists() and not pending.exists(), "receipt_exists")
    try:
        with pending.open("xb") as stream:
            stream.write(encoded)
        os.link(pending, destination)
        pending.unlink()
    except Exception:
        pending.unlink(missing_ok=True)
        raise ProofError("receipt_write") from None


def prove(artifacts: Path, manifest: dict[str, Any], output: Path) -> dict[str, Any]:
    """Retain no raw diagnostics; failures can never resemble a passed receipt."""
    try:
        return _prove(artifacts, manifest, output)
    except ProofError as error:
        # _prove has unwound its private temp tree and attempted verified cleanup.
        # A failed cleanup stays failed; this record makes no cleanup-success claim.
        if not output.resolve().exists():
            failure = {"schemaVersion": 1, "status": "failed", "category": str(error),
                       "diagnostics": error.diagnostics, "publicationPerformed": False}
            _write_receipt(output.resolve(), (json.dumps(failure, sort_keys=True) + "\n").encode())
        raise
    except Exception:
        error = ProofError("preflight")
        if not output.resolve().exists():
            _write_receipt(output.resolve(), (json.dumps({"schemaVersion": 1, "status": "failed", "category": "preflight",
                           "diagnostics": {}, "publicationPerformed": False}, sort_keys=True) + "\n").encode())
        raise error from None


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--artifacts", type=Path, required=True)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        prove(args.artifacts, read_json(args.manifest.resolve(strict=True), maximum=32 * 1024 * 1024), args.output)
    except ProofError as error:
        raise SystemExit("ADMISSION_CONSUMER_FAIL:" + str(error)) from None
    except Exception:
        raise SystemExit("ADMISSION_CONSUMER_FAIL:preflight") from None
    print("ADMISSION_CONSUMER_PASS")


if __name__ == "__main__":
    main()
