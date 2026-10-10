"""Mocked closed-source contracts only; no source approval or native evidence."""
from copy import deepcopy
from datetime import timedelta
import hashlib
import json
import tempfile
from pathlib import Path
import unittest
from unittest.mock import patch

import core_source_continuation as continuation
import product_release_metadata as metadata
import prepare_maintenance_build as maintenance
import prove_product_release_artifacts as artifacts
import selected_core_producer as core
import selected_core_consumer as consumer
import selected_core_payload as core_payload
import selected_studio_payload as payload
import selected_studio_plan_schema as schema
import selected_control_seal as seal
import selected_control_transport as transport
from test_selected_control_transport import zipped
import test_selected_control_seal as seal_tests
from test_selected_product_payload import fixture as product_fixture
from test_selected_studio_payload import encoded


class CoreSourceContinuationContracts(unittest.TestCase):
    def setUp(self):
        """Load the original Core policies, continuation catalog, and frozen plan observations."""
        self.contract = continuation.load_contract()
        self.originals = json.loads(core.CONTRACT.read_bytes())['sources']
        self.history = json.loads(Path(__file__).with_name('selected_product_plan_shapes.json').read_bytes())['cells']

    def source(self, line='3.8', *, contract=None):
        """Bind a test source to the original observation for the requested release line."""
        observation = deepcopy(self.history['core-' + line]['source']['observation'])
        return continuation.bind(line, observation, contract or self.contract)

    def source_git(self, line):
        """Fixed independent Git responses, not read back from mutated policy."""
        row = self.contract['sources'][line]
        trees = {row['original_commit']: row['original_tree']} | {r['commit']: r['tree'] for r in row['chain']}
        parents = {r['commit']: r['parents'] for r in row['chain']}
        def git(root, *args, **kwargs):
            if args[0] == 'rev-parse':
                return trees[args[1].removesuffix('^{tree}')]
            if args[:3] == ('show', '-s', '--format=%P'):
                return ' '.join(parents[args[3]])
            if args[:2] == ('rev-list', '--reverse'):
                return '\n'.join(r['commit'] for r in row['chain'])
            raise AssertionError(args)
        entries = {commit: {r['path']: tuple(r[key][k] for k in ('mode', 'type', 'blob')) for r in row['delta']}
                   for key, commit in (('before', row['original_commit']), ('after', row['commit']))}
        data = {}
        # Pin consistent synthetic bytes privately, retaining independent Git inventory.
        contract = deepcopy(self.contract)
        for r in contract['sources'][line]['delta']:
            for key, commit in (('before', row['original_commit']), ('after', row['commit'])):
                content = (key + r['path']).encode()
                r[key].update(bytes=len(content), sha256=metadata.sha256(content))
                data[(commit, r['path'])] = content
        return contract, git, entries, data

    def verify(self, line='3.8', mutation=None):
        """Exercise continuation source verification against optionally mutated synthetic Git
        evidence.
        """
        contract, git, entries, data = self.source_git(line)
        if mutation:
            mutation(contract['sources'][line])
        source = self.source(line, contract=contract)
        with patch.object(maintenance, 'git', side_effect=git), \
             patch.object(maintenance, 'tree_entries', side_effect=lambda root, commit: entries[commit]), \
             patch.object(maintenance, 'git_bytes', side_effect=lambda root, commit, path: data[(commit, path)]):
            continuation.verify_source(Path('/unused'), source, self.originals[line], contract)

    def test_fixed_candidate_identities_and_preserved_original_policies(self):
        """Verify fixed candidate identities and preserved original policies."""
        expected = {'3.8': ('7e5e6bcf97791e4f7f7165e15579abae18ec7203', 'd5c10535cee3524d91f5ae54b5cecb967c26b606', 4, 3, 44),
                    '3.9': ('86fffea6da3cfe67c0279f552c2a32940ef75ae2', '8beb9e093091c098f542a08e82e43625a3b8e520', 1, 1, 61)}
        for line, (commit, tree, chain, delta, tests) in expected.items():
            with self.subTest(line=line):
                source = self.source(line); row = self.contract['sources'][line]
                self.assertEqual((commit, tree), (source['commit'], source['tree']))
                self.assertEqual((chain, delta), (len(row['chain']), len(row['delta'])))
                value = core.policy(source)
                self.assertEqual(tests, len(value['test_projects']))
                for key in self.originals[line].keys() - {'commit', 'tree', 'files'}:
                    self.assertEqual(self.originals[line][key], value[key])
                changed = {r['path']: r['after']['blob'] for r in row['delta']}
                self.assertEqual(self.originals[line]['files'] | changed, value['files'])
                self.verify(line)

    def test_closed_source_identity_rejects_untrusted_selections(self):
        """Verify closed source identity rejects untrusted selections."""
        changes = ({'kind': 'maintenance'}, {'product': 'studio'}, {'line': '3.10'}, {'commit': 'a' * 40},
                   {'tree': 'b' * 40}, {'parents': []}, {'original_commit': 'c' * 40}, {'original_tree': 'd' * 40},
                   {'continuation_contract_sha256': 'e' * 64}, {'pr': 8696})
        for change in changes:
            with self.subTest(change=change), self.assertRaises((ValueError, KeyError)):
                core.policy(self.source() | change)
        for key in continuation.SOURCE_KEYS:
            source = self.source(); source.pop(key)
            with self.subTest(missing=key), self.assertRaises(ValueError):
                continuation.policy(source, self.originals['3.8'], self.contract)

    def test_candidate_never_impersonates_release_branch_observation(self):
        """Verify candidate never impersonates release branch observation."""
        for change in ({'commit': self.source()['commit'], 'tree': self.source()['tree']},
                       {'ref': 'refs/heads/release/3.9.0'}, {'commit': 'f' * 40}, {'tree': 'f' * 40}):
            source = self.source(); source['observation'].update(change)
            with self.subTest(change=change), self.assertRaisesRegex(ValueError, 'baseline_observation'):
                continuation.policy(source, self.originals['3.8'], self.contract)
        with patch.object(continuation, 'verify_source') as verify:
            source = self.source()
            actual = metadata.bind_source(Path('/unused'), 'core', '3.8', source['commit'], source['observation'])
            self.assertEqual(source, actual)
            verify.assert_called_once()

    def test_complete_ancestry_and_delta_are_verified_against_independent_git(self):
        """Verify complete ancestry and delta are verified against independent Git."""
        mutations = [lambda r: r['chain'].pop(0),
                     lambda r: r['chain'][0].update(parents=['a' * 40]),
                     lambda r: r['chain'][0].update(tree='a' * 40),
                     lambda r: r['delta'].pop(),
                     lambda r: r['delta'][0].update(path='unexpected.csproj'),
                     lambda r: r['delta'][0]['after'].update(mode='100755'),
                     lambda r: r['delta'][0]['after'].update(blob='a' * 40),
                     lambda r: r['delta'][0]['before'].update(blob='b' * 40),
                     lambda r: r['delta'][0]['after'].update(sha256='a' * 64),
                     lambda r: r['delta'][0]['before'].update(bytes=0)]
        for index, mutation in enumerate(mutations):
            with self.subTest(index=index), self.assertRaises((ValueError, KeyError)):
                self.verify(mutation=mutation)

    def test_original_version_recipe_and_all_native_lanes_remain_required(self):
        """Verify original version recipe and all native lanes remain required."""
        for line, count in (('3.8', 44), ('3.9', 61)):
            source = self.source(line); version = line + '.999'
            commands = maintenance.recipes(source, version, Path('/proof'))
            self.assertEqual(1 + count * 2, len(commands))
            self.assertEqual('Compile+Pack', commands[0][1][1])
            self.assertTrue(all('net10.0' in cmd for _, cmd in commands[1:]))
            self.assertFalse(any('--filter' in cmd or any('PackageVersion=' in arg or 'Version=' in arg for arg in cmd)
                                 for _, cmd in commands))
            self.assertEqual([], maintenance.version_arguments(source, version))
            self.assertNotIn('-p:PackageVersion=' + version, metadata.metadata_command(core.SAMPLE_PROJECT, version, source))
            self.assertEqual(version, core.environment(source, version, {})['VERSION'])
            self.assertEqual(json.loads(consumer.CONTRACT.read_bytes())['sources'][line],
                             consumer.source_contract({'source': source, 'line': line}))
            self.assertEqual(core.expected_skips(self.history['core-' + line]['source'], {}), core.expected_skips(source, {}))

    def test_internal_clone_origin_does_not_change_closed_public_source(self):
        """Verify internal clone origin does not change closed public source."""
        source = self.source()
        internal = source | {'source_repository': 'elsa-workflows/elsa-core'}
        self.assertEqual(source, core.source_binding(internal))
        self.assertEqual(core.policy(source), core.policy(internal))
        with patch.object(continuation, 'verify_source') as verify, \
             patch.object(maintenance, 'git', side_effect=lambda root, command, identity:
                          source['tree'] if identity.endswith('^{tree}') else
                          core.policy(source)['files'][identity.split(':', 1)[1]]):
            core.verify_source(Path('/unused'), internal)
            self.assertEqual(source, verify.call_args.args[1])
        with self.assertRaisesRegex(ValueError, 'core_source_repository'):
            core.policy(source | {'source_repository': 'someone/else'})
        with self.assertRaises(ValueError):
            core.policy(internal | {'unknown': True})
        with self.assertRaises(ValueError):
            continuation.policy(internal, self.originals['3.8'], self.contract)
        plan, _, _, _ = self.candidate_plan('3.8'); plan['source'] = internal
        with self.assertRaises(ValueError):
            core.validate_plan(plan)

    def candidate_plan(self, line):
        """Build a candidate plan with source and inventory hashes rebound to reviewed changes."""
        files, expected, now = product_fixture('core', line)
        plan = json.loads(files['plan.json']); source = continuation.bind(line, plan['source']['observation'], self.contract)
        plan['source'] = source
        plan['inventory'].update(source_commit=source['commit'], source_tree=source['tree'])
        sample = next(r for r in plan['inventory']['selected'] if r['project'] == core.SAMPLE_PROJECT)
        sample['metadata']['original_content_project_sha256'] = next(r['after']['sha256'] for r in
            self.contract['sources'][line]['delta'] if r['path'] == core.SAMPLE_PROJECT)
        plan['inventory']['sha256'] = metadata.canonical_hash(metadata.public_inventory(plan['inventory']))
        return plan, files, expected, now

    def test_candidate_plan_uses_true_baseline_observation_and_actual_project_hash(self):
        """Verify candidate plan uses true baseline observation and actual project hash."""
        contracts = payload.load_contracts()
        for line in ('3.8', '3.9'):
            plan, _, _, now = self.candidate_plan(line)
            schema.validate(plan, contracts, now=now)
            core.validate_plan(plan)
            core_payload._source(plan, contracts['core'])
            changed = deepcopy(plan)
            next(r for r in changed['inventory']['selected'] if r['project'] == core.SAMPLE_PROJECT)['metadata']['original_content_project_sha256'] = 'a' * 64
            with self.assertRaisesRegex(ValueError, 'core_continuation_project_metadata'):
                schema.validate(changed, contracts, now=now)
            changed = deepcopy(plan); changed['source']['observation']['commit'] = changed['source']['commit']
            with self.assertRaises(ValueError): schema.validate(changed, contracts, now=now)

    def test_missing_stale_ambiguous_and_candidate_as_baseline_observations_rejected(self):
        """Verify missing stale ambiguous and candidate as baseline observations rejected."""
        contracts = payload.load_contracts(); plan, _, _, now = self.candidate_plan('3.8')
        changes = ('missing', 'stale', 'duplicate', 'empty_tags', 'candidate')
        for change in changes:
            changed = deepcopy(plan); observation = changed['source']['observation']
            if change == 'missing':
                observation.pop('tag_observation')
            elif change == 'stale':
                observation['branch_observation']['observed_at'] = (now - timedelta(hours=2)).isoformat()
            elif change == 'duplicate':
                observation['tag_history'].append(deepcopy(observation['tag_history'][0]))
            elif change == 'empty_tags':
                observation['tag_history'] = []
            else:
                observation.update(commit=changed['source']['commit'], tree=changed['source']['tree'])
            data = encoded(changed)
            with self.subTest(change=change), self.assertRaises(ValueError):
                schema.admit(changed, metadata.sha256(data), data, now.isoformat(), contracts)

    def test_original_and_candidate_tag_snapshots_pass_early_artifact_admission(self):
        """Verify original and candidate tag snapshots pass early artifact admission."""
        contracts = payload.load_contracts(); object_types = set()
        for line in ('3.8', '3.9'):
            original_files, _, now = product_fixture('core', line)
            candidate, _, _, _ = self.candidate_plan(line)
            for kind, plan in (('original', json.loads(original_files['plan.json'])), ('candidate', candidate)):
                # Keep the actual recorded annotated and lightweight GitHub tag rows.
                examples = {tag['object']['type']: tag for tag in plan['source']['observation']['tag_history']}
                for object_type, tag in examples.items():
                    object_types.add(object_type)
                    selected = deepcopy(plan); selected['source']['observation']['tag_history'] = [tag]
                    data = encoded(selected); digest = metadata.sha256(data)
                    with self.subTest(line=line, kind=kind, object_type=object_type):
                        self.assertEqual(selected, artifacts.admit(data, digest, checked_at=now.isoformat()))
                        schema.admit(selected, digest, data, now.isoformat(), contracts)
        self.assertEqual({'tag', 'commit'}, object_types)

    def test_git_valid_tag_ref_boundaries_remain_pure(self):
        """Verify Git valid tag ref boundaries remain pure."""
        tag = self.history['core-3.8']['source']['observation']['tag_history'][0]
        for suffix in ('/nested/tag', '/valid./child', '/file.locked', '/café', '/at@name', '/percent%2fpath'):
            ref = tag['ref'] + suffix
            with self.subTest(ref=ref), \
                 patch.object(Path, 'read_bytes', side_effect=AssertionError('filesystem')), \
                 patch.object(maintenance, 'git', side_effect=AssertionError('Git')), \
                 patch('urllib.request.urlopen', side_effect=AssertionError('network')):
                continuation.validate_tag_history('3.8', [tag | {'ref': ref, 'url':
                    'https://api.github.com/repos/elsa-workflows/elsa-core/git/' + ref}])

    def test_malformed_tag_snapshots_fail_before_any_artifact_work(self):
        """Verify malformed tag snapshots fail before any artifact work."""
        contracts = payload.load_contracts()
        for line in ('3.8', '3.9'):
            original_files, _, now = product_fixture('core', line)
            candidate, _, _, _ = self.candidate_plan(line)
            for kind, plan in (('original', json.loads(original_files['plan.json'])), ('candidate', candidate)):
                tag = plan['source']['observation']['tag_history'][0]
                snapshots = [[], [tag, deepcopy(tag)], None, {}, [None],
                    [{k: v for k, v in tag.items() if k != 'node_id'}], [tag | {'extra': True}],
                    [tag | {'ref': 'refs/tags/9.9.0'}], [tag | {'node_id': 1}],
                    [tag | {'node_id': '/private'}], [tag | {'node_id': 'private:value'}],
                    [tag | {'url': 'https://example.com/tag'}], [tag | {'object': None}]]
                snapshots.extend([tag | {'node_id': value}] for value in ('', ' ', '\t', '\n', '\u00a0', 'node id', 'node\x7f'))
                suffixes = ('?query', '#fragment', ':private', '/../../escape', '..preview', '@{1}',
                            ' space', '~1', '^1', '*', '[1]', '\\child', '//child', '/', '.',
                            '/.hidden', '/.', '/name.lock', '/name.lock/child', '/name.lock.lock')
                for suffix in (*suffixes, *(chr(code) for code in (*range(32), 127))):
                    ref = tag['ref'] + suffix
                    snapshots.append([tag | {'ref': ref, 'url':
                        'https://api.github.com/repos/elsa-workflows/elsa-core/git/' + ref}])
                for mutation in ({'sha': 'a'}, {'sha': 'a' * 39 + 'G'}, {'sha': 1}, {'type': 'tree'},
                                 {'url': 'https://example.com/object'}, {'extra': True}):
                    snapshots.append([tag | {'object': tag['object'] | mutation}])
                snapshots.append([tag | {'object': {k: v for k, v in tag['object'].items() if k != 'sha'}}])
                for index, snapshot in enumerate(snapshots):
                    changed = deepcopy(plan); changed['source']['observation']['tag_history'] = snapshot
                    data = encoded(changed); digest = metadata.sha256(data)
                    with self.subTest(line=line, kind=kind, snapshot=index):
                        with self.assertRaisesRegex(ValueError, 'plan_malformed'):
                            artifacts.admit(data, digest, checked_at=now.isoformat())
                        with patch.object(Path, 'read_bytes', side_effect=AssertionError('filesystem')), \
                             patch.object(maintenance, 'git', side_effect=AssertionError('Git')), \
                             patch('urllib.request.urlopen', side_effect=AssertionError('network')), \
                             self.assertRaises(ValueError):
                            schema.admit(changed, digest, data, now.isoformat(), contracts)
                        with tempfile.TemporaryDirectory() as directory:
                            output = Path(directory) / 'proof'
                            with patch.object(artifacts, 'verify_controller', side_effect=AssertionError('controller touched')), \
                                 patch.object(metadata, 'checkout_source', side_effect=AssertionError('source setup')), \
                                 patch.object(core, 'verify_source', side_effect=AssertionError('source verification')), \
                                 patch.object(artifacts, 'preflight', side_effect=AssertionError('preflight')), \
                                 patch.object(artifacts, 'refresh_remote', side_effect=AssertionError('remote')), \
                                 patch.object(maintenance, 'prepare', side_effect=AssertionError('native')), \
                                 patch.object(maintenance, 'git', side_effect=AssertionError('Git')), \
                                 patch('urllib.request.urlopen', side_effect=AssertionError('network')):
                                with self.assertRaisesRegex(ValueError, 'plan_malformed'):
                                    artifacts.execute(Path(directory), data, digest, output)
                            self.assertFalse(output.exists())

    def test_new_planner_inputs_are_bound_and_stale_or_unbound_inputs_rejected(self):
        """Verify new planner inputs are bound and stale or unbound inputs rejected."""
        self.assertIs(metadata.PLANNER_INPUTS, artifacts.PLANNER_INPUTS)
        self.assertTrue({'scripts/integration-program/core_source_continuation.py',
                         'scripts/integration-program/core_source_continuation_contract.json',
                         'scripts/integration-program/selected_core_contract.json'} <= metadata.PLANNER_INPUTS)
        contracts = payload.load_contracts(); plan, _, _, now = self.candidate_plan('3.8')
        for path in metadata.PLANNER_INPUTS:
            for remove in (True, False):
                changed = deepcopy(plan)
                if remove: changed['controller']['input_sha256'].pop(path)
                else: changed['controller']['input_sha256'][path] = 'a' * 64
                with self.subTest(path=path, remove=remove), self.assertRaises(ValueError):
                    schema.admit(changed, metadata.sha256(encoded(changed)), encoded(changed), now.isoformat(), contracts)

    def candidate_files(self, line):
        """Fresh synthetic candidate bytes; retained historical fixtures stay intact."""
        plan, files, expected, now = self.candidate_plan(line)
        source = plan['source']; old = source['original_commit']
        producer, consumer_receipt = (json.loads(files[name]) for name in
                                     ('producer/receipt.json', 'consumer/receipt.json'))
        replacements = {}
        for record in producer['packages']['selected']:
            name = 'producer/nuget/' + record['file']
            before = files[name]
            members = {path: data.replace(old.encode(), source['commit'].encode()) for path, data in
                       transport.zip_members(before, leaf=True).items()}
            data = zipped(list(members.items())); files[name] = data
            replacements[metadata.sha256(before)] = metadata.sha256(data)
            replacements[hashlib.sha512(before).hexdigest()] = hashlib.sha512(data).hexdigest()
            record.update(sha256=metadata.sha256(data), size=len(data), inventory=transport.inventory(members))
        def retarget(value):
            if type(value) is dict:
                if value.get('kind') == 'observed-core-release-branch':
                    return deepcopy(source)
                return {key: retarget(item) for key, item in value.items()}
            if type(value) is list:
                return [retarget(item) for item in value]
            if type(value) is str:
                return replacements.get(value, value.replace(old, source['commit']))
            return value
        producer, consumer_receipt = retarget(producer), retarget(consumer_receipt)
        for native in producer['package_verification']:
            native['files'] = [{'name': r['file'], 'sha256': r['sha256'], 'size': r['size']}
                               for r in producer['packages']['selected'] if r['id'] == native['id']]
        files['plan.json'] = encoded(plan); plan_hash = metadata.sha256(files['plan.json'])
        producer['plan_sha256'] = producer['execution']['plan_sha256'] = plan_hash
        files['producer/receipt.json'] = encoded(producer)
        producer_hash = metadata.sha256(files['producer/receipt.json'])
        consumer_receipt['plan_sha256'] = consumer_receipt['execution']['plan_sha256'] = plan_hash
        consumer_receipt['artifact_receipt_sha256'] = producer_hash
        consumer_receipt['producer_plan_admission']['artifact_receipt_sha256'] = producer_hash
        consumer_receipt['producer_execution'] = producer['execution']
        files['consumer/receipt.json'] = encoded(consumer_receipt)
        return files, expected, now

    def test_pure_candidate_seal_and_independent_readback_bind_complete_source(self):
        """Verify pure candidate seal and independent readback bind complete source."""
        contracts = payload.load_contracts()
        for line in ('3.8', '3.9'):
            files, context, now = self.candidate_files(line)
            hashes = tuple(metadata.sha256(files[name]) for name in
                           ('plan.json', 'producer/receipt.json', 'consumer/receipt.json'))
            provider_fixture = seal_tests.SealContracts(); provider_fixture.setUp()
            with patch('pathlib.Path.read_bytes', side_effect=AssertionError('pure file IO')), \
                 patch.object(maintenance, 'git', side_effect=AssertionError('pure git')), \
                 patch('urllib.request.urlopen', side_effect=AssertionError('pure network')):
                sealed = seal.stage(files, context, hashes, contracts=contracts, now=now)
                data = zipped(list(sealed.items()))
                manifest_hash = metadata.sha256(sealed[transport.MANIFEST])
                expected = context | {'manifest_sha256': manifest_hash}
                provider = provider_fixture.provider | {'manifest_sha256': manifest_hash,
                    'archive_sha256': metadata.sha256(data), 'archive_size': len(data),
                    'artifact_name': f"selected-control-core-{line}-{context['context']['head_sha']}-{context['context']['run_id']}-{context['context']['run_attempt']}"}
                result = seal.readback(data, provider, expected, contracts=contracts, now=now)
                self.assertEqual(json.loads(files['plan.json'])['source'], result['source'])
                self.assertFalse(result['product_code_executed_during_readback'])

    def test_pure_seal_rejects_source_swaps_even_after_outer_hashes_are_recomputed(self):
        """Verify pure seal rejects source swaps even after outer hashes are recomputed."""
        contracts = payload.load_contracts()
        for line in ('3.8', '3.9'):
            files, expected, now = self.candidate_files(line)
            for name in ('producer/receipt.json', 'consumer/receipt.json'):
                changed = dict(files); receipt = json.loads(changed[name])
                receipt['source'] = self.history['core-' + line]['source']
                changed[name] = encoded(receipt)
                hashes = tuple(metadata.sha256(changed[n]) for n in
                               ('plan.json', 'producer/receipt.json', 'consumer/receipt.json'))
                with patch('pathlib.Path.read_bytes', side_effect=AssertionError('pure file IO')), \
                     patch.object(maintenance, 'git', side_effect=AssertionError('pure git')), \
                     patch('urllib.request.urlopen', side_effect=AssertionError('pure network')):
                    with self.subTest(line=line, receipt=name), self.assertRaises(ValueError):
                        seal.stage(changed, expected, hashes, contracts=contracts, now=now)
