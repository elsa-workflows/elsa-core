#!/usr/bin/env python3
"""Fail when docker/ElsaStudio.Dockerfile builds or runs a server host.

elsa-studio-v3 must publish the Studio-only Blazor Server host. The 3.9
image ran Elsa.ModularServer.Web, the same entry point as
elsa-server-and-studio-v3. This check is static: it reads the Dockerfile
and rejects server project paths, restore/publish targets, and CMD.
"""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_DOCKERFILE = ROOT / "docker/ElsaStudio.Dockerfile"
STUDIO_PROJECT = "studio/src/hosts/Elsa.Studio.Host.Server/Elsa.Studio.Host.Server.csproj"
STUDIO_DLL = "Elsa.Studio.Host.Server.dll"
SERVER_MARKERS = (
    "Elsa.ModularServer.Web",
    "Elsa.Server.Web",
    "Elsa.ServerAndStudio",
    "Elsa.Server.Web.dll",
    "Elsa.ModularServer.Web.dll",
    "Elsa.ServerAndStudio.Web.dll",
)


def dockerfile_errors(text: str) -> list[str]:
    """Return why the Studio Dockerfile is not a Studio-only host."""
    errors = []
    for marker in SERVER_MARKERS:
        if marker in text:
            errors.append(f"Studio Dockerfile must not mention {marker}.")
    if STUDIO_PROJECT not in text:
        errors.append(f"Studio Dockerfile must restore and publish {STUDIO_PROJECT}.")
    if f'CMD ["dotnet", "{STUDIO_DLL}"]' not in text:
        errors.append(f'Studio Dockerfile CMD must be ["dotnet", "{STUDIO_DLL}"].')
    return errors


def main() -> int:
    path = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_DOCKERFILE
    if not path.is_file():
        print(f"Studio Dockerfile not found: {path}", file=sys.stderr)
        return 1
    errors = dockerfile_errors(path.read_text(encoding="utf-8"))
    for error in errors:
        print(error, file=sys.stderr)
    if errors:
        return 1
    print(f"Studio Dockerfile publishes {STUDIO_PROJECT} and runs {STUDIO_DLL}.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
