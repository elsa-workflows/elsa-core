#!/usr/bin/env python3
"""Disposable package-only Studio hosts and a real, owned Elsa backend.

This module materializes reviewed host glue. The caller supplies verified package
inputs and must validate each restored project before starting any listener.
RuntimeHandle is private in-memory data; never put its credentials in receipts or files.
"""
from __future__ import annotations

from contextlib import contextmanager
from dataclasses import dataclass, field
import base64
import hashlib
import json
import os
from pathlib import Path
import re
import secrets
import signal
import socket
import subprocess
import threading
import time
from typing import Callable, Iterator
from urllib.error import URLError
from urllib.request import urlopen
from xml.etree import ElementTree as ET

import prove_consolidated_package_consumers as packages

FIXTURE = Path(__file__).resolve().parent / "paired-package-browser"
HOST_NAMES = {"server": "Server", "wasm": "Wasm", "hosted-wasm": "HostedWasm", "custom-elements": "CustomElements"}
VERSIONS = ("3.8.4", "3.9.0", "3.10.0")
PLATFORM_VERSIONS = {"net8.0": "8.0.24", "net9.0": "9.0.13", "net10.0": "10.0.3"}
BACKEND_PACKAGES = ("Elsa", "Elsa.Identity", "Elsa.Workflows.Api", "Elsa.Expressions.JavaScript", "Elsa.Persistence.EFCore.Sqlite", "Elsa.Bpmn.Interchange",
                    "Elsa.WorkflowContexts", "Elsa.Secrets", "Elsa.Secrets.Persistence.EFCore.Sqlite")
PERMISSION_PROFILES = ("full", "denied", "deny-secrets", "deny-workflow-contexts")
DENIED_PERMISSIONS = ("read:workflow-definitions", "read:workflow-instances", "read:activity-descriptors",
                      "read:workflow-context-provider-descriptors")
LEGACY_EDITOR_PERMISSIONS = (
    "read:workflow-definitions", "write:workflow-definitions", "publish:workflow-definitions",
    "retract:workflow-definitions", "exec:workflow-definitions", "read:workflow-instances",
    "read:activity-execution", "read:activity-descriptors", "read:activity-descriptors-options",
    "read:expression-descriptors", "read:storage-drivers", "read:variable-descriptors",
    "read:commit-strategies", "read:incident-strategies", "read:log-persistence-strategies",
    "read:workflow-activation-strategies", "read:output-converters", "read:installed-features")
EDITOR_PERMISSIONS = (
    "workflows/definitions:view", "workflows/definitions:write", "workflows/definitions:publish",
    "workflows/definitions:retract", "workflows/definitions:execute", "workflows/instances:view",
    "workflows/activity-executions:view", "workflows/descriptors/activities:view",
    "workflows/descriptors/expressions:view", "workflows/descriptors/storage-drivers:view",
    "workflows/descriptors/variables:view", "workflows/descriptors/commit-strategies:view",
    "workflows/descriptors/incident-strategies:view", "workflows/descriptors/log-persistence-strategies:view",
    "workflows/descriptors/activation-strategies:view", "system/features:view")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


@dataclass(frozen=True)
class CellRequest:
    host: str
    framework: str
    version: str
    backend_features: tuple[str, ...] = ("workflow-contexts", "secrets")
    permission_profile: str = "full"
    route_prefix: str = ""

    def __post_init__(self) -> None:
        require(self.host in HOST_NAMES and self.framework in packages.FRAMEWORKS and self.version in VERSIONS,
                "Unknown host/framework/version cell")
        require(type(self.backend_features) is tuple and len(set(self.backend_features)) == len(self.backend_features)
                and set(self.backend_features) <= {"workflow-contexts", "secrets"}, "Unknown or duplicate backend feature")
        require(self.permission_profile in PERMISSION_PROFILES, "Unknown permission profile")
        require(self.route_prefix == "" or (self.host == "hosted-wasm" and
                re.fullmatch(r"[a-z][a-z0-9-]{0,31}", self.route_prefix) is not None), "Unsafe hosted route prefix")


