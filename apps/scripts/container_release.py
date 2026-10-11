#!/usr/bin/env python3
"""Build, verify, and publish versioned Elsa Apps container images."""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import io
import json
import os
import re
import secrets
import subprocess
import sys
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path
from typing import Any, Iterable


_NUM = r"(?:0|[1-9][0-9]*)"
_PRERELEASE_ID = rf"(?:{_NUM}|(?:[0-9]*[A-Za-z-][0-9A-Za-z-]*))"
ELSA_VERSION = re.compile(rf"^3\.{_NUM}\.{_NUM}(?:-{_PRERELEASE_ID}(?:\.{_PRERELEASE_ID})*)?$")
REQUIRED_PLATFORMS = ("linux/amd64", "linux/arm64")
APPS_REPOSITORY = "elsa-workflows/elsa-apps"
BEARER_API_ENDPOINT = "/elsa/api/workflow-definitions?page=0&pageSize=1"
DASHBOARD_API_ENDPOINT = "/elsa/api/dashboard/overview?range=24h&includeSystem=false"
EXTENSION_PACKAGES = (
    "Elsa.Agents.",
    "Elsa.Logging.",
    "Elsa.Studio.Agents",
    "Elsa.Studio.Http.Webhooks",
    "Elsa.Studio.WorkflowContexts",
)

IMAGES: tuple[dict[str, Any], ...] = (
    {
        "id": "server",
        "name": "server",
        "repository": "elsaworkflows/elsa-server-app",
        "dockerfile": "src/Elsa.Server/Dockerfile",
        "port": 8080,
        "assetsPath": "/app/elsa-project.assets.json",
        "packages": ["core", "extensions"],
        "smokeAuth": True,
        "aliases": [{"name": "server-alias", "repository": "elsaworkflows/elsa-server"}],
    },
    {
        "id": "studio-server",
        "name": "studio-server",
        "repository": "elsaworkflows/elsa-studio-blazor-server-app",
        "dockerfile": "src/Elsa.Studio.BlazorServer/Dockerfile",
        "port": 8080,
        "smokeAssets": ["/_framework/blazor.server.js"],
        "assetsPath": "/app/elsa-project.assets.json",
        "packages": ["core", "studio", "extensions"],
    },
    {
        "id": "studio-wasm",
        "name": "studio-wasm",
        "repository": "elsaworkflows/elsa-studio-blazor-wasm-app",
        "dockerfile": "src/Elsa.Studio.BlazorWasm/Dockerfile",
        "port": 8080,
        "smokeAssets": [
            "/_framework/dotnet.js",
            "/_framework/blazor.webassembly.js",
            "/Elsa.Studio.BlazorWasm.Client.styles.css",
        ],
        "assetsPath": "/app/elsa-project.assets.json",
        "packages": ["core", "studio", "extensions"],
        "aliases": [{"name": "studio-wasm-alias", "repository": "elsaworkflows/elsa-studio"}],
    },
    {
        "id": "studio-wasm-standalone",
        "name": "studio-wasm-standalone",
        "repository": "elsaworkflows/elsa-studio-blazor-wasm-standalone-app",
        "dockerfile": "src/Elsa.Studio.BlazorWasm.Client/Dockerfile",
        "port": 80,
        "smokeAssets": [
            "/_framework/dotnet.js",
            "/_framework/blazor.webassembly.js",
            "/Elsa.Studio.BlazorWasm.Client.styles.css",
        ],
        "assetsPath": "/usr/share/nginx/html/_framework/elsa-project.assets.json",
        "packages": ["core", "studio", "extensions"],
    },
    {
        "id": "server-studio-server",
        "name": "server-studio-server",
        "repository": "elsaworkflows/elsa-server-studio-blazor-server-app",
        "dockerfile": "src/Elsa.Server.Studio.BlazorServer/Dockerfile",
        "port": 8080,
        "smokeAssets": ["/_framework/blazor.server.js"],
        "assetsPath": "/app/elsa-project.assets.json",
        "packages": ["core", "studio", "extensions"],
        "smokeAuth": True,
    },
    {
        "id": "server-studio-wasm",
        "name": "server-studio-wasm",
        "repository": "elsaworkflows/elsa-server-studio-blazor-wasm-app",
        "dockerfile": "src/Elsa.Server.Studio.BlazorWasm/Dockerfile",
        "port": 8080,
        "smokeAssets": ["/_framework/dotnet.js", "/_framework/blazor.webassembly.js"],
        "assetsPath": "/app/elsa-project.assets.json",
        "packages": ["core", "studio", "extensions"],
        "smokeAuth": True,
    },
)


class ReleaseError(RuntimeError):
    """Raised when release inputs or image evidence fail validation."""


def validate_inventory(images: Iterable[dict[str, Any]] = IMAGES) -> None:
    entries = list(images)
    if len(entries) != 6:
        raise ReleaseError(f"Expected six application images, found {len(entries)}")

    ids = [item.get("id") for item in entries]
    repositories = [item.get("repository") for item in entries]
    dockerfiles = [item.get("dockerfile") for item in entries]
    if len(set(ids)) != 6 or len(set(repositories)) != 6 or len(set(dockerfiles)) != 6:
        raise ReleaseError("Image ids, repositories, and Dockerfiles must be unique")

    aliases = {
        item["id"]: alias["repository"]
        for item in entries
        for alias in item.get("aliases", [])
    }
    expected = {
        "studio-wasm": "elsaworkflows/elsa-studio",
        "server": "elsaworkflows/elsa-server",
    }
    if aliases != expected:
        raise ReleaseError(f"Image aliases do not match the release contract: {aliases}")

    for item in entries:
        if not re.fullmatch(r"[a-z0-9][a-z0-9._-]*", str(item.get("id", ""))):
            raise ReleaseError(f"Invalid image id: {item.get('id')!r}")
        if not Path(str(item.get("dockerfile", ""))).is_file():
            raise ReleaseError(f"Dockerfile does not exist: {item.get('dockerfile')}")
        if item.get("port") not in (80, 8080):
            raise ReleaseError(f"Unsupported smoke-test port for {item.get('id')}")


def read_package_versions(path: Path) -> dict[str, str]:
    root = ET.parse(path).getroot()
    values: dict[str, str] = {}
    for name in ("ElsaVersion", "ElsaStudioVersion", "ElsaExtensionsVersion"):
        node = root.find(f".//{name}")
        if node is None or not node.text:
            raise ReleaseError(f"Missing {name} in {path}")
        values[name] = node.text.strip()
    for family, version in values.items():
        if not is_elsa_version(version):
            raise ReleaseError(f"Unsupported {family} package version: {version!r}")
    return values


def normalize_bool(value: str | bool | None) -> bool:
    if isinstance(value, bool):
        return value
    return str(value or "").strip().lower() in ("1", "true", "yes", "on")


def is_elsa_version(version: str) -> bool:
    return bool(ELSA_VERSION.fullmatch(version))


def parse_image_selection(selection: str, images: Iterable[dict[str, Any]] = IMAGES) -> list[dict[str, Any]]:
    entries = list(images)
    if selection.strip().lower() in ("", "all"):
        return entries
    requested = [name.strip() for name in selection.split(",") if name.strip()]
    if not requested or len(requested) != len(set(requested)):
        raise ReleaseError("Image selection must contain unique comma-separated profile names")
    by_name = {item["name"]: item for item in entries}
    unknown = set(requested) - set(by_name)
    if unknown:
        raise ReleaseError(f"Unknown image profile name(s): {', '.join(sorted(unknown))}")
    return [item for item in entries if item["name"] in requested]


def package_families_for(images: Iterable[dict[str, Any]]) -> set[str]:
    return {family for image in images for family in image["packages"]}


def validate_publication(event: str, version: str, publish: bool, ref: str, release_tag: str = "") -> None:
    if not is_elsa_version(version):
        raise ReleaseError(f"Expected a strict Elsa 3.x SemVer release version, got {version!r}")

    if event == "pull_request":
        if publish:
            raise ReleaseError("Pull requests cannot publish container images")
        return

    if event == "release":
        if release_tag != version or ref != f"refs/tags/{version}":
            raise ReleaseError("Published release tag and workflow ref must match the exact version")
        if not publish:
            raise ReleaseError("A published release event must publish its container images")
        return

    if event == "workflow_dispatch":
        if publish and ref not in ("refs/heads/main", f"refs/tags/{version}"):
            raise ReleaseError("Publishing is allowed only from main or the matching release tag")
        return

    raise ReleaseError(f"Unsupported workflow event: {event!r}")


def validate_supersede_request(
    run_id: str,
    event: str,
    version: str,
    publish: bool,
    ref: str,
    selected_images: list[dict[str, Any]],
    package_versions: dict[str, str],
) -> None:
    if not run_id:
        return
    if not re.fullmatch(r"[1-9][0-9]*", run_id):
        raise ReleaseError("supersede_run_id must be a positive GitHub Actions run id")
    if event != "workflow_dispatch" or not publish or ref != "refs/heads/main":
        raise ReleaseError("A correction requires a publishing workflow_dispatch from main")
    if selected_images != list(IMAGES):
        raise ReleaseError("A correction requires all six application image profiles")
    if any(package_versions[family] != version for family in ("core", "studio", "extensions")):
        raise ReleaseError("A correction requires all package versions to equal the release version")


