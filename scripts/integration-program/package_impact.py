#!/usr/bin/env python3
"""Select tests affected by a source-project change using the recorded inventory."""

from __future__ import annotations

import argparse
import json
from collections import defaultdict, deque
from pathlib import Path
from typing import Iterable

ProjectKey = tuple[str, str]


class InventoryGraph:
    def __init__(self, inventory: dict) -> None:
        self.projects: dict[ProjectKey, dict] = {
            (repository, project["path"]): project
            for repository, projects in inventory["project_inventory"].items()
            for project in projects
        }
        package_projects: dict[str, list[ProjectKey]] = defaultdict(list)
        for key, project in self.projects.items():
            package_id = project.get("package_id")
            if package_id:
                package_projects[package_id].append(key)

        repository_keys = {
            key: repository
            for key, repository in inventory["repositories"].items()
        }
        repositories_by_slug = {
            repository.get("slug", key): key
            for key, repository in repository_keys.items()
        }
        self.reverse_edges: dict[ProjectKey, set[ProjectKey]] = defaultdict(set)
        self.ambiguous_package_edges: set[tuple[str, ProjectKey]] = set()

        def add_edge(dependency: ProjectKey, consumer: ProjectKey) -> None:
            if dependency in self.projects:
                self.reverse_edges[dependency].add(consumer)

        for key, project in self.projects.items():
            repository, _ = key
            for reference in project.get("project_references", []):
                if reference.get("target_project"):
                    add_edge((repository, reference["target_project"]), key)
                    continue

                external_repository = reference.get("external_repository")
                external_project = reference.get("external_project")
                if external_repository and external_project:
                    slug = external_repository.rsplit("/", 1)[-1]
                    dependency_repository = repositories_by_slug.get(slug)
                    if dependency_repository:
                        add_edge((dependency_repository, external_project), key)

            for reference in project.get("package_references", []):
                candidates = package_projects.get(reference.get("id"), [])
                if len(candidates) == 1:
                    add_edge(candidates[0], key)
                elif len(candidates) > 1:
                    self.ambiguous_package_edges.add((reference["id"], key))
                    for candidate in candidates:
                        add_edge(candidate, key)

    @classmethod
    def from_path(cls, path: Path) -> InventoryGraph:
        return cls(json.loads(path.read_text(encoding="utf-8")))

    def affected_projects(self, changed_projects: Iterable[ProjectKey]) -> set[ProjectKey]:
        reached = set(changed_projects)
        unknown = reached - self.projects.keys()
        if unknown:
            raise ValueError(f"Unknown project(s): {sorted(unknown)}")

        pending = deque(reached)
        while pending:
            dependency = pending.popleft()
            for consumer in self.reverse_edges.get(dependency, set()):
                if consumer not in reached:
                    reached.add(consumer)
                    pending.append(consumer)
        return reached

    def affected_tests(self, changed_projects: Iterable[ProjectKey]) -> set[ProjectKey]:
        return {
            key
            for key in self.affected_projects(changed_projects)
            if self.projects[key].get("is_test_project")
        }

    def package_ids(self, selected_projects: Iterable[ProjectKey]) -> set[str]:
        selected = set(selected_projects)
        unknown = selected - self.projects.keys()
        if unknown:
            raise ValueError(f"Unknown selected project(s): {sorted(unknown)}")

        return {
            self.projects[key]["package_id"]
            for key in selected
            if self.projects[key].get("is_packable") and self.projects[key].get("package_id")
        }


def parse_project_key(value: str) -> ProjectKey:
    repository, separator, path = value.partition(":")
    if not separator or not repository or not path:
        raise argparse.ArgumentTypeError("project must be REPOSITORY:path/to/project.csproj")
    return repository, path


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--inventory", type=Path, required=True)
    parser.add_argument("--changed", type=parse_project_key, action="append", required=True)
    parser.add_argument("--release-unit", type=parse_project_key, action="append", required=True)
    args = parser.parse_args()

    graph = InventoryGraph.from_path(args.inventory)
    tests = sorted(graph.affected_tests(args.changed))
    package_ids = sorted(graph.package_ids(args.release_unit))
    output = {
        "changed_projects": [f"{repository}:{path}" for repository, path in args.changed],
        "affected_test_projects": [f"{repository}:{path}" for repository, path in tests],
        "affected_test_project_count": len(tests),
        "release_unit_projects": [f"{repository}:{path}" for repository, path in args.release_unit],
        "package_ids_to_pack": package_ids,
        "ambiguous_package_edges_resolved_conservatively": len(graph.ambiguous_package_edges),
    }
    print(json.dumps(output, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
