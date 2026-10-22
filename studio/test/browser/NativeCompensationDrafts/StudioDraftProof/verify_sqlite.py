#!/usr/bin/env python3
"""Read-only verification for the isolated compensation draft database.

The request carries expected step names and non-secret literal values on stdin.
Output contains only booleans, row count, and SHA-256 digests; names, IDs,
definition JSON, entered values, and database contents stay in-process.
"""
import argparse
import hashlib
import json
import sqlite3
import sys
from pathlib import Path


def get_case_insensitive(value, key):
    if not isinstance(value, dict):
        return None
    for candidate, item in value.items():
        if candidate.casefold() == key.casefold():
            return item
    return None


def quote_identifier(value):
    return '"' + value.replace('"', '""') + '"'


def canonical_digest(value):
    canonical = json.dumps(value, ensure_ascii=False, separators=(",", ":"), allow_nan=False)
    return hashlib.sha256(canonical.encode("utf-8")).hexdigest()


def value_digest(value):
    if isinstance(value, bytes):
        kind = "bytes"
        payload = value
    else:
        kind = type(value).__name__
        payload = json.dumps(value, ensure_ascii=False, separators=(",", ":"), allow_nan=False).encode("utf-8")
    return [kind, hashlib.sha256(payload).hexdigest()]


def literal_value(child):
    text = get_case_insensitive(child, "text")
    expression = get_case_insensitive(text, "expression")
    if not isinstance(expression, dict) or get_case_insensitive(expression, "type") != "Literal":
        raise ValueError("child-text-not-literal")
    value = get_case_insensitive(expression, "value")
    if not isinstance(value, str):
        raise ValueError("child-text-value")
    return value


def activity_projection(child, expected_literal, expected_version):
    child_id = get_case_insensitive(child, "id")
    child_type = get_case_insensitive(child, "type")
    child_version = get_case_insensitive(child, "version")
    if not isinstance(child_id, str) or not child_id:
        raise ValueError("child-identity")
    if child_type != "Elsa.WriteLine":
        raise ValueError("child-type")
    if type(child_version) is not int or child_version != expected_version:
        raise ValueError("child-pin")
    value = literal_value(child)
    if value != expected_literal:
        raise ValueError("child-literal-mismatch")
    return {
        "id": child_id,
        "type": child_type,
        "version": child_version,
        "literalSha256": hashlib.sha256(value.encode("utf-8")).hexdigest(),
    }


def verify_latest_definition(root, request):
    found = []

    def visit(value):
        if isinstance(value, dict):
            if get_case_insensitive(value, "type") == "Elsa.CompensationScope":
                found.append(value)
            for child in value.values():
                visit(child)
        elif isinstance(value, list):
            for child in value:
                visit(child)

    visit(root)
    if len(found) != 1:
        raise ValueError("scope-count")
    scope = found[0]
    if (get_case_insensitive(scope, "version") != 1 or
            get_case_insensitive(scope, "draftSchemaVersion") != 1):
        raise ValueError("scope-version")

    scope_id = get_case_insensitive(scope, "id")
    steps = get_case_insensitive(scope, "steps")
    expected_steps = request["expectedSteps"]
    if not isinstance(scope_id, str) or not scope_id or not isinstance(steps, list) or len(steps) != 2:
        raise ValueError("scope-shape")
    if not isinstance(expected_steps, list) or len(expected_steps) != 2:
        raise ValueError("expected-step-shape")

    identities = [scope_id]
    identity_steps = []
    layout_steps = []
    diagnostic_targets = []
    for actual_step, expected_step in zip(steps, expected_steps, strict=True):
        if not isinstance(actual_step, dict) or not isinstance(expected_step, dict):
            raise ValueError("step-shape")
        step_id = get_case_insensitive(actual_step, "stepId")
        step_name = get_case_insensitive(actual_step, "name")
        forward = get_case_insensitive(actual_step, "forward")
        handler = get_case_insensitive(actual_step, "handler")
        if not isinstance(step_id, str) or not step_id or not isinstance(step_name, str):
            raise ValueError("step-identity-or-name")
        if step_name != expected_step["stepName"]:
            raise ValueError("step-name-mismatch")
        if not isinstance(forward, dict) or not isinstance(handler, dict):
            raise ValueError("paired-children")

        forward_projection = activity_projection(
            forward, expected_step["forwardLiteral"], request["expectedChildVersion"]
        )
        handler_projection = activity_projection(
            handler, expected_step["handlerLiteral"], request["expectedChildVersion"]
        )
        identities.extend((step_id, forward_projection["id"], handler_projection["id"]))
        identity_steps.append({
            "stepId": step_id,
            "forward": forward_projection,
            "handler": handler_projection,
        })
        layout_steps.append({"stepId": step_id, "stepName": step_name})
        diagnostic_targets.extend((
            [scope_id, step_id, forward_projection["id"], handler_projection["id"]],
            [scope_id, step_id, forward_projection["id"]],
            [scope_id, step_id, handler_projection["id"]],
        ))

    if len(set(identities)) != len(identities):
        raise ValueError("duplicate-identities")
    identity_steps.sort(key=lambda item: item["stepId"])
    identity_digest = canonical_digest({"scopeId": scope_id, "steps": identity_steps})
    layout_digest = canonical_digest({"scopeId": scope_id, "steps": layout_steps})
    diagnostic_targets.sort(key=lambda item: json.dumps(item, ensure_ascii=False, separators=(",", ":")))
    return identity_digest, layout_digest, canonical_digest(diagnostic_targets)


