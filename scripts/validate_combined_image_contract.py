#!/usr/bin/env python3
"""Fail closed when the combined Community image is not a Server+Studio host.

The documented elsaworkflows/elsa-server-and-studio-v3 contract is one process that
serves Elsa Studio (login, designer, dashboard, workflows) and the Elsa Server API.
Pointing docker/ElsaServerAndStudio.Dockerfile at Elsa.ModularServer.Web, a
Studio-only host, or an Apps/Templates 3.9 package consumer is a regression.
"""

from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOCKERFILE = ROOT / "docker" / "ElsaServerAndStudio.Dockerfile"
HOST_PROJECT = ROOT / "studio" / "src" / "hosts" / "Elsa.ServerAndStudio.Web" / "Elsa.ServerAndStudio.Web.csproj"
HOST_PROGRAM = ROOT / "studio" / "src" / "hosts" / "Elsa.ServerAndStudio.Web" / "Program.cs"
HOST_PAGE = ROOT / "studio" / "src" / "hosts" / "Elsa.ServerAndStudio.Web" / "Pages" / "_Host.cshtml"
HOST_SETTINGS = ROOT / "studio" / "src" / "hosts" / "Elsa.ServerAndStudio.Web" / "appsettings.json"

FORBIDDEN_DOCKERFILE_MARKERS = (
    "Elsa.ModularServer.Web",
    "Elsa.Studio.Host.Server.dll",
    "apps/src/",
    "Elsa.Server.Studio.BlazorServer",
    "Company.ElsaCombined.Host",
)
REQUIRED_DOCKERFILE_MARKERS = (
    "studio/src/hosts/Elsa.ServerAndStudio.Web/Elsa.ServerAndStudio.Web.csproj",
    "Elsa.ServerAndStudio.Web.dll",
    "scripts/integration-program/build_studio_clientlibs.sh",
    "STUDIO_CLIENTLIB_SKIP_TESTS=1",
    "node:22-bookworm",
)
FORBIDDEN_PROJECT_MARKERS = (
    'PackageReference Include="Elsa"',
    'Version="3.9.0"',
    "apps/src/",
)
REQUIRED_PROGRAM_MARKERS = (
    "MapHealthChecks(\"/health/live\"",
    "MapHealthChecks(\"/health/ready\"",
    "MapWorkflowsApi(",
    "MapFallbackToPage(\"/_Host\")",
    "AddElsaIdentity",
    "AddWorkflowsModule",
)
FORBIDDEN_PROGRAM_MARKERS = (
    "MapHealthChecks(\"/\",",
    "MapHealthChecks(\"/\")",
)
REQUIRED_PAGE_MARKERS = (
    '_content/Elsa.Studio.Workflows.Designer/designer.css',
    "_framework/blazor.server.js",
    "typeof(App)",
)
REQUIRED_SETTINGS_MARKERS = (
    '"Url": "http://127.0.0.1:8080/elsa/api"',
    '"RoutePrefix": "elsa/api"',
)


def _require_contains(path: Path, required: tuple[str, ...], forbidden: tuple[str, ...] = (), *, root: Path = ROOT) -> list[str]:
    errors: list[str] = []
    text = path.read_text(encoding="utf-8")
    label = path.relative_to(root) if path.is_relative_to(root) else path
    for marker in required:
        if marker not in text:
            errors.append(f"{label} is missing required marker: {marker}")
    for marker in forbidden:
        if marker in text:
            errors.append(f"{label} contains forbidden marker: {marker}")
    return errors


def validate_combined_image_contract(root: Path | None = None) -> list[str]:
    base = root or ROOT
    dockerfile = base / "docker" / "ElsaServerAndStudio.Dockerfile"
    host_project = base / "studio" / "src" / "hosts" / "Elsa.ServerAndStudio.Web" / "Elsa.ServerAndStudio.Web.csproj"
    host_program = base / "studio" / "src" / "hosts" / "Elsa.ServerAndStudio.Web" / "Program.cs"
    host_page = base / "studio" / "src" / "hosts" / "Elsa.ServerAndStudio.Web" / "Pages" / "_Host.cshtml"
    host_settings = base / "studio" / "src" / "hosts" / "Elsa.ServerAndStudio.Web" / "appsettings.json"
    errors: list[str] = []
    for path in (dockerfile, host_project, host_program, host_page, host_settings):
        if not path.is_file():
            errors.append(f"Missing required combined-image file: {path.relative_to(base)}")
    if errors:
        return errors
    errors.extend(_require_contains(dockerfile, REQUIRED_DOCKERFILE_MARKERS, FORBIDDEN_DOCKERFILE_MARKERS, root=base))
    errors.extend(_require_contains(host_project, ('Sdk="Microsoft.NET.Sdk.Web"',), FORBIDDEN_PROJECT_MARKERS, root=base))
    if '<ProjectReference Include="' not in host_project.read_text(encoding="utf-8"):
        errors.append("Combined host must reference in-repo Server and Studio projects, not released packages.")
    errors.extend(_require_contains(host_program, REQUIRED_PROGRAM_MARKERS, FORBIDDEN_PROGRAM_MARKERS, root=base))
    errors.extend(_require_contains(host_page, REQUIRED_PAGE_MARKERS, root=base))
    errors.extend(_require_contains(host_settings, REQUIRED_SETTINGS_MARKERS, root=base))
    return errors


def main() -> int:
    errors = validate_combined_image_contract()
    if errors:
        print("Combined image contract failed:", file=__import__("sys").stderr)
        for error in errors:
            print(f"  {error}", file=__import__("sys").stderr)
        return 1
    print("Combined image contract passed: Server+Studio host, ClientLib build, in-process API.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
