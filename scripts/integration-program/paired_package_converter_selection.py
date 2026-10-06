"""Observe the converter actually loaded by an owned, isolated WASM build.

Raw EventPipe/decoder output stays private. Only the reviewed policy tuple and
allowlisted hashes leave this module. An SDK name or adjacent DLL is not selection
evidence. The supported capture platform is POSIX with startup EventPipe tracing.
"""
from __future__ import annotations

import base64
import hashlib
import json
import os
from pathlib import Path
import re
import secrets
import signal
import subprocess
import time
from zipfile import ZipFile

import paired_package_provenance as provenance
import prove_consolidated_package_consumers as packages
import verify_browser_package_resources as resources
from run_persisted_workflow_upgrade_fixture import safe_members

FIXTURE = Path(__file__).parent / "paired-package-browser" / "converter-selection"
PACK_ID = "microsoft.net.sdk.webassembly.pack"
PACK_VERSION = "10.0.8"
ARCHIVE_SHA256 = "73a7820139c952a9c8c22197d83a87540f1af38d4ae411c3ec97a1908b87cd23"
TASK = "Microsoft.NET.Sdk.WebAssembly.Pack.Tasks"
IMPLEMENTATION = "Microsoft.NET.WebAssembly.Webcil"
CORELIB = "System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"
TRACE = re.compile(r"runtime-([1-9][0-9]*)\.nettrace")
require = provenance.require
sha256 = provenance.sha256
regular_file = provenance.regular_file


def _write_private(path: Path, value: dict) -> None:
    with path.open("x", encoding="utf-8") as target:
        path.chmod(0o600)
        json.dump(value, target, sort_keys=True)
        target.write("\n")


def _private_directory(path: Path) -> Path:
    aliases = {Path("/tmp"): Path("/private/tmp"), Path("/var"): Path("/private/var")}
    require(path.is_absolute() and all(not part.is_symlink() or aliases.get(part) == part.resolve()
                                     for part in (path, *path.parents)),
            "Symlink in converter private directory")
    path.mkdir(parents=True, mode=0o700, exist_ok=False)
    path.chmod(0o700)
    return path.resolve()


def _files(root: Path) -> dict[str, str]:
    return {path.relative_to(root).as_posix(): sha256(regular_file(path))
            for path in sorted(root.rglob("*")) if path.is_file() or path.is_symlink()}


def _decoder_dependencies(root: Path, output: Path | None) -> None:
    lock = json.loads(regular_file(root / "packages.lock.json").read_text())["dependencies"]["net10.0"]
    pins = json.loads(regular_file(root / "dependency-archives.json").read_text())["packages"]
    require(set(lock) == set(pins), "Decoder dependency inventory differs from reviewed archives")
    assets = json.loads(regular_file(root / "obj/project.assets.json").read_text())
    require(set(assets["targets"]) == {"net10.0"} and
            set(assets["targets"]["net10.0"]) == {name + "/" + entry["resolved"] for name, entry in lock.items()},
            "Decoder restored dependency graph differs from lock")
    expected_libraries = {name + "/" + entry["resolved"]: name.lower() + "/" + entry["resolved"]
                          for name, entry in lock.items()}
    require(assets.get("packageFolders") == {str(root.resolve() / "packages"): {}} and
            isinstance(assets.get("libraries"), dict) and set(assets["libraries"]) == set(expected_libraries) and
            all(isinstance(assets["libraries"][name], dict) and
                assets["libraries"][name].get("type") == "package" and
                assets["libraries"][name].get("path") == path for name, path in expected_libraries.items()),
            "Decoder restored package resolution differs from isolated cache")
    expected_runtime = {}
    for name, entry in lock.items():
        pin = pins[name]
        package = root / "packages" / name.lower() / entry["resolved"]
        archive = regular_file(package / f'{name.lower()}.{entry["resolved"]}.nupkg')
        data = archive.read_bytes()
        require(pin["version"] == entry["resolved"] and sha256(archive) == pin["sha256"] and len(data) == pin["bytes"]
                and base64.b64encode(hashlib.sha512(data).digest()).decode() == pin["sha512"],
                "Decoder dependency differs from official signed archive bytes")
        require(json.loads(regular_file(package / ".nupkg.metadata").read_text()).get("contentHash") == entry["contentHash"],
                "Decoder NuGet content hash differs from locked restore")
        library = assets["targets"]["net10.0"][name + "/" + entry["resolved"]]
        require(library.get("type") == "package", "Decoder dependency uses source fallback")
        require(set(library) <= {"type", "dependencies", "compile", "runtime", "build"},
                "Decoder dependency contains unreviewed asset kinds")
        selected = pin.get("selected_assets")
        require(isinstance(selected, dict) and set(selected) == {"compile", "runtime", "build"} and
                all(isinstance(members, list) and all(isinstance(member, str) for member in members) and
                    members == sorted(set(members)) for members in selected.values()),
                "Decoder selected dependency inventory is not pinned")
        require(all(isinstance(library.get(kind, {}), dict) and
                    sorted(library.get(kind, {})) == members for kind, members in selected.items()),
                "Decoder selected dependency inventory differs from reviewed assets")
        runtime = {member for member in selected["runtime"] if not member.endswith("/_._")}
        if runtime:
            expected_runtime[name + "/" + entry["resolved"]] = runtime
        with ZipFile(archive) as zipped:
            safe_members(zipped)
            for kind, members in selected.items():
                for member in members:
                    body = zipped.read(member)
                    require(regular_file(package / member).read_bytes() == body,
                            "Decoder selected dependency differs from reviewed archive member")
                    if output is not None and kind == "runtime" and not member.endswith("/_._"):
                        require(regular_file(output / Path(member).name).read_bytes() == body,
                                "Decoder loaded runtime dependency differs from reviewed archive member")
    if output is not None:
        deps = json.loads(regular_file(output / "Decoder.deps.json").read_text())
        target_name = ".NETCoreApp,Version=v10.0"
        require(deps.get("runtimeTarget", {}).get("name") == target_name and
                set(deps.get("targets", {})) == {target_name}, "Decoder runtime target differs")
        target = deps["targets"][target_name]
        expected_runtime["Decoder/1.0.0"] = {"Decoder.dll"}
        require(set(target) == set(expected_runtime) and all(
            isinstance(value, dict) and set(value) <= {"dependencies", "runtime"} and
            isinstance(value.get("runtime"), dict) and set(value["runtime"]) == expected_runtime[name]
            for name, value in target.items()), "Decoder runtime closure differs from reviewed assets")


