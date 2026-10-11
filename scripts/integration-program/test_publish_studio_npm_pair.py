"""Closed artifact/recovery contracts; no provider credentials or publications."""
import copy
import itertools
import json
import unittest
from unittest.mock import patch
import zipfile

import publish_studio_npm_pair as publication
import prove_studio_npm_pair as pair
import consolidated_package_recovery as recovery
import test_prove_studio_npm_pair as fixtures


class Registry:
    def __init__(self, candidate, states=('missing', 'missing')):
        self.candidate = candidate
        self.states = dict(zip((pair.WASM, pair.REACT), states))

    def get(self, url, **kwargs):
        for name, item in self.candidate['artifacts'].items():
            if url == publication.REGISTRIES['feedz'] + publication.urllib.parse.quote(name, safe='@'):
                value = {'name': name, 'versions': {'3.9.0': {'name': name, 'version': '3.9.0'}}}
                return recovery.ReadResult(200, json.dumps(value).encode(), True)
            tarball = publication.REGISTRIES['feedz'] + name.split('/')[-1] + '.tgz'
            if url == tarball:
                return recovery.ReadResult(200, item['bytes'], True)
            if url == publication.metadata_url('feedz', name, self.candidate['identity']['version']):
                state = self.states[name]
                if state == 'missing':
                    return recovery.ReadResult(404, b'', True)
                if state == 'unknown':
                    return recovery.ReadResult(401, b'private-secret-provider-body', True)
                value = {'name': name, 'version': self.candidate['identity']['version'],
                         'dist': {'integrity': item['report']['sha512_integrity'] if state == 'matching' else 'wrong', 'tarball': tarball}}
                return recovery.ReadResult(200, json.dumps(value).encode(), True)
        return recovery.ReadResult(None, b'', False, 'timeout')


class Provider:
    def __init__(self, candidate, archive):
        packet = candidate['packet']
        self.artifact = {'id': packet['artifact_id'], 'name': packet['artifact_name'], 'size_in_bytes': archive.stat().st_size,
            'digest': 'sha256:' + packet['archive_sha256'], 'expired': False, 'expires_at': '2099-01-01T00:00:00Z',
            'workflow_run': {'id': packet['run_id'], 'head_sha': packet['source_commit'], 'repository_id': 151148482, 'head_repository_id': 151148482}}
        self.run = {'id': packet['run_id'], 'run_attempt': packet['run_attempt'], 'head_sha': packet['source_commit'],
            'status': 'completed', 'conclusion': 'success', 'path': '.github/workflows/studio-npm-pair.yml',
            'event': 'workflow_dispatch', 'head_branch': 'main', 'repository': {'id': 151148482, 'full_name': publication.executor.REPOSITORY},
            'head_repository': {'id': 151148482, 'full_name': publication.executor.REPOSITORY}}

    def get(self, path):
        return self.artifact if '/artifacts/' in path else self.run


class Publisher:
    def __init__(self, registry, failure=None):
        self.registry, self.failure, self.calls = registry, failure, []

    def publish(self, candidate, registry, name, tag):
        self.calls.append((name, tag, candidate['artifacts'][name]['bytes']))
        self.registry.states[name] = 'matching'
        return name != self.failure


