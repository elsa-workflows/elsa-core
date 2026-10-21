#!/usr/bin/env python3
"""Separately authenticate and exercise the stored-definition Publish API.

This probe never reads or reuses browser authentication state. Tokens and API
diagnostic IDs remain in memory; the receipt-facing result has fixed IDs and
fixed status codes only.
"""
import json
import math
import os
import sqlite3
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path


REFUSAL_CODE = "NATIVE_COMPENSATION_DRAFT_ONLY"
REFUSAL_MESSAGE = "Native compensation scopes are draft-only and cannot be published or executed."
ASSERTION_IDS = (
    "NC-API-STORED-PUBLISH-REFUSAL",
    "NC-API-STORED-PUBLISH-TARGETED-DIAGNOSTICS",
    "NC-API-STORED-PUBLISH-ROWS-UNCHANGED",
)
MAX_RESPONSE_BYTES = 65_536


class ProbeFailure(Exception):
    def __init__(self, assertion_id, code):
        super().__init__(code)
        self.assertion_id = assertion_id
        self.code = code


def get_ci(value, key):
    if not isinstance(value, dict):
        return None
    for candidate, item in value.items():
        if isinstance(candidate, str) and candidate.casefold() == key.casefold():
            return item
    return None


def required_path(name):
    value = os.environ.get(name)
    if not value or not Path(value).is_absolute():
        raise ProbeFailure(ASSERTION_IDS[0], "api-input-invalid")
    return Path(value)


def loopback_api_base():
    raw = os.environ.get("ELSA_COMP_PROOF_BACKEND_URL", "")
    try:
        parsed = urllib.parse.urlsplit(raw)
        port = parsed.port
    except ValueError:
        raise ProbeFailure(ASSERTION_IDS[0], "api-origin-invalid") from None
    if (parsed.scheme != "http" or parsed.hostname not in {"127.0.0.1", "localhost", "::1"} or
            port is None or not 1024 <= port <= 65535 or parsed.username or parsed.password or
            parsed.query or parsed.fragment or parsed.path.rstrip("/") != "/elsa/api"):
        raise ProbeFailure(ASSERTION_IDS[0], "api-origin-invalid")
    return f"http://{parsed.netloc}/elsa/api"


def deadline_from_environment():
    try:
        seconds = float(os.environ["ELSA_COMP_PROOF_API_TIMEOUT_SECONDS"])
    except (KeyError, ValueError):
        raise ProbeFailure(ASSERTION_IDS[0], "api-timeout-invalid") from None
    if not math.isfinite(seconds) or seconds <= 0:
        raise ProbeFailure(ASSERTION_IDS[0], "api-timeout-invalid")
    return time.monotonic() + seconds


def remaining(deadline, assertion_id):
    value = deadline - time.monotonic()
    if value <= 0:
        raise ProbeFailure(assertion_id, "api-deadline-exceeded")
    return value


def bounded_timeout(deadline, assertion_id):
    return min(8.0, remaining(deadline, assertion_id))


class NoRedirectHandler(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, new_url):
        # A redirect is an unexpected result. Never follow it to another origin.
        return None


def make_opener():
    return urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirectHandler())


def post(opener, url, payload, bearer, deadline, assertion_id):
    body = None if payload is None else json.dumps(payload, separators=(",", ":")).encode("utf-8")
    headers = {"Accept": "application/json"}
    if body is not None:
        headers["Content-Type"] = "application/json"
    if bearer is not None:
        headers["Authorization"] = "Bearer " + bearer
    request = urllib.request.Request(url, data=body, headers=headers, method="POST")
    try:
        with opener.open(request, timeout=bounded_timeout(deadline, assertion_id)) as response:
            status = response.status
            raw = response.read(MAX_RESPONSE_BYTES + 1)
    except urllib.error.HTTPError as response:
        try:
            status = response.code
            raw = response.read(MAX_RESPONSE_BYTES + 1)
        finally:
            response.close()
    except Exception:
        raise ProbeFailure(assertion_id, "api-request-failed") from None
    if len(raw) > MAX_RESPONSE_BYTES:
        raise ProbeFailure(assertion_id, "api-response-too-large")
    return status, raw


def parse_json(raw, assertion_id):
    try:
        value = json.loads(raw)
    except Exception:
        raise ProbeFailure(assertion_id, "api-response-invalid") from None
    if not isinstance(value, dict):
        raise ProbeFailure(assertion_id, "api-response-invalid")
    return value


