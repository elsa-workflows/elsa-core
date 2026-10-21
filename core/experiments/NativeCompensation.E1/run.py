#!/usr/bin/env python3
"""Reproducible native process-kill controller for #8930; Python never executes workflow transitions."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import queue
import shutil
import sqlite3
import subprocess
import sys
import threading
import time

from validation import MATRIX_CASES, validate_matrix

HERE = Path(__file__).resolve().parent
DLL = HERE / "bin/Debug/net10.0/NativeCompensation.E1.dll"
def dotnet_host():
    requested = os.environ.get("E1_DOTNET", "dotnet")
    resolved = shutil.which(requested)
    if not resolved:
        raise RuntimeError(f"Cannot resolve .NET host {requested!r}; set E1_DOTNET to the host used to build the fixture")
    return resolved

parser = argparse.ArgumentParser()
parser.add_argument("output", type=Path)
parser.add_argument("--case", choices=("all", *MATRIX_CASES), default="all")


class BackendFailure(RuntimeError):
    """A failed controller phase with its captured child diagnostics intact."""

    def __init__(self, message, result):
        super().__init__(message)
        self.result = result


def ledger(directory):
    with sqlite3.connect(directory / "participant.db") as db:
        db.row_factory = sqlite3.Row
        return {
            "operations": [dict(row) for row in db.execute("SELECT * FROM operations ORDER BY slot")],
            "calls": [dict(row) for row in db.execute("SELECT * FROM calls ORDER BY sequence")],
        }


def snapshot(directory):
    with sqlite3.connect(directory / "elsa.db") as db:
        tables = [row[0] for row in db.execute("SELECT name FROM sqlite_master WHERE type='table'")]
        table = next((name for name in tables if name.endswith("WorkflowInstances")), None)
        if table is None:
            return {"tables": tables, "state": None}
        db.row_factory = sqlite3.Row
        rows = [dict(row) for row in db.execute(f'SELECT * FROM "{table}"')]
        for row in rows:
            data = row.pop("Data", None)
            row["dataSha256"] = hashlib.sha256((data or "").encode()).hexdigest()
            row["snapshot"] = json.loads(data) if data else None
        return {"table": table, "rows": rows}


def backend(directory, url, kill_at=None, ambient=False, handoff="Compensated", mode="run",
            timeout_seconds=90, command_override=None):
    command = command_override or [dotnet_host(), str(DLL), mode, str(directory), url]
    if command_override is None:
        if mode == "run":
            command += ["--barriers"]
        if ambient:
            command += ["--ambient"]
    environment = dict(os.environ, E1_HANDOFF=handoff)
    try:
        process = subprocess.Popen(command, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                   text=True, bufsize=1, env=environment)
    except Exception as error:
        result = {"pid": None, "exit": None, "reaped": True, "killed": False, "cleanup_terminated": False,
                  "events": [], "stderr": "", "stdout_reader_completed": True,
                  "stderr_reader_completed": True, "stdout_reader_error": None, "stderr_reader_error": None,
                  "controller_error": f"{type(error).__name__}: {error}"}
        raise BackendFailure(result["controller_error"], result) from error
    lines = queue.Queue()
    reader_errors = []
    stderr_parts = []
    stderr_errors = []

    def read_lines():
        try:
            for line in process.stdout:
                lines.put(("line", line))
        except Exception as error:
            reader_errors.append(f"{type(error).__name__}: {error}")
            lines.put(("reader_error", error))
        finally:
            lines.put(("eof", None))

    def read_stderr():
        try:
            stderr_parts.append(process.stderr.read())
        except Exception as error:
            stderr_errors.append(f"{type(error).__name__}: {error}")

    reader = threading.Thread(target=read_lines, daemon=True)
    stderr_reader = threading.Thread(target=read_stderr, daemon=True)
    reader.start()
    stderr_reader.start()
    events = []
    deadline = time.monotonic() + timeout_seconds  # timeout is failure detection, never correctness synchronization
    killed = False
    controller_error = None
    cleanup_terminated = False
    try:
        while True:
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                raise TimeoutError(f"Backend timeout after {timeout_seconds}s")
            try:
                kind, payload = lines.get(timeout=min(1, remaining))
            except queue.Empty:
                continue
            if kind == "eof":
                try:
                    process.wait(timeout=max(0, deadline - time.monotonic()))
                except subprocess.TimeoutExpired as error:
                    raise TimeoutError(f"Backend did not exit after closing stdout within {timeout_seconds}s") from error
                break
            if kind == "reader_error":
                raise RuntimeError(f"Backend stdout reader failed: {payload}") from payload
            event = json.loads(payload)
            events.append(event)
            if event["kind"] == "barrier":
                if event["value"]["name"] == kill_at:
                    if process.poll() is None:
                        process.kill()  # SIGKILL: no Dispose/finally/graceful drain
                    killed = True
                    break
                process.stdin.write("continue\n")
                process.stdin.flush()
    except Exception as error:
        controller_error = f"{type(error).__name__}: {error}"
    finally:
        if process.poll() is None:
            cleanup_terminated = True
            try:
                process.kill()  # Only the child created by this controller is ever terminated.
            except ProcessLookupError:
                # The owned child exited between the poll and kill calls.
                pass
        try:
            process.stdin.close()
        except (BrokenPipeError, OSError):
            # A terminated child's input pipe may already be closed.
            pass
        try:
            process.wait(timeout=15)
        except subprocess.TimeoutExpired:
            cleanup_terminated = True
            try:
                process.kill()
            except ProcessLookupError:
                # The owned child exited before the cleanup retry.
                pass
            process.wait()
        reader.join(timeout=5)
        stderr_reader.join(timeout=5)
        if reader.is_alive() and controller_error is None:
            controller_error = "RuntimeError: Backend stdout reader did not finish after child reaping"
        if stderr_reader.is_alive() and controller_error is None:
            controller_error = "RuntimeError: Backend stderr reader did not finish after child reaping"
        if reader_errors and controller_error is None:
            controller_error = f"Backend stdout reader failed: {reader_errors[0]}"
        if stderr_errors and controller_error is None:
            controller_error = f"Backend stderr reader failed: {stderr_errors[0]}"
        for stream in (process.stdout, process.stderr):
            try:
                stream.close()
            except (OSError, ValueError):
                # Preserve phase diagnostics when a teardown pipe is already closed.
                pass
        reader.join(timeout=1)
        stderr_reader.join(timeout=1)

    result = {
        "pid": process.pid,
        "exit": process.returncode,
        "reaped": process.poll() is not None,
        "killed": killed,
        "cleanup_terminated": cleanup_terminated,
        "events": events,
        "stderr": "".join(stderr_parts),
        "stdout_reader_completed": not reader.is_alive(),
        "stderr_reader_completed": not stderr_reader.is_alive(),
        "stdout_reader_error": reader_errors[0] if reader_errors else None,
        "stderr_reader_error": stderr_errors[0] if stderr_errors else None,
        "controller_error": controller_error,
    }
    if stderr_errors and result["controller_error"] is None:
        result["controller_error"] = f"Backend stderr reader failed: {stderr_errors[0]}"
    if kill_at and not killed and result["controller_error"] is None:
        result["controller_error"] = f"Backend did not reach requested barrier {kill_at!r}"
    if result["controller_error"]:
        raise BackendFailure(result["controller_error"], result)
    return result


def run_backend_phase(result, phase, *args, **kwargs):
    """Attach a failed backend phase before propagating its structured error."""
    try:
        phase_result = backend(*args, **kwargs)
    except BackendFailure as error:
        result[phase] = error.result
        raise
    except Exception as error:
        phase_result = {"pid": None, "exit": None, "reaped": True, "killed": False,
                        "cleanup_terminated": False, "events": [], "stderr": "",
                        "stdout_reader_completed": True, "stderr_reader_completed": True,
                        "stdout_reader_error": None, "stderr_reader_error": None,
                        "controller_error": f"{type(error).__name__}: {error}"}
        result[phase] = phase_result
        raise BackendFailure(phase_result["controller_error"], phase_result) from error
    result[phase] = phase_result
    return phase_result


def run_case(name, kill_at=None, ambient=False, no_dedupe=False, fail_undo=False, handoff="Compensated", restart_handoff=None):
    directory = args.output / name
    if directory.exists():
        raise RuntimeError(f"Refusing to replace evidence directory {directory}")
    directory.mkdir()
    command = [sys.executable, str(HERE / "participant.py"), str(directory / "participant.db")]
    if no_dedupe:
        command += ["--no-dedupe"]
    if fail_undo:
        command += ["--fail-undo"]
    participant = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
    port = json.loads(participant.stdout.readline())["port"]
    url = f"http://127.0.0.1:{port}/"
    result = {"case": name, "kill_at": kill_at, "participant_pid": participant.pid,
              "configuration": {"ambient": ambient, "no_dedupe": no_dedupe, "fail_undo": fail_undo, "handoff": handoff, "restart_handoff": restart_handoff}}
    try:
        run_backend_phase(result, "first", directory, url, kill_at, ambient, handoff)
        result["after_first"] = {"elsa": snapshot(directory), "participant": ledger(directory)}
        if kill_at:
            run_backend_phase(result, "restart", directory, url, handoff=restart_handoff or handoff)
        result["final"] = {"elsa": snapshot(directory), "participant": ledger(directory)}
        final_events = result.get("restart", result["first"])["events"]
        final = next((event["value"] for event in final_events if event["kind"] == "result"), None)
        result["observed_result"] = final
        result["classification"] = validate_matrix(result)
    except Exception as error:
        result["assertion_failure"] = str(error) or type(error).__name__
    finally:
        participant.terminate()
        participant.wait(timeout=10)
        result["participant_stderr"] = participant.stderr.read()
        (directory / "evidence.json").write_text(json.dumps(result, indent=2))
    print(json.dumps({"case": name, "observed": result.get("observed_result"), "assertion_failure": result.get("assertion_failure"), "evidence": str(directory / "evidence.json")}))
    return "assertion_failure" not in result


cases = MATRIX_CASES

def main(argv=None):
    global args
    args = parser.parse_args(argv)
    args.output.mkdir(parents=True, exist_ok=True)
    failed = False
    for case, parameters in cases.items():
        if args.case in ("all", case):
            failed = not run_case(case, **parameters) or failed
    return 1 if failed else 0

if __name__ == "__main__":
    sys.exit(main())