@dataclass(frozen=True)
class CellLayout:
    request: CellRequest
    group_root: Path
    project_paths: dict[str, Path]
    runtime_root: Path
    packages_root: Path
    sdk: str
    input_hashes: dict[str, str]


@dataclass(frozen=True)
class RuntimeHandle:
    request: CellRequest
    studio_url: str
    backend_url: str
    username: str
    password: str = field(repr=False)
    safe_ids: dict[str, str] = field(default_factory=dict)
    process_ids: tuple[int, ...] = ()


def permission_grants(request: CellRequest) -> tuple[str, ...]:
    """Published 3.8.4 vocabulary differs from the 3.9/3.10 resource catalog."""
    if request.permission_profile == "full":
        return ("*",)
    if request.permission_profile == "denied":
        return DENIED_PERMISSIONS
    legacy = request.version == "3.8.4"
    grants = list(LEGACY_EDITOR_PERMISSIONS if legacy else EDITOR_PERMISSIONS)
    if request.permission_profile != "deny-workflow-contexts":
        grants.append("read:workflow-context-provider-descriptors")
    if request.permission_profile != "deny-secrets":
        grants.extend(("read:secrets", "write:secrets", "delete:secrets", "test:secrets", "use:secrets", "export:secrets", "import:secrets")
                      if legacy else ("secrets:view", "secrets:write", "secrets:delete", "secrets:test"))
    return tuple(grants)


def _host_source(host: str, version: str) -> dict[str, Path]:
    pins = {record["file"]: record["sha256"] for record in
            json.loads((FIXTURE / "hosts" / "source-glue.json").read_text())["files"]}
    paths = {}
    variants = ("common", version) if version != "3.10.0" else ("common",)
    for variant in variants:
        directory = FIXTURE / "hosts" / variant / host
        for path in directory.rglob("*"):
            if path.is_file():
                require(not path.is_symlink(), "Symlinked host glue")
                require(pins.get(str(path.relative_to(FIXTURE / "hosts"))) == sha256(path), "Pinned host source glue changed")
                paths[str(path.relative_to(directory))] = path
    return paths


def _project(host: str, request: CellRequest) -> tuple[str, str]:
    if host == "backend":
        sdk, ids, platform = "Microsoft.NET.Sdk.Web", [name for name in BACKEND_PACKAGES
            if name != "Elsa.Bpmn.Interchange" or request.version != "3.8.4"], []
    else:
        original = ET.parse(_host_source(host, request.version)["source-project.xml"]).getroot()
        sdk = original.attrib["Sdk"]
        ids = [Path(item.attrib["Include"].replace("\\", "/")).stem for item in original.findall(".//ProjectReference")]
        ids = [name for name in ids if not name.startswith("Elsa.Studio.Host.")]
        if host != "hosted-wasm":
            ids.append("Elsa.Studio.WorkflowContexts")
        platform = [item.attrib["Include"] for item in original.findall(".//PackageReference")]
    root = ET.Element("Project", {"Sdk": sdk})
    props = ET.SubElement(root, "PropertyGroup")
    for key, value in {
        "TargetFramework": request.framework, "IsPackable": "false", "ImplicitUsings": "enable",
        "Nullable": "enable", "LangVersion": "latest", "ManagePackageVersionsCentrally": "false",
        "RestorePackagesWithLockFile": "true", "RestoreFallbackFolders": "",
        "DisableImplicitNuGetFallbackFolder": "true", "DisableImplicitLibraryPacksFolder": "true",
        "BlazorWebAssemblyLoadAllGlobalizationData": "true",
    }.items():
        ET.SubElement(props, key).text = value
    if host == "backend" and request.version in ("3.9.0", "3.10.0"):
        ET.SubElement(props, "DefineConstants").text = "$(DefineConstants);FIXTURE_BPMN"
    group = ET.SubElement(root, "ItemGroup")
    for name in sorted(set(ids)):
        ET.SubElement(group, "PackageReference", {"Include": name, "Version": request.version})
    for name in platform:
        ET.SubElement(group, "PackageReference", {"Include": name, "Version": PLATFORM_VERSIONS[request.framework]})
    if host == "hosted-wasm":
        # The sole permitted source edge is disposable, nonpackable host glue.
        ET.SubElement(group, "ProjectReference", {"Include": "../wasm/Elsa.Studio.Host.Wasm.csproj"})
    name = "PairedBackend.csproj" if host == "backend" else f"Elsa.Studio.Host.{HOST_NAMES[host]}.csproj"
    ET.indent(root)
    return name, ET.tostring(root, encoding="unicode") + "\n"


