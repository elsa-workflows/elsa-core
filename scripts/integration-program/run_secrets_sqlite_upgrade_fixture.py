#!/usr/bin/env python3
"""Reproduce the official Extensions 3.8.1 -> Core 3.8.4 SQLite migration handoff."""
import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import sqlite3
import subprocess
import sys
import tempfile
import urllib.request
import xml.etree.ElementTree as ET
import zipfile


ROOT = Path(__file__).resolve().parents[2]
FIXTURE = Path(__file__).resolve().parent / 'secrets-sqlite-upgrade'
MANIFEST = FIXTURE / 'artifacts.json'
NUGET_FLAT = 'https://api.nuget.org/v3-flatcontainer'
OLD_PROJECT = FIXTURE / 'extensions-3.8.1/ExtensionsRunner.csproj'
CORE_PROJECT = FIXTURE / 'core-3.8.4/CoreRunner.csproj'
SYNTHETIC_SECRET_ID = 'synthetic-secret-for-upgrade-fixture'
SYNTHETIC_ROWS = [
    {
        'Id': 'synthetic-row-version-1',
        'SecretId': SYNTHETIC_SECRET_ID,
        'Name': 'synthetic-credential',
        'Scope': 'fixture-only',
        'EncryptedValue': 'SYNTHETIC-CIPHERTEXT-SENTINEL-V1',
        'Description': 'synthetic fixture row; not a credential',
        'Version': 1,
        'IsLatest': 0,
        'Status': 0,
        'ExpiresIn': '00:15:00',
        'ExpiresAt': '2030-01-02T03:04:05+00:00',
        'LastAccessedAt': '2026-09-23T10:00:00+00:00',
        'TenantId': 'synthetic-tenant',
        'CreatedAt': '2026-09-23T09:00:00+00:00',
        'UpdatedAt': '2026-09-23T09:00:00+00:00',
        'Owner': 'synthetic-owner',
    },
    {
        'Id': 'synthetic-row-version-2',
        'SecretId': SYNTHETIC_SECRET_ID,
        'Name': 'synthetic-credential',
        'Scope': 'fixture-only',
        'EncryptedValue': 'SYNTHETIC-CIPHERTEXT-SENTINEL-V2',
        'Description': 'synthetic fixture row; not a credential',
        'Version': 2,
        'IsLatest': 1,
        'Status': 0,
        'ExpiresIn': '00:15:00',
        'ExpiresAt': '2030-01-02T03:04:05+00:00',
        'LastAccessedAt': '2026-09-23T10:00:00+00:00',
        'TenantId': 'synthetic-tenant',
        'CreatedAt': '2026-09-23T09:00:00+00:00',
        'UpdatedAt': '2026-09-23T10:00:00+00:00',
        'Owner': 'synthetic-owner',
    },
]


def run(command, *, cwd=None, env=None, capture=False):
    return subprocess.run(
        command,
        cwd=cwd,
        env=env,
        check=True,
        text=True,
        stdout=subprocess.PIPE if capture else None,
        stderr=subprocess.STDOUT if capture else None,
    )


def local_name(tag):
    return tag.rsplit('}', 1)[-1]


def package_url(package, version):
    package_id = package['id'].lower()
    return f'{NUGET_FLAT}/{package_id}/{version}/{package_id}.{version}.nupkg'


def verify_package(package, phase, feed_dir):
    url = package_url(package, package['version'])
    with urllib.request.urlopen(url, timeout=60) as response:
        content = response.read()
    digest = hashlib.sha512(content).digest()
    digest_hex = digest.hex()
    if digest_hex != package['sha512']:
        raise ValueError(f"SHA-512 mismatch for {package['id']} {package['version']}")

    file_name = f"{package['id'].lower()}.{package['version']}.nupkg"
    (feed_dir / file_name).write_bytes(content)
    with zipfile.ZipFile(feed_dir / file_name) as archive:
        nuspec_path = next(path for path in archive.namelist() if path.lower().endswith('.nuspec'))
        nuspec = ET.fromstring(archive.read(nuspec_path))
    metadata = next(node for node in nuspec if local_name(node.tag) == 'metadata')
    values = {local_name(node.tag): (node.text or '').strip() for node in metadata}
    if values.get('id', '').casefold() != package['id'].casefold():
        raise ValueError(f"Unexpected nuspec ID in {package['id']} {package['version']}")
    if values.get('version') != package['version']:
        raise ValueError(f"Unexpected nuspec version in {package['id']} {package['version']}")
    repository = next((node.attrib for node in metadata if local_name(node.tag) == 'repository'), {})
    if repository.get('url') != phase['repository'] or repository.get('commit') != phase['sourceCommit']:
        raise ValueError(f"Nuspec source provenance mismatch for {package['id']} {package['version']}")
    return {
        'id': package['id'],
        'version': package['version'],
        'url': url,
        'bytes': len(content),
        'sha512': digest_hex,
        'sha512Base64': base64.b64encode(digest).decode('ascii'),
        'repository': repository.get('url'),
        'sourceCommit': repository.get('commit'),
    }


