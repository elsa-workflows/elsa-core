"""Tests for the fail-closed, synthetic Secrets SQLite mapping contract."""
from copy import deepcopy
import json
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).parent))
try:
    import run_secrets_sqlite_bridge_contract as bridge_runner
    import secrets_sqlite_bridge_mapping as mapping
finally:
    sys.path.pop(0)


class SecretsSqliteBridgeMappingTests(unittest.TestCase):
    def setUp(self):
        self.snapshot, self.rows = mapping.synthetic_fixture()

    def test_projects_logical_identity_and_two_version_history_without_ciphertext(self):
        projected = mapping.project_synthetic_records(self.snapshot, self.rows)
        aggregate = projected[0]
        secret = aggregate['Secret']

        self.assertEqual(secret['Id'], 'synthetic-secret')
        self.assertEqual(secret['Name'], 'Synthetic-Key')
        self.assertEqual(secret['DisplayName'], 'Synthetic-Key')
        self.assertEqual(aggregate['normalizedNameForStorage'], 'synthetic-key')
        self.assertEqual(secret['Status'], 'Active')
        self.assertEqual([version['Version'] for version in secret['Versions']], [1, 2])
        self.assertEqual([version['Status'] for version in secret['Versions']], ['Retired', 'Active'])
        self.assertEqual([version['Payload']['Metadata']['legacy.rowId'] for version in secret['Versions']],
                         ['synthetic-row-v1', 'synthetic-row-v2'])
        self.assertEqual([version['Payload']['Metadata']['legacy.isLatest'] for version in secret['Versions']],
                         ['false', 'true'])
        self.assertEqual(aggregate['derivedCurrentVersionAtReferenceTime'], 2)
        self.assertEqual(set(secret), {
            'Id', 'Name', 'DisplayName', 'Description', 'TypeName', 'StoreName', 'Scope',
            'Tags', 'Status', 'CreatedAt', 'UpdatedAt', 'Versions',
        })
        metadata_values = [
            value
            for version in secret['Versions']
            for value in version['Payload']['Metadata'].values()
        ]
        self.assertTrue(all(isinstance(value, str) for value in metadata_values))
        serialized = json.dumps(projected)
        self.assertNotIn('synthetic-only-ciphertext', serialized)
        self.assertNotIn('EncryptedValue', serialized)

    def test_maps_every_known_legacy_status_and_rejects_unknown_code(self):
        snapshot = deepcopy(self.snapshot)
        rows = []
        for status_code in mapping.STATUS_BY_LEGACY_VALUE:
            row = deepcopy(self.rows[1])
            row.update(
                Id=f'synthetic-row-status-{status_code}',
                SecretId=f'synthetic-secret-status-{status_code}',
                Name=f'Synthetic-Status-{status_code}',
                Version=1,
                IsLatest=True,
                Status=status_code,
            )
            rows.append(row)

        projected = mapping.project_synthetic_records(snapshot, rows)
        self.assertEqual(
            [entry['Secret']['Status'] for entry in projected],
            [mapping.STATUS_BY_LEGACY_VALUE[code] for code in mapping.STATUS_BY_LEGACY_VALUE],
        )
        rows[0]['Status'] = 4
        with self.assertRaisesRegex(mapping.BridgeContractError, 'Unsupported legacy status'):
            mapping.project_synthetic_records(snapshot, rows)

    def test_fails_closed_for_unsupported_tenant_owner_and_duplicate_names(self):
        cases = [
            ('tenant', lambda rows: rows[0].update(TenantId='tenant-7')),
            ('owner', lambda rows: rows[0].update(Owner='user-7')),
            ('normalized name', lambda rows: rows.append({
                **rows[0], 'Id': 'other-row', 'SecretId': 'other-secret',
                'Name': 'synthetic-key', 'Version': 1, 'IsLatest': True,
            })),
        ]
        for label, mutate in cases:
            with self.subTest(label=label):
                rows = deepcopy(self.rows)
                mutate(rows)
                with self.assertRaises(mapping.BridgeContractError):
                    mapping.project_synthetic_records(self.snapshot, rows)

    def test_fails_closed_for_schema_history_and_row_shape_drift(self):
        cases = [
            ('unknown table', lambda snapshot, rows: snapshot['tables'].append('Other')),
            ('unknown column', lambda snapshot, rows: snapshot['secretsColumns'].append('Other')),
            ('unknown history', lambda snapshot, rows: snapshot['migrationHistory'][0].__setitem__(0, 'unknown')),
            ('unknown row field', lambda snapshot, rows: rows[0].update(Unexpected='value')),
            ('missing row field', lambda snapshot, rows: rows[0].pop('Owner')),
        ]
        for label, mutate in cases:
            with self.subTest(label=label):
                snapshot = deepcopy(self.snapshot)
                rows = deepcopy(self.rows)
                mutate(snapshot, rows)
                with self.assertRaises(mapping.BridgeContractError):
                    mapping.project_synthetic_records(snapshot, rows)

    def test_fails_closed_for_ambiguous_versions_invalid_times_and_missing_ciphertext(self):
        cases = [
            ('duplicate version', lambda rows: rows[1].update(Version=1)),
            ('incorrect latest marker', lambda rows: rows[1].update(IsLatest=False)),
            ('non-binary latest marker', lambda rows: rows[1].update(IsLatest=2)),
            ('timestamp without offset', lambda rows: rows[1].update(ExpiresAt='2026-09-23T13:00:00')),
            ('invalid timestamp', lambda rows: rows[0].update(CreatedAt='not-a-time')),
            ('missing ciphertext', lambda rows: rows[1].update(EncryptedValue='')),
        ]
        for label, mutate in cases:
            with self.subTest(label=label):
                rows = deepcopy(self.rows)
                mutate(rows)
                with self.assertRaises(mapping.BridgeContractError):
                    mapping.project_synthetic_records(self.snapshot, rows)

    def test_mapping_columns_cover_exact_pinned_sqlite_schema(self):
        mapping_path = (Path(__file__).parent / 'secrets-sqlite-bridge-contract' / 'mapping.json')
        mapping_document = json.loads(mapping_path.read_text())
        columns = mapping_document['legacySqliteSchema']['columns']
        self.assertEqual({column['name'] for column in columns}, set(mapping.LEGACY_COLUMNS))
        self.assertTrue(all('sqlite' in column and 'coreCandidate' in column and 'policy' in column
                            for column in columns))
        self.assertEqual(set(self.snapshot['secretsColumns']), set(mapping.LEGACY_COLUMNS))

    def test_runner_subprocess_timeout_is_reported(self):
        with self.assertRaisesRegex(RuntimeError, '0.01s timeout'):
            bridge_runner.run(
                [sys.executable, '-c', 'import time; time.sleep(1)'],
                capture=True,
                timeout=0.01,
            )


if __name__ == '__main__':
    unittest.main()
