"""Prepare and verify a disposable source-bound Extensions checkout for #8260."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

from package_impact import InventoryGraph

CORE = "elsa-core"
EXTENSIONS = "elsa-extensions"
SLACK = (EXTENSIONS, "src/modules/communication/Elsa.Slack/Elsa.Slack.csproj")
CHANGED_CORE = (CORE, "src/modules/Elsa/Elsa.csproj")
PACKAGE_REFERENCE = re.compile(rb'<PackageReference Include="([^"]+)"\s*/>')
CSHELLS_PROJECT = re.compile(
    rb'<ProjectReference Include="[^\"]*[/\\]cshells[/\\]src[/\\]CShells\.Abstractions[/\\]CShells\.Abstractions\.csproj"\s*/>'
)
CSHELLS_CONSUMERS = {
    "src/modules/scheduling/Elsa.Scheduling.Quartz/Elsa.Scheduling.Quartz.csproj",
    "src/modules/servicebus/Elsa.ServiceBus.MassTransit/Elsa.ServiceBus.MassTransit.csproj",
    "src/modules/servicebus/Elsa.ServiceBus.MassTransit.AzureServiceBus/Elsa.ServiceBus.MassTransit.AzureServiceBus.csproj",
    "src/modules/servicebus/Elsa.ServiceBus.MassTransit.RabbitMq/Elsa.ServiceBus.MassTransit.RabbitMq.csproj",
}


def git(root: Path, *args: str) -> bytes:
    return subprocess.check_output(["git", "-C", str(root), *args], stderr=subprocess.PIPE)


def digest(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def expected_bindings(inventory: dict) -> dict[str, dict[str, str]]:
    """Find source-owned package edges reachable from the recorded Core-change roots."""
    graph = InventoryGraph(inventory)
    owners: dict[str, list[tuple[str, str]]] = {}
    for key, project in graph.projects.items():
        if project.get("package_id"):
            owners.setdefault(project["package_id"].casefold(), []).append(key)

    pending = list(graph.affected_tests([CHANGED_CORE])) + [SLACK]
    reached = set()
    bindings: dict[str, dict[str, str]] = {}
    while pending:
        key = pending.pop()
        if key in reached:
            continue
        reached.add(key)
        project = graph.projects[key]
        for reference in project.get("project_references", []):
            target = None
            if reference.get("target_project"):
                target = (key[0], reference["target_project"])
            elif reference.get("external_project"):
                name = reference.get("external_repository", "").rsplit("/", 1)[-1]
                if name in (CORE, EXTENSIONS):
                    target = (name, reference["external_project"])
            if target in graph.projects:
                pending.append(target)
        for reference in project.get("package_references", []):
            package_id = reference["id"]
            candidates = owners.get(package_id.casefold(), [])
            core_candidates = [candidate for candidate in candidates if candidate[0] == CORE]
            if not core_candidates:
                continue
            if len(candidates) != 1:
                raise ValueError(f"Source-owned package {package_id} has ambiguous owners: {candidates}")
            pending.append(candidates[0])
            if key[0] == EXTENSIONS:
                bindings.setdefault(key[1], {})[package_id] = candidates[0][1]

    identities = {package for entries in bindings.values() for package in entries}
    if len(bindings) != 17 or len(identities) != 26:
        raise ValueError(
            f"Pinned source-binding scope changed: {len(bindings)} projects, {len(identities)} package IDs; "
            "review inventory and affected closure before changing the proof"
        )
    return dict(sorted(bindings.items()))


def source_bound_project(original: bytes, replacements: dict[str, str]) -> bytes:
    found: set[str] = set()

    def replace(match: re.Match[bytes]) -> bytes:
        package_id = match.group(1).decode("utf-8")
        if package_id not in replacements:
            return match.group(0)
        if package_id in found:
            raise ValueError(f"Duplicate package reference {package_id}")
        found.add(package_id)
        return f'<ProjectReference Include="{replacements[package_id]}" />'.encode("utf-8")

    result = PACKAGE_REFERENCE.sub(replace, original)
    if found != set(replacements):
        raise ValueError(f"Package references do not match reviewed inventory: missing={sorted(set(replacements) - found)}")
    return result


def expected_files(inventory: dict, core: Path, overlay: Path) -> list[dict]:
    versions = ET.parse(overlay / "Directory.Packages.props").getroot()
    cshells_versions = [item.attrib.get("Version") for item in versions.iter("PackageVersion")
                       if item.attrib.get("Include") == "CShells.Abstractions"]
    if cshells_versions != ["0.0.28"]:
        raise ValueError(f"Pinned external CShells.Abstractions package version changed: {cshells_versions}")
    rows = []
    source_packages = expected_bindings(inventory)
    for relative in sorted(source_packages.keys() | CSHELLS_CONSUMERS):
        packages = source_packages.get(relative, {})
        project = overlay / relative
        original = git(overlay, "show", f"HEAD:{relative}")
        replacements = {
            package: os.path.relpath(core / owner, project.parent).replace(os.sep, "/")
            for package, owner in packages.items()
        }
        transformed = source_bound_project(original, replacements)
        external_package = None
        if relative in CSHELLS_CONSUMERS:
            transformed, count = CSHELLS_PROJECT.subn(b'<PackageReference Include="CShells.Abstractions" />', transformed)
            if count != 1:
                raise ValueError(f"Expected one external CShells project edge in {relative}; found {count}")
            external_package = {"package_id": "CShells.Abstractions", "version": "0.0.28"}
        rows.append({
            "project": relative,
            "before_sha256": digest(original),
            "after_sha256": digest(transformed),
            "bindings": [{"package_id": package, "core_project": packages[package],
                          "project_reference": replacements[package]} for package in sorted(packages)],
            "external_package_boundary": external_package,
            "content": transformed,
        })
    return rows


def verify_overlay(inventory: dict, core: Path, pristine: Path, overlay: Path, receipt: dict) -> None:
    if receipt.get("schema_version") != 1:
        raise ValueError("Unsupported source-binding receipt schema")
    pins = inventory["repositories"]
    for name, root in ((CORE, core), (EXTENSIONS, pristine), (EXTENSIONS, overlay)):
        actual = git(root, "rev-parse", "HEAD").decode().strip()
        if actual != pins[name]["commit"]:
            raise ValueError(f"{name} checkout pin mismatch at {root}: {actual}")
    for root in (core, pristine):
        if git(root, "status", "--porcelain", "--untracked-files=normal").strip():
            raise ValueError(f"Original pinned checkout is not clean: {root}")
    if receipt.get("core") != str(core.resolve()) or receipt.get("pristine_extensions") != str(pristine.resolve()) or receipt.get("overlay_extensions") != str(overlay.resolve()):
        raise ValueError("Source-binding receipt does not match supplied checkout paths")
    if receipt.get("core_commit") != pins[CORE]["commit"] or receipt.get("extensions_commit") != pins[EXTENSIONS]["commit"]:
        raise ValueError("Source-binding receipt does not match inventory commits")
    rows = expected_files(inventory, core, overlay)
    expected = [{key: value for key, value in row.items() if key != "content"} for row in rows]
    if receipt.get("files") != expected:
        raise ValueError("Source-binding receipt differs from the reviewed inventory transformation")
    modified = set(git(overlay, "status", "--porcelain", "--untracked-files=normal").decode().splitlines())
    wanted = {f" M {row['project']}" for row in rows}
    if modified != wanted:
        raise ValueError(f"Source-bound overlay has unexpected changes: {sorted(modified ^ wanted)}")
    for row in rows:
        if (overlay / row["project"]).read_bytes() != row["content"]:
            raise ValueError(f"Source-bound project changed since receipt: {row['project']}")


def prepare(inventory: dict, core: Path, pristine: Path, overlay: Path) -> dict:
    if overlay.exists():
        raise ValueError(f"Disposable overlay already exists: {overlay}")
    if git(core, "status", "--porcelain", "--untracked-files=normal").strip() or git(pristine, "status", "--porcelain", "--untracked-files=normal").strip():
        raise ValueError("Pinned input source checkouts must be clean")
    for name, root in ((CORE, core), (EXTENSIONS, pristine)):
        if git(root, "rev-parse", "HEAD").decode().strip() != inventory["repositories"][name]["commit"]:
            raise ValueError(f"Source pin mismatch before overlay creation: {name}")
    git(pristine, "worktree", "add", "--detach", str(overlay), "HEAD")
    rows = expected_files(inventory, core, overlay)
    for row in rows:
        (overlay / row["project"]).write_bytes(row["content"])
    receipt = {
        "schema_version": 1,
        "core": str(core.resolve()),
        "pristine_extensions": str(pristine.resolve()),
        "overlay_extensions": str(overlay.resolve()),
        "core_commit": inventory["repositories"][CORE]["commit"],
        "extensions_commit": inventory["repositories"][EXTENSIONS]["commit"],
        "files": [{key: value for key, value in row.items() if key != "content"} for row in rows],
    }
    verify_overlay(inventory, core, pristine, overlay, receipt)
    return receipt


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--inventory", type=Path, required=True)
    parser.add_argument("--core", type=Path, required=True)
    parser.add_argument("--extensions", type=Path, required=True)
    parser.add_argument("--overlay", type=Path, required=True)
    parser.add_argument("--receipt", type=Path, required=True)
    args = parser.parse_args()
    inventory = json.loads(args.inventory.read_text(encoding="utf-8"))
    core, pristine, overlay = (path.resolve() for path in (args.core, args.extensions, args.overlay))
    receipt = prepare(inventory, core, pristine, overlay)
    args.receipt.parent.mkdir(parents=True, exist_ok=True)
    args.receipt.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(f"Prepared {len(receipt['files'])} disposable source-bound Extensions projects: {overlay}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
