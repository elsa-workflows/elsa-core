#!/usr/bin/env python3
"""Package-only bounded Core, Studio and Extensions consumers of selected archives.

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
import selected_extensions_contract as extensions
import selected_core_consumer as core
import selected_maintenance_39 as maintenance39
from selected_consumer_cache_retirement import retire_successful_cell_caches
from selected_product_diagnostics import prerequisite_context, original_build_group_context
from selected_product_consumer_metadata import nearest_group
from product_artifact_execution import selected_execution, validate_selected_execution
from prove_consolidated_packages import archive_names, dependency_groups, framework_reference_groups, metadata as nuspec, require

ROOT = Path(__file__).resolve().parents[2]
FIXTURE = ROOT / 'scripts/integration-program/selected-studio-consumer/Program.cs'
LOCAL = 'selected-local-archives'
EMPTY_CONTENT = 'contentFiles/any/any/_._'
EMPTY_CONTENT_METADATA = {'buildAction': 'None', 'codeLanguage': 'any', 'copyToOutput': False}
EMPTY_CONTENT_ACCOUNTING = 'native-synthetic-excluded-content'
EMPTY_BUILD_ACCOUNTING = 'native-synthetic-excluded-build'
EMPTY_BUILD_ORIGIN = 'selected-root-nuspec-build-exclusion'
ORIGINAL_EXCLUDED_BUILD_ACCOUNTING = 'native-original-excluded-flat-build'
ORIGINAL_EXCLUDED_BUILD_ORIGIN = 'original-excluded-flat-build'
DEPENDENCY_FLAT_BUILD_ACCOUNTING = 'native-selected-dependency-excluded-flat-build'
DEPENDENCY_FLAT_BUILD_ORIGIN = 'selected-dependency-original-flat-build'
ROOT_FLAT_BUILD_ACCOUNTING = 'native-selected-root-excluded-flat-build'
ROOT_FLAT_BUILD_ORIGIN = 'selected-root-nuspec-flat-build-exclusion'
FLAT_BUILD_ROOT = 'Elsa.Caching.Distributed.ProtoActor'
FLAT_BUILD_WITNESS = 'Elsa.Actors.ProtoActor'
FLAT_BUILD_ROOTS = {
    FLAT_BUILD_ROOT: 'src/modules/caching/Elsa.Caching.Distributed.ProtoActor/Elsa.Caching.Distributed.ProtoActor.csproj',
    'Elsa.Workflows.Runtime.ProtoActor': 'src/modules/runtimes/Elsa.Workflows.Runtime.ProtoActor/Elsa.Workflows.Runtime.ProtoActor.csproj'}
DIRECT_FLAT_BUILD_ACCOUNTING = 'native-direct-reference-excluded-flat-build'
DIRECT_FLAT_BUILD_ORIGIN = 'direct-reference-nuspec-flat-build-exclusion'
# Actual #8693 archive/nuspec witnesses, not an open package/route-flags policy.
# Remove with the selected-control adapter. Native nearest groups were observed
# for all three supported TFMs; runtime rechecks the frozen archives and graph.
DIRECT_FLAT_CASES = {
    ('core', 'Elsa.AI.Copilot', 'GitHub.Copilot.SDK/1.0.0'): {
        'project': 'src/modules/Elsa.AI.Copilot/Elsa.AI.Copilot.csproj',
        'frameworks': ('net8.0', 'net9.0', 'net10.0'),
        'sources': {'3.8': ('7e5e6bcf97791e4f7f7165e15579abae18ec7203', 'd5c10535cee3524d91f5ae54b5cecb967c26b606'),
                    '3.9': ('86fffea6da3cfe67c0279f552c2a32940ef75ae2', '8beb9e093091c098f542a08e82e43625a3b8e520')}},
    ('extensions', 'Elsa.Logging.Serilog', 'Serilog/4.3.0'): {
        'project': 'src/modules/diagnostics/Elsa.Logging.Serilog/Elsa.Logging.Serilog.csproj',
        'frameworks': ('net8.0', 'net9.0', 'net10.0'),
        'sources': {'3.8': ('20c7a51599b4c85bf591c4615d34674e0f5706bf', '421c27c3365dd28e447a9beec14edec69541b9be'),
                    '3.9': ('e4935c95e211f2885c0348a4ab9872f112df7b4a', '27c3e51e6a180174bed5424b3c1cf975e7e32e6c')}}}
DIRECT_FLAT_CASES[('core', 'Elsa.Api.Common', 'FastEndpoints.Swagger/8.2.0')] = {
    'project': 'src/common/Elsa.Api.Common/Elsa.Api.Common.csproj', 'frameworks': ('net10.0',),
    'sources': DIRECT_FLAT_CASES[('core', 'Elsa.AI.Copilot', 'GitHub.Copilot.SDK/1.0.0')]['sources']}
DIRECT_FLAT_PARENTS = (
    {'id': 'Serilog.Extensions.Logging', 'version': '10.0.0', 'range': '4.2.0',
     'archive_sha256': '3207d62bf4895943f13f3ae70949f19fa4b2f9205598fddd61deb4bb2f3435d1',
     'nuspec_sha256': '66df1d930a2694b0499f439ad28feb088706b377974903e2e4418305f6a5472c',
     'groups': {'net8.0': 'net8.0', 'net9.0': 'net9.0', 'net10.0': 'net10.0'}},
    {'id': 'Serilog.Formatting.Compact', 'version': '3.0.0', 'range': '4.0.0',
     'archive_sha256': '9de8c462a244306f4fda2159bdbb0250faf92d946dc5b7544cb088f4def58c76',
     'nuspec_sha256': 'ae4ccc1b4a71d372d63af397e740e5c18426aab45cc660486beabb372011b138',
     'groups': {'net8.0': 'net8.0', 'net9.0': 'net8.0', 'net10.0': 'net8.0'}},
    {'id': 'Serilog.Sinks.File', 'version': '7.0.0', 'range': '4.2.0',
     'archive_sha256': '2f165852850f9028c82156ab2629675e7a9088cac5349b68462966cd336d523a',
     'nuspec_sha256': '269ff2dccec088353ff0f9a97b1f2636228d83a072a462a74fd6d68177877fbc',
     'groups': {'net8.0': 'net8.0', 'net9.0': 'net9.0', 'net10.0': 'net9.0'}})

ORIGINAL_EMPTY_LIB_ACCOUNTING = 'native-original-excluded-lib'
ORIGINAL_EMPTY_LIB_ORIGIN = 'original-excluded-lib'
STUDIO38_CONTRACT_SOURCE = {
    'src/framework/Elsa.Studio.Core/Services/DefaultRemoteBackendAccessor.cs': '2818998450af0126555755dec8f2ff45fe582c6558ac09c064c5053e761bb909',
    'src/framework/Elsa.Studio.Core/Options/BackendOptions.cs': 'd640f27152eb46631bc157733c4e8284cae845923d717f6fdb9df9235466c538',
    'src/framework/Elsa.Studio.Core/Models/RemoteBackend.cs': 'c96cacf7fc3d755cff76861d2e837fc35aa359b2779137ee6cae99253a33f1e4',
}

# Public diagnostics use exact fixed labels, never arbitrary exception text.
PUBLIC_FAILURE_CODES = producer.PUBLIC_FAILURE_CODES | frozenset((
    'consumer_archive_dependencies consumer_archive_framework_references consumer_archive_inventory '
    'consumer_archive_provenance consumer_artifact_bijection consumer_artifact_controller_identity '
    'consumer_artifact_identity consumer_artifact_name consumer_asset_archive_member consumer_asset_archive_path '
    'consumer_cache_not_empty consumer_cache_retirement_directory consumer_cache_retirement_failed '
    'consumer_cache_retirement_file consumer_cache_retirement_identity consumer_cache_retirement_incomplete '
    'consumer_cache_retirement_ownership consumer_cache_retirement_path consumer_cache_retirement_symlink '
    'consumer_cache_retirement_unsafe_platform consumer_control_not_implemented consumer_coverage_ledger '
    'consumer_current_prerequisites_ineligible consumer_dependency_framework consumer_dependency_range_conflict '
    'consumer_discovery_inputs_changed consumer_execution_stage_kind consumer_external_archive_hash '
    'consumer_external_archive_path consumer_external_cache_hash consumer_external_cache_source '
    'consumer_external_catalog_identity consumer_external_feed_mapping consumer_feed_inventory consumer_feed_origin '
    'consumer_input_hash consumer_input_path consumer_inputs_changed consumer_locked_document_changed '
    'consumer_mirror_archive_changed consumer_native_content_hash consumer_native_declared_edges '
    'consumer_native_dependency_range_changed consumer_native_graph_framework consumer_native_inspector_identity '
    'consumer_native_library_partition consumer_native_lock_duplicate consumer_native_lock_identity '
    'consumer_native_lock_partition consumer_native_missing_nuspec_edge consumer_native_project_fallback '
    'consumer_native_pruned_edge_retained consumer_native_root_range consumer_native_selected_version '
    'consumer_native_semantics_identity consumer_native_unreachable_package consumer_native_unreviewed_external '
    'consumer_original_archive_ambiguity consumer_original_archive_candidates consumer_original_archive_collision '
    'consumer_original_archive_path consumer_original_cache_source consumer_original_build_archive consumer_original_build_assets_hash '
    'consumer_original_build_boundary consumer_original_build_cache consumer_original_build_collision '
    'consumer_original_build_graph consumer_original_build_group consumer_original_build_incoming '
    'consumer_original_build_origin consumer_original_build_original_boundary consumer_original_build_project '
    'consumer_original_build_real_group consumer_original_build_real_member consumer_original_build_selected '
    'consumer_original_lib_archive consumer_original_lib_boundary consumer_original_lib_cache '
    'consumer_original_lib_collision consumer_original_lib_context consumer_original_lib_group '
    'consumer_original_lib_inventory consumer_original_lib_metadata consumer_original_lib_origin '
    'consumer_original_lib_path consumer_original_lib_selected '
    'consumer_original_feed_config consumer_original_nuspec_identity consumer_original_selected_registry_package '
    'consumer_output_location consumer_planner_controller_identity consumer_producer_controller_tree '
    'consumer_producer_identity consumer_producer_planner_inputs consumer_producer_stage_incomplete '
    'consumer_producer_start_future consumer_project_fallback consumer_relative_path consumer_representative_missing '
    'consumer_restore_closure consumer_restore_content_hash consumer_restore_framework consumer_restore_isolation '
    'consumer_restore_version consumer_runtime_contract consumer_runtime_receipt consumer_runtime_source_contract '
    'consumer_sdk_ambient_targeting_root consumer_sdk_archive_candidates consumer_sdk_archive_collision '
    'consumer_sdk_archive_identity consumer_sdk_archive_path consumer_sdk_cache_metadata_path consumer_sdk_cache_path '
    'consumer_sdk_download_bytes consumer_sdk_download_not_https consumer_sdk_download_source '
    'consumer_sdk_download_version consumer_sdk_duplicate_download consumer_sdk_nuspec_count '
    'consumer_sdk_original_download consumer_sdk_original_pruning consumer_sdk_prune_range '
    'consumer_sdk_restore_policy_changed consumer_sdk_unreviewed_download consumer_selected_inventory '
    'consumer_snapshot_bytes consumer_snapshot_frameworks consumer_snapshot_identity consumer_snapshot_partition '
    'consumer_snapshot_path consumer_snapshot_project consumer_synthetic_build_archive '
    'consumer_synthetic_build_boundary consumer_synthetic_build_cache consumer_synthetic_build_collision '
    'consumer_synthetic_build_dependency consumer_synthetic_build_dependency_group consumer_synthetic_build_flags '
    'consumer_synthetic_build_graph consumer_synthetic_build_incoming consumer_synthetic_build_origin '
    'consumer_synthetic_build_original_flags consumer_synthetic_build_original_group '
    'consumer_synthetic_build_original_member consumer_synthetic_build_selected consumer_synthetic_content_archive '
    'consumer_synthetic_content_boundary consumer_synthetic_content_cache consumer_synthetic_content_collision '
    'consumer_synthetic_content_inventory consumer_synthetic_content_origin consumer_synthetic_content_selected '
    'core_consumer_fixture_hash core_consumer_native_archive core_consumer_native_assembly '
    'core_consumer_native_assembly_identity core_consumer_native_assembly_inventory '
    'core_consumer_native_dll_inventory core_consumer_native_frameworks core_consumer_native_inventory '
    'core_consumer_runtime_frameworks core_consumer_runtime_packages core_consumer_source_contract'
).split())
PUBLIC_FAILURE_CODES |= frozenset((
    'consumer_flat_build_archive consumer_flat_build_boundary consumer_flat_build_cache '
    'consumer_flat_build_case consumer_flat_build_catalog_context consumer_flat_build_collision '
    'consumer_flat_build_flags consumer_flat_build_graph consumer_flat_build_group '
    'consumer_flat_build_incoming consumer_flat_build_marker consumer_flat_build_original_flags '
    'consumer_flat_build_original_group consumer_flat_build_prerequisite consumer_flat_build_private_omission '
    'consumer_flat_build_private_override consumer_flat_build_project consumer_flat_build_real_group '
    'consumer_flat_build_real_member consumer_flat_build_root consumer_flat_build_root_snapshot '
    'consumer_flat_build_selected_path consumer_flat_build_witness consumer_flat_build_witness_context '
).split())

PUBLIC_FAILURE_STAGES = frozenset(('consumer-setup', 'consumer-semantics-preflight', 'current-consumer-prerequisites',
    'original-external-archive-catalog', 'selected-restore-compile', 'core-runtime-contract',
    'studio-runtime-contract', 'extensions-runtime-contract'))


def runtime_contract(plan: dict) -> dict:
    """Return the product's representative runtime fixture, checks, and assembly policy."""
    if plan['product'] in ('studio', 'extensions') and plan.get('line') == '3.9':
        maintenance39.validate_plan(plan)
    if plan['product'] == 'core':
        core.validate_plan(plan)
        return core.runtime_contract()
    if plan['product'] == 'extensions':
        return {'package': 'elsa.io.http', 'fixture': ROOT / 'scripts/integration-program/selected-extensions-consumer/Program.cs',
                'checks': {'httpFactoryRegistered': True, 'urlContentResolved': True, 'networkRequests': 0},
                'required_packages': ('Elsa.IO.Http', 'Elsa.IO'), 'assembly_release_version': '1.0.0',
                'description': ('Extensions39' if plan.get('line') == '3.9' else 'Extensions38') +
                    ' HTTP shell registration and URL binary content with fake handler',
                'limitation': 'HTTP API probe does not certify full shell composition, marker features or all product functionality.'}
    require(plan['product'] == 'studio', 'consumer_control_not_implemented')
    return {'package': 'elsa.studio.core', 'fixture': FIXTURE, 'checks': {'backendUriPreserved': True},
            'required_packages': ('Elsa.Studio.Core',), 'assembly_release_version': plan['requested_version'],
            'description': ('Studio39' if plan.get('line') == '3.9' else 'Studio38') +
                ' backend-options accessor preserves configured URI',
            'limitation': 'Backend accessor contract does not certify Studio browser/deployed behavior or all package functionality.'}


