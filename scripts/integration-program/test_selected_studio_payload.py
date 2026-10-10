"""Synthetic byte-level Studio control receipts; no native proof is simulated as actual evidence."""
from copy import deepcopy
from datetime import datetime, timedelta, timezone
import hashlib
import json
from pathlib import Path
import unittest
from uuid import uuid4

import historical_studio_npm_continuation as lifecycle
import product_release_metadata as metadata
import prove_product_release_artifacts as producer
import prove_product_release_consumers as consumer
import prove_studio_npm_pair as npm
import selected_control_transport as transport
import selected_studio_payload as payload
from test_selected_control_transport import zipped, tarred
import tarfile


def encoded(value):
    return (json.dumps(value, indent=2, sort_keys=True) + '\n').encode()


def fixture():
    now = datetime.now(timezone.utc)
    stamp = lambda minutes: (now - timedelta(minutes=minutes)).isoformat()
    contract = json.loads(payload.CONTRACT.read_bytes())
    candidate = next(row for row in producer.maintenance.load_candidates()['candidates'] if row['commit'] == contract['source_commit'])
    source = {'product': 'studio', 'line': '3.8', 'kind': 'admitted-maintenance-descendant',
        **{key: candidate[key] for key in ('commit', 'tree', 'parents', 'original_commit', 'original_parents', 'contained_commit', 'contained_tree')},
        'candidate_register_sha256': metadata.sha256(producer.maintenance.CANDIDATES.read_bytes()),
        'original_register_sha256': metadata.sha256(producer.maintenance.REGISTER.read_bytes())}
    controller = {'commit': 'a' * 40, 'tree': 'b' * 40}
    context = {'repository': transport.REPOSITORY, 'repository_id': transport.REPOSITORY_ID,
        'event': 'push', 'ref': transport.HOSTED_REF, 'head_sha': 'a' * 40, 'workflow_sha': 'a' * 40,
        'workflow_ref': f'{transport.REPOSITORY}/{transport.WORKFLOW}@{transport.HOSTED_REF}',
        'workflow_path': transport.WORKFLOW, 'run_id': '123', 'run_attempt': '2', 'job': 'control'}
    version, identifier = '3.8.999', 'Elsa.Studio.Core'
    frameworks = ['net8.0', 'net9.0', 'net10.0']
    output = {'IncludeBuildOutput': 'true', 'IncludeContentInPack': 'true', 'IncludeSymbols': 'true',
        'SymbolPackageFormat': 'snupkg', 'GenerateElsaPackageManifest': '',
        'ElsaPackageManifestIncludeInPackage': '', 'ElsaPackageManifestPackagePath': ''}
    path, test = 'src/framework/Elsa.Studio.Core/Elsa.Studio.Core.csproj', 'src/framework/Elsa.Studio.Core.Tests/Elsa.Studio.Core.Tests.csproj'
    policy = {'id': identifier, 'project': path, 'frameworks': frameworks, 'include_build_output': True,
        'project_url': 'https://github.com/elsa-workflows/elsa-studio', 'repository_url': 'https://github.com/elsa-workflows/elsa-core',
        'symbol_format': 'snupkg', 'symbols': True, 'metadata': {'status': 'observed', 'nuspec_sha256': '1' * 64,
        'dependency_groups': [{'framework': item, 'dependencies': []} for item in sorted(frameworks)],
        'framework_reference_groups': [], 'restore_assets_sha256': '2' * 64,
        'restore_scope': {'config_files': [{'path': 'NuGet.Config', 'sha256': '3' * 64}],
            'public_sources': ['https://api.nuget.org/v3/index.json'], 'local_sources': []},
        'original_output_policy': {item: deepcopy(output) for item in frameworks},
        'original_content_project_sha256': '4' * 64, 'manifest_content_targets': [], 'sdk_pack_targets_sha256': '5' * 64,
        'projection': {'NoBuild': True, 'ContinuePackingAfterGeneratingNuspec': False, 'IncludeBuildOutput': False,
            'ElsaPackageManifestIncludeInPackage': False, 'IncludeContentInPack': False}}}
    projects = [{'path': item, 'package_id': name, 'is_packable': pack, 'is_test_project': item == test,
        'target_frameworks': targets, 'references_by_framework': {target: {'project_references': [], 'package_references': []} for target in targets},
        'project_references': [], 'package_references': []} for item, name, pack, targets in
        ((path, identifier, True, frameworks), (test, 'Elsa.Studio.Core.Tests', False, ['net10.0']),
         (contract['files'][2]['path'], 'Elsa.Studio.Host.CustomElements', False, frameworks))]
    projects[1]['project_references'] = [{'target_project': path}]
    projects[1]['references_by_framework']['net10.0']['project_references'] = [{'target_project': path}]
    inventory = {'source_commit': source['commit'], 'source_tree': source['tree'], 'requested_version': version,
        'projects': projects, 'selected': [policy], 'excluded': [{'id': 'Elsa.Studio.Core.Tests', 'project': test, 'reason': 'source_nonpackable'},
            {'id': 'Elsa.Studio.Host.CustomElements', 'project': contract['files'][2]['path'], 'reason': 'source_nonpackable'}],
        'release_recipe': {'solution': 'Elsa.Studio.sln', 'sha256': '6' * 64, 'projects': [path, test, contract['files'][2]['path']],
            'workflow': contract['files'][3]['path'], 'workflow_sha256': contract['files'][3]['sha256']},
        'applicable_tests': [test], 'ownership_policy': metadata.ownership_policy()}
    inventory['sha256'] = metadata.canonical_hash(metadata.public_inventory(inventory))
    observation = lambda url: {'url': url, 'observed_at': stamp(20), 'status': 'observed', 'sha256': '7' * 64, 'bytes': 1}
    decision = {'duplicate': False, 'latest_in_line': None, 'monotonic': True, 'normalized': version, 'reused': False}
    history = lambda name: {'id': name, 'eligible': True, 'reason': None, 'versions': [], 'decision': deepcopy(decision),
        'observation': observation('https://registry.npmjs.org/' + name)}
    nuget_history = history(identifier)
    nuget_history['feed'] = 'https://api.nuget.org/v3/index.json'
    nuget_history['observation']['url'] = 'https://api.nuget.org/v3-flatcontainer/elsa.studio.core/index.json'
    npm_intent = {'atomic': True, 'line': '3.8', 'source_commit': source['commit'], 'source_tree': source['tree'],
        'workflow': {'path': contract['files'][3]['path'], 'sha256': contract['files'][3]['sha256']},
        'manifests': [{'path': row['path'], 'sha256': row['sha256'], 'checked_in_version': row['package']['version'],
            'checked_in_dependencies': row['package'].get('dependencies', {}), 'peer_dependencies': row['package'].get('peerDependencies', {})}
            for row in contract['files'][:2]],
        'packages': [{'id': name, 'version': version, 'expected_tarball': name.removeprefix('@').replace('/', '-') + '-' + version + '.tgz'}
            for name in (npm.WASM, npm.REACT)], 'wrapper_dependency_intent': {npm.WASM: version},
        'artifact_proof': False, 'historical_workflow_executed': False}
    plan = {'schema': 1, 'mode': 'read-only-product-release-plan', 'controller': controller | {
        'execution': {'repository': transport.REPOSITORY, 'event_name': 'push', 'ref': transport.HOSTED_REF, 'run_id': '123', 'run_attempt': '2'},
        'input_sha256': {name: metadata.sha256((consumer.ROOT / name).read_bytes()) for name in producer.PLANNER_INPUTS}},
        'source': source, 'product': 'studio', 'line': '3.8', 'requested_version': version, 'inventory': inventory,
        'expected_artifacts': [identifier + '.' + version + suffix for suffix in ('.nupkg', '.snupkg')] +
            [row['expected_tarball'] for row in npm_intent['packages']], 'excluded_artifacts': [],
        'prerequisites_excluded_from_publication': [], 'prerequisites': [], 'selected_dependency_intent': [],
        'histories': [{'id': identifier, 'eligible': True, 'reason': None, 'feeds': [nuget_history]}] + [history(name) for name in (npm.WASM, npm.REACT)],
        'npm': npm_intent, 'eligible': True, 'reasons': [], 'published': False, 'version_allocated': False, 'tag_created': False,
        'semantics': {'sdk_version': metadata.SDK, 'assemblies': [{'name': name, 'version': '7.0.0.0', 'file_version': '7.0.0.0', 'sha256': '8' * 64} for name in
            ('NuGet.Versioning', 'NuGet.Frameworks', 'NuGet.Packaging', 'NuGet.Configuration', 'NuGet.Common')]},
        'limits': [], 'requested_version_input': version, 'observed_at': stamp(20), 'consumer_feed_policy': {
            'config_path': 'NuGet.Config', 'config_sha256': '3' * 64, 'history_authority': 'all-original-mapped-feeds', 'mapping_enabled': True,
            'packages': [{'id': identifier, 'sources': ['nuget']}], 'sources': [{'name': 'nuget', 'url': 'https://api.nuget.org/v3/index.json'}]},
        'feed_service_observations': {'https://api.nuget.org/v3/index.json': {'base': 'https://api.nuget.org/v3-flatcontainer/', 'eligible': True,
            'observation': observation('https://api.nuget.org/v3/index.json')}}}
    files = {'plan.json': encoded(plan)}
    plan_hash = metadata.sha256(files['plan.json'])
    def execution(role, minutes):
        return {'kind': 'github-actions-selected-control', 'id': str(uuid4()), 'started_at': stamp(minutes), 'role': role,
            'controller': controller, 'plan_sha256': plan_hash, 'product': 'studio', 'line': '3.8', 'source': source,
            'context': context, 'authority': 'runner-environment-only-provider-unverified'}
    artifact_execution, consumer_execution = execution('artifact', 19), execution('consumer', 10)
    groups = ''.join('<group targetFramework="' + item + '" />' for item in sorted(frameworks))
    spec = ('<package><metadata><id>' + identifier + '</id><version>' + version + '</version><repository type="git" url="https://github.com/elsa-workflows/elsa-core" commit="' + source['commit'] + '"/><dependencies>' + groups + '</dependencies></metadata></package>').encode()
    records = []
    dlls = {f'lib/{item}/{identifier}.dll': ('synthetic-assembly-' + item).encode() for item in frameworks}
    for suffix, entries in (('.nupkg', {'a.nuspec': spec} | dlls), ('.snupkg', {'a.nuspec': spec, 'lib/net10.0/Elsa.Studio.Core.pdb': b'synthetic-pdb'})):
        data = zipped(list(entries.items()))
        name = identifier + '.' + version + suffix
        files['producer/nuget/' + name] = data
        records.append({'file': name, 'id': identifier, 'version': version, 'sha256': metadata.sha256(data),
            'size': len(data), 'inventory': transport.inventory(entries)})
    wasm = {'_framework/blazor.webassembly.js': b'js', '_framework/dotnet.js': b'js', 'index.html': b'html',
        'appsettings.json': b'{}', '_content/component.js': b'js', '_framework/dotnet.native.fixture.wasm': b'\x00asmfixture',
        '_framework/Elsa.Studio.Host.CustomElements.fixture.wasm': b'\x00asmfixture'}
    react = {'dist/' + name: data for name, data in wasm.items() if name != 'index.html'}
    react.update({'dist/elsa-studio-wasm-react.js': b'export {}', 'dist/elsa-studio-wasm-react.umd.cjs': b'module.exports={}'})
    reports = {}
    for key, entries, original, intent in zip(('wasm', 'react'), (wasm, react), contract['files'][:2], npm_intent['packages']):
        package = deepcopy(original['package'])
        package['version'] = version
        if key == 'react': package['dependencies'][npm.WASM] = version
        entries['package.json'] = encoded(package)
        data = tarred([('package/' + name, value, tarfile.REGTYPE) for name, value in entries.items()])
        files['producer/npm/' + intent['expected_tarball']] = data
        reports[key] = {'file': intent['expected_tarball'], 'name': intent['id'], 'version': version,
            'sha512_integrity': transport.sha512_integrity(data), 'package_metadata_sha256': metadata.sha256(entries['package.json']),
            'inventory': {row['path']: {'size': row['size'], 'sha256': row['sha256']} for row in transport.inventory(entries)}}
    generated = {'package': npm.REACT, 'source_package': npm.WASM, 'source_version': version,
        'source_sha512_integrity': reports['wasm']['sha512_integrity'], 'assets': [
            {'path': 'public/' + name, 'source_path': name, 'size': len(data), 'sha256': metadata.sha256(data)}
            for name, data in sorted(wasm.items()) if name.startswith(('_content/', '_framework/')) or name == 'appsettings.json']}
    pair = {'source_commit': source['commit'], 'source_tree': source['tree'], 'version': version,
        'execution': artifact_execution, 'framework': 'net10.0', 'commands': [{'step': step, 'success': True, 'exit_code': 0} for step in payload.STEPS],
        'success': True, 'published': False, 'historical_workflow_executed': False, 'original_lifecycle_preserved': False,
        'inline_postinstall_preserved': True, 'stage': 'complete', **reports,
        'lifecycle_correction': lifecycle.CONTINUATIONS['3.8'] | {'path': lifecycle.PATH, 'before_blob': lifecycle.BEFORE_BLOB,
            'after_blob': lifecycle.AFTER_BLOB, 'scope': 'inline-copy-script-only'}, 'consumer': {
            'local_archives': {row['name']: {'version': version, 'sha512_integrity': row['sha512_integrity'], 'resolution': 'same-run-local-archive'} for row in reports.values()},
            'empty_install_cache': True, 'normal_lifecycle': True, 'esm': True, 'commonjs': True, 'vite': True,
            'input_sha256': {'package.json': '1' * 64, 'package-lock.json': '2' * 64}, 'lifecycle_generated_assets': generated}}
    files['producer/npm/receipt.json'] = encoded(pair)
    artifact = {'schema': 1, 'mode': 'selected-product-artifact-control', 'plan_sha256': plan_hash,
        'planner_controller': plan['controller'], 'artifact_controller': controller, 'source': source,
        'product': 'studio', 'line': '3.8', 'version': version, 'execution': artifact_execution, 'success': True,
        'published': False, 'version_allocated': False, 'tag_created': False, 'stage': 'complete', 'artifact_proof': True,
        'preflight': {'product_work_executed': False, 'node': 'v22.22.1', 'npm': '10.9.0', 'sdk': metadata.SDK,
            'host_framework': 'net10.0', 'host_supported_frameworks': frameworks,
            'original_workflow_sha256': contract['files'][3]['sha256'], 'host_project_sha256': contract['files'][2]['sha256']},
        'product_tests': {'executions': [{'project': test, 'framework': 'net10.0', 'sha256': '3' * 64,
            'counters': dict.fromkeys(producer.maintenance.TRX_COUNTERS, 0) | {'total': 1, 'executed': 1, 'passed': 1}}], 'inherited_skipped_placeholders': []},
        'packages': {'selected': records, 'private_recipe_only_outputs': []}, 'npm': pair}
    files['producer/receipt.json'] = encoded(artifact)
    artifact_hash = metadata.sha256(files['producer/receipt.json'])
    isolation = {name: metadata.sha256(b'<Project />\n') for name in ('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props')}
    isolation['global.json'] = metadata.sha256((json.dumps({'sdk': {'version': metadata.SDK, 'rollForward': 'disable'}}) + '\n').encode())
    def cell(framework, runtime):
        inputs = isolation | {'NuGet.Config': '1' * 64, 'Consumer.csproj': '2' * 64, 'packages.lock.json': '3' * 64,
            'Program.cs': metadata.sha256(consumer.FIXTURE.read_bytes() if runtime else b'extern alias selected;\npublic class CompileContract {}\n')}
        asset = f'lib/{framework}/{identifier}.dll'
        row = {'id': identifier, 'framework': framework, 'success': True, 'fresh_cache': True, 'package_reference_only': True,
            'accounting': 'managed-reference-compile', 'original_output_policy': output, 'discovery_scope': 'offline-original-archive-metadata-only',
            'native_lock_sha256': '3' * 64, 'archive_sha256': records[0]['sha256'], 'isolation': isolation, 'input_sha256': inputs,
            'restored': [{'id': identifier, 'version': version, 'assets_type': 'package', 'source': 'selected-local-archives',
                'sha256': records[0]['sha256'], 'sha512': hashlib.sha512(files['producer/nuget/' + records[0]['file']]).hexdigest()}],
            'restored_payloads': [{'id': identifier, 'version': version, 'payloads': [{'path': asset, 'kind': kind,
                'sha256': metadata.sha256(dlls[asset]), 'size': len(dlls[asset])} for kind in ('compile', 'runtime')]}]}
        if runtime:
            inputs['AssemblyProof.cs'] = metadata.sha256((consumer.ROOT / 'scripts/integration-program/selected-consumer/AssemblyProof.cs').read_bytes())
            row['runtime'] = {'contract': consumer.runtime_contract(plan)['description'], 'loaded_assemblies': [
                {'name': identifier, 'version': version + '.0', 'informationalVersion': version + '+' + source['commit'],
                 'sha256': metadata.sha256(dlls[asset]), 'package_id': identifier, 'package_version': version, 'package_asset': asset}]}
        return row
    result = {'schema': 1, 'mode': 'selected-product-consumers', 'plan_sha256': plan_hash, 'artifact_receipt_sha256': artifact_hash,
        'source': source, 'controller': controller, 'execution': consumer_execution, 'producer_execution': artifact_execution,
        'planner_controller': plan['controller'], 'artifact_controller': controller,
        'producer_plan_admission': {'scope': 'historical-producer-start-only', 'eligible_at_producer_start': True,
            'plan_observed_at': plan['observed_at'], 'producer_started_at': artifact_execution['started_at'],
            'producer_execution_id': artifact_execution['id'], 'artifact_receipt_sha256': artifact_hash},
        'current_consumer_admission': {'eligible': True, 'scope': 'current-complete-selected-product-prerequisites', 'checked_at': stamp(9),
            'histories': len(plan['histories']), 'prerequisites': 0, 'observations_sha256': '4' * 64},
        'success': True, 'published': False, 'coverage': [cell(item, False) for item in frameworks],
        'runtime': [cell(item, True) for item in frameworks], 'stage': 'complete', 'runtime_contract_source': consumer.STUDIO38_CONTRACT_SOURCE,
        'preflight': {'scope': 'standalone-semantics-utility-only', 'sdk_version': metadata.SDK, 'native_assemblies': plan['semantics']['assemblies']},
        'external_catalog_sha256': '5' * 64, 'limitations': ['Complete selected restore/compile coverage is distinct from representative runtime behavior.', consumer.runtime_contract(plan)['limitation']]}
    files['consumer/receipt.json'] = encoded(result)
    expected = {'context': context, 'controller': controller, 'product': 'studio', 'line': '3.8'}
    return files, expected, now


