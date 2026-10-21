#!/usr/bin/env python3
"""Isolated launcher for a future local Server Studio browser proof.

The caller supplies compiled hosts and a reviewed Playwright config/spec. This
script supervises processes only; it does not implement a browser journey.
"""
import argparse
import base64
import ctypes
import ctypes.util
import hashlib
import json
import math
import os
import re
import secrets
import shutil
import signal
import socket
import stat
import subprocess
import sys
import tempfile
import time
import uuid
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
STARTUP_ASSEMBLY = "Elsa.NativeCompensation.StudioProofStartup"
ADMIN_USER = "ELSA_COMP_PROOF_ADMIN_USERNAME"
ADMIN_PASSWORD = "ELSA_COMP_PROOF_ADMIN_PASSWORD"
KEY_DIR = "ELSA_COMPENSATION_STUDIO_DP_KEY_DIR"
class StopProof(Exception):
    pass


class ProcBsdInfo(ctypes.Structure):
    _fields_ = [
        ("flags", ctypes.c_uint32), ("status", ctypes.c_uint32), ("xstatus", ctypes.c_uint32),
        ("pid", ctypes.c_uint32), ("ppid", ctypes.c_uint32),
        ("uid", ctypes.c_uint32), ("gid", ctypes.c_uint32),
        ("ruid", ctypes.c_uint32), ("rgid", ctypes.c_uint32),
        ("svuid", ctypes.c_uint32), ("svgid", ctypes.c_uint32),
        ("reserved", ctypes.c_uint32), ("comm", ctypes.c_char * 16), ("name", ctypes.c_char * 32),
        ("nfiles", ctypes.c_uint32), ("pgid", ctypes.c_uint32), ("pjobc", ctypes.c_uint32),
        ("tty_device", ctypes.c_uint32), ("tty_pgid", ctypes.c_uint32), ("nice", ctypes.c_int32),
        ("start_seconds", ctypes.c_uint64), ("start_microseconds", ctypes.c_uint64),
    ]


_LIBPROC = None


def libproc():
    global _LIBPROC
    if sys.platform != "darwin":
        raise StopProof("browser-process-identity-requires-macos-libproc")
    if _LIBPROC is None:
        library = ctypes.util.find_library("proc") or "/usr/lib/libproc.dylib"
        _LIBPROC = ctypes.CDLL(library, use_errno=True)
        _LIBPROC.proc_pidinfo.argtypes = [ctypes.c_int, ctypes.c_int, ctypes.c_uint64,
                                          ctypes.c_void_p, ctypes.c_int]
        _LIBPROC.proc_pidinfo.restype = ctypes.c_int
        _LIBPROC.proc_listpids.argtypes = [ctypes.c_uint32, ctypes.c_uint32,
                                           ctypes.c_void_p, ctypes.c_int]
        _LIBPROC.proc_listpids.restype = ctypes.c_int
    return _LIBPROC


def process_info(pid):
    info = ProcBsdInfo()
    copied = libproc().proc_pidinfo(pid, 3, 0, ctypes.byref(info), ctypes.sizeof(info))
    if copied != ctypes.sizeof(info) or info.pid != pid:
        try:
            os.kill(pid, 0)
        except ProcessLookupError:
            return None
        except PermissionError:
            raise StopProof("browser-process-identity-unreadable") from None
        raise StopProof("browser-process-identity-unreadable")
    try:
        session_id = os.getsid(pid)
    except OSError:
        # A just-killed macOS process can remain as SZOMB until collected. It
        # cannot execute or receive signals; owned PGID absence is checked below.
        if info.status == 5:  # SZOMB from sys/proc.h.
            return None
        raise StopProof("browser-process-session-unreadable") from None
    return {
        "pid": int(info.pid),
        "ppid": int(info.ppid),
        "pgid": int(info.pgid),
        "sid": int(session_id),
        "birth": f"{int(info.start_seconds)}:{int(info.start_microseconds):06d}",
        "birthUnixMicros": int(info.start_seconds) * 1_000_000 + int(info.start_microseconds),
    }


def process_table():
    library = libproc()
    buffer_size = 1024 * 1024
    buffer = ctypes.create_string_buffer(buffer_size)
    bytes_used = library.proc_listpids(1, 0, buffer, buffer_size)
    if bytes_used < 0 or bytes_used >= buffer_size:
        raise StopProof("browser-process-table-unavailable")
    count = bytes_used // ctypes.sizeof(ctypes.c_int)
    pids = ctypes.cast(buffer, ctypes.POINTER(ctypes.c_int))
    table = {}
    for index in range(count):
        pid = int(pids[index])
        if pid <= 0:
            continue
        try:
            item = process_info(pid)
        except StopProof:
            continue
        if item is not None:
            table[pid] = item
    return table


def read_owned_process_info(pid, timeout=1.5):
    """Allow a killed process to finish leaving libproc, without signalling it.

    A process can disappear between proc_pidinfo and getsid. Retry this read-only
    observation; callers still compare the returned birth/group before any signal.
    Persistent uncertainty remains a cleanup failure.
    """
    deadline = time.monotonic() + timeout
    while True:
        try:
            return process_info(pid)
        except StopProof as error:
            if (str(error) not in ("browser-process-identity-unreadable", "browser-process-session-unreadable") or
                    time.monotonic() >= deadline):
                raise
            time.sleep(0.03)


def descends_from(pid, root_births, table):
    seen = set()
    current = table.get(pid)
    while current is not None:
        parent = current["ppid"]
        if parent in root_births:
            parent_info = table.get(parent)
            return parent_info is not None and parent_info["birth"] == root_births[parent]
        if parent <= 1 or parent in seen:
            return False
        seen.add(parent)
        current = table.get(parent)
    return False


def atomic_private_json(path, payload):
    temporary = path.with_name(path.name + ".tmp")
    with temporary.open("w", encoding="utf-8") as stream:
        json.dump(payload, stream, separators=(",", ":"))
        stream.write("\n")
    os.chmod(temporary, 0o600)
    os.replace(temporary, path)


