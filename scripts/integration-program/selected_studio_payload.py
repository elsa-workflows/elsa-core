"""Closed six-cell selected-control payload validation, without execution.

Native/compiler/test/runtime judgments are inherited from exact successful
source-bound receipts. This module repeats their public byte/content joins.
"""
from __future__ import annotations

import base64
from copy import deepcopy
from datetime import datetime, timezone
import hashlib
import io
import json
from pathlib import Path
import re
import zipfile

import historical_studio_npm_continuation as lifecycle
import product_release_metadata as metadata
import prove_product_release_artifacts as producer
import prove_product_release_consumers as consumer
import prove_consolidated_packages as packages
import prove_studio_npm_pair as npm
import selected_control_transport as transport
import selected_studio_plan_schema as plan_schema
import selected_core_payload as core_payload
import selected_extensions_payload as extensions_payload
import selected_maintenance_39 as maintenance39
import selected_extensions_contract as extensions
from prove_consolidated_packages import require

CONTRACT = Path(__file__).with_name('selected_studio38_payload_contract.json')
PRODUCER_KEYS = set('schema mode plan_sha256 planner_controller artifact_controller source product line version execution success published version_allocated tag_created stage preflight product_tests packages artifact_proof npm'.split())
CONSUMER_KEYS = set('schema mode plan_sha256 artifact_receipt_sha256 source controller execution producer_execution planner_controller artifact_controller producer_plan_admission current_consumer_admission success published coverage runtime stage runtime_contract_source preflight external_catalog_sha256 limitations'.split())
CELL_KEYS = set('id framework success fresh_cache package_reference_only accounting original_output_policy restored_payloads discovery_scope native_lock_sha256 archive_sha256 restored isolation input_sha256 sdk_restore'.split())
NPM_KEYS = set('source_commit source_tree version execution framework commands success published historical_workflow_executed original_lifecycle_preserved inline_postinstall_preserved stage wasm react consumer lifecycle_correction'.split())
STEPS = ('node-version', 'historical-host-publish', 'pack-wasm', 'historical-wrapper-install',
         'historical-wrapper-copy', 'historical-wrapper-build', 'pack-react', 'historical-consumer-lock',
         'historical-consumer-normal-install', 'historical-consumer-esm', 'historical-consumer-commonjs', 'historical-consumer-vite')


def sequence(value) -> list:
    require(type(value) is list and len(value) <= transport.MAX_MEMBERS, 'selected_payload_array')
    return value


def hashes(value, keys: set[str]) -> None:
    transport.closed(value, keys, 'selected_payload_hashes')
    for item in value.values():
        transport.digest(item)


def text(value: object) -> str:
    require(type(value) is str and value and len(value) <= 4096 and
            not any(ord(char) < 32 for char in value) and not value.startswith(('/', '\\')) and ':' not in value,
            'selected_payload_text')
    return value


def package_id(value):
    require(type(value) is str and re.fullmatch(r'[A-Za-z0-9_][A-Za-z0-9_.-]*', value), 'selected_payload_package_id')
    return value


def inventory(rows: list[dict]) -> dict:
    result = {}
    for row in sequence(rows):
        transport.closed(row, {'path', 'size', 'sha256'}, 'selected_payload_inventory')
        name = transport.safe_name(row['path'])
        require(name not in result, 'selected_payload_inventory_duplicate')
        transport.integer(row['size'], transport.MAX_PACKAGE_BYTES)
        transport.digest(row['sha256'])
        result[name] = row
    transport.unique_names(list(result))
    require(result and sum(row['size'] for row in result.values()) <= transport.MAX_PACKAGE_BYTES, 'selected_payload_inventory_size')
    return result


def archive_record(row: dict, plan: dict, *, selected: bool = True) -> None:
    transport.closed(row, {'file', 'id', 'version', 'sha256', 'size', 'inventory'}, 'selected_payload_archive')
    package_id(row['id'])
    transport.safe_name(row['file'])
    require(row['version'] == plan['requested_version'] and '/' not in row['file'] and
            row['file'].endswith(('.nupkg', '.snupkg')), 'selected_payload_archive_identity')
    if selected:
        require(row['file'] in {f"{row['id']}.{row['version']}.nupkg", f"{row['id']}.{row['version']}.snupkg"},
                'selected_payload_archive_identity')
    transport.digest(row['sha256'])
    transport.integer(row['size'], transport.MAX_PACKAGE_BYTES, positive=True)
    inventory(row['inventory'])


