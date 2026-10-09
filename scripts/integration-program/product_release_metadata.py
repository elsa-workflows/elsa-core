"""Exact-source, pre-build SDK metadata for selected-product release plans."""
from __future__ import annotations

from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
from pathlib import Path
import re
import subprocess

import prepare_maintenance_build as maintenance
from package_impact import InventoryGraph
from prove_consolidated_packages import dependency_groups, framework_reference_groups, parse_metadata, require, run

ROOT = Path(__file__).resolve().parents[2]
SDK = '10.0.300'
CORE_REFS = {'3.8': 'refs/heads/release/3.8.4', '3.9': 'refs/heads/release/3.9.0'}
DESCENDANTS = {
    ('studio', '3.8'): '41ed15db7bfce7af5be2d362001518e8e692c22e',
    ('studio', '3.9'): 'e7cf096bc117d970dc0bbc11e26dabd2ea2e1b00',
    ('extensions', '3.8'): '92c27a3dd2c7f1dc107cba34749dd293a5d77743',
    ('extensions', '3.9'): 'bd7b846efae7e93676c6c3a594e682a5a958b766',
}
OWNERSHIP_DOCUMENT = 'docs/integration-program/consolidation/secrets-legacy-package-disposition.md'
OWNERSHIP_SHA256 = '1796509a72de4bd6d3831765df9453da773fb41069cb1202166505fb695b5958'
CANONICAL_OWNERS = {
    'elsa.studio.secrets': 'studio',
    **{('elsa.secrets.persistence.efcore' + suffix): 'core'
       for suffix in ('', '.postgresql', '.sqlserver', '.sqlite')},
}
PROPERTIES = ('IsPackable,IsTestProject,PackageId,PackageVersion,AssemblyName,TargetFrameworks,TargetFramework,'
              'IncludeSymbols,SymbolPackageFormat,IncludeBuildOutput,IncludeContentInPack,RepositoryUrl,PackageProjectUrl,NETCoreSdkVersion,'
              'GenerateElsaPackageManifest,ElsaPackageManifestIncludeInPackage,ElsaPackageManifestPackagePath,MSBuildToolsPath')


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def canonical_hash(value: object) -> str:
    return sha256(json.dumps(value, sort_keys=True, separators=(',', ':')).encode())


def restore_scope(source: Path, assets: dict, sdk_tools: str) -> dict:
    restore = assets['project']['restore']
    configs = restore['configFilePaths']
    require(isinstance(configs, list) and len(configs) == 1 and
            Path(configs[0]).resolve() == (source / 'NuGet.Config').resolve(), 'restore_config_scope')
    urls, local = [], []
    for value in restore['sources']:
        if value.startswith('https://'):
            urls.append(value)
        else:
            require(Path(value).resolve() == (Path(sdk_tools).parents[1] / 'library-packs').resolve(),
                    'restore_source_scope')
            local.append('selected-sdk-library-packs')
    return {'config_files': [{'path': 'NuGet.Config', 'sha256': sha256((source / 'NuGet.Config').read_bytes())}],
            'public_sources': sorted(urls), 'local_sources': local}


def ownership_policy() -> dict:
    require(sha256((ROOT / OWNERSHIP_DOCUMENT).read_bytes()) == OWNERSHIP_SHA256, 'ownership_policy_identity')
    return {'path': OWNERSHIP_DOCUMENT, 'sha256': OWNERSHIP_SHA256, 'lines': ['3.8', '3.9'],
            'canonical_owners': CANONICAL_OWNERS}


def manifest_target_identity(assets: dict) -> list[dict]:
    identities = []
    for key, library in assets['libraries'].items():
        identifier, version = key.rsplit('/', 1)
        if identifier.casefold() != 'elsa.platform.packagemanifest.generator':
            continue
        entries = [entry for entry in library['files'] if entry == 'build/Elsa.Platform.PackageManifest.Generator.targets']
        require(len(entries) == 1 and library.get('sha512'), 'manifest_target_identity')
        files = [Path(folder) / library['path'] / entries[0] for folder in assets['packageFolders']]
        files = [path for path in files if path.is_file() and not path.is_symlink()]
        require(len(files) == 1, 'manifest_target_identity')
        identities.append({'id': identifier, 'version': version, 'restore_sha512': library['sha512'],
                           'entry': entries[0], 'sha256': sha256(files[0].read_bytes())})
    return identities


