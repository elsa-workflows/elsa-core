"""Cheap synthetic receipt tests; these do not execute the native updater."""
import contextlib
import io
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest

from dependabot_native_proof import DeadlineRunner, NativeProgress, diagnostic_complete, reconcile


class NativeProofReceiptTests(unittest.TestCase):
    def test_timeout_kills_descendant_and_retains_only_safe_progress(self):
        with tempfile.TemporaryDirectory() as directory:
            state = Path(directory)
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
            state = Path(directory)
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


if __name__ == '__main__':
    unittest.main()
