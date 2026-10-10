"""Original SDK restore policy and separate targeting-pack evidence, never product packages."""
from __future__ import annotations

import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import zipfile

import plan_product_release as planner
import product_release_metadata as metadata
import prove_consolidated_packages as archives
import prove_consolidated_package_consumers as consumers

require = metadata.require
DOWNLOAD_VERSIONS = {'net8.0': '8.0.27', 'net9.0': '9.0.16'}
PACK_IDS = {'Microsoft.NETCore.App.Ref', 'Microsoft.AspNetCore.App.Ref'}
PRUNE_RANGE = re.compile(r'^\(,(\d+\.\d+\.\d+)\]$')


def original_policy(assets: dict, framework: str, *, source_downloads: list[dict] | None = None) -> dict:
    frame = assets.get('project', {}).get('frameworks', {}).get(framework, {})
    pruning = frame.get('packagesToPrune', {})
    require(type(pruning) is dict and all(type(k) is str and planner.ID.fullmatch(k) and
        type(v) is str and PRUNE_RANGE.fullmatch(v) for k, v in pruning.items()), 'consumer_sdk_original_pruning')
    downloads, observed_source = [], []
    declared = {row['id']: row['version'] for row in source_downloads or []}
    require(len({identifier.casefold() for identifier in declared}) == len(source_downloads or []) and
            not {identifier.casefold() for identifier in declared} & {identifier.casefold() for identifier in PACK_IDS},
            'consumer_source_download_declarations')
    for row in frame.get('downloadDependencies', []):
        require(type(row) is dict and set(row) == {'name', 'version'} and type(row['name']) is str and
            type(row['version']) is str, 'consumer_sdk_original_download')
        if row['name'] not in PACK_IDS:
            require(row['name'] in declared and row['version'] ==
                    f'[{declared[row["name"]]}, {declared[row["name"]]}]', 'consumer_sdk_original_download')
            observed_source.append(row['name'])
            continue
        parts = row['version'].strip('[]').split(',')
        require(row['version'].startswith('[') and row['version'].endswith(']') and len(parts) == 2 and
            parts[0].strip() == parts[1].strip() and re.fullmatch(r'\d+\.\d+\.\d+', parts[0].strip()),
            'consumer_sdk_original_download')
        require(parts[0].strip() == DOWNLOAD_VERSIONS.get(framework), 'consumer_sdk_download_version')
        downloads.append({'id': row['name'], 'version': parts[0].strip()})
    require(len({row['id'] for row in downloads}) == len(downloads), 'consumer_sdk_duplicate_download')
    require(len(observed_source) == len(declared) and set(observed_source) == set(declared),
            'consumer_source_download_partition')
    return {'pruning': dict(pruning), 'downloads': sorted(downloads, key=lambda row: row['id'])}


