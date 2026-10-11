#!/usr/bin/env python3
"""Verify Task #8748 source provenance and the independent Templates product boundary.

Keep this acceptance guard while the imported product/history is maintained; operational
publisher/release authority remains #8741/#8725, outside this source gate.
"""

import argparse
import base64
import json
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

from apps_consumer_inventory import git, git_environment, regular_file, require, tracked_product
from templates_product_inventory import current_templates_projects

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = "docs/integration-program/templates-source-import.json"


def verify_restore_graph(root: Path, path: Path, projects: list[Path]) -> None:
    restored = json.loads(path.read_text())["projects"]
    require({Path(project).resolve() for project in restored} == set(projects),
            "Templates restore graph must contain exactly the registered build projects")
    for project, document in restored.items():
        configs = [Path(config).resolve() for config in document["restore"]["configFilePaths"]]
        require(configs == [root / "templates/NuGet.Config"], f"Restore must use only Templates NuGet.Config: {project}")


def verify_project_evaluation(root: Path, project: Path, evaluation: dict) -> None:
    properties, items = evaluation["Properties"], evaluation["Items"]
    require(properties["TargetFramework"] == "net10.0", project)
    require(properties["UseProjectReferences"].lower() == "false", project)
    require(properties["ManagePackageVersionsCentrally"].lower() == "true", project)
    for name, relative in (("RestoreConfigFile", "NuGet.Config"), ("DirectoryPackagesPropsPath", "Directory.Packages.props"),
                           ("DirectoryBuildPropsPath", "Directory.Build.props"), ("DirectoryBuildTargetsPath", "Directory.Build.targets")):
        require(Path(properties[name]).resolve() == root / "templates" / relative, f"Inherited root policy: {project} {name}")
    require(not items["ProjectReference"], f"Templates currently has no source references: {project}")
    if project.name == "Elsa.Templates.csproj":
        require(properties["IsPackable"].lower() == "true" and properties["PackageId"] == "Elsa.Templates", project)
        declared = ET.parse(project).findtext(".//Version")
        require(properties["PackageVersion"] == declared, "Templates version must remain independently declared")
        require(properties["RepositoryUrl"] == "https://github.com/elsa-workflows/elsa-core", "Core package provenance required")
        require(Path(properties["PackageOutputPath"]).resolve() == root / "templates/artifacts/package/release", project)
    else:
        require(properties["IsPackable"].lower() == "false", project)
        require({item["Identity"] for item in items["PackageReference"]}
                == {"Microsoft.NET.Test.Sdk", "xunit", "xunit.runner.visualstudio"}, project)


def verify(evaluate: bool = False, restore_graph: Path | None = None) -> None:
    manifest = json.loads(regular_file(ROOT, MANIFEST).read_text())
    imported, source = manifest["importCommit"], manifest["sourceCommit"]
    require(git(ROOT, "show", "-s", "--format=%P", imported).split() == manifest["importParents"], "Import parents changed")
    require(manifest["importParents"][:2] == [manifest["coreBase"], source], "Original source must follow Core base in import ancestry")
    require(git(ROOT, "rev-parse", imported + ":templates") == manifest["sourceTree"]
            == git(ROOT, "rev-parse", source + "^{tree}"), "Original Templates subtree changed")
    # Only the mapped Templates subtree changes in the immutable original import.
    changed = git(ROOT, "diff", "--name-only", manifest["coreBase"], imported).splitlines()
    require(all(path.startswith("templates/") for path in changed), "Original import includes unrelated changes")
    git(ROOT, "merge-base", "--is-ancestor", imported, "HEAD")
    tips = set()
    for row in manifest["sourceRefs"]:
        commit = row["commit"]
        git(ROOT, "merge-base", "--is-ancestor", commit, imported)
        tips.add(commit)
        if row["type"] == "tag":
            raw = base64.b64decode(row["rawTagObjectBase64"], validate=True)
            digest = subprocess.check_output(["git", "hash-object", "--no-filters", "-t", "tag", "--stdin"],
                                             input=raw, env=git_environment(), cwd=ROOT).decode().strip()
            require(digest == row["sha"] and raw.startswith(f"object {commit}\n".encode()), "Original annotated tag object changed")
        else:
            require(row["type"] == "commit" and row["sha"] == commit, "Source ref identity changed")
    require(len(manifest["sourceRefs"]) == len({row["ref"] for row in manifest["sourceRefs"]}) == 17, "Incomplete or duplicate original source refs")
    require(int(git(ROOT, "rev-list", "--count", *sorted(tips))) == manifest["sourceHistoryCommits"] == 41, "Source history changed")
    source_assets = {}
    for line in git(ROOT, "ls-tree", "-r", source).splitlines():
        metadata, path = line.split("\t", 1)
        mode, kind, blob = metadata.split()
        require(kind == "blob" and mode in {"100644", "100755"}, path)
        source_assets[path] = (mode, blob)
    rows = manifest["assets"]
    require(len(rows) == len(source_assets) == 65, "Incomplete original asset inventory")
    require(len({row["sourcePath"] for row in rows}) == len(rows), "Duplicate original source path")
    for row in rows:
        path = row["sourcePath"]
        require(row["destinationPath"] == "templates/" + path, f"Unexpected mapping: {path}")
        require(source_assets[path] == (row["sourceMode"], row["sourceBlob"]), f"Source metadata changed: {path}")
    contribution = manifest["appliedContribution"]
    git(ROOT, "merge-base", "--is-ancestor", contribution["originalCommit"], imported)
    git(ROOT, "merge-base", "--is-ancestor", imported, contribution["appliedCommit"])
    git(ROOT, "merge-base", "--is-ancestor", contribution["appliedCommit"], "HEAD")
    require(git(ROOT, "show", "-s", "--format=%an <%ae>", contribution["appliedCommit"]) == contribution["author"], "Contribution author changed")
    for relative, (mode, blob) in tracked_product(ROOT, "templates").items():
        file = regular_file(ROOT, relative)
        require(git(ROOT, "hash-object", "--no-filters", str(file)) == blob, f"Current Templates bytes differ from Git: {relative}")
        actual_mode = "100755" if file.stat().st_mode & 0o111 else "100644"
        require(actual_mode == mode, f"Current Templates mode differs from Git: {relative}")
    projects = current_templates_projects(ROOT)
    if restore_graph:
        verify_restore_graph(ROOT, restore_graph, projects)
    if evaluate:
        properties = "TargetFramework,UseProjectReferences,IsPackable,ManagePackageVersionsCentrally,RestoreConfigFile,DirectoryPackagesPropsPath,DirectoryBuildPropsPath,DirectoryBuildTargetsPath,PackageId,PackageVersion,PackageOutputPath,RepositoryUrl"
        for project in projects:
            output = subprocess.check_output(["dotnet", "msbuild", str(project), "-nologo", "-p:Configuration=Release",
                                              "-getProperty:" + properties, "-getItem:PackageReference,ProjectReference"],
                                             cwd=ROOT, env=git_environment(), text=True)
            verify_project_evaluation(ROOT, project, json.loads(output))
    print(f"Templates import verified: 65 original assets, 41 original commits, 17 refs, {len(projects)} build projects; MSBuild evaluation={evaluate}.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evaluate", action="store_true")
    parser.add_argument("--restore-graph", type=Path)
    arguments = parser.parse_args()
    verify(arguments.evaluate, arguments.restore_graph)
