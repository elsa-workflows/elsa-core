"""Current Apps inventory and checkout safety, independent of the historical import."""

import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import xml.etree.ElementTree as ET


def require(condition: bool, message: object) -> None:
    if not condition:
        raise ValueError(str(message))


def git_environment() -> dict[str, str]:
    # Evidence must come from the selected checkout, never ambient repository/config routing.
    return {key: value for key, value in os.environ.items() if not key.startswith("GIT_")}


def git(root: Path, *arguments: str) -> str:
    return subprocess.check_output(["git", "-C", str(root), *arguments], cwd=root,
                                   env=git_environment(), text=True).strip()


def regular_file(root: Path, relative: str) -> Path:
    path = PurePosixPath(relative)
    require(not path.is_absolute() and ".." not in path.parts, f"Unsafe checkout path: {relative}")
    file = root / path
    for component in (file, *file.parents):
        if component == root:
            break
        require(not component.is_symlink(), f"Mapped asset traverses a symlink: {relative}")
    require(file.resolve().is_relative_to(root), f"Mapped asset escapes checkout: {relative}")
    require(file.is_file(), f"Missing mapped asset: {relative}")
    return file


def tracked_apps(root: Path) -> dict[str, tuple[str, str]]:
    git(root, "diff", "--cached", "--quiet", "HEAD", "--", "apps/")
    rows = {}
    for row in git(root, "ls-files", "--stage", "-z", "--", "apps/").split("\0"):
        if not row:
            continue
        metadata, path = row.split("\t", 1)
        mode, blob, stage = metadata.split()
        require(stage == "0" and mode in {"100644", "100755"}, f"Unsafe tracked Apps entry: {path}")
        rows[path] = (mode, blob)
    require(rows, "Current Apps inventory is empty")
    return rows


def current_apps_projects(root: Path) -> list[Path]:
    tracked = {path for path in tracked_apps(root) if path.endswith(".csproj")}
    solution = json.loads(regular_file(root, "Elsa.Apps.slnf").read_text())["solution"]
    require(solution["path"] == "Elsa.sln", "Apps filter must select the root solution")
    selected = solution["projects"]
    require(len(selected) == len(set(selected)) and set(selected) == tracked,
            "Apps filter must match every current tracked Apps project exactly once")
    root_projects = [path.replace("\\", "/") for path in re.findall(
        r'"([^"\n]+\.csproj)"', regular_file(root, "Elsa.sln").read_text(encoding="utf-8-sig"))]
    apps_projects = [path for path in root_projects if path.startswith("apps/")]
    require(len(apps_projects) == len(set(apps_projects)) and set(apps_projects) == tracked,
            "Root solution must match the current tracked Apps project inventory")
    return [regular_file(root, path) for path in sorted(tracked)]


def declared_package_versions(root: Path) -> dict[str, str]:
    document = ET.parse(regular_file(root, "apps/Directory.Packages.props"))
    versions = {name: document.findtext(f".//{name}") for name in
                ("ElsaVersion", "ElsaStudioVersion", "ElsaExtensionsVersion")}
    require(all(isinstance(value, str) and value.strip() and "$(" not in value
                for value in versions.values()), "Apps must declare its current package-family versions")
    return versions