def restore(project, config, packages_dir, *, update_lockfiles):
    command = ['dotnet', 'restore', str(project), '--configfile', str(config), '--packages', str(packages_dir)]
    if not update_lockfiles:
        command.append('--locked-mode')
    try:
        return run(command, cwd=ROOT, capture=True).stdout
    except subprocess.CalledProcessError as error:
        raise RuntimeError(error.stdout) from error


def verify_lock(project, packages):
    lock_path = project.parent / 'packages.lock.json'
    if not lock_path.exists():
        raise FileNotFoundError(f'Missing committed lockfile: {lock_path}')
    lock = json.loads(lock_path.read_text())
    resolved = lock['dependencies']['net10.0']
    direct_packages = {}
    for expected in packages:
        package = next((value for package_id, value in resolved.items()
                        if package_id.casefold() == expected['id'].casefold()), None)
        if package is None:
            raise ValueError(f"Lockfile is missing {expected['id']}")
        exact_range = f"[{expected['version']}, {expected['version']}]"
        if package['resolved'] != expected['version'] or package['requested'] != exact_range:
            raise ValueError(f"Lockfile does not pin {expected['id']} exactly to {expected['version']}")
        direct_packages[expected['id']] = {
            'version': package['resolved'],
            'requested': package['requested'],
            'nugetLockContentHash': package['contentHash'],
        }
    return {
        'sha256': hashlib.sha256(lock_path.read_bytes()).hexdigest(),
        'directPackages': direct_packages,
    }


def lock_phases(manifest, temp_root, *, update_lockfiles):
    feed_dir = temp_root / 'feed'
    packages_dir = temp_root / 'nuget-packages'
    feed_dir.mkdir()
    packages_dir.mkdir()
    verified = {}
    lock_summaries = {}
    for phase_name, phase in manifest['phases'].items():
        verified[phase_name] = [verify_package(package, phase, feed_dir) for package in phase['packages']]

    config = temp_root / 'NuGet.Config'
    feed = str(feed_dir).replace('&', '&amp;')
    config.write_text(
        '<?xml version="1.0" encoding="utf-8"?>\n'
        '<configuration><packageSources><clear />'
        f'<add key="verified-artifacts" value="{feed}" />'
        f'<add key="nuget.org" value="{NUGET_FLAT.rsplit("/v3-flatcontainer", 1)[0]}/v3/index.json" />'
        '</packageSources></configuration>\n'
    )

    for project, phase_name in ((OLD_PROJECT, 'extensions-3.8.1'), (CORE_PROJECT, 'core-3.8.4')):
        restore(project, config, packages_dir, update_lockfiles=update_lockfiles)
        if update_lockfiles:
            continue
        lock_summaries[phase_name] = verify_lock(project, manifest['phases'][phase_name]['packages'])
    return verified, lock_summaries, config, packages_dir


def run_phase(project, config, packages_dir, action, database_path):
    env = os.environ.copy()
    env['NUGET_PACKAGES'] = str(packages_dir)
    command = [
        'dotnet', 'run', '--no-restore', '--configuration', 'Release',
        '--project', str(project), '--', action, str(database_path),
    ]
    try:
        output = run(command, cwd=ROOT, env=env, capture=True).stdout
    except subprocess.CalledProcessError as error:
        raise RuntimeError(error.stdout) from error
    return json.loads(next(line for line in reversed(output.splitlines()) if line.startswith('{')))


def seed_synthetic_rows(database_path):
    columns = list(SYNTHETIC_ROWS[0])
    names = ', '.join(f'"{column}"' for column in columns)
    placeholders = ', '.join(f':{column}' for column in columns)
    with sqlite3.connect(database_path) as connection:
        connection.executemany(
            f'INSERT INTO "Secrets" ({names}) VALUES ({placeholders})', SYNTHETIC_ROWS)