def row_projection(row, column_names):
    return [
        [column, *value_digest(row[column])]
        for column in sorted(column_names, key=str.casefold)
    ]


def read_snapshot(database_path, request):
    uri = database_path.resolve(strict=True).as_uri() + "?mode=ro"
    connection = sqlite3.connect(uri, uri=True, timeout=3)
    try:
        connection.execute("PRAGMA query_only = ON")
        connection.execute("PRAGMA busy_timeout = 3000")
        tables = {
            row[0] for row in connection.execute(
                "SELECT name FROM sqlite_master WHERE type='table'"
            )
        }
        table = next((name for name in tables if name.casefold() in {
            "workflowdefinitions", "elsa.workflowdefinitions"
        }), None)
        if table is None:
            raise ValueError("definition-table")

        table_info = connection.execute(f"PRAGMA table_info({quote_identifier(table)})").fetchall()
        columns = {row[1].casefold(): row[1] for row in table_info}
        required = {"definitionid", "id", "name", "version", "islatest", "ispublished", "stringdata"}
        if not required.issubset(columns):
            raise ValueError("definition-columns")

        rows = connection.execute(
            f"SELECT * FROM {quote_identifier(table)} "
            f"WHERE {quote_identifier(columns['definitionid'])} = ?",
            (request["definitionId"],),
        ).fetchall()
        if not rows:
            raise ValueError("definition-rows-missing")
        column_names = [row[1] for row in table_info]
        row_dicts = [dict(zip(column_names, row, strict=True)) for row in rows]
        if any(row[columns["definitionid"]] != request["definitionId"] for row in row_dicts):
            raise ValueError("definition-id-mismatch")

        latest_rows = [row for row in row_dicts if row[columns["islatest"]] in (1, True, "1")]
        if len(latest_rows) != 1:
            raise ValueError("latest-row-count")
        latest = latest_rows[0]
        if latest[columns["name"]] != request["workflowName"]:
            raise ValueError("latest-name-mismatch")
        if type(latest[columns["version"]]) is not int:
            raise ValueError("latest-version-invalid")
        if latest[columns["ispublished"]] not in (0, False, "0", "false", "False"):
            raise ValueError("published")

        string_data = latest[columns["stringdata"]]
        if not isinstance(string_data, str) or not string_data:
            raise ValueError("root-json")
        root = json.loads(string_data)
        identity_sha256, layout_sha256, diagnostic_targets_sha256 = verify_latest_definition(root, request)

        rows_projection = [row_projection(row, column_names) for row in row_dicts]
        rows_projection.sort(key=lambda item: json.dumps(item, ensure_ascii=False, separators=(",", ":")))
        return {
            "identitySha256": identity_sha256,
            "layoutSha256": layout_sha256,
            "diagnosticTargetsSha256": diagnostic_targets_sha256,
            "rowSetSha256": canonical_digest(rows_projection),
            "definitionRowCount": len(rows),
        }
    finally:
        connection.close()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--database", type=Path, required=True)
    parser.add_argument("--mode", choices=("snapshot",), required=True)
    args = parser.parse_args()
    try:
        raw = sys.stdin.read(16_385)
        if len(raw) > 16_384:
            return 2
        request = json.loads(raw)
        required_strings = ("workflowName", "definitionId")
        if (not isinstance(request, dict) or not all(
                isinstance(request.get(key), str) and request[key] for key in required_strings)):
            return 2
        if type(request.get("expectedChildVersion")) is not int or request["expectedChildVersion"] <= 0:
            return 2
        expected_steps = request.get("expectedSteps")
        if not isinstance(expected_steps, list) or len(expected_steps) != 2:
            return 2
        for step in expected_steps:
            if (not isinstance(step, dict) or
                    not isinstance(step.get("stepName"), str) or not step["stepName"] or
                    not isinstance(step.get("forwardLiteral"), str) or not step["forwardLiteral"] or
                    not isinstance(step.get("handlerLiteral"), str) or not step["handlerLiteral"]):
                return 2
        snapshot = read_snapshot(args.database, request)
    except Exception:
        return 1

    print(json.dumps({
        "schemaVersion": 2,
        "status": "OK",
        **snapshot,
        "unpublished": True,
        "literalValuesVerified": True,
    }, separators=(",", ":")))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
