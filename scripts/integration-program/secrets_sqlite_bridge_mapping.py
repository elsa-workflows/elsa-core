"""Synthetic mapping contract for the hash-pinned Secrets package pair.

This is a review fixture, not a production data converter. It deliberately
rejects source fields whose semantics have no native Core 3.8.4 target.
"""
from datetime import datetime, timezone
import re


LEGACY_TABLES = ['Secrets', '__EFMigrationsHistory', '__EFMigrationsLock']
LEGACY_COLUMNS = [
    'Id', 'SecretId', 'Name', 'Scope', 'EncryptedValue', 'Description', 'Version',
    'IsLatest', 'Status', 'ExpiresIn', 'ExpiresAt', 'LastAccessedAt', 'TenantId',
    'CreatedAt', 'UpdatedAt', 'Owner',
]
LEGACY_MIGRATION_HISTORY = [['20240915164114_V3_3', '10.0.9']]
STATUS_BY_LEGACY_VALUE = {
    0: 'Active',
    1: 'Retired',
    2: 'Expired',
    3: 'Revoked',
}
CORE_NAME = re.compile(r'^[A-Za-z][A-Za-z0-9._:-]{1,199}$')
CORE_REFERENCE_TIME = datetime(2026, 9, 23, 12, 0, tzinfo=timezone.utc)


class BridgeContractError(ValueError):
    """The legacy fixture cannot be represented without a semantic loss."""


def _instant(value, field):
    if value is None:
        return None
    if not isinstance(value, str):
        raise BridgeContractError(f'{field} must be an ISO-8601 string or null')
    try:
        parsed = datetime.fromisoformat(value.replace('Z', '+00:00'))
    except ValueError as error:
        raise BridgeContractError(f'{field} is not a valid ISO-8601 timestamp') from error
    if parsed.tzinfo is None:
        raise BridgeContractError(f'{field} must include an explicit UTC offset')
    return parsed


def _normalize_core_name(name):
    if not isinstance(name, str):
        raise BridgeContractError('Name must be a string')
    trimmed = name.strip()
    if not CORE_NAME.fullmatch(trimmed):
        raise BridgeContractError(f'Name is not accepted by Core 3.8.4: {name!r}')
    return trimmed.lower()


def validate_layout(snapshot):
    if snapshot.get('tables') != LEGACY_TABLES:
        raise BridgeContractError(f'Unexpected legacy tables: {snapshot.get("tables")}')
    if snapshot.get('secretsColumns') != LEGACY_COLUMNS:
        raise BridgeContractError(f'Unexpected legacy Secrets columns: {snapshot.get("secretsColumns")}')
    if snapshot.get('migrationHistory') != LEGACY_MIGRATION_HISTORY:
        raise BridgeContractError(f'Unexpected legacy migration history: {snapshot.get("migrationHistory")}')


