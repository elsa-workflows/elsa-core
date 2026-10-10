"""Pure Core specialization for the selected-control semantic seal.

Only load_contracts reads files, and only fixed tracked contracts beside this
module. validate_specialization never reads artifact-directed paths or runs tools.
"""
from __future__ import annotations

import base64
import binascii
import hashlib
from pathlib import Path, PurePosixPath
import re
from uuid import UUID

import core_source_continuation as continuation
import prove_consolidated_packages as archives
from prove_consolidated_packages import require
from selected_control_transport import MAX_PACKAGE_BYTES, closed, digest, integer, safe_name, unique_names, strict_json

FRAMEWORKS = ('net8.0', 'net9.0', 'net10.0')
SAMPLE_PROJECT = 'src/apps/Elsa.SamplePackage/Elsa.SamplePackage.csproj'
REQUIRED_ASSEMBLIES = {'Elsa', 'Elsa.Workflows.Core', 'Elsa.Workflows.Runtime'}
RUNTIME_DESCRIPTION = 'Core original nested variable-scope workflow executes with exact output and Finished/Finished'
COUNTERS = {'total', 'executed', 'passed', 'failed', 'error', 'timeout', 'aborted', 'inconclusive',
            'passedButRunAborted', 'notRunnable', 'notExecuted', 'disconnected', 'warning', 'completed', 'inProgress', 'pending'}
NATIVE_KEYS = {'id', 'version', 'frameworks', 'assembly_name', 'include_build_output', 'satellites',
               'dependencies', 'repository', 'symbols', 'package_manifest', 'sdk_assets', 'files'}
SYMBOL_KEYS = {'assembly', 'assembly_sha256', 'pdb', 'pdb_sha256', 'symbol', 'assembly_version',
               'informational_version', 'documents'}


def load_contracts() -> dict:
    """Load trusted controller contracts, separately from the pure validator."""
    return {name: strict_json(Path(__file__).with_name(filename).read_bytes()) for name, filename in
            (('producer', 'selected_core_contract.json'), ('consumer', 'selected_core_consumer_contract.json'),
             ('continuation', 'core_source_continuation_contract.json'))}


def _rows(value: object, label: str) -> list:
    """Require a list of evidence rows using the supplied failure label."""
    require(type(value) is list, label)
    return value


def _text(value: object) -> str:
    """Require nonempty text without control characters."""
    require(type(value) is str and bool(value) and not any(ord(c) < 32 or ord(c) == 127 for c in value),
            'core_payload_text')
    return value


def _hash(data: bytes) -> str:
    """Return the SHA-256 digest of payload bytes."""
    return hashlib.sha256(data).hexdigest()


def _member(members: dict, path: str, expected: str) -> bytes:
    """Return a safe archive member only if its bytes match the expected digest."""
    safe_name(path)
    digest(expected)
    require(path in members and type(members[path]) is bytes and _hash(members[path]) == expected,
            'core_payload_member')
    return members[path]


def _source(plan: dict, contracts: dict) -> dict:
    """Validate the selected Core source and derive its admitted producer policy."""
    closed(contracts, {'producer', 'consumer', 'continuation'}, 'core_payload_contracts')
    source = plan['source']
    value = contracts['producer']['sources'].get(plan['line'])
    if source.get('kind') == continuation.KIND:
        require(value is not None, 'core_payload_original_source')
        value = continuation.policy(source, value, contracts['continuation'])
    require(value is not None and plan['product'] == source['product'] == 'core' and
            plan['line'] == source['line'] and source['kind'] in ('observed-core-release-branch', continuation.KIND) and
            (source['commit'], source['tree']) == (value['commit'], value['tree']) and plan['npm'] is None,
            'core_payload_original_source')
    inventory = plan['inventory']
    continuation.validate_project_metadata(source, inventory['selected'], contracts['continuation'])
    require(inventory['release_recipe']['solution'] == 'Elsa.sln' and
            inventory['release_recipe']['workflow'] == '.github/workflows/packages.yml', 'core_payload_recipe')
    tests = [row['path'] for row in inventory['projects'] if row['is_test_project']]
    require(len(tests) == len(set(tests)) and set(tests) == set(value['test_projects']), 'core_payload_test_inventory')
    for row in inventory['selected']:
        safe_name(row['project'])
        require(type(row['symbols']) is bool and (row['symbols'] or
                (row['id'], row['project']) == ('Elsa.SamplePackage', SAMPLE_PROJECT)), 'core_payload_symbols_policy')
    return value


