import json
from pathlib import Path
import unittest

from run_secrets_sqlserver_bridge import assert_report_redacted, validate_runner_result


ROOT = Path(__file__).resolve().parents[2]
FIXTURE = ROOT / 'scripts/integration-program/secrets-sqlserver-bridge'
SCRIPT = ROOT / 'scripts/integration-program/run_secrets_sqlserver_bridge.py'


class SqlServerSecretsBridgeFixtureTests(unittest.TestCase):
    def test_reuses_reviewed_artifact_and_core_source_pins(self):
        bridge = json.loads((FIXTURE / 'artifacts.json').read_text())
        sdk = json.loads((FIXTURE / 'global.json').read_text())
        collision = json.loads((FIXTURE.parent / 'secrets-sqlserver-upgrade/artifacts.json').read_text())
        sqlite = json.loads((FIXTURE.parent / 'secrets-sqlite-bridge-contract/artifacts.json').read_text())

        self.assertEqual(bridge['targetCoreSourceCommit'], 'c37e9d7a2fa7e7c2af802b211e3d59db45fc2f6f')
        self.assertEqual(sdk['sdk']['version'], '10.0.300')
        self.assertEqual(sdk['sdk']['rollForward'], 'disable')
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
        self.assertIn('const string novelName = "tenant-b:forged-novel"', runner)
        self.assertIn('var forgedRowAbsent = tenantBAfter.All(row => row.Name != novelName);', runner)
        self.assertLess(runner.index('var persistedSecrets = await ReadAllTenantVisibleAsync'),
                        runner.index('var expiryMappingExact = persistedSecrets.SelectMany'))
        self.assertIn('var statusMappingExact = persistedSecrets.All', runner)
        self.assertIn('var tenantMappingExact = persistedSecrets.All', runner)
        self.assertIn('rejects a repository write with a novel cross-tenant name', readme.lower())
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
        self.assertIn("'--packages', str(packages), '-m:1']", script)
        self.assertIn("core_restore.append('--locked-mode')", script)
        self.assertIn('verify_current_core_lock(current_project)', script)
        self.assertIn("result.get('originalDestinationUnchanged') is not expected_original_unchanged", script)

    def test_pinned_core_restore_uses_committed_full_lock_graph(self):
        project = FIXTURE / 'current-core/SqlServerBridgeRunner.csproj'
        lock_path = project.parent / 'packages.lock.json'
        script = SCRIPT.read_text()
        project_text = project.read_text()
        lock = json.loads(lock_path.read_text())
        graph = lock['dependencies']['net10.0']

        self.assertIn('<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>', project_text)
        self.assertGreater(len(graph), 0)
        for package_id, package in graph.items():
            with self.subTest(package=package_id):
                self.assertTrue(package.get('resolved'))
                self.assertRegex(package.get('contentHash', ''), r'^[A-Za-z0-9+/]+={0,2}$')
        self.assertRegex(script, r"'currentCorePackageLock':\s*core_lock")
        self.assertIn("CURRENT_PROJECT.parent / 'packages.lock.json'", script)
        self.assertIn("args.update_core_lockfile", script)

    def test_report_guard_rejects_injected_material_and_unexpected_fields(self):
        valid = {
            'phase': 'extensions-3.8.1-sqlserver', 'result': 'seeded',
            'sourceRows': 5, 'plaintextPrinted': False, 'keysPrinted': False,
            'ciphertextPrinted': False,
        }
        self.assertEqual(validate_runner_result(valid, source=True), valid)
        with self.assertRaisesRegex(RuntimeError, 'unexpected report field'):
            validate_runner_result({**valid, 'secretValue': 'Synthetic tenant A secret value'}, source=True)
        with self.assertRaisesRegex(RuntimeError, 'unsafe sourceRowHashSha256'):
            validate_runner_result({**valid, 'sourceRowHashSha256': 'Synthetic tenant A secret value'}, source=True)
        with self.assertRaisesRegex(RuntimeError, 'did not prove its redaction flags'):
            validate_runner_result({**valid, 'plaintextPrinted': True}, source=True)
        self.assertEqual(validate_runner_result({
            'phase': 'extensions-3.8.1-sqlserver', 'result': 'snapshotted',
            'appliedMigrations': ['unexpected-history'],
        }, source=True)['result'], 'snapshotted')

        report = dict(sourceConnectionStringPrinted=False, generatedPasswordPrinted=False,
                      plaintextPrinted=False, keysPrinted=False, ciphertextPrinted=False,
                      cutoverAllowed=False, oldPackageSeed=valid)
        self.assertIn('"oldPackageSeed"', assert_report_redacted(
            report, connections=('Server=synthetic;Password=synthetic-password',),
            password='synthetic-password'))
        with self.assertRaisesRegex(RuntimeError, 'protected synthetic material'):
            assert_report_redacted({**report, 'injected': 'synthetic-password'},
                                   connections=('Server=synthetic;Password=synthetic-password',),
                                   password='synthetic-password')
        with self.assertRaisesRegex(RuntimeError, 'protected synthetic material'):
            assert_report_redacted({**report, 'injected': 'Synthetic tenant A secret value'},
                                   connections=(), password='synthetic-password')


if __name__ == '__main__':
    unittest.main()