def materialize(request: CellRequest, group_root: Path, *, nuget_config: str,
                packages_root: Path, sdk: str) -> CellLayout:
    require(re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+", sdk) is not None, "Unpinned SDK")
    require(not group_root.is_symlink() and not packages_root.is_symlink(), "Symlinked group/cache root")
    group_root, packages_root = group_root.resolve(), packages_root.resolve()
    require(not group_root.is_relative_to(Path(__file__).resolve().parents[2]), "Fixture must live outside repository imports")
    # Parse before writing; exact feed/source mapping is validated by the caller.
    require(ET.fromstring(nuget_config).tag == "configuration", "Invalid NuGet config")
    identity = {"schema": 1, "version": request.version, "framework": request.framework, "sdk": sdk,
                "packages_root": str(packages_root), "nuget_config_sha256": hashlib.sha256(nuget_config.encode()).hexdigest()}
    marker = group_root / "group.json"
    if group_root.exists():
        require(marker.is_file() and not marker.is_symlink() and json.loads(marker.read_text()) == identity,
                "Shared group identity differs or unowned existing directory")
    else:
        group_root.mkdir(parents=True, mode=0o700)
        require(not packages_root.exists() or not any(packages_root.iterdir()), "Initial group package cache is not fresh")
        marker.write_text(json.dumps(identity, sort_keys=True) + "\n")
    packages_root.mkdir(parents=True, exist_ok=True)
    hosts = ["backend", request.host]
    if request.host == "hosted-wasm":
        hosts.insert(1, "wasm")
    projects, hashes = {}, {}
    for host in hosts:
        directory = group_root / "projects" / host
        require(not directory.is_symlink() and directory.resolve().is_relative_to(group_root), "Unsafe shared project directory")
        name, project = _project(host, request)
        expected = {"NuGet.Config": nuget_config.encode(), name: project.encode()}
        if host in ("backend", "server"):
            expected["RuntimeEvidence.cs"] = (FIXTURE / "backend" / "RuntimeEvidence.cs").read_bytes()
        if host == "backend":
            expected["Program.cs"] = (FIXTURE / "backend" / "Program.cs").read_bytes()
        else:
            expected.update({name: path.read_bytes() for name, path in _host_source(host, request.version).items()
                             if name != "source-project.xml"})
            if host != "hosted-wasm":
                code = expected["Program.cs"].decode("utf-8-sig")
                code = "using Elsa.Studio.WorkflowContexts.Extensions;\n" + code
                receiver = "services" if host == "wasm" else "builder.Services"
                code = code.replace("// Build the application.", f"{receiver}.AddWorkflowContextsModule();\n\n// Build the application.")
                if host == "server":
                    code = code.replace('app.MapControllers();', 'app.MapControllers();\napp.MapGet("/_fixture/assemblies", () => RuntimeEvidence.LoadedAssemblies());')
                expected["Program.cs"] = code.encode()
            if host in ("wasm", "custom-elements"):
                # A public runtime configuration asset, with no account or token.
                expected["wwwroot/appsettings.json"] = b'{}\n'
        first = not directory.exists()
        if first:
            directory.mkdir(parents=True)
            packages.prepare_isolation(directory, sdk)
        for filename, content in expected.items():
            path = directory / filename
            require(path.parent.resolve().is_relative_to(group_root), "Materialized input escaped group")
            if first:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(content)
            else:
                require(path.is_file() and not path.is_symlink() and path.read_bytes() == content, "Shared project glue/config changed")
            hashes[str(path.relative_to(group_root))] = sha256(path)
        for filename in ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "global.json"):
            path = directory / filename
            require(path.is_file() and not path.is_symlink(), "Missing isolation input")
            expected_isolation = (json.dumps({"sdk": {"version": sdk, "rollForward": "disable"}}) + "\n"
                                  if filename == "global.json" else "<Project />\n")
            require(path.read_text() == expected_isolation, "Shared project isolation changed")
            hashes[str(path.relative_to(group_root))] = sha256(path)
        projects[host] = directory / name
    runtime = group_root / "runtime" / f"{request.host}-{secrets.token_hex(8)}"
    require(runtime.parent.resolve().is_relative_to(group_root), "Runtime state escaped group")
    runtime.mkdir(parents=True, mode=0o700)
    return CellLayout(request, group_root, projects, runtime, packages_root, sdk, hashes)