def resolve_run(args: argparse.Namespace) -> dict[str, Any]:
    packages = read_package_versions(Path(args.packages))
    event = args.event
    publish = False

    selection = "all" if event == "release" else args.images
    selected_images = parse_image_selection(selection)
    required_families = package_families_for(selected_images)

    if event == "pull_request":
        version = packages["ElsaVersion"]
    elif event == "release":
        version = args.release_tag
        publish = True
        mismatched = {family: package for family, package in packages.items() if package != version}
        if mismatched:
            values = ", ".join(f"{family}={package}" for family, package in mismatched.items())
            raise ReleaseError(f"Published release package versions must match tag {version}: {values}")
    elif event == "workflow_dispatch":
        version = args.version
        publish = normalize_bool(args.publish)
    else:
        raise ReleaseError(f"Unsupported workflow event: {event!r}")

    if publish and event == "workflow_dispatch":
        input_names = {"core": "core_version", "studio": "studio_version", "extensions": "extensions_version"}
        missing = [input_names[family] for family in sorted(required_families) if not getattr(args, input_names[family])]
        if missing:
            raise ReleaseError(f"Published dispatch requires explicit versions for selected package families: {', '.join(missing)}")
        if not re.fullmatch(r"[0-9a-f]{40}", getattr(args, "expected_commit", "")):
            raise ReleaseError("Published dispatch requires expected_commit as a full 40-character lowercase Git SHA")
        if args.expected_commit != args.commit:
            raise ReleaseError("Published dispatch expected_commit must equal the checked-out workflow commit")

    validate_publication(event, version, publish, args.ref, args.release_tag)
    if not re.fullmatch(r"[0-9a-f]{40}", args.commit):
        raise ReleaseError("Source commit must be a full 40-character lowercase Git SHA")

    if event == "workflow_dispatch":
        packages = {
            "ElsaVersion": getattr(args, "core_version") or packages["ElsaVersion"],
            "ElsaStudioVersion": getattr(args, "studio_version") or packages["ElsaStudioVersion"],
            "ElsaExtensionsVersion": getattr(args, "extensions_version") or packages["ElsaExtensionsVersion"],
        }
        for family, package_version in packages.items():
            if not is_elsa_version(package_version):
                raise ReleaseError(f"Unsupported {family} package version: {package_version!r}")

    package_versions = {
        "core": packages["ElsaVersion"],
        "studio": packages["ElsaStudioVersion"],
        "extensions": packages["ElsaExtensionsVersion"],
    }
    supersede_run_id = str(getattr(args, "supersede_run_id", "") or "").strip()
    validate_supersede_request(
        supersede_run_id,
        event,
        version,
        publish,
        args.ref,
        selected_images,
        package_versions,
    )

    return {
        "version": version,
        "publish": publish,
        "sourceRef": args.ref,
        "sourceCommit": args.commit,
        "packageVersions": package_versions,
        "supersedeRunId": supersede_run_id,
        "images": selected_images,
        "requiredFamilies": sorted(required_families),
        "matrix": {"include": selected_images},
    }


def github_output(values: dict[str, Any], path: str | None) -> None:
    lines = []
    for key, value in values.items():
        encoded = json.dumps(value, separators=(",", ":")) if isinstance(value, (dict, list)) else str(value).lower() if isinstance(value, bool) else str(value)
        lines.append(f"{key}={encoded}")
    content = "\n".join(lines) + "\n"
    if path:
        with Path(path).open("a", encoding="utf-8") as output:
            output.write(content)
    else:
        print(content, end="")


def labels_for(version: str, commit: str, source_ref: str, package_versions: dict[str, str]) -> dict[str, str]:
    return {
        "org.opencontainers.image.source": f"https://github.com/{APPS_REPOSITORY}",
        "org.opencontainers.image.revision": commit,
        "org.opencontainers.image.version": version,
        "org.opencontainers.image.ref.name": source_ref,
        "com.elsa.packages.core": package_versions["core"],
        "com.elsa.packages.studio": package_versions["studio"],
        "com.elsa.packages.extensions": package_versions["extensions"],
    }


def run_command(command: list[str], *, check: bool = True, env: dict[str, str] | None = None) -> subprocess.CompletedProcess[str]:
    result = subprocess.run(command, text=True, capture_output=True, env=env)
    if check and result.returncode:
        output = (result.stderr or result.stdout).strip()
        raise ReleaseError(f"Command failed ({result.returncode}): {' '.join(command)}\n{output}")
    return result


def package_family(package_id: str) -> str | None:
    if package_id == "Elsa.Logging" or any(package_id.startswith(prefix) for prefix in EXTENSION_PACKAGES):
        return "extensions"
    if package_id == "Elsa.Studio" or package_id.startswith("Elsa.Studio."):
        return "studio"
    if package_id == "Elsa" or package_id.startswith("Elsa."):
        return "core"
    return None


def validate_assets_json(assets: dict[str, Any], expected: dict[str, str], required_families: list[str]) -> dict[str, list[dict[str, str]]]:
    resolved: dict[str, list[dict[str, str]]] = {family: [] for family in required_families}
    for key, metadata in assets.get("libraries", {}).items():
        if metadata.get("type") != "package" or "/" not in key:
            continue
        package_id, version = key.rsplit("/", 1)
        family = package_family(package_id)
        if family in resolved:
            resolved[family].append({"id": package_id, "version": version})

    for family in required_families:
        packages = resolved[family]
        if not packages:
            raise ReleaseError(f"Embedded project.assets.json has no {family} Elsa packages")
        wrong = [item for item in packages if item["version"] != expected[family]]
        if wrong:
            details = ", ".join(f"{item['id']}={item['version']}" for item in wrong)
            raise ReleaseError(f"Embedded {family} package versions do not match {expected[family]}: {details}")
        packages.sort(key=lambda item: item["id"].casefold())
    return resolved


def read_embedded_assets(reference: str, platform: str, path: str) -> dict[str, Any]:
    output = run_command(
        ["docker", "run", "--rm", "--platform", platform, "--entrypoint", "cat", reference, path]
    ).stdout
    try:
        return json.loads(output)
    except json.JSONDecodeError as error:
        raise ReleaseError(f"Could not parse embedded package asset evidence from {reference}: {error}") from error


def inspect_manifest(reference: str) -> dict[str, Any] | None:
    result = run_command(["docker", "buildx", "imagetools", "inspect", reference], check=False)
    if result.returncode:
        output = f"{result.stdout}\n{result.stderr}"
        if re.search(r"manifest unknown|not found|no such manifest|name unknown", output, re.I):
            return None
        raise ReleaseError(f"Could not inspect registry image {reference}: {output.strip()}")

    root_match = re.search(r"^Digest:\s+(sha256:[0-9a-f]{64})\s*$", result.stdout, re.M)
    if not root_match:
        raise ReleaseError(f"Registry inspection did not return a root digest for {reference}")

    platforms: dict[str, str] = {}
    sections = re.split(r"(?m)^  Name:\s+", result.stdout)
    for section in sections[1:]:
        name = section.splitlines()[0].strip()
        platform_match = re.search(r"(?m)^  Platform:\s+(linux/[^\s]+)\s*$", section)
        if not platform_match:
            continue
        digest_match = re.search(r"@((?:sha256):[0-9a-f]{64})", name)
        if digest_match:
            platform = platform_match.group(1)
            if platform == "linux/arm64/v8":
                platform = "linux/arm64"
            if platform in platforms:
                raise ReleaseError(f"Registry inspection returned duplicate platform {platform} for {reference}")
            platforms[platform] = digest_match.group(1)

    return {"digest": root_match.group(1), "platforms": platforms}


def image_reference(repository: str, tag: str) -> str:
    return f"{repository}:{tag}"


def repository_from_reference(reference: str) -> str:
    if not isinstance(reference, str) or not reference:
        raise ReleaseError("Image reference must be a non-empty string")
    name = reference.split("@", 1)[0]
    last_slash = name.rfind("/")
    last_colon = name.rfind(":")
    if last_colon > last_slash:
        name = name[:last_colon]
    if not name or name.endswith("/"):
        raise ReleaseError(f"Could not resolve repository from image reference {reference!r}")
    return name


def verify_manifest(reference: str, *, expected_digest: str | None = None) -> dict[str, Any]:
    manifest = inspect_manifest(reference)
    if manifest is None:
        raise ReleaseError(f"Registry image does not exist: {reference}")
    missing = set(REQUIRED_PLATFORMS) - set(manifest["platforms"])
    if missing:
        raise ReleaseError(f"{reference} is missing required platforms: {sorted(missing)}")
    extra = set(manifest["platforms"]) - set(REQUIRED_PLATFORMS)
    if extra:
        raise ReleaseError(f"{reference} contains unsupported platforms: {sorted(extra)}")
    if expected_digest and manifest["digest"] != expected_digest:
        raise ReleaseError(f"{reference} digest {manifest['digest']} does not match {expected_digest}")
    manifest["platforms"] = {
        platform: manifest["platforms"][platform] for platform in REQUIRED_PLATFORMS
    }
    return manifest


