"""Offline negative guards, never evidence that PostgreSQL or .NET scenarios ran."""
import copy
import errno
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
from unittest.mock import patch

import prove_consolidated_admission_consumers as runner
import test_prove_consolidated_packages as package_fixtures
from test_prove_consolidated_admission_consumers import cell


class AdmissionRunnerBoundaryTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()

    def test_original_archives_cannot_be_mutated_omitted_or_augmented(self):
        fixture = package_fixtures.PackageProofTests()
        fixture.setUp()
        fixture.directory = fixture.directory.resolve()
        self.addCleanup(fixture.doCleanups)
        fixture.row.update(expected_framework_reference_groups=[], expected_symbol_framework_reference_groups=[], expected_sdk_assets=[])
        fixture.artifacts()
        for kind in ('nupkg', 'snupkg'):
            fixture.row[kind + '_sha256'] = hashlib.sha256((fixture.directory / fixture.row[kind]).read_bytes()).hexdigest()
        expected = runner.frozen_archives(fixture.directory, fixture.manifest)
        self.assertEqual(2, len(expected))
        extra = fixture.directory / 'unexpected.txt'
        extra.write_text('not an archive')
        with self.assertRaisesRegex(runner.ProofError, 'archive_inventory'):
            runner.frozen_archives(fixture.directory, fixture.manifest)
        extra.unlink()
        path = fixture.directory / fixture.row['nupkg']
        path.write_bytes(path.read_bytes() + b'mutation')
        with self.assertRaisesRegex(runner.ProofError, 'archive_bytes'):
            runner.frozen_archives(fixture.directory, fixture.manifest)
        path.unlink()
        with self.assertRaises(ValueError):
            runner.frozen_archives(fixture.directory, fixture.manifest)

    def test_private_outputs_reject_secret_markers_but_source_is_not_exported(self):
        (self.root / 'Program.cs').write_text(runner.SECRET_MARKERS[0])
        log = self.root / 'runtime.log'
        report = self.root / 'cell-report.json'
        log.write_text('PACKAGE_CONSUMER_PASS')
        report.write_text('{}')
        runner._private_scan(self.root, ('database-password',))
        for path, value in ((log, runner.SECRET_MARKERS[0]), (report, 'database-password')):
            with self.subTest(path=path.name):
                previous = path.read_text()
                path.write_text(value)
                with self.assertRaises(runner.ProofError):
                    runner._private_scan(self.root, ('database-password',))
                path.write_text(previous)

    def test_diagnostics_allow_only_trusted_coordinates_and_source_assertions(self):
        marker = runner.SECRET_MARKERS[0]
        log = self.root / 'build.log'
        log.write_text('\n'.join([
            f'{self.root}/Program.cs(11,2): error CS0123: {marker}',
            f'/outside/Program.cs(9,1): error CS0123: {marker}',
            f'{self.root}/Unknown.cs(9,1): error CS0123: {marker}',
            'PACKAGE_CONSUMER_FAIL:admission:assertion:bootstrap-not-durable',
            'PACKAGE_CONSUMER_FAIL:report:assertion:invented-untrusted-value',
            'PACKAGE_CONSUMER_DIAGNOSTIC:postgres:42P01:Migrations.cs:44',
            f'PACKAGE_CONSUMER_DIAGNOSTIC:{marker}:none:Program.cs:1',
        ]))
        result = runner.failure_diagnostics(log, self.root)
        self.assertEqual([{'file': 'Program.cs', 'line': 11, 'column': 2, 'code': 'CS0123'}], result['compilerDiagnostics'])
        self.assertEqual({'stage': 'admission', 'category': 'assertion:bootstrap-not-durable'}, result['fixtureFailure'])
        self.assertEqual({'category': 'postgres', 'sqlState': '42P01', 'file': 'Migrations.cs', 'line': 44}, result['runtimeDiagnostic'])
        self.assertNotIn(marker, json.dumps(result))
        self.assertNotIn(str(self.root), json.dumps(result))

    def test_normalization_retains_only_logical_verified_locations(self):
        report = cell()
        for row in report['loadedAssemblies']:
            row['location'] = str(self.root / 'net8.0-classic/bin/Release/net8.0' / (row['name'] + '.dll'))
        record = {'tfm': 'net8.0', 'feature': 'classic', 'report': report,
                  'loadedAssemblies': copy.deepcopy(report['loadedAssemblies']),
                  'restoredPackages': [{'id': 'Elsa', 'source': '/private/feed'}, {'id': 'Npgsql', 'source': 'https://api.nuget.org/v3/index.json'}]}
        runner.normalize_locations([record], self.root, {'elsa': {}})
        encoded = json.dumps(record)
        self.assertNotIn(str(self.root), encoded)
        self.assertNotIn('/private/feed', encoded)
        self.assertTrue(all(row['location'].startswith('/consumer/net8.0-classic/bin/') for row in record['loadedAssemblies']))
        self.assertEqual('exact-verified-artifact-feed', record['restoredPackages'][0]['source'])

    def test_failed_or_unexpected_exceptions_never_retain_success_or_raw_messages(self):
        for index, error in enumerate((runner.ProofError('runtime_exit', {'cell': {'tfm': 'net8.0', 'feature': 'classic'}}),
                                       ValueError(runner.SECRET_MARKERS[0]))):
            output = self.root / str(index)
            with patch.object(runner, '_prove', side_effect=error), self.assertRaises(runner.ProofError):
                runner.prove(self.root, {}, output)
            data = json.loads((output / 'admission-consumer-proof.json').read_text())
            self.assertEqual('failed', data['status'])
            self.assertFalse(data['publicationPerformed'])
            self.assertNotIn('cleanup', data)
            self.assertNotIn(runner.SECRET_MARKERS[0], json.dumps(data))
            self.assertEqual({'admission-consumer-proof.json'}, {path.name for path in output.iterdir()})

    def test_receipt_creation_never_overwrites_prior_evidence(self):
        output = self.root / 'retained'
        runner._write_receipt(output, b'{"status":"failed"}')
        with self.assertRaises(runner.ProofError):
            runner._write_receipt(output, b'{"status":"passed"}')
        self.assertEqual(b'{"status":"failed"}', (output / 'admission-consumer-proof.json').read_bytes())

    def test_published_receipt_remains_committed_when_staging_unlink_fails(self):
        output = self.root / 'retained'
        original_unlink = runner.os.unlink
        original_link = runner.os.link
        failure_injected = False
        staging_paths = []

        def record_link(source, destination):
            staging_paths.append(source)
            self.addCleanup(shutil.rmtree, source.parent, ignore_errors=True)
            return original_link(source, destination)

        def fail_staging_unlink(path, *args, **kwargs):
            nonlocal failure_injected
            if str(path).endswith('.pending') and not failure_injected:
                self.assertEqual(b'{"status":"passed"}',
                                 (output / 'admission-consumer-proof.json').read_bytes())
                failure_injected = True
                raise OSError(runner.SECRET_MARKERS[0])
            return original_unlink(path, *args, **kwargs)

        with patch.object(runner.os, 'link', side_effect=record_link), \
                patch.object(runner.os, 'unlink', side_effect=fail_staging_unlink):
            runner._write_receipt(output, b'{"status":"passed"}')
        self.assertTrue(failure_injected)
        self.assertEqual(b'{"status":"passed"}', (output / 'admission-consumer-proof.json').read_bytes())
        self.assertEqual({'admission-consumer-proof.json'}, {path.name for path in output.iterdir()})
        self.assertEqual(1, len(staging_paths))
        self.assertFalse(staging_paths[0].is_relative_to(output))

    def test_receipt_publication_failure_is_sanitized_and_leaves_no_success(self):
        for index, error in enumerate((OSError(runner.SECRET_MARKERS[0]),
                                       OSError(errno.EXDEV, runner.SECRET_MARKERS[0]))):
            with self.subTest(errno=error.errno):
                output = self.root / str(index)
                with patch.object(runner.os, 'link', side_effect=error), \
                        self.assertRaisesRegex(runner.ProofError, '^receipt_write$') as caught:
                    runner._write_receipt(output, b'{"status":"passed"}')
                self.assertNotIn(runner.SECRET_MARKERS[0], str(caught.exception))
                self.assertEqual([], list(output.iterdir()))

    def test_receipt_publication_failure_is_not_masked_by_staging_cleanup(self):
        output = self.root / 'retained'
        original_mkdtemp = runner.tempfile.mkdtemp
        original_rmtree = shutil.rmtree

        def record_staging(*args, **kwargs):
            staging = original_mkdtemp(*args, **kwargs)
            self.addCleanup(original_rmtree, staging, ignore_errors=True)
            return staging

        with patch.object(runner.tempfile, 'mkdtemp', side_effect=record_staging), \
                patch.object(runner.os, 'link', side_effect=OSError(runner.SECRET_MARKERS[0])), \
                patch.object(runner.shutil, 'rmtree', side_effect=OSError(runner.SECRET_MARKERS[0])), \
                self.assertRaisesRegex(runner.ProofError, '^receipt_write$'):
            runner._write_receipt(output, b'{"status":"passed"}')
        self.assertEqual([], list(output.iterdir()))

    def test_receipt_publication_race_never_overwrites_prior_evidence(self):
        output = self.root / 'retained'
        original_link = runner.os.link

        def competing_publication(source, destination):
            destination.write_bytes(b'{"status":"failed"}')
            return original_link(source, destination)

        with patch.object(runner.os, 'link', side_effect=competing_publication), \
                self.assertRaisesRegex(runner.ProofError, '^receipt_write$'):
            runner._write_receipt(output, b'{"status":"passed"}')
        self.assertEqual(b'{"status":"failed"}', (output / 'admission-consumer-proof.json').read_bytes())
        self.assertEqual({'admission-consumer-proof.json'}, {path.name for path in output.iterdir()})

    def test_cold_postgres_pull_keeps_stderr_private_and_accepts_only_stdout_id(self):
        identifier = 'a' * 64
        image_id = 'sha256:' + 'b' * 64
        log = self.root / 'docker.log'
        owned = {}
        results = [
            subprocess.CompletedProcess([], 0, (identifier + '\n').encode(), b'Unable to find image locally\nPulling layers\n'),
            subprocess.CompletedProcess([], 0, (image_id + '\n').encode(), b'inspect diagnostic\n'),
            subprocess.CompletedProcess([], 0, b'127.0.0.1:54321\n', b'port diagnostic\n'),
            subprocess.CompletedProcess([], 0),
        ]
        pending = iter(results)

        def command_output(*args, **kwargs):
            result = next(pending)
            # Model subprocess' merged-stream behavior, not an impossible mock.
            if kwargs.get('stderr') == subprocess.STDOUT:
                return subprocess.CompletedProcess([], result.returncode, result.stdout + result.stderr)
            return result

        with patch.object(runner.subprocess, 'run', side_effect=command_output) as execute, \
                patch.object(runner, 'image_identity', return_value={'id': image_id}) as inspect:
            connection, service = runner._start_postgres('owned', 'private-password', log, owned)
        self.assertIs(service, owned)
        self.assertEqual(identifier, owned['containerId'])
        self.assertEqual({'id': image_id}, owned['image'])
        self.assertIn('Port=54321;', connection)
        inspect.assert_called_once_with(image_id)
        self.assertEqual(4, execute.call_count)
        self.assertIn('--pull=missing', execute.call_args_list[0].args[0])
        for call in execute.call_args_list[:3]:
            self.assertEqual(subprocess.PIPE, call.kwargs['stdout'])
            self.assertEqual(subprocess.PIPE, call.kwargs['stderr'])
        for result in results[:3]:
            self.assertIn(result.stdout + result.stderr, log.read_bytes())
        self.assertNotIn(b'private-password', log.read_bytes())

    def test_postgres_waits_for_final_tcp_server_or_times_out_despite_ready_socket(self):
        identifier = 'a' * 64
        image_id = 'sha256:' + 'b' * 64
        for final_tcp_ready in (True, False):
            with self.subTest(final_tcp_ready=final_tcp_ready):
                owned = {}
                tcp_results = iter((2, 0 if final_tcp_ready else 2))

                def probe(command, **kwargs):
                    self.assertEqual(['docker', 'exec', identifier, 'pg_isready'], command[:4])
                    self.assertEqual(subprocess.DEVNULL, kwargs['stdout'])
                    self.assertEqual(subprocess.DEVNULL, kwargs['stderr'])
                    self.assertEqual(10, kwargs['timeout'])
                    # The image's temporary initialization server accepts sockets
                    # throughout this model, but not TCP on the final server port.
                    tcp = ('-h' in command and command[command.index('-h') + 1] == '127.0.0.1'
                           and '-p' in command and command[command.index('-p') + 1] == '5432')
                    return subprocess.CompletedProcess(command, next(tcp_results) if tcp else 0)

                with patch.object(runner, '_capture', side_effect=[identifier, image_id, '127.0.0.1:54321']), \
                        patch.object(runner, 'image_identity', return_value={'id': image_id}), \
                        patch.object(runner.subprocess, 'run', side_effect=probe) as execute, \
                        patch.object(runner.time, 'monotonic', side_effect=[0, 1, 60]), \
                        patch.object(runner.time, 'sleep') as sleep:
                    if final_tcp_ready:
                        connection, service = runner._start_postgres('owned', 'private-password', self.root / 'docker.log', owned)
                        self.assertIs(service, owned)
                        self.assertEqual({'id': image_id}, service['image'])
                        self.assertIn('Host=127.0.0.1;Port=54321;', connection)
                    else:
                        with self.assertRaisesRegex(runner.ProofError, '^postgres_start_timeout$'):
                            runner._start_postgres('owned', 'private-password', self.root / 'docker.log', owned)
                    self.assertEqual(2, execute.call_count)
                    sleep.assert_called_once_with(0.5)
                # A timed-out startup retains the exact owned identity for cleanup.
                self.assertEqual(identifier, owned['containerId'])

    def test_postgres_contaminated_stdout_is_not_parsed_as_a_container_id(self):
        identifier = 'a' * 64
        for index, stdout in enumerate(('pull diagnostic\n' + identifier, identifier + '\npull diagnostic')):
            with self.subTest(position=index):
                log = self.root / f'docker-{index}.log'
                owned = {}
                result = subprocess.CompletedProcess([], 0, stdout.encode(), b'private diagnostic\n')
                with patch.object(runner.subprocess, 'run', return_value=result) as execute, \
                        patch.object(runner, 'image_identity') as inspect, \
                        self.assertRaisesRegex(runner.ProofError, '^container_identity$'):
                    runner._start_postgres('owned', 'private-password', log, owned)
                self.assertEqual({}, owned)
                self.assertEqual(1, execute.call_count)
                inspect.assert_not_called()
                self.assertEqual(result.stdout + result.stderr, log.read_bytes())

    def test_service_command_failure_keeps_both_streams_out_of_public_receipt(self):
        output = self.root / 'retained'
        log = self.root / 'private-docker.log'
        marker = runner.SECRET_MARKERS[0].encode()
        result = subprocess.CompletedProcess([], 1, b'private stdout:' + marker, b'private stderr:' + marker)

        def fail_command(*args):
            return runner._capture(['docker', 'run', 'private-argument'], log)

        with patch.object(runner.subprocess, 'run', return_value=result), \
                patch.object(runner, '_prove', side_effect=fail_command), \
                self.assertRaisesRegex(runner.ProofError, '^service_command$') as caught:
            runner.prove(self.root, {}, output)
        self.assertEqual({}, caught.exception.diagnostics)
        self.assertEqual(result.stdout + result.stderr, log.read_bytes())
        receipt = json.loads((output / 'admission-consumer-proof.json').read_text())
        self.assertEqual('failed', receipt['status'])
        self.assertEqual('service_command', receipt['category'])
        self.assertNotIn(marker.decode(), json.dumps(receipt))
        self.assertNotIn('private-argument', json.dumps(receipt))
        self.assertEqual({'admission-consumer-proof.json'}, {path.name for path in output.iterdir()})

    def test_container_absence_requires_successful_query_and_exact_id_absence(self):
        identifier = 'a' * 64
        cases = (["" , identifier], ["", runner.ProofError('service_command')])
        for responses in cases:
            with self.subTest(responses=str(responses)), patch.object(runner, '_capture', side_effect=responses), self.assertRaises(runner.ProofError):
                runner._cleanup_postgres('owned', {'containerId': identifier}, self.root / 'docker.log')
        with patch.object(runner, '_capture', side_effect=['', 'b' * 64]):
            self.assertTrue(runner._cleanup_postgres('owned', {'containerId': identifier}, self.root / 'docker.log')['absenceVerified'])

    def test_runtime_timeout_kills_and_reaps_only_owned_group(self):
        class Process:
            pid = 12345
            waits = 0
            def wait(self, timeout=None):
                self.waits += 1
                if timeout is not None:
                    raise subprocess.TimeoutExpired('owned', timeout)
                return -9
        process = Process()
        with patch.object(runner.subprocess, 'Popen', return_value=process), \
                patch.object(runner, '_process_start_token', return_value='a' * 64), \
                patch.object(runner, '_group_exists', side_effect=[True, False]), \
                patch.object(runner.os, 'killpg') as kill, \
                self.assertRaisesRegex(runner.ProofError, 'runtime_timeout'):
            runner._run_runtime(['owned'], self.root, {}, self.root / 'runtime.log')
        kill.assert_called_once_with(process.pid, runner.signal.SIGKILL)
        self.assertEqual(2, process.waits)


if __name__ == '__main__':
    unittest.main()