def validate_packages(plan: dict, receipt: dict, files: dict[str, bytes]) -> dict:
    transport.closed(receipt['packages'], {'selected', 'private_recipe_only_outputs'}, 'selected_payload_packages')
    selected = {row['id'].casefold(): row for row in plan['inventory']['selected']}
    expected = {name for name in plan['expected_artifacts'] if name.endswith(('.nupkg', '.snupkg'))}
    records, payloads = {}, {}
    for row in sequence(receipt['packages']['selected']):
        archive_record(row, plan)
        require(row['file'] in expected and row['file'] not in records and row['id'].casefold() in selected,
                'selected_payload_archive_bijection')
        path = 'producer/nuget/' + row['file']
        require(path in files, 'selected_payload_archive_missing')
        data = files[path]
        require(len(data) == row['size'] and metadata.sha256(data) == row['sha256'], 'selected_payload_archive_bytes')
        members = transport.zip_members(data, leaf=True)
        require(transport.inventory(members) == row['inventory'], 'selected_payload_archive_inventory')
        with zipfile.ZipFile(io.BytesIO(data)) as zipped:
            nuspec = packages.metadata(zipped)
        repository = nuspec.find('repository')
        require(nuspec.findtext('id') == row['id'] and nuspec.findtext('version') == plan['requested_version'] and
                repository is not None and set(repository.attrib) <= {'type', 'url', 'branch', 'commit'} and
                repository.get('commit') == plan['source']['commit'] and repository.get('url', '').removesuffix('.git') ==
                'https://github.com/elsa-workflows/elsa-core', 'selected_payload_nuspec_identity')
        policy = selected[row['id'].casefold()]
        require(packages.dependency_groups(nuspec) == policy['metadata']['dependency_groups'] and
                packages.framework_reference_groups(nuspec) == policy['metadata']['framework_reference_groups'],
                'selected_payload_nuspec_dependencies')
        records[row['file']] = row
        if row['file'].endswith('.nupkg'):
            payloads[row['id'].casefold()] = {'record': row, 'members': members, 'policy': policy, 'data': data}
    require(set(records) == expected and set(payloads) == set(selected), 'selected_payload_archive_bijection')
    excluded = {row['id'].casefold() for row in plan['inventory']['excluded'] if row['id']}
    private_names = []
    for row in sequence(receipt['packages']['private_recipe_only_outputs']):
        archive_record(row, plan, selected=False)
        require(row['id'].casefold() in excluded and row['id'].casefold() not in selected and row['file'].casefold() not in {name.casefold() for name in records} and
                'producer/nuget/' + row['file'] not in files, 'selected_payload_private_output_scope')
        private_names.append(row['file'])
    transport.unique_names(private_names)
    return payloads


def validate_tests(plan: dict, receipt: dict, contracts: dict) -> None:
    result = transport.closed(receipt['product_tests'], {'executions', 'inherited_skipped_placeholders'}, 'selected_payload_tests')
    projects = {row['path']: row for row in plan['inventory']['projects']}
    expected = {(project, framework) for project in plan['inventory']['applicable_tests']
                for framework in projects[project]['target_frameworks']}
    policies = {}
    for row in contracts['register']['inherited_skipped_placeholders']:
        if row['product'] == plan['product'] and plan['source']['original_commit'] in row['commits']:
            policy = {key: value for key, value in row.items() if key not in ('commits', 'product')}
            policies[(row['project'], row['framework'])] = policy | {'source_commit': plan['source']['commit']}
    require(set(policies) <= expected, 'selected_payload_placeholder_source')
    observed = []
    for skipped, rows in ((False, result['executions']), (True, result['inherited_skipped_placeholders'])):
        for row in sequence(rows):
            key = row.get('project'), row.get('framework')
            require(key in expected and key not in observed, 'selected_payload_test_bijection')
            observed.append(key)
            base = {'project', 'framework', 'counters', 'sha256'}
            transport.closed(row, base | (set(policies.get(key, {})) | {'not_executed_result_count'} if skipped else set()),
                             'selected_payload_test_execution')
            transport.digest(row['sha256'])
            counters = transport.closed(row['counters'], set(producer.maintenance.TRX_COUNTERS), 'selected_payload_test_counters')
            require(all(type(value) is int and value >= 0 for value in counters.values()), 'selected_payload_test_counts')
            if skipped:
                require(key in policies and all(row[name] == value for name, value in policies[key].items()) and
                        row['not_executed_result_count'] == 1 and
                        counters == dict.fromkeys(producer.maintenance.TRX_COUNTERS, 0) | {'total': 1},
                        'selected_payload_placeholder')
            else:
                require(key not in policies and counters['total'] == counters['executed'] == counters['passed'] > 0 and
                        all(value == 0 for name, value in counters.items() if name not in ('total', 'executed', 'passed')),
                        'selected_payload_test_failed_or_skipped')
    require(set(observed) == expected, 'selected_payload_test_bijection')