def inspect_database(database_path):
    with sqlite3.connect(database_path) as connection:
        integrity = connection.execute('PRAGMA integrity_check').fetchone()[0]
        tables = sorted(row[0] for row in connection.execute(
            "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name"))
        secrets_columns = [row[1] for row in connection.execute('PRAGMA table_info("Secrets")')]
        history = []
        if '__EFMigrationsHistory' in tables:
            history = [list(row) for row in connection.execute(
                'SELECT MigrationId, ProductVersion FROM "__EFMigrationsHistory" ORDER BY MigrationId')]
        rows = []
        if 'Secrets' in tables:
            columns = set(secrets_columns)
            if {'Id', 'EncryptedValue', 'Version', 'SecretId'} <= columns:
                selected = [column for column in (
                    'Id', 'SecretId', 'Name', 'Scope', 'Description', 'Version', 'IsLatest',
                    'Status', 'TenantId', 'Owner', 'ExpiresIn', 'ExpiresAt', 'LastAccessedAt',
                    'CreatedAt', 'UpdatedAt', 'EncryptedValue') if column in columns]
                query = ', '.join(f'"{column}"' for column in selected)
                for values in connection.execute(f'SELECT {query} FROM "Secrets" ORDER BY "Version"'):
                    row = dict(zip(selected, values))
                    sentinel = row.pop('EncryptedValue', None)
                    row['encryptedValueSha512'] = hashlib.sha512(str(sentinel).encode()).hexdigest() if sentinel is not None else None
                    rows.append(row)
            else:
                row_count = connection.execute('SELECT COUNT(*) FROM "Secrets"').fetchone()[0]
                rows = [{'rowCount': row_count, 'legacyEncryptedValueColumnPresent': False}]
        return {
            'integrityCheck': integrity,
            'tables': tables,
            'secretsColumns': secrets_columns,
            'migrationHistory': history,
            'syntheticRows': rows,
        }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--update-lockfiles', action='store_true',
                        help='Regenerate the committed package lockfiles from the pinned artifacts')
    parser.add_argument('--report-out', type=Path,
                        help='Write the machine-readable fixture result to this path')
    args = parser.parse_args()
    manifest = json.loads(MANIFEST.read_text())
    if manifest['targetFramework'] != 'net10.0':
        raise ValueError('Unexpected fixture target framework')

    with tempfile.TemporaryDirectory(prefix='elsa-secrets-sqlite-upgrade-') as temp_name:
        temp_root = Path(temp_name)
        verified, lock_summaries, config, packages_dir = lock_phases(
            manifest, temp_root, update_lockfiles=args.update_lockfiles)
        if args.update_lockfiles:
            print(json.dumps({'lockfilesUpdated': True, 'verifiedPackages': verified}, indent=2))
            return 0

        database_path = temp_root / 'secrets.db'
        old_migration = run_phase(OLD_PROJECT, config, packages_dir, 'migrate', database_path)
        if old_migration['result'] != 'migrated':
            raise RuntimeError(f'Extensions 3.8.1 migration did not complete: {old_migration}')
        seed_synthetic_rows(database_path)
        before = inspect_database(database_path)
        if before['integrityCheck'] != 'ok' or len(before['syntheticRows']) != 2:
            raise RuntimeError(f'Invalid Extensions 3.8.1 fixture state: {before}')

        core_upgrade = run_phase(CORE_PROJECT, config, packages_dir, 'upgrade', database_path)
        after = inspect_database(database_path)
        if after['integrityCheck'] != 'ok' or after['syntheticRows'] != before['syntheticRows']:
            raise RuntimeError(f'Core upgrade did not preserve the synthetic SQLite fixture rows: before={before}, after={after}')
        old_reopen = None
        if core_upgrade['result'] == 'failed':
            old_reopen = run_phase(OLD_PROJECT, config, packages_dir, 'inspect', database_path)
            expected_reopen = {
                'result': 'readable',
                'rowCount': 2,
                'versions': [1, 2],
                'secretIds': [SYNTHETIC_SECRET_ID],
                'latestFlags': [False, True],
                'encryptedValueSha256': [
                    hashlib.sha256(row['EncryptedValue'].encode()).hexdigest().upper()
                    for row in SYNTHETIC_ROWS
                ],
            }
            if any(old_reopen.get(key) != value for key, value in expected_reopen.items()):
                raise RuntimeError(f'Old package graph could not reopen after failed upgrade: {old_reopen}')
            if after != before:
                raise RuntimeError(f'Failed upgrade changed the old database state: before={before}, after={after}')

        report = {
            'fixture': 'Extensions 3.8.1 -> Core 3.8.4',
            'targetFramework': manifest['targetFramework'],
            'verifiedPackages': verified,
            'packageLocks': lock_summaries,
            'oldMigration': old_migration,
            'databaseBeforeCore': before,
            'coreUpgrade': core_upgrade,
            'databaseAfterCore': after,
            'oldPackageReopenAfterFailure': old_reopen,
            'schemaMigrationCodeChanged': False,
            'credentialConversionVerified': False,
        }
        rendered_report = json.dumps(report, indent=2) + '\n'
        if args.report_out:
            args.report_out.parent.mkdir(parents=True, exist_ok=True)
            args.report_out.write_text(rendered_report)
        print(rendered_report, end='')
    return 0


if __name__ == '__main__':
    try:
        sys.exit(main())
    except Exception as error:
        print(f'Fixture failed: {type(error).__name__}: {error}', file=sys.stderr)
        sys.exit(1)