class StudioPayloadContracts(unittest.TestCase):
    def setUp(self):
        self.files, self.expected, self.now = fixture()

    def validate(self):
        return payload.validate(self.files, self.expected, *(metadata.sha256(self.files[name]) for name in
            ('plan.json', 'producer/receipt.json', 'consumer/receipt.json')), contracts=payload.load_contracts(), now=self.now)

    def mutate(self, name, action):
        value = json.loads(self.files[name])
        action(value)
        self.files[name] = encoded(value)

    def test_full_synthetic_source_bound_vertical_bytes(self):
        result = self.validate()
        self.assertEqual(len(result['consumer']['coverage']), 3)
        self.assertEqual(len(result['consumer']['runtime']), 3)

    def test_unknown_private_receipt_keys(self):
        for name in ('plan.json', 'producer/receipt.json', 'consumer/receipt.json', 'producer/npm/receipt.json'):
            original = self.files[name]
            self.mutate(name, lambda value: value.update(private_path='/private/tmp/raw.log'))
            with self.subTest(name=name), self.assertRaises(ValueError): self.validate()
            self.files[name] = original

    def test_archive_bytes_and_file_partition(self):
        for change in ('tamper', 'extra', 'missing'):
            original = dict(self.files)
            name = next(name for name in self.files if name.endswith('.nupkg'))
            if change == 'tamper': self.files[name] += b'changed'
            elif change == 'extra': self.files['producer/nuget/excluded.3.8.999.nupkg'] = b'private'
            else: del self.files[name]
            with self.subTest(change=change), self.assertRaises(ValueError): self.validate()
            self.files = original

    def test_native_runtime_and_tfm_mismatches(self):
        changes = [lambda value: value['coverage'].pop(),
            lambda value: value['coverage'].append(deepcopy(value['coverage'][0])),
            lambda value: value['runtime'][0]['runtime']['loaded_assemblies'][0].update(sha256='f' * 64),
            lambda value: value['runtime'][0]['runtime']['loaded_assemblies'][0].update(location='/private/tmp/assembly.dll'),
            lambda value: value['runtime'][0]['runtime']['loaded_assemblies'][0].update(version='1.0.0.0'),
            lambda value: value['coverage'][0]['restored'][0].update(source='https://api.nuget.org/v3/index.json'),
            lambda value: value['coverage'][0]['restored_payloads'][0]['payloads'][0].update(sha256='f' * 64),
            lambda value: value['current_consumer_admission'].update(eligible=False),
            lambda value: value['current_consumer_admission'].update(histories=0)]
        original = self.files['consumer/receipt.json']
        for change in changes:
            self.mutate('consumer/receipt.json', change)
            with self.assertRaises(ValueError): self.validate()
            self.files['consumer/receipt.json'] = original

    def test_no_embedded_historical_manifest_or_success_flag_only(self):
        self.mutate('producer/receipt.json', lambda value: value['npm']['wasm'].update(manifest={'claimed': True}))
        with self.assertRaises(ValueError): self.validate()

    def test_unused_configured_feed_does_not_invent_service_observation(self):
        import selected_studio_plan_schema as schema
        plan = json.loads(self.files['plan.json'])
        plan['consumer_feed_policy']['sources'].append({'name': 'original-unused', 'url': 'https://f.feedz.io/personal/webhooks-core/nuget/index.json'})
        schema.validate(plan, payload.load_contracts(), now=self.now)
        original = deepcopy(plan['feed_service_observations'])
        del plan['feed_service_observations']['https://api.nuget.org/v3/index.json']
        with self.assertRaises(ValueError):
            schema.validate(plan, payload.load_contracts(), now=self.now)
        plan['feed_service_observations'] = original
        unused = 'https://f.feedz.io/personal/webhooks-core/nuget/index.json'
        plan['feed_service_observations'][unused] = deepcopy(original['https://api.nuget.org/v3/index.json'])
        plan['feed_service_observations'][unused]['observation']['url'] = unused
        with self.assertRaisesRegex(ValueError, 'feed_service_inventory'):
            schema.validate(plan, payload.load_contracts(), now=self.now)

    def test_native_nuspec_identity_and_dependency_groups_are_byte_bound(self):
        plan, receipt = (json.loads(self.files[name]) for name in ('plan.json', 'producer/receipt.json'))
        original = dict(self.files)
        for before, after in ((b'<id>Elsa.Studio.Core</id>', b'<id>Elsa.Studio.Other</id>'),
                              (b'<dependencies>', b'<dependencies><group targetFramework="net7.0" />')):
            changed_receipt = deepcopy(receipt)
            row = changed_receipt['packages']['selected'][0]
            name = 'producer/nuget/' + row['file']
            members = transport.zip_members(original[name], leaf=True)
            members['a.nuspec'] = members['a.nuspec'].replace(before, after)
            self.files[name] = zipped(list(members.items()))
            row.update(sha256=metadata.sha256(self.files[name]), size=len(self.files[name]), inventory=transport.inventory(members))
            with self.subTest(after=after), self.assertRaisesRegex(ValueError, 'nuspec_'):
                payload.validate_packages(plan, changed_receipt, self.files)
            self.files = dict(original)

    def test_unlisted_skips_fail_and_pass_counts_do_not_absorb_them(self):
        plan, receipt = (json.loads(self.files[name]) for name in ('plan.json', 'producer/receipt.json'))
        receipt['product_tests']['executions'][0]['counters'].update(passed=0, executed=0)
        with self.assertRaisesRegex(ValueError, 'failed_or_skipped'):
            payload.validate_tests(plan, receipt, payload.load_contracts())
        row = receipt['product_tests']['executions'].pop()
        receipt['product_tests']['inherited_skipped_placeholders'].append(row)
        with self.assertRaises(ValueError): payload.validate_tests(plan, receipt, payload.load_contracts())

    def test_private_recipe_metadata_is_accounted_without_uploading_archive(self):
        plan, receipt = (json.loads(self.files[name]) for name in ('plan.json', 'producer/receipt.json'))
        row = deepcopy(receipt['packages']['selected'][0])
        row.update(id='Elsa.Studio.Core.Tests', file='original-recipe-private-output.nupkg')
        receipt['packages']['private_recipe_only_outputs'] = [row]
        self.assertEqual(set(payload.validate_packages(plan, receipt, self.files)), {'elsa.studio.core'})
        self.files['producer/nuget/' + row['file']] = b'excluded'
        with self.assertRaisesRegex(ValueError, 'private_output_scope'): payload.validate_packages(plan, receipt, self.files)
        del self.files['producer/nuget/' + row['file']]
        row['inventory'][0]['path'] = '/private/tmp/secret'
        with self.assertRaises(ValueError): payload.validate_packages(plan, receipt, self.files)

    def test_actual_historical_npm_metadata_and_generated_lifecycle_joins(self):
        plan, receipt = (json.loads(self.files[name]) for name in ('plan.json', 'producer/receipt.json'))
        for change in (lambda value: value['npm']['commands'][0].update(exit_code=1),
                       lambda value: value['npm']['consumer']['lifecycle_generated_assets']['assets'][0].update(sha256='f' * 64),
                       lambda value: value['npm']['consumer'].update(normal_lifecycle=False),
                       lambda value: value['npm']['react'].update(sha512_integrity='sha512-wrong'),
                       lambda value: value['npm']['lifecycle_correction'].update(scope='helper-transplant')):
            altered = deepcopy(receipt)
            change(altered)
            with self.assertRaises(ValueError): payload.validate_npm(plan, altered, self.files, payload.load_contracts()['studio'])

    def test_rehashed_react_tar_cannot_prepopulate_generated_public_assets(self):
        plan, receipt = (json.loads(self.files[name]) for name in ('plan.json', 'producer/receipt.json'))
        report = receipt['npm']
        name = 'producer/npm/' + report['react']['file']
        members = transport.tar_members(self.files[name])
        generated = report['consumer']['lifecycle_generated_assets']['assets'][0]
        wasm = transport.tar_members(self.files['producer/npm/' + report['wasm']['file']])
        members[generated['path']] = wasm[generated['source_path']]
        changed = tarred([('package/' + path, data, tarfile.REGTYPE) for path, data in members.items()])
        self.files[name] = changed
        report['react'].update(sha512_integrity=transport.sha512_integrity(changed), inventory={
            row['path']: {'size': row['size'], 'sha256': row['sha256']} for row in transport.inventory(members)})
        report['consumer']['local_archives'][npm.REACT]['sha512_integrity'] = report['react']['sha512_integrity']
        self.files['producer/npm/receipt.json'] = encoded(report)
        with self.assertRaisesRegex(ValueError, 'npm_generated_collision'):
            payload.validate_npm(plan, receipt, self.files, payload.load_contracts()['studio'])

    def test_execution_bindings_and_historical_clock_are_not_optional(self):
        original = self.files['producer/receipt.json']
        for change in (lambda value: value['execution']['controller'].update(tree='f' * 40),
                       lambda value: value['execution']['source'].update(commit='f' * 40),
                       lambda value: value['execution']['context'].update(run_attempt='99'),
                       lambda value: value['execution'].update(started_at=(self.now + timedelta(days=1)).isoformat()),
                       lambda value: value['execution'].update(started_at=(self.now - timedelta(days=1)).isoformat())):
            self.mutate('producer/receipt.json', change)
            with self.assertRaises(ValueError): self.validate()
            self.files['producer/receipt.json'] = original

    def test_other_cells_remain_explicitly_unimplemented(self):
        for product, line in (('studio', '3.9'), ('core', '3.8'), ('extensions', '3.8')):
            self.expected.update(product=product, line=line)
            with self.assertRaisesRegex(ValueError, 'not_implemented'): self.validate()


if __name__ == '__main__': unittest.main()
