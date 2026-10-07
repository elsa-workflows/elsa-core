#!/usr/bin/env python3
"""Disposable package-only Studio hosts and a real, owned Elsa backend.

This module materializes reviewed host glue. The caller supplies verified package
inputs and must validate each restored project before starting any listener.
RuntimeHandle is private in-memory data; never put its credentials in receipts or files.
"""
from __future__ import annotations

from contextlib import contextmanager
from dataclasses import dataclass, field, replace
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
from urllib.error import HTTPError, URLError
from urllib.request import urlopen
from xml.etree import ElementTree as ET

import prove_consolidated_package_consumers as packages
import run_paired_package_browser_matrix as browser_processes

FIXTURE = Path(__file__).resolve().parent / "paired-package-browser"
HOST_NAMES = {"server": "Server", "wasm": "Wasm", "hosted-wasm": "HostedWasm", "custom-elements": "CustomElements"}
VERSIONS = ("3.8.4", "3.9.0", "3.10.0")
PLATFORM_VERSIONS = {"net8.0": "8.0.24", "net9.0": "9.0.13", "net10.0": "10.0.3"}
BACKEND_PACKAGES = ("Elsa", "Elsa.Identity", "Elsa.Workflows.Api", "Elsa.Expressions.JavaScript", "Elsa.Persistence.EFCore.Sqlite", "Elsa.Bpmn.Interchange",
                    "Elsa.WorkflowContexts", "Elsa.Secrets", "Elsa.Secrets.Persistence.EFCore.Sqlite")
PERMISSION_PROFILES = ("full", "denied", "deny-secrets", "deny-workflow-contexts")
DESIGNER_MODES = ("x6", "react-flow")
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
    """A matrix cell with runtime variants; candidate designer mode adds no matrix identity."""

    host: str
    framework: str
    version: str
    backend_features: tuple[str, ...] = ("workflow-contexts", "secrets")
    permission_profile: str = "full"
    route_prefix: str = ""
    designer_mode: str = "x6"

    def __post_init__(self) -> None:
        require(self.host in HOST_NAMES and self.framework in packages.FRAMEWORKS and self.version in VERSIONS,
                "Unknown host/framework/version cell")
        require(type(self.backend_features) is tuple and len(set(self.backend_features)) == len(self.backend_features)
                and set(self.backend_features) <= {"workflow-contexts", "secrets"}, "Unknown or duplicate backend feature")
        require(self.permission_profile in PERMISSION_PROFILES, "Unknown permission profile")
        require(self.designer_mode in DESIGNER_MODES and
                (self.designer_mode == "x6" or self.version == "3.10.0"),
                "Unknown or noncandidate designer mode")
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
    if host in ("wasm", "custom-elements"):
        from paired_package_converter_selection import PACK_VERSION
        # Installed SDK servicing can change the default converter pack. Pin
        # the reviewed build tool before framework-reference resolution; the
        # existing runtime trace and archive checks still verify actual use.
        target = ET.SubElement(root, "Target", {
            "Name": "PinReviewedWebAssemblyConverterPack", "BeforeTargets": "ProcessFrameworkReferences"})
        tools = ET.SubElement(target, "ItemGroup")
        ET.SubElement(tools, "KnownWebAssemblySdkPack", {
            "Update": "Microsoft.NET.Sdk.WebAssembly.Pack", "WebAssemblySdkPackVersion": PACK_VERSION})
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
                registrations = [f"{receiver}.AddWorkflowContextsModule();"]
                if request.version == "3.10.0" and host in ("wasm", "custom-elements"):
                    # The accepted candidate exposes this public option; released host glue stays unchanged.
                    # Both modes use the same built fixture and select only through owned runtime configuration.
                    registrations.append(f'{receiver}.Configure<Elsa.Studio.Workflows.Designer.Options.DesignerOptions>(configuration.GetSection("DesignerOptions"));')
                require(code.count("// Build the application.") == 1, "Missing or ambiguous host registration boundary")
                code = code.replace("// Build the application.", "\n".join(registrations) + "\n\n// Build the application.")
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
        if key.upper().startswith(("NUGET_", "MSBUILD", "DOTNET_")) or key.casefold().startswith(("fixture__", "fixture:", "designeroptions__", "designeroptions:")):
            del env[key]
    env.update(NUGET_PACKAGES=str(layout.packages_root), DOTNET_CLI_HOME=str(layout.group_root / "dotnet-home"),
               NUGET_HTTP_CACHE_PATH=str(layout.group_root / "http-cache"), NUGET_PLUGINS_CACHE_PATH=str(layout.group_root / "plugins-cache"),
               DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER="1", MSBUILDDISABLENODEREUSE="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")
    return env