def read_bound(path: Path, digest: str) -> bytes:
    """Read a regular input file only if its path is symlink-free and its digest matches."""
    require(path.is_file() and not any(part.is_symlink() for part in (path, *path.parents)), 'consumer_input_path')
    data = path.read_bytes()
    require(metadata.sha256(data) == digest, 'consumer_input_hash')
    return data


def safe_relative(value: str) -> str:
    """Require a canonical relative POSIX path without traversal or backslashes."""
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
    validate_selected_execution(receipt['execution'], 'artifact', receipt.get('artifact_controller', {}),
                                planner.read_json(plan_bytes), plan_hash)
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
    """Verify producer controller trees and the planner inputs committed in each tree."""
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
    """Bind successful producer evidence and exact archive bytes to the selected plan."""
    if plan['product'] == 'core':
        producer.core.validate_plan(plan)
    require(receipt['success'] is True and receipt['artifact_proof'] is True and receipt['published'] is False and
            receipt['plan_sha256'] == plan_hash and receipt['source'] == plan['source'] and
            all(receipt[key] == plan[key] for key in ('product', 'line')) and
            receipt['version'] == plan['requested_version'], 'consumer_producer_identity')
    validate_selected_execution(receipt['execution'], 'artifact', receipt.get('artifact_controller', {}), plan, plan_hash)
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
            if plan['product'] == 'core':
                require(producer.core.repository_matches(repository, policy, plan['source'], canonical_url_matches=
                    repository.get('url', '').removesuffix('.git').rstrip('/') == 'https://github.com/elsa-workflows/elsa-core'),
                    'consumer_archive_provenance')
            require(dependency_groups(package) == policy['metadata']['dependency_groups'], 'consumer_archive_dependencies')
            require(framework_reference_groups(package) == policy['metadata'].get('framework_reference_groups', []),
                    'consumer_archive_framework_references')
            if name.endswith('.nupkg'):
                result[row['id'].casefold()] = {'id': row['id'], 'nupkg': name, 'nupkg_sha256': row['sha256'],
                    'nupkg_sha512': hashlib.sha512(data).hexdigest(), 'content_hash': consumers.base64_sha512(data),
                    'dependency_groups': dependency_groups(package), 'inventory': inventory, 'policy': policy}
    require(set(result) == set(selected), 'consumer_selected_inventory')
    if plan['product'] == 'core':
        for item in result.values():
            item['artifact_files'] = [{'name': row['file'], 'sha256': row['sha256'], 'size': row['size']}
                                      for row in records if row['id'] == item['id']]
        core.bind_assemblies(plan, receipt, result)
        core.validate_runtime(result)
    return result


def load_snapshots(plan: dict, plan_hash: str, folder: Path) -> dict:
    """Load original restore assets after verifying their receipt, hashes, and framework joins."""
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
        require(all(framework in assets['targets'] and framework in assets.get('project', {}).get('frameworks', {})
                    for framework in policy['frameworks']), 'consumer_snapshot_frameworks')
        result[row['project']] = assets
    return result


def render_config(artifacts: Path, graph: dict, policy: dict) -> str:
    """Render an isolated NuGet configuration with sources mapped to the admitted graph."""
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
    for group in groups.values():
        if not len(group):
            mapping.remove(group)
    return ET.tostring(root, encoding='unicode')


def validate_restored(assets: dict, framework: str, graph: dict, root: Path, cache: Path, artifacts: Path,
                      policy: dict) -> None:
    """Require the expected restored graph, sources, configuration, and exclusive cache."""
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
    """Verify cached packages against selected archives or the original external catalog."""
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


