#!/usr/bin/env python3
"""Package-only Studio 3.8 consumers of exact reviewed selected archives.

Every selected applicable framework is a separate cold restore/compile cell.
Original hashed planning assets bound the allowed external versions and hashes.
"""
from __future__ import annotations

import argparse
from datetime import datetime
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import xml.etree.ElementTree as ET
import zipfile

import plan_product_release as planner
import product_release_metadata as metadata
import prove_product_release_artifacts as producer
import prove_consolidated_package_consumers as consumers
import prove_consolidated_packages as archives
import selected_product_consumer_metadata as resolution
from selected_product_consumer_metadata import nearest_group
from product_artifact_execution import local_execution, validate_local_execution
from prove_consolidated_packages import archive_names, dependency_groups, metadata as nuspec, require

ROOT = Path(__file__).resolve().parents[2]
FIXTURE = ROOT / 'scripts/integration-program/selected-studio-consumer/Program.cs'
LOCAL = 'selected-local-archives'
STUDIO38_CONTRACT_SOURCE = {
    'src/framework/Elsa.Studio.Core/Services/DefaultRemoteBackendAccessor.cs': '2818998450af0126555755dec8f2ff45fe582c6558ac09c064c5053e761bb909',
    'src/framework/Elsa.Studio.Core/Options/BackendOptions.cs': 'd640f27152eb46631bc157733c4e8284cae845923d717f6fdb9df9235466c538',
    'src/framework/Elsa.Studio.Core/Models/RemoteBackend.cs': 'c96cacf7fc3d755cff76861d2e837fc35aa359b2779137ee6cae99253a33f1e4',
}


def read_bound(path: Path, digest: str) -> bytes:
    require(path.is_file() and not any(part.is_symlink() for part in (path, *path.parents)), 'consumer_input_path')
    data = path.read_bytes()
    require(metadata.sha256(data) == digest, 'consumer_input_hash')
    return data


def safe_relative(value: str) -> str:
    path = PurePosixPath(value)
    require(value and path.as_posix() == value and not path.is_absolute() and '..' not in path.parts and
            '\\' not in value, 'consumer_relative_path')
    return value


def admit_producer_stage(plan_bytes: bytes, plan_hash: str, receipt_bytes: bytes, receipt_hash: str) -> tuple[dict, dict, dict]:
    """Historical admission derives its clock only from the reviewed successful receipt."""
    require(metadata.sha256(receipt_bytes) == receipt_hash, 'consumer_input_hash')
    receipt = planner.read_json(receipt_bytes)
    require(receipt['schema'] == 1 and receipt['mode'] == 'selected-product-artifact-control' and
            receipt['stage'] == 'complete' and receipt['success'] is True and receipt['artifact_proof'] is True and
            all(receipt[key] is False for key in ('published', 'version_allocated', 'tag_created')),
            'consumer_producer_stage_incomplete')
    validate_local_execution(receipt['execution'])
    started = receipt['execution']['started_at']
    require(datetime.fromisoformat(started) <= datetime.fromisoformat(planner.now()), 'consumer_producer_start_future')
    plan = producer.admit(plan_bytes, plan_hash, checked_at=started)
    require(receipt['plan_sha256'] == plan_hash and receipt['source'] == plan['source'] and
            receipt['planner_controller'] == plan['controller'] and
            all(receipt[key] == plan[key] for key in ('product', 'line')) and
            receipt['version'] == plan['requested_version'], 'consumer_producer_identity')
    admission = {'scope': 'historical-producer-start-only', 'eligible_at_producer_start': True,
                 'plan_observed_at': plan['observed_at'], 'producer_started_at': started,
                 'producer_execution_id': receipt['execution']['id'], 'artifact_receipt_sha256': receipt_hash}
    return plan, receipt, admission


