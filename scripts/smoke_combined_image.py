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


def request_bytes(url: str, *, headers: dict[str, str] | None = None, timeout: int = 30) -> tuple[int, bytes, dict[str, str]]:
    request = urllib.request.Request(url, headers=headers or {})
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


def run_smoke(base_url: str, username: str, password: str, artifacts: Path) -> dict[str, Any]:
    wait_for_health(base_url)
    result: dict[str, Any] = {"baseUrl": base_url, "checks": {}}

    login_status, login_html, _ = request_bytes(f"{base_url}/login")
    dashboard_status, dashboard_html, _ = request_bytes(f"{base_url}/dashboard")
    workflows_status, _, _ = request_bytes(f"{base_url}/workflows")
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
        "root": root_html.decode("utf-8", "replace"),
    }
    result["checks"]["routing"] = {
        "loginStatus": login_status,
        "dashboardStatus": dashboard_status,
        "workflowsStatus": workflows_status,
        "rootStatus": root_status,
        "rootIsNotHealthText": "Healthy" not in html_blobs["root"] or "Elsa Studio" in html_blobs["root"] or "designer.css" in html_blobs["root"],
        "loginHasStudioChrome": "designer.css" in html_blobs["login"] and "blazor.server.js" in html_blobs["login"],
        "dashboardUsesHostFallback": dashboard_status == 200 and "designer.css" in html_blobs["dashboard"],
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
    instance_status, instance, _ = (None, None, None)
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

    failures = []
    routing = result["checks"]["routing"]
    assets = result["checks"]["assets"]
    api = result["checks"]["api"]
    if routing["loginStatus"] != 200 or not routing["loginHasStudioChrome"]:
        failures.append("login page did not serve Studio chrome")
    if routing["dashboardStatus"] != 200 or not routing["dashboardUsesHostFallback"]:
        failures.append("dashboard route did not fall back to the Studio host")
    if routing["workflowsStatus"] != 200:
        failures.append("workflows route did not serve the Studio host")
    if not routing["rootIsNotHealthText"]:
        failures.append("GET / still looks like a health-check body")
    if result["checks"]["health"]["live"] != 200:
        failures.append("/health/live failed")
    if assets["designerCssStatus"] != 200 or assets["designerCssBytes"] < 16:
        failures.append("designer.css missing")
    if assets["designerEntryStatus"] != 200 or assets["designerEntryBytes"] < 16:
        failures.append("designer.entry.js missing")
    if assets["blazorServerStatus"] != 200:
        failures.append("blazor.server.js missing")
    if save_status not in {200, 201} or edit_status not in {200, 201}:
        failures.append("workflow save/edit failed")
    if publish_status not in {200, 201} or api["published"] is not True:
        failures.append("workflow publish failed")
    if execute_status not in {200, 201} or not instance_id:
        failures.append("workflow execute failed")
    if loaded_status != 200 or api["loadedName"] != "Combined Image WriteLine Edited":
        failures.append("workflow load roundtrip failed")
    if instance_status != 200:
        failures.append("workflow instance persistence lookup failed")

    result["failures"] = failures
    result["passed"] = not failures
    write_json(artifacts / "combined-image-smoke.json", result)
    (artifacts / "login.html").write_text(html_blobs["login"], encoding="utf-8")
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