def synthetic_content_marker(assets: dict, original: dict, framework: str, key: str,
                             package: zipfile.ZipFile, names: list[str], folder: Path, catalog: dict,
                             graph: dict) -> dict:
    """Account for NuGet's excluded-content sentinel, never as archived bytes."""
    identifier, version = key.rsplit('/', 1)
    record = catalog.get((identifier.casefold(), version))
    expected = {EMPTY_CONTENT: EMPTY_CONTENT_METADATA}
    require(record is not None and original is not None and
        assets['targets'][framework][key]['contentFiles'] == expected and
        type(assets['targets'][framework][key]['contentFiles'][EMPTY_CONTENT]['copyToOutput']) is bool,
        'consumer_synthetic_content_origin')
    direct = assets.get('project', {}).get('frameworks', {}).get(framework, {}).get('dependencies', {})
    require(type(direct) is dict and len(direct) == 1, 'consumer_synthetic_content_boundary')
    root, dependency = next(iter(direct.items()))
    require(root.casefold() in graph and graph[root.casefold()]['selected'] and
        type(dependency) is dict and set(dependency) <= {'target', 'version', 'aliases'} and
        dependency.get('target') == 'Package' and dependency.get('version') ==
        '[{0}, {0}]'.format(graph[root.casefold()]['version']) and
        dependency.get('aliases', 'selected') == 'selected', 'consumer_synthetic_content_boundary')
    before, current = original.get('libraries', {}).get(key, {}), assets['libraries'][key]
    require(before.get('type') == current.get('type') == 'package' and
        before.get('sha512') == current.get('sha512') == record['content_hash'] and
        metadata.sha256(Path(package.filename).read_bytes()) == record['archive_sha256'],
        'consumer_synthetic_content_archive')
    require(not any(name.casefold() == EMPTY_CONTENT.casefold() for name in names),
            'consumer_synthetic_content_collision')
    content = {name for name in names if name.startswith('contentFiles/') and not name.endswith('/_._')}
    require(content and content <= set(before.get('files', [])), 'consumer_synthetic_content_inventory')
    group = original.get('targets', {}).get(framework, {}).get(key, {}).get('contentFiles', {})
    if group == expected and type(group[EMPTY_CONTENT]['copyToOutput']) is bool:
        origin = 'original-excluded-content'
    else:
        require(type(group) is dict and group and set(group) <= content and
            all(type(value) is dict and type(value.get('copyToOutput')) is bool and
                type(value.get('buildAction')) is str and type(value.get('codeLanguage')) is str
                for value in group.values()), 'consumer_synthetic_content_origin')
        origin = 'package-reference-transitive-content-exclusion'
    path = folder / EMPTY_CONTENT
    require(not path.exists() and not any(part.is_symlink() for part in (path, *path.parents)),
            'consumer_synthetic_content_cache')
    return {'path': EMPTY_CONTENT, 'kind': 'contentFiles', 'accounting': EMPTY_CONTENT_ACCOUNTING,
            'metadata': dict(EMPTY_CONTENT_METADATA), 'archive_sha256': record['archive_sha256'], 'origin': origin}


def build_exclusion_edge(groups: list[dict], framework: str, identifier: str) -> dict:
    """Bound the explicit selected-root nuspec edge; do not solve include flags."""
    matching = [group for group in groups if group['framework'] == framework]
    require(len(matching) == 1, 'consumer_synthetic_build_dependency_group')
    edges = [edge for edge in matching[0]['dependencies'] if edge['id'].casefold() == identifier.casefold()]
    require(len(edges) == 1 and all(type(edges[0].get(key)) is str for key in ('id', 'version', 'include', 'exclude')),
            'consumer_synthetic_build_dependency')
    supported = {'all', 'none', 'runtime', 'compile', 'build', 'buildtransitive', 'contentfiles', 'native', 'analyzers'}
    flags = {field: {value.strip().casefold() for value in edges[0][field].split(',') if value.strip()}
             for field in ('include', 'exclude')}
    require(all(value <= supported for value in flags.values()) and
        bool(flags['exclude'] & {'build', 'buildtransitive', 'all'}), 'consumer_synthetic_build_flags')
    return edges[0]


def synthetic_build_marker(assets: dict, original: dict, framework: str, key: str,
                           package: zipfile.ZipFile, names: list[str], folder: Path, catalog: dict,
                           graph: dict, selected: dict) -> dict:
    """Narrow ClearIfExists proof: one default root, one excluding edge, real original build group."""
    identifier, version = key.rsplit('/', 1)
    record = catalog.get((identifier.casefold(), version))
    marker = f'build/{framework}/_._'
    require(record is not None and type(original) is dict and
        assets['targets'][framework][key].get('build') == {marker: {}}, 'consumer_synthetic_build_origin')
    direct = assets.get('project', {}).get('frameworks', {}).get(framework, {}).get('dependencies', {})
    require(type(direct) is dict and len(direct) == 1, 'consumer_synthetic_build_boundary')
    root, dependency = next(iter(direct.items()))
    root_key = root.casefold()
    require(root_key in selected and root_key in graph and graph[root_key]['selected'] is True and
        type(dependency) is dict and set(dependency) <= {'target', 'version', 'aliases'} and
        dependency.get('target') == 'Package' and dependency.get('version') ==
        '[{0}, {0}]'.format(graph[root_key]['version']) and dependency.get('aliases', 'selected') == 'selected',
        'consumer_synthetic_build_boundary')
    require(assets.get('projectFileDependencyGroups') == {
        framework: [root + ' >= ' + graph[root_key]['version'] + ' <= ' + graph[root_key]['version']]},
        'consumer_synthetic_build_boundary')
    targets = assets['targets'][framework]
    require(len(targets) == len(graph) and {name.rsplit('/', 1)[0].casefold() for name in targets} == set(graph) and
        all(target.get('type') == 'package' for target in targets.values()), 'consumer_synthetic_build_graph')
    incoming = []
    for name, target in targets.items():
        dependencies = target.get('dependencies', {})
        require(type(dependencies) is dict and all(type(edge) is str for edge in dependencies) and
            len({edge.casefold() for edge in dependencies}) == len(dependencies), 'consumer_synthetic_build_incoming')
        incoming.extend((name, value) for edge, value in dependencies.items() if edge.casefold() == identifier.casefold())
    require(len(incoming) == 1 and incoming[0][0] == root + '/' + graph[root_key]['version'],
            'consumer_synthetic_build_incoming')
    edge = build_exclusion_edge(selected[root_key]['dependency_groups'], framework, identifier)
    require(incoming[0][1] == edge['version'], 'consumer_synthetic_build_dependency')
    before, current = original.get('libraries', {}).get(key, {}), assets['libraries'][key]
    require(before.get('type') == current.get('type') == 'package' and
        before.get('sha512') == current.get('sha512') == record['content_hash'] and
        metadata.sha256(Path(package.filename).read_bytes()) == record['archive_sha256'],
        'consumer_synthetic_build_archive')
    original_direct = original.get('project', {}).get('frameworks', {}).get(framework, {}).get('dependencies', {})
    original_edge = original_direct.get(identifier, {})
    require(type(original_edge) is dict and original_edge.get('target') == 'Package' and
        set(original_edge) <= {'target', 'version', 'versionCentrallyManaged'} and
        original_edge.get('version') == '[' + version + ', )' and
        identifier + ' >= ' + version in original.get('projectFileDependencyGroups', {}).get(framework, []),
        'consumer_synthetic_build_original_flags')
    original_target = original.get('targets', {}).get(framework, {}).get(key, {})
    group = original_target.get('build', {})
    require(not original_target.get('buildTransitive') and not original_target.get('buildMultiTargeting'),
        'consumer_synthetic_build_original_group')
    require(type(group) is dict and group and all(type(value) is dict and value == {} for value in group.values()),
            'consumer_synthetic_build_original_group')
    for entry in group:
        safe_relative(entry)
        require(entry.isascii() and entry in names and entry in before.get('files', []) and
            entry.rsplit('/', 1)[0] == f'build/{framework}' and
            Path(entry).name.casefold() in {identifier.casefold() + '.props', identifier.casefold() + '.targets'},
            'consumer_synthetic_build_original_member')
    # Default original direct flags retain BuildTransitive; no such selected item
    # in this group proves the ClearIfExists branch even when that flag survives.
    first = sorted(group, key=lambda entry: (entry.rfind('/'), entry.casefold()))[0]
    require(first.rsplit('/', 1)[0] + '/_._' == marker and
        not any(name.casefold() == marker.casefold() for name in names), 'consumer_synthetic_build_collision')
    path = folder / marker
    require(not path.exists() and not any(part.is_symlink() for part in (path, *path.parents)) and
        not any(entry.relative_to(folder).as_posix().casefold() == marker.casefold() for entry in folder.rglob('*')),
            'consumer_synthetic_build_cache')
    return {'path': marker, 'kind': 'build', 'accounting': EMPTY_BUILD_ACCOUNTING, 'metadata': {},
            'archive_sha256': record['archive_sha256'], 'origin': EMPTY_BUILD_ORIGIN}



def flat_build_plan_context(plan: dict, framework: str, key: str, kind: str, *, witness: bool,
                            root_id: str = FLAT_BUILD_ROOT) -> dict:
    """Closed #8693 source/plan joins for the two observed flat exclusions; remove with the adapter."""
    identifier, version = key.rsplit('/', 1)
    require(plan['product'] == 'extensions' and plan['line'] in ('3.8', '3.9') and
        kind in ('build', 'buildMultiTargeting') and root_id in FLAT_BUILD_ROOTS and
        (witness or plan['line'] == '3.8' or root_id == 'Elsa.Workflows.Runtime.ProtoActor') and
        (identifier, version) == (('Grpc.Tools', '2.70.0') if witness else ('Proto.Cluster.CodeGen', '1.8.0')),
        'consumer_flat_build_case')
    if root_id == 'Elsa.Workflows.Runtime.ProtoActor':
        sources = DIRECT_FLAT_CASES[('extensions', 'Elsa.Logging.Serilog', 'Serilog/4.3.0')]['sources']
        require(sources.get(plan['line']) == (plan['source']['commit'], plan['source']['tree']), 'consumer_flat_build_case')
    policies = plan['inventory']['selected']
    roots = [row for row in policies if row['id'] == root_id]
    require(len(roots) == 1 and roots[0]['project'] == FLAT_BUILD_ROOTS[root_id] and
        framework in roots[0]['frameworks'], 'consumer_flat_build_root')
    root = roots[0]
    projects = [row for row in plan['inventory']['projects'] if row['path'] == root['project'] and row['package_id'] == root['id']]
    require(len(projects) == 1, 'consumer_flat_build_project')
    refs = [row for row in projects[0]['references_by_framework'][framework]['package_references'] if row['id'] == identifier]
    require(len(refs) == 1 and refs[0]['PrivateAssets'].casefold() == ('all' if witness else '') and
        refs[0]['IncludeAssets'] == refs[0]['ExcludeAssets'] == refs[0]['Version'] == refs[0]['VersionOverride'] == '',
        'consumer_flat_build_original_flags')
    groups = root['metadata']['dependency_groups']
    active = [row for row in groups if row['framework'] == framework]
    require(len(active) == 1, 'consumer_flat_build_group')
    result = {'root_id': root['id'], 'root_project': root['project'], 'framework': framework,
        'root_assets_sha256': root['metadata']['restore_assets_sha256'],
        'source': {key: plan['source'][key] for key in ('commit', 'tree')},
        'planner_controller_sha256': metadata.canonical_hash(plan['controller'])}
    if witness:
        require(not any(row['id'].casefold() == identifier.casefold() for row in active[0]['dependencies']),
                'consumer_flat_build_private_omission')
        witnesses = [row for row in policies if row['id'] == FLAT_BUILD_WITNESS]
        require(len(witnesses) == 1 and framework in witnesses[0]['frameworks'], 'consumer_flat_build_witness')
        other = witnesses[0]
        path = [row for row in active[0]['dependencies'] if row['id'] == other['id']]
        require(len(path) == 1 and path[0]['version'] == plan['requested_version'], 'consumer_flat_build_selected_path')
        result.update(witness_id=other['id'], witness_project=other['project'],
            witness_assets_sha256=other['metadata']['restore_assets_sha256'],
            selected_path=[root['id'], other['id']], private_override_version='2.76.0',
            incoming={'id': 'Grpc.AspNetCore', 'version': '2.70.0', 'range': '2.70.0'})
    else:
        edge = build_exclusion_edge(groups, framework, identifier)
        excluded = {flag.strip().casefold() for flag in edge['exclude'].split(',')}
        require(bool(excluded & {'build', 'all'}), 'consumer_flat_build_flags')
        prerequisites = [row for row in plan['prerequisites'] if
            (row['consumer'], row['project'], row['framework'], row['id'], row['version'], row['range']) ==
            (root['id'], root['project'], framework, identifier, version, edge['version'])]
        require(len(prerequisites) == 1 and prerequisites[0]['eligible'] is True, 'consumer_flat_build_prerequisite')
        result['edge'] = dict(edge)
    return result


