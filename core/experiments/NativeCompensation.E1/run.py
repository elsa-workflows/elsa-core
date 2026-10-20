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


def backend(directory, url, kill_at=None, ambient=False, handoff="Compensated", mode="run"):
    command = [dotnet_host(), str(DLL), mode, str(directory), url]
    if mode == "run":
        command += ["--barriers"]
    if ambient:
        command += ["--ambient"]
    environment = dict(os.environ, E1_HANDOFF=handoff)
    process = subprocess.Popen(command, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                               text=True, bufsize=1, env=environment)
    lines = queue.Queue()
    def read_lines():
        for line in process.stdout:
            lines.put(line)
        lines.put(None)
    reader = threading.Thread(target=read_lines, daemon=True)
    reader.start()
    events = []
    deadline = time.monotonic() + 90  # timeout is failure detection, never correctness synchronization
    killed = False
    while True:
        if time.monotonic() > deadline:
            process.kill()
            raise RuntimeError(f"Backend timeout; events={events}")
        try:
            line = lines.get(timeout=1)
        except queue.Empty:
            continue
        if line is None:
            break
        event = json.loads(line)
        events.append(event)
        if event["kind"] == "barrier":
            if event["value"]["name"] == kill_at:
                process.kill()  # SIGKILL: no Dispose/finally/graceful drain
                killed = True
                break
            process.stdin.write("continue\n")
            process.stdin.flush()
    process.wait(timeout=15)
    stderr = process.stderr.read()
    reader.join(timeout=5)
    result = {"pid": process.pid, "exit": process.returncode, "killed": killed, "events": events, "stderr": stderr}
    if kill_at and not killed:
        raise RuntimeError(f"Did not reach {kill_at}: {result}")
    return result


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
        result["first"] = backend(directory, url, kill_at, ambient, handoff)
        result["after_first"] = {"elsa": snapshot(directory), "participant": ledger(directory)}
        if kill_at:
            result["restart"] = backend(directory, url, handoff=restart_handoff or handoff)
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
