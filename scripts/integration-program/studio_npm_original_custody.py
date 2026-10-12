#!/usr/bin/env python3
"""Fixed original-input custody for #8736; registry verification stays in #8734.

Keep as the reviewed publisher input stage, or remove after a reviewed replacement
and immutable original custody/recovery proof. This grants no publisher authority.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import os
from pathlib import Path
import re

import consolidated_recovery_inputs as paths
import publish_studio_npm_pair as publication

PACKET_SHA256 = '0af4dd27274201e2cb6d13d8d4709304cd4e0da2c0603260d3e3355e4c3703f4'
ARCHIVE_SIZE = 94760475
TRANSPORT_FILES = {'original.zip', 'packet.json', 'artifact.json', 'producer-run.json', 'retrieval.json'}
ARTIFACT_FIELDS = {'id', 'name', 'size_in_bytes', 'digest', 'expired', 'expires_at', 'workflow_run'}
RUN_FIELDS = {'id', 'run_attempt', 'head_sha', 'status', 'conclusion', 'path', 'event', 'head_branch',
              'repository', 'head_repository'}
REPOSITORY = publication.executor.REPOSITORY
require = publication.require


def artifact_record(value):
    return {key: value.get(key) for key in ARTIFACT_FIELDS - {'workflow_run'}} | {
        'workflow_run': {key: value.get('workflow_run', {}).get(key)
                         for key in ('id', 'head_sha', 'repository_id', 'head_repository_id')}}


def run_record(value):
    return {key: value.get(key) for key in RUN_FIELDS - {'repository', 'head_repository'}} | {
        key: {field: value.get(key, {}).get(field) for field in ('id', 'full_name')}
        for key in ('repository', 'head_repository')}


class RetainedProvider:
    def __init__(self, artifact, run):
        self.artifact, self.run = artifact, run

    def get(self, path):
        return self.artifact if '/artifacts/' in path else self.run


def executor_identity(expected_head, *, api, env=None):
    env = os.environ if env is None else env
    source, run, attempt = (env.get(key, '') for key in ('GITHUB_SHA', 'GITHUB_RUN_ID', 'GITHUB_RUN_ATTEMPT'))
    require(isinstance(expected_head, str) and re.fullmatch('[0-9a-f]{40}', expected_head)
            and expected_head == source, 'executor_head_changed')
    require(env.get('GITHUB_ACTIONS') == 'true' and env.get('GITHUB_REPOSITORY') == REPOSITORY
            and env.get('GITHUB_EVENT_NAME') == 'workflow_dispatch'
            and env.get('GITHUB_REF') == 'refs/heads/main' and env.get('GITHUB_JOB') == 'verify'
            and env.get('GITHUB_WORKFLOW_SHA') == source
            and env.get('GITHUB_WORKFLOW_REF') == f'{REPOSITORY}/{publication.WORKFLOW}@refs/heads/main'
            and re.fullmatch('[1-9][0-9]*', run) and re.fullmatch('[1-9][0-9]*', attempt),
            'executor_runtime_changed')
    require(not any(env.get(key) for key in ('GH_TOKEN', 'GITHUB_TOKEN', 'NODE_AUTH_TOKEN', 'NPM_TOKEN',
                'ELSA_STUDIO_FEEDZ_PUBLISH_KEY_BASE64', 'ACTIONS_ID_TOKEN_REQUEST_URL',
                'ACTIONS_ID_TOKEN_REQUEST_TOKEN')) and publication.REVIEWED_PUBLICATION_POLICY is None,
            'unexpected_publisher_authority')
    require(publication.pair.git(publication.pair.ROOT, 'rev-parse', 'HEAD') == source
            and publication.pair.git(publication.pair.ROOT, 'status', '--porcelain') == '',
            'executor_source_changed')
    runtime = api.get(f'repos/{REPOSITORY}/actions/runs/{run}/attempts/{attempt}')
    require(runtime.get('id') == int(run) and runtime.get('run_attempt') == int(attempt)
            and runtime.get('head_sha') == source and runtime.get('event') == 'workflow_dispatch'
            and runtime.get('path') == publication.WORKFLOW and runtime.get('status') == 'in_progress'
            and runtime.get('head_branch') == 'main'
            and all(runtime.get(key, {}).get('full_name') == REPOSITORY
                    and runtime.get(key, {}).get('id') == 151148482
                    for key in ('repository', 'head_repository')), 'executor_provider_changed')
    return {'source_commit': source, 'run_id': int(run), 'run_attempt': int(attempt),
            'ref': 'refs/heads/main', 'workflow': publication.WORKFLOW, 'clean': True}


def admit_custody(inputs, expected_head, transfer_id, transfer_digest, *, api=None, env=None):
    api = api or publication.executor.GitHubApi()  # Anonymous public readback only.
    identity = executor_identity(expected_head, api=api, env=env)
    inputs = paths._absolute_without_symlink_ancestors(inputs, 'Custody inputs')
    require(inputs.is_dir() and {path.name for path in inputs.iterdir()} == TRANSPORT_FILES,
            'transport_shape')
    for filename in TRANSPORT_FILES:
        publication.pair.regular(inputs / filename)
    packet, packet_hash = publication.load(inputs / 'packet.json', publication.MAX_METADATA)
    require(packet_hash == PACKET_SHA256, 'packet_changed')
    archive = inputs / 'original.zip'
    require(archive.stat().st_size == ARCHIVE_SIZE
            and paths._file_sha256(archive) == packet['archive_sha256'], 'original_archive_changed')
    artifact, _ = publication.load(inputs / 'artifact.json', publication.MAX_METADATA)
    run, _ = publication.load(inputs / 'producer-run.json', publication.MAX_METADATA)
    retrieval, _ = publication.load(inputs / 'retrieval.json', publication.MAX_METADATA)
    require(artifact == artifact_record(artifact) and run == run_record(run), 'unsafe_provider_record')
    candidate = {'packet': packet, 'identity': {'version': packet['version']}}
    publication.provider_admission(candidate, ARCHIVE_SIZE, api=RetainedProvider(artifact, run))
    # Independently reread provider identity after transfer. The existing executor
    # will repeat this admission alongside ZIP/receipt and package verification.
    publication.provider_admission(candidate, ARCHIVE_SIZE, api=api)
    require(set(retrieval) == {'schema', 'packet_sha256', 'archive_sha256', 'archive_size',
                              'retrieved_at', 'executor_source_commit', 'executor_run_id',
                              'executor_run_attempt'} and retrieval == {
        'schema': 1, 'packet_sha256': PACKET_SHA256, 'archive_sha256': packet['archive_sha256'],
        'archive_size': ARCHIVE_SIZE, 'retrieved_at': retrieval.get('retrieved_at'),
        'executor_source_commit': identity['source_commit'], 'executor_run_id': identity['run_id'],
        'executor_run_attempt': identity['run_attempt']}, 'retrieval_changed')
    observed = datetime.fromisoformat(retrieval['retrieved_at'].replace('Z', '+00:00'))
    require(observed.tzinfo is not None and observed <= datetime.now(timezone.utc), 'retrieval_time_invalid')
    require(type(transfer_id) is int and transfer_id > 0 and transfer_id != packet['artifact_id']
            and isinstance(transfer_digest, str) and publication.SHA.fullmatch(transfer_digest),
            'transfer_identity_invalid')
    transfer = api.get(f'repos/{REPOSITORY}/actions/artifacts/{transfer_id}')
    expiry = datetime.fromisoformat(transfer['expires_at'].replace('Z', '+00:00'))
    require(transfer.get('id') == transfer_id
            and transfer.get('name') == f"studio-npm-original-input-{identity['run_id']}-{identity['run_attempt']}"
            and transfer.get('digest') == 'sha256:' + transfer_digest
            and transfer.get('expired') is False and expiry.tzinfo is not None
            and expiry > datetime.now(timezone.utc)
            and type(transfer.get('size_in_bytes')) is int
            and 0 < transfer['size_in_bytes'] <= publication.MAX_ARCHIVE
            and artifact_record(transfer)['workflow_run'] == {'id': identity['run_id'], 'head_sha': identity['source_commit'],
                                            'repository_id': 151148482, 'head_repository_id': 151148482},
            'transfer_provider_changed')
    return {'schema': 1, 'success': True, 'published': False, 'publisher_authority': False,
            'packet_sha256': packet_hash, 'original_packet': packet, 'original_archive_size': ARCHIVE_SIZE,
            'original_provider': artifact, 'original_producer': run, 'retrieval': retrieval,
            'transfer_provider': artifact_record(transfer), 'executor': identity,
            'registry_observation_scope': 'anonymous_public', 'registry_verification': 'separate_executor_receipts'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inputs', type=Path, required=True)
    parser.add_argument('--expected-head', required=True)
    parser.add_argument('--transfer-id', required=True)
    parser.add_argument('--transfer-digest', required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    try:
        output = paths._absolute_without_symlink_ancestors(args.output, 'Custody output')
        inputs = paths._absolute_without_symlink_ancestors(args.inputs, 'Custody inputs')
        require(inputs not in output.parents and output != inputs and not output.exists()
                and output.parent.is_dir(), 'unsafe_report_destination')
    except Exception:
        print('{"success":false,"publisher_authority":false,"failure_code":"unsafe_report_destination"}')
        return 1
    try:
        require(re.fullmatch('[1-9][0-9]*', args.transfer_id), 'transfer_identity_invalid')
        result = admit_custody(inputs, args.expected_head, int(args.transfer_id), args.transfer_digest)
    except publication.PublicationError as error:
        result = {'success': False, 'publisher_authority': False, 'failure_code': str(error)}
    except Exception:
        result = {'success': False, 'publisher_authority': False, 'failure_code': 'custody_input_or_provider_unknown'}
    try:
        publication.pair.write_json(output, result)
    except Exception:
        print('{"success":false,"publisher_authority":false,"failure_code":"report_write_failed"}')
        return 1
    return 0 if result['success'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