def isolated_environment(layout: CellLayout) -> dict[str, str]:
    require(not any(os.environ.get(name) for name in ("GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN")), "Token entered host execution")
    env = os.environ.copy()
    for key in tuple(env):
        if key.upper().startswith(("NUGET_", "MSBUILD", "DOTNET_")) or key.casefold().startswith(("fixture__", "fixture:")):
            del env[key]
    env.update(NUGET_PACKAGES=str(layout.packages_root), DOTNET_CLI_HOME=str(layout.group_root / "dotnet-home"),
               NUGET_HTTP_CACHE_PATH=str(layout.group_root / "http-cache"), NUGET_PLUGINS_CACHE_PATH=str(layout.group_root / "plugins-cache"),
               DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER="1", MSBUILDDISABLENODEREUSE="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")
    return env


def build(layout: CellLayout, *, converter_decoder: Path | None = None) -> list[dict]:
    clients = {host for host in layout.project_paths if host in ("wasm", "custom-elements")}
    require(not clients or converter_decoder is not None, "WASM build requires an explicitly prepared converter decoder")
    if clients:
        import paired_package_converter_selection as converters
    commands = []
    env = isolated_environment(layout)
    sdk_log = layout.group_root / "logs" / "execution-sdk.log"
    first_project = next(iter(layout.project_paths.values()))
    commands.append(packages._run_command(["dotnet", "--version"], first_project.parent, env, sdk_log, 60))
    require(sdk_log.read_text().strip() == layout.sdk, "Actual execution SDK differs from group pin")
    for host, project in layout.project_paths.items():
        stamp = project.parent / "build-reuse.json"
        input_hashes = {name: digest for name, digest in layout.input_hashes.items()
                        if name.startswith(str(project.parent.relative_to(layout.group_root)) + "/")}
        assets_path = project.parent / "obj" / "project.assets.json"
        output = project.parent / "bin" / "Release" / layout.request.framework
        if stamp.is_file() and not stamp.is_symlink():
            previous = json.loads(stamp.read_text())
            actual_output = {str(path.relative_to(output)): sha256(path) for path in output.rglob("*") if path.is_file()}
            if previous.get("inputs") == input_hashes and assets_path.is_file() and previous.get("assets") == sha256(assets_path) and actual_output and previous.get("outputs") == actual_output:
                record = {"stage": "reuse_verified_build", "project": host, "project_assets_sha256": sha256(assets_path)}
                if host in clients:
                    record["converter_selection"] = converters.verify_reused_selection(layout, project, converter_decoder, env)
                commands.append(record)
                continue
        # Hosted wrapper builds its already-reviewed fixture client edge.
        for phase, args in (("restore", ["restore", project.name, "--configfile", "NuGet.Config", "--packages", str(layout.packages_root), "--force-evaluate", "--no-cache"]),
                            ("build", ["build", project.name, "--no-restore", "--configuration", "Release", "-p:UseSharedCompilation=false"])):
            log = layout.group_root / "logs" / f"{host}-{phase}.log"
            command = ["dotnet", *args, "--nologo"]
            if host in clients and phase == "build":
                commands.append(converters.capture_build(layout, project, command, env, log, converter_decoder))
            else:
                commands.append(packages._run_command(command, project.parent, env, log, 1200))
        stamp.write_text(json.dumps({"schema": 1, "inputs": input_hashes, "assets": sha256(assets_path),
                                    "outputs": {str(path.relative_to(output)): sha256(path)
                                                for path in output.rglob("*") if path.is_file()}}, sort_keys=True) + "\n")
    return commands


