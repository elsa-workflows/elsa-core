#!/usr/bin/env python3
"""Validate the recorded program graph; optionally read GitHub to check drift."""
import argparse
import collections
import json
from pathlib import Path
import subprocess


def api_pages(path):
    pages = json.loads(subprocess.check_output(['gh', 'api', '--paginate', '--slurp', path], text=True))
    return [row for page in pages for row in page]


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


def validate(data):
    rows = data['issues']
    issues = {row['number']: row for row in rows}
    require(len(issues) == len(rows), 'Duplicate issue number')
    program = data['program']
    require(program in issues, 'Program missing from issue index')
    require([row['number'] for row in rows if row['level'] == 'Program'] == [program],
            'Expected exactly the declared program root')
    levels = ['Program', 'Epic', 'Feature', 'Story', 'Task']
    parents = data['parent_edges']
    dependencies = data['blocking_edges']
    require(all(row['level'] in levels for row in rows), 'Unknown semantic level')
    require(len(parents) == len(issues) - 1, 'Expected one parent edge per non-root issue')
    require(len({child for parent, child in parents}) == len(parents), 'Each child must have one parent')
    require({child for parent, child in parents} == set(issues) - {program}, 'Parent coverage differs from the issue index')
    for edges in (parents, dependencies):
        require(len(edges) == len(set(map(tuple, edges))), 'Duplicate edge')
        require(all(a in issues and b in issues and a != b for a, b in edges), 'Unknown issue or self edge')
        acyclic(edges)
    for parent, child in parents:
        require(levels.index(issues[child]['level']) == levels.index(issues[parent]['level']) + 1,
                f'Invalid hierarchy level: {parent} -> {child}')
    return issues


def verify_live(data, fetch=api_pages):
    issues = validate(data)
    base = f"repos/{data['repository']}/issues/"
    children, blockers = collections.defaultdict(set), collections.defaultdict(set)
    for parent, child in data['parent_edges']:
        children[parent].add(issues[child]['url'])
    for blocker, blocked in data['blocking_edges']:
        blockers[blocked].add(issues[blocker]['url'])
    # Read every indexed issue, including leaves: new children or unrecorded
    # blockers are drift too. Compare full URLs so cross-repository issue numbers
    # cannot accidentally match. Pagination covers future program growth.
    for number in issues:
        for endpoint, expected in [('sub_issues', children[number]),
                                   ('dependencies/blocked_by', blockers[number])]:
            actual = {row['html_url'] for row in fetch(f'{base}{number}/{endpoint}?per_page=100')}
            require(actual == expected,
                    f'Native {endpoint} drift for #{number}: missing={sorted(expected - actual)}, '
                    f'unrecorded={sorted(actual - expected)}')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--live', action='store_true', help='Read GitHub with authenticated gh; never mutates issues')
    parser.add_argument('--snapshot', type=Path, default=Path(__file__).with_name('hierarchy.json'))
    args = parser.parse_args()
    data = json.loads(args.snapshot.read_text())
    issues = validate(data)
    parents, dependencies = data['parent_edges'], data['blocking_edges']
    if args.live:
        verify_live(data)
    print(f'PASS: {len(issues)} issues, {len(parents)} parent edges, {len(dependencies)} separate blocking edges; both graphs acyclic' + ('; live relationships verified' if args.live else '; snapshot only'))


if __name__ == '__main__':
    main()