def direct_flat_build_case(plan: dict, root: str, key: str, framework: str) -> bool:
    case = DIRECT_FLAT_CASES.get((plan['product'], root, key))
    return case is not None and framework in case['frameworks'] and case['sources'].get(plan['line']) == (plan['source']['commit'], plan['source']['tree'])


def direct_flat_build_plan_context(plan: dict, root_id: str, framework: str, key: str, kind: str) -> dict:
    """Reconstruct the exact direct-reference case and all fixed incoming witnesses."""
    case = DIRECT_FLAT_CASES.get((plan['product'], root_id, key))
    require(case is not None and kind == 'build' and framework in case['frameworks'] and
        case['sources'].get(plan['line']) == (plan['source']['commit'], plan['source']['tree']),
        'consumer_flat_build_case')
    identifier, version = key.rsplit('/', 1)
    roots = [row for row in plan['inventory']['selected'] if row['id'] == root_id]
    require(len(roots) == 1 and roots[0]['project'] == case['project'] and framework in roots[0]['frameworks'],
            'consumer_flat_build_root')
    root = roots[0]
    projects = [row for row in plan['inventory']['projects'] if row['path'] == case['project'] and row['package_id'] == root_id]
    require(len(projects) == 1, 'consumer_flat_build_project')
    refs = [row for row in projects[0]['references_by_framework'][framework]['package_references'] if row['id'] == identifier]
    require(len(refs) == 1 and all(refs[0][field] == '' for field in
        ('Version', 'VersionOverride', 'PrivateAssets', 'IncludeAssets', 'ExcludeAssets')), 'consumer_flat_build_original_flags')
    edge = build_exclusion_edge(root['metadata']['dependency_groups'], framework, identifier)
    require(edge == {'id': identifier, 'version': version, 'include': '', 'exclude': 'Build,Analyzers'},
            'consumer_flat_build_flags')
    prerequisites = [row for row in plan['prerequisites'] if
        (row['consumer'], row['project'], row['framework'], row['id'], row['version'], row['range']) ==
        (root_id, case['project'], framework, identifier, version, version)]
    require(len(prerequisites) == 1 and prerequisites[0]['eligible'] is True, 'consumer_flat_build_prerequisite')
    incoming = [{'id': root_id, 'version': plan['requested_version'], 'group': framework, 'edge': dict(edge)}]
    if identifier == 'Serilog':
        for parent in DIRECT_FLAT_PARENTS:
            incoming.append({key: parent[key] for key in ('id', 'version', 'archive_sha256', 'nuspec_sha256')} |
                {'group': parent['groups'][framework], 'edge': {'id': identifier, 'version': parent['range'],
                    'include': '', 'exclude': 'Build,Analyzers'}})
    return {'root_id': root_id, 'root_project': case['project'], 'framework': framework,
        'root_assets_sha256': root['metadata']['restore_assets_sha256'],
        'source': {key: plan['source'][key] for key in ('commit', 'tree')},
        'planner_controller_sha256': metadata.canonical_hash(plan['controller']), 'edge': dict(edge), 'incoming': incoming}


def verify_direct_flat_incoming(context: dict, assets: dict, graph: dict, catalog: dict,
                                selected: dict, semantics: planner.Semantics) -> None:
    """Every actual incoming edge must match its native admitted group and Build exclusion."""
    framework, identifier = context['framework'], context['edge']['id']
    expected = sorted((row['id'] + '/' + row['version'], row['edge']['version']) for row in context['incoming'])
    require(sorted(flat_build_incoming(assets['targets'][framework], identifier)) == expected,
            'consumer_flat_build_incoming')
    for row in context['incoming']:
        folded = row['id'].casefold()
        node = graph.get(folded, {})
        require(node.get('id') == row['id'] and node.get('version') == row['version'] and
            node.get('selected') is (row['id'] == context['root_id']), 'consumer_flat_build_incoming')
        if folded in selected:
            group = nearest_group(selected[folded]['dependency_groups'], framework, semantics)
        else:
            record = catalog.get((folded, row['version']), {})
            require(record.get('archive_sha256') == row['archive_sha256'] and
                record.get('nuspec_sha256') == row['nuspec_sha256'] and
                metadata.sha256(record['archive'].read_bytes()) == row['archive_sha256'], 'consumer_flat_build_archive')
            native = nearest_group(record['groups'], framework, semantics)
            require(native['framework'] == row['group'], 'consumer_flat_build_incoming')
            with zipfile.ZipFile(record['archive']) as package:
                group = next((value for value in dependency_groups(nuspec(package)) if value['framework'] == row['group']), {})
            native_edges = [value for value in native['dependencies'] if value['id'] == identifier]
            require(native_edges == [{'id': identifier, 'range': '[' + row['edge']['version'] + ', )'}],
                    'consumer_flat_build_incoming')
        require(group.get('framework') == row['group'] and
            [edge for edge in group.get('dependencies', []) if edge['id'].casefold() == identifier.casefold()] == [row['edge']],
            'consumer_flat_build_flags')
        ranges = [node.get('dependencies', {}).get(identifier), row['edge']['version']]
        checks = semantics.call('ranges', values=[{'range': value, 'version': context['edge']['version']} for value in ranges])
        require(len(checks) == 2 and all(check['satisfies'] for check in checks) and
            checks[0]['normalized'] == checks[1]['normalized'], 'consumer_flat_build_incoming')


def flat_build_incoming(targets: dict, identifier: str) -> list[tuple[str, str]]:
    incoming = []
    for key, target in targets.items():
        edges = target.get('dependencies', {})
        require(type(edges) is dict and all(type(name) is str for name in edges) and
            len({name.casefold() for name in edges}) == len(edges), 'consumer_flat_build_incoming')
        incoming.extend((key, value) for name, value in edges.items() if name.casefold() == identifier.casefold())
    return incoming


