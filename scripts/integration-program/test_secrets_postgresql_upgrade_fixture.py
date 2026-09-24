import copy
import json
from pathlib import Path
import unittest

from run_secrets_postgresql_upgrade_fixture import validate_known_baseline


FIXTURE = Path(__file__).resolve().parent / 'secrets-postgresql-upgrade'


class KnownBaselineTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.report = json.loads((FIXTURE / 'observed-result.json').read_text())

    def validate(self, report):
        validate_known_baseline(
            report['oldMigration'],
            report['coreUpgrade'],
            report['databaseBeforeCore'],
            report['databaseAfterCore'],
            report['oldPackageReopenAfterFailure'],
        )

    def test_recorded_package_pair_matches_exact_known_baseline(self):
        self.validate(self.report)

    def test_rejects_unexpected_core_outcome_and_failure(self):
        report = copy.deepcopy(self.report)
        report['coreUpgrade']['result'] = 'migrated'
        with self.assertRaisesRegex(ValueError, 'must reproduce the expected migration failure'):
            self.validate(report)

        for field, value in (
            ('exceptionType', 'Microsoft.EntityFrameworkCore.DbUpdateException'),
            ('exceptionMessage', '42P07: a different relation already exists'),
            ('sqlState', '23505'),
        ):
            with self.subTest(field=field):
                report = copy.deepcopy(self.report)
                report['coreUpgrade'][field] = value
                with self.assertRaisesRegex(ValueError, 'Unexpected Core migration exception'):
                    self.validate(report)

    def test_rejects_unexpected_migration_ids(self):
        for section, field in (
            ('oldMigration', 'appliedMigrations'),
            ('coreUpgrade', 'appliedMigrationsBeforeUpgrade'),
            ('coreUpgrade', 'pendingMigrationsBeforeUpgrade'),
        ):
            with self.subTest(section=section, field=field):
                report = copy.deepcopy(self.report)
                report[section][field] = ['unexpected-migration']
                with self.assertRaisesRegex(ValueError, 'migration IDs'):
                    self.validate(report)

    def test_rejects_changed_schema_data_or_old_reader(self):
        report = copy.deepcopy(self.report)
        report['databaseAfterCore']['columns'][0]['dataType'] = 'different'
        with self.assertRaisesRegex(ValueError, 'complete PostgreSQL state'):
            self.validate(report)

        report = copy.deepcopy(self.report)
        report['oldPackageReopenAfterFailure']['encryptedValueSha256'][0] = 'unexpected'
        with self.assertRaisesRegex(ValueError, 'could not reopen and read'):
            self.validate(report)

    def test_locked_hash_and_nuget_cache_prove_the_verified_artifact_source(self):
        for phase in self.report['packageLocks'].values():
            for package in phase['directPackages'].values():
                with self.subTest(package=package['packageCachePath']):
                    self.assertEqual(package['verifiedArtifactSha512'], package['cachedPackageSha512'])
                    self.assertEqual(package['nugetLockContentHash'], package['nugetCachedContentHash'])
                    self.assertEqual(
                        package['assetsLibraryPath'],
                        '/'.join(package['packageCachePath'].split('/')[:-1]),
                    )
                    self.assertTrue(package['restoredFromVerifiedFeed'])

    def test_result_report_does_not_include_connection_secrets(self):
        rendered = json.dumps(self.report)
        for marker in ('secrets-fixture', 'Password=', 'POSTGRES_PASSWORD', 'Host=127.0.0.1'):
            with self.subTest(marker=marker):
                self.assertNotIn(marker, rendered)


if __name__ == '__main__':
    unittest.main()
