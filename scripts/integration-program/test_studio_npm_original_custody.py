"""Original custody/admission contracts, using the accepted producer/provider fixtures."""
import ast
import copy
from contextlib import redirect_stdout
import io
import json
import os
import subprocess
import sys
import textwrap
import unittest
from unittest.mock import patch

import studio_npm_original_custody as custody
import publish_studio_npm_pair as publication
import test_publish_studio_npm_pair as fixtures

WORKFLOW = publication.pair.ROOT / publication.WORKFLOW
SOURCE = 'c' * 40
RUN = 42


def reader_code():
    workflow = WORKFLOW.read_text()
    return textwrap.dedent(workflow.split("          python3 - <<'PY'\n", 1)[1].split('          PY\n', 1)[0])


def reviewed_packet():
    tree = ast.parse(reader_code())
    return next(node.value.value for node in ast.walk(tree)
                if isinstance(node, ast.Assign) and any(isinstance(target, ast.Name)
                and target.id == 'packet_bytes' for target in node.targets))


class CustodyContracts(unittest.TestCase):
    def setUp(self):
        self.fixture = fixtures.PublicationContracts()
        self.fixture.setUp()
        self.addCleanup(self.fixture.doCleanups)
        self.root = self.fixture.root
        self.candidate, self.archive, self.packet = self.fixture.bundle(self.fixture.fixture.proof)
        self.provider = fixtures.Provider(self.candidate, self.archive)
        self.inputs = self.root / 'inputs'
        self.inputs.mkdir()
        (self.inputs / 'original.zip').write_bytes(self.archive.read_bytes())
        self.packet_bytes = (json.dumps(self.packet, indent=2, sort_keys=True) + '\n').encode()
        (self.inputs / 'packet.json').write_bytes(self.packet_bytes)
        self.env = {'GITHUB_ACTIONS': 'true', 'GITHUB_REPOSITORY': custody.REPOSITORY,
            'GITHUB_SHA': SOURCE, 'GITHUB_RUN_ID': str(RUN), 'GITHUB_RUN_ATTEMPT': '1',
            'GITHUB_EVENT_NAME': 'workflow_dispatch', 'GITHUB_REF': 'refs/heads/main', 'GITHUB_JOB': 'verify',
            'GITHUB_WORKFLOW_SHA': SOURCE,
            'GITHUB_WORKFLOW_REF': f'{custody.REPOSITORY}/{publication.WORKFLOW}@refs/heads/main',
            'EXPECTED_HEAD': SOURCE, 'RUNNER_TEMP': str(self.root)}
        self.runtime = self.provider.run | {'id': RUN, 'head_sha': SOURCE, 'status': 'in_progress',
            'conclusion': None, 'path': publication.WORKFLOW}
        self.transfer = copy.deepcopy(self.provider.artifact)
        self.transfer.update(id=456, name=f'studio-npm-original-input-{RUN}-1', digest='sha256:' + 'd' * 64)
        self.transfer['workflow_run'].update(id=RUN, head_sha=SOURCE)
        self.retrieval = {'schema': 1, 'packet_sha256': publication.digest(self.packet_bytes),
            'archive_sha256': self.packet['archive_sha256'], 'archive_size': self.archive.stat().st_size,
            'retrieved_at': '2026-01-01T00:00:00Z', 'executor_source_commit': SOURCE,
            'executor_run_id': RUN, 'executor_run_attempt': 1}
        self.write_transport()
        for name, value in (('PACKET_SHA256', publication.digest(self.packet_bytes)),
                            ('ARCHIVE_SIZE', self.archive.stat().st_size)):
            replacement = patch.object(custody, name, value)
            replacement.start()
            self.addCleanup(replacement.stop)
        replacement = patch.object(publication.pair, 'git', side_effect=lambda root, *args: SOURCE if args[0] == 'rev-parse' else '')
        replacement.start()
        self.addCleanup(replacement.stop)

    def write_transport(self):
        for name, value in (('artifact.json', custody.artifact_record(self.provider.artifact)),
                            ('producer-run.json', custody.run_record(self.provider.run)), ('retrieval.json', self.retrieval)):
            (self.inputs / name).write_text(json.dumps(value))

    def get(self, path):
        if path.endswith('/artifacts/456'):
            return self.transfer
        if path.endswith(f'/runs/{RUN}/attempts/1'):
            return self.runtime
        return self.provider.get(path)

    def admit(self, **kwargs):
        return custody.admit_custody(self.inputs, SOURCE, 456, 'd' * 64, api=self, env=self.env, **kwargs)

    def test_original_and_transfer_are_bound_to_independent_executor(self):
        result = self.admit()
        self.assertTrue(result['success'])
        self.assertFalse(result['publisher_authority'])
        self.assertEqual(self.packet, result['original_packet'])
        self.assertEqual(123, result['original_provider']['id'])
        self.assertEqual(456, result['transfer_provider']['id'])
        self.assertEqual(SOURCE, result['executor']['source_commit'])
        self.assertNotEqual(SOURCE, self.packet['source_commit'])
        self.assertEqual(self.archive.read_bytes(), (self.inputs / 'original.zip').read_bytes())

    def test_packet_bytes_all_fields_and_archive_are_immutable(self):
        for key in self.packet:
            changed = self.packet | {key: 2 if type(self.packet[key]) is int else 'wrong'}
            (self.inputs / 'packet.json').write_text(json.dumps(changed))
            with self.subTest(key=key), self.assertRaisesRegex(publication.PublicationError, 'packet_changed'):
                self.admit()
        (self.inputs / 'packet.json').write_bytes(self.packet_bytes.rstrip())
        with self.assertRaisesRegex(publication.PublicationError, 'packet_changed'):
            self.admit()
        (self.inputs / 'packet.json').write_bytes(self.packet_bytes)
        archive = self.inputs / 'original.zip'
        data = archive.read_bytes()
        archive.write_bytes(bytes([data[0] ^ 1]) + data[1:])
        with self.assertRaisesRegex(publication.PublicationError, 'original_archive_changed'):
            self.admit()

    def test_transport_missing_extra_symlink_and_parent_traversal_fail_closed(self):
        leaf = self.inputs / 'packet.json'
        leaf.unlink()
        with self.assertRaisesRegex(publication.PublicationError, 'transport_shape'):
            self.admit()
        leaf.symlink_to(self.archive)
        with self.assertRaises(publication.pair.ProofError):
            self.admit()
        leaf.unlink()
        leaf.write_bytes(self.packet_bytes)
        (self.inputs / 'unexpected').write_bytes(b'private')
        with self.assertRaisesRegex(publication.PublicationError, 'transport_shape'):
            self.admit()
        with self.assertRaisesRegex(ValueError, 'parent traversal'):
            custody.admit_custody(self.inputs / '..' / 'inputs', SOURCE, 456, 'd' * 64, api=self, env=self.env)
        alias = self.root / 'alias'
        alias.symlink_to(self.inputs, target_is_directory=True)
        with self.assertRaisesRegex(ValueError, 'symlink'):
            custody.admit_custody(alias, SOURCE, 456, 'd' * 64, api=self, env=self.env)

    def test_retained_and_fresh_provider_failures_stop_custody(self):
        for field, changed in (('id', 999), ('name', 'wrong'), ('digest', 'wrong'), ('expired', True),
                               ('expires_at', '2000-01-01T00:00:00Z'), ('size_in_bytes', 1)):
            for retained in (True, False):
                original = copy.deepcopy(self.provider.artifact)
                self.provider.artifact[field] = changed
                if retained:
                    self.write_transport()
                with self.subTest(field=field, retained=retained), self.assertRaises(publication.PublicationError):
                    self.admit()
                self.provider.artifact = original
                self.write_transport()
        for field, changed in (('run_attempt', 2), ('head_sha', SOURCE), ('conclusion', 'failure'),
                               ('status', 'in_progress'), ('path', publication.WORKFLOW)):
            original = copy.deepcopy(self.provider.run)
            self.provider.run[field] = changed
            with self.subTest(field=field), self.assertRaises(publication.PublicationError):
                self.admit()
            self.provider.run = original
        with patch.object(self, 'get', side_effect=RuntimeError('private-token-or-signed-url')):
            with self.assertRaises(RuntimeError):
                self.admit()

    def test_transfer_and_retrieval_cannot_relabel_original_or_executor(self):
        for field, changed in (('id', 123), ('name', self.packet['artifact_name']), ('digest', 'sha256:' + 'e' * 64),
                               ('expired', True), ('size_in_bytes', 0)):
            original = copy.deepcopy(self.transfer)
            self.transfer[field] = changed
            with self.subTest(field=field), self.assertRaisesRegex(publication.PublicationError, 'transfer_provider_changed'):
                self.admit()
            self.transfer = original
        self.transfer['workflow_run']['head_sha'] = self.packet['source_commit']
        with self.assertRaisesRegex(publication.PublicationError, 'transfer_provider_changed'):
            self.admit()
        self.transfer['workflow_run']['head_sha'] = SOURCE
        for field, changed in (('executor_source_commit', self.packet['source_commit']), ('executor_run_id', 12),
                               ('executor_run_attempt', 2), ('packet_sha256', '0' * 64)):
            original = dict(self.retrieval)
            self.retrieval[field] = changed
            self.write_transport()
            with self.subTest(field=field), self.assertRaisesRegex(publication.PublicationError, 'retrieval_changed'):
                self.admit()
            self.retrieval = original

    def test_wrong_executor_input_runtime_source_and_authority_are_rejected(self):
        for field, changed in (('GITHUB_SHA', 'a' * 40), ('GITHUB_REF', 'refs/heads/worker'),
                ('GITHUB_EVENT_NAME', 'push'), ('GITHUB_JOB', 'publish'), ('GITHUB_WORKFLOW_SHA', 'b' * 40),
                ('GITHUB_WORKFLOW_REF', 'wrong'), ('GITHUB_RUN_ID', '0'), ('GH_TOKEN', 'private'),
                ('ACTIONS_ID_TOKEN_REQUEST_TOKEN', 'private'), ('NPM_TOKEN', 'private')):
            with self.subTest(field=field), self.assertRaises(publication.PublicationError):
                custody.executor_identity(SOURCE, api=self, env=self.env | {field: changed})
        for head in ('', 'main', 'b' * 40):
            with self.subTest(head=head), self.assertRaisesRegex(publication.PublicationError, 'executor_head_changed'):
                custody.executor_identity(head, api=self, env=self.env)
        with patch.object(publication, 'REVIEWED_PUBLICATION_POLICY', {}):
            with self.assertRaisesRegex(publication.PublicationError, 'unexpected_publisher_authority'):
                self.admit()
        with patch.object(publication.pair, 'git', return_value='dirty'):
            with self.assertRaisesRegex(publication.PublicationError, 'executor_source_changed'):
                self.admit()
        self.runtime['head_sha'] = 'b' * 40
        with self.assertRaisesRegex(publication.PublicationError, 'executor_provider_changed'):
            self.admit()

    def test_retained_records_refuse_raw_provider_fields(self):
        path = self.inputs / 'artifact.json'
        value = json.loads(path.read_text()) | {'archive_download_url': 'https://signed.invalid/?secret=private'}
        path.write_text(json.dumps(value))
        with self.assertRaisesRegex(publication.PublicationError, 'unsafe_provider_record'):
            self.admit()

    def reader(self, *, archive=None, env=None):
        code = reader_code()
        # Exercise the actual checkout-free workflow body with the shared small
        # ZIP/provider fixture. Production pins are checked separately below.
        packet_literal = "b'''" + self.packet_bytes.decode() + "'''"
        start = code.index("b'''{")
        end = code.index("'''", start + 4) + 3
        code = code[:start] + packet_literal + code[end:]
        code = code.replace('0af4dd27274201e2cb6d13d8d4709304cd4e0da2c0603260d3e3355e4c3703f4', publication.digest(self.packet_bytes))
        code = code.replace('94760475', str(self.archive.stat().st_size))
        def gh(command, **kwargs):
            self.assertEqual(subprocess.DEVNULL, kwargs['stderr'])
            path = command[-1]
            if path.endswith('/zip'):
                kwargs['stdout'].write(self.archive.read_bytes() if archive is None else archive)
                return subprocess.CompletedProcess(command, 0)
            value = self.provider.artifact if '/artifacts/' in path else self.provider.run
            return subprocess.CompletedProcess(command, 0, json.dumps(value).encode())
        stdout = io.StringIO()
        with patch.dict(os.environ, self.env if env is None else env, clear=True), \
             patch.object(subprocess, 'run', side_effect=gh), redirect_stdout(stdout):
            try:
                exec(compile(code, 'checkout-free-reader', 'exec'), {})
                status = 0
            except SystemExit as error:
                status = error.code
        return status, stdout.getvalue()

    def test_reader_preserves_original_bytes_and_strips_provider_secrets(self):
        self.provider.artifact['archive_download_url'] = 'https://signed.invalid/?private-token'
        self.provider.run['logs_url'] = 'https://private.invalid/body'
        status, stdout = self.reader()
        self.assertEqual(0, status, stdout)
        transferred = self.root / 'studio-npm-original-input'
        self.assertEqual(custody.TRANSPORT_FILES, {path.name for path in transferred.iterdir()})
        self.assertEqual(self.archive.read_bytes(), (transferred / 'original.zip').read_bytes())
        self.assertEqual(self.packet_bytes, (transferred / 'packet.json').read_bytes())
        self.assertNotIn('private', ''.join(path.read_text() for path in transferred.glob('*.json')))

    def test_reader_provider_unavailable_and_wrong_source_fail_before_download(self):
        for field, changed in (('conclusion', 'failure'), ('run_attempt', 2), ('head_sha', SOURCE)):
            original = copy.deepcopy(self.provider.run)
            self.provider.run[field] = changed
            status, stdout = self.reader()
            self.assertEqual(1, status)
            self.assertEqual('original_producer_changed', json.loads(stdout)['failure_code'])
            self.assertFalse((self.root / 'studio-npm-original-input').exists())
            self.provider.run = original
        with patch.object(self.provider, 'artifact', None):
            status, stdout = self.reader()
        self.assertEqual(1, status)
        self.assertEqual('original_retrieval_unknown', json.loads(stdout)['failure_code'])

    def test_reader_wrong_executor_and_wrong_zip_fail_without_a_transfer_packet(self):
        status, stdout = self.reader(env=self.env | {'EXPECTED_HEAD': 'wrong'})
        self.assertEqual(1, status)
        self.assertEqual('executor_request_invalid', json.loads(stdout)['failure_code'])
        data = self.archive.read_bytes()
        status, stdout = self.reader(archive=bytes([data[0] ^ 1]) + data[1:])
        self.assertEqual(1, status)
        self.assertEqual('original_zip_changed', json.loads(stdout)['failure_code'])
        self.assertFalse((self.root / 'studio-npm-original-input' / 'packet.json').exists())

    def test_custody_cli_sanitizes_provider_failure_and_preserves_inputs(self):
        output = self.root / 'custody.json'
        arguments = ['custody', '--inputs', str(self.inputs), '--expected-head', SOURCE,
            '--transfer-id', '456', '--transfer-digest', 'd' * 64, '--output', str(output)]
        with patch.object(sys, 'argv', arguments), \
             patch.object(custody, 'admit_custody', side_effect=RuntimeError('private-token-or-signed-url')):
            self.assertEqual(1, custody.main())
        self.assertEqual({'success': False, 'publisher_authority': False,
                          'failure_code': 'custody_input_or_provider_unknown'}, json.loads(output.read_text()))
        self.assertEqual(self.archive.read_bytes(), (self.inputs / 'original.zip').read_bytes())

    def test_missing_transfer_id_retains_sanitized_failure_before_admission(self):
        output = self.root / 'missing-transfer.json'
        arguments = ['custody', '--inputs', str(self.inputs), '--expected-head', SOURCE,
            '--transfer-id', '', '--transfer-digest', '', '--output', str(output)]
        with patch.object(sys, 'argv', arguments), patch.object(custody, 'admit_custody') as admit:
            self.assertEqual(1, custody.main())
        admit.assert_not_called()
        self.assertEqual('transfer_identity_invalid', json.loads(output.read_text())['failure_code'])

    def test_report_write_failure_is_sanitized(self):
        output = self.root / 'report.json'
        arguments = ['custody', '--inputs', str(self.inputs), '--expected-head', SOURCE,
            '--transfer-id', '456', '--transfer-digest', 'd' * 64, '--output', str(output)]
        stdout = io.StringIO()
        with patch.object(sys, 'argv', arguments), redirect_stdout(stdout), \
             patch.object(custody, 'admit_custody', return_value={'success': True}), \
             patch.object(publication.pair, 'write_json', side_effect=OSError('private-local-path')):
            self.assertEqual(1, custody.main())
        self.assertEqual({'success': False, 'publisher_authority': False,
                          'failure_code': 'report_write_failed'}, json.loads(stdout.getvalue()))


