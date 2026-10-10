"""Six source-bound synthetic byte controls; no product/native execution evidence."""
from copy import deepcopy
from datetime import datetime, timedelta, timezone
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET
from unittest.mock import patch

import product_release_metadata as metadata
import selected_control_seal as seal
import test_selected_control_seal as seal_tests
import selected_control_transport as transport
import selected_studio_payload as payload
import selected_studio_plan_schema as schema
from test_selected_studio_payload import fixture as studio_fixture, encoded
from test_selected_core_payload import fixture as core_fixture
from test_selected_control_transport import zipped, Fixture as ProviderFixture

SHAPES = Path(__file__).with_name('selected_product_plan_shapes.json')


def fixture(product, line, *, source_mutation=None):
    if product == 'studio':
        return studio_fixture(line)
    files, expected, now = studio_fixture()
    plan, producer, consumer = (json.loads(files[name]) for name in ('plan.json', 'producer/receipt.json', 'consumer/receipt.json'))
    contracts = payload.load_contracts()
    historic = json.loads(SHAPES.read_bytes())['cells'][product + '-' + line]
    source = deepcopy(historic['source'])
    if product == 'core':
        # A synthetic current observation retains the original source/ref/tag shape.
        source['observation']['observed_at'] = plan['observed_at']
        for key in ('branch_observation', 'tag_observation'):
            source['observation'][key]['observed_at'] = plan['observed_at']
    if source_mutation is not None:
        source_mutation(source)
    version = line + '.999'
    selected, native = {}, None
    template = deepcopy(plan['inventory']['selected'][0])
    if product == 'core':
        _, core_producer, core_consumer, selected, _ = core_fixture(line)
        native = core_producer['package_verification']
        producer['product_tests'] = core_producer['product_tests']
        records = core_producer['packages']['selected']
    else:
        records = []
        for identifier in ('Elsa.IO.Http', 'Elsa.IO'):
            policy = deepcopy(template)
            policy.update(id=identifier, project='src/modules/io/' + identifier + '/' + identifier + '.csproj')
            policy['metadata']['dependency_groups'] = []
            entries = {f'lib/{target}/{identifier}.dll': (identifier + target).encode() for target in policy['frameworks']}
            if identifier == 'Elsa.IO.Http':
                for values in policy['metadata']['original_output_policy'].values():
                    values.update(GenerateElsaPackageManifest='true', ElsaPackageManifestIncludeInPackage='true',
                                  ElsaPackageManifestPackagePath='elsa-package.json')
                entries['elsa-package.json'] = encoded({'schemaVersion': '1.0', 'package': {'id': identifier, 'version': version},
                    'compatibility': {'runtimeKinds': ['elsa.server']}, 'extensions': {'targetFrameworks': policy['frameworks'],
                    'repositoryUrl': 'https://github.com/elsa-workflows/elsa-core'}, 'features': [
                    {'id': 'Elsa.IO.Http.HTTP', **payload.extensions.MANIFEST_FEATURE,
                     'dependencies': [{'featureId': 'Elsa.IO.Http.I/O'}]}]})
            spec = (f'<package><metadata><id>{identifier}</id><version>{version}</version>'
                f'<repository type="git" url="https://github.com/elsa-workflows/elsa-core" commit="{source["commit"]}" />'
                '</metadata></package>').encode()
            entries['a.nuspec'] = spec
            own = []
            for suffix, members in (('nupkg', entries), ('snupkg', {'a.nuspec': spec, 'lib/net10.0/' + identifier + '.pdb': b'pdb'})):
                data = zipped(list(members.items()))
                record = {'file': f'{identifier}.{version}.{suffix}', 'id': identifier, 'version': version,
                          'sha256': metadata.sha256(data), 'size': len(data), 'inventory': transport.inventory(members)}
                records.append(record); own.append((record, data))
            selected[identifier.casefold()] = {'record': own[0][0], 'members': entries, 'policy': policy, 'data': own[0][1]}
            for record, data in own:
                files['producer/nuget/' + record['file']] = data
    files = {name: data for name, data in files.items() if not name.startswith('producer/npm/') and
             (not name.startswith('producer/nuget/') or name.split('/')[-1] in {r['file'] for r in records})}
    if product == 'core':
        for record in records:
            if record['file'].endswith('.nupkg'):
                data = selected[record['id'].casefold()]['data']
            else:
                # Core's fixture records bind original PDB bytes; reproduce its exact ZIP order.
                item = selected[record['id'].casefold()]
                members = {record['id'] + '.nuspec': item['members'][record['id'] + '.nuspec']} | {
                    f'lib/{f}/{record["id"]}.pdb': (record['id'] + f + '.pdb').encode() for f in item['policy']['frameworks']}
                data = zipped(list(members.items()))
                record.update(sha256=metadata.sha256(data), size=len(data), inventory=transport.inventory(members))
                next(r for r in native if r['id'] == record['id'])['files'] = [
                    {'name': r['file'], 'sha256': r['sha256'], 'size': r['size']} for r in records if r['id'] == record['id']]
            files['producer/nuget/' + record['file']] = data
        for item in selected.values():
            policy = item['policy']; old = deepcopy(policy['metadata']['original_output_policy'])
            policy.update({key: deepcopy(template[key]) for key in ('project_url', 'repository_url', 'symbol_format')})
            policy['metadata'] = deepcopy(template['metadata']); policy['metadata']['dependency_groups'] = []
            policy['metadata']['original_output_policy'] = {f: deepcopy(template['metadata']['original_output_policy'][f]) | values for f, values in old.items()}
    for record in records:
        record['inventory'].sort(key=lambda row: row['path'])
    policies = [item['policy'] for item in selected.values()]
    tests = historic['test_projects']
    projects = [{'path': r['project'], 'package_id': r['id'], 'is_packable': True, 'is_test_project': False,
        'target_frameworks': r['frameworks'], 'references_by_framework': {f: {'project_references': [], 'package_references': []} for f in r['frameworks']},
        'project_references': [], 'package_references': []} for r in policies]
    projects += [{'path': path, 'package_id': Path(path).stem, 'is_packable': False, 'is_test_project': True,
        'target_frameworks': targets, 'references_by_framework': {f: {'project_references': [{'target_project': policies[0]['project']}], 'package_references': []} for f in targets},
        'project_references': [{'target_project': policies[0]['project']}], 'package_references': []} for path, targets in tests.items()]
    plan.update(product=product, line=line, source=source, requested_version=version, requested_version_input=version, npm=None,
                expected_artifacts=[r['file'] for r in records], excluded_artifacts=[{'id': name, 'reason': 'studio_only'} for name in payload.producer.planner.NPM_IDS])
    inventory = plan['inventory']
    inventory.update(source_commit=source['commit'], source_tree=source['tree'], requested_version=version,
        projects=projects, selected=policies, excluded=[{'id': r['package_id'], 'project': r['path'], 'reason': 'source_nonpackable'} for r in projects if not r['is_packable']],
        applicable_tests=sorted(tests), release_recipe=historic['recipe'] | {'projects': [r['path'] for r in projects]})
    if line == '3.9' and product == 'extensions':
        for row in contracts['maintenance39']['sources']['extensions']['excluded_canonical']:
            inventory['excluded'].append(deepcopy(row))
            r = deepcopy(projects[0]); r.update(path=row['project'], package_id=row['id']); projects.append(r)
            inventory['release_recipe']['projects'].append(row['project'])
    inventory['sha256'] = metadata.canonical_hash(metadata.public_inventory(inventory))
    history = deepcopy(plan['histories'][0]); history['feeds'][0]['decision']['normalized'] = version
    plan['histories'] = [deepcopy(history) for _ in policies]
    for row, policy in zip(plan['histories'], policies):
        row['id'] = row['feeds'][0]['id'] = policy['id']
    files['plan.json'] = encoded(plan); plan_hash = metadata.sha256(files['plan.json'])
    producer.update(product=product, line=line, source=source, version=version, plan_sha256=plan_hash,
        packages={'selected': records, 'private_recipe_only_outputs': []}, preflight={'sdk': metadata.SDK, 'product_work_executed': False})
    producer.pop('npm')
    if product == 'core':
        producer['package_verification'] = native
    else:
        producer['manifest_verification'] = []
        producer['product_tests'] = {'executions': [], 'inherited_skipped_placeholders': []}
        for path, targets in tests.items():
            for target in targets:
                policy = next((r for r in contracts['register']['inherited_skipped_placeholders'] if r['project'] == path), None)
                row = {'project': path, 'framework': target, 'sha256': '3'*64,
                    'counters': dict.fromkeys(payload.producer.maintenance.TRX_COUNTERS, 0) | {'total': 1}}
                if policy:
                    row.update({k:v for k,v in policy.items() if k not in ('product','commits')}); row.update(source_commit=source['commit'], not_executed_result_count=1)
                    producer['product_tests']['inherited_skipped_placeholders'].append(row)
                else:
                    row['counters'].update(executed=1, passed=1); producer['product_tests']['executions'].append(row)
        for item in selected.values():
            members, policy = item['members'], item['policy']; manifest = None
            if 'elsa-package.json' in members:
                manifest = {'path': 'elsa-package.json', 'sha256': metadata.sha256(members['elsa-package.json']), 'id': policy['id'],
                    'version': version, 'frameworks': policy['frameworks'], 'selectable_features': [payload.extensions.MANIFEST_FEATURE], 'runtime_kinds': ['elsa.server']}
            producer['manifest_verification'].append({'id': policy['id'], 'package_manifest': manifest,
                'sdk_assets': ([{'path': 'elsa-package.json', 'sha256': metadata.sha256(members['elsa-package.json'])}] if manifest else [])})
    for execution in (producer['execution'], consumer['execution']):
        execution.update(product=product, line=line, source=source, plan_sha256=plan_hash)
    files['producer/receipt.json'] = encoded(producer); producer_hash = metadata.sha256(files['producer/receipt.json'])
    consumer.update(plan_sha256=plan_hash, artifact_receipt_sha256=producer_hash, source=source, producer_execution=producer['execution'],
        runtime_contract_source=payload.runtime_source(plan, contracts))
    consumer['producer_plan_admission'].update(artifact_receipt_sha256=producer_hash)
    consumer['current_consumer_admission']['histories'] = len(plan['histories'])
    description = payload.runtime_policy(plan, contracts)
    consumer['limitations'][1] = description['limitation']
    template_cell = deepcopy(consumer['coverage'][0])
    def cell(policy, target, runtime=False):
        row = deepcopy(template_cell); item = selected[policy['id'].casefold()]
        row.update(id=policy['id'], framework=target, archive_sha256=item['record']['sha256'], original_output_policy=policy['metadata']['original_output_policy'][target])
        row['sdk_restore']['original_assets_sha256'] = policy['metadata']['restore_assets_sha256']
        row['input_sha256']['Program.cs'] = metadata.sha256(contracts['fixtures'][product] if runtime else b'extern alias selected;\npublic class CompileContract {}\n')
        admitted = list(selected.values()) if runtime else [item]
        row['restored'] = [{'id': x['policy']['id'], 'version': version, 'assets_type': 'package', 'source': 'selected-local-archives',
            'sha256': x['record']['sha256'], 'sha512': hashlib.sha512(x['data']).hexdigest()} for x in admitted]
        row['restored_payloads'] = [{'id': x['policy']['id'], 'version': version, 'payloads': [
            {'path': path, 'kind': 'runtime', 'sha256': metadata.sha256(data), 'size': len(data)} for path, data in x['members'].items()
            if path == f'lib/{target}/{x["policy"]["id"]}.dll']} for x in admitted]
        if runtime:
            row['input_sha256']['AssemblyProof.cs'] = metadata.sha256(contracts['assembly_proof'])
            if product == 'core':
                row['runtime'] = next(x['runtime'] for x in core_consumer['runtime'] if x['framework'] == target)
            else:
                row['runtime'] = {'contract': description['description'], 'loaded_assemblies': [
                    {'name': x['policy']['id'], 'version': '1.0.0.0', 'informationalVersion': '1.0.0+'+source['commit'],
                    'sha256': metadata.sha256(x['members'][f'lib/{target}/{x["policy"]["id"]}.dll']), 'package_id': x['policy']['id'],
                    'package_version': version, 'package_asset': f'lib/{target}/{x["policy"]["id"]}.dll'} for x in admitted]}
        return row
    consumer['coverage'] = [cell(policy, target) for policy in policies for target in policy['frameworks']]
    root = selected[description['package']]['policy']
    consumer['runtime'] = [cell(root, target, True) for target in root['frameworks']]
    files['consumer/receipt.json'] = encoded(consumer)
    expected.update(product=product, line=line)
    return files, expected, now