def project_synthetic_records(snapshot, rows):
    """Validate records and emit a review-only Core aggregate projection.

    EncryptedValue is required as input but never copied into the projection.
    The separate .NET proof demonstrates controlled decryption and Core re-
    encryption using synthetic keys and data.
    """
    validate_layout(snapshot)
    if not rows:
        raise BridgeContractError('No legacy secret rows were supplied')

    required = set(LEGACY_COLUMNS)
    by_secret_id = {}
    row_ids = set()
    for row in rows:
        if set(row) != required:
            raise BridgeContractError('A legacy row has missing or unknown columns')
        if not isinstance(row['Id'], str) or not row['Id'] or row['Id'] in row_ids:
            raise BridgeContractError('Legacy row IDs must be non-empty and unique')
        row_ids.add(row['Id'])
        if not isinstance(row['SecretId'], str) or not row['SecretId']:
            raise BridgeContractError('Legacy logical SecretId must be non-empty')
        if not isinstance(row['Name'], str) or not row['Name']:
            raise BridgeContractError('Name must be a non-empty string')
        if not isinstance(row['Description'], str):
            raise BridgeContractError('Description must be a string in the pinned non-null SQLite schema')
        if row['Scope'] is not None and not isinstance(row['Scope'], str):
            raise BridgeContractError('Scope must be a string or null')
        if not isinstance(row['EncryptedValue'], str) or not row['EncryptedValue']:
            raise BridgeContractError('EncryptedValue must be present for the separate crypto proof')
        if row['TenantId'] not in (None, ''):
            raise BridgeContractError('Core 3.8.4 has no TenantId field; tenant-bearing rows are a hard blocker')
        if row['Owner'] not in (None, ''):
            raise BridgeContractError('Core 3.8.4 has no Owner field; owner-bearing rows are a hard blocker')
        if not isinstance(row['Version'], int) or isinstance(row['Version'], bool) or row['Version'] < 1:
            raise BridgeContractError('Versions must be unique positive integers')
        if (not isinstance(row['Status'], int) or isinstance(row['Status'], bool)
                or row['Status'] not in STATUS_BY_LEGACY_VALUE):
            raise BridgeContractError(f'Unsupported legacy status value: {row["Status"]!r}')
        if not isinstance(row['IsLatest'], (bool, int)) or row['IsLatest'] not in (False, True):
            raise BridgeContractError('IsLatest must be a boolean/0-or-1 SQLite value')
        if _instant(row['CreatedAt'], 'CreatedAt') is None:
            raise BridgeContractError('CreatedAt is required')
        if _instant(row['UpdatedAt'], 'UpdatedAt') is None:
            raise BridgeContractError('UpdatedAt is required')
        _instant(row['ExpiresAt'], 'ExpiresAt')
        _instant(row['LastAccessedAt'], 'LastAccessedAt')
        if row['ExpiresIn'] is not None and not isinstance(row['ExpiresIn'], str):
            raise BridgeContractError('ExpiresIn must retain its SQLite TimeSpan text or null')
        by_secret_id.setdefault(row['SecretId'], []).append(row)

    aggregates = []
    normalized_names = {}
    for secret_id, versions in by_secret_id.items():
        versions.sort(key=lambda row: row['Version'])
        numbers = [row['Version'] for row in versions]
        if len(set(numbers)) != len(numbers):
            raise BridgeContractError(f'Duplicate version numbers for logical secret {secret_id!r}')
        latest_rows = [row for row in versions if bool(row['IsLatest'])]
        if len(latest_rows) != 1 or latest_rows[0]['Version'] != numbers[-1]:
            raise BridgeContractError(f'IsLatest must identify exactly the highest version for {secret_id!r}')

        latest = latest_rows[0]
        normalized_name = _normalize_core_name(latest['Name'])
        owner = normalized_names.get(normalized_name)
        if owner is not None and owner != secret_id:
            raise BridgeContractError(
                f'Core normalized-name collision between {owner!r} and {secret_id!r}')
        normalized_names[normalized_name] = secret_id

        core_versions = []
        for row in versions:
            metadata = {
                'legacy.rowId': row['Id'],
                'legacy.secretId': row['SecretId'],
                'legacy.name': row['Name'],
                'legacy.description': row['Description'],
                'legacy.updatedAt': row['UpdatedAt'],
                'legacy.isLatest': str(bool(row['IsLatest'])).lower(),
                'legacy.statusValue': str(row['Status']),
            }
            if row['Scope'] is not None:
                metadata['legacy.scope'] = row['Scope']
            if row['ExpiresIn'] is not None:
                metadata['legacy.expiresIn'] = row['ExpiresIn']
            if row['LastAccessedAt'] is not None:
                metadata['legacy.lastAccessedAt'] = row['LastAccessedAt']
            core_versions.append({
                'Version': row['Version'],
                'Status': STATUS_BY_LEGACY_VALUE[row['Status']],
                'CreatedAt': row['CreatedAt'],
                'ExpiresAt': row['ExpiresAt'],
                'Payload': {'Metadata': metadata},
            })

        aggregate_status = STATUS_BY_LEGACY_VALUE[latest['Status']]
        active_unexpired = [
            row for row in versions
            if STATUS_BY_LEGACY_VALUE[row['Status']] == 'Active'
            and (row['ExpiresAt'] is None or _instant(row['ExpiresAt'], 'ExpiresAt') > CORE_REFERENCE_TIME)
        ]
        current_version = max(active_unexpired, key=lambda row: row['Version']) if active_unexpired else None
        created = min(versions, key=lambda row: _instant(row['CreatedAt'], 'CreatedAt'))['CreatedAt']
        updated = max(versions, key=lambda row: _instant(row['UpdatedAt'], 'UpdatedAt'))['UpdatedAt']
        aggregates.append({
            'Secret': {
                'Id': secret_id,
                'Name': latest['Name'],
                'DisplayName': latest['Name'],
                'Description': latest['Description'],
                'TypeName': 'text',
                'StoreName': 'encrypted',
                'Scope': latest['Scope'],
                'Tags': [],
                'Status': aggregate_status,
                'CreatedAt': created,
                'UpdatedAt': updated,
                'Versions': core_versions,
            },
            'normalizedNameForStorage': normalized_name,
            'derivedCurrentVersionAtReferenceTime': current_version['Version'] if current_version else None,
        })

    return aggregates


