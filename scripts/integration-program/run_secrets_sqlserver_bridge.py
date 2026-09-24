#!/usr/bin/env python3
"""Run the synthetic Extensions 3.8.1 SQL Server -> pinned Core Secrets bridge."""
import argparse
import hashlib
import json
import os
import re
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import zipfile
import xml.etree.ElementTree as ET

import run_secrets_sqlserver_upgrade_fixture as sqlserver

ROOT = Path(__file__).resolve().parents[2]
FIXTURE = Path(__file__).resolve().parent / 'secrets-sqlserver-bridge'
MANIFEST = FIXTURE / 'artifacts.json'
OLD_PROJECT = FIXTURE / 'extensions-3.8.1/ExtensionsRunner.csproj'
CURRENT_PROJECT = FIXTURE / 'current-core/SqlServerBridgeRunner.csproj'
PINNED_CORE_COMMIT = 'c37e9d7a2fa7e7c2af802b211e3d59db45fc2f6f'
PINNED_SDK = '10.0.300'
CURRENT_STAGE = 'startup'


def run(command, *, cwd=None, env=None, capture=False, timeout=900):
    try:
        return subprocess.run(command, cwd=cwd, env=env, check=True, text=True,
                              stdout=subprocess.PIPE if capture else None,
                              stderr=subprocess.STDOUT if capture else None, timeout=timeout)
    except subprocess.CalledProcessError as error:
        raise RuntimeError(error.stdout or f'{command[0]} failed with exit code {error.returncode}') from error


def verify_local_tooling(package, feed, source_commit):
    source = FIXTURE / package['localPath']
    content = source.read_bytes()
    digest = hashlib.sha512(content).hexdigest()
    if digest != package['sha512']:
        raise ValueError('Pinned package-manifest tooling artifact SHA-512 mismatch')
    destination = feed / f"{package['id'].lower()}.{package['version']}.nupkg"
    destination.write_bytes(content)
    with zipfile.ZipFile(destination) as archive:
        nuspec = ET.fromstring(archive.read(next(name for name in archive.namelist() if name.lower().endswith('.nuspec'))))
    metadata = next(node for node in nuspec if node.tag.rsplit('}', 1)[-1] == 'metadata')
    fields = {node.tag.rsplit('}', 1)[-1]: (node.text or '').strip() for node in metadata}
    repository = next((node.attrib for node in metadata if node.tag.rsplit('}', 1)[-1] == 'repository'), {})
    if fields.get('id', '').casefold() != package['id'].casefold() or fields.get('version') != package['version']:
        raise ValueError('Pinned package-manifest tooling nuspec identity mismatch')
    if repository.get('commit') != source_commit:
        raise ValueError('Pinned package-manifest tooling nuspec source commit mismatch')
    return {'id': package['id'], 'version': package['version'], 'sha512': digest,
            'sourceCommit': source_commit, 'localArtifactVerified': True}


def sql_server_password(connection):
    values = dict(part.split('=', 1) for part in connection.split(';') if '=' in part)
    return next(value for key, value in values.items() if key.casefold() in ('password', 'pwd'))


def sqlcmd(container, password, query, database=None):
    command = ['exec', '--env', f'SQLCMDPASSWORD={password}', container,
               '/opt/mssql-tools18/bin/sqlcmd', '-S', 'localhost', '-U', 'sa', '-C']
    if database:
        command.extend(['-d', database])
    command.extend(['-Q', query, '-b'])
    return sqlserver.docker_run(command, redact=(password,))


def create_database(container, name, password):
    sqlcmd(container, password, f'CREATE DATABASE [{name}]')


def execute_sql(container, database, sql, password):
    return sqlcmd(container, password, sql, database)


def connection_for(base, database):
    builder = dict(part.split('=', 1) for part in base.split(';') if '=' in part)
    builder['Database'] = database
    return ';'.join(f'{key}={value}' for key, value in builder.items())