def load_raw_browser_capture(path):
    if not path.is_file():
        raise StopProof("browser-process-capture-missing")
    if path.stat().st_mode & 0o077:
        raise StopProof("browser-process-capture-not-private")
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        raise StopProof("browser-process-capture-invalid") from None
    if (not isinstance(data, dict) or data.get("schemaVersion") != 1 or
            data.get("source") != "Chromium-CDP-SystemInfo.getProcessInfo" or
            not isinstance(data.get("snapshots"), list) or not data["snapshots"] or
            len(data["snapshots"]) > 100):
        raise StopProof("browser-process-capture-invalid")
    for snapshot in data["snapshots"]:
        if not isinstance(snapshot, dict):
            raise StopProof("browser-process-capture-invalid")
        if (not isinstance(snapshot.get("capturedAtUnixMs"), int) or
                not isinstance(snapshot.get("ownerPid"), int) or snapshot["ownerPid"] <= 0 or
                not isinstance(snapshot.get("ownerParentPid"), int) or snapshot["ownerParentPid"] <= 0 or
                not isinstance(snapshot.get("processes"), list) or not snapshot["processes"]):
            raise StopProof("browser-process-capture-invalid")
        for process in snapshot["processes"]:
            if (not isinstance(process, dict) or not isinstance(process.get("id"), int) or process["id"] <= 0 or
                    not isinstance(process.get("type"), str) or not process["type"] or len(process["type"]) > 64):
                raise StopProof("browser-process-capture-invalid")
    return data


def load_browser_ownership(path, runner_pid, runner_birth):
    if not isinstance(runner_birth, str) or not re.fullmatch(r"[0-9]+:[0-9]{6}", runner_birth):
        raise StopProof("browser-runner-birth-identity-unverified")
    if not path.is_file():
        return {
            "schemaVersion": 1,
            "source": "Chromium-CDP-plus-macOS-proc_pidinfo",
            "runnerPid": runner_pid,
            "runnerPgid": runner_pid,
            "runnerBirth": runner_birth,
            "processedSnapshotCount": 0,
            "browserProcessSeen": False,
            "processes": [],
        }
    if path.stat().st_mode & 0o077:
        raise StopProof("browser-process-ownership-not-private")
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        raise StopProof("browser-process-ownership-invalid") from None
    if (not isinstance(data, dict) or data.get("schemaVersion") != 1 or
            data.get("source") != "Chromium-CDP-plus-macOS-proc_pidinfo" or
            data.get("runnerPid") != runner_pid or data.get("runnerPgid") != runner_pid or
            data.get("runnerBirth") != runner_birth or
            not isinstance(data.get("processedSnapshotCount"), int) or
            data["processedSnapshotCount"] < 0 or
            not isinstance(data.get("processes"), list)):
        raise StopProof("browser-process-ownership-invalid")
    for item in data["processes"]:
        if (not isinstance(item, dict) or not isinstance(item.get("pid"), int) or item["pid"] <= 0 or
                not isinstance(item.get("ppid"), int) or not isinstance(item.get("pgid"), int) or
                not isinstance(item.get("sid"), int) or not isinstance(item.get("birth"), str) or
                not re.fullmatch(r"[0-9]+:[0-9]{6}", item["birth"]) or
                not isinstance(item.get("source"), str) or
                not isinstance(item.get("verified"), bool) or
                not isinstance(item.get("observedAtUnixMs"), int)):
            raise StopProof("browser-process-ownership-invalid")
    return data


def ownership_key(item):
    return f"{item['pid']}:{item['birth']}"


def refresh_browser_ownership(capture_path, ownership_path, runner_pid, runner_birth):
    capture = load_raw_browser_capture(capture_path)
    ownership = load_browser_ownership(ownership_path, runner_pid, runner_birth)
    table = process_table()
    processes = {ownership_key(item): item for item in ownership["processes"]}
    root_births = {item["pid"]: item["birth"] for item in ownership["processes"] if item.get("source") == "cdp"}
    browser_groups = {(item["pgid"], item["sid"]) for item in ownership["processes"] if item.get("source") == "cdp"}
    processed = ownership["processedSnapshotCount"]
    if processed > len(capture["snapshots"]):
        raise StopProof("browser-process-ownership-invalid")

    def add(info, *, source, process_type, observed_at):
        if info is None:
            return
        item = {
            "pid": info["pid"], "ppid": info["ppid"], "pgid": info["pgid"], "sid": info["sid"],
            "birth": info["birth"], "source": source, "type": process_type,
            "observedAtUnixMs": observed_at, "verified": True,
        }
        processes.setdefault(ownership_key(item), item)

    for snapshot in capture["snapshots"][processed:]:
        observed_at = snapshot["capturedAtUnixMs"]
        owner_pid = snapshot["ownerPid"]
        owner = table.get(owner_pid)
        current_roots = dict(root_births)
        runner_info = table.get(runner_pid)
        if runner_info is not None and runner_info["birth"] == runner_birth:
            current_roots[runner_pid] = runner_birth
        if owner is not None:
            if owner["birthUnixMicros"] > observed_at * 1000:
                raise StopProof("browser-owner-pid-reused")
            if owner["pgid"] != runner_pid and not descends_from(owner_pid, current_roots, table):
                raise StopProof("browser-owner-process-group-unverified")
            if (owner["ppid"] != snapshot["ownerParentPid"] and owner["pgid"] != runner_pid and
                    not descends_from(owner_pid, {snapshot["ownerParentPid"]: table.get(snapshot["ownerParentPid"], {}).get("birth", "")}, table)):
                raise StopProof("browser-owner-parent-unverified")
            add(owner, source="playwright-owner", process_type=None, observed_at=observed_at)
            current_roots[owner_pid] = owner["birth"]

        browser_seen_here = False
        for item in snapshot["processes"]:
            pid = item["id"]
            if item["type"] == "browser":
                browser_seen_here = True
                ownership["browserProcessSeen"] = True
            info = table.get(pid)
            if info is None:
                info = read_owned_process_info(pid)
                if info is None:
                    continue
                if info["birthUnixMicros"] > observed_at * 1000:
                    # Do not add a PID reused after the CDP observation to the
                    # table used for descendant ownership discovery.
                    continue
                table[pid] = info
            elif info["birthUnixMicros"] > observed_at * 1000:
                # This numeric PID was reused after the CDP observation. Never signal it.
                continue
            related = ((info["pgid"] == runner_pid and info["sid"] == runner_pid) or
                       descends_from(pid, current_roots, table) or
                       (info["pgid"], info["sid"]) in browser_groups)
            if not related:
                raise StopProof("browser-process-ancestry-unverified")
            add(info, source="cdp", process_type=item["type"], observed_at=observed_at)
            current_roots[pid] = info["birth"]
            root_births[pid] = info["birth"]
            browser_groups.add((info["pgid"], info["sid"]))
        if not browser_seen_here:
            raise StopProof("chromium-browser-process-not-reported")
        ownership["processedSnapshotCount"] += 1

    # Include every live child beneath a CDP-confirmed Chromium PID and every
    # member of this runner's unique process group, without reading command lines.
    now_ms = int(time.time() * 1000)
    for pid, info in table.items():
        if info["pgid"] == runner_pid and info["sid"] == runner_pid:
            add(info, source="runner-group", process_type=None, observed_at=now_ms)
        elif ((info["pgid"], info["sid"]) in browser_groups or
              (root_births and descends_from(pid, root_births, table))):
            add(info, source="browser-descendant", process_type=None, observed_at=now_ms)

    ownership["processes"] = sorted(processes.values(), key=lambda item: (item["pid"], item["birth"]))
    atomic_private_json(ownership_path, ownership)
    return ownership


