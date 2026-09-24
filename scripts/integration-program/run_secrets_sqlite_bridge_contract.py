#!/usr/bin/env python3
"""Verify a synthetic old Data Protection -> Core AES-GCM Secrets bridge."""
import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import shutil
import sqlite3
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

from secrets_sqlite_bridge_mapping import run_contract_fixtures


ROOT = Path(__file__).resolve().parents[2]
FIXTURE = Path(__file__).resolve().parent / 'secrets-sqlite-bridge-contract'
MANIFEST = FIXTURE / 'artifacts.json'
NUGET_FLAT = 'https://api.nuget.org/v3-flatcontainer'
OLD_PROJECT = FIXTURE / 'extensions-3.8.1/ExtensionsCryptoRunner.csproj'
CORE_PROJECT = FIXTURE / 'core-3.8.4/CoreCryptoRunner.csproj'
CURRENT_PROJECT = FIXTURE / 'current-core/CurrentCoreBridgeRunner.csproj'
PINNED_DOTNET_SDK_VERSION = '10.0.300'
PINNED_TARGET_CORE_SOURCE_COMMIT = '7b06b82d0ea89c12d49c3c28da8d770bfca13faf'
PINNED_CORE_BUILD_INPUTS = (
    'src',
    'Directory.Build.props',
    'Directory.Build.targets',
    'Directory.Packages.props',
    'global.json',
    'NuGet.Config',
    'nuget.config',
)
TRANSIENT_PACKAGE_HTTP_CODES = {429, 500, 502, 503, 504}


def run(command, *, cwd=None, env=None, capture=False, timeout=None):
    try:
        return subprocess.run(
            command,
            cwd=cwd,
            env=env,
            check=True,
            text=True,
            stdout=subprocess.PIPE if capture else None,
            stderr=subprocess.STDOUT if capture else None,
            timeout=timeout,
        )
    except subprocess.TimeoutExpired as error:
        output = error.stdout or ''
        if isinstance(output, bytes):
            output = output.decode(errors='replace')
        raise RuntimeError(
            f'Command exceeded its {timeout}s timeout: {command[0]}\n{output}'
        ) from error


def local_name(tag):
    return tag.rsplit('}', 1)[-1]


def package_url(package):
    if 'downloadUrl' in package:
        return package['downloadUrl']
    package_id = package['id'].lower()
    version = package['version']
    return f'{NUGET_FLAT}/{package_id}/{version}/{package_id}.{version}.nupkg'


def download_package(url):
    for attempt in range(4):
        try:
            with urllib.request.urlopen(url, timeout=60) as response:
                return response.read()
        except urllib.error.HTTPError as error:
            error.close()
            if error.code not in TRANSIENT_PACKAGE_HTTP_CODES or attempt == 3:
                raise
            print(f'Transient package HTTP {error.code}; retrying fixture download', file=sys.stderr)
            time.sleep(2 ** attempt)
    raise RuntimeError('Package download exhausted retries without a response')


def verify_package(package, phase, feed_dir):
    url = package_url(package)
    content = download_package(url)
    digest = hashlib.sha512(content).digest()
    if digest.hex() != package['sha512']:
        raise ValueError(f"SHA-512 mismatch for {package['id']} {package['version']}")

    file_name = f"{package['id'].lower()}.{package['version']}.nupkg"
    (feed_dir / file_name).write_bytes(content)
    with zipfile.ZipFile(feed_dir / file_name) as archive:
        nuspec_path = next(path for path in archive.namelist() if path.lower().endswith('.nuspec'))
        nuspec = ET.fromstring(archive.read(nuspec_path))
    metadata = next(node for node in nuspec if local_name(node.tag) == 'metadata')
    values = {local_name(node.tag): (node.text or '').strip() for node in metadata}
    repository = next((node.attrib for node in metadata if local_name(node.tag) == 'repository'), {})
    if values.get('id', '').casefold() != package['id'].casefold():
        raise ValueError(f"Unexpected nuspec ID in {package['id']} {package['version']}")
    if values.get('version') != package['version']:
        raise ValueError(f"Unexpected nuspec version in {package['id']} {package['version']}")
    expected_repository = package.get('repository', phase.get('repository'))
    expected_source_commit = package.get('sourceCommit', phase.get('sourceCommit'))
    if ((expected_repository and repository.get('url') != expected_repository)
            or (expected_source_commit and repository.get('commit') != expected_source_commit)):
        raise ValueError(f"Nuspec source provenance mismatch for {package['id']} {package['version']}")
    return {
        'id': package['id'],
        'version': package['version'],
        'url': url,
        'sha512': digest.hex(),
        'sha512Base64': base64.b64encode(digest).decode('ascii'),
        'repository': repository.get('url'),
        'sourceCommit': repository.get('commit'),
    }


