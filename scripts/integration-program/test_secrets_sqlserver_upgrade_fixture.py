import copy
import json
from pathlib import Path
import unittest

from run_secrets_sqlserver_upgrade_fixture import validate_known_baseline


FIXTURE = Path(__file__).resolve().parent / 'secrets-sqlserver-upgrade'


class KnownBaselineTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.report = json.loads((FIXTURE / 'observed-result.json').read_text())

    def validate(self, report):
        validate_known_baseline(
            report['oldMigration'], report['coreUpgrade'], report['databaseBeforeCore'],
            report['databaseAfterCore'], report['oldPackageReopenAfterFailure'])

    def test_recorded_pair_matches_the_exact_baseline(self):
        self.validate(self.report)

    def test_rejects_changed_migration_result_or_identity(self):
        for section, field, value in (
            ('oldMigration', 'appliedMigrations', ['wrong']),
            ('coreUpgrade', 'pendingMigrationsBeforeUpgrade', ['wrong']),
            ('coreUpgrade', 'result', 'migrated'),
            ('coreUpgrade', 'errorNumber', 2601),
        ):
            with self.subTest(section=section, field=field):
                report = copy.deepcopy(self.report)
                report[section][field] = value
                with self.assertRaises(ValueError):
                    self.validate(report)

    def test_rejects_changed_database_or_old_reader(self):
        report = copy.deepcopy(self.report)
        report['databaseAfterCore']['columns'][0]['dataType'] = 'changed'
        with self.assertRaisesRegex(ValueError, 'complete SQL Server state'):
            self.validate(report)

        report = copy.deepcopy(self.report)
        report['oldPackageReopenAfterFailure']['versions'] = [2]
        with self.assertRaisesRegex(ValueError, 'could not reopen and read'):
            self.validate(report)

    def test_verified_direct_packages_match_restored_cache_and_locks(self):
        for phase in self.report['packageLocks'].values():
            for package in phase['directPackages'].values():
                with self.subTest(package=package['packageCachePath']):
                    self.assertEqual(package['verifiedArtifactSha512'], package['cachedPackageSha512'])
                    self.assertEqual(package['nugetLockContentHash'], package['nugetCachedContentHash'])
                    self.assertEqual(package['assetsLibraryPath'],
                                     '/'.join(package['packageCachePath'].split('/')[:-1]))
                    self.assertTrue(package['restoredFromVerifiedFeed'])

    def test_report_excludes_runtime_password_and_connection_string(self):
        rendered = json.dumps(self.report)
        for marker in ('MSSQL_SA_PASSWORD', 'Password=', 'Server=127.0.0.1',
                       'SYNTHETIC-CIPHERTEXT-SENTINEL'):
            with self.subTest(marker=marker):
                self.assertNotIn(marker, rendered)


if __name__ == '__main__':
    unittest.main()
