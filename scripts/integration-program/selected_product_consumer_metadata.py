"""Finite original archive catalog and native discovered graph validation."""
from __future__ import annotations

import json
from pathlib import Path
import shutil
import zipfile

import plan_product_release as planner
import product_release_metadata as metadata
import prove_consolidated_packages as archives
import selected_consumer_sdk as sdk

require = metadata.require


def nearest_group(groups: list[dict], framework: str, semantics: planner.Semantics) -> dict:
    if not groups:
        return {'framework': 'any', 'dependencies': []}
    choice = semantics.call('frameworks', values=[{'consumer': framework,
                'candidates': [row['framework'] for row in groups]}])[0]
    require(choice['compatible'], 'consumer_dependency_framework')
    return next(row for row in groups if row['framework'] == choice['nearest'])


def build_inspector(root: Path, output: Path) -> Path:
    """Reuse the native inspector in private SDK-pinned tooling, not the checkout."""
    output.mkdir()
    (output / 'global.json').write_text(json.dumps({'sdk': {'version': metadata.SDK, 'rollForward': 'disable'}}))
    destination = output / 'scripts/integration-program/VerifyPackageSymbolPair'
    destination.mkdir(parents=True)
    for name in ('Program.cs', 'VerifyPackageSymbolPair.csproj'):
        shutil.copyfile(root / 'scripts/integration-program/VerifyPackageSymbolPair' / name, destination / name)
    return archives.build_symbol_verifier(output, output)


