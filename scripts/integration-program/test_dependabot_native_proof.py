"""Cheap synthetic receipt tests; these do not execute the native updater."""
import contextlib
import io
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest
from unittest import mock

from dependabot_native_proof import (DeadlineRunner, NativeProgress, diagnostic_complete, reconcile,
    units_from_configuration, unit_projects, reconcile_enumeration, probe_workspaces, probe_readback,
    safe_tool_result, child_environment, read_closed_json, digest, recheck_content, inventory,
    product_smoke_readback, smoke_native, smoke_project, smoke_can_continue, SMOKE_PROJECTS, SMOKE_TFMS,
    UNIT_IDS, UNIT_SELECTORS, UNIT_COUNTS, RETAINED, TOOL_MANIFEST)


class NativeProofReceiptTests(unittest.TestCase):
    def test_timeout_kills_descendant_and_retains_only_safe_progress(self):
        with tempfile.TemporaryDirectory() as directory:
            state = Path(directory).resolve()
            parent = "import os,signal; from pathlib import Path; signal.signal(signal.SIGTERM, signal.SIG_IGN); pid=os.fork(); Path('descendant.pid').write_text(str(os.getpid())) if pid==0 else print('SECRET https://private',flush=True); signal.pause()"
            summary = {'acceptance': False, 'stages': []}
            output = io.StringIO()
            started = time.monotonic()
            with contextlib.redirect_stdout(output):
                runner = DeadlineRunner(state, summary, budget_seconds=3, grace_seconds=0.1,
                                        progress_seconds=0.1, reserve_seconds=0)
                runner.native_progress = NativeProgress(state, {'A.csproj': 'Directory.Packages.props'})
                parent = "print('ELSA8636_WORKSPACE_0_STARTED',flush=True); "+parent
                with self.assertRaisesRegex(ValueError, '^overall_deadline$'):
                    runner.run('native_discovery', [sys.executable, '-c', parent])
            self.assertLess(time.monotonic() - started, 8)
            receipt = json.loads((state/'summary.json').read_text())
            self.assertEqual('timed_out', receipt['stages'][0]['state'])
            self.assertFalse(receipt['acceptance'])
            self.assertEqual(0, receipt['native_progress']['workspace_index'])
            self.assertEqual('started', receipt['native_progress']['phase'])
            self.assertNotIn('SECRET', output.getvalue())
            self.assertNotIn('https://', json.dumps(receipt))
            self.assertGreater(len(output.getvalue().splitlines()), 1)
            pid = (state/'descendant.pid').read_text()
            status = subprocess.run(['ps', '-o', 'stat=', '-p', pid], capture_output=True, text=True)
            self.assertTrue(not status.stdout.strip() or status.stdout.strip().startswith('Z'))
            with contextlib.redirect_stdout(output), self.assertRaisesRegex(ValueError, '^overall_deadline$'):
                runner.run('caller_build', [sys.executable, '-c', 'raise SystemExit(0)'])
            self.assertEqual('not_started_deadline', summary['stages'][-1]['state'])

    def test_native_markers_are_closed_and_unknown_lines_cannot_leak(self):
        with tempfile.TemporaryDirectory() as directory:
            state = Path(directory).resolve()
            project = 'nested/A.csproj'
            progress = NativeProgress(state, {project: 'Directory.Packages.props'})
            absolute = str(state.resolve()/project)
            stamp = '2026/10/11 01:00:00 INFO '
            valid = stamp+'Performing individual restores for project '+absolute+' using target frameworks net8.0, net9.0, net10.0'
            output = state/'native_discovery.out'
            self.assertEqual({'phase': 'single_restore_selected', 'project': project},
                             progress.parse(stamp+'Performing single restore for project '+absolute))
            output.write_text('ELSA8636_WORKSPACE_0_STARTED\n'+valid+'\n')
            observed = progress.poll(output, 1)
            self.assertEqual({'workspace_index': 0, 'phase': 'individual_restores_selected',
                              'project': project, 'observed_seconds': 1}, observed)
            for line in ['SECRET https://private', stamp+'Performing single restore for project /SECRET.csproj',
                         valid+' SECRET https://private', 'PREFIX '+valid, valid+'\x00',
                         stamp+'Performing single restore for project '+absolute+'\rSECRET',
                         'ELSA8636_WORKSPACE_2_STARTED',
                         'ELSA8636_WORKSPACE_1_STARTED SECRET', stamp+'Performing single restore for project '+absolute+'/../SECRET']:
                with self.subTest(line=line):
                    self.assertIsNone(progress.parse(line))
            with output.open('a') as stream:
                stream.write('SECRET https://private\n')
            self.assertEqual(observed, progress.poll(output, 2))
            with output.open('a') as stream:
                stream.write('ELSA8636_WORKSPACE_0_RETURNED\nELSA8636_WORKSPACE_1_STARTED\n')
            self.assertEqual({'workspace_index': 1, 'phase': 'started', 'observed_seconds': 3}, progress.poll(output, 3))
            self.assertNotIn('SECRET', json.dumps(progress.last))
            output.write_text('SECRET'*20000+'\n')
            self.assertEqual(1, progress.poll(output, 4)['workspace_index'])
            output.unlink()
            output.symlink_to(state/'missing')
            self.assertEqual(1, progress.poll(output, 5)['workspace_index'])

    def test_deadline_after_native_work_cannot_skip_content_rechecks(self):
        summary = {'reconciliation': {'complete': True}, 'content_recheck': 'not_checked_deadline'}
        self.assertFalse(diagnostic_complete(summary))
        summary.update(target_content_recheck='checked', changed_target_files=[])
        self.assertFalse(diagnostic_complete(summary))
        summary.update(upstream_content_recheck='checked', changed_upstream_files=[])
        self.assertFalse(diagnostic_complete(summary))
        summary.pop('content_recheck')
        self.assertTrue(diagnostic_complete(summary))

    def test_missing_and_failed_records_remain_visible_without_error_values(self):
        expected = {'core/A.csproj': 'Directory.Packages.props',
                    'extensions/src/Retained/Retained.csproj': 'extensions/src/Directory.Packages.props'}
        workspaces = [{'Path': '', 'IsSuccess': False,
                       'Error': {'error-type': 'private_source_authentication_failure',
                                 'error-details': {'message': 'SECRET https://private/token'}},
                       'Projects': [{'FilePath': 'core/A.csproj', 'IsSuccess': True,
                                     'ImportedFiles': ['../Directory.Packages.props'],
                                     'TargetFrameworks': ['net10.0'],
                                     'Dependencies': [{'Name': 'Known', 'Version': '1.2.3'},
                                                      {'Name': 'SECRET', 'Version': 'https://private'}]}]}]
        result = reconcile(expected, workspaces, {'Directory.Packages.props': {'Known': {'1.2.3'}}}, set(expected) | {'Directory.Packages.props'})
        self.assertEqual('omitted', result['projects'][1]['status'])
        self.assertEqual('workspace_failed', result['projects'][0]['status'])
        self.assertFalse(result['complete'])
        encoded = json.dumps(result)
        self.assertNotIn('SECRET', encoded)
        self.assertNotIn('https://', encoded)
        self.assertEqual(['private_source_authentication_failure'], result['workspaces'][0]['errors'])

    def test_success_requires_central_import_tfms_and_versions_and_retains_empty(self):
        expected = {'A.csproj': 'Directory.Packages.props', 'B.csproj': 'Directory.Packages.props'}
        native = {'Path': '', 'IsSuccess': True, 'Error': None,
                  'Projects': [{'FilePath': 'A.csproj', 'IsSuccess': True, 'Error': None,
                                'ImportedFiles': ['Directory.Packages.props'], 'TargetFrameworks': ['net8.0'],
                                'Dependencies': [{'Name': 'Known', 'Version': '1.2.3'}]},
                               {'FilePath': 'B.csproj', 'IsSuccess': True, 'Error': None,
                                'ImportedFiles': [], 'TargetFrameworks': [], 'Dependencies': []}]}
        result = reconcile(expected, [native], {'Directory.Packages.props': {'Known': {'1.2.3'}}}, set(expected) | {'Directory.Packages.props'})
        self.assertEqual(['observed', 'incomplete_metadata'], [p['status'] for p in result['projects']])
        self.assertFalse(result['complete'])
        self.assertEqual(0, result['projects'][1]['dependency_count'])

    def test_nested_and_retained_project_relative_central_paths(self):
        cases = [('', 'core/src/modules/Foo/Foo.csproj',
                  'core/src/modules/Foo/Foo.csproj', 'Directory.Packages.props',
                  '../../../../Directory.Packages.props'),
                 ('extensions/src/Elsa.Testing.Extensions', 'Elsa.Testing.Extensions.csproj',
                  'extensions/src/Elsa.Testing.Extensions/Elsa.Testing.Extensions.csproj',
                  'extensions/src/Directory.Packages.props', '../Directory.Packages.props')]
        for workspace, file_path, project, central, relative_central in cases:
            for special_only in (False, True):
                with self.subTest(workspace=workspace, special_only=special_only):
                    native = {'Path': workspace, 'IsSuccess': True, 'Projects': [{
                        'FilePath': file_path, 'IsSuccess': True,
                        'ImportedFiles': [] if special_only else [relative_central, '../../../../../Secrets.props'],
                        'PackageManagementSpecialFileRelativePath': relative_central,
                        'TargetFrameworks': ['net10.0'],
                        'Dependencies': [{'Name': 'Known', 'Version': '1.2.3'}]}]}
                    result = reconcile({project: central}, [native],
                                       {central: {'Known': {'1.2.3'}}}, {project, central, 'Secrets.props'})
                    row = result['projects'][0]
                    self.assertEqual('observed', row['status'])
                    self.assertTrue(result['complete'])
                    self.assertEqual(central, row['central_file'])
                    self.assertEqual([] if special_only else [central], row['imported_files'])

    def test_version_from_another_product_does_not_satisfy_central_join(self):
        native = {'Path': '', 'IsSuccess': True, 'Projects': [{
            'FilePath': 'A.csproj', 'IsSuccess': True, 'ImportedFiles': ['Directory.Packages.props'],
            'TargetFrameworks': ['net10.0'], 'Dependencies': [{'Name': 'Known', 'Version': '2.0.0'}]}]}
        versions = {'Directory.Packages.props': {'Known': {'1.2.3'}},
                    'studio/src/Directory.Packages.props': {'Known': {'2.0.0'}}}
        result = reconcile({'A.csproj': 'Directory.Packages.props'}, [native], versions,
                           {'A.csproj', 'Directory.Packages.props'})
        self.assertEqual('version_requires_review', result['projects'][0]['status'])
        self.assertFalse(result['complete'])

    def test_duplicate_or_escaping_record_cannot_certify_coverage(self):
        native = {'Path': '', 'IsSuccess': True, 'Projects': [
            {'FilePath': '../A.csproj'}, {'FilePath': 'A.csproj'}, {'FilePath': 'A.csproj'}]}
        result = reconcile({'A.csproj': 'Directory.Packages.props'}, [native], {}, {'A.csproj'})
        self.assertFalse(result['complete'])
        self.assertEqual('duplicate', result['projects'][0]['status'])
        self.assertEqual(1, result['invalid_record_count'])


class NativeBatchAdapterTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.state = Path(self.temporary.name).resolve()
        self.expected = {}
        for prefix, count in zip(('core/src', 'core/test', 'extensions', 'studio', 'build'), UNIT_COUNTS):
            for index in range(count):
                self.expected[f'{prefix}/Elsa.Fixture{index}/Fixture.csproj'] = 'Directory.Packages.props'
        replaced = next(path for path in self.expected if path.startswith('extensions/'))
        self.expected[RETAINED] = self.expected.pop(replaced)
        self.tracked = set(self.expected) | {TOOL_MANIFEST, 'Elsa.sln', 'Directory.Packages.props'}

    def write(self, name, value):
        (self.state/name).write_text(json.dumps(value))

    def enumerate_fixture(self):
        for index, (unit, paths) in enumerate(unit_projects(self.expected).items()):
            directories = sorted({str(Path(path).parent) for path in paths})
            if unit == 'tooling':
                directories = ['build', '.config']
                project = paths[0]
                self.expected['build/_build.csproj'] = self.expected.pop(project)
                self.tracked.remove(project)
                self.tracked.add('build/_build.csproj')
                paths = ['build/_build.csproj']
            self.write(f'enumeration-{index}.json', {'Id': unit,
                'Directories': ['/'+path for path in directories], 'EntryPoints': paths})

    def test_fixed_schema_derives_selectors_and_rejects_mutated_pin_or_topology(self):
        data = ''.join('  - package-ecosystem: "nuget"\n    directories:\n'+
            ''.join(f'      - "{path}"\n' for path in selectors)+'    target-branch: "main"\n'
            for selectors in UNIT_SELECTORS).encode()
        blob = hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()
        with self.assertRaisesRegex(ValueError, '^configuration_pin_mismatch$'):
            units_from_configuration(data)
        with mock.patch('dependabot_native_proof.CONFIGURATION_BLOB', blob):
            units = units_from_configuration(data)
            self.assertEqual(list(UNIT_IDS), [unit['Id'] for unit in units])
            self.assertEqual(list(UNIT_SELECTORS), [unit['Selectors'] for unit in units])
        unsafe = data.replace(b'/core/src/**/*', b'/')
        blob = hashlib.sha1(b'blob '+str(len(unsafe)).encode()+b'\0'+unsafe).hexdigest()
        with mock.patch('dependabot_native_proof.CONFIGURATION_BLOB', blob):
            with self.assertRaisesRegex(ValueError, '^configuration_schema_mismatch$'):
                units_from_configuration(unsafe)

    def test_native_enumeration_reconciles_all_five_units_without_root_solution(self):
        self.enumerate_fixture()
        result = reconcile_enumeration(self.state, self.expected, self.tracked)
        self.assertTrue(result['complete'])
        self.assertEqual(356, sum(len(row['entry_projects']) for row in result['units']))
        self.assertEqual(['build', '.config'], result['units'][-1]['directories'])
        self.write('enumeration-0.json', {'Id': 'core-production', 'Directories': ['/'], 'EntryPoints': ['Elsa.sln']})
        result = reconcile_enumeration(self.state, self.expected, self.tracked)
        self.assertFalse(result['complete'])
        self.assertNotIn('"directories": ["/"]', json.dumps(result))

    def test_nested_solution_or_unmodeled_entrypoint_is_a_gap(self):
        self.enumerate_fixture()
        path = next(path for path in self.expected if path.startswith('core/src/'))
        nested = str(Path(path).with_name('Nested.slnx'))
        self.tracked.add(nested)
        raw = json.loads((self.state/'enumeration-0.json').read_text())
        raw['EntryPoints'].append(nested)
        self.write('enumeration-0.json', raw)
        result = reconcile_enumeration(self.state, self.expected, self.tracked)
        self.assertFalse(result['complete'])
        self.assertEqual([nested], result['units'][0]['other_entrypoint_gaps'])

    def test_probe_modes_are_bounded_and_never_select_root_or_all_products(self):
        self.assertEqual([], probe_workspaces('enumerate'))
        self.assertEqual(['build', '.config'], probe_workspaces('tooling'))
        self.assertEqual(['extensions/src/Elsa.Testing.Extensions'], probe_workspaces('retained-probe'))
        for mode in ('all', '/', 'SECRET https://private'):
            with self.assertRaisesRegex(ValueError, '^unknown_mode$'):
                probe_workspaces(mode)

    def test_incremental_return_readback_preserves_full_manifest_and_rejects_unsafe_fields(self):
        workspace = 'extensions/src/Elsa.Testing.Extensions'
        self.write('workspace-0.call.json', {'Index': 0, 'Workspace': '/'+workspace,
                   'State': 'returned', 'ElapsedSeconds': 2.5})
        self.write('workspace-0.json', {'Path': workspace, 'IsSuccess': True, 'Projects': []})
        result = probe_readback(self.state, 'retained-probe', self.expected, {}, self.tracked)
        self.assertEqual(356, len(result['reconciliation']['projects']))
        self.assertTrue(result['all_calls_returned'])
        self.assertFalse(result['reconciliation']['complete'])
        self.assertEqual('omitted', next(row for row in result['reconciliation']['projects'] if row['path'] == RETAINED)['status'])
        self.write('workspace-0.call.json', {'Index': 0, 'Workspace': 'SECRET https://private',
                   'State': 'returned', 'ElapsedSeconds': float('nan')})
        self.write('workspace-0.json', {'Path': workspace, 'Projects': [{'Dependencies': None}]})
        result = probe_readback(self.state, 'retained-probe', self.expected, {}, self.tracked)
        self.assertFalse(result['all_calls_returned'])
        self.assertNotIn('SECRET', json.dumps(result))
        self.assertNotIn('https://', json.dumps(result))

    def test_reserved_finalization_can_read_completed_call_after_work_deadline(self):
        summary = {'stages': []}
        runner = DeadlineRunner(self.state, summary, budget_seconds=30, reserve_seconds=20)
        runner.deadline = time.monotonic()-1
        self.assertGreater(runner.finalization_deadline, time.monotonic())
        self.write('workspace-0.call.json', {'Index': 0, 'Workspace': '/build', 'State': 'returned', 'ElapsedSeconds': 1})
        self.write('workspace-0.json', {'Path': 'build', 'IsSuccess': True, 'Projects': []})
        result = probe_readback(self.state, 'tooling', self.expected, {}, self.tracked)
        self.assertEqual('returned', result['calls'][0]['state'])
        self.assertEqual('not_read', result['calls'][1]['state'])
        self.assertFalse(result['all_calls_returned'])

    def test_incremental_progress_reads_each_complete_marker_once_and_reports_truncation(self):
        progress = NativeProgress(self.state, {}, workspace_count=3)
        path = self.state/'native.out'
        path.write_text('ELSA8636_WORKSPACE_2_STARTED\nELSA8636_WORKSPACE_2_RETURNED\n')
        progress.poll(path, 1)
        progress.poll(path, 2)
        self.assertEqual(2, len(progress.events))
        self.assertEqual('returned', progress.last['phase'])
        path.write_text('SECRET\n')
        progress.poll(path, 3)
        self.assertEqual(1, progress.gaps)
        self.assertNotIn('SECRET', json.dumps(progress.events))
        self.assertIsNone(progress.parse('ELSA8636_WORKSPACE_3_RETURNED'))

    def test_root_tool_schema_retains_only_known_id_valid_version_and_manifest(self):
        raw = {'Path': '.config', 'DotNetToolsJson': {'FilePath': 'dotnet-tools.json',
            'IsSuccess': True, 'Dependencies': [{'Name': 'dotnet-ef', 'Version': '9.0.12'},
                {'Name': 'SECRET', 'Version': 'https://private'}]}}
        result = safe_tool_result([raw], '9.0.12')
        self.assertEqual('observed', result['status'])
        self.assertEqual('9.0.12', result['observations'][0]['version'])
        self.assertTrue(result['workspaces'][0]['matches_target_version'])
        self.assertFalse(safe_tool_result([raw], '9.0.13')['workspaces'][0]['matches_target_version'])
        self.assertNotIn('SECRET', json.dumps(result))
        raw['DotNetToolsJson']['Dependencies'][0]['Version'] = 'https://private'
        self.assertEqual('not_observed', safe_tool_result([raw], '9.0.12')['status'])

    def test_child_environment_excludes_credentials_and_uses_private_caches(self):
        with mock.patch.dict(os.environ, {'GH_TOKEN': 'SECRET', 'NUGET_AUTH_TOKEN': 'SECRET',
                'NuGetPackageSourceCredentials_Feedz': 'SECRET', 'HOME': '/user',
                'DOTNET_CLI_HOME': '/user', 'NUGET_PACKAGES': '/user'}):
            environment = child_environment(self.state)
        self.assertFalse(set(environment) & {'GH_TOKEN', 'NUGET_AUTH_TOKEN', 'NuGetPackageSourceCredentials_Feedz'})
        for name in ('HOME', 'DOTNET_CLI_HOME', 'NUGET_PACKAGES'):
            path = Path(environment[name])
            self.assertTrue(path.is_relative_to(self.state))
            self.assertEqual(0o700, path.stat().st_mode & 0o777)

    def test_timeout_final_readback_keeps_completed_call_even_without_returned_process(self):
        summary = {'stages': [], 'acceptance': False}
        runner = DeadlineRunner(self.state, summary, budget_seconds=1, reserve_seconds=0.7,
                                grace_seconds=0.05, progress_seconds=1)
        runner.readback = lambda: summary.update(probe=probe_readback(
            self.state, 'retained-probe', self.expected, {}, self.tracked))
        script = "import json,signal; from pathlib import Path; Path('workspace-0.call.json').write_text(json.dumps({'Index':0,'Workspace':'/extensions/src/Elsa.Testing.Extensions','State':'returned','ElapsedSeconds':0.1})); signal.pause()"
        with contextlib.redirect_stdout(io.StringIO()), self.assertRaisesRegex(ValueError, '^overall_deadline$'):
            runner.run('native_discovery', [sys.executable, '-c', script])
        receipt = json.loads((self.state/'summary.json').read_text())
        self.assertEqual('returned', receipt['probe']['calls'][0]['state'])
        self.assertEqual(356, len(receipt['probe']['reconciliation']['projects']))
        self.assertFalse(receipt['probe']['reconciliation']['complete'])
        self.assertFalse(receipt['acceptance'])

    def test_oversized_unknown_line_cannot_turn_its_suffix_into_a_marker(self):
        progress = NativeProgress(self.state, {})
        path = self.state/'native.out'
        path.write_text('SECRET'*1000+'ELSA8636_WORKSPACE_0_RETURNED')
        progress.poll(path, 1)
        with path.open('a') as stream:
            stream.write('\nELSA8636_WORKSPACE_0_STARTED\n')
        progress.poll(path, 2)
        self.assertEqual(['started'], [event['phase'] for event in progress.events])
        self.assertEqual(1, progress.gaps)

    def test_json_hash_and_progress_reject_linked_files_and_parent_directories(self):
        physical = self.state/'physical'
        physical.mkdir()
        record = physical/'record.json'
        record.write_text('{"value":1}')
        log = physical/'native.out'
        log.write_text('ELSA8636_WORKSPACE_0_RETURNED\n')
        linked_file = self.state/'linked.json'
        linked_file.symlink_to(record)
        linked_parent = self.state/'linked-parent'
        linked_parent.symlink_to(physical, target_is_directory=True)
        for path in (linked_file, linked_parent/'record.json'):
            with self.subTest(path=path.name):
                with self.assertRaises((OSError, ValueError)):
                    read_closed_json(path, self.state)
                with self.assertRaises((OSError, ValueError)):
                    digest(path, self.state)
        linked_log = self.state/'linked.out'
        linked_log.symlink_to(log)
        for path in (linked_log, linked_parent/'native.out'):
            progress = NativeProgress(self.state, {})
            self.assertIsNone(progress.poll(path, 1))
            self.assertEqual([], progress.events)
            self.assertEqual(1, progress.gaps)
        self.assertEqual({'value': 1}, read_closed_json(record, self.state))

    def test_same_content_link_replacement_and_missing_files_fail_final_rechecks(self):
        directory = self.state/'source'
        directory.mkdir()
        path = directory/'known.txt'
        path.write_text('same content')
        before = {'source/known.txt': digest(path, self.state)}
        self.assertEqual(('checked', []), recheck_content(self.state, before, lambda: True))
        target = self.state/'same.txt'
        target.write_text('same content')
        path.unlink()
        path.symlink_to(target)
        self.assertEqual(('checked', ['source/known.txt']), recheck_content(self.state, before, lambda: True))
        path.unlink()
        directory.rmdir()
        replacement = self.state/'replacement'
        replacement.mkdir()
        (replacement/'known.txt').write_text('same content')
        directory.symlink_to(replacement, target_is_directory=True)
        self.assertEqual(('checked', ['source/known.txt']), recheck_content(self.state, before, lambda: True))
        directory.unlink()
        self.assertEqual(('checked', ['source/known.txt']), recheck_content(self.state, before, lambda: True))
        self.assertEqual(('not_checked_deadline', []), recheck_content(self.state, before, lambda: False))

    def test_inventory_rejects_internal_link_before_census(self):
        actual = self.state/'actual'
        actual.mkdir()
        (actual/'A.csproj').write_text('<Project/>')
        (self.state/'alias').symlink_to(actual, target_is_directory=True)
        with self.assertRaisesRegex(ValueError, '^snapshot_symlink_escape$'):
            inventory(self.state)


class ProductSmokeTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.state = Path(temporary.name).resolve()
        self.snapshot = self.state/'repository'
        self.snapshot.mkdir()
        self.expected = {project: prefix+'/Directory.Packages.props' for project, prefix in
                         zip(SMOKE_PROJECTS, ('extensions/src', 'studio/src'))}
        self.reference = 'core/src/Fixture/Fixture.csproj'
        self.tracked = set(SMOKE_PROJECTS) | set(self.expected.values()) | {self.reference}
        self.versions = {central: {'Conditional': {'8.0.1', '9.0.1', '10.0.1'}}
                         for central in self.expected.values()}
        for index, project in enumerate(SMOKE_PROJECTS):
            parent = str(Path(project).parent)
            central = self.expected[project]
            relative_central = os.path.relpath(central, parent)
            relative_reference = os.path.relpath(self.reference, parent)
            dependencies = [{'Name': 'Conditional', 'Version': tfm[3:]+'.1',
                             'TargetFrameworks': [tfm], 'Type': 'PackageReference', 'IsTopLevel': True}
                            for tfm in SMOKE_TFMS]
            self.write(f'workspace-{index}.json', {'Path': parent, 'IsSuccess': True, 'Projects': [{
                'FilePath': Path(project).name, 'IsSuccess': True, 'TargetFrameworks': list(SMOKE_TFMS),
                'ImportedFiles': [relative_central], 'ReferencedProjectPaths': [relative_reference],
                'PackageManagementSpecialFileRelativePath': relative_central, 'Dependencies': dependencies}]})
            for tfm in SMOKE_TFMS:
                defining = str(self.snapshot/central)
                self.write(f'evaluation-{index}-{tfm}.json', {'Properties': {
                    'TargetFramework': tfm, 'TargetFrameworks': ';'.join(SMOKE_TFMS),
                    'ManagePackageVersionsCentrally': 'true', 'DirectoryPackagesPropsPath': defining},
                    'Items': {'PackageVersion': [{'Identity': 'Conditional', 'Version': tfm[3:]+'.1',
                                                 'DefiningProjectFullPath': defining}],
                              'PackageReference': [{'Identity': 'Conditional', 'DefiningProjectFullPath': str(self.snapshot/project)}],
                              'GlobalPackageReference': [], 'ProjectReference': [{'FullPath': str(self.snapshot/self.reference)}]}})

    def write(self, name, value):
        (self.state/name).write_text(json.dumps(value))

    def readback(self):
        return product_smoke_readback(self.state, self.snapshot, self.expected, self.tracked, self.versions)

    def mutate(self, name, change):
        path = self.state/name
        value = json.loads(path.read_text())
        change(value)
        self.write(name, value)

    def test_conditional_versions_are_joined_by_tfm_and_reference_edges_retained(self):
        result = self.readback()
        self.assertTrue(result['semantic_preflight_complete'])
        self.assertFalse(result['acceptance'])
        self.assertEqual((356, 173), (result['selected_denominator'], result['product_denominator']))
        record = result['projects'][0]['native']['records'][0]
        self.assertEqual([self.reference], record['references'])
        self.assertEqual(SMOKE_TFMS, tuple(dep['TargetFrameworks'][0] for dep in record['dependencies']))
        self.mutate('evaluation-0-net8.0.json', lambda value: value['Items']['PackageVersion'][0].update(Version='9.0.1'))
        result = self.readback()
        self.assertFalse(result['semantic_preflight_complete'])
        self.assertIn({'tfm': 'net8.0', 'kind': 'native_version_attribution', 'id': 'Conditional'}, result['projects'][0]['gaps'])

    def test_missing_empty_failed_or_wrong_workspace_never_passes(self):
        for change in (lambda value: value.update(Projects=[]),
                       lambda value: value.update(IsSuccess=False),
                       lambda value: value.update(Path='SECRET https://private')):
            original = (self.state/'workspace-0.json').read_text()
            self.mutate('workspace-0.json', change)
            result = self.readback()
            self.assertFalse(result['semantic_preflight_complete'])
            self.assertNotIn('SECRET', json.dumps(result))
            (self.state/'workspace-0.json').write_text(original)
        (self.state/'evaluation-0-net9.0.json').unlink()
        self.assertFalse(self.readback()['semantic_preflight_complete'])

    def test_cpm_applicability_and_ambiguous_overrides_are_not_literal_matches(self):
        for change in (lambda value: value['Properties'].update(ManagePackageVersionsCentrally='false'),
                       lambda value: value['Properties'].update(TargetFramework='net7.0'),
                       lambda value: value['Items']['PackageReference'][0].update(Version='8.0.1')):
            original = (self.state/'evaluation-0-net8.0.json').read_text()
            self.mutate('evaluation-0-net8.0.json', change)
            self.assertFalse(self.readback()['semantic_preflight_complete'])
            (self.state/'evaluation-0-net8.0.json').write_text(original)

    def test_exact_pair_and_mode_admission_with_no_arbitrary_workspace(self):
        self.assertEqual(list(map(lambda path: str(Path(path).parent), SMOKE_PROJECTS)), probe_workspaces('product-smoke'))
        for index in (-1, 2, False, '0', '/SECRET'):
            with self.assertRaisesRegex(ValueError, '^unknown_smoke_project$'):
                smoke_project(index)
        self.assertEqual(SMOKE_PROJECTS[1], smoke_project(1))
        with self.assertRaisesRegex(ValueError, '^unknown_mode$'):
            probe_workspaces('product-smoke /SECRET')

    def test_closure_records_and_duplicate_reference_edges_are_not_silently_dropped(self):
        self.mutate('workspace-0.json', lambda value: value['Projects'].append({
            'FilePath': os.path.relpath(self.reference, Path(SMOKE_PROJECTS[0]).parent),
            'IsSuccess': True, 'TargetFrameworks': ['net10.0'], 'ImportedFiles': [],
            'ReferencedProjectPaths': [], 'Dependencies': []}))
        result = self.readback()
        self.assertEqual(self.reference, result['projects'][0]['native']['records'][1]['path'])
        self.assertTrue(result['semantic_preflight_complete'])
        self.mutate('workspace-0.json', lambda value: value['Projects'][0]['ReferencedProjectPaths'].append(
            value['Projects'][0]['ReferencedProjectPaths'][0]))
        result = self.readback()
        self.assertFalse(result['semantic_preflight_complete'])
        self.assertIn({'tfm': 'net8.0', 'kind': 'reference_edges'}, result['projects'][0]['gaps'])

    def test_collapsed_native_conditional_version_remains_unresolved(self):
        self.mutate('workspace-0.json', lambda value: value['Projects'][0].update(
            Dependencies=value['Projects'][0]['Dependencies'][-1:]))
        result = self.readback()
        self.assertFalse(result['semantic_preflight_complete'])
        self.assertTrue(smoke_can_continue(result['projects'][0]))

    def test_unknown_reference_or_import_cannot_be_matched_or_continue(self):
        for key in ('ReferencedProjectPaths', 'ImportedFiles'):
            original = (self.state/'workspace-0.json').read_text()
            self.mutate('workspace-0.json', lambda value: value['Projects'][0][key].append('../../../../SECRET.csproj'))
            result = self.readback()
            self.assertFalse(result['semantic_preflight_complete'])
            self.assertNotEqual('observed', result['projects'][0]['native']['status'])
            self.assertFalse(smoke_can_continue(result['projects'][0]))
            self.assertNotIn('SECRET', json.dumps(result))
            (self.state/'workspace-0.json').write_text(original)

    def test_global_reference_and_evaluation_enum_are_preserved_without_raw_values(self):
        for tfm in SMOKE_TFMS:
            self.mutate(f'evaluation-0-{tfm}.json', lambda value: value['Items'].update(
                GlobalPackageReference=[dict(value['Items']['PackageReference'][0], Version=value['Items']['PackageVersion'][0]['Version'])],
                PackageReference=[]))
        self.mutate('workspace-0.json', lambda value: [dep.update(EvaluationResult={
            'ResultType': 'Success', 'OriginalValue': 'SECRET', 'EvaluatedValue': 'SECRET',
            'ErrorMessage': 'https://private'}) for dep in value['Projects'][0]['Dependencies']])
        result = self.readback()
        self.assertTrue(result['semantic_preflight_complete'])
        dependency = result['projects'][0]['native']['records'][0]['dependencies'][0]
        self.assertEqual({'present': True, 'result_type': 'Success'}, dependency['EvaluationResult'])
        self.assertNotIn('SECRET', json.dumps(result))
        self.assertNotIn('https://', json.dumps(result))
        self.mutate('workspace-0.json', lambda value: value['Projects'][0]['Dependencies'][0].update(
            EvaluationResult={'ResultType': 'SECRET'}))
        result = self.readback()
        self.assertFalse(result['semantic_preflight_complete'])
        self.assertFalse(smoke_can_continue(result['projects'][0]))
        self.assertNotIn('SECRET', json.dumps(result))


if __name__ == '__main__':
    unittest.main()