def excluded_build_fixture(line='3.8', framework='net8.0', *, exclude='Build,Analyzers', include=''):
    """Rehash all original archive/plan/execution joins before testing the marker guard."""
    files, expected, now = studio_fixture(line)
    plan, producer, consumer = (json.loads(files[name]) for name in ('plan.json', 'producer/receipt.json', 'consumer/receipt.json'))
    identifier = 'Microsoft.AspNetCore.Components.WebAssembly'
    version = {'net8.0': '8.0.24', 'net9.0': '9.0.13', 'net10.0': '10.0.3'}[framework]
    policy = plan['inventory']['selected'][0]
    group = next(row for row in policy['metadata']['dependency_groups'] if row['framework'] == framework)
    group['dependencies'] = [{'id': identifier, 'version': version, 'include': include, 'exclude': exclude}]
    plan['prerequisites'] = [{'consumer': policy['id'], 'project': policy['project'], 'framework': framework,
        'id': identifier, 'range': version, 'version': version, 'feeds': [], 'eligible': True,
        'reason': None, 'compatibility_scope': 'dependency-metadata-only'}]
    observation = deepcopy(plan['histories'][0]['feeds'][0]['observation'])
    observation['url'] = 'https://api.nuget.org/v3-flatcontainer/' + identifier.lower() + '/' + version + '/' + identifier.lower() + '.nuspec'
    plan['prerequisites'][0]['feeds'] = [{key: value for key, value in plan['prerequisites'][0].items()
        if key not in ('feeds', 'compatibility_scope')} | {'feed': 'https://api.nuget.org/v3/index.json', 'observation': observation}]
    plan['prerequisites_excluded_from_publication'] = [identifier]
    plan['inventory']['sha256'] = metadata.canonical_hash(metadata.public_inventory(plan['inventory']))
    files['plan.json'] = encoded(plan); plan_hash = metadata.sha256(files['plan.json'])
    for record in producer['packages']['selected']:
        name = 'producer/nuget/' + record['file']; members = transport.zip_members(files[name], leaf=True)
        spec = ET.fromstring(members['a.nuspec'])
        node = next(node for node in spec.findall('metadata/dependencies/group') if node.get('targetFramework') == framework)
        ET.SubElement(node, 'dependency', id=identifier, version=version, include=include, exclude=exclude)
        members['a.nuspec'] = ET.tostring(spec)
        data = zipped(list(members.items())); files[name] = data
        record.update(sha256=metadata.sha256(data), size=len(data), inventory=transport.inventory(members))
        if record['file'].endswith('.nupkg'): main_data = data
    for execution in (producer['execution'], producer['npm']['execution'], consumer['execution'], consumer['producer_execution']):
        execution['plan_sha256'] = plan_hash
    producer['plan_sha256'] = plan_hash
    files['producer/npm/receipt.json'] = encoded(producer['npm'])
    files['producer/receipt.json'] = encoded(producer); producer_hash = metadata.sha256(files['producer/receipt.json'])
    consumer.update(plan_sha256=plan_hash, artifact_receipt_sha256=producer_hash)
    consumer['current_consumer_admission']['prerequisites'] = 1
    consumer['producer_plan_admission']['artifact_receipt_sha256'] = producer_hash
    for cell in consumer['coverage'] + consumer['runtime']:
        cell['archive_sha256'] = metadata.sha256(main_data)
        for row in cell['restored']: row.update(sha256=metadata.sha256(main_data), sha512=hashlib.sha512(main_data).hexdigest())
    cell = next(row for row in consumer['coverage'] if row['framework'] == framework)
    external = {'id': identifier, 'version': version, 'source': 'https://api.nuget.org/v3/index.json',
        'sha256': 'a' * 64, 'archive_sha512': 'b' * 128, 'nuget_content_hash': 'A' * 86 + '==', 'signed': True}
    marker = {'path': f'build/{framework}/_._', 'kind': 'build', 'accounting': payload.consumer.EMPTY_BUILD_ACCOUNTING,
        'metadata': {}, 'archive_sha256': external['sha256'], 'origin': payload.consumer.EMPTY_BUILD_ORIGIN}
    cell['restored'].append(external)
    cell['restored_payloads'].append({'id': identifier, 'version': version, 'payloads': [marker,
        {'path': f'lib/{framework}/{identifier}.dll', 'kind': 'runtime', 'sha256': 'c' * 64, 'size': 10}]})
    files['consumer/receipt.json'] = encoded(consumer)
    return files, expected, now


