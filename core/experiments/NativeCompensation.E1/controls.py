#!/usr/bin/env python3
"""Adversarial controls against the unchanged native loader/store; results are observations, not support claims."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import sqlite3
import subprocess
import sys

import run as native

parser = argparse.ArgumentParser()
parser.add_argument("output", type=Path)
options = parser.parse_args()
options.output.mkdir(parents=True, exist_ok=True)


def digest(value):
    return hashlib.sha256(value.encode()).hexdigest()


for name in ("stale-writer", "outer-corrupt", "inner-schema", "missing-work", "callback-owner", "callback-child", "definition-pin"):
    directory = options.output / name
    directory.mkdir()  # Never overwrite prior evidence.
    participant = subprocess.Popen([sys.executable, str(native.HERE / "participant.py"), str(directory / "participant.db")],
                                   stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
    port = json.loads(participant.stdout.readline())["port"]
    url = f"http://127.0.0.1:{port}/"
    result = {"case": name, "participant_pid": participant.pid}
    try:
        cut = "forward-ack-0" if name in ("missing-work", "callback-owner", "callback-child") else "after-intent-reserve-0"
        result["first"] = native.backend(directory, url, kill_at=cut)
        result["before"] = {"elsa": native.snapshot(directory), "participant": native.ledger(directory)}
        if name == "stale-writer":
            result["control"] = native.backend(directory, url, mode="stale")
        elif name == "definition-pin":
            os.environ["E1_GRAPH_VERSION"] = "e1-definition-v2"
            try:
                result["control"] = native.backend(directory, url)
            finally:
                del os.environ["E1_GRAPH_VERSION"]
        else:
            with sqlite3.connect(directory / "elsa.db") as db:
                raw = db.execute("SELECT Data FROM WorkflowInstances").fetchone()[0]
                state = json.loads(raw)
                if name == "outer-corrupt":
                    altered = "{synthetic-corrupt-outer-snapshot"
                else:
                    if name == "inner-schema":
                        state["properties"]["elsa.experimental.native-compensation.e1"]["Schema"] = 999
                    if name == "missing-work":
                        state["scheduledActivities"]["$values"][0]["activityNodeId"] = "required-work-node-removed"
                    if name == "callback-owner":
                        state["completionCallbacks"]["$values"][-1]["ownerInstanceId"] = "required-owner-removed"
                    if name == "callback-child":
                        state["completionCallbacks"]["$values"][-1]["childNodeId"] = "required-child-removed"
                    altered = json.dumps(state, separators=(",", ":"))
                result["injectedSha256"] = digest(altered)
                db.execute("UPDATE WorkflowInstances SET Data=?", (altered,))
            result["inspection"] = native.backend(directory, url, mode="inspect")
            result["control"] = native.backend(directory, url, mode="resave" if name == "outer-corrupt" else "run")
            with sqlite3.connect(directory / "elsa.db") as db:
                raw_after = db.execute("SELECT Data FROM WorkflowInstances").fetchone()[0]
                result["afterSha256"] = digest(raw_after)
                result["originalBytesRetainedInRow"] = raw_after == altered
        result["after"] = {"elsa": native.snapshot(directory), "participant": native.ledger(directory)}
    except Exception as error:
        result["control_error"] = str(error)
    finally:
        participant.terminate()
        participant.wait(timeout=10)
        result["participant_stderr"] = participant.stderr.read()
        (directory / "evidence.json").write_text(json.dumps(result, indent=2))
    print(json.dumps({"case": name, "events": result.get("control", {}).get("events", [])[-1:],
                      "originalBytesRetainedInRow": result.get("originalBytesRetainedInRow"), "control_error": result.get("control_error")}), flush=True)