def _generated_static_assets(layout: CellLayout, host: str, project: Path) -> dict:
    """Pin the shared WASM client's generated bundle, or its verified absence."""
    if host != "wasm":
        return {}
    path = project.parent / "obj/Release" / layout.request.framework / "scopedcss/bundle/Elsa.Studio.Host.Wasm.styles.css"
    require(not any(part.is_symlink() for part in (path, *path.parents)), "Symlinked generated fixture asset")
    require(not path.exists() or path.is_file(), "Generated fixture asset is not a regular file")
    # The SDK emits this bundle only when it has scoped CSS inputs. Record the
    # same authority during standalone build so Hosted can reuse its converter proof.
    return {path.relative_to(project.parent).as_posix(): sha256(path)} if path.is_file() else {}


def build(layout: CellLayout, *, converter_decoder: Path | None = None,
          report_operation: Callable[[str, str], None] = lambda _component, _phase: None) -> list[dict]:
    clients = {host for host in layout.project_paths if host in ("wasm", "custom-elements")}
    require(not clients or converter_decoder is not None, "WASM build requires an explicitly prepared converter decoder")
    if clients:
        import paired_package_converter_selection as converters
    commands = []
    completed_projects = set()
    env = isolated_environment(layout)
    sdk_log = layout.group_root / "logs" / "execution-sdk.log"
    first_project = next(iter(layout.project_paths.values()))
    report_operation("sdk", "probe")
    commands.append(packages._run_command(["dotnet", "--version"], first_project.parent, env, sdk_log, 60))
    require(sdk_log.read_text().strip() == layout.sdk, "Actual execution SDK differs from group pin")
    for host, project in layout.project_paths.items():
        report_operation(host, "reuse_validation")
        stamp = project.parent / "build-reuse.json"
        input_hashes = {name: digest for name, digest in layout.input_hashes.items()
                        if name.startswith(str(project.parent.relative_to(layout.group_root)) + "/")}
        assets_path = project.parent / "obj" / "project.assets.json"
        output = project.parent / "bin" / "Release" / layout.request.framework
        if stamp.is_file() and not stamp.is_symlink():
            previous = json.loads(stamp.read_text())
            actual_output = {str(path.relative_to(output)): sha256(path) for path in output.rglob("*") if path.is_file()}
            generated = _generated_static_assets(layout, host, project)
            generated_verified = host != "wasm" or previous.get("generated_static_assets") == generated
            if previous.get("inputs") == input_hashes and assets_path.is_file() and previous.get("assets") == sha256(assets_path) and actual_output and previous.get("outputs") == actual_output and generated_verified:
                record = {"stage": "reuse_verified_build", "project": host, "project_assets_sha256": sha256(assets_path)}
                if host in clients:
                    record["converter_selection"] = converters.verify_reused_selection(layout, project, converter_decoder, env)
                commands.append(record)
                completed_projects.add(host)
                continue
        # The owned client is restored/built first. A recursive wrapper restore
        # would replace its project-local config provenance with the wrapper's.
        if host == "hosted-wasm":
            require("wasm" in completed_projects, "Hosted wrapper requires its completed client build")
        for phase, args in (("restore", ["restore", project.name, "--configfile", "NuGet.Config", "--packages", str(layout.packages_root), "--force-evaluate", "--no-cache"]),
                            ("build", ["build", project.name, "--no-restore", "--configuration", "Release", "-p:UseSharedCompilation=false"])):
            log = layout.group_root / "logs" / f"{host}-{phase}.log"
            command = ["dotnet", *args, *(["--no-dependencies"] if host == "hosted-wasm" else []), "--nologo"]
            report_operation(host, phase)
            if host in clients and phase == "build":
                commands.append(converters.capture_build(layout, project, command, env, log, converter_decoder,
                                                         report_operation=report_operation))
            else:
                commands.append(packages._run_command(command, project.parent, env, log, 1200))
        stamp.write_text(json.dumps({"schema": 1, "inputs": input_hashes, "assets": sha256(assets_path),
                                    "generated_static_assets": _generated_static_assets(layout, host, project),
                                    "outputs": {str(path.relative_to(output)): sha256(path)
                                                for path in output.rglob("*") if path.is_file()}}, sort_keys=True) + "\n")
        completed_projects.add(host)
    return commands


