#!/usr/bin/env python3
"""Characterize the public Extensions 3.8.1 -> Core 3.8.4 SQL Server migration handoff."""
import argparse
import base64
import hashlib
import json
import os
import socket
from pathlib import Path
import secrets
import string
import subprocess
import sys
import tempfile
import time
import urllib.request
import xml.etree.ElementTree as ET
import zipfile


ROOT = Path(__file__).resolve().parents[2]
FIXTURE = Path(__file__).resolve().parent / 'secrets-sqlserver-upgrade'
MANIFEST = FIXTURE / 'artifacts.json'
NUGET_FLAT = 'https://api.nuget.org/v3-flatcontainer'
OLD_PROJECT = FIXTURE / 'extensions-3.8.1/ExtensionsRunner.csproj'
CORE_PROJECT = FIXTURE / 'core-3.8.4/CoreRunner.csproj'
EXPECTED_OLD_MIGRATIONS = ['20241011092820_V3_3']
EXPECTED_CORE_PENDING_MIGRATIONS = ['20260531141743_Initial']
EXPECTED_SQLSERVER_ERROR_NUMBER = 2714
EXPECTED_SECRETS_COLUMNS = [
    'Id', 'SecretId', 'Name', 'Scope', 'EncryptedValue', 'Description', 'Version', 'IsLatest', 'Status',
    'ExpiresIn', 'ExpiresAt', 'LastAccessedAt', 'TenantId', 'CreatedAt', 'UpdatedAt', 'Owner',
]
EXPECTED_INDEXES = [
    'PK___EFMigrationsHistory',
    'IX_Secret_ExpiresAt', 'IX_Secret_LastAccessedAt', 'IX_Secret_Name', 'IX_Secret_Scope',
    'IX_Secret_Status', 'IX_Secret_TenantId', 'IX_Secret_Version', 'PK_Secrets',
]
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
        'IsLatest': False,
        'Status': 0,
        'ExpiresIn': '00:15:00',
        'ExpiresAt': '2030-01-02T03:04:05Z',
        'LastAccessedAt': '2026-09-23T10:00:00Z',
        'TenantId': 'synthetic-tenant',
        'CreatedAt': '2026-09-23T09:00:00Z',
        'UpdatedAt': '2026-09-23T09:00:00Z',
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
        'IsLatest': True,
        'Status': 0,
        'ExpiresIn': '00:15:00',
        'ExpiresAt': '2030-01-02T03:04:05Z',
        'LastAccessedAt': '2026-09-23T10:00:00Z',
        'TenantId': 'synthetic-tenant',
        'CreatedAt': '2026-09-23T09:00:00Z',
        'UpdatedAt': '2026-09-23T10:00:00Z',
        'Owner': 'synthetic-owner',
    },
]


