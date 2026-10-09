from contextlib import ExitStack
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET
import zipfile

import prepare_maintenance_build as maintenance


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
                restore_assets=[{'framework': 'net8.0', 'path': '/private-secret/restore', 'sha256': 'b' * 64}],
                framework_properties={'net8.0': {'compiler_evidence': {'sdk_version': '10.0.300',
                    'sdk_root': '/private-secret/sdk', 'compiler_sha256': 'c' * 64, 'tools': {}}}})
            if failure == 'sdk-metadata':
                raise ValueError('/private-secret/metadata failure')
        with ExitStack() as stack:
            for name, options in {
                'git': {'return_value': 'd' * 40}, 'verify_source': {'return_value': None},
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
                        '<package><metadata><id>Fixture</id><dependencies><group targetFramework="net8.0" /></dependencies></metadata></package>')
                return ''
            return json.dumps({'Properties': {'ProjectAssetsFile': str(assets)}, 'Items': {}})
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

    def write_package_fixture(self, dependency='3.8.4', packed_version='3.8.4-proof.42.1', frameworks=(), groups=None):
        artifacts = self.root / 'artifacts'; artifacts.mkdir(exist_ok=True)
        groups = groups if groups is not None else f'<group targetFramework="net8.0"><dependency id="Elsa.Api.Client" version="{dependency}" /></group>'
        nuspec = f'''<package><metadata><id>Elsa.Studio.Fixture</id><version>{packed_version}</version>
          <repository type="git" url="https://github.com/elsa-workflows/elsa-studio" commit="{self.row['commit']}" />
          <dependencies>{groups}</dependencies>
          </metadata></package>'''
        path = artifacts / 'fixture.nupkg'
        with zipfile.ZipFile(path, 'w') as zipped:
            zipped.writestr('fixture.nuspec', nuspec)
            zipped.writestr('build/Fixture.targets', '<Project/>')
            for framework in frameworks:
                zipped.writestr(f'lib/{framework}/Elsa.Studio.Fixture.dll', f'assembly-{framework}')
        if frameworks:
            with zipfile.ZipFile(path.with_suffix('.snupkg'), 'w') as zipped:
                zipped.writestr('fixture.nuspec', nuspec)
                for framework in frameworks:
                    zipped.writestr(f'lib/{framework}/Elsa.Studio.Fixture.pdb', f'symbols-{framework}')
        return artifacts

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
                  'expected_symbol_dependency_groups': expected, 'sdk_symbol_nuspec_sha256': 'b' * 64}
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

    def sdk_dependency_fixture(self):
        groups = [{'framework': 'net8.0', 'dependencies': [
            {'id': 'Elsa.Api.Client', 'version': '3.8.4', 'include': '', 'exclude': ''}]}]
        return {'expected_dependency_groups': groups, 'sdk_nuspec_sha256': 'a' * 64,
                'expected_symbol_dependency_groups': groups, 'sdk_symbol_nuspec_sha256': 'b' * 64}

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
                ('secret-password /private/runner-host-42', 'unknown-check-failure')]):
            output = self.root / f'proof-{index}'
            with patch.object(maintenance, 'verify_source', side_effect=ValueError(message)):
                with self.assertRaises(ValueError):
                    maintenance.prepare(maintenance.ROOT, self.row, '3.8.4-proof.42.1', output)
            receipt = json.loads((output / 'receipt.json').read_text())
            self.assertEqual(receipt['error']['reason'], expected)
            self.assertNotIn(message, json.dumps(receipt))
            self.assertNotIn('/private/runner-host', json.dumps(receipt))