def validate_npm(plan: dict, receipt: dict, files: dict[str, bytes], contract: dict) -> None:
    report = transport.closed(receipt['npm'], NPM_KEYS, 'selected_payload_npm')
    require(report['source_commit'] == plan['source']['commit'] and report['source_tree'] == plan['source']['tree'] and
            report['version'] == plan['requested_version'] and report['execution'] == receipt['execution'] and
            report['framework'] == 'net10.0' and report['success'] is True and report['stage'] == 'complete' and
            all(report[key] is False for key in ('published', 'historical_workflow_executed', 'original_lifecycle_preserved')) and
            report['inline_postinstall_preserved'] is True, 'selected_payload_npm_identity')
    require(report['lifecycle_correction'] == lifecycle.CONTINUATIONS[plan['line']] | {'path': lifecycle.PATH,
        'before_blob': lifecycle.BEFORE_BLOB, 'after_blob': lifecycle.AFTER_BLOB, 'scope': 'inline-copy-script-only'},
        'selected_payload_npm_lifecycle')
    commands = sequence(report['commands'])
    require(len(commands) == len(STEPS), 'selected_payload_npm_commands')
    for row, step in zip(commands, STEPS):
        transport.closed(row, {'step', 'success', 'exit_code'}, 'selected_payload_npm_command')
        require(row['step'] == step and row['success'] is True and type(row['exit_code']) is int and row['exit_code'] == 0,
                'selected_payload_npm_command')
    source_manifests = [row for row in contract['files'] if 'package' in row]
    require(plan['npm']['workflow'] == {'path': contract['files'][3]['path'], 'sha256': contract['files'][3]['sha256']} and
            len(plan['npm']['manifests']) == 2, 'selected_payload_npm_source')
    members_by_name, reports = {}, {}
    for key, original, intent in zip(('wasm', 'react'), source_manifests, plan['npm']['packages']):
        archive = transport.closed(report[key], {'file', 'name', 'version', 'sha512_integrity', 'inventory', 'package_metadata_sha256'},
                                   'selected_payload_npm_archive')
        name = npm.WASM if key == 'wasm' else npm.REACT
        require(archive['name'] == intent['id'] == name and archive['version'] == intent['version'] == plan['requested_version'] and
                archive['file'] == intent['expected_tarball'], 'selected_payload_npm_archive_identity')
        data = files['producer/npm/' + archive['file']]
        require(transport.sha512_integrity(data) == archive['sha512_integrity'], 'selected_payload_npm_integrity')
        members = transport.tar_members(data)
        actual_inventory = {row['path']: {'size': row['size'], 'sha256': row['sha256']} for row in transport.inventory(members)}
        require(archive['inventory'] == actual_inventory and archive['package_metadata_sha256'] == metadata.sha256(members['package.json']),
                'selected_payload_npm_inventory')
        package = transport.strict_json(members['package.json'])
        expected = deepcopy(original['package'])
        expected['version'] = plan['requested_version']
        if key == 'react':
            expected['dependencies'][npm.WASM] = plan['requested_version']
        require(package == expected, 'selected_payload_npm_original_metadata')
        manifests = plan['npm']['manifests']
        intent_manifest = manifests[0 if key == 'wasm' else 1]
        require(intent_manifest == {'path': original['path'], 'sha256': original['sha256'],
                'checked_in_version': original['package']['version'],
                'checked_in_dependencies': original['package'].get('dependencies', {}),
                'peer_dependencies': original['package'].get('peerDependencies', {})}, 'selected_payload_npm_manifest_intent')
        reports[name], members_by_name[name] = archive, members
    wasm, react = members_by_name[npm.WASM], members_by_name[npm.REACT]
    require(all(path in wasm for path in ('_framework/blazor.webassembly.js', '_framework/dotnet.js', 'index.html', 'appsettings.json')) and
            any(path.startswith('_content/') for path in wasm), 'selected_payload_npm_host_assets')
    for prefix in ('dotnet.native.', 'Elsa.Studio.Host.CustomElements.'):
        require(any(path.startswith('_framework/' + prefix) and path.endswith('.wasm') and value.startswith(b'\x00asm')
                    for path, value in wasm.items()), 'selected_payload_npm_managed_assets')
    for entry in npm.entrypoints(transport.strict_json(react['package.json'])):
        require(entry in react and react[entry], 'selected_payload_npm_entrypoint')
    copied = {path: value for path, value in wasm.items() if path.startswith(('_content/', '_framework/')) or path == 'appsettings.json'}
    require(all(react.get('dist/' + path) == value for path, value in copied.items()), 'selected_payload_npm_dist_bytes')
    downstream = transport.closed(report['consumer'], {'local_archives', 'empty_install_cache', 'normal_lifecycle',
        'esm', 'commonjs', 'vite', 'input_sha256', 'lifecycle_generated_assets'}, 'selected_payload_npm_consumer')
    require(all(downstream[key] is True for key in ('empty_install_cache', 'normal_lifecycle', 'esm', 'commonjs', 'vite')),
            'selected_payload_npm_consumer_failed')
    require(downstream['local_archives'] == {name: {'version': plan['requested_version'], 'sha512_integrity': reports[name]['sha512_integrity'],
        'resolution': 'same-run-local-archive'} for name in (npm.WASM, npm.REACT)}, 'selected_payload_npm_consumer_pair')
    hashes(downstream['input_sha256'], {'package.json', 'package-lock.json'})
    generated = {'package': npm.REACT, 'source_package': npm.WASM, 'source_version': plan['requested_version'],
        'source_sha512_integrity': reports[npm.WASM]['sha512_integrity'], 'assets': [
        {'path': 'public/' + path, 'source_path': path, 'size': len(value), 'sha256': metadata.sha256(value)}
        for path, value in sorted(copied.items())]}
    require(not set(react) & {row['path'] for row in generated['assets']}, 'selected_payload_npm_generated_collision')
    require(downstream['lifecycle_generated_assets'] == generated, 'selected_payload_npm_generated_bytes')
    require(transport.strict_json(files['producer/npm/receipt.json']) == report, 'selected_payload_npm_separate_receipt')