def prepare_decoder(root: Path, sdk: str, environment: dict[str, str]) -> Path:
    """Build the small locked decoder once per run; reuse only exact source/output hashes.

    root is independent of all version/TFM package caches. Normal `dotnet` keeps
    the machine's build-slot wrapper in control. No Elsa package is built here.
    """
    root = root.absolute()
    require(not root.resolve().is_relative_to(Path(__file__).resolve().parents[2]),
            "Converter decoder must live outside repository imports")
    cache = environment.get("NUGET_PACKAGES")
    require(not cache or not root.resolve().is_relative_to(Path(cache).resolve()),
            "Converter decoder must not mutate a fixture package cache")
    require(not any(environment.get(key) for key in ("GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN")),
            "Token entered converter decoder preparation")
    expected = {path.name: sha256(regular_file(path)) for path in FIXTURE.iterdir()}
    expected["sdk"] = sdk
    stamp = root / "decoder-build.json"
    output = root / "bin" / "Release" / "net10.0"
    if stamp.exists():
        previous = json.loads(regular_file(stamp).read_text())
        require(previous.get("inputs") == expected and previous.get("outputs") == _files(output)
                and previous.get("restored_assets_sha256") == sha256(regular_file(root / "obj/project.assets.json"))
                and all(sha256(regular_file(root / name)) == digest for name, digest in expected.items() if name != "sdk"),
                "Decoder source or built output changed")
        _decoder_dependencies(root, output)
        return regular_file(output / "Decoder.dll")
    root = _private_directory(root)
    stamp = root / "decoder-build.json"
    output = root / "bin" / "Release" / "net10.0"
    for name in expected:
        if name != "sdk":
            (root / name).write_bytes((FIXTURE / name).read_bytes())
    (root / "global.json").write_text(json.dumps({"sdk": {"version": sdk, "rollForward": "disable"}}))
    (root / "NuGet.Config").write_text('<configuration><packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources><packageSourceMapping><clear /><packageSource key="nuget.org"><package pattern="*" /></packageSource></packageSourceMapping></configuration>')
    env = {key: value for key, value in environment.items() if not key.upper().startswith(("NUGET_", "DOTNET_", "MSBUILD"))}
    env.update(NUGET_PACKAGES=str(root / "packages"), DOTNET_CLI_HOME=str(root / "dotnet-home"),
               NUGET_HTTP_CACHE_PATH=str(root / "http-cache"), NUGET_PLUGINS_CACHE_PATH=str(root / "plugins-cache"),
               DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER="1", MSBUILDDISABLENODEREUSE="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")
    for phase, args in (("restore", ["restore", "--locked-mode", "--configfile", "NuGet.Config"]),
                        ("build", ["build", "--no-restore", "--configuration", "Release", "-p:UseSharedCompilation=false"])):
        log = root / (phase + "-private.log")
        _owned_command(["dotnet", *args, "--disable-build-servers", "--nologo",
                               "-p:ImportDirectoryBuildProps=false", "-p:ImportDirectoryBuildTargets=false",
                               "-p:ImportDirectoryPackagesProps=false"], root, env, log, 180)
        if phase == "restore":
            # Verify compiler and imported build inputs before the decoder build consumes them.
            _decoder_dependencies(root, None)
            restored_assets_sha256 = sha256(regular_file(root / "obj/project.assets.json"))
    require(sha256(regular_file(root / "obj/project.assets.json")) == restored_assets_sha256,
            "Decoder restored assets changed during build")
    _decoder_dependencies(root, output)
    _write_private(stamp, {"schema": 2, "inputs": expected, "outputs": _files(output),
                           "restored_assets_sha256": restored_assets_sha256})
    return regular_file(output / "Decoder.dll")


