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
    """Extract an embedded Python heredoc from a workflow step."""
    return step['run'].split("<<'" + terminator + "'\n", 1)[1].rsplit('\n' + terminator, 1)[0]


def encoded(value):
    """Encode synthetic workflow JSON with sorted keys and a trailing newline."""
    return (json.dumps(value, sort_keys=True) + '\n').encode()


class Response(io.BytesIO):
    status = 200


class HostedWorkflowContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        """Load the hosted workflow and action, then extract scripts for offline execution."""
        cls.workflow = yaml.safe_load(WORKFLOW.read_text())
        cls.action = yaml.safe_load(ACTION.read_text())
        cls.jobs = cls.workflow['jobs']
        cls.script = python_script(cls.jobs['retrieve']['steps'][0])
        cls.capacity = python_script(cls.jobs['readback']['steps'][1])
        cls.readback = python_script(cls.jobs['readback']['steps'][3])

    def test_exact_six_cells_and_token_role_boundaries(self):
        """Verify exact six cells and token role boundaries."""
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
        """Verify private ordered action and trigger coverage."""
        self.assertEqual(self.action['runs']['using'], 'composite')
        plan_step = next(step for step in self.action['runs']['steps'] if step.get('name') == 'Fresh selected plan')
        self.assertEqual('${{ runner.temp }}/selected-planning-cache', plan_step['env']['NUGET_PACKAGES'])
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
        uploads = [step['with'] for step in steps if 'upload-artifact' in step.get('uses', '') and step.get('id') == 'upload']
        self.assertEqual(len(uploads), 1)
        self.assertEqual(uploads[0]['path'], '${{ runner.temp }}/selected-upload/')
        self.assertIn('${{ inputs.product }}-${{ inputs.line }}', uploads[0]['name'])
        diagnostic = next(step for step in steps if step.get('id') == 'failure-diagnostics')
        self.assertEqual('failure()', diagnostic['if'])
        failed_upload = next(step for step in steps if step.get('with', {}).get('name', '').startswith('selected-failure-diagnostics-'))
        self.assertEqual("failure() && steps.failure-diagnostics.outcome == 'success'", failed_upload['if'])
        self.assertEqual('${{ runner.temp }}/selected-failure-diagnostics/', failed_upload['with']['path'])
        self.assertEqual(2, len([step for step in steps if 'upload-artifact' in step.get('uses', '')]))
        self.assertEqual('${{ steps.upload.outputs.artifact-id }}', self.action['outputs']['artifact-id']['value'])
        planner = yaml.safe_load((ROOT / '.github/workflows/product-release-plan.yml').read_text())
        trigger = planner.get('on', planner.get(True))
        self.assertIn('.github/actions/selected-product-control/**', trigger['pull_request']['paths'])
        normal, optimized = planner['jobs']['contracts']['steps'][-1]['run'].splitlines()
        self.assertIn('test_selected_control_workflow', normal.split())
        self.assertIn('test_selected_control_workflow', optimized.split())
        for command in (normal, optimized):
            self.assertEqual(1, command.split().count('test_selected_core_manifest_hints'))

    def test_selector_full_suite_installs_exact_native_sdk_before_both_modes(self):
        import product_release_metadata as metadata
        workflow = yaml.safe_load((ROOT / '.github/workflows/package-impact-closure.yml').read_text())
        steps = workflow['jobs']['selector-and-plan']['steps']
        sdk = next(step for step in steps if step.get('uses', '').startswith('actions/setup-dotnet@'))
        suite = next(step for step in steps if step.get('name') == 'Test selector and closure classification')
        self.assertEqual(metadata.SDK, sdk['with']['dotnet-version'])
        self.assertRegex(sdk['uses'], r'@[a-f0-9]{40}$')
        self.assertLess(steps.index(sdk), steps.index(suite))
        self.assertNotIn('if', sdk)
        self.assertNotIn('continue-on-error', suite)
        self.assertEqual(["python -m unittest discover -s scripts/integration-program -p 'test_*.py'",
            "python -O -m unittest discover -s scripts/integration-program -p 'test_*.py'"], suite['run'].splitlines())

    def test_ineligible_plan_reports_only_closed_reason_categories_before_snapshot(self):
        """Verify ineligible plan reports only closed reason categories before snapshot."""
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
        """Verify exact transfer and readback allowlists and capacity order."""
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
        """Create synthetic six-cell job, artifact, archive, and environment data for retrieval."""
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
        """Run the workflow retrieval script against mocked provider responses and disk capacity."""
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
        """Verify six original downloads preserve bytes and credentials."""
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
        """Verify all metadata and aggregate capacity fail before ZIP download."""
        def bad_last(data): data['artifacts']['461']['digest'] = 'sha256:' + 'f' * 64
        for modify, capacity in ((bad_last, 2 ** 50), (None, 1)):
            with self.subTest(capacity=capacity), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary).resolve()
                with self.assertRaises(SystemExit): self.execute_retrieval(root, modify=modify, disk_free=capacity)
                self.assertFalse(any(request.full_url.endswith('/zip') for _, request in self.calls))
                self.assertFalse((root/'selected-transport').exists())
                self.assertFalse((root/'output').exists())

    def test_capacity_uses_all_six_sizes_and_allows_exact_sufficient_disk(self):
        """Verify capacity uses all six sizes and allows exact sufficient disk."""
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve(); data = self.fixture(root)
            needed = 3 * sum(a['size_in_bytes'] for a in data['artifacts'].values()) + 6 * 64 * 1024**2 + 18 * 4096
            with self.assertRaises(SystemExit): self.execute_retrieval(root, disk_free=needed-1)
            self.assertFalse(any(request.full_url.endswith('/zip') for _, request in self.calls))
            self.execute_retrieval(root, disk_free=needed)
            outputs = dict(line.split('=', 1) for line in (root/'output').read_text().splitlines())
            self.assertEqual(int(outputs['required-bytes']), needed)

    def test_bad_provider_metadata_and_redirects_fail_closed(self):
        """Verify bad provider metadata and redirects fail closed."""
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
        """Verify changed last download never emits complete batch outputs."""
        def changed(data): data['archives']['461'] = b'changed last original archive'
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve()
            with self.assertRaises(SystemExit): self.execute_retrieval(root, modify=changed)
            self.assertFalse((root/'output').exists())
            self.assertFalse((root/'selected-transport/extensions-3.9/provider.json').exists())

    def test_cross_cell_and_rehashed_expected_tuple_negatives(self):
        """Verify cross cell and rehashed expected tuple negatives."""
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
        """Stage retrieved fixture archives as inputs to the independent readback job."""
        data = self.execute_retrieval(root)
        shutil.move(root/'selected-transport', root/'selected-readback-input')
        values = dict(line.split('=', 1) for line in (root/'output').read_text().splitlines())
        env = {key: value for key, value in data['env'].items() if key != 'GH_TOKEN'}
        env.update(PROVIDER_HASHES=values['provider-hashes'])
        for cell, (_, _, job) in CELLS.items(): env[job.upper()] = json.dumps(data['tuples'][cell])
        return data, env

    def execute_readback(self, env):
        """Run the workflow readback script and capture subprocess calls without native
        execution.
        """
        with patch.dict(os.environ, env, clear=True), patch.object(subprocess, 'run') as run, patch('sys.stderr', new_callable=io.StringIO):
            try: exec(compile(self.readback, str(WORKFLOW), 'exec'), {})
            finally: self.readback_calls = run.call_args_list
        return self.readback_calls

    def test_readback_hashes_all_cells_before_fixed_cli_invocations(self):
        """Verify readback hashes all cells before fixed CLI invocations."""
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
        """Verify all six rebound real ZIP semantics use their distinct control job."""
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
        """Verify readback missing extra changed symlink and mapping fail before any CLI."""
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
        """Verify readback capacity precedes download and one CLI failure stops batch."""
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
        """Verify source availability uses fixed common file with exact tree and bytes."""
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