def validate_sdk_restore(plan: dict, value: dict, selected: dict, framework: str, restored: dict,
                         original_assets_sha256: str) -> None:
    transport.closed(value, {'sdk_version', 'original_assets_sha256', 'pruning_enabled', 'pruning_sha256', 'pruned_edges', 'downloads',
        'toolchain_hash_scope'}, 'selected_payload_sdk_restore')
    require(value['sdk_version'] == metadata.SDK and type(value['pruning_enabled']) is bool and
        value['toolchain_hash_scope'] == 'new-frozen-bootstrap-catalog-joined-to-fresh-original-feed-bytes',
        'selected_payload_sdk_restore_scope')
    transport.digest(value['original_assets_sha256'])
    require(value['original_assets_sha256'] == original_assets_sha256, 'selected_payload_sdk_original_assets')
    transport.digest(value['pruning_sha256'])
    seen = set()
    for row in sequence(value['pruned_edges']):
        transport.closed(row, {'from_id', 'from_version', 'id', 'range', 'prune_range'}, 'selected_payload_sdk_pruned_edge')
        parent, identifier = package_id(row['from_id']), package_id(row['id'])
        require(value['pruning_enabled'] and identifier.casefold() not in selected and
            (parent.casefold(), identifier.casefold()) not in seen, 'selected_payload_sdk_pruned_scope')
        require(parent.casefold() in restored and row['from_version'] == restored[parent.casefold()]['version'],
                'selected_payload_sdk_pruned_parent')
        seen.add((parent.casefold(), identifier.casefold()))
        text(row['from_version']); text(row['range'])
        require(type(row['prune_range']) is str and re.fullmatch(r'\(,\d+\.\d+\.\d+\]', row['prune_range']),
                'selected_payload_sdk_prune_range')
    seen = set()
    sources = {row['url'] for row in plan['consumer_feed_policy']['sources']}
    for row in sequence(value['downloads']):
        transport.closed(row, {'id', 'version', 'source', 'sha256', 'archive_sha512', 'nuget_content_hash', 'signed'},
                         'selected_payload_sdk_download')
        require(row['id'] in {'Microsoft.NETCore.App.Ref', 'Microsoft.AspNetCore.App.Ref'} and
            row['id'].casefold() not in selected and row['id'] not in seen and row['source'] in sources and
            type(row['signed']) is bool, 'selected_payload_sdk_download_identity')
        require(row['id'].casefold() not in restored and row['version'] ==
            consumer.resolution.sdk.DOWNLOAD_VERSIONS.get(framework), 'selected_payload_sdk_download_version')
        seen.add(row['id']); text(row['version'])
        transport.digest(row['sha256']); transport.digest(row['archive_sha512'], 128)
        try:
            decoded = base64.b64decode(row['nuget_content_hash'], validate=True)
        except (ValueError, TypeError):
            raise ValueError('selected_payload_sdk_content_hash') from None
        require(len(decoded) == 64 and base64.b64encode(decoded).decode() == row['nuget_content_hash'],
                'selected_payload_sdk_content_hash')


