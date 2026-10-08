import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
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

    def write_trx_fixture(self, product='studio', name='private-runner-host', assembly=None):
        results = self.root / ('test-results' if product == 'studio' else 'source/testresults')
        results.mkdir(parents=True, exist_ok=True)
        assembly = assembly or self.root / 'source/bin/Release/net8.0/Fixture.Tests.dll'
        path = results / (name + '.trx')
        path.write_text(f'<TestRun><ResultSummary><Counters total="3" passed="3" failed="0" private="private-secret-host" /></ResultSummary>'
            f'<TestDefinitions><UnitTest><TestMethod codeBase="{assembly}" /></UnitTest></TestDefinitions></TestRun>')
        return path

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

    def test_missing_tests_cannot_be_reported_as_success(self):
        with self.assertRaises(ValueError):
            maintenance.verify_tests(self.root, self.row)
        extension = next(row for row in self.register['sources'] if row['product'] == 'extensions')
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

    def test_generated_source_requires_verified_embedded_bytes_even_when_wildcard_mapped(self):
        prefix = f"https://raw.githubusercontent.com/{self.row['source_repository']}/{self.row['commit']}/"
        checksum = hashlib.sha256(b'generated source').hexdigest()
        document = {'path': '/_/obj/private-secret-host.g.cs', 'algorithm': 'sha256',
                    'checksum': checksum, 'embedded_checksum': checksum}
        details = {'source_link': {'documents': {'/_/*': prefix + '*'}}, 'documents': [document]}
        result = maintenance.verify_documents(details, maintenance.ROOT, self.row)
        self.assertEqual(result, [{'path': '[embedded]/document-1', 'source': 'embedded',
                                  'algorithm': 'sha256', 'checksum': checksum}])
        self.assertNotIn('private-secret-host', json.dumps(result))
        self.assertEqual(maintenance.verify_documents(dict(details, documents=[dict(document, path='/unmapped/private-secret-host.g.cs')]),
            maintenance.ROOT, self.row), result)
        for embedded in [None, '0' * 64]:
            with self.subTest(embedded=embedded), self.assertRaisesRegex(ValueError, 'verified embedded'):
                maintenance.verify_documents(dict(details, documents=[dict(document, embedded_checksum=embedded)]),
                                             maintenance.ROOT, self.row)
        original = subprocess.run(['git', 'show', self.row['commit'] + ':Directory.Build.props'],
                                  cwd=maintenance.ROOT, check=True, capture_output=True).stdout
        tracked = dict(document, path='/_/Directory.Build.props')
        self.assertNotEqual(checksum, hashlib.sha256(original).hexdigest())
        with self.assertRaisesRegex(ValueError, 'Tracked source checksum mismatch'):
            maintenance.verify_documents(dict(details, documents=[tracked]), maintenance.ROOT, self.row)

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
        row = next(row for row in self.register['sources'] if row['product'] == 'extensions')
        known = self.root / 'source/bin/Release/net8.0/Fixture.Tests.dll'
        private = '/private/secret-token/machine-host/Unknown.dll'
        for name, assembly in [('first', known), ('second', known), ('third', private)]:
            self.write_trx_fixture('extensions', name, assembly)
        context = {}
        with self.assertRaises(ValueError):
            maintenance.verify_tests(self.root, row, context)
        cell = {'project': 'Fixture.Tests.csproj', 'framework': 'net8.0'}
        self.assertEqual(context, {'expected_cells': [cell],
            'admitted_observed_cells': [cell | {'occurrences': 2}], 'unknown_path_count': 1,
            'duplicate_cell_count': 1, 'positive_summary_count': 3, 'summary_count': 3})
        retained = json.dumps(context)
        for value in [str(self.root), 'secret-token', 'machine-host', 'Unknown.dll', '/private']:
            self.assertNotIn(value, retained)

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

    def write_package_fixture(self, dependency='3.8.4', packed_version='3.8.4-proof.42.1', frameworks=()):
        artifacts = self.root / 'artifacts'; artifacts.mkdir(exist_ok=True)
        nuspec = f'''<package><metadata><id>Elsa.Studio.Fixture</id><version>{packed_version}</version>
          <repository type="git" url="https://github.com/elsa-workflows/elsa-studio" commit="{self.row['commit']}" />
          <dependencies><group targetFramework="net8.0"><dependency id="Elsa.Api.Client" version="{dependency}" /></group></dependencies>
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

    def test_package_identity_dependencies_and_complete_inventory_are_verified(self):
        policy = [{'id': 'Elsa.Studio.Fixture', 'assembly_name': 'Elsa.Studio.Fixture',
                   'frameworks': ['net8.0'], 'include_build_output': False, 'symbols': False, 'satellites': []}]
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
                   'frameworks': frameworks, 'include_build_output': True, 'symbols': True, 'satellites': []}]
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
                row = next(row for row in self.register['sources'] if row['product'] == product)
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
                ('secret-password /private/runner-host-42', 'unknown-check-failure')]):
            output = self.root / f'proof-{index}'
            with patch.object(maintenance, 'verify_source', side_effect=ValueError(message)):
                with self.assertRaises(ValueError):
                    maintenance.prepare(maintenance.ROOT, self.row, '3.8.4-proof.42.1', output)
            receipt = json.loads((output / 'receipt.json').read_text())
            self.assertEqual(receipt['error']['reason'], expected)
            self.assertNotIn(message, json.dumps(receipt))
            self.assertNotIn('/private/runner-host', json.dumps(receipt))