class FailureDiagnosticContracts(unittest.TestCase):
    def setUp(self):
        """Bind synthetic original receipts to the actual controller and a fixed hosted job."""
        from test_selected_product_payload import fixture
        import product_release_metadata as metadata
        import product_artifact_execution as execution
        self.root = Path(self.enterContext(tempfile.TemporaryDirectory())).resolve()
        self.files, _, _ = fixture('core', '3.8')
        self.plan, self.producer, self.consumer = (json.loads(self.files[name]) for name in
            ('plan.json', 'producer/receipt.json', 'consumer/receipt.json'))
        controller = {'commit': metadata.git(ROOT, 'rev-parse', 'HEAD'),
                      'tree': metadata.git(ROOT, 'rev-parse', 'HEAD^{tree}')}
        self.plan['controller'].update(controller)
        self.plan['controller']['input_sha256'] = {path: metadata.sha256((ROOT / path).read_bytes())
                                                   for path in metadata.PLANNER_INPUTS}
        context = self.producer['execution']['context']
        context.update(head_sha=controller['commit'], workflow_sha=controller['commit'])
        self.environment = dict(os.environ) | {name: context[key] for key, name in execution.HOSTED_FIELDS.items()}
        self.environment.update(GITHUB_ACTIONS='true', GITHUB_HEAD_REF='', GITHUB_BASE_REF='',
                                RUNNER_TEMP=str(self.root), PYTHONDONTWRITEBYTECODE='1',
                                PYTHONPATH=str(ROOT / 'scripts/integration-program'))
        self.plan_bytes = encoded(self.plan)
        plan_hash = metadata.sha256(self.plan_bytes)
        for receipt in (self.producer, self.consumer):
            receipt.update(planner_controller=self.plan['controller'], artifact_controller=controller,
                           plan_sha256=plan_hash)
        self.consumer['controller'] = controller
        for value in (self.producer['execution'], self.consumer['execution'], self.consumer['producer_execution']):
            value.update(controller=controller, context=deepcopy(context), plan_sha256=plan_hash)
        selected = self.plan['inventory']['selected'][0]
        self.producer.update(success=False, stage='original-product-recipe', failure_code='original-product-recipe-failed',
            product_recipe_failure={'status': 'observed', 'stage': 'package-verification',
                'reason': 'source-producer-unverified', 'receipt_sha256': 'e' * 64,
                'source_producer_failure': {'package': selected['id'], 'framework': selected['frameworks'][0],
                                          'check': 'embedded-checksum-mismatch'}})
        self.consumer.update(success=False, stage='selected-restore-compile', failure_code='selected-restore-compile-failed')
        self.canary = 'PRIVATE_SECRET_CANARY_8693'
        self.producer['unvalidated_private_field'] = self.consumer['unvalidated_private_field'] = self.canary
        self.producer['preflight']['unvalidated_private_field'] = {'raw': self.canary}
        self.consumer['coverage'][0]['unvalidated_private_field'] = {'raw': self.canary}
        plan_path = self.root / 'selected-plan/plan.json'
        plan_path.parent.mkdir(); plan_path.write_bytes(self.plan_bytes)
        self.action = yaml.safe_load(ACTION.read_text())

    def script(self, name):
        return next(step['run'] for step in self.action['runs']['steps'] if step.get('name') == name)

    def run_pipeline(self, role, after_control=None):
        """Execute the actual set-e command block and failure-only staging with fake product CLIs."""
        if role == 'consumer':
            self.producer.update(success=True, stage='complete', artifact_proof=True)
            self.producer.pop('failure_code', None)
        self.consumer['artifact_receipt_sha256'] = hashlib.sha256(encoded(self.producer)).hexdigest()
        fixtures = self.root / 'fixtures'; fixtures.mkdir()
        for name, value in (('producer', self.producer), ('consumer', self.consumer)):
            (fixtures / (name + '.json')).write_bytes(encoded(value))
        executable = self.root / 'bin/python3'; executable.parent.mkdir()
        executable.write_text('#!' + sys.executable + '\n' + '''import os, runpy, shutil, sys
from pathlib import Path
args = sys.argv[1:]
script = next(value for value in args if value.endswith('.py'))
if script.endswith('retain_selected_failure_diagnostics.py'):
    sys.argv = args[args.index(script):]
    runpy.run_path(script, run_name='__main__')
else:
    role = 'producer' if script.endswith('prove_product_release_artifacts.py') else 'consumer'
    target = Path(args[args.index('--output') + 1]) / 'retained/receipt.json'
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(Path(os.environ['RUNNER_TEMP']) / 'fixtures' / (role + '.json'), target)
    with (Path(os.environ['RUNNER_TEMP']) / 'calls').open('a') as stream:
        stream.write(role + '\\n')
    raise SystemExit((13 if role == 'producer' else 17) if role == os.environ['FAILED_ROLE'] else 0)
''')
        executable.chmod(0o755)
        environment = self.environment | {'PATH': str(executable.parent) + os.pathsep + os.environ['PATH'], 'FAILED_ROLE': role}
        control = subprocess.run(['bash', '-c', self.script('Original producer and cold selected consumers')],
            cwd=ROOT, env=environment, capture_output=True)
        if after_control is not None:
            after_control()
        diagnostic = subprocess.run(['bash', '-c', self.script('Stage only validated failed-control diagnostic receipts')],
            cwd=ROOT, env=environment, capture_output=True)
        self.assertEqual(13 if role == 'producer' else 17, control.returncode)
        self.assertNotIn(self.canary.encode(), control.stdout + control.stderr + diagnostic.stdout + diagnostic.stderr)
        return diagnostic

    def test_actual_failed_producer_keeps_only_safe_bound_diagnostic_and_original_exit(self):
        result = self.run_pipeline('producer')
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        output = self.root / 'selected-failure-diagnostics'
        self.assertEqual(['producer.json'], [path.name for path in output.iterdir()])
        value = json.loads((output / 'producer.json').read_bytes())
        self.assertFalse(value['success'])
        self.assertEqual('selected-product-failure-diagnostics-only-not-control-proof', value['scope'])
        self.assertEqual(hashlib.sha256(encoded(self.producer)).hexdigest(), value['original_receipt_sha256'])
        self.assertEqual(self.producer['product_recipe_failure'], value['product_recipe_failure'])
        self.assertNotIn(self.canary, json.dumps(value))
        self.assertEqual('producer\n', (self.root / 'calls').read_text())
        self.assertFalse((self.root / 'selected-upload').exists())
        import test_selected_control_seal as seal_tests
        from test_selected_control_transport import zipped
        import selected_control_seal as seal
        canonical = seal_tests.SealContracts()
        canonical.setUp()
        data = zipped([('producer.json', (output / 'producer.json').read_bytes())])
        canonical.provider.update(archive_size=len(data), archive_sha256=hashlib.sha256(data).hexdigest())
        with self.assertRaisesRegex(ValueError, 'selected_transport_unlisted_path'):
            seal.readback(data, canonical.provider, canonical.expected, contracts=canonical.contracts, now=canonical.now)

    def test_late_consumer_receipt_over_four_mib_is_projected_without_coverage_or_private_fields(self):
        self.consumer['coverage'] *= 200
        self.assertGreater(len(encoded(self.consumer)), 4 * 1024 ** 2)
        result = self.run_pipeline('consumer')
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        output = self.root / 'selected-failure-diagnostics'
        self.assertEqual(['consumer.json'], [path.name for path in output.iterdir()])
        value = json.loads((output / 'consumer.json').read_bytes())
        self.assertEqual({'status': 'unavailable'}, value['dependency_range_failure'])
        self.assertNotIn('coverage', value)
        self.assertNotIn(self.canary, json.dumps(value))
        self.assertLess((output / 'consumer.json').stat().st_size, 4096)
        self.assertEqual('producer\nconsumer\n', (self.root / 'calls').read_text())

    def test_plan_over_four_mib_uses_the_transport_bound(self):
        self.plan_bytes += b' ' * (4 * 1024 ** 2)
        plan_hash = hashlib.sha256(self.plan_bytes).hexdigest()
        (self.root / 'selected-plan/plan.json').write_bytes(self.plan_bytes)
        for receipt in (self.producer, self.consumer):
            receipt['plan_sha256'] = plan_hash
        for value in (self.producer['execution'], self.consumer['execution'], self.consumer['producer_execution']):
            value['plan_sha256'] = plan_hash
        result = self.run_pipeline('producer')
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        value = json.loads((self.root / 'selected-failure-diagnostics/producer.json').read_bytes())
        self.assertEqual(plan_hash, value['plan_sha256'])

    def test_plan_source_controller_and_public_failure_identity_are_closed(self):
        import retain_selected_failure_diagnostics as diagnostics
        controller = {key: self.plan['controller'][key] for key in ('commit', 'tree')}
        for case in ('plan', 'source', 'controller', 'published', 'stage'):
            with self.subTest(case=case):
                receipt = deepcopy(self.producer)
                if case == 'plan': receipt['plan_sha256'] = '0' * 64
                elif case == 'source': receipt['source']['commit'] = '0' * 40
                elif case == 'controller': receipt['artifact_controller']['commit'] = '0' * 40
                elif case == 'published': receipt['published'] = True
                else: receipt['stage'] = self.canary
                with patch.dict(os.environ, self.environment, clear=True), self.assertRaises(ValueError):
                    diagnostics.project(receipt, 'e' * 64, 'artifact', self.plan,
                        hashlib.sha256(self.plan_bytes).hexdigest(), controller)

    def test_plan_over_transport_limit_is_closed(self):
        def oversize_plan():
            with (self.root / 'selected-plan/plan.json').open('wb') as stream:
                stream.seek(transport.MAX_PLAN_BYTES); stream.write(b'\n')
        result = self.run_pipeline('producer', oversize_plan)
        self.assertEqual(1, result.returncode)
        self.assertFalse((self.root / 'selected-failure-diagnostics').exists())

    def test_invalid_optional_details_are_explicitly_unavailable(self):
        self.producer['product_recipe_failure']['source_producer_failure']['package'] = self.canary
        result = self.run_pipeline('producer')
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        value = json.loads((self.root / 'selected-failure-diagnostics/producer.json').read_bytes())
        self.assertEqual({'status': 'unavailable'}, value['product_recipe_failure'])
        self.assertNotIn(self.canary, json.dumps(value))

    def test_wrong_run_identity_is_closed_and_does_not_change_original_failure(self):
        self.producer['execution']['context']['run_id'] = '999999'
        result = self.run_pipeline('producer')
        self.assertEqual(1, result.returncode)
        self.assertFalse((self.root / 'selected-failure-diagnostics').exists())

    def test_valid_consumer_range_details_are_bound_to_the_selected_root(self):
        selected = self.plan['inventory']['selected'][0]
        self.consumer['dependency_range_failure'] = {'root_id': selected['id'], 'framework': selected['frameworks'][0],
            'phase': 'discovery', 'reason': 'count-mismatch', 'requested_checks': 3, 'returned_checks': 0}
        result = self.run_pipeline('consumer')
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        value = json.loads((self.root / 'selected-failure-diagnostics/consumer.json').read_bytes())
        self.assertEqual(self.consumer['dependency_range_failure'], value['dependency_range_failure'])

    def test_consumer_range_root_canary_and_wrong_framework_are_unavailable(self):
        import retain_selected_failure_diagnostics as diagnostics
        selected = next(row for row in self.plan['inventory']['selected'] if row['frameworks'] == ['net10.0'])
        controller = {key: self.plan['controller'][key] for key in ('commit', 'tree')}
        for root, framework in ((self.canary, 'net10.0'), (selected['id'], 'net8.0')):
            with self.subTest(root=root, framework=framework):
                receipt = deepcopy(self.consumer)
                receipt['dependency_range_failure'] = {'root_id': root, 'framework': framework,
                    'phase': 'discovery', 'reason': 'count-mismatch', 'requested_checks': 3, 'returned_checks': 0}
                with patch.dict(os.environ, self.environment, clear=True):
                    value = diagnostics.project(receipt, 'e' * 64, 'consumer', self.plan,
                        hashlib.sha256(self.plan_bytes).hexdigest(), controller)
                self.assertEqual({'status': 'unavailable'}, value['dependency_range_failure'])
                self.assertNotIn(self.canary, json.dumps(value))

    def test_linked_plan_and_consumer_inputs_are_closed(self):
        for target in ('selected-plan/plan.json', 'selected-consumer/retained/receipt.json'):
            with self.subTest(target=target):
                path = self.root / target
                def link():
                    original = path.with_suffix('.private')
                    path.rename(original); path.symlink_to(original)
                result = self.run_pipeline('consumer', link)
                self.assertEqual(1, result.returncode)
                self.assertFalse((self.root / 'selected-failure-diagnostics').exists())
                path.unlink(); path.with_suffix('.private').rename(path)
                shutil.rmtree(self.root / 'fixtures'); shutil.rmtree(self.root / 'bin')

    def test_missing_linked_nonregular_and_oversize_inputs_are_closed(self):
        import retain_selected_failure_diagnostics as diagnostics
        producer_path = self.root / 'selected-producer/retained/receipt.json'
        producer_path.parent.mkdir(parents=True)
        producer_path.write_bytes(encoded(self.producer))
        source = self.root / 'original.json'; source.write_bytes(encoded(self.producer))
        for case in ('missing', 'final-link', 'parent-link', 'fifo', 'oversize'):
            with self.subTest(case=case):
                producer_path.unlink()
                if case == 'final-link': producer_path.symlink_to(source)
                elif case == 'parent-link':
                    producer_path.parent.rmdir()
                    target = self.root / 'linked-retained'; target.mkdir(); (target / 'receipt.json').write_bytes(encoded(self.producer))
                    producer_path.parent.symlink_to(target)
                elif case == 'fifo': os.mkfifo(producer_path)
                elif case == 'oversize':
                    with producer_path.open('wb') as stream:
                        stream.seek(transport.MAX_RECEIPT_BYTES); stream.write(b'\n')
                stdout = io.StringIO()
                with patch.object(sys, 'argv', ['diagnostic', '--runner-temp', str(self.root)]), patch('sys.stdout', stdout):
                    self.assertEqual(1, diagnostics.main())
                self.assertNotIn(self.canary, stdout.getvalue())
                self.assertFalse((self.root / 'selected-failure-diagnostics').exists())
                if case == 'parent-link':
                    producer_path.parent.unlink(); producer_path.parent.mkdir()
                elif producer_path.exists() or producer_path.is_symlink(): producer_path.unlink()
                producer_path.write_bytes(encoded(self.producer))


if __name__ == '__main__': unittest.main()