def validate_cell(plan: dict, cell: dict, selected: dict, contracts: dict, *, runtime: bool) -> None:
    transport.closed(cell, CELL_KEYS | ({'runtime'} if runtime else set()), 'selected_payload_cell')
    require(cell['id'].casefold() in selected and cell['id'] == selected[cell['id'].casefold()]['record']['id'],
            'selected_payload_cell_package')
    item = selected[cell['id'].casefold()]
    framework = cell['framework']
    require(framework in item['policy']['frameworks'] and all(cell[key] is True for key in
            ('success', 'fresh_cache', 'package_reference_only')) and
            cell['discovery_scope'] == 'offline-original-archive-metadata-only', 'selected_payload_cell_scope')
    policy = item['policy']['metadata']['original_output_policy'][framework]
    managed = policy['IncludeBuildOutput'].lower() != 'false'
    require(cell['original_output_policy'] == policy and cell['accounting'] == (
        'managed-reference-compile' if managed else 'output-content-only-restore-build') and
        cell['archive_sha256'] == item['record']['sha256'], 'selected_payload_cell_output_policy')
    transport.digest(cell['native_lock_sha256'])
    isolation = {name: metadata.sha256(b'<Project />\n') for name in
                 ('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props')}
    isolation['global.json'] = metadata.sha256((json.dumps({'sdk': {'version': metadata.SDK, 'rollForward': 'disable'}}) + '\n').encode())
    require(cell['isolation'] == isolation, 'selected_payload_isolation')
    keys = set(isolation) | {'NuGet.Config', 'Consumer.csproj', 'packages.lock.json', 'Program.cs'}
    hashes(cell['input_sha256'], keys | ({'AssemblyProof.cs'} if runtime else set()))
    program = contracts['fixtures'][plan['product']] if runtime else (
        ('extern alias selected;\n' if managed else '') + 'public class CompileContract {}\n').encode()
    require(cell['input_sha256']['Program.cs'] == metadata.sha256(program) and
            cell['input_sha256']['packages.lock.json'] == cell['native_lock_sha256'] and
            all(cell['input_sha256'][key] == value for key, value in isolation.items()), 'selected_payload_inputs')
    if runtime:
        require(cell['input_sha256']['AssemblyProof.cs'] == metadata.sha256(contracts['assembly_proof']),
            'selected_payload_runtime_fixture')
    restored = {}
    sources = {row['url'] for row in plan['consumer_feed_policy']['sources']}
    for row in sequence(cell['restored']):
        require(type(row) is dict, 'selected_payload_restored_object')
        package_id(row['id'])
        folded = row['id'].casefold()
        require(folded not in restored, 'selected_payload_restored_duplicate')
        text(row['version'])
        if folded in selected:
            transport.closed(row, {'id', 'version', 'assets_type', 'source', 'sha256', 'sha512'}, 'selected_payload_restored_selected')
            archive = selected[folded]
            require(row['id'] == archive['record']['id'] and row['version'] == plan['requested_version'] and
                    row['source'] == 'selected-local-archives' and row['assets_type'] == 'package' and
                    row['sha256'] == archive['record']['sha256'] and row['sha512'] == hashlib.sha512(archive['data']).hexdigest(),
                    'selected_payload_restored_selected_bytes')
        else:
            transport.closed(row, {'id', 'version', 'source', 'sha256', 'archive_sha512', 'nuget_content_hash', 'signed'},
                             'selected_payload_restored_external')
            require(row['source'] in sources and type(row['signed']) is bool, 'selected_payload_restored_external_source')
            transport.digest(row['sha256'])
            transport.digest(row['archive_sha512'], 128)
            try:
                content_hash = base64.b64decode(row['nuget_content_hash'], validate=True)
            except (ValueError, TypeError):
                raise ValueError('selected_payload_native_content_hash') from None
            require(len(content_hash) == 64 and base64.b64encode(content_hash).decode() == row['nuget_content_hash'],
                    'selected_payload_native_content_hash')
        restored[folded] = row
    require(cell['id'].casefold() in restored, 'selected_payload_restored_root')
    validate_sdk_restore(plan, cell['sdk_restore'], selected, framework, restored,
                         item['policy']['metadata']['restore_assets_sha256'])
    payload_rows, payloads = {}, {}
    for row in sequence(cell['restored_payloads']):
        transport.closed(row, {'id', 'version', 'payloads'}, 'selected_payload_restored_payloads')
        folded = package_id(row['id']).casefold()
        require(folded in restored and folded not in payload_rows and row['version'] == restored[folded]['version'],
                'selected_payload_restored_payload_identity')
        payload_rows[folded] = row
        seen = set()
        for asset in sequence(row['payloads']):
            if type(asset) is dict and 'accounting' in asset:
                transport.closed(asset, {'path', 'kind', 'accounting', 'metadata', 'archive_sha256', 'origin'},
                                 'selected_payload_synthetic_content')
                require(folded not in selected and asset['path'] == consumer.EMPTY_CONTENT and
                    asset['kind'] == 'contentFiles' and asset['accounting'] == consumer.EMPTY_CONTENT_ACCOUNTING and
                    asset['origin'] in ('original-excluded-content', 'package-reference-transitive-content-exclusion') and
                    type(asset['metadata']) is dict and asset['metadata'] == consumer.EMPTY_CONTENT_METADATA and
                    type(asset['metadata'].get('copyToOutput')) is bool and
                    asset['archive_sha256'] == restored[folded]['sha256'] and
                    (asset['path'], asset['kind']) not in seen, 'selected_payload_synthetic_content_identity')
                transport.digest(asset['archive_sha256'])
                seen.add((asset['path'], asset['kind']))
                continue
            transport.closed(asset, {'path', 'kind', 'sha256', 'size'}, 'selected_payload_asset')
            path = transport.safe_name(asset['path'])
            require(asset['kind'] in {'compile', 'runtime', 'contentFiles', 'build', 'buildMultiTargeting', 'native', 'runtimeTargets', 'resource'} and
                    (path, asset['kind']) not in seen, 'selected_payload_asset_kind')
            seen.add((path, asset['kind']))
            transport.digest(asset['sha256'])
            transport.integer(asset['size'], transport.MAX_PACKAGE_BYTES)
            key = folded, path
            if key in payloads:
                require(all(payloads[key][name] == asset[name] for name in ('sha256', 'size')), 'selected_payload_asset_ambiguity')
            payloads[key] = asset
            if folded in selected:
                data = selected[folded]['members'].get(path)
                require(data is not None and len(data) == asset['size'] and metadata.sha256(data) == asset['sha256'],
                        'selected_payload_asset_bytes')
    require(set(payload_rows) == set(restored), 'selected_payload_restored_payload_bijection')
    if runtime:
        evidence = transport.closed(cell['runtime'], {'contract', 'loaded_assemblies'}, 'selected_payload_runtime')
        contract = runtime_policy(plan, contracts)
        require(evidence['contract'] == contract['description'], 'selected_payload_runtime_contract')
        names = []
        for row in sequence(evidence['loaded_assemblies']):
            transport.closed(row, {'name', 'version', 'informationalVersion', 'sha256', 'package_id', 'package_version', 'package_asset'},
                             'selected_payload_loaded_assembly')
            name, version = text(row['name']), text(row['version'])
            text(row['informationalVersion'])
            folded = package_id(row['package_id']).casefold()
            asset = transport.safe_name(row['package_asset'])
            require(name.startswith('Elsa') and Path(asset).stem == name and folded in restored and
                    row['package_version'] == restored[folded]['version'] and (folded, asset) in payloads and
                    row['sha256'] == payloads[(folded, asset)]['sha256'], 'selected_payload_loaded_payload')
            require(any(item['path'] == asset and item['kind'] == 'runtime' for item in payload_rows[folded]['payloads']),
                    'selected_payload_loaded_runtime_asset')
            names.append(name)
            if folded in selected and plan['product'] != 'core':
                release = plan['requested_version'] if plan['product'] == 'studio' else '1.0.0'
                require(version == release.split('+')[0].split('-')[0] + '.0' and
                        row['informationalVersion'] == release + '+' + plan['source']['commit'],
                        'selected_payload_loaded_assembly_policy')
        transport.unique_names(names)
        require(set(contract['required_packages']) <= set(names), 'selected_payload_runtime_representative')