def _free_port() -> int:
    with socket.socket() as listener:
        listener.bind(("127.0.0.1", 0))
        return listener.getsockname()[1]


def _wait_ready(process: subprocess.Popen, url: str, timeout: float) -> None:
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        require(process.poll() is None, "Owned host exited before readiness; inspect private process log")
        try:
            with urlopen(url, timeout=2) as response:
                if response.status == 200:
                    return
        except (URLError, TimeoutError):
            pass
        time.sleep(0.2)
    raise RuntimeError("Owned host readiness timed out")


def _validate_fixture_edges(layout: CellLayout, host: str, project: Path) -> None:
    references = ET.parse(project).findall(".//ProjectReference")
    expected = layout.project_paths.get("wasm") if host == "hosted-wasm" else None
    require(len(references) == (1 if expected else 0), "Unexpected source ProjectReference")
    if expected:
        require((project.parent / references[0].attrib["Include"]).resolve() == expected, "Source edge is not fixture-only WASM glue")
    assets = json.loads((project.parent / "obj" / "project.assets.json").read_text())
    require(isinstance(assets.get("targets"), dict) and bool(assets["targets"]), "Missing restored asset targets")
    for target in assets["targets"].values():
        for key, library in target.items():
            if library.get("type") == "project":
                require(expected is not None and key.split("/")[0] == expected.stem,
                        "Restored library uses source/project fallback")
                record = assets.get("libraries", {}).get(key, {})
                require((project.parent / record.get("msbuildProject", "")).resolve() == expected,
                        "Restored project edge escapes fixture glue")
            else:
                require(library.get("type") == "package", "Restored library is not a package")