def archive_catalog(originals: dict, selected: dict, config: Path, semantics: planner.Semantics,
                    inspector: Path, output: Path, *, source_downloads: dict | None = None) -> tuple[dict, dict]:
    """Freeze original bytes before any discovery; source cache is never a proof cache."""
    output.mkdir()
    pool = output / 'archives'
    pool.mkdir()
    catalog, inspection_cache = {}, {'archive_inspector': inspector}
    declarations = (source_downloads or {}).get('projects', {})
    sdk_projects = {project: {framework: sdk.original_policy(assets, framework,
        source_downloads=declarations.get(project, {}).get('frameworks', {}).get(framework))
        for framework in assets.get('project', {}).get('frameworks', {})} for project, assets in originals.items()}
    downloads = sdk.freeze_downloads(originals, inspector, pool, semantics, sdk_projects)
    for project, assets in originals.items():
        for key, library in assets['libraries'].items():
            if library['type'] != 'package':
                continue
            identifier, version = key.rsplit('/', 1)
            folded = identifier.casefold()
            require(folded not in selected, 'consumer_original_selected_registry_package')
            require(planner.ID.fullmatch(identifier) is not None, 'consumer_external_catalog_identity')
            identity = (folded, version)
            context = {'project': project, 'frameworks': [framework for framework, target in assets['targets'].items()
                       if key in target], 'content_hash': library['sha512']}
            if identity in catalog:
                require(catalog[identity]['content_hash'] == library['sha512'], 'consumer_original_archive_ambiguity')
                catalog[identity]['original_contexts'].append(context)
                catalog[identity]['effective_contexts'].extend(sdk.effective_contexts(assets, key, sdk_projects[project]))
                continue
            relative = Path(library.get('path', f'{folded}/{version.lower()}'))
            require(not relative.is_absolute() and '..' not in relative.parts, 'consumer_original_archive_path')
            candidates = [Path(folder) / relative / f'{folded}.{version.lower()}.nupkg' for folder in assets['packageFolders']]
            candidates = [path for path in candidates if path.exists()]
            require(len(candidates) == 1, 'consumer_original_archive_candidates')
            source = candidates[0]
            require(source.is_file() and not any(part.is_symlink() for part in (source, *source.parents)),
                    'consumer_original_archive_path')
            destination = pool / relative / source.name
            require(not destination.exists(), 'consumer_original_archive_collision')
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, destination)
            # Native integrity/hash and nuspec parsing operate only on frozen bytes.
            frozen_assets = {'libraries': {key: library}, 'packageFolders': {str(pool): {}}}
            _, verified = archives.restored_archive(frozen_assets, identifier, version, cache=inspection_cache)
            with zipfile.ZipFile(destination) as package:
                names = archives.archive_names(package)
                nuspecs = [name for name in names if name.endswith('.nuspec')]
                require(len(nuspecs) == 1, 'consumer_original_nuspec_identity')
                data = package.read(nuspecs[0])
            native = semantics.call('nuspecs', values=[data.decode('utf-8-sig')])[0]
            require(native['id'].casefold() == folded and native['version'] == version, 'consumer_original_nuspec_identity')
            catalog[identity] = {'id': identifier, 'version': version, 'content_hash': verified['nuget_content_hash'],
                'archive_sha256': verified['archive_sha256'], 'signed': verified['signed'],
                'archive': destination, 'nuspec_sha256': metadata.sha256(data), 'groups': native['groups'],
                'original_contexts': [context], 'effective_contexts': sdk.effective_contexts(assets, key, sdk_projects[project])}
    ids = sorted({row['id'] for row in catalog.values()} | {row['id'] for row in selected.values()} |
                 {row['id'] for row in downloads.values()})
    native = semantics.call('feeds', config=str(config), ids=ids)
    sources = {row['name']: row['url'] for row in native['sources']}
    require(sources and all(url in planner.FEED_BASES for url in sources.values()), 'consumer_feed_origin')
    mapping = {row['id'].casefold(): row['sources'] for row in native['packages']}
    require(set(mapping) == {identifier.casefold() for identifier in ids}, 'consumer_feed_inventory')
    mirrors = {}
    for index, name in enumerate(sorted(sources)):
        directory = output / ('feed-' + str(index))
        directory.mkdir()
        mirrors[name] = str(directory)
    for (folded, _), row in list(catalog.items()) + list(downloads.items()):
        names = mapping[folded]
        require(names and all(name in sources for name in names), 'consumer_external_feed_mapping')
        for name in names:
            destination = Path(mirrors[name]) / row['archive'].name
            shutil.copyfile(row['archive'], destination)
            require(metadata.sha256(destination.read_bytes()) == row['archive_sha256'], 'consumer_mirror_archive_changed')
    receipt = {'original_sources': sources, 'offline_mirrors': mirrors, 'mapping': mapping,
               'source_downloads': source_downloads,
               'signature_scope': 'Native signature integrity and content identity; not signer or feed-origin certification',
               'toolchain_downloads': [{key: str(value) if isinstance(value, Path) else value for key, value in row.items()}
                                      for row in downloads.values()],
               'archives': [{key: str(value) if isinstance(value, Path) else value for key, value in row.items()}
                            for row in catalog.values()]}
    (output / 'catalog.private.json').write_text(json.dumps(receipt, indent=2, sort_keys=True))
    return catalog, {'sources': sources, 'mapping': mapping, 'mirrors': mirrors, 'sdk_downloads': downloads,
        'original_assets': originals,
        'sdk_projects': sdk_projects}


def validate_native_tools(plan: dict, semantics: planner.Semantics, inspector: Path) -> None:
    require(plan['semantics']['sdk_version'] == metadata.SDK and
            semantics.call('identity') == plan['semantics']['assemblies'], 'consumer_native_semantics_identity')
    for row in plan['semantics']['assemblies']:
        require(metadata.sha256((inspector.parent / (row['name'] + '.dll')).read_bytes()) == row['sha256'],
                'consumer_native_inspector_identity')