def _free_port() -> int:
    with socket.socket() as listener:
        listener.bind(("127.0.0.1", 0))
        return listener.getsockname()[1]


def _wait_ready(process: subprocess.Popen, url: str, timeout: float, *,
                report_status: Callable[[int], None] | None = None) -> None:
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        require(process.poll() is None, "Owned host exited before readiness; inspect private process log")
        try:
            with urlopen(url, timeout=2) as response:
                if report_status is not None:
                    report_status(response.status)
                if response.status == 200:
                    return
        except HTTPError as error:
            try:
                if report_status is not None:
                    report_status(error.code)
            finally:
                error.close()
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
               timeout_seconds: float = 90, lifetime_seconds: float = 360,
               report_startup: Callable[[str, str], None] | None = None,
               report_readiness_status: Callable[[str, int], None] | None = None) -> Iterator[RuntimeHandle]:
    if report_startup is not None:
        report_startup("pair", "validation")
    require(callable(validate_project) and 0 < timeout_seconds <= 300, "Missing project verifier or invalid startup bound")
    require(os.name == "posix" and 0 < lifetime_seconds <= 600, "Invalid owned process lifetime/platform")
    _validate_launch(layout, validate_project)
    if report_startup is not None:
        report_startup("pair", "configuration")
    backend_origin, studio_origin, password, env, backend_env = _runtime_environment(layout)
    public_config = _public_configuration(layout, env, backend_origin)
    processes, logs, lifetime = [], [], None
    try:
        for host, origin, ready in (("backend", backend_origin, "/_fixture/ready"),
                                    (layout.request.host, studio_origin, "/")):
            log_path = layout.runtime_root / f"{host}-private.log"
            if report_startup is not None:
                report_startup(host, "launch")
            process, log = _launch_host(layout, host, origin, backend_env if host == "backend" else env, log_path)
            processes.append(process)
            logs.append(log)
            if report_startup is not None:
                report_startup(host, "readiness")
            if report_readiness_status is None:
                _wait_ready(process, origin + ready, timeout_seconds)
            else:
                _wait_ready(process, origin + ready, timeout_seconds,
                            report_status=lambda status: report_readiness_status(host, status))
        def expire() -> None:
            for process in processes:
                _kill_process(process)
        lifetime = threading.Timer(lifetime_seconds, expire)
        lifetime.daemon = True
        lifetime.start()
        yield _runtime_handle(layout, studio_origin, backend_origin, password, processes)
    finally:
        if lifetime is not None:
            lifetime.cancel()
        try:
            _stop_all(processes)
        finally:
            for log in logs:
                log.close()
            if public_config is not None:
                public_config.write_bytes(b'{}\n')


def _validate_launch(layout: CellLayout, validate_project: Callable[[Path], dict]) -> None:
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


def _runtime_environment(layout: CellLayout):
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
    return backend_origin, studio_origin, password, env, backend_env


def _public_configuration(layout: CellLayout, env: dict, backend_origin: str) -> Path | None:
    if layout.request.version == "3.10.0":
        env["DesignerOptions__UseReactFlow"] = str(layout.request.designer_mode == "react-flow").lower()
    client = "wasm" if layout.request.host == "hosted-wasm" else layout.request.host
    public_config = None
    if client in ("wasm", "custom-elements"):
        public_config = layout.project_paths[client].parent / "wwwroot" / "appsettings.json"
        require(not public_config.is_symlink(), "Symlinked client runtime config")
        settings = {"Backend": {"Url": backend_origin + "/elsa/api"},
                    "Authentication": {"Provider": "ElsaIdentity"},
                    "Localization": {"DefaultCulture": "en-US", "SupportedCultures": ["en-US"]}}
        if layout.request.version == "3.10.0":
            settings["DesignerOptions"] = {"UseReactFlow": layout.request.designer_mode == "react-flow"}
        public_config.write_text(json.dumps(settings) + "\n")
    return public_config