def _skip_groups(contract: dict) -> list[dict[str, str]]:
    """Retained rows expose which whole provider gate was skipped, never its env."""
    groups = [{row['display_identity']: row['reason'] for row in contract['declared_skips']}]
    conditional = contract['conditional']
    if conditional:
        for gate in conditional['gates']:
            reason = (f"{gate['description']} is not covered by this run: set {gate['env']} "
                      'to a connection string to include it.')
            groups.append({'Elsa.UserTasks.Persistence.ConformanceTests.' + cls['class'] + '.' + method['method']: reason
                for cls in conditional['classes'] if cls['provider'] == gate['provider']
                for method in conditional['methods'] if method['base_class'] == cls['base_class']})
    return groups


def _tests(value: dict, contract: dict) -> None:
    """Validate the complete Core test census, counters, and source-bound skip sets."""
    closed(value, {'executions', 'inherited_skipped_placeholders'}, 'core_payload_tests')
    require(value['inherited_skipped_placeholders'] == [], 'core_payload_test_placeholders')
    seen, skipped = set(), {}
    for row in _rows(value['executions'], 'core_payload_test_rows'):
        closed(row, {'project', 'framework', 'counters', 'sha256', 'expected_skips'}, 'core_payload_test')
        safe_name(row['project'])
        cell = (row['project'], row['framework'])
        require(cell not in seen, 'core_payload_test_duplicate')
        seen.add(cell)
        digest(row['sha256'])
        counts = closed(row['counters'], COUNTERS, 'core_payload_counters')
        for number in counts.values():
            integer(number, 2**63 - 1)
        cell_skips = {}
        for case in _rows(row['expected_skips'], 'core_payload_skips'):
            closed(case, {'identity', 'reason'}, 'core_payload_skip')
            identity, reason = _text(case['identity']), _text(case['reason'])
            require(identity not in skipped and identity not in cell_skips, 'core_payload_skip_duplicate')
            prefix = ('Elsa.Workflows.ComponentTests.' if PurePosixPath(row['project']).stem == 'Elsa.Workflows.ComponentTests'
                else 'Elsa.UserTasks.Persistence.ConformanceTests.' if
                PurePosixPath(row['project']).stem == 'Elsa.UserTasks.Persistence.ConformanceTests' else None)
            require(prefix is not None and identity.startswith(prefix), 'core_payload_skip_cell')
            cell_skips[identity] = reason
        skipped.update(cell_skips)
        require(counts['passed'] > 0 and counts['total'] == counts['passed'] + len(cell_skips) and
                counts['executed'] == counts['passed'] and counts['notExecuted'] in (0, len(cell_skips)) and
                all(number == 0 for key, number in counts.items() if key not in ('total', 'passed', 'executed', 'notExecuted')),
                'core_payload_test_counts')
    require(seen == {(project, 'net10.0') for project in contract['test_projects']}, 'core_payload_test_cells')
    groups = _skip_groups(contract)
    require(all(skipped.get(key) == reason for key, reason in groups[0].items()), 'core_payload_declared_skips')
    allowed = dict(groups[0])
    for group in groups[1:]:
        present = set(group) & set(skipped)
        require(not present or present == set(group), 'core_payload_conditional_skip_gate')
        allowed.update(group if present else {})
    require(skipped == allowed, 'core_payload_skip_contract')