def audit_native_graph(assets: dict, lock: dict, root_id: str, framework: str, selected: dict,
                       catalog: dict, version: str, semantics: planner.Semantics, *, sdk_policy: dict | None = None,
                       pruned_edges: list | None = None) -> dict:
    require(set(assets['targets']) == {framework} and lock.get('version') == 1 and
            set(lock['dependencies']) == {framework}, 'consumer_native_graph_framework')
    require(set(assets['libraries']) == set(assets['targets'][framework]), 'consumer_native_library_partition')
    if sdk_policy is not None:
        sdk.validate_projection(assets, framework, sdk_policy)
    graph = {}
    locked = {identifier.casefold(): row for identifier, row in lock['dependencies'][framework].items()}
    require(len(locked) == len(lock['dependencies'][framework]), 'consumer_native_lock_duplicate')
    for key, target in assets['targets'][framework].items():
        identifier, resolved = key.rsplit('/', 1)
        folded = identifier.casefold()
        library = assets['libraries'][key]
        require(folded not in graph and target['type'] == 'package' and library['type'] == 'package',
                'consumer_native_project_fallback')
        if folded in selected:
            package = selected[folded]
            require(resolved == version, 'consumer_native_selected_version')
            content_hash = package['content_hash']
            dependencies = {row['id']: row['version'] for row in nearest_group(
                package['dependency_groups'], framework, semantics)['dependencies']}
        else:
            require((folded, resolved) in catalog, 'consumer_native_unreviewed_external')
            package = catalog[(folded, resolved)]
            content_hash = package['content_hash']
            dependencies = {row['id']: row['range'] for row in nearest_group(package['groups'], framework, semantics)['dependencies']}
        require(library['sha512'] == content_hash and folded in locked, 'consumer_native_content_hash')
        row = locked[folded]
        require(row['resolved'] == resolved and row['contentHash'] == content_hash and
                row['type'] == ('Direct' if folded == root_id.casefold() else 'Transitive'), 'consumer_native_lock_identity')
        actual_edges = {name.casefold() for name in target.get('dependencies', {})}
        for name in list(dependencies):
            prunable = (sdk_policy is not None and name.casefold() not in selected and
                sdk.pruned_edge(package, framework, name, dependencies[name], sdk_policy, semantics))
            if prunable:
                require(name.casefold() not in actual_edges and name.casefold() not in
                    {key.casefold() for key in row.get('dependencies', {})}, 'consumer_native_pruned_edge_retained')
                if pruned_edges is not None:
                    pruned_edges.append({'from_id': identifier, 'from_version': resolved, 'id': name,
                        'range': dependencies[name], 'prune_range': sdk_policy['pruning'][name]})
                dependencies.pop(name)
        require({name.casefold() for name in row.get('dependencies', {})} ==
                {name.casefold() for name in dependencies} == actual_edges, 'consumer_native_declared_edges')
        graph[folded] = {'id': identifier, 'version': resolved, 'content_hash': content_hash,
                         'dependencies': dependencies, 'selected': folded in selected}
    require(set(graph) == set(locked) and root_id.casefold() in graph, 'consumer_native_lock_partition')
    requested = locked[root_id.casefold()].get('requested')
    require(isinstance(requested, str) and semantics.call('ranges', values=[{'range': requested, 'version': version}])[0]
            ['normalized'] == f'[{version}, {version}]', 'consumer_native_root_range')
    ranges = []
    for row in graph.values():
        for identifier, dependency_range in row['dependencies'].items():
            require(identifier.casefold() in graph, 'consumer_native_missing_nuspec_edge')
            resolved = graph[identifier.casefold()]['version']
            key = row['id'] + '/' + row['version']
            native_edges = {name.casefold(): value for name, value in assets['targets'][framework][key].get('dependencies', {}).items()}
            lock_edges = {name.casefold(): value for name, value in locked[row['id'].casefold()].get('dependencies', {}).items()}
            ranges.extend({'range': value, 'version': resolved} for value in
                (dependency_range, native_edges[identifier.casefold()], lock_edges[identifier.casefold()]))
    checks = semantics.call('ranges', values=ranges)
    require(len(checks) == len(ranges) and all(row['satisfies'] for row in checks), 'consumer_dependency_range_conflict')
    require(all(len({row['normalized'] for row in checks[index:index + 3]}) == 1
                for index in range(0, len(checks), 3)), 'consumer_native_dependency_range_changed')
    # Only nodes reachable through full actual nuspec edges may enter the proof.
    reachable, pending = set(), [root_id.casefold()]
    while pending:
        folded = pending.pop()
        if folded in reachable:
            continue
        reachable.add(folded)
        pending.extend(identifier.casefold() for identifier in graph[folded]['dependencies'])
    require(reachable == set(graph), 'consumer_native_unreachable_package')
    return graph