def proven_flat_build_marker(assets: dict, original: dict, framework: str, key: str, kind: str,
                             package: zipfile.ZipFile, names: list[str], folder: Path, catalog: dict,
                             graph: dict, selected: dict, plan: dict, original_policy: dict, *, witness: bool, direct_reference: bool = False,
                             semantics: planner.Semantics | None = None) -> dict:
    """Closed native proofs share only archive/cache and sole-root boundary checks."""
    root_ids = list(assets.get('project', {}).get('frameworks', {}).get(framework, {}).get('dependencies', {}))
    context = (direct_flat_build_plan_context(plan, root_ids[0] if len(root_ids) == 1 else '', framework, key, kind)
        if direct_reference else flat_build_plan_context(plan, framework, key, kind, witness=witness,
            root_id=root_ids[0] if len(root_ids) == 1 else ''))
    identifier, version = key.rsplit('/', 1)
    root = selected.get(context['root_id'].casefold(), {})
    require(root.get('policy') == next(row for row in plan['inventory']['selected'] if row['id'] == context['root_id']) and
        original is original_policy['original_assets'].get(context['root_project']) and
        original.get('project', {}).get('restore', {}).get('projectPath', '').endswith('/' + context['root_project']),
        'consumer_flat_build_root_snapshot')
    targets = assets['targets'][framework]
    marker = kind + '/_._'
    direct = assets.get('project', {}).get('frameworks', {}).get(framework, {}).get('dependencies', {})
    root_key = context['root_id'] + '/' + plan['requested_version']
    require(type(direct) is dict and set(direct) == {context['root_id']} and
        type(direct[context['root_id']]) is dict and set(direct[context['root_id']]) <= {'target', 'version', 'aliases'} and
        direct[context['root_id']].get('target') == 'Package' and
        direct[context['root_id']].get('version') == '[{0}, {0}]'.format(plan['requested_version']) and
        direct[context['root_id']].get('aliases', 'selected') == 'selected' and
        assets.get('projectFileDependencyGroups') == {framework: [context['root_id'] + ' >= ' +
            plan['requested_version'] + ' <= ' + plan['requested_version']]}, 'consumer_flat_build_boundary')
    require(len(targets) == len(graph) and {name.rsplit('/', 1)[0].casefold() for name in targets} == set(graph) and
        all(target.get('type') == 'package' for target in targets.values()) and
        graph[context['root_id'].casefold()]['selected'] is True and
        all(graph[name.rsplit('/', 1)[0].casefold()]['version'] == name.rsplit('/', 1)[1] for name in targets),
        'consumer_flat_build_graph')
    current = targets[key]
    require(current.get(kind) == {marker: {}} and not current.get('buildTransitive'), 'consumer_flat_build_marker')
    incoming = flat_build_incoming(targets, identifier)
    if witness:
        require(kind == 'build' and key not in original['targets'][framework] and key not in original['libraries'],
                'consumer_flat_build_private_override')
        override_key = identifier + '/' + context['private_override_version']
        override = original.get('project', {}).get('frameworks', {}).get(framework, {}).get('dependencies', {}).get(identifier, {})
        require(set(override) <= {'target', 'version', 'versionCentrallyManaged', 'suppressParent'} and
            override.get('target') == 'Package' and override.get('suppressParent') == 'All' and
            override.get('version') == '[' + context['private_override_version'] + ', )' and
            original['targets'][framework].get(override_key, {}).get('type') == 'package',
            'consumer_flat_build_private_override')
        witness_key = context['witness_id'] + '/' + plan['requested_version']
        require(witness_key in targets and graph[context['witness_id'].casefold()]['selected'] is True and
            targets[root_key].get('dependencies', {}).get(context['witness_id']) == plan['requested_version'],
            'consumer_flat_build_selected_path')
        require(incoming == [('Grpc.AspNetCore/2.70.0', '2.70.0')] and
            flat_build_incoming(original['targets'][framework], identifier) == incoming and
            original['targets'][framework]['Grpc.AspNetCore/2.70.0'].get('type') == 'package', 'consumer_flat_build_incoming')
        reachable = set()
        pending = [context['root_id'].casefold()]
        while pending:
            node = pending.pop()
            if node in reachable:
                continue
            reachable.add(node)
            pending.extend(name.casefold() for name in graph[node].get('dependencies', {}))
        candidates = []
        for folded, row in selected.items():
            project = row['policy']['project']
            snapshot = original_policy['original_assets'].get(project, {})
            target = snapshot.get('targets', {}).get(framework, {}).get(key, {})
            if folded in reachable and folded != context['root_id'].casefold() and target.get(kind) == {marker: {}}:
                candidates.append((row, snapshot))
        require(len(candidates) == 1 and candidates[0][0]['id'] == context['witness_id'] and
            candidates[0][0]['policy'] == next(row for row in plan['inventory']['selected'] if row['id'] == context['witness_id']),
            'consumer_flat_build_witness')
        before_assets = candidates[0][1]
        require(before_assets.get('project', {}).get('restore', {}).get('projectPath', '').endswith('/' + context['witness_project']) and
            original_policy['sdk_projects'][context['root_project']][framework] ==
                original_policy['sdk_projects'][context['witness_project']][framework] and
            not any(name.casefold() == identifier.casefold() for name in before_assets['project']['frameworks'][framework].get('dependencies', {})) and
            flat_build_incoming(before_assets['targets'][framework], identifier) == incoming and
            before_assets['targets'][framework]['Grpc.AspNetCore/2.70.0'].get('type') == 'package',
            'consumer_flat_build_witness_context')
    else:
        before_assets = original
        if direct_reference:
            require(semantics is not None, 'consumer_flat_build_incoming')
            verify_direct_flat_incoming(context, assets, graph, catalog, selected, semantics)
        else:
            require(incoming == [(root_key, context['edge']['version'])], 'consumer_flat_build_incoming')
        original_direct = original['project']['frameworks'][framework].get('dependencies', {}).get(identifier, {})
        require(set(original_direct) <= {'target', 'version', 'versionCentrallyManaged'} and
            original_direct.get('target') == 'Package' and original_direct.get('version') == '[' + version + ', )' and
            identifier + ' >= ' + version in original.get('projectFileDependencyGroups', {}).get(framework, []),
            'consumer_flat_build_original_flags')
    before_target = before_assets['targets'][framework].get(key, {})
    require(before_target.get('type') == 'package' and not before_target.get('buildTransitive') and
        (before_target.get(kind) == {marker: {}} if witness else bool(before_target.get(kind))),
        'consumer_flat_build_original_group')
    before, library = before_assets['libraries'].get(key, {}), assets['libraries'][key]
    record = catalog.get((identifier.casefold(), version))
    require(record is not None and record.get('id') == identifier and record.get('version') == version and
        before.get('type') == library.get('type') == 'package' and
        before.get('sha512') == library.get('sha512') == record['content_hash'] and
        metadata.sha256(Path(package.filename).read_bytes()) == record['archive_sha256'], 'consumer_flat_build_archive')
    catalog_project = context['witness_project'] if witness else context['root_project']
    contexts = [row for row in record.get('original_contexts', []) if row.get('project') == catalog_project and
        framework in row.get('frameworks', []) and row.get('content_hash') == record['content_hash']]
    require(len(contexts) == 1, 'consumer_flat_build_catalog_context')
    real = [name for name in names if name.casefold().startswith(kind.casefold() + '/') and name.casefold().endswith(('.props', '.targets'))]
    require(real and not any(name.casefold().startswith('buildtransitive/') for name in names), 'consumer_flat_build_real_group')
    for entry in real:
        safe_relative(entry)
        require(entry.isascii() and entry in before.get('files', []) and entry in library.get('files', []),
                'consumer_flat_build_real_member')
        read_bound(folder / entry, metadata.sha256(package.read(entry)))
    entrypoints = [entry for entry in real if Path(entry).name.casefold() in {identifier.casefold() + '.props', identifier.casefold() + '.targets'}]
    require(entrypoints and all(entry.rsplit('/', 1)[0] == kind for entry in entrypoints), 'consumer_flat_build_real_member')
    if not witness:
        require(before_target[kind] == {entry: {} for entry in entrypoints}, 'consumer_flat_build_original_group')
    first = sorted(entrypoints, key=lambda entry: (entry.rfind('/'), entry.casefold()))[0]
    require(first.rsplit('/', 1)[0] + '/_._' == marker and not any(name.casefold() == marker.casefold() for name in names),
            'consumer_flat_build_collision')
    path = folder / marker
    require(not path.exists() and not any(part.is_symlink() for part in (path, *path.parents)) and
        not any(entry.relative_to(folder).as_posix().casefold() == marker.casefold() for entry in folder.rglob('*')),
        'consumer_flat_build_cache')
    return {'path': marker, 'kind': kind, 'metadata': {}, 'archive_sha256': record['archive_sha256'],
        'accounting': DIRECT_FLAT_BUILD_ACCOUNTING if direct_reference else DEPENDENCY_FLAT_BUILD_ACCOUNTING if witness else ROOT_FLAT_BUILD_ACCOUNTING,
        'origin': DIRECT_FLAT_BUILD_ORIGIN if direct_reference else DEPENDENCY_FLAT_BUILD_ORIGIN if witness else ROOT_FLAT_BUILD_ORIGIN, 'provenance': context}


def original_excluded_build_marker(assets: dict, original: dict, framework: str, key: str, kind: str,
                                   package: zipfile.ZipFile, names: list[str], folder: Path, catalog: dict,
                                   graph: dict, selected: dict) -> dict:
    """Inherit an identical admitted original native exclusion, not a route-flags solver."""
    identifier, version = key.rsplit('/', 1)
    marker = kind + '/_._'
    require(kind in ('build', 'buildMultiTargeting') and type(original) is dict,
            'consumer_original_build_origin')
    targets = assets['targets'][framework]
    original_targets = original.get('targets', {}).get(framework, {})
    current_target, original_target = targets[key], original_targets.get(key, {})
    conditions = (
        ('original-package-missing', original_target.get('type') == 'package'),
        ('current-group-mismatch', current_target.get(kind) == {marker: {}}),
        ('original-group-mismatch', original_target.get(kind) == {marker: {}}),
        ('current-build-transitive', not current_target.get('buildTransitive')),
        ('original-build-transitive', not original_target.get('buildTransitive')))
    try:
        require(all(matches for _, matches in conditions), 'consumer_original_build_group')
    except ValueError as error:
        error._selected_build_group_failure = {'guard': 'consumer_original_build_group', 'id': identifier,
            'version': version, 'framework': framework, 'kind': kind,
            'reason': next(reason for reason, matches in conditions if not matches)}
        raise
    direct = assets.get('project', {}).get('frameworks', {}).get(framework, {}).get('dependencies', {})
    require(type(direct) is dict and len(direct) == 1, 'consumer_original_build_boundary')
    root, dependency = next(iter(direct.items()))
    folded_root = root.casefold()
    require(folded_root in selected and folded_root in graph and graph[folded_root]['selected'] is True and
        type(dependency) is dict and set(dependency) <= {'target', 'version', 'aliases'} and
        dependency.get('target') == 'Package' and dependency.get('version') ==
        '[{0}, {0}]'.format(graph[folded_root]['version']) and dependency.get('aliases', 'selected') == 'selected' and
        assets.get('projectFileDependencyGroups') == {
            framework: [root + ' >= ' + graph[folded_root]['version'] + ' <= ' + graph[folded_root]['version']]},
        'consumer_original_build_boundary')
    policy = selected[folded_root]['policy']
    original_path = original.get('project', {}).get('restore', {}).get('projectPath')
    require(type(original_path) is str and original_path.endswith('/' + policy['project']),
            'consumer_original_build_project')
    original_hash = policy['metadata']['restore_assets_sha256']
    require(type(original_hash) is str and re.fullmatch(r'[0-9a-f]{64}', original_hash),
            'consumer_original_build_assets_hash')
    require(len(targets) == len(graph) and {name.rsplit('/', 1)[0].casefold() for name in targets} == set(graph) and
        all(target.get('type') == 'package' for target in targets.values()), 'consumer_original_build_graph')
    incoming = []
    for values in (targets, original_targets):
        edges = []
        for name, target in values.items():
            dependencies = target.get('dependencies', {})
            require(type(dependencies) is dict and all(type(edge) is str for edge in dependencies) and
                len({edge.casefold() for edge in dependencies}) == len(dependencies), 'consumer_original_build_incoming')
            edges.extend((name, value) for edge, value in dependencies.items() if edge.casefold() == identifier.casefold())
        require(len(edges) == 1 and edges[0][0] != key, 'consumer_original_build_incoming')
        incoming.append(edges[0])
    require(incoming[0] == incoming[1], 'consumer_original_build_incoming')
    original_direct = original.get('project', {}).get('frameworks', {}).get(framework, {}).get('dependencies', {})
    require(type(original_direct) is dict and not any(name.casefold() == identifier.casefold() for name in original_direct),
            'consumer_original_build_original_boundary')
    record = catalog.get((identifier.casefold(), version))
    before, current = original.get('libraries', {}).get(key, {}), assets['libraries'][key]
    require(record is not None and before.get('type') == current.get('type') == 'package' and
        before.get('sha512') == current.get('sha512') == record['content_hash'] and
        metadata.sha256(Path(package.filename).read_bytes()) == record['archive_sha256'],
        'consumer_original_build_archive')
    real = [name for name in names if name.casefold().startswith(kind.casefold() + '/') and
            name.casefold().endswith(('.props', '.targets'))]
    require(real and not any(name.casefold().startswith('buildtransitive/') for name in names),
            'consumer_original_build_real_group')
    for entry in real:
        require(entry.isascii() and
            entry in before.get('files', []) and entry in current.get('files', []),
            'consumer_original_build_real_member')
        read_bound(folder / entry, metadata.sha256(package.read(entry)))
    # GetBuildItemsForPackageId selects package-named entrypoints, not every
    # helper props/targets file shipped beside or below them. Keep all helper
    # inventories and cache bytes checked without treating them as entrypoints.
    entrypoints = [entry for entry in real if Path(entry).name.casefold() in
                   {identifier.casefold() + '.props', identifier.casefold() + '.targets'}]
    # NuGet selects the compatible Build group before package-name filtering.
    # For admitted modern .NET TFMs, build/native is a different, incompatible
    # framework group, even when it contains package-named props or targets.
    # Keep other nested package-named layouts closed rather than resolve TFMs.
    require(all(entry.rsplit('/', 1)[0] == kind or (kind == 'build' and
        framework in ('net8.0', 'net9.0', 'net10.0') and entry.rsplit('/', 1)[0] == 'build/native')
        for entry in entrypoints), 'consumer_original_build_real_member')
    applicable = [entry for entry in entrypoints if entry.rsplit('/', 1)[0] == kind]
    require(applicable, 'consumer_original_build_real_member')
    # Native ClearIfExists uses the character offset of the last slash, then
    # OrdinalIgnoreCase path within the applicable flat ASCII group.
    first = sorted(applicable, key=lambda entry: (entry.rfind('/'), entry.upper()))[0]
    require(first.rsplit('/', 1)[0] + '/_._' == marker and
        not any(name.casefold() == marker.casefold() for name in names), 'consumer_original_build_collision')
    path = folder / marker
    require(not path.exists() and not any(part.is_symlink() for part in (path, *path.parents)) and
        not any(entry.relative_to(folder).as_posix().casefold() == marker.casefold() for entry in folder.rglob('*')),
            'consumer_original_build_cache')
    return {'path': marker, 'kind': kind, 'accounting': ORIGINAL_EXCLUDED_BUILD_ACCOUNTING, 'metadata': {},
            'archive_sha256': record['archive_sha256'], 'origin': ORIGINAL_EXCLUDED_BUILD_ORIGIN,
            'original_assets_sha256': original_hash}