def _producer(value: dict) -> None:
    """Validate the closed schema and hashes for SDK or generator provenance evidence."""
    kind = value.get('kind')
    if kind == 'sdk' and 'compiler_sha256' in value:
        closed(value, {'kind', 'sdk_version', 'compiler_sha256', 'frameworks'}, 'core_payload_sdk_producer')
        digest(value['compiler_sha256'])
        packs = _rows(value['frameworks'], 'core_payload_sdk_frameworks')
        require(bool(packs), 'core_payload_sdk_frameworks')
        for pack in packs:
            closed(pack, {'Identity', 'TargetingPackName', 'TargetingPackVersion'}, 'core_payload_sdk_framework')
            for item in pack.values():
                _text(item)
    elif kind == 'sdk':
        closed(value, {'kind', 'sdk_version', 'content_sha256', 'source_emitting_targets_sha256'}, 'core_payload_sdk_tool')
        digest(value['content_sha256']); digest(value['source_emitting_targets_sha256'])
    elif kind == 'framework':
        closed(value, {'kind', 'package_id', 'package_version', 'content_sha256'}, 'core_payload_framework_tool')
        digest(value['content_sha256'])
        _text(value['package_id']); _text(value['package_version'])
    else:
        closed(value, {'kind', 'package_id', 'package_version', 'archive_sha256', 'restore_sha512', 'nuget_content_hash',
                       'signed', 'archive_entry', 'content_sha256', 'targeting_pack', 'checked_archive_contents'},
               'core_payload_nuget_tool')
        require(kind == 'nuget' and type(value['signed']) is bool and type(value['targeting_pack']) is bool,
                'core_payload_nuget_tool')
        _text(value['package_id']); _text(value['package_version']); safe_name(value['archive_entry'])
        for key in ('archive_sha256', 'content_sha256'):
            digest(value[key])
        require(value['restore_sha512'] == value['nuget_content_hash'], 'core_payload_generator_hash')
        for key in ('restore_sha512', 'nuget_content_hash'):
            require(type(value[key]) is str and len(base64.b64decode(value[key], validate=True)) == 64,
                    'core_payload_generator_hash')
        require(type(value['checked_archive_contents']) is dict and bool(value['checked_archive_contents']),
                'core_payload_generator_contents')
        unique_names(list(value['checked_archive_contents']))
        for path, checksum in value['checked_archive_contents'].items():
            safe_name(path); digest(checksum)
        require(value['checked_archive_contents'].get(value['archive_entry']) == value['content_sha256'],
                'core_payload_generator_content')
    if 'sdk_version' in value:
        _text(value['sdk_version'])


def _manifest_hint_producer(value: dict, policy: dict) -> None:
    """Admit only Core's pinned hint sources, joined to the selected plan's restore."""
    closed(value, {'external_package', 'archive_entry', 'archive_sha256', 'feed', 'restore_sha512'},
           'core_payload_manifest_hint_producer')
    identifier, version = 'Elsa.Platform.PackageManifest.Generator', '0.0.1-preview.53'
    require(value['external_package'] == identifier + '/' + version and
            value['archive_sha256'] == archives.GENERATOR_SOURCE_PINS[version] and
            value['feed'] == archives.GENERATOR_FEED and
            value['archive_entry'] in archives.GENERATOR_SOURCE_ENTRIES[version], 'core_payload_manifest_hint_identity')
    checksum = value['restore_sha512']
    require(type(checksum) is str, 'core_payload_manifest_hint_restore')
    decoded = base64.b64decode(checksum, validate=True)
    require(len(decoded) == 64 and base64.b64encode(decoded).decode() == checksum,
            'core_payload_manifest_hint_restore')
    targets = _rows(policy['metadata']['manifest_content_targets'], 'core_payload_manifest_hint_targets')
    require(len(targets) == 1, 'core_payload_manifest_hint_targets')
    target = closed(targets[0], {'id', 'version', 'restore_sha512', 'entry', 'sha256'},
                    'core_payload_manifest_hint_target')
    require(_text(target['id']).casefold() == identifier.casefold() and target['version'] == version and
            target['restore_sha512'] == checksum and
            target['entry'] == 'build/Elsa.Platform.PackageManifest.Generator.targets',
            'core_payload_manifest_hint_plan_restore')
    digest(target['sha256'])


