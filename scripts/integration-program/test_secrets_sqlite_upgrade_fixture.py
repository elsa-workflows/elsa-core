import copy
import json
from pathlib import Path
import unittest

from run_secrets_sqlite_upgrade_fixture import validate_known_baseline


FIXTURE = Path(__file__).resolve().parent / 'secrets-sqlite-upgrade'


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

    def test_rejects_unexpected_core_outcome(self):
        report = copy.deepcopy(self.report)
        report['coreUpgrade']['result'] = 'migrated'
        with self.assertRaisesRegex(ValueError, 'must reproduce the expected migration failure'):
            self.validate(report)

    def test_rejects_unexpected_core_exception(self):
        for field, value in (
            ('exceptionType', 'Microsoft.EntityFrameworkCore.DbUpdateException'),
            ('exceptionMessage', 'SQLite Error 1: unexpected table state.'),
        ):
            with self.subTest(field=field):
                report = copy.deepcopy(self.report)
                report['coreUpgrade'][field] = value
                with self.assertRaisesRegex(ValueError, 'Unexpected Core migration exception'):
                    self.validate(report)

    def test_rejects_unexpected_migration_ids(self):
        mutations = (
            ('oldMigration', 'appliedMigrations', 'migration IDs'),
            ('coreUpgrade', 'appliedMigrationsBeforeUpgrade', 'migration IDs'),
            ('coreUpgrade', 'pendingMigrationsBeforeUpgrade', 'migration IDs'),
        )
        for section, field, message in mutations:
            with self.subTest(section=section, field=field):
                report = copy.deepcopy(self.report)
                report[section][field] = ['unexpected-migration']
                with self.assertRaisesRegex(ValueError, message):
                    self.validate(report)

        report = copy.deepcopy(self.report)
        report['databaseBeforeCore']['migrationHistory'][0][0] = 'unexpected-migration'
        with self.assertRaisesRegex(ValueError, 'migration history'):
            self.validate(report)

    def test_rejects_changed_database_state_and_old_package_reopen(self):
        report = copy.deepcopy(self.report)
        report['databaseAfterCore']['syntheticRows'][0]['Owner'] = 'changed'
        with self.assertRaisesRegex(ValueError, 'complete SQLite state'):
            self.validate(report)

        report = copy.deepcopy(self.report)
        report['oldPackageReopenAfterFailure']['encryptedValueSha256'][0] = 'unexpected'
        with self.assertRaisesRegex(ValueError, 'could not reopen and read'):
            self.validate(report)


if __name__ == '__main__':
    unittest.main()
