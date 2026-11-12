#!/usr/bin/env python3
"""Publish a single npm tarball to a registry with fail-closed handling.

Distinguishes an already-published version conflict from real failures such as
authentication or network errors, and optionally confirms the package version is
available in the registry after a successful publish.
"""
from __future__ import annotations

import argparse
import os
import subprocess
import sys
import time
from pathlib import Path


class PublishError(Exception):
    """A failure that should stop the workflow."""


def _npm(args: list[str], check: bool = True, timeout: int | None = 120, **kwargs) -> subprocess.CompletedProcess[str]:
    result = subprocess.run(
        ["npm", *args],
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        timeout=timeout,
        **kwargs,
    )
    if check and result.returncode != 0:
        raise PublishError(result.stdout)
    return result


def _already_published_output(output: str) -> bool:
    lowered = output.lower()
    markers = (
        "epublishconflict",
        "cannot publish over",
        "you cannot publish over the previously published versions",
        "version already exists",
    )
    return any(marker in lowered for marker in markers)


def _auth_error(output: str) -> bool:
    lowered = output.lower()
    return any(marker in lowered for marker in (
        "e401",
        "e403",
        "401 unauthorized",
        "403 forbidden",
        "eneedauth",
        "no auth token",
        "invalid token",
    ))


def _network_error(output: str) -> bool:
    lowered = output.lower()
    return any(marker in lowered for marker in (
        "enotfound",
        "econnrefused",
        "econnreset",
        "etimedout",
        "eai_again",
        "socket hang up",
        "network error",
    ))


def publish(tarball: Path, package: str, version: str, tag: str, registry: str) -> None:
    """Publish the tarball, treating an already-published conflict as success only
    when the registry confirms the version exists.
    """
    result = _npm([
        "publish", str(tarball),
        "--access", "public",
        "--tag", tag,
        "--registry", registry,
    ], check=False, timeout=180)
    output = result.stdout
    print(output, end="")

    if result.returncode == 0:
        return

    if _already_published_output(output):
        view = _npm(["view", f"{package}@{version}", "--registry", registry], check=False)
        if view.returncode == 0:
            print(
                f"::notice::{package}@{version} is already published in the registry; skipping.",
                file=sys.stderr,
            )
            return
        raise PublishError(
            f"{package}@{version} reported a publish conflict, but the version is not available "
            f"in the registry."
        )

    if _auth_error(output):
        raise PublishError(f"Authentication failed while publishing {package}@{version}.")

    if _network_error(output):
        raise PublishError(f"Network error while publishing {package}@{version}.")

    raise PublishError(f"Failed to publish {package}@{version}.")


def confirm_available(package: str, version: str, registry: str, retries: int, delay: int) -> None:
    """Wait until the registry reports the published version is available."""
    last_output = ""
    for attempt in range(retries + 1):
        result = _npm(["view", f"{package}@{version}", "--registry", registry], check=False)
        if result.returncode == 0:
            print(f"::notice::{package}@{version} is available in the registry.")
            return
        last_output = result.stdout
        if attempt < retries:
            print(
                f"::warning::{package}@{version} not yet visible (attempt {attempt + 1}/{retries + 1}); "
                f"retrying in {delay}s...",
                file=sys.stderr,
            )
            time.sleep(delay)

    raise PublishError(
        f"{package}@{version} was published but is not available in the registry after "
        f"{retries + 1} attempts.\n{last_output}"
    )


def main() -> int:
    parser = argparse.ArgumentParser(description="Publish an npm tarball to a registry.")
    parser.add_argument("--tarball", required=True, type=Path, help="Path to the .tgz archive.")
    parser.add_argument("--package", required=True, help="Scoped npm package name.")
    parser.add_argument("--version", required=True, help="Package version to publish.")
    parser.add_argument("--tag", required=True, help="npm dist-tag to apply.")
    parser.add_argument("--registry", required=True, help="Registry URL.")
    parser.add_argument(
        "--verify-published",
        action="store_true",
        help="Confirm the version is available in the registry after publishing.",
    )
    parser.add_argument("--retries", type=int, default=6, help="Registry confirmation retries.")
    parser.add_argument("--retry-delay", type=int, default=10, help="Seconds between retries.")
    args = parser.parse_args()

    token = os.environ.get("NODE_AUTH_TOKEN", "")
    if not token:
        print("::error::NODE_AUTH_TOKEN is empty; nothing was published.", file=sys.stderr)
        return 1

    try:
        publish(args.tarball, args.package, args.version, args.tag, args.registry)
        if args.verify_published:
            confirm_available(
                args.package, args.version, args.registry, args.retries, args.retry_delay
            )
    except PublishError as exc:
        print(f"::error::{exc}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
