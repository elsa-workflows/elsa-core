"""Cheap source-runner contracts. Fake dotnet emits real private logs/TRX; no SDK or PG."""
from contextlib import redirect_stdout
import io
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

import run_socket_source_tests as runner

FAKE = r'''
import json, os, pathlib, sys, xml.etree.ElementTree as ET
args = sys.argv[1:]
mode = os.environ['SOCKET_FAKE_MODE']
framework = next(a.split('=', 1)[1] for a in args if a.startswith('-p:TargetFramework='))
assert '-p:TargetFrameworks=' + framework in args
print('private-only-synthetic-ticket')
print('private-only-synthetic-secret', file=sys.stderr)
project = pathlib.Path(args[1])
if args[0] == 'restore':
    if mode == 'restore-failed':
        print('error NU1301: private-only-synthetic-secret', file=sys.stderr)
        sys.exit(17)
    assets = project.with_name('obj') / 'project.assets.json'
    assets.parent.mkdir(parents=True, exist_ok=True)
    target = 'net9.0' if mode == 'wrong-assets' else framework
    assets.write_text(json.dumps({'targets': {target: {}}, 'project': {'frameworks': {target: {}}}}))
else:
    assert '--no-restore' in args and args[args.index('--framework') + 1] == framework
    assert '-m:1' in args and '-p:GeneratePackageOnBuild=false' in args
    if mode == 'test-failed':
        print('error CS0103: private-only-synthetic-secret', file=sys.stderr)
        sys.exit(23)
    private = pathlib.Path(args[args.index('--results-directory') + 1])
    if mode == 'missing-trx':
        sys.exit(0)
    ns = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'
    ET.register_namespace('', ns)
    root = ET.Element('{'+ns+'}TestRun')
    definitions = ET.SubElement(root, '{'+ns+'}TestDefinitions')
    results = ET.SubElement(root, '{'+ns+'}Results')
    count = 0 if mode == 'zero' else 2
    namespace = ('Elsa.Slack.Tests.SocketMode.Example' if '/slack/' in args[1] else
                 'Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests.SocketExample')
    if mode == 'wrong-namespace': namespace = 'Unselected.PrivateTests'
    if mode == 'wrong-filter': namespace = 'Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests.AdmissionExample'
    for index in range(count):
        test = ET.SubElement(definitions, '{'+ns+'}UnitTest', id=str(index))
        method = 'Example' if mode == 'duplicate' else 'Example'+str(index)
        ET.SubElement(test, '{'+ns+'}TestMethod', className=namespace, name=method)
        ET.SubElement(results, '{'+ns+'}UnitTestResult', testId=('absent' if mode=='unknown-method' else str(index)),
                      executionId=str(index), testName=method+'(private-only-synthetic-secret)',
                      outcome=('NotExecuted' if mode=='skipped' else 'Failed' if mode in ('false-pass','test-case-failed') else 'Passed'))
    summary = ET.SubElement(root, '{'+ns+'}ResultSummary')
    ET.SubElement(summary, '{'+ns+'}Counters', total=str(count+1 if mode=='wrong-count' else count),
                  executed=str(0 if mode=='skipped' else count), passed=str(0 if mode in ('skipped','false-pass','test-case-failed') else count),
                  failed=str(count if mode in ('false-pass','test-case-failed') else 0), error='0', timeout='0', aborted='0',
                  inconclusive='0', notRunnable='0', notExecuted=str(count if mode=='skipped' else 0))
    ET.ElementTree(root).write(private / 'socket-source.trx', encoding='utf-8')
    if mode == 'extra-trx': (private/'other.trx').write_text('private-only-extra')
    if mode == 'dirty': project.write_text(project.read_text()+'\nchanged')
    if mode == 'test-case-failed': sys.exit(1)
'''


class SourceRunnerTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.base = Path(self.directory.name).resolve()
        self.root = self.base / 'source'
        self.root.mkdir()
        for path in (runner.SLACK, runner.POSTGRES, runner.RUNNER, runner.WORKFLOW):
            target = self.root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text('tracked-source')
        (self.root / '.gitignore').write_text('**/obj/\n')
        for project, namespace, name in (
                (runner.SLACK, 'Elsa.Slack.Tests.SocketMode', 'Example'),
                (runner.POSTGRES, 'Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests', 'SocketExample')):
            (self.root / project).with_name(name+'.cs').write_text(
                f'namespace {namespace};\npublic class {name} {{\n'
                ' public void Example0() {}\n public void Example1() {}\n public void Example() {}\n}\n')
        (self.root / 'Directory.Packages.props').write_text(
            '<Project><ItemGroup><PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.9"/></ItemGroup></Project>')
        self.fake = self.root / 'fake.py'
        self.fake.write_text(FAKE)
        self.git('init', '-q')
        self.git('add', '.')
        self.git('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-qm', 'fixture')
        self.head = self.git('rev-parse', 'HEAD')
        self.commands = []
        self.original_execute = runner.execute

    def git(self, *args):
        return subprocess.check_output(['git', '-C', str(self.root), *args], text=True).strip()

    def exercise(self, mode='pass', suite='slack-unit-loopback', framework='net10.0'):
        output = self.base / ('evidence-'+str(len(list(self.base.glob('evidence-*')))))

        def execute(command, root, log, timeout):
            self.commands.append(command)
            return self.original_execute([sys.executable, str(self.fake), *command[1:]], root, log, timeout=10)

        stdout = io.StringIO()
        with patch.object(runner, 'execute', side_effect=execute), patch.dict(os.environ, SOCKET_FAKE_MODE=mode), redirect_stdout(stdout):
            passed = runner.run(self.root, output, self.head, suite, framework, '123', '1')
        self.assertNotIn('private-only', stdout.getvalue())
        public = output / 'retained'
        self.assertEqual({'receipt.json'} if passed else {'failure.json'}, {p.name for p in public.iterdir()})
        data = json.loads(next(public.iterdir()).read_text())
        self.assertNotIn('private-only', json.dumps(data))
        private = ''.join(p.read_text() for p in (output/'private').glob('*.log'))
        if self.commands:
            self.assertIn('private-only-synthetic-secret', private)
            self.assertIn('private-only-synthetic-ticket', private)
        return passed, data

    def test_four_exact_cells_keep_identity_framework_and_filter(self):
        cells = [('slack-unit-loopback', f'net{major}.0') for major in (8, 9, 10)] + [('postgres-socket', 'net10.0')]
        for suite, framework in cells:
            with self.subTest(suite=suite, framework=framework):
                passed, data = self.exercise(suite=suite, framework=framework)
                self.assertTrue(passed)
                self.assertEqual((suite, framework, self.head), (data['suite'], data['framework'], data['sourceRevision']))
                self.assertEqual(2, data['passed'])
                self.assertEqual(0, data['failed']+data['skipped'])
                self.assertFalse(data['packageOnlyProof'] or data['fullSocketAcceptance'] or data['fixtureCleanupVerified'])
                self.assertFalse(data['postgresqlProof'])
                self.assertEqual(suite == 'postgres-socket', data['postgresqlSourceTests'])
                self.assertEqual(self.git('rev-parse', 'HEAD^{tree}'), data['sourceTree'])
                self.assertEqual({runner.SUITES[suite][0], runner.RUNNER, runner.WORKFLOW}, set(data['inputSha256']))
                test = self.commands[-1]
                self.assertEqual(suite == 'postgres-socket', '--filter' in test)
                if '--filter' in test:
                    self.assertEqual('FullyQualifiedName~Socket', test[test.index('--filter')+1])

    def test_restore_failure_retains_codes_only_and_never_tests(self):
        passed, data = self.exercise('restore-failed')
        self.assertFalse(passed)
        self.assertEqual(('restore_failed', 17, ['NU1301']), (data['category'], data['exitCode'], data['diagnosticCodes']))
        self.assertEqual(['restore'], [c[1] for c in self.commands])

    def test_wrong_restored_framework_never_tests(self):
        passed, data = self.exercise('wrong-assets')
        self.assertFalse(passed)
        self.assertEqual('restored_framework_mismatch', data['category'])
        self.assertEqual(['restore'], [c[1] for c in self.commands])

    def test_nonzero_test_cannot_publish_success(self):
        passed, data = self.exercise('test-failed')
        self.assertFalse(passed)
        self.assertEqual(('test_failed', 23, ['CS0103']), (data['category'], data['exitCode'], data['diagnosticCodes']))

    def test_invalid_actual_trx_results_fail_closed(self):
        for mode in ('missing-trx', 'zero', 'skipped', 'false-pass', 'wrong-count', 'duplicate', 'unknown-method', 'extra-trx', 'wrong-namespace'):
            with self.subTest(mode=mode):
                passed, data = self.exercise(mode)
                self.assertFalse(passed)
                self.assertEqual('trx_invalid', data['category'])

    def test_postgres_filter_rejects_unselected_method_metadata(self):
        passed, data = self.exercise('wrong-filter', suite='postgres-socket')
        self.assertFalse(passed)
        self.assertEqual('trx_invalid', data['category'])

    def test_source_mutation_after_test_cannot_publish_success(self):
        passed, data = self.exercise('dirty')
        self.assertFalse(passed)
        self.assertEqual('source_postcheck_failed', data['category'])
        self.assertEqual('working_tree_dirty', data['sourceState']['reason'])
        self.assertEqual([{'path': runner.SLACK, 'status': ' M'}], data['sourceState']['changedTracked'])
        self.assertEqual([runner.SLACK], data['sourceState']['inputHashMismatches'])
        self.assertEqual(2, data['testEvidence']['counters']['passed'])
        self.assertEqual([], data['testEvidence']['failedTests'])

    def test_wrong_head_or_dirty_source_never_invokes_dotnet(self):
        for dirty in (False, True):
            with self.subTest(dirty=dirty):
                if dirty: (self.root / runner.SLACK).write_text('unreviewed')
                with patch.object(runner, 'execute') as execute:
                    output = self.base / ('bad-source-'+str(dirty))
                    self.assertFalse(runner.run(self.root, output, self.head if dirty else 'a'*40,
                                                'slack-unit-loopback', 'net8.0', '123', '1'))
                    execute.assert_not_called()
                self.assertEqual('source_validation_failed', json.loads((output/'retained/failure.json').read_text())['category'])

    def test_bad_selections_and_existing_output_rejected_before_children(self):
        for suite, framework in [('arbitrary', 'net10.0'), ('postgres-socket', 'net8.0'), ('slack-unit-loopback', 'net7.0')]:
            with self.subTest(suite=suite, framework=framework), patch.object(runner, 'execute') as execute:
                with self.assertRaises(runner.SourceTestError):
                    runner.run(self.root, self.base/'invalid', self.head, suite, framework, '123', '1')
                execute.assert_not_called()
        for output in (self.root, self.base/'used'):
            output.mkdir(exist_ok=True)
            (output/'keep').write_text('user data')
            with self.assertRaises(runner.SourceTestError):
                runner.run(self.root, output, self.head, 'slack-unit-loopback', 'net10.0', '123', '1')
            self.assertEqual('user data', (output/'keep').read_text())

    def test_output_symlink_ancestor_is_rejected_before_children_or_writes(self):
        destination = self.base / 'real-output'
        destination.mkdir()
        alias = self.base / 'output-alias'
        alias.symlink_to(destination, target_is_directory=True)
        with patch.object(runner, 'execute') as execute:
            with self.assertRaises(runner.SourceTestError):
                runner.run(self.root, alias / 'new-evidence', self.head, 'slack-unit-loopback', 'net10.0', '123', '1')
            execute.assert_not_called()
        self.assertEqual([], list(destination.iterdir()))

    def test_arbitrary_exception_text_is_not_retained_or_printed(self):
        with patch.object(runner, 'execute', side_effect=RuntimeError('/private/secret/token=private-only')), redirect_stdout(io.StringIO()) as stdout:
            output = self.base/'exception'
            self.assertFalse(runner.run(self.root, output, self.head, 'slack-unit-loopback', 'net10.0', '123', '1'))
        retained = (output/'retained/failure.json').read_text()
        self.assertNotIn('private-only', retained+stdout.getvalue())
        self.assertEqual('restore_failed', json.loads(retained)['category'])

    def test_failed_test_identities_are_bound_to_head_source_not_private_messages(self):
        passed, data = self.exercise('test-case-failed', suite='postgres-socket')
        self.assertFalse(passed)
        self.assertEqual('test_failed', data['category'])
        evidence = data['testEvidence']
        self.assertEqual('available', evidence['status'])
        self.assertEqual(2, evidence['counters']['failed'])
        self.assertEqual(2, len(evidence['failedTests']))
        for row in evidence['failedTests']:
            self.assertIn('.SocketExample.Example', row['method'])
            self.assertEqual('Failed', row['outcome'])
            self.assertEqual(64, len(row['caseSha256']))
            self.assertEqual(64, len(row['sourceSha256']))
            self.assertTrue(row['sourceFile'].endswith('/SocketExample.cs'))
        # A valid-looking but untracked symbol cannot become a public failed identity.
        source = self.root / runner.POSTGRES
        source.with_name('SocketExample.cs').write_text('public void PrivateNewSymbol() {}')
        trx = self.base / 'evidence-0/private/socket-source.trx'
        trx.write_text(trx.read_text().replace('Example0', 'PrivateNewSymbol'))
        safe = runner.test_failure_evidence(self.root, self.head, trx.parent, runner.POSTGRES,
                                            'postgres-socket', runner.SUITES['postgres-socket'][3], 1)
        self.assertEqual(1, safe['omittedFailureIdentities'])
        self.assertNotIn('PrivateNewSymbol', json.dumps(safe))

    def test_untracked_names_are_private_and_python_caches_bind_only_tracked_owners(self):
        (self.root / 'private-only-token.txt').write_text('private-only-secret')
        cache = self.root / '__pycache__'
        cache.mkdir()
        (cache / 'fake.cpython-312.pyc').write_bytes(b'private-only-bytecode')
        (cache / 'private-only.cpython-312.pyc').write_bytes(b'private-only-bytecode')
        state = runner.source_state(self.root, self.head, None)
        self.assertEqual('working_tree_dirty', state['reason'])
        self.assertEqual({'python-cache': 1, 'other': 2}, state['untrackedKinds'])
        self.assertEqual(['fake.py'], state['pythonCacheOwners'])
        self.assertNotIn('private-only', json.dumps(state))
        self.assertEqual([], state['changedTracked'])

    def test_rename_new_path_is_not_public(self):
        self.git('mv', 'fake.py', 'private-only-new-name.py')
        state = runner.source_state(self.root, self.head, None)
        self.assertEqual('working_tree_dirty', state['reason'])
        self.assertEqual([{'path': 'fake.py', 'status': 'R '}], state['changedTracked'])
        self.assertNotIn('private-only', json.dumps(state))

    def test_failed_identity_diagnostics_are_bounded_without_weakening_counters(self):
        import xml.etree.ElementTree as ET
        self.exercise('test-case-failed')
        trx = self.base / 'evidence-0/private/socket-source.trx'
        tree = ET.parse(trx)
        ns = '{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}'
        definitions, results = tree.getroot().find(ns+'TestDefinitions'), tree.getroot().find(ns+'Results')
        prototype_method = definitions[0].find(ns+'TestMethod').attrib
        definitions.clear()
        results.clear()
        for index in range(40):
            definition = ET.SubElement(definitions, ns+'UnitTest', id=str(index))
            ET.SubElement(definition, ns+'TestMethod', **prototype_method)
            ET.SubElement(results, ns+'UnitTestResult', testId=str(index), outcome='Failed',
                          testName='private-only-parameter-'+str(index))
        counters = tree.getroot().find(ns+'ResultSummary/'+ns+'Counters')
        counters.set('total', '40')
        counters.set('executed', '40')
        counters.set('failed', '40')
        tree.write(trx)
        safe = runner.test_failure_evidence(self.root, self.head, trx.parent, runner.SLACK,
                                            'slack-unit-loopback', runner.SUITES['slack-unit-loopback'][3], 1)
        self.assertEqual(40, safe['counters']['failed'])
        self.assertEqual(32, len(safe['failedTests']))
        self.assertTrue(safe['truncated'])
        self.assertNotIn('private-only', json.dumps(safe))

    def test_dependency_codes_and_ids_are_bound_to_head_catalog_not_error_text(self):
        assets = (self.root / runner.SLACK).with_name('obj') / 'project.assets.json'
        assets.parent.mkdir()
        assets.write_text(json.dumps({'logs': [
            {'code': 'NU1202', 'libraryId': 'Microsoft.AspNetCore.Mvc.Testing', 'targetGraphs': ['net8.0'],
             'message': 'private-only-secret at /private/path'},
            {'code': 'NU1202', 'libraryId': 'private-only-package'},
            {'code': 'private-only-code', 'libraryId': 'Microsoft.AspNetCore.Mvc.Testing'},
            {'code': 'NU1202', 'libraryId': ['private-only-list']},
        ]}))
        expected = [{'code': 'NU1202', 'packageId': 'Microsoft.AspNetCore.Mvc.Testing', 'framework': 'net8.0'}]
        self.assertEqual(expected, runner.dependency_diagnostics(self.root, self.head, runner.SLACK, 'net8.0'))
        # Worktree/catalog poisoning cannot authorize a package from the error payload.
        (self.root / 'Directory.Packages.props').write_text(
            '<Project><PackageVersion Include="private-only-package"/></Project>')
        self.assertEqual(expected, runner.dependency_diagnostics(self.root, self.head, runner.SLACK, 'net8.0'))
        assets.unlink()
        assets.symlink_to(self.root / 'Directory.Packages.props')
        self.assertEqual([], runner.dependency_diagnostics(self.root, self.head, runner.SLACK, 'net8.0'))

    def test_publication_failure_cannot_leave_a_partial_success_receipt(self):
        link = runner.os.link

        def fail_success(source, destination):
            if destination.name == 'receipt.json':
                raise OSError('private-only-filesystem-error')
            link(source, destination)

        with patch.object(runner.os, 'link', side_effect=fail_success):
            passed, data = self.exercise()
        self.assertFalse(passed)
        self.assertEqual('receipt_write_failed', data['category'])

    def test_workflow_matrix_and_upload_are_explicit(self):
        import yaml
        workflow = yaml.safe_load((runner.ROOT / runner.WORKFLOW).read_text())
        job = workflow['jobs']['source-tests']
        self.assertEqual([{'suite': 'slack-unit-loopback', 'framework': f'net{m}.0'} for m in (8, 9, 10)] +
                         [{'suite': 'postgres-socket', 'framework': 'net10.0'}], job['strategy']['matrix']['include'])
        steps = job['steps']
        checkout = steps[0]['with']
        self.assertFalse(checkout['persist-credentials'])
        self.assertIn('head.sha', checkout['ref'])
        upload = steps[-1]
        self.assertEqual('${{ runner.temp }}/socket-source/retained/receipt.json\n${{ runner.temp }}/socket-source/retained/failure.json\n', upload['with']['path'])
        test = next(s for s in steps if s.get('id') == 'tests')
        self.assertIn('run_socket_source_tests.py', test['run'])
        self.assertNotIn('dotnet ', test['run'])
        trigger = workflow.get('on', workflow.get(True))
        for event in ('push', 'pull_request'):
            self.assertIn('test/workers/**', trigger[event]['paths'])
            self.assertIn('test/integration/Directory.*', trigger[event]['paths'])
            self.assertIn('test/integration/Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests/**', trigger[event]['paths'])


if __name__ == '__main__':
    unittest.main()