def validate_consumers(plan: dict, receipt: dict, result: dict, selected: dict, receipt_hash: str, contracts: dict, *, now: datetime) -> None:
    transport.closed(result, CONSUMER_KEYS, 'selected_payload_consumer')
    require(type(result['schema']) is int and result['schema'] == 1 and result['mode'] == 'selected-product-consumers' and
            result['success'] is True and result['published'] is False and result['stage'] == 'complete' and
            result['plan_sha256'] == receipt['plan_sha256'] and result['artifact_receipt_sha256'] == receipt_hash and
            result['source'] == plan['source'] and result['planner_controller'] == plan['controller'] and
            result['artifact_controller'] == receipt['artifact_controller'] and result['producer_execution'] == receipt['execution'],
            'selected_payload_consumer_identity')
    transport.validate_execution_pair(plan, receipt['plan_sha256'], receipt['execution'], result['execution'],
                                     receipt['artifact_controller'], result['controller'], now=now)
    require(result['producer_plan_admission'] == {'scope': 'historical-producer-start-only', 'eligible_at_producer_start': True,
        'plan_observed_at': plan['observed_at'], 'producer_started_at': receipt['execution']['started_at'],
        'producer_execution_id': receipt['execution']['id'], 'artifact_receipt_sha256': receipt_hash},
        'selected_payload_historical_admission')
    current = transport.closed(result['current_consumer_admission'], {'eligible', 'scope', 'checked_at', 'histories',
        'prerequisites', 'observations_sha256'}, 'selected_payload_current_admission')
    require(current['eligible'] is True and current['scope'] == 'current-complete-selected-product-prerequisites' and
            type(current['histories']) is int and current['histories'] == len(plan['histories']) and
            type(current['prerequisites']) is int and current['prerequisites'] == len(plan['prerequisites']) and
            transport.utc(current['checked_at'], now=now) >= transport.utc(result['execution']['started_at'], now=now),
            'selected_payload_current_admission')
    transport.digest(current['observations_sha256'])
    require(result['preflight'] == {'scope': 'standalone-semantics-utility-only', 'sdk_version': metadata.SDK,
        'native_assemblies': plan['semantics']['assemblies']} and result['runtime_contract_source'] == runtime_source(plan, contracts),
        'selected_payload_consumer_source')
    transport.digest(result['external_catalog_sha256'])
    contract = runtime_policy(plan, contracts)
    require(result['limitations'] == ['Complete selected restore/compile coverage is distinct from representative runtime behavior.',
        contract['limitation']], 'selected_payload_consumer_limitations')
    expected = {(row['id'], framework) for row in plan['inventory']['selected'] for framework in row['frameworks']}
    for runtime, rows in ((False, result['coverage']), (True, result['runtime'])):
        observed = []
        for row in sequence(rows):
            validate_cell(plan, row, selected, contracts, runtime=runtime)
            observed.append((row['id'], row['framework']))
        wanted = ({(selected[contract['package']]['record']['id'], framework)
            for framework in selected[contract['package']]['policy']['frameworks']} if runtime else expected)
        require(len(observed) == len(wanted) and set(observed) == wanted, 'selected_payload_cell_bijection')


