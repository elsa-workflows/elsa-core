from __future__ import annotations

from copy import deepcopy
from datetime import datetime, timedelta, timezone
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import product_artifact_execution as execution
import product_release_metadata as metadata
import prove_product_release_consumers as consumers
import prove_historical_studio_npm_pair as historical
import snapshot_product_planning_assets as snapshots


class SelectedExecutionTests(unittest.TestCase):
    def setUp(self):
        self.controller = {'commit': 'a' * 40, 'tree': 'b' * 40}
        self.hash = 'c' * 64
        self.environment = {'GITHUB_ACTIONS': 'true', 'GITHUB_REPOSITORY': execution.REPOSITORY,
            'GITHUB_REPOSITORY_ID': execution.REPOSITORY_ID, 'GITHUB_EVENT_NAME': 'push',
            'GITHUB_REF': execution.HOSTED_REF, 'GITHUB_SHA': self.controller['commit'],
            'GITHUB_WORKFLOW_REF': f'{execution.REPOSITORY}/{execution.WORKFLOW}@{execution.HOSTED_REF}',
            'GITHUB_WORKFLOW_SHA': self.controller['commit'], 'GITHUB_RUN_ID': '123',
            'GITHUB_RUN_ATTEMPT': '2', 'GITHUB_JOB': 'control'}
        self.plan = {'controller': self.controller | {'execution': {'repository': execution.REPOSITORY,
            'event_name': 'push', 'ref': execution.HOSTED_REF, 'run_id': '123', 'run_attempt': '2'}},
            'product': 'studio', 'line': '3.8', 'source': {'commit': 'd' * 40, 'tree': 'e' * 40}}
        environment = patch.dict(os.environ, self.environment, clear=True)
        environment.start()
        self.addCleanup(environment.stop)

    def create(self, role='artifact'):
        return execution.selected_execution(role, self.controller, self.plan, self.hash)

    def validate(self, value, role='artifact'):
        execution.validate_selected_execution(value, role, self.controller, self.plan, self.hash)

    def test_hosted_roles_bind_same_job_and_plan_without_provider_claim(self):
        artifact, consumer = self.create(), self.create('consumer')
        self.validate(artifact)
        self.validate(consumer, 'consumer')
        self.assertNotEqual(artifact['id'], consumer['id'])
        self.assertEqual(artifact['context'], consumer['context'])
        self.assertEqual(self.plan['source'], artifact['source'])
        self.assertEqual('runner-environment-only-provider-unverified', artifact['authority'])
        self.assertNotIn('provider_verified', artifact)
        with self.assertRaises(ValueError):
            execution.validate_local_execution(artifact)
        with self.assertRaisesRegex(ValueError, 'hosted_control_not_supported'):
            execution.local_execution()

    def test_partial_fork_pr_manual_wrong_workflow_ref_and_head_are_rejected(self):
        for key in self.environment:
            with self.subTest(missing=key), patch.dict(os.environ, self.environment, clear=True):
                del os.environ[key]
                with self.assertRaises(ValueError):
                    self.create()
        for change in ({'GITHUB_REPOSITORY': 'fork/elsa-core'}, {'GITHUB_REPOSITORY_ID': '123'},
                       {'GITHUB_EVENT_NAME': 'pull_request'}, {'GITHUB_EVENT_NAME': 'pull_request_target'},
                       {'GITHUB_EVENT_NAME': 'workflow_dispatch'}, {'GITHUB_REF': 'refs/heads/main'},
                       {'GITHUB_HEAD_REF': 'fork'}, {'GITHUB_BASE_REF': 'main'},
                       {'GITHUB_WORKFLOW_SHA': 'f' * 40}, {'GITHUB_SHA': 'f' * 40},
                       {'GITHUB_WORKFLOW_REF': 'wrong/path@' + execution.HOSTED_REF},
                       {'GITHUB_RUN_ID': '0'}, {'GITHUB_RUN_ATTEMPT': '02'}, {'GITHUB_JOB': '../job'}):
            with self.subTest(change=change), patch.dict(os.environ, change), self.assertRaises(ValueError):
                self.create()

    def test_schema_clock_role_source_controller_plan_and_context_fail_closed(self):
        original = self.create()
        mutations = [lambda x: x.update(extra=True), lambda x: x.pop('context'),
            lambda x: x.update(kind='hosted'), lambda x: x.update(id='invalid'),
            lambda x: x.update(started_at=(datetime.now(timezone.utc) + timedelta(hours=1)).isoformat()),
            lambda x: x.update(started_at=datetime.now().isoformat()),
            lambda x: x.update(role='consumer'), lambda x: x.update(controller=self.controller | {'tree': 'f' * 40}),
            lambda x: x.update(source={'commit': 'f' * 40}), lambda x: x.update(plan_sha256='f' * 64),
            lambda x: x.update(product='core'), lambda x: x.update(line='3.9'),
            lambda x: x['context'].update(run_attempt='3'),
            lambda x: x.update(authority='provider-verified')]
        for mutate in mutations:
            value = deepcopy(original)
            mutate(value)
            with self.subTest(value=value), self.assertRaises((ValueError, KeyError)):
                self.validate(value)
        self.plan['controller']['commit'] = 'f' * 40
        with self.assertRaisesRegex(ValueError, 'artifact_hosted_planner_controller'):
            self.validate(original)
        self.plan['controller']['commit'] = self.controller['commit']
        self.plan['controller']['execution']['run_id'] = '124'
        with self.assertRaisesRegex(ValueError, 'artifact_hosted_planner_execution'):
            self.validate(original)

    def test_historical_hosted_producer_admission_uses_validated_actual_start(self):
        value = self.create()
        receipt = {'schema': 1, 'mode': 'selected-product-artifact-control', 'stage': 'complete',
                   'success': True, 'artifact_proof': True, 'published': False, 'version_allocated': False,
                   'tag_created': False, 'execution': value, 'artifact_controller': self.controller,
                   'planner_controller': self.plan['controller'], 'plan_sha256': self.hash,
                   'source': self.plan['source'], 'product': 'studio', 'line': '3.8', 'version': '3.8.999'}
        plan = self.plan | {'requested_version': '3.8.999', 'observed_at': value['started_at']}
        raw = json.dumps(receipt).encode()
        with patch.object(consumers.producer, 'admit', return_value=plan) as admit:
            _, _, admission = consumers.admit_producer_stage(json.dumps(plan).encode(), self.hash,
                                                               raw, metadata.sha256(raw))
            self.assertEqual(value['started_at'], admit.call_args.kwargs['checked_at'])
            self.assertEqual('historical-producer-start-only', admission['scope'])
            self.assertNotIn('fresh_now', admission)
        receipt['execution']['context']['job'] = 'other'
        raw = json.dumps(receipt).encode()
        with patch.object(consumers.producer, 'admit') as admit, self.assertRaises(ValueError):
            consumers.admit_producer_stage(json.dumps(plan).encode(), self.hash, raw, metadata.sha256(raw))
        admit.assert_not_called()

    def test_historical_npm_checks_exact_artifact_envelope_before_any_command(self):
        value = self.create()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            with patch.object(historical.npm, 'Runner') as runner:
                with self.assertRaisesRegex(historical.npm.ProofError, 'historical-host-framework'):
                    historical.prove(root, root / 'private', root / 'retained', self.plan, value,
                                     'net9.0', self.controller, self.hash)
                altered = deepcopy(value)
                altered['context']['run_attempt'] = '3'
                with self.assertRaisesRegex(ValueError, 'artifact_hosted_binding'):
                    historical.prove(root, root / 'private', root / 'retained', self.plan, altered,
                                     'net10.0', self.controller, self.hash)
                runner.assert_not_called()
                self.assertFalse((root / 'private').exists())

    def test_hosted_consumer_cannot_promote_local_producer_receipt(self):
        receipt = {'execution': execution.local_execution({})}
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            with patch.object(consumers, 'read_bound', return_value=b'{}'), \
                    patch.object(consumers, 'admit_producer_stage', return_value=(self.plan, receipt, {})), \
                    patch.object(consumers.producer, 'verify_controller', return_value=self.controller), \
                    patch.object(consumers, 'admit_artifacts') as admit, \
                    patch.object(consumers.planner, 'build_helper') as helper:
                with self.assertRaisesRegex(ValueError, 'consumer_execution_stage_kind'):
                    consumers.execute(root, root / 'plan', self.hash, root / 'receipt', 'f' * 64,
                                      root / 'archives', root / 'snapshot', root / 'output')
                admit.assert_not_called()
                helper.assert_not_called()
                self.assertFalse((root / 'output').exists())

    def test_local_schema_remains_unchanged_and_partial_hosted_env_is_not_cleared(self):
        with patch.dict(os.environ, {}, clear=True):
            value = self.create()
            self.assertEqual({'kind', 'id', 'started_at'}, set(value))
            self.validate(value)
        for key in self.environment:
            with self.subTest(partial=key), patch.dict(os.environ, {key: self.environment[key]}, clear=True), \
                    self.assertRaises(ValueError):
                self.create()


class PrivateSnapshotTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.planning = self.root / 'planning'
        self.source = self.planning / 'source.private'
        self.project = self.source / 'src/Example/Example.csproj'
        self.project.parent.mkdir(parents=True)
        self.project.write_text('<Project/>')
        self.assets_path = self.project.parent / 'obj/project.assets.json'
        self.assets_path.parent.mkdir()
        self.assets = {'project': {'restore': {'projectPath': str(self.project)},
                                  'frameworks': {'net8.0': {}, 'net10.0': {}}},
                       'targets': {'net8.0': {'External/1.0.0': {'type': 'package'}}, 'net10.0': {}},
                       'packageFolders': {str(self.root / 'private-cache'): {}}}
        self.policy = {'id': 'Example', 'project': 'src/Example/Example.csproj',
                       'frameworks': ['net8.0', 'net10.0'], 'metadata': {}}
        self.plan = {'controller': {'commit': 'a' * 40}, 'source': {'commit': 'b' * 40},
                     'product': 'studio', 'line': '3.8', 'requested_version': '3.8.999',
                     'inventory': {'selected': [self.policy]}}
        self.plan_path = self.planning / 'plan.json'
        self.plan_path.write_text('{}')
        self.output = self.root / 'snapshot'
        self.hash = 'c' * 64
        self.refresh_assets()
        admission = patch.object(snapshots.artifacts, 'admit', return_value=self.plan)
        admission.start()
        self.addCleanup(admission.stop)

    def refresh_assets(self):
        self.raw = json.dumps(self.assets, indent=3).encode() + b'\n'
        self.assets_path.write_bytes(self.raw)
        self.policy['metadata']['restore_assets_sha256'] = metadata.sha256(self.raw)

    def run_snapshot(self):
        return snapshots.snapshot(self.plan_path, self.hash, self.output)

    def test_tiny_actual_snapshot_preserves_raw_paths_and_consumer_schema(self):
        receipt = self.run_snapshot()
        row = receipt['selected'][0]
        self.assertEqual(self.raw, (self.output / row['file']).read_bytes())
        self.assertIn(str(self.root / 'private-cache').encode(), self.raw)
        self.assertEqual({'receipt.private.json', row['file']}, {p.name for p in self.output.iterdir()})
        self.plan['controller'] = receipt['planner_controller']
        self.assertEqual(self.assets, consumers.load_snapshots(self.plan, self.hash, self.output)[self.policy['project']])
        self.assertFalse(receipt['product_build_executed'])
        self.assertFalse(receipt['publication'])
        with self.assertRaises(ValueError):
            self.run_snapshot()

    def test_wrong_plan_hash_is_admitted_before_any_output(self):
        with patch.object(snapshots.artifacts, 'admit', side_effect=ValueError('plan_hash')):
            with self.assertRaisesRegex(ValueError, 'plan_hash'):
                self.run_snapshot()
        self.assertFalse(self.output.exists())

    def test_wrong_missing_duplicate_assets_and_partition_fail_before_writes(self):
        for mode in ('wrong', 'missing', 'duplicate', 'partition'):
            with self.subTest(mode=mode):
                self.refresh_assets()
                duplicate = self.assets_path.with_name('duplicate')
                if mode == 'wrong':
                    self.policy['metadata']['restore_assets_sha256'] = 'f' * 64
                elif mode == 'missing':
                    self.assets_path.unlink()
                elif mode == 'duplicate':
                    duplicate.mkdir()
                    (duplicate / 'project.assets.json').write_bytes(self.raw)
                else:
                    self.plan['inventory']['selected'].append(deepcopy(self.policy))
                try:
                    with self.assertRaises(ValueError):
                        self.run_snapshot()
                    self.assertFalse(self.output.exists())
                finally:
                    if duplicate.exists():
                        (duplicate / 'project.assets.json').unlink()
                        duplicate.rmdir()
                    self.plan['inventory']['selected'] = [self.policy]

    def test_source_and_framework_joins_reject_foreign_relative_or_missing_project(self):
        for change in ('foreign', 'relative', 'framework', 'target', 'missing-project'):
            self.assets['project']['restore']['projectPath'] = str(self.project)
            self.assets['project']['frameworks'] = {'net8.0': {}, 'net10.0': {}}
            self.assets['targets'] = {'net8.0': {}, 'net10.0': {}}
            if change == 'foreign':
                self.assets['project']['restore']['projectPath'] = str(self.root / 'other.csproj')
            elif change == 'relative':
                self.assets['project']['restore']['projectPath'] = 'src/Example/Example.csproj'
            elif change == 'framework':
                self.assets['project']['frameworks'].pop('net10.0')
            elif change == 'target':
                self.assets['targets'].pop('net10.0')
            else:
                self.project.unlink()
            self.refresh_assets()
            with self.subTest(change=change), self.assertRaises(ValueError):
                self.run_snapshot()
            self.assertFalse(self.output.exists())

    def test_symlink_input_output_or_source_component_is_rejected(self):
        link = self.root / 'link'
        link.symlink_to(self.planning, target_is_directory=True)
        with self.assertRaisesRegex(ValueError, 'snapshot_path'):
            snapshots.snapshot(link / 'plan.json', self.hash, self.output)
        with self.assertRaisesRegex(ValueError, 'snapshot_path'):
            snapshots.snapshot(self.plan_path, self.hash, link / 'snapshot')
        self.project.unlink()
        self.project.symlink_to(self.plan_path)
        with self.assertRaisesRegex(ValueError, 'snapshot_path'):
            self.run_snapshot()
        self.assertFalse(self.output.exists())


if __name__ == '__main__':
    unittest.main()