def verify_producer_controllers(root: Path, plan: dict, receipt: dict) -> None:
    require(receipt.get('planner_controller') == plan['controller'], 'consumer_planner_controller_identity')
    artifact = receipt.get('artifact_controller', {})
    require(isinstance(artifact, dict) and set(artifact) == {'commit', 'tree'} and
            all(isinstance(artifact[key], str) and re.fullmatch(r'[a-f0-9]{40}', artifact[key]) for key in artifact),
            'consumer_artifact_controller_identity')
    for controller in (plan['controller'], artifact):
        require(metadata.git(root, 'rev-parse', controller['commit'] + '^{tree}') == controller['tree'],
                'consumer_producer_controller_tree')
        for path, expected in plan['controller']['input_sha256'].items():
            require(metadata.sha256(producer.maintenance.git_bytes(root, controller['commit'], path)) == expected,
                    'consumer_producer_planner_inputs')


def admit_artifacts(plan: dict, plan_hash: str, receipt: dict, artifacts: Path, root: Path) -> dict:
    require(receipt['success'] is True and receipt['artifact_proof'] is True and receipt['published'] is False and
            receipt['plan_sha256'] == plan_hash and receipt['source'] == plan['source'] and
            all(receipt[key] == plan[key] for key in ('product', 'line')) and
            receipt['version'] == plan['requested_version'], 'consumer_producer_identity')
    validate_local_execution(receipt['execution'])
    verify_producer_controllers(root, plan, receipt)
    selected = {row['id'].casefold(): row for row in plan['inventory']['selected']}
    expected = {name for name in plan['expected_artifacts'] if name.endswith(('.nupkg', '.snupkg'))}
    records = receipt['packages']['selected']
    require(len(records) == len(expected) and {row['file'] for row in records} == expected and
            {path.name for path in artifacts.iterdir()} == expected, 'consumer_artifact_bijection')
    result = {}
    for row in records:
        name = safe_relative(row['file'])
        require('/' not in name, 'consumer_artifact_name')
        data = read_bound(artifacts / name, row['sha256'])
        require(len(data) == row['size'] and row['version'] == plan['requested_version'] and
                row['id'].casefold() in selected, 'consumer_artifact_identity')
        with zipfile.ZipFile(artifacts / name) as archive:
            names = archive_names(archive)
            inventory = [{'path': item, 'size': len(archive.read(item)), 'sha256': metadata.sha256(archive.read(item))}
                         for item in sorted(names)]
            require(inventory == row['inventory'], 'consumer_archive_inventory')
            package = nuspec(archive)
            repository = package.find('repository')
            require(package.findtext('id') == row['id'] and package.findtext('version') == row['version'] and
                    repository is not None and repository.get('commit') == plan['source']['commit'], 'consumer_archive_provenance')
            policy = selected[row['id'].casefold()]
            require(dependency_groups(package) == policy['metadata']['dependency_groups'], 'consumer_archive_dependencies')
            if name.endswith('.nupkg'):
                result[row['id'].casefold()] = {'id': row['id'], 'nupkg': name, 'nupkg_sha256': row['sha256'],
                    'nupkg_sha512': hashlib.sha512(data).hexdigest(), 'content_hash': consumers.base64_sha512(data),
                    'dependency_groups': dependency_groups(package), 'inventory': inventory, 'policy': policy}
    require(set(result) == set(selected), 'consumer_selected_inventory')
    return result


def load_snapshots(plan: dict, plan_hash: str, folder: Path) -> dict:
    path = folder / 'receipt.private.json'
    require(path.is_file() and not any(part.is_symlink() for part in (path, *path.parents)), 'consumer_snapshot_path')
    receipt = planner.read_json(path.read_bytes())
    require(receipt['schema'] == 1 and receipt['mode'] == 'private-original-planning-assets-snapshot' and
            receipt['plan_sha256'] == plan_hash and receipt['planner_controller'] == plan['controller'] and
            receipt['source'] == plan['source'] and receipt['product'] == plan['product'] and receipt['line'] == plan['line'] and
            receipt['version'] == plan['requested_version'] and receipt['product_build_executed'] is False and
            receipt['publication'] is False, 'consumer_snapshot_identity')
    selected = {row['project']: row for row in plan['inventory']['selected']}
    rows = receipt['selected']
    require(len(rows) == len(selected) and {row['project'] for row in rows} == set(selected), 'consumer_snapshot_partition')
    result = {}
    for row in rows:
        policy = selected[row['project']]
        require(row['id'] == policy['id'] and row['frameworks'] == policy['frameworks'] and
                row['sha256'] == policy['metadata']['restore_assets_sha256'], 'consumer_snapshot_project')
        data = read_bound(folder / safe_relative(row['file']), row['sha256'])
        require(len(data) == row['bytes'], 'consumer_snapshot_bytes')
        assets = planner.read_json(data)
        require(all(framework in assets['targets'] for framework in policy['frameworks']), 'consumer_snapshot_frameworks')
        result[row['project']] = assets
    return result