class SelectedProductPayloadTests(unittest.TestCase):
    def validate(self, values):
        files, expected, now = values
        return payload.validate(files, expected, *(metadata.sha256(files[name]) for name in
            ('plan.json', 'producer/receipt.json', 'consumer/receipt.json')), contracts=payload.load_contracts(), now=now)

    def test_full_six_cell_byte_dispatch(self):
        for product in ('core', 'studio', 'extensions'):
            for line in ('3.8', '3.9'):
                with self.subTest(product=product, line=line):
                    values = fixture(product, line)
                    result = self.validate(values)
                    self.assertEqual(result['plan']['product'], product)
                    self.assertEqual(len(result['consumer']['runtime']), 3)
                    sealed = seal.stage(values[0], values[1], tuple(metadata.sha256(values[0][name]) for name in
                        ('plan.json', 'producer/receipt.json', 'consumer/receipt.json')), contracts=payload.load_contracts(), now=values[2])
                    self.assertEqual('original npm lifecycle/import/Vite execution' in json.loads(sealed[transport.MANIFEST])['inherited_judgments'], product == 'studio')
                    provider_fixture = ProviderFixture(); provider_fixture.setUp()
                    provider = provider_fixture.provider
                    manifest_hash = metadata.sha256(sealed[transport.MANIFEST])
                    original_zip = zipped(list(sealed.items()))
                    context = values[1]['context']
                    provider.update(artifact_name=f"selected-control-{product}-{line}-{context['head_sha']}-{context['run_id']}-{context['run_attempt']}",
                        manifest_sha256=manifest_hash, archive_sha256=metadata.sha256(original_zip), archive_size=len(original_zip),
                        created_at=(values[2]-timedelta(minutes=2)).isoformat(), retrieved_at=(values[2]-timedelta(minutes=1)).isoformat(),
                        expires_at=(values[2]+timedelta(days=1)).isoformat())
                    result = seal.readback(original_zip, provider, values[1] | {'manifest_sha256': manifest_hash},
                        contracts=payload.load_contracts(), now=values[2])
                    self.assertTrue(result['success'])
                    self.assertFalse(result['product_code_executed_during_readback'])

    def test_core_missing_branch_or_tag_observation_rehashed_payload_is_rejected(self):
        for line in ('3.8', '3.9'):
            for key in ('branch_observation', 'tag_observation'):
                def missing(source):
                    row = source['observation'][key]
                    row['status'] = 'missing'; row.pop('bytes'); row.pop('sha256')
                values = fixture('core', line, source_mutation=missing)
                with self.subTest(line=line, key=key), self.assertRaisesRegex(ValueError, 'selected_plan_core_observation'):
                    self.validate(values)
                self.files, self.context = values[:2]
                seal_tests.SealContracts.assert_cli_rejected(self)

    def test_separate_sdk_download_ledger_is_typed_and_never_product_closure(self):
        def download():
            return {'id': 'Microsoft.NETCore.App.Ref', 'version': '8.0.27',
                'source': 'https://api.nuget.org/v3/index.json', 'sha256': 'a'*64, 'archive_sha512': 'b'*128,
                'nuget_content_hash': 'A'*86+'==', 'signed': True}
        values = fixture('studio', '3.8'); files = values[0]
        receipt = json.loads(files['consumer/receipt.json'])
        cell = receipt['coverage'][0]
        cell['sdk_restore']['downloads'] = [download()]
        files['consumer/receipt.json'] = encoded(receipt)
        self.validate(values)
        baseline = deepcopy(receipt)
        for change in ('version', 'source', 'duplicate', 'hash', 'extra', 'pruned_selected', 'pruned_parent'):
            receipt = deepcopy(baseline); sdk = receipt['coverage'][0]['sdk_restore']
            if change == 'version': sdk['downloads'][0]['version'] = '8.0.0'
            elif change == 'source': sdk['downloads'][0]['source'] = '/private/cache'
            elif change == 'duplicate': sdk['downloads'].append(download())
            elif change == 'hash': sdk['downloads'][0]['nuget_content_hash'] = 'invalid'
            elif change == 'extra': sdk['downloads'][0]['path'] = '/private/cache'
            else:
                sdk['pruning_enabled'] = True
                sdk['pruned_edges'] = [{'from_id': cell['id'] if change == 'pruned_selected' else 'Unknown',
                    'from_version': '3.8.999', 'id': cell['id'] if change == 'pruned_selected' else 'System.Threading.Channels',
                    'range': '[8.0.0, )', 'prune_range': '(,8.0.32767]'}]
            files['consumer/receipt.json'] = encoded(receipt)
            with self.subTest(change=change), self.assertRaises(ValueError): self.validate(values)
            self.files, self.context = values[:2]
            seal_tests.SealContracts.assert_cli_rejected(self)

    def test_sdk_ledger_binds_original_assets_for_every_selected_cell(self):
        for product in ('core', 'studio', 'extensions'):
            for line in ('3.8', '3.9'):
                values = fixture(product, line)
                self.validate(values)
                receipt = json.loads(values[0]['consumer/receipt.json'])
                receipt['coverage'][0]['sdk_restore']['original_assets_sha256'] = 'f' * 64
                values[0]['consumer/receipt.json'] = encoded(receipt)
                with self.subTest(product=product, line=line), self.assertRaisesRegex(
                        ValueError, 'selected_payload_sdk_original_assets'):
                    self.validate(values)
                self.files, self.context = values[:2]
                seal_tests.SealContracts.assert_cli_rejected(self)

    def test_synthetic_content_accounting_is_closed_external_only_and_not_payload_bytes(self):
        values = fixture('studio', '3.8'); files = values[0]
        receipt = json.loads(files['consumer/receipt.json']); cell = receipt['coverage'][0]
        identifier = 'Microsoft.AspNetCore.Components.CustomElements'
        external = {'id': identifier, 'version': '9.0.13', 'source': 'https://api.nuget.org/v3/index.json',
            'sha256': 'a' * 64, 'archive_sha512': 'b' * 128, 'nuget_content_hash': 'A' * 86 + '==', 'signed': True}
        marker = {'path': payload.consumer.EMPTY_CONTENT, 'kind': 'contentFiles',
            'accounting': payload.consumer.EMPTY_CONTENT_ACCOUNTING,
            'metadata': dict(payload.consumer.EMPTY_CONTENT_METADATA), 'archive_sha256': external['sha256'],
            'origin': 'original-excluded-content'}
        cell['restored'].append(external)
        cell['restored_payloads'].append({'id': identifier, 'version': external['version'], 'payloads': [marker]})
        files['consumer/receipt.json'] = encoded(receipt); self.validate(values)
        baseline = deepcopy(receipt)
        for origin in ('original-excluded-content', 'package-reference-transitive-content-exclusion'):
            receipt = deepcopy(baseline); row = receipt['coverage'][0]['restored_payloads'][-1]
            row['payloads'][0]['origin'] = origin
            row['payloads'].append({'path': 'lib/net9.0/external.dll', 'kind': 'runtime',
                'sha256': 'c' * 64, 'size': 10})
            files['consumer/receipt.json'] = encoded(receipt)
            with self.subTest(origin=origin): self.validate(values)
        for change in ('path', 'kind', 'metadata', 'copy_integer', 'fake_bytes', 'hash', 'duplicate', 'selected', 'origin', 'mixed', 'mixed-reversed'):
            receipt = deepcopy(baseline); cell = receipt['coverage'][0]
            row = cell['restored_payloads'][-1]; marker = row['payloads'][0]
            if change == 'path': marker['path'] = 'contentFiles/any/any/arbitrary_._'
            elif change == 'kind': marker['kind'] = 'runtime'
            elif change == 'metadata': marker['metadata']['copyToOutput'] = True
            elif change == 'copy_integer': marker['metadata']['copyToOutput'] = 0
            elif change == 'fake_bytes': marker.update(size=0, sha256=metadata.sha256(b''))
            elif change == 'hash': marker['archive_sha256'] = 'f' * 64
            elif change == 'duplicate': row['payloads'].append(deepcopy(marker))
            elif change == 'origin': marker['origin'] = 'unbound'
            elif change in ('mixed', 'mixed-reversed'):
                row['payloads'].append({'path': 'contentFiles/any/net9.0/js/package.json',
                    'kind': 'contentFiles', 'sha256': 'c' * 64, 'size': 10})
                if change == 'mixed-reversed': row['payloads'].reverse()
            else:
                cell['restored_payloads'][0]['payloads'].append(marker)
                cell['restored_payloads'].pop(); cell['restored'].pop()
            files['consumer/receipt.json'] = encoded(receipt)
            error = 'selected_payload_synthetic_content_group' if change.startswith('mixed') else '.'
            with self.subTest(change=change), self.assertRaisesRegex(ValueError, error): self.validate(values)
            self.files, self.context = values[:2]
            seal_tests.SealContracts.assert_cli_rejected(self)

    def test_source_bound_excluded_build_marker_seals_both_lines_and_each_framework(self):
        for line in ('3.8', '3.9'):
            for framework in ('net8.0', 'net9.0', 'net10.0'):
                values = excluded_build_fixture(line, framework)
                with self.subTest(line=line, framework=framework):
                    self.validate(values)
                    seal.stage(values[0], values[1], tuple(metadata.sha256(values[0][name]) for name in
                        ('plan.json', 'producer/receipt.json', 'consumer/receipt.json')),
                        contracts=payload.load_contracts(), now=values[2])

    def test_rehashed_build_marker_requires_actual_plan_archive_exclusion(self):
        for include, exclude in (('All', 'Analyzers'), ('Unknown', 'Build'), ('', 'Unknown')):
            values = excluded_build_fixture(include=include, exclude=exclude)
            with self.subTest(include=include, exclude=exclude), self.assertRaisesRegex(ValueError, 'consumer_synthetic_build_flags'):
                self.validate(values)
            self.files, self.context = values[:2]
            seal_tests.SealContracts.assert_cli_rejected(self)

    def test_rehashed_build_marker_is_closed_and_build_group_is_singleton_in_both_orders(self):
        values = excluded_build_fixture(); baseline = json.loads(values[0]['consumer/receipt.json'])
        for change in ('path', 'metadata', 'kind', 'origin', 'hash', 'fake_bytes', 'selected', 'version', 'mixed', 'mixed-reversed'):
            receipt = deepcopy(baseline); cell = receipt['coverage'][0]
            row = cell['restored_payloads'][-1]; marker = row['payloads'][0]
            if change == 'path': marker['path'] = 'build/net8.0/arbitrary_._'
            elif change == 'metadata': marker['metadata']['unexpected'] = False
            elif change == 'kind': marker['kind'] = 'runtime'
            elif change == 'origin': marker['origin'] = 'original-excluded-content'
            elif change == 'hash': marker['archive_sha256'] = 'f' * 64
            elif change == 'fake_bytes': marker.update(size=0, sha256=metadata.sha256(b''))
            elif change == 'version':
                row['version'] = cell['restored'][-1]['version'] = '8.0.1'
            elif change == 'selected':
                cell['restored_payloads'][0]['payloads'].append(marker)
                cell['restored_payloads'].pop(); cell['restored'].pop()
            else:
                row['payloads'].append({'path': 'build/net8.0/Microsoft.AspNetCore.Components.WebAssembly.props',
                    'kind': 'build', 'sha256': 'd' * 64, 'size': 10})
                if change == 'mixed-reversed': row['payloads'].reverse()
            values[0]['consumer/receipt.json'] = encoded(receipt)
            error = ('selected_payload_synthetic_build_group' if change.startswith('mixed') else
                'selected_payload_synthetic_build_prerequisite' if change == 'version' else '.')
            with self.subTest(change=change), self.assertRaisesRegex(ValueError, error): self.validate(values)
            self.files, self.context = values[:2]
            seal_tests.SealContracts.assert_cli_rejected(self)

    def test_frozen_retained_file_closure_is_conditional_on_product(self):
        for product in ('core', 'studio', 'extensions'):
            files, _, _ = fixture(product, '3.9')
            with self.subTest(product=product), tempfile.TemporaryDirectory() as folder:
                root = Path(folder).resolve()
                for name, data in files.items():
                    path = root / name
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_bytes(data)
                hashes = [metadata.sha256(files[n]) for n in ('plan.json', 'producer/receipt.json', 'consumer/receipt.json')]
                frozen, _ = seal.freeze_inputs(root/'plan.json', hashes[0], root/'producer', hashes[1], root/'consumer/receipt.json', hashes[2])
                self.assertEqual(frozen, files)
                (root/'producer'/'excluded.3.9.999.nupkg').write_bytes(b'private recipe output')
                with self.assertRaisesRegex(ValueError, 'retained_closure'):
                    seal.freeze_inputs(root/'plan.json', hashes[0], root/'producer', hashes[1], root/'consumer/receipt.json', hashes[2])

    def test_excluded_metadata_cannot_alias_selected_archive(self):
        values = fixture('extensions', '3.9')
        files = values[0]
        receipt = json.loads(files['producer/receipt.json'])
        row = deepcopy(receipt['packages']['selected'][0])
        row['id'] = 'Elsa.Secrets.Persistence.EFCore'
        receipt['packages']['private_recipe_only_outputs'].append(row)
        files['producer/receipt.json'] = encoded(receipt)
        with self.assertRaisesRegex(ValueError, 'private_output_scope'):
            self.validate(values)

    def test_cross_product_native_schema_cannot_be_relabelled(self):
        for product in ('core', 'extensions'):
            values = fixture(product, '3.8'); files = values[0]
            receipt = json.loads(files['producer/receipt.json'])
            wrong = 'manifest_verification' if product == 'core' else 'package_verification'
            receipt[wrong] = []; files['producer/receipt.json'] = encoded(receipt)
            with self.subTest(product=product), self.assertRaisesRegex(ValueError, 'producer'): self.validate(values)

    def test_extensions_manifest_and_assembly_policy_stay_strict(self):
        for mutation in ('manifest', 'native', 'assembly', 'placeholder'):
            values = fixture('extensions','3.9'); files = values[0]
            if mutation == 'assembly':
                r=json.loads(files['consumer/receipt.json']);r['runtime'][0]['runtime']['loaded_assemblies'][0]['version']='3.9.999.0';files['consumer/receipt.json']=encoded(r)
            else:
                r=json.loads(files['producer/receipt.json'])
                if mutation=='manifest':r['manifest_verification'][0]['package_manifest']['version']='1.0.0'
                elif mutation=='native':r['manifest_verification'][0]['compiler_evidence']={}
                else:r['product_tests']['inherited_skipped_placeholders'][0]['skip_reason']='invented'
                files['producer/receipt.json']=encoded(r)
            with self.subTest(mutation=mutation), self.assertRaises(ValueError): self.validate(values)

    def test_malformed_extensions_schemas_reach_generic_cli_rejection(self):
        for mutation in ('package_id', 'package_object', 'extensions_object', 'runtime_object', 'feature_runtime_object'):
            values = fixture('extensions', '3.8')
            files, context, _ = values
            receipt = json.loads(files['producer/receipt.json'])
            manifest_row = next(row for row in receipt['manifest_verification'] if row['id'] == 'Elsa.IO.Http')
            if mutation == 'package_id':
                manifest_row['id'] = 123
                expected_error = 'extensions_payload_package_id'
            else:
                record = next(row for row in receipt['packages']['selected'] if row['id'] == 'Elsa.IO.Http' and row['file'].endswith('.nupkg'))
                name = 'producer/nuget/' + record['file']
                members = transport.zip_members(files[name], leaf=True)
                manifest = json.loads(members['elsa-package.json'])
                if mutation == 'package_object':
                    manifest['package'] = []
                elif mutation == 'extensions_object':
                    manifest['extensions'] = []
                elif mutation == 'runtime_object':
                    manifest['compatibility'] = []
                else:
                    manifest['features'][0]['compatibility'] = []
                members['elsa-package.json'] = encoded(manifest)
                files[name] = zipped(list(members.items()))
                record.update(sha256=metadata.sha256(files[name]), size=len(files[name]), inventory=transport.inventory(members))
                manifest_hash = metadata.sha256(members['elsa-package.json'])
                manifest_row['package_manifest']['sha256'] = manifest_hash
                manifest_row['sdk_assets'][0]['sha256'] = manifest_hash
                expected_error = 'extensions_payload_manifest_schema'
            files['producer/receipt.json'] = encoded(receipt)
            # Rehashed archive/manifest/receipt projections reach the intended
            # schema boundary before downstream consumer receipt admission.
            with self.subTest(mutation=mutation), self.assertRaisesRegex(ValueError, expected_error):
                self.validate(values)
            self.files, self.context = files, context
            seal_tests.SealContracts.assert_cli_rejected(self)

    def test_core_per_asset_native_policy_has_no_studio_version_fallback(self):
        values=fixture('core','3.9');files=values[0];r=json.loads(files['consumer/receipt.json'])
        r['runtime'][0]['runtime']['loaded_assemblies'][0]['version']='3.9.999.0';files['consumer/receipt.json']=encoded(r)
        with self.assertRaisesRegex(ValueError,'native_identity'):self.validate(values)

    def test_pure_common_and_specializations_do_not_read_paths(self):
        values=fixture('extensions','3.8');files,expected,now=values;contracts=payload.load_contracts()
        with patch('pathlib.Path.read_bytes', side_effect=AssertionError('pure validator file IO')):
            payload.validate(files,expected,*(metadata.sha256(files[n]) for n in ('plan.json','producer/receipt.json','consumer/receipt.json')),contracts=contracts,now=now)

    def test_core_shape_exceptions_do_not_widen_other_products(self):
        files, _, now = studio_fixture()
        contracts = payload.load_contracts()
        for mutation in ('empty_url', 'net7'):
            plan = json.loads(files['plan.json'])
            if mutation == 'empty_url':
                plan['inventory']['selected'][0]['repository_url'] = ''
            else:
                plan['inventory']['projects'][0]['target_frameworks'].append('net7.0')
            with self.subTest(mutation=mutation), self.assertRaisesRegex(ValueError, 'scope'):
                schema.validate(plan, contracts, now=now)

    def test_actual_pinned_public_plan_object_shapes_are_closed(self):
        snapshot = json.loads(SHAPES.read_bytes())
        fixtures = snapshot['cells']
        self.assertEqual(set(fixtures), {p + '-' + l for p in ('core', 'studio', 'extensions') for l in ('3.8', '3.9')})
        for cell, value in fixtures.items():
            transport.digest(value['plan_sha256'])
            for path, shapes in (snapshot['common_object_shapes'] | value['object_shapes']).items():
                if path in schema.SHAPES:
                    for shape in shapes:
                        with self.subTest(cell=cell, path=path):
                            self.assertIn(set(shape.split()), [set(s.split()) for s in schema.SHAPES[path]])
                else:
                    self.assertTrue(path.endswith(('.observation', '.branch_observation', '.tag_observation')), path)
            schema._walk(value['source'], 'source', now=datetime.now(timezone.utc))


if __name__ == '__main__':
    unittest.main()