def _documents(row: dict, commit: str, policy: dict) -> None:
    """Validate source document provenance or the explicit no-documents applicability branch."""
    documents = _rows(row['documents'], 'core_payload_documents')
    seen = set()
    for document in documents:
        source = document.get('source')
        if source == 'original-git':
            closed(document, {'path', 'source', 'url', 'algorithm', 'checksum'}, 'core_payload_document')
            safe_name(document['path'])
            require(not any(c in document['path'] for c in ('?', '#', '%')) and
                    document['url'] == 'https://raw.githubusercontent.com/elsa-workflows/elsa-core/' + commit + '/' + document['path'],
                    'core_payload_sourcelink')
        else:
            closed(document, {'path', 'source', 'family', 'producer', 'algorithm', 'checksum'}, 'core_payload_embedded_document')
            require(source == 'embedded' and re.fullmatch(r'\[embedded\]/document-[1-9][0-9]*', document['path']) is not None and
                    document['family'] in {'sdk', 'manifest-hints', *archives.GENERATOR_TOOLS}, 'core_payload_embedded_family')
            family, producer = document['family'], document['producer']
            if family == 'manifest-hints':
                _manifest_hint_producer(producer, policy)
            else:
                _producer(producer)
                require((family == 'sdk') == ('compiler_sha256' in producer), 'core_payload_embedded_producer')
                if family == 'razor':
                    require(producer['kind'] == 'sdk' and 'content_sha256' in producer, 'core_payload_embedded_producer')
                elif family != 'sdk':
                    require(producer['kind'] in ('framework', 'nuget') and
                            producer['package_id'] in archives.GENERATOR_TOOLS[family][1], 'core_payload_embedded_producer')
        require(document['path'] not in seen and document['algorithm'] in ('sha1', 'sha256'), 'core_payload_document_identity')
        seen.add(document['path'])
        digest(document['checksum'], 40 if document['algorithm'] == 'sha1' else 64)
    if not documents:
        applicability = closed(row['source_applicability'], {'classification', 'document_count', 'executable_method_bodies',
            'nonabstract_methods_without_body', 'native_or_external_methods', 'nonmodule_types', 'reference_assembly'},
            'core_payload_source_applicability')
        require(applicability['classification'] == 'no-documents-no-executable-method-bodies' and
                applicability['reference_assembly'] is False, 'core_payload_source_applicability')
        integer(applicability['nonmodule_types'], 2**31 - 1, positive=True)
        for key in ('document_count', 'executable_method_bodies', 'nonabstract_methods_without_body', 'native_or_external_methods'):
            require(type(applicability[key]) is int and applicability[key] == 0, 'core_payload_source_applicability')


def _symbols(row: dict, policy: dict, members: dict, symbol_inventory: dict, commit: str) -> None:
    """Validate assembly and packaged-PDB joins plus source metadata.

    Private-PDB and source-document checksum-content judgments remain inherited.
    """
    private = policy['symbols'] is False
    keys = SYMBOL_KEYS | ({'source_applicability'} if not row.get('documents') else set()) | ({'symbol_package'} if private else set())
    closed(row, keys, 'core_payload_symbol')
    assembly = safe_name(row['assembly'])
    require(row['pdb'] == assembly[:-4] + '.pdb', 'core_payload_pdb_path')
    _member(members, assembly, row['assembly_sha256'])
    digest(row['pdb_sha256'])
    if private:
        require((policy['id'], policy['project']) == ('Elsa.SamplePackage', SAMPLE_PROJECT) and
                row['symbol_package'] is False, 'core_payload_private_pdb')
    else:
        require(row['pdb'] in symbol_inventory and symbol_inventory[row['pdb']]['sha256'] == row['pdb_sha256'],
                'core_payload_pdb_archive')
    require(type(row['assembly_version']) is str and re.fullmatch(r'\d+\.\d+\.\d+\.\d+', row['assembly_version']) is not None and
            type(row['informational_version']) is str and len(row['informational_version']) > 41 and
            row['informational_version'].endswith('+' + commit), 'core_payload_assembly_identity')
    symbol = closed(row['symbol'], {'key', 'pdb_name', 'guid', 'stamp', 'checksum_algorithm', 'declared_checksum',
                                   'normalized_checksum', 'pdb_sha256', 'pdb_size'}, 'core_payload_native_symbol')
    name = PurePosixPath(row['pdb']).name.lower()
    guid = str(UUID(symbol['guid']))
    require(symbol['guid'] == guid and symbol['pdb_name'] == name and
            symbol['key'] == f"{name}/{guid.replace('-', '')}FFFFFFFF/{name}" and
            symbol['pdb_sha256'] == row['pdb_sha256'], 'core_payload_symbol_identity')
    integer(symbol['stamp'], 2**32 - 1)
    integer(symbol['pdb_size'], 32 * 1024**2, positive=True)
    require(symbol['checksum_algorithm'] in ('SHA1', 'SHA256') and
            symbol['declared_checksum'] == symbol['normalized_checksum'], 'core_payload_symbol_checksum')
    digest(symbol['declared_checksum'], 40 if symbol['checksum_algorithm'] == 'SHA1' else 64)
    if not private:
        require(symbol['pdb_size'] == symbol_inventory[row['pdb']]['size'], 'core_payload_pdb_size')
    _documents(row, commit, policy)


