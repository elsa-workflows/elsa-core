"""Execute fixed retrieval against offline HTTP fixtures; no real GETs."""
from copy import deepcopy
from datetime import datetime, timedelta, timezone
import base64
import hashlib
import io
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import urllib.error
import urllib.request

import yaml
import selected_control_transport as transport

WORKFLOW = Path(__file__).resolve().parents[2] / transport.WORKFLOW


class Response(io.BytesIO):
    status = 200


class HostedWorkflowContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = yaml.safe_load(WORKFLOW.read_text())
        cls.jobs = cls.workflow['jobs']
        cls.retrieve = cls.jobs['retrieve']['steps'][0]['run']
        cls.script = cls.retrieve.split("<<'PY'\n", 1)[1].rsplit('\nPY', 1)[0]

    def test_trusted_single_cell_and_token_role_boundaries(self):
        trigger = self.workflow.get('on', self.workflow.get(True))
        self.assertEqual(trigger, {'push': {'branches': [transport.HOSTED_REF.removeprefix('refs/heads/')]}})
        self.assertEqual(self.workflow['permissions'], {})
        self.assertEqual(self.jobs['control']['permissions'], {'contents': 'read', 'actions': 'none'})
        self.assertEqual(self.jobs['retrieve']['permissions'], {'contents': 'none', 'actions': 'read'})
        self.assertEqual(self.jobs['readback']['permissions'], {'contents': 'read', 'actions': 'none'})
        uses = [step.get('uses', '') for step in self.jobs['retrieve']['steps']]
        self.assertFalse(any('checkout' in value or 'download-artifact' in value for value in uses))
        self.assertNotIn('sys.path', self.script)
        self.assertNotIn('scripts/integration-program', self.script)
        self.assertNotIn('zipfile', self.script)
        self.assertNotIn('subprocess', self.script)
        for job in ('control', 'readback'):
            self.assertNotIn('GH_TOKEN', json.dumps(self.jobs[job]))
            self.assertNotIn('github.token', json.dumps(self.jobs[job]))
        control = '\n'.join(step.get('run', '') for step in self.jobs['control']['steps'])
        self.assertIn('--product studio --line 3.8 --version 3.8.5', control)
        self.assertLess(control.index('snapshot_product_planning_assets.py'), control.index('prove_product_release_artifacts.py'))
        self.assertLess(control.index('prove_product_release_consumers.py'), control.index('selected_control_seal.py seal'))
        uploads = [step['with']['path'] for step in self.jobs['control']['steps'] if 'upload-artifact' in step.get('uses', '')]
        self.assertEqual(uploads, ['${{ runner.temp }}/selected-upload/'])
        checkout = self.jobs['readback']['steps'][0]['with']
        self.assertEqual(checkout['ref'], '${{ github.sha }}')
        self.assertFalse(checkout['persist-credentials'])
        self.assertIn('artifact-ids', self.jobs['readback']['steps'][1]['with'])
        self.assertNotIn('name', self.jobs['readback']['steps'][1]['with'])
        for job in self.jobs.values():
            for step in job['steps']:
                if 'uses' in step: self.assertRegex(step['uses'], r'@[a-f0-9]{40}$')

    def fixture(self, root):
        now = datetime.now(timezone.utc)
        sha, repo, ref, workflow = 'a' * 40, transport.REPOSITORY, transport.HOSTED_REF, transport.WORKFLOW
        context = {'repository': repo, 'repository_id': transport.REPOSITORY_ID, 'event': 'push', 'ref': ref,
            'head_sha': sha, 'workflow_sha': sha, 'workflow_ref': f'{repo}/{workflow}@{ref}', 'workflow_path': workflow,
            'run_id': '123', 'run_attempt': '2', 'job': 'control'}
        expected = {'context': context, 'controller': {'commit': sha, 'tree': 'b' * 40},
            'product': 'studio', 'line': '3.8', 'manifest_sha256': 'c' * 64}
        raw, archive = json.dumps(expected).encode(), b'exact original fixture ZIP bytes, never parsed in retrieve'
        environment = {'GITHUB_REPOSITORY': repo, 'GITHUB_REPOSITORY_ID': transport.REPOSITORY_ID,
            'GITHUB_EVENT_NAME': 'push', 'GITHUB_REF': ref, 'GITHUB_SHA': sha, 'GITHUB_WORKFLOW_SHA': sha,
            'GITHUB_WORKFLOW_REF': f'{repo}/{workflow}@{ref}', 'GITHUB_RUN_ID': '123', 'GITHUB_RUN_ATTEMPT': '2',
            'ARTIFACT_ID': '456', 'ARCHIVE_DIGEST': hashlib.sha256(archive).hexdigest(),
            'EXPECTED_B64': base64.b64encode(raw).decode(), 'EXPECTED_SHA256': hashlib.sha256(raw).hexdigest(),
            'RUNNER_TEMP': str(root), 'GITHUB_OUTPUT': str(root / 'output'), 'GH_TOKEN': 'fixture-secret'}
        repository = {'id': int(transport.REPOSITORY_ID), 'full_name': repo}
        run = {'id': 123, 'run_attempt': 2, 'head_sha': sha, 'head_branch': ref.removeprefix('refs/heads/'),
            'event': 'push', 'path': workflow, 'repository': repository, 'head_repository': repository,
            'status': 'in_progress', 'conclusion': None}
        jobs = {'total_count': 1, 'jobs': [{'name': 'control', 'status': 'completed', 'conclusion': 'success',
            'head_sha': sha, 'run_id': 123, 'run_attempt': 2}]}
        artifact = {'id': 456, 'name': f'selected-control-studio-3.8-{sha}-123-2', 'expired': False,
            'digest': 'sha256:' + environment['ARCHIVE_DIGEST'], 'size_in_bytes': len(archive),
            'workflow_run': {'id': 123, 'repository_id': repository['id'], 'head_repository_id': repository['id'],
                'head_sha': sha, 'head_branch': run['head_branch']},
            'created_at': (now - timedelta(minutes=1)).isoformat(), 'expires_at': (now + timedelta(days=1)).isoformat()}
        return environment, run, jobs, artifact, archive

    def execute_retrieval(self, root, *, modify=None, location='https://fixture.blob.core.windows.net/original?signature=private', second_redirect=False):
        environment, run, jobs, artifact, archive = self.fixture(root)
        if modify: modify(run, jobs, artifact)
        calls = []
        class Opener:
            def __init__(self, authenticated): self.authenticated = authenticated
            def open(self, request, **kwargs):
                calls.append((self.authenticated, request))
                if self.authenticated:
                    if request.full_url.endswith('/zip'):
                        raise urllib.error.HTTPError(request.full_url, 302, 'redirect', {'Location': location}, None)
                    data = jobs if '/jobs?' in request.full_url else artifact if '/artifacts/' in request.full_url else run
                    return Response(json.dumps(data).encode())
                if second_redirect:
                    raise urllib.error.HTTPError(request.full_url, 302, 'redirect', {'Location': 'https://evil.invalid'}, None)
                return Response(archive)
        openers = iter((Opener(True), Opener(False)))
        def opener_factory(*handlers):
            self.assertEqual(len(handlers), 1)
            self.assertIsNone(handlers[0].redirect_request(None, None, 302, '', {}, location))
            return next(openers)
        with patch.dict(os.environ, environment, clear=True), patch.object(urllib.request, 'build_opener', side_effect=opener_factory), patch('sys.stderr', new_callable=io.StringIO):
            exec(compile(self.script, str(WORKFLOW), 'exec'), {})
        return calls, archive, environment

    def test_fixed_gets_download_original_without_authorization_forwarding(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve()
            calls, archive, environment = self.execute_retrieval(root)
            self.assertEqual({path.name for path in (root / 'selected-transport').iterdir()}, {'original.zip', 'provider.json', 'expected.json'})
            self.assertEqual((root / 'selected-transport/original.zip').read_bytes(), archive)
            self.assertEqual(len(calls), 5)
            for authenticated, request in calls:
                self.assertEqual(request.get_method(), 'GET')
                if authenticated:
                    self.assertEqual(request.get_header('Authorization'), 'Bearer fixture-secret')
                    self.assertTrue(request.full_url.startswith('https://api.github.com/repos/elsa-workflows/elsa-core/actions/'))
                else:
                    self.assertIsNone(request.get_header('Authorization'))
                    self.assertNotIn('fixture-secret', repr(request.headers))
            provider = json.loads((root / 'selected-transport/provider.json').read_bytes())
            expected = json.loads((root / 'selected-transport/expected.json').read_bytes())
            transport.validate_provider(provider, expected, now=datetime.now(timezone.utc))
            self.assertNotIn('fixture-secret', (root / 'output').read_text())
            self.assertNotIn('signature', json.dumps(provider))

    def test_bad_provider_metadata_and_redirects_fail_closed(self):
        changes = [lambda run, jobs, artifact: run.update(run_attempt=3),
            lambda run, jobs, artifact: run.update(event='pull_request'),
            lambda run, jobs, artifact: run['head_repository'].update(id=1),
            lambda run, jobs, artifact: jobs['jobs'][0].update(conclusion='failure'),
            lambda run, jobs, artifact: artifact.update(id=457),
            lambda run, jobs, artifact: artifact.update(expired=True),
            lambda run, jobs, artifact: artifact.update(digest='sha256:' + 'f' * 64),
            lambda run, jobs, artifact: artifact.update(size_in_bytes=1)]
        for change in changes:
            with self.subTest(change=change), tempfile.TemporaryDirectory() as temporary:
                with self.assertRaises(SystemExit): self.execute_retrieval(Path(temporary).resolve(), modify=change)
        for location in ('http://fixture.blob.core.windows.net/payload', 'https://user:secret@fixture.blob.core.windows.net/payload',
                         'https://evil.invalid/payload', 'https://fixture.blob.core.windows.net:8080/payload'):
            with self.subTest(location=location), tempfile.TemporaryDirectory() as temporary:
                with self.assertRaises(SystemExit): self.execute_retrieval(Path(temporary).resolve(), location=location)
        with tempfile.TemporaryDirectory() as temporary:
            with self.assertRaises(SystemExit): self.execute_retrieval(Path(temporary).resolve(), second_redirect=True)


if __name__ == '__main__': unittest.main()