def run(command, *, cwd=None, env=None, capture=False, input_text=None):
    return subprocess.run(
        command,
        cwd=cwd,
        env=env,
        input=input_text,
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


def verify_lock(project, packages, packages_dir, feed_dir):
    lock_path = project.parent / 'packages.lock.json'
    if not lock_path.exists():
        raise FileNotFoundError(f'Missing committed lockfile: {lock_path}')
    lock = json.loads(lock_path.read_text())
    resolved = lock['dependencies']['net10.0']
    assets_path = project.parent / 'obj/project.assets.json'
    assets = json.loads(assets_path.read_text())
    asset_package_folders = [Path(folder).resolve() for folder in assets.get('packageFolders', {})]
    if asset_package_folders != [packages_dir.resolve()]:
        raise ValueError(f'Runner assets do not use the isolated NuGet package cache: {asset_package_folders}')
    asset_libraries = assets.get('libraries', {})
    direct_packages = {}
    for expected in packages:
        package = next((value for package_id, value in resolved.items()
                        if package_id.casefold() == expected['id'].casefold()), None)
        if package is None:
            raise ValueError(f"Lockfile is missing {expected['id']}")
        exact_range = f"[{expected['version']}, {expected['version']}]"
        if package['resolved'] != expected['version'] or package['requested'] != exact_range:
            raise ValueError(f"Lockfile does not pin {expected['id']} exactly to {expected['version']}")
        package_id = expected['id'].lower()
        package_version = expected['version'].lower()
        cached_package_path = packages_dir / package_id / package_version / f'{package_id}.{package_version}.nupkg'
        if not cached_package_path.is_file():
            raise FileNotFoundError(f"NuGet cache is missing the restored package archive: {expected['id']} {expected['version']}")
        cached_hash = hashlib.sha512(cached_package_path.read_bytes()).hexdigest()
        if cached_hash != expected['sha512']:
            raise ValueError(f"NuGet cached archive does not match the verified artifact for {expected['id']}")
        metadata_path = cached_package_path.parent / '.nupkg.metadata'
        metadata = json.loads(metadata_path.read_text())
        source = metadata.get('source', '')
        if source.startswith('file://'):
            restored_source = Path(urllib.request.url2pathname(source.removeprefix('file://'))).resolve()
        elif '://' not in source:
            restored_source = Path(source).resolve()
        else:
            restored_source = None
        if restored_source != feed_dir.resolve():
            raise ValueError(f"NuGet cache source does not identify the verified feed for {expected['id']}: {source}")
        if package['contentHash'] != metadata.get('contentHash'):
            raise ValueError(f"Lockfile content hash does not match NuGet's cached package content hash for {expected['id']}")
        asset_key = next((key for key in asset_libraries
                          if key.casefold() == f'{expected["id"]}/{expected["version"]}'.casefold()), None)
        if asset_key is None:
            raise ValueError(f"Runner assets are missing direct package {expected['id']} {expected['version']}")
        asset_library = asset_libraries[asset_key]
        if asset_library.get('path') != f'{package_id}/{package_version}' or asset_library.get('sha512') != metadata['contentHash']:
            raise ValueError(f"Runner assets do not resolve {expected['id']} to the verified package cache path")
        direct_packages[expected['id']] = {
            'version': package['resolved'],
            'requested': package['requested'],
            'nugetLockContentHash': package['contentHash'],
            'nugetCachedContentHash': metadata['contentHash'],
            'verifiedArtifactSha512': expected['sha512'],
            'cachedPackageSha512': cached_hash,
            'packageCachePath': f'{package_id}/{package_version}/{package_id}.{package_version}.nupkg',
            'assetsLibraryPath': asset_library['path'],
            'restoredFromVerifiedFeed': True,
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
        '</packageSources>'
        '<packageSourceMapping>'
        '<packageSource key="verified-artifacts">'
        '<package pattern="Elsa.Secrets.Persistence.EFCore" />'
        '<package pattern="Elsa.Secrets.Persistence.EFCore.SqlServer" />'
        '</packageSource>'
        '<packageSource key="nuget.org"><package pattern="*" /></packageSource>'
        '</packageSourceMapping></configuration>\n'
    )

    for project, phase_name in ((OLD_PROJECT, 'extensions-3.8.1'), (CORE_PROJECT, 'core-3.8.4')):
        restore(project, config, packages_dir, update_lockfiles=update_lockfiles)
        if update_lockfiles:
            continue
        lock_summaries[phase_name] = verify_lock(
            project, manifest['phases'][phase_name]['packages'], packages_dir, feed_dir)
    return verified, lock_summaries, config, packages_dir


def run_phase(project, config, packages_dir, action, connection_string):
    env = os.environ.copy()
    env['NUGET_PACKAGES'] = str(packages_dir)
    command = [
        'dotnet', 'run', '--no-restore', '--configuration', 'Release',
        '--project', str(project), '--', action, connection_string,
    ]
    try:
        output = run(command, cwd=ROOT, env=env, capture=True).stdout
    except subprocess.CalledProcessError as error:
        password = connection_string.split('Password=', 1)[1].split(';', 1)[0]
        safe_output = (error.stdout or '').replace(connection_string, '<redacted connection string>')
        raise RuntimeError(safe_output.replace(password, '<redacted>')) from None
    return json.loads(next(line for line in reversed(output.splitlines()) if line.startswith('{')))


def docker_run(command, *, redact=()):
    try:
        # Docker may echo environment arguments on failure. Never inherit its
        # stdout/stderr for calls that carry the disposable SQL password.
        return run(['docker', *command], capture=True)
    except subprocess.CalledProcessError as error:
        output = error.stdout or ''
        safe_command = ' '.join(command)
        safe_output = output
        for value in redact:
            safe_command = safe_command.replace(value, '<redacted>')
            safe_output = safe_output.replace(value, '<redacted>')
        raise RuntimeError(f'docker {safe_command} failed: {safe_output}') from None


def start_sqlserver(image):
    name = f'elsa-secrets-sql-fixture-{os.getpid()}'
    password = ''.join((
        secrets.choice(string.ascii_uppercase), secrets.choice(string.ascii_lowercase),
        secrets.choice(string.digits), chr(33), secrets.token_urlsafe(28),
    ))
    docker_run([
        'run', '--pull=missing', '--platform', 'linux/amd64', '--detach', '--name', name,
        '--env', 'ACCEPT_EULA=Y', '--env', 'MSSQL_PID=Developer',
        '--env', f'MSSQL_SA_PASSWORD={password}',
        '--publish', '127.0.0.1::1433', image,
    ], redact=(password,))
    try:
        ports = json.loads(docker_run(['inspect', '--format', '{{json .NetworkSettings.Ports}}', name]).stdout)
        port = int(ports['1433/tcp'][0]['HostPort'])
        for _ in range(120):
            try:
                with socket.create_connection(('127.0.0.1', port), timeout=1):
                    pass
            except OSError:
                time.sleep(1)
                continue
            ready = subprocess.run([
                'docker', 'exec', '--env', f'SQLCMDPASSWORD={password}', name,
                '/opt/mssql-tools18/bin/sqlcmd', '-S', 'localhost', '-U', 'sa', '-C',
                '-Q', 'SELECT 1', '-b',
            ], text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            if ready.returncode == 0:
                docker_run([
                    'exec', '--env', f'SQLCMDPASSWORD={password}', name,
                    '/opt/mssql-tools18/bin/sqlcmd', '-S', 'localhost', '-U', 'sa', '-C',
                    '-Q', 'CREATE DATABASE [elsa_fixture]', '-b',
                ], redact=(password,))
                connection = (f'Server=127.0.0.1,{port};Database=elsa_fixture;User Id=sa;'
                              f'Password={password};Encrypt=True;TrustServerCertificate=True')
                return name, connection
            time.sleep(1)
        raise TimeoutError('SQL Server fixture container did not become ready')
    except Exception:
        subprocess.run(['docker', 'rm', '--force', name], text=True,
                       stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        raise


def expected_synthetic_rows():
    expected = []
    for row in SYNTHETIC_ROWS:
        preserved = {key: value for key, value in row.items() if key != 'EncryptedValue'}
        preserved['encryptedValueSha512'] = hashlib.sha512(row['EncryptedValue'].encode()).hexdigest()
        expected.append(preserved)
    return expected


def expected_old_reopen():
    return {
        'phase': 'extensions-3.8.1',
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


def validate_known_baseline(old_migration, core_upgrade, before, after, old_reopen):
    """Require the exact outcome observed for the hash-pinned package pair."""
    if old_migration.get('phase') != 'extensions-3.8.1' or old_migration.get('result') != 'migrated':
        raise ValueError(f'Unexpected Extensions 3.8.1 migration result: {old_migration}')
    if old_migration.get('appliedMigrations') != EXPECTED_OLD_MIGRATIONS:
        raise ValueError(f'Unexpected Extensions 3.8.1 migration IDs: {old_migration}')
    if core_upgrade.get('phase') != 'core-3.8.4' or core_upgrade.get('result') != 'failed':
        raise ValueError(f'Core 3.8.4 must reproduce the expected migration failure: {core_upgrade}')
    if core_upgrade.get('appliedMigrationsBeforeUpgrade') != EXPECTED_OLD_MIGRATIONS:
        raise ValueError(f'Unexpected applied migration IDs before Core upgrade: {core_upgrade}')
    if core_upgrade.get('pendingMigrationsBeforeUpgrade') != EXPECTED_CORE_PENDING_MIGRATIONS:
        raise ValueError(f'Unexpected pending Core migration IDs: {core_upgrade}')
    if (core_upgrade.get('exceptionType') != 'Microsoft.Data.SqlClient.SqlException'
            or core_upgrade.get('errorNumber') != EXPECTED_SQLSERVER_ERROR_NUMBER):
        raise ValueError(f'Unexpected Core migration exception: {core_upgrade}')
    if "already an object named 'Secrets'" not in core_upgrade.get('exceptionMessage', ''):
        raise ValueError(f'Unexpected Core migration exception message: {core_upgrade}')
    if before.get('migrationHistory') != [{'MigrationId': EXPECTED_OLD_MIGRATIONS[0], 'ProductVersion': '10.0.9'}]:
        raise ValueError(f'Unexpected pre-upgrade migration history: {before}')
    if before.get('syntheticRows') != expected_synthetic_rows():
        raise ValueError(f'Unexpected pre-upgrade synthetic rows: {before}')
    actual_secrets_columns = [column['name'] for column in before.get('columns', []) if column['table'] == 'Secrets']
    if actual_secrets_columns != EXPECTED_SECRETS_COLUMNS:
        raise ValueError(f'Unexpected pre-upgrade SQL Server Secrets schema: {before}')
    expiry_column = next(column for column in before['columns'] if column['table'] == 'Secrets' and column['name'] == 'ExpiresIn')
    if expiry_column['dataType'] != 'time':
        raise ValueError(f'Unexpected legacy SQL Server expiry storage type: {expiry_column}')
    actual_indexes = [index['name'] for index in before.get('indexes', [])]
    if actual_indexes != EXPECTED_INDEXES:
        raise ValueError(f'Unexpected pre-upgrade SQL Server indexes: {actual_indexes}')
    if after != before:
        raise ValueError(f'Core failure changed the captured SQL Server state: before={before}, after={after}')
    if old_reopen != expected_old_reopen():
        raise ValueError(f'Extensions 3.8.1 could not reopen and read the unchanged rows: {old_reopen}')


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

    container = None
    try:
        with tempfile.TemporaryDirectory(prefix='elsa-secrets-sqlserver-upgrade-') as temp_name:
            temp_root = Path(temp_name)
            verified, lock_summaries, config, packages_dir = lock_phases(
                manifest, temp_root, update_lockfiles=args.update_lockfiles)
            if args.update_lockfiles:
                print(json.dumps({'lockfilesUpdated': True, 'verifiedPackages': verified}, indent=2))
                return 0

            container, connection_string = start_sqlserver(manifest['sqlServerImage'])
            old_migration = run_phase(OLD_PROJECT, config, packages_dir, 'migrate', connection_string)
            if (old_migration.get('phase') != 'extensions-3.8.1'
                    or old_migration.get('result') != 'migrated'
                    or old_migration.get('appliedMigrations') != EXPECTED_OLD_MIGRATIONS):
                raise RuntimeError(f'Unexpected Extensions 3.8.1 migration result or IDs: {old_migration}')
            run_phase(OLD_PROJECT, config, packages_dir, 'seed', connection_string)
            before = run_phase(OLD_PROJECT, config, packages_dir, 'snapshot', connection_string)

            core_upgrade = run_phase(CORE_PROJECT, config, packages_dir, 'upgrade', connection_string)
            after = run_phase(OLD_PROJECT, config, packages_dir, 'snapshot', connection_string)
            old_reopen = run_phase(OLD_PROJECT, config, packages_dir, 'inspect', connection_string)
            validate_known_baseline(old_migration, core_upgrade, before, after, old_reopen)

            report = {
                'fixture': 'Extensions 3.8.1 -> Core 3.8.4 SQL Server',
                'targetFramework': manifest['targetFramework'],
                'sqlServerImage': manifest['sqlServerImage'],
                'verifiedPackages': verified,
                'packageLocks': lock_summaries,
                'oldMigration': old_migration,
                'databaseBeforeCore': before,
                'coreUpgrade': core_upgrade,
                'databaseAfterCore': after,
                'oldPackageReopenAfterFailure': old_reopen,
                'schemaMigrationCodeChanged': False,
                'credentialConversionVerified': False,
                'downgradeExecuted': False,
            }
            rendered_report = json.dumps(report, indent=2) + '\n'
            if args.report_out:
                args.report_out.parent.mkdir(parents=True, exist_ok=True)
                args.report_out.write_text(rendered_report)
            print(rendered_report, end='')
        return 0
    finally:
        if container:
            subprocess.run(['docker', 'rm', '--force', container], text=True,
                           stdout=subprocess.PIPE, stderr=subprocess.STDOUT)


if __name__ == '__main__':
    try:
        sys.exit(main())
    except Exception as error:
        print(f'Fixture failed: {type(error).__name__}: {error}', file=sys.stderr)
        sys.exit(1)