def verify_dotnet_sdk(cwd=FIXTURE):
    version = run(['dotnet', '--version'], cwd=cwd, capture=True).stdout.strip()
    if version != PINNED_DOTNET_SDK_VERSION:
        raise RuntimeError(
            f'Expected .NET SDK {PINNED_DOTNET_SDK_VERSION} from the fixture global.json, got {version}')
    return version


def restore(project, config, packages_dir, *, update_lockfiles, cwd=FIXTURE):
    command = ['dotnet', 'restore', str(project), '--configfile', str(config), '--packages', str(packages_dir), '-m:1']
    if not update_lockfiles:
        command.append('--locked-mode')
    try:
        return run(command, cwd=cwd, capture=True, timeout=300).stdout
    except subprocess.CalledProcessError as error:
        raise RuntimeError(error.stdout) from error


def verify_lock(project, package):
    lock_path = project.parent / 'packages.lock.json'
    if not lock_path.exists():
        raise FileNotFoundError(f'Missing committed lockfile: {lock_path}')
    lock = json.loads(lock_path.read_text())
    resolved = lock['dependencies']['net10.0']
    item = next((value for package_id, value in resolved.items()
                 if package_id.casefold() == package['id'].casefold()), None)
    exact_range = f"[{package['version']}, {package['version']}]"
    if item is None or item['resolved'] != package['version'] or item['requested'] != exact_range:
        raise ValueError(f"Lockfile does not pin {package['id']} exactly to {package['version']}")
    return {
        'sha256': hashlib.sha256(lock_path.read_bytes()).hexdigest(),
        'resolvedPackage': {
            'id': package['id'],
            'version': item['resolved'],
            'requested': item['requested'],
            'nugetLockContentHash': item['contentHash'],
        },
    }


def run_phase(project, config, packages_dir, arguments, *, cwd=FIXTURE):
    env = os.environ.copy()
    env['NUGET_PACKAGES'] = str(packages_dir)
    command = [
        'dotnet', 'run', '--no-restore', '--configuration', 'Release',
        '--project', str(project), '--', *map(str, arguments),
    ]
    try:
        output = run(command, cwd=cwd, env=env, capture=True, timeout=900).stdout
    except subprocess.CalledProcessError as error:
        raise RuntimeError(error.stdout) from error
    try:
        return json.loads(next(line for line in reversed(output.splitlines()) if line.startswith('{')))
    except (StopIteration, json.JSONDecodeError) as error:
        raise RuntimeError(f'Runner did not return valid JSON: {output}') from error