@contextmanager
def start_pair(layout: CellLayout, *, validate_project: Callable[[Path], dict],
               timeout_seconds: float = 90, lifetime_seconds: float = 360) -> Iterator[RuntimeHandle]:
    require(callable(validate_project) and 0 < timeout_seconds <= 300, "Missing project verifier or invalid startup bound")
    require(os.name == "posix" and 0 < lifetime_seconds <= 600, "Invalid owned process lifetime/platform")
    for relative, digest in layout.input_hashes.items():
        path = layout.group_root / relative
        require(path.is_file() and not path.is_symlink() and path.resolve().is_relative_to(layout.group_root)
                and sha256(path) == digest, "Materialized input changed before launch")
    for host, project in layout.project_paths.items():
        require(not list(project.parent.glob("appsettings*.json")), "Unreviewed host settings entered fixture")
        _validate_fixture_edges(layout, host, project)
        evidence = validate_project(project)
        assets = project.parent / "obj" / "project.assets.json"
        require(evidence.get("project_assets_sha256") == sha256(assets), "Project provenance not bound to restored assets")
    backend_origin = f"http://127.0.0.1:{_free_port()}"
    studio_origin = f"http://127.0.0.1:{_free_port()}"
    require(backend_origin != studio_origin, "Backend and Studio origins collided")
    password = secrets.token_urlsafe(32)
    env = isolated_environment(layout)
    env.update(ASPNETCORE_ENVIRONMENT="Development", Fixture__RuntimeRoot=str(layout.runtime_root),
               Fixture__StudioOrigin=studio_origin, Fixture__Password=password,
               Fixture__PermissionProfile=layout.request.permission_profile,
               Fixture__WorkflowContexts=str("workflow-contexts" in layout.request.backend_features),
               Fixture__Secrets=str("secrets" in layout.request.backend_features),
               Identity__Tokens__SigningKey=secrets.token_urlsafe(48), Backend__Url=backend_origin + "/elsa/api",
               Authentication__Provider="ElsaIdentity", Localization__DefaultCulture="en-US",
               Hosting__ApiUrl=backend_origin + "/elsa/api")
    # This key and the exact grant array enter only the owned backend process.
    backend_env = env | {"Fixture__SecretsEncryptionKey": base64.b64encode(secrets.token_bytes(32)).decode("ascii"),
                         "Fixture__PermissionGrants": json.dumps(permission_grants(layout.request))}
    client = "wasm" if layout.request.host == "hosted-wasm" else layout.request.host
    public_config = None
    if client in ("wasm", "custom-elements"):
        public_config = layout.project_paths[client].parent / "wwwroot" / "appsettings.json"
        require(not public_config.is_symlink(), "Symlinked client runtime config")
        public_config.write_text(json.dumps({"Backend": {"Url": backend_origin + "/elsa/api"},
                                           "Authentication": {"Provider": "ElsaIdentity"},
                                           "Localization": {"DefaultCulture": "en-US", "SupportedCultures": ["en-US"]}}) + "\n")
    processes, logs, lifetime = [], [], None
    try:
        for host, origin, ready in (("backend", backend_origin, "/_fixture/ready"),
                                    (layout.request.host, studio_origin, "/")):
            project = layout.project_paths[host]
            log_path = layout.runtime_root / f"{host}-private.log"
            log = log_path.open("xb")
            log_path.chmod(0o600)
            logs.append(log)
            if host in ("wasm", "custom-elements"):
                command = ["dotnet", "run", "--project", str(project), "--no-build", "--no-restore", "--configuration", "Release", "--urls", origin]
            else:
                command = ["dotnet", str(project.parent / "bin" / "Release" / layout.request.framework / (project.stem + ".dll")), "--urls", origin]
            process = subprocess.Popen(command, cwd=project.parent, env=backend_env if host == "backend" else env,
                                       stdout=log, stderr=subprocess.STDOUT, start_new_session=True)
            processes.append(process)
            _wait_ready(process, origin + ready, timeout_seconds)
        def expire() -> None:
            for process in processes:
                if process.poll() is None:
                    try:
                        os.killpg(process.pid, signal.SIGKILL)
                    except ProcessLookupError:
                        pass
        lifetime = threading.Timer(lifetime_seconds, expire)
        lifetime.daemon = True
        lifetime.start()
        prefix = f"/{layout.request.route_prefix}/" if layout.request.route_prefix else "/"
        yield RuntimeHandle(layout.request, studio_origin + prefix, backend_origin + "/elsa/api", "paired-browser", password,
                            {"definition_name": "paired-browser-" + secrets.token_hex(6), "activity_value": "synthetic-browser-value"},
                            tuple(process.pid for process in processes))
    finally:
        if lifetime is not None:
            lifetime.cancel()
        for process in reversed(processes):
            if process.poll() is None:
                try:
                    os.killpg(process.pid, signal.SIGTERM)
                    process.wait(timeout=10)
                except (ProcessLookupError, subprocess.TimeoutExpired):
                    if process.poll() is None:
                        os.killpg(process.pid, signal.SIGKILL)
                    process.wait(timeout=10)
            else:
                process.wait(timeout=10)
        for log in logs:
            log.close()
        if public_config is not None:
            public_config.write_bytes(b'{}\n')
