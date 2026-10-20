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

HERE = Path(__file__).resolve().parent
DLL = HERE / "bin/Debug/net10.0/NativeCompensation.E1.dll"
DOTNET = "/usr/local/share/dotnet/dotnet"  # execute an already-built DLL; no second build/testhost
parser = argparse.ArgumentParser()
parser.add_argument("output", type=Path)
parser.add_argument("--case", default="all")


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
    command = [DOTNET, str(DLL), mode, str(directory), url]
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
        if not ambient and not fail_undo and kill_at != "fault-owned-before-native-recovery":
            assert final is not None, final_events
            assert final["journal"]["Phase"] == "Compensated", final
            operations = result["final"]["participant"]["operations"]
            assert len(operations) == 2, operations
            assert all(row["closed"] == 1 for row in operations), operations
            assert all(row["undo_effects"] == 1 for row in operations), operations
            assert operations[0]["reserve_effects"] == 1, operations
            assert operations[1]["reserve_effects"] > 1 if no_dedupe else operations[1]["reserve_effects"] == 1, operations
            undo_slots = [next(row["slot"] for row in operations if row["operation_key"] == call["operation_key"])
                          for call in result["final"]["participant"]["calls"] if call["direction"] == "undo" and call["disposition"] == "effect"]
            assert undo_slots == [1, 0], undo_slots
            assert [entry["CompletionOrdinal"] for entry in final["journal"]["Entries"]] == [1, 2], final
            assert final["incidents"] == (1 if handoff == "Fault" else 0), final
            if handoff != "Fault":
                assert all(fault["FaultCount"] == 0 for fault in final["faults"]), final
                assert final["journal"]["OutwardCount"] == 1, final
            else:
                assert final["journal"]["OutwardCount"] == 0, final
            if kill_at:
                interrupted_journal = next(event["value"]["journal"] for event in reversed(result["first"]["events"]) if event["kind"] == "barrier")
                assert interrupted_journal["Seed"] == final["journal"]["Seed"], final
                for old_entry in interrupted_journal["Entries"]:
                    restored = next(entry for entry in final["journal"]["Entries"] if entry["Slot"] == old_entry["Slot"])
                    for field in ("OccurrenceId", "OperationKey", "UndoKey"):
                        assert old_entry[field] == restored[field], (field, old_entry, restored)
                stored = result["after_first"]["elsa"].get("rows", [])
                if stored:
                    committed_journal = stored[0]["snapshot"]["properties"].get("elsa.experimental.native-compensation.e1", {})
                    for old_entry in committed_journal.get("Entries", []):
                        restored = next(entry for entry in final["journal"]["Entries"] if entry["Slot"] == old_entry["Slot"])
                        for field in ("ProtectedRequest", "ForwardContextId", "UndoContextId"):
                            if old_entry.get(field):
                                assert old_entry[field] == restored[field], (field, old_entry, restored)
        if fail_undo:
            assert final is not None and final["status"] == "Running" and final["subStatus"] == "Suspended", final
            assert final["journal"]["Phase"] == "AwaitingIntervention", final
            assert final["journal"]["ProtectedOriginalCause"] and final["journal"]["ProtectedRecoveryCause"], final
            assert final["journal"]["OutwardCount"] == 0, final
        if ambient:
            assert result["first"]["exit"] == 2, result["first"]
            assert any("AmbientTransactionWarning" in event["value"].get("Message", "") for event in final_events if event["kind"] == "error"), final_events
            assert result["final"]["participant"]["calls"] == [], result["final"]["participant"]
            result["classification"] = "native-provider-gap-observed"
        elif kill_at == "fault-owned-before-native-recovery":
            assert final is not None and final["incidents"] == 1 and final["faults"][0]["FaultCount"] == 1, final
            assert final["journal"]["Phase"] == "Compensated" and final["journal"]["FaultOwnershipReserved"], final
            result["classification"] = "native-fault-handoff-gap-observed"
        elif no_dedupe:
            result["classification"] = "participant-duplicate-risk-observed"
        else:
            result["classification"] = "narrow-prototype-checks-passed"
    except Exception as error:
        result["assertion_failure"] = str(error)
    finally:
        participant.terminate()
        participant.wait(timeout=10)
        result["participant_stderr"] = participant.stderr.read()
        (directory / "evidence.json").write_text(json.dumps(result, indent=2))
    print(json.dumps({"case": name, "observed": result.get("observed_result"), "assertion_failure": result.get("assertion_failure"), "evidence": str(directory / "evidence.json")}))


cases = {
    "baseline": {},
    "ambient": {"ambient": True},
    "scope-activated": {"kill_at": "scope-activated"},
    "unacknowledged-scope": {"kill_at": "durable-unacknowledged-scope"},
    "before-intent": {"kill_at": "before-intent-reserve-0"},
    "after-intent": {"kill_at": "after-intent-reserve-0"},
    "unacknowledged-intent": {"kill_at": "durable-unacknowledged-reserve-0"},
    "lost-forward-result": {"kill_at": "participant-committed-reserve-1"},
    "before-undo": {"kill_at": "after-intent-undo-1"},
    "lost-undo-result": {"kill_at": "participant-committed-undo-1"},
    "between-handlers": {"kill_at": "between-handlers"},
    "settled": {"kill_at": "settled-before-handoff"},
    "settled-fault": {"kill_at": "settled-before-handoff", "handoff": "Fault", "restart_handoff": "Compensated"},
    "fault-owned": {"kill_at": "fault-owned-before-native-recovery"},
    "dedupe-disabled": {"kill_at": "participant-committed-reserve-1", "no_dedupe": True},
    "undo-failure": {"fail_undo": True},
}
if __name__ == "__main__":
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    for case, parameters in cases.items():
        if args.case in ("all", case):
            run_case(case, **parameters)