def _launch_host(layout: CellLayout, host: str, origin: str, env: dict, log_path: Path):
    project = layout.project_paths[host]
    descriptor = os.open(log_path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    log = os.fdopen(descriptor, "wb")
    try:
        if host in ("wasm", "custom-elements"):
            command = ["dotnet", "run", "--project", str(project), "--no-build", "--no-restore", "--configuration", "Release", "--urls", origin]
        else:
            command = ["dotnet", str(project.parent / "bin" / "Release" / layout.request.framework / (project.stem + ".dll")), "--urls", origin]
        return subprocess.Popen(command, cwd=project.parent, env=env, stdout=log,
                                stderr=subprocess.STDOUT, start_new_session=True), log
    except BaseException:
        log.close()
        raise


def _kill_process(process) -> None:
    if process.poll() is None:
        try:
            os.killpg(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass


def _stop_process(process) -> None:
    if process.poll() is None:
        try:
            os.killpg(process.pid, signal.SIGTERM)
            process.wait(timeout=10)
        except (ProcessLookupError, subprocess.TimeoutExpired):
            _kill_process(process)
            process.wait(timeout=10)
    else:
        process.wait(timeout=10)


def _stop_all(processes, *, stop=None) -> None:
    error = None
    for process in reversed(tuple(processes)):
        try:
            (stop or _stop_process)(process)
        except BaseException as failure:
            error = failure
        else:
            processes.remove(process)
    if error is not None:
        raise error


def _runtime_handle(layout, studio_origin, backend_origin, password, processes, safe_ids=None):
    prefix = f"/{layout.request.route_prefix}/" if layout.request.route_prefix else "/"
    return RuntimeHandle(layout.request, studio_origin + prefix, backend_origin + "/elsa/api", "paired-browser", password,
                         dict(safe_ids) if safe_ids is not None else {"definition_name": "paired-browser-" + secrets.token_hex(6), "activity_value": "synthetic-browser-value"},
                         tuple(process.pid for process in processes))


def _build_identity(layout: CellLayout) -> dict:
    """Bind the existing build stamps to actual files, then freeze them across phases."""
    identity = {}
    for host, project in layout.project_paths.items():
        stamp = project.parent / "build-reuse.json"
        output = project.parent / "bin" / "Release" / layout.request.framework
        assets = project.parent / "obj" / "project.assets.json"
        for path in (stamp, assets, output):
            require(path.exists() and path.resolve().is_relative_to(layout.group_root) and
                    not any(parent.is_symlink() for parent in (path, *path.parents)), "Unsafe phase build path")
        previous = json.loads(stamp.read_text())
        require(previous.get("schema") == 1, "Missing verified phase build stamp")
        inputs = {name: digest for name, digest in layout.input_hashes.items()
                  if name.startswith(str(project.parent.relative_to(layout.group_root)) + "/")}
        paths = list(output.rglob("*"))
        require(not any(path.is_symlink() for path in paths), "Symlinked phase build output")
        outputs = {str(path.relative_to(output)): sha256(path) for path in paths if path.is_file()}
        generated = _generated_static_assets(layout, host, project)
        require(outputs and previous.get("inputs") == inputs and previous.get("assets") == sha256(assets)
                and previous.get("outputs") == outputs and
                (host != "wasm" or previous.get("generated_static_assets") == generated), "Phase build identity differs from verified build")
        identity[host] = {"stamp": sha256(stamp), "assets": sha256(assets), "outputs": outputs,
                          "generated_static_assets": generated}
    return identity


class _DesignerPhases:
    """Private process owner; callers retain only sanitized observations, never this object."""

    def __init__(self, layout, validate_project, timeout_seconds, lifetime_seconds):
        self.layout = replace(layout, input_hashes=dict(layout.input_hashes), project_paths=dict(layout.project_paths))
        require(layout.runtime_root.is_dir() and layout.runtime_root.resolve() == layout.runtime_root and
                layout.runtime_root.is_relative_to(layout.group_root), "Unsafe private designer runtime root")
        self.validate_project = validate_project
        self.timeout_seconds = timeout_seconds
        _validate_launch(self.layout, validate_project)
        self.build_identity = _build_identity(self.layout)
        self.backend_origin, self.studio_origin, self.password, self.env, self.backend_env = _runtime_environment(self.layout)
        self.safe_ids = {"definition_name": "paired-browser-" + secrets.token_hex(6), "activity_value": "synthetic-browser-value"}
        self.processes, self.logs, self.births = [], [], {}
        self.lock = threading.RLock()
        self.expired = threading.Event()
        self.closed = False
        self.cleanup_failed = False
        self.active = False
        self.completed = 0
        self.timer = threading.Timer(lifetime_seconds, self._expire)
        self.timer.daemon = True
        self.public_config = None
        self.deadline = None

    def _stop_owned(self, process):
        started = self.births.get(process.pid)
        if started is None:
            # Popen owns the direct child, but no descendants can be certified.
            if process.poll() is None:
                process.kill()
            process.wait(timeout=10)
            raise RuntimeError("Designer descendant ownership unavailable")
        browser_processes._cleanup_browser_process(process, started)
        # Read-only group check after birth-guarded descendant cleanup. Never
        # signal a PGID after its root/descendant ownership has disappeared.
        import paired_package_converter_selection as converters
        require(converters._wait_group_empty(process.pid), "Designer group cleanup unverified")

    def _expire(self):
        with self.lock:
            self.expired.set()
            try:
                _stop_all(self.processes, stop=self._stop_owned)
            except BaseException:
                self.cleanup_failed = True

    def _launch(self, layout, host, origin, env, log_name, ready):
        with self.lock:
            require(not self.expired.is_set() and not self.closed and time.monotonic() < self.deadline,
                    "Designer phase owner expired or closed")
            process, log = _launch_host(layout, host, origin, env, layout.runtime_root / log_name)
            self.processes.append(process)
            self.logs.append(log)
            info = browser_processes._browser_process_info(process.pid)
            require(info is not None and not info[2], "Designer process birth identity unavailable")
            self.births[process.pid] = info[1]
        _wait_ready(process, origin + ready, self.timeout_seconds)
        return process

    def _healthy(self):
        require(not self.closed and not self.cleanup_failed and not self.expired.is_set() and time.monotonic() < self.deadline and self.backend.poll() is None,
                "Designer phase backend exited or owner expired")

    @contextmanager
    def phase(self, designer_mode: str) -> Iterator[RuntimeHandle]:
        try:
            self._healthy()
            require(not self.active and self.completed < 2 and designer_mode == DESIGNER_MODES[self.completed],
                    "Designer phases must run X6 then React exactly once")
            self.active = True
            layout = replace(self.layout, request=replace(self.layout.request, designer_mode=designer_mode))
            _validate_launch(layout, self.validate_project)
            require(_build_identity(layout) == self.build_identity, "Verified build changed between designer phases")
            env = dict(self.env)
            self.public_config = _public_configuration(layout, env, self.backend_origin)
            studio = self._launch(layout, layout.request.host, self.studio_origin, env,
                                  f"{layout.request.host}-{designer_mode}-private.log", "/")
            self._healthy()
            try:
                yield _runtime_handle(layout, self.studio_origin, self.backend_origin, self.password,
                                      [self.backend, studio], self.safe_ids)
                self._healthy()
                require(studio.poll() is None, "Designer phase Studio exited")
            finally:
                with self.lock:
                    if studio in self.processes:
                        self._stop_owned(studio)
                        self.processes.remove(studio)
                if self.public_config is not None:
                    self.public_config.write_bytes(b'{}\n')
                    self.public_config = None
            self.completed += 1
            self.active = False
        except BaseException:
            self.close()
            raise

    def close(self):
        with self.lock:
            self.closed = True
        self.timer.cancel()
        try:
            with self.lock:
                _stop_all(self.processes, stop=self._stop_owned)
            require(not self.cleanup_failed, "Designer watchdog cleanup unverified")
        finally:
            for log in self.logs:
                log.close()
            if self.public_config is not None:
                self.public_config.write_bytes(b'{}\n')
                self.public_config = None


@contextmanager
def start_designer_phases(layout: CellLayout, *, validate_project: Callable[[Path], dict],
                          timeout_seconds: float = 90, lifetime_seconds: float = 600) -> Iterator[_DesignerPhases]:
    """Run candidate X6 then React against one live backend and unchanged build.

    Each ``with owner.phase(mode)`` supplies private RuntimeHandle data for a fresh
    browser session. It reaps Studio before the next phase; the backend, database,
    keys, credentials and CORS origins survive until the outer context exits.
    No browser proof or portable receipt is produced by this lifecycle helper.
    """
    require(layout.request.version == "3.10.0" and layout.request.designer_mode == "x6" and
            layout.request.host in HOST_NAMES, "Unsupported designer phase cell")
    require(callable(validate_project) and 0 < timeout_seconds <= 300 and os.name == "posix" and
            0 < lifetime_seconds <= 600, "Invalid designer phase verifier or lifetime")
    owner = _DesignerPhases(layout, validate_project, timeout_seconds, lifetime_seconds)
    try:
        owner.deadline = time.monotonic() + lifetime_seconds
        owner.timer.start()
        owner.backend = owner._launch(layout, "backend", owner.backend_origin, owner.backend_env,
                                      "backend-designer-phases-private.log", "/_fixture/ready")
        yield owner
    finally:
        owner.close()


class _OptionalFeatureRuntime:
    """Private one-shot backend stop capability, bound to captured process objects."""

    def __init__(self, owner, studio, handle):
        self.owner, self.studio, self.handle = owner, studio, handle
        self.disconnected = False

    def check_live_studio(self):
        owner = self.owner
        require(not owner.closed and not owner.cleanup_failed and not owner.expired.is_set()
                and time.monotonic() < owner.deadline and self.studio.poll() is None,
                "Optional feature Studio exited or owner expired")
        if not self.disconnected:
            owner._healthy()

    def disconnect_backend(self) -> dict:
        # The parent calls this only after its private browser readiness handshake.
        # The watchdog and final cleanup share this lock and the same ownership set.
        with self.owner.lock:
            require(not self.disconnected, "Optional backend disconnect is one-shot")
            self.check_live_studio()
            backend = self.owner.backend
            self.owner._stop_owned(backend)
            require(backend.poll() is not None, "Owned backend stop was not observed")
            self.owner.processes.remove(backend)
            self.disconnected = True
            self.check_live_studio()
            return {"owned_backend_stopped": True, "studio_alive_after_stop": True}


@contextmanager
def start_optional_feature_probe(layout: CellLayout, *, validate_project: Callable[[Path], dict],
                                 timeout_seconds: float = 90, lifetime_seconds: float = 360):
    """Reuse a verified candidate build with fresh private state for one probe.

    The existing owner supplies birth-guarded launch, watchdog and cleanup. This
    scope never enters its two-designer sequence: a deliberate backend stop is
    valid here, while remaining a failure in a normal designer phase.
    """
    require(layout.runtime_root.is_dir() and not any(layout.runtime_root.iterdir()),
            "Optional feature probe requires a fresh empty runtime root")
    with start_designer_phases(layout, validate_project=validate_project,
                               timeout_seconds=timeout_seconds, lifetime_seconds=lifetime_seconds) as owner:
        env = dict(owner.env)
        owner.public_config = _public_configuration(layout, env, owner.backend_origin)
        studio = owner._launch(layout, layout.request.host, owner.studio_origin, env,
                               layout.request.host + "-optional-private.log", "/")
        handle = _runtime_handle(layout, owner.studio_origin, owner.backend_origin, owner.password,
                                 [owner.backend, studio], owner.safe_ids)
        probe = _OptionalFeatureRuntime(owner, studio, handle)
        probe.check_live_studio()
        yield probe
        probe.check_live_studio()