def authenticate(opener, api_base, deadline):
    username = os.environ.get("ELSA_COMP_PROOF_ADMIN_USERNAME")
    password = os.environ.get("ELSA_COMP_PROOF_ADMIN_PASSWORD")
    if not username or not password:
        raise ProbeFailure(ASSERTION_IDS[0], "api-credentials-missing")
    status, raw = post(opener, api_base + "/identity/login", {
        "username": username,
        "password": password,
    }, None, deadline, ASSERTION_IDS[0])
    if status != 200:
        raise ProbeFailure(ASSERTION_IDS[0], "api-login-rejected")
    response = parse_json(raw, ASSERTION_IDS[0])
    authenticated = get_ci(response, "isAuthenticated")
    token = get_ci(response, "accessToken")
    if authenticated is not True or not isinstance(token, str) or not token:
        raise ProbeFailure(ASSERTION_IDS[0], "api-login-rejected")
    return token


def load_private_capture(path):
    try:
        capture = json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        raise ProbeFailure(ASSERTION_IDS[0], "private-capture-invalid") from None
    if (not isinstance(capture, dict) or capture.get("schemaVersion") != 2 or
            not isinstance(capture.get("workflowName"), str) or not capture["workflowName"] or
            not isinstance(capture.get("definitionId"), str) or not capture["definitionId"] or
            not isinstance(capture.get("identitySha256"), str) or
            len(capture["identitySha256"]) != 64 or
            any(char not in "0123456789abcdef" for char in capture["identitySha256"]) or
            type(capture.get("expectedChildVersion")) is not int or capture["expectedChildVersion"] <= 0):
        raise ProbeFailure(ASSERTION_IDS[0], "private-capture-invalid")
    expected_steps = capture.get("expectedSteps")
    if not isinstance(expected_steps, list) or len(expected_steps) != 2:
        raise ProbeFailure(ASSERTION_IDS[0], "private-capture-invalid")
    for step in expected_steps:
        if (not isinstance(step, dict) or
                not all(isinstance(step.get(key), str) and step[key] for key in
                        ("stepName", "forwardLiteral", "handlerLiteral"))):
            raise ProbeFailure(ASSERTION_IDS[0], "private-capture-invalid")
    return capture


def run_sqlite_snapshot(verifier, python, database, capture, deadline, code):
    request = {
        "workflowName": capture["workflowName"],
        "definitionId": capture["definitionId"],
        "expectedChildVersion": capture["expectedChildVersion"],
        "expectedSteps": capture["expectedSteps"],
    }
    child_env = {key: os.environ[key] for key in ("PATH", "LANG", "LC_ALL") if key in os.environ}
    try:
        result = subprocess.run(
            [str(python), str(verifier), "--database", str(database), "--mode", "snapshot"],
            input=json.dumps(request, separators=(",", ":")),
            text=True,
            capture_output=True,
            env=child_env,
            timeout=bounded_timeout(deadline, code),
            check=False,
        )
    except Exception:
        raise ProbeFailure(code, "sqlite-snapshot-failed") from None
    if result.returncode != 0 or len(result.stdout) > 2_048:
        raise ProbeFailure(code, "sqlite-snapshot-failed")
    try:
        snapshot = json.loads(result.stdout)
    except Exception:
        raise ProbeFailure(code, "sqlite-snapshot-invalid") from None
    if (not isinstance(snapshot, dict) or snapshot.get("schemaVersion") != 2 or
            snapshot.get("status") != "OK" or snapshot.get("unpublished") is not True or
            snapshot.get("literalValuesVerified") is not True or
            type(snapshot.get("definitionRowCount")) is not int or snapshot["definitionRowCount"] < 1 or
            any(not isinstance(snapshot.get(key), str) or len(snapshot[key]) != 64 or
                any(char not in "0123456789abcdef" for char in snapshot[key])
                for key in ("identitySha256", "layoutSha256", "rowSetSha256"))):
        raise ProbeFailure(code, "sqlite-snapshot-invalid")
    return snapshot