def _group_members(pgid: int) -> list[int]:
    result = subprocess.run(["ps", "-axo", "pid=,pgid=,stat="], capture_output=True, text=True, check=True)
    return [int(fields[0]) for line in result.stdout.splitlines() if len(fields := line.split()) == 3
            and int(fields[1]) == pgid and "Z" not in fields[2]]


def _terminate(pgid: int) -> None:
    try:
        os.killpg(pgid, signal.SIGKILL)
    except ProcessLookupError:
        pass


def _wait_group_empty(pgid: int) -> bool:
    deadline = time.monotonic() + 10
    while _group_members(pgid):
        if time.monotonic() >= deadline:
            return False
        time.sleep(0.1)
    return True


def _owned_command(command: list[str], cwd: Path, environment: dict, log: Path, timeout: int) -> dict:
    """Wait for the wrapper AND its whole group, even when the parent exits early."""
    require(os.name == "posix", "Converter capture requires POSIX owned process groups")
    with log.open("xb") as target:
        log.chmod(0o600)
        process = subprocess.Popen(command, cwd=cwd, env=environment, stdout=target,
                                   stderr=subprocess.STDOUT, start_new_session=True)
        try:
            code = process.wait(timeout=timeout)
            require(_wait_group_empty(process.pid), "Owned converter command retained live descendants")
            require(code == 0, "Owned converter command failed; inspect private diagnostics")
        finally:
            _terminate(process.pid)
            process.wait()
            require(_wait_group_empty(process.pid), "Converter command cleanup retained live descendants")
    return {"command": command, "exit_code": code, "log": str(log), "group_quiescent": True}


def _trace_inventory(directory: Path) -> list[dict]:
    records = []
    for path in sorted(directory.iterdir()):
        match = TRACE.fullmatch(path.name)
        require(match is not None, "Unexpected converter trace file")
        path = regular_file(path)
        require(path.stat().st_size > 0, "Empty converter trace")
        path.chmod(0o600)
        owner = subprocess.run(["ps", "-p", match[1], "-o", "pid="], capture_output=True)
        require(owner.returncode in (0, 1), "Could not verify converter trace owner exit")
        require(not owner.stdout.strip(),
                "A converter trace owner is still running")
        records.append({"file": path.name, "sha256": sha256(path), "bytes": path.stat().st_size})
    require(bool(records), "No actual converter runtime trace was produced")
    return records