def validate(files: dict[str, bytes], expected: dict, plan_hash: str, producer_hash: str, consumer_hash: str,
             *, contracts: dict, now: datetime | None = None) -> dict:
    """Validate canonical payload bytes; no caller as-of/backdated admission."""
    now = now or datetime.now(timezone.utc)
    transport.closed(expected, {'context', 'controller', 'product', 'line'}, 'selected_payload_expected')
    require(expected['product'] in ('core', 'studio', 'extensions') and expected['line'] in ('3.8', '3.9'),
            'selected_payload_cell_not_implemented')
    transport.validate_context(expected['context'], expected['controller'])
    transport.unique_names(list(files))
    for name, data in files.items():
        require(type(data) is bytes and len(data) <= transport.file_limit(name), 'selected_payload_size')
    for name, sha, limit in (('plan.json', plan_hash, transport.MAX_PLAN_BYTES),
        ('producer/receipt.json', producer_hash, transport.MAX_RECEIPT_BYTES),
        ('consumer/receipt.json', consumer_hash, transport.MAX_RECEIPT_BYTES)):
        require(name in files and metadata.sha256(files[name]) == transport.digest(sha), 'selected_payload_input_hash')
    plan = transport.strict_json(files['plan.json'], transport.MAX_PLAN_BYTES)
    plan_schema.validate(plan, contracts, now=now)
    extra = {'studio': {'npm'}, 'core': {'package_verification'}, 'extensions': {'manifest_verification'}}[plan['product']]
    receipt = transport.closed(transport.strict_json(files['producer/receipt.json']),
        (PRODUCER_KEYS - {'npm'}) | extra, 'selected_payload_producer')
    result = transport.strict_json(files['consumer/receipt.json'])
    transport.validate_execution(receipt['execution'], 'artifact', receipt['artifact_controller'], plan, plan_hash, now=now)
    require(receipt['execution']['context'] == expected['context'] and receipt['artifact_controller'] == expected['controller'] and
            (plan['product'], plan['line']) == (expected['product'], expected['line']),
            'selected_payload_expected_context')
    plan_schema.admit(plan, plan_hash, files['plan.json'], receipt['execution']['started_at'], contracts)
    require( type(receipt['schema']) is int and receipt['schema'] == 1 and
            receipt['mode'] == 'selected-product-artifact-control' and receipt['source'] == plan['source'] and
            receipt['product'] == plan['product'] and receipt['line'] == plan['line'] and receipt['version'] == plan['requested_version'] and
            receipt['plan_sha256'] == plan_hash and receipt['planner_controller'] == plan['controller'] and
            receipt['success'] is True and receipt['artifact_proof'] is True and receipt['stage'] == 'complete' and
            all(receipt[key] is False for key in ('published', 'version_allocated', 'tag_created')), 'selected_payload_producer_identity')
    allowed = {'plan.json', 'producer/receipt.json', 'consumer/receipt.json'} | (
        {'producer/npm/receipt.json'} if plan['product'] == 'studio' else set()) | {
        'producer/' + ('npm/' if name.endswith('.tgz') else 'nuget/') + name for name in plan['expected_artifacts']}
    require(set(files) == allowed, 'selected_payload_exact_file_set')
    if plan['product'] == 'studio':
        contract = contracts['studio'] if plan['line'] == '3.8' else contracts['studio39']
        require((contract['source_commit'], contract['source_tree']) == (plan['source']['commit'], plan['source']['tree']),
                'selected_payload_source_contract')
        preflight = transport.closed(receipt['preflight'], {'product_work_executed', 'node', 'npm', 'sdk', 'host_framework',
            'host_supported_frameworks', 'original_workflow_sha256', 'host_project_sha256'}, 'selected_payload_preflight')
        require(preflight['product_work_executed'] is False and preflight['sdk'] == metadata.SDK and
                type(preflight['node']) is str and re.fullmatch(r'v22\.[0-9]+\.[0-9]+', preflight['node']) and
                type(preflight['npm']) is str and re.fullmatch(r'[0-9]+\.[0-9]+\.[0-9]+', preflight['npm']) and
                int(preflight['npm'].split('.')[0]) >= 9 and preflight['host_framework'] == 'net10.0' and
                preflight['host_supported_frameworks'] == next(row['target_frameworks'] for row in plan['inventory']['projects']
                    if row['path'] == contract['files'][2]['path']) and
                preflight['original_workflow_sha256'] == contract['files'][3]['sha256'] and
                preflight['host_project_sha256'] == contract['files'][2]['sha256'], 'selected_payload_preflight')
    else:
        require(receipt['preflight'] == {'product_work_executed': False, 'sdk': metadata.SDK}, 'selected_payload_preflight')
    if plan['product'] != 'core':
        validate_tests(plan, receipt, contracts)
    selected = validate_packages(plan, receipt, files)
    if plan['product'] == 'studio':
        validate_npm(plan, receipt, files, contract)
    elif plan['product'] == 'extensions':
        extensions_payload.validate_specialization(plan, receipt, selected)
    validate_consumers(plan, receipt, result, selected, producer_hash, contracts, now=now)
    if plan['product'] == 'core':
        core_payload.validate_specialization(plan, receipt, result, selected, contracts=contracts['core'])
    return {'plan': plan, 'producer': receipt, 'consumer': result}