class PublicationContracts(unittest.TestCase):
    def setUp(self):
        self.fixture = fixtures.PairContracts()
        self.fixture.setUp()
        self.addCleanup(self.fixture.doCleanups)
        self.root = self.fixture.root
        self.identity = pair.release_candidate_identity('a' * 40, 'b' * 40, '3.10.0-preview.1', '12', '1')
        self.candidate, self.zip, self.packet = self.bundle(self.identity)
        self.admission = {'native_approval_verified': True,
                          'packet_sha256': publication.digest(json.dumps(self.packet, sort_keys=True, separators=(',', ':')).encode()),
                          'producer_source_commit': self.identity['source_commit'],
                          'executor_source_commit': 'c' * 40, 'executor_run_id': 42, 'executor_run_attempt': 1,
                          'registry_provenance_policy': 'no-registry-attestation', 'registry_attestation_verified': False}

    def bundle(self, identity):
        receipt = identity | {'success': True, 'source_unchanged': True, 'publisher_authority': False,
            'consumer_inputs_unchanged_after_install': True, 'consumer': {
                'offline_install': True, 'esm_import': True, 'commonjs_require': True, 'vite_build': True,
                'assets': {'lifecycle_scripts_enabled': True, 'stale_assets_removed': True, 'unmanaged_files_preserved': True}}}
        artifacts = {}
        for key, name in (('wasm', pair.WASM), ('react', pair.REACT)):
            payload = self.fixture.payload(name, proof=identity)
            if key == 'react':
                manifest = json.loads(payload['package/' + pair.PROOF])
                manifest['wasm_sha512_integrity'] = receipt['wasm']['sha512_integrity']
                payload['package/' + pair.PROOF] = json.dumps(manifest).encode()
            filename = name.removeprefix('@').replace('/', '-') + '-' + identity['version'] + '.tgz'
            archive = self.fixture.archive(payload, filename)
            receipt[key] = pair.verify_archive(archive, name, identity,
                wasm_integrity=receipt['wasm']['sha512_integrity'] if key == 'react' else None)
            artifacts[name] = {'bytes': archive.read_bytes(), 'report': receipt[key]}
        local = {name: {'version': identity['version'], 'sha512_integrity': item['report']['sha512_integrity'],
                        'resolution': 'same-run-local-archive'} for name, item in artifacts.items()}
        receipt['consumer']['elsa_resolution'] = local
        inputs = {'package.json': {'dependencies': {name: 'file:../../retained/' + item['report']['file'] for name, item in artifacts.items()}},
                  'package-lock.json': {'packages': {'node_modules/' + name: {'version': identity['version'],
                    'resolved': 'file:../../retained/' + item['report']['file'], 'integrity': item['report']['sha512_integrity']}
                    for name, item in artifacts.items()}}}
        inputs = {'consumer-' + key: json.dumps(value).encode() for key, value in inputs.items()}
        receipt['consumer_inputs'] = {key: {'retained_file': 'consumer-' + key,
            'sha256': publication.digest(inputs['consumer-' + key])} for key in ('package.json', 'package-lock.json')}
        steps = ['node-version', 'node-identity', 'npm-identity', 'source-init', 'source-fetch', 'source-checkout',
                 'source-origin', 'sdk-identity', 'clientlibs', 'custom-elements-publish', 'pack-wasm', 'wrapper-install',
                 'wrapper-build', 'pack-react', 'consumer-lock-offline', 'consumer-install-offline',
                 'consumer-lifecycle-refresh', 'consumer-esm', 'consumer-commonjs', 'consumer-vite']
        receipt.update(stage='complete', commands=[{'step': step, 'success': True, 'exit_code': 0} for step in steps])
        raw = json.dumps(receipt).encode()
        archive = self.root / 'original.zip'
        with zipfile.ZipFile(archive, 'w') as zipped:
            zipped.writestr('receipt.json', raw)
            for name, payload in inputs.items():
                zipped.writestr(name, payload)
            for item in artifacts.values():
                zipped.writestr(item['report']['file'], item['bytes'])
        prefix = 'studio-npm-candidate' if identity.get('kind') == 'release-candidate' else 'studio-npm-pair'
        packet = {'source_commit': identity['source_commit'], 'source_tree': identity['source_tree'],
                  'run_id': 12, 'run_attempt': 1, 'artifact_id': 123,
                  'artifact_name': f'{prefix}-{identity["source_commit"]}-12-1', 'version': identity['version'],
                  'archive_sha256': publication.digest(archive.read_bytes()), 'receipt_sha256': publication.digest(raw)}
        return {'identity': identity, 'packet': packet, 'artifacts': artifacts}, archive, packet

    def test_original_pair_admission_and_changed_identity_rejected(self):
        self.assertEqual(self.candidate, publication.admit_original(self.zip, self.packet, api=Provider(self.candidate, self.zip)))
        for key, value in (('archive_sha256', '0' * 64), ('receipt_sha256', '0' * 64), ('run_attempt', 2), ('artifact_name', 'different')):
            with self.subTest(key=key), self.assertRaises((publication.PublicationError, pair.ProofError)):
                publication.admit_original(self.zip, self.packet | {key: value})

    def test_proof_admission_never_confers_publication_authority(self):
        candidate, archive, packet = self.bundle(self.fixture.proof)
        self.assertEqual(candidate, publication.admit_original(archive, packet, api=Provider(candidate, archive)))
        with self.assertRaisesRegex(publication.PublicationError, 'proof_publication_refused'), patch.object(publication, 'native_admission') as native:
            publication.execute(candidate, 'feedz', operation='publish')
        native.assert_not_called()

    def test_verify_never_invokes_native_or_publisher(self):
        with patch.object(publication, 'native_admission', side_effect=AssertionError('called')):
            result = publication.execute(self.candidate, 'feedz', transport=Registry(self.candidate), publisher=object())
        self.assertTrue(result['success'])
        self.assertFalse(result['publisher_authority'])
        self.assertEqual([], result['uploads'])

    def test_both_member_matrix_before_any_authority_or_upload(self):
        for states in itertools.product(('matching', 'missing', 'conflicting', 'unknown'), repeat=2):
            registry = Registry(self.candidate, states)
            publisher = Publisher(registry)
            with self.subTest(states=states), patch.object(publication, 'native_admission', return_value=self.admission) as native:
                if set(states) & {'conflicting', 'unknown'}:
                    with self.assertRaisesRegex(publication.PublicationError, 'remote_pair_not_admitted'):
                        publication.execute(self.candidate, 'feedz', operation='publish', transport=registry, publisher=publisher)
                    native.assert_not_called()
                    self.assertEqual([], publisher.calls)
                else:
                    result = publication.execute(self.candidate, 'feedz', operation='publish', transport=registry, publisher=publisher)
                    self.assertTrue(result['success'])
                    self.assertEqual(self.admission, result['native_admission'])
                    self.assertEqual([name for name, state in zip((pair.WASM, pair.REACT), states) if state == 'missing'], [call[0] for call in publisher.calls])
                    for name, tag, data in publisher.calls:
                        self.assertEqual('next', tag)
                        self.assertEqual(self.candidate['artifacts'][name]['bytes'], data)

    def test_uncertain_wasm_stops_then_resume_only_wrapper_same_bytes(self):
        registry = Registry(self.candidate)
        first = Publisher(registry, pair.WASM)
        with patch.object(publication, 'native_admission', return_value=self.admission):
            result = publication.execute(self.candidate, 'feedz', operation='publish', transport=registry, publisher=first)
            self.assertFalse(result['success'])
            self.assertEqual([pair.WASM], [row[0] for row in first.calls])
            self.assertEqual('matching', result['after'][0]['state'])
            resumed = Publisher(registry)
            result = publication.execute(self.candidate, 'feedz', operation='publish', transport=registry, publisher=resumed)
        self.assertTrue(result['success'])
        self.assertEqual([pair.REACT], [row[0] for row in resumed.calls])
        self.assertEqual(self.candidate['artifacts'][pair.REACT]['bytes'], resumed.calls[0][2])

    def test_wasm_disappearing_after_successful_readback_blocks_react_write(self):
        registry = Registry(self.candidate)
        publisher = Publisher(registry)
        original_get = registry.get
        original_bytes = {name: item['bytes'] for name, item in self.candidate['artifacts'].items()}
        original_zip = self.zip.read_bytes()
        successful_wasm_reads = []
        wasm_url = publication.REGISTRIES['feedz'] + pair.WASM.split('/')[-1] + '.tgz'
        def disappear_after_readback(url, **kwargs):
            response = original_get(url, **kwargs)
            if url == wasm_url and publisher.calls:
                self.assertEqual(original_bytes[pair.WASM], response.body)
                self.assertTrue(response.complete)
                successful_wasm_reads.append(url)
                registry.states[pair.WASM] = 'missing'
            return response
        with patch.object(registry, 'get', side_effect=disappear_after_readback), \
             patch.object(publication, 'native_admission', return_value=self.admission):
            result = publication.execute(self.candidate, 'feedz', operation='publish', transport=registry, publisher=publisher)
        self.assertEqual([wasm_url], successful_wasm_reads)
        self.assertEqual([pair.WASM], [call[0] for call in publisher.calls])
        self.assertFalse(result['success'])
        self.assertEqual('wasm_dependency_not_matching_before_react', result['failure_code'])
        self.assertEqual([{'package': pair.WASM, 'state': 'missing'}, {'package': pair.REACT, 'state': 'missing'}], result['after'])
        self.assertEqual([{'package': pair.WASM, 'command_succeeded': True}], result['uploads'])
        self.assertEqual(self.admission, result['native_admission'])
        self.assertEqual(original_bytes, {name: item['bytes'] for name, item in self.candidate['artifacts'].items()})
        self.assertEqual(original_zip, self.zip.read_bytes())

    def test_failed_native_gate_never_publishes(self):
        with self.assertRaisesRegex(publication.PublicationError, 'publication_policy_unconfigured'):
            publication.execute(self.candidate, 'feedz', operation='publish', transport=Registry(self.candidate), publisher=object())

    def test_unknown_body_omitted_and_feedz_404_unknown_without_contract(self):
        result = publication.execute(self.candidate, 'feedz', transport=Registry(self.candidate, ('unknown', 'unknown')))
        self.assertFalse(result['success'])
        self.assertNotIn('private-secret', json.dumps(result))
        registry = Registry(self.candidate)
        with patch.object(registry, 'get', return_value=recovery.ReadResult(404, b'', True)):
            self.assertEqual('unknown', publication.observe(self.candidate, 'feedz', pair.WASM, registry)['state'])

    def test_unsafe_remote_tarball_never_fetched(self):
        for url in ('http://registry.npmjs.org/a', 'https://registry.npmjs.org.evil/a', 'https://f.feedz.io/elsa-workflows/elsa-3/npm/../secret',
                    'https://registry.npmjs.org/a?token=secret', 'https://127.0.0.1/a'):
            self.assertFalse(publication.safe_tarball(url))
        metadata = {'name': pair.WASM, 'version': self.identity['version'], 'dist': {
                    'integrity': self.candidate['artifacts'][pair.WASM]['report']['sha512_integrity'], 'tarball': 'https://evil.invalid/private'}}
        registry = Registry(self.candidate)
        with patch.object(registry, 'get', return_value=recovery.ReadResult(200, json.dumps(metadata).encode(), True)) as get:
            self.assertEqual('unknown', publication.observe(self.candidate, 'feedz', pair.WASM, registry)['state'])
            self.assertEqual(1, get.call_count)

    def native_context(self, candidate, registry='feedz'):
        policy = {'environment': 'reviewed-npm', 'environment_id': 10, 'reviewer_rule_id': 11,
                  'branch_rule_id': 12, 'reviewer_ids': [20], 'branch_policy': {'id': 13, 'type': 'branch', 'name': 'main'},
                  'allowed_ref': 'refs/heads/main', 'publisher_identity': registry, 'actions': [{
                      'packet_sha256': publication.digest(json.dumps(candidate['packet'], sort_keys=True, separators=(',', ':')).encode()),
                      'registry': registry, 'action': 'publish',
                      'registry_provenance_policy': publication.REGISTRY_PROVENANCE_POLICIES[registry]}]}
        source = 'c' * 40  # Executor D follows the already-created original at S.
        self.assertNotEqual(candidate['identity']['source_commit'], source)
        runtime = {'id': 42, 'run_attempt': 1, 'head_sha': source, 'event': 'workflow_dispatch',
                   'path': publication.WORKFLOW, 'status': 'in_progress', 'head_branch': 'main',
                   'repository': {'full_name': publication.executor.REPOSITORY},
                   'head_repository': {'full_name': publication.executor.REPOSITORY}, 'triggering_actor': {'id': 21}}
        environment = {'id': 10, 'name': 'reviewed-npm', 'can_admins_bypass': False,
                       'deployment_branch_policy': {'protected_branches': False, 'custom_branch_policies': True},
                       'protection_rules': [{'id': 12, 'type': 'branch_policy'}, {'id': 11, 'type': 'required_reviewers',
                       'prevent_self_review': True, 'reviewers': [{'type': 'User', 'reviewer': {'id': 20}}]}]}
        approval = [{'state': 'approved', 'user': {'id': 20}, 'environments': [{'id': 10, 'name': 'reviewed-npm'}]}]
        values = {'GITHUB_ACTIONS': 'true', 'GITHUB_REPOSITORY': publication.executor.REPOSITORY,
                  'GITHUB_EVENT_NAME': 'workflow_dispatch', 'GITHUB_RUN_ATTEMPT': '1', 'GITHUB_RUN_ID': '42',
                  'GITHUB_SHA': source, 'GITHUB_JOB': 'publish', 'GITHUB_REF': 'refs/heads/main',
                  'GITHUB_WORKFLOW_SHA': source,
                  'GITHUB_WORKFLOW_REF': f'{publication.executor.REPOSITORY}/{publication.WORKFLOW}@refs/heads/main'}
        class Api:
            def get(self, path):
                if '/attempts/' in path:
                    return runtime
                if path.endswith('/approvals'):
                    return approval
                if 'deployment-branch-policies?' in path:
                    return {'total_count': 1, 'branch_policies': [policy['branch_policy']]}
                return environment
        return policy, runtime, environment, values, Api()

    def test_later_executor_is_independent_and_exact_native_gates_remain(self):
        for mutation in (None, 'actor', 'ref', 'rule', 'extra_reviewer', 'action', 'packet', 'runtime', 'head', 'dirty', 'workflow', 'workflow_ref', 'runtime_workflow'):
            candidate = copy.deepcopy(self.candidate)
            policy, runtime, environment, values, api = self.native_context(candidate)
            head = values['GITHUB_SHA']
            if mutation == 'actor':
                runtime['triggering_actor']['id'] = 20
            elif mutation == 'ref':
                values['GITHUB_REF'] = 'refs/heads/other'
            elif mutation == 'rule':
                environment['can_admins_bypass'] = True
            elif mutation == 'extra_reviewer':
                environment['protection_rules'][1]['reviewers'].append({'type': 'Team', 'reviewer': {'id': 30}})
            elif mutation == 'action':
                policy['actions'] = []
            elif mutation == 'packet':
                candidate['packet']['source_commit'] = 'd' * 40
            elif mutation == 'runtime':
                runtime['head_sha'] = 'd' * 40
            elif mutation == 'head':
                head = 'd' * 40
            elif mutation == 'workflow':
                values['GITHUB_WORKFLOW_SHA'] = 'd' * 40
            elif mutation == 'workflow_ref':
                values['GITHUB_WORKFLOW_REF'] = 'wrong/workflow@refs/heads/main'
            elif mutation == 'runtime_workflow':
                runtime['path'] = '.github/workflows/wrong.yml'
            with self.subTest(mutation=mutation), patch.object(publication, 'REVIEWED_PUBLICATION_POLICY', policy), \
                 patch.dict(publication.os.environ, values, clear=True), \
                 patch.object(pair, 'git', side_effect=lambda root, *args: head if args[0] == 'rev-parse' else (' M source' if mutation == 'dirty' else '')):
                if mutation is None:
                    self.assertEqual(self.admission, publication.native_admission(candidate, 'feedz', 'publish', api=api))
                else:
                    with self.assertRaises(publication.PublicationError):
                        publication.native_admission(candidate, 'feedz', 'publish', api=api)

    def test_registry_provenance_policy_is_explicit_before_publisher_access(self):
        for registry in ('feedz', 'npmjs'):
            # Synthetic 3.9 tests the gate; real maintenance producer admission
            # remains unconfigured and this is no provider/lifecycle proof.
            candidate = self.candidate if registry == 'feedz' else self.bundle(
                pair.release_candidate_identity('a' * 40, 'b' * 40, '3.9.1', '12', '1'))[0]
            for mode in (None, 'unknown', 'no-registry-attestation' if registry == 'npmjs' else 'publisher-invocation'):
                policy, runtime, environment, values, api = self.native_context(candidate, registry)
                if mode is None:
                    policy['actions'][0].pop('registry_provenance_policy')
                else:
                    policy['actions'][0]['registry_provenance_policy'] = mode
                with self.subTest(registry=registry, mode=mode), patch.object(publication, 'REVIEWED_PUBLICATION_POLICY', policy), \
                     patch.dict(publication.os.environ, values, clear=True), \
                     patch.object(pair, 'git', side_effect=lambda root, *args: values['GITHUB_SHA'] if args[0] == 'rev-parse' else ''), \
                     patch.object(publication.executor, 'GitHubApi', return_value=api), \
                     patch.object(publication, 'observe', side_effect=lambda candidate, registry, name, transport: {'package': name, 'state': 'missing'}), \
                     patch.object(publication, 'NpmPublisher') as publisher:
                    with self.assertRaisesRegex(publication.PublicationError, 'candidate_action_not_authorized'):
                        publication.execute(candidate, registry, operation='publish', transport=object())
                    publisher.assert_not_called()
            if registry == 'npmjs':
                policy, runtime, environment, values, api = self.native_context(candidate, registry)
                with patch.object(publication, 'REVIEWED_PUBLICATION_POLICY', policy), patch.dict(publication.os.environ, values, clear=True), \
                     patch.object(pair, 'git', side_effect=lambda root, *args: values['GITHUB_SHA'] if args[0] == 'rev-parse' else ''):
                    record = publication.native_admission(candidate, registry, 'publish', api=api)
                self.assertEqual('publisher-invocation', record['registry_provenance_policy'])
                self.assertFalse(record['registry_attestation_verified'])

    def test_publisher_toolchain_rejected_before_token_lookup(self):
        with patch.object(publication.executor, 'bounded_child', side_effect=[b'v22.23.3', b'10.9.9']), \
             self.assertRaisesRegex(publication.PublicationError, 'publisher_toolchain_unsupported'):
            publication.NpmPublisher().publish(self.candidate, 'feedz', pair.WASM, 'next')

    def test_current_310_npmjs_refused_before_read_or_authority(self):
        with self.assertRaisesRegex(publication.PublicationError, 'npmjs_line_refused'):
            publication.execute(self.candidate, 'npmjs', transport=object())

    def test_default_transport_budget_completes_missing_pair_and_partial_read_failure_is_retained(self):
        import base64
        registry = Registry(self.candidate)
        publisher = Publisher(registry)
        calls = []
        def wire(command, payload, **kwargs):
            request = json.loads(payload)
            calls.append(request['url'])
            response = registry.get(request['url'])
            return json.dumps({'status': response.status, 'complete': response.complete,
                'failure_category': response.failure_category, 'body': base64.b64encode(response.body).decode()}).encode()
        with patch.object(publication.executor, 'bounded_child', side_effect=wire), \
             patch.object(publication, 'native_admission', return_value=self.admission):
            result = publication.execute(self.candidate, 'feedz', operation='publish', publisher=publisher)
        self.assertTrue(result['success'])
        self.assertEqual(20, len(calls))
        registry = Registry(self.candidate)
        publisher = Publisher(registry)
        original = registry.get
        def failing(url, **kwargs):
            if publisher.calls and pair.REACT.split('/')[-1] in url:
                raise RuntimeError('private-provider-secret')
            return original(url, **kwargs)
        with patch.object(registry, 'get', side_effect=failing), patch.object(publication, 'native_admission', return_value=self.admission):
            result = publication.execute(self.candidate, 'feedz', operation='publish', transport=registry, publisher=publisher)
        self.assertFalse(result['success'])
        self.assertEqual([pair.WASM], [row['package'] for row in result['uploads']])
        self.assertNotIn('private-provider-secret', json.dumps(result))

    def test_provider_metadata_drift_and_unavailability_fail_closed(self):
        for field, value in (('digest', 'sha256:' + '0' * 64), ('expired', True), ('size_in_bytes', 1)):
            provider = Provider(self.candidate, self.zip)
            provider.artifact[field] = value
            with self.subTest(field=field), self.assertRaises(publication.PublicationError):
                publication.provider_admission(self.candidate, self.zip.stat().st_size, api=provider)
        provider = Provider(self.candidate, self.zip)
        provider.run['run_attempt'] = 2
        with self.assertRaisesRegex(publication.PublicationError, 'provider_run_changed'):
            publication.provider_admission(self.candidate, self.zip.stat().st_size, api=provider)
        for origin in ('artifact', 'run'):
            provider = Provider(self.candidate, self.zip)
            (provider.artifact['workflow_run'] if origin == 'artifact' else provider.run)['head_sha'] = 'd' * 40
            with self.subTest(origin=origin), self.assertRaises(publication.PublicationError):
                publication.provider_admission(self.candidate, self.zip.stat().st_size, api=provider)
        with patch.object(provider, 'get', side_effect=RuntimeError('private-error')), self.assertRaisesRegex(publication.PublicationError, 'provider_metadata_unknown'):
            publication.provider_admission(self.candidate, self.zip.stat().st_size, api=provider)

    def test_missing_retained_consumer_inputs_and_failed_stage_rejected(self):
        with zipfile.ZipFile(self.zip) as source:
            original = {name: source.read(name) for name in source.namelist()}
        for field, value in (('consumer_inputs', {}), ('stage', 'partial'), ('commands', [])):
            payloads = dict(original)
            receipt = json.loads(payloads['receipt.json'])
            receipt[field] = value
            payloads['receipt.json'] = json.dumps(receipt).encode()
            with zipfile.ZipFile(self.zip, 'w') as target:
                for name, data in payloads.items():
                    target.writestr(name, data)
            packet = self.packet | {'archive_sha256': publication.digest(self.zip.read_bytes()),
                                    'receipt_sha256': publication.digest(payloads['receipt.json'])}
            with self.subTest(field=field), self.assertRaises(publication.PublicationError):
                publication.admit_original(self.zip, packet, api=Provider(self.candidate, self.zip))

    def test_oidc_child_has_required_github_context_without_token_fallback(self):
        keys = ('GITHUB_ACTIONS', 'GITHUB_WORKFLOW_REF', 'GITHUB_REPOSITORY', 'GITHUB_SERVER_URL',
                'GITHUB_EVENT_NAME', 'GITHUB_REPOSITORY_ID', 'GITHUB_REPOSITORY_OWNER_ID', 'GITHUB_REF',
                'GITHUB_SHA', 'GITHUB_RUN_ID', 'GITHUB_RUN_ATTEMPT', 'RUNNER_ENVIRONMENT',
                'ACTIONS_ID_TOKEN_REQUEST_URL', 'ACTIONS_ID_TOKEN_REQUEST_TOKEN')
        environment = {key: 'fixture' for key in keys} | {'GITHUB_ACTIONS': 'true', 'NODE_AUTH_TOKEN': 'never-forward'}
        with patch.dict(publication.os.environ, environment, clear=True), \
             patch.object(publication.executor, 'bounded_child', side_effect=[b'v22.23.3', b'11.11.0', b'']) as child:
            self.assertTrue(publication.NpmPublisher().publish(self.candidate, 'npmjs', pair.WASM, 'next'))
        forwarded = child.call_args.kwargs['env']
        self.assertTrue(all(forwarded[key] == environment[key] for key in keys))
        self.assertNotIn('NODE_AUTH_TOKEN', forwarded)
        self.assertNotIn('--provenance=false', child.call_args.args[0])

    def test_existing_workflow_retains_default_proof_and_main_only_310_candidate(self):
        source = (pair.ROOT / '.github/workflows/studio-npm-pair.yml').read_text()
        self.assertIn('default: proof', source)
        self.assertIn('test "$GITHUB_EVENT_NAME" = workflow_dispatch', source)
        self.assertIn('test "$GITHUB_REF" = refs/heads/main', source)
        self.assertIn('3.10.*', source)
        self.assertIn('candidate_args=(--release-candidate)', source)
        self.assertIn('studio-npm-candidate', source)
        self.assertNotIn('id-token:', source)
        self.assertNotIn('npm publish', source)

    def test_symlink_original_rejected(self):
        link = self.root / 'link.zip'
        link.symlink_to(self.zip)
        with self.assertRaisesRegex(pair.ProofError, 'unsafe-file'):
            publication.admit_original(link, self.packet)


if __name__ == '__main__':
    unittest.main()