def sha256_file(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(chunk)
    return digest.hexdigest()


def clone_sqlite(source, destination):
    with sqlite3.connect(source) as source_db, sqlite3.connect(destination) as destination_db:
        source_db.backup(destination_db)


def reject_fixture(current_project, config, packages_dir, old_key_ring, wrong_key_ring,
                   missing_key_ring, tenant_map, source_db, target_db, expected_code, *, cwd=ROOT):
    result = run_phase(
        current_project, config, packages_dir,
        [source_db, target_db, old_key_ring, wrong_key_ring, missing_key_ring, tenant_map, 'success'],
        cwd=cwd)
    if result.get('result') != 'rejected' or result.get('rejectionCode') != expected_code:
        raise RuntimeError(f'Expected {expected_code} rejection, got {result}')
    if result.get('targetUnchanged') is not True:
        raise RuntimeError(f'{expected_code} rejection changed the target: {result}')
    return result


def verify_current_core_source_pin(source_root=None):
    source_root = source_root or ROOT
    pin = subprocess.run(
        ['git', '-C', str(source_root), 'cat-file', '-e', f'{PINNED_TARGET_CORE_SOURCE_COMMIT}^{{commit}}'],
        check=False, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, text=True)
    if pin.returncode:
        raise ValueError(f'Unable to resolve pinned Core target {PINNED_TARGET_CORE_SOURCE_COMMIT}: {pin.stderr.strip()}')
    difference = subprocess.run(
        ['git', '-C', str(source_root), 'diff', '--quiet', PINNED_TARGET_CORE_SOURCE_COMMIT, '--', *PINNED_CORE_BUILD_INPUTS],
        check=False, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, text=True)
    if difference.returncode == 1:
        raise ValueError(f'Core source/build inputs differ from pinned target {PINNED_TARGET_CORE_SOURCE_COMMIT}')
    if difference.returncode:
        raise RuntimeError(f'Unable to compare pinned Core source/build inputs: {difference.stderr.strip()}')

    untracked_source = subprocess.run(
        ['git', '-C', str(source_root), 'ls-files', '--others', '--exclude-standard', '--', *PINNED_CORE_BUILD_INPUTS],
        check=False, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
    if untracked_source.returncode:
        raise RuntimeError(f'Unable to inspect untracked Core source/build inputs: {untracked_source.stderr.strip()}')
    if untracked_source.stdout.strip():
        paths = ', '.join(untracked_source.stdout.splitlines())
        raise ValueError(f'Untracked Core source/build inputs are present outside pinned target {PINNED_TARGET_CORE_SOURCE_COMMIT}: {paths}')


def prepare_current_core_checkout(destination):
    run(['git', 'clone', '--shared', '--no-checkout', str(ROOT), str(destination)],
        capture=True, timeout=300)
    run(['git', '-C', str(destination), 'checkout', '--detach', PINNED_TARGET_CORE_SOURCE_COMMIT],
        cwd=destination, capture=True, timeout=300)
    verify_current_core_source_pin(destination)

    project_relative_path = CURRENT_PROJECT.relative_to(ROOT)
    project_directory = destination / project_relative_path.parent
    if project_directory.exists():
        shutil.rmtree(project_directory)
    shutil.copytree(
        CURRENT_PROJECT.parent,
        project_directory,
        ignore=shutil.ignore_patterns('bin', 'obj', '.vs'),
    )
    return project_directory / CURRENT_PROJECT.name


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--update-lockfiles', action='store_true',
                        help='Regenerate both locked package graphs from the pinned artifacts')
    parser.add_argument('--report-out', type=Path,
                        help='Write the machine-readable proof result to this path')
    args = parser.parse_args()
    manifest = json.loads(MANIFEST.read_text())
    if manifest['targetFramework'] != 'net10.0':
        raise ValueError('Unexpected fixture target framework')
    mapping_contract = run_contract_fixtures()
    sdk_version = verify_dotnet_sdk()

    with tempfile.TemporaryDirectory(prefix='elsa-secrets-bridge-contract-') as temp_name:
        temp_root = Path(temp_name)
        feed_dir = temp_root / 'feed'
        packages_dir = temp_root / 'nuget-packages'
        feed_dir.mkdir()
        packages_dir.mkdir()
        artifact_phases = dict(manifest['phases'])
        artifact_phases['source-build-tooling'] = manifest['source-build-tooling']
        verified = {
            name: [verify_package(package, phase, feed_dir) for package in phase['packages']]
            for name, phase in artifact_phases.items()
        }

        config = temp_root / 'NuGet.Config'
        feed = str(feed_dir).replace('&', '&amp;')
        verified_patterns = ''.join(
            f'<package pattern="{package["id"]}" />'
            for phase in artifact_phases.values()
            for package in phase['packages'])
        config.write_text(
            '<?xml version="1.0" encoding="utf-8"?>\n'
            '<configuration><packageSources><clear />'
            f'<add key="verified-artifacts" value="{feed}" />'
            f'<add key="nuget.org" value="{NUGET_FLAT.rsplit("/v3-flatcontainer", 1)[0]}/v3/index.json" />'
            '</packageSources><packageSourceMapping>'
            f'<packageSource key="verified-artifacts">{verified_patterns}</packageSource>'
            '<packageSource key="nuget.org"><package pattern="*" /></packageSource>'
            '</packageSourceMapping></configuration>\n'
        )

        current_core_source = temp_root / 'pinned-current-core'
        current_project = prepare_current_core_checkout(current_core_source)

        lock_summaries = {}
        for project, phase_name in ((OLD_PROJECT, 'extensions-3.8.1'), (CORE_PROJECT, 'core-3.8.4')):
            restore(project, config, packages_dir, update_lockfiles=args.update_lockfiles)
            if not args.update_lockfiles:
                lock_summaries[phase_name] = verify_lock(
                    project, manifest['phases'][phase_name]['packages'][0])
        if args.update_lockfiles:
            print(json.dumps({'lockfilesUpdated': True, 'verifiedPackages': verified}, indent=2))
            return 0
        restore(current_project, config, packages_dir, update_lockfiles=False, cwd=FIXTURE)

        old_key_ring = temp_root / 'old-data-protection-keys'
        wrong_key_ring = temp_root / 'wrong-data-protection-keys'
        missing_key_ring = temp_root / 'missing-data-protection-keys'
        ciphertext_path = temp_root / 'synthetic-legacy-ciphertext.txt'
        source_db = temp_root / 'legacy-source.db'
        seed_result = run_phase(
            OLD_PROJECT, config, packages_dir,
            ['seed', source_db, old_key_ring, wrong_key_ring, ciphertext_path])
        historical_core_result = run_phase(
            CORE_PROJECT, config, packages_dir,
            [old_key_ring, wrong_key_ring, missing_key_ring, ciphertext_path])
        tenant_map_path = temp_root / 'tenant-map.json'
        tenant_map_path.write_text(json.dumps({'tenant-a': 'tenant-a', 'tenant-b': 'tenant-b'}))
        current_target_db = temp_root / 'current-core-target.db'
        if current_target_db.exists() or current_target_db.is_symlink():
            raise RuntimeError(f'Current-Core target path was not fresh: {current_target_db}')
        current_result = run_phase(
            current_project, config, packages_dir,
            [source_db, current_target_db, old_key_ring, wrong_key_ring, missing_key_ring,
             tenant_map_path, 'success'],
            cwd=FIXTURE)

        if seed_result.get('phase') != 'extensions-3.8.1' or seed_result.get('result') != 'seeded':
            raise RuntimeError(f"Legacy phase did not seed the synthetic SQLite source: {seed_result}")
        expected_hash = seed_result['plaintextSha256']
        if len(expected_hash) != 2:
            raise RuntimeError(f"Legacy phase did not create two crypto-proof versions: {seed_result}")
        required_core_results = {
            'result': 'verified',
            'syntheticVersions': 2,
            'sourcePlaintextSha256': expected_hash,
            'coreReadBackSha256': expected_hash,
            'roundTripPassed': True,
            'rawLegacyCiphertextCopyRejected': True,
            'wrongLegacyKeyRingRejected': True,
            'missingLegacyKeyRingRejected': True,
            'wrongCoreEncryptionKeyRejected': True,
            'missingCoreEncryptionKeyRejected': True,
            'corePayloadHasProtectedValue': True,
            'legacyVersionMetadataPreserved': True,
            'latestActiveVersion': 2,
            'previousVersionExpired': True,
        }
        mismatches = {key: {'expected': value, 'actual': historical_core_result.get(key)}
                      for key, value in required_core_results.items()
                      if historical_core_result.get(key) != value}
        if mismatches:
            raise RuntimeError(f'Historical Core synthetic crypto proof did not meet its contract: {mismatches}')

        expected_current = {
            'result': 'converted',
            'targetCoreSourceCommit': PINNED_TARGET_CORE_SOURCE_COMMIT,
            'targetMigrationIds': [
                '20260531141623_Initial',
                '20260825230122_SecretTenancy',
                '20260914120000_SecretDefaultTenantUniqueness',
                '20260923164123_ManagedSecretOwnership',
            ],
            'sourceRows': 5,
            'targetAggregates': 4,
            'targetVersions': 5,
            'persistedAggregates': 4,
            'persistedVersions': 5,
            'sidecarRows': 5,
            'nativeTenantMappingVerified': True,
            'defaultTenantStoredAsEmpty': True,
            'crossTenantIsolationVerified': True,
            'crossTenantSameNameVerified': True,
            'crossTenantWriteDenied': True,
            'sidecarFieldValuesExact': True,
            'lifecycleOwnershipMarkersNotInvented': True,
            'encryptedValuesRewritten': True,
            'rawLegacyCiphertextRejectedByCoreStore': True,
            'wrongCoreKeyRejected': True,
            'legacyOwnerAuthorizationAdapterRequired': True,
            'legacyIdCompatibilityAdapterRequired': True,
            'cutoverAllowed': False,
            'plaintextPrinted': False,
            'keysPrinted': False,
            'ciphertextPrinted': False,
        }
        current_mismatches = {key: {'expected': value, 'actual': current_result.get(key)}
                              for key, value in expected_current.items()
                              if current_result.get(key) != value}
        if current_mismatches:
            raise RuntimeError(f'Current-Core SQLite bridge proof did not meet its contract: {current_mismatches}')

        failure_scenarios = {}
        source_before = sha256_file(source_db)
        collision_target = temp_root / 'collision-target.db'
        if collision_target.exists() or collision_target.is_symlink():
            raise RuntimeError(f'Collision target path was not fresh: {collision_target}')
        collision_result = run_phase(
            current_project, config, packages_dir,
            [source_db, collision_target, old_key_ring, wrong_key_ring, missing_key_ring,
             tenant_map_path, 'id-collision'],
            cwd=FIXTURE)
        if collision_result.get('result') != 'rejected' or collision_result.get('rejectionCode') != 'AggregateIdCollision' or collision_result.get('targetUnchanged') is not True:
            raise RuntimeError(f'Aggregate-ID collision did not fail closed: {collision_result}')
        failure_scenarios['aggregateIdCollision'] = collision_result

        rollback_target = temp_root / 'rollback-target.db'
        if rollback_target.exists() or rollback_target.is_symlink():
            raise RuntimeError(f'Rollback target path was not fresh: {rollback_target}')
        rollback_result = run_phase(
            current_project, config, packages_dir,
            [source_db, rollback_target, old_key_ring, wrong_key_ring, missing_key_ring,
             tenant_map_path, 'fail-after-core-save'],
            cwd=FIXTURE)
        if rollback_result.get('result') != 'rejected' or rollback_result.get('rejectionCode') != 'InjectedWriteFailure' or rollback_result.get('targetUnchanged') is not True:
            raise RuntimeError(f'Injected write failure did not roll back the target: {rollback_result}')
        failure_scenarios['injectedAfterCoreSave'] = rollback_result

        rejection_mutations = {
            'unknownMigrationHistory': (
                "UPDATE __EFMigrationsHistory SET MigrationId='99999999999999_Unknown'", 'UnknownSourceMigrationHistory'),
            'unknownSchema': ("ALTER TABLE Secrets ADD COLUMN Unexpected TEXT NULL", 'UnknownSourceSchema'),
            'unmappedTenant': ("UPDATE Secrets SET TenantId='tenant-c' WHERE Id='legacy-row-tenant-a-v1'", 'UnmappedTenant'),
            'unknownStatus': ("UPDATE Secrets SET Status=99 WHERE Id='legacy-row-tenant-a-v1'", 'UnknownStatus'),
            'invalidLatestMarker': ("UPDATE Secrets SET IsLatest=1 WHERE Id='legacy-row-default-v1'", 'InvalidLatestMarker'),
            'normalizedNameCollision': ("UPDATE Secrets SET TenantId='' WHERE Id='legacy-row-tenant-a-v1'", 'NormalizedNameCollision'),
        }
        for name, (mutation, rejection_code) in rejection_mutations.items():
            candidate_db = temp_root / f'{name}.db'
            clone_sqlite(source_db, candidate_db)
            with sqlite3.connect(candidate_db) as candidate:
                candidate.execute(mutation)
                candidate.commit()
            candidate_target = temp_root / f'{name}-target.db'
            if candidate_target.exists() or candidate_target.is_symlink():
                raise RuntimeError(f'Rejection target path was not fresh: {candidate_target}')
            failure_scenarios[name] = reject_fixture(
                current_project, config, packages_dir, old_key_ring, wrong_key_ring,
                missing_key_ring, tenant_map_path, candidate_db, candidate_target, rejection_code,
                cwd=FIXTURE)

        existing_target = temp_root / 'existing-target.db'
        existing_target.write_bytes(b'preserve existing target bytes')
        existing_target_hash = sha256_file(existing_target)
        existing_target_result = run_phase(
            current_project, config, packages_dir,
            [source_db, existing_target, old_key_ring, wrong_key_ring, missing_key_ring,
             tenant_map_path, 'success'],
            cwd=FIXTURE)
        if (existing_target_result.get('result') != 'rejected'
                or existing_target_result.get('rejectionCode') != 'TargetAlreadyExists'
                or existing_target_result.get('targetUnchanged') is not True
                or sha256_file(existing_target) != existing_target_hash):
            raise RuntimeError(f'Existing target was not preserved and rejected: {existing_target_result}')
        failure_scenarios['existingTargetPreserved'] = existing_target_result

        symlink_target = temp_root / 'symlink-target.db'
        symlink_target.write_bytes(b'preserve symlink destination bytes')
        symlink_target_hash = sha256_file(symlink_target)
        symlink_alias = temp_root / 'symlink-target-alias.db'
        symlink_alias.symlink_to(symlink_target)
        symlink_result = run_phase(
            current_project, config, packages_dir,
            [source_db, symlink_alias, old_key_ring, wrong_key_ring, missing_key_ring,
             tenant_map_path, 'success'],
            cwd=FIXTURE)
        if (symlink_result.get('result') != 'rejected'
                or symlink_result.get('rejectionCode') != 'TargetAlreadyExists'
                or symlink_result.get('targetUnchanged') is not True
                or symlink_alias.is_symlink() is not True
                or sha256_file(symlink_target) != symlink_target_hash):
            raise RuntimeError(f'Symlink target was not preserved and rejected without following it: {symlink_result}')
        failure_scenarios['symlinkTargetPreserved'] = symlink_result

        alias_result = run_phase(
            current_project, config, packages_dir,
            [source_db, source_db, old_key_ring, wrong_key_ring, missing_key_ring,
             tenant_map_path, 'success'],
            cwd=FIXTURE)
        if (alias_result.get('result') != 'rejected'
                or alias_result.get('rejectionCode') != 'InvalidDatabasePaths'
                or alias_result.get('targetUnchanged') is not True
                or sha256_file(source_db) != source_before):
            raise RuntimeError(f'Source/target alias was not rejected before mutation: {alias_result}')
        failure_scenarios['sourceTargetAlias'] = alias_result

        reopened = run_phase(OLD_PROJECT, config, packages_dir, ['verify', source_db, old_key_ring])
        if reopened.get('result') != 'reopened-and-verified' or reopened.get('sourceUnchangedReadable') is not True:
            raise RuntimeError(f'Original source did not reopen after current-Core fixture: {reopened}')
        source_after = sha256_file(source_db)
        if source_before != source_after:
            raise RuntimeError('The current-Core fixture changed the original legacy SQLite source file.')

        report = {
            'fixture': f'Synthetic Extensions 3.8.1 SQLite source -> Core SQLite target at {PINNED_TARGET_CORE_SOURCE_COMMIT}',
            'targetFramework': manifest['targetFramework'],
            'sdkVersion': sdk_version,
            'targetCoreSourceCommit': PINNED_TARGET_CORE_SOURCE_COMMIT,
            'verifiedPackages': verified,
            'packageLocks': lock_summaries,
            'dataProtectionPurpose': manifest['dataProtectionPurpose'],
            'dataProtectionApplicationName': manifest['dataProtectionApplicationName'],
            'mappingContract': mapping_contract,
            'legacyPhase': {
                'result': seed_result['result'],
                'schemaMaterialization': seed_result['schemaMaterialization'],
                'migrationSqlSha256': seed_result['migrationSqlSha256'],
                'normalMigrateDiagnostic': seed_result['normalMigrateDiagnostic'],
                'migrationIds': seed_result['migrationIds'],
                'syntheticRows': seed_result['syntheticRows'],
                'plaintextSha256': expected_hash,
            },
            'corePhase': {
                key: historical_core_result[key]
                for key in required_core_results
            },
            'currentCorePhase': current_result,
            'failClosedScenarios': failure_scenarios,
            'sourceFileSha256Before': source_before,
            'sourceFileSha256After': source_after,
            'sourceReopenedAndVerified': True,
            'plaintextPrinted': False,
            'keysPrinted': False,
            'ciphertextPrinted': False,
            'productionMigrationChanged': False,
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