def _manifest(native: dict, policy: dict, members: dict, version: str) -> None:
    """Validate generated manifest and SDK asset evidence against original package policy."""
    policies = policy['metadata']['original_output_policy']
    required = {p['ElsaPackageManifestPackagePath'] for p in policies.values() if
                p['GenerateElsaPackageManifest'].lower() == p['ElsaPackageManifestIncludeInPackage'].lower() == 'true'}
    require(len(required) <= 1, 'core_payload_manifest_policy')
    manifest = native['package_manifest']
    require(not archives.ADMISSION_SHELL_FEATURES.get(policy['id']) or bool(required), 'core_payload_manifest_required')
    if not required:
        require(manifest is None and 'elsa-package.json' not in members, 'core_payload_manifest_absent')
    else:
        keys = {'path', 'sha256', 'id', 'version', 'frameworks'}
        catalog = policy['id'] in archives.ADMISSION_SHELL_FEATURES
        closed(manifest, keys | ({'selectable_features', 'runtime_kinds'} if catalog else set()), 'core_payload_manifest')
        require(manifest['path'] == next(iter(required)) and manifest['id'] == policy['id'] and
                manifest['version'] == version and manifest['frameworks'] == policy['frameworks'], 'core_payload_manifest_identity')
        data = strict_json(_member(members, manifest['path'], manifest['sha256']))
        require(data.get('package', {}).get('id') == policy['id'] and data.get('package', {}).get('version') == version and
                set(data.get('extensions', {}).get('targetFrameworks', [])) == set(policy['frameworks']) and
                data.get('extensions', {}).get('repositoryUrl', '').rstrip('/') == 'https://github.com/elsa-workflows/elsa-core',
                'core_payload_manifest_bytes')
        if catalog:
            require({key: manifest[key] for key in ('selectable_features', 'runtime_kinds')} ==
                    archives.verify_admission_catalog(data, policy), 'core_payload_manifest_catalog')
    actual = {path for path in members if path.split('/')[0].casefold() in {'build', 'buildtransitive', 'buildmultitargeting'} or
              path in required or path == 'elsa-package.json'}
    assets = _rows(native['sdk_assets'], 'core_payload_sdk_assets')
    paths = []
    for asset in assets:
        closed(asset, {'path', 'sha256'}, 'core_payload_sdk_asset')
        paths.append(asset['path']); _member(members, asset['path'], asset['sha256'])
    unique_names(paths)
    require(set(paths) == actual, 'core_payload_sdk_inventory')


def _excluded_outputs(plan: dict, producer: dict, selected: dict) -> None:
    """Admit recipe-only metadata without admitting or reading excluded bytes."""
    allowed = {row['id'].casefold() for row in plan['inventory']['excluded'] if row['id']}
    records = _rows(producer['packages']['private_recipe_only_outputs'], 'core_payload_private_outputs')
    selected_files = {record['file'].casefold() for record in producer['packages']['selected']}
    files = []
    for record in records:
        closed(record, {'file', 'id', 'version', 'sha256', 'size', 'inventory'}, 'core_payload_excluded_output')
        name, identifier = safe_name(record['file']), _text(record['id'])
        require('/' not in name and name.endswith(('.nupkg', '.snupkg')) and
                identifier.casefold() in allowed and identifier.casefold() not in selected and
                name.casefold() not in selected_files and record['version'] == plan['requested_version'],
                'core_payload_excluded_identity')
        files.append(name)
        digest(record['sha256'])
        integer(record['size'], MAX_PACKAGE_BYTES, positive=True)
        inventory = _rows(record['inventory'], 'core_payload_excluded_inventory')
        require(bool(inventory), 'core_payload_excluded_inventory')
        paths = []
        for member in inventory:
            closed(member, {'path', 'size', 'sha256'}, 'core_payload_excluded_member')
            paths.append(safe_name(member['path']))
            integer(member['size'], MAX_PACKAGE_BYTES)
            digest(member['sha256'])
        unique_names(paths)
        integer(sum(member['size'] for member in inventory), MAX_PACKAGE_BYTES)
    unique_names(files)


