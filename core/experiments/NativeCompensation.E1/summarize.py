#!/usr/bin/env python3
"""Export synthetic IDs, counts and hashes; never export Data Protection keys or protected payloads."""
import argparse
import hashlib
import json
from pathlib import Path
from validation import load_evidence

parser = argparse.ArgumentParser()
parser.add_argument("matrix", type=Path)
parser.add_argument("controls", type=Path)
parser.add_argument("output", type=Path)
parser.add_argument("--controls-source-head", required=True)


def journal_summary(journal):
    if not journal:
        return None
    keys = ("Schema", "Revision", "Seed", "DefinitionVersion", "HandlerVersion", "Phase", "Handoff",
            "CompletionOrdinal", "OutwardCount", "OwnerToken", "FaultOwnershipReserved", "FaultedContextId")
    summary = {key: journal.get(key) for key in keys}
    summary["originalCauseRetained"] = bool(journal.get("ProtectedOriginalCause"))
    summary["recoveryCauseRetained"] = bool(journal.get("ProtectedRecoveryCause"))
    summary["entries"] = []
    entries = journal.get("Entries", [])
    if isinstance(entries, dict):
        entries = entries.get("$values", [])
    for entry in entries:
        fields = ("Slot", "OccurrenceId", "OperationKey", "UndoKey", "Status", "ForwardContextId", "UndoContextId", "AttemptId", "CompletionOrdinal")
        item = {field: entry.get(field) for field in fields}
        item["protectedCaptureHashes"] = {field: hashlib.sha256(entry[field].encode()).hexdigest() if entry.get(field) else None
                                         for field in ("ProtectedRequest", "ProtectedResult", "ProtectedUndoReceipt")}
        summary["entries"].append(item)
    return summary


def snapshot_summary(snapshot):
    rows = snapshot.get("rows", [])
    if not rows:
        return None
    row = rows[0]
    state = row.get("snapshot")
    if state is None:
        return {"opaqueDataSha256": row.get("dataSha256"), "decodedState": None}
    return {
        "opaqueDataSha256": row.get("dataSha256"), "definitionVersion": state.get("definitionVersionId"),
        "status": state.get("status"), "subStatus": state.get("subStatus"),
        "incidents": len(state.get("incidents", {}).get("$values", [])),
        "contexts": [{key: context.get(key) for key in ("id", "parentContextId", "scheduledActivityNodeId", "status", "isExecuting", "faultCount")}
                     for context in state.get("activityExecutionContexts", {}).get("$values", [])],
        "work": state.get("scheduledActivities", {}).get("$values", []),
        "callbacks": state.get("completionCallbacks", {}).get("$values", []),
        "journal": journal_summary(state.get("properties", {}).get("elsa.experimental.native-compensation.e1"))
    }


def main(argv=None):
    options = parser.parse_args(argv)
    summary = {
        "result": "Partial: E1 does not pass; no M1 readiness or supported-provider claim",
        "baseSourceHead": "51d316882e6a79376145aef1283aa4004a3f400d",
        "matrixSourceHead": "f3d4b31863dc7e2a029d6e93454a25a905e9b813",
        "controlsSourceHead": options.controls_source_head,
        "sdk": "10.0.300", "targetFramework": "net10.0", "matrix": [], "controls": []
    }
    for category, directory in (("matrix", options.matrix), ("controls", options.controls)):
        for file, source in load_evidence(directory, category):
            before = source.get("after_first", source.get("before", {}))
            after = source.get("final", source.get("after", {}))
            record = {
                "case": source["case"], "classification": source["classification"], "configuration": source.get("configuration"),
                "killBarrier": source.get("kill_at"), "backendPid": source["first"]["pid"],
                "newBackendPid": source.get("restart", source.get("control", {})).get("pid"),
                "participantPid": source["participant_pid"],
                "firstExit": source["first"]["exit"],
                "nextExit": source.get("restart", source.get("control", {})).get("exit"),
                "before": snapshot_summary(before.get("elsa", {})), "after": snapshot_summary(after.get("elsa", {})),
                "participantAtCut": before.get("participant"), "participantFinal": after.get("participant"),
                "opaqueOriginalBytesRetainedInNativeRow": source.get("originalBytesRetainedInRow"),
                "evidenceFile": str(file), "evidenceSha256": hashlib.sha256(file.read_bytes()).hexdigest()
            }
            record["nativeErrors"] = [event["value"] for phase in ("first", "restart", "control")
                                      for event in source.get(phase, {}).get("events", []) if event["kind"] == "error"]
            stale = next((event["value"] for event in source.get("control", {}).get("events", []) if event["kind"] == "stale-writer"), None)
            if stale:
                record["staleWriter"] = {"newerRevision": stale["newerRevision"], "newerOwner": stale["newerOwner"],
                                         "staleAccepted": stale["staleAccepted"], "canonicalRevision": stale["canonical"]["Revision"], "canonicalOwner": stale["canonical"]["OwnerToken"]}
            summary[category].append(record)
    options.output.write_text(json.dumps(summary, indent=2) + "\n")
    print(f"Exported {len(summary['matrix'])} matrix observations and {len(summary['controls'])} native gap controls; E1 remains Partial.")


if __name__ == "__main__":
    main()