def run_old(action, connection, packages_dir, key_ring=None):
    env = os.environ.copy()
    env['NUGET_PACKAGES'] = str(packages_dir)
    arguments = [action, connection]
    if key_ring is not None:
        arguments.append(str(key_ring))
    command = ['dotnet', str(OLD_PROJECT.parent / 'bin/Release/net10.0/ExtensionsRunner.dll'), *arguments]
    try:
        output = run(command, cwd=ROOT, env=env, capture=True).stdout
    except RuntimeError as error:
        raise RuntimeError(str(error).replace(connection, '<redacted connection string>')) from error
    return json.loads(next(line for line in reversed(output.splitlines()) if line.startswith('{')))


def prepare_pinned_core(destination):
    run(['git', 'clone', '--shared', '--no-checkout', str(ROOT), str(destination)], capture=True)
    run(['git', '-C', str(destination), 'checkout', '--detach', PINNED_CORE_COMMIT], capture=True)
    relative = CURRENT_PROJECT.relative_to(ROOT)
    project_dir = destination / relative.parent
    project_dir.mkdir(parents=True, exist_ok=True)
    shutil.copytree(CURRENT_PROJECT.parent, project_dir, dirs_exist_ok=True,
                    ignore=shutil.ignore_patterns('bin', 'obj'))
    changed = subprocess.run(['git', '-C', str(destination), 'diff', '--quiet', PINNED_CORE_COMMIT,
                              '--', 'src', 'Directory.Build.props', 'Directory.Build.targets',
                              'Directory.Packages.props', 'global.json', 'NuGet.Config', 'nuget.config'],
                             check=False, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, text=True)
    if changed.returncode != 0:
        raise RuntimeError('Pinned Core source/build inputs differ from the reviewed source commit')
    return project_dir / CURRENT_PROJECT.name


def run_current(project, config, packages_dir, source_connection, target_connection,
                old_key_ring, wrong_key_ring, missing_key_ring, tenant_map, scenario, cwd):
    env = os.environ.copy()
    env['NUGET_PACKAGES'] = str(packages_dir)
    runner = project.parent / 'bin/Release/net10.0/SqlServerBridgeRunner.dll'
    command = ['dotnet', str(runner), source_connection, target_connection,
               str(old_key_ring), str(wrong_key_ring), str(missing_key_ring), str(tenant_map), scenario]
    try:
        output = run(command, cwd=cwd, env=env, capture=True).stdout
    except RuntimeError as error:
        safe = str(error)
        for connection in (source_connection, target_connection):
            safe = safe.replace(connection, '<redacted connection string>')
        raise RuntimeError(safe) from error
    return json.loads(next(line for line in reversed(output.splitlines()) if line.startswith('{')))


def run_rejection(project, config, packages, source, target_name, container, base_connection, password,
                  old_key, wrong_key, missing_key, tenant_map, scenario, cwd, expected,
                  expected_original_unchanged=True):
    source_before = run_old('snapshot', source, packages)
    create_database(container, target_name, password)
    target = connection_for(base_connection, target_name)
    result = run_current(project, config, packages, source, target, old_key, wrong_key,
                         missing_key, tenant_map, scenario, cwd)
    source_after = run_old('snapshot', source, packages)
    if source_after != source_before:
        raise RuntimeError(f'{expected} rejection changed source data')
    result['sourceUnchanged'] = True
    if result.get('result') != 'rejected' or result.get('rejectionCode') != expected:
        print(json.dumps({'expectedCode': expected, 'actualCode': result.get('rejectionCode'),
                          'actualResult': result.get('result')}), file=sys.stderr)
        raise RuntimeError(f'Expected {expected} rejection, got {result}')
    if result.get('conversionWritesUnchanged') is not True:
        print(json.dumps({'expectedCode': expected, 'conversionWritesUnchanged': result.get('conversionWritesUnchanged')}), file=sys.stderr)
        raise RuntimeError(f'{expected} rejection changed the destination: {result}')
    if result.get('originalDestinationUnchanged') is not expected_original_unchanged:
        print(json.dumps({'expectedCode': expected, 'expectedOriginalUnchanged': expected_original_unchanged,
                          'actualOriginalUnchanged': result.get('originalDestinationUnchanged')}), file=sys.stderr)
        raise RuntimeError(f'{expected} rejection changed its original fresh destination state: {result}')
    return result