def terminate_verified_browser_processes(ownership_path, runner_pid, runner_birth, runner_process):
    ownership = load_browser_ownership(ownership_path, runner_pid, runner_birth)
    if ownership["processedSnapshotCount"] < 1 or not ownership["browserProcessSeen"]:
        return False
    if runner_process.pid != runner_pid:
        return False

    # Let Playwright close and reap its own Chromium children first. Killing the
    # children and their owner together can strand unreaped zombies on macOS.
    # The fallback below still targets only independently verified identities.
    terminate_verified_runner_only(runner_process, runner_birth)

    table = process_table()
    records = {ownership_key(item): item for item in ownership["processes"] if item.get("verified") is True}
    root_births = {item["pid"]: item["birth"] for item in records.values() if item.get("source") == "cdp"}
    browser_groups = {(item["pgid"], item["sid"]) for item in records.values() if item.get("source") == "cdp"}
    now_ms = int(time.time() * 1000)
    for pid, info in table.items():
        if info["pgid"] == runner_pid and info["sid"] == runner_pid:
            item = {
                **info, "birth": info["birth"], "source": "runner-group", "type": None,
                "observedAtUnixMs": now_ms, "verified": True,
            }
            records.setdefault(ownership_key(item), item)
        elif ((info["pgid"], info["sid"]) in browser_groups or
              (root_births and descends_from(pid, root_births, table))):
            item = {
                **info, "birth": info["birth"], "source": "browser-descendant", "type": None,
                "observedAtUnixMs": now_ms, "verified": True,
            }
            records.setdefault(ownership_key(item), item)

    candidates = []
    for record in records.values():
        current = read_owned_process_info(record["pid"])
        if current is None or current["birth"] != record["birth"]:
            # The captured process exited or its PID was reused. Do not signal the new PID.
            continue
        if current["pgid"] != record["pgid"] or current["sid"] != record["sid"]:
            return False
        candidates.append(record)

    # Signal children first. Every signal is preceded by a fresh PID/birth/group check.
    depths = {}
    candidate_pids = {item["pid"] for item in candidates}
    current_table = process_table()

    def depth(pid, seen=None):
        if pid in depths:
            return depths[pid]
        seen = set() if seen is None else seen
        if pid in seen:
            return 0
        seen.add(pid)
        info = current_table.get(pid)
        parent = info["ppid"] if info else 0
        value = 1 + depth(parent, seen) if parent in candidate_pids else 0
        depths[pid] = value
        return value

    candidates.sort(key=lambda item: depth(item["pid"]), reverse=True)
    for record in candidates:
        current = read_owned_process_info(record["pid"])
        if current is None or current["birth"] != record["birth"]:
            continue
        if current["pgid"] != record["pgid"] or current["sid"] != record["sid"]:
            return False
        try:
            os.kill(record["pid"], signal.SIGTERM)
        except ProcessLookupError:
            pass
        except OSError:
            return False

    until = time.monotonic() + 0.6
    while time.monotonic() < until:
        if all((lambda current, record: current is None or current["birth"] != record["birth"])(
                read_owned_process_info(record["pid"]), record) for record in candidates):
            break
        time.sleep(0.05)

    for record in candidates:
        current = read_owned_process_info(record["pid"])
        if current is None or current["birth"] != record["birth"]:
            continue
        if current["pgid"] != record["pgid"] or current["sid"] != record["sid"]:
            return False
        try:
            os.kill(record["pid"], signal.SIGKILL)
        except ProcessLookupError:
            pass
        except OSError:
            return False

    time.sleep(0.1)
    final_table = process_table()
    for record in records.values():
        current = final_table.get(record["pid"])
        if current is not None and current["birth"] == record["birth"]:
            return False
    owned_groups = set(browser_groups)
    owned_groups.add((runner_pid, runner_pid))
    if not owned_process_groups_gone(owned_groups):
        return False
    if root_births and any(descends_from(pid, root_births, final_table) for pid in final_table):
        return False
    return True


def wait_for_browser_runner(process, capture_path, ownership_path, runner_birth, deadline):
    last_mtime = None
    while True:
        if capture_path.is_file():
            current_mtime = capture_path.stat().st_mtime_ns
            if current_mtime != last_mtime:
                refresh_browser_ownership(capture_path, ownership_path, process.pid, runner_birth)
                last_mtime = current_mtime
        if process.poll() is not None:
            if capture_path.is_file():
                refresh_browser_ownership(capture_path, ownership_path, process.pid, runner_birth)
            return process.returncode
        if time.monotonic() >= deadline:
            if capture_path.is_file():
                refresh_browser_ownership(capture_path, ownership_path, process.pid, runner_birth)
            raise StopProof("deadline-browser-proof")
        time.sleep(min(0.1, max(0, deadline - time.monotonic())))


def process_groups_gone(children):
    for _, process in children:
        if process.poll() is None:
            return False
        try:
            os.killpg(process.pid, 0)
            return False
        except ProcessLookupError:
            continue
        except OSError:
            return False
    return True


def owned_process_groups_gone(groups, timeout=3.0):
    """Require empty process groups for every verified PGID/SID pair.

    This probes only. A reused numeric PGID conservatively leaves cleanup
    unconfirmed; the function never signals a group.
    """
    deadline = time.monotonic() + timeout
    while True:
        table = process_table()
        live_groups = {(info["pgid"], info["sid"]) for info in table.values()}
        any_group_exists = bool(groups & live_groups)
        if not any_group_exists:
            for pgid, _sid in groups:
                try:
                    os.killpg(pgid, 0)
                    any_group_exists = True
                    break
                except ProcessLookupError:
                    continue
                except OSError:
                    # Permission errors and other uncertainty are not proof of absence.
                    any_group_exists = True
                    break
        if not any_group_exists:
            return True
        if time.monotonic() >= deadline:
            return False
        time.sleep(0.05)


