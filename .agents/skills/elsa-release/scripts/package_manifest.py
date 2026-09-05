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


def _local_name(tag):
    return tag.rsplit('}', 1)[-1]


def embedded_content(path, cfg, version, upstream_manifests):
    """Derive archive-content expectations from the template source itself."""

    rules = cfg.get('content_expectations')
    if not rules:
        return None
    root = path / rules.get('source_root', '.')
    files = []
    seen = set()
    for pattern in rules.get('source_globs', []):
        for source in sorted(root.glob(pattern)):
            if not source.is_file() or source in seen:
                continue
            seen.add(source)
            references = []
            try:
                document = ET.parse(source)
            except ET.ParseError as exc:
                raise ValueError(f'Embedded template project is invalid XML: {source}: {exc}') from exc
            for node in document.getroot().iter():
                if _local_name(node.tag) != 'PackageReference':
                    continue
                package_id = (node.attrib.get('Include') or '').strip()
                if not package_id or not any(package_id.startswith(prefix) for prefix in rules.get('package_prefixes', [])):
                    continue
                package_version = (node.attrib.get('Version') or '').strip()
                if not package_version:
                    raise ValueError(f'Embedded Elsa PackageReference has no version: {source}: {package_id}')
                if package_version != version:
                    raise ValueError(
                        f'Embedded package {package_id} in {source} is {package_version}, expected {version}'
                    )
                references.append({'id': package_id, 'version': package_version})
            source_path = source.relative_to(root).as_posix()
            archive_source_prefix = rules.get('archive_source_prefix', '')
            archive_path = source_path
            if archive_source_prefix and archive_path.startswith(archive_source_prefix):
                archive_path = archive_path[len(archive_source_prefix):]
            files.append({
                'path': archive_path,
                'source_path': source_path,
                'references': references,
            })
    if not files:
        raise ValueError('Content expectations matched no embedded template projects')

    template_configs = []
    for pattern in rules.get('template_config_globs', []):
        for source in sorted(root.glob(pattern)):
            if source.is_file():
                source_path = source.relative_to(root).as_posix()
                archive_source_prefix = rules.get('archive_source_prefix', '')
                archive_path = source_path
                if archive_source_prefix and archive_path.startswith(archive_source_prefix):
                    archive_path = archive_path[len(archive_source_prefix):]
                template_configs.append(archive_path)
    if rules.get('template_config_globs') and not template_configs:
        raise ValueError('Content expectations matched no template.json files')

    known_ids = set()
    for upstream in rules.get('upstream_repositories', []):
        manifest = upstream_manifests.get(upstream)
        if not manifest:
            raise ValueError(f'Bind upstream {upstream} before evaluating embedded template references')
        known_ids.update(item['id'] for item in manifest.get('nuget', []))
    embedded_ids = {ref['id'] for file in files for ref in file['references']}
    unknown = sorted(embedded_ids - known_ids, key=str.lower)
    if unknown:
        raise ValueError(f'Embedded Elsa packages are not published by configured upstreams: {unknown}')
    return {
        'package_id': (cfg.get('expected_package_ids') or [None])[0],
        'source_root': rules.get('source_root', '.'),
        'archive_prefix': rules.get('archive_prefix', ''),
        'expected_version': version,
        'known_published_ids': sorted(known_ids, key=str.lower),
        'files': sorted(files, key=lambda item: item['path']),
        'template_configs': sorted(set(template_configs)),
    }


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
    expected_ids = cfg.get('expected_package_ids')
    if expected_ids is not None and sorted(expected_ids, key=str.lower) != sorted(
        (p['id'] for p in packages), key=str.lower
    ):
        raise ValueError('Evaluated package inventory differs from the release profile')
    policy = state['profile']['release_kinds'][version.kind]
    dependencies = {}
    upstream_manifests = {}
    for upstream in cfg['dependencies']:
        binding = entry(state, upstream).get('binding')
        if not binding:
            raise ValueError(f'Bind upstream {upstream} before evaluating this manifest')
        upstream_manifest = read(binding['manifest'])
        upstream_manifests[upstream] = upstream_manifest
        dependencies.update({p['id']:p.get('version',state['version']) for p in upstream_manifest['nuget']})
    result = {
        'version': version.version, 'source_commit': sha,
        'nuget': packages, 'expected_dependencies': dependencies,
        'feeds': [f for f in state['profile']['feeds'] if f['name'] in policy['feeds']],
        'npm': [{'name': n, 'version': version.version, 'dist_tag': policy['npm_dist_tag'], 'registry': 'https://registry.npmjs.org'} for n in cfg['npm']],
    }
    content = embedded_content(path, cfg, version.version, upstream_manifests)
    if content is not None:
        result['content_expectations'] = content
    return result


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