def runtime_policy(plan: dict, contracts: dict) -> dict:
    """Fixed source-supported behavior descriptions; no artifact-directed file IO."""
    contract = contracts['runtime'][plan['product']]
    if plan['product'] == 'core':
        return contract
    prefix = 'Studio' if plan['product'] == 'studio' else 'Extensions'
    original_prefix = prefix + '38'
    require(contract['description'].startswith(original_prefix), 'selected_payload_runtime_description')
    return contract | {'description': prefix + plan['line'].replace('.', '') +
                       contract['description'][len(original_prefix):]}


def runtime_source(plan: dict, contracts: dict) -> dict:
    if plan['product'] == 'core':
        return contracts['core']['consumer']['sources'][plan['line']]
    if plan['line'] == '3.9':
        return contracts['maintenance39']['sources'][plan['product']]['files']
    return extensions.SOURCE_BLOBS if plan['product'] == 'extensions' else consumer.STUDIO38_CONTRACT_SOURCE


def load_contracts() -> dict:
    """Fixed tracked controller inputs only; never artifact-declared paths."""
    candidates, register = producer.maintenance.CANDIDATES.read_bytes(), producer.maintenance.REGISTER.read_bytes()
    core = core_payload.load_contracts()
    fixtures = {product: (consumer.ROOT / ('scripts/integration-program/selected-' + product + '-consumer/Program.cs')).read_bytes()
                for product in ('studio', 'extensions', 'core')}
    original39 = transport.strict_json(maintenance39.CONTRACT.read_bytes())
    for product in ('studio', 'extensions'):
        require(metadata.sha256(fixtures[product]) == original39['sources'][product]['fixture']['sha256'],
                'selected_payload_fixture_contract')
    for path, sha in core['consumer']['fixtures'].items():
        require(metadata.sha256((consumer.ROOT / path).read_bytes()) == sha, 'selected_payload_core_fixture_contract')
    return {'studio': transport.strict_json(CONTRACT.read_bytes()),
        'studio39': transport.strict_json(CONTRACT.with_name('selected_studio39_payload_contract.json').read_bytes()),
        'core': core, 'maintenance39': original39,
        'runtime': {'studio': consumer.runtime_contract({'product': 'studio', 'line': '3.8', 'requested_version': ''}),
                    'extensions': consumer.runtime_contract({'product': 'extensions', 'line': '3.8'}),
                    'core': consumer.core.runtime_contract()},
        'candidates': transport.strict_json(candidates), 'register': transport.strict_json(register),
        'candidate_register_sha256': metadata.sha256(candidates), 'original_register_sha256': metadata.sha256(register),
        'ownership': metadata.ownership_policy(),
        'planner_inputs': {name: metadata.sha256((consumer.ROOT / name).read_bytes()) for name in producer.PLANNER_INPUTS},
        'fixtures': fixtures, 'fixture': fixtures['studio'],
        'assembly_proof': (consumer.ROOT / 'scripts/integration-program/selected-consumer/AssemblyProof.cs').read_bytes()}
