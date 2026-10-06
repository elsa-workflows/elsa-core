"""Strict synthetic native-export shape/browser binding; provenance promotion is separate."""
from __future__ import annotations

from datetime import datetime, timedelta
import hashlib
import json
import math
from pathlib import Path
import re
import stat

from paired_package_provenance import regular_file
import run_paired_package_browser_matrix as browser

MAX_BYTES = 1024 * 1024
SCHEMA = "https://elsaworkflows.io/schemas/workflow-definition/v3.0.0/schema.json"
PYTHON_ASSERTIONS = {"package_provenance", "browser_resources", "released_document"}
ACTIVITY_FIELDS = {"id", "nodeId", "name", "type", "version", "customProperties", "metadata"}


def _pairs(pairs):
    result = {}
    for key, value in pairs:
        browser.require(key not in result, "Duplicate released document JSON key")
        result[key] = value
    return result


def _constant(_value):
    raise ValueError("Nonfinite released document JSON value")


def _bounded(value, depth=0):
    browser.require(depth <= 16, "Released document nesting exceeds bound")
    if isinstance(value, dict):
        browser.require(len(value) <= 32, "Released document object exceeds bound")
        for key, item in value.items():
            browser.require(len(key) <= 128, "Released document key exceeds bound")
            _bounded(item, depth + 1)
    elif isinstance(value, list):
        browser.require(len(value) <= 32, "Released document array exceeds bound")
        for item in value:
            _bounded(item, depth + 1)
    elif isinstance(value, str):
        browser.require(len(value) <= 256, "Released document string exceeds bound")
    elif type(value) in (int, float):
        browser.require(abs(value) <= 10000 and math.isfinite(value), "Released document number exceeds bound")


def _object(value, fields):
    browser.require(isinstance(value, dict) and set(value) == set(fields), "Unexpected released document fields")
    return value


def _activity(value, fields, *, name, kind, parent):
    value = _object(value, ACTIVITY_FIELDS | fields)
    identifier = value["id"]
    browser.require(isinstance(identifier, str) and re.fullmatch(r"[0-9a-f]{1,16}", identifier), "Invalid synthetic activity identity")
    browser.require(value["nodeId"] == parent + ":" + identifier and value["name"] == name
                    and value["type"] == kind and type(value["version"]) is int and value["version"] == 1,
                    "Unexpected synthetic activity identity/type")
    return identifier


