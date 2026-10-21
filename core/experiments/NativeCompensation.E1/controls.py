#!/usr/bin/env python3
"""Adversarial controls against the unchanged native loader/store; results are observations, not support claims."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import sqlite3

import run as native
from validation import CONTROL_CASES, validate_control

parser = argparse.ArgumentParser()
parser.add_argument("output", type=Path)


def digest(value):
    return hashlib.sha256(value.encode()).hexdigest()


def run_control(name, output):
    directory = output / name
    directory.mkdir()  # Never overwrite prior evidence.
    result = {"case": name, "participant_pid": None, "participant_stderr": ""}
    try:
        with native.ParticipantProcess(result, native.participant_command(directory)) as url:
            cut = "forward-ack-0" if name in ("missing-work", "callback-owner", "callback-child") else "after-intent-reserve-0"
            native.run_backend_phase(result, "first", directory, url, kill_at=cut)
            result["before"] = {"elsa": native.snapshot(directory), "participant": native.ledger(directory)}
            if name == "stale-writer":
                native.run_backend_phase(result, "control", directory, url, mode="stale")
            elif name == "definition-pin":
                os.environ["E1_GRAPH_VERSION"] = "e1-definition-v2"
                try:
                    native.run_backend_phase(result, "control", directory, url)
                finally:
                    del os.environ["E1_GRAPH_VERSION"]
            else:
                with sqlite3.connect(directory / "elsa.db") as db:
                    raw = db.execute("SELECT Data FROM WorkflowInstances").fetchone()[0]
                    (directory / "original-snapshot.txt").write_text(raw)
                    state = json.loads(raw)
                    if name == "outer-corrupt":
                        altered = "{synthetic-corrupt-outer-snapshot"
                    elif name == "outer-compression":
                        altered = raw
                        db.execute("UPDATE WorkflowInstances SET DataCompressionAlgorithm='GZip'")
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
                    (directory / "injected-snapshot.txt").write_text(altered)
                    db.execute("UPDATE WorkflowInstances SET Data=?", (altered,))
                native.run_backend_phase(result, "inspection", directory, url, mode="inspect")
                native.run_backend_phase(result, "control", directory, url,
                                         mode="resave" if name in ("outer-corrupt", "outer-compression") else "run")
                with sqlite3.connect(directory / "elsa.db") as db:
                    raw_after = db.execute("SELECT Data FROM WorkflowInstances").fetchone()[0]
                    result["afterSha256"] = digest(raw_after)
                    result["originalBytesRetainedInRow"] = raw_after == altered
            result["after"] = {"elsa": native.snapshot(directory), "participant": native.ledger(directory)}
            result["classification"] = validate_control(result)
    except Exception as error:
        result["control_error"] = str(error) or type(error).__name__
    finally:
        (directory / "evidence.json").write_text(json.dumps(result, indent=2))
    print(json.dumps({"case": name, "events": result.get("control", {}).get("events", [])[-1:],
                      "originalBytesRetainedInRow": result.get("originalBytesRetainedInRow"), "control_error": result.get("control_error")}), flush=True)

    return "control_error" not in result

def main(argv=None):
    options = parser.parse_args(argv)
    options.output.mkdir(parents=True, exist_ok=True)
    failed = False
    for name in sorted(CONTROL_CASES):
        failed = not run_control(name, options.output) or failed
    return 1 if failed else 0

if __name__ == "__main__":
    sys.exit(main())