def empty_lib_marker_shape(marker: str, framework: str, properties: dict) -> None:
    """Closed inherited lib shape for the supported modern .NET framework family."""
    path = re.fullmatch(r'lib/net(6|7|8|9|10)\.0/_\._', marker)
    consumer = re.fullmatch(r'net(8|9|10)\.0', framework)
    require(path is not None and consumer is not None and int(path[1]) <= int(consumer[1]),
            'consumer_original_lib_path')
    require(type(properties) is dict and properties in ({}, {'related': '.xml'}, {'related': '.pdb'},
            {'related': '.pdb;.xml'}), 'consumer_original_lib_metadata')


def original_empty_lib_marker(assets: dict, original: dict, framework: str, key: str, kind: str,
                              package: zipfile.ZipFile, names: list[str], folder: Path, catalog: dict,
                              graph: dict, selected: dict) -> dict:
    """Inherit one original native excluded lib group, with no invented payload bytes."""
    identifier, version = key.rsplit('/', 1)
    require(type(original) is dict and kind in ('compile', 'runtime'), 'consumer_original_lib_origin')
    target = assets['targets'][framework][key]
    group = target.get(kind, {})
    require(type(group) is dict and len(group) == 1 and
        original.get('targets', {}).get(framework, {}).get(key, {}).get('type') == target.get('type') == 'package' and
        original['targets'][framework][key].get(kind) == group, 'consumer_original_lib_group')
    marker, properties = next(iter(group.items()))
    empty_lib_marker_shape(marker, framework, properties)
    direct = assets.get('project', {}).get('frameworks', {}).get(framework, {}).get('dependencies', {})
    require(type(direct) is dict and len(direct) == 1, 'consumer_original_lib_boundary')
    root, dependency = next(iter(direct.items()))
    folded_root = root.casefold()
    require(folded_root in selected and folded_root in graph and graph[folded_root]['selected'] is True and
        type(dependency) is dict and set(dependency) <= {'target', 'version', 'aliases'} and
        dependency.get('target') == 'Package' and dependency.get('version') ==
        '[{0}, {0}]'.format(graph[folded_root]['version']) and dependency.get('aliases', 'selected') == 'selected' and
        assets.get('projectFileDependencyGroups') == {
            framework: [root + ' >= ' + graph[folded_root]['version'] + ' <= ' + graph[folded_root]['version']]} and
        len(assets['targets'][framework]) == len(graph) and
        {name.rsplit('/', 1)[0].casefold() for name in assets['targets'][framework]} == set(graph) and
        all(item.get('type') == 'package' for item in assets['targets'][framework].values()),
        'consumer_original_lib_boundary')
    policy = selected[folded_root]['policy']
    project = policy['project']
    original_path = original.get('project', {}).get('restore', {}).get('projectPath')
    original_hash = policy['metadata']['restore_assets_sha256']
    record = catalog.get((identifier.casefold(), version))
    require(type(original_path) is str and original_path.endswith('/' + project) and
        type(original_hash) is str and re.fullmatch(r'[0-9a-f]{64}', original_hash) and record is not None and
        any(context.get('project') == project and framework in context.get('frameworks', []) and
            context.get('content_hash') == record['content_hash'] for context in record.get('original_contexts', [])),
        'consumer_original_lib_context')
    before, current = original.get('libraries', {}).get(key, {}), assets['libraries'][key]
    require(before.get('type') == current.get('type') == 'package' and
        before.get('sha512') == current.get('sha512') == graph[identifier.casefold()]['content_hash'] == record['content_hash'] and
        metadata.sha256(Path(package.filename).read_bytes()) == record['archive_sha256'],
        'consumer_original_lib_archive')
    cache_metadata = planner.read_json(metadata.read_regular_file(folder / '.nupkg.metadata', maximum=4 * 1024 ** 2))
    require(cache_metadata.get('source') == record.get('cache_provenance', {}).get('source') and
        type(cache_metadata.get('source')) is str and cache_metadata.get('contentHash') == record['content_hash'],
        'consumer_original_lib_cache')
    require(not any(name.casefold() == marker.casefold() for name in names), 'consumer_original_lib_collision')
    directory = marker.rsplit('/', 1)[0]
    members = [name for name in names if name.casefold().startswith(directory.casefold() + '/')]
    # Original native group selection is inherited, not recomputed. Restrict this
    # adapter to flat DLL groups without ref/contract alternatives.
    require(members and all(name.isascii() and name.rsplit('/', 1)[0] == directory and
        Path(name).suffix in ('.dll', '.xml', '.pdb') for name in members) and
        not any(name.casefold().startswith(('ref/', 'lib/contract/')) for name in names), 'consumer_original_lib_inventory')
    # OrdinalIgnoreCase compares ASCII uppercase code points, including their
    # position relative to punctuation; lowercase sorting is not equivalent.
    dlls = sorted((name for name in members if name.endswith('.dll')), key=lambda name: (name.rfind('/'), name.upper()))
    require(bool(dlls), 'consumer_original_lib_inventory')
    require(not any(name.casefold() == marker.casefold() for values in (before, current)
        for name in values.get('files', [])), 'consumer_original_lib_inventory')
    for name in members:
        require(name in before.get('files', []) and name in current.get('files', []), 'consumer_original_lib_inventory')
        read_bound(folder / name, metadata.sha256(package.read(name)))
    prefix = dlls[0][:-4]
    related = sorted(name[len(prefix):] for name in members if name.startswith(prefix + '.') and
                     Path(name).suffix in ('.xml', '.pdb'))
    require(properties == ({'related': ';'.join(related)} if related else {}), 'consumer_original_lib_metadata')
    path = folder / marker
    require(not path.exists() and not any(part.is_symlink() for part in (path, *path.parents)) and
        not any(entry.relative_to(folder).as_posix().casefold() == marker.casefold() for entry in folder.rglob('*')),
        'consumer_original_lib_cache')
    return {'path': marker, 'kind': kind, 'accounting': ORIGINAL_EMPTY_LIB_ACCOUNTING, 'metadata': properties,
            'archive_sha256': record['archive_sha256'], 'origin': ORIGINAL_EMPTY_LIB_ORIGIN,
            'original_assets_sha256': original_hash}