def load_diagnostic_targets(database, definition_id, workflow_name):
    """Read only the target IDs needed to validate response diagnostics in memory."""
    uri = database.resolve(strict=True).as_uri() + "?mode=ro"
    connection = sqlite3.connect(uri, uri=True, timeout=3)
    try:
        connection.execute("PRAGMA query_only = ON")
        table = next((row[0] for row in connection.execute(
            "SELECT name FROM sqlite_master WHERE type='table'")
            if row[0].casefold() in {"workflowdefinitions", "elsa.workflowdefinitions"}), None)
        if table is None:
            raise ValueError()
        columns_info = connection.execute(
            'PRAGMA table_info("' + table.replace('"', '""') + '")').fetchall()
        columns = {row[1].casefold(): row[1] for row in columns_info}
        required = {"definitionid", "name", "islatest", "stringdata"}
        if not required.issubset(columns):
            raise ValueError()
        quoted_table = '"' + table.replace('"', '""') + '"'
        quote_column = lambda name: '"' + name.replace('"', '""') + '"'
        rows = connection.execute(
            f"SELECT {quote_column(columns['name'])}, {quote_column(columns['stringdata'])} FROM {quoted_table} "
            f"WHERE {quote_column(columns['definitionid'])} = ? AND {quote_column(columns['islatest'])} = 1",
            (definition_id,),
        ).fetchall()
        if len(rows) != 1 or rows[0][0] != workflow_name or not isinstance(rows[0][1], str):
            raise ValueError()
        root = json.loads(rows[0][1])
    except Exception:
        raise ProbeFailure(ASSERTION_IDS[1], "diagnostic-targets-unavailable") from None
    finally:
        connection.close()

    scopes = []

    def visit(value):
        if isinstance(value, dict):
            if get_ci(value, "type") == "Elsa.CompensationScope":
                scopes.append(value)
            for child in value.values():
                visit(child)
        elif isinstance(value, list):
            for child in value:
                visit(child)

    visit(root)
    if len(scopes) != 1:
        raise ProbeFailure(ASSERTION_IDS[1], "diagnostic-targets-unavailable")
    scope = scopes[0]
    scope_id = get_ci(scope, "id")
    steps = get_ci(scope, "steps")
    if not isinstance(scope_id, str) or not scope_id or not isinstance(steps, list) or len(steps) != 2:
        raise ProbeFailure(ASSERTION_IDS[1], "diagnostic-targets-unavailable")
    targets = {}
    for step in steps:
        if not isinstance(step, dict):
            raise ProbeFailure(ASSERTION_IDS[1], "diagnostic-targets-unavailable")
        step_id = get_ci(step, "stepId")
        forward = get_ci(step, "forward")
        handler = get_ci(step, "handler")
        forward_id = get_ci(forward, "id")
        handler_id = get_ci(handler, "id")
        if (not isinstance(step_id, str) or not step_id or step_id in targets or
                not isinstance(forward_id, str) or not forward_id or
                not isinstance(handler_id, str) or not handler_id):
            raise ProbeFailure(ASSERTION_IDS[1], "diagnostic-targets-unavailable")
        targets[step_id] = {"forward": forward_id, "handler": handler_id}
    return scope_id, targets


def validate_refusal(payload, status):
    if status != 400 or get_ci(payload, "code") != REFUSAL_CODE:
        raise ProbeFailure(ASSERTION_IDS[0], "stored-publish-refusal-mismatch")
    errors = get_ci(payload, "errors")
    general_errors = get_ci(errors, "generalErrors")
    if not isinstance(general_errors, list) or len(general_errors) != 6 or any(
            item != REFUSAL_MESSAGE for item in general_errors):
        raise ProbeFailure(ASSERTION_IDS[0], "stored-publish-message-mismatch")


def validate_diagnostics(payload, scope_id, targets):
    data = get_ci(payload, "data")
    diagnostics = get_ci(data, "diagnostics")
    if not isinstance(diagnostics, list) or len(diagnostics) != 6:
        raise ProbeFailure(ASSERTION_IDS[1], "stored-publish-diagnostics-mismatch")
    expected = {}
    for step_id, children in targets.items():
        expected[(step_id, "scope")] = [children["forward"], children["handler"]]
        expected[(step_id, "forward")] = [children["forward"]]
        expected[(step_id, "handler")] = [children["handler"]]
    observed = {}
    for diagnostic in diagnostics:
        step_id = get_ci(diagnostic, "stepId")
        role = get_ci(diagnostic, "role")
        key = (step_id, role)
        if key in observed or key not in expected:
            raise ProbeFailure(ASSERTION_IDS[1], "stored-publish-diagnostics-mismatch")
        related = get_ci(diagnostic, "relatedActivityIds")
        mappings = get_ci(diagnostic, "mappingIds")
        if (get_ci(diagnostic, "scopeActivityId") != scope_id or
                not isinstance(related, list) or related != expected[key] or mappings != []):
            raise ProbeFailure(ASSERTION_IDS[1], "stored-publish-diagnostics-mismatch")
        observed[key] = True
    if set(observed) != set(expected):
        raise ProbeFailure(ASSERTION_IDS[1], "stored-publish-diagnostics-mismatch")