def synthetic_fixture():
    """Return disposable schema and rows used by the mapping tests and report."""
    snapshot = {
        'tables': list(LEGACY_TABLES),
        'secretsColumns': list(LEGACY_COLUMNS),
        'migrationHistory': [list(item) for item in LEGACY_MIGRATION_HISTORY],
    }
    rows = [
        {
            'Id': 'synthetic-row-v1', 'SecretId': 'synthetic-secret', 'Name': 'Synthetic-Key',
            'Scope': 'credential', 'EncryptedValue': 'synthetic-only-ciphertext-v1',
            'Description': 'previous version', 'Version': 1, 'IsLatest': False, 'Status': 1,
            'ExpiresIn': '00:15:00', 'ExpiresAt': '2026-09-23T11:00:00+00:00',
            'LastAccessedAt': None, 'TenantId': None, 'CreatedAt': '2026-09-22T10:00:00+00:00',
            'UpdatedAt': '2026-09-22T10:00:00+00:00', 'Owner': None,
        },
        {
            'Id': 'synthetic-row-v2', 'SecretId': 'synthetic-secret', 'Name': 'Synthetic-Key',
            'Scope': 'credential', 'EncryptedValue': 'synthetic-only-ciphertext-v2',
            'Description': 'current version', 'Version': 2, 'IsLatest': True, 'Status': 0,
            'ExpiresIn': '01:00:00', 'ExpiresAt': '2026-09-23T13:00:00+00:00',
            'LastAccessedAt': '2026-09-23T11:30:00+00:00', 'TenantId': None,
            'CreatedAt': '2026-09-23T11:00:00+00:00', 'UpdatedAt': '2026-09-23T11:30:00+00:00',
            'Owner': None,
        },
    ]
    return snapshot, rows


def run_contract_fixtures():
    """Run positive and negative synthetic mapping examples for CI reports."""
    snapshot, rows = synthetic_fixture()
    projected = project_synthetic_records(snapshot, rows)
    if len(projected) != 1 or len(projected[0]['Secret']['Versions']) != 2:
        raise BridgeContractError('The multi-version positive fixture did not project two versions')
    if projected[0]['derivedCurrentVersionAtReferenceTime'] != 2:
        raise BridgeContractError('The positive fixture did not select its active, unexpired latest version')
    if projected[0]['Secret']['Versions'][0]['Status'] != 'Retired':
        raise BridgeContractError('The positive fixture did not preserve the retired status')

    rejected = {}
    negative_cases = {
        'tenant': lambda s, r: r[0].update(TenantId='synthetic-tenant'),
        'owner': lambda s, r: r[0].update(Owner='synthetic-owner'),
        'unknown_status': lambda s, r: r[0].update(Status=99),
        'duplicate_normalized_name': lambda s, r: r.append({**r[0], 'Id': 'other-v1', 'SecretId': 'other-secret', 'Version': 1, 'IsLatest': True, 'Name': 'synthetic-key'}),
        'invalid_latest_marker': lambda s, r: r[1].update(IsLatest=False),
        'duplicate_version': lambda s, r: r[1].update(Version=1),
        'invalid_timestamp': lambda s, r: r[0].update(CreatedAt='not-a-timestamp'),
        'invalid_expiry_offset': lambda s, r: r[1].update(ExpiresAt='2026-09-23T13:00:00'),
        'missing_ciphertext': lambda s, r: r[1].update(EncryptedValue=''),
        'unknown_schema': lambda s, r: s['secretsColumns'].append('UnknownColumn'),
        'unknown_migration_history': lambda s, r: s['migrationHistory'][0].__setitem__(0, 'unknown-migration'),
    }
    for name, mutate in negative_cases.items():
        mutated_snapshot = {
            'tables': list(snapshot['tables']),
            'secretsColumns': list(snapshot['secretsColumns']),
            'migrationHistory': [list(item) for item in snapshot['migrationHistory']],
        }
        mutated_rows = [dict(row) for row in rows]
        mutate(mutated_snapshot, mutated_rows)
        try:
            project_synthetic_records(mutated_snapshot, mutated_rows)
        except BridgeContractError:
            rejected[name] = True
        else:
            raise BridgeContractError(f'Negative fixture was unexpectedly accepted: {name}')
    return {
        'result': 'verified',
        'syntheticLogicalSecrets': len(projected),
        'syntheticVersions': len(projected[0]['Secret']['Versions']),
        'derivedCurrentVersionAtReferenceTime': projected[0]['derivedCurrentVersionAtReferenceTime'],
        'unsupportedStatusValuesRejected': True,
        'tenantAndOwnerValuesRejected': True,
        'normalizedNameDuplicatesRejected': True,
        'unknownSchemaAndHistoryRejected': True,
        'ambiguousVersionAndTimestampInputsRejected': True,
        'missingCiphertextRejected': True,
        'negativeFixturesRejected': sorted(rejected),
        'plaintextPrinted': False,
        'ciphertextPrinted': False,
    }
