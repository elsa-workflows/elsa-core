from __future__ import annotations

from copy import deepcopy
from contextlib import redirect_stdout
from datetime import datetime, timedelta, timezone
import fnmatch
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
import zipfile
from unittest.mock import patch

import plan_product_release as planner
import product_release_metadata as metadata
import prove_product_release_artifacts as artifacts
import prove_historical_studio_npm_pair as historical
import product_artifact_execution as execution


class ProductArtifactAdmissionTests(unittest.TestCase):
    def setUp(self):
        self.time = datetime.now(timezone.utc).isoformat()
        source = {'product': 'studio', 'line': '3.8', 'commit': 'a' * 40, 'tree': 'b' * 40}
        project = {'path': 'src/Example/Example.csproj', 'package_id': 'Example', 'is_packable': True,
                   'is_test_project': False, 'target_frameworks': ['net10.0'], 'project_references': [], 'package_references': []}
        inventory = {'source_commit': source['commit'], 'source_tree': source['tree'], 'requested_version': '3.8.99',
                     'projects': [project], 'excluded': [], 'ownership_policy': metadata.ownership_policy(),
                     'release_recipe': {'projects': [project['path']]},
                     'selected': [{'id': 'Example', 'project': project['path'], 'frameworks': ['net10.0'], 'symbols': True,
                                   'metadata': {'status': 'observed', 'dependency_groups': []}}]}
        inventory['sha256'] = metadata.canonical_hash(inventory)
        self.plan = {'schema': 1, 'mode': 'read-only-product-release-plan', 'product': 'studio', 'line': '3.8',
                     'requested_version': '3.8.99', 'source': source, 'controller': {'commit': 'c' * 40, 'tree': 'd' * 40,
                                    'input_sha256': {path: 'e' * 64 for path in artifacts.PLANNER_INPUTS}},
                     'inventory': inventory, 'eligible': True, 'reasons': [], 'published': False, 'version_allocated': False,
                     'tag_created': False, 'observed_at': self.time, 'feed_service_observations': {}, 'prerequisites': [],
                     'selected_dependency_intent': [], 'prerequisites_excluded_from_publication': [],
                     'npm': {'atomic': True, 'line': '3.8', 'source_commit': source['commit'], 'source_tree': source['tree'],
                             'packages': [{'id': name, 'version': '3.8.99', 'expected_tarball': name[1:].replace('/', '-') + '-3.8.99.tgz'}
                                          for name in planner.NPM_IDS]},
                     'histories': [{'id': name, 'eligible': True, 'reason': None, 'observation': self.observation()}
                                   for name in ('Example', *planner.NPM_IDS)]}
        self.plan['expected_artifacts'] = ['Example.3.8.99.nupkg', 'Example.3.8.99.snupkg'] + [
            row['expected_tarball'] for row in self.plan['npm']['packages']]

    def observation(self):
        return {'status': 'observed', 'observed_at': self.time, 'sha256': 'e' * 64, 'bytes': 100}

    def admit(self, plan=None):
        data = json.dumps(plan or self.plan).encode()
        return artifacts.admit(data, metadata.sha256(data), checked_at=self.time)

    def test_exact_reviewed_plan_admitted_without_process_or_network(self):
        with patch.object(artifacts.metadata, 'git', side_effect=AssertionError('process forbidden')):
            self.assertEqual(self.plan, self.admit())

    def test_incomplete_controller_inputs_rejected(self):
        self.plan['controller']['input_sha256'].pop(next(iter(artifacts.PLANNER_INPUTS)))
        with self.assertRaisesRegex(ValueError, 'plan_controller_inputs'):
            self.admit()

    def test_wrong_hash_rejected(self):
        with self.assertRaisesRegex(ValueError, 'plan_hash'):
            artifacts.admit(json.dumps(self.plan).encode(), 'f' * 64)

    def test_duplicate_json_key_rejected(self):
        data = b'{"schema":1,"schema":1}'
        with self.assertRaises(ValueError):
            artifacts.admit(data, metadata.sha256(data))

    def test_stale_future_missing_and_ineligible_observations_rejected(self):
        for changes in ({'observed_at': (datetime.fromisoformat(self.time) - timedelta(hours=2)).isoformat()},
                        {'observed_at': (datetime.fromisoformat(self.time) + timedelta(seconds=1)).isoformat()},
                        {'status': 'unavailable'}, {'sha256': 'invalid'}, {'bytes': 0}):
            plan = deepcopy(self.plan); plan['histories'][0]['observation'].update(changes)
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                self.admit(plan)

    def test_wrong_selection_and_ineligible_plan_rejected(self):
        for key, value in (('product', 'core'), ('line', '3.9'), ('requested_version', '3.9.99'),
                           ('eligible', False), ('published', True), ('version_allocated', True), ('tag_created', True)):
            plan = deepcopy(self.plan); plan[key] = value
            with self.subTest(key=key), self.assertRaises(ValueError):
                self.admit(plan)

    def test_inventory_hash_and_partition_rejected(self):
        for mutate in (lambda p: p['inventory']['selected'].clear(),
                       lambda p: p['inventory']['excluded'].append({'project': 'unknown', 'id': 'Unknown', 'reason': 'test'}),
                       lambda p: p['expected_artifacts'].append('Unselected.3.8.99.nupkg'),
                       lambda p: p['histories'].pop()):
            plan = deepcopy(self.plan); mutate(plan)
            with self.assertRaises(ValueError):
                self.admit(plan)

    def test_eligible_prerequisite_can_be_missing_on_one_mapped_feed(self):
        present = {'eligible': True, 'reason': None, 'observation': self.observation()}
        missing = {'eligible': False, 'reason': 'prerequisite_missing',
                   'observation': {'status': 'missing', 'observed_at': self.time}}
        self.plan['prerequisites'] = [{'id': 'External', 'eligible': True, 'reason': None, 'feeds': [present, missing]}]
        self.plan['prerequisites_excluded_from_publication'] = ['External']
        self.assertEqual(self.plan, self.admit())

    def test_prerequisites_cannot_widen_selected_scope(self):
        self.plan['prerequisites'] = [{'id': 'Example', 'eligible': True, 'reason': None, 'observation': self.observation()}]
        self.plan['prerequisites_excluded_from_publication'] = ['Example']
        with self.assertRaisesRegex(ValueError, 'plan_prerequisite_scope'):
            self.admit()

    def test_pair_requires_exact_same_source_version_and_names(self):
        for key, value in (('source_commit', 'f' * 40), ('atomic', False), ('line', '3.9')):
            plan = deepcopy(self.plan); plan['npm'][key] = value
            with self.subTest(key=key), self.assertRaisesRegex(ValueError, 'plan_npm_identity'):
                self.admit(plan)

    def test_failed_admission_has_no_directory_or_controller_side_effect(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / 'proof'
            with patch.object(artifacts, 'verify_controller', side_effect=AssertionError('controller touched')):
                with self.assertRaises(ValueError):
                    artifacts.execute(Path(directory), b'{}', 'f' * 64, output)
            self.assertFalse(output.exists())

    def test_release_plan_trigger_covers_consumed_runtime_tests_helpers_and_documents(self):
        workflow = (artifacts.ROOT / '.github/workflows/product-release-plan.yml').read_text()
        filters = [line.split("'")[1] for line in workflow.splitlines() if line.strip().startswith("- '")]
        paths = ['scripts/integration-program/prove_product_release_artifacts.py',
                 'scripts/integration-program/test_prove_product_release_artifacts.py',
                 'scripts/integration-program/VerifyPackageSymbolPair/Program.cs',
                 '.agents/skills/elsa-release/scripts/verify_packages.py',
                 'docs/integration-program/maintenance-containment.json',
                 'docs/integration-program/maintenance-core-candidates.json', metadata.OWNERSHIP_DOCUMENT]
        for path in paths:
            with self.subTest(path=path):
                self.assertTrue(any(fnmatch.fnmatch(path, pattern) for pattern in filters))

    def refresh_remote(self, *, status='observed', age=0):
        clock = datetime(2026, 10, 9, 20, 0, tzinfo=timezone.utc)
        checked = []
        body = json.dumps({'name': planner.NPM_IDS[0], 'versions': {'3.8.4': {}},
                           'time': {'3.8.4': clock.isoformat()}}).encode()

        class Observations:
            def get(inner, url):
                nonlocal clock
                clock += timedelta(seconds=1)
                identifier = next(value for value in planner.NPM_IDS if planner.history_url(value, True) == url)
                data = body.replace(planner.NPM_IDS[0].encode(), identifier.encode())
                return {'url': url, 'observed_at': (clock - timedelta(seconds=age)).isoformat(),
                        'status': status, 'sha256': metadata.sha256(data), 'bytes': len(data), '_body': data}

        class Feeds:
            def __init__(inner, *args):
                inner.policy = {}

            def prefetch(inner, *args):
                pass

            def history(inner, identifier, version, line, semantics, when):
                checked.append(when)
                return {'eligible': True}

        class Semantics:
            def call(inner, operation, **values):
                self.assertEqual('history', operation)
                return {'duplicate': False, 'reused': False, 'monotonic': True}

        plan = self.plan | {'consumer_feed_policy': {}}
        with patch.object(planner, 'Observations', Observations), patch.object(planner, 'FeedMetadata', Feeds), \
                patch.object(planner, 'now', side_effect=lambda: clock.isoformat()):
            artifacts.refresh_remote(plan, Path('/unused'), Semantics())
        return checked, clock

    def test_remote_refresh_checks_after_both_npm_observations(self):
        checked, clock = self.refresh_remote()
        self.assertEqual([clock.isoformat()], checked)

    def test_remote_refresh_rejects_unavailable_and_stale_npm(self):
        for status, age in (('unavailable', 0), ('observed', planner.MAX_AGE_SECONDS + 1)):
            with self.subTest(status=status, age=age), self.assertRaisesRegex(ValueError, 'artifact_fresh_prerequisite_failed'):
                self.refresh_remote(status=status, age=age)

    def test_selected_retention_rejects_missing_unknown_and_wrong_dependency_archives(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / 'archives'
            source.mkdir()
            for index, (name, identity, dependency) in enumerate((
                    ('Example.3.8.99.nupkg', 'Example', ''),
                    ('Unknown.3.8.99.nupkg', 'Unknown', ''),
                    ('Example.3.8.99.nupkg', 'Example', '<dependencies><dependency id="Wrong" version="1.0.0" /></dependencies>'))):
                for previous in source.iterdir():
                    previous.unlink()
                with zipfile.ZipFile(source / name, 'w') as archive:
                    archive.writestr(identity + '.nuspec', '<package><metadata><id>' + identity +
                        '</id><version>3.8.99</version>' + dependency + '</metadata></package>')
                with self.subTest(identity=identity, dependency=dependency), self.assertRaises(ValueError):
                    artifacts.retain_selected(self.plan, source, root / ('retained-' + str(index)))

    def test_local_execution_is_uuid_utc_and_has_no_hosted_numeric_fields(self):
        local = execution.local_execution({})
        execution.validate_local_execution(local)
        self.assertEqual({'kind', 'id', 'started_at'}, set(local))
        self.assertEqual('local-control', local['kind'])
        self.assertTrue(local['started_at'].endswith('+00:00'))
        self.assertNotEqual(local['id'], execution.local_execution({})['id'])

    def test_hosted_execution_context_and_envelope_rejected(self):
        for environment in ({'GITHUB_ACTIONS': 'true'}, {'GITHUB_RUN_ID': '123'}, {'GITHUB_RUN_ATTEMPT': '1'}):
            with self.subTest(environment=environment), self.assertRaisesRegex(ValueError, 'hosted_control_not_supported'):
                execution.local_execution(environment)
        for change in ({'kind': 'github-actions'}, {'run_id': '123'}, {'id': '123'}, {'started_at': '2026-10-09T12:00:00'}):
            candidate = execution.local_execution({}) | change
            with self.subTest(change=change), self.assertRaises(ValueError):
                execution.validate_local_execution(candidate)

    def preflight(self, outputs=None, *, recipe_framework='net10.0'):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        source = Path(temporary.name)
        private = source / 'private'
        private.mkdir()
        workflow = source / 'packages.source'
        workflow.write_text('        run: dotnet publish ./src/hosts/Elsa.Studio.Host.CustomElements --configuration Release '
                            '-o ./packages/wasm /p:Version=${VERSION} -f ' + recipe_framework + '\n')
        host = source / historical.HOST / 'Elsa.Studio.Host.CustomElements.csproj'
        host.parent.mkdir(parents=True)
        host.write_text('<Project/>')
        plan = {'npm': {'workflow': {'path': 'packages.source', 'sha256': metadata.sha256(workflow.read_bytes())}}}
        values = iter(outputs or ['v22.23.3', '10.9.4', '10.0.300 [/sdk]', '10.0.300',
                                  '{"Properties":{"TargetFrameworks":"net8.0;net9.0;net10.0","TargetFramework":""}}'])
        commands = []
        shared_run = artifacts.run
        def run(command, cwd, **kwargs):
            commands.append(command)
            self.assertNotIn('log', kwargs)
            output = next(values)
            stderr = ('[dotnet-build-slots] waiting for a build slot (2 in use, waited 0s).\n'
                      '[dotnet-build-slots] got slot 0 after 6s\n'
                      if command[:2] == ['dotnet', 'msbuild'] else '')
            script = 'import sys; sys.stdout.write(' + repr(output) + '); sys.stderr.write(' + repr(stderr) + ')'
            return shared_run([sys.executable, '-c', script], cwd, timeout=kwargs['timeout'], env=kwargs['env'])
        with patch.object(artifacts, 'run', side_effect=run):
            result = artifacts.preflight(source, plan, private)
        return result, commands, (private / 'preflight-host-frameworks.log').read_text()

    def test_failed_tool_preflight_stops_before_helper_or_product_recipe(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            controller = root / 'controller'
            controller.mkdir()
            output = root / 'proof'
            plan = deepcopy(self.plan)
            plan['inventory']['release_recipe'].update(solution='Elsa.Studio.sln', sha256=metadata.sha256(b'solution'),
                                                      workflow='packages.source', workflow_sha256=metadata.sha256(b'workflow'))
            plan['inventory']['sha256'] = metadata.canonical_hash(metadata.public_inventory(plan['inventory']))
            data = json.dumps(plan).encode()
            def checkout(controller, binding, source):
                source.mkdir()
                (source / 'Elsa.Studio.sln').write_bytes(b'solution')
                (source / 'packages.source').write_bytes(b'workflow')
            def failed_preflight(source, admitted, private):
                self.assertEqual(json.loads((output / 'global.json').read_text()),
                    {'sdk': {'version': metadata.SDK, 'rollForward': 'disable'}})
                self.assertFalse((source / 'global.json').exists())
                raise ValueError('artifact_node_version')
            plan_path = root / 'plan.json'; plan_path.write_bytes(data)
            stdout = io.StringIO()
            with patch.object(artifacts, 'verify_controller', return_value=plan['controller']), \
                    patch.object(artifacts, 'selected_execution', return_value=execution.local_execution({})), \
                    patch.object(metadata, 'checkout_source', side_effect=checkout), \
                    patch.object(planner, 'npm_intent', return_value=plan['npm']), \
                    patch.object(artifacts, 'preflight', side_effect=failed_preflight), \
                    patch.object(planner, 'build_helper') as helper, patch.object(artifacts.maintenance, 'prepare') as producer, \
                    patch.object(artifacts, 'ROOT', controller), patch.object(sys, 'argv', ['proof', '--plan', str(plan_path),
                        '--plan-sha256', metadata.sha256(data), '--output', str(output)]), redirect_stdout(stdout):
                self.assertEqual(1, artifacts.main())
                helper.assert_not_called()
                producer.assert_not_called()
            self.assertEqual(json.loads(stdout.getvalue()), {'success': False, 'failure_code': 'artifact_node_version',
                'failure_stage': 'tool-preflight', 'retained_receipt_created': True})
            receipt = json.loads((output / 'retained/receipt.json').read_text())
            self.assertFalse(receipt['success'])
            self.assertEqual('tool-preflight-failed', receipt['failure_code'])

    def test_tool_preflight_only_evaluates_source_supported_recipe(self):
        result, commands, framework_log = self.preflight()
        self.assertFalse(result['product_work_executed'])
        self.assertEqual('net10.0', result['host_framework'])
        self.assertEqual(5, len(commands))
        self.assertTrue(all(not any(value in command for value in ('restore', 'build', 'pack', 'publish', 'test'))
                            for command in commands))
        self.assertEqual(json.dumps(commands[-1]) + '\n' +
                         '{"Properties":{"TargetFrameworks":"net8.0;net9.0;net10.0","TargetFramework":""}}',
                         framework_log)
        self.assertNotIn('[dotnet-build-slots]', framework_log)

    def test_incompatible_tool_or_host_recipe_fails_preflight(self):
        defaults = ['v22.23.3', '10.9.4', '10.0.300 [/sdk]', '10.0.300',
                    '{"Properties":{"TargetFrameworks":"net8.0;net9.0;net10.0","TargetFramework":""}}']
        for index, value, reason in ((0, 'v25.8.0', 'artifact_node_version'), (1, '8.1.0', 'artifact_npm_version'),
                (2, '10.0.101 [/sdk]', 'artifact_sdk_unavailable'), (3, '10.0.101', 'artifact_sdk_selection'),
                (4, '{"Properties":{"TargetFramework":"net8.0"}}', 'artifact_host_unsupported_framework')):
            values = list(defaults)
            values[index] = value
            with self.subTest(reason=reason), self.assertRaisesRegex(ValueError, reason):
                self.preflight(values)
        for stdout in ('MSBuild error: project file not found',
                       'unexpected host output\n{"Properties":{"TargetFrameworks":"net10.0"}}'):
            values = list(defaults)
            values[4] = stdout
            with self.subTest(stdout=stdout), self.assertRaises(json.JSONDecodeError):
                self.preflight(values)
        with self.assertRaisesRegex(ValueError, 'artifact_host_recipe_framework'):
            self.preflight(recipe_framework='net9.0')

    def test_staging_historical_version_preserves_lifecycle_and_files(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'package.json'
            original = {'name': planner.NPM_IDS[1], 'version': '0.0.0', 'files': ['dist'],
                        'scripts': {'postinstall': 'npm run copy:elsa-studio-wasm', 'copy:elsa-studio-wasm': 'cp -r original public/'},
                        'dependencies': {planner.NPM_IDS[0]: '^3.4.0'}}
            path.write_text(json.dumps(original))
            staged = historical.stage_version(path, planner.NPM_IDS[1], '3.8.99', dependency='3.8.99')
            self.assertEqual(original['files'], staged['files'])
            self.assertEqual(original['scripts'], staged['scripts'])
            self.assertEqual('3.8.99', staged['dependencies'][planner.NPM_IDS[0]])


class ArtifactCliDiagnosticsTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.plan = self.root / 'plan.json'
        self.plan.write_bytes(b'{"schema":1}')
        self.output = self.root / 'output'

    def invoke(self, *, digest=None):
        argv = ['proof', '--plan', str(self.plan), '--plan-sha256',
            digest or metadata.sha256(self.plan.read_bytes()), '--output', str(self.output)]
        stdout = io.StringIO()
        with patch.object(sys, 'argv', argv), redirect_stdout(stdout):
            status = artifacts.main()
        self.assertEqual(status, 1)
        return json.loads(stdout.getvalue())

    def test_genuine_early_hash_failure_reports_closed_code_without_receipt(self):
        result = self.invoke(digest='f' * 64)
        self.assertEqual(result, {'success': False, 'failure_code': 'plan_hash', 'failure_stage': 'unclassified', 'retained_receipt_created': False})
        self.assertFalse(self.output.exists())

    def test_unexpected_arbitrary_error_and_malformed_json_remain_generic(self):
        self.plan.write_bytes(b'not-json')
        result = self.invoke()
        self.assertEqual(result, {'success': False, 'failure_code': 'artifact_control_failed', 'failure_stage': 'unclassified', 'retained_receipt_created': False})
        self.assertFalse(self.output.exists())
        for error in (ValueError('artifact_hosted_context /private/secret/token'),
                      RuntimeError('artifact_hosted_context'), OSError('/private/secret/token')):
            with self.subTest(error=error), patch.object(artifacts, 'execute', side_effect=error):
                self.assertEqual(self.invoke(), result)

    def test_existing_receipt_is_not_reported_created_by_failed_attempt(self):
        retained = self.output / 'retained'; retained.mkdir(parents=True)
        receipt = retained / 'receipt.json'; receipt.write_bytes(b'previous private receipt')
        result = self.invoke(digest='f' * 64)
        self.assertFalse(result['retained_receipt_created'])
        self.assertEqual(receipt.read_bytes(), b'previous private receipt')

    def test_new_regular_receipt_is_reported_without_reading_its_private_fields(self):
        def fail(*args, **kwargs):
            retained = self.output / 'retained'; retained.mkdir(parents=True)
            (retained / 'receipt.json').write_bytes(b'private secret raw diagnostics, not JSON')
            raise ValueError('artifact_hosted_context')
        with patch.object(artifacts, 'execute', side_effect=fail):
            self.assertEqual(self.invoke(), {'success': False, 'failure_code': 'artifact_hosted_context',
                'failure_stage': 'unclassified', 'retained_receipt_created': True})

    def test_symlink_receipt_never_counts_as_created(self):
        target = self.root / 'foreign'; target.write_bytes(b'secret')
        def fail(*args, **kwargs):
            retained = self.output / 'retained'; retained.mkdir(parents=True)
            (retained / 'receipt.json').symlink_to(target)
            raise ValueError('artifact_hosted_context')
        with patch.object(artifacts, 'execute', side_effect=fail):
            self.assertFalse(self.invoke()['retained_receipt_created'])

    def test_new_failed_receipt_reports_only_fixed_producer_stage(self):
        def fail(*args, **kwargs):
            retained = self.output / 'retained'; retained.mkdir(parents=True)
            (retained / 'receipt.json').write_text(json.dumps({'schema': 1,
                'mode': 'selected-product-artifact-control', 'success': False, 'stage': 'tool-preflight',
                'failure_code': 'tool-preflight-failed', 'private': '/private/token'}))
            raise RuntimeError('/private/token')
        with patch.object(artifacts, 'execute', side_effect=fail):
            self.assertEqual(self.invoke(), {'success': False, 'failure_code': 'artifact_control_failed',
                'failure_stage': 'tool-preflight', 'retained_receipt_created': True})

    def test_producer_stage_rejects_complete_setup_complete_and_raw_receipt_values(self):
        for index, stage in enumerate(('complete', 'setup-complete', 'tool-preflight /private/token')):
            self.output = self.root / str(index)
            def fail(*args, **kwargs):
                retained = self.output / 'retained'; retained.mkdir(parents=True)
                (retained / 'receipt.json').write_text(json.dumps({'schema': 1,
                    'mode': 'selected-product-artifact-control', 'success': False, 'stage': stage,
                    'failure_code': stage + '-failed', 'private': '/private/token'}))
                raise RuntimeError('/private/token')
            with self.subTest(stage=stage), patch.object(artifacts, 'execute', side_effect=fail):
                self.assertEqual(self.invoke(), {'success': False, 'failure_code': 'artifact_control_failed',
                    'failure_stage': 'unclassified', 'retained_receipt_created': True})


class CoreRecipeFailureDiagnosticsTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.controller = self.root / 'controller'
        self.controller.mkdir()
        self.output = self.root / 'proof'
        self.identity = {'commit': 'a' * 40, 'tree': 'b' * 40}
        self.execution = execution.local_execution({})
        self.plan = {'product': 'core', 'line': '3.8', 'requested_version': '3.8.5',
                     'controller': self.identity, 'npm': None,
                     'source': {'product': 'core', 'line': '3.8', 'commit': 'c' * 40, 'tree': 'd' * 40},
                     'inventory': {'release_recipe': {'solution': 'Elsa.sln', 'sha256': metadata.sha256(b'solution'),
                         'workflow': 'packages.yml', 'workflow_sha256': metadata.sha256(b'workflow')}}}
        self.inner = {'schema': 1, 'success': False, 'published': False, 'maintenance_refs_activated': False,
                      'selection': self.plan['source'] | {'source_repository': artifacts.maintenance.CORE_REPOSITORY},
                      'version': '3.8.5', 'controller_commit': self.identity['commit'],
                      'controller_tree': self.identity['tree'],
                      'controller_sha256': metadata.sha256(Path(artifacts.maintenance.__file__).read_bytes()),
                      'run_id': None, 'run_attempt': None, 'stage': 'build-command',
                      'error': {'code': 'build-command-failed', 'reason': 'unknown-check-failure'},
                      'focus': {'step': 1, 'directory': '.'},
                      'commands': [{'success': False, 'argv': ['/private/secret'], 'cwd': '/private/secret',
                          'process': {'status': 'exited', 'exit_code': 7},
                          'diagnostics': {'codes': [{'severity': 'error', 'code': 'NU1101', 'count': 2}],
                              'nuke_failed_targets': ['Restore'], 'unretained_code_occurrences': 0}}]}
        self.failure = RuntimeError('/private/secret original failure')

    def invoke(self, mutate=None, *, direct=False, producer_complete=False):
        def checkout(controller, binding, source):
            source.mkdir()
            (source / 'Elsa.sln').write_bytes(b'solution')
            (source / 'packages.yml').write_bytes(b'workflow')

        def fail(*args, **kwargs):
            producer = self.output / 'private/producer'
            producer.mkdir()
            receipt = producer / 'receipt.json'
            receipt.write_text(json.dumps(self.inner))
            if mutate:
                mutate(receipt)
            if producer_complete:
                return {'success': True, 'tests': {}, 'packages': []}
            raise self.failure

        plan_path = self.root / 'plan.json'
        plan_path.write_text('{}')
        stdout = io.StringIO()
        with patch.object(artifacts, 'ROOT', self.controller), patch.object(artifacts, 'admit', return_value=self.plan), \
                patch.object(artifacts, 'verify_controller', return_value=self.identity), \
                patch.object(artifacts, 'selected_execution', return_value=self.execution), \
                patch.object(artifacts.core, 'validate_plan'), patch.object(artifacts.core, 'verify_source'), \
                patch.object(metadata, 'checkout_source', side_effect=checkout), \
                patch.object(planner, 'npm_intent', return_value=None), patch.object(artifacts, 'preflight', return_value={}), \
                patch.object(planner, 'build_helper'), patch.object(artifacts, 'refresh_remote'), \
                patch.object(artifacts.maintenance, 'prepare', side_effect=fail), \
                patch.object(artifacts, 'retain_selected', side_effect=self.failure), \
                patch.object(sys, 'argv', ['proof', '--plan', str(plan_path), '--plan-sha256', 'e' * 64,
                                         '--output', str(self.output)]), redirect_stdout(stdout):
            if direct:
                with self.assertRaises(RuntimeError) as caught:
                    artifacts.execute(self.controller, b'{}', 'e' * 64, self.output)
                self.assertIs(caught.exception, self.failure)
            else:
                self.assertEqual(artifacts.main(), 1)
        return (json.loads(stdout.getvalue()) if not direct else None,
                json.loads((self.output / 'retained/receipt.json').read_text()))

    def test_nested_failed_native_receipt_reaches_cli_without_raw_fields_or_proof(self):
        result, receipt = self.invoke()
        self.assertEqual(result['failure_stage'], 'original-product-recipe')
        self.assertEqual(result['failure_code'], 'artifact_control_failed')
        summary = result['product_recipe_failure']
        self.assertEqual(summary['stage'], 'build-command')
        self.assertEqual(summary['reason'], 'unknown-check-failure')
        self.assertEqual(summary['command_step'], 1)
        self.assertEqual(summary['receipt_sha256'], metadata.sha256(
            (self.output / 'private/producer/receipt.json').read_bytes()))
        self.assertEqual(summary['process'], {'status': 'exited', 'exit_code': 7})
        self.assertEqual(summary['diagnostics'], {'codes': [{'severity': 'error', 'code': 'NU1101', 'count': 2}],
            'nuke_failed_targets': ['Restore'], 'unretained_code_occurrences': 0})
        self.assertEqual(summary, receipt['product_recipe_failure'])
        self.assertFalse(receipt['success'])
        self.assertNotIn('artifact_proof', receipt)
        self.assertNotIn('/private/secret', json.dumps(result))
        self.assertNotIn('/private/secret', json.dumps(receipt))

    def test_all_inner_verification_stages_report_only_closed_reason(self):
        for stage in ('source-verification', 'toolchain', 'inventory', 'test-evidence',
                      'symbol-inspector', 'sdk-metadata', 'package-verification'):
            with self.subTest(stage=stage):
                self.output = self.root / stage
                self.inner.update(stage=stage, error={'code': stage + '-failed', 'reason': 'test-evidence-invalid'})
                result, receipt = self.invoke()
                summary = result['product_recipe_failure']
                self.assertEqual(summary['stage'], stage)
                self.assertEqual(summary['reason'], 'test-evidence-invalid')
                self.assertEqual(set(summary), {'status', 'stage', 'reason', 'receipt_sha256'})
                self.assertEqual(summary, receipt['product_recipe_failure'])
                self.assertNotIn('/private/secret', json.dumps(result))

    def test_identity_and_closed_field_mutations_are_unavailable(self):
        mutations = [
            {'schema': True}, {'success': True}, {'published': True}, {'maintenance_refs_activated': True},
            {'selection': self.inner['selection'] | {'commit': 'f' * 40}},
            {'selection': self.inner['selection'] | {'source_repository': 'private/secret'}},
            {'selection': self.inner['selection'] | {'extra': '/private/secret'}},
            {'version': '3.8.6'}, {'controller_commit': 'f' * 40}, {'controller_tree': 'f' * 40},
            {'controller_sha256': 'f' * 64}, {'run_id': '42'}, {'run_attempt': '2'},
            {'stage': '/private/secret'}, {'stage': []},
            {'error': {'code': 'inventory-failed', 'reason': 'unknown-check-failure'}},
            {'error': {'code': 'build-command-failed', 'reason': '/private/secret'}},
            {'error': {'code': 'build-command-failed', 'reason': []}},
            {'focus': {'step': True, 'directory': '.'}}, {'focus': {'step': 2, 'directory': '.'}},
            {'focus': {'step': 1, 'directory': '/private/secret'}}, {'commands': []}]
        command = deepcopy(self.inner['commands'][0])
        for change in ({'success': True}, {'process': {'status': '/private/secret', 'exit_code': 1}},
                       {'process': {'status': 'exited', 'exit_code': 0}},
                       {'process': {'status': 'exited', 'exit_code': True}},
                       {'process': {'status': 'exited', 'exit_code': 2 ** 31}},
                       {'diagnostics': {'codes': [], 'nuke_failed_targets': ['/private/secret'],
                                        'unretained_code_occurrences': 0}},
                       {'diagnostics': {'codes': [{'severity': 'error', 'code': 'NU1101 /private/secret', 'count': 1}],
                                        'nuke_failed_targets': [], 'unretained_code_occurrences': 0}},
                       {'diagnostics': {'codes': [], 'nuke_failed_targets': [], 'unretained_code_occurrences': True}}):
            mutations.append({'commands': [command | change]})
        for index, mutation in enumerate(mutations):
            with self.subTest(mutation=mutation):
                self.output = self.root / f'mutation-{index}'
                original = self.inner
                self.inner = deepcopy(original) | mutation
                result, receipt = self.invoke()
                self.inner = original
                self.assertEqual(result['product_recipe_failure'], {'status': 'unavailable'})
                self.assertEqual(receipt['product_recipe_failure'], {'status': 'unavailable'})
                self.assertNotIn('/private/secret', json.dumps(result))

    def test_missing_symlink_malformed_and_oversized_diagnostics_preserve_original_exception(self):
        def symlink(receipt):
            target = self.root / 'foreign.json'
            target.write_text(json.dumps(self.inner))
            receipt.unlink()
            receipt.symlink_to(target)
        def parent_symlink(receipt):
            target = receipt.parent.with_name('moved')
            receipt.parent.rename(target)
            receipt.parent.symlink_to(target, target_is_directory=True)
        mutations = [lambda receipt: receipt.unlink(), symlink,
                     parent_symlink, lambda receipt: receipt.write_bytes(b'[]'),
                     lambda receipt: receipt.write_bytes(b'{"schema":1,"schema":1}'),
                     lambda receipt: receipt.write_bytes(b'private secret not JSON'),
                     lambda receipt: receipt.write_bytes(b'[' * 2000 + b']' * 2000),
                     lambda receipt: receipt.write_bytes(b' ' * (artifacts.DIAGNOSTIC_LIMIT + 1))]
        for index, mutation in enumerate(mutations):
            with self.subTest(index=index):
                self.output = self.root / f'file-{index}'
                _, receipt = self.invoke(mutation, direct=True)
                self.assertEqual(receipt['product_recipe_failure'], {'status': 'unavailable'})
                self.assertEqual(receipt['failure_code'], 'original-product-recipe-failed')

    def test_hosted_receipt_requires_matching_run_and_attempt(self):
        self.execution = {'context': {'run_id': '123', 'run_attempt': '2'}}
        self.inner.update(run_id='123', run_attempt='2')
        result, _ = self.invoke()
        self.assertEqual(result['product_recipe_failure']['status'], 'observed')
        self.output = self.root / 'other-attempt'
        self.inner['run_attempt'] = '1'
        result, _ = self.invoke()
        self.assertEqual(result['product_recipe_failure'], {'status': 'unavailable'})

    def test_failure_after_successful_producer_has_no_nested_failure_summary(self):
        self.inner['success'] = True
        result, receipt = self.invoke(producer_complete=True)
        self.assertEqual(result['failure_stage'], 'original-product-recipe')
        self.assertNotIn('product_recipe_failure', result)
        self.assertNotIn('product_recipe_failure', receipt)

    def test_cli_revalidates_projection_and_ignores_preexisting_receipt(self):
        _, receipt = self.invoke()
        path = self.output / 'retained/receipt.json'
        receipt['product_recipe_failure']['reason'] = '/private/secret'
        path.write_text(json.dumps(receipt))
        self.assertEqual(artifacts.public_recipe_failure(self.output, False, 'original-product-recipe'),
                         {'product_recipe_failure': {'status': 'unavailable'}})
        self.assertEqual(artifacts.public_recipe_failure(self.output, True, 'original-product-recipe'), {})
        self.assertEqual(artifacts.public_recipe_failure(self.output, False, 'tool-preflight'), {})


if __name__ == '__main__':
    unittest.main()
