#!/usr/bin/env python3
"""Throwaway Docker proof for combined-image identity restart and SQLite path continuity.

Builds a local image from the current tree. Restarts the same container so default
`/app/elsa_workflows.db` (plus WAL) is opened in place. Does not push, publish, tag for
release, or touch candidate artifacts.
"""

from __future__ import annotations

import argparse
import json
import os
import secrets
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT / "scripts") not in sys.path:
    sys.path.insert(0, str(ROOT / "scripts"))
from smoke_combined_image import generate_throwaway_env, redact, run_smoke, write_json  # noqa: E402


def run(command: list[str], **kwargs) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, check=True, text=True, **kwargs)


def wait_running(name: str, timeout_seconds: int = 30) -> None:
    deadline = time.time() + timeout_seconds
    while time.time() < deadline:
        result = subprocess.run(
            ["docker", "inspect", "-f", "{{.State.Running}}", name],
            capture_output=True,
            text=True,
        )
        if result.returncode == 0 and result.stdout.strip() == "true":
            return
        time.sleep(1)
    raise TimeoutError(f"Container {name} did not return to running")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--image", default=os.environ.get("ELSA_COMBINED_THROWAWAY_IMAGE", "elsa-server-and-studio-throwaway:local"))
    parser.add_argument("--skip-build", action="store_true")
    parser.add_argument("--port", default="13000")
    parser.add_argument("--artifacts", default=str(Path("/tmp/elsa-combined-image-correctives")))
    args = parser.parse_args()
    artifacts = Path(args.artifacts)
    artifacts.mkdir(parents=True, exist_ok=True)
    source_sha = run(["git", "-C", str(ROOT), "rev-parse", "HEAD"], capture_output=True).stdout.strip()
    env = generate_throwaway_env()
    password = env["DefaultAdminUser__AdminPassword"]
    container = f"elsa-combined-throwaway-{secrets.token_hex(3)}"
    image = args.image

    if not args.skip_build:
        run(
            [
                "docker",
                "buildx",
                "build",
                "--load",
                "-f",
                str(ROOT / "docker" / "ElsaServerAndStudio.Dockerfile"),
                "-t",
                image,
                "--label",
                "elsa.combined.throwaway=1",
                "--label",
                f"elsa.combined.source-sha={source_sha}",
                str(ROOT),
            ],
            cwd=ROOT,
        )

    docker_env: list[str] = []
    for key, value in env.items():
        docker_env.extend(["-e", f"{key}={value}"])
    run(
        [
            "docker",
            "run",
            "-d",
            "--name",
            container,
            "-p",
            f"127.0.0.1:{args.port}:8080",
            *docker_env,
            image,
        ]
    )
    try:
        first = run_smoke(f"http://127.0.0.1:{args.port}", "admin", password, artifacts / "first")
        write_json(artifacts / "first-smoke.json", first)
        if not first["passed"]:
            print(json.dumps(redact(first), indent=2))
            return 1
        sqlite_check = run(
            ["docker", "exec", container, "ls", "-l", "/app/elsa_workflows.db"],
            capture_output=True,
        )
        (artifacts / "sqlite-ls.txt").write_text(sqlite_check.stdout, encoding="utf-8")
        subprocess.run(
            ["docker", "cp", f"{container}:/app/elsa_workflows.db", str(artifacts / "elsa_workflows.db")],
            check=False,
        )
        role_name = first["checks"]["identity"]["roleName"]
        workflow_name = first["checks"]["api"]["loadedName"]
        run(["docker", "restart", container])
        wait_running(container)
        os.environ["ELSA_COMBINED_EXPECT_ROLE"] = role_name
        os.environ["ELSA_COMBINED_EXPECT_WORKFLOW"] = workflow_name
        second = run_smoke(f"http://127.0.0.1:{args.port}", "admin", password, artifacts / "restart")
        write_json(artifacts / "restart-smoke.json", second)
        print(json.dumps(redact({
            "firstPassed": first["passed"],
            "restartPassed": second["passed"],
            "roleName": role_name,
            "workflowName": workflow_name,
            "sqlitePath": "/app/elsa_workflows.db",
            "image": image,
            "sourceSha": source_sha,
            "firstFailures": first.get("failures"),
            "restartFailures": second.get("failures"),
        }), indent=2))
        return 0 if second["passed"] else 1
    finally:
        subprocess.run(["docker", "rm", "-f", container], check=False, capture_output=True)


if __name__ == "__main__":
    raise SystemExit(main())