def validate_released_document(path: Path, source_cell: tuple[str, str, str], browser_receipt: dict) -> dict:
    """Return only a byte/identity binding, never modify or accept the retained matrix.

    3.9's shape comes from an actual Server/net10 native export. A 3.8 variant
    must be reviewed after its real export is observed. Fixture/package/loaded/
    resource provenance and portable file retention remain the caller's gates.
    """
    browser.require(type(source_cell) is tuple and len(source_cell) == 3, "Invalid released source cell")
    version, framework, host = source_cell
    browser.identity({"version": version, "framework": framework, "host": host})
    browser.require(version == "3.9.0", "Released document version has no observed shape")
    browser.validate_browser_receipt(browser_receipt, source_cell)
    browser.require(browser_receipt["result"] in {"passed", "incomplete"}, "Failed browser cannot author accepted released document")
    assertions = {item["name"]: item["passed"] for item in browser_receipt["assertions"]}
    browser.require(all(assertions[name] is True for name in browser.required_assertions(browser_receipt) - PYTHON_ASSERTIONS),
                    "Released document requires complete baseline browser assertions")
    proof = browser_receipt.get("proof", {})
    browser.require(proof.get("last_completed_stage") == "released_document_exported", "Native released export was not demonstrated")
    path = regular_file(browser._external_path(path))
    browser.require(stat.S_IMODE(path.stat().st_mode) == 0o600, "Released document must remain private")
    with path.open("rb") as stream:
        raw = stream.read(MAX_BYTES + 1)
    browser.require(0 < len(raw) <= MAX_BYTES, "Released document size exceeds bound")
    document_hash = hashlib.sha256(raw).hexdigest()
    browser.require(document_hash == proof.get("released_document_sha256"), "Released document differs from actual browser download")
    try:
        document = json.loads(raw.decode("utf-8"), object_pairs_hook=_pairs, parse_constant=_constant)
    except (UnicodeError, ValueError, RecursionError):
        raise ValueError("Invalid released document JSON") from None
    _bounded(document)
    document = _object(document, {"$schema", "id", "definitionId", "name", "createdAt", "version", "toolVersion",
                                  "variables", "inputs", "outputs", "outcomes", "customProperties", "isReadonly",
                                  "isSystem", "isLatest", "isPublished", "options", "root"})
    browser.require(document["$schema"] == SCHEMA and document["toolVersion"] == version + ".0"
                    and type(document["version"]) is int and document["version"] == 1, "Wrong released document schema/version")
    for field in ("id", "definitionId"):
        browser.require(isinstance(document[field], str) and re.fullmatch(r"[0-9a-f]{1,16}", document[field]), "Invalid synthetic workflow identity")
    browser.require(isinstance(document["name"], str) and re.fullmatch(r"paired-browser-[0-9a-f]{12}", document["name"]), "Non-synthetic workflow name")
    browser.require(isinstance(document["createdAt"], str) and re.fullmatch(r"\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d{1,7})?(?:Z|\+00:00)", document["createdAt"]), "Invalid native workflow timestamp")
    try:
        timestamp = datetime.fromisoformat(document["createdAt"])
    except ValueError:
        raise ValueError("Invalid native workflow timestamp") from None
    browser.require(timestamp.utcoffset() == timedelta(0), "Workflow timestamp must be UTC")
    browser.require(all(document[field] == [] for field in ("variables", "inputs", "outcomes"))
                    and document["customProperties"] == {} and document["options"] == {"autoUpdateConsumingWorkflows": False}
                    and document["options"]["autoUpdateConsumingWorkflows"] is False,
                    "Unexpected synthetic workflow configuration")
    browser.require(document["isReadonly"] is False and document["isSystem"] is False
                    and document["isLatest"] is True and document["isPublished"] is False, "Unexpected baseline workflow state")
    root = document["root"]
    root_id = _activity(root, {"activities", "variables", "connections"}, name="Flowchart1", kind="Elsa.Flowchart", parent="Workflow1")
    browser.require(root["customProperties"] == {"notFoundConnections": [], "canStartWorkflow": False, "runAsynchronously": False}
                    and root["customProperties"]["canStartWorkflow"] is False and root["customProperties"]["runAsynchronously"] is False
                    and root["metadata"] == {} and root["variables"] == [] and root["connections"] == [], "Unexpected synthetic flowchart configuration")
    browser.require(isinstance(root["activities"], list) and len(root["activities"]) == 1, "Synthetic workflow must have one activity")
    activity = root["activities"][0]
    activity_id = _activity(activity, {"outputName", "outputValue"}, name="SetOutput1", kind="Elsa.SetOutput", parent=root["nodeId"])
    browser.require(activity_id != root_id and activity["customProperties"] == {"canStartWorkflow": False, "runAsynchronously": False}
                    and activity["customProperties"]["canStartWorkflow"] is False and activity["customProperties"]["runAsynchronously"] is False,
                    "Unexpected synthetic activity configuration")
    browser.require(activity["outputName"] == {"typeName": "String", "expression": {"type": "Literal", "value": "sentinel"}}
                    and activity["outputValue"] == {"typeName": "Object", "expression": {"type": "Literal", "value": "synthetic-browser-value"}}, "Unexpected synthetic output expression")
    designer = _object(_object(activity["metadata"], {"designer"})["designer"], {"position", "size"})
    for field, names in (("position", {"x", "y"}), ("size", {"width", "height"})):
        coordinates = _object(designer[field], names)
        browser.require(all(type(value) in (int, float) and math.isfinite(value) and abs(value) <= 10000
                            and (field != "size" or value > 0) for value in coordinates.values()), "Invalid native designer geometry")
    browser.require(document["outputs"] == [{"type": "String", "name": "sentinel", "displayName": "sentinel",
                                             "description": "", "category": "Primitives"}], "Unexpected synthetic declared output")
    expected_hashes = {"definition_id_sha256": document["definitionId"], "activity_id_sha256": activity_id,
                       "value_sha256": activity["outputValue"]["expression"]["value"]}
    for key, value in expected_hashes.items():
        browser.require(proof.get(key) == hashlib.sha256(value.encode()).hexdigest(), "Released identity/value differs from browser proof")
    binding = {"schema": 1, "source_cell": {"version": version, "framework": framework, "host": host},
               "document_sha256": document_hash, "bytes": len(raw), "tool_version": document["toolVersion"],
               **{key: proof[key] for key in expected_hashes}}
    binding["binding_sha256"] = hashlib.sha256(json.dumps(binding, sort_keys=True, separators=(",", ":")).encode()).hexdigest()
    return binding