def terminate_verified_runner_only(process, runner_birth):
    """Stop only the owned Playwright runner when no browser capture is available.

    This deliberately does not signal the runner's process group: Chromium can be
    detached from the runner's lifetime, and without CDP evidence its processes
    are not safe to target. The caller must retain scratch and report unconfirmed
    cleanup when this fallback is used.
    """
    if process.poll() is not None:
        return True
    current = read_owned_process_info(process.pid)
    if current is None:
        return process.poll() is not None
    if (current["birth"] != runner_birth or current["pgid"] != process.pid or
            current["sid"] != process.pid):
        return False
    try:
        os.kill(process.pid, signal.SIGTERM)
    except ProcessLookupError:
        return process.poll() is not None
    except OSError:
        return False
    try:
        process.wait(timeout=2)
    except subprocess.TimeoutExpired:
        current = read_owned_process_info(process.pid)
        if (current is None or current["birth"] != runner_birth or
                current["pgid"] != process.pid or current["sid"] != process.pid):
            return process.poll() is not None
        try:
            os.kill(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        except OSError:
            return False
        try:
            process.wait(timeout=1)
        except (subprocess.TimeoutExpired, OSError):
            return False
    except OSError:
        return False
    return process.poll() is not None


def arguments():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", type=Path, required=True,
                        help="Absolute dotnet executable or wrapper used to launch the compiled hosts.")
    parser.add_argument("--node", type=Path, required=True,
                        help="Absolute Node executable used to launch the Playwright JavaScript CLI.")
    parser.add_argument("--core-dll", required=True, type=Path)
    parser.add_argument("--studio-output", required=True, type=Path)
    parser.add_argument("--startup-dll", required=True, type=Path)
    parser.add_argument("--startup-deps", required=True, type=Path)
    parser.add_argument("--playwright-cli", required=True, type=Path)
    parser.add_argument("--playwright-browsers-path", required=True, type=Path,
                        help="Private, pre-provisioned Playwright browser cache directory.")
    parser.add_argument("--playwright-config", required=True, type=Path)
    parser.add_argument("--browser-spec", required=True, type=Path)
    parser.add_argument("--source-root", required=True, type=Path)
    parser.add_argument("--expected-head", required=True)
    parser.add_argument("--build-manifest", required=True, type=Path,
                        help="Mode-0600 external JSON build record; its runtime output hashes must match the copied files.")
    parser.add_argument("--scratch-parent", required=True, type=Path,
                        help="External directory for private host runs, outside the source and receipt directories.")
    parser.add_argument("--source-file", required=True, action="append",
                        help="Repository-relative source path to hash into the safe receipt; repeat for every proof-relevant file.")
    parser.add_argument("--assertion-id", required=True, action="append",
                        help="Required UI assertion identifier. The receipt records it as NOT_EVALUATED until a reviewed result parser exists.")
    parser.add_argument("--receipt-dir", type=Path, required=True)
    parser.add_argument("--core-port", required=True, type=int)
    parser.add_argument("--studio-port", required=True, type=int)
    parser.add_argument("--deadline-seconds", type=float, default=180)
    return parser.parse_args()


def existing(path, directory=False):
    if not path.is_absolute():
        raise StopProof("paths-must-be-absolute")
    resolved = path.resolve(strict=True)
    if (directory and not resolved.is_dir()) or (not directory and not resolved.is_file()):
        raise StopProof("invalid-input-path")
    return resolved


def check_port(port):
    if not 1024 <= port <= 65535:
        raise StopProof("port-out-of-range")
    try:
        with socket.socket() as sock:
            sock.bind(("127.0.0.1", port))
    except OSError:
        raise StopProof("port-in-use") from None


def left(deadline, stage):
    seconds = deadline - time.monotonic()
    if seconds <= 0:
        raise StopProof(f"deadline-{stage}")
    return seconds


def wait_for_port(process, port, deadline, stage):
    while True:
        left(deadline, stage)
        if process.poll() is not None:
            raise StopProof(f"{stage}-host-exited")
        try:
            with socket.create_connection(("127.0.0.1", port), timeout=0.2):
                return
        except OSError:
            time.sleep(0.1)


def start(name, command, cwd, env, children):
    try:
        process = subprocess.Popen(
            command, cwd=cwd, env=env, shell=False, start_new_session=True,
            stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
        )
    except OSError:
        raise StopProof(f"{name}-could-not-start") from None
    children.append((name, process))
    return process


def clean_environment(temp_directory, browser_cache=None):
    # Keep only locale and PATH; application and runtime configuration is explicit.
    # In particular, do not pass through arbitrary DOTNET_*, ASPNETCORE_*, AWS_*, or
    # application configuration variables from the invoking shell.
    allowed = ["PATH", "LANG", "LC_ALL"]
    env = {key: os.environ[key] for key in allowed if key in os.environ}
    env.update({
        "HOME": str(temp_directory),
        "TMPDIR": str(temp_directory),
        "TEMP": str(temp_directory),
        "TMP": str(temp_directory),
        "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
        "DOTNET_NOLOGO": "true",
    })
    if browser_cache is not None:
        env["PLAYWRIGHT_BROWSERS_PATH"] = str(browser_cache)
    return env