def verify_decoded(decoded: dict, inventory: list[dict], package_cache: Path, sdk: str) -> dict:
    """Pure selection check plus exact archive/cache binding; raw paths never returned."""
    require(decoded.get("schema") == 1 and isinstance(decoded.get("traces"), list)
            and isinstance(decoded.get("records"), list), "Malformed converter decoder output")
    by_name = {row["file"]: row for row in inventory}
    require(len(by_name) == len(inventory) and inventory, "Missing or duplicate trace inventory")
    traces = decoded["traces"]
    require(len(traces) == len(inventory) and {row.get("file") for row in traces} == set(by_name),
            "Decoder trace inventory differs")
    for row in traces:
        require(row.get("sha256") == by_name[row["file"]]["sha256"] and row.get("parser_completed") is True
                and type(row.get("events_lost")) is int and row["events_lost"] == 0,
                "Converter trace is incomplete, changed or lost events")
    successes = {TASK: [], IMPLEMENTATION: []}
    owners = set()
    for row in decoded["records"]:
        trace = row.get("trace")
        require(trace in by_name and row.get("trace_sha256") == by_name[trace]["sha256"]
                and type(row.get("pid")) is int and TRACE.fullmatch(trace)
                and row["pid"] == int(TRACE.fullmatch(trace)[1]), "Converter event trace/owner differs")
        require(row.get("provider") == "Microsoft-Windows-DotNETRuntime" and row.get("event_id") == 291
                and row.get("event_name") == "AssemblyLoader/Stop" and type(row.get("success")) is bool,
                "Malformed converter runtime event")
        require(all(isinstance(row.get(field), str) for field in ("assembly", "requestor", "context", "requestor_context")),
                "Missing converter runtime binding fields")
        if not row["success"]:
            continue  # Unsuccessful bind probes do not establish a loaded result.
        name = (row.get("result") or "").split(",", 1)[0]
        require(name in successes and row["assembly"] == row["result"] and row["context"],
                "Successful converter identity differs")
        successes[name].append(row)
        owners.add((trace, row["pid"]))
    require(len(owners) == 1 and all(successes.values()), "Missing, split or multiple converter owners")
    selected = {}
    for name, rows in successes.items():
        identities = {(row["result"], row.get("path"), row["context"]) for row in rows}
        require(len(identities) == 1, "Conflicting successful converter identities/paths/contexts")
        selected[name] = rows[0]
    task, implementation = selected[TASK], selected[IMPLEMENTATION]
    require(task["context"] == implementation["context"], "Converter task/implementation contexts differ")
    edge = lambda row: row["requestor"] == task["result"] and row["requestor_context"] == task["context"]
    require(any(edge(row) for row in successes[IMPLEMENTATION]), "No actual task to converter bind was observed")
    for name, rows in successes.items():
        for row in rows:
            core_path_load = row["requestor"] == CORELIB and row["requestor_context"] == "Default" and row.get("requested_path") == row.get("path")
            require(core_path_load or (name == IMPLEMENTATION and edge(row) and not row.get("requested_path")),
                    "Unreviewed converter requestor binding")
    archive = regular_file(package_cache.absolute() / PACK_ID / PACK_VERSION / f"{PACK_ID}.{PACK_VERSION}.nupkg")
    root = archive.parent
    require(sha256(archive) == ARCHIVE_SHA256, "Selected converter archive is not reviewed")
    hashes = {}
    with ZipFile(archive) as zipped:
        members = safe_members(zipped)
        for name, row in selected.items():
            member = "tools/net10.0/" + name + ".dll"
            expected = root / member
            require(isinstance(row.get("path"), str) and Path(row["path"]).is_absolute()
                    and ".." not in Path(row["path"]).parts and regular_file(Path(row["path"])) == expected,
                    "Actual converter load escaped reviewed isolated package path")
            require(sum(entry.filename == member for entry in members) == 1
                    and regular_file(expected).read_bytes() == zipped.read(member), "Loaded converter differs from original archive member")
            hashes[name] = sha256(expected)
    require(sha256(archive) == ARCHIVE_SHA256, "Converter archive changed during verification")
    policy = json.loads(resources.CONVERTER_POLICY.read_text())["converters"]
    pin = policy.get(hashes[TASK], {})
    require(pin.get("sdk_version") == sdk and pin.get("implementation_sha256") == hashes[IMPLEMENTATION]
            and re.fullmatch(r"[0-9a-f]{64}", pin.get("source_sha256", "")), "Actual selected converter is not approved")
    return {"schema": 1, "converter": {"sdk_version": sdk, "task_sha256": hashes[TASK],
            "implementation_sha256": hashes[IMPLEMENTATION], "source_sha256": pin["source_sha256"]},
            "pack": {"id": "Microsoft.NET.Sdk.WebAssembly.Pack", "version": PACK_VERSION, "archive_sha256": ARCHIVE_SHA256},
            "traces": inventory, "successful_load_records": sum(map(len, successes.values())),
            "unique_task_identity": 1, "unique_implementation_identity": 1,
            "same_trace_process": True, "actual_task_requestor_edge": True,
            "context_scope": "reported_target_bind_context"}