def render_config(artifacts: Path, graph: dict, policy: dict) -> str:
    root = ET.Element('configuration')
    sources = ET.SubElement(root, 'packageSources')
    ET.SubElement(sources, 'clear')
    ET.SubElement(sources, 'add', key=LOCAL, value=str(artifacts.resolve()))
    for name, url in sorted(policy['sources'].items()):
        ET.SubElement(sources, 'add', key=name, value=url)
    mapping = ET.SubElement(root, 'packageSourceMapping')
    groups = {LOCAL: ET.SubElement(mapping, 'packageSource', key=LOCAL)}
    for name in policy['sources']:
        groups[name] = ET.SubElement(mapping, 'packageSource', key=name)
    for folded, row in sorted(graph.items()):
        names = [LOCAL] if row['selected'] else policy['mapping'][folded]
        for name in names:
            ET.SubElement(groups[name], 'package', pattern=row['id'])
    return ET.tostring(root, encoding='unicode')


def validate_restored(assets: dict, framework: str, graph: dict, root: Path, cache: Path, artifacts: Path,
                      policy: dict) -> None:
    require(set(assets['targets']) == {framework}, 'consumer_restore_framework')
    actual = {}
    for key, item in assets['targets'][framework].items():
        identifier, version = key.rsplit('/', 1)
        folded = identifier.casefold()
        require(folded not in actual and folded in graph and version == graph[folded]['version'], 'consumer_restore_version')
        require(item['type'] == 'package' and key in assets['libraries'] and
                assets['libraries'][key]['type'] == 'package', 'consumer_project_fallback')
        require(assets['libraries'][key]['sha512'] == graph[folded]['content_hash'], 'consumer_restore_content_hash')
        actual[folded] = version
    require(set(actual) == set(graph), 'consumer_restore_closure')
    restore = assets['project']['restore']
    require(set(restore['sources']) == set(policy['sources'].values()) | {str(artifacts.resolve())} and
            restore.get('configFilePaths') == [str(root / 'NuGet.Config')] and
            {str(Path(path).resolve()) for path in assets['packageFolders']} == {str(cache.resolve())} and
            not restore.get('fallbackFolders'), 'consumer_restore_isolation')


def verify_cache(graph: dict, policy: dict, cache: Path, artifacts: Path, selected: dict,
                 restored: dict, inspector: Path, catalog: dict) -> list[dict]:
    records = []
    for folded, row in sorted(graph.items()):
        folder = cache / folded / row['version']
        if row['selected']:
            record = consumers.verify_cached_package(row['id'], row['version'], artifacts / selected[folded]['nupkg'], cache, artifacts)
            record['source'] = LOCAL
        else:
            archive = folder / f"{folded}.{row['version']}.nupkg"
            require(archive.is_file() and not any(part.is_symlink() for part in (archive, *archive.parents)),
                    'consumer_external_archive_path')
            content = archive.read_bytes()
            expected = catalog[(folded, row['version'])]
            _, verified = archives.restored_archive(restored, row['id'], row['version'],
                                                     cache={'archive_inspector': inspector})
            require(verified['archive_sha256'] == expected['archive_sha256'] and
                    verified['nuget_content_hash'] == row['content_hash'], 'consumer_external_archive_hash')
            cache_metadata = planner.read_json((folder / '.nupkg.metadata').read_bytes())
            allowed = {policy['sources'][name] for name in policy['mapping'][folded]}
            require(cache_metadata.get('source') in allowed, 'consumer_external_cache_source')
            sha512 = folder / f"{folded}.{row['version']}.nupkg.sha512"
            require(sha512.read_text().strip() == consumers.base64_sha512(content), 'consumer_external_cache_hash')
            record = {'id': row['id'], 'version': row['version'], 'source': cache_metadata['source'],
                      'sha256': verified['archive_sha256'], 'archive_sha512': hashlib.sha512(content).hexdigest(),
                      'nuget_content_hash': verified['nuget_content_hash'], 'signed': verified['signed']}
        records.append(record)
    return records


