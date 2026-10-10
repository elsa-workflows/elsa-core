"""Synthetic restored hints through the real emitter/seal/readback; no native proof."""
from copy import deepcopy
from datetime import timedelta
import base64
import json
import unittest
from unittest.mock import patch

import prepare_maintenance_build as maintenance
import product_release_metadata as metadata
import prove_consolidated_packages as archives
import selected_control_seal as seal
import selected_control_transport as transport
import selected_studio_payload as payload
import test_core_source_continuation as candidate_tests
import test_prepare_maintenance_build as maintenance_tests
import test_selected_control_transport as transport_tests
from test_selected_product_payload import fixture as product_fixture
from test_selected_studio_payload import encoded

VERSION = '0.0.1-preview.53'
IDENTIFIER = 'Elsa.Platform.PackageManifest.Generator'


class CoreManifestHintSealTests(unittest.TestCase):
    def setUp(self):
        self.hints = maintenance_tests.MaintenanceContracts()
        self.hints.setUp()
        self.addCleanup(self.hints.doCleanups)
        self.candidates = candidate_tests.CoreSourceContinuationContracts()
        self.candidates.setUp()
        self.contracts = payload.load_contracts()
        provider = transport_tests.Fixture()
        provider.setUp()
        self.provider = provider.provider

    def emitted_fixture(self, line='3.8', kind='original', framework='net8.0'):
        values = (self.candidates.candidate_files(line) if kind == 'continuation'
                  else product_fixture('core', line))
        files, _, _ = values
        plan, producer, consumer = (json.loads(files[name]) for name in
                                   ('plan.json', 'producer/receipt.json', 'consumer/receipt.json'))
        selected = plan['inventory']['selected'][0]
        source_row = plan['source'] | {'source_repository': 'elsa-workflows/elsa-core'}
        root, row, policy, document, archive, assets = self.hints.restored_hint_fixture(
            VERSION, framework, source_row, entry_name='ManifestFeatureCategoryAttribute.cs')
        policy.update(id=selected['id'], project=selected['project'])
        pin = maintenance.digest(archive.read_bytes())
        with patch.dict(archives.GENERATOR_SOURCE_PINS, {VERSION: pin}):
            # Run the actual retained document writer, including restored archive,
            # compiler input, embedded checksum and exact Core source admission.
            emitted = maintenance.verify_documents({'source_link': {'documents': {'/_/*':
                f"https://raw.githubusercontent.com/elsa-workflows/elsa-core/{row['commit']}/*"}},
                'documents': [document]}, root, row, policy, framework)[0]
        selected['metadata']['manifest_content_targets'] = [{'id': IDENTIFIER, 'version': VERSION,
            'restore_sha512': assets['libraries'][IDENTIFIER + '/' + VERSION]['sha512'],
            'entry': 'build/Elsa.Platform.PackageManifest.Generator.targets', 'sha256': metadata.sha256(b'targets')}]
        symbol = next(s for s in producer['package_verification'][0]['symbols']
                      if s['assembly'].split('/')[1] == framework)
        symbol['documents'] = [emitted]
        self.bind(files, plan, producer, consumer)
        return values, pin, emitted

    def bind(self, files, plan, producer, consumer):
        """Recompute outer joins so mutations reach the document policy itself."""
        plan['inventory']['sha256'] = metadata.canonical_hash(metadata.public_inventory(plan['inventory']))
        files['plan.json'] = encoded(plan)
        plan_hash = metadata.sha256(files['plan.json'])
        producer['plan_sha256'] = producer['execution']['plan_sha256'] = plan_hash
        consumer['plan_sha256'] = consumer['execution']['plan_sha256'] = plan_hash
        files['producer/receipt.json'] = encoded(producer)
        producer_hash = metadata.sha256(files['producer/receipt.json'])
        consumer['artifact_receipt_sha256'] = consumer['producer_plan_admission']['artifact_receipt_sha256'] = producer_hash
        consumer['producer_execution'] = producer['execution']
        files['consumer/receipt.json'] = encoded(consumer)

    def stage(self, values):
        files, expected, now = values
        return seal.stage(files, expected, tuple(metadata.sha256(files[name]) for name in
            ('plan.json', 'producer/receipt.json', 'consumer/receipt.json')), contracts=self.contracts, now=now)

    def readback(self, values, sealed):
        _, context, now = values
        data = transport_tests.zipped(list(sealed.items()))
        manifest_hash = metadata.sha256(sealed[transport.MANIFEST])
        provider = self.provider | {'manifest_sha256': manifest_hash,
            'archive_sha256': metadata.sha256(data), 'archive_size': len(data),
            'artifact_name': f"selected-control-{context['product']}-{context['line']}-{context['context']['head_sha']}-{context['context']['run_id']}-{context['context']['run_attempt']}",
            'created_at': (now - timedelta(minutes=2)).isoformat(), 'retrieved_at': (now - timedelta(minutes=1)).isoformat(),
            'expires_at': (now + timedelta(days=1)).isoformat()}
        return seal.readback(data, provider, context | {'manifest_sha256': manifest_hash}, contracts=self.contracts, now=now)

    def test_actual_emitted_core_preview53_documents_seal_and_read_back_all_twelve_cells(self):
        for line in ('3.8', '3.9'):
            for kind in ('original', 'continuation'):
                for framework in ('net8.0', 'net9.0', 'net10.0'):
                    with self.subTest(line=line, kind=kind, framework=framework):
                        values, pin, emitted = self.emitted_fixture(line, kind, framework)
                        self.assertEqual(emitted['family'], 'manifest-hints')
                        with patch.dict(archives.GENERATOR_SOURCE_PINS, {VERSION: pin}), \
                             patch('pathlib.Path.read_bytes', side_effect=AssertionError('pure file IO')), \
                             patch.object(maintenance, 'git', side_effect=AssertionError('pure Git')), \
                             patch('urllib.request.urlopen', side_effect=AssertionError('pure network')):
                            sealed = self.stage(values)
                            self.assertEqual(values[0]['producer/receipt.json'], sealed['producer/receipt.json'])
                            result = self.readback(values, sealed)
                        self.assertTrue(result['success'])
                        self.assertEqual(json.loads(values[0]['plan.json'])['source'], result['source'])
                        self.assertFalse(result['product_code_executed_during_readback'])

    def test_rehashed_hint_producer_and_plan_mutations_fail_seal_and_independent_readback(self):
        values, pin, emitted = self.emitted_fixture()
        checksum = emitted['producer']['restore_sha512']
        alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/'
        noncanonical = checksum[:85] + alphabet[alphabet.index(checksum[85]) | 1] + '=='
        mutations = [
            ('producer-extra', lambda p, d: d['producer'].update(private_path='/tmp/private')),
            ('producer-missing', lambda p, d: d['producer'].pop('restore_sha512')),
            ('producer-null', lambda p, d: d.update(producer=None)),
            ('family-sdk', lambda p, d: d.update(family='sdk')),
            ('family-regex', lambda p, d: d.update(family='regex')),
            ('family-unknown', lambda p, d: d.update(family='other-hints')),
            ('wrong-package', lambda p, d: d['producer'].update(external_package='Other.Generator/' + VERSION)),
            ('preview50', lambda p, d: d['producer'].update(external_package=IDENTIFIER + '/0.0.1-preview.50')),
            ('unreviewed-version', lambda p, d: d['producer'].update(external_package=IDENTIFIER + '/0.0.1-preview.54')),
            ('wrong-pin', lambda p, d: d['producer'].update(archive_sha256='a' * 64)),
            ('wrong-feed', lambda p, d: d['producer'].update(feed='https://example.invalid/nuget/index.json')),
            ('near-feed', lambda p, d: d['producer'].update(feed=archives.GENERATOR_FEED + '/')),
            ('unreviewed-entry', lambda p, d: d['producer'].update(archive_entry=archives.GENERATOR_HINTS_PREFIX + 'Other.cs')),
            ('traversal-entry', lambda p, d: d['producer'].update(archive_entry='../ManifestFeatureCategoryAttribute.cs')),
            ('nonstring-entry', lambda p, d: d['producer'].update(archive_entry=[])),
            ('restore-null', lambda p, d: d['producer'].update(restore_sha512=None)),
            ('restore-alphabet', lambda p, d: d['producer'].update(restore_sha512='!' * 88)),
            ('restore-length', lambda p, d: d['producer'].update(restore_sha512=base64.b64encode(b'x' * 63).decode())),
            ('restore-noncanonical', lambda p, d: d['producer'].update(restore_sha512=noncanonical)),
            ('restore-mismatch', lambda p, d: d['producer'].update(restore_sha512=base64.b64encode(b'x' * 64).decode())),
            ('checksum-invalid', lambda p, d: d.update(checksum='z' * 64)),
            ('algorithm-invalid', lambda p, d: d.update(algorithm='sha512')),
            ('private-document-path', lambda p, d: d.update(path='/tmp/private.cs')),
            ('document-extra', lambda p, d: d.update(remote_fetched=True)),
        ]
        target_mutations = [
            ('plan-missing-target', lambda rows: rows.clear()),
            ('plan-duplicate-target', lambda rows: rows.append(deepcopy(rows[0]))),
            ('plan-null-target', lambda rows: rows.__setitem__(0, None)),
            ('plan-target-extra', lambda rows: rows[0].update(path='/tmp/private')),
            ('plan-target-missing', lambda rows: rows[0].pop('id')),
            ('plan-target-nonstring-id', lambda rows: rows[0].update(id=123)),
            ('plan-wrong-id', lambda rows: rows[0].update(id='Other.Generator')),
            ('plan-wrong-version', lambda rows: rows[0].update(version='0.0.1-preview.50')),
            ('plan-wrong-restore', lambda rows: rows[0].update(restore_sha512=base64.b64encode(b'x' * 64).decode())),
            ('plan-wrong-entry', lambda rows: rows[0].update(entry='build/Other.targets')),
            ('plan-invalid-checksum', lambda rows: rows[0].update(sha256='bad')),
        ]
        mutations += [(name, lambda p, d, mutate=mutate: mutate(
            p['inventory']['selected'][0]['metadata']['manifest_content_targets'])) for name, mutate in target_mutations]
        with patch.dict(archives.GENERATOR_SOURCE_PINS, {VERSION: pin}):
            baseline = self.stage(values)
            for name, mutate in mutations:
                changed = deepcopy(values)
                files = changed[0]
                plan, producer, consumer = (json.loads(files[n]) for n in
                                           ('plan.json', 'producer/receipt.json', 'consumer/receipt.json'))
                mutate(plan, producer['package_verification'][0]['symbols'][0]['documents'][0])
                self.bind(files, plan, producer, consumer)
                with self.subTest(mutation=name, boundary='seal'), self.assertRaises(ValueError):
                    self.stage(changed)
                # Rebuild every outer hash and ZIP inventory without calling the
                # seal validator: readback must independently reject forged bytes.
                manifest = json.loads(baseline[transport.MANIFEST])
                manifest.update(plan_sha256=metadata.sha256(files['plan.json']),
                    producer_receipt_sha256=metadata.sha256(files['producer/receipt.json']),
                    consumer_receipt_sha256=metadata.sha256(files['consumer/receipt.json']),
                    artifact_execution=producer['execution'], consumer_execution=consumer['execution'],
                    files=transport.inventory(files))
                forged = files | {transport.MANIFEST: encoded(manifest)}
                with self.subTest(mutation=name, boundary='readback'), self.assertRaises(ValueError):
                    self.readback(changed, forged)

    def test_synthetic_archive_is_rejected_by_unpatched_official_pin(self):
        values, _, _ = self.emitted_fixture()
        with self.assertRaisesRegex(ValueError, 'core_payload_manifest_hint_identity'):
            self.stage(values)

    def test_extensions_preview50_emitter_and_seal_readback_remain_unchanged(self):
        version = '0.0.1-preview.50'
        for line in ('3.8', '3.9'):
            row = next(r for r in self.hints.register['sources'] if r['product'] == 'extensions' and r['line'] == line)
            root, row, policy, document, archive, _ = self.hints.restored_hint_fixture(version, row=row)
            with self.subTest(line=line), patch.dict(archives.GENERATOR_SOURCE_PINS,
                                                   {version: maintenance.digest(archive.read_bytes())}):
                emitted = maintenance.verify_documents({'source_link': {'documents': {'/_/*':
                    f"https://raw.githubusercontent.com/{row['source_repository']}/{row['commit']}/*"}},
                    'documents': [document]}, root, row, policy, 'net10.0')[0]
                self.assertEqual(emitted['family'], 'manifest-hints')
                self.assertEqual(emitted['producer']['external_package'], IDENTIFIER + '/' + version)
                values = product_fixture('extensions', line)
                self.assertTrue(self.readback(values, self.stage(values))['success'])


if __name__ == '__main__':
    unittest.main()
