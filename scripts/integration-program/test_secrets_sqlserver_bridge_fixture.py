import json
from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[2]
FIXTURE = ROOT / 'scripts/integration-program/secrets-sqlserver-bridge'
SCRIPT = ROOT / 'scripts/integration-program/run_secrets_sqlserver_bridge.py'


class SqlServerSecretsBridgeFixtureTests(unittest.TestCase):
    def test_reuses_reviewed_artifact_and_core_source_pins(self):
        bridge = json.loads((FIXTURE / 'artifacts.json').read_text())
        collision = json.loads((FIXTURE.parent / 'secrets-sqlserver-upgrade/artifacts.json').read_text())
        sqlite = json.loads((FIXTURE.parent / 'secrets-sqlite-bridge-contract/artifacts.json').read_text())

        self.assertEqual(bridge['targetCoreSourceCommit'], '0b20ab54a60a61b025d51a268f5b747e3a3c4860')
        self.assertEqual(bridge['sqlServerImage'], collision['sqlServerImage'])
        for phase in ('extensions-3.8.1', 'core-3.8.4'):
            self.assertEqual(bridge['phases'][phase]['packages'], collision['phases'][phase]['packages'])
            self.assertEqual(bridge['phases'][phase]['sourceCommit'], collision['phases'][phase]['sourceCommit'])
        self.assertEqual(bridge['source-build-tooling']['sourceCommit'], sqlite['source-build-tooling']['sourceCommit'])
        self.assertEqual(
            [{key: package[key] for key in ('id', 'version', 'sha512')}
             for package in bridge['source-build-tooling']['packages']],
            [{key: package[key] for key in ('id', 'version', 'sha512')}
             for package in sqlite['source-build-tooling']['packages']])

    def test_fixture_guards_crypto_tenant_mapping_expiry_and_redaction(self):
        runner = (FIXTURE / 'current-core/Program.cs').read_text()
        source = (FIXTURE / 'extensions-3.8.1/Program.cs').read_text()
        script = SCRIPT.read_text()
        readme = (FIXTURE / 'README.md').read_text()

        for migration in (
                '20260531141743_Initial', '20260825230253_SecretTenancy',
                '20260914120000_SecretDefaultTenantUniqueness', '20260923164247_ManagedSecretOwnership'):
            self.assertIn(migration, runner)
        self.assertIn('Elsa.Secrets.Encryption', runner)
        self.assertIn('Elsa.Secrets.Encryption', source)
        self.assertIn('DefaultSecretValueProtector', runner)
        self.assertIn('ElsaSecretsLegacyV381', runner)
        self.assertIn('BeginTransactionAsync', runner)
        self.assertIn('ExpiresInRaw', runner)
        self.assertIn('TimeSpan.FromDays(1)', runner)
        self.assertIn('TimeSpan.TicksPerDay - 1', source)
        self.assertIn('sourceRowHashSha256', source)
        self.assertIn('ciphertextPrinted = false', runner)
        self.assertIn('plaintextPrinted = false', source)
        self.assertIn('cutoverAllowed = false', runner)
        self.assertIn('tenant write authorization is outside this fixture', readme.lower())
        self.assertNotRegex(script, r'(?i)dotnet\s+nuget\s+push|nuget\s+push|gh\s+release')

    def test_ci_runs_and_retains_redacted_report(self):
        workflow = (ROOT / '.github/workflows/integration-program-tools.yml').read_text()
        self.assertIn('run_secrets_sqlserver_bridge.py', workflow)
        self.assertIn('secrets-sqlserver-bridge-report.json', workflow)
        self.assertIn('actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02', workflow)
        self.assertIn('retention-days: 30', workflow)

    def test_runner_preflights_before_migration_and_scrubs_failures(self):
        runner = (FIXTURE / 'current-core/Program.cs').read_text()
        script = SCRIPT.read_text()
        self.assertLess(runner.index('var groups = Preflight(source, tenantMap, Array.Empty<ExistingTarget>());'),
                        runner.index('await MigrateTargetAsync(services);'))
        self.assertIn('except RuntimeError as error:', script)
        self.assertIn("safe = safe.replace(connection, '<redacted connection string>')", script)
        self.assertIn("print(f'Fixture failed at {CURRENT_STAGE}: {type(error).__name__}{detail}', file=sys.stderr)", script)
        self.assertIn("'bin/Release/net10.0/SqlServerBridgeRunner.dll'", script)
        self.assertIn("result.get('originalDestinationUnchanged') is not expected_original_unchanged", script)


if __name__ == '__main__':
    unittest.main()