def verify_labels(reference: str, expected: dict[str, str], platforms: Iterable[str]) -> None:
    requested_platforms = tuple(platforms)
    if len(requested_platforms) != len(REQUIRED_PLATFORMS) or set(requested_platforms) != set(REQUIRED_PLATFORMS):
        raise ReleaseError(f"Label verification requires exactly these platforms: {list(REQUIRED_PLATFORMS)}")

    manifest = verify_manifest(reference)
    repository = repository_from_reference(reference)
    for platform in REQUIRED_PLATFORMS:
        platform_reference = f"{repository}@{manifest['platforms'][platform]}"
        run_command(["docker", "pull", "--platform", platform, platform_reference])
        result = run_command(
            ["docker", "image", "inspect", "--format", "{{json .Config.Labels}}", platform_reference]
        )
        try:
            actual = json.loads(result.stdout)
        except json.JSONDecodeError as error:
            raise ReleaseError(f"Could not read image labels from {platform_reference}: {error}") from error
        if not isinstance(actual, dict):
            raise ReleaseError(f"Could not read image labels from {platform_reference}: expected a JSON object")
        for key, value in expected.items():
            if key == "org.opencontainers.image.ref.name":
                allowed_refs = {"refs/heads/main", f"refs/tags/{expected['org.opencontainers.image.version']}"}
                if actual.get(key) not in allowed_refs:
                    raise ReleaseError(
                        f"{platform_reference} has an unapproved publication ref label: {actual.get(key)!r}"
                    )
                continue
            if actual.get(key) != value:
                raise ReleaseError(
                    f"{platform_reference} label {key!r} is {actual.get(key)!r}; expected {value!r}"
                )


def get_image_plan_with_packages(
    repository: str,
    version: str,
    commit: str,
    source_ref: str,
    package_versions: dict[str, str],
    *,
    skip_version_tag: bool = False,
) -> dict[str, Any]:
    expected_labels = labels_for(version, commit, source_ref, package_versions)
    release_reference = image_reference(repository, version)
    release_manifest = None if skip_version_tag else inspect_manifest(release_reference)
    if release_manifest:
        verified = verify_manifest(release_reference)
        verify_labels(release_reference, expected_labels, REQUIRED_PLATFORMS)
        return {"source_ref": release_reference, "reuse": True, "manifest": verified}

    candidate_reference = image_reference(repository, f"{version}-sha-{commit}")
    candidate_manifest = inspect_manifest(candidate_reference)
    if candidate_manifest:
        verified = verify_manifest(candidate_reference)
        verify_labels(candidate_reference, expected_labels, REQUIRED_PLATFORMS)
        return {"source_ref": candidate_reference, "reuse": True, "manifest": verified}

    return {"source_ref": candidate_reference, "reuse": False, "manifest": None}


def wait_for_http(container_id: str, container_port: int, timeout_seconds: int = 120) -> tuple[str, int]:
    port_output = run_command(["docker", "port", container_id, f"{container_port}/tcp"]).stdout.strip()
    host_port = port_output.splitlines()[0].rsplit(":", 1)[-1]
    url = f"http://127.0.0.1:{host_port}/"
    expires = time.monotonic() + timeout_seconds
    last_status = 0
    while time.monotonic() < expires:
        state = run_command(
            ["docker", "inspect", "--format", "{{.State.Running}}", container_id]
        ).stdout.strip()
        if state.lower() != "true":
            logs = run_command(["docker", "logs", container_id], check=False)
            raise ReleaseError(f"Container exited before HTTP smoke test:\n{logs.stdout}\n{logs.stderr}")
        try:
            with urllib.request.urlopen(url, timeout=3) as response:
                last_status = response.status
        except urllib.error.HTTPError as error:
            last_status = error.code
        except (urllib.error.URLError, TimeoutError, ConnectionError, OSError):
            time.sleep(2)
            continue
        if last_status == 200:
            return url, last_status
        time.sleep(2)
    raise ReleaseError(f"HTTP smoke test did not pass for {url}; last status was {last_status}")