class HostedAuthorityContracts(unittest.TestCase):
    def test_workflow_fixed_packet_bytes_match_reviewed_issue_identity(self):
        packet = reviewed_packet()
        self.assertEqual('0af4dd27274201e2cb6d13d8d4709304cd4e0da2c0603260d3e3355e4c3703f4', publication.digest(packet))
        value = json.loads(packet)
        self.assertEqual(9, len(value))
        self.assertEqual(11607565131, value['artifact_id'])
        self.assertEqual(37911724081, value['run_id'])
        self.assertEqual('0.0.0-proof.37911724081.1', value['version'])

    def test_only_manual_exact_head_and_separated_reader_verifier_authority(self):
        import yaml
        workflow = yaml.safe_load(WORKFLOW.read_text())
        trigger = workflow.get('on', workflow.get(True))
        self.assertEqual({'workflow_dispatch'}, set(trigger))
        self.assertEqual({'expected_head'}, set(trigger['workflow_dispatch']['inputs']))
        self.assertEqual({}, workflow['permissions'])
        self.assertEqual({'retrieve', 'verify'}, set(workflow['jobs']))
        reader, verifier = (workflow['jobs'][key] for key in ('retrieve', 'verify'))
        self.assertEqual({'actions': 'read'}, reader['permissions'])
        self.assertNotIn('if', reader)  # Invalid dispatch must fail, never skip green.
        self.assertEqual({'contents': 'read', 'actions': 'none'}, verifier['permissions'])
        self.assertFalse(any('checkout' in step.get('uses', '') for step in reader['steps']))
        self.assertEqual(1, sum('GH_TOKEN' in step.get('env', {}) for step in reader['steps']))
        checkout = next(step for step in verifier['steps'] if 'checkout' in step.get('uses', ''))
        self.assertFalse(checkout['with']['persist-credentials'])
        self.assertEqual('${{ github.sha }}', checkout['with']['ref'])
        for job in (reader, verifier):
            self.assertNotIn('environment', job)
        text = WORKFLOW.read_text()
        for forbidden in ('secrets.', 'id-token:', '--operation publish', 'npm publish', 'setup-node', 'setup-dotnet'):
            self.assertNotIn(forbidden, text)
        commands = [step.get('run', '') for step in verifier['steps'] if 'publish_studio_npm_pair.py' in step.get('run', '')]
        self.assertEqual(2, len(commands))
        self.assertTrue(any('--registry feedz' in command for command in commands))
        self.assertTrue(any('--registry npmjs' in command for command in commands))
        self.assertTrue(all('--operation' not in command for command in commands))


if __name__ == '__main__':
    unittest.main()
