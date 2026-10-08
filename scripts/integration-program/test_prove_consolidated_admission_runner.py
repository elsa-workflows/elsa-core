"""Offline negative guards, never evidence that PostgreSQL or .NET scenarios ran."""
import copy
import hashlib
import json
from pathlib import Path
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