def verify_asset_payloads(assets: dict, framework: str, graph: dict, cache: Path, artifacts: Path, selected: dict,
                          *, catalog: dict | None = None, original_assets: dict | None = None,
                          plan: dict | None = None, original_policy: dict | None = None,
                          semantics: planner.Semantics | None = None) -> list[dict]:
    """Join restored payload bytes and permitted empty markers to archive evidence."""
    evidence = []
    for key, library in assets['targets'][framework].items():
        identifier, version = key.rsplit('/', 1)
        folded = identifier.casefold()
        folder = cache / folded / version
        archive = artifacts / selected[folded]['nupkg'] if graph[folded]['selected'] else folder / f'{folded}.{version}.nupkg'
        require(archive.is_file() and not any(part.is_symlink() for part in (archive, *archive.parents)),
                'consumer_asset_archive_path')
        payloads = []
        with zipfile.ZipFile(archive) as package:
            names = archive_names(package)
            for kind in ('compile', 'runtime', 'contentFiles', 'build', 'buildMultiTargeting', 'native', 'runtimeTargets', 'resource'):
                for entry in library.get(kind, {}):
                    safe_relative(entry)
                    if kind in ('compile', 'runtime') and entry.startswith('lib/') and entry.endswith('/_._') and entry not in names:
                        require(not graph[folded]['selected'], 'consumer_original_lib_selected')
                        payloads.append(original_empty_lib_marker(assets, original_assets, framework, key, kind,
                            package, names, folder, catalog or {}, graph, selected))
                        continue
                    if kind == 'contentFiles' and entry == EMPTY_CONTENT and entry not in names:
                        require(not graph[folded]['selected'], 'consumer_synthetic_content_selected')
                        payloads.append(synthetic_content_marker(assets, original_assets, framework, key,
                                                                package, names, folder, catalog or {}, graph))
                        continue
                    if kind == 'build' and entry == f'build/{framework}/_._' and entry not in names:
                        require(not graph[folded]['selected'], 'consumer_synthetic_build_selected')
                        payloads.append(synthetic_build_marker(assets, original_assets, framework, key,
                            package, names, folder, catalog or {}, graph, selected))
                        continue
                    if kind in ('build', 'buildMultiTargeting') and entry == kind + '/_._' and entry not in names:
                        require(not graph[folded]['selected'], 'consumer_original_build_selected')
                        direct = assets.get('project', {}).get('frameworks', {}).get(framework, {}).get('dependencies', {})
                        root_case = plan is not None and original_policy is not None and len(direct) == 1 and next(iter(direct)) in FLAT_BUILD_ROOTS
                        before = (original_assets or {}).get('targets', {}).get(framework, {}).get(key, {})
                        witness_case = root_case and key == 'Grpc.Tools/2.70.0' and not before
                        exclusion_case = root_case and (plan['line'] == '3.8' or next(iter(direct)) == 'Elsa.Workflows.Runtime.ProtoActor') and key == 'Proto.Cluster.CodeGen/1.8.0' and before.get(kind) != {kind + '/_._': {}}
                        direct_case = plan is not None and original_policy is not None and len(direct) == 1 and \
                            direct_flat_build_case(plan, next(iter(direct)), key, framework)
                        if witness_case or exclusion_case or direct_case:
                            payloads.append(proven_flat_build_marker(assets, original_assets, framework, key, kind,
                                package, names, folder, catalog or {}, graph, selected, plan, original_policy, witness=witness_case,
                                direct_reference=direct_case, semantics=semantics))
                        else:
                            payloads.append(original_excluded_build_marker(assets, original_assets, framework, key, kind,
                                package, names, folder, catalog or {}, graph, selected))
                        continue
                    require(entry in names, 'consumer_asset_archive_member')
                    content = package.read(entry)
                    read_bound(folder / entry, metadata.sha256(content))
                    payloads.append({'path': entry, 'kind': kind, 'sha256': metadata.sha256(content), 'size': len(content)})
        evidence.append({'id': identifier, 'version': version, 'payloads': payloads})
    return evidence


def render_project(identifier: str, version: str, framework: str, references: list[str], *, executable: bool, managed: bool, locked: bool = True, sdk_policy: dict | None = None) -> str:
    """Render a consumer project with one exact package reference and the required SDK policy."""
    project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
    properties = ET.SubElement(project, 'PropertyGroup')
    for name, value in {'TargetFramework': framework, 'OutputType': 'Exe' if executable else 'Library',
                        'ImplicitUsings': 'enable', 'Nullable': 'enable', 'RestorePackagesWithLockFile': 'true',
                        'RestoreLockedMode': str(locked).lower(), 'NuGetAudit': 'false',
                        'RestoreEnablePackagePruning': str(bool(sdk_policy and sdk_policy['pruning'])).lower(),
                        'DisableImplicitLibraryPacksFolder': 'true', 'DisableImplicitNuGetFallbackFolder': 'true',
                        'UseAppHost': 'false'}.items():
        ET.SubElement(properties, name).text = value
    if sdk_policy and sdk_policy['downloads']:
        ET.SubElement(properties, 'NetCoreTargetingPackRoot').text = '$(MSBuildThisFileDirectory)targeting-packs'
    group = ET.SubElement(project, 'ItemGroup')
    package = ET.SubElement(group, 'PackageReference', Include=identifier, Version=f'[{version}]')
    if managed and not executable:
        package.set('Aliases', 'selected')
    for name in references:
        ET.SubElement(group, 'FrameworkReference', Include=name)
    return ET.tostring(project, encoding='unicode')


def validate_ledger(plan: dict, ledger: list[dict]) -> None:
    """Require exactly one successful compile cell for every selected package and framework."""
    expected = {(row['id'], framework) for row in plan['inventory']['selected'] for framework in row['frameworks']}
    actual = [(row['id'], row['framework']) for row in ledger]
    require(len(actual) == len(expected) and set(actual) == expected and all(row['success'] is True for row in ledger),
            'consumer_coverage_ledger')


def retain_runtime_rows(verified: list[dict]) -> list[dict]:
    """Project verified assembly identities and hashes without private runtime paths."""
    return [{key: row[key] for key in ('name', 'version', 'informationalVersion', 'sha256',
            'package_id', 'package_version', 'package_asset')} for row in verified]


def cold_environment(output: Path) -> tuple[Path, dict]:
    """Create an isolated consumer environment with a new package cache and CLI home."""
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
    """Restore an isolated discovery project and return its audited graph, feed policy, and lock."""
    output.mkdir()
    consumers.prepare_isolation(output, metadata.SDK)
    sdk_policy = original_policy['sdk_projects'][package['policy']['project']][framework]
    universe = {folded: {'id': row['id'], 'selected': True} for folded, row in selected.items()}
    universe.update({folded: {'id': row['id'], 'selected': False} for (folded, _), row in catalog.items()})
    universe.update({folded: {'id': row['id'], 'selected': False}
        for (folded, _), row in original_policy['sdk_downloads'].items()
        if {'id': row['id'], 'version': row['version']} in sdk_policy['downloads']})
    policy = {'sources': original_policy['mirrors'], 'mapping': original_policy['mapping']}
    (output / 'NuGet.Config').write_text(render_config(artifacts, universe, policy))
    project = output / 'Consumer.csproj'
    project.write_text(render_project(package['id'], plan['requested_version'], framework, references,
                                     executable=runtime, managed=managed, locked=False, sdk_policy=sdk_policy))
    cache, environment = cold_environment(output)
    inputs = {path.name: metadata.sha256(path.read_bytes()) for path in output.iterdir() if path.is_file()}
    consumers._run_command(['dotnet', 'restore', str(project), '--configfile', str(output / 'NuGet.Config'),
        '--packages', str(cache), '--no-cache', '--nologo'], output, environment, output / 'restore.log', 1200)
    require(all(metadata.sha256((output / name).read_bytes()) == digest for name, digest in inputs.items()),
            'consumer_discovery_inputs_changed')
    assets = planner.read_json((output / 'obj/project.assets.json').read_bytes())
    lock = (output / 'packages.lock.json').read_bytes()
    graph = resolution.audit_native_graph(assets, planner.read_json(lock), package['id'], framework,
                                         selected, catalog, plan['requested_version'], semantics,
                                         sdk_policy=sdk_policy, phase='discovery')
    resolution.sdk.verify_downloads(assets, framework, sdk_policy, original_policy['sdk_downloads'], cache,
        policy['sources'], policy['mapping'], original_policy['inspector'], proof=False)
    # Discovery uses finite original mirrors, and remains distinct from proof.
    require(not (output / 'targeting-packs').exists() and not (output / 'targeting-packs').is_symlink(),
            'consumer_sdk_ambient_targeting_root')
    validate_restored(assets, framework, graph, output, cache, artifacts, policy)
    proof_policy = {'sources': original_policy['sources'],
                    'mapping': {folded: original_policy['mapping'][folded] for folded in
                        set(graph) | {row['id'].casefold() for row in sdk_policy['downloads']}}}
    return graph, proof_policy, lock