def stop_all(children):
    failed = False
    for _, process in reversed(children):
        pgid = process.pid
        try:
            os.killpg(pgid, signal.SIGTERM)
        except ProcessLookupError:
            pass
        except OSError:
            try:
                process.terminate()
            except ProcessLookupError:
                pass
            except OSError:
                pass
        time.sleep(0.4)
        try:
            os.killpg(pgid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        except OSError:
            try:
                process.kill()
            except ProcessLookupError:
                pass
            except OSError:
                pass
        try:
            process.wait(timeout=2)
        except subprocess.TimeoutExpired:
            failed = True
            continue
        except OSError:
            failed = True
            continue
        until = time.monotonic() + 2
        while time.monotonic() < until:
            try:
                os.killpg(pgid, 0)
            except ProcessLookupError:
                break
            except OSError:
                failed = True
                break
            time.sleep(0.05)
        else:
            failed = True
    return failed


def write_overlay(root, username, password):
    core_db = f"Data Source={(root / 'core.sqlite').as_posix()};Cache=Shared"
    empty_feed = root / "empty-nuplane-feed"
    empty_feed.mkdir(mode=0o700)
    identity_signing_key = secrets.token_urlsafe(48)
    secrets_encryption_key = base64.b64encode(secrets.token_bytes(32)).decode("ascii")
    broker_client_secret = secrets.token_urlsafe(48)
    features = {
        "Identity": {"SigningKey": identity_signing_key},
        "Secrets": {"EncryptionKey": secrets_encryption_key},
        "ExternalAuthentication": {
            "AuthenticationClients": {"0": {"IsEnabled": False}},
            "Secrets": {"StudioServerClientSecret": broker_client_secret},
        },
        "SqliteWorkflowPersistence": {"ConnectionString": core_db},
        "SqliteIdentityPersistence": {"ConnectionString": core_db},
        "SqliteExternalAuthenticationPersistence": {"ConnectionString": core_db},
        "SqliteAlterationsPersistence": {"ConnectionString": core_db},
        "SqliteUserTasksPersistence": {"ConnectionString": f"Data Source={(root / 'user-tasks.sqlite').as_posix()};Cache=Shared"},
        "SqliteStructuredLogPersistence": {"ConnectionString": f"Data Source={(root / 'logs.sqlite').as_posix()};Cache=Shared"},
        "DefaultAdminUser": {
            "AdminUserName": username,
            "AdminPassword": password,
            "AdminRoleName": "admin",
            "AdminRolePermissions": ["*"],
        },
    }
    path = root / "core-shell-overlay.json"
    with path.open("x", encoding="utf-8") as stream:
        json.dump({
            "CShells": {"Shells": {"Default": {"Features": features}}},
            "Nuplane": {
                "Setup": {
                    "AutomaticReconciliation": False,
                    "Feeds": {
                        "0": {
                            "DirectoryPath": str(empty_feed),
                            "Directory": {"Watch": False},
                        },
                    },
                },
                "Loading": {"Enabled": False},
            },
        }, stream)
    os.chmod(path, 0o600)
    return path


def copy_compiled_output(source, destination):
    source_root = source.resolve()

    def ignore_environment_settings(directory, names):
        if Path(directory).resolve() != source_root:
            return set()
        return {
            name for name in names
            if name != "appsettings.json" and name.startswith("appsettings.") and name.endswith(".json")
        }

    shutil.copytree(source, destination, ignore=ignore_environment_settings)


def sha256_file(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def source_snapshot(source_root, source_files):
    git_env = {"PATH": os.environ.get("PATH", ""), "GIT_OPTIONAL_LOCKS": "0", "GIT_CONFIG_NOSYSTEM": "1"}
    head = subprocess.run(
        ["git", "-C", str(source_root), "rev-parse", "--verify", "HEAD"],
        check=True, capture_output=True, env=git_env,
    ).stdout.decode("ascii").strip()
    status = subprocess.run(
        ["git", "-C", str(source_root), "status", "--porcelain=v1", "--untracked-files=all"],
        check=True, capture_output=True, env=git_env,
    ).stdout
    hashes = {}
    allowed_source_suffixes = {".cs", ".razor", ".csproj", ".ts", ".tsx", ".js", ".mjs", ".py", ".css", ".html"}
    for relative in source_files:
        candidate = Path(relative)
        if candidate.is_absolute() or ".." in candidate.parts:
            raise StopProof("source-file-must-be-repository-relative")
        if candidate.suffix.lower() not in allowed_source_suffixes or any(
            part.lower().startswith(("appsettings", ".env")) for part in candidate.parts
        ):
            raise StopProof("receipt-source-must-be-code-not-configuration")
        resolved = (source_root / candidate).resolve(strict=True)
        if not resolved.is_file() or not resolved.is_relative_to(source_root):
            raise StopProof("source-file-outside-repository")
        key = resolved.relative_to(source_root).as_posix()
        if key in hashes:
            raise StopProof("duplicate-source-file")
        hashes[key] = sha256_file(resolved)
    return head, not status, hashes


def binary_snapshot(*roots):
    """Hash copied runtime binaries and every file in the copied Studio wwwroot."""
    hashes = {}
    suffixes = (".dll", ".deps.json", ".runtimeconfig.json", ".dylib", ".so", ".exe")
    for root in roots:
        root = Path(root)
        studio_wwwroot = root / "wwwroot" if root.name == "studio-host" else None
        if studio_wwwroot is not None:
            if not studio_wwwroot.is_dir() or not studio_wwwroot.resolve().is_relative_to(root.resolve()):
                raise StopProof("build-studio-wwwroot-missing")

        for path in sorted(root.rglob("*")):
            if not path.is_file():
                continue
            relative_path = path.relative_to(root)
            is_runtime_binary = path.name.endswith(suffixes)
            is_studio_asset = studio_wwwroot is not None and relative_path.parts[0] == "wwwroot"
            if is_runtime_binary or is_studio_asset:
                key = f"{root.name}/{path.relative_to(root).as_posix()}"
                hashes[key] = sha256_file(path)
    return hashes


def validate_build_manifest(path, expected_head, binary_hashes):
    if stat.S_IMODE(path.stat().st_mode) != 0o600:
        raise StopProof("build-manifest-must-be-mode-0600")
    try:
        manifest = json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        raise StopProof("build-manifest-invalid") from None
    if (not isinstance(manifest, dict) or type(manifest.get("schemaVersion")) is not int or
            manifest.get("schemaVersion") != 1 or
            manifest.get("sourceHead") != expected_head.lower() or
            manifest.get("sourceTreeClean") is not True):
        raise StopProof("build-manifest-source-mismatch")

    builds = manifest.get("builds")
    required_components = {"core", "server-studio", "startup"}
    if not isinstance(builds, list) or len(builds) != len(required_components):
        raise StopProof("build-manifest-build-records-invalid")
    components = set()
    for build in builds:
        if (not isinstance(build, dict) or
                build.get("component") not in required_components or
                build.get("component") in components or
                not isinstance(build.get("command"), str) or not build["command"].strip() or
                len(build["command"]) > 4096 or
                type(build.get("exitCode")) is not int or build["exitCode"] != 0):
            raise StopProof("build-manifest-build-records-invalid")
        components.add(build["component"])
    if components != required_components:
        raise StopProof("build-manifest-build-records-invalid")

    recorded_hashes = manifest.get("binarySha256")
    if not isinstance(recorded_hashes, dict) or not recorded_hashes:
        raise StopProof("build-manifest-binary-hashes-invalid")
    if any(not isinstance(key, str) or not isinstance(value, str) or
           not re.fullmatch(r"[a-f0-9]{64}", value)
           for key, value in recorded_hashes.items()):
        raise StopProof("build-manifest-binary-hashes-invalid")
    if recorded_hashes != binary_hashes:
        raise StopProof("build-manifest-binary-hashes-mismatch")
    return sha256_file(path)


def write_safe_receipt(receipt_dir, state):
    receipt_dir.mkdir(mode=0o700, parents=True, exist_ok=True)
    os.chmod(receipt_dir, 0o700)
    run_id = state["runId"]
    temp_path = receipt_dir / f".{run_id}.tmp"
    final_path = receipt_dir / f"{run_id}.json"
    with temp_path.open("x", encoding="utf-8") as stream:
        json.dump(state, stream, indent=2, sort_keys=True)
        stream.write("\n")
    os.chmod(temp_path, 0o600)
    os.replace(temp_path, final_path)
    return final_path


def run():
    args = arguments()
    deadline = time.monotonic() + max(args.deadline_seconds, 0)
    receipt_dir = args.receipt_dir.expanduser().absolute()
    expected_head_is_sha = bool(re.fullmatch(r"[0-9a-fA-F]{40}", args.expected_head))
    assertion_ids_are_safe = bool(args.assertion_id) and all(
        re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._:-]{0,127}", item) for item in args.assertion_id
    )
    state = {
        "schemaVersion": 1,
        "runId": str(uuid.uuid4()),
        "createdUtc": datetime.now(timezone.utc).isoformat(),
        "proofStatus": "UNQUALIFIED",
        "qualificationNote": "The supervisor does not validate UI assertion result records.",
        "expectedHead": args.expected_head.lower() if expected_head_is_sha else None,
        "actualHead": None,
        "sourceTreeClean": None,
        "sourceSha256": {},
        "binarySha256": {},
        "harnessSha256": {},
        "buildProvenance": {
            "manifestSha256": None,
            "sourceHead": None,
            "outputHashesMatched": False,
        },
        "requiredAssertions": [
            {"id": assertion_id, "status": "NOT_EVALUATED"}
            for assertion_id in args.assertion_id
        ] if assertion_ids_are_safe else [{"id": "INVALID_INPUT_REDACTED", "status": "NOT_EVALUATED"}],
        "runnerOutcome": "NOT_STARTED",
        "runnerExitCode": None,
        "failureCode": None,
        "cleanupStatus": "NOT_STARTED",
        "browserDescendantTermination": "NOT_VERIFIED",
        "assertionResults": {"file": None, "sha256": None, "interpretation": "NOT_EVALUATED"},
        "storedPublishApiAssertion": {
            "ids": [
                "NC-API-STORED-PUBLISH-REFUSAL",
                "NC-API-STORED-PUBLISH-TARGETED-DIAGNOSTICS",
                "NC-API-STORED-PUBLISH-ROWS-UNCHANGED",
            ],
            "status": "NOT_EVALUATED",
            "file": None,
            "sha256": None,
            "interpretation": "NOT_EVALUATED",
        },
        "browserProcessEvidence": {"file": None, "sha256": None, "interpretation": "NOT_EVALUATED"},
    }
    children = []
    root = None
    cleanup_failed = False
    receipt_path = None
    browser_process = None
    browser_runner_birth = None
    browser_capture_valid = False
    browser_ownership_path = None
    private_browser_process_capture_path = None

    try:
        if os.name != "posix":
            raise StopProof("process-group-supervision-requires-posix")
        if sys.platform != "darwin":
            raise StopProof("browser-process-identity-requires-macos-libproc")
        process_info(os.getpid())
        if not math.isfinite(args.deadline_seconds) or args.deadline_seconds <= 0 or args.core_port == args.studio_port:
            raise StopProof("invalid-deadline-or-duplicate-port")
        if not expected_head_is_sha:
            raise StopProof("expected-head-must-be-full-commit-sha")
        if not assertion_ids_are_safe or len(set(args.assertion_id)) != len(args.assertion_id):
            raise StopProof("required-assertion-ids-must-be-nonempty-and-unique")

        dotnet = existing(args.dotnet)
        if not os.access(dotnet, os.X_OK):
            raise StopProof("dotnet-wrapper-not-executable")
        node = existing(args.node)
        if not os.access(node, os.X_OK):
            raise StopProof("node-executable-not-executable")
        core = existing(args.core_dll)
        studio_output = existing(args.studio_output, directory=True)
        startup_dll = existing(args.startup_dll)
        startup_deps = existing(args.startup_deps)
        playwright = existing(args.playwright_cli)
        browser_cache = existing(args.playwright_browsers_path, directory=True)
        config = existing(args.playwright_config)
        spec = existing(args.browser_spec)
        package_manifest = existing(config.parent / "package.json")
        package_lock = existing(config.parent / "package-lock.json")
        typescript_config = existing(config.parent / "tsconfig.json")
        sqlite_verifier = existing(config.parent / "verify_sqlite.py")
        stored_publish_probe = existing(config.parent / "stored_publish_probe.py")
        source_root = existing(args.source_root, directory=True)
        build_manifest = existing(args.build_manifest)
        if not args.scratch_parent.is_absolute():
            raise StopProof("scratch-parent-must-be-absolute")
        scratch_parent = args.scratch_parent.expanduser().absolute().resolve(strict=False)
        resolved_receipt_dir = receipt_dir.resolve(strict=False)
        if (scratch_parent.is_relative_to(resolved_receipt_dir) or
                resolved_receipt_dir.is_relative_to(scratch_parent)):
            raise StopProof("scratch-parent-and-receipt-directory-must-be-separate")
        if scratch_parent.is_relative_to(HERE.resolve()):
            raise StopProof("scratch-parent-must-be-outside-fixture-source")
        if (scratch_parent.is_relative_to(source_root) or source_root.is_relative_to(scratch_parent)):
            raise StopProof("scratch-parent-must-be-outside-source-tree")
        if resolved_receipt_dir.is_relative_to(source_root):
            raise StopProof("receipt-directory-must-be-outside-source-tree")
        if build_manifest.is_relative_to(source_root):
            raise StopProof("build-manifest-must-be-outside-source-tree")
        if build_manifest.is_relative_to(scratch_parent):
            raise StopProof("build-manifest-must-be-outside-private-scratch")
        for port in (args.core_port, args.studio_port):
            check_port(port)
        run_parent = scratch_parent / "runs"

        head, clean, source_hashes = source_snapshot(source_root, args.source_file)
        left(deadline, "source-preflight")
        state["actualHead"] = head
        state["sourceTreeClean"] = clean
        state["sourceSha256"] = source_hashes
        if head.lower() != args.expected_head.lower():
            raise StopProof("source-head-does-not-match-expected-head")
        if not clean:
            raise StopProof("source-tree-is-not-clean")

        startup_source_dir = source_root / "scripts" / "native-compensation-draft-proof" / "StudioProofDataProtectionStartup"
        state["harnessSha256"] = {
            "dotnetWrapper": sha256_file(dotnet),
            "nodeExecutable": sha256_file(node),
            "playwrightCli": sha256_file(playwright),
            "playwrightConfig": sha256_file(config),
            "browserSpec": sha256_file(spec),
            "sqliteVerifier": sha256_file(sqlite_verifier),
            "playwrightPackage": sha256_file(package_manifest),
            "playwrightLockfile": sha256_file(package_lock),
            "typescriptConfig": sha256_file(typescript_config),
            "proofSupervisor": sha256_file(HERE / "run-proof-skeleton.py"),
            "storedPublishProbe": sha256_file(stored_publish_probe),
            "startupSource": sha256_file(startup_source_dir /
                                          "StudioProofDataProtectionStartup.cs"),
            "startupProject": sha256_file(startup_source_dir /
                                           "StudioProofDataProtectionStartup.csproj"),
        }
        username = os.environ.get(ADMIN_USER)
        password = os.environ.get(ADMIN_PASSWORD)
        if not username or not password:
            raise StopProof("fixture-credentials-required-in-environment")

        run_parent.mkdir(mode=0o700, parents=True, exist_ok=True)
        os.chmod(run_parent, 0o700)
        os.umask(0o077)
        root = Path(tempfile.mkdtemp(prefix="elsa-native-comp-proof-", dir=run_parent))
        keys = root / "studio-dp-keys"
        keys.mkdir(mode=0o700)
        core_host = root / "core-host"
        copy_compiled_output(core.parent, core_host)
        copied_core = core_host / core.name
        studio = root / "studio-host"
        copy_compiled_output(studio_output, studio)
        copied_dll = studio / f"{STARTUP_ASSEMBLY}.dll"
        copied_deps = studio / f"{STARTUP_ASSEMBLY}.deps.json"
        shutil.copy2(startup_dll, copied_dll)
        shutil.copy2(startup_deps, copied_deps)
        results = root / "browser-results"
        results.mkdir(mode=0o700)
        artifact_dir = results / "playwright-artifacts"
        artifact_dir.mkdir(mode=0o700)
        receipt_dir.mkdir(mode=0o700, parents=True, exist_ok=True)
        os.chmod(receipt_dir, 0o700)
        assertion_result_path = receipt_dir / f"{state['runId']}.assertions.json"
        state["assertionResults"]["file"] = assertion_result_path.name
        stored_publish_result_path = receipt_dir / f"{state['runId']}.stored-publish.json"
        state["storedPublishApiAssertion"]["file"] = stored_publish_result_path.name
        browser_process_evidence_path = receipt_dir / f"{state['runId']}.browser-process.json"
        state["browserProcessEvidence"]["file"] = browser_process_evidence_path.name
        private_capture_path = results / "private-capture.json"
        private_browser_process_capture_path = results / "browser-process-private.json"
        browser_ownership_path = results / "browser-process-ownership.json"
        overlay = write_overlay(root, username, password)
        temp_directory = root / "temp"
        temp_directory.mkdir(mode=0o700)
        state["binarySha256"] = binary_snapshot(core_host, studio)
        state["buildProvenance"]["manifestSha256"] = validate_build_manifest(
            build_manifest, head, state["binarySha256"]
        )
        state["buildProvenance"]["sourceHead"] = head
        state["buildProvenance"]["outputHashesMatched"] = True
        left(deadline, "fixture-preparation")

        core_url = f"http://127.0.0.1:{args.core_port}"
        studio_url = f"http://127.0.0.1:{args.studio_port}"
        backend_url = f"{core_url}/elsa/api"
        core_env = clean_environment(temp_directory)
        core_env.update({
            "ASPNETCORE_ENVIRONMENT": "Production",
            "DOTNET_ENVIRONMENT": "Production",
            "ASPNETCORE_URLS": core_url,
            "Elsa__PlatformIntegration__ShellOverlayPath": str(overlay),
            "OTEL_SDK_DISABLED": "true",
        })
        left(deadline, "core-launch")
        core_process = start("core", [str(dotnet), str(copied_core), "--urls", core_url], core_host, core_env, children)
        wait_for_port(core_process, args.core_port, deadline, "core-startup")

        studio_env = clean_environment(temp_directory)
        studio_env.update({
            "ASPNETCORE_ENVIRONMENT": "Production",
            "DOTNET_ENVIRONMENT": "Production",
            "ASPNETCORE_URLS": studio_url,
            "ASPNETCORE_HOSTINGSTARTUPASSEMBLIES": STARTUP_ASSEMBLY,
            "ASPNETCORE_PREVENTHOSTINGSTARTUP": "false",
            KEY_DIR: str(keys),
            "Authentication__Provider": "ElsaIdentity",
            "Backend__Url": backend_url,
            "OTEL_SDK_DISABLED": "true",
        })
        left(deadline, "studio-launch")
        studio_process = start(
            "studio",
            [str(dotnet), "exec", "--additional-deps", str(copied_deps),
             str(studio / "Elsa.Studio.Host.Server.dll"), "--urls", studio_url],
            studio, studio_env, children,
        )
        wait_for_port(studio_process, args.studio_port, deadline, "studio-startup")
        marker = keys / ".studio-proof-hosting-startup-loaded"
        while not marker.exists():
            left(deadline, "hosting-startup-load")
            if studio_process.poll() is not None:
                raise StopProof("hosting-startup-host-exited")
            time.sleep(0.05)

        browser_env = clean_environment(temp_directory, browser_cache=browser_cache)
        browser_env.update({
            ADMIN_USER: username,
            ADMIN_PASSWORD: password,
            "ELSA_COMP_PROOF_STUDIO_URL": studio_url,
            "ELSA_COMP_PROOF_SQLITE_PATH": str(root / "core.sqlite"),
            "ELSA_COMP_PROOF_ASSERTION_RESULTS": str(assertion_result_path),
            "ELSA_COMP_PROOF_PRIVATE_CAPTURE": str(private_capture_path),
            "ELSA_COMP_PROOF_BROWSER_PROCESS_CAPTURE": str(private_browser_process_capture_path),
            "ELSA_COMP_PROOF_BROWSER_PROCESS_EVIDENCE": str(browser_process_evidence_path),
            "ELSA_COMP_PROOF_SQLITE_VERIFIER": str(sqlite_verifier),
            "ELSA_COMP_PROOF_PYTHON": str(Path(sys.executable).resolve()),
            "ELSA_COMP_PROOF_ARTIFACT_DIR": str(artifact_dir),
            "OTEL_SDK_DISABLED": "true",
        })
        browser_command = [str(node), str(playwright), "test", str(spec), "--config", str(config),
                           "--output", str(artifact_dir), "--reporter=line", "--workers=1", "--retries=0"]
        left(deadline, "browser-launch")
        browser = start("browser-proof", browser_command, spec.parent, browser_env, children)
        browser_process = browser
        runner_identity = process_info(browser.pid)
        if runner_identity is None or runner_identity["pgid"] != browser.pid or runner_identity["sid"] != browser.pid:
            raise StopProof("browser-runner-process-group-unverified")
        browser_runner_birth = runner_identity["birth"]
        exit_code = wait_for_browser_runner(
            browser,
            private_browser_process_capture_path,
            browser_ownership_path,
            browser_runner_birth,
            deadline,
        )
        browser_ownership = load_browser_ownership(browser_ownership_path, browser.pid, browser_runner_birth)
        browser_capture_valid = browser_ownership["processedSnapshotCount"] > 0 and browser_ownership["browserProcessSeen"]
        if not browser_capture_valid:
            raise StopProof("browser-process-capture-missing")
        state["runnerExitCode"] = exit_code
        if exit_code != 0:
            raise StopProof("browser-proof-nonzero-exit")
        api_env = clean_environment(temp_directory)
        api_env.update({
            ADMIN_USER: username,
            ADMIN_PASSWORD: password,
            "ELSA_COMP_PROOF_BACKEND_URL": backend_url,
            "ELSA_COMP_PROOF_API_ASSERTION_RESULTS": str(stored_publish_result_path),
            "ELSA_COMP_PROOF_PRIVATE_CAPTURE": str(private_capture_path),
            "ELSA_COMP_PROOF_SQLITE_PATH": str(root / "core.sqlite"),
            "ELSA_COMP_PROOF_SQLITE_VERIFIER": str(sqlite_verifier),
            "ELSA_COMP_PROOF_PYTHON": str(Path(sys.executable).resolve()),
            "ELSA_COMP_PROOF_API_TIMEOUT_SECONDS": str(left(deadline, "stored-publish-api")),
        })
        api_process = start(
            "stored-publish-api",
            [str(Path(sys.executable).resolve()), str(stored_publish_probe)],
            spec.parent,
            api_env,
            children,
        )
        try:
            api_exit_code = api_process.wait(timeout=left(deadline, "stored-publish-api"))
        except subprocess.TimeoutExpired:
            raise StopProof("stored-publish-api-timeout") from None
        if api_exit_code != 0:
            raise StopProof("stored-publish-api-failed")
        if not list(keys.glob("key-*.xml")):
            raise StopProof("studio-data-protection-key-not-persisted")
        left(deadline, "completion")
        final_head, final_clean, final_source_hashes = source_snapshot(source_root, args.source_file)
        if final_head.lower() != head.lower() or not final_clean or final_source_hashes != source_hashes:
            raise StopProof("source-changed-during-proof")
        state["runnerOutcome"] = "RUNNER_SUCCEEDED_ASSERTIONS_NOT_EVALUATED"
    except StopProof as error:
        state["runnerOutcome"] = "STOPPED"
        state["failureCode"] = str(error)
    except KeyboardInterrupt:
        state["runnerOutcome"] = "INTERRUPTED"
        state["failureCode"] = "keyboard-interrupt"
    except Exception:
        # Do not include exception text or host/test output; either may contain values.
        state["runnerOutcome"] = "FAILED"
        state["failureCode"] = "unexpected-exception-details-suppressed"
    finally:
        if 'assertion_result_path' in locals() and assertion_result_path.is_file():
            try:
                state["assertionResults"]["sha256"] = sha256_file(assertion_result_path)
            except OSError:
                state["assertionResults"]["sha256"] = None
        if 'stored_publish_result_path' in locals() and stored_publish_result_path.is_file():
            try:
                state["storedPublishApiAssertion"]["sha256"] = sha256_file(stored_publish_result_path)
            except OSError:
                state["storedPublishApiAssertion"]["sha256"] = None
        if 'browser_process_evidence_path' in locals() and browser_process_evidence_path.is_file():
            try:
                state["browserProcessEvidence"]["sha256"] = sha256_file(browser_process_evidence_path)
            except OSError:
                state["browserProcessEvidence"]["sha256"] = None
        browser_terminated = browser_process is None
        if browser_process is not None:
            try:
                if private_browser_process_capture_path.is_file():
                    refresh_browser_ownership(
                        private_browser_process_capture_path,
                        browser_ownership_path,
                        browser_process.pid,
                        browser_runner_birth,
                    )
                    ownership = load_browser_ownership(
                        browser_ownership_path, browser_process.pid, browser_runner_birth
                    )
                    browser_capture_valid = (
                        ownership["processedSnapshotCount"] > 0 and ownership["browserProcessSeen"]
                    )
            except Exception:
                browser_capture_valid = False
            if browser_capture_valid:
                try:
                    browser_terminated = terminate_verified_browser_processes(
                        browser_ownership_path,
                        browser_process.pid,
                        browser_runner_birth,
                        browser_process,
                    )
                except Exception:
                    browser_terminated = False
            else:
                # The Popen handle and birth identity authorize stopping only the
                # runner process. Its group may contain unobserved Chromium, so
                # leave that group alone and retain scratch below.
                browser_terminated = terminate_verified_runner_only(
                    browser_process, browser_runner_birth
                )
        non_browser_children = [
            item for item in children if browser_process is None or item[1] is not browser_process
        ]
        if non_browser_children:
            try:
                cleanup_failed = stop_all(non_browser_children)
            except Exception:
                cleanup_failed = True
        groups_gone = process_groups_gone(non_browser_children)
        cleanup_failed = cleanup_failed or not groups_gone
        if browser_process is not None:
            if browser_capture_valid and browser_terminated and groups_gone:
                state["browserDescendantTermination"] = "VERIFIED"
            else:
                state["browserDescendantTermination"] = "NOT_VERIFIED"
                cleanup_failed = True
        if browser_process is None and groups_gone:
            state["browserDescendantTermination"] = "NOT_APPLICABLE"
        if cleanup_failed:
            state["cleanupStatus"] = "NOT_CONFIRMED_PRIVATE_SCRATCH_RETAINED"
            state["runnerOutcome"] = "STOPPED_CLEANUP_UNCONFIRMED"
            state["failureCode"] = "owned-process-teardown-not-confirmed"
        else:
            state["cleanupStatus"] = "CONFIRMED"
            if root is not None:
                shutil.rmtree(root, ignore_errors=True)
                if root.exists():
                    state["cleanupStatus"] = "PRIVATE_SCRATCH_CLEANUP_FAILED"
                    state["runnerOutcome"] = "STOPPED_CLEANUP_UNCONFIRMED"
                    state["failureCode"] = "private-scratch-removal-not-confirmed"
        try:
            receipt_path = write_safe_receipt(receipt_dir, state)
        except Exception:
            receipt_path = None
            state["cleanupStatus"] = "RECEIPT_WRITE_FAILED"

    state["receiptFile"] = receipt_path.name if receipt_path else None
    return state


def main():
    state = run()
    if state["receiptFile"] is None:
        print("Proof skeleton ended without a safe receipt.", file=sys.stderr)
        return 2
    print(f"Runner outcome: {state['runnerOutcome']}; proofStatus=UNQUALIFIED; receipt={state['receiptFile']}")
    if state["runnerOutcome"] == "RUNNER_SUCCEEDED_ASSERTIONS_NOT_EVALUATED":
        return 0
    return 130 if state["runnerOutcome"] == "INTERRUPTED" else 1


if __name__ == "__main__":
    raise SystemExit(main())
