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
    product_smoke_readback, smoke_native, smoke_project, smoke_can_continue, reference_output, SMOKE_PROJECTS, SMOKE_TFMS,
    writer_registration_guard, writer_admission, writer_changes, writer_events, writer_operations, run_writer_case,
    WRITER_PROJECT, WRITER_PACKAGE, WRITER_CENTRAL, WRITER_OLD,
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
            self.reference_fixture(index, self.reference)

    def reference_fixture(self, index, path, edges=None, frameworks=SMOKE_TFMS, aggregate=None):
        self.tracked.add(path)
        edges = edges or {tfm: [] for tfm in frameworks}
        references = aggregate if aggregate is not None else sorted({ref for refs in edges.values() for ref in refs})
        self.mutate(f'workspace-{index}.json', lambda value: value['Projects'].append({
            'FilePath': os.path.relpath(path, Path(SMOKE_PROJECTS[index]).parent),
            'IsSuccess': True, 'TargetFrameworks': list(frameworks), 'ImportedFiles': [],
            'ReferencedProjectPaths': [os.path.relpath(ref, Path(path).parent) for ref in references],
            'Dependencies': []}))
        multiple = ';'.join(frameworks) if len(frameworks) > 1 else ''
        self.write(reference_output(self.state, index, path), {'Properties': {
            'TargetFramework': '' if multiple else frameworks[0], 'TargetFrameworks': multiple}})
        for tfm in frameworks:
            self.write(reference_output(self.state, index, path, tfm), {'Properties': {
                'TargetFramework': tfm, 'TargetFrameworks': multiple}, 'Items': {
                'ProjectReference': [{'FullPath': str(self.snapshot/ref)} for ref in edges[tfm]]}})

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
        self.assertTrue(smoke_can_continue(result['projects'][0]))
        self.assertFalse(result['acceptance'])
        self.assertEqual((356, 173), (result['selected_denominator'], result['product_denominator']))
        record = result['projects'][0]['native']['records'][0]
        self.assertEqual([self.reference], record['references'])
        self.assertEqual(SMOKE_TFMS, tuple(dep['TargetFrameworks'][0] for dep in record['dependencies']))
        self.mutate('evaluation-0-net8.0.json', lambda value: value['Items']['PackageVersion'][0].update(Version='9.0.1'))
        result = self.readback()
        self.assertFalse(result['semantic_preflight_complete'])
        self.assertIn({'tfm': 'net8.0', 'kind': 'native_dependency_mismatch', 'id': 'Conditional'}, result['projects'][0]['gaps'])
        self.assertFalse(smoke_can_continue(result['projects'][0]))

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
        result = self.readback()
        self.assertEqual(self.reference, result['projects'][0]['native']['records'][1]['path'])
        self.assertTrue(result['semantic_preflight_complete'])
        self.mutate('workspace-0.json', lambda value: value['Projects'][0]['ReferencedProjectPaths'].append(
            value['Projects'][0]['ReferencedProjectPaths'][0]))
        result = self.readback()
        self.assertFalse(result['semantic_preflight_complete'])
        self.assertIn({'path': SMOKE_PROJECTS[0], 'kind': 'duplicate_native_reference'}, result['projects'][0]['gaps'])

    def test_collapsed_native_conditional_version_remains_unresolved(self):
        self.mutate('workspace-0.json', lambda value: value['Projects'][0].update(
            Dependencies=value['Projects'][0]['Dependencies'][-1:]))
        result = self.readback()
        self.assertFalse(result['semantic_preflight_complete'])
        self.assertTrue(smoke_can_continue(result['projects'][0]))

    def test_email_topology_uses_independent_direct_edges_not_native_self_consistency(self):
        # Synthetic property/item outputs shaped after the pinned Email source topology;
        # the original run artifact did not contain these independent closure evaluations.
        names = ['Elsa.Features', 'Elsa.Mediator', 'Elsa.Common', 'Elsa.Expressions',
                 'Elsa.Workflows.Core', 'Elsa.Api.Common', 'Elsa.Caching', 'Elsa.Workflows.Management']
        paths = {name: f"core/src/{'common' if name in names[:2]+['Elsa.Api.Common'] else 'modules'}/{name}/{name}.csproj"
                 for name in names}
        direct = [[], [], names[:2], ['Elsa.Features', 'Elsa.Common'],
                  ['Elsa.Common', 'Elsa.Expressions'], ['Elsa.Workflows.Core', 'Elsa.Features'],
                  ['Elsa.Features'], ['Elsa.Api.Common', 'Elsa.Caching', 'Elsa.Workflows.Core']]
        closures = {}
        for name, targets in zip(names, direct):
            reached = set(targets)
            for target in targets:
                reached.update(closures[target])
            closures[name] = reached
        self.mutate('workspace-0.json', lambda value: value.update(Projects=value['Projects'][:1]))
        for name, targets in zip(names, direct):
            self.reference_fixture(0, paths[name], {tfm: [paths[target] for target in targets] for tfm in SMOKE_TFMS},
                                   aggregate=[paths[target] for target in sorted(closures[name])])
        for tfm in SMOKE_TFMS:
            self.mutate(f'evaluation-0-{tfm}.json', lambda value: value['Items'].update(
                ProjectReference=[{'FullPath': str(self.snapshot/paths[names[-1]])}]))
        self.mutate('workspace-0.json', lambda value: value['Projects'][0].update(
            ReferencedProjectPaths=[os.path.relpath(path, Path(SMOKE_PROJECTS[0]).parent) for path in paths.values()]))
        result = self.readback()
        self.assertTrue(result['semantic_preflight_complete'])
        evidence = result['projects'][0]['reference_evidence']
        self.assertEqual(9, len(evidence['projects']))
        self.assertEqual('not_certified', evidence['native_duplicate_multiplicity'])
        self.assertEqual('not_exposed', evidence['native_reference_tfm_attribution'])
        for tfm in SMOKE_TFMS:
            self.mutate(reference_output(self.state, 0, paths[names[-1]], tfm),
                        lambda value: value['Items'].update(ProjectReference=[]))
        result = self.readback()
        self.assertFalse(result['semantic_preflight_complete'])
        self.assertFalse(smoke_can_continue(result['projects'][0]))

    def test_reference_stages_execute_and_refresh_closed_evidence(self):
        summary = {'stages': []}
        runner = DeadlineRunner(self.state, summary, budget_seconds=30, reserve_seconds=5)
        runner.readback = lambda: summary.update(product_smoke=self.readback())
        for name, tfm in (('reference_declaration', 'declared'), ('reference_evaluation', 'net8.0')):
            output = reference_output(self.state, 0, self.reference, tfm)
            payload = output.read_text()
            output.unlink()
            self.assertFalse(smoke_can_continue(self.readback()['projects'][0]))
            with contextlib.redirect_stdout(io.StringIO()):
                runner.run(name, [sys.executable, '-c', 'print('+repr(payload)+')'], output=output)
            self.assertEqual('passed', summary['stages'][-1]['state'])
            self.assertTrue(smoke_can_continue(summary['product_smoke']['projects'][0]))
        self.assertEqual(['reference_declaration', 'reference_evaluation'],
                         [stage['name'] for stage in summary['stages']])

    def test_tfm_conditioned_edges_and_native_framework_subset_keep_exact_closure(self):
        leaf = 'core/src/Leaf/Leaf.csproj'
        self.reference_fixture(0, leaf)
        self.mutate(reference_output(self.state, 0, self.reference, 'net10.0'),
                    lambda value: value['Items'].update(ProjectReference=[{'FullPath': str(self.snapshot/leaf)}]))
        self.mutate('workspace-0.json', lambda value: (
            value['Projects'][1].update(TargetFrameworks=['net8.0'],
                ReferencedProjectPaths=[os.path.relpath(leaf, Path(self.reference).parent)]),
            value['Projects'][0]['ReferencedProjectPaths'].append(os.path.relpath(leaf, Path(SMOKE_PROJECTS[0]).parent))))
        result = self.readback()
        self.assertTrue(result['semantic_preflight_complete'])
        reachable = result['projects'][0]['reference_evidence']['evaluated_reachability']
        self.assertNotIn(leaf, next(row['paths'] for row in reachable if row['path'] == SMOKE_PROJECTS[0] and row['tfm'] == 'net8.0'))
        self.assertIn(leaf, next(row['paths'] for row in reachable if row['path'] == SMOKE_PROJECTS[0] and row['tfm'] == 'net10.0'))

    def test_missing_unsafe_duplicate_or_overridden_closure_edges_block_progression(self):
        output = reference_output(self.state, 0, self.reference, 'net8.0')
        original = output.read_text()
        for items in ([{'FullPath': '/SECRET/Unsafe.csproj'}],
                      [{'FullPath': str(self.snapshot/self.reference)}],
                      [{'FullPath': str(self.snapshot/self.reference)}]*2,
                      [{'FullPath': str(self.snapshot/self.reference), 'SetTargetFramework': 'TargetFramework=net9.0'}],
                      [{'FullPath': str(self.snapshot/self.reference), 'AdditionalProperties': 'SECRET=https://private'}]):
            with self.subTest(items=items):
                self.mutate(output, lambda value: value['Items'].update(ProjectReference=items))
                result = self.readback()
                self.assertFalse(result['semantic_preflight_complete'])
                self.assertFalse(smoke_can_continue(result['projects'][0]))
                self.assertNotIn('SECRET', json.dumps(result))
                self.assertNotIn('https://', json.dumps(result))
                output.write_text(original)
        output.unlink()
        self.assertFalse(smoke_can_continue(self.readback()['projects'][0]))

    def test_missing_failed_duplicate_or_unexplained_native_closure_records_block_progression(self):
        original = (self.state/'workspace-0.json').read_text()
        for change in (lambda value: value.update(Projects=value['Projects'][:1]),
                       lambda value: value['Projects'][1].update(IsSuccess=False),
                       lambda value: value['Projects'].append(value['Projects'][1]),
                       lambda value: value['Projects'][0].update(ReferencedProjectPaths=[])):
            self.mutate('workspace-0.json', change)
            result = self.readback()
            self.assertFalse(result['semantic_preflight_complete'])
            self.assertFalse(smoke_can_continue(result['projects'][0]))
            (self.state/'workspace-0.json').write_text(original)
        self.reference_fixture(0, 'core/src/Unreachable/Unreachable.csproj')
        result = self.readback()
        self.assertFalse(smoke_can_continue(result['projects'][0]))
        self.assertTrue(any(gap['kind'] == 'unreachable_closure_record' for gap in result['projects'][0]['gaps']))

    def test_duplicate_direct_edge_to_valid_leaf_is_a_distinct_gap(self):
        leaf = 'core/src/Leaf/Leaf.csproj'
        self.reference_fixture(0, leaf)
        self.mutate(reference_output(self.state, 0, self.reference, 'net8.0'),
                    lambda value: value['Items'].update(ProjectReference=[{'FullPath': str(self.snapshot/leaf)}]*2))
        self.mutate('workspace-0.json', lambda value: (
            value['Projects'][1].update(ReferencedProjectPaths=[os.path.relpath(leaf, Path(self.reference).parent)]),
            value['Projects'][0]['ReferencedProjectPaths'].append(os.path.relpath(leaf, Path(SMOKE_PROJECTS[0]).parent))))
        row = self.readback()['projects'][0]
        self.assertFalse(smoke_can_continue(row))
        self.assertEqual([{'kind': 'duplicate_direct_reference', 'path': self.reference, 'tfm': 'net8.0'}], row['gaps'])

    def test_duplicate_frameworks_in_each_observation_remain_unresolved(self):
        duplicates = [SMOKE_TFMS[0], *SMOKE_TFMS]
        cases = (
            ('native_root', 'workspace-0.json',
             lambda value: value['Projects'][0].update(TargetFrameworks=duplicates)),
            ('native_closure', 'workspace-0.json',
             lambda value: value['Projects'][1].update(TargetFrameworks=duplicates)),
            ('root_evaluation', 'evaluation-0-net8.0.json',
             lambda value: value['Properties'].update(TargetFrameworks=';'.join(duplicates))),
            ('closure_evaluation', reference_output(self.state, 0, self.reference, 'net8.0'),
             lambda value: value['Properties'].update(TargetFrameworks=';'.join(duplicates))),
        )
        for name, output, change in cases:
            with self.subTest(observation=name):
                path = self.state/output
                original = path.read_text()
                try:
                    self.mutate(output, change)
                    result = self.readback()
                    self.assertFalse(result['semantic_preflight_complete'])
                    self.assertFalse(smoke_can_continue(result['projects'][0]))
                finally:
                    path.write_text(original)

    def test_unlike_framework_mapping_and_unsupported_declarations_remain_gaps(self):
        declaration = reference_output(self.state, 0, self.reference)
        original = declaration.read_text()
        for frameworks in ('net8.0;net8.0', 'SECRET', 'net7.0'):
            self.mutate(declaration, lambda value: value['Properties'].update(TargetFrameworks=frameworks))
            result = self.readback()
            self.assertFalse(smoke_can_continue(result['projects'][0]))
            self.assertNotIn('SECRET', json.dumps(result))
            declaration.write_text(original)
        self.mutate(declaration, lambda value: value['Properties'].update(TargetFrameworks='', TargetFramework='net10.0'))
        self.mutate('workspace-0.json', lambda value: value['Projects'][1].update(TargetFrameworks=['net10.0']))
        self.mutate(reference_output(self.state, 0, self.reference, 'net10.0'),
                    lambda value: value['Properties'].update(TargetFrameworks=''))
        result = self.readback()
        self.assertFalse(smoke_can_continue(result['projects'][0]))
        self.assertTrue(any(gap['kind'] == 'reference_framework_mapping' for gap in result['projects'][0]['gaps']))

    def test_extra_or_conflicting_known_top_level_rows_block_second_call(self):
        original = (self.state/'workspace-0.json').read_text()
        for versions in self.versions.values():
            versions['ExtraKnown'] = {'8.0.1'}
        for changes in ({'Version': '99.0.0'}, {}, {'Type': 'DotNetTool'},
                        {'TargetFrameworks': []}, {'TargetFrameworks': ['net8.0', 'net8.0']},
                        {'Name': 'ExtraKnown'}):
            with self.subTest(changes=changes):
                self.mutate('workspace-0.json', lambda value: value['Projects'][0]['Dependencies'].append(
                    dict(value['Projects'][0]['Dependencies'][0], **changes)))
                result = self.readback()
                self.assertFalse(result['semantic_preflight_complete'])
                self.assertFalse(smoke_can_continue(result['projects'][0]))
                (self.state/'workspace-0.json').write_text(original)

    def test_missing_unconditional_or_conflicting_attribution_blocks_second_call(self):
        for tfm in SMOKE_TFMS:
            self.mutate(f'evaluation-0-{tfm}.json', lambda value:
                value['Items']['PackageVersion'][0].update(Version='8.0.1'))
        self.mutate('workspace-0.json', lambda value: value['Projects'][0].update(
            Dependencies=value['Projects'][0]['Dependencies'][:1]))
        result = self.readback()
        self.assertFalse(result['semantic_preflight_complete'])
        self.assertFalse(smoke_can_continue(result['projects'][0]))

    def test_missing_entire_conditional_dependency_is_not_known_aggregate_loss(self):
        self.mutate('workspace-0.json', lambda value: value['Projects'][0].update(Dependencies=[]))
        result = self.readback()
        self.assertFalse(result['semantic_preflight_complete'])
        self.assertFalse(smoke_can_continue(result['projects'][0]))

    def test_only_sdk_package_reference_type_matches_direct_or_global_inputs(self):
        original = (self.state/'workspace-0.json').read_text()
        for item_kind in ('PackageReference', 'GlobalPackageReference'):
            if item_kind == 'GlobalPackageReference':
                for tfm in SMOKE_TFMS:
                    self.mutate(f'evaluation-0-{tfm}.json', lambda value: value['Items'].update(
                        GlobalPackageReference=[dict(value['Items']['PackageReference'][0],
                            Version=value['Items']['PackageVersion'][0]['Version'])], PackageReference=[]))
            self.assertTrue(self.readback()['semantic_preflight_complete'])
            for native_type in ('PackageVersion', 'GlobalPackageReference', 'DotNetTool'):
                with self.subTest(item_kind=item_kind, native_type=native_type):
                    self.mutate('workspace-0.json', lambda value: [dep.update(Type=native_type)
                        for dep in value['Projects'][0]['Dependencies']])
                    result = self.readback()
                    self.assertFalse(result['semantic_preflight_complete'])
                    self.assertFalse(smoke_can_continue(result['projects'][0]))
                    (self.state/'workspace-0.json').write_text(original)

    def test_known_non_success_evaluation_results_block_second_call_without_raw_values(self):
        original = (self.state/'workspace-0.json').read_text()
        for result_type in ('PropertyIgnored', 'CircularReference', 'PropertyNotFound'):
            with self.subTest(result_type=result_type):
                self.mutate('workspace-0.json', lambda value: [dep.update(EvaluationResult={
                    'ResultType': result_type, 'OriginalValue': 'SECRET', 'EvaluatedValue': 'SECRET',
                    'ErrorMessage': 'https://private'}) for dep in value['Projects'][0]['Dependencies']])
                result = self.readback()
                self.assertFalse(result['semantic_preflight_complete'])
                self.assertFalse(smoke_can_continue(result['projects'][0]))
                self.assertIn({'kind': 'native_evaluation_result', 'path': SMOKE_PROJECTS[0],
                               'id': 'Conditional'}, result['projects'][0]['gaps'])
                self.assertNotIn('SECRET', json.dumps(result))
                self.assertNotIn('https://', json.dumps(result))
                (self.state/'workspace-0.json').write_text(original)

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
        self.assertTrue(smoke_can_continue(result['projects'][0]))
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

class WriterSmokeContractTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.state = Path(self.temporary.name).resolve()
        self.snapshot = self.state/'repository'
        self.snapshot.mkdir()
        self.central = self.snapshot/WRITER_CENTRAL
        self.central.parent.mkdir(parents=True)
        self.central.write_text('<Project><ItemGroup><PackageVersion Include="Microsoft.AspNetCore.SignalR.Client" Version="9.0.13"/></ItemGroup></Project>')
        self.tracked = {WRITER_CENTRAL, WRITER_PROJECT}
        project = self.snapshot/WRITER_PROJECT
        project.parent.mkdir(parents=True)
        project.write_text('<Project/>')
        self.before = {path: digest(self.snapshot/path, self.snapshot) for path in self.tracked}
        (self.state/'writer-central-before.xml').write_bytes(self.central.read_bytes())

    def admission_fixture(self):
        dependency = {'Name': WRITER_PACKAGE, 'Version': '9.0.13', 'Type': 'PackageReference',
            'IsTopLevel': True, 'TargetFrameworks': ['net9.0'], 'EvaluationResult': {'present': False}}
        native = {'status': 'observed', 'records': [{'path': WRITER_PROJECT, 'central_file': WRITER_CENTRAL,
                                                   'dependencies': [dependency]}]}
        evaluations = [{'tfm': tfm, 'status': 'evaluated', 'central_file': WRITER_CENTRAL,
            'direct_versions': [{'id': WRITER_PACKAGE, 'version': WRITER_OLD[tfm]}],
            'items': {'ProjectReference': []}} for tfm in SMOKE_TFMS]
        return native, evaluations

    def test_admission_requires_one_real_survivor_and_all_three_conditional_evaluations(self):
        native, evaluations = self.admission_fixture()
        self.assertEqual('9.0.13', writer_admission(native, evaluations)['OldVersion'])
        for change in ('missing', 'duplicate', 'unknown_version', 'failed_producer', 'tfm_gap', 'version_normalized'):
            native, evaluations = self.admission_fixture()
            if change == 'missing': native['records'][0]['dependencies'] = []
            if change == 'duplicate': native['records'][0]['dependencies'] *= 2
            if change == 'unknown_version': native['records'][0]['dependencies'][0]['Version'] = '1SECRET'
            if change == 'failed_producer': native['status'] = 'failed_or_empty'
            if change == 'tfm_gap': evaluations.pop()
            if change == 'version_normalized': evaluations[0]['direct_versions'][0]['version'] = '9.0.13'
            with self.subTest(change=change), self.assertRaisesRegex(ValueError, '^writer_producer_incomplete$'):
                writer_admission(native, evaluations)

    def test_failed_producer_never_creates_request_or_starts_writer(self):
        runner = mock.Mock()
        runner.run.side_effect = ValueError('stage_failed')
        with mock.patch('dependabot_native_proof.writer_registration_guard', return_value={}), self.assertRaisesRegex(ValueError, '^stage_failed$'):
            run_writer_case(runner, ['caller'], self.state, self.state, self.snapshot,
                            self.tracked, {WRITER_PACKAGE}, self.before, {'writer': {}})
        self.assertEqual(['writer_producer'], [call.args[0] for call in runner.run.call_args_list])
        self.assertFalse((self.state/'writer-request.json').exists())

    def test_cold_producer_refuses_existing_external_global_json_without_exposing_path(self):
        external = self.state/'global.json'
        external.write_text('SECRET')
        with self.assertRaisesRegex(ValueError, '^writer_registration_ancestor_global_json$'):
            writer_registration_guard(self.snapshot)
        external.unlink()
        external.symlink_to(self.state/'missing')
        with self.assertRaisesRegex(ValueError, '^writer_registration_ancestor_global_json$'):
            writer_registration_guard(self.snapshot)

    def test_phase_deadline_prevents_borrowing_and_preserves_global_reserve(self):
        with contextlib.redirect_stdout(io.StringIO()), mock.patch('dependabot_native_proof.time.monotonic', return_value=100):
            runner = DeadlineRunner(self.state, {'stages': []}, budget_seconds=840, reserve_seconds=60)
            runner.begin_phase('setup')
            self.assertEqual(280, runner.effective_deadline())
            self.assertEqual(880, runner.deadline)
        with mock.patch('dependabot_native_proof.time.monotonic', return_value=281), self.assertRaisesRegex(ValueError, '^overall_deadline$'):
            runner.check()
        with contextlib.redirect_stdout(io.StringIO()), mock.patch('dependabot_native_proof.time.monotonic', return_value=850):
            runner.begin_phase('writer')
            self.assertEqual(880, runner.effective_deadline())

    def test_only_recorded_central_version_changes_are_reconciled(self):
        operations = {'status': 'observed', 'operations': [{'id': WRITER_PACKAGE, 'new': '10.0.12', 'paths': [WRITER_CENTRAL]}]}
        self.central.write_text(self.central.read_text().replace('9.0.13', '10.0.12'))
        result = writer_changes(self.snapshot, self.before, operations, lambda: True)
        self.assertEqual('reconciled', result['status'])
        self.assertEqual([WRITER_CENTRAL], result['changed_paths'])
        (self.snapshot/WRITER_PROJECT).write_text('<Project Changed="true"/>')
        self.assertEqual('unaccounted_write', writer_changes(self.snapshot, self.before, operations, lambda: True)['status'])

    def test_unknown_new_files_and_nonversion_xml_edits_fail_without_raw_paths(self):
        operations = {'status': 'observed', 'operations': []}
        (self.snapshot/'SECRET-token').write_text('https://private')
        result = writer_changes(self.snapshot, self.before, operations, lambda: True)
        self.assertEqual('unaccounted_write', result['status'])
        self.assertNotIn('SECRET', json.dumps(result))
        (self.snapshot/'SECRET-token').unlink()
        operations['operations'] = [{'id': WRITER_PACKAGE, 'new': '10.0.12', 'paths': [WRITER_CENTRAL]}]
        self.central.write_text(self.central.read_text().replace('<Project>', '<Project Secret="https://private">'))
        result = writer_changes(self.snapshot, self.before, operations, lambda: True)
        self.assertEqual('unaccounted_write', result['status'])
        self.assertNotIn('https://', json.dumps(result))

    def event_fixture(self, **changes):
        event = {'Sequence': 0, 'Kind': 'request', 'State': 'returned', 'Ordinal': 0, 'Attempt': 0,
            'AttemptProject': None, 'Workspace': None, 'Tfm': None, 'ElapsedSeconds': 1, 'CentralHash': 'a'*64,
            'Observation': {'Error': 'SECRET https://private'}, 'Raw': 'SECRET'}
        event.update(changes)
        return event

    def test_missing_warning_requires_exact_single_awaited_discovery(self):
        events = [self.event_fixture(Sequence=0, Kind='attempt', State='started', Attempt=1, AttemptProject=WRITER_PROJECT),
                  self.event_fixture(Sequence=1, Kind='discovery', State='started', Attempt=1, Ordinal=2),
                  self.event_fixture(Sequence=2, Kind='discovery', State='returned', Attempt=1, Ordinal=2),
                  self.event_fixture(Sequence=3, Kind='missing_dependency', State='observed', Attempt=1)]
        (self.state/'writer-events.json').write_text(json.dumps({'Events': events, 'Dropped': False}))
        warnings = writer_events(self.state, self.tracked)['missing_warnings']
        self.assertTrue(warnings[0]['bound'])
        self.assertIsNone(warnings[0]['writer_loop_tfm'])
        self.assertIsNone(warnings[0]['rerun_survivor'])
        events[2]['Ordinal'] = 3
        (self.state/'writer-events.json').write_text(json.dumps({'Events': events, 'Dropped': False}))
        self.assertFalse(writer_events(self.state, self.tracked)['missing_warnings'][0]['bound'])

    def test_event_and_operation_projection_omit_raw_errors_and_unobserved_tfm(self):
        event = self.event_fixture()
        (self.state/'writer-events.json').write_text(json.dumps({'Events': [event], 'Dropped': False}))
        result = writer_events(self.state, self.tracked)
        self.assertEqual('observed', result['status'])
        self.assertNotIn('SECRET', json.dumps(result))
        event['Tfm'] = 'net9.0'
        (self.state/'writer-events.json').write_text(json.dumps({'Events': [event], 'Dropped': False}))
        self.assertNotEqual('observed', writer_events(self.state, self.tracked)['status'])
        (self.state/'writer-operations.json').write_text(json.dumps([{'Type': 'DirectUpdate', 'DependencyName': WRITER_PACKAGE,
            'OldVersion': '9.0.13', 'NewVersion': '10.0.12', 'UpdatedFiles': [WRITER_CENTRAL], 'Error': 'SECRET https://private'}]))
        result = writer_operations(self.state, self.tracked, {WRITER_PACKAGE})
        self.assertEqual('observed', result['status'])
        self.assertNotIn('SECRET', json.dumps(result))


if __name__ == '__main__':
    unittest.main()