def verify_asset_payloads(assets: dict, framework: str, graph: dict, cache: Path, artifacts: Path, selected: dict) -> list[dict]:
    evidence = []
    for key, library in assets['targets'][framework].items():
        identifier, version = key.rsplit('/', 1)
        folded = identifier.casefold()
        folder = cache / folded / version
        archive = artifacts / selected[folded]['nupkg'] if graph[folded]['selected'] else folder / f'{folded}.{version}.nupkg'
        payloads = []
        with zipfile.ZipFile(archive) as package:
            names = archive_names(package)
            for kind in ('compile', 'runtime', 'contentFiles', 'build', 'buildMultiTargeting', 'native', 'runtimeTargets', 'resource'):
                for entry in library.get(kind, {}):
                    safe_relative(entry)
                    require(entry in names, 'consumer_asset_archive_member')
                    content = package.read(entry)
                    read_bound(folder / entry, metadata.sha256(content))
                    payloads.append({'path': entry, 'kind': kind, 'sha256': metadata.sha256(content), 'size': len(content)})
        evidence.append({'id': identifier, 'version': version, 'payloads': payloads})
    return evidence


def render_project(identifier: str, version: str, framework: str, references: list[str], *, executable: bool, managed: bool, locked: bool = True) -> str:
    project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
    properties = ET.SubElement(project, 'PropertyGroup')
    for name, value in {'TargetFramework': framework, 'OutputType': 'Exe' if executable else 'Library',
                        'ImplicitUsings': 'enable', 'Nullable': 'enable', 'RestorePackagesWithLockFile': 'true',
                        'RestoreLockedMode': str(locked).lower(), 'NuGetAudit': 'false'}.items():
        ET.SubElement(properties, name).text = value
    group = ET.SubElement(project, 'ItemGroup')
    package = ET.SubElement(group, 'PackageReference', Include=identifier, Version=f'[{version}]')
    if managed and not executable:
        package.set('Aliases', 'selected')
    for name in references:
        ET.SubElement(group, 'FrameworkReference', Include=name)
    return ET.tostring(project, encoding='unicode')


def validate_ledger(plan: dict, ledger: list[dict]) -> None:
    expected = {(row['id'], framework) for row in plan['inventory']['selected'] for framework in row['frameworks']}
    actual = [(row['id'], row['framework']) for row in ledger]
    require(len(actual) == len(expected) and set(actual) == expected and all(row['success'] is True for row in ledger),
            'consumer_coverage_ledger')


def retain_runtime_rows(verified: list[dict]) -> list[dict]:
    return [{key: row[key] for key in ('name', 'version', 'informationalVersion', 'sha256',
            'package_id', 'package_version', 'package_asset')} for row in verified]


