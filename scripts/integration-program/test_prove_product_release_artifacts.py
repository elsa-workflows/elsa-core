from __future__ import annotations

from copy import deepcopy
from datetime import datetime, timedelta, timezone
import fnmatch
import json
from pathlib import Path
import tempfile
import unittest
import zipfile
from unittest.mock import patch

import plan_product_release as planner
import product_release_metadata as metadata
import prove_product_release_artifacts as artifacts
import prove_historical_studio_npm_pair as historical


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
                    artifacts.execute(Path(directory), b'{}', 'f' * 64, output, '1', '1')
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


if __name__ == '__main__':
    unittest.main()