def cell(plan: dict, package: dict, framework: str, selected: dict, artifacts: Path,
         semantics: planner.Semantics, output: Path, *, catalog: dict,
         original_policy: dict, inspector: Path, runtime: bool = False) -> dict:
    """Prove one package/framework restore and compile, optionally exercising its runtime
    fixture.
    """
    output.mkdir()
    isolation = consumers.prepare_isolation(output, metadata.SDK)
    project = output / 'Consumer.csproj'
    output_policy = package['policy']['metadata']['original_output_policy'][framework]
    managed = output_policy['IncludeBuildOutput'].lower() != 'false'
    references = nearest_group(package['policy']['metadata']['framework_reference_groups'], framework, semantics).get('references', [])
    graph, policy, lock_bytes = discover(plan, package, framework, selected, artifacts, semantics, catalog,
        original_policy, output / 'discovery', references, runtime=runtime, managed=managed)
    sdk_policy = original_policy['sdk_projects'][package['policy']['project']][framework]
    proof_nodes = dict(graph)
    proof_nodes.update({row['id'].casefold(): {'id': row['id'], 'selected': False} for row in sdk_policy['downloads']})
    (output / 'NuGet.Config').write_text(render_config(artifacts, proof_nodes, policy))
    project.write_text(render_project(package['id'], plan['requested_version'], framework, references, executable=runtime, managed=managed, sdk_policy=sdk_policy))
    (output / 'packages.lock.json').write_bytes(lock_bytes)
    (output / 'Program.cs').write_text(runtime_contract(plan)['fixture'].read_text() if runtime else
        ('extern alias selected;\n' if managed else '') + 'public class CompileContract {}\n')
    if runtime:
        (output / 'AssemblyProof.cs').write_bytes((ROOT / 'scripts/integration-program/selected-consumer/AssemblyProof.cs').read_bytes())
    cache, environment = cold_environment(output)
    inputs = {path.name: metadata.sha256(path.read_bytes()) for path in output.iterdir() if path.is_file()}
    commands = []
    commands.append(consumers._run_command(['dotnet', 'restore', str(project), '--locked-mode', '--configfile', str(output / 'NuGet.Config'),
        '--packages', str(cache), '--no-cache', '--nologo'], output, environment, output / 'restore.log', 1200))
    restored = planner.read_json((output / 'obj/project.assets.json').read_bytes())
    require(not (output / 'targeting-packs').exists() and not (output / 'targeting-packs').is_symlink(),
            'consumer_sdk_ambient_targeting_root')
    validate_restored(restored, framework, graph, output, cache, artifacts, policy)
    require((output / 'packages.lock.json').read_bytes() == lock_bytes, 'consumer_locked_document_changed')
    pruned_edges = []
    resolution.audit_native_graph(restored, planner.read_json(lock_bytes), package['id'], framework,
                                  selected, catalog, plan['requested_version'], semantics, sdk_policy=sdk_policy,
                                  pruned_edges=pruned_edges, phase='locked-proof')
    sdk_downloads = resolution.sdk.verify_downloads(restored, framework, sdk_policy, original_policy['sdk_downloads'],
        cache, policy['sources'], policy['mapping'], inspector, proof=True)
    cached = verify_cache(graph, policy, cache, artifacts, selected, restored, inspector, catalog)
    payloads = verify_asset_payloads(restored, framework, graph, cache, artifacts, selected, catalog=catalog,
        original_assets=original_policy['original_assets'][package['policy']['project']], plan=plan, original_policy=original_policy, semantics=semantics)
    commands.append(consumers._run_command(['dotnet', 'build', str(project), '-c', 'Release', '--no-restore', '--disable-build-servers',
        '--nologo'], output, environment, output / 'build.log', 1200))
    require(all(metadata.sha256((output / name).read_bytes()) == digest for name, digest in inputs.items()) and
            not (output / 'targeting-packs').exists() and not (output / 'targeting-packs').is_symlink(), 'consumer_inputs_changed')
    result = {'id': package['id'], 'framework': framework, 'success': True, 'fresh_cache': True, 'package_reference_only': True,
              'accounting': 'managed-reference-compile' if managed else 'output-content-only-restore-build',
              'original_output_policy': output_policy, 'restored_payloads': payloads,
              'sdk_restore': resolution.sdk.retained_policy(sdk_policy, pruned_edges, sdk_downloads,
                  package['policy']['metadata']['restore_assets_sha256']),
              'discovery_scope': 'offline-original-archive-metadata-only',
              'native_lock_sha256': metadata.sha256(lock_bytes), 'archive_sha256': package['nupkg_sha256'], 'restored': cached, 'isolation': isolation, 'input_sha256': inputs}
    if runtime:
        consumers._run_command(['dotnet', 'run', '--project', str(project), '--no-restore', '--no-build', '-c', 'Release',
                               '--framework', framework], output, environment, output / 'runtime.log', 300)
        lines = [line.removeprefix('SELECTED_CONSUMER_PROOF=') for line in (output / 'runtime.log').read_text().splitlines()
                 if line.startswith('SELECTED_CONSUMER_PROOF=')]
        require(len(lines) == 1, 'consumer_runtime_receipt')
        proof = planner.read_json(lines[0].encode())
        contract = runtime_contract(plan)
        require(all(type(proof.get(key)) is type(value) and proof[key] == value for key, value in contract['checks'].items()),
                'consumer_runtime_contract')
        result['runtime'] = {'contract': contract['description'],
            'loaded_assemblies': retain_runtime_rows(consumers.verify_loaded_assemblies(proof, restored, framework, output,
                cache, artifacts, selected, plan['requested_version'], plan['source']['commit'],
                required_packages=contract['required_packages'],
                assembly_release_version=contract['assembly_release_version'],
                original_assembly_policies=plan['product'] == 'core'))}
    return result


def execute(root: Path, plan_path: Path, plan_hash: str, receipt_path: Path, receipt_hash: str,
            artifacts: Path, snapshots: Path, output: Path, *, retire_caches: bool = False) -> dict:
    """Admit producer evidence, refresh prerequisites, and retain complete consumer results."""
    plan, receipt, historical_admission = admit_producer_stage(read_bound(plan_path, plan_hash), plan_hash,
        read_bound(receipt_path, receipt_hash), receipt_hash)
    if plan['product'] == 'core':
        core.validate_plan(plan)
    else:
        require(plan['product'] in ('studio', 'extensions') and plan['line'] in ('3.8', '3.9'), 'consumer_control_not_implemented')
    controller = producer.verify_controller(root, plan)
    execution = selected_execution('consumer', controller, plan, plan_hash)
    require(execution.get('kind') == receipt['execution'].get('kind'), 'consumer_execution_stage_kind')
    selected = admit_artifacts(plan, plan_hash, receipt, artifacts, root)
    originals = load_snapshots(plan, plan_hash, snapshots)
    if plan['product'] == 'core':
        core.verify_source(root, plan)
    elif plan['line'] == '3.9':
        maintenance39.verify_source(root, plan)
    elif plan['product'] == 'extensions':
        extensions.verify_source(root, plan['source']['commit'])
    else:
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
              'runtime_contract_source': (core.source_contract(plan) if plan['product'] == 'core' else
                  maintenance39.contract(plan['product'])['files'] if plan['line'] == '3.9' else
                  extensions.SOURCE_BLOBS if plan['product'] == 'extensions' else STUDIO38_CONTRACT_SOURCE)}
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
        source_downloads = resolution.sdk.bind_source_downloads(root, plan, originals, private / 'source-downloads', semantics)
        catalog, original_policy = resolution.archive_catalog(originals, selected, config, semantics,
            inspector, private / 'original-external-catalog', source_downloads=source_downloads)
        original_policy['inspector'] = inspector
        for package in selected.values():
            package['effective_contexts'] = [context for project, assets in originals.items()
                for key in assets['libraries'] if key.casefold() == (package['id'] + '/' + plan['requested_version']).casefold()
                for context in resolution.sdk.effective_contexts(assets, key, original_policy['sdk_projects'][project])]
        result['external_catalog_sha256'] = metadata.sha256(
            (private / 'original-external-catalog/catalog.private.json').read_bytes())

        def prove_cell(package: dict, framework: str, name: str, *, runtime: bool = False) -> dict:
            """Prove one consumer cell and optionally retire its caches after success."""
            cell_output = private / name
            completed = cell(plan, package, framework, selected, artifacts, semantics, cell_output,
                catalog=catalog, original_policy=original_policy, inspector=inspector, runtime=runtime)
            if retire_caches:
                retire_successful_cell_caches(private, cell_output, completed)
            return completed

        for package in selected.values():
            for framework in package['policy']['frameworks']:
                result['stage'] = 'selected-restore-compile'
                result['focus'] = {'id': package['id'], 'framework': framework}
                name = metadata.sha256((package['id'] + '/' + framework).encode())[:16]
                result['coverage'].append(prove_cell(package, framework, name))
                result.pop('focus', None)
        validate_ledger(plan, result['coverage'])
        result['stage'] = plan['product'] + '-runtime-contract'
        contract = runtime_contract(plan)
        require(contract['package'] in selected, 'consumer_representative_missing')
        package = selected[contract['package']]
        for framework in package['policy']['frameworks']:
            result['focus'] = {'id': package['id'], 'framework': framework}
            result['runtime'].append(prove_cell(package, framework, 'runtime-' + framework, runtime=True))
            result.pop('focus', None)
        result.update(success=True, stage='complete', limitations=[
            'Complete selected restore/compile coverage is distinct from representative runtime behavior.',
            contract['limitation']])
        return result
    except Exception as error:
        result['failure_code'] = result['stage'] + '-failed'
        result.update(resolution.public_range_failure(error))
        detail = getattr(error, '_selected_prerequisite_failure', None)
        if type(error) is ValueError and str(error) == 'artifact_fresh_prerequisite_failed' and \
                prerequisite_context(detail, role='consumer', stage=result['stage']) is not None:
            result['prerequisite_failure'] = detail
        detail = getattr(error, '_selected_build_group_failure', None)
        if type(error) is ValueError and str(error) == 'consumer_original_build_group' and \
                original_build_group_context(detail, role='consumer', stage=result['stage'], product=plan['product']) is not None:
            result['original_build_group_failure'] = detail
        raise
    finally:
        (retained / 'receipt.json').write_text(json.dumps(result, indent=2, sort_keys=True) + '\n')


def main() -> int:
    """Run the consumer CLI and report bounded stage and dependency-range failures."""
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('plan', 'artifact-receipt', 'artifacts', 'planning-assets', 'output'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--plan-sha256', required=True)
    parser.add_argument('--artifact-receipt-sha256', required=True)
    parser.add_argument('--retire-successful-cell-caches', action='store_true',
        help='Persist successful private cell proofs, then retire only their two owned NuGet caches.')
    args = parser.parse_args()
    output_existed = True
    try:
        output_existed = args.output.exists() or args.output.is_symlink()
        execute(ROOT, args.plan, args.plan_sha256, args.artifact_receipt, args.artifact_receipt_sha256,
                args.artifacts, args.planning_assets, args.output, retire_caches=args.retire_successful_cell_caches)
        return 0
    except Exception as error:
        code = str(error) if type(error) is ValueError and str(error) in PUBLIC_FAILURE_CODES else 'consumer_control_failed'
        receipt_created, stage = producer.failure_receipt_status(args.output, output_existed,
            mode='selected-product-consumers', stages=PUBLIC_FAILURE_STAGES)
        detail = original_build_group_context(getattr(error, '_selected_build_group_failure', None),
            role='consumer', stage=stage, product=stage.removesuffix('-runtime-contract')) \
            if receipt_created and code == 'consumer_original_build_group' else None
        reason = {'original_build_group_reason': detail['reason']} if detail is not None else {}
        print(json.dumps({'success': False, 'failure_code': code, 'failure_stage': stage,
            'retained_receipt_created': receipt_created} | reason | resolution.public_range_failure(error)))
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