def login_and_probe_api(url: str, username: str, password: str) -> dict[str, Any]:
    api_root = f"{url.rstrip('/')}/elsa/api"
    credentials = json.dumps(
        {"username": username, "password": password}
    ).encode("utf-8")
    login_request = urllib.request.Request(
        f"{api_root}/identity/login",
        data=credentials,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(login_request, timeout=15) as response:
            login_status = response.status
            login = json.loads(response.read())
    except (urllib.error.URLError, urllib.error.HTTPError, TimeoutError, json.JSONDecodeError) as error:
        raise ReleaseError(
            f"Synthetic admin login request to /elsa/api/identity/login failed: {error}"
        ) from error
    access_token = login.get("accessToken") or login.get("AccessToken")
    if login_status != 200 or not access_token:
        raise ReleaseError("Synthetic admin login did not return an access token")

    workflow_api = probe_bearer_json_api(url, BEARER_API_ENDPOINT, access_token)
    result = workflow_api["body"]
    items = result.get("items", result.get("Items")) if isinstance(result, dict) else None
    total_count = result.get("totalCount", result.get("TotalCount")) if isinstance(result, dict) else None
    if not isinstance(items, list) or not isinstance(total_count, int):
        raise ReleaseError(
            f"Bearer-authenticated workflow definitions API request to {BEARER_API_ENDPOINT} did not return a paged JSON response"
        )

    dashboard_api = probe_bearer_json_api(url, DASHBOARD_API_ENDPOINT, access_token)
    dashboard_evidence = validate_dashboard_overview(dashboard_api["body"])

    return {
        "identityLogin": {"status": login_status, "endpoint": "/elsa/api/identity/login"},
        "bearerApi": {
            "status": workflow_api["status"],
            "endpoint": BEARER_API_ENDPOINT,
            "contentType": workflow_api["contentType"],
        },
        "dashboardApi": {
            "status": dashboard_api["status"],
            "endpoint": DASHBOARD_API_ENDPOINT,
            "contentType": dashboard_api["contentType"],
            **dashboard_evidence,
        },
    }


def validate_dashboard_overview(body: dict[str, Any]) -> dict[str, Any]:
    runtime = body.get("runtime")
    workflow_instances = body.get("workflowInstances")
    if not isinstance(runtime, dict) or not isinstance(workflow_instances, dict):
        raise ReleaseError(
            f"Bearer-authenticated dashboard API request to {DASHBOARD_API_ENDPOINT} returned an unexpected JSON object"
        )

    runtime_status = runtime.get("status")
    is_accepting_work = runtime.get("isAcceptingWork")
    metric_names = ("running", "completed", "faulted", "suspended", "interrupted", "incidentBearing")
    metrics = {name: workflow_instances.get(name) for name in metric_names}
    if (
        runtime_status != "AcceptingWork"
        or is_accepting_work is not True
        or any(type(value) is not int or value < 0 for value in metrics.values())
    ):
        raise ReleaseError(
            f"Bearer-authenticated dashboard API request to {DASHBOARD_API_ENDPOINT} did not report an accepting workflow runtime and metrics"
        )

    return {
        "runtimeStatus": runtime_status,
        "isAcceptingWork": is_accepting_work,
        "workflowMetricsValid": True,
        "running": metrics["running"],
    }


def probe_bearer_json_api(url: str, endpoint: str, access_token: str) -> dict[str, Any]:
    request = urllib.request.Request(
        f"{url.rstrip('/')}{endpoint}",
        headers={"Authorization": f"Bearer {access_token}"},
    )
    try:
        with urllib.request.urlopen(request, timeout=15) as response:
            status = response.status
            content_type = response.headers.get("Content-Type", "").lower()
            body = json.loads(response.read())
    except (urllib.error.URLError, urllib.error.HTTPError, TimeoutError) as error:
        raise ReleaseError(f"Bearer-authenticated API request to {endpoint} failed: {error}") from error
    except json.JSONDecodeError as error:
        raise ReleaseError(f"Bearer-authenticated API request to {endpoint} returned invalid JSON") from error
    if status != 200 or not content_type.startswith("application/json") or not isinstance(body, dict):
        raise ReleaseError(f"Bearer-authenticated API request to {endpoint} did not return a JSON object")
    return {"status": status, "contentType": content_type, "body": body}


def validate_smoke_asset_evidence(path: str, status: int, byte_count: int, content_type: str) -> None:
    if status != 200 or byte_count < 100:
        raise ReleaseError(f"Browser asset {path} returned HTTP {status} or only {byte_count} bytes")
    is_css = path.lower().endswith(".css")
    content_type = content_type.lower()
    valid_content_type = (
        content_type.startswith("text/css")
        if is_css
        else "javascript" in content_type or "ecmascript" in content_type
    )
    if not valid_content_type:
        raise ReleaseError(f"Browser asset {path} has unexpected content type {content_type!r}")


def verify_smoke_assets(url: str, asset_paths: Iterable[str]) -> list[dict[str, Any]]:
    verified = []
    for path in asset_paths:
        try:
            with urllib.request.urlopen(f"{url.rstrip('/')}{path}", timeout=15) as response:
                body = response.read()
                status = response.status
                content_type = response.headers.get("Content-Type", "").lower()
        except (urllib.error.URLError, urllib.error.HTTPError, TimeoutError) as error:
            raise ReleaseError(f"Browser asset request for {path} failed: {error}") from error
        validate_smoke_asset_evidence(path, status, len(body), content_type)
        if body.lstrip().lower().startswith((b"<!doctype html", b"<html")):
            raise ReleaseError(f"Browser asset {path} returned the HTML fallback page")
        verified.append({"path": path, "status": status, "bytes": len(body), "contentType": content_type})
    return verified


def smoke_image(
    reference: str,
    platform: str,
    port: int,
    local: bool = False,
    auth_enabled: bool = False,
    smoke_assets: Iterable[str] = (),
    image_digest: str | None = None,
) -> dict[str, Any]:
    platform_args = [] if local else ["--platform", platform]
    username = "container-smoke"
    password = secrets.token_urlsafe(32) if auth_enabled else ""
    run_env = {
        **os.environ,
        "Identity__AdminUser__UserName": username,
        "Identity__AdminUser__Password": password,
    } if auth_enabled else None
    auth_args = (
        [
            "--env",
            "Identity__AdminUser__UserName",
            "--env",
            "Identity__AdminUser__Password",
        ]
        if auth_enabled
        else []
    )
    container_id = ""
    try:
        run = run_command(
            [
                "docker",
                "run",
                "--detach",
                *platform_args,
                "--publish",
                f"127.0.0.1::{port}",
                "--env",
                "ASPNETCORE_ENVIRONMENT=Production",
                *auth_args,
                reference,
            ],
            env=run_env,
        )
        container_id = run.stdout.strip()
        url, status = wait_for_http(container_id, port)
        image_digest = image_digest or (reference.rsplit("@", 1)[-1] if "@" in reference else reference)
        result = {
            "platform": platform,
            "imageDigest": image_digest,
            "status": "success",
            "endpoint": url,
            "httpStatus": status,
        }
        result["browserAssets"] = verify_smoke_assets(url, smoke_assets)
        if auth_enabled:
            result.update(login_and_probe_api(url, username, password))
        return result
    except ReleaseError as error:
        raise ReleaseError(f"Smoke test failed for {reference} on {platform}: {error}") from error
    finally:
        if container_id:
            run_command(["docker", "rm", "--force", container_id], check=False)


def verify_registry_image(
    repository: str,
    reference: str,
    version: str,
    commit: str,
    source_ref: str,
    package_versions: dict[str, str],
    port: int,
    image: dict[str, Any],
) -> dict[str, Any]:
    manifest = verify_manifest(reference)
    verify_labels(reference, labels_for(version, commit, source_ref, package_versions), REQUIRED_PLATFORMS)
    smoke = []
    package_evidence: dict[str, list[dict[str, str]]] | None = None
    for platform in REQUIRED_PLATFORMS:
        platform_reference = f"{repository}@{manifest['platforms'][platform]}"
        assets = read_embedded_assets(platform_reference, platform, image["assetsPath"])
        evidence = validate_assets_json(assets, package_versions, image["packages"])
        if package_evidence is not None and evidence != package_evidence:
            raise ReleaseError(f"Resolved Elsa package evidence differs between platforms for {repository}")
        package_evidence = evidence
        smoke.append(
            smoke_image(
                platform_reference,
                platform,
                port,
                auth_enabled=image.get("smokeAuth", False),
                smoke_assets=image.get("smokeAssets", []),
            )
        )
    return {
        "digest": manifest["digest"],
        "registryVerified": True,
        "platforms": [
            {"platform": platform, "digest": manifest["platforms"][platform]}
            for platform in REQUIRED_PLATFORMS
        ],
        "smoke": {"success": True, "imageDigest": manifest["digest"], "platforms": smoke},
        "resolvedPackages": package_evidence,
    }


def write_json(path: Path, value: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def select_latest_image_artifacts(
    paths: Iterable[Path],
    selected_images: list[dict[str, Any]],
    run_id: str,
    current_attempt: int,
) -> list[dict[str, Any]]:
    expected = {image["id"] for image in selected_images}
    all_ids = {image["id"] for image in IMAGES}
    artifacts: dict[tuple[str, int], list[Path]] = {}
    artifact_names: dict[tuple[str, int], str] = {}
    pattern = re.compile(r"^container-image-([a-z0-9-]+)-([0-9]+)-([0-9]+)$")

    for path in paths:
        artifact_name = path.parent.name
        match = pattern.fullmatch(artifact_name)
        if not match:
            raise ReleaseError(f"Malformed image evidence artifact name: {artifact_name!r}")
        image_id, artifact_run_id, attempt_text = match.groups()
        if image_id not in all_ids:
            raise ReleaseError(f"Image evidence artifact contains an unknown profile: {image_id!r}")
        if image_id not in expected:
            raise ReleaseError(f"Unexpected image evidence artifact for unselected profile {image_id!r}")
        if path.name != f"{image_id}.json":
            raise ReleaseError(f"Image evidence artifact {artifact_name} has unexpected file {path.name!r}")
        if artifact_run_id != str(run_id):
            raise ReleaseError(f"Image evidence artifact belongs to run {artifact_run_id}, expected {run_id}")
        attempt = int(attempt_text)
        if attempt < 1 or attempt > current_attempt:
            raise ReleaseError(f"Image evidence artifact has invalid/future attempt {attempt}")
        key = (image_id, attempt)
        artifacts.setdefault(key, []).append(path)
        artifact_names[key] = artifact_name

    selected: list[dict[str, Any]] = []
    for image in selected_images:
        image_id = image["id"]
        attempts = [attempt for candidate_id, attempt in artifacts if candidate_id == image_id]
        if not attempts:
            raise ReleaseError(f"Missing image evidence artifact for selected profile {image_id!r}")
        attempt = max(attempts)
        key = (image_id, attempt)
        candidates = artifacts[key]
        if len(candidates) != 1:
            raise ReleaseError(f"Duplicate image evidence artifacts for {image_id!r} attempt {attempt}")
        selected.append(
            {
                "id": image_id,
                "path": candidates[0],
                "artifactName": artifact_names[key],
                "runId": str(run_id),
                "runAttempt": attempt,
            }
        )
    return selected


def read_fragments(
    selected_artifacts: list[dict[str, Any]], selected_images: list[dict[str, Any]]
) -> list[dict[str, Any]]:
    expected_ids = [image["id"] for image in selected_images]
    artifact_ids = [item["id"] for item in selected_artifacts]
    if artifact_ids != expected_ids:
        raise ReleaseError(f"Expected selected artifacts in profile order; found {artifact_ids}")
    fragments = []
    for artifact in selected_artifacts:
        fragment = json.loads(Path(artifact["path"]).read_text(encoding="utf-8"))
        if fragment.get("id") != artifact["id"]:
            raise ReleaseError(
                f"Image evidence artifact {artifact['artifactName']} contains profile {fragment.get('id')!r}"
            )
        fragment["evidenceRun"] = {
            "artifactName": artifact["artifactName"],
            "runId": artifact["runId"],
            "runAttempt": artifact["runAttempt"],
        }
        fragments.append(fragment)
    return fragments


def validate_fragment_evidence(
    fragments: list[dict[str, Any]],
    selected_images: list[dict[str, Any]],
    version: str,
    commit: str,
    package_versions: dict[str, str],
    publication: str,
) -> None:
    by_id = {fragment["id"]: fragment for fragment in fragments}
    expected_platforms = REQUIRED_PLATFORMS if publication == "published" else ("linux/amd64",)
    for image in selected_images:
        fragment = by_id[image["id"]]
        if fragment.get("name") != image["name"] or fragment.get("repository") != image["repository"]:
            raise ReleaseError(f"Image evidence identity does not match profile {image['name']}")
        digest = fragment.get("digest", "")
        if not re.fullmatch(r"sha256:[0-9a-f]{64}", digest):
            raise ReleaseError(f"Image evidence for {image['name']} has an invalid root digest")
        source_ref = fragment.get("sourceRef", "")
        allowed_source_refs = {
            image_reference(image["repository"], version),
            image_reference(image["repository"], f"{version}-sha-{commit}"),
        }
        if publication != "published":
            allowed_source_refs.add(f"elsa-local-{image['id']}:{version}-{commit}")
        if source_ref not in allowed_source_refs:
            raise ReleaseError(f"Image evidence source reference is not bound to {image['name']} release inputs")

        platform_rows = fragment.get("platforms", [])
        platform_map = {row.get("platform"): row.get("digest") for row in platform_rows}
        if len(platform_rows) != len(platform_map) or set(platform_map) != set(expected_platforms):
            raise ReleaseError(f"Image evidence for {image['name']} has incomplete or unexpected platform manifests")
        if any(not re.fullmatch(r"sha256:[0-9a-f]{64}", value or "") for value in platform_map.values()):
            raise ReleaseError(f"Image evidence for {image['name']} has an invalid platform digest")

        smoke = fragment.get("smoke", {})
        smoke_rows = smoke.get("platforms", [])
        smoke_map = {row.get("platform"): row for row in smoke_rows}
        if (
            smoke.get("success") is not True
            or smoke.get("imageDigest") != digest
            or set(smoke_map) != set(expected_platforms)
            or len(smoke_rows) != len(smoke_map)
        ):
            raise ReleaseError(f"Image evidence for {image['name']} is not bound to successful platform smoke results")
        for platform in expected_platforms:
            row = smoke_map[platform]
            if (
                row.get("status") != "success"
                or row.get("httpStatus") != 200
                or row.get("imageDigest") != platform_map[platform]
            ):
                raise ReleaseError(f"Image evidence for {image['name']} failed HTTP smoke on {platform}")
            browser_assets = row.get("browserAssets", [])
            if [asset.get("path") for asset in browser_assets] != image.get("smokeAssets", []):
                raise ReleaseError(f"Image evidence for {image['name']} is missing browser framework assets on {platform}")
            try:
                for asset in browser_assets:
                    validate_smoke_asset_evidence(
                        asset.get("path", ""),
                        asset.get("status", 0),
                        asset.get("bytes", 0),
                        asset.get("contentType", ""),
                    )
            except ReleaseError as error:
                raise ReleaseError(
                    f"Image evidence for {image['name']} contains a failed browser asset on {platform}: {error}"
                ) from error
            if image.get("smokeAuth"):
                if (
                    row.get("identityLogin", {}).get("status") != 200
                    or row.get("bearerApi", {}).get("status") != 200
                    or row.get("identityLogin", {}).get("endpoint") != "/elsa/api/identity/login"
                    or row.get("bearerApi", {}).get("endpoint") != BEARER_API_ENDPOINT
                    or row.get("bearerApi", {}).get("contentType", "").split(";", 1)[0] != "application/json"
                    or row.get("dashboardApi", {}).get("status") != 200
                    or row.get("dashboardApi", {}).get("endpoint") != DASHBOARD_API_ENDPOINT
                    or row.get("dashboardApi", {}).get("contentType", "").split(";", 1)[0] != "application/json"
                    or row.get("dashboardApi", {}).get("runtimeStatus") != "AcceptingWork"
                    or row.get("dashboardApi", {}).get("isAcceptingWork") is not True
                    or row.get("dashboardApi", {}).get("workflowMetricsValid") is not True
                    or type(row.get("dashboardApi", {}).get("running")) is not int
                    or row.get("dashboardApi", {}).get("running", -1) < 0
                ):
                    raise ReleaseError(f"Image evidence for {image['name']} lacks successful authenticated API smoke")

        packages = fragment.get("resolvedPackages", {})
        if set(packages) != set(image["packages"]):
            raise ReleaseError(f"Image evidence for {image['name']} has the wrong resolved package families")
        for family in image["packages"]:
            rows = packages[family]
            if not rows or any(row.get("version") != package_versions[family] for row in rows):
                raise ReleaseError(f"Image evidence for {image['name']} has incomplete {family} package provenance")
            if any(package_family(row.get("id", "")) != family for row in rows):
                raise ReleaseError(f"Image evidence for {image['name']} has misclassified {family} package provenance")


def same_platforms(left: dict[str, Any], right: dict[str, Any]) -> bool:
    left_platforms = {item["platform"]: item["digest"] for item in left["platforms"]}
    right_platforms = {item["platform"]: item["digest"] for item in right["platforms"]}
    return left_platforms == right_platforms


def run_docker_promotion(
    fragments: list[dict[str, Any]],
    selected_images: list[dict[str, Any]],
    version: str,
    commit: str,
    source_ref: str,
    package_versions: dict[str, str],
    *,
    prior_by_reference: dict[str, dict[str, Any]] | None = None,
    prior_commit: str = "",
    prior_source_ref: str = "",
    prior_package_versions: dict[str, str] | None = None,
) -> list[dict[str, Any]]:
    by_id = {fragment["id"]: fragment for fragment in fragments}
    correction = prior_by_reference is not None
    if correction:
        planned_targets = {
            image_reference(image["repository"], version)
            for image in selected_images
        }
        planned_targets.update(
            image_reference(alias["repository"], version)
            for image in selected_images
            for alias in image.get("aliases", [])
        )
        if set(prior_by_reference) != planned_targets or not prior_commit or not prior_source_ref or not prior_package_versions:
            raise ReleaseError("Correction receipt and selected version tags do not match")

    def inspect_target(reference: str, digest: str, platforms: dict[str, str]) -> str:
        existing = inspect_manifest(reference)
        if existing is None:
            if correction:
                raise ReleaseError(f"Correction target is missing: {reference}")
            return "missing"
        manifest = verify_manifest(reference)
        if correction:
            prior = prior_by_reference[reference]
            if manifest["digest"] == digest and manifest["platforms"] == platforms:
                verify_labels(reference, labels_for(version, commit, source_ref, package_versions), REQUIRED_PLATFORMS)
                return "candidate"
            if manifest["digest"] == prior["digest"] and manifest["platforms"] == prior["platforms"]:
                verify_labels(
                    reference,
                    labels_for(version, prior_commit, prior_source_ref, prior_package_versions),
                    REQUIRED_PLATFORMS,
                )
                return "prior"
            raise ReleaseError(f"Correction target {reference} differs from both verified receipts")
        verify_labels(reference, labels_for(version, commit, source_ref, package_versions), REQUIRED_PLATFORMS)
        if manifest["digest"] != digest:
            raise ReleaseError(f"Refusing to overwrite conflicting version tag {reference}")
        if manifest["platforms"] != platforms:
            raise ReleaseError(f"Refusing to reuse version tag with conflicting platforms {reference}")
        return "candidate"

    planned: list[tuple[str, str, str, dict[str, Any]]] = []

    # Check every destination before creating any version tags.
    for image in selected_images:
        source = by_id[image["id"]]
        version_ref = image_reference(image["repository"], version)
        immutable_source_ref = f"{image['repository']}@{source['digest']}"
        source_manifest = verify_manifest(immutable_source_ref, expected_digest=source["digest"])
        fragment_platforms = {item["platform"]: item["digest"] for item in source["platforms"]}
        if source_manifest["platforms"] != fragment_platforms:
            raise ReleaseError(f"Source manifest platforms do not match verified smoke evidence for {image['name']}")
        if correction:
            verify_labels(immutable_source_ref, labels_for(version, commit, source_ref, package_versions), REQUIRED_PLATFORMS)
        inspect_target(version_ref, source_manifest["digest"], source_manifest["platforms"])
        planned.append((version_ref, immutable_source_ref, image["id"], source_manifest))

    for image in selected_images:
        source = by_id[image["id"]]
        source_platforms = {item["platform"]: item["digest"] for item in source["platforms"]}
        for alias in image.get("aliases", []):
            alias_ref = image_reference(alias["repository"], version)
            inspect_target(alias_ref, source["digest"], source_platforms)

    for version_ref, source_ref_for_image, image_id, source_manifest in planned:
        state = inspect_target(version_ref, source_manifest["digest"], source_manifest["platforms"])
        if state in ("missing", "prior"):
            run_command(["docker", "buildx", "imagetools", "create", "--tag", version_ref, source_ref_for_image])
        promoted = verify_manifest(version_ref, expected_digest=source_manifest["digest"])
        if promoted["platforms"] != source_manifest["platforms"]:
            raise ReleaseError(f"Platform manifests changed while promoting {version_ref}")
        verify_labels(
            version_ref,
            labels_for(version, commit, source_ref, package_versions),
            REQUIRED_PLATFORMS,
        )
        by_id[image_id]["promoted"] = {"reference": version_ref, **promoted}

    output: list[dict[str, Any]] = []
    for image in selected_images:
        source = by_id[image["id"]]
        promoted = source["promoted"]
        if promoted["platforms"] != {
            item["platform"]: item["digest"] for item in source["platforms"]
        }:
            raise ReleaseError(f"Promoted image platforms do not match smoke evidence for {promoted['reference']}")
        verify_labels(
            promoted["reference"],
            labels_for(version, commit, source_ref, package_versions),
            REQUIRED_PLATFORMS,
        )
        entry = {
            "name": image["name"],
            "repository": image["repository"],
            "tag": version,
            "sourceRef": source["sourceRef"],
            "digest": promoted["digest"],
            "platforms": source["platforms"],
            "registryVerified": True,
            "smoke": source["smoke"],
            "packages": image["packages"],
            "packageVersions": {family: package_versions[family] for family in image["packages"]},
            "resolvedPackages": source["resolvedPackages"],
        }
        output.append(entry)

    canonical_by_id = {item["id"]: next(row for row in output if row["name"] == item["name"]) for item in selected_images}
    for image in selected_images:
        canonical = canonical_by_id[image["id"]]
        for alias in image.get("aliases", []):
            alias_ref = image_reference(alias["repository"], version)
            source_platforms = {item["platform"]: item["digest"] for item in canonical["platforms"]}
            state = inspect_target(alias_ref, canonical["digest"], source_platforms)
            if state in ("missing", "prior"):
                run_command(
                    [
                        "docker",
                        "buildx",
                        "imagetools",
                        "create",
                        "--tag",
                        alias_ref,
                        f"{canonical['repository']}@{canonical['digest']}",
                    ]
                )
            alias_manifest = verify_manifest(alias_ref, expected_digest=canonical["digest"])
            if alias_manifest["platforms"] != {
                item["platform"]: item["digest"] for item in canonical["platforms"]
            }:
                raise ReleaseError(f"Alias platform manifests do not match {canonical['repository']}")
            verify_labels(
                alias_ref,
                labels_for(version, commit, source_ref, package_versions),
                REQUIRED_PLATFORMS,
            )
            output.append(
                {
                    **canonical,
                    "name": alias["name"],
                    "repository": alias["repository"],
                    "alias_of": image["name"],
                }
            )

    return output


def validate_receipt_platforms(value: Any, context: str) -> dict[str, str]:
    if not isinstance(value, list):
        raise ReleaseError(f"Prior receipt has invalid platform evidence for {context}")
    platforms: dict[str, str] = {}
    for item in value:
        if not isinstance(item, dict):
            raise ReleaseError(f"Prior receipt has invalid platform evidence for {context}")
        platform = item.get("platform")
        digest = item.get("digest")
        if platform not in REQUIRED_PLATFORMS or platform in platforms:
            raise ReleaseError(f"Prior receipt has duplicate or unsupported platforms for {context}")
        if not isinstance(digest, str) or not re.fullmatch(r"sha256:[0-9a-f]{64}", digest):
            raise ReleaseError(f"Prior receipt has an invalid platform digest for {context}")
        platforms[platform] = digest
    if set(platforms) != set(REQUIRED_PLATFORMS):
        raise ReleaseError(f"Prior receipt is missing a required platform for {context}")
    return {platform: platforms[platform] for platform in REQUIRED_PLATFORMS}


def prior_publication_source_ref(run: dict[str, Any], version: str) -> str:
    event = run.get("event")
    head_branch = run.get("head_branch")
    if event == "release":
        if head_branch != version:
            raise ReleaseError("Prior release event must target the exact version tag")
        source_ref = f"refs/tags/{version}"
    elif event == "workflow_dispatch":
        if head_branch == "main":
            source_ref = "refs/heads/main"
        elif head_branch == version:
            source_ref = f"refs/tags/{version}"
        else:
            raise ReleaseError("Prior dispatch must come from main or the matching version tag")
    else:
        raise ReleaseError("Prior publication must be a release or workflow_dispatch event")

    # head_ref/ref are not included by every Actions API response, but reject them if present
    # and inconsistent with the accepted branch/tag provenance.
    if run.get("head_ref") not in (None, "", head_branch, source_ref):
        raise ReleaseError("Prior run ref metadata does not match its accepted publication source")
    if run.get("ref") not in (None, source_ref):
        raise ReleaseError("Prior run ref metadata does not match its accepted publication source")
    return source_ref


def prior_workflow_inputs_match(
    inputs: Any, *, event: str, version: str, old_commit: str
) -> bool:
    if not isinstance(inputs, dict):
        return False
    if event == "release":
        return (
            inputs.get("version") == ""
            and inputs.get("publish") is False
            and inputs.get("images") == ""
            and all(inputs.get(f"{family}_version") == "" for family in ("core", "studio", "extensions"))
            and inputs.get("expected_commit") == old_commit
        )
    image_selection = inputs.get("images")
    if not isinstance(image_selection, str):
        return False
    try:
        selects_all_images = parse_image_selection(image_selection) == list(IMAGES)
    except ReleaseError:
        return False
    return (
        inputs.get("version") == version
        and inputs.get("publish") is True
        and selects_all_images
        and inputs.get("expected_commit") == old_commit
        and all(inputs.get(f"{family}_version") == version for family in ("core", "studio", "extensions"))
    )


def validate_superseded_receipt(
    receipt: Any,
    *,
    version: str,
    package_versions: dict[str, str],
    repository: str,
    run: dict[str, Any],
) -> tuple[dict[str, dict[str, Any]], dict[str, Any]]:
    if not isinstance(receipt, dict):
        raise ReleaseError("Prior receipt is not a JSON object")
    old_commit = str(run.get("head_sha", ""))
    run_id = str(run.get("id", ""))
    run_attempt = run.get("run_attempt")
    expected_workflow = ".github/workflows/container-images.yml"
    source = receipt.get("appsSource")
    prior_run = receipt.get("workflowRun")
    inputs = receipt.get("workflowInputs")
    expected_source_ref = prior_publication_source_ref(run, version)
    prior_event = run.get("event")
    if (
        not re.fullmatch(r"[0-9a-f]{40}", old_commit)
        or receipt.get("schemaVersion") != 1
        or receipt.get("releaseVersion") != version
        or receipt.get("publication") != "published"
        or receipt.get("appsRepository") != repository
        or receipt.get("appsSourceCommit") != old_commit
        or not isinstance(source, dict)
        or source.get("commit") != old_commit
        or source.get("ref") != expected_source_ref
        or receipt.get("packageVersions") != package_versions
        or not isinstance(prior_run, dict)
        or prior_run.get("repository") != repository
        or str(prior_run.get("id", "")) != run_id
        or prior_run.get("runAttempt") != run_attempt
        or prior_run.get("workflow") != expected_workflow
        or prior_run.get("url") != run.get("html_url")
        or prior_run.get("event") != prior_event
        or prior_run.get("ref") != expected_source_ref
        or prior_run.get("headSha") != old_commit
        or prior_run.get("conclusion") != "success"
        or not prior_workflow_inputs_match(
            inputs, event=str(prior_event), version=version, old_commit=old_commit
        )
    ):
        raise ReleaseError("Prior receipt provenance does not match the successful full-version publication")
    expected_targets: dict[str, tuple[dict[str, Any], str | None]] = {}
    for image in IMAGES:
        expected_targets[image_reference(image["repository"], version)] = (image, None)
        for alias in image.get("aliases", []):
            expected_targets[image_reference(alias["repository"], version)] = (image, alias["name"])

    receipt_images = receipt.get("images")
    if not isinstance(receipt_images, list) or len(receipt_images) != len(expected_targets):
        raise ReleaseError("Prior receipt must contain exactly the six profiles and two aliases")
    by_reference: dict[str, dict[str, Any]] = {}
    for entry in receipt_images:
        if not isinstance(entry, dict):
            raise ReleaseError("Prior receipt contains an invalid image entry")
        reference = image_reference(str(entry.get("repository", "")), str(entry.get("tag", "")))
        if reference not in expected_targets or reference in by_reference:
            raise ReleaseError("Prior receipt contains an unexpected or duplicate image reference")
        image, alias_name = expected_targets[reference]
        expected_name = alias_name or image["name"]
        if (
            entry.get("name") != expected_name
            or entry.get("tag") != version
            or entry.get("packageVersions") != {family: version for family in image["packages"]}
            or (alias_name is not None and entry.get("alias_of") != image["name"])
            or (alias_name is None and "alias_of" in entry)
        ):
            raise ReleaseError(f"Prior receipt image identity or package versions are invalid for {reference}")
        digest = entry.get("digest")
        if not isinstance(digest, str) or not re.fullmatch(r"sha256:[0-9a-f]{64}", digest):
            raise ReleaseError(f"Prior receipt has an invalid image digest for {reference}")
        image_platforms = validate_receipt_platforms(entry.get("platforms"), reference)
        by_reference[reference] = {
            "digest": digest,
            "platforms": image_platforms,
        }

    for image in IMAGES:
        canonical = by_reference[image_reference(image["repository"], version)]
        for alias in image.get("aliases", []):
            alias_entry = by_reference[image_reference(alias["repository"], version)]
            if alias_entry["digest"] != canonical["digest"] or alias_entry["platforms"] != canonical["platforms"]:
                raise ReleaseError(f"Prior receipt alias does not match {image['name']}")

    references = [
        {"reference": reference, "digest": entry["digest"],
         "platforms": [{"platform": platform, "digest": entry["platforms"][platform]}
                       for platform in REQUIRED_PLATFORMS]}
        for reference, entry in by_reference.items()
    ]
    supersedes = {
        "workflowRun": {
            "repository": repository,
            "id": run_id,
            "runAttempt": run_attempt,
            "workflow": expected_workflow,
            "url": run.get("html_url"),
            "event": prior_event,
            "ref": expected_source_ref,
            "headSha": old_commit,
        },
        "appsSourceCommit": old_commit,
        "references": references,
    }
    return by_reference, supersedes


def github_api_json(endpoint: str) -> dict[str, Any]:
    result = subprocess.run(
        ["gh", "api", endpoint], stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, env=os.environ.copy()
    )
    if result.returncode:
        raise ReleaseError(f"GitHub API request failed while verifying prior publication (exit {result.returncode})")
    try:
        value = json.loads(result.stdout)
    except json.JSONDecodeError as error:
        raise ReleaseError("GitHub API returned invalid JSON while verifying prior publication") from error
    if not isinstance(value, dict):
        raise ReleaseError("GitHub API returned an unexpected response while verifying prior publication")
    return value


def github_api_artifact_zip(endpoint: str) -> bytes:
    result = subprocess.run(
        ["gh", "api", endpoint], stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=os.environ.copy()
    )
    if result.returncode:
        raise ReleaseError(f"Could not download prior receipt artifact (exit {result.returncode})")
    return result.stdout


def validate_superseded_archive(
    archive: bytes,
    artifact: dict[str, Any],
    *,
    expected_name: str,
    run: dict[str, Any],
    version: str,
    package_versions: dict[str, str],
    repository: str,
) -> tuple[dict[str, Any], dict[str, dict[str, Any]], dict[str, Any]]:
    if artifact.get("name") != expected_name or artifact.get("expired") is not False:
        raise ReleaseError("Prior run does not have the exact unexpired receipt artifact")
    artifact_run = artifact.get("workflow_run")
    if not isinstance(artifact_run, dict) or (
        str(artifact_run.get("id", "")) != str(run.get("id", ""))
        or artifact_run.get("head_sha") != run.get("head_sha")
        or artifact_run.get("head_branch") != run.get("head_branch")
        or artifact_run.get("event") not in (None, run.get("event"))
        or artifact_run.get("run_attempt") not in (None, run.get("run_attempt"))
    ):
        raise ReleaseError("Prior receipt artifact metadata does not match its workflow run")
    size = artifact.get("size_in_bytes")
    if type(size) is not int or size < 1 or size > 5 * 1024 * 1024 or len(archive) > 5 * 1024 * 1024:
        raise ReleaseError("Prior receipt artifact is unexpectedly large or has invalid size metadata")
    api_digest = artifact.get("digest")
    if not isinstance(api_digest, str) or not re.fullmatch(r"sha256:[0-9a-f]{64}", api_digest):
        raise ReleaseError("GitHub did not provide a valid prior receipt archive digest")
    actual_digest = "sha256:" + hashlib.sha256(archive).hexdigest()
    if actual_digest != api_digest:
        raise ReleaseError("Downloaded prior receipt archive does not match GitHub artifact metadata")

    try:
        with zipfile.ZipFile(io.BytesIO(archive)) as zipped:
            members = zipped.infolist()
            if len(members) != 1 or members[0].filename != "container-release-receipt.json":
                raise ReleaseError("Prior receipt archive must contain only container-release-receipt.json")
            if members[0].file_size > 2 * 1024 * 1024 or members[0].is_dir():
                raise ReleaseError("Prior receipt artifact has an invalid or oversized JSON member")
            receipt_text = zipped.read(members[0]).decode("utf-8")
        receipt = json.loads(receipt_text)
    except (zipfile.BadZipFile, UnicodeDecodeError, json.JSONDecodeError, OSError) as error:
        raise ReleaseError("Prior receipt artifact is not a valid single-file JSON archive") from error

    by_reference, supersedes = validate_superseded_receipt(
        receipt,
        version=version,
        package_versions=package_versions,
        repository=repository,
        run=run,
    )
    artifact_id = artifact.get("id")
    if type(artifact_id) is not int or artifact_id < 1:
        raise ReleaseError("Prior receipt artifact has an invalid GitHub artifact id")
    supersedes["receiptArtifact"] = {
        "id": artifact_id,
        "name": expected_name,
        "archiveDigest": actual_digest,
    }
    return receipt, by_reference, supersedes


def load_superseded_publication(
    run_id: str,
    *,
    version: str,
    package_versions: dict[str, str],
    repository: str,
    current_run_id: str,
    current_commit: str,
) -> tuple[dict[str, Any], dict[str, dict[str, Any]], dict[str, Any]]:
    if not re.fullmatch(r"[1-9][0-9]*", run_id) or run_id == current_run_id:
        raise ReleaseError("supersede_run_id must identify a different completed publication run")
    if repository != APPS_REPOSITORY:
        raise ReleaseError("Corrections are supported only in the canonical Apps repository")
    run = github_api_json(f"repos/{repository}/actions/runs/{run_id}")
    workflow_path = str(run.get("path", "")).split("@", 1)[0]
    source_repository = (run.get("head_repository") or {}).get("full_name")
    run_repository = (run.get("repository") or {}).get("full_name")
    old_commit = str(run.get("head_sha", ""))
    attempt = run.get("run_attempt")
    prior_publication_source_ref(run, version)
    if (
        str(run.get("id", "")) != run_id
        or run.get("status") != "completed"
        or run.get("conclusion") != "success"
        or run.get("event") not in ("release", "workflow_dispatch")
        or workflow_path != ".github/workflows/container-images.yml"
        or run_repository != repository
        or source_repository != repository
        or type(attempt) is not int
        or attempt < 1
        or not re.fullmatch(r"[0-9a-f]{40}", old_commit)
    ):
        raise ReleaseError("supersede_run_id is not a completed successful canonical full-version publication")
    if old_commit == current_commit:
        raise ReleaseError("A correction requires a new source commit containing the fix")

    # The prior source must be in the corrected source's history; the candidate itself must be in main.
    for ancestor, descendant, message in (
        (old_commit, current_commit, "Prior publication source is not an ancestor of the correction source"),
        (current_commit, "refs/remotes/origin/main", "Correction source is not in main history"),
    ):
        result = subprocess.run(
            ["git", "merge-base", "--is-ancestor", ancestor, descendant],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
        )
        if result.returncode:
            raise ReleaseError(message)

    artifacts_response = github_api_json(f"repos/{repository}/actions/runs/{run_id}/artifacts?per_page=100")
    artifacts = artifacts_response.get("artifacts")
    if not isinstance(artifacts, list) or artifacts_response.get("total_count") != len(artifacts):
        raise ReleaseError("Could not enumerate every artifact from the prior publication run")
    artifact_name = f"container-release-receipt-{version}-{run_id}-{attempt}"
    matches = [artifact for artifact in artifacts if isinstance(artifact, dict) and artifact.get("name") == artifact_name]
    if len(matches) != 1:
        raise ReleaseError("Prior successful run must have exactly one receipt artifact with the expected name")
    artifact = matches[0]
    artifact_id = artifact.get("id")
    if type(artifact_id) is not int or artifact_id < 1:
        raise ReleaseError("Prior receipt artifact has an invalid GitHub artifact id")
    archive = github_api_artifact_zip(f"repos/{repository}/actions/artifacts/{artifact_id}/zip")
    receipt, _by_reference, supersedes = validate_superseded_archive(
        archive,
        artifact,
        expected_name=artifact_name,
        run=run,
        version=version,
        package_versions=package_versions,
        repository=repository,
    )
    return receipt, _by_reference, supersedes


def receipt_metadata(args: argparse.Namespace) -> dict[str, Any]:
    run_url = f"https://github.com/{args.repository}/actions/runs/{args.run_id}"
    return {
        "schemaVersion": 1,
        "releaseVersion": args.version,
        "appsRepository": args.repository,
        "appsSource": {"ref": args.source_ref, "commit": args.commit},
        "appsSourceCommit": args.commit,
        "workflowRun": {
            "repository": args.repository,
            "id": args.run_id,
            "url": run_url,
            "workflow": args.workflow,
            "runAttempt": args.run_attempt,
            "event": args.event,
            "ref": args.source_ref,
            "headSha": args.commit,
            "conclusion": "success",
        },
        "packageVersions": {
            "core": args.core_version,
            "studio": args.studio_version,
            "extensions": args.extensions_version,
        },
        "workflowInputs": {
            "version": getattr(args, "input_version", ""),
            "publish": normalize_bool(getattr(args, "input_publish", "false")),
            "images": getattr(args, "input_images", ""),
            "core_version": getattr(args, "input_core_version", ""),
            "studio_version": getattr(args, "input_studio_version", ""),
            "extensions_version": getattr(args, "input_extensions_version", ""),
            "supersede_run_id": getattr(args, "supersede_run_id", ""),
            "expected_commit": (
                getattr(args, "expected_commit", "") or args.commit
                if args.event == "release"
                else getattr(args, "expected_commit", "")
            ),
        },
        "publication": "published" if args.publication == "published" else "build-only",
    }


def build_only_images(
    fragments: list[dict[str, Any]], selected_images: list[dict[str, Any]], version: str,
    package_versions: dict[str, str],
) -> list[dict[str, Any]]:
    by_id = {fragment["id"]: fragment for fragment in fragments}
    images = []
    for image in selected_images:
        fragment = by_id[image["id"]]
        entry = {
            "name": image["name"],
            "repository": image["repository"],
            "tag": version,
            "sourceRef": fragment["sourceRef"],
            "digest": fragment["digest"],
            "platforms": fragment["platforms"],
            "registryVerified": False,
            "smoke": fragment["smoke"],
            "packages": image["packages"],
            "packageVersions": {family: package_versions[family] for family in image["packages"]},
            "resolvedPackages": fragment["resolvedPackages"],
        }
        images.append(entry)
        for alias in image.get("aliases", []):
            images.append({**entry, "name": alias["name"], "repository": alias["repository"], "alias_of": image["name"]})
    return images


def aggregate_resolved_packages(images: Iterable[dict[str, Any]]) -> dict[str, list[dict[str, str]]]:
    merged: dict[str, dict[tuple[str, str], dict[str, str]]] = {}
    for image in images:
        for family, packages in image.get("resolvedPackages", {}).items():
            family_rows = merged.setdefault(family, {})
            for package in packages:
                family_rows[(package["id"], package["version"])] = package
    return {
        family: [rows[key] for key in sorted(rows, key=lambda value: (value[0].casefold(), value[1]))]
        for family, rows in sorted(merged.items())
    }


def create_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)

    validate = subparsers.add_parser("validate-run")
    validate.add_argument("--event", required=True)
    validate.add_argument("--version", default="")
    validate.add_argument("--publish", default="false")
    validate.add_argument("--images", default="all")
    validate.add_argument("--core-version", default="")
    validate.add_argument("--studio-version", default="")
    validate.add_argument("--extensions-version", default="")
    validate.add_argument("--expected-commit", default="")
    validate.add_argument("--supersede-run-id", default="")
    validate.add_argument("--ref", required=True)
    validate.add_argument("--commit", required=True)
    validate.add_argument("--release-tag", default="")
    validate.add_argument("--packages", default="Directory.Packages.props")
    validate.add_argument("--output", default=os.environ.get("GITHUB_OUTPUT"))

    matrix = subparsers.add_parser("matrix")
    matrix.add_argument("--output", default="")

    plan = subparsers.add_parser("plan-image")
    plan.add_argument("--repository", required=True)
    plan.add_argument("--version", required=True)
    plan.add_argument("--commit", required=True)
    plan.add_argument("--source-ref", required=True)
    plan.add_argument("--core-version", required=True)
    plan.add_argument("--studio-version", required=True)
    plan.add_argument("--extensions-version", required=True)
    plan.add_argument("--skip-version-tag", action="store_true")
    plan.add_argument("--output", default=os.environ.get("GITHUB_OUTPUT"))

    verify = subparsers.add_parser("verify-image")
    verify.add_argument("--id", required=True)
    verify.add_argument("--repository", required=True)
    verify.add_argument("--reference", required=True)
    verify.add_argument("--version", required=True)
    verify.add_argument("--commit", required=True)
    verify.add_argument("--source-ref", required=True)
    verify.add_argument("--core-version", required=True)
    verify.add_argument("--studio-version", required=True)
    verify.add_argument("--extensions-version", required=True)
    verify.add_argument("--port", required=True, type=int)
    verify.add_argument("--output-file", required=True)

    local = subparsers.add_parser("verify-local")
    local.add_argument("--id", required=True)
    local.add_argument("--repository", required=True)
    local.add_argument("--reference", required=True)
    local.add_argument("--version", required=True)
    local.add_argument("--commit", required=True)
    local.add_argument("--source-ref", required=True)
    local.add_argument("--core-version", required=True)
    local.add_argument("--studio-version", required=True)
    local.add_argument("--extensions-version", required=True)
    local.add_argument("--port", required=True, type=int)
    local.add_argument("--output-file", required=True)

    finalize = subparsers.add_parser("finalize")
    finalize.add_argument("--publication", choices=("published", "build-only"), required=True)
    finalize.add_argument("--version", required=True)
    finalize.add_argument("--commit", required=True)
    finalize.add_argument("--source-ref", required=True)
    finalize.add_argument("--images", default="all")
    finalize.add_argument("--core-version", required=True)
    finalize.add_argument("--studio-version", required=True)
    finalize.add_argument("--extensions-version", required=True)
    finalize.add_argument("--expected-commit", default="")
    finalize.add_argument("--input-version", default="")
    finalize.add_argument("--input-publish", default="false")
    finalize.add_argument("--input-images", default="")
    finalize.add_argument("--input-core-version", default="")
    finalize.add_argument("--input-studio-version", default="")
    finalize.add_argument("--input-extensions-version", default="")
    finalize.add_argument("--supersede-run-id", default="")
    finalize.add_argument("--event", required=True)
    finalize.add_argument("--repository", default=os.environ.get("GITHUB_REPOSITORY", APPS_REPOSITORY))
    finalize.add_argument("--run-id", default=os.environ.get("GITHUB_RUN_ID", "local"))
    finalize.add_argument("--run-attempt", type=int, default=os.environ.get("GITHUB_RUN_ATTEMPT", "1"))
    finalize.add_argument("--workflow", default=".github/workflows/container-images.yml")
    finalize.add_argument("--artifact-dir", required=True)
    finalize.add_argument("--output-file", default="artifacts/container-release-receipt.json")

    return parser


def main(argv: list[str] | None = None) -> int:
    args = create_parser().parse_args(argv)
    try:
        validate_inventory()
        if args.command == "validate-run":
            run = resolve_run(args)
            github_output(
                {
                    "release_version": run["version"],
                    "publish": run["publish"],
                    "source_ref": run["sourceRef"],
                    "source_commit": run["sourceCommit"],
                    "core_version": run["packageVersions"]["core"],
                    "studio_version": run["packageVersions"]["studio"],
                    "extensions_version": run["packageVersions"]["extensions"],
                    "supersede_run_id": run["supersedeRunId"],
                    "images": ",".join(image["name"] for image in run["images"]),
                    "matrix": run["matrix"],
                },
                args.output,
            )
        elif args.command == "matrix":
            content = json.dumps({"include": list(IMAGES)}, separators=(",", ":")) + "\n"
            if args.output:
                Path(args.output).write_text(content, encoding="utf-8")
            else:
                print(content, end="")
        elif args.command == "plan-image":
            package_versions = {
                "core": args.core_version,
                "studio": args.studio_version,
                "extensions": args.extensions_version,
            }
            plan = get_image_plan_with_packages(
                args.repository,
                args.version,
                args.commit,
                args.source_ref,
                package_versions,
                skip_version_tag=args.skip_version_tag,
            )
            github_output(
                {
                    "source_ref": plan["source_ref"],
                    "reuse": plan["reuse"],
                    "digest": (plan["manifest"] or {}).get("digest", ""),
                },
                args.output,
            )
        elif args.command == "verify-image":
            image = next(item for item in IMAGES if item["id"] == args.id)
            package_versions = {
                "core": args.core_version,
                "studio": args.studio_version,
                "extensions": args.extensions_version,
            }
            evidence = verify_registry_image(
                args.repository,
                args.reference,
                args.version,
                args.commit,
                args.source_ref,
                package_versions,
                args.port,
                image,
            )
            write_json(
                Path(args.output_file),
                {
                    "id": args.id,
                    "name": image["name"],
                    "repository": args.repository,
                    "sourceRef": args.reference,
                    **evidence,
                },
            )
        elif args.command == "verify-local":
            run_command(["docker", "image", "inspect", args.reference])
            labels_result = run_command(
                ["docker", "image", "inspect", "--format", "{{json .Config.Labels}}", args.reference]
            )
            labels = json.loads(labels_result.stdout)
            package_versions = {
                "core": args.core_version,
                "studio": args.studio_version,
                "extensions": args.extensions_version,
            }
            image = next(item for item in IMAGES if item["id"] == args.id)
            for key, value in labels_for(args.version, args.commit, args.source_ref, package_versions).items():
                if labels.get(key) != value:
                    raise ReleaseError(f"Local image label {key!r} does not match expected provenance")
            image_id = run_command(["docker", "image", "inspect", "--format", "{{.Id}}", args.reference]).stdout.strip()
            assets = json.loads(
                run_command(["docker", "run", "--rm", "--entrypoint", "cat", args.reference, image["assetsPath"]]).stdout
            )
            resolved_packages = validate_assets_json(assets, package_versions, image["packages"])
            smoke = [
                smoke_image(
                    args.reference,
                    "linux/amd64",
                    args.port,
                    local=True,
                    auth_enabled=image.get("smokeAuth", False),
                    smoke_assets=image.get("smokeAssets", []),
                    image_digest=image_id,
                )
            ]
            write_json(
                Path(args.output_file),
                {
                    "id": args.id,
                    "name": image["name"],
                    "repository": args.repository,
                    "sourceRef": args.reference,
                    "digest": image_id,
                    "platforms": [{"platform": "linux/amd64", "digest": image_id}],
                    "smoke": {"success": True, "imageDigest": image_id, "platforms": smoke},
                    "resolvedPackages": resolved_packages,
                },
            )
        elif args.command == "finalize":
            selected_images = parse_image_selection(args.images)
            artifact_root = Path(args.artifact_dir)
            if not artifact_root.is_dir():
                raise ReleaseError(f"Image evidence artifact directory does not exist: {artifact_root}")
            selected_artifacts = select_latest_image_artifacts(
                artifact_root.rglob("*.json"), selected_images, args.run_id, args.run_attempt
            )
            fragments = read_fragments(selected_artifacts, selected_images)
            package_versions = {
                "core": args.core_version,
                "studio": args.studio_version,
                "extensions": args.extensions_version,
            }
            validate_fragment_evidence(
                fragments,
                selected_images,
                args.version,
                args.commit,
                package_versions,
                args.publication,
            )
            supersede_run_id = str(getattr(args, "supersede_run_id", "") or "").strip()
            superseded_receipt = None
            supersedes = None
            if args.publication == "published":
                if supersede_run_id:
                    validate_supersede_request(
                        supersede_run_id,
                        args.event,
                        args.version,
                        True,
                        args.source_ref,
                        selected_images,
                        package_versions,
                    )
                    if args.expected_commit != args.commit:
                        raise ReleaseError("Correction expected_commit must equal the checked-out source commit")
                    if str(args.run_id) == supersede_run_id:
                        raise ReleaseError("A correction cannot supersede its own workflow run")
                    superseded_receipt, prior_by_reference, supersedes = load_superseded_publication(
                        supersede_run_id,
                        version=args.version,
                        package_versions=package_versions,
                        repository=args.repository,
                        current_run_id=str(args.run_id),
                        current_commit=args.commit,
                    )
                    images = run_docker_promotion(
                        fragments,
                        selected_images,
                        args.version,
                        args.commit,
                        args.source_ref,
                        package_versions,
                        prior_by_reference=prior_by_reference,
                        prior_commit=superseded_receipt["appsSourceCommit"],
                        prior_source_ref=superseded_receipt["appsSource"]["ref"],
                        prior_package_versions=superseded_receipt["packageVersions"],
                    )
                else:
                    images = run_docker_promotion(
                        fragments,
                        selected_images,
                        args.version,
                        args.commit,
                        args.source_ref,
                        package_versions,
                    )
                registry_verified_at = dt.datetime.now(dt.timezone.utc).isoformat().replace("+00:00", "Z")
            else:
                images = build_only_images(fragments, selected_images, args.version, package_versions)
                registry_verified_at = None
            receipt = receipt_metadata(args)
            if supersedes is not None:
                receipt["supersedes"] = supersedes
            receipt["images"] = images
            receipt["resolvedPackages"] = aggregate_resolved_packages(images)
            receipt["imageEvidence"] = [
                {
                    "profile": fragment["name"],
                    **fragment["evidenceRun"],
                }
                for fragment in fragments
            ]
            receipt["registryVerifiedAt"] = registry_verified_at
            receipt["smoke"] = {
                "success": all(image.get("smoke", {}).get("success") for image in images),
                "results": [
                    {
                        "repository": image["repository"],
                        "tag": image["tag"],
                        "imageDigest": image["digest"],
                        **image["smoke"],
                    }
                    for image in images
                ],
            }
            write_json(Path(args.output_file), receipt)
            print(f"Wrote {args.output_file}")
        return 0
    except (ReleaseError, StopIteration, ET.ParseError, OSError, json.JSONDecodeError) as error:
        print(f"container release error: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