def git(root: Path, *args: str) -> str:
    return maintenance.git(root, *args)


def bind_source(controller: Path, product: str, line: str, commit: str, observation: dict | None = None) -> dict:
    require(product in ('core', 'studio', 'extensions') and line in ('3.8', '3.9'), 'source_selection')
    require(re.fullmatch(r'[a-f0-9]{40}', commit) is not None, 'source_selection')
    if product == 'core':
        require(observation is not None and observation['ref'] == CORE_REFS[line] and
                observation['commit'] == commit and observation['tree'] == git(controller, 'rev-parse', commit + '^{tree}'),
                'source_selection')
        return {'product': product, 'line': line, 'kind': 'observed-core-release-branch',
                'commit': commit, 'tree': observation['tree'], 'observation': observation}
    require(DESCENDANTS.get((product, line)) == commit, 'source_selection')
    rows = [row for row in maintenance.registered_core_candidates(maintenance.load_register())
            if (row['product'], row['line'], row['commit'], row['kind']) == (product, line, commit, 'maintenance')]
    require(len(rows) == 1, 'source_selection')
    maintenance.verify_source(controller, rows[0])
    row = rows[0]
    return {'product': product, 'line': line, 'kind': 'admitted-maintenance-descendant',
            **{key: row[key] for key in ('commit', 'tree', 'parents', 'original_commit', 'original_parents',
                                        'contained_commit', 'contained_tree')},
            'candidate_register_sha256': sha256(maintenance.CANDIDATES.read_bytes()),
            'original_register_sha256': sha256(maintenance.REGISTER.read_bytes())}


def checkout_source(controller: Path, binding: dict, destination: Path) -> None:
    require(not destination.exists(), 'source_destination_exists')
    run(['git', 'clone', '--quiet', '--no-checkout', '--shared', str(controller), str(destination)], controller,
        env=maintenance.build_environment())
    run(['git', '-c', 'advice.detachedHead=false', 'checkout', '--quiet', '--detach', binding['commit']], destination,
        env=maintenance.build_environment())
    require(git(destination, 'rev-parse', 'HEAD^{tree}') == binding['tree'] and not git(destination, 'status', '--porcelain'),
            'source_checkout_identity')


def metadata_command(project: str, version: str) -> list[str]:
    return ['dotnet', 'msbuild', project, '-nologo', '-p:Configuration=Release', f'-p:Version={version}',
            f'-p:PackageVersion={version}', '-p:NoBuild=true', '-p:BuildProjectReferences=false',
            '-p:GeneratePackageOnBuild=false', '-p:ContinuePackingAfterGeneratingNuspec=false']


def evaluate_project(source: Path, project: str, version: str) -> dict:
    values = json.loads(run(metadata_command(project, version) +
        ['-getProperty:' + PROPERTIES, '-getItem:ProjectReference,PackageReference'], source,
        env=maintenance.build_environment()))
    properties = values['Properties']
    require(properties['NETCoreSdkVersion'] == SDK, 'metadata_sdk_identity')
    frameworks = (properties['TargetFrameworks'] or properties['TargetFramework']).split(';')
    require(all(frameworks) and len(set(frameworks)) == len(frameworks), 'metadata_framework_identity')
    references = []
    for item in values['Items']['ProjectReference']:
        target = (source / project).parent / item['Identity'].replace('\\', '/')
        require(target.resolve().is_relative_to(source.resolve()), 'metadata_project_reference')
        references.append({'target_project': target.resolve().relative_to(source.resolve()).as_posix()})
    return {'path': project, 'package_id': properties['PackageId'],
            'is_packable': properties['IsPackable'].lower() == 'true',
            'is_test_project': properties['IsTestProject'].lower() == 'true',
            'target_frameworks': frameworks, 'project_references': references,
            'package_references': [{'id': item['Identity']} for item in values['Items']['PackageReference']],
            'properties': properties}


def validate_project_references(projects: list[dict]) -> None:
    paths = {project['path'] for project in projects}
    require(all(reference['target_project'] in paths for project in projects
                for reference in project['project_references']), 'untracked_project_reference')


def project_scope(project: dict, product: str, recipe_projects: set[str]) -> str | None:
    path = project['path']
    if not project['is_packable']:
        return 'source_nonpackable'
    if project['is_test_project']:
        return 'source_test_project'
    if path not in recipe_projects:
        return 'outside_original_release_recipe'
    if not path.startswith('src/'):
        return 'source_sample_or_tooling'
    owner = CANONICAL_OWNERS.get(project['package_id'].casefold(), product)
    return 'canonical_owner_' + owner if owner != product else None


