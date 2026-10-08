"""Cheap adversarial orchestration tests; fake boundaries are never runtime proof."""
from contextlib import ExitStack
from copy import deepcopy
import errno
import hashlib
import json
from pathlib import Path
import re
import tempfile
import unittest
from unittest.mock import patch

import prove_consolidated_socket_consumer as runner
from test_verify_socket_package_consumer import valid_report, HEAD, VERSION, SERVER


class SocketConsumerRunnerTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()

    def cell(self, *, mutation=None):
        """Run actual cell orchestration against explicitly fake SDK/service boundaries."""
        artifacts = self.root / 'artifacts'
        artifacts.mkdir()
        by_id = {name.casefold(): {'id': name} for name in runner.REQUIRED_PACKAGES}
        identity = (VERSION, HEAD, by_id, {}, set())
        service = {'containerId': 'c' * 64, 'serverVersion': SERVER}
        packages = [{'id': name, 'source': str(artifacts)} for name in runner.REQUIRED_PACKAGES]
        restored = {'internal': [{'id': name} for name in runner.REQUIRED_PACKAGES]}
        commands = []

        def command(arguments, root, environment, log, timeout):
            commands.append(arguments)
            self.assertEqual('1', environment['DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER'])
            self.assertNotIn('ELSA_SECRET_UNTRUSTED', environment)
            self.assertIn('-p:TargetFrameworks=net10.0', arguments)
            self.assertIn('-p:CandidateVersion=' + VERSION, arguments)
            self.assertEqual(runner.lifecycle.COMMAND_TIMEOUT, timeout)
            log.write_text('safe command output')
            (root / 'obj').mkdir(exist_ok=True)
            (root / 'obj/project.assets.json').write_text('{}')
            binary = root / 'bin/Release/net10.0/Elsa.Slack.Tests.dll'
            binary.parent.mkdir(parents=True, exist_ok=True)
            binary.write_bytes(b'fixture binary')

        def runtime(arguments, root, environment, log):
            self.assertEqual(['dotnet', str(root / 'bin/Release/net10.0/Elsa.Slack.Tests.dll')], arguments[:2])
            self.assertEqual(['--feature', 'classic', '--tfm', 'net10.0', '--output'], arguments[2:7])
            self.assertEqual(HEAD, environment['ELSA_SOCKET_PACKAGE_SOURCE_REVISION'])
            self.assertEqual(VERSION, environment['ELSA_SOCKET_PACKAGE_CANDIDATE_VERSION'])
            database = re.search(r'Database=(socket_consumer_[0-9a-f]{24});', environment['ELSA_SOCKET_PACKAGE_CONNECTION_STRING'])[1]
            data = valid_report()
            data['scenarios'][0]['databaseIdentitySha256'] = hashlib.sha256(database.encode()).hexdigest()
            for row in data['loadedAssemblies']:
                row['location'] = str(root / 'bin/Release/net10.0' / (row['name'] + '.dll'))
            Path(arguments[-1]).write_text(json.dumps(data))
            log.write_text('SOCKET_PACKAGE_CONSUMER_PASS')
            if mutation:
                mutation(root)
            return {'pid': 1234, 'observedLinuxStartTokenSha256': 'd' * 64, 'exitCode': 0,
                    'reaped': True, 'processGroupAbsent': True, 'fixtureStartIdentityIndependentlyVerified': False}

        with ExitStack() as stack:
            stack.enter_context(patch.dict(runner.os.environ, {'ELSA_SECRET_UNTRUSTED': 'must not inherit'}))
            stack.enter_context(patch.object(runner.consumers, '_run_command', side_effect=command))
            stack.enter_context(patch.object(runner.consumers, 'verify_restore_isolation'))
            stack.enter_context(patch.object(runner.consumers, 'validate_project_assets', return_value=restored))
            stack.enter_context(patch.object(runner.consumers, '_package_evidence', return_value=packages))
            loaded = stack.enter_context(patch.object(runner.consumers, 'verify_loaded_assemblies',
                                                       side_effect=lambda report, *a, **kw: deepcopy(report['loadedAssemblies'])))
            stack.enter_context(patch.object(runner.lifecycle, '_sql', side_effect=lambda container, database, sql, log: '' if database == 'postgres' else database))
            stack.enter_context(patch.object(runner, '_run_runtime', side_effect=runtime))
            result = runner._cell(self.root, artifacts, identity, '10.0.100', service,
                                  'Host=127.0.0.1;Database=postgres;Password=private-password;', self.root / 'service.log')
        self.assertEqual(['restore', 'build'], [command[1] for command in commands])
        self.assertEqual(runner.REQUIRED_PACKAGES, loaded.call_args.kwargs['required_packages'])
        return result, by_id

    def test_cell_binds_actual_report_inputs_and_preserves_raw_paths_until_verification(self):
        cell, by_id = self.cell()
        self.assertFalse(cell['process']['fixtureStartIdentityIndependentlyVerified'])
        self.assertEqual('e' * 64, cell['process']['fixtureReportedStartIdentitySha256'])
        self.assertIn(str(self.root), cell['report']['loadedAssemblies'][0]['location'])
        runner.lifecycle.normalize_locations([cell], self.root, by_id)
        encoded = json.dumps(cell)
        self.assertNotIn(str(self.root), encoded)
        self.assertTrue(all(row['location'].startswith('/consumer/net10.0-classic/bin/')
                            for row in cell['report']['loadedAssemblies']))
        self.assertTrue(all(row['source'] == 'exact-verified-artifact-feed' for row in cell['restoredPackages']))

    def test_cell_rejects_changed_executable_assets_inputs_and_extra_report_files(self):
        mutations = [
            (lambda root: (root / 'bin/Release/net10.0/Elsa.Slack.Tests.dll').write_bytes(b'changed'), 'consumer_outputs_changed'),
            (lambda root: (root / 'obj/project.assets.json').write_text('{"changed":true}'), 'consumer_outputs_changed'),
            (lambda root: (root / 'Program.cs').write_text('changed'), 'consumer_inputs_changed'),
            (lambda root: (root / 'runtime-evidence/extra.json').write_text('{}'), 'runtime_evidence_inventory'),
        ]
        for index, (mutate, category) in enumerate(mutations):
            with self.subTest(category=category), tempfile.TemporaryDirectory() as temporary:
                with patch.object(self, 'root', Path(temporary).resolve()), self.assertRaisesRegex(runner.ProofError, '^' + category + '$'):
                    self.cell(mutation=mutate)

    def test_fixture_is_generic_candidate_only_and_rejects_project_framework_or_raw_references(self):
        files = runner._fixture_files()
        self.assertTrue(any(path.name == 'Program.cs' for path in files))
        project = next(path for path in files if path.suffix == '.csproj').read_text()
        for addition in ('<ProjectReference Include="../source.csproj" />',
                         '<FrameworkReference Include="Microsoft.AspNetCore.App" />',
                         '<Reference Include="unverified.dll" />'):
            with self.subTest(addition=addition), tempfile.TemporaryDirectory() as temporary:
                directory = Path(temporary).resolve()
                (directory / 'SocketPackageConsumer.csproj').write_text(project.replace('</Project>', addition + '</Project>'))
                with patch.object(runner, 'FIXTURE', directory), self.assertRaisesRegex(runner.ProofError, 'fixture_project'):
                    runner._fixture_files()

    def test_socket_diagnostics_are_fixed_source_bound_and_never_raw_text(self):
        marker = runner.SECRET_MARKERS[0]
        log = self.root / 'runtime.log'
        log.write_text('\n'.join([
            f'{self.root}/Program.cs(11,2): error CS0123: {marker}',
            f'/elsewhere/Program.cs(9,1): error CS0123: {marker}',
            f'{self.root}/Unknown.cs(9,1): error CS0123: {marker}',
            'SOCKET_PACKAGE_CONSUMER_FAIL:socket-vertical:assertion:output-already-exists',
            'SOCKET_PACKAGE_CONSUMER_FAIL:report:assertion:invented-untrusted-value',
            'SOCKET_PACKAGE_CONSUMER_DIAGNOSTIC:postgres:42P01:Migrations.cs:44',
            f'SOCKET_PACKAGE_CONSUMER_DIAGNOSTIC:{marker}:none:Program.cs:1',
        ]))
        result = runner.failure_diagnostics(log, self.root)
        self.assertEqual([{'file': 'Program.cs', 'line': 11, 'column': 2, 'code': 'CS0123'}], result['compilerDiagnostics'])
        self.assertEqual({'stage': 'socket-vertical', 'category': 'assertion:output-already-exists'}, result['fixtureFailure'])
        self.assertEqual({'category': 'postgres', 'sqlState': '42P01', 'file': 'Migrations.cs', 'line': 44}, result['runtimeDiagnostic'])
        self.assertNotIn(marker, json.dumps(result))
        self.assertNotIn(str(self.root), json.dumps(result))
        self.assertEqual(result, runner._safe_diagnostics(result))

    def test_runtime_wrapper_keeps_shared_ownership_and_uses_socket_diagnostics(self):
        log = self.root / 'runtime.log'
        log.write_text('SOCKET_PACKAGE_CONSUMER_FAIL:configuration:timeout')
        command, environment = ['dotnet', 'fixture.dll'], {'fixture-only': 'value'}
        process = {'pid': 1234, 'reaped': True, 'processGroupAbsent': True}
        with patch.object(runner.lifecycle, '_run_runtime', return_value=process) as run:
            self.assertIs(process, runner._run_runtime(command, self.root, environment, log))
            run.assert_called_once_with(command, self.root, environment, log)
        with patch.object(runner.lifecycle, '_run_runtime', side_effect=runner.ProofError('runtime_exit', {'untrusted': runner.SECRET_MARKERS[0]})):
            with self.assertRaises(runner.ProofError) as caught:
                runner._run_runtime(command, self.root, environment, log)
        self.assertEqual({'stage': 'configuration', 'category': 'timeout'}, caught.exception.diagnostics['fixtureFailure'])
        self.assertNotIn(runner.SECRET_MARKERS[0], json.dumps(caught.exception.diagnostics))

    def prove_fake(self, *, source_changed=False, archives_changed=False, cleanup_failure=False,
                   cleanup_unverified=False, startup_failure=False):
        """Actual orchestration/temporary cleanup, fake service, package and runtime calls."""
        artifacts = self.root / 'archives'
        artifacts.mkdir()
        output = self.root / 'retained'
        identity = (VERSION, HEAD, {'elsa': {}}, {}, set())
        manifest = {'build_inputs': {'sdk': '10.0.100'}}
        private_paths = []
        events = []

        def start(name, password, log, owned):
            events.append('start')
            private_paths.append(log.parent)
            owned.update(containerId='c' * 64, image={'id': 'sha256:' + 'd' * 64})
            if startup_failure:
                raise runner.ProofError('service_command')
            return 'Host=127.0.0.1;Database=postgres;Password=' + password + ';', owned

        def cell(private, *args):
            private_paths.append(private)
            root = private / 'net10.0-classic'
            root.mkdir()
            report = valid_report()
            report['loadedAssemblies'] = []
            return {'tfm': 'net10.0', 'feature': 'classic', 'report': report,
                    'loadedAssemblies': [], 'restoredPackages': []}

        def cleanup(name, service, log):
            events.append('cleanup')
            if cleanup_failure:
                raise OSError('private cleanup error')
            self.assertEqual('c' * 64, service['containerId'])
            return {'containerRemoved': True, 'absenceVerified': not cleanup_unverified}

        write = runner._write_receipt

        def retain(path, encoded):
            self.assertTrue(private_paths)
            self.assertTrue(all(not directory.exists() for directory in private_paths))
            events.append('retain')
            write(path, encoded)

        original_is_file = Path.is_file
        with ExitStack() as stack:
            stack.enter_context(patch.object(Path, 'is_file', lambda path: str(path) == '/proc/sys/kernel/random/boot_id' or original_is_file(path)))
            stack.enter_context(patch.object(runner.consumers, '_validated_manifest', return_value=identity))
            stack.enter_context(patch.object(runner.subprocess, 'check_output', side_effect=[HEAD + '\n', '']))
            stack.enter_context(patch.object(runner.lifecycle, 'tracked_inputs', side_effect=[{'source': 'same'}, {'source': 'changed' if source_changed else 'same'}]))
            stack.enter_context(patch.object(runner.lifecycle, 'frozen_archives', side_effect=[{'archive': 'same'}, {'archive': 'changed' if archives_changed else 'same'}]))
            stack.enter_context(patch.object(runner.lifecycle, '_start_postgres', side_effect=start))
            stack.enter_context(patch.object(runner.lifecycle, '_sql', return_value=str(SERVER)))
            stack.enter_context(patch.object(runner.lifecycle, '_cleanup_postgres', side_effect=cleanup))
            stack.enter_context(patch.object(runner, '_cell', side_effect=cell))
            stack.enter_context(patch.object(runner, '_write_receipt', side_effect=retain))
            receipt = runner.prove(artifacts, manifest, output)
        return receipt, events, output

    def test_one_checkpoint_scope_is_retained_only_after_owned_cleanup(self):
        receipt, events, output = self.prove_fake()
        self.assertEqual(['start', 'cleanup', 'retain'], events)
        self.assertEqual('passed', receipt['status'])
        self.assertFalse(receipt['fullSocketAcceptance'])
        self.assertFalse(receipt['publicationPerformed'])
        self.assertEqual('first-vertical-classic-net10.0', receipt['scope'])
        self.assertEqual(1, len(receipt['cells']))
        self.assertEqual({'socket-consumer-proof.json'}, {path.name for path in output.iterdir()})
        self.assertNotIn(str(self.root), json.dumps(receipt))

    def test_cleanup_failure_and_source_or_original_archive_drift_never_pass(self):
        for options, category in (({'cleanup_failure': True}, 'service_cleanup'),
                                  ({'cleanup_unverified': True}, 'service_cleanup'),
                                  ({'startup_failure': True}, 'service_command'),
                                  ({'source_changed': True}, 'source_changed'),
                                  ({'archives_changed': True}, 'archives_changed')):
            with self.subTest(category=category), tempfile.TemporaryDirectory() as temporary:
                with patch.object(self, 'root', Path(temporary).resolve()), self.assertRaisesRegex(runner.ProofError, '^' + category + '$'):
                    self.prove_fake(**options)
                data = json.loads((Path(temporary) / 'retained/socket-consumer-proof.json').read_text())
                self.assertEqual('failed', data['status'])
                self.assertNotIn('cleanup', data)
                self.assertNotIn('cells', data)

    def test_secret_scan_rejects_socket_marker_in_logs_and_report_not_source(self):
        marker = runner.SECRET_MARKERS[0]
        (self.root / 'Program.cs').write_text(marker)
        log, report = self.root / 'runtime.log', self.root / 'cell-report.json'
        log.write_text('safe output')
        report.write_text('{}')
        runner.lifecycle._private_scan(self.root, runner.SECRET_MARKERS)
        for path in (log, report):
            with self.subTest(path=path.name):
                previous = path.read_text()
                path.write_text(marker)
                with self.assertRaises(runner.ProofError):
                    runner.lifecycle._private_scan(self.root, runner.SECRET_MARKERS)
                path.write_text(previous)

    def test_failure_boundary_drops_arbitrary_categories_and_diagnostic_values(self):
        marker = runner.SECRET_MARKERS[0]
        for index, error in enumerate((ValueError(marker), runner.ProofError(marker),
                                       runner.ProofError('runtime_exit', {'payload': marker, 'runtimeDiagnostic': {'file': [marker]}}))):
            output = self.root / str(index)
            with patch.object(runner, '_prove', side_effect=error), self.assertRaises(runner.ProofError):
                runner.prove(self.root, {}, output)
            encoded = (output / 'socket-consumer-proof.json').read_text()
            self.assertNotIn(marker, encoded)
            self.assertEqual('failed', json.loads(encoded)['status'])
            self.assertNotIn('cleanup', json.loads(encoded))

    def test_receipt_never_overwrites_and_cross_device_publication_fails_closed(self):
        output = self.root / 'receipt'
        runner._write_receipt(output, b'{"status":"failed"}')
        with self.assertRaises(runner.ProofError):
            runner._write_receipt(output, b'{"status":"passed"}')
        self.assertEqual(b'{"status":"failed"}', (output / 'socket-consumer-proof.json').read_bytes())
        other = self.root / 'other'
        with patch.object(runner.os, 'link', side_effect=OSError(errno.EXDEV, 'private value')):
            with self.assertRaisesRegex(runner.ProofError, '^receipt_write$'):
                runner._write_receipt(other, b'{"status":"passed"}')
        self.assertEqual([], list(other.iterdir()))
        link = self.root / 'link'
        link.symlink_to(output, target_is_directory=True)
        with self.assertRaises(runner.ProofError):
            runner._write_receipt(link, b'{"status":"passed"}')


if __name__ == '__main__':
    unittest.main()