def bind_source_downloads(root: Path, plan: dict, originals: dict, output: Path,
                          semantics: planner.Semantics) -> dict:
    """Classify present non-SDK rows using fresh evaluation of the exact admitted source."""
    affected = [project for project, assets in originals.items() if any(
        type(row) is dict and row.get('name') not in PACK_IDS
        for frame in assets.get('project', {}).get('frameworks', {}).values()
        for row in frame.get('downloadDependencies', []))]
    result = {'source': plan['source'], 'sdk_version': metadata.SDK, 'projects': {}}
    if not affected:
        return result
    selected = {row['project']: row for row in plan['inventory']['selected']}
    require(set(affected) <= set(selected), 'consumer_source_download_selected_project')
    output.mkdir()
    consumers.prepare_isolation(output, metadata.SDK)
    home = output / 'home'
    home.mkdir()
    environment = {key: os.environ[key] for key in ('PATH', 'TMPDIR', 'DOTNET_ROOT', 'DOTNET_ROOT_X64') if key in os.environ}
    environment.update(HOME=str(home), DOTNET_CLI_HOME=str(home), NUGET_PACKAGES=str(output / 'packages'),
        NUGET_HTTP_CACHE_PATH=str(output / 'http-cache'), NUGET_PLUGINS_CACHE_PATH=str(output / 'plugins-cache'),
        DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER='1', MSBUILDDISABLENODEREUSE='1',
        DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1')
    source = output / 'source'
    metadata.checkout_source(root, plan['source'], source)
    for project in affected:
        policy = selected[project]
        path = source / project
        require(not Path(project).is_absolute() and '..' not in Path(project).parts and
            path.resolve().is_relative_to(source.resolve()) and path.is_file() and
            not any(part.is_symlink() for part in (path, *path.parents)), 'consumer_source_download_project_path')
        digest = metadata.sha256(path.read_bytes())
        require(digest == policy['metadata']['original_content_project_sha256'], 'consumer_source_download_project_hash')
        frameworks, evaluation_hashes = {}, {}
        for framework in policy['frameworks']:
            command = metadata.metadata_command(project, plan['requested_version']) + [f'-p:TargetFramework={framework}',
                '-getProperty:PackageId,TargetFramework,NETCoreSdkVersion', '-getItem:PackageDownload']
            raw = metadata.run(command, source, env=environment, timeout=120)
            evaluated = planner.read_json(raw.encode())
            require(evaluated['Properties'] == {'PackageId': policy['id'], 'TargetFramework': framework,
                'NETCoreSdkVersion': metadata.SDK}, 'consumer_source_download_evaluation_identity')
            declarations = []
            for item in evaluated['Items']['PackageDownload']:
                identifier, version = item['Identity'], item.get('Version', '')
                exact = re.fullmatch(r'\[([^,\[\]\s]+)(?:,\s*\1)?\]', version)
                require(type(identifier) is str and planner.ID.fullmatch(identifier) and
                    identifier.casefold() not in {name.casefold() for name in PACK_IDS} and exact is not None,
                    'consumer_source_download_exact_declaration')
                declarations.append({'id': identifier, 'version': exact[1]})
            require(len({row['id'].casefold() for row in declarations}) == len(declarations),
                    'consumer_source_download_duplicate_declaration')
            versions = semantics.call('versions', values=[row['version'] for row in declarations])
            require(len(versions) == len(declarations), 'consumer_source_download_version_partition')
            for declaration, version in zip(declarations, versions):
                declaration['version'] = version['normalized']
            declarations.sort(key=lambda row: row['id'])
            original_policy(originals[project], framework, source_downloads=declarations)
            frameworks[framework] = declarations
            evaluation_hashes[framework] = metadata.sha256(raw.encode())
            (output / (metadata.sha256((project + '/' + framework).encode()) + '.private.json')).write_text(raw)
        require(metadata.sha256(path.read_bytes()) == digest, 'consumer_source_download_project_hash')
        result['projects'][project] = {'id': policy['id'], 'project_sha256': digest,
            'original_assets_sha256': policy['metadata']['restore_assets_sha256'], 'frameworks': frameworks,
            'evaluation_sha256': evaluation_hashes}
    require(not metadata.git(source, 'status', '--porcelain'), 'consumer_source_download_source_changed')
    (output / 'source-downloads.private.json').write_text(json.dumps(result, indent=2, sort_keys=True))
    return result


def effective_contexts(assets: dict, key: str, policies: dict | None = None) -> list[dict]:
    return [{'framework': framework, 'dependencies': target[key].get('dependencies', {}),
             'packages_to_prune': (policies.get(framework, {'pruning': {}}) if policies is not None else
                                  original_policy(assets, framework))['pruning']}
            for framework, target in assets['targets'].items() if key in target]


def validate_projection(assets: dict, framework: str, policy: dict) -> None:
    actual = original_policy(assets, framework)
    require(actual == policy, 'consumer_sdk_restore_policy_changed')


def pruned_edge(package: dict, framework: str, identifier: str, dependency_range: str,
                policy: dict, semantics: planner.Semantics) -> bool:
    threshold = policy['pruning'].get(identifier)
    if not threshold:
        return False
    # SDK-pinned NuGet ShouldPrunePackage uses dependencyRange.Satisfies(pruneRange.MaxVersion).
    maximum = PRUNE_RANGE.fullmatch(threshold)
    require(maximum is not None, 'consumer_sdk_prune_range')
    if not semantics.call('ranges', values=[{'range': dependency_range, 'version': maximum[1]}])[0]['satisfies']:
        return False
    contexts = [row for row in package.get('effective_contexts', []) if row['framework'] == framework and
                row['packages_to_prune'] == policy['pruning']]
    return bool(contexts) and all(row['packages_to_prune'].get(identifier) == threshold and
        identifier.casefold() not in {name.casefold() for name in row['dependencies']} for row in contexts)


def freeze_downloads(originals: dict, inspector: Path, pool: Path, semantics: planner.Semantics,
                     policies: dict | None = None) -> dict:
    """Original snapshots bind versions only; these hashes are NEW frozen bootstrap evidence."""
    candidates = {}
    for project, assets in originals.items():
        for framework in assets.get('project', {}).get('frameworks', {}):
            policy = policies[project][framework] if policies is not None else original_policy(assets, framework)
            for row in policy['downloads']:
                key = row['id'], row['version']
                candidates.setdefault(key, set()).update(Path(folder) / row['id'].lower() / row['version'] /
                    (row['id'].lower() + '.' + row['version'] + '.nupkg') for folder in assets['packageFolders'])
    result = {}
    for (identifier, version), paths in sorted(candidates.items()):
        existing = [path for path in paths if path.exists()]
        require(len(existing) == 1, 'consumer_sdk_archive_candidates')
        source = existing[0]
        sidecar = source.with_suffix('.nupkg.sha512')
        require(all(path.is_file() and not any(part.is_symlink() for part in (path, *path.parents))
                    for path in (source, sidecar)), 'consumer_sdk_archive_path')
        destination = pool / identifier.lower() / version / source.name
        require(not destination.exists(), 'consumer_sdk_archive_collision')
        destination.parent.mkdir(parents=True)
        shutil.copyfile(source, destination)
        shutil.copyfile(sidecar, destination.with_suffix('.nupkg.sha512'))
        frame = {'downloadDependencies': [{'name': identifier, 'version': f'[{version}, {version}]'}]}
        frozen = {'project': {'frameworks': {'sdk': frame}}, 'packageFolders': {str(pool): {}}}
        _, verified = archives.restored_archive(frozen, identifier, version, targeting_pack=True,
                                                cache={'archive_inspector': inspector})
        with zipfile.ZipFile(destination) as package:
            nuspecs = [name for name in archives.archive_names(package) if name.endswith('.nuspec')]
            require(len(nuspecs) == 1, 'consumer_sdk_nuspec_count')
            native = semantics.call('nuspecs', values=[package.read(nuspecs[0]).decode('utf-8-sig')])[0]
        require(native['id'] == identifier and native['version'] == version, 'consumer_sdk_archive_identity')
        result[(identifier.casefold(), version)] = {'id': identifier, 'version': version,
            'archive': destination, 'archive_sha256': verified['archive_sha256'],
            'archive_sha512': hashlib.sha512(destination.read_bytes()).hexdigest(),
            'nuget_content_hash': verified['nuget_content_hash'], 'signed': verified['signed']}
    return result


def verify_downloads(assets: dict, framework: str, policy: dict, catalog: dict, cache: Path,
                     sources: dict, mapping: dict, inspector: Path, *, proof: bool) -> list[dict]:
    validate_projection(assets, framework, policy)
    records = []
    for row in policy['downloads']:
        key = row['id'].casefold(), row['version']
        require(key in catalog, 'consumer_sdk_unreviewed_download')
        expected = catalog[key]
        archive, verified = archives.restored_archive(assets, row['id'], row['version'], targeting_pack=True,
                                                      cache={'archive_inspector': inspector})
        require(archive.is_relative_to(cache) and not any(part.is_symlink() for part in (archive, *archive.parents)),
                'consumer_sdk_cache_path')
        data = archive.read_bytes()
        cache_metadata = archive.parent / '.nupkg.metadata'
        require(cache_metadata.is_file() and not cache_metadata.is_symlink(), 'consumer_sdk_cache_metadata_path')
        require(verified['archive_sha256'] == expected['archive_sha256'] and
            hashlib.sha512(data).hexdigest() == expected['archive_sha512'] and
            verified['nuget_content_hash'] == expected['nuget_content_hash'] and
            verified['signed'] == expected['signed'], 'consumer_sdk_download_bytes')
        origin = planner.read_json(cache_metadata.read_bytes()).get('source')
        require(origin in {sources[name] for name in mapping[key[0]]}, 'consumer_sdk_download_source')
        if proof:
            require(origin in planner.FEED_BASES, 'consumer_sdk_download_not_https')
        records.append({**row, 'source': origin, 'sha256': verified['archive_sha256'],
            'archive_sha512': expected['archive_sha512'], 'nuget_content_hash': verified['nuget_content_hash'],
            'signed': verified['signed']})
    return records


def retained_policy(policy: dict, pruned: list[dict], downloads: list[dict], original_assets_sha256: str) -> dict:
    return {'sdk_version': metadata.SDK, 'original_assets_sha256': original_assets_sha256,
        'pruning_enabled': bool(policy['pruning']),
        'pruning_sha256': metadata.sha256(json.dumps(policy['pruning'], sort_keys=True).encode()),
        'pruned_edges': pruned, 'downloads': downloads,
        'toolchain_hash_scope': 'new-frozen-bootstrap-catalog-joined-to-fresh-original-feed-bytes'}