def stage_project(source: Path, project: dict, version: str, destination: Path) -> dict:
    """Only output-file collection is projected away; SDK groups remain actual."""
    destination.mkdir(parents=True)
    command = metadata_command(project['path'], version) + ['-restore',
        '-target:_GetRestoreProjectStyle;GenerateNuspec', '-p:IncludeBuildOutput=false',
        '-p:ElsaPackageManifestIncludeInPackage=false', '-p:IncludeContentInPack=false',
        f'-p:RestoreConfigFile={source / "NuGet.Config"}', f'-p:NuspecOutputPath={destination}',
        f'-p:PackageOutputPath={destination / "forbidden-packages"}']
    stage = 'generate_nuspec'
    try:
        run(command, source, env=maintenance.build_environment(), timeout=300, log=destination / 'command.private.log')
        stage = 'nuspec_metadata'
        files = list(destination.glob('*.nuspec'))
        main = [path for path in files if not path.name.endswith('.symbols.nuspec')]
        require(len(main) == 1 and not list(destination.rglob('*.nupkg')), 'metadata_output_identity')
        data = main[0].read_bytes()
        metadata = parse_metadata(data)
        require(metadata.findtext('id') == project['package_id'] and metadata.findtext('version') == version,
                'metadata_package_identity')
        dependencies, framework_references = dependency_groups(metadata), framework_reference_groups(metadata)
        stage = 'restore_assets'
        assets = source / Path(project['path']).parent / 'obj/project.assets.json'
        require(assets.is_file() and not assets.is_symlink(), 'metadata_restore_identity')
        restored = json.loads(assets.read_text())
        stage = 'original_output_policy'
        original = evaluate_project(source, project['path'], version)
        require(all(original[key] == project[key] for key in ('path', 'package_id', 'is_packable', 'target_frameworks')),
                'restored_project_identity')
        original_policies = {}
        for framework in project['target_frameworks']:
            properties = json.loads(run(metadata_command(project['path'], version) +
                [f'-p:TargetFramework={framework}', '-getProperty:' + PROPERTIES], source,
                env=maintenance.build_environment()))['Properties']
            require(properties['PackageId'] == project['package_id'] and properties['PackageVersion'] == version,
                    'restored_project_identity')
            original_policies[framework] = {key: properties[key] for key in ('IncludeBuildOutput', 'IncludeContentInPack', 'IncludeSymbols',
                'SymbolPackageFormat', 'GenerateElsaPackageManifest', 'ElsaPackageManifestIncludeInPackage',
                'ElsaPackageManifestPackagePath')}
        stage = 'restore_config_scope'
        scope = restore_scope(source, restored, original['properties']['MSBuildToolsPath'])
        stage = 'manifest_target_identity'
        targets = manifest_target_identity(restored)
        stage = 'sdk_pack_target_identity'
        sdk_targets_hash = sha256((Path(original['properties']['MSBuildToolsPath']) / 'NuGet.Build.Tasks.Pack.targets').read_bytes())
        return {'status': 'observed', 'nuspec_sha256': sha256(data),
                'dependency_groups': dependencies,
                'framework_reference_groups': framework_references,
                'restore_assets_sha256': sha256(assets.read_bytes()),
                'restore_scope': scope,
                'original_output_policy': original_policies,
                'original_content_project_sha256': sha256((source / project['path']).read_bytes()),
                'manifest_content_targets': targets,
                'sdk_pack_targets_sha256': sdk_targets_hash,
                'projection': {'NoBuild': True, 'ContinuePackingAfterGeneratingNuspec': False, 'IncludeBuildOutput': False,
                               'ElsaPackageManifestIncludeInPackage': False, 'IncludeContentInPack': False},
                '_assets': restored}
    except (ValueError, OSError, subprocess.TimeoutExpired) as error:
        category = ('command_timeout' if isinstance(error, subprocess.TimeoutExpired) else
                    'metadata_io_failed' if isinstance(error, OSError) else
                    'command_failed' if str(error).startswith('Command failed (') else 'metadata_contract_failed')
        return {'status': 'unavailable', 'reason': 'source_metadata_unavailable',
                'failure_stage': stage, 'failure_category': category}


