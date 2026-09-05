#!/usr/bin/env python3
"""Evaluate solution projects to declare expected packages before downloading CI output."""
from __future__ import annotations

import argparse
from concurrent.futures import ThreadPoolExecutor
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

from release_train import command, config, entry, read, save
from release_support import parse_version


def evaluate_project(path, version, fixed_packages):
    properties = json.loads(command(['dotnet', 'msbuild', str(path), '-nologo', f'-p:Version={version}', '-p:Configuration=Release', '-getProperty:IsPackable,PackageId,PackageVersion']))['Properties']
    if properties['IsPackable'].lower() != 'true':
        return None
    package = {'id': properties['PackageId'], 'version': properties['PackageVersion']}
    if not package['id']:
        raise ValueError(f'Empty evaluated PackageId: {path}')
    fixed = fixed_packages.get(package['id'])
    if fixed:
        # An exception must still be anchored to an explicit source declaration.
        versions = [n.text for n in ET.parse(path).getroot().iter() if n.tag.split('}')[-1] in ('Version','PackageVersion')]
        if fixed['version'] not in versions:
            raise ValueError(f'Fixed version changed in {path}; review the profile exception')
        package.update(fixed)
    elif package['version'] != version:
        raise ValueError(f"Unexpected evaluated package version {package} in {path}")
    return package


def generate(state, name, path):
    cfg = config(state, name)
    item = entry(state, name)
    version = parse_version(state['version'], state['kind'])
    sha = command(['git', 'rev-parse', 'HEAD'], path)
    listing = command(['dotnet', 'sln', cfg['solution'], 'list'], path)
    projects = [path / line.strip().replace('\\','/') for line in listing.splitlines() if line.strip().endswith('.csproj')]
    if not projects:
        raise ValueError('No solution projects found')
    with ThreadPoolExecutor(max_workers=4) as pool:
        packages = list(pool.map(lambda p: evaluate_project(p, version.version, cfg['fixed_packages']), projects))
    packages = sorted((p for p in packages if p), key=lambda p: p['id'])
    if not packages or len({p['id'].lower() for p in packages}) != len(packages):
        raise ValueError('Empty or duplicate expected package IDs')
    if set(cfg['fixed_packages']) - {p['id'] for p in packages}:
        raise ValueError('A configured fixed-version package disappeared; review the package inventory')
    policy = state['profile']['release_kinds'][version.kind]
    dependencies = {}
    for upstream in cfg['dependencies']:
        binding = entry(state, upstream).get('binding')
        if not binding:
            raise ValueError(f'Bind upstream {upstream} before evaluating this manifest')
        dependencies.update({p['id']:p.get('version',state['version']) for p in read(binding['manifest'])['nuget']})
    return {
        'version': version.version, 'source_commit': sha,
        'nuget': packages, 'expected_dependencies': dependencies,
        'feeds': [f for f in state['profile']['feeds'] if f['name'] in policy['feeds']],
        'npm': [{'name': n, 'version': version.version, 'dist_tag': policy['npm_dist_tag'], 'registry': 'https://registry.npmjs.org'} for n in cfg['npm']],
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--state', required=True, type=Path)
    parser.add_argument('--repo', required=True)
    parser.add_argument('--repo-path', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    try:
        result = generate(read(args.state), args.repo, args.repo_path.resolve())
        if args.output.exists() and read(args.output) != result:
            raise ValueError('Existing manifest differs; use a new file and review the source/package changes')
        save(args.output, result)
        print(json.dumps({'manifest':str(args.output), 'nuget_packages':len(result['nuget']), 'npm_packages':len(result['npm'])}))
        return 0
    except (ValueError, OSError, KeyError) as e:
        print(f'error: {e}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
