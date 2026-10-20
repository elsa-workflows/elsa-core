"""One source of proof assertions for live controllers and retained evidence."""
import json
from pathlib import Path
import signal

KEY = "elsa.experimental.native-compensation.e1"
CONTROL_CASES = frozenset(("stale-writer", "outer-corrupt", "outer-compression", "inner-schema", "missing-work", "callback-owner", "callback-child", "definition-pin"))

MATRIX_CASES = {
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

def state_at(result, phase):
    rows = result[phase]["elsa"].get("rows", [])
    return rows[0].get("snapshot") if rows else None


def values(value):
    return value.get("$values", []) if isinstance(value, dict) else value


def assert_interruption(result, following):
    first = result["first"]
    assert first["killed"] and first["exit"] == -signal.SIGKILL, "Expected actual SIGKILL"
    assert following["pid"] != first["pid"] != result["participant_pid"], "Expected fresh backend PID"
    assert following["pid"] != result["participant_pid"]


def validate_matrix(result):
    name = result["case"]
    assert name in MATRIX_CASES, "Unexpected matrix case"
    assert "assertion_failure" not in result and "control_error" not in result, "Recorded controller failure"
    expected = MATRIX_CASES[name]
    kill_at = expected.get("kill_at")
    ambient = expected.get("ambient", False)
    fail_undo = expected.get("fail_undo", False)
    no_dedupe = expected.get("no_dedupe", False)
    handoff = expected.get("handoff", "Compensated")
    assert result["kill_at"] == kill_at
    for field, default in (("ambient", False), ("fail_undo", False), ("no_dedupe", False), ("handoff", "Compensated"), ("restart_handoff", None)):
        assert result["configuration"][field] == expected.get(field, default), field
    following = result.get("restart", result["first"])
    if kill_at:
        assert_interruption(result, following)
        assert result["first"]["events"][-1]["kind"] == "barrier"
        assert result["first"]["events"][-1]["value"]["name"] == kill_at
    else:
        assert not result["first"]["killed"]
    assert following["exit"] == (2 if ambient else 0), "Unexpected backend exit"
    final_events = following["events"]
    final = next((event["value"] for event in final_events if event["kind"] == "result"), None)
    assert result.get("observed_result") == final
    if not ambient:
        expected_status = ("Running", "Suspended") if fail_undo else ("Finished", "Faulted" if handoff == "Fault" else "Finished")
        assert (final["status"], final["subStatus"]) == expected_status, "Unexpected native status/substatus"
        state = state_at(result, "final")
        assert (state["status"], state["subStatus"]) == expected_status
        assert final["faults"] == [{"Id": context["id"], "FaultCount": context["faultCount"]} for context in values(state["activityExecutionContexts"])]
    cut = state_at(result, "after_first")
    if name in ("scope-activated", "unacknowledged-scope", "before-intent", "after-intent", "unacknowledged-intent"):
        assert result["after_first"]["participant"]["calls"] == [], "Participant invoked before intent cut"
        assert result["after_first"]["participant"]["operations"] == []
        journal = cut["properties"][KEY]
        if name in ("scope-activated", "unacknowledged-scope", "before-intent"):
            assert journal["Entries"] == []
            assert any(work["activityNodeId"].endswith(":reserve-0") for work in values(cut["scheduledActivities"]))
        else:
            assert journal["Entries"][0]["Status"] == "Armed"
            assert journal["Entries"][0]["ForwardContextId"]
            assert any(context["id"] == journal["Entries"][0]["ForwardContextId"] and context["isExecuting"] for context in values(cut["activityExecutionContexts"]))
    if name == "before-undo":
        assert all(row["undo_effects"] == 0 for row in result["after_first"]["participant"]["operations"])
        assert cut["properties"][KEY]["Entries"][1]["Status"] == "Compensating"
        assert cut["properties"][KEY]["Entries"][1]["ProtectedResult"]
    if name in ("lost-forward-result", "dedupe-disabled"):
        assert cut["properties"][KEY]["Entries"][1]["ProtectedResult"] is None
        assert result["after_first"]["participant"]["operations"][1]["reserve_effects"] == 1
    if name == "lost-undo-result":
        assert cut["properties"][KEY]["Entries"][1]["ProtectedUndoReceipt"] is None
        assert result["after_first"]["participant"]["operations"][1]["undo_effects"] == 1
    if name == "between-handlers":
        assert [entry["Status"] for entry in cut["properties"][KEY]["Entries"]] == ["Compensating", "Compensated"]
        assert [row["undo_effects"] for row in result["after_first"]["participant"]["operations"]] == [0, 1]
    if name in ("settled", "settled-fault"):
        assert cut["properties"][KEY]["Phase"] == "Compensated"
        assert cut["properties"][KEY]["OutwardCount"] == 0
        assert any(work["activityNodeId"].endswith(":handoff") for work in values(cut["scheduledActivities"]))
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
            assert final["faults"] and all(fault["FaultCount"] == 1 for fault in final["faults"])
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
    if name == "fault-owned":
        operations = result["final"]["participant"]["operations"]
        assert len(operations) == 2 and {row["slot"] for row in operations} == {0, 1}
        assert all(row["reserve_effects"] == row["undo_effects"] == row["closed"] == 1 for row in operations)
        assert len(values(cut["incidents"])) == 1
        assert all(context["faultCount"] == 1 for context in values(cut["activityExecutionContexts"]))
        roots = [context for context in values(state_at(result, "final")["activityExecutionContexts"]) if not context.get("parentContextId")]
        assert len(roots) == 1 and roots[0]["faultCount"] == 1
    if name == "undo-failure":
        assert values(state_at(result, "final")["bookmarks"]), "Intervention bookmark missing"
        assert any(entry["Status"] != "Compensated" for entry in final["journal"]["Entries"])
        assert all(row["undo_effects"] == row["closed"] == 0 for row in result["final"]["participant"]["operations"])
    if kill_at:
        before_journal = cut["properties"][KEY]
        after_journal = state_at(result, "final")["properties"][KEY]
        for field in ("Seed", "DefinitionVersion", "HandlerVersion", "Handoff", "ProtectedOriginalCause", "ProtectedRecoveryCause"):
            if before_journal.get(field):
                assert before_journal[field] == after_journal[field], field
    return result["classification"]


def validate_control(result):
    name = result["case"]
    assert name in CONTROL_CASES
    assert "assertion_failure" not in result and "control_error" not in result, "Recorded controller failure"
    assert_interruption(result, result["control"])
    assert result["control"]["exit"] == (2 if name == "inner-schema" else 0)
    events = result["control"]["events"]
    after_rows = result["after"]["elsa"].get("rows", [])
    after_state = after_rows[0].get("snapshot") if after_rows else None
    if name == "stale-writer":
        stale = next(event["value"] for event in events if event["kind"] == "stale-writer")
        assert stale["staleAccepted"] and stale["canonical"]["Revision"] < stale["newerRevision"], stale
        assert stale["canonical"]["OwnerToken"] == "synthetic-owner-a" and stale["newerOwner"] == "synthetic-owner-b", stale
        result["classification"] = "native-write-fence-absent"
    elif name in ("outer-corrupt", "outer-compression"):
        inspection = next(event["value"] for event in result["inspection"]["events"] if event["kind"] == "inspect")
        assert inspection["found"] and inspection["state"] is None, inspection
        assert after_state is None and not result["originalBytesRetainedInRow"], result
        assert any(event["kind"] == "resaved" for event in events), events
        result["classification"] = "native-outer-fallback-destroys-opaque-state-on-resave"
    elif name == "inner-schema":
        assert after_state["properties"]["elsa.experimental.native-compensation.e1"]["Schema"] == 999, after_state
        assert after_state["subStatus"] == "Faulted" and not result["originalBytesRetainedInRow"], after_state
        assert result["after"]["participant"]["calls"] == [], result["after"]["participant"]
        result["classification"] = "inner-codec-denies-effect-but-native-terminalizes-and-rewrites"
    elif name == "missing-work":
        assert not result["originalBytesRetainedInRow"], result
        result["classification"] = "native-required-work-reference-omitted"
    elif name in ("callback-owner", "callback-child"):
        assert any(row["slot"] == 1 and row["reserve_effects"] == 1 for row in result["after"]["participant"]["operations"]), result["after"]["participant"]
        assert not result["originalBytesRetainedInRow"], result
        result["classification"] = "native-callback-reference-omitted-before-new-effect"
    elif name == "definition-pin":
        assert after_state["definitionVersionId"] == "e1-definition-v2", after_state
        assert after_state["properties"]["elsa.experimental.native-compensation.e1"]["DefinitionVersion"] == "e1-definition-v1", after_state
        result["classification"] = "caller-supplied-mismatched-graph-accepted"
    if name in ("missing-work", "callback-owner", "callback-child"):
        inspection = next(event["value"]["state"] for event in result["inspection"]["events"] if event["kind"] == "inspect")
        if name == "missing-work":
            assert any(work["ActivityNodeId"] == "required-work-node-removed" for work in inspection["ScheduledActivities"])
        elif name == "callback-owner":
            assert any(callback["OwnerInstanceId"] == "required-owner-removed" for callback in inspection["CompletionCallbacks"])
        else:
            assert any(callback["ChildNodeId"] == "required-child-removed" for callback in inspection["CompletionCallbacks"])
        callbacks = values(after_state["completionCallbacks"])
        assert all(callback["ownerInstanceId"] != "required-owner-removed" and callback["childNodeId"] != "required-child-removed" for callback in callbacks)
        assert all(work["activityNodeId"] != "required-work-node-removed" for work in values(after_state["scheduledActivities"]))
        journal = after_state["properties"][KEY]
        entries = values(journal["Entries"])
        operations = result["after"]["participant"]["operations"]
        assert journal["Phase"] != "Compensated" and journal["OutwardCount"] == 0
        assert entries and any(entry["Status"] != "Compensated" for entry in entries)
        assert all(row["closed"] == row["undo_effects"] == 0 for row in operations), "Obligations unexpectedly settled"
        if name == "missing-work":
            assert (after_state["status"], after_state["subStatus"]) == ("Finished", "Faulted")
            assert not any(row["slot"] == 1 for row in operations), "Unexpected slot-1 effect"
        else:
            assert (after_state["status"], after_state["subStatus"]) == ("Running", "Suspended")
            assert {row["slot"] for row in operations} == {0, 1}
    return result["classification"]


def load_evidence(directory, category):
    expected = set(MATRIX_CASES) if category == "matrix" else set(CONTROL_CASES)
    files = sorted(Path(directory).glob("*/evidence.json"))
    records = []
    seen = set()
    for file in files:
        record = json.loads(file.read_text())
        name = record["case"]
        assert file.parent.name == name, "Directory/case mismatch"
        assert name not in seen, "Duplicate case"
        seen.add(name)
        previous = record.get("classification")
        classification = validate_matrix(record) if category == "matrix" else validate_control(record)
        assert previous == classification, "Classification mismatch"
        records.append((file, record))
    assert seen == expected, ("Unexpected case set", expected - seen, seen - expected)
    return records