def _native(plan: dict, producer: dict, selected: dict) -> dict:
    """Validate Core package receipts against admitted bytes and apply the selected assembly, symbol, and source-metadata checks."""
    _excluded_outputs(plan, producer, selected)
    rows = _rows(producer['package_verification'], 'core_payload_native_rows')
    require(len(rows) == len(selected), 'core_payload_native_inventory')
    native_by_id = {}
    records = producer['packages']['selected']
    for native in rows:
        closed(native, NATIVE_KEYS, 'core_payload_native')
        folded = native['id'].casefold()
        require(folded in selected and folded not in native_by_id, 'core_payload_native_inventory')
        native_by_id[folded] = native
        item, version = selected[folded], plan['requested_version']
        policy, members = item['policy'], item['members']
        require(native['id'] == policy['id'] and native['version'] == version, 'core_payload_native_identity')
        files = _rows(native['files'], 'core_payload_native_files')
        for file in files:
            closed(file, {'name', 'sha256', 'size'}, 'core_payload_native_file')
            safe_name(file['name']); digest(file['sha256']); integer(file['size'], MAX_PACKAGE_BYTES, positive=True)
        expected = [{'name': r['file'], 'sha256': r['sha256'], 'size': r['size']} for r in records if r['id'] == native['id']]
        require(sorted(files, key=lambda r: r['name']) == sorted(expected, key=lambda r: r['name']), 'core_payload_native_files')
        paired = [r for r in records if r['id'] == native['id'] and r['file'].endswith('.snupkg')]
        require(len(paired) == int(policy['symbols']), 'core_payload_symbols_partition')
        symbol_inventory = {r['path']: r for r in paired[0]['inventory']} if paired else {}
        policies = policy['metadata']['original_output_policy']
        emitted = {f for f in policy['frameworks'] if policies[f]['IncludeBuildOutput'].lower() != 'false'}
        require(type(native['include_build_output']) is bool and native['include_build_output'] == bool(emitted) and
                len(_rows(native['frameworks'], 'core_payload_native_frameworks')) == len(emitted) and set(native['frameworks']) == emitted, 'core_payload_output_policy')
        assembly = _text(native['assembly_name'])
        require('/' not in assembly and '\\' not in assembly, 'core_payload_assembly_name')
        expected_dlls = {f'lib/{f}/{assembly}.dll' for f in emitted}
        satellites = _rows(native['satellites'], 'core_payload_satellites')
        satellite_paths = []
        for satellite in satellites:
            closed(satellite, {'framework', 'culture', 'target_path', 'final_output_path', 'package_path', 'sha256'}, 'core_payload_satellite')
            culture = satellite['culture']
            require(type(culture) is str and re.fullmatch(r'[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*', culture) is not None and
                    satellite['framework'] in emitted and satellite['target_path'] == f'{culture}/{assembly}.resources.dll' and
                    satellite['package_path'] == f"lib/{satellite['framework']}/{satellite['target_path']}", 'core_payload_satellite_identity')
            safe_name(satellite['final_output_path'])
            _member(members, satellite['package_path'], satellite['sha256'])
            satellite_paths.append(satellite['package_path'])
        unique_names(satellite_paths)
        require(set(satellite_paths).isdisjoint(expected_dlls) and
                {p for p in members if p.startswith('lib/') and p.endswith('.dll')} == expected_dlls | set(satellite_paths),
                'core_payload_dll_inventory')
        symbols = _rows(native['symbols'], 'core_payload_symbols')
        require(len(symbols) == len(expected_dlls) and {s['assembly'] for s in symbols} == expected_dlls,
                'core_payload_symbol_inventory')
        for symbol in symbols:
            _symbols(symbol, policy, members, symbol_inventory, plan['source']['commit'])
        nuspecs = [p for p in members if p.endswith('.nuspec')]
        require(len(nuspecs) == 1, 'core_payload_nuspec')
        metadata = archives.parse_metadata(members[nuspecs[0]])
        require(native['dependencies'] == archives.dependency_groups(metadata) and
                type(native['repository']) is dict and all(type(k) is str and type(v) is str for k, v in native['repository'].items()) and
                metadata.find('repository') is not None and native['repository'] == metadata.find('repository').attrib,
                'core_payload_native_nuspec')
        _manifest(native, policy, members, version)
    return native_by_id