def cold_environment(output: Path) -> tuple[Path, dict]:
    cache, home = output / 'packages', output / 'home'
    require(not cache.exists(), 'consumer_cache_not_empty')
    home.mkdir()
    environment = {key: os.environ[key] for key in ('PATH', 'TMPDIR', 'DOTNET_ROOT', 'DOTNET_ROOT_X64') if key in os.environ}
    environment.update(HOME=str(home), NUGET_PACKAGES=str(cache), NUGET_HTTP_CACHE_PATH=str(output / 'http-cache'),
                       NUGET_PLUGINS_CACHE_PATH=str(output / 'plugins-cache'), DOTNET_CLI_HOME=str(home),
                       DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER='1', MSBUILDDISABLENODEREUSE='1',
                       DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1')
    return cache, environment


def discover(plan: dict, package: dict, framework: str, selected: dict, artifacts: Path,
             semantics: planner.Semantics, catalog: dict, original_policy: dict, output: Path,
             references: list[str], *, runtime: bool, managed: bool) -> tuple[dict, dict, bytes]:
    output.mkdir()
    consumers.prepare_isolation(output, metadata.SDK)
    universe = {folded: {'id': row['id'], 'selected': True} for folded, row in selected.items()}
    universe.update({folded: {'id': row['id'], 'selected': False} for (folded, _), row in catalog.items()})
    policy = {'sources': original_policy['mirrors'], 'mapping': original_policy['mapping']}
    (output / 'NuGet.Config').write_text(render_config(artifacts, universe, policy))
    project = output / 'Consumer.csproj'
    project.write_text(render_project(package['id'], plan['requested_version'], framework, references,
                                     executable=runtime, managed=managed, locked=False))
    cache, environment = cold_environment(output)
    inputs = {path.name: metadata.sha256(path.read_bytes()) for path in output.iterdir() if path.is_file()}
    consumers._run_command(['dotnet', 'restore', str(project), '--configfile', str(output / 'NuGet.Config'),
        '--packages', str(cache), '--no-cache', '--nologo'], output, environment, output / 'restore.log', 1200)
    require(all(metadata.sha256((output / name).read_bytes()) == digest for name, digest in inputs.items()),
            'consumer_discovery_inputs_changed')
    assets = planner.read_json((output / 'obj/project.assets.json').read_bytes())
    lock = (output / 'packages.lock.json').read_bytes()
    graph = resolution.audit_native_graph(assets, planner.read_json(lock), package['id'], framework,
                                         selected, catalog, plan['requested_version'], semantics)
    # Discovery uses finite original mirrors, and remains distinct from proof.
    validate_restored(assets, framework, graph, output, cache, artifacts, policy)
    proof_policy = {'sources': original_policy['sources'],
                    'mapping': {folded: original_policy['mapping'][folded] for folded in graph}}
    return graph, proof_policy, lock


def cell(plan: dict, package: dict, framework: str, selected: dict, artifacts: Path,
         semantics: planner.Semantics, output: Path, *, catalog: dict,
         original_policy: dict, inspector: Path, runtime: bool = False) -> dict:
    output.mkdir()
    isolation = consumers.prepare_isolation(output, metadata.SDK)
    project = output / 'Consumer.csproj'
    output_policy = package['policy']['metadata']['original_output_policy'][framework]
    managed = output_policy['IncludeBuildOutput'].lower() != 'false'
    references = nearest_group(package['policy']['metadata']['framework_reference_groups'], framework, semantics).get('references', [])
    graph, policy, lock_bytes = discover(plan, package, framework, selected, artifacts, semantics, catalog,
        original_policy, output / 'discovery', references, runtime=runtime, managed=managed)
    (output / 'NuGet.Config').write_text(render_config(artifacts, graph, policy))
    project.write_text(render_project(package['id'], plan['requested_version'], framework, references, executable=runtime, managed=managed))
    (output / 'packages.lock.json').write_bytes(lock_bytes)
    (output / 'Program.cs').write_text(FIXTURE.read_text() if runtime else
        ('extern alias selected;\n' if managed else '') + 'public class CompileContract {}\n')
    cache, environment = cold_environment(output)
    inputs = {path.name: metadata.sha256(path.read_bytes()) for path in output.iterdir() if path.is_file()}
    commands = []
    commands.append(consumers._run_command(['dotnet', 'restore', str(project), '--locked-mode', '--configfile', str(output / 'NuGet.Config'),
        '--packages', str(cache), '--no-cache', '--nologo'], output, environment, output / 'restore.log', 1200))
    restored = planner.read_json((output / 'obj/project.assets.json').read_bytes())
    validate_restored(restored, framework, graph, output, cache, artifacts, policy)
    require((output / 'packages.lock.json').read_bytes() == lock_bytes, 'consumer_locked_document_changed')
    resolution.audit_native_graph(restored, planner.read_json(lock_bytes), package['id'], framework,
                                  selected, catalog, plan['requested_version'], semantics)
    cached = verify_cache(graph, policy, cache, artifacts, selected, restored, inspector, catalog)
    payloads = verify_asset_payloads(restored, framework, graph, cache, artifacts, selected)
    commands.append(consumers._run_command(['dotnet', 'build', str(project), '-c', 'Release', '--no-restore', '--disable-build-servers',
        '--nologo'], output, environment, output / 'build.log', 1200))
    require(all(metadata.sha256((output / name).read_bytes()) == digest for name, digest in inputs.items()), 'consumer_inputs_changed')
    result = {'id': package['id'], 'framework': framework, 'success': True, 'fresh_cache': True, 'package_reference_only': True,
              'accounting': 'managed-reference-compile' if managed else 'output-content-only-restore-build',
              'original_output_policy': output_policy, 'restored_payloads': payloads,
              'discovery_scope': 'offline-original-archive-metadata-only',
              'native_lock_sha256': metadata.sha256(lock_bytes), 'archive_sha256': package['nupkg_sha256'], 'restored': cached, 'isolation': isolation, 'input_sha256': inputs}
    if runtime:
        consumers._run_command(['dotnet', 'run', '--project', str(project), '--no-restore', '--no-build', '-c', 'Release',
                               '--framework', framework], output, environment, output / 'runtime.log', 300)
        lines = [line.removeprefix('SELECTED_CONSUMER_PROOF=') for line in (output / 'runtime.log').read_text().splitlines()
                 if line.startswith('SELECTED_CONSUMER_PROOF=')]
        require(len(lines) == 1, 'consumer_runtime_receipt')
        proof = planner.read_json(lines[0].encode())
        require(proof['backendUriPreserved'] is True, 'consumer_runtime_contract')
        result['runtime'] = {'contract': 'Studio38 backend-options accessor preserves configured URI',
            'loaded_assemblies': retain_runtime_rows(consumers.verify_loaded_assemblies(proof, restored, framework, output,
                cache, artifacts, selected, plan['requested_version'], plan['source']['commit'],
                required_packages=('Elsa.Studio.Core',)))}
    return result


def execute(root: Path, plan_path: Path, plan_hash: str, receipt_path: Path, receipt_hash: str,
            artifacts: Path, snapshots: Path, output: Path) -> dict:
    plan, receipt, historical_admission = admit_producer_stage(read_bound(plan_path, plan_hash), plan_hash,
        read_bound(receipt_path, receipt_hash), receipt_hash)
    require(plan['product'] == 'studio' and plan['line'] == '3.8', 'consumer_control_not_implemented')
    execution = local_execution()
    controller = producer.verify_controller(root, plan)
    selected = admit_artifacts(plan, plan_hash, receipt, artifacts, root)
    originals = load_snapshots(plan, plan_hash, snapshots)
    for path, expected in STUDIO38_CONTRACT_SOURCE.items():
        require(metadata.sha256(producer.maintenance.git_bytes(root, plan['source']['commit'], path)) == expected,
                'consumer_runtime_source_contract')
    require(not output.exists() and not output.resolve().is_relative_to(root.resolve()) and
            not any(part.is_symlink() for part in (output, *output.parents)), 'consumer_output_location')
    output.mkdir(parents=True)
    private, retained = output / 'private', output / 'retained'
    private.mkdir()
    retained.mkdir()
    result = {'schema': 1, 'mode': 'selected-product-consumers', 'plan_sha256': plan_hash, 'artifact_receipt_sha256': receipt_hash,
              'source': plan['source'], 'controller': controller, 'execution': execution, 'producer_execution': receipt['execution'],
              'planner_controller': receipt['planner_controller'], 'artifact_controller': receipt['artifact_controller'],
              'producer_plan_admission': historical_admission,
              'current_consumer_admission': {'eligible': False, 'scope': 'current-complete-selected-product-prerequisites'},
              'success': False, 'published': False, 'coverage': [], 'runtime': [], 'stage': 'consumer-setup',
              'runtime_contract_source': STUDIO38_CONTRACT_SOURCE}
    try:
        config = private / 'NuGet.Config'
        config.write_bytes(producer.maintenance.git_bytes(root, plan['source']['commit'], 'NuGet.Config'))
        require(metadata.sha256(config.read_bytes()) == plan['consumer_feed_policy']['config_sha256'], 'consumer_original_feed_config')
        helper = private / 'semantics'
        helper.mkdir()
        # Tool-only preflight matches the producer; no product/consumer restore runs here.
        result['stage'] = 'consumer-semantics-preflight'
        semantics = planner.build_helper(helper)
        require(plan['semantics']['sdk_version'] == metadata.SDK and
                semantics.call('identity') == plan['semantics']['assemblies'], 'consumer_native_semantics_identity')
        result['preflight'] = {'scope': 'standalone-semantics-utility-only', 'sdk_version': metadata.SDK,
                               'native_assemblies': plan['semantics']['assemblies']}
        result['stage'] = 'current-consumer-prerequisites'
        current = producer.refresh_remote(plan, private, semantics)
        require(current['eligible'] is True, 'consumer_current_prerequisites_ineligible')
        current_bytes = json.dumps(current, indent=2, sort_keys=True).encode()
        (private / 'current-consumer-admission.private.json').write_bytes(current_bytes)
        result['current_consumer_admission'].update(eligible=True, checked_at=current['checked_at'],
            histories=len(current['histories']), prerequisites=len(current['prerequisites']),
            observations_sha256=metadata.sha256(current_bytes))
        result['stage'] = 'original-external-archive-catalog'
        inspector = resolution.build_inspector(root, private / 'archive-inspector')
        resolution.validate_native_tools(plan, semantics, inspector)
        catalog, original_policy = resolution.archive_catalog(originals, selected, config, semantics,
                                                              inspector, private / 'original-external-catalog')
        result['external_catalog_sha256'] = metadata.sha256(
            (private / 'original-external-catalog/catalog.private.json').read_bytes())
        for package in selected.values():
            for framework in package['policy']['frameworks']:
                result['stage'] = 'selected-restore-compile'
                result['focus'] = {'id': package['id'], 'framework': framework}
                name = metadata.sha256((package['id'] + '/' + framework).encode())[:16]
                result['coverage'].append(cell(plan, package, framework, selected, artifacts, semantics,
                    private / name, catalog=catalog, original_policy=original_policy, inspector=inspector))
        validate_ledger(plan, result['coverage'])
        result['stage'] = 'studio-runtime-contract'
        require('elsa.studio.core' in selected, 'consumer_studio_representative_missing')
        package = selected['elsa.studio.core']
        for framework in package['policy']['frameworks']:
            result['focus'] = {'id': package['id'], 'framework': framework}
            result['runtime'].append(cell(plan, package, framework, selected, artifacts, semantics,
                private / ('runtime-' + framework), catalog=catalog, original_policy=original_policy,
                inspector=inspector, runtime=True))
        result.pop('focus', None)
        result.update(success=True, stage='complete', limitations=[
            'Complete selected restore/compile coverage is distinct from representative runtime behavior.',
            'Backend accessor contract does not certify Studio browser/deployed behavior or all package functionality.'])
        return result
    except Exception:
        result['failure_code'] = result['stage'] + '-failed'
        raise
    finally:
        (retained / 'receipt.json').write_text(json.dumps(result, indent=2, sort_keys=True) + '\n')


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('plan', 'artifact-receipt', 'artifacts', 'planning-assets', 'output'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--plan-sha256', required=True)
    parser.add_argument('--artifact-receipt-sha256', required=True)
    args = parser.parse_args()
    try:
        execute(ROOT, args.plan, args.plan_sha256, args.artifact_receipt, args.artifact_receipt_sha256,
                args.artifacts, args.planning_assets, args.output)
        return 0
    except Exception:
        print('Selected consumer control failed; raw diagnostics remain private.')
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
