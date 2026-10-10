from contextlib import ExitStack
import hashlib
from http.server import BaseHTTPRequestHandler, HTTPServer
import io
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
from threading import Thread
import unittest
import urllib.error
from unittest.mock import patch
import xml.etree.ElementTree as ET
import zipfile

import prepare_maintenance_build as maintenance
import prove_consolidated_packages as consolidated


class SelectionTests(unittest.TestCase):
    def test_unregistered_or_cross_line_inputs_fail(self):
        for product, line, commit, version in [
            ('core', '3.8', 'a' * 40, '3.8.4-proof.1.1'),
            ('studio', '3.8', 'a' * 40, '3.8.4-proof.1.1'),
            ('studio', '3.8', '9bff3f785fd13bd80a3a7ecf88fec4aec8eef7ae', '3.10.0-proof.1.1'),
        ]:
            with self.subTest(product=product, version=version), self.assertRaises(ValueError):
                maintenance.selection(maintenance.load_register(), product, line, commit, version)

class MaintenanceContracts(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.register = maintenance.load_register()
        self.row = self.register['sources'][0]
        (self.root / 'test-inventory.json').write_text(json.dumps([{'project': 'Fixture.Tests.csproj', 'assembly_name': 'Fixture.Tests', 'frameworks': ['net8.0']}]))

    def write_trx_fixture(self, product='studio', name='private-runner-host', assembly=None,
                          counters=None, outcomes=None, test_class='Fixture.Tests', method='Runs', definition_ids=None):
        results = self.root / ('test-results' if product == 'studio' else 'source/testresults')
        results.mkdir(parents=True, exist_ok=True)
        assembly = assembly or self.root / 'source/bin/Release/net8.0/Fixture.Tests.dll'
        outcomes = ['Passed'] * 3 if outcomes is None else outcomes
        counters = counters or {'total': len(outcomes), 'executed': len(outcomes), 'passed': len(outcomes),
                                'failed': 0, 'notExecuted': 0}
        tree = ET.Element('TestRun')
        ET.SubElement(ET.SubElement(tree, 'ResultSummary', outcome='Completed'), 'Counters',
                      {key: str(value) for key, value in (dict.fromkeys(maintenance.TRX_COUNTERS, 0) | counters).items()})
        definitions, results_element = ET.SubElement(tree, 'TestDefinitions'), ET.SubElement(tree, 'Results')
        entries = ET.SubElement(tree, 'TestEntries')
        defined = set()
        for index, outcome in enumerate(outcomes):
            identifier = definition_ids[index] if definition_ids is not None else f'test-{index}'
            if identifier not in defined:
                definition = ET.SubElement(definitions, 'UnitTest', id=identifier)
                ET.SubElement(definition, 'Execution', id=f'execution-{index}')
                ET.SubElement(definition, 'TestMethod', codeBase=str(assembly), className=test_class, name=method)
                defined.add(identifier)
            ET.SubElement(entries, 'TestEntry', testId=identifier, executionId=f'execution-{index}')
            ET.SubElement(results_element, 'UnitTestResult', testId=identifier, executionId=f'execution-{index}', outcome=outcome)
        path = results / (name + '.trx')
        path.write_text(ET.tostring(tree, encoding='unicode'))
        return path

    def write_placeholder_fixture(self):
        source = self.root / 'source'
        source.mkdir(exist_ok=True)
        # Immutable object reads only; no checkout or product build is needed.
        gitdir = maintenance.git(maintenance.ROOT, 'rev-parse', '--absolute-git-dir')
        (source / '.git').write_text('gitdir: ' + gitdir + '\n')
        tests = [{'project': 'Fixture.Tests.csproj', 'assembly_name': 'Fixture.Tests', 'frameworks': ['net8.0']}]
        paths = []
        policies = self.register['inherited_skipped_placeholders']
        for index, policy in enumerate(policies):
            assembly_name = Path(policy['project']).stem
            tests.append({'project': policy['project'], 'assembly_name': assembly_name, 'frameworks': [policy['framework']]})
            assembly = source / Path(policy['project']).parent / 'bin/Release' / policy['framework'] / (assembly_name + '.dll')
            paths.append(self.write_trx_fixture('extensions', f'0{index}-placeholder', assembly,
                {'total': 1, 'executed': 0, 'passed': 0, 'failed': 0, 'notExecuted': 0}, ['NotExecuted'],
                policy['class'], policy['method']))
        (self.root / 'test-inventory.json').write_text(json.dumps(tests))
        paths.append(self.write_trx_fixture('extensions', '02-runnable'))
        return paths

    def test_all_four_exact_sources_and_only_proof_versions_are_admitted(self):
        for row in self.register['sources']:
            with self.subTest(row=row['source_ref']):
                accepted = maintenance.selection(self.register, row['product'], row['line'], row['commit'],
                                                 row['dependency_version'] + '-proof.42.1')
                self.assertEqual(accepted['tree'], row['tree'])
                for version in ['3.10.0-proof.1.1', '3.8.4', 'v3.8.4-proof.1.1',
                                row['dependency_version'] + '-proof.01.1', '$(touch /tmp/unsafe)']:
                    with self.assertRaises(ValueError):
                        maintenance.selection(self.register, row['product'], row['line'], row['commit'], version)
                with self.assertRaises(ValueError):
                    maintenance.selection(self.register, 'extensions' if row['product'] == 'studio' else 'studio',
                                          row['line'], row['commit'], row['dependency_version'] + '-proof.42.1')

    def test_subprocess_environment_drops_credentials_and_actions_write_handles(self):
        with patch.dict(os.environ, {'GH_TOKEN': 'secret', 'NODE_AUTH_TOKEN': 'secret',
                                    'FEEDZ_API_KEY': 'secret', 'GITHUB_TOKEN': 'secret',
                                    'ACTIONS_ID_TOKEN_REQUEST_TOKEN': 'secret', 'GITHUB_OUTPUT': '/unsafe',
                                    'PATH': '/safe'}, clear=True):
            self.assertEqual(maintenance.build_environment(), {'PATH': '/safe', 'EmbedUntrackedSources': 'true'})

    def test_recipe_restore_config_comes_only_from_source_and_preserves_core_environment(self):
        config = self.root / 'NuGet.Config'; config.write_text('<configuration />')
        gate = 'ELSA_POSTGRES_DB'
        for product in ('studio', 'extensions', 'core'):
            with self.subTest(product=product), patch.dict(os.environ,
                    {'HOME': '/unchanged-home', 'RestoreConfigFile': '/untrusted/config',
                     'GITHUB_TOKEN': 'secret'}, clear=True), \
                    patch.object(maintenance, 'original_core', return_value=product == 'core'), \
                    patch('selected_core_producer.environment', return_value={'VERSION': '3.8.999', gate: 'connection'}):
                environment = maintenance.recipe_environment({'product': product}, '3.8.999', self.root)
                self.assertEqual(str(config.resolve()), environment['RestoreConfigFile'])
                self.assertNotIn('GITHUB_TOKEN', environment)
                if product == 'core':
                    self.assertEqual('3.8.999', environment['VERSION'])
                    self.assertEqual('connection', environment[gate])
                else:
                    self.assertEqual('/unchanged-home', environment['HOME'])
        with patch.dict(os.environ, {'RestoreConfigFile': '/untrusted/config'}, clear=True):
            self.assertNotIn('RestoreConfigFile', maintenance.build_environment())

    def test_recipe_restore_config_missing_directory_or_symlink_fails(self):
        config = self.root / 'NuGet.Config'
        for kind in ('missing', 'directory', 'symlink'):
            if kind == 'directory':
                config.mkdir()
            elif kind == 'symlink':
                config.rmdir()
                other = self.root / 'Other.Config'; other.write_text('<configuration />')
                config.symlink_to(other)
            with self.subTest(kind=kind), self.assertRaisesRegex(ValueError, 'regular file'):
                maintenance.recipe_environment(self.row, '3.8.999', self.root)

    def test_prepare_passes_bound_environment_and_rejects_changed_source_config_before_tools(self):
        original = b'<configuration />'
        for product, changed in (('studio', False), ('extensions', False), ('extensions', True), ('extensions', 'plan')):
            row = next(row for row in self.register['sources'] if row['product'] == product)
            output = self.root / (product + str(changed))
            def checkout(root, *args, **_kwargs):
                if args[:1] == ('checkout',):
                    (root / 'NuGet.Config').write_bytes(b'<changed />' if changed is True else original)
                return 'd' * 40
            with self.subTest(product=product, changed=changed), \
                    patch.object(maintenance, 'git', side_effect=checkout), \
                    patch.object(maintenance, 'git_bytes', return_value=original), \
                    patch.object(maintenance, 'verify_source'), \
                    patch.object(maintenance, 'inspect_toolchain', return_value={}) as tools, \
                    patch.object(maintenance, 'recipes', return_value=[('.', ['fixture-only'])]), \
                    patch.object(maintenance, 'run_build_command', side_effect=ValueError('stop-before-build')) as command:
                with self.assertRaisesRegex(ValueError, 'differs from plan' if changed == 'plan' else
                        'differs from source' if changed else 'stop-before-build'):
                    plan = {'consumer_feed_policy': {'config_sha256': '0' * 64}} if changed == 'plan' else None
                    maintenance.prepare(maintenance.ROOT, row, '3.8.999', output, plan=plan)
                if changed:
                    tools.assert_not_called(); command.assert_not_called()
                else:
                    self.assertEqual(str((output / 'source/NuGet.Config').resolve()),
                                     command.call_args.kwargs['environment']['RestoreConfigFile'])

    def test_source_tree_parent_and_workflow_inventory_are_verified(self):
        maintenance.verify_source(maintenance.ROOT, self.row)
        for field in ['tree', 'parent']:
            with self.subTest(field=field), self.assertRaises(ValueError):
                maintenance.verify_source(maintenance.ROOT, dict(self.row, **{field: 'a' * 40}))
        with self.assertRaises(ValueError):
            maintenance.verify_source(maintenance.ROOT, dict(self.row, workflows=[]))

    def test_containment_moves_only_workflows_preserving_blobs_and_original_parent(self):
        source = self.root / 'git'; source.mkdir()
        subprocess.run(['git', 'init', '-q', str(source)], check=True)
        workflows = source / '.github/workflows'; workflows.mkdir(parents=True)
        (workflows / 'packages.yml').write_text('on: push\n')
        (source / 'Product.cs').write_text('class Product {}\n')
        env = os.environ | {'GIT_AUTHOR_NAME': 'Test', 'GIT_AUTHOR_EMAIL': 'test@example.invalid',
                            'GIT_COMMITTER_NAME': 'Test', 'GIT_COMMITTER_EMAIL': 'test@example.invalid'}
        subprocess.run(['git', 'add', '.'], cwd=source, check=True)
        subprocess.run(['git', 'commit', '-qm', 'source'], cwd=source, env=env, check=True)
        parent = maintenance.git(source, 'rev-parse', 'HEAD')
        # Source register requires an original parent; add the actual release commit.
        (source / 'Product.cs').write_text('class Product { }\n')
        subprocess.run(['git', 'commit', '-qam', 'release'], cwd=source, env=env, check=True)
        commit = maintenance.git(source, 'rev-parse', 'HEAD')
        row = dict(self.row, commit=commit, parent=parent, tree=maintenance.git(source, 'rev-parse', 'HEAD^{tree}'),
                   workflows=['.github/workflows/packages.yml'])
        result = maintenance.prepare_containment(source, self.root / 'contained', {'sources': [row]})['sources'][0]
        self.assertEqual(maintenance.git(source, 'rev-parse', result['commit'] + '^'), commit)
        self.assertEqual(maintenance.git(source, 'ls-tree', '-r', '--name-only', result['commit'], '--', '.github/workflows'), '')
        changed = maintenance.git(source, 'diff', '--no-renames', '--name-only', commit, result['commit']).splitlines()
        self.assertEqual(changed, ['.github/maintenance-inert-workflows/packages.yml.source', '.github/workflows/packages.yml'])
        self.assertEqual(maintenance.git(source, 'show-ref', '--heads').split()[0], commit)
        for name, value in [('diff.renames', 'false'), ('diff.noprefix', 'true'), ('diff.external', '/private/not-an-executable'),
                            ('diff.orderFile', '/private/not-an-orderfile'), ('diff.relative', 'true')]:
            maintenance.git(source, 'config', name, value)
        rerun = maintenance.prepare_containment(source, self.root / 'again', {'sources': [row]})['sources'][0]
        self.assertEqual(result['commit'], rerun['commit'])
        self.assertEqual(result['patch_sha256'], rerun['patch_sha256'])

    def test_studio_quality_checks_precede_generated_asset_build_and_npm_pack_is_excluded(self):
        row = next(r for r in self.register['sources'] if r['product'] == 'studio' and r['line'] == '3.9')
        commands = [cmd for _, cmd in maintenance.recipes(row, '3.9.0-proof.42.1', self.root)]
        self.assertLess(commands.index(['npm', 'run', 'check:generated']), commands.index(['npm', 'run', 'build']))
        self.assertLess(commands.index(['npm', 'test']), commands.index(['npm', 'run', 'build']))
        self.assertNotIn(['npm', 'pack'], commands)
        self.assertFalse(any('publish' in command for command in commands))

    def test_existing_or_checkout_nested_output_is_rejected_before_build(self):
        for output in [self.root, maintenance.ROOT / 'new-maintenance-proof']:
            with self.subTest(output=output), self.assertRaises(ValueError):
                maintenance.prepare(maintenance.ROOT, self.row, '3.8.4-proof.42.1', output)

    def test_failed_command_retains_false_receipt_and_no_success_seal(self):
        output = self.root / 'proof'
        with patch.object(maintenance, 'verify_source', side_effect=ValueError('source changed')):
            with self.assertRaisesRegex(ValueError, 'source changed'):
                maintenance.prepare(maintenance.ROOT, self.row, '3.8.4-proof.42.1', output)
        receipt = json.loads((output / 'receipt.json').read_text())
        self.assertFalse(receipt['success'])
        self.assertFalse(receipt['published'])
        self.assertEqual(receipt['error'], {'code': 'source-verification-failed', 'reason': 'unknown-check-failure'})
        self.assertNotIn('source changed', json.dumps(receipt))
        self.assertNotIn(str(self.root), json.dumps(receipt))

    def prepare_inventory_fixture(self, failure=None):
        output = self.root / ('proof-' + (failure or 'success'))
        policy = {'id': 'Fixture', 'project': 'src/Fixture.csproj', 'assembly_name': 'Fixture',
                  'frameworks': ['net8.0'], 'symbols': True, 'include_build_output': True, 'satellites': [],
                  'private_context': '/private-secret/context'}
        config = b'<configuration />'
        def checkout(root, *args, **_kwargs):
            if args[:1] == ('checkout',):
                (root / 'NuGet.Config').write_bytes(config)
            return 'd' * 40
        def tests(*_args):
            if failure == 'test-evidence':
                raise ValueError('Test evidence rejected')
            return {'executions': []}
        def execute(command, *_args, **_kwargs):
            if command[:2] == ['dotnet', 'build'] and failure == 'symbol-inspector':
                raise ValueError('/private-secret/helper failure')
            return '10.0.300'
        def stage(*_args):
            policy.update(expected_dependency_groups=[], sdk_nuspec_sha256='a' * 64,
                expected_framework_reference_groups=[{'framework': 'net8.0', 'references': ['Microsoft.AspNetCore.App']}],
                expected_symbol_framework_reference_groups=[],
                restore_assets=[{'framework': 'net8.0', 'path': '/private-secret/restore', 'sha256': 'b' * 64}],
                framework_properties={'net8.0': {'compiler_evidence': {'sdk_version': '10.0.300',
                    'sdk_root': '/private-secret/sdk', 'compiler_sha256': 'c' * 64, 'tools': {}}}})
            if failure == 'sdk-metadata':
                raise ValueError('/private-secret/metadata failure')
        with ExitStack() as stack:
            for name, options in {
                'git': {'side_effect': checkout}, 'git_bytes': {'return_value': config},
                'verify_source': {'return_value': None},
                'recipes': {'return_value': []}, 'run': {'side_effect': execute},
                'evaluate_inventory': {'return_value': [policy]}, 'verify_tests': {'side_effect': tests},
                'stage_maintenance_metadata': {'side_effect': stage}, 'verify_artifacts': {'return_value': []},
            }.items():
                stack.enter_context(patch.object(maintenance, name, **options))
            if failure:
                with self.assertRaises(ValueError):
                    maintenance.prepare(maintenance.ROOT, self.row, '3.8.4-proof.42.1', output)
            else:
                maintenance.prepare(maintenance.ROOT, self.row, '3.8.4-proof.42.1', output)
        return output

    def test_post_evaluation_failures_retain_safe_base_inventory_and_failed_receipt(self):
        expected = [{'id': 'Fixture', 'project': 'src/Fixture.csproj', 'assembly_name': 'Fixture',
                     'frameworks': ['net8.0'], 'symbols': True, 'include_build_output': True, 'satellites': [],
                     'restore_inputs': [], 'producers': {}}]
        for stage in ['test-evidence', 'symbol-inspector', 'sdk-metadata']:
            with self.subTest(stage=stage):
                output = self.prepare_inventory_fixture(stage)
                inventory = json.loads((output / 'evaluated-inventory.json').read_text())
                self.assertEqual(inventory, expected)
                receipt = json.loads((output / 'receipt.json').read_text())
                self.assertFalse(receipt['success'])
                self.assertFalse(receipt['published'])
                self.assertFalse(receipt['maintenance_refs_activated'])
                self.assertEqual(receipt['error']['code'], stage + '-failed')
                self.assertNotIn('private-secret', json.dumps(inventory) + json.dumps(receipt))

    def test_success_replaces_base_inventory_with_safe_enriched_evidence(self):
        output = self.prepare_inventory_fixture()
        inventory = json.loads((output / 'evaluated-inventory.json').read_text())
        self.assertEqual(inventory[0]['id'], 'Fixture')
        self.assertEqual(inventory[0]['expected_dependency_groups'], [])
        self.assertEqual(inventory[0]['expected_framework_reference_groups'],
                         [{'framework': 'net8.0', 'references': ['Microsoft.AspNetCore.App']}])
        self.assertEqual(inventory[0]['expected_symbol_framework_reference_groups'], [])
        self.assertEqual(inventory[0]['sdk_nuspec_sha256'], 'a' * 64)
        self.assertEqual(inventory[0]['restore_inputs'], [{'framework': 'net8.0', 'sha256': 'b' * 64}])
        self.assertEqual(inventory[0]['producers'], {'net8.0': {'sdk_version': '10.0.300',
                         'compiler_sha256': 'c' * 64, 'tools': {}}})
        receipt = json.loads((output / 'receipt.json').read_text())
        self.assertTrue(receipt['success'])
        self.assertEqual(receipt['stage'], 'complete')
        self.assertFalse(receipt['published'])
        self.assertFalse(receipt['maintenance_refs_activated'])
        self.assertNotIn('private-secret', json.dumps(inventory) + json.dumps(receipt))

    def test_missing_tests_cannot_be_reported_as_success(self):
        with self.assertRaises(ValueError):
            maintenance.verify_tests(self.root, self.row)
        extension = dict(next(row for row in self.register['sources'] if row['product'] == 'extensions'), commit='0' * 40)
        (self.root / 'command-00.log').write_text('Build succeeded.\n')
        with self.assertRaises(ValueError):
            maintenance.verify_tests(self.root, extension)
        self.write_trx_fixture('extensions')
        self.assertEqual(maintenance.verify_tests(self.root, extension)['executions'][0]['counters']['passed'], 3)

    def test_source_document_checksum_binds_original_git_not_changed_workspace(self):
        prefix = f"https://raw.githubusercontent.com/{self.row['source_repository']}/{self.row['commit']}/"
        original = subprocess.run(['git', 'show', self.row['commit'] + ':Directory.Build.props'],
                                  cwd=maintenance.ROOT, check=True, capture_output=True).stdout
        details = {'source_link': {'documents': {'/_/*': prefix + '*'}}, 'documents': [
            {'path': '/_/Directory.Build.props', 'algorithm': 'sha256', 'checksum': hashlib.sha256(original).hexdigest(),
             'embedded_checksum': None}]}
        verified = maintenance.verify_documents(details, maintenance.ROOT, self.row)
        self.assertEqual(verified[0]['source'], 'original-git')
        details['documents'][0]['checksum'] = '0' * 64
        with self.assertRaises(ValueError):
            maintenance.verify_documents(details, maintenance.ROOT, self.row)
        details['source_link']['documents'] = {'/_/*': 'https://example.invalid/*'}
        with self.assertRaises(ValueError):
            maintenance.verify_documents(details, maintenance.ROOT, self.row)

    def test_generated_source_requires_project_framework_and_actual_producer(self):
        prefix = f"https://raw.githubusercontent.com/{self.row['source_repository']}/{self.row['commit']}/"
        checksum = hashlib.sha256(b'generated source').hexdigest()
        project = 'src/Fixture/Fixture.csproj'
        path = 'src/Fixture/obj/Release/net8.0/Fixture.AssemblyInfo.cs'
        document = {'path': '/_/' + path, 'algorithm': 'sha256', 'checksum': checksum, 'embedded_checksum': checksum}
        details = {'source_link': {'documents': {'/_/*': prefix + '*'}}, 'documents': [document]}
        policy = {'id': 'Fixture', 'project': project, 'source_commit': self.row['commit'],
                  'frameworks': ['net8.0'], 'framework_properties': {'net8.0': {}}}
        producer = {'kind': 'sdk', 'sdk_version': '10.0.300', 'compiler_sha256': 'a' * 64,
                    'sdk_root': '/private-secret/sdk', 'archive_path': '/private-secret/archive',
                    'frameworks': [{'Identity': 'Microsoft.NETCore.App', 'TargetingPackName': 'Microsoft.NETCore.App.Ref',
                                    'TargetingPackVersion': '8.0.28', 'TargetingPackPath': '/private-secret/pack'}]}
        with patch.object(maintenance, 'verify_generator_identity', return_value=producer) as verified:
            result = maintenance.verify_documents(details, maintenance.ROOT, self.row, policy, 'net8.0')
            self.assertEqual(result[0]['family'], 'sdk')
            self.assertEqual(result[0]['path'], '[embedded]/document-1')
            self.assertNotIn('private-secret', json.dumps(result))
            self.assertEqual(verified.call_args.args[3], 'sdk')
            for bad_path in ['/unmapped/private-secret.g.cs', '/_/obj/private-secret.g.cs',
                             '/_/src/Other/obj/Release/net8.0/Other.AssemblyInfo.cs',
                             '/_/src/Fixture/obj/Release/net9.0/Fixture.AssemblyInfo.cs',
                             '/_/src/Fixture/obj/Release/net8.0/arbitrary.cs']:
                with self.subTest(path=bad_path), self.assertRaisesRegex(ValueError, 'producer evidence'):
                    maintenance.verify_documents(dict(details, documents=[dict(document, path=bad_path)]),
                                                 maintenance.ROOT, self.row, policy, 'net8.0')
            for mutation in [dict(policy, source_commit='0' * 40), dict(policy, frameworks=[]),
                             dict(policy, framework_properties={}), None]:
                with self.subTest(policy=mutation), self.assertRaisesRegex(ValueError, 'producer evidence'):
                    maintenance.verify_documents(details, maintenance.ROOT, self.row, mutation, 'net8.0')
        with patch.object(maintenance, 'verify_generator_identity', side_effect=ValueError('/private-secret/wrong tool')):
            with self.assertRaisesRegex(ValueError, '^Source producer evidence rejected$'):
                maintenance.verify_documents(details, maintenance.ROOT, self.row, policy, 'net8.0')
        for embedded in [None, '0' * 64]:
            with self.subTest(embedded=embedded), self.assertRaisesRegex(ValueError, 'producer evidence'):
                maintenance.verify_documents(dict(details, documents=[dict(document, embedded_checksum=embedded)]),
                                             maintenance.ROOT, self.row, policy, 'net8.0')
        tracked = dict(document, path='/_/Directory.Build.props')
        with self.assertRaisesRegex(ValueError, 'Tracked source checksum mismatch'):
            maintenance.verify_documents(dict(details, documents=[tracked]), maintenance.ROOT, self.row, policy, 'net8.0')

    def test_external_hints_require_archive_bytes_and_actual_compile_membership(self):
        row = next(row for row in self.register['sources'] if row['product'] == 'extensions')
        archive = self.root / 'package.nupkg'; archive.write_bytes(b'archive')
        entry = 'contentFiles/cs/any/Hints/Hint.cs'
        extracted = self.root / entry; extracted.parent.mkdir(parents=True); extracted.write_bytes(b'hints')
        checksum = hashlib.sha256(b'hints').hexdigest()
        document = {'path': '/_1/elsa.platform.packagemanifest.generator/0.0.1-preview.50/' + entry,
                    'algorithm': 'sha256', 'checksum': checksum, 'embedded_checksum': checksum}
        inputs = [{'path': entry, 'tracked': False}]
        policy = {'source_commit': row['commit'], 'frameworks': ['net10.0'], 'project': 'Fixture.csproj',
                  'framework_properties': {'net10.0': {'compiler_evidence': {'compile_inputs': inputs}}}}
        external = {'archive_entry': entry, 'archive_sha256': 'a' * 64, 'external_package':
                    'Elsa.Platform.PackageManifest.Generator/0.0.1-preview.50', 'feed': 'official'}
        identity = {'archive_sha256': 'a' * 64, 'restore_sha512': 'restored hash'}
        with patch.object(maintenance, 'verify_external_document', return_value=external), \
             patch.object(maintenance, 'restored_assets', return_value={}), \
             patch.object(maintenance, 'restored_archive', return_value=(archive, identity)):
            result = maintenance.verify_non_git_document(document, None, self.root, row, policy, 'net10.0', {})
            self.assertEqual(result['family'], 'manifest-hints')
            self.assertNotIn(str(self.root), json.dumps(result))
            inputs[:] = [{'path': extracted.resolve().as_posix()}]
            self.assertEqual(maintenance.verify_non_git_document(document, None, self.root / 'other-source',
                row, policy, 'net10.0', {})['family'], 'manifest-hints')
            for mutation in ['membership', 'bytes', 'archive', 'version']:
                selected = dict(document)
                inputs[:] = [{'path': entry}]
                extracted.write_bytes(b'hints'); identity['archive_sha256'] = 'a' * 64
                if mutation == 'membership':
                    inputs.clear()
                elif mutation == 'bytes':
                    extracted.write_bytes(b'changed')
                elif mutation == 'archive':
                    identity['archive_sha256'] = 'b' * 64
                else:
                    selected['path'] = selected['path'].replace('preview.50', 'preview.53')
                with self.subTest(mutation=mutation), self.assertRaisesRegex(ValueError, '^Source producer evidence rejected$'):
                    maintenance.verify_non_git_document(selected, None, self.root, row, policy, 'net10.0', {})

    def test_physical_families_are_original_project_and_release_bound(self):
        row = next(row for row in self.register['sources'] if row['product'] == 'extensions' and row['line'] == '3.8')
        policy = {'id': 'Elsa.Caching.Distributed.ProtoActor', 'project':
                  'src/modules/caching/Elsa.Caching.Distributed.ProtoActor/Elsa.Caching.Distributed.ProtoActor.csproj'}
        prefix = str(Path(policy['project']).parent) + '/obj/Release/net10.0/'
        grain = 'protopotato/LocalCache-' + 'A' * 32 + '.cs'
        self.assertEqual(maintenance.maintenance_family(row, policy, 'net10.0', prefix + grain), 'protograin')
        self.assertEqual(maintenance.maintenance_family(row, policy, 'net10.0', prefix + 'Proto/LocalCacheMessages.cs'), 'grpc')
        for selected, selected_policy, framework, path in [
                (dict(row, line='3.9'), policy, 'net10.0', prefix + grain),
                (row, dict(policy, id='Other'), 'net10.0', prefix + grain),
                (row, policy, 'net8.0', prefix + grain), (row, policy, 'net10.0', prefix + 'Proto/Arbitrary.cs')]:
            self.assertIsNone(maintenance.maintenance_family(selected, selected_policy, framework, path))

    def test_known_generated_families_still_require_actual_producer_verification(self):
        policy = {'id': 'Fixture', 'project': 'src/Fixture/Fixture.csproj', 'source_commit': self.row['commit'],
                  'frameworks': ['net10.0'], 'framework_properties': {'net10.0': {}}}
        names = {
            'Fixture.GlobalUsings.g.cs': 'sdk', 'EmbeddedAttribute.cs': 'razor',
            'InterfaceStubGeneratorV2/Refit.Generator.InterfaceStubGeneratorV2/IFixture.g.cs': 'refit',
            'Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs': 'logging',
            'System.Text.RegularExpressions.Generator/System.Text.RegularExpressions.Generator.RegexGenerator/RegexGenerator.g.cs': 'regex',
            'System.Text.Json.SourceGeneration/System.Text.Json.SourceGeneration.JsonSourceGenerator/Context.Fixture.g.cs': 'json',
            'Microsoft.CodeAnalysis.Razor.Compiler/Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator/Pages_Fixture_razor.g.cs': 'razor',
            'Microsoft.CodeAnalysis.ResxSourceGenerator.CSharp/Microsoft.CodeAnalysis.ResxSourceGenerator.CSharp.CSharpResxGenerator/Translations.Designer.cs': 'resx',
            'PolySharp.SourceGenerators/PolySharp.SourceGenerators.PolyfillsGenerator/System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute.g.cs': 'polysharp'}
        document = {'checksum': 'a' * 64, 'embedded_checksum': 'a' * 64}
        for name, family in names.items():
            with self.subTest(family=family), patch.object(maintenance, 'verify_generator_identity', return_value={'kind': 'sdk'}) as verifier:
                result = maintenance.verify_non_git_document(document, 'src/Fixture/obj/Release/net10.0/' + name,
                    self.root, self.row, policy, 'net10.0', {})
                self.assertEqual(result['family'], family)
                self.assertEqual(verifier.call_args.args[3], family)

    def test_metadata_stage_binds_restore_and_retains_only_public_projection(self):
        source = self.root / 'source'; source.mkdir()
        (source / 'NuGet.Config').write_text('<configuration />')
        (source / 'Fixture.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk" />')
        assets = source / 'obj/project.assets.json'; assets.parent.mkdir(); assets.write_text('{}')
        policy = {'id': 'Fixture', 'project': 'Fixture.csproj', 'symbols': True, 'frameworks': ['net8.0']}
        evidence = {'sdk_version': '10.0.300', 'compiler_sha256': 'c' * 64, 'sdk_root': '/private-secret/sdk',
                    'compile_inputs': [{'path': '/private-secret/compile'}], 'tools': {'refit': {
                        'kind': 'nuget', 'package_id': 'Refit', 'package_version': '9.0.2',
                        'tool_path': '/private-secret/tool', 'archive_path': '/private-secret/archive', 'content_sha256': 'd' * 64}}}
        commands = []
        def execute(command, cwd, **kwargs):
            self.assertEqual(cwd, source.resolve())
            self.assertNotIn('GH_TOKEN', kwargs['env'])
            commands.append(command)
            if '-getProperty:ProjectAssetsFile' in command:
                return str(assets)
            if '-target:_GetRestoreProjectStyle;GenerateNuspec' in command:
                destination = Path(next(arg.split('=', 1)[1] for arg in command if arg.startswith('-p:NuspecOutputPath=')))
                for suffix in ['', '.symbols']:
                    (destination / ('Fixture.3.8.4-proof.42.1' + suffix + '.nuspec')).write_text(
                        '<package><metadata><id>Fixture</id><dependencies><group targetFramework="net8.0" /></dependencies>'
                        '<frameworkReferences><group targetFramework="net8.0">'
                        '<frameworkReference name="Microsoft.AspNetCore.App" /></group></frameworkReferences></metadata></package>')
                return ''
            return json.dumps({'Properties': {'ProjectAssetsFile': str(assets), 'GenerateElsaPackageManifest': '',
                'ElsaPackageManifestIncludeInPackage': '', 'ElsaPackageManifestPackagePath': ''}, 'Items': {}})
        with patch.dict(os.environ, {'GH_TOKEN': 'private-secret'}), patch.object(maintenance, 'run', side_effect=execute), \
             patch.object(maintenance, 'capture_compiler_evidence', return_value=evidence) as captured:
            maintenance.stage_maintenance_metadata(source, self.row, [policy], '3.8.4-proof.42.1', Path('/inspector'))
        self.assertIn('-p:NoBuild=true', commands[1])
        self.assertIn('-p:ContinuePackingAfterGeneratingNuspec=false', commands[1])
        self.assertIn('-p:EmbedUntrackedSources=true', commands[1])
        self.assertTrue(any(arg.endswith('/forbidden-packages') for arg in commands[1]))
        self.assertEqual(captured.call_args.kwargs['physical_families'], ())
        retained = maintenance.public_inventory([policy])
        self.assertEqual(retained[0]['expected_symbol_dependency_groups'], [{'framework': 'net8.0', 'dependencies': []}])
        for key in ['expected_framework_reference_groups', 'expected_symbol_framework_reference_groups']:
            self.assertEqual(retained[0][key], [{'framework': 'net8.0', 'references': ['Microsoft.AspNetCore.App']}])
        self.assertEqual(retained[0]['restore_inputs'][0]['sha256'], hashlib.sha256(b'{}').hexdigest())
        self.assertNotIn('private-secret', json.dumps(retained))
        self.assertNotIn(str(source), json.dumps(retained))
        self.assertEqual(list(source.glob('maintenance-metadata-*')), [])

    def test_proof_embedding_property_is_fixed_in_environment_and_studio_commands(self):
        with patch.dict(os.environ, {'EmbedUntrackedSources': 'false', 'GH_TOKEN': 'private-secret'}, clear=True):
            environment = maintenance.build_environment()
        self.assertEqual(environment, {'EmbedUntrackedSources': 'true'})
        observed = subprocess.run([sys.executable, '-c',
            "import os; print(os.environ.get('EmbedUntrackedSources')); print(os.environ.get('GH_TOKEN'))"],
            env=environment, capture_output=True, text=True, check=True).stdout.splitlines()
        self.assertEqual(observed, ['true', 'None'])
        for row in self.register['sources']:
            commands = maintenance.recipes(row, row['dependency_version'] + '-proof.42.1', self.root)
            for _, command in commands:
                if command[:2] in [['dotnet', 'build'], ['dotnet', 'test'], ['dotnet', 'pack']]:
                    self.assertIn('/p:EmbedUntrackedSources=true', command)

    def test_failed_extensions_test_evidence_retains_only_admitted_cells_and_counts(self):
        row = dict(next(row for row in self.register['sources'] if row['product'] == 'extensions'), commit='0' * 40)
        known = self.root / 'source/bin/Release/net8.0/Fixture.Tests.dll'
        private = '/private/secret-token/machine-host/Unknown.dll'
        for name, assembly in [('first', known), ('second', known), ('third', private)]:
            self.write_trx_fixture('extensions', name, assembly)
        context = {}
        with self.assertRaises(ValueError):
            maintenance.verify_tests(self.root, row, context)
        cell = {'project': 'Fixture.Tests.csproj', 'framework': 'net8.0'}
        self.assertEqual(context['expected_cells'], [cell])
        self.assertEqual(context['admitted_observed_cells'], [cell | {'occurrences': 2}])
        self.assertEqual(context['unknown_path_count'], 1)
        self.assertEqual(context['duplicate_cell_count'], 1)
        self.assertEqual(context['positive_summary_count'], 2)
        self.assertEqual(context['summary_count'], 3)
        self.assertEqual(context['unknown_test_identity_count'], 1)
        self.assertEqual(context['failure_reasons'], ['test-cells-incomplete-or-duplicate', 'test-identity-unknown'])
        retained = json.dumps(context)
        for value in [str(self.root), 'secret-token', 'machine-host', 'Unknown.dll', '/private']:
            self.assertNotIn(value, retained)

    def test_both_original_sources_separate_exact_placeholders_from_positive_tests(self):
        for row in [row for row in self.register['sources'] if row['product'] == 'extensions']:
            with self.subTest(commit=row['commit']):
                self.write_placeholder_fixture()
                context = {}
                result = maintenance.verify_tests(self.root, row, context)
                self.assertEqual(len(result['executions']), 1)
                self.assertEqual(result['executions'][0]['counters']['passed'], 3)
                placeholders = result['inherited_skipped_placeholders']
                self.assertEqual(len(placeholders), 2)
                for actual, policy in zip(placeholders, self.register['inherited_skipped_placeholders']):
                    self.assertEqual(actual['source_commit'], row['commit'])
                    self.assertEqual(actual['source_blob'], policy['source_blob'])
                    self.assertEqual(actual['skip_reason'], policy['skip_reason'])
                    self.assertEqual(actual['not_executed_result_count'], 1)
                    self.assertEqual(actual['counters'], dict.fromkeys(maintenance.TRX_COUNTERS, 0) | {'total': 1})
                self.assertEqual(context['positive_summary_count'], 1)
                self.assertEqual(context['summary_count'], 3)
                self.assertEqual(context['failure_reasons'], [])
                self.assertNotIn(str(self.root), json.dumps(result))

    def test_placeholder_policy_requires_immutable_blob_and_original_skip_declaration(self):
        row = next(row for row in self.register['sources'] if row['product'] == 'extensions')
        self.write_placeholder_fixture()
        for field, changed in [('source_blob', '0' * 40), ('skip_reason', 'private-secret-host'),
                               ('method', 'UnknownPrivateMethod'), ('class', 'Unknown.PrivateClass'),
                               ('class', 'Elsa.ServiceBus.AzureServiceBus.ComponentTests.AzureServiceBus')]:
            register = json.loads(json.dumps(self.register))
            register['inherited_skipped_placeholders'][0][field] = changed
            with self.subTest(field=field), patch.object(maintenance, 'load_register', return_value=register):
                with self.assertRaises(ValueError) as rejected:
                    maintenance.verify_tests(self.root, row)
                self.assertNotIn(changed, str(rejected.exception))
        with self.assertRaises(ValueError):
            maintenance.verify_tests(self.root, dict(row, commit='0' * 40))
        inventory = json.loads((self.root / 'test-inventory.json').read_text())
        (self.root / 'test-inventory.json').write_text(json.dumps(inventory[:-1]))
        with self.assertRaisesRegex(ValueError, 'placeholder inventory mismatch'):
            maintenance.verify_tests(self.root, row)

    def test_placeholder_runtime_requires_sole_known_linked_notexecuted_identity_and_exact_counts(self):
        row = next(row for row in self.register['sources'] if row['product'] == 'extensions')
        mutations = ['class', 'method', 'link', 'missing-result', 'extra-result', 'new-definition',
                     'passed-outcome', 'failed-outcome', 'zero-counts', 'missing-file', 'duplicate-file',
                     'nonoriginal-skip-counter', 'summary', 'missing-entry', 'entry-link', 'execution-link']
        for mutation in mutations:
            with self.subTest(mutation=mutation):
                path = self.write_placeholder_fixture()[0]
                tree = ET.parse(path)
                method = tree.find('.//TestMethod')
                result = tree.find('.//UnitTestResult')
                counters = tree.find('.//Counters')
                if mutation in ('class', 'method'):
                    method.set('className' if mutation == 'class' else 'name', 'private-secret-identity')
                elif mutation == 'link':
                    result.set('testId', 'private-unlinked-id')
                elif mutation == 'missing-result':
                    tree.find('.//Results').remove(result)
                elif mutation == 'extra-result':
                    tree.find('.//Results').append(ET.fromstring(ET.tostring(result)))
                elif mutation == 'new-definition':
                    definition = ET.fromstring(ET.tostring(tree.find('.//UnitTest')))
                    definition.set('id', 'private-new-id')
                    definition.find('TestMethod').set('name', 'private-new-method')
                    tree.find('.//TestDefinitions').append(definition)
                    extra = ET.fromstring(ET.tostring(result)); extra.set('testId', 'private-new-id')
                    tree.find('.//Results').append(extra)
                    counters.set('total', '2'); counters.set('notExecuted', '2')
                elif mutation in ('passed-outcome', 'failed-outcome'):
                    result.set('outcome', 'Passed' if mutation == 'passed-outcome' else 'Failed')
                elif mutation == 'zero-counts':
                    counters.set('total', '0')
                elif mutation == 'nonoriginal-skip-counter':
                    counters.set('notExecuted', '1')
                elif mutation == 'summary':
                    tree.find('.//ResultSummary').set('outcome', 'Failed')
                elif mutation == 'missing-entry':
                    tree.find('.//TestEntries').clear()
                elif mutation == 'entry-link':
                    tree.find('.//TestEntry').set('testId', 'private-unknown-entry')
                elif mutation == 'execution-link':
                    result.set('executionId', 'private-unknown-execution')
                tree.write(path)
                if mutation == 'missing-file':
                    path.unlink()
                duplicate = path.with_name('duplicate-placeholder.trx')
                if mutation == 'duplicate-file':
                    duplicate.write_bytes(path.read_bytes())
                context = {}
                with self.assertRaises(ValueError):
                    maintenance.verify_tests(self.root, row, context)
                if duplicate.exists():
                    duplicate.unlink()
                self.assertNotIn('private-secret-identity', json.dumps(context))
                self.assertNotIn('private-new', json.dumps(context))
                self.assertEqual(context['positive_summary_count'], 1)  # Later runnable cell still collected.

    def test_runnable_cells_reject_unexpected_skips_failures_zero_and_duplicate_test_identities(self):
        for mutation in ['skip', 'failure', 'zero', 'unexpected-outcome', 'duplicate-id', 'missing-method']:
            with self.subTest(mutation=mutation):
                path = self.write_trx_fixture()
                tree = ET.parse(path)
                counters = tree.find('.//Counters')
                result = tree.find('.//UnitTestResult')
                if mutation in ('skip', 'failure'):
                    result.set('outcome', 'NotExecuted' if mutation == 'skip' else 'Failed')
                    counters.set('passed', '2')
                    counters.set('notExecuted' if mutation == 'skip' else 'failed', '1')
                elif mutation == 'zero':
                    for name in ('total', 'executed', 'passed'):
                        counters.set(name, '0')
                    tree.find('.//Results').clear()
                    tree.find('.//TestDefinitions').clear()
                elif mutation == 'unexpected-outcome':
                    result.set('outcome', 'private-secret-outcome')
                elif mutation == 'duplicate-id':
                    tree.findall('.//UnitTestResult')[1].set('testId', result.get('testId'))
                else:
                    definition = tree.find('.//UnitTest')
                    definition.remove(definition.find('TestMethod'))
                tree.write(path)
                context = {}
                with self.assertRaises(ValueError):
                    maintenance.verify_tests(self.root, self.row, context)
                self.assertNotIn('private-secret-outcome', json.dumps(context))

    def test_flat_dynamic_theory_executions_share_definition_with_complete_unique_execution_links(self):
        # Original Quartz 3.8 emits four MemberData executions for one definition.
        self.write_trx_fixture(outcomes=['Passed'] * 7,
                               definition_ids=['dynamic'] * 4 + ['static-1', 'static-2', 'static-3'])
        context = {}
        result = maintenance.verify_tests(self.root, self.row, context)
        self.assertEqual(result['executions'][0]['counters']['passed'], 7)
        self.assertEqual(context['positive_summary_count'], 1)
        self.assertEqual(context['cells'][0]['structure'], {
            'definition_count': 4, 'result_count': 7, 'entry_count': 7, 'unique_execution_count': 7,
            'repeated_definition_result_count': 3, 'nested_result_count': 0, 'unsupported_structure_count': 0,
            'linkage_valid': True, 'summary_completed': True})

    def test_flat_execution_linkage_rejects_conflicting_or_missing_links_and_unsupported_shapes(self):
        mutations = ['duplicate-execution', 'empty-execution', 'unknown-definition', 'duplicate-definition',
                     'missing-anchor', 'duplicate-anchor', 'orphan-anchor', 'cross-linked-anchor',
                     'missing-entry', 'duplicate-entry', 'orphan-entry', 'cross-linked-entry',
                     'missing-result', 'nested-result', 'empty-aggregate', 'inner-results',
                     'duplicate-container', 'failed-leaf', 'skipped-leaf', 'failed-summary', 'false-counts']
        for mutation in mutations:
            with self.subTest(mutation=mutation):
                path = self.write_trx_fixture(outcomes=['Passed'] * 7,
                    definition_ids=['dynamic'] * 4 + ['static-1', 'static-2', 'static-3'])
                tree = ET.parse(path)
                definitions = tree.find('TestDefinitions')
                results = tree.find('Results')
                entries = tree.find('TestEntries')
                if mutation == 'duplicate-execution':
                    results[1].set('executionId', results[0].get('executionId'))
                    entries[1].set('executionId', entries[0].get('executionId'))
                elif mutation == 'empty-execution':
                    results[1].set('executionId', '')
                    entries[1].set('executionId', '')
                elif mutation == 'unknown-definition':
                    results[1].set('testId', 'private-secret-identity')
                    entries[1].set('testId', 'private-secret-identity')
                elif mutation == 'duplicate-definition':
                    definitions.append(ET.fromstring(ET.tostring(definitions[0])))
                elif mutation == 'missing-anchor':
                    definitions[0].remove(definitions[0].find('Execution'))
                elif mutation == 'duplicate-anchor':
                    definitions[0].append(ET.fromstring(ET.tostring(definitions[0].find('Execution'))))
                elif mutation in ('orphan-anchor', 'cross-linked-anchor'):
                    definitions[0].find('Execution').set('id',
                        'private-secret-identity' if mutation == 'orphan-anchor' else 'execution-4')
                elif mutation == 'missing-entry':
                    entries.remove(entries[1])
                elif mutation == 'duplicate-entry':
                    entries.append(ET.fromstring(ET.tostring(entries[1])))
                elif mutation == 'orphan-entry':
                    ET.SubElement(entries, 'TestEntry', testId='dynamic', executionId='private-secret-identity')
                elif mutation == 'cross-linked-entry':
                    entries[1].set('testId', 'static-1')
                elif mutation == 'missing-result':
                    results.remove(results[1])
                elif mutation == 'nested-result':
                    ET.SubElement(results[0], 'InnerResults').append(results[1])
                    results.remove(results[1])
                elif mutation == 'empty-aggregate':
                    ET.SubElement(results, 'TestResultAggregation', testId='private-secret-identity', outcome='Passed')
                elif mutation == 'inner-results':
                    ET.SubElement(results[0], 'InnerResults')
                elif mutation == 'duplicate-container':
                    ET.SubElement(tree.getroot(), 'TestEntries')
                elif mutation in ('failed-leaf', 'skipped-leaf'):
                    results[1].set('outcome', 'Failed' if mutation == 'failed-leaf' else 'NotExecuted')
                elif mutation == 'failed-summary':
                    tree.find('ResultSummary').set('outcome', 'Failed')
                elif mutation == 'false-counts':
                    tree.find('.//Counters').set('passed', '6')
                tree.write(path)
                context = {}
                with self.assertRaises(ValueError):
                    maintenance.verify_tests(self.root, self.row, context)
                structure = context['cells'][0]['structure']
                if mutation not in ('failed-leaf', 'skipped-leaf', 'failed-summary', 'false-counts'):
                    self.assertFalse(structure['linkage_valid'])
                    self.assertIn('test-execution-linkage-invalid', context['failure_reasons'])
                self.assertNotIn('private-secret-identity', json.dumps(context))
                self.assertNotIn(str(self.root), json.dumps(context))

    def test_failed_first_cell_still_collects_later_known_counters_and_unknown_names_stay_private(self):
        row = next(row for row in self.register['sources'] if row['product'] == 'extensions')
        paths = self.write_placeholder_fixture()
        tree = ET.parse(paths[0]); tree.find('.//Counters').set('passed', '1'); tree.write(paths[0])
        self.write_trx_fixture('extensions', '03-unknown-private-host', '/private/secret-token/Unknown.dll',
                               test_class='private-secret-class', method='private-secret-method')
        context = {}
        with self.assertRaises(ValueError):
            maintenance.verify_tests(self.root, row, context)
        self.assertEqual(len(context['cells']), 3)
        self.assertEqual(context['positive_summary_count'], 1)
        self.assertEqual(context['summary_count'], 4)
        self.assertEqual(context['unknown_path_count'], 1)
        self.assertEqual(len(context['inherited_skipped_placeholders']), 1)
        self.assertEqual(context['cells'][0]['counters']['passed'], 1)
        for private in ['secret-token', 'Unknown.dll', 'private-secret', '03-unknown-private-host', str(self.root)]:
            self.assertNotIn(private, json.dumps(context))

    def test_all_standard_failure_counters_and_summary_schema_fail_closed_for_both_categories(self):
        row = next(row for row in self.register['sources'] if row['product'] == 'extensions')
        names = [name for name in maintenance.TRX_COUNTERS if name not in ('total', 'executed', 'passed')]
        for category in ['placeholder', 'runnable']:
            for field in [*names, 'missing-counter', 'unknown-counter', 'malformed-counter', 'failed-summary']:
                with self.subTest(category=category, field=field):
                    paths = self.write_placeholder_fixture()
                    path = paths[0] if category == 'placeholder' else paths[-1]
                    tree = ET.parse(path); counters = tree.find('.//Counters')
                    if field == 'missing-counter':
                        del counters.attrib['error']
                    elif field == 'unknown-counter':
                        counters.set('private-secret-counter', '1')
                    elif field == 'malformed-counter':
                        counters.set('timeout', 'private-secret-value')
                    elif field == 'failed-summary':
                        tree.find('.//ResultSummary').set('outcome', 'Failed')
                    else:
                        counters.set(field, '1')
                    tree.write(path)
                    context = {}
                    with self.assertRaises(ValueError):
                        maintenance.verify_tests(self.root, row, context)
                    self.assertNotIn('private-secret', json.dumps(context))

    def test_empty_documents_require_real_bodyless_nonreference_metadata(self):
        prefix = f"https://raw.githubusercontent.com/{self.row['source_repository']}/{self.row['commit']}/"
        details = {'documents': [], 'source_link': {'documents': {'/_/*': prefix + '*'}},
                   'executable_method_bodies': 0, 'nonabstract_methods_without_body': 0,
                   'native_or_external_methods': 0, 'nonmodule_types': 2, 'reference_assembly': False}
        self.assertEqual(maintenance.verify_documents(details, maintenance.ROOT, self.row), [])
        for field in ['executable_method_bodies', 'nonabstract_methods_without_body', 'native_or_external_methods']:
            for value in [1, -1, None, False, '0']:
                with self.subTest(field=field, value=value), self.assertRaises(ValueError):
                    maintenance.verify_documents(details | {field: value}, maintenance.ROOT, self.row)
        for field, value in [('nonmodule_types', 0), ('nonmodule_types', False), ('nonmodule_types', -1),
                             ('reference_assembly', True), ('reference_assembly', None), ('reference_assembly', 0)]:
            with self.subTest(field=field, value=value), self.assertRaises(ValueError):
                maintenance.verify_documents(details | {field: value}, maintenance.ROOT, self.row)
        for field in ['executable_method_bodies', 'nonabstract_methods_without_body', 'native_or_external_methods',
                      'nonmodule_types', 'reference_assembly']:
            missing = dict(details); missing.pop(field)
            with self.subTest(missing=field), self.assertRaises(ValueError):
                maintenance.verify_documents(missing, maintenance.ROOT, self.row)
        for maps in [{}, {'/_/*': 'https://example.invalid/*'}]:
            with self.subTest(maps=maps), self.assertRaises(ValueError):
                maintenance.verify_documents(details | {'source_link': {'documents': maps}}, maintenance.ROOT, self.row)

    def test_inventory_records_explicit_source_build_output_and_assembly_identity(self):
        source = self.root / 'source'; source.mkdir()
        (source / 'NuGet.Config').write_text('<configuration />')
        (source / 'Elsa.Studio.sln').write_text('Project("{fixture}") = "Fixture", "Fixture.csproj", "{fixture}"\n')
        values = {'IsPackable': 'true', 'IsTestProject': 'true', 'AssemblyName': 'Evaluated.Assembly',
                  'PackageId': 'Elsa.Studio.Fixture', 'PackageVersion': '3.8.4-proof.42.1',
                  'TargetFrameworks': 'net8.0;net9.0', 'TargetFramework': '', 'IncludeSymbols': 'true',
                  'IncludeBuildOutput': 'true'}
        with patch.object(maintenance, 'run', side_effect=lambda command, *_args, **_kwargs: json.dumps(
                {'Items': {'SatelliteDllsProjectOutputGroupOutput': []}} if '-target:SatelliteDllsProjectOutputGroup' in command
                else {'Properties': values})) as runner:
            for include in ['true', 'false']:
                values['IncludeBuildOutput'] = include
                inventory = maintenance.evaluate_inventory(source, self.row, '3.8.4-proof.42.1', self.root)
                self.assertEqual(inventory[0]['assembly_name'], 'Evaluated.Assembly')
                self.assertEqual(inventory[0]['include_build_output'], include == 'true')
                self.assertEqual(inventory[0]['frameworks'], ['net8.0', 'net9.0'])
            self.assertIn('IncludeBuildOutput', runner.call_args.args[0][-1])
            values['IncludeBuildOutput'] = ''
            with self.assertRaisesRegex(ValueError, 'not evaluated'):
                maintenance.evaluate_inventory(source, self.row, '3.8.4-proof.42.1', self.root)

    def test_manifest_failure_focus_clears_previous_package_framework_and_reason_is_closed(self):
        artifacts = self.write_package_fixture()
        policy = self.framework_reference_policy_fixture()
        policy['framework_properties'] = {'net8.0': {'manifest_required': True, 'manifest_path': 'elsa-package.json'}}
        with zipfile.ZipFile(artifacts / 'fixture.nupkg', 'a') as archive:
            archive.writestr('elsa-package.json', json.dumps({'package': {'id': policy['id'], 'version': '1.0.0'}}))
        context = {'package': 'Previous.Package', 'framework': 'net9.0'}
        with patch.object(maintenance, 'verify_sdk_dependencies'), patch.object(maintenance, 'verify_sdk_assets'):
            with self.assertRaisesRegex(ValueError, 'Generated package manifest identity/version mismatch') as failure:
                maintenance.verify_artifacts(artifacts, [policy], self.row | {'product': 'extensions'},
                    '3.8.4-proof.42.1', self.root, Path('unused-inspector'), self.root, context=context)
        self.assertEqual({'package': policy['id']}, context)
        self.assertEqual('package-manifest-identity-version-mismatch', maintenance.verification_reason(str(failure.exception)))

    def write_package_fixture(self, dependency='3.8.4', packed_version='3.8.4-proof.42.1', frameworks=(), groups=None,
                              references='', symbol_references=None):
        artifacts = self.root / 'artifacts'; artifacts.mkdir(exist_ok=True)
        groups = groups if groups is not None else f'<group targetFramework="net8.0"><dependency id="Elsa.Api.Client" version="{dependency}" /></group>'
        nuspec = f'''<package><metadata><id>Elsa.Studio.Fixture</id><version>{packed_version}</version>
          <repository type="git" url="https://github.com/{self.row['source_repository']}" commit="{self.row['commit']}" />
          <dependencies>{groups}</dependencies>
          </metadata></package>'''
        path = artifacts / 'fixture.nupkg'
        with zipfile.ZipFile(path, 'w') as zipped:
            zipped.writestr('fixture.nuspec', nuspec.replace('</metadata>', references + '</metadata>'))
            zipped.writestr('build/Fixture.targets', '<Project/>')
            for framework in frameworks:
                zipped.writestr(f'lib/{framework}/Elsa.Studio.Fixture.dll', f'assembly-{framework}')
        if frameworks or symbol_references is not None:
            with zipfile.ZipFile(path.with_suffix('.snupkg'), 'w') as zipped:
                selected = references if symbol_references is None else symbol_references
                zipped.writestr('fixture.nuspec', nuspec.replace('</metadata>', selected + '</metadata>'))
                for framework in frameworks:
                    zipped.writestr(f'lib/{framework}/Elsa.Studio.Fixture.pdb', f'symbols-{framework}')
        return artifacts

    def framework_reference_policy_fixture(self, **overrides):
        return {'id': 'Elsa.Studio.Fixture', 'assembly_name': 'Elsa.Studio.Fixture', 'frameworks': ['net8.0'],
                'include_build_output': False, 'symbols': True, 'satellites': [],
                **self.sdk_dependency_fixture(), **overrides}

    def test_sdk_framework_reference_groups_reject_archive_tampering_in_either_or_both_packages(self):
        baseline = ('<frameworkReferences><group targetFramework="net8.0">'
                    '<frameworkReference name="Microsoft.AspNetCore.App" /></group></frameworkReferences>')
        expected = [{'framework': 'net8.0', 'references': ['Microsoft.AspNetCore.App']}]
        policy = self.framework_reference_policy_fixture(expected_framework_reference_groups=expected,
                                                        expected_symbol_framework_reference_groups=expected)
        for target in ['main', 'symbols', 'both']:
            for mutation in ['matching', 'removed', 'changed', 'added-reference', 'changed-framework', 'added-group']:
                altered = baseline
                if mutation == 'removed':
                    altered = ''
                elif mutation == 'changed':
                    altered = baseline.replace('Microsoft.AspNetCore.App', 'Unexpected.Framework')
                elif mutation == 'added-reference':
                    altered = baseline.replace('</group>', '<frameworkReference name="Unexpected.Framework" /></group>')
                elif mutation == 'changed-framework':
                    altered = baseline.replace('net8.0', 'net9.0')
                elif mutation == 'added-group':
                    altered = baseline.replace('</frameworkReferences>', '<group targetFramework="net9.0" /></frameworkReferences>')
                artifacts = self.write_package_fixture(references=altered if target in ['main', 'both'] else baseline,
                    symbol_references=altered if target in ['symbols', 'both'] else baseline)
                with self.subTest(target=target, mutation=mutation):
                    if mutation == 'matching':
                        maintenance.verify_artifacts(artifacts, [policy], self.row, '3.8.4-proof.42.1',
                                                     maintenance.ROOT, Path('/unused'), self.root)
                    else:
                        with self.assertRaisesRegex(ValueError, '^SDK framework reference groups disagree with package$') as error:
                            maintenance.verify_artifacts(artifacts, [policy], self.row, '3.8.4-proof.42.1',
                                                         maintenance.ROOT, Path('/unused'), self.root)
                        self.assertEqual(maintenance.verification_reason(str(error.exception)),
                                         'sdk-framework-reference-groups-mismatch')

    def test_sdk_framework_reference_expectations_are_required_lists_in_both_packages(self):
        artifacts = self.write_package_fixture(symbol_references='')
        policy = self.framework_reference_policy_fixture()
        for key in ['expected_framework_reference_groups', 'expected_symbol_framework_reference_groups']:
            for value in ['missing', None, {}]:
                selected = dict(policy)
                if value == 'missing':
                    selected.pop(key)
                else:
                    selected[key] = value
                with self.subTest(key=key, value=value):
                    with self.assertRaisesRegex(ValueError, '^SDK framework reference metadata missing$') as error:
                        maintenance.verify_artifacts(artifacts, [selected], self.row, '3.8.4-proof.42.1',
                                                     maintenance.ROOT, Path('/unused'), self.root)
                    self.assertEqual(maintenance.verification_reason(str(error.exception)),
                                     'sdk-framework-reference-metadata-missing')

    def test_empty_framework_reference_expectations_preserve_empty_groups(self):
        policy = self.framework_reference_policy_fixture()
        for references, expected in [('', []), ('<frameworkReferences />', []),
                ('<frameworkReferences><group targetFramework="net8.0" /></frameworkReferences>',
                 [{'framework': 'net8.0', 'references': []}])]:
            selected = dict(policy, expected_framework_reference_groups=expected,
                            expected_symbol_framework_reference_groups=expected)
            artifacts = self.write_package_fixture(references=references, symbol_references=references)
            with self.subTest(references=references):
                maintenance.verify_artifacts(artifacts, [selected], self.row, '3.8.4-proof.42.1',
                                             maintenance.ROOT, Path('/unused'), self.root)
                if expected:
                    for key in ['expected_framework_reference_groups', 'expected_symbol_framework_reference_groups']:
                        with self.subTest(key=key), self.assertRaisesRegex(ValueError, 'SDK framework reference groups'):
                            maintenance.verify_artifacts(artifacts, [dict(selected, **{key: []})], self.row,
                                '3.8.4-proof.42.1', maintenance.ROOT, Path('/unused'), self.root)

    def test_matching_framework_references_cannot_target_unevaluated_frameworks(self):
        policy = self.framework_reference_policy_fixture()
        for target in ['main', 'symbols', 'both']:
            selected = dict(policy)
            references = {}
            for package, key in [('main', 'expected_framework_reference_groups'),
                                 ('symbols', 'expected_symbol_framework_reference_groups')]:
                framework = 'net9.0' if target in [package, 'both'] else 'net8.0'
                selected[key] = [{'framework': framework, 'references': []}]
                references[package] = f'<frameworkReferences><group targetFramework="{framework}" /></frameworkReferences>'
            artifacts = self.write_package_fixture(references=references['main'], symbol_references=references['symbols'])
            with self.subTest(target=target):
                with self.assertRaisesRegex(ValueError, '^SDK framework reference framework unsupported$') as error:
                    maintenance.verify_artifacts(artifacts, [selected], self.row, '3.8.4-proof.42.1',
                                                 maintenance.ROOT, Path('/unused'), self.root)
                self.assertEqual(maintenance.verification_reason(str(error.exception)),
                                 'sdk-framework-reference-framework-unsupported')

    def test_complete_sdk_dependency_groups_are_required_even_when_archives_agree(self):
        expected = [{'framework': 'net8.0', 'dependencies': [
            {'id': 'Contoso.Serializer', 'version': '[1.2.3, 2.0.0)', 'include': '', 'exclude': 'Build,Analyzers'},
            {'id': 'Elsa.Api.Client', 'version': '3.8.4', 'include': '', 'exclude': ''}]},
            {'framework': 'net9.0', 'dependencies': []}]
        baseline = ('<group targetFramework="net8.0"><dependency id="Elsa.Api.Client" version="3.8.4" />'
                    '<dependency id="Contoso.Serializer" version="[1.2.3, 2.0.0)" exclude="Build,Analyzers" /></group>'
                    '<group targetFramework="net9.0" />')
        policy = {'id': 'Elsa.Studio.Fixture', 'assembly_name': 'Elsa.Studio.Fixture',
                  'frameworks': ['net8.0', 'net9.0'], 'include_build_output': False, 'symbols': True, 'satellites': [],
                  'expected_dependency_groups': expected, 'sdk_nuspec_sha256': 'a' * 64,
                  'expected_symbol_dependency_groups': expected, 'sdk_symbol_nuspec_sha256': 'b' * 64,
                  'expected_framework_reference_groups': [], 'expected_symbol_framework_reference_groups': []}
        for mutation in ['baseline', 'missing-Elsa', 'missing-framework', 'third-party-version', 'asset-metadata',
                         'missing-expected', 'missing-symbol-expected', 'changed-symbol-expected']:
            groups = baseline
            if mutation == 'missing-Elsa':
                groups = groups.replace('<dependency id="Elsa.Api.Client" version="3.8.4" />', '')
            elif mutation == 'missing-framework':
                groups = groups.replace('<group targetFramework="net9.0" />', '')
            elif mutation == 'third-party-version':
                groups = groups.replace('[1.2.3, 2.0.0)', '9.9.9')
            elif mutation == 'asset-metadata':
                groups = groups.replace('Build,Analyzers', 'Build')
            selected = dict(policy)
            if mutation == 'missing-expected':
                selected.pop('expected_dependency_groups')
            elif mutation == 'missing-symbol-expected':
                selected.pop('expected_symbol_dependency_groups')
            elif mutation == 'changed-symbol-expected':
                selected['expected_symbol_dependency_groups'] = []
            artifacts = self.write_package_fixture(groups=groups)
            (artifacts / 'fixture.snupkg').write_bytes((artifacts / 'fixture.nupkg').read_bytes())
            with self.subTest(mutation=mutation):
                if mutation == 'baseline':
                    maintenance.verify_artifacts(artifacts, [selected], self.row, '3.8.4-proof.42.1',
                                                 maintenance.ROOT, Path('/unused'), self.root)
                else:
                    with self.assertRaisesRegex(ValueError, 'SDK dependency'):
                        maintenance.verify_artifacts(artifacts, [selected], self.row, '3.8.4-proof.42.1',
                                                     maintenance.ROOT, Path('/unused'), self.root)

    def sdk_dependency_fixture(self, dependency='3.8.4'):
        groups = [{'framework': 'net8.0', 'dependencies': [
            {'id': 'Elsa.Api.Client', 'version': dependency, 'include': '', 'exclude': ''}]}]
        return {'expected_dependency_groups': groups, 'sdk_nuspec_sha256': 'a' * 64,
                'expected_symbol_dependency_groups': groups, 'sdk_symbol_nuspec_sha256': 'b' * 64,
                'expected_framework_reference_groups': [], 'expected_symbol_framework_reference_groups': []}

    def test_package_identity_dependencies_and_complete_inventory_are_verified(self):
        policy = [{'id': 'Elsa.Studio.Fixture', 'assembly_name': 'Elsa.Studio.Fixture',
                   'frameworks': ['net8.0'], 'include_build_output': False, 'symbols': False, 'satellites': [],
                   **self.sdk_dependency_fixture()}]
        version = '3.8.4-proof.42.1'
        artifacts = self.write_package_fixture()
        receipt = maintenance.verify_artifacts(artifacts, policy, self.row, version, maintenance.ROOT, Path('/unused'), self.root)
        self.assertEqual(receipt[0]['dependencies'][0]['dependencies'][0]['version'], '3.8.4')
        self.assertFalse(receipt[0]['include_build_output'])
        with self.assertRaisesRegex(ValueError, 'assembly payload'):
            maintenance.verify_artifacts(artifacts, [dict(policy[0], include_build_output=True)], self.row,
                                         version, maintenance.ROOT, Path('/unused'), self.root)
        self.write_package_fixture(dependency='3.10.0')
        with self.assertRaises(ValueError):
            maintenance.verify_artifacts(artifacts, policy, self.row, version, maintenance.ROOT, Path('/unused'), self.root)
        self.write_package_fixture(packed_version='3.9.0')
        with self.assertRaises(ValueError):
            maintenance.verify_artifacts(artifacts, policy, self.row, version, maintenance.ROOT, Path('/unused'), self.root)
        (artifacts / 'fixture.nupkg').unlink()
        with self.assertRaises(ValueError):
            maintenance.verify_artifacts(artifacts, policy, self.row, version, maintenance.ROOT, Path('/unused'), self.root)

    def test_every_evaluated_framework_requires_its_named_assembly_and_verified_symbols(self):
        frameworks = ['net8.0', 'net9.0']
        artifacts = self.write_package_fixture(frameworks=frameworks)
        version = '3.8.4-proof.42.1'
        policy = [{'id': 'Elsa.Studio.Fixture', 'assembly_name': 'Elsa.Studio.Fixture',
                   'frameworks': frameworks, 'include_build_output': True, 'symbols': True, 'satellites': [],
                   **self.sdk_dependency_fixture()}]
        original = subprocess.run(['git', 'show', self.row['commit'] + ':Directory.Build.props'],
                                  cwd=maintenance.ROOT, check=True, capture_output=True).stdout
        prefix = f"https://raw.githubusercontent.com/{self.row['source_repository']}/{self.row['commit']}/"
        inspected = []
        real_run = maintenance.run
        def inspect(command, *_args, **_kwargs):
            if command[0] == 'git':
                return real_run(command, *_args, **_kwargs)
            self.assertEqual(command[-1], '--inspect-symbols')
            assembly, symbols = Path(command[2]).read_bytes(), Path(command[3]).read_bytes()
            framework = assembly.decode().removeprefix('assembly-')
            self.assertEqual(symbols, f'symbols-{framework}'.encode())
            inspected.append(framework)
            return json.dumps({'details': {'assembly_name': 'Elsa.Studio.Fixture', 'assembly_version': '3.8.0.0',
                'informational_version': version + '+' + self.row['commit'],
                'source_link': {'documents': {'/_/*': prefix + '*'}}, 'documents': [
                    {'path': '/_/Directory.Build.props', 'algorithm': 'sha256',
                     'checksum': hashlib.sha256(original).hexdigest(), 'embedded_checksum': None}]},
                'symbol': {'key': 'fixture.pdb/key/fixture.pdb', 'pdb_name': 'fixture.pdb', 'guid': 'fixture-guid',
                    'stamp': 1, 'checksum_algorithm': 'SHA256', 'declared_checksum': 'fixture-checksum',
                    'normalized_checksum': 'fixture-checksum', 'pdb_sha256': hashlib.sha256(symbols).hexdigest(),
                    'pdb_size': len(symbols)}})
        with patch.object(maintenance, 'run', side_effect=inspect):
            receipt = maintenance.verify_artifacts(artifacts, policy, self.row, version, maintenance.ROOT, Path('/inspector'), self.root)
        self.assertEqual(inspected, frameworks)
        self.assertEqual(receipt[0]['frameworks'], frameworks)
        self.assertEqual([symbol['assembly'] for symbol in receipt[0]['symbols']],
                         ['lib/net8.0/Elsa.Studio.Fixture.dll', 'lib/net9.0/Elsa.Studio.Fixture.dll'])
        self.assertTrue(all(symbol['documents'][0]['source'] == 'original-git' for symbol in receipt[0]['symbols']))
        # SDK-evaluated resource satellites have exact emitted bytes but no primary-style PDB.
        source = self.root / 'source'; source.mkdir()
        (source / 'NuGet.Config').write_text('<configuration />')
        subprocess.run(['git', 'init', '-q'], cwd=source, check=True)
        objects = Path(maintenance.git(maintenance.ROOT, 'rev-parse', '--path-format=absolute', '--git-common-dir')) / 'objects'
        (source / '.git/objects/info/alternates').write_text(str(objects) + '\n')
        emitted = source / 'bin/Release/net8.0/fr/Elsa.Studio.Fixture.resources.dll'
        emitted.parent.mkdir(parents=True); emitted.write_bytes(b'original French satellite')
        item = {'TargetPath': 'fr\\Elsa.Studio.Fixture.resources.dll', 'Culture': 'fr', 'FinalOutputPath': str(emitted)}
        with patch.object(maintenance, 'run', return_value=json.dumps({'Items': {'SatelliteDllsProjectOutputGroupOutput': [item]}})) as evaluation:
            satellites = maintenance.evaluate_satellites(source, 'Fixture.csproj', 'net8.0', 'Elsa.Studio.Fixture', version)
        self.assertIn('-p:TargetFramework=net8.0', evaluation.call_args.args[0])
        self.assertEqual(satellites[0]['culture'], 'fr')
        self.assertEqual(satellites[0]['target_path'], 'fr/Elsa.Studio.Fixture.resources.dll')
        self.assertEqual(satellites[0]['final_output_path'], 'bin/Release/net8.0/fr/Elsa.Studio.Fixture.resources.dll')
        self.assertNotIn(str(source), json.dumps(satellites))
        policy[0]['satellites'] = satellites
        with zipfile.ZipFile(artifacts / 'fixture.nupkg', 'a') as zipped:
            zipped.writestr(satellites[0]['package_path'], emitted.read_bytes())
        inspected.clear()
        with patch.object(maintenance, 'run', side_effect=inspect):
            receipt = maintenance.verify_artifacts(artifacts, policy, self.row, version, source, Path('/inspector'), self.root)
        self.assertEqual(inspected, frameworks)
        self.assertEqual(receipt[0]['satellites'][0]['sha256'], hashlib.sha256(b'original French satellite').hexdigest())
        emitted.unlink()
        with self.assertRaisesRegex(ValueError, 'satellite bytes missing'):
            maintenance.verify_artifacts(artifacts, policy, self.row, version, source, Path('/inspector'), self.root)
        emitted.write_bytes(b'changed satellite')
        with self.assertRaisesRegex(ValueError, 'satellite bytes differ'):
            maintenance.verify_artifacts(artifacts, policy, self.row, version, source, Path('/inspector'), self.root)
        emitted.write_bytes(b'original French satellite')
        with zipfile.ZipFile(artifacts / 'fixture.nupkg', 'a') as zipped:
            zipped.writestr('lib/net8.0/rogue/Elsa.Studio.Fixture.resources.dll', b'unlisted satellite')
        with self.assertRaisesRegex(ValueError, 'assembly payload'):
            maintenance.verify_artifacts(artifacts, policy, self.row, version, source, Path('/inspector'), self.root)
        policy[0]['satellites'] = []
        self.write_package_fixture(frameworks=['net8.0'])
        with self.assertRaisesRegex(ValueError, 'assembly payload'):
            maintenance.verify_artifacts(artifacts, policy, self.row, version, maintenance.ROOT, Path('/inspector'), self.root)
        self.write_package_fixture(frameworks=frameworks)
        # A different DLL at the right TFM cannot replace the evaluated assembly.
        with zipfile.ZipFile(artifacts / 'fixture.nupkg', 'a') as zipped:
            zipped.writestr('lib/net8.0/Unexpected.dll', 'wrong assembly')
        with self.assertRaisesRegex(ValueError, 'assembly payload'):
            maintenance.verify_artifacts(artifacts, policy, self.row, version, maintenance.ROOT, Path('/inspector'), self.root)
        # A present DLL still requires its corresponding original symbol payload.
        self.write_package_fixture(frameworks=frameworks)
        (artifacts / 'fixture.snupkg').unlink()
        with self.assertRaisesRegex(ValueError, 'Symbol package missing'):
            maintenance.verify_artifacts(artifacts, policy, self.row, version, maintenance.ROOT, Path('/inspector'), self.root)

    def test_trx_binds_actual_project_and_framework_rejecting_duplicate_positive_cells(self):
        for product in ['studio', 'extensions']:
            with self.subTest(product=product):
                row = dict(next(row for row in self.register['sources'] if row['product'] == product), commit='0' * 40)
                path = self.write_trx_fixture(product)
                receipt = maintenance.verify_tests(self.root, row)
                self.assertEqual(receipt['executions'][0]['project'], 'Fixture.Tests.csproj')
                self.assertEqual(receipt['executions'][0]['framework'], 'net8.0')
                for private in ['private-runner-host', 'private-secret-host', str(self.root)]:
                    self.assertNotIn(private, json.dumps(receipt))
                duplicate = self.write_trx_fixture(product, 'duplicate')
                with self.assertRaises(ValueError):
                    maintenance.verify_tests(self.root, row)
                duplicate.unlink()
                path.write_text(path.read_text().replace('net8.0', 'net9.0'))
                with self.assertRaises(ValueError):
                    maintenance.verify_tests(self.root, row)

    def test_evidence_upload_does_not_include_private_logs_or_trx(self):
        workflow = (maintenance.ROOT / '.github/workflows/prepare-maintenance-build.yml').read_text()
        upload = workflow.split('name: Retain original artifacts')[1]
        self.assertNotIn('*.log', upload)
        self.assertNotIn('/test-results', upload)
        self.assertNotIn('secrets.', workflow)
        self.assertNotIn('id-token:', workflow)
        self.assertIn('persist-credentials: false', workflow)

    def test_failed_process_retains_only_exit_code_closed_target_and_diagnostic_counts(self):
        private = '/private/runner-host-42/secret-password'
        log = self.root / 'private.log'
        record = {'success': False}
        command = [sys.executable, '-c',
            f'print("error NU1101: {private}"); print("warning CS0168: {private}"); '
            f'print("Target Restore has thrown an exception: {private}"); raise SystemExit(7)']
        with self.assertRaises(ValueError):
            maintenance.run_build_command(command, self.root, log, record)
        self.assertEqual(record['process'], {'status': 'exited', 'exit_code': 7})
        self.assertEqual(record['diagnostics']['codes'], [
            {'severity': 'error', 'code': 'NU1101', 'count': 1},
            {'severity': 'warning', 'code': 'CS0168', 'count': 1}])
        self.assertEqual(record['diagnostics']['nuke_failed_targets'], ['Restore'])
        self.assertFalse(record['success'])
        self.assertNotIn(private, json.dumps(record))
        self.assertNotIn('runner-host', json.dumps(record))
        self.assertNotIn('secret-password', json.dumps(record))
        self.assertNotIn(str(self.root), json.dumps(record))
        self.assertIn(private, log.read_text())

    def test_diagnostic_code_counts_are_bounded_and_unknown_messages_are_not_retained(self):
        log = self.root / 'private.log'
        log.write_text('[]\n' + ''.join(f'error CS{code:04}: /private/secret-host\n' for code in range(40)) +
                       'Target PrivateSecret has thrown an exception\nsecret=do-not-retain\n')
        result = maintenance.closed_diagnostics(log)
        self.assertEqual(len(result['codes']), 32)
        self.assertEqual(result['unretained_code_occurrences'], 8)
        self.assertEqual(result['nuke_failed_targets'], [])
        self.assertNotIn('private', json.dumps(result).lower())
        self.assertNotIn('secret', json.dumps(result).lower())

    def test_optional_process_outcome_preserves_shared_runner_contract_and_timeout_cleanup(self):
        outcome = {}
        result = maintenance.run([sys.executable, '-c', 'print("safe")'], self.root, outcome=outcome)
        self.assertEqual(result, 'safe\n')
        self.assertEqual(outcome, {'status': 'exited', 'exit_code': 0})
        self.assertEqual(maintenance.run([sys.executable, '-c', 'print("unchanged")'], self.root), 'unchanged\n')
        with self.assertRaises(ValueError):
            maintenance.run([sys.executable, '-c', 'raise SystemExit(4)'], self.root)
        with self.assertRaises(subprocess.TimeoutExpired):
            maintenance.run([sys.executable, '-c', 'import time; time.sleep(1)'], self.root, timeout=0.02, outcome=outcome)
        self.assertEqual(outcome['status'], 'timed-out')
        self.assertIsInstance(outcome['exit_code'], int)
        with self.assertRaises(FileNotFoundError):
            maintenance.run([str(self.root / 'missing-private-executable')], self.root, outcome=outcome)
        self.assertEqual(outcome, {'status': 'start-failed', 'exit_code': None})

    def test_verification_receipt_maps_only_known_static_reasons_and_hides_private_exceptions(self):
        for index, (message, expected) in enumerate([
                ('Packed repository provenance mismatch', 'package-repository-mismatch'),
                ('Tracked source checksum mismatch', 'source-checksum-mismatch'),
                ('Unexpected evaluated version', 'evaluated-version-mismatch'),
                ('Unexpected evaluated version: private-secret /private/runner-host', 'evaluated-version-mismatch'),
                ('Unexpected Elsa dependency: private-secret /private/runner-host', 'elsa-dependency-mismatch'),
                ('Unexpected Elsa dependency suffix: private-secret', 'unknown-check-failure'),
                ('Command failed: Unexpected Elsa dependency: private-secret', 'unknown-check-failure'),
                ('Generated package manifest identity/version mismatch: private-package-id', 'package-manifest-identity-version-mismatch'),
                ('Generated package manifest identity/version mismatch suffix: private-secret', 'unknown-check-failure'),
                ('secret-password /private/runner-host-42', 'unknown-check-failure')]):
            output = self.root / f'proof-{index}'
            with patch.object(maintenance, 'verify_source', side_effect=ValueError(message)):
                with self.assertRaises(ValueError):
                    maintenance.prepare(maintenance.ROOT, self.row, '3.8.4-proof.42.1', output)
            receipt = json.loads((output / 'receipt.json').read_text())
            self.assertEqual(receipt['error']['reason'], expected)
            self.assertNotIn(message, json.dumps(receipt))
            self.assertNotIn('/private/runner-host', json.dumps(receipt))


    def test_core_package_and_assembly_metadata_bind_preserved_recipe_and_exact_selected_commit(self):
        for candidate in maintenance.load_candidates()['candidates']:
            with self.subTest(product=candidate['product'], line=candidate['line']):
                original = next(row for row in self.register['sources'] if row['commit'] == candidate['original_commit'])
                version = original['dependency_version'] + '-proof.42.1'
                self.row = maintenance.selection(self.register, candidate['product'], candidate['line'],
                                                 candidate['commit'], version, 'core')
                def packages():
                    return self.write_package_fixture(dependency=self.row['dependency_version'],
                                                      packed_version=version, frameworks=['net8.0'])
                artifacts = packages()
                policy = [{'id': 'Elsa.Studio.Fixture', 'assembly_name': 'Elsa.Studio.Fixture', 'frameworks': ['net8.0'],
                           'include_build_output': True, 'symbols': True, 'satellites': [],
                           'expected_sdk_assets': [{'path': 'build/Fixture.targets', 'source_path': 'Fixture.targets',
                               'sha256': hashlib.sha256(b'<Project/>').hexdigest()}], 'framework_properties': {
                               'net8.0': {'manifest_required': False, 'manifest_path': ''}},
                           **self.sdk_dependency_fixture(self.row['dependency_version'])}]
                prefix = f"https://raw.githubusercontent.com/{self.row['source_repository']}/{self.row['commit']}/"
                informational_prefix = version if candidate['product'] == 'studio' else '1.0.0'
                details = {'assembly_name': 'Elsa.Studio.Fixture', 'assembly_version': '1.0.0.0',
                    'informational_version': informational_prefix + '+' + self.row['commit'],
                    'source_link': {'documents': {'/_/*': prefix + '*'}}, 'documents': [],
                    'executable_method_bodies': 0, 'nonabstract_methods_without_body': 0,
                    'native_or_external_methods': 0, 'nonmodule_types': 1, 'reference_assembly': False}
                inspection = {'details': details, 'symbol': dict.fromkeys(('key', 'pdb_name', 'guid', 'stamp',
                    'checksum_algorithm', 'declared_checksum', 'normalized_checksum', 'pdb_sha256', 'pdb_size'), 'fixture')}
                with patch.object(maintenance, 'run', side_effect=lambda *_args, **_kwargs: json.dumps(inspection)):
                    receipt = maintenance.verify_artifacts(artifacts, policy, self.row, version, maintenance.ROOT,
                                                          Path('/inspector'), self.root)
                    self.assertEqual(receipt[0]['repository']['commit'], self.row['commit'])
                    self.assertEqual(receipt[0]['version'], version)
                    self.assertEqual(receipt[0]['symbols'][0]['informational_version'],
                                     informational_prefix + '+' + self.row['commit'])
                    wrong_prefixes = ['unexpected', '1.0.0' if candidate['product'] == 'studio' else version]
                    for invalid in [informational_prefix + '+' + 'a' * 40,
                                    *[value + '+' + self.row['commit'] for value in wrong_prefixes],
                                    'unexpected+' + informational_prefix + '+' + self.row['commit']]:
                        with self.subTest(informational_version=invalid), \
                                self.assertRaisesRegex(ValueError, 'Core assembly commit mismatch'):
                            details['informational_version'] = invalid
                            maintenance.verify_artifacts(artifacts, policy, self.row, version, maintenance.ROOT,
                                                         Path('/inspector'), self.root)
                    details['informational_version'] = informational_prefix + '+' + self.row['commit']
                    for symbols in (False, True):
                        for field in ('commit', 'version'):
                            packages()
                            path = artifacts / ('fixture.snupkg' if symbols else 'fixture.nupkg')
                            with zipfile.ZipFile(path) as archive:
                                entries = {name: archive.read(name) for name in archive.namelist()}
                            before = self.row['commit'] if field == 'commit' else version
                            after = 'a' * 40 if field == 'commit' else '99.0.0-proof.42.1'
                            entries['fixture.nuspec'] = entries['fixture.nuspec'].replace(before.encode(), after.encode())
                            with zipfile.ZipFile(path, 'w') as archive:
                                for name, data in entries.items():
                                    archive.writestr(name, data)
                            with self.subTest(symbols=symbols, field=field), \
                                    self.assertRaisesRegex(ValueError, 'repository provenance|Symbol metadata|Packed version|Symbol identity'):
                                maintenance.verify_artifacts(artifacts, policy, self.row, version, maintenance.ROOT,
                                                             Path('/inspector'), self.root)


class CoreCandidateContracts(unittest.TestCase):
    def setUp(self):
        self.register = maintenance.load_register()
        self.candidates = maintenance.load_candidates()
        self.bridges = [row for row in self.candidates['candidates'] if row['kind'] == 'metadata-bridge']
        self.candidate = self.bridges[0]
        self.row = self.select(self.candidate)

    def select(self, candidate):
        return maintenance.selection(self.register, candidate['product'], candidate['line'], candidate['commit'],
                                     candidate['line'] + '.0-proof.42.1', 'core')

    def candidate_object(self, updates=None, parents=None):
        candidate = dict(self.candidate)
        with tempfile.TemporaryDirectory() as temporary:
            env = maintenance.build_environment() | {'GIT_INDEX_FILE': str(Path(temporary) / 'index'),
                'GIT_AUTHOR_NAME': 'Test', 'GIT_AUTHOR_EMAIL': 'test@example.invalid',
                'GIT_COMMITTER_NAME': 'Test', 'GIT_COMMITTER_EMAIL': 'test@example.invalid',
                'GIT_AUTHOR_DATE': '2026-10-09T01:00:00Z', 'GIT_COMMITTER_DATE': '2026-10-09T01:00:00Z'}
            maintenance.git(maintenance.ROOT, 'read-tree', candidate['commit'], env=env)
            for path, data in (updates or {}).items():
                blob = subprocess.run(['git', 'hash-object', '-w', '--stdin'], cwd=maintenance.ROOT,
                    input=data, capture_output=True, check=True).stdout.decode().strip()
                maintenance.git(maintenance.ROOT, 'update-index', '--add', '--cacheinfo',
                                f'100644,{blob},{path}', env=env)
            candidate['tree'] = maintenance.git(maintenance.ROOT, 'write-tree', env=env)
            candidate['parents'] = parents if parents is not None else candidate['parents']
            candidate['commit'] = maintenance.git(maintenance.ROOT, 'commit-tree', candidate['tree'],
                *[arg for parent in candidate['parents'] for arg in ('-p', parent)], '-m', 'Rejected fixture', env=env)
        return candidate

    def verify_registered(self, candidate):
        candidates = self.candidates | {'candidates': [candidate]}
        with patch.object(maintenance, 'load_candidates', return_value=candidates):
            maintenance.verify_core_candidate(maintenance.ROOT, self.select(candidate))

    def test_four_exact_bridges_and_full_original_merge_parents_are_bound(self):
        for candidate in self.bridges:
            with self.subTest(product=candidate['product'], line=candidate['line']):
                maintenance.verify_core_candidate(maintenance.ROOT, self.select(candidate))
                self.assertEqual(len(candidate['original_parents']), 2 if candidate['line'] == '3.9' else 1)
                with self.assertRaises(ValueError):
                    maintenance.selection(self.register, candidate['product'], candidate['line'],
                                          candidate['commit'], candidate['line'] + '.0-proof.42.1')
        for original in self.register['sources']:
            self.assertEqual(maintenance.selection(self.register, original['product'], original['line'],
                original['commit'], original['dependency_version'] + '-proof.42.1'), original)

    def test_four_metadata_descendants_and_four_exact_continuations_remain_exact(self):
        from historical_studio_npm_continuation import CONTINUATIONS
        from extensions_manifest_continuation import CONTINUATIONS as EXTENSIONS
        descendants = [row for row in self.candidates['candidates'] if row['kind'] == 'maintenance']
        self.assertEqual(len(descendants), 8)
        metadata = [row for row in descendants if [change['path'] for change in row['delta']] == ['Directory.Build.props']]
        self.assertEqual(len(metadata), 4)
        self.assertEqual({row['commit'] for row in descendants if row not in metadata},
                         {row['commit'] for row in (*CONTINUATIONS.values(), *EXTENSIONS.values())})
        for candidate in metadata:
            with self.subTest(product=candidate['product'], line=candidate['line']):
                maintenance.verify_core_candidate(maintenance.ROOT, self.select(candidate))
                self.assertEqual([change['path'] for change in candidate['delta']], ['Directory.Build.props'])
                props = maintenance.git_bytes(maintenance.ROOT, candidate['commit'], 'Directory.Build.props')
                self.assertIn(b'<RepositoryUrl>https://github.com/elsa-workflows/elsa-core</RepositoryUrl>', props)
                self.assertIn(b'<PackageProjectUrl>https://github.com/elsa-workflows/elsa-core</PackageProjectUrl>', props)

    def test_unknown_cross_cell_kind_and_duplicate_candidates_fail(self):
        for product, line, commit in [('extensions', '3.8', self.row['commit']),
                                      ('studio', '3.9', self.row['commit']), ('studio', '3.8', 'a' * 40)]:
            with self.subTest(product=product, line=line), self.assertRaises(ValueError):
                maintenance.selection(self.register, product, line, commit, line + '.0-proof.42.1', 'core')
        mutations = [dict(self.candidate, original_commit='a' * 40),
                     dict(self.candidate, source_repository='other/repository'),
                     dict(self.candidate, kind='arbitrary-descendant'),
                     dict(self.candidate, dependency_version='99.0.0')]
        for candidate in mutations:
            with self.subTest(candidate=candidate), self.assertRaises(ValueError):
                self.verify_registered(candidate)
        duplicate = {'schema': 1, 'candidates': [self.candidate, dict(self.candidate, product='extensions')]}
        with patch.object(maintenance, 'load_candidates', return_value=duplicate), self.assertRaises(ValueError):
            self.select(self.candidate)

    def test_registered_wrong_graph_and_metadata_identities_fail(self):
        for key, value in [('tree', 'a' * 40), ('contained_commit', 'a' * 40),
                           ('contained_tree', 'a' * 40), ('original_parents', ['a' * 40]),
                           ('parents', [self.candidate['original_commit']])]:
            with self.subTest(key=key), self.assertRaises(ValueError):
                self.verify_registered(dict(self.candidate, **{key: value}))
        for parents in [[self.candidate['original_commit']],
                        [self.candidate['contained_commit'], self.candidates['candidates'][1]['contained_commit']]]:
            with self.subTest(parents=parents), self.assertRaises(ValueError):
                self.verify_registered(self.candidate_object(parents=parents))
        props = maintenance.git_bytes(maintenance.ROOT, self.row['commit'], 'Directory.Build.props')
        for updates in [{'Directory.Build.props': props + b'<!-- extra metadata -->'},
                        {'Directory.Build.props': props.replace(b'<PackageProjectUrl>', b'<ChangedProjectUrl>')},
                        {'Directory.Packages.props': b'changed pins'},
                        {'.github/workflows/new.yml': b'on: push'},
                        {'.github/maintenance-inert-workflows/packages.yml.source': b'changed workflow'},
                        {'src/Changed.cs': b'class Changed {}'}]:
            with self.subTest(paths=list(updates)), self.assertRaises(ValueError):
                self.verify_registered(self.candidate_object(updates))

    def test_parent_admission_does_not_admit_unregistered_descendant(self):
        descendant = self.candidate_object(parents=[self.candidate['commit']])
        with self.assertRaises(ValueError):
            self.select(descendant)
        # A separate controller can see the Git object without containing it in its ancestry.
        with tempfile.TemporaryDirectory() as temporary:
            controller = Path(temporary)
            subprocess.run(['git', 'init', '-q', str(controller)], check=True)
            objects = maintenance.git(maintenance.ROOT, 'rev-parse', '--path-format=absolute', '--git-path', 'objects')
            (controller / '.git/objects/info/alternates').write_text(objects + '\n')
            maintenance.git(controller, 'update-ref', 'HEAD', self.candidate['original_commit'])
            with self.assertRaises(ValueError):
                maintenance.verify_source(controller, self.row)

    def details(self, row=None):
        row = row or self.row
        path = 'Directory.Build.props'
        data = maintenance.git_bytes(maintenance.ROOT, row['commit'], path)
        url = f"https://raw.githubusercontent.com/{row['source_repository']}/{row['commit']}/{path}"
        return {'source_link': {'documents': {'/_/*': url.rsplit('/', 1)[0] + '/*'}}, 'documents': [
            {'path': '/_/' + path, 'algorithm': 'sha256', 'checksum': hashlib.sha256(data).hexdigest(),
             'embedded_checksum': None}]}, data, url

    def response(self, data, url):
        response = io.BytesIO(data)
        response.geturl = lambda: url
        return response

    def test_core_fetches_immutable_bytes_and_reuses_only_strict_exact_url_cache(self):
        details, data, url = self.details()
        cache = {('remote', url): b'untrusted permissive cache'}
        with patch('prove_consolidated_packages.urllib.request.OpenerDirector.open',
                   side_effect=lambda *_args, **_kwargs: self.response(data, url)) as fetch:
            for _ in range(2):
                verified = maintenance.verify_documents(details, maintenance.ROOT, self.row, cache=cache)
                self.assertEqual(verified[0]['source'], 'core-git')
                self.assertTrue(verified[0]['remote_fetched'])
            self.assertEqual(fetch.call_count, 1)
            self.assertEqual(fetch.call_args.args[0], url)
        wrong = json.loads(json.dumps(details)); wrong['documents'][0]['checksum'] = '0' * 64
        with patch('prove_consolidated_packages.urllib.request.OpenerDirector.open') as fetch, self.assertRaises(ValueError):
            maintenance.verify_documents(wrong, maintenance.ROOT, self.row)
        fetch.assert_not_called()

    def test_core_missing_wrong_foreign_redirect_and_duplicate_documents_fail(self):
        details, data, url = self.details()
        for payload, returned_url in [(b'wrong bytes', url), (data, 'https://foreign.invalid/private-secret')]:
            with patch('prove_consolidated_packages.urllib.request.OpenerDirector.open',
                       return_value=self.response(payload, returned_url)), self.assertRaisesRegex(ValueError, 'Core remote source mismatch'):
                maintenance.verify_documents(details, maintenance.ROOT, self.row)
        error = urllib.error.HTTPError(url, 404, 'private-secret', {}, None)
        self.addCleanup(error.close)
        with patch('prove_consolidated_packages.urllib.request.OpenerDirector.open', side_effect=error), \
                self.assertRaisesRegex(ValueError, 'Core remote source unavailable'):
            maintenance.verify_documents(details, maintenance.ROOT, self.row)
        foreign = json.loads(json.dumps(details)); foreign['source_link']['documents'] = {'/_/*': 'https://foreign.invalid/*'}
        with self.assertRaisesRegex(ValueError, 'SourceLink repository'):
            maintenance.verify_documents(foreign, maintenance.ROOT, self.row)
        with self.assertRaisesRegex(ValueError, 'Duplicate PDB document'):
            maintenance.verify_documents(dict(details, documents=details['documents'] * 2), maintenance.ROOT,
                self.row, cache={('remote-strict', url): data})

    def test_original_mode_never_fetches_and_core_placeholder_uses_original_anchor(self):
        original = self.register['sources'][0]
        details, _, _ = self.details(original)
        with patch('prove_consolidated_packages.urllib.request.urlopen') as fetch:
            maintenance.verify_documents(details, maintenance.ROOT, original)
        fetch.assert_not_called()
        for candidate in [row for row in self.candidates['candidates'] if row['product'] == 'extensions']:
            row = self.select(candidate)
            policies = maintenance.placeholder_policies(maintenance.ROOT, row)
            self.assertEqual(len(policies), 2)
            self.assertTrue(all(policy['source_commit'] == row['commit'] for policy in policies.values()))
            changed = dict(row, commit=self.candidate_object({
                self.register['inherited_skipped_placeholders'][0]['source_file']: b'changed placeholder'})['commit'])
            with self.assertRaises(ValueError):
                maintenance.placeholder_policies(maintenance.ROOT, changed)


    def test_remote_failure_receipt_keeps_closed_reason_and_no_private_error(self):
        details, _, url = self.details()
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / 'proof'
            def reject(*_args):
                return maintenance.verify_documents(details, maintenance.ROOT, self.row)
            with patch.object(maintenance, 'verify_source', side_effect=reject), \
                    patch('prove_consolidated_packages.urllib.request.OpenerDirector.open',
                          side_effect=OSError('private-secret /private/runner-host HTTP response')), \
                    self.assertRaises(ValueError):
                maintenance.prepare(maintenance.ROOT, self.row, '3.8.4-proof.42.1', output)
            receipt = json.loads((output / 'receipt.json').read_text())
            self.assertFalse(receipt['success'])
            self.assertEqual(receipt['error']['reason'], 'source-remote-unavailable')
            self.assertEqual(receipt['candidates_sha256'], maintenance.digest(maintenance.CANDIDATES.read_bytes()))
            for private in ('private-secret', '/private/runner-host', str(output)):
                self.assertNotIn(private, json.dumps(receipt))

    def test_strict_redirect_is_rejected_before_target_request_and_default_still_follows(self):
        details, data, _ = self.details()
        requests = []
        class Handler(BaseHTTPRequestHandler):
            def do_GET(self):
                requests.append(self.path)
                if self.path == '/redirect':
                    self.send_response(302)
                    self.send_header('Location', '/source')
                    self.end_headers()
                else:
                    self.send_response(200)
                    self.end_headers()
                    self.wfile.write(data)
            def log_message(self, *_args):
                pass
        server = HTTPServer(('127.0.0.1', 0), Handler)
        thread = Thread(target=lambda: server.serve_forever(poll_interval=0.01), daemon=True)
        thread.start()
        def stop():
            server.shutdown()
            server.server_close()
            thread.join(timeout=2)
        self.addCleanup(stop)
        url = f'http://127.0.0.1:{server.server_port}/redirect'
        document = details['documents'][0]
        with self.assertRaisesRegex(ValueError, 'Remote source redirected'):
            consolidated.verify_tracked_document(maintenance.ROOT, self.row['commit'], 'Directory.Build.props',
                document, url, True, {}, reject_redirects=True)
        self.assertEqual(requests, ['/redirect'])
        record = consolidated.verify_tracked_document(maintenance.ROOT, self.row['commit'], 'Directory.Build.props',
            document, url, True, {})
        self.assertTrue(record['remote_fetched'])
        self.assertEqual(requests, ['/redirect', '/redirect', '/source'])

    def test_strict_response_read_is_bounded_and_rejects_short_wrong_and_oversized_bytes(self):
        details, data, url = self.details()
        for payload in (data[:-1], b'x' * len(data), data + b'oversized' * 1000):
            response = self.response(payload, url)
            with patch.object(response, 'read', wraps=response.read) as read, \
                    patch('prove_consolidated_packages.urllib.request.OpenerDirector.open', return_value=response), \
                    self.assertRaisesRegex(ValueError, 'Core remote source mismatch'):
                maintenance.verify_documents(details, maintenance.ROOT, self.row)
            read.assert_called_once_with(len(data) + 1)
        response = self.response(data, url)
        with patch.object(response, 'read', wraps=response.read) as read, \
                patch('prove_consolidated_packages.urllib.request.OpenerDirector.open', return_value=response):
            maintenance.verify_documents(details, maintenance.ROOT, self.row)
        read.assert_called_once_with(len(data) + 1)


class MaintenanceWorkflowSelectionContracts(unittest.TestCase):
    def setUp(self):
        self.register = maintenance.load_register()
        self.environment = {'EVENT': 'push', 'REF': 'refs/heads/codex/elsa-integration-maintenance-candidates-8683',
                            'GITHUB_RUN_ID': '8683', 'GITHUB_RUN_ATTEMPT': '2'}

    def test_exact_core_branch_selects_only_eight_explicit_registered_shas_through_admission(self):
        with patch.object(maintenance, 'selection', wraps=maintenance.selection) as admit:
            rows = maintenance.workflow_selections(self.register, self.environment)
        candidates = maintenance.load_candidates()['rehearsal_sources']
        self.assertEqual([(row['product'], row['line'], row['commit']) for row in rows],
                         [(row['product'], row['line'], row['commit']) for row in candidates])
        self.assertEqual(admit.call_count, 8)
        self.assertTrue(all(call.args[-1] == 'core' for call in admit.call_args_list))
        for row in rows:
            self.assertEqual(row['source_repository'], maintenance.CORE_REPOSITORY)
            self.assertEqual(row['version'], row['dependency_version'] + '-proof.8683.2')

    def test_missing_duplicate_wrong_cell_unknown_and_extra_rehearsal_fields_fail(self):
        catalog = maintenance.load_candidates()
        valid = catalog['rehearsal_sources']
        mutations = [valid[:-1], valid + [valid[0]], [valid[0], valid[0], *valid[2:]],
                     [dict(valid[0], commit='a' * 40), *valid[1:]],
                     [dict(valid[0], product='extensions'), *valid[1:]],
                     [dict(valid[0], line='3.9'), *valid[1:]],
                     [dict(valid[0], source_kind='core'), *valid[1:]]]
        for sources in mutations:
            with self.subTest(sources=sources), patch.object(maintenance, 'load_candidates',
                    return_value=catalog | {'rehearsal_sources': sources}), \
                    self.assertRaisesRegex(ValueError, 'Incomplete maintenance rehearsal cells'):
                maintenance.workflow_selections(self.register, self.environment)
        # Registry order never selects a latest tip or changes the explicit B+D checkpoint.
        with patch.object(maintenance, 'load_candidates', return_value=catalog | {
                'candidates': list(reversed(catalog['candidates']))}):
            rows = maintenance.workflow_selections(self.register, self.environment)
        self.assertEqual([row['commit'] for row in rows], [row['commit'] for row in valid])
        self.assertEqual([row['kind'] for row in rows].count('maintenance'), 4)
        self.assertEqual([row['kind'] for row in rows].count('metadata-bridge'), 4)
        extra = dict(catalog['candidates'][-1], commit='a' * 40,
                     parents=[catalog['candidates'][-1]['commit']])
        with patch.object(maintenance, 'load_candidates', return_value=catalog | {
                'candidates': [*catalog['candidates'], extra]}):
            rows = maintenance.workflow_selections(self.register, self.environment)
            self.assertEqual([row['commit'] for row in rows], [row['commit'] for row in valid])
            with self.assertRaisesRegex(ValueError, 'Unregistered maintenance rehearsal selection'):
                maintenance.build_selection(self.register, dict(extra, version='3.9.0-proof.8683.1'), self.environment)

    def test_cached_selector_rerun_uses_actual_build_attempt_for_both_push_branches(self):
        for ref in ('refs/heads/codex/maintenance-builds-8677', self.environment['REF']):
            cached_environment = self.environment | {'REF': ref, 'GITHUB_RUN_ATTEMPT': '1'}
            cached = maintenance.workflow_selections(self.register, cached_environment)
            for selected in cached:
                actual = maintenance.build_selection(self.register, selected | {'dependency_version': '99.0.0'},
                    cached_environment | {'GITHUB_RUN_ATTEMPT': '2'})
                self.assertEqual(actual['commit'], selected['commit'])
                self.assertEqual(actual['version'], selected['dependency_version'] + '-proof.8683.2')
                self.assertNotEqual(actual['version'], selected['version'])
            with self.assertRaises(ValueError):
                maintenance.build_selection(self.register, cached[0], cached_environment | {'GITHUB_RUN_ATTEMPT': '01'})
            for environment in ({'EVENT': 'push', 'REF': 'refs/heads/main'},
                                {'EVENT': 'pull_request'}, {'REF': ref + '-foreign'}):
                with self.subTest(environment=environment), self.assertRaises(ValueError):
                    maintenance.build_selection(self.register, cached[0], cached_environment | environment)

    def test_manual_build_keeps_explicit_main_version_despite_different_actual_attempt(self):
        for candidate in (self.register['sources'][0], maintenance.load_candidates()['candidates'][-1]):
            selected = dict(candidate, version=candidate['line'] + '.99-proof.123.1')
            environment = self.environment | {'EVENT': 'workflow_dispatch', 'REF': 'refs/heads/main'}
            actual = maintenance.build_selection(self.register, selected, environment)
            self.assertEqual(actual['version'], selected['version'])
            self.assertEqual(actual['commit'], selected['commit'])
            with self.assertRaisesRegex(ValueError, 'requires main'):
                maintenance.build_selection(self.register, selected, environment | {'REF': self.environment['REF']})

    def test_original_branch_retains_exact_original_matrix_and_ignores_push_inputs(self):
        environment = self.environment | {'REF': 'refs/heads/codex/maintenance-builds-8677', 'SOURCE_KIND': 'core'}
        with patch.object(maintenance, 'load_candidates', side_effect=AssertionError('Original path must stay independent')):
            rows = maintenance.workflow_selections(self.register, environment)
        self.assertEqual(rows, [dict(row, version=row['dependency_version'] + '-proof.8683.2')
                                for row in self.register['sources']])

    def test_other_event_ref_combinations_and_invalid_run_identity_fail_closed(self):
        for event, ref in [('push', 'refs/heads/main'), ('push', 'refs/heads/codex/elsa-integration-maintenance-candidates-86830'),
                           ('push', 'refs/tags/codex/elsa-integration-maintenance-candidates-8683'),
                           ('pull_request', self.environment['REF']), ('workflow_dispatch', self.environment['REF'])]:
            with self.subTest(event=event, ref=ref), self.assertRaises(ValueError):
                maintenance.workflow_selections(self.register, self.environment | {'EVENT': event, 'REF': ref})
        for run in ('0', '01', 'private-secret'):
            with self.subTest(run=run), self.assertRaises(ValueError):
                maintenance.workflow_selections(self.register, self.environment | {'GITHUB_RUN_ID': run})
        candidates = maintenance.load_candidates()
        with patch.object(maintenance, 'load_candidates', return_value=candidates | {'candidates': candidates['candidates'][:-1]}), \
                self.assertRaisesRegex(ValueError, 'Incomplete maintenance rehearsal cells'):
            maintenance.workflow_selections(self.register, self.environment)

    def test_manual_main_selects_only_requested_exact_original_or_core_commit(self):
        for kind, candidates in [('original', self.register['sources']),
                                 ('core', maintenance.load_candidates()['candidates'])]:
            row = candidates[-1]
            environment = {'EVENT': 'workflow_dispatch', 'REF': 'refs/heads/main', 'PRODUCT': row['product'],
                           'LINE': row['line'], 'SOURCE_COMMIT': row['commit'], 'SOURCE_KIND': kind,
                           'PROOF_VERSION': row['line'] + '.0-proof.8683.2'}
            selected = maintenance.workflow_selections(self.register, environment)
            self.assertEqual(len(selected), 1)
            self.assertEqual(selected[0]['commit'], row['commit'])
            for changes in ({'SOURCE_COMMIT': 'a' * 40}, {'PROOF_VERSION': '3.8.4'}, {'SOURCE_KIND': 'publisher'}):
                with self.subTest(kind=kind, changes=changes), self.assertRaises(ValueError):
                    maintenance.workflow_selections(self.register, environment | changes)


class ContinuingCandidateContracts(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name) / 'git'
        self.root.mkdir()
        maintenance.git(self.root, 'init', '-q')
        self.env = maintenance.build_environment() | {
            'GIT_AUTHOR_NAME': 'Test', 'GIT_AUTHOR_EMAIL': 'test@example.invalid',
            'GIT_COMMITTER_NAME': 'Test', 'GIT_COMMITTER_EMAIL': 'test@example.invalid',
            'GIT_INDEX_FILE': str(Path(temporary.name) / 'index')}
        self.props = (b'<Project><RepositoryUrl>https://github.com/elsa-workflows/elsa-studio</RepositoryUrl>'
                      b'<PackageProjectUrl>https://github.com/elsa-workflows/elsa-studio</PackageProjectUrl></Project>')
        files = {'Directory.Build.props': self.props, 'Directory.Packages.props': b'pinned',
                 '.github/workflows/packages.yml': b'on: push', '.github/actions/publish/action.yml': b'publisher',
                 'Elsa.Studio.sln': b'original layout', 'src/Fixture/Fixture.csproj': b'project',
                 'src/Fixture/Existing.csproj': b'existing project',
                 'src/Fixture/Existing.csproj.user': (
                     b'<Project><PropertyGroup><BuildProjectReferences>false</BuildProjectReferences>'
                     b'</PropertyGroup></Project>'),
                 'src/Fixture/Feature.cs': b'class Feature { public int Value() => 1; }',
                 'test/Placeholder.cs': b'unchanged placeholder', 'docs/obsolete.md': b'old documentation',
                 'src/Fixture/ClientLib/package.json': b'{"scripts":{"build":"webpack"}}',
                 'src/Fixture/ClientLib/webpack.config.js': b'build policy'}
        base = self.object(None, files)
        original = self.object(base['commit'], {'docs/README.md': b'release'})
        self.original = dict(maintenance.load_register()['sources'][0], **original,
                             parent=base['commit'], workflows=['.github/workflows/packages.yml'])
        self.original.pop('parents'); self.original.pop('delta')
        maintenance.git(self.root, 'update-ref', 'HEAD', original['commit'])
        self.register = {'sources': [self.original], 'inherited_skipped_placeholders': [
            {'product': 'studio', 'commits': [original['commit']], 'source_file': 'test/Placeholder.cs'}]}
        contained = maintenance.prepare_containment(self.root, Path(temporary.name) / 'contained', self.register)
        containment = Path(temporary.name) / 'containment.json'
        maintenance.write_json(containment, contained)
        k = contained['sources'][0]
        bridge = self.object(k['commit'], {'Directory.Build.props': self.props.replace(
            b'<RepositoryUrl>https://github.com/elsa-workflows/elsa-studio</RepositoryUrl>',
            b'<RepositoryUrl>https://github.com/elsa-workflows/elsa-core</RepositoryUrl>')})
        bridge.pop('delta')
        self.bridge = dict(bridge, product='studio', line='3.8', source_kind='core', kind='metadata-bridge',
            source_repository=maintenance.CORE_REPOSITORY, original_commit=original['commit'],
            original_parents=[base['commit']], contained_commit=k['commit'], contained_tree=k['tree'])
        self.catalog = {'schema': 2, 'candidates': [self.bridge], 'rehearsal_sources': [],
                        'published': False, 'maintenance_refs_activated': False}
        for name, value in [('load_register', self.register), ('load_candidates', self.catalog)]:
            mock = patch.object(maintenance, name, return_value=value)
            mock.start(); self.addCleanup(mock.stop)
        mock = patch.object(maintenance, 'CONTAINMENT', containment)
        mock.start(); self.addCleanup(mock.stop)

    def object(self, parent, updates, parents=None):
        maintenance.git(self.root, 'read-tree', parent if parent else '--empty', env=self.env)
        for path, value in updates.items():
            if value is None:
                maintenance.git(self.root, 'update-index', '--force-remove', '--', path, env=self.env)
            else:
                mode, data = value if isinstance(value, tuple) else ('100644', value)
                blob = subprocess.run(['git', 'hash-object', '-w', '--stdin'], cwd=self.root, input=data,
                                      check=True, capture_output=True).stdout.decode().strip()
                maintenance.git(self.root, 'update-index', '--add', '--cacheinfo', f'{mode},{blob},{path}', env=self.env)
        tree = maintenance.git(self.root, 'write-tree', env=self.env)
        parents = parents if parents is not None else [parent] if parent else []
        commit = maintenance.git(self.root, 'commit-tree', tree,
            *[arg for p in parents for arg in ('-p', p)], '-m', 'Temporary candidate fixture', env=self.env)
        before = maintenance.tree_entries(self.root, parent) if parent else {}
        return {'commit': commit, 'tree': tree, 'parents': parents,
                'delta': maintenance.candidate_tree_delta(before, maintenance.tree_entries(self.root, commit))}

    def descendant(self, updates=None, parent=None, parents=None, register=True):
        parent = parent or self.bridge
        row = dict(parent, **self.object(parent['commit'], updates or {
            'src/Fixture/Feature.cs': b'class Feature { public int Value() => 2; }'}, parents), kind='maintenance')
        if register:
            self.catalog['candidates'].append(row)
        return row

    def select(self, row):
        return maintenance.selection(self.register, row['product'], row['line'], row['commit'], '3.8.4-proof.1.1', 'core')

    def verify(self, row):
        maintenance.verify_core_candidate(self.root, self.select(row))

    def test_reviewed_source_and_tests_add_modify_delete_then_registered_continuation(self):
        d = self.descendant({'src/Fixture/Feature.cs': b'class Feature { public int Value() => 2; }',
            'test/Regression.cs': b'class Regression { void Runs() { Assert.Equal(2, new Feature().Value()); } }',
            'docs/obsolete.md': None, 'src/Fixture/Data.txt': ('100755', b'fixture data')})
        self.verify(d)
        e = self.descendant({'src/Fixture/Feature.cs': b'class Feature { public int Value() => 3; }'}, parent=d, register=False)
        with self.assertRaisesRegex(ValueError, 'Unregistered'):
            self.select(e)
        self.catalog['candidates'].append(e)
        self.verify(e)
        with self.assertRaises(ValueError):
            maintenance.verify_source(self.root, self.select(e))
        maintenance.git(self.root, 'update-ref', 'HEAD', e['commit'])
        maintenance.verify_source(self.root, self.select(e))

    def test_clientlib_source_and_test_fixture_formats_remain_reviewable(self):
        paths = {'src/Fixture/ClientLib/src/feature.ts': b'export const value = 2;',
                 'src/Fixture/ClientLib/src/view.vue': b'<template>changed</template>',
                 'src/Fixture/ClientLib/src/theme.css': b'body { color: red; }',
                 'src/Fixture/ClientLib/src/__fixtures__/activity.json': b'{"value":2}',
                 'tests/browser/feature.spec.ts': b'expect(value).toBe(2)',
                 'test/fixtures/data.xml': b'<value>2</value>', 'docs/example.yaml': b'example: 2'}
        self.verify(self.descendant(paths))

    def test_metadata_followup_only_replaces_exact_project_url_and_keeps_core_repository(self):
        props = maintenance.git_bytes(self.root, self.bridge['commit'], 'Directory.Build.props')
        d = self.descendant({'Directory.Build.props': props.replace(
            b'<PackageProjectUrl>https://github.com/elsa-workflows/elsa-studio</PackageProjectUrl>',
            b'<PackageProjectUrl>https://github.com/elsa-workflows/elsa-core</PackageProjectUrl>')})
        self.verify(d)
        self.verify(self.descendant(parent=d))
        e = self.descendant({'Directory.Build.props': props}, parent=d)
        with self.assertRaisesRegex(ValueError, 'metadata mismatch'):
            self.verify(e)

    def test_registered_parent_required_with_same_cell_anchor_and_contained_binding(self):
        for field, value in [('parents', ['a' * 40]), ('original_parents', ['a' * 40]),
                             ('contained_commit', 'a' * 40), ('contained_tree', 'a' * 40)]:
            d = self.descendant(register=False)
            d[field] = value
            self.catalog['candidates'].append(d)
            with self.subTest(field=field), self.assertRaisesRegex(ValueError, 'parent chain'):
                self.select(d)
            self.catalog['candidates'].pop()
        d = self.descendant(register=False)
        d['line'] = '3.9'
        with self.assertRaises(ValueError):
            self.select(d)

    def test_cycles_hidden_merge_parents_and_wrong_actual_tree_or_parent_fail(self):
        d = self.descendant()
        saved = dict(d)
        d['parents'] = [d['commit']]
        with self.assertRaisesRegex(ValueError, 'parent chain'):
            self.select(d)
        d.update(saved)
        d['tree'] = 'a' * 40
        with self.assertRaisesRegex(ValueError, 'graph mismatch'):
            self.verify(d)
        d.update(saved)
        d['parents'] = [self.bridge['commit'], self.bridge['contained_commit']]
        with self.assertRaises(ValueError):
            self.select(d)
        d.update(saved)
        hidden = self.descendant(parents=[self.bridge['commit'], self.original['commit']], register=False)
        hidden['parents'] = [self.bridge['commit']]
        self.catalog['candidates'].append(hidden)
        with self.assertRaisesRegex(ValueError, 'graph mismatch'):
            self.verify(hidden)

    def test_complete_exact_delta_rejects_omitted_blob_mode_noop_and_path_aliases(self):
        d = self.descendant({'src/Fixture/Feature.cs': b'changed', 'test/Regression.cs': b'regression'})
        valid = json.loads(json.dumps(d['delta']))
        mutations = [valid[:-1], valid[::-1], valid + [valid[-1]],
                     [dict(valid[0], before=None), valid[1]],
                     [dict(valid[0], after=valid[0]['before']), valid[1]]]
        for key, value in [('blob', 'a' * 40), ('mode', '100755'), ('type', 'commit')]:
            mutation = json.loads(json.dumps(valid)); mutation[0]['after'][key] = value; mutations.append(mutation)
        for path in ('../escape.cs', '/absolute.cs', 'src/./Fixture.cs', 'src\\Fixture.cs', 'src/evil\n.cs', 'src/file%20.cs'):
            mutations.append([dict(valid[0], path=path), valid[1]])
        for delta in mutations:
            d['delta'] = delta
            with self.subTest(delta=delta), self.assertRaisesRegex(ValueError, 'delta mismatch'):
                self.verify(d)
        d['delta'] = valid
        self.verify(d)

    def test_declared_control_changes_additions_and_placeholder_drift_still_reject(self):
        paths = ('Directory.Packages.props', 'Elsa.Studio.sln', 'src/Fixture/Fixture.csproj',
                 'src/Fixture/Fixture.csproj.user', 'src/Fixture/Existing.csproj.user',
                 'src/Fixture/Imported.targets', 'src/Fixture/Directory.Build.props',
                 'src/Fixture/ClientLib/package.json', 'src/Fixture/ClientLib/package-lock.json',
                 'src/Fixture/ClientLib/webpack.config.js', 'src/Fixture/ClientLib/tsconfig.json',
                 'src/Fixture/ClientLib/scripts/generate.js', 'src/Fixture/global.json',
                 'src/Fixture/NuGet.Config', 'build/Build.cs', 'build.sh',
                 '.github/workflows/new.yml', '.github/maintenance-inert-workflows/packages.yml.source',
                 '.github/actions/publish/action.yml', 'test/Placeholder.cs', 'core/src/Moved.cs')
        for path in paths:
            d = self.descendant({path: b'changed'}, register=False)
            self.catalog['candidates'].append(d)
            with self.subTest(path=path), self.assertRaisesRegex(ValueError, 'protected control'):
                self.verify(d)
            self.catalog['candidates'].pop()

    def test_regular_blob_only_rejects_symlink_and_closed_fields_forbid_dependency_override(self):
        d = self.descendant({'src/Fixture/link': ('120000', b'../private')})
        with self.assertRaisesRegex(ValueError, 'delta mismatch'):
            self.select(d)
        self.catalog['candidates'].pop()
        d = self.descendant()
        for key, value in [('dependency_version', '99.0.0'), ('unexpected', True)]:
            d[key] = value
            with self.subTest(key=key), self.assertRaisesRegex(ValueError, 'identity mismatch'):
                self.select(d)
            d.pop(key)
        self.catalog['extra'] = True
        with self.assertRaisesRegex(ValueError, 'identity mismatch'):
            self.select(d)
