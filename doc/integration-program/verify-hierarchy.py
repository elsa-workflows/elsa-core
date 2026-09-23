#!/usr/bin/env python3
"""Validate the recorded program graph; optionally read GitHub to check drift."""
import argparse
import collections
import json
from pathlib import Path
import subprocess


def api(path):
    return json.loads(subprocess.check_output(['gh', 'api', path], text=True))


def require(condition, message):
    if not condition:
        raise ValueError(message)


def acyclic(edges):
    graph = collections.defaultdict(list)
    for source, target in edges:
        graph[source].append(target)
    active, visited = set(), set()

    def visit(node):
        if node in active:
            raise ValueError(f'Cycle at #{node}')
        if node in visited:
            return
        active.add(node)
        for child in graph[node]:
            visit(child)
        active.remove(node)
        visited.add(node)

    for node in list(graph):
        visit(node)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--live', action='store_true', help='Read GitHub with authenticated gh; never mutates issues')
    args = parser.parse_args()
    data = json.loads(Path(__file__).with_name('hierarchy.json').read_text())
    issues = {row['number']: row for row in data['issues']}
    require(len(issues) == len(data['issues']) == 69, "Expected 69 unique issues")
    require(collections.Counter(row['level'] for row in issues.values()) == {'Program': 1, 'Epic': 10, 'Feature': 40, 'Story': 6, 'Task': 12}, 'Unexpected semantic level counts')
    levels = ['Program', 'Epic', 'Feature', 'Story', 'Task']
    parents = data['parent_edges']
    dependencies = data['blocking_edges']
    require(len(parents) == 68, 'Expected 68 parent edges')
    require(len({child for parent, child in parents}) == 68, 'Each child must have one parent')
    require({child for parent, child in parents} == set(issues) - {8194}, 'Parent coverage differs from the issue index')
    for parent, child in parents:
        require(levels.index(issues[child]['level']) == levels.index(issues[parent]['level']) + 1, f'Invalid hierarchy level: {parent} -> {child}')
    for edges in (parents, dependencies):
        require(len(edges) == len(set(map(tuple, edges))), 'Duplicate edge')
        require(all(a in issues and b in issues and a != b for a, b in edges), 'Unknown issue or self edge')
        acyclic(edges)
    if args.live:
        base = 'repos/elsa-workflows/elsa-core/issues/'
        for parent, child in parents:
            require(api(f'{base}{child}/parent')['number'] == parent, f'Native parent drift for #{child}')
        for blocker, blocked in dependencies:
            rows = api(f'{base}{blocked}/dependencies/blocked_by?per_page=100')
            require(blocker in {row['number'] for row in rows}, f'Native dependency missing: {blocker} -> {blocked}')
    print(f'PASS: {len(issues)} issues, {len(parents)} parent edges, {len(dependencies)} separate blocking edges; both graphs acyclic' + ('; live relationships verified' if args.live else '; snapshot only'))


if __name__ == '__main__':
    main()