def evaluate_inventory(source: Path, binding: dict, version: str, private: Path, *, workers: int = 4) -> dict:
    paths = git(source, 'ls-files', '*.csproj').splitlines()
    require(paths and len(paths) == len(set(paths)) and all(not Path(path).is_absolute() and '..' not in Path(path).parts
            and (source / path).is_file() and not (source / path).is_symlink() for path in paths), 'source_project_universe')
    solution = {'core': 'Elsa.sln', 'studio': 'Elsa.Studio.sln', 'extensions': 'Elsa.Extensions.sln'}[binding['product']]
    recipe_projects = {path.replace('\\', '/') for path in re.findall(
        r'^Project\([^\n]+?= "[^"]+", "([^"]+\.csproj)"', (source / solution).read_text(encoding='utf-8-sig'), re.M)}
    require(recipe_projects and recipe_projects <= set(paths), 'original_recipe_inventory')
    workflow = '.github/workflows/packages.yml' if binding['product'] == 'core' else '.github/maintenance-inert-workflows/packages.yml.source'
    require((source / workflow).is_file(), 'original_recipe_identity')
    if binding['product'] != 'studio':
        require(json.loads((source / '.nuke/parameters.json').read_text())['Solution'] == solution and
                (b'Compile+Pack' if binding['product'] == 'core' else b'Compile+Test+Pack') in
                (source / workflow).read_bytes(), 'original_recipe_identity')
    else:
        require(b'dotnet pack Elsa.Studio.sln' in (source / workflow).read_bytes(), 'original_recipe_identity')
    owner_policy = ownership_policy()
    with ThreadPoolExecutor(max_workers=workers) as pool:
        projects = list(pool.map(lambda path: evaluate_project(source, path, version), paths))
    validate_project_references(projects)
    selected, excluded = [], []
    for project in projects:
        reason = project_scope(project, binding['product'], recipe_projects)
        if reason:
            excluded.append({'project': project['path'], 'id': project['package_id'], 'reason': reason})
        else:
            values = project['properties']
            require(values['PackageVersion'] == version, 'metadata_requested_version')
            selected.append({'id': project['package_id'], 'project': project['path'],
                'frameworks': project['target_frameworks'], 'include_build_output': values['IncludeBuildOutput'].lower() != 'false',
                'symbols': values['IncludeSymbols'].lower() == 'true' and values['IncludeBuildOutput'].lower() != 'false',
                'symbol_format': values['SymbolPackageFormat'],
                'repository_url': values['RepositoryUrl'], 'project_url': values['PackageProjectUrl']})
    require(selected and len({row['id'].casefold() for row in selected}) == len(selected), 'inventory_package_identity')
    by_path = {row['path']: row for row in projects}
    with ThreadPoolExecutor(max_workers=workers) as pool:
        staged = list(pool.map(lambda row: stage_project(source, by_path[row['project']], version,
                           private / sha256(row['project'].encode())[:16]), selected))
    for row, metadata in zip(selected, staged):
        row['metadata'] = metadata
    graph = InventoryGraph({'repositories': {'source': {'slug': 'source'}}, 'project_inventory': {'source': projects}})
    closure = graph.affected_tests([('source', row['project']) for row in selected])
    require(not list(source.glob('**/bin/**/*.dll')) and not list(source.glob('**/*.nupkg')), 'product_build_forbidden')
    result = {'source_commit': binding['commit'], 'source_tree': binding['tree'], 'requested_version': version,
              'projects': projects, 'selected': selected, 'excluded': excluded,
              'release_recipe': {'solution': solution, 'sha256': sha256((source / solution).read_bytes()),
                                 'projects': sorted(recipe_projects), 'workflow': workflow,
                                 'workflow_sha256': sha256((source / workflow).read_bytes())},
              'applicable_tests': sorted(path for _, path in closure),
              'ownership_policy': owner_policy}
    # Hash the complete public inventory projection, excluding private restored paths/data.
    result['sha256'] = canonical_hash(public_inventory(result))
    return result


def public_inventory(inventory: dict) -> dict:
    return {**{key: value for key, value in inventory.items() if key not in ('sha256', 'selected', 'projects')},
        'selected': [{**row, 'metadata': {key: value for key, value in row['metadata'].items() if not key.startswith('_')}}
                     for row in inventory['selected']],
        'projects': [{key: value for key, value in row.items() if key != 'properties'} for row in inventory['projects']]}
