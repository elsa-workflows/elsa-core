"""Real synthetic ZIP readback and safe staging, without product/provider work."""
from copy import deepcopy
from contextlib import redirect_stdout
from datetime import timedelta
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import product_release_metadata as metadata
import selected_control_seal as seal
import selected_control_transport as transport
import selected_studio_payload as payload
from test_selected_control_transport import zipped
from test_selected_studio_payload import fixture, encoded


class SealContracts(unittest.TestCase):
    def setUp(self):
        self.files, self.context, self.now = fixture()
        self.contracts = payload.load_contracts()
        self.hashes = tuple(metadata.sha256(self.files[name]) for name in
            ('plan.json', 'producer/receipt.json', 'consumer/receipt.json'))
        self.sealed = seal.stage(self.files, self.context, self.hashes, contracts=self.contracts, now=self.now)
        self.expected = self.context | {'manifest_sha256': metadata.sha256(self.sealed[transport.MANIFEST])}
        context = self.context['context']
        self.provider = {'schema': 1, 'repository': context['repository'], 'repository_id': context['repository_id'],
            'run_id': context['run_id'], 'run_attempt': context['run_attempt'], 'head_sha': context['head_sha'],
            'head_branch': transport.HOSTED_REF.removeprefix('refs/heads/'), 'event': 'push',
            'workflow_path': transport.WORKFLOW, 'control_job': 'control', 'control_conclusion': 'success',
            'run_status': 'in_progress', 'run_conclusion': None, 'artifact_id': 456,
            'artifact_name': f"selected-control-studio-3.8-{context['head_sha']}-{context['run_id']}-{context['run_attempt']}",
            'archive_sha256': '0' * 64, 'archive_size': 1, 'manifest_sha256': self.expected['manifest_sha256'],
            'created_at': (self.now - timedelta(minutes=2)).isoformat(),
            'expires_at': (self.now + timedelta(days=1)).isoformat(), 'expired': False,
            'retrieved_at': (self.now - timedelta(minutes=1)).isoformat()}

    def archive(self, sealed=None):
        sealed = self.sealed if sealed is None else sealed
        data = zipped(list(sealed.items()))
        self.expected['manifest_sha256'] = self.provider['manifest_sha256'] = metadata.sha256(sealed[transport.MANIFEST])
        self.provider.update(archive_size=len(data), archive_sha256=metadata.sha256(data))
        return data

    def readback(self, data):
        return seal.readback(data, self.provider, self.expected, contracts=self.contracts, now=self.now)

    def test_studio_vertical_original_zip_in_independent_job(self):
        original = self.archive()
        with patch.dict('os.environ', {'GITHUB_JOB': 'readback', 'GITHUB_RUN_ID': 'different'}):
            result = self.readback(original)
        self.assertTrue(result['success'])
        self.assertFalse(result['product_code_executed_during_readback'])
        self.assertEqual(result['consumer_execution_scope'], 'preupload-exact-local-archives')
        self.assertEqual(result['provider']['archive_sha256'], metadata.sha256(original))
        self.assertEqual(len(result['files']), len(self.files))
        self.assertEqual(self.sealed['producer/receipt.json'], self.files['producer/receipt.json'])

    def test_transport_only_manifest_cannot_authorize_control(self):
        changed = dict(self.sealed)
        manifest = json.loads(changed[transport.MANIFEST])
        manifest['scope'] = transport.TRANSPORT_SCOPE
        changed[transport.MANIFEST] = encoded(manifest)
        with self.assertRaisesRegex(ValueError, 'manifest_binding'):
            self.readback(self.archive(changed))

    def test_full_closure_and_semantic_joins_rechecked_after_outer_rehash(self):
        for kind in ('extra', 'missing', 'private', 'runtime', 'npm', 'source', 'execution'):
            changed = dict(self.sealed)
            manifest = json.loads(changed[transport.MANIFEST])
            if kind == 'extra': changed['producer/nuget/unlisted.1.0.0.nupkg'] = b'excluded'
            elif kind == 'missing': del changed['producer/npm/receipt.json']
            elif kind == 'private': manifest['private_location'] = '/private/tmp/raw.log'
            elif kind in ('runtime', 'source', 'execution'):
                result = json.loads(changed['consumer/receipt.json'])
                if kind == 'runtime': result['runtime'][0]['runtime']['loaded_assemblies'][0]['sha256'] = 'f' * 64
                elif kind == 'source': result['source']['commit'] = 'f' * 40
                else: result['execution']['context']['run_attempt'] = '99'
                changed['consumer/receipt.json'] = encoded(result)
                manifest['consumer_receipt_sha256'] = metadata.sha256(changed['consumer/receipt.json'])
            elif kind == 'npm':
                result = json.loads(changed['producer/npm/receipt.json'])
                result['consumer']['lifecycle_generated_assets']['assets'][0]['sha256'] = 'f' * 64
                changed['producer/npm/receipt.json'] = encoded(result)
            manifest['files'] = transport.inventory({key: value for key, value in changed.items() if key != transport.MANIFEST})
            changed[transport.MANIFEST] = encoded(manifest)
            with self.subTest(kind=kind), self.assertRaises(ValueError): self.readback(self.archive(changed))

    def test_provider_identity_and_time_are_rechecked(self):
        data = self.archive()
        cases = {'run_attempt': '99', 'head_sha': 'f' * 40, 'event': 'pull_request', 'artifact_id': 0,
                 'artifact_name': 'latest', 'archive_size': len(data) + 1, 'archive_sha256': 'f' * 64,
                 'expired': True, 'retrieved_at': (self.now + timedelta(seconds=1)).isoformat(),
                 'expires_at': self.now.isoformat(), 'control_conclusion': 'failure'}
        original = deepcopy(self.provider)
        for key, value in cases.items():
            self.provider[key] = value
            with self.subTest(key=key), self.assertRaises(ValueError): self.readback(data)
            self.provider = deepcopy(original)

    def test_freeze_exact_safe_retained_files_preserves_bytes(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve()
            paths = {name: root / name for name in self.files}
            for name, data in self.files.items():
                paths[name].parent.mkdir(parents=True, exist_ok=True)
                paths[name].write_bytes(data)
            frozen, originals = seal.freeze_inputs(paths['plan.json'], self.hashes[0], root / 'producer', self.hashes[1],
                                                  paths['consumer/receipt.json'], self.hashes[2])
            self.assertEqual(frozen, self.files)
            self.assertEqual(len(originals), len(self.files))
            output = seal.output_location(root / 'sealed', [root / 'producer', root / 'consumer', paths['plan.json']])
            seal.write_new(output, self.sealed)
            self.assertEqual({path.relative_to(output).as_posix(): path.read_bytes() for path in output.rglob('*') if path.is_file()}, self.sealed)
            with self.assertRaises(ValueError): seal.output_location(output, [])
            with self.assertRaises(ValueError): seal.output_location(root / 'producer/out', [root / 'producer'])
            (root / 'producer/raw.log').write_text('private')
            with self.assertRaisesRegex(ValueError, 'closure'):
                seal.freeze_inputs(paths['plan.json'], self.hashes[0], root / 'producer', self.hashes[1],
                                   paths['consumer/receipt.json'], self.hashes[2])

    def test_malformed_nuspec_cli_rejects_generically_before_output(self):
        receipt = json.loads(self.files['producer/receipt.json'])
        row = receipt['packages']['selected'][0]
        name = 'producer/nuget/' + row['file']
        members = transport.zip_members(self.files[name], leaf=True)
        members['a.nuspec'] = b'<package><metadata>malformed private input'
        self.files[name] = zipped(list(members.items()))
        row.update(sha256=metadata.sha256(self.files[name]), size=len(self.files[name]), inventory=transport.inventory(members))
        self.files['producer/receipt.json'] = encoded(receipt)
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve()
            inputs, output = root / 'inputs', root / 'output'
            for path, data in self.files.items():
                target = inputs / path
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(data)
            args = ['selected_control_seal.py', 'seal', '--plan', str(inputs / 'plan.json'),
                '--plan-sha256', metadata.sha256(self.files['plan.json']), '--producer-retained', str(inputs / 'producer'),
                '--producer-receipt-sha256', metadata.sha256(self.files['producer/receipt.json']),
                '--consumer-receipt', str(inputs / 'consumer/receipt.json'),
                '--consumer-receipt-sha256', metadata.sha256(self.files['consumer/receipt.json']), '--output', str(output)]
            printed = io.StringIO()
            # Only trusted runner/Git identity boundaries are stubbed. Real frozen
            # ZIP bytes and the actual nuspec parser reach the CLI failure guard.
            with patch('sys.argv', args), patch.object(seal.producer, 'verify_controller', return_value=self.context['controller']), \
                 patch.object(seal, 'hosted_context', return_value=self.context['context']), redirect_stdout(printed):
                self.assertEqual(seal.main(), 1)
            self.assertEqual(printed.getvalue(), 'Selected seal/readback rejected inputs; no selected control acceptance emitted.\n')
            self.assertFalse(output.exists())
            self.assertNotIn('malformed private input', printed.getvalue())

    def test_retained_symlink_and_rejected_write_leave_no_output(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve()
            retained = root / 'producer'
            retained.mkdir()
            (retained / 'receipt.json').symlink_to(root / 'outside')
            with self.assertRaisesRegex(ValueError, 'path'): seal.retained_files(retained)
            output = root / 'new'
            with patch.object(Path, 'open', side_effect=OSError('fixture write failure')):
                with self.assertRaises(OSError): seal.write_new(output, {'plan.json': b'bytes'})
            self.assertFalse(output.exists())


if __name__ == '__main__': unittest.main()