def write_result(path, records):
    path.parent.mkdir(mode=0o700, parents=True, exist_ok=True)
    temporary = path.with_name(path.name + ".tmp")
    payload = {
        "schemaVersion": 1,
        "interpretation": "NOT_EVALUATED_BY_SUPERVISOR",
        "assertions": records,
    }
    with temporary.open("x", encoding="utf-8") as stream:
        json.dump(payload, stream, separators=(",", ":"))
        stream.write("\n")
    os.chmod(temporary, 0o600)
    os.replace(temporary, path)
    os.chmod(path, 0o600)


def run_api_proof(capture, database, verifier, python, api_base, deadline, records):
    opener = make_opener()
    token = authenticate(opener, api_base, deadline)
    scope_id, targets = load_diagnostic_targets(
        database, capture["definitionId"], capture["workflowName"])
    before = run_sqlite_snapshot(verifier, python, database, capture, deadline, ASSERTION_IDS[2])
    if before["identitySha256"] != capture["identitySha256"]:
        raise ProbeFailure(ASSERTION_IDS[2], "stored-draft-identity-mismatch")
    encoded_id = urllib.parse.quote(capture["definitionId"], safe="")
    publish_response = None
    publish_failure = None
    try:
        publish_response = post(
            opener,
            api_base + "/workflow-definitions/" + encoded_id + "/publish",
            {},  # Match the empty JSON request used by Elsa's API client (no body yields HTTP 415).
            token,
            deadline,
            ASSERTION_IDS[0],
        )
    except ProbeFailure as failure:
        publish_failure = failure

    after = run_sqlite_snapshot(verifier, python, database, capture, deadline, ASSERTION_IDS[2])
    if (after["rowSetSha256"] != before["rowSetSha256"] or
            after["identitySha256"] != before["identitySha256"] or
            after["layoutSha256"] != before["layoutSha256"] or
            after["definitionRowCount"] != before["definitionRowCount"]):
        raise ProbeFailure(ASSERTION_IDS[2], "stored-publish-mutated-definition-rows")
    records[2] = {"id": ASSERTION_IDS[2], "status": "PASS", "code": "all-definition-rows-unchanged"}
    if publish_failure is not None:
        raise publish_failure
    status, raw = publish_response
    payload = parse_json(raw, ASSERTION_IDS[0])
    validate_refusal(payload, status)
    records[0] = {"id": ASSERTION_IDS[0], "status": "PASS", "code": "fixed-refusal-verified"}
    validate_diagnostics(payload, scope_id, targets)
    records[1] = {"id": ASSERTION_IDS[1], "status": "PASS", "code": "targeted-diagnostics-verified"}


def run_probe():
    records = [{"id": item, "status": "NOT_RUN", "code": None} for item in ASSERTION_IDS]
    api_result = None
    try:
        api_result = required_path("ELSA_COMP_PROOF_API_ASSERTION_RESULTS")
        capture = load_private_capture(required_path("ELSA_COMP_PROOF_PRIVATE_CAPTURE"))
        database = required_path("ELSA_COMP_PROOF_SQLITE_PATH")
        verifier = required_path("ELSA_COMP_PROOF_SQLITE_VERIFIER")
        python = required_path("ELSA_COMP_PROOF_PYTHON")
        api_base = loopback_api_base()
        deadline = deadline_from_environment()
        run_api_proof(capture, database, verifier, python, api_base, deadline, records)
        return 0, records
    except ProbeFailure as failure:
        record = next((item for item in records if item["id"] == failure.assertion_id), records[0])
        record.update(status="FAIL", code=failure.code)
        return 1, records
    except Exception:
        records[0].update(status="FAIL", code="probe-failed")
        return 1, records
    finally:
        if api_result is not None:
            write_result(api_result, records)


if __name__ == "__main__":
    try:
        exit_code, results = run_probe()
        raise SystemExit(exit_code)
    except Exception:
        # Never emit credentials, token data, response bodies, or exception text.
        raise SystemExit(2)
