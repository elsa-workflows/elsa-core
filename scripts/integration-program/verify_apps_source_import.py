#!/usr/bin/env python3
"""Verify the original Apps history/tree, mapped assets and package-consumer boundary."""

import argparse
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def require(condition: bool, message: object) -> None:
    if not condition:
        raise ValueError(str(message))


def git(*args: str) -> str:
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True).strip()


def verify(evaluate: bool = False) -> None:
    manifest = json.loads((ROOT / "docs/integration-program/apps-source-import.json").read_text())
    imported = manifest["importCommit"]
    require(git('show', '-s', '--format=%P', imported).split() == manifest['importParents'], 'Import parents changed')
    require(git('rev-parse', f'{imported}:apps') == manifest['sourceTree'], 'Initial Apps subtree changed')
    require(git('rev-parse', f"{manifest['sourceCommit']}^{{tree}}") == manifest['sourceTree'], 'Source tree mismatch')
    subprocess.run(["git", "merge-base", "--is-ancestor", imported, "HEAD"], cwd=ROOT, check=True)
    tips = {row["sha"] for row in manifest["sourceRefs"]}
    require(int(git('rev-list', '--count', *sorted(tips))) == manifest['sourceHistoryCommits'], 'Source history changed')
    for tip in tips:
        subprocess.run(["git", "merge-base", "--is-ancestor", tip, imported], cwd=ROOT, check=True)

    source_assets = {}
    for line in git("ls-tree", "-r", manifest["sourceCommit"]).splitlines():
        metadata, path = line.split("\t", 1)
        mode, _, blob = metadata.split()
        source_assets[path] = (mode, blob)
    rows = manifest["assets"]
    require(len(rows) == len(source_assets) == 89, 'Incomplete asset inventory')
    require(len({row['sourcePath'] for row in rows}) == len(rows), 'Duplicate source path')
    require(len({row['destinationPath'] for row in rows}) == len(rows), 'Duplicate destination path')
    adapted = manifest["adaptedSourceFiles"]
    for row in rows:
        source = row["sourcePath"]
        destination = row["destinationPath"]
        require(destination == f'apps/{source}', f'Unexpected mapping: {source}')
        require(source_assets[source] == (row['sourceMode'], row['sourceBlob']), f'Source metadata changed: {source}')
        file = ROOT / destination
        for component in (file, *file.parents):
            if component == ROOT:
                break
            require(not component.is_symlink(), f"Mapped asset traverses a symlink: {destination}")
        require(file.resolve().is_relative_to(ROOT), f"Mapped asset escapes checkout: {destination}")
        require(file.is_file(), f'Missing mapped asset: {destination}')
        expected_executable = row["sourceMode"] == "100755"
        require(bool(file.stat().st_mode & 0o111) == expected_executable, f'File mode changed: {destination}')
        if destination not in adapted:
            require(git('hash-object', str(file)) == row['sourceBlob'], f'Unreviewed source delta: {destination}')

    projects = sorted((ROOT / "apps/src").glob("*/*.csproj"))
    selected = json.loads((ROOT / "Elsa.Apps.slnf").read_text())["solution"]["projects"]
    require(len(projects) == 9, 'Apps project count changed')
    require(sorted(selected) == sorted((str(project.relative_to(ROOT)) for project in projects)), 'Apps filter boundary changed')
    if evaluate:
        for project in projects:
            result = subprocess.check_output([
                "dotnet", "msbuild", str(project), "-nologo",
                "-getProperty:TargetFramework,UseProjectReferences,IsPackable,ManagePackageVersionsCentrally,DirectoryPackagesPropsPath,ElsaVersion,ElsaStudioVersion,ElsaExtensionsVersion",
                "-getItem:ProjectReference,PackageReference",
            ], cwd=ROOT, text=True)
            evaluation = json.loads(result)
            properties = evaluation["Properties"]
            require(properties['TargetFramework'] == 'net10.0', project)
            require(properties['UseProjectReferences'].lower() == 'false', project)
            require(properties['IsPackable'].lower() == 'false', project)
            require(properties['ManagePackageVersionsCentrally'].lower() == 'true', project)
            require(Path(properties['DirectoryPackagesPropsPath']).resolve() == ROOT / 'apps/Directory.Packages.props', project)
            require(all((properties[name] == '3.9.0' for name in ('ElsaVersion', 'ElsaStudioVersion', 'ElsaExtensionsVersion'))), project)
            for reference in evaluation["Items"]["ProjectReference"]:
                require(Path(reference['FullPath']).resolve().is_relative_to(ROOT / 'apps'), reference['Identity'])
            require(evaluation['Items']['PackageReference'], f'Missing package consumers: {project}')
    print(f"Apps import verified: 89 mapped assets, 76 original commits, 10 source refs, 9 projects; MSBuild evaluation={evaluate}.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evaluate", action="store_true", help="also evaluate all nine projects with the installed .NET SDK")
    verify(parser.parse_args().evaluate)
