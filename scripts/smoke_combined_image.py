#!/usr/bin/env python3
"""Production-image UI and API smoke for the combined Server+Studio host.

This is a local throwaway proof, not a released image, candidate rebuild, or QA signoff.
It talks to an already-running container or optionally builds one from the current source tree.

Secrets are generated for the run and redacted from written artifacts.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import secrets
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
REDACT_KEYS = ("password", "passwd", "secret", "token", "signing", "authorization", "apikey", "api-key")
BLAZOR_NOT_FOUND = "Sorry, there's nothing at this address."
HTTP_TRIGGER_BASE = "/workflows"


def redact(value: Any) -> Any:
    if isinstance(value, dict):
        redacted = {}
        for key, item in value.items():
            if any(marker in str(key).lower() for marker in REDACT_KEYS):
                redacted[key] = "[redacted]"
            else:
                redacted[key] = redact(item)
        return redacted
    if isinstance(value, list):
        return [redact(item) for item in value]
    if isinstance(value, str):
        return re.sub(r"(Bearer\s+)[A-Za-z0-9._\-+=/]+", r"\1[redacted]", value)
    return value


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(redact(payload), indent=2) + "\n", encoding="utf-8")


def request_json(url: str, *, method: str = "GET", token: str | None = None, body: dict | None = None, timeout: int = 30) -> tuple[int, Any, dict[str, str]]:
    data = None if body is None else json.dumps(body).encode("utf-8")
    headers = {"Accept": "application/json"}
    if body is not None:
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = f"Bearer {token}"
    request = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            raw = response.read()
            payload = json.loads(raw.decode("utf-8")) if raw else None
            return response.status, payload, dict(response.headers)
    except urllib.error.HTTPError as error:
        raw = error.read()
        try:
            payload = json.loads(raw.decode("utf-8")) if raw else {"error": str(error)}
        except json.JSONDecodeError:
            payload = {"error": raw.decode("utf-8", "replace")}
        return error.code, payload, dict(error.headers)


def request_bytes(
    url: str,
    *,
    method: str = "GET",
    headers: dict[str, str] | None = None,
    body: bytes | None = None,
    timeout: int = 30,
) -> tuple[int, bytes, dict[str, str]]:
    request = urllib.request.Request(url, data=body, headers=headers or {}, method=method)
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            return response.status, response.read(), dict(response.headers)
    except urllib.error.HTTPError as error:
        return error.code, error.read(), dict(error.headers)


def wait_for_health(base_url: str, timeout_seconds: int = 120) -> None:
    deadline = time.time() + timeout_seconds
    last_error = None
    while time.time() < deadline:
        try:
            status, _, _ = request_bytes(f"{base_url}/health/live", timeout=5)
            if status == 200:
                return
            last_error = f"status {status}"
        except Exception as error:  # noqa: BLE001 - smoke waits for startup
            last_error = str(error)
        time.sleep(2)
    raise TimeoutError(f"Combined image did not become healthy: {last_error}")


def login(base_url: str, username: str, password: str, timeout_seconds: int = 90) -> str:
    deadline = time.time() + timeout_seconds
    last: tuple[int, Any] | None = None
    while time.time() < deadline:
        status, payload, _ = request_json(
            f"{base_url}/elsa/api/identity/login",
            method="POST",
            body={"username": username, "password": password},
        )
        last = (status, payload)
        if status == 200 and isinstance(payload, dict) and payload.get("isAuthenticated") and payload.get("accessToken"):
            return str(payload["accessToken"])
        time.sleep(2)
    raise RuntimeError(f"Login failed: {redact(last)}")


def unwrap_definition(payload: Any) -> dict[str, Any]:
    if not isinstance(payload, dict):
        return {}
    nested = payload.get("workflowDefinition")
    return nested if isinstance(nested, dict) else payload


def unwrap_execution_state(payload: Any) -> dict[str, Any]:
    if not isinstance(payload, dict):
        return {}
    nested = payload.get("workflowState")
    return nested if isinstance(nested, dict) else payload


def literal(type_name: str, value: Any) -> dict[str, Any]:
    return {"typeName": type_name, "expression": {"type": "Literal", "value": value}}


def http_endpoint(activity_id: str, path: str, method: str, *, can_start: bool) -> dict[str, Any]:
    return {
        "path": literal("String", path),
        "supportedMethods": {
            "typeName": "String[]",
            "expression": {"type": "Object", "value": json.dumps([method])},
        },
        "authorize": literal("Boolean", False),
        "id": activity_id,
        "name": activity_id,
        "type": "Elsa.HttpEndpoint",
        "version": 1,
        "customProperties": {
            "canStartWorkflow": can_start,
            "runAsynchronously": False,
        },
    }


def write_http_response(activity_id: str, body: str) -> dict[str, Any]:
    return {
        "statusCode": {
            "typeName": "System.Net.HttpStatusCode, System.Net.Primitives",
            "expression": {"type": "Literal", "value": "OK"},
        },
        "content": literal("Object", body),
        "id": activity_id,
        "name": activity_id,
        "type": "Elsa.WriteHttpResponse",
        "version": 1,
        "customProperties": {
            "canStartWorkflow": False,
            "runAsynchronously": False,
        },
    }


def connect(source: str, target: str) -> dict[str, Any]:
    return {"source": {"activity": source, "port": "Done"}, "target": {"activity": target, "port": "In"}}


def flowchart(message: str) -> dict[str, Any]:
    return {
        "id": "flowchart-1",
        "name": "Flowchart1",
        "type": "Elsa.Flowchart",
        "version": 1,
        "activities": [
            {
                "id": "write-line-1",
                "name": "WriteLine1",
                "type": "Elsa.WriteLine",
                "version": 1,
                "text": {
                    "typeName": "String",
                    "expression": {
                        "type": "Literal",
                        "value": message,
                    },
                },
            }
        ],
        "connections": [],
    }


def http_start_resume_flowchart(start_path: str, resume_path: str) -> dict[str, Any]:
    start = http_endpoint("http-start", start_path, "POST", can_start=True)
    started = write_http_response("write-started", "started")
    resume = http_endpoint("http-resume", resume_path, "GET", can_start=False)
    resumed = write_http_response("write-resumed", "resumed")
    return {
        "id": "http-flowchart-1",
        "name": "HttpFlowchart1",
        "type": "Elsa.Flowchart",
        "version": 1,
        "activities": [start, started, resume, resumed],
        "connections": [
            connect("http-start", "write-started"),
            connect("write-started", "http-resume"),
            connect("http-resume", "write-resumed"),
        ],
    }


def role_names(payload: Any) -> list[str]:
    if not isinstance(payload, dict):
        return []
    roles = payload.get("roles")
    if not isinstance(roles, list):
        return []
    names = []
    for role in roles:
        if isinstance(role, dict) and role.get("name"):
            names.append(str(role["name"]))
    return names


def definition_names(payload: Any) -> list[str]:
    if isinstance(payload, dict):
        items = payload.get("items") or payload.get("workflowDefinitions") or payload.get("data")
        if items is None and "name" in payload:
            return [str(payload["name"])]
    elif isinstance(payload, list):
        items = payload
    else:
        items = None
    if not isinstance(items, list):
        return []
    names = []
    for item in items:
        if isinstance(item, dict) and item.get("name"):
            names.append(str(item["name"]))
    return names


def has_studio_chrome(html: str) -> bool:
    return "designer.css" in html and "blazor.server.js" in html


def collect_failures(result: dict[str, Any]) -> list[str]:
    """Fail closed on readiness 503, compressed JS 500, Faulted instances, missing dashboard content, and HTTP regressions."""
    failures: list[str] = []
    routing = result.get("checks", {}).get("routing") or {}
    health = result.get("checks", {}).get("health") or {}
    assets = result.get("checks", {}).get("assets") or {}
    api = result.get("checks", {}).get("api") or {}
    identity = result.get("checks", {}).get("identity") or {}
    http_triggers = result.get("checks", {}).get("httpTriggers") or {}
    upgrade = result.get("checks", {}).get("upgrade") or {}

    if routing.get("loginStatus") != 200 or not routing.get("loginHasStudioChrome"):
        failures.append("login page did not serve Studio chrome")
    if routing.get("dashboardStatus") != 200 or not routing.get("dashboardUsesHostFallback"):
        failures.append("dashboard route did not serve the Studio host")
    if routing.get("dashboardIsBlazorNotFound"):
        failures.append("dashboard route served Blazor not-found instead of dashboard content")
    if routing.get("rootIsBlazorNotFound"):
        failures.append("GET / served Blazor not-found instead of dashboard content")
    if routing.get("workflowsDefinitionsStatus") != 200 or not routing.get("workflowsDefinitionsUsesHostFallback"):
        failures.append("workflows definitions route did not serve the Studio host")
    if routing.get("workflowsInstancesStatus") != 200 or not routing.get("workflowsInstancesUsesHostFallback"):
        failures.append("workflows instances route did not serve the Studio host")
    if routing.get("rootIsNotHealthText") is False:
        failures.append("GET / still looks like a health-check body")
    if health.get("live") != 200:
        failures.append("/health/live failed")
    if health.get("ready") != 200:
        failures.append("/health/ready failed")
    if assets.get("designerCssStatus") != 200 or (assets.get("designerCssBytes") or 0) < 16:
        failures.append("designer.css missing")
    if assets.get("designerEntryStatus") != 200 or (assets.get("designerEntryBytes") or 0) < 16:
        failures.append("designer.entry.js missing")
    if assets.get("blazorServerStatus") != 200:
        failures.append("blazor.server.js missing")
    if assets.get("compressionRequestStatus") != 200:
        failures.append("compressed designer.entry.js failed")
    if api.get("saveStatus") not in {200, 201} or api.get("editStatus") not in {200, 201}:
        failures.append("workflow save/edit failed")
    if api.get("publishStatus") not in {200, 201} or api.get("published") is not True:
        failures.append("workflow publish failed")
    if api.get("executeStatus") not in {200, 201} or not api.get("instanceId"):
        failures.append("workflow execute failed")
    if api.get("loadStatus") != 200 or api.get("loadedName") != "Combined Image WriteLine Edited":
        failures.append("workflow load roundtrip failed")
    if api.get("instanceStatus") != 200:
        failures.append("workflow instance persistence lookup failed")
    instance_status = api.get("instanceWorkflowStatus")
    instance_sub = api.get("instanceSubStatus")
    if instance_status == "Faulted" or instance_sub == "Faulted":
        failures.append("workflow instance Faulted")
    elif instance_status and instance_status != "Finished":
        failures.append("workflow instance did not finish")
    if identity.get("createStatus") not in {200, 201} or not identity.get("roleId"):
        failures.append("persisted identity role create failed")
    if identity.get("listStatus") != 200 or not identity.get("rolePresent"):
        failures.append("persisted identity role was not listed")
    if identity.get("expectedRoleMissing"):
        failures.append("expected identity role missing after restart")
    if http_triggers.get("startStatus") != 200 or "started" not in str(http_triggers.get("startBody") or ""):
        failures.append("HTTP trigger start on /workflows/{path} failed")
    if http_triggers.get("resumeStatus") != 200 or "resumed" not in str(http_triggers.get("resumeBody") or ""):
        failures.append("HTTP trigger resume on /workflows/{path} failed")
    if http_triggers.get("unmatchedStatus") != 404 or http_triggers.get("unmatchedServedStudio"):
        failures.append("unmatched HTTP trigger path did not 404")
    if upgrade.get("expectedWorkflowMissing"):
        failures.append("expected prior-volume workflow missing from elsa_workflows.db")
    return failures


def wait_for_http_trigger(url: str, *, method: str = "GET", timeout_seconds: int = 60) -> tuple[int, bytes]:
    deadline = time.time() + timeout_seconds
    last_status, last_body = 0, b""
    while time.time() < deadline:
        last_status, last_body, _ = request_bytes(url, method=method, timeout=10)
        if last_status != 404:
            return last_status, last_body
        time.sleep(1)
    return last_status, last_body


def run_smoke(base_url: str, username: str, password: str, artifacts: Path) -> dict[str, Any]:
    wait_for_health(base_url)
    result: dict[str, Any] = {"baseUrl": base_url, "checks": {}}

    login_status, login_html, _ = request_bytes(f"{base_url}/login")
    dashboard_status, dashboard_html, _ = request_bytes(f"{base_url}/dashboard")
    workflows_status, workflows_html, _ = request_bytes(f"{base_url}/workflows/definitions")
    instances_status, instances_html, _ = request_bytes(f"{base_url}/workflows/instances")
    root_status, root_html, _ = request_bytes(f"{base_url}/")
    live_status, _, _ = request_bytes(f"{base_url}/health/live")
    ready_status, _, _ = request_bytes(f"{base_url}/health/ready")
    designer_status, designer_css, designer_headers = request_bytes(
        f"{base_url}/_content/Elsa.Studio.Workflows.Designer/designer.css",
        headers={"Accept-Encoding": "gzip"},
    )
    entry_status, designer_js, _ = request_bytes(f"{base_url}/_content/Elsa.Studio.Workflows.Designer/designer.entry.js")
    blazor_status, _, _ = request_bytes(f"{base_url}/_framework/blazor.server.js")
    compressed_status, _, compressed_headers = request_bytes(
        f"{base_url}/_content/Elsa.Studio.Workflows.Designer/designer.entry.js",
        headers={"Accept-Encoding": "gzip"},
    )

    html_blobs = {
        "login": login_html.decode("utf-8", "replace"),
        "dashboard": dashboard_html.decode("utf-8", "replace"),
        "workflows": workflows_html.decode("utf-8", "replace"),
        "instances": instances_html.decode("utf-8", "replace"),
        "root": root_html.decode("utf-8", "replace"),
    }
    result["checks"]["routing"] = {
        "loginStatus": login_status,
        "dashboardStatus": dashboard_status,
        "workflowsDefinitionsStatus": workflows_status,
        "workflowsInstancesStatus": instances_status,
        "rootStatus": root_status,
        "rootIsNotHealthText": "Healthy" not in html_blobs["root"] or "Elsa Studio" in html_blobs["root"] or "designer.css" in html_blobs["root"],
        "loginHasStudioChrome": has_studio_chrome(html_blobs["login"]),
        "dashboardUsesHostFallback": dashboard_status == 200 and "designer.css" in html_blobs["dashboard"],
        "dashboardIsBlazorNotFound": BLAZOR_NOT_FOUND in html_blobs["dashboard"],
        "rootIsBlazorNotFound": BLAZOR_NOT_FOUND in html_blobs["root"],
        "workflowsDefinitionsUsesHostFallback": workflows_status == 200 and "designer.css" in html_blobs["workflows"],
        "workflowsInstancesUsesHostFallback": instances_status == 200 and "designer.css" in html_blobs["instances"],
    }
    result["checks"]["health"] = {
        "live": live_status,
        "ready": ready_status,
        "rootIsNotLiveHealthEndpoint": root_status == 200 and live_status == 200,
    }
    result["checks"]["assets"] = {
        "designerCssStatus": designer_status,
        "designerCssBytes": len(designer_css),
        "designerEntryStatus": entry_status,
        "designerEntryBytes": len(designer_js),
        "blazorServerStatus": blazor_status,
        "contentEncoding": compressed_headers.get("Content-Encoding") or designer_headers.get("Content-Encoding") or "",
        "compressionRequestStatus": compressed_status,
    }

    token = login(base_url, username, password)
    definition_id = f"combined-smoke-{secrets.token_hex(4)}"
    save_status, saved, _ = request_json(
        f"{base_url}/elsa/api/workflow-definitions",
        method="POST",
        token=token,
        body={
            "publish": False,
            "model": {
                "definitionId": definition_id,
                "name": "Combined Image WriteLine",
                "root": flowchart("hello from combined image"),
            },
        },
    )
    edit_status, edited, _ = request_json(
        f"{base_url}/elsa/api/workflow-definitions",
        method="POST",
        token=token,
        body={
            "publish": False,
            "model": {
                "definitionId": definition_id,
                "name": "Combined Image WriteLine Edited",
                "root": flowchart("hello from combined image after edit"),
            },
        },
    )
    publish_status, published, _ = request_json(
        f"{base_url}/elsa/api/workflow-definitions/{definition_id}/publish",
        method="POST",
        token=token,
        body={},
    )
    execute_status, executed, _ = request_json(
        f"{base_url}/elsa/api/workflow-definitions/{definition_id}/execute",
        method="POST",
        token=token,
        body={},
    )
    loaded_status, loaded, _ = request_json(
        f"{base_url}/elsa/api/workflow-definitions/by-definition-id/{definition_id}?versionOptions=Latest",
        token=token,
    )
    saved_definition = unwrap_definition(saved)
    edited_definition = unwrap_definition(edited)
    published_definition = unwrap_definition(published)
    loaded_definition = unwrap_definition(loaded)
    execution_state = unwrap_execution_state(executed)
    instance_id = execution_state.get("id")
    instance_status, instance = None, None
    if instance_id:
        instance_status, instance, _ = request_json(
            f"{base_url}/elsa/api/workflow-instances/{instance_id}",
            token=token,
        )

    result["checks"]["api"] = {
        "login": "ok",
        "saveStatus": save_status,
        "editStatus": edit_status,
        "publishStatus": publish_status,
        "executeStatus": execute_status,
        "loadStatus": loaded_status,
        "instanceStatus": instance_status,
        "definitionId": definition_id,
        "savedName": saved_definition.get("name"),
        "editedName": edited_definition.get("name"),
        "published": published_definition.get("isPublished"),
        "loadedName": loaded_definition.get("name"),
        "loadedPublished": loaded_definition.get("isPublished"),
        "instanceId": instance_id,
        "instanceWorkflowStatus": (instance or {}).get("status") if isinstance(instance, dict) else execution_state.get("status"),
        "instanceSubStatus": (instance or {}).get("subStatus") if isinstance(instance, dict) else execution_state.get("subStatus"),
    }

    role_name = os.environ.get("ELSA_COMBINED_ROLE_NAME") or f"qa-persist-role-{secrets.token_hex(4)}"
    create_status, created_role, _ = request_json(
        f"{base_url}/elsa/api/identity/roles",
        method="POST",
        token=token,
        body={"name": role_name, "permissions": ["identity/roles:view"]},
    )
    list_status, listed_roles, _ = request_json(f"{base_url}/elsa/api/identity/roles", token=token)
    listed_names = role_names(listed_roles)
    expected_role = os.environ.get("ELSA_COMBINED_EXPECT_ROLE")
    result["checks"]["identity"] = {
        "createStatus": create_status,
        "listStatus": list_status,
        "roleName": role_name,
        "roleId": created_role.get("id") if isinstance(created_role, dict) else None,
        "rolePresent": role_name in listed_names,
        "listedNames": listed_names,
        "expectedRole": expected_role,
        "expectedRoleMissing": bool(expected_role) and expected_role not in listed_names,
    }

    start_path = f"qa-start-{secrets.token_hex(3)}"
    resume_path = f"qa-resume-{secrets.token_hex(3)}"
    http_definition_id = f"combined-http-{secrets.token_hex(4)}"
    http_save_status, _, _ = request_json(
        f"{base_url}/elsa/api/workflow-definitions",
        method="POST",
        token=token,
        body={
            "publish": True,
            "model": {
                "definitionId": http_definition_id,
                "name": "Combined Image HTTP Start Resume",
                "root": http_start_resume_flowchart(start_path, resume_path),
            },
        },
    )
    start_status, start_body = wait_for_http_trigger(f"{base_url}{HTTP_TRIGGER_BASE}/{start_path}", method="POST")
    resume_status, resume_body = wait_for_http_trigger(f"{base_url}{HTTP_TRIGGER_BASE}/{resume_path}", method="GET")
    unmatched_status, unmatched_body, _ = request_bytes(f"{base_url}{HTTP_TRIGGER_BASE}/not-a-combined-http-endpoint")
    unmatched_html = unmatched_body.decode("utf-8", "replace")
    result["checks"]["httpTriggers"] = {
        "saveStatus": http_save_status,
        "startPath": f"{HTTP_TRIGGER_BASE}/{start_path}",
        "resumePath": f"{HTTP_TRIGGER_BASE}/{resume_path}",
        "startStatus": start_status,
        "startBody": start_body.decode("utf-8", "replace"),
        "resumeStatus": resume_status,
        "resumeBody": resume_body.decode("utf-8", "replace"),
        "unmatchedStatus": unmatched_status,
        "unmatchedServedStudio": "designer.css" in unmatched_html or BLAZOR_NOT_FOUND in unmatched_html,
    }

    expected_workflow = os.environ.get("ELSA_COMBINED_EXPECT_WORKFLOW")
    definitions_status, definitions_payload, _ = request_json(
        f"{base_url}/elsa/api/workflow-definitions?page=0&pageSize=100",
        token=token,
    )
    listed_definition_names = definition_names(definitions_payload)
    result["checks"]["upgrade"] = {
        "listStatus": definitions_status,
        "expectedWorkflow": expected_workflow,
        "listedNames": listed_definition_names,
        "expectedWorkflowMissing": bool(expected_workflow) and expected_workflow not in listed_definition_names,
        "sqliteFileName": "elsa_workflows.db",
    }

    result["failures"] = collect_failures(result)
    result["passed"] = not result["failures"]
    write_json(artifacts / "combined-image-smoke.json", result)
    write_json(artifacts / "combined-image-identity.json", result["checks"]["identity"])
    (artifacts / "login.html").write_text(html_blobs["login"], encoding="utf-8")
    (artifacts / "dashboard.html").write_text(html_blobs["dashboard"], encoding="utf-8")
    return result


def generate_throwaway_env() -> dict[str, str]:
    password = f"Throwaway-{secrets.token_urlsafe(12)}"
    signing_key = secrets.token_urlsafe(48)
    return {
        "ASPNETCORE_ENVIRONMENT": "Production",
        "HTTP_PORTS": "8080",
        "HTTP__BASEURL": "http://127.0.0.1:8080",
        "Backend__Url": "http://127.0.0.1:8080/elsa/api",
        "Identity__Tokens__SigningKey": signing_key,
        "DefaultAdminUser__AdminUserName": "admin",
        "DefaultAdminUser__AdminPassword": password,
        "DefaultAdminUser__AdminRoleName": "admin",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-url", default=os.environ.get("ELSA_COMBINED_SMOKE_BASE_URL", "http://127.0.0.1:13000"))
    parser.add_argument("--username", default=os.environ.get("ELSA_COMBINED_SMOKE_USERNAME", "admin"))
    parser.add_argument("--password", default=os.environ.get("ELSA_COMBINED_SMOKE_PASSWORD"))
    parser.add_argument("--artifacts", default=str(Path("/tmp/elsa-combined-image-smoke")))
    args = parser.parse_args()
    password = args.password
    if not password:
        print("Set ELSA_COMBINED_SMOKE_PASSWORD or pass --password for throwaway credentials.", file=sys.stderr)
        return 2
    artifacts = Path(args.artifacts)
    try:
        result = run_smoke(args.base_url.rstrip("/"), args.username, password, artifacts)
    except Exception as error:  # noqa: BLE001 - smoke must record failures honestly
        payload = {"passed": False, "failures": [str(error)]}
        write_json(artifacts / "combined-image-smoke.json", payload)
        print(json.dumps(redact(payload), indent=2))
        return 1
    print(json.dumps(redact(result), indent=2))
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
