#!/usr/bin/env python3
"""Verify the original Apps history/tree, mapped assets and package-consumer boundary."""

import argparse
import json
import subprocess
from pathlib import Path

from apps_consumer_inventory import (current_apps_projects, declared_package_versions,
                                     git as checkout_git, git_environment, regular_file, require, tracked_apps)

ROOT = Path(__file__).resolve().parents[2]


def git(*args: str) -> str:
    return checkout_git(ROOT, *args)


def verify_restore_graph(path: Path, projects: list[Path]) -> None:
    graph = json.loads(path.read_text())
    restored = graph["projects"]
    require({Path(project).resolve() for project in restored} == set(projects), "Root restore graph must contain exactly the current Apps projects")
    for project, document in restored.items():
        configs = [Path(config).resolve() for config in document["restore"]["configFilePaths"]]
        require(configs == [ROOT / "apps/NuGet.Config"], f"Root restore must use only Apps NuGet.Config: {project}")


def verify_project_evaluation(root: Path, project: Path, evaluation: dict, versions: dict[str, str], projects: set[Path]) -> None:
    properties = evaluation["Properties"]
    require(properties['TargetFramework'] == 'net10.0', project)
    require(properties['UseProjectReferences'].lower() == 'false', project)
    require(properties['IsPackable'].lower() == 'false', project)
    require(Path(properties['RestoreConfigFile']).resolve() == root / 'apps/NuGet.Config', project)
    require(properties['ManagePackageVersionsCentrally'].lower() == 'true', project)
    require(Path(properties['DirectoryPackagesPropsPath']).resolve() == root / 'apps/Directory.Packages.props', project)
    require(all(properties[name] == value for name, value in versions.items()), project)
    for reference in evaluation["Items"]["ProjectReference"]:
        require(Path(reference['FullPath']).resolve() in projects, reference['Identity'])


def verify(evaluate: bool = False, restore_graph: Path | None = None) -> None:
    manifest = json.loads(regular_file(ROOT, "docs/integration-program/apps-source-import.json").read_text())
    imported = manifest["importCommit"]
    require(git('show', '-s', '--format=%P', imported).split() == manifest['importParents'], 'Import parents changed')
    require(git('rev-parse', f'{imported}:apps') == manifest['sourceTree'], 'Initial Apps subtree changed')
    require(git('rev-parse', f"{manifest['sourceCommit']}^{{tree}}") == manifest['sourceTree'], 'Source tree mismatch')
    git("merge-base", "--is-ancestor", imported, "HEAD")
    tips = {row["sha"] for row in manifest["sourceRefs"]}
    require(int(git('rev-list', '--count', *sorted(tips))) == manifest['sourceHistoryCommits'], 'Source history changed')
    for tip in tips:
        git("merge-base", "--is-ancestor", tip, imported)

    source_assets = {}
    for line in git("ls-tree", "-r", manifest["sourceCommit"]).splitlines():
        metadata, path = line.split("\t", 1)
        mode, _, blob = metadata.split()
        source_assets[path] = (mode, blob)
    rows = manifest["assets"]
    require(len(rows) == len(source_assets) == 89, 'Incomplete asset inventory')
    require(len({row['sourcePath'] for row in rows}) == len(rows), 'Duplicate source path')
    require(len({row['destinationPath'] for row in rows}) == len(rows), 'Duplicate destination path')
    for row in rows:
        source = row["sourcePath"]
        destination = row["destinationPath"]
        require(destination == f'apps/{source}', f'Unexpected mapping: {source}')
        require(source_assets[source] == (row['sourceMode'], row['sourceBlob']), f'Source metadata changed: {source}')
        require(git("rev-parse", f"{imported}:{destination}") == row["sourceBlob"],
                f"Historical import asset changed: {destination}")

    # Current development is compared with the current Git index, not original donor blobs.
    current = tracked_apps(ROOT)
    for destination, (mode, blob) in current.items():
        file = regular_file(ROOT, destination)
        require(bool(file.stat().st_mode & 0o111) == (mode == "100755"), f"File mode changed: {destination}")
        require(git("hash-object", "--no-filters", str(file)) == blob, f"Current tracked Apps content differs: {destination}")
    projects = current_apps_projects(ROOT)
    if restore_graph:
        verify_restore_graph(restore_graph, projects)
    if evaluate:
        versions = declared_package_versions(ROOT)
        for project in projects:
            result = subprocess.check_output([
                "dotnet", "msbuild", str(project), "-nologo",
                "-getProperty:TargetFramework,UseProjectReferences,IsPackable,RestoreConfigFile,ManagePackageVersionsCentrally,DirectoryPackagesPropsPath,ElsaVersion,ElsaStudioVersion,ElsaExtensionsVersion",
                "-getItem:ProjectReference,PackageReference",
            ], cwd=ROOT, env=git_environment(), text=True)
            verify_project_evaluation(ROOT, project, json.loads(result), versions, set(projects))
    print(f"Apps import verified: 89 mapped assets, 76 original commits, 10 source refs, {len(projects)} current projects; MSBuild evaluation={evaluate}.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evaluate", action="store_true", help="also evaluate all current Apps projects with the installed .NET SDK")
    parser.add_argument("--restore-graph", type=Path, help="verify a root Apps-filter GenerateRestoreGraphFile result")
    args = parser.parse_args()
    verify(args.evaluate, args.restore_graph)
