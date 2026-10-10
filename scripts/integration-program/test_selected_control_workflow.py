"""Execute fixed six-cell retrieval/readback against offline fixtures only."""
from copy import deepcopy
from datetime import datetime, timedelta, timezone
import base64
import hashlib
import io
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import urllib.error
import urllib.request

import yaml
import selected_control_transport as transport

ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = ROOT / transport.WORKFLOW
ACTION = ROOT / '.github/actions/selected-product-control/action.yml'
CELLS = {f'{product}-{line}': (product, line, f'control_{product}_{line.replace(".", "_")}')
    for product in ('core', 'studio', 'extensions') for line in ('3.8', '3.9')}


def python_script(step, terminator='PY'):
    return step['run'].split("<<'" + terminator + "'\n", 1)[1].rsplit('\n' + terminator, 1)[0]


def encoded(value):
    return (json.dumps(value, sort_keys=True) + '\n').encode()


class Response(io.BytesIO):
    status = 200


class HostedWorkflowContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = yaml.safe_load(WORKFLOW.read_text())
        cls.action = yaml.safe_load(ACTION.read_text())
        cls.jobs = cls.workflow['jobs']
        cls.script = python_script(cls.jobs['retrieve']['steps'][0])
        cls.capacity = python_script(cls.jobs['readback']['steps'][1])
        cls.readback = python_script(cls.jobs['readback']['steps'][3])

    def test_exact_six_cells_and_token_role_boundaries(self):
        trigger = self.workflow.get('on', self.workflow.get(True))
        self.assertEqual(trigger, {'push': {'branches': [transport.HOSTED_REF.removeprefix('refs/heads/')]}})
        self.assertEqual(self.workflow['permissions'], {})
        control_ids = {value[2] for value in CELLS.values()}
        self.assertEqual(set(self.jobs), control_ids | {'retrieve', 'readback'})
        self.assertEqual(set(self.jobs['retrieve']['needs']), control_ids)
        self.assertEqual(set(self.jobs['readback']['needs']), control_ids | {'retrieve'})
        self.assertEqual(self.jobs['retrieve']['permissions'], {'contents': 'none', 'actions': 'read'})
        self.assertEqual(self.jobs['readback']['permissions'], {'contents': 'read', 'actions': 'none'})
        for cell, (product, line, job_id) in CELLS.items():
            job = self.jobs[job_id]
            self.assertEqual(job['name'], 'control-' + cell)
            self.assertEqual(job['permissions'], {'contents': 'read', 'actions': 'none'})
            self.assertNotIn('strategy', job)  # Explicit outputs, never ambiguous matrix outputs.
            self.assertEqual(job['steps'][1]['uses'], './.github/actions/selected-product-control')
            self.assertEqual(job['steps'][1]['with'], {'product': product, 'line': line,
                'version': '3.8.5' if line == '3.8' else '3.9.1'})
            for name in ('artifact-id', 'archive-digest', 'expected', 'expected-sha256'):
                self.assertEqual(job['outputs'][name], '${{ steps.cell.outputs.' + name + ' }}')
            for role in ('retrieve', 'readback'):
                step = self.jobs[role]['steps'][0 if role == 'retrieve' else 3]
                self.assertEqual(step['env'][job_id.upper()], '${{ toJSON(needs.' + job_id + '.outputs) }}')
        for job_id in control_ids | {'readback'}:
            job = self.jobs[job_id]
            self.assertNotIn('GH_TOKEN', json.dumps(job))
            self.assertNotIn('github.token', json.dumps(job))
            checkout = job['steps'][0]['with']
            self.assertEqual(checkout['ref'], '${{ github.sha }}')
            self.assertFalse(checkout['persist-credentials'])
            self.assertEqual(checkout['fetch-depth'], 0)
        for term in ('sys.path', 'scripts/integration-program', 'zipfile', 'subprocess'):
            self.assertNotIn(term, self.script)
        self.assertFalse(any('checkout' in step.get('uses', '') or 'download-artifact' in step.get('uses', '')
            for step in self.jobs['retrieve']['steps']))
        for job in self.jobs.values():
            for step in job['steps']:
                if step.get('uses', '').startswith('actions/'):
                    self.assertRegex(step['uses'], r'@[a-f0-9]{40}$')
        for step in self.action['runs']['steps']:
            if 'uses' in step: self.assertRegex(step['uses'], r'@[a-f0-9]{40}$')
            if 'run' in step: self.assertEqual(step['shell'], 'bash')

    def test_private_ordered_action_and_trigger_coverage(self):
        self.assertEqual(self.action['runs']['using'], 'composite')
        self.assertEqual(set(self.action['inputs']), {'product', 'line', 'version'})
        steps = self.action['runs']['steps']
        control = '\n'.join(step.get('run', '') for step in steps)
        self.assertIn('--product "$PRODUCT" --line "$LINE" --version "$VERSION"', control)
        self.assertLess(control.index('snapshot_product_planning_assets.py'), control.index('prove_product_release_artifacts.py'))
        self.assertLess(control.index('prove_product_release_consumers.py'), control.index('selected_control_seal.py seal'))
        self.assertIn('--retire-successful-cell-caches', control)
        self.assertIn('/Directory.Build.props', control)
        self.assertNotIn("plan['npm']", control)
        nodes = [step for step in steps if 'setup-node' in step.get('uses', '')]
        self.assertEqual(len(nodes), 1)
        self.assertEqual(nodes[0]['if'], "inputs.product == 'studio'")
        self.assertEqual(nodes[0]['with']['node-version'], '22')
        uploads = [step['with'] for step in steps if 'upload-artifact' in step.get('uses', '')]
        self.assertEqual(len(uploads), 1)
        self.assertEqual(uploads[0]['path'], '${{ runner.temp }}/selected-upload/')
        self.assertIn('${{ inputs.product }}-${{ inputs.line }}', uploads[0]['name'])
        planner = yaml.safe_load((ROOT / '.github/workflows/product-release-plan.yml').read_text())
        trigger = planner.get('on', planner.get(True))
        self.assertIn('.github/actions/selected-product-control/**', trigger['pull_request']['paths'])
        normal, optimized = planner['jobs']['contracts']['steps'][-1]['run'].splitlines()
        self.assertIn('test_selected_control_workflow', normal.split())
        self.assertIn('test_selected_control_workflow', optimized.split())

    def test_ineligible_plan_reports_only_closed_reason_categories_before_snapshot(self):
        steps = self.action['runs']['steps']
        diagnostic = next(step for step in steps if step.get('name') == 'Reject ineligible plan with safe policy categories')
        diagnostic_index = steps.index(diagnostic)
        snapshot_index = next(index for index, step in enumerate(steps)
                              if 'snapshot_product_planning_assets.py' in step.get('run', ''))
        producer_index = next(index for index, step in enumerate(steps)
                              if 'prove_product_release_artifacts.py' in step.get('run', ''))
        self.assertLess(diagnostic_index, snapshot_index)
        self.assertLess(diagnostic_index, producer_index)
        self.assertNotIn('continue-on-error', diagnostic)
        self.assertLess(diagnostic_index, next(index for index, step in enumerate(steps)
                          if step.get('name') == 'Cheap exact source commit availability preflight'))
        script = python_script(diagnostic, 'PY_DIAGNOSTIC')

        def execute(plan, *, create=True):
            with tempfile.TemporaryDirectory() as temporary:
                path = Path(temporary) / 'plan.json'
                if create:
                    path.write_text(json.dumps(plan))
                environment = {**os.environ, 'PLAN_PATH': str(path)}
                return subprocess.run([sys.executable, '-B', '-c', script, str(path)], env=environment,
                                      capture_output=True, text=True, check=False)

        private_values = ('private-package-id', '/private/plan/source.csproj',
                          'https://private.example/observation', 'Raw exception: /private/cache')
        known = execute({'eligible': False, 'reasons': [
            {'category': 'version_reused', 'id': private_values[0]},
            {'category': 'history_missing', 'id': private_values[1]},
            {'category': 'history_missing', 'id': private_values[2]}]})
        self.assertEqual(known.returncode, 1)
        self.assertEqual(json.loads(known.stderr), {
            'status': 'plan_ineligible', 'reason_categories': ['history_missing', 'version_reused']})
        self.assertTrue(all(value not in known.stdout + known.stderr for value in private_values))

        for plan, create in (({'eligible': False, 'reasons': [{'category': 'history_missing'},
                                {'category': 'unreviewed_reason; ' + private_values[3]}]}, True),
                             ({'eligible': False, 'reasons': [{'category': 'history_missing'}, 'malformed']}, True),
                             ({'eligible': False, 'reasons': 'malformed'}, True),
                             ({'eligible': False, 'reasons': []}, True),
                             (None, False)):
            with self.subTest(plan=plan, create=create):
                result = execute(plan, create=create)
                self.assertEqual(result.returncode, 1)
                self.assertEqual(json.loads(result.stderr), {
                    'status': 'plan_ineligible', 'reason_categories': ['unclassified']})
                self.assertTrue(all(value not in result.stdout + result.stderr for value in private_values))

        eligible = execute({'eligible': True, 'reasons': []})
        self.assertEqual(eligible.returncode, 0)
        self.assertEqual(eligible.stdout + eligible.stderr, '')

    def test_exact_transfer_and_readback_allowlists_and_capacity_order(self):
        transfer = self.jobs['retrieve']['steps'][1]['with']
        self.assertEqual(set(transfer['path'].splitlines()), {
            '${{ runner.temp }}/selected-transport/' + cell + '/' + name
            for cell in CELLS for name in ('original.zip', 'provider.json', 'expected.json')})
        self.assertEqual(transfer['retention-days'], 1)
        readback = self.jobs['readback']['steps']
        self.assertIn('disk_usage', readback[1]['run'])
        self.assertIn('download-artifact', readback[2]['uses'])
        self.assertEqual(readback[2]['with']['artifact-ids'], '${{ needs.retrieve.outputs.transport-id }}')
        self.assertNotIn('name', readback[2]['with'])
        self.assertEqual(set(readback[-1]['with']['path'].splitlines()), {
            '${{ runner.temp }}/selected-readback/' + cell + '/readback.json' for cell in CELLS})

    def fixture(self, root):
        now = datetime.now(timezone.utc)
        sha, repo, ref, workflow = 'a' * 40, transport.REPOSITORY, transport.HOSTED_REF, transport.WORKFLOW
        env = {'GITHUB_REPOSITORY': repo, 'GITHUB_REPOSITORY_ID': transport.REPOSITORY_ID,
            'GITHUB_EVENT_NAME': 'push', 'GITHUB_REF': ref, 'GITHUB_SHA': sha, 'GITHUB_WORKFLOW_SHA': sha,
            'GITHUB_WORKFLOW_REF': f'{repo}/{workflow}@{ref}', 'GITHUB_RUN_ID': '123', 'GITHUB_RUN_ATTEMPT': '2',
            'RUNNER_TEMP': str(root), 'GITHUB_OUTPUT': str(root / 'output'), 'GH_TOKEN': 'fixture-secret'}
        repository = {'id': int(transport.REPOSITORY_ID), 'full_name': repo}
        run = {'id': 123, 'run_attempt': 2, 'head_sha': sha, 'head_branch': ref.removeprefix('refs/heads/'),
            'event': 'push', 'path': workflow, 'repository': deepcopy(repository), 'head_repository': deepcopy(repository),
            'status': 'in_progress', 'conclusion': None}
        jobs, artifacts, archives, tuples = [], {}, {}, {}
        for index, (cell, (product, line, job_id)) in enumerate(CELLS.items()):
            context = {'repository': repo, 'repository_id': transport.REPOSITORY_ID, 'event': 'push', 'ref': ref,
                'head_sha': sha, 'workflow_sha': sha, 'workflow_ref': f'{repo}/{workflow}@{ref}', 'workflow_path': workflow,
                'run_id': '123', 'run_attempt': '2', 'job': job_id}
            expected = {'context': context, 'controller': {'commit': sha, 'tree': 'b' * 40},
                'product': product, 'line': line, 'manifest_sha256': hashlib.sha256(cell.encode()).hexdigest()}
            raw, archive = encoded(expected), ('original unparsed ZIP fixture ' + cell).encode()
            identifier = str(456 + index)
            tuples[cell] = {'artifact-id': identifier, 'archive-digest': hashlib.sha256(archive).hexdigest(),
                'expected': base64.b64encode(raw).decode(), 'expected-sha256': hashlib.sha256(raw).hexdigest()}
            jobs.append({'name': 'control-' + cell, 'status': 'completed', 'conclusion': 'success',
                'head_sha': sha, 'run_id': 123, 'run_attempt': 2})
            artifacts[identifier] = {'id': int(identifier), 'name': f'selected-control-{cell}-{sha}-123-2', 'expired': False,
                'digest': 'sha256:' + tuples[cell]['archive-digest'], 'size_in_bytes': len(archive),
                'workflow_run': {'id': 123, 'repository_id': repository['id'], 'head_repository_id': repository['id'],
                    'head_sha': sha, 'head_branch': run['head_branch']},
                'created_at': (now - timedelta(minutes=1)).isoformat(), 'expires_at': (now + timedelta(days=1)).isoformat()}
            archives[identifier] = archive
        return {'env': env, 'run': run, 'jobs': {'total_count': 6, 'jobs': list(reversed(jobs))},
            'artifacts': artifacts, 'archives': archives, 'tuples': tuples}

    def execute_retrieval(self, root, *, modify=None, location='https://fixture.blob.core.windows.net/original',
                          second_redirect=False, disk_free=2 ** 50):
        data = self.fixture(root)
        if modify: modify(data)
        for cell, (_, _, job_id) in CELLS.items():
            if cell in data['tuples']: data['env'][job_id.upper()] = json.dumps(data['tuples'][cell])
        self.calls = []
        owner = self
        class Opener:
            def __init__(self, authenticated): self.authenticated = authenticated
            def open(self, request, **kwargs):
                owner.calls.append((self.authenticated, request))
                if self.authenticated:
                    if request.full_url.endswith('/zip'):
                        identifier = request.full_url.split('/')[-2]
                        raise urllib.error.HTTPError(request.full_url, 302, 'redirect', {'Location': location + '?id=' + identifier}, None)
                    result = data['jobs'] if '/jobs?' in request.full_url else (
                        data['artifacts'][request.full_url.rsplit('/', 1)[-1]] if '/artifacts/' in request.full_url else data['run'])
                    return Response(encoded(result))
                if second_redirect:
                    raise urllib.error.HTTPError(request.full_url, 302, 'redirect', {'Location': 'https://evil.invalid'}, None)
                return Response(data['archives'][request.full_url.rsplit('=', 1)[-1]])
        openers = iter((Opener(True), Opener(False)))
        def opener_factory(*handlers):
            self.assertEqual(len(handlers), 1)
            self.assertIsNone(handlers[0].redirect_request(None, None, 302, '', {}, location))
            return next(openers)
        with patch.dict(os.environ, data['env'], clear=True), patch.object(urllib.request, 'build_opener', side_effect=opener_factory), \
                patch.object(shutil, 'disk_usage', return_value=SimpleNamespace(free=disk_free)), patch('sys.stderr', new_callable=io.StringIO):
            exec(compile(self.script, str(WORKFLOW), 'exec'), {})
        return data

    def test_six_original_downloads_preserve_bytes_and_credentials(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve(); data = self.execute_retrieval(root)
            self.assertEqual({p.name for p in (root/'selected-transport').iterdir()}, set(CELLS))
            self.assertEqual(len(self.calls), 20)
            self.assertTrue(all(not request.full_url.endswith('/zip') for _, request in self.calls[:8]))
            for authenticated, request in self.calls:
                self.assertEqual(request.get_method(), 'GET')
                self.assertEqual(request.get_header('Authorization'), 'Bearer fixture-secret' if authenticated else None)
                if authenticated: self.assertTrue(request.full_url.startswith('https://api.github.com/repos/elsa-workflows/elsa-core/actions/'))
                else: self.assertNotIn('fixture-secret', repr(request.headers))
            for cell, values in data['tuples'].items():
                folder = root/'selected-transport'/cell
                self.assertEqual({p.name for p in folder.iterdir()}, {'original.zip', 'provider.json', 'expected.json'})
                self.assertEqual((folder/'original.zip').read_bytes(), data['archives'][values['artifact-id']])
                provider, expected = (json.loads((folder/name).read_bytes()) for name in ('provider.json', 'expected.json'))
                transport.validate_provider(provider, expected, now=datetime.now(timezone.utc))
                self.assertNotIn('fixture-secret', json.dumps(provider)); self.assertNotIn('?id=', json.dumps(provider))
            output = (root/'output').read_text()
            self.assertNotIn('fixture-secret', output)
            self.assertEqual(set(json.loads(output.splitlines()[0].split('=', 1)[1])), set(CELLS))

    def test_all_metadata_and_aggregate_capacity_fail_before_zip_download(self):
        def bad_last(data): data['artifacts']['461']['digest'] = 'sha256:' + 'f' * 64
        for modify, capacity in ((bad_last, 2 ** 50), (None, 1)):
            with self.subTest(capacity=capacity), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary).resolve()
                with self.assertRaises(SystemExit): self.execute_retrieval(root, modify=modify, disk_free=capacity)
                self.assertFalse(any(request.full_url.endswith('/zip') for _, request in self.calls))
                self.assertFalse((root/'selected-transport').exists())
                self.assertFalse((root/'output').exists())

    def test_capacity_uses_all_six_sizes_and_allows_exact_sufficient_disk(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve(); data = self.fixture(root)
            needed = 3 * sum(a['size_in_bytes'] for a in data['artifacts'].values()) + 6 * 64 * 1024**2 + 18 * 4096
            with self.assertRaises(SystemExit): self.execute_retrieval(root, disk_free=needed-1)
            self.assertFalse(any(request.full_url.endswith('/zip') for _, request in self.calls))
            self.execute_retrieval(root, disk_free=needed)
            outputs = dict(line.split('=', 1) for line in (root/'output').read_text().splitlines())
            self.assertEqual(int(outputs['required-bytes']), needed)

    def test_bad_provider_metadata_and_redirects_fail_closed(self):
        changes = [lambda d: d['run'].update(run_attempt=3), lambda d: d['run'].update(event='pull_request'),
            lambda d: d['run']['head_repository'].update(id=1), lambda d: d['run'].update(head_sha='f'*40),
            lambda d: d['run'].update(path='foreign.yml'), lambda d: d['jobs']['jobs'][0].update(conclusion='failure'),
            lambda d: d['jobs'].update(total_count=7), lambda d: d['jobs']['jobs'].append(deepcopy(d['jobs']['jobs'][0])),
            lambda d: d['artifacts']['456'].update(id=457), lambda d: d['artifacts']['456'].update(expired=True),
            lambda d: d['artifacts']['456'].update(digest='sha256:'+'f'*64), lambda d: d['artifacts']['456'].update(size_in_bytes=True),
            lambda d: d['artifacts']['456'].update(size_in_bytes=2*1024**3+1),
            lambda d: d['artifacts']['456'].update(created_at=(datetime.now(timezone.utc)+timedelta(days=1)).isoformat()),
            lambda d: d['artifacts']['456'].update(expires_at=(datetime.now(timezone.utc)-timedelta(days=1)).isoformat()),
            lambda d: d['artifacts']['456'].update(name=d['artifacts']['457']['name'])]
        for change in changes:
            with self.subTest(change=change), tempfile.TemporaryDirectory() as temporary:
                with self.assertRaises(SystemExit): self.execute_retrieval(Path(temporary).resolve(), modify=change)
                self.assertFalse(any(request.full_url.endswith('/zip') for _, request in self.calls))
        for location in ('http://fixture.blob.core.windows.net/payload', 'https://user:secret@fixture.blob.core.windows.net/payload',
                         'https://evil.invalid/payload', 'https://fixture.blob.core.windows.net:8080/payload'):
            with self.subTest(location=location), tempfile.TemporaryDirectory() as temporary:
                with self.assertRaises(SystemExit): self.execute_retrieval(Path(temporary).resolve(), location=location)
        with tempfile.TemporaryDirectory() as temporary:
            with self.assertRaises(SystemExit): self.execute_retrieval(Path(temporary).resolve(), second_redirect=True)

    def test_changed_last_download_never_emits_complete_batch_outputs(self):
        def changed(data): data['archives']['461'] = b'changed last original archive'
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve()
            with self.assertRaises(SystemExit): self.execute_retrieval(root, modify=changed)
            self.assertFalse((root/'output').exists())
            self.assertFalse((root/'selected-transport/extensions-3.9/provider.json').exists())

    def test_cross_cell_and_rehashed_expected_tuple_negatives(self):
        def expected_change(data, change):
            values = data['tuples']['core-3.8']; expected = json.loads(base64.b64decode(values['expected']))
            change(expected); raw = encoded(expected)
            values.update(expected=base64.b64encode(raw).decode(), **{'expected-sha256': hashlib.sha256(raw).hexdigest()})
        changes = [lambda d: d['tuples'].pop('core-3.8'), lambda d: d['tuples']['core-3.8'].update(extra='private'),
            lambda d: d['tuples']['core-3.8'].update(d['tuples']['studio-3.8']),
            lambda d: d['tuples']['core-3.8'].update(**{'artifact-id': d['tuples']['core-3.9']['artifact-id']}),
            lambda d: expected_change(d, lambda x: x.update(product='studio')),
            lambda d: expected_change(d, lambda x: x.update(line='3.9')),
            lambda d: expected_change(d, lambda x: x['context'].update(job='control_studio_3_8')),
            lambda d: expected_change(d, lambda x: x['context'].update(run_attempt='3')),
            lambda d: expected_change(d, lambda x: x['controller'].update(commit='f'*40))]
        for change in changes:
            with self.subTest(change=change), tempfile.TemporaryDirectory() as temporary:
                with self.assertRaises(SystemExit): self.execute_retrieval(Path(temporary).resolve(), modify=change)
                self.assertFalse(any(request.full_url.endswith('/zip') for _, request in self.calls))

    def readback_fixture(self, root):
        data = self.execute_retrieval(root)
        shutil.move(root/'selected-transport', root/'selected-readback-input')
        values = dict(line.split('=', 1) for line in (root/'output').read_text().splitlines())
        env = {key: value for key, value in data['env'].items() if key != 'GH_TOKEN'}
        env.update(PROVIDER_HASHES=values['provider-hashes'])
        for cell, (_, _, job) in CELLS.items(): env[job.upper()] = json.dumps(data['tuples'][cell])
        return data, env

    def execute_readback(self, env):
        with patch.dict(os.environ, env, clear=True), patch.object(subprocess, 'run') as run, patch('sys.stderr', new_callable=io.StringIO):
            try: exec(compile(self.readback, str(WORKFLOW), 'exec'), {})
            finally: self.readback_calls = run.call_args_list
        return self.readback_calls

    def test_readback_hashes_all_cells_before_fixed_cli_invocations(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve(); data, env = self.readback_fixture(root)
            calls = self.execute_readback(env)
            self.assertEqual(len(calls), 6)
            for (cell, values), call in zip(data['tuples'].items(), calls):
                args = call.args[0]
                self.assertEqual(args[1:4], ['-B', 'scripts/integration-program/selected_control_seal.py', 'readback'])
                self.assertEqual(args[args.index('--archive-sha256')+1], values['archive-digest'])
                self.assertEqual(args[-1], str(root/'selected-readback'/cell))
                self.assertEqual(call.kwargs, {'check': True})

    def test_all_six_rebound_real_zip_semantics_use_their_distinct_control_job(self):
        import selected_control_seal as seal
        import selected_studio_payload as payload
        from test_selected_control_transport import zipped
        from test_selected_product_payload import fixture
        contracts = payload.load_contracts()
        def bind_job(value, job):
            if isinstance(value, dict):
                if 'job' in value: value['job'] = job
                for item in value.values(): bind_job(item, job)
            elif isinstance(value, list):
                for item in value: bind_job(item, job)
        def genuine_synthetic_zip(data):
            for cell, (product, line, job) in CELLS.items():
                files, expected, now = fixture(product, line)
                for name in ('producer/receipt.json', 'consumer/receipt.json', 'producer/npm/receipt.json'):
                    if name in files:
                        value = json.loads(files[name]); bind_job(value, job); files[name] = encoded(value)
                result = json.loads(files['consumer/receipt.json'])
                receipt_hash = hashlib.sha256(files['producer/receipt.json']).hexdigest()
                result['artifact_receipt_sha256'] = result['producer_plan_admission']['artifact_receipt_sha256'] = receipt_hash
                files['consumer/receipt.json'] = encoded(result); bind_job(expected, job)
                hashes = tuple(hashlib.sha256(files[name]).hexdigest() for name in
                    ('plan.json', 'producer/receipt.json', 'consumer/receipt.json'))
                sealed = seal.stage(files, expected, hashes, contracts=contracts, now=now)
                expected['manifest_sha256'] = hashlib.sha256(sealed[transport.MANIFEST]).hexdigest()
                raw, archive = encoded(expected), zipped(list(sealed.items()))
                values = data['tuples'][cell]; identifier = values['artifact-id']
                values.update(expected=base64.b64encode(raw).decode(), **{
                    'expected-sha256': hashlib.sha256(raw).hexdigest(), 'archive-digest': hashlib.sha256(archive).hexdigest()})
                data['artifacts'][identifier].update(digest='sha256:'+values['archive-digest'], size_in_bytes=len(archive))
                data['archives'][identifier] = archive
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve(); data = self.execute_retrieval(root, modify=genuine_synthetic_zip)
            shutil.move(root/'selected-transport', root/'selected-readback-input')
            hashes = dict(line.split('=', 1) for line in (root/'output').read_text().splitlines())
            env = {key: value for key, value in data['env'].items() if key != 'GH_TOKEN'}
            env['PROVIDER_HASHES'] = hashes['provider-hashes']
            for cell, (_, _, job) in CELLS.items(): env[job.upper()] = json.dumps(data['tuples'][cell])
            results = []
            def verify(args, *, check):
                self.assertTrue(check)
                path = Path(args[args.index('--original-zip')+1]); folder = path.parent
                provider, expected = (json.loads((folder/name).read_bytes()) for name in ('provider.json', 'expected.json'))
                results.append(seal.readback(path.read_bytes(), provider, expected, contracts=contracts))
            with patch.dict(os.environ, env, clear=True), patch.object(subprocess, 'run', side_effect=verify):
                exec(compile(self.readback, str(WORKFLOW), 'exec'), {})
            self.assertEqual({r['product']+'-'+r['line'] for r in results}, set(CELLS))
            self.assertTrue(all(r['success'] and not r['product_code_executed_during_readback'] and
                r['context']['job'] == CELLS[r['product']+'-'+r['line']][2] for r in results))

    def test_readback_missing_extra_changed_symlink_and_mapping_fail_before_any_cli(self):
        for kind in ('missing', 'extra', 'directory', 'case', 'symlink', 'bytes', 'providers', 'swap'):
            with self.subTest(kind=kind), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary).resolve(); _, env = self.readback_fixture(root)
                folder = root/'selected-readback-input'/'extensions-3.9'
                if kind == 'missing': (folder/'expected.json').unlink()
                elif kind == 'extra': (folder/'raw.log').write_text('private')
                elif kind == 'directory': (folder/'empty').mkdir()
                elif kind == 'case': (folder/'expected.json').rename(folder/'Expected.json')
                elif kind == 'symlink':
                    (folder/'expected.json').unlink(); (folder/'expected.json').symlink_to(root/'output')
                elif kind == 'bytes': (folder/'original.zip').write_bytes(b'changed')
                elif kind == 'providers':
                    values = json.loads(env['PROVIDER_HASHES']); values['extra'] = 'f'*64; env['PROVIDER_HASHES'] = json.dumps(values)
                else:
                    env['CONTROL_CORE_3_8'], env['CONTROL_STUDIO_3_8'] = env['CONTROL_STUDIO_3_8'], env['CONTROL_CORE_3_8']
                with self.assertRaises(SystemExit): self.execute_readback(env)
                self.assertFalse(self.readback_calls)

    def test_readback_capacity_precedes_download_and_one_cli_failure_stops_batch(self):
        for value, free in (('invalid', 2**50), ('100', 99)):
            with self.subTest(value=value), patch.dict(os.environ, {'REQUIRED_BYTES': value, 'RUNNER_TEMP': '/fixture'}, clear=True), \
                    patch.object(shutil, 'disk_usage', return_value=SimpleNamespace(free=free)), self.assertRaises(SystemExit):
                exec(compile(self.capacity, str(WORKFLOW), 'exec'), {})
        with tempfile.TemporaryDirectory() as temporary:
            _, env = self.readback_fixture(Path(temporary).resolve())
            with patch.dict(os.environ, env, clear=True), patch.object(subprocess, 'run', side_effect=subprocess.CalledProcessError(1, 'fixed-cli')) as run, \
                    patch('sys.stderr', new_callable=io.StringIO), self.assertRaises(SystemExit):
                exec(compile(self.readback, str(WORKFLOW), 'exec'), {})
            self.assertEqual(run.call_count, 1)

    def test_source_availability_uses_fixed_common_file_with_exact_tree_and_bytes(self):
        step = next(s for s in self.action['runs']['steps'] if s.get('name') == 'Cheap exact source commit availability preflight')
        script = python_script(step, 'PY_SOURCE')
        for correct in (True, False):
            with self.subTest(correct=correct), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary).resolve(); (root/'selected-plan').mkdir()
                (root/'selected-plan/plan.json').write_bytes(encoded({'source': {'commit': 'a'*40, 'tree': 'b'*40}, 'npm': None}))
                with patch.dict(os.environ, {'RUNNER_TEMP': str(root)}, clear=True), \
                        patch.object(subprocess, 'check_output', side_effect=['b'*40+'\n', b'original props']), \
                        patch.object(urllib.request, 'urlopen', return_value=Response(b'original props' if correct else b'changed')) as get, \
                        patch('sys.stderr', new_callable=io.StringIO):
                    if correct: exec(compile(script, str(ACTION), 'exec'), {})
                    else:
                        with self.assertRaises(SystemExit): exec(compile(script, str(ACTION), 'exec'), {})
                self.assertEqual(get.call_args.args[0], 'https://raw.githubusercontent.com/elsa-workflows/elsa-core/'+'a'*40+'/Directory.Build.props')


if __name__ == '__main__': unittest.main()
