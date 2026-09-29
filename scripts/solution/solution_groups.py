#!/usr/bin/env python3
"""Group Elsa.sln projects into Foundation, optional domains, Studio and Apps, and generate focused solution filters.

The grouping comes from solution-groups.json next to this script. Run without arguments to rewrite Elsa.sln's project
folders and the Elsa.*.slnf filters; run with --check to fail when they are out of date or a project is unclassified.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import uuid
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = Path(__file__).resolve().parent / "solution-groups.json"
SOLUTION = ROOT / "Elsa.sln"
FOLDER_TYPE = "2150E333-8FDC-42A3-9474-1A3956D46DE8"
PROJECT_LINE = re.compile(r'^Project\("\{(?P<type>[^}]+)\}"\) = "(?P<name>[^"]+)", "(?P<path>[^"]+)", "\{(?P<guid>[^}]+)\}"$')
NESTED = re.compile(r"^\s*\{(?P<child>[^}]+)\} = \{(?P<parent>[^}]+)\}\s*$")
TEST_SUFFIX = re.compile(r"\.(Tests|UnitTests|IntegrationTests|ComponentTests|ConformanceTests)$")
FOUNDATION, EXTENSIONS, STUDIO, APPS, SAMPLES = "Foundation", "Extensions", "Studio", "Apps", "Samples"
GENERATED_FOLDER_NAMESPACE = uuid.UUID("6f3c1f2e-8d9a-4b8e-9a51-1c2d3e4f5a6b")


@dataclass(frozen=True)
class SolutionProject:
    name: str
    path: str
    guid: str

    @property
    def is_test(self) -> bool:
        # Elsa.Testing.* are shared test harnesses: grouped with tests and exempt from the foundation-only rule.
        return self.path.startswith("test/") or self.name.startswith("Elsa.Testing.") or bool(TEST_SUFFIX.search(self.name))


def read_solution(text: str) -> tuple[list[SolutionProject], dict[str, str], dict[str, str]]:
    """Return the csproj projects, the solution folders (guid -> name) and the nesting (child -> parent)."""
    projects: list[SolutionProject] = []
    folders: dict[str, str] = {}
    for line in text.splitlines():
        match = PROJECT_LINE.match(line)
        if not match:
            continue
        if match["type"].upper() == FOLDER_TYPE:
            folders[match["guid"].upper()] = match["name"]
        elif match["path"].endswith(".csproj"):
            projects.append(SolutionProject(match["name"], match["path"].replace("\\", "/"), match["guid"].upper()))
    nested_section = re.search(r"GlobalSection\(NestedProjects\) = preSolution\r?\n(.*?)\r?\n\s*EndGlobalSection", text, re.S)
    nesting = {}
    if nested_section:
        for line in nested_section.group(1).splitlines():
            match = NESTED.match(line)
            if match:
                nesting[match["child"].upper()] = match["parent"].upper()
    return projects, folders, nesting


def source_group(project: SolutionProject, manifest: dict) -> str | None:
    if project.name in manifest["foundation"]:
        return FOUNDATION
    for domain, rules in manifest["domains"].items():
        if (project.name in rules.get("names", [])
                or any(project.name.startswith(prefix) for prefix in rules.get("namePrefixes", []))
                or any(project.path.startswith(prefix) for prefix in rules.get("paths", []))):
            return domain
    for group in (STUDIO, APPS, SAMPLES):
        if any(project.path.startswith(prefix) for prefix in manifest[group.lower()]["paths"]):
            return group
    return None


def project_references(project: SolutionProject, by_path: dict[str, SolutionProject]) -> list[SolutionProject]:
    file = ROOT / project.path
    if not file.is_file():
        return []
    references = []
    for include in re.findall(r'<ProjectReference\s+Include="([^"$]+)"', file.read_text(encoding="utf-8-sig")):
        target = os.path.normpath(os.path.join(os.path.dirname(project.path), include.replace("\\", "/")))
        if target in by_path:
            references.append(by_path[target])
    return references


def classify(projects: list[SolutionProject], manifest: dict) -> dict[SolutionProject, str]:
    """Map every project to Foundation, a domain, Studio or Apps; tests follow the project they cover."""
    by_path = {project.path: project for project in projects}
    by_name = {project.name: project for project in projects}
    groups: dict[SolutionProject, str] = {}
    unclassified = []
    for project in projects:
        if project.is_test:
            continue
        group = source_group(project, manifest)
        if group is None:
            unclassified.append(project.path)
        else:
            groups[project] = group
    if unclassified:
        raise ValueError("Projects missing from scripts/solution/solution-groups.json: " + ", ".join(sorted(unclassified)))
    for project in projects:
        if not project.is_test:
            continue
        if project.name in manifest["foundation"]:
            groups[project] = FOUNDATION
            continue
        covered = by_name.get(TEST_SUFFIX.sub("", project.name))
        if covered in groups:
            groups[project] = groups[covered]
            continue
        referenced = {groups[reference] for reference in project_references(project, by_path) if reference in groups}
        optional = referenced - {FOUNDATION}
        groups[project] = optional.pop() if len(optional) == 1 else (FOUNDATION if not optional else EXTENSIONS)

    # The foundation must build on its own: its source projects may reference only foundation projects.
    leaks = sorted(f"{project.name} -> {reference.name} ({groups[reference]})"
                   for project, group in groups.items() if group == FOUNDATION and not project.is_test
                   for reference in project_references(project, by_path) if groups.get(reference) != FOUNDATION)
    if leaks:
        raise ValueError("Foundation projects reference optional projects: " + "; ".join(leaks))
    return groups


def folder_path(group: str, is_test: bool, domains: list[str]) -> tuple[str, ...]:
    base = (EXTENSIONS, group) if group in domains else (group,)
    return base + ("Tests",) if is_test else base


def folder_guid(path: tuple[str, ...]) -> str:
    return str(uuid.uuid5(GENERATED_FOLDER_NAMESPACE, "/".join(path))).upper()


def rewrite_solution(text: str, groups: dict[SolutionProject, str], domains: list[str]) -> str:
    newline = "\r\n" if "\r\n" in text else "\n"
    projects, folders, nesting = read_solution(text)
    generated = {folder_guid(path): path for project, group in groups.items()
                 for path in _prefixes(folder_path(group, project.is_test, domains))}
    solution_items = _folders_with_items(text)

    # Keep an existing folder only when it still holds solution items, directly or through a kept descendant folder.
    kept = set()
    for guid in folders:
        if guid in generated:
            continue
        current = guid
        if current in solution_items:
            while current is not None and current in folders:
                kept.add(current)
                current = nesting.get(current)

    lines = text.split(newline)
    output = []
    skip = False
    for line in lines:
        match = PROJECT_LINE.match(line)
        if match and match["type"].upper() == FOLDER_TYPE and match["guid"].upper() not in kept:
            skip = True
            continue
        if skip:
            if line == "EndProject":
                skip = False
            continue
        output.append(line)

    # Declare the generated folders before Global, in path order.
    global_index = output.index("Global")
    declarations = []
    for guid, path in sorted(generated.items(), key=lambda item: item[1]):
        declarations += [f'Project("{{{FOLDER_TYPE}}}") = "{path[-1]}", "{path[-1]}", "{{{guid}}}"', "EndProject"]
    output[global_index:global_index] = declarations

    new_nesting = {}
    for guid in kept:
        parent = nesting.get(guid)
        if parent in kept:
            new_nesting[guid] = parent
    for guid, path in generated.items():
        if len(path) > 1:
            new_nesting[guid] = folder_guid(path[:-1])
    for project, group in groups.items():
        new_nesting[project.guid] = folder_guid(folder_path(group, project.is_test, domains))
    body = [f"\t\t{{{child}}} = {{{parent}}}" for child, parent in sorted(new_nesting.items(), key=_nesting_order(output))]

    text = newline.join(output)
    section = re.search(r"(\tGlobalSection\(NestedProjects\) = preSolution\r?\n)(.*?)(\r?\n\tEndGlobalSection)", text, re.S)
    if section:
        return text[:section.start(2)] + newline.join(body) + text[section.end(2):]
    anchor = text.index("\tGlobalSection(ExtensibilityGlobals)") if "\tGlobalSection(ExtensibilityGlobals)" in text \
        else text.rindex("EndGlobal")
    block = newline.join(["\tGlobalSection(NestedProjects) = preSolution", *body, "\tEndGlobalSection"]) + newline
    return text[:anchor] + block + text[anchor:]


def _prefixes(path: tuple[str, ...]) -> list[tuple[str, ...]]:
    return [path[:index] for index in range(1, len(path) + 1)]


def _folders_with_items(text: str) -> set[str]:
    guids = set()
    for match in re.finditer(r'Project\("\{' + FOLDER_TYPE + r'\}"\) = "[^"]+", "[^"]+", "\{([^}]+)\}"\r?\n(.*?)EndProject',
                             text, re.S | re.I):
        if "ProjectSection(SolutionItems)" in match.group(2):
            guids.add(match.group(1).upper())
    return guids


def _nesting_order(lines: list[str]):
    order = {}
    for index, line in enumerate(lines):
        match = PROJECT_LINE.match(line)
        if match:
            order[match["guid"].upper()] = index
    return lambda item: order.get(item[0], len(lines))


def filters(groups: dict[SolutionProject, str], projects: list[SolutionProject], domains: list[str]) -> dict[str, list[str]]:
    by_path = {project.path: project for project in projects}
    by_name = {project.name: project for project in projects}

    def closure(selected: set[SolutionProject]) -> list[str]:
        pending, seen = list(selected), set(selected)
        while pending:
            for reference in project_references(pending.pop(), by_path):
                if reference not in seen:
                    seen.add(reference)
                    pending.append(reference)
        return sorted(project.path for project in seen)

    def members(*names: str) -> set[SolutionProject]:
        return {project for project, group in groups.items() if group in names}

    result = {"Elsa.Foundation.slnf": closure(members(FOUNDATION))}
    for domain in domains:
        result[f"Elsa.{domain}.slnf"] = closure(members(FOUNDATION, domain))
    studio = members(FOUNDATION, STUDIO)
    if "Elsa.Api.Client" in by_name:
        studio.add(by_name["Elsa.Api.Client"])
    result["Elsa.Studio.slnf"] = closure(studio)
    result["Elsa.Extensions.slnf"] = closure(members(*domains, EXTENSIONS))
    return result


def render_filter(projects: list[str]) -> str:
    return json.dumps({"solution": {"path": "Elsa.sln", "projects": projects}}, indent=2) + "\n"


def generate(root: Path = ROOT) -> dict[Path, str]:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    domains = list(manifest["domains"])
    text = (root / SOLUTION.name).read_text(encoding="utf-8-sig")
    projects, _, _ = read_solution(text)
    missing = sorted(project.path for project in projects if not (root / project.path).is_file())
    if missing:
        raise ValueError("Elsa.sln lists project files that do not exist: " + ", ".join(missing))
    groups = classify(projects, manifest)
    outputs = {root / SOLUTION.name: rewrite_solution(text, groups, domains)}
    for name, selected in filters(groups, projects, domains).items():
        outputs[root / name] = render_filter(selected)
    return outputs


def stale(outputs: dict[Path, str]) -> list[Path]:
    """The generated files whose committed content differs from what the manifest produces."""
    return [path for path, content in outputs.items()
            if not path.is_file() or path.read_text(encoding="utf-8-sig") != content]


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="fail if Elsa.sln or a generated filter is out of date")
    args = parser.parse_args(argv)
    try:
        outputs = generate()
    except ValueError as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1
    out_of_date = stale(outputs)
    names = ", ".join(path.name for path in out_of_date)
    if args.check:
        if out_of_date:
            print(f"Out of date: {names}. Run python3 scripts/solution/solution_groups.py.", file=sys.stderr)
            return 1
        print(f"Solution grouping and {len(outputs) - 1} filters are up to date.")
        return 0
    for path in out_of_date:
        bom = path.name == SOLUTION.name and path.is_file() and path.read_bytes().startswith(b"\xef\xbb\xbf")
        path.write_bytes((b"\xef\xbb\xbf" if bom else b"") + outputs[path].encode("utf-8"))
    print("Updated: " + (names or "nothing"))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