def _runtime(plan: dict, consumer: dict, selected: dict, native: dict, contracts: dict) -> None:
    """Join loaded Core assembly evidence to native policy and restored runtime assets."""
    require(consumer['runtime_contract_source'] == contracts['consumer']['sources'][plan['line']], 'core_payload_runtime_source')
    require({n.casefold() for n in REQUIRED_ASSEMBLIES} <= set(selected) and
            len(selected['elsa']['policy']['frameworks']) == 3 and set(selected['elsa']['policy']['frameworks']) == set(FRAMEWORKS),
            'core_payload_runtime_packages')
    rows = _rows(consumer['runtime'], 'core_payload_runtime')
    require(len(rows) == 3 and {(r['id'], r['framework']) for r in rows} == {('Elsa', f) for f in FRAMEWORKS}, 'core_payload_runtime_cells')
    for row in rows:
        runtime = closed(row['runtime'], {'contract', 'loaded_assemblies'}, 'core_payload_runtime_result')
        require(runtime['contract'] == RUNTIME_DESCRIPTION, 'core_payload_runtime_contract')
        loaded = _rows(runtime['loaded_assemblies'], 'core_payload_loaded')
        names = set()
        for assembly in loaded:
            closed(assembly, {'name', 'version', 'informationalVersion', 'sha256', 'package_id', 'package_version', 'package_asset'},
                   'core_payload_loaded_assembly')
            name = _text(assembly['name'])
            require(name.startswith('Elsa') and name not in names, 'core_payload_loaded_duplicate')
            names.add(name)
            _text(assembly['version']); _text(assembly['informationalVersion']); digest(assembly['sha256'])
            asset = safe_name(assembly['package_asset'])
            require(PurePosixPath(asset).stem == name, 'core_payload_loaded_name')
            folded = assembly['package_id'].casefold()
            if folded in selected:
                item = selected[folded]
                parts = asset.split('/')
                require(assembly['package_version'] == plan['requested_version'] and len(parts) == 3 and
                        parts[0] == 'lib' and parts[1] in item['policy']['frameworks'] and parts[2] == name + '.dll',
                        'core_payload_loaded_package')
                _member(item['members'], asset, assembly['sha256'])
                policies = [s for s in native[folded]['symbols'] if s['assembly'] == asset]
                require(len(policies) == 1 and (assembly['sha256'], assembly['version'], assembly['informationalVersion']) ==
                        (policies[0]['assembly_sha256'], policies[0]['assembly_version'], policies[0]['informational_version']),
                        'core_payload_loaded_native_identity')
            else:
                _text(assembly['package_id']); _text(assembly['package_version'])
            # Common coverage joins prove these asset bytes were restored for this cell.
            require(any(p['id'].casefold() == folded and p['version'] == assembly['package_version'] and
                        any(v['path'] == asset and v['sha256'] == assembly['sha256'] and v['kind'] == 'runtime'
                            for v in p['payloads']) for p in row['restored_payloads']), 'core_payload_loaded_restored')
        require(REQUIRED_ASSEMBLIES <= names, 'core_payload_loaded_required')


def validate_specialization(plan: dict, producer_receipt: dict, consumer_receipt: dict,
                            selected_archives: dict[str, dict], *, contracts: dict) -> None:
    """Check Core joins after shared envelope/archive/coverage admission.

    Native execution claims remain inherited; retained JSON is not a rerun.
    """
    try:
        contract = _source(plan, contracts)
        require('npm' not in producer_receipt and 'npm' not in consumer_receipt, 'core_payload_npm')
        _tests(producer_receipt['product_tests'], contract)
        native = _native(plan, producer_receipt, selected_archives)
        _runtime(plan, consumer_receipt, selected_archives, native, contracts)
    except (KeyError, TypeError, AttributeError, IndexError, OverflowError, binascii.Error) as error:
        raise ValueError('core_payload_schema') from error
