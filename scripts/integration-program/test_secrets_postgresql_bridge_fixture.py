import json
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[2]
FIXTURE = ROOT / 'scripts/integration-program/secrets-postgresql-bridge'


class PostgreSqlSecretsBridgeFixtureTests(unittest.TestCase):
    def test_reuses_the_reviewed_artifact_and_core_source_pins(self):
        bridge = json.loads((FIXTURE / 'artifacts.json').read_text())
        collision = json.loads((FIXTURE.parent / 'secrets-postgresql-upgrade/artifacts.json').read_text())
        sqlite = json.loads((FIXTURE.parent / 'secrets-sqlite-bridge-contract/artifacts.json').read_text())

        self.assertEqual(bridge['targetCoreSourceCommit'], 'c37e9d7a2fa7e7c2af802b211e3d59db45fc2f6f')
        for phase in ('extensions-3.8.1', 'core-3.8.4'):
            self.assertEqual(bridge['phases'][phase]['packages'], collision['phases'][phase]['packages'])
            self.assertEqual(bridge['phases'][phase]['sourceCommit'], collision['phases'][phase]['sourceCommit'])
        self.assertEqual(bridge['source-build-tooling']['sourceCommit'], sqlite['source-build-tooling']['sourceCommit'])
        self.assertEqual(
            [{key: package[key] for key in ('id', 'version', 'sha512')}
             for package in bridge['source-build-tooling']['packages']],
            [{key: package[key] for key in ('id', 'version', 'sha512')}
             for package in sqlite['source-build-tooling']['packages']])

    def test_fixture_guards_crypto_mapping_migrations_and_redaction(self):
        runner = (FIXTURE / 'current-core/Program.cs').read_text()
        source = (FIXTURE / 'extensions-3.8.1/Program.cs').read_text()
        script = (ROOT / 'scripts/integration-program/run_secrets_postgresql_bridge.py').read_text()

        for migration in (
                '20260531141856_Initial', '20260825224525_SecretTenancy',
                '20260914120000_SecretDefaultTenantUniqueness', '20260923164247_ManagedSecretOwnership'):
            self.assertIn(migration, runner)
        self.assertIn('Elsa.Secrets.Encryption', runner)
        self.assertIn('Elsa.Secrets.Encryption', source)
        self.assertIn('DefaultSecretValueProtector', runner)
        self.assertIn('ElsaSecretsLegacyV381', runner)
        self.assertIn('BeginTransactionAsync', runner)
        self.assertIn('wrongDataProtectionContextRejected', runner)
        self.assertIn('sourceRowHashSha256', source)
        self.assertIn('ciphertextPrinted = false', runner)
        self.assertIn('plaintextPrinted = false', source)
        self.assertIn('cutoverAllowed = false', runner)
        self.assertIn('const string novelName = "tenant-b:forged-novel"', runner)
        self.assertIn('var forgedRowAbsent = tenantBAfter.All(row => row.Name != novelName);', runner)
        self.assertNotIn('error is DbUpdateException or InvalidOperationException', runner)
        self.assertNotRegex(script, r'(?i)dotnet\s+nuget\s+push|nuget\s+push|gh\s+release')

    def test_ci_runs_and_retains_only_the_redacted_report(self):
        workflow = (ROOT / '.github/workflows/integration-program-tools.yml').read_text()
        self.assertIn('run_secrets_postgresql_bridge.py', workflow)
        self.assertIn('secrets-postgresql-bridge-report.json', workflow)
        self.assertIn('actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02', workflow)
        self.assertIn('retention-days: 30', workflow)

    def test_runner_failure_output_does_not_expose_connections_or_provider_text(self):
        runner = (FIXTURE / 'current-core/Program.cs').read_text()
        script = (ROOT / 'scripts/integration-program/run_secrets_postgresql_bridge.py').read_text()

        self.assertIn("DataType={mismatch.DataTypeName}", runner)
        self.assertNotIn('mismatch.MessageText', runner)
        self.assertIn('conversionWritesUnchanged = error.ConversionWritesUnchanged', runner)
        self.assertIn('originalDestinationUnchanged = error.OriginalDestinationUnchanged', runner)
        self.assertLess(runner.index('var groups = Preflight(source, tenantMap, Array.Empty<ExistingTarget>());'),
                        runner.index('await MigrateTargetAsync(services);'))
        self.assertIn('except RuntimeError as error:', script)
        self.assertIn("safe = safe.replace(connection, '<redacted connection string>')", script)
        self.assertIn("print(f'Fixture failed at {CURRENT_STAGE}: {type(error).__name__}', file=sys.stderr)", script)
        self.assertIn("'bin/Release/net10.0/PostgreSqlBridgeRunner.dll'", script)
        self.assertIn("result.get('originalDestinationUnchanged') is not expected_original_unchanged", script)


if __name__ == '__main__':
    unittest.main()
