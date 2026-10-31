#!/usr/bin/env python3
"""Artifact-only Studio npm reconciliation. Native publication policy is unset.

Proof artifacts can be verified, never published. No receipt or CLI flag confers
publication authority. Activation requires a separately reviewed native workflow,
protected environment, publisher identity and exact candidate/action policy.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import tempfile
import time
import urllib.parse
import zipfile

import consolidated_package_executor as executor
import consolidated_package_recovery as recovery
import prove_studio_npm_pair as pair

REGISTRIES = {'feedz': 'https://f.feedz.io/elsa-workflows/elsa-3/npm/',
              'npmjs': 'https://registry.npmjs.org/'}
WORKFLOW = '.github/workflows/publish-studio-original-pair.yml'
REGISTRY_PROVENANCE_POLICIES = {'feedz': 'no-registry-attestation', 'npmjs': 'publisher-invocation'}
REVIEWED_PUBLICATION_POLICY = None
# Feedz version-absence semantics require separate provider evidence.
VERIFIED_MISSING_STATUS = {"npmjs": 404}
MAX_ARCHIVE = 128 * 1024 * 1024
MAX_METADATA = 1024 * 1024
SHA = re.compile(r'[0-9a-f]{64}\Z')


class PublicationError(ValueError):
    """Only fixed codes escape the executor; provider/child bodies stay private."""


def require(condition, code):
    if not condition:
        raise PublicationError(code)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def load(path, limit):
    pair.regular(path)
    require(path.stat().st_size <= limit, 'input_limit')
    data = path.read_bytes()
    require(len(data) <= limit, 'input_limit')
    return recovery._json(data), digest(data)


def admit_original(archive, packet, *, api=None):
    """Caller pins original provider metadata + ZIP/receipt digests, not authority."""
    require(set(packet) == {'source_commit', 'source_tree', 'run_id', 'run_attempt', 'artifact_id',
                           'artifact_name', 'archive_sha256', 'receipt_sha256', 'version'}, 'packet_shape')
    require(all(isinstance(packet[k], str) and re.fullmatch(r'[0-9a-f]{40}', packet[k])
                for k in ('source_commit', 'source_tree'))
            and all(type(packet[k]) is int and packet[k] > 0 for k in ('run_id', 'run_attempt', 'artifact_id'))
            and all(isinstance(packet[k], str) and SHA.fullmatch(packet[k])
                    for k in ('archive_sha256', 'receipt_sha256')), 'packet_identity')
    pair.regular(archive)
    require(0 < archive.stat().st_size <= MAX_ARCHIVE, 'archive_limit')
    data = archive.read_bytes()
    require(len(data) <= MAX_ARCHIVE and digest(data) == packet['archive_sha256'], 'original_archive_changed')
    with tempfile.TemporaryDirectory(prefix='elsa-npm-admit-') as temporary:
        folder = Path(temporary).resolve() / 'retained'
        folder.mkdir()
        with zipfile.ZipFile(archive) as zipped:
            members = zipped.infolist()
            require(0 < len(members) <= 5 and len({m.filename for m in members}) == len(members)
                    and sum(m.file_size for m in members) <= MAX_ARCHIVE, 'zip_shape')
            for member in members:
                require('/' not in member.filename and '\\' not in member.filename
                        and not member.is_dir() and (member.external_attr >> 16) & 0o170000 in (0, 0o100000)
                        and not member.flag_bits & 1, 'zip_member')
                payload = zipped.read(member)
                require(len(payload) == member.file_size, 'zip_member')
                (folder / member.filename).write_bytes(payload)
        receipt, receipt_hash = load(folder / 'receipt.json', 8 * MAX_METADATA)
        require(receipt_hash == packet['receipt_sha256'] and receipt.get('success') is True
                and receipt.get('source_unchanged') is True and receipt.get('published') is False
                and receipt.get('publisher_authority') is False
                and receipt.get('consumer_inputs_unchanged_after_install') is True, 'producer_receipt')
        identity_keys = ('schema', 'source_repository', 'source_commit', 'source_tree', 'version',
                         'run_id', 'run_attempt', 'framework', 'published')
        if receipt.get('kind') == 'release-candidate':
            identity_keys += ('kind',)
        identity = {key: receipt[key] for key in identity_keys}
        expected = (pair.release_candidate_identity if receipt.get('kind') == 'release-candidate' else pair.identity)(
            packet['source_commit'], packet['source_tree'], packet['version'],
            str(packet['run_id']), str(packet['run_attempt']))
        require(identity == expected, 'producer_identity')
        prefix = 'studio-npm-candidate' if identity.get('kind') == 'release-candidate' else 'studio-npm-pair'
        require(packet['artifact_name'] == f"{prefix}-{packet['source_commit']}-{packet['run_id']}-{packet['run_attempt']}",
                'artifact_identity')
        artifacts = {}
        for key, name in (('wasm', pair.WASM), ('react', pair.REACT)):
            report = receipt[key]
            filename = report['file']
            require(isinstance(filename, str) and filename == name.removeprefix('@').replace('/', '-') + '-' + identity['version'] + '.tgz', 'archive_name')
            path = folder / filename
            checked = pair.verify_archive(path, name, identity, expected=report['inventory'],
                integrity=report['sha512_integrity'], wasm_integrity=artifacts[pair.WASM]['report']['sha512_integrity'] if key == 'react' else None)
            require(checked == report, 'archive_receipt_changed')
            artifacts[name] = {'bytes': path.read_bytes(), 'report': checked}
        expected_files = {'receipt.json', *(item['report']['file'] for item in artifacts.values())}
        retained_inputs = receipt.get('consumer_inputs', {})
        require(set(retained_inputs) == {'package.json', 'package-lock.json'}, 'consumer_inputs_missing')
        for key, entry in retained_inputs.items():
            require(entry.get('retained_file') == 'consumer-' + key, 'consumer_input_name')
            require(set(entry) == {'retained_file', 'sha256'} and entry['retained_file'] in
                    {'consumer-package.json', 'consumer-package-lock.json'}, 'consumer_input')
            _, actual = load(folder / entry['retained_file'], 8 * MAX_METADATA)
            require(actual == entry['sha256'], 'consumer_input_changed')
            expected_files.add(entry['retained_file'])
        require(set(m.filename for m in members) == expected_files, 'unexpected_zip_member')
        consumer = receipt.get('consumer', {})
        lock, _ = load(folder / 'consumer-package-lock.json', 8 * MAX_METADATA)
        local = pair.verify_local_lock(lock, {name: (folder / item['report']['file'], item['report'])
                                            for name, item in artifacts.items()}, folder.parent / 'private/consumer')
        require(consumer.get('elsa_resolution') == local, 'consumer_resolution_changed')
        package, _ = load(folder / 'consumer-package.json', MAX_METADATA)
        require(all(package.get('dependencies', {}).get(name) ==
                    'file:../../retained/' + item['report']['file'] for name, item in artifacts.items()), 'consumer_dependency_changed')
        commands = receipt.get('commands', [])
        expected_steps = ['node-version', 'node-identity', 'npm-identity', 'source-init', 'source-fetch',
                          'source-checkout', 'source-origin', 'sdk-identity', 'clientlibs', 'custom-elements-publish',
                          'pack-wasm', 'wrapper-install', 'wrapper-build', 'pack-react', 'consumer-lock-offline',
                          'consumer-install-offline', 'consumer-lifecycle-refresh', 'consumer-esm', 'consumer-commonjs', 'consumer-vite']
        require(receipt.get('stage') == 'complete' and isinstance(commands, list)
                and [row.get('step') for row in commands] == expected_steps
                and all(row.get('success') is True and row.get('exit_code') == 0 for row in commands), 'producer_stages')
        require(all(consumer.get(key) is True for key in ('offline_install', 'esm_import', 'commonjs_require', 'vite_build'))
                and consumer.get('assets', {}).get('lifecycle_scripts_enabled') is True
                and consumer.get('assets', {}).get('stale_assets_removed') is True
                and consumer.get('assets', {}).get('unmanaged_files_preserved') is True, 'consumer_proof')
        candidate = {'identity': identity, 'packet': packet, 'artifacts': artifacts}
        provider_admission(candidate, len(data), api=api)
        return candidate


def provider_admission(candidate, size, *, api=None):
    """Anonymous bounded provider readback; unavailable metadata is a hard stop."""
    packet = candidate['packet']
    api = api or executor.GitHubApi()
    prefix = 'repos/' + executor.REPOSITORY
    try:
        artifact = api.get(f"{prefix}/actions/artifacts/{packet['artifact_id']}")
        run = api.get(f"{prefix}/actions/runs/{packet['run_id']}/attempts/{packet['run_attempt']}")
        expiry = datetime.fromisoformat(artifact['expires_at'].replace('Z', '+00:00'))
        producer = artifact['workflow_run']
        require(type(artifact.get('id')) is int and artifact['id'] == packet['artifact_id']
                and artifact.get('name') == packet['artifact_name'] and artifact.get('size_in_bytes') == size
                and artifact.get('digest') == 'sha256:' + packet['archive_sha256'] and artifact.get('expired') is False
                and expiry.tzinfo is not None and expiry > datetime.now(timezone.utc)
                and producer.get('id') == packet['run_id'] and producer.get('head_sha') == packet['source_commit']
                and producer.get('repository_id') == producer.get('head_repository_id') == 151148482,
                'provider_artifact_changed')
        require(run.get('id') == packet['run_id'] and run.get('run_attempt') == packet['run_attempt']
                and run.get('head_sha') == packet['source_commit'] and run.get('status') == 'completed'
                and run.get('conclusion') == 'success' and run.get('path') == '.github/workflows/studio-npm-pair.yml'
                and run.get('repository', {}).get('id') == run.get('head_repository', {}).get('id') == 151148482
                and run.get('repository', {}).get('full_name') == run.get('head_repository', {}).get('full_name') == executor.REPOSITORY,
                'provider_run_changed')
        if candidate['identity'].get('kind') == 'release-candidate':
            require(run.get('event') == 'workflow_dispatch' and run.get('head_branch') == 'main', 'candidate_producer_not_main')
            require(candidate['identity']['version'].startswith('3.10.'), 'maintenance_producer_unconfigured')
        else:
            require(run.get('event') in {'push', 'workflow_dispatch'}, 'provider_run_changed')
    except PublicationError:
        raise
    except Exception:
        raise PublicationError('provider_metadata_unknown') from None


def metadata_url(registry, name, version):
    return REGISTRIES[registry] + urllib.parse.quote(name, safe='@') + '/' + version


class NpmReadTransport(executor.HttpTransport):
    def _allowed_url(self, url):
        return any(url.startswith(base) for base in REGISTRIES.values()) and safe_tarball(url)


def safe_tarball(url):
    try:
        parsed = urllib.parse.urlsplit(url)
        return (isinstance(url, str) and len(url) <= 2048 and not parsed.query and not parsed.fragment
                and not any(ord(c) <= 32 or ord(c) == 127 for c in url)
                and '\\' not in url and '\\' not in urllib.parse.unquote(parsed.path)
                and not any(ord(c) <= 32 or ord(c) == 127 for c in urllib.parse.unquote(parsed.path))
                and '%' not in urllib.parse.unquote(parsed.path)
                and '..' not in urllib.parse.unquote(parsed.path).split('/')
                and any(url.startswith(base) for base in REGISTRIES.values()))
    except (ValueError, TypeError):
        return False


def read(transport, url, limit):
    try:
        return transport.get(url, max_bytes=limit, timeout=25)
    except Exception:
        return recovery.ReadResult(None, b'', False, 'transport_failed')


def observe(candidate, registry, name, transport):
    item = candidate['artifacts'][name]
    unknown = {'package': name, 'state': 'unknown'}
    response = read(transport, metadata_url(registry, name, candidate['identity']['version']), MAX_METADATA)
    if not response.complete or response.failure_category is not None:
        return unknown
    if response.status == 404 and registry == 'feedz':
        # Version 404 alone is insufficient: reconcile the public full document.
        packument = read(transport, REGISTRIES[registry] + urllib.parse.quote(name, safe='@'), MAX_METADATA)
        try:
            value = recovery._json(packument.body)
            versions = value.get('versions')
            if (packument.complete and packument.failure_category is None and packument.status == 200
                    and value.get('name') == name and isinstance(versions, dict) and 0 < len(versions) <= 4096
                    and all(isinstance(key, str) and isinstance(row, dict) and row.get('name') == name
                            and row.get('version') == key for key, row in versions.items())
                    and candidate['identity']['version'] not in versions):
                return {'package': name, 'state': 'missing'}
        except (ValueError, AttributeError, TypeError):
            # Malformed public metadata cannot establish absence; keep unknown.
            pass
        return unknown
    if response.status == VERIFIED_MISSING_STATUS.get(registry):
        return {'package': name, 'state': 'missing'}
    if response.status != 200:
        return unknown
    try:
        metadata = recovery._json(response.body)
        if metadata.get('name') != name or metadata.get('version') != candidate['identity']['version']:
            return {'package': name, 'state': 'conflicting'}
        dist = metadata['dist']
        if dist.get('integrity') != item['report']['sha512_integrity']:
            return {'package': name, 'state': 'conflicting'}
        url = dist['tarball']
        if not safe_tarball(url) or not url.startswith(REGISTRIES[registry]):
            return unknown
        remote = read(transport, url, MAX_ARCHIVE)
        if not remote.complete or remote.failure_category is not None or remote.status != 200:
            return unknown
        return {'package': name, 'state': 'matching' if remote.body == item['bytes'] else 'conflicting'}
    except (ValueError, KeyError, TypeError, AttributeError):
        return unknown


def native_admission(candidate, registry, action, *, api=None):
    """Unconfigured until exact native workflow/environment/identity are reviewed."""
    policy = REVIEWED_PUBLICATION_POLICY
    require(isinstance(policy, dict), 'publication_policy_unconfigured')
    require(set(policy) == {'environment', 'environment_id', 'reviewer_rule_id', 'branch_rule_id',
                           'reviewer_ids', 'branch_policy', 'allowed_ref', 'actions', 'publisher_identity'}, 'publication_policy_invalid')
    require(all(type(policy[key]) is int and policy[key] > 0 for key in
                ('environment_id', 'reviewer_rule_id', 'branch_rule_id'))
            and isinstance(policy['environment'], str) and re.fullmatch(r'[A-Za-z0-9_-]{1,100}', policy['environment'])
            and isinstance(policy['reviewer_ids'], list) and 0 < len(policy['reviewer_ids']) <= 6
            and all(type(value) is int and value > 0 for value in policy['reviewer_ids'])
            and len(set(policy['reviewer_ids'])) == len(policy['reviewer_ids'])
            and isinstance(policy['actions'], list) and isinstance(policy['branch_policy'], dict), 'publication_policy_invalid')
    # Exact candidate, registry and requested action must be source-pinned before
    # credentials/OIDC are exposed. Arbitrary receipt/dispatch approval is ignored.
    binding = {'packet_sha256': digest(json.dumps(candidate['packet'], sort_keys=True, separators=(',', ':')).encode()),
               'registry': registry, 'action': action,
               'registry_provenance_policy': REGISTRY_PROVENANCE_POLICIES[registry]}
    require(binding in policy['actions'] and policy['publisher_identity'] == registry, 'candidate_action_not_authorized')
    env = os.environ
    source, run = env.get('GITHUB_SHA', ''), env.get('GITHUB_RUN_ID', '')
    require(env.get('GITHUB_ACTIONS') == 'true' and env.get('GITHUB_REPOSITORY') == executor.REPOSITORY
            and env.get('GITHUB_EVENT_NAME') == 'workflow_dispatch' and env.get('GITHUB_RUN_ATTEMPT') == '1'
            and re.fullmatch('[0-9a-f]{40}', source) and re.fullmatch('[1-9][0-9]*', run)
            and env.get('GITHUB_JOB') == 'publish' and policy['allowed_ref'] == 'refs/heads/main'
            and env.get('GITHUB_REF') == policy['allowed_ref']
            and env.get('GITHUB_WORKFLOW_SHA') == source
            and env.get('GITHUB_WORKFLOW_REF') == f"{executor.REPOSITORY}/{WORKFLOW}@{policy['allowed_ref']}", 'runtime_not_admitted')
    require(pair.git(pair.ROOT, 'rev-parse', 'HEAD') == source
            and pair.git(pair.ROOT, 'status', '--porcelain', '--untracked-files=no') == '', 'executor_source_changed')
    api = api or executor.GitHubApi()
    prefix = f'repos/{executor.REPOSITORY}'
    runtime = api.get(f'{prefix}/actions/runs/{run}/attempts/1')
    require(runtime.get('id') == int(run) and runtime.get('run_attempt') == 1 and runtime.get('head_sha') == source
            and runtime.get('event') == 'workflow_dispatch' and runtime.get('path') == WORKFLOW
            and runtime.get('status') == 'in_progress' and runtime.get('head_branch') == 'main'
            and runtime.get('repository', {}).get('full_name') == executor.REPOSITORY
            and runtime.get('head_repository', {}).get('full_name') == executor.REPOSITORY, 'native_run_changed')
    environment = api.get(f"{prefix}/environments/{policy['environment']}")
    require(environment.get('id') == policy['environment_id'] and environment.get('name') == policy['environment']
            and environment.get('can_admins_bypass') is False
            and environment.get('deployment_branch_policy') == {'protected_branches': False, 'custom_branch_policies': True}, 'native_environment_changed')
    rules = environment.get('protection_rules', [])
    require(len(rules) == 2 and {row.get('type') for row in rules} == {'branch_policy', 'required_reviewers'}, 'native_rules_changed')
    branch = next(row for row in rules if row['type'] == 'branch_policy')
    review = next(row for row in rules if row['type'] == 'required_reviewers')
    require(branch.get('id') == policy['branch_rule_id'] and review.get('id') == policy['reviewer_rule_id']
            and review.get('prevent_self_review') is True
            and isinstance(review.get('reviewers'), list)
            and all(isinstance(row, dict) and row.get('type') == 'User'
                    and type(row.get('reviewer', {}).get('id')) is int for row in review['reviewers'])
            and sorted(row['reviewer']['id'] for row in review['reviewers']) == sorted(policy['reviewer_ids']), 'native_rules_changed')
    branches = executor.paged(api, f"{prefix}/environments/{policy['environment']}/deployment-branch-policies", 'branch_policies')
    require([{k: row.get(k) for k in ('id', 'type', 'name')} for row in branches] == [policy['branch_policy']]
            and policy['branch_policy'].get('name') == 'main' and policy['branch_policy'].get('type') == 'branch', 'native_ref_changed')
    approvals = api.get(f'{prefix}/actions/runs/{run}/approvals')
    require(isinstance(approvals, list) and len(approvals) < 100, 'native_approval_unknown')
    selected = [row for row in approvals if any(value.get('id') == policy['environment_id']
                and value.get('name') == policy['environment'] for value in row.get('environments', []))]
    actor = runtime.get('triggering_actor', {}).get('id')
    require(type(actor) is int and actor > 0 and len(selected) == 1 and selected[0].get('state') == 'approved'
            and selected[0].get('user', {}).get('id') in policy['reviewer_ids']
            and selected[0]['user']['id'] != actor, 'native_approval_missing')
    # This records reviewed policy and observed native identity, not an uploaded
    # attestation. The producer S and later executor D are independently bound.
    return {'native_approval_verified': True, 'packet_sha256': binding['packet_sha256'],
            'producer_source_commit': candidate['identity']['source_commit'],
            'executor_source_commit': source, 'executor_run_id': int(run), 'executor_run_attempt': 1,
            'registry_provenance_policy': binding['registry_provenance_policy'],
            'registry_attestation_verified': False}


class NpmPublisher:
    def publish(self, candidate, registry, name, tag):
        # Called only after fresh native admission. The parent process still has
        # its workflow environment; activation must isolate credential exposure
        # in the native job/step. No inherited user npmrc,
        # lifecycle scripts, stderr, stdout or token value reaches the receipt.
        versions = {}
        for command in ('node', 'npm'):
            try:
                value = executor.bounded_child([command, '--version'], b'', deadline=time.monotonic()+10,
                                              limit=128, env={'PATH': os.environ.get('PATH', os.defpath)}).decode().strip().lstrip('v')
                require(re.fullmatch(r'[0-9]+\.[0-9]+\.[0-9]+', value), 'publisher_toolchain_invalid')
                versions[command] = tuple(int(part) for part in value.split('.'))
            except (executor.ExecutorError, UnicodeError):
                raise PublicationError('publisher_toolchain_invalid') from None
        require(versions['node'] >= (22, 14, 0) and versions['npm'] >= (11, 5, 1), 'publisher_toolchain_unsupported')
        with tempfile.TemporaryDirectory(prefix='elsa-npm-publish-') as temporary:
            folder = Path(temporary).resolve()
            archive = folder / candidate['artifacts'][name]['report']['file']
            archive.write_bytes(candidate['artifacts'][name]['bytes'])
            config = folder / 'npmrc'
            config.write_text('registry=' + REGISTRIES[registry] + '\n')
            config.chmod(0o600)
            env = {'PATH': os.environ.get('PATH', os.defpath), 'HOME': str(folder),
                   'NPM_CONFIG_USERCONFIG': str(config), 'NPM_CONFIG_GLOBALCONFIG': str(folder / 'global-npmrc'),
                   'NPM_CONFIG_CACHE': str(folder / 'cache'), 'NPM_CONFIG_IGNORE_SCRIPTS': 'true'}
            (folder / 'global-npmrc').write_text('')
            if registry == 'feedz':
                token = os.environ.get('ELSA_STUDIO_FEEDZ_PUBLISH_KEY_BASE64', '')
                require(0 < len(token) <= 8192 and re.fullmatch('[A-Za-z0-9+/=]+', token), 'publisher_credential_missing')
                config.write_text(config.read_text() + '//f.feedz.io/elsa-workflows/elsa-3/npm/:_authToken=' + token + '\n')
            else:
                for key in ('GITHUB_ACTIONS', 'GITHUB_WORKFLOW_REF', 'GITHUB_REPOSITORY', 'GITHUB_SERVER_URL',
                            'GITHUB_EVENT_NAME', 'GITHUB_REPOSITORY_ID', 'GITHUB_REPOSITORY_OWNER_ID', 'GITHUB_REF',
                            'GITHUB_SHA', 'GITHUB_RUN_ID', 'GITHUB_RUN_ATTEMPT', 'RUNNER_ENVIRONMENT',
                            'ACTIONS_ID_TOKEN_REQUEST_URL', 'ACTIONS_ID_TOKEN_REQUEST_TOKEN'):
                    require(isinstance(os.environ.get(key), str) and bool(os.environ[key]), 'publisher_identity_missing')
                    env[key] = os.environ[key]
            try:
                executor.bounded_child(['npm', 'publish', str(archive), '--registry', REGISTRIES[registry], '--tag', tag,
                                        '--ignore-scripts', '--access', 'public'], b'', deadline=time.monotonic()+120,
                                       limit=65536, env=env)
                return True
            except executor.ExecutorError:
                return False  # A failure can follow a successful remote write.


def execute(candidate, registry, *, operation='verify', transport=None, publisher=None):
    require(registry in REGISTRIES and operation in {'verify', 'publish'}, 'operation_invalid')
    identity = candidate['identity']
    require(not (registry == 'npmjs' and identity['version'].startswith('3.10.')), 'npmjs_line_refused')
    if operation == 'publish':
        require(identity.get('kind') == 'release-candidate', 'proof_publication_refused')
    transport = transport or NpmReadTransport(seconds=600, max_requests=24)
    states = [observe(candidate, registry, name, transport) for name in (pair.WASM, pair.REACT)]
    result = {'schema': 1, 'operation': operation, 'registry': registry, 'identity': identity,
              'original_archive_sha256': candidate['packet']['archive_sha256'], 'registry_observation_scope': 'anonymous_public', 'before': states,
              'after': list(states), 'uploads': [], 'native_admission': None, 'publisher_authority': False, 'success': False}
    if operation == 'verify':
        result['success'] = all(row['state'] in {'matching', 'missing'} for row in states)
        return result
    require(all(row['state'] in {'matching', 'missing'} for row in states), 'remote_pair_not_admitted')
    result['native_admission'] = native_admission(candidate, registry, operation)
    result['publisher_authority'] = True
    publisher = publisher or NpmPublisher()
    tag = 'next' if '-' in identity['version'] else 'latest'
    for index, name in enumerate((pair.WASM, pair.REACT)):
        # Reconcile both again before every write, including immutable resumes.
        states = [observe(candidate, registry, item, transport) for item in (pair.WASM, pair.REACT)]
        result['after'] = states
        if not all(row['state'] in {'matching', 'missing'} for row in states):
            return result
        if states[index]['state'] == 'matching':
            continue
        if name == pair.REACT and states[0]['state'] != 'matching':
            result['failure_code'] = 'wasm_dependency_not_matching_before_react'
            return result
        try:
            result['native_admission'] = native_admission(candidate, registry, operation)
            published = publisher.publish(candidate, registry, name, tag)
        except (PublicationError, executor.ExecutorError):
            result['failure_code'] = 'publication_admission_or_command_failed'
            return result
        except Exception:
            result['failure_code'] = 'publication_execution_unknown'
            return result
        result['uploads'].append({'package': name, 'command_succeeded': published})
        readback = observe(candidate, registry, name, transport)
        result['after'][index] = readback
        if not published or readback['state'] != 'matching':
            return result
    result['after'] = [observe(candidate, registry, name, transport) for name in (pair.WASM, pair.REACT)]
    result['success'] = all(row['state'] == 'matching' for row in result['after'])
    return result


def report_destination(output, archive, packet):
    require(not output.is_symlink(), 'unsafe_report_destination')
    destination = output.resolve()
    require(destination.parent.is_dir() and (not destination.exists() or destination.is_file()),
            'unsafe_report_destination')
    for original in (archive, packet):
        require(destination != original.resolve() and
                not (destination.exists() and original.exists() and destination.samefile(original)),
                'unsafe_report_destination')
    return destination


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--archive', type=Path, required=True)
    parser.add_argument('--packet', type=Path, required=True)
    parser.add_argument('--packet-sha256', required=True)
    parser.add_argument('--registry', choices=REGISTRIES, required=True)
    parser.add_argument('--operation', choices=('verify', 'publish'), default='verify')
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    try:
        output = report_destination(args.output, args.archive, args.packet)
        temporary_report = tempfile.TemporaryDirectory(prefix='.elsa-npm-report-', dir=output.parent)
    except Exception:
        print(json.dumps({'success': False, 'publisher_authority': False,
                          'failure_code': 'unsafe_report_destination'}))
        return 1
    try:
        packet, packet_hash = load(args.packet, MAX_METADATA)
        require(SHA.fullmatch(args.packet_sha256) and packet_hash == args.packet_sha256, 'packet_changed')
        candidate = admit_original(args.archive, packet)
        result = execute(candidate, args.registry, operation=args.operation)
    except (PublicationError, pair.ProofError, executor.ExecutorError) as error:
        result = {'success': False, 'publisher_authority': False, 'failure_code': str(error)}
    except Exception:
        result = {'success': False, 'publisher_authority': False, 'failure_code': 'input_or_execution_failed'}
    # No raw provider/child output or local input paths are retained.
    try:
        with temporary_report as temporary:
            staged = Path(temporary) / 'report.json'
            pair.write_json(staged, result)
            report_destination(output, args.archive, args.packet)
            # Replacing the leaf does not truncate an input through a late link.
            # Parent directories still require exclusive local ownership.
            os.replace(staged, output)
    except Exception:
        print(json.dumps({'success': False, 'publisher_authority': result['publisher_authority'],
                          'failure_code': 'report_write_failed'}))
        return 1
    print(json.dumps({'success': result['success'], 'publisher_authority': result['publisher_authority']}))
    return 0 if result['success'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