def main():
    global CURRENT_STAGE
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--report-out', type=Path)
    args = parser.parse_args()
    manifest = json.loads(MANIFEST.read_text())
    if manifest['targetFramework'] != 'net10.0' or manifest['targetCoreSourceCommit'] != PINNED_CORE_COMMIT:
        raise ValueError('Unexpected fixture target framework or Core source pin')
    CURRENT_STAGE = 'sdk-version'
    sdk = subprocess.run(['dotnet', '--version'], cwd=FIXTURE, check=True, text=True,
                         stdout=subprocess.PIPE).stdout.strip()
    if sdk != PINNED_SDK:
        raise RuntimeError(f'Expected .NET SDK {PINNED_SDK}, got {sdk}')

    with tempfile.TemporaryDirectory(prefix='elsa-secrets-sqlserver-bridge-') as temp_name:
        temp = Path(temp_name)
        feed = temp / 'feed'
        packages = temp / 'packages'
        feed.mkdir()
        packages.mkdir()
        artifact_phases = dict(manifest['phases'])
        artifact_phases['source-build-tooling'] = manifest['source-build-tooling']
        CURRENT_STAGE = 'verified-package-artifacts'
        verified = {
            phase: [verify_local_tooling(package, feed, data.get('sourceCommit')) if 'localPath' in package
                    else sqlserver.verify_package(package, data, feed)
                    for package in data['packages']]
            for phase, data in artifact_phases.items()
        }
        config = temp / 'NuGet.Config'
        local = str(feed).replace('&', '&amp;')
        patterns = ''.join(f'<package pattern="{item["id"]}" />'
                           for phase in artifact_phases.values() for item in phase['packages'])
        config.write_text(
            '<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear />'
            f'<add key="verified-artifacts" value="{local}" />'
            '<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />'
            '</packageSources><packageSourceMapping>'
            f'<packageSource key="verified-artifacts">{patterns}</packageSource>'
            '<packageSource key="nuget.org"><package pattern="*" /></packageSource>'
            '</packageSourceMapping></configuration>\n')
        CURRENT_STAGE = 'old-package-restore'
        sqlserver.restore(OLD_PROJECT, config, packages, update_lockfiles=False)
        old_locks = sqlserver.verify_lock(OLD_PROJECT, manifest['phases']['extensions-3.8.1']['packages'], packages, feed)
        target_source = temp / 'pinned-core'
        CURRENT_STAGE = 'pinned-core-checkout'
        current_project = prepare_pinned_core(target_source)
        env = os.environ.copy()
        env['NUGET_PACKAGES'] = str(packages)
        CURRENT_STAGE = 'pinned-core-restore'
        run(['dotnet', 'restore', str(current_project), '--configfile', str(config),
             '--packages', str(packages), '-m:1'], cwd=target_source, env=env, capture=True, timeout=600)
        CURRENT_STAGE = 'runner-builds'
        run(['dotnet', 'build', str(OLD_PROJECT), '--no-restore', '--configuration', 'Release', '-m:1'],
            cwd=ROOT, env=env, capture=True, timeout=900)
        run(['dotnet', 'build', str(current_project), '--no-restore', '--configuration', 'Release', '-m:1'],
            cwd=target_source, env=env, capture=True, timeout=900)

        container = None
        try:
            CURRENT_STAGE = 'sqlserver-startup'
            container, base_connection = sqlserver.start_sqlserver(manifest['sqlServerImage'])
            password = sql_server_password(base_connection)
            old_key = temp / 'old-key-ring'
            wrong_key = temp / 'wrong-key-ring'
            missing_key = temp / 'missing-key-ring'
            tenant_map = temp / 'tenant-map.json'
            tenant_map.write_text(json.dumps({'tenant-a': 'tenant-a', 'tenant-b': 'tenant-b'}))

            source_name = f'bridge_source_{os.getpid()}'
            target_name = f'bridge_target_{os.getpid()}'
            create_database(container, source_name, password)
            create_database(container, target_name, password)
            source = connection_for(base_connection, source_name)
            target = connection_for(base_connection, target_name)

            CURRENT_STAGE = 'released-source-seed'
            seed = run_old('seed', source, packages, old_key)
            if (seed.get('result') != 'seeded'
                    or seed.get('appliedMigrations') != ['20241011092820_V3_3']
                    or seed.get('expiresInAllRepresentableBySqlTime') is not True
                    or seed.get('expiresInMaximumTicks') != 863999999999):
                raise RuntimeError('Released-package synthetic seed failed its migration/expiry contract')
            source_before = run_old('snapshot', source, packages)

            CURRENT_STAGE = 'core-conversion'
            converted = run_current(current_project, config, packages, source, target, old_key,
                                    wrong_key, missing_key, tenant_map, 'success', target_source)
            CURRENT_STAGE = 'released-source-reopen'
            reopened = run_old('inspect', source, packages, old_key)
            source_after = run_old('snapshot', source, packages)
            if reopened.get('result') != 'readable' or reopened.get('sourceUnchangedReadable') is not True:
                raise RuntimeError('Released package could not decrypt and reopen its source after conversion')
            if source_before != source_after:
                raise RuntimeError('Source rows changed during fresh-destination conversion')
            if converted.get('result') != 'converted' or converted.get('sourceRows') != 5:
                raise RuntimeError('Pinned Core bridge did not convert the expected synthetic rows')
            if seed.get('sourceAggregateIds') != converted.get('sourceAggregateIds'):
                raise RuntimeError('Source logical secret IDs changed during conversion')
            if seed.get('ciphertextSha256') != reopened.get('ciphertextSha256'):
                raise RuntimeError('Source ciphertext changed during conversion')
            if seed.get('sourceRowHashSha256') != reopened.get('sourceRowHashSha256'):
                raise RuntimeError('Full source row hash changed during conversion')
            if seed.get('plaintextSha256') != reopened.get('plaintextSha256'):
                raise RuntimeError('Fresh old-package plaintext hashes changed during conversion')
            if reopened.get('expiresInAllRepresentableBySqlTime') is not True:
                raise RuntimeError('Old-package reread found an ExpiresIn outside SQL Server time range')

            expected_migrations = [
                '20260531141743_Initial', '20260825230253_SecretTenancy',
                '20260914120000_SecretDefaultTenantUniqueness', '20260923164247_ManagedSecretOwnership',
            ]
            if converted.get('targetMigrationIds') != expected_migrations:
                raise RuntimeError('Pinned target migration IDs changed')
            if not all(converted.get(key) is True for key in (
                    'nativeTenantMappingVerified', 'crossTenantReadIsolationVerified',
                    'crossTenantSameNameVerified', 'sidecarFieldValuesExact',
                    'expiresAtMappingExact', 'expiresInSidecarExact',
                    'sourceExpiresInValuesSqlTimeRepresentable', 'encryptedValuesRewritten',
                    'wrongCoreKeyRejected', 'missingCoreKeyRejected', 'rawLegacyCiphertextRejectedByCoreStore',
                    'legacyOwnerAuthorizationAdapterRequired', 'legacyIdCompatibilityAdapterRequired',
                    'defaultTenantStoredAsEmpty', 'wrongOldKeyRejected', 'missingOldKeyRejected',
                    'wrongDataProtectionContextRejected')):
                raise RuntimeError('Bridge proof did not verify all mapping/encryption/tenant invariants')

            CURRENT_STAGE = 'rejection-scenarios'
            failure_scenarios = {}
            for suffix, scenario, expected, expected_original in (
                    ('id-collision', 'id-collision', 'AggregateIdCollision', False),
                    ('rollback', 'fail-after-core-save', 'InjectedWriteFailure', False),
                    ('wrong-old-key', 'wrong-old-key', 'OldKeyUnavailable', True),
                    ('missing-old-key', 'missing-old-key', 'OldKeyUnavailable', True),
                    ('wrong-data-protection-context', 'wrong-data-protection-context', 'OldKeyUnavailable', True),
                    ('wrong-core-key', 'wrong-core-key', 'WrongCoreKey', True),
                    ('missing-core-key', 'missing-core-key', 'MissingCoreKey', True)):
                CURRENT_STAGE = f'rejection-{suffix}'
                failure_scenarios[suffix] = run_rejection(
                    current_project, config, packages, source, f'bridge_{suffix}_{os.getpid()}',
                    container, base_connection, password, old_key, wrong_key, missing_key, tenant_map,
                    scenario, target_source, expected, expected_original_unchanged=expected_original)

            CURRENT_STAGE = 'rejection-preexisting-destination-schema'
            schema_target = f'bridge_schema_collision_{os.getpid()}'
            create_database(container, schema_target, password)
            execute_sql(container, schema_target, 'CREATE SCHEMA [Elsa]', password)
            execute_sql(container, schema_target, 'CREATE TABLE [Elsa].[Existing] ([Id] int)', password)
            schema_collision = run_current(current_project, config, packages, source,
                connection_for(base_connection, schema_target), old_key, wrong_key, missing_key,
                tenant_map, 'success', target_source)
            if (schema_collision.get('rejectionCode') != 'TargetAlreadyExists'
                    or schema_collision.get('conversionWritesUnchanged') is not True
                    or schema_collision.get('originalDestinationUnchanged') is not True):
                raise RuntimeError('Existing target schema did not fail closed')
            failure_scenarios['preexistingDestinationSchema'] = schema_collision

            CURRENT_STAGE = 'rejection-preexisting-destination-history'
            history_target = f'bridge_history_collision_{os.getpid()}'
            create_database(container, history_target, password)
            execute_sql(container, history_target, 'CREATE SCHEMA [Elsa]', password)
            execute_sql(container, history_target,
                        'CREATE TABLE [Elsa].[__EFMigrationsHistory] '
                        '([MigrationId] nvarchar(150) NOT NULL PRIMARY KEY, [ProductVersion] nvarchar(32) NOT NULL)', password)
            history_collision = run_current(current_project, config, packages, source,
                connection_for(base_connection, history_target), old_key, wrong_key, missing_key,
                tenant_map, 'success', target_source)
            if (history_collision.get('rejectionCode') != 'TargetAlreadyExists'
                    or history_collision.get('conversionWritesUnchanged') is not True
                    or history_collision.get('originalDestinationUnchanged') is not True):
                raise RuntimeError('Existing target migration history did not fail closed')
            failure_scenarios['preexistingDestinationHistory'] = history_collision

            source_scenarios = (
                ('source-history', "UPDATE [Elsa].[__EFMigrationsHistory] SET [MigrationId] = 'unexpected-history'", 'UnknownSourceMigrationHistory'),
                ('source-schema', 'CREATE TABLE [Elsa].[Unexpected] ([Id] int)', 'UnknownSourceSchema'),
                ('unknown-status', "UPDATE [Elsa].[Secrets] SET [Status] = 99 WHERE [Id] = 'legacy-row-default-v2'", 'UnknownStatus'),
                ('invalid-version', "UPDATE [Elsa].[Secrets] SET [Version] = 0 WHERE [Id] = 'legacy-row-default-v2'", 'InvalidVersionSequence'),
                ('invalid-latest-marker', "UPDATE [Elsa].[Secrets] SET [IsLatest] = 0 WHERE [Id] = 'legacy-row-default-v2'", 'InvalidLatestMarker'),
            )
            for suffix, mutation, expected in source_scenarios:
                CURRENT_STAGE = f'source-rejection-{suffix}'
                bad_source_name = f'bridge_{suffix}_{os.getpid()}'
                create_database(container, bad_source_name, password)
                bad_source = connection_for(base_connection, bad_source_name)
                bad_seed = run_old('seed', bad_source, packages, old_key)
                if bad_seed.get('result') != 'seeded':
                    raise RuntimeError(f'Could not seed {suffix} input')
                execute_sql(container, bad_source_name, mutation, password)
                failure_scenarios[suffix] = run_rejection(
                    current_project, config, packages, bad_source, f'bridge_{suffix}_target_{os.getpid()}',
                    container, base_connection, password, old_key, wrong_key, missing_key, tenant_map,
                    'success', target_source, expected)

            CURRENT_STAGE = 'redacted-report'
            report = {
                'fixture': 'Extensions 3.8.1 -> pinned Core SQL Server Secrets fresh-destination bridge',
                'targetFramework': manifest['targetFramework'],
                'sqlServerImage': manifest['sqlServerImage'],
                'targetCoreSourceCommit': PINNED_CORE_COMMIT,
                'fixtureCodeCommit': run(['git', '-C', str(ROOT), 'rev-parse', 'HEAD'], capture=True).stdout.strip(),
                'checkedSourceFiles': {
                    str(path.relative_to(ROOT)): hashlib.sha256(path.read_bytes()).hexdigest()
                    for path in (Path(__file__).resolve(), CURRENT_PROJECT.parent / 'Program.cs', OLD_PROJECT.parent / 'Program.cs')
                },
                'verifiedPackages': verified,
                'oldPackageLock': old_locks,
                'oldPackageSeed': seed,
                'bridge': converted,
                'oldPackageReopenAfterBridge': reopened,
                'failureScenarios': failure_scenarios,
                'sqlServerExpiresInLimit': 'The released SQL Server time column stores non-negative durations below 24 hours; longer durations are outside this source fixture and #6356 remains open.',
                'sourceConnectionStringPrinted': False,
                'generatedPasswordPrinted': False,
                'plaintextPrinted': False,
                'keysPrinted': False,
                'ciphertextPrinted': False,
                'cutoverAllowed': False,
                'unsupportedCases': [
                    'customer key-ring custody', 'arbitrary customer schemas',
                    'production migration and rollback', 'security review of a real converter',
                    'legacy owner authorization and ID-addressed API compatibility',
                    'legacy SQL Server ExpiresIn values of 24 hours or longer'
                ]
            }
            rendered = json.dumps(report, indent=2) + '\n'
            if args.report_out:
                args.report_out.parent.mkdir(parents=True, exist_ok=True)
                args.report_out.write_text(rendered)
            print(rendered, end='')
        finally:
            if container:
                subprocess.run(['docker', 'rm', '--force', container], stdout=subprocess.PIPE,
                               stderr=subprocess.STDOUT, text=True)

    return 0


if __name__ == '__main__':
    try:
        sys.exit(main())
    except Exception as error:
        detail = ''
        match = re.search(r'Bridge fixture failed(?: at [A-Za-z0-9_-]+)?: [A-Za-z0-9_]+(?: SQL error number=[0-9]+)?', str(error))
        if match:
            detail = f': {match.group(0)}'
        print(f'Fixture failed at {CURRENT_STAGE}: {type(error).__name__}{detail}', file=sys.stderr)
        sys.exit(1)