def _decode(decoder: Path, capture: Path, inventory: list[dict], package_cache: Path, sdk: str, environment: dict) -> dict:
    # prepare_decoder verifies the reusable binary against its source/build hashes.
    decoder = prepare_decoder(decoder.parents[3], sdk, environment)
    output = capture / ("decoded-" + secrets.token_hex(8) + "-private.json")
    env = {key: value for key, value in environment.items() if "EVENTPIPE" not in key.upper()}
    command = ["dotnet", str(decoder), *(str(capture / "traces" / row["file"]) for row in inventory)]
    _owned_command(command, capture, env, output, 120)
    require(output.stat().st_size <= 4 * 1024 * 1024, "Converter decoder output exceeded bound")
    result = verify_decoded(json.loads(output.read_text()), inventory, package_cache, sdk)
    require(_trace_inventory(capture / "traces") == inventory, "Converter traces changed during decode")
    result["decoder"] = {"assembly_sha256": sha256(decoder),
                         "source_files_sha256": {path.name: sha256(regular_file(path)) for path in FIXTURE.iterdir()}}
    result["selection_verifier_sha256"] = sha256(Path(__file__))
    return result


def capture_build(layout, project: Path, command: list[str], environment: dict, log_path: Path,
                  decoder: Path, timeout_seconds: int = 1200) -> dict:
    """Build through the normal slot wrapper and return command + portable selection.

    The decoder is prepared explicitly once by the run owner, not via an ambient
    executable or arbitrary caller JSON. Fresh capture directories cannot replay
    another build's traces. No assumption that the wrapper itself is a CLR process.
    """
    project = regular_file(project)
    require(project in {path for name, path in layout.project_paths.items() if name in ("wasm", "custom-elements")}
            and command[:2] == ["dotnet", "build"] and project.name in command
            and "--no-restore" in command and "-p:UseSharedCompilation=false" in command,
            "Converter capture requires the exact owned client build")
    require(not any("EVENTPIPE" in key.upper() for key in environment)
            and environment.get("DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER") == "1"
            and environment.get("MSBUILDDISABLENODEREUSE") == "1", "Unreviewed build tracing/server environment")
    decoder = prepare_decoder(decoder.parents[3], layout.sdk, environment)
    capture = _private_directory(layout.group_root / ("converter-capture-" + secrets.token_hex(8)))
    _private_directory(capture / "traces")
    env = environment | {"DOTNET_EnableEventPipe": "1", "DOTNET_EventPipeConfig": "Microsoft-Windows-DotNETRuntime:0x4:4",
                         "DOTNET_EventPipeCircularMB": "40", "DOTNET_EventPipeOutputPath": str(capture / "traces" / "runtime-{pid}.nettrace")}
    build_command = command + ["--disable-build-servers"]
    # Cold conversion is required when no verified build is reused. Timestamp
    # skips must not masquerade as observing the current converter implementation.
    webcil = project.parent / "obj" / "Release" / layout.request.framework / "webcil"
    require(not webcil.exists(), "Existing WebCIL intermediates require a fresh reviewed build group")
    result = _owned_command(build_command, project.parent, env, log_path, timeout_seconds)
    inventory = _trace_inventory(capture / "traces")
    selection = _decode(decoder, capture, inventory, layout.packages_root, layout.sdk, environment)
    _write_private(capture / "selection.json", selection)
    marker = project.parent / "converter-selection-private.json"
    require(not marker.exists() and not marker.is_symlink(), "Refusing to replace converter selection")
    _write_private(marker, {"schema": 1, "capture": capture.name, "selection_sha256": sha256(capture / "selection.json")})
    return {"command": result["command"], "exit_code": 0, "log": result["log"], "converter_selection": selection}


def verify_reused_selection(layout, project: Path, decoder: Path, environment: dict) -> dict:
    """Re-decode immutable original traces before reusing a hash-verified build."""
    marker = json.loads(regular_file(project.parent / "converter-selection-private.json").read_text())
    require(set(marker) == {"schema", "capture", "selection_sha256"} and marker["schema"] == 1
            and re.fullmatch(r"converter-capture-[0-9a-f]{16}", marker["capture"]), "Invalid reused converter selection")
    capture = layout.group_root / marker["capture"]
    path = regular_file(capture / "selection.json")
    require(sha256(path) == marker["selection_sha256"], "Original converter selection changed")
    old = json.loads(path.read_text())
    actual = _decode(decoder, capture, _trace_inventory(capture / "traces"), layout.packages_root, layout.sdk, environment)
    require(old == actual, "Reused converter binding differs from original build")
    return actual
