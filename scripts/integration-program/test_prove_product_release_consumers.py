from __future__ import annotations

from copy import deepcopy
from datetime import datetime, timedelta, timezone
import json
import subprocess
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET
import zipfile
from unittest.mock import patch

import product_release_metadata as metadata
import test_prove_product_release_artifacts as artifact_contracts
import prove_product_release_consumers as proof
from product_artifact_execution import local_execution


class Semantics:
    def call(self, operation, **values):
        if operation == 'ranges':
            return [{'satisfies': True, 'normalized': item['range']} for item in values['values']]
        if operation == 'identity':
            return []
        if operation == 'frameworks':
            return [{'compatible': True, 'nearest': item['candidates'][0]} for item in values['values']]
        raise AssertionError(operation)


class SelectedProductConsumerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        temporary = tempfile.TemporaryDirectory()
        cls.addClassCleanup(temporary.cleanup)
        cls.controller_root = Path(temporary.name).resolve()
        def git(*args):
            return subprocess.check_output(['git', *args], cwd=cls.controller_root, text=True).strip()
        git('init', '-q')
        path = cls.controller_root / 'planner.py'
        path.write_text('immutable planner input')
        git('add', '.')
        git('-c', 'user.name=Consumer Contract', '-c', 'user.email=consumer@example.invalid', 'commit', '-qm', 'planner')
        cls.planner_controller = {'commit': git('rev-parse', 'HEAD'), 'tree': git('rev-parse', 'HEAD^{tree}'),
                                 'input_sha256': {'planner.py': metadata.sha256(path.read_bytes())}}
        (cls.controller_root / 'artifact.py').write_text('distinct artifact controller')
        git('add', '.')
        git('-c', 'user.name=Consumer Contract', '-c', 'user.email=consumer@example.invalid', 'commit', '-qm', 'artifact')
        cls.artifact_controller = {'commit': git('rev-parse', 'HEAD'), 'tree': git('rev-parse', 'HEAD^{tree}')}

    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.artifacts = self.root / 'archives'
        self.artifacts.mkdir()
        self.hash = 'a' * 64
        self.source = {'commit': 'b' * 40, 'tree': 'c' * 40}
        self.policy = {'id': 'Example', 'project': 'src/Example.csproj', 'frameworks': ['net8.0', 'net10.0'],
                       'metadata': {'dependency_groups': [{'framework': 'net8.0', 'dependencies': []}]}}
        self.plan = {'observed_at': proof.planner.now(), 'source': self.source, 'controller': deepcopy(self.planner_controller), 'product': 'studio', 'line': '3.8',
                     'requested_version': '3.8.999', 'inventory': {'selected': [self.policy]},
                     'expected_artifacts': ['Example.3.8.999.nupkg']}
        self.graph = {'example': {'id': 'Example', 'version': '3.8.999', 'content_hash': 'A' * 86 + '==',
                                 'dependencies': {}, 'selected': True},
                      'external': {'id': 'External', 'version': '1.2.3', 'content_hash': 'B' * 86 + '==',
                                   'dependencies': {}, 'selected': False}}
        self.feeds = {'sources': {'original': proof.planner.NUGET_INDEX},
                      'mapping': {'example': ['original'], 'external': ['original']}}

    def artifact_receipt(self, entries=None, *, framework_references=''):
        path = self.artifacts / self.plan['expected_artifacts'][0]
        identifier = self.policy['id']
        with zipfile.ZipFile(path, 'w') as archive:
            archive.writestr(identifier + '.nuspec', '<package><metadata><id>' + identifier + '</id><version>3.8.999</version>'
                '<repository commit="' + self.source['commit'] + '"/><dependencies><group targetFramework="net8.0"/>'
                '</dependencies>' + framework_references + '</metadata></package>')
            for name, data in (entries or {}).items():
                archive.writestr(name, data)
        with zipfile.ZipFile(path) as archive:
            inventory = [{'path': name, 'size': len(archive.read(name)), 'sha256': metadata.sha256(archive.read(name))}
                         for name in sorted(archive.namelist())]
        return {'schema': 1, 'mode': 'selected-product-artifact-control', 'stage': 'complete',
                'version_allocated': False, 'tag_created': False, 'success': True, 'artifact_proof': True, 'published': False, 'plan_sha256': self.hash,
                'source': self.source, 'product': 'studio', 'line': '3.8', 'version': '3.8.999',
                'execution': local_execution({}), 'planner_controller': deepcopy(self.planner_controller),
                'artifact_controller': deepcopy(self.artifact_controller), 'packages': {'selected': [{'file': path.name, 'id': identifier,
                'version': '3.8.999', 'sha256': metadata.sha256(path.read_bytes()), 'size': path.stat().st_size,
                'inventory': inventory}]}}

    def historical_inputs(self):
        fixture = artifact_contracts.ProductArtifactAdmissionTests()
        fixture.setUp()
        plan = fixture.plan
        observed = datetime.now(timezone.utc) - timedelta(hours=2)
        plan['observed_at'] = observed.isoformat()
        for row in plan['histories']:
            row['observation']['observed_at'] = observed.isoformat()
        data = json.dumps(plan).encode()
        digest = metadata.sha256(data)
        receipt = self.artifact_receipt()
        receipt.update(source=plan['source'], planner_controller=plan['controller'],
                       plan_sha256=digest, version=plan['requested_version'])
        receipt['execution']['started_at'] = (observed + timedelta(minutes=5)).isoformat()
        return data, digest, receipt

    def stage(self, data, digest, receipt, receipt_hash=None):
        raw = json.dumps(receipt).encode()
        return proof.admit_producer_stage(data, digest, raw, receipt_hash or metadata.sha256(raw))

    def test_expired_plan_is_historical_only_at_bound_successful_producer_start(self):
        data, digest, receipt = self.historical_inputs()
        with self.assertRaisesRegex(ValueError, 'plan_observation_stale'):
            proof.producer.admit(data, digest)
        plan, bound, admission = self.stage(data, digest, receipt)
        self.assertEqual(receipt, bound)
        self.assertEqual(json.loads(data), plan)
        self.assertEqual('historical-producer-start-only', admission['scope'])
        self.assertEqual(receipt['execution']['started_at'], admission['producer_started_at'])
        self.assertNotIn('fresh_now', admission)

    def test_producer_start_cannot_be_future_invalid_stale_or_before_plan(self):
        data, digest, original = self.historical_inputs()
        observed = datetime.fromisoformat(json.loads(data)['observed_at'])
        for started in ((datetime.now(timezone.utc) + timedelta(minutes=1)).isoformat(), 'invalid',
                        (observed + timedelta(seconds=proof.planner.MAX_AGE_SECONDS + 1)).isoformat(),
                        (observed - timedelta(seconds=1)).isoformat(), observed.replace(tzinfo=None).isoformat()):
            receipt = deepcopy(original)
            receipt['execution']['started_at'] = started
            with self.subTest(started=started), self.assertRaises(ValueError):
                self.stage(data, digest, receipt)

    def test_historical_stage_requires_unchanged_successful_receipt_identity(self):
        data, digest, original = self.historical_inputs()
        receipt_hash = metadata.sha256(json.dumps(original).encode())
        altered = deepcopy(original)
        altered['execution']['started_at'] = proof.planner.now()
        with self.assertRaisesRegex(ValueError, 'consumer_input_hash'):
            self.stage(data, digest, altered, receipt_hash)
        for mutate in (lambda p: p.update(success=False), lambda p: p.update(stage='setup-complete'),
                       lambda p: p.update(plan_sha256='f' * 64), lambda p: p.update(source={'commit': 'f' * 40}),
                       lambda p: p.update(planner_controller={'commit': 'f' * 40}), lambda p: p.update(version_allocated=True)):
            receipt = deepcopy(original)
            mutate(receipt)
            with self.assertRaises(ValueError):
                self.stage(data, digest, receipt)

    def test_exact_archive_receipt_admitted(self):
        result = proof.admit_artifacts(self.plan, self.hash, self.artifact_receipt(), self.artifacts, self.controller_root)
        self.assertEqual({'example'}, set(result))

    def test_native_framework_references_must_match_plan_before_fixture_can_supply_them(self):
        self.policy['metadata']['framework_reference_groups'] = [
            {'framework': 'net8.0', 'references': ['Microsoft.AspNetCore.App']}]
        valid = ('<frameworkReferences><group targetFramework="net8.0">'
                 '<frameworkReference name="Microsoft.AspNetCore.App"/></group></frameworkReferences>')
        receipt = self.artifact_receipt(framework_references=valid)
        self.assertEqual({'example'}, set(proof.admit_artifacts(
            self.plan, self.hash, receipt, self.artifacts, self.controller_root)))
        for actual in ('', valid.replace('net8.0', 'net9.0'),
                       valid.replace('Microsoft.AspNetCore.App', 'Microsoft.NETCore.App'),
                       valid.replace('</frameworkReferences>', '<group targetFramework="net9.0">'
                           '<frameworkReference name="Microsoft.AspNetCore.App"/></group></frameworkReferences>')):
            receipt = self.artifact_receipt(framework_references=actual)
            with self.subTest(actual=actual), self.assertRaisesRegex(ValueError, 'consumer_archive_framework_references'):
                proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts, self.controller_root)
        self.policy['metadata']['framework_reference_groups'] = []
        receipt = self.artifact_receipt(framework_references=valid)
        with self.assertRaisesRegex(ValueError, 'consumer_archive_framework_references'):
            proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts, self.controller_root)

    def test_producer_controller_git_objects_and_planner_inputs_bound(self):
        original = self.artifact_receipt()
        proof.verify_producer_controllers(self.controller_root, self.plan, original)
        self.assertNotEqual(original['planner_controller']['commit'], original['artifact_controller']['commit'])
        for mutate, code in (
                (lambda p: p.pop('planner_controller'), 'consumer_planner_controller_identity'),
                (lambda p: p['planner_controller'].update(commit='e' * 40), 'consumer_planner_controller_identity'),
                (lambda p: p.pop('artifact_controller'), 'consumer_artifact_controller_identity'),
                (lambda p: p['artifact_controller'].update(tree='e' * 40), 'consumer_producer_controller_tree')):
            receipt = deepcopy(original)
            mutate(receipt)
            with self.subTest(code=code), self.assertRaisesRegex(ValueError, code):
                proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts, self.controller_root)
        with patch.object(proof.producer.maintenance, 'git_bytes', return_value=b'changed planner input'):
            with self.assertRaisesRegex(ValueError, 'consumer_producer_planner_inputs'):
                proof.admit_artifacts(self.plan, self.hash, original, self.artifacts, self.controller_root)

    def test_changed_archive_and_wrong_source_receipt_rejected(self):
        receipt = self.artifact_receipt()
        path = self.artifacts / self.plan['expected_artifacts'][0]
        path.write_bytes(path.read_bytes() + b'changed')
        with self.assertRaisesRegex(ValueError, 'consumer_input_hash'):
            proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts, self.controller_root)
        receipt['source'] = {'commit': 'e' * 40}
        with self.assertRaisesRegex(ValueError, 'consumer_producer_identity'):
            proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts, self.controller_root)

    def test_private_or_duplicate_archives_cannot_enter_selection(self):
        receipt = self.artifact_receipt()
        (self.artifacts / 'Excluded.3.8.999.nupkg').write_bytes(b'excluded')
        with self.assertRaisesRegex(ValueError, 'consumer_artifact_bijection'):
            proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts, self.controller_root)
        receipt['packages']['selected'].append(receipt['packages']['selected'][0])
        with self.assertRaisesRegex(ValueError, 'consumer_artifact_bijection'):
            proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts, self.controller_root)

    def snapshot(self):
        folder = self.root / 'snapshots'
        folder.mkdir()
        raw = json.dumps({'targets': {'net8.0': {}, 'net10.0': {}}}).encode()
        digest = metadata.sha256(raw)
        self.policy['metadata']['restore_assets_sha256'] = digest
        (folder / (digest + '.json')).write_bytes(raw)
        receipt = {'schema': 1, 'mode': 'private-original-planning-assets-snapshot', 'plan_sha256': self.hash,
                   'planner_controller': self.plan['controller'], 'source': self.source, 'product': 'studio', 'line': '3.8',
                   'version': '3.8.999', 'product_build_executed': False, 'publication': False,
                   'selected': [{'id': 'Example', 'project': self.policy['project'], 'frameworks': self.policy['frameworks'],
                                 'sha256': digest, 'bytes': len(raw), 'file': digest + '.json',
                                 'resolved_targets': {'untrusted-derived-data': True}}]}
        path = folder / 'receipt.private.json'
        path.write_text(json.dumps(receipt))
        return folder, receipt

    def test_snapshot_uses_raw_hash_and_ignores_derived_targets(self):
        folder, _ = self.snapshot()
        result = proof.load_snapshots(self.plan, self.hash, folder)
        self.assertEqual({'net8.0': {}, 'net10.0': {}}, result[self.policy['project']]['targets'])

    def test_wrong_snapshot_hash_identity_path_and_partition_rejected(self):
        folder, original = self.snapshot()
        for mutate in (lambda p: p.update(plan_sha256='f' * 64), lambda p: p['selected'][0].update(project='wrong'),
                       lambda p: p['selected'][0].update(file='../escape.json'), lambda p: p['selected'].clear(),
                       lambda p: p['selected'][0].update(sha256='f' * 64)):
            receipt = deepcopy(original)
            mutate(receipt)
            (folder / 'receipt.private.json').write_text(json.dumps(receipt))
            with self.assertRaises(ValueError):
                proof.load_snapshots(self.plan, self.hash, folder)

    def test_symbolic_snapshot_file_rejected(self):
        folder, receipt = self.snapshot()
        row = receipt['selected'][0]
        original = folder / row['file']
        real = folder / 'original.json'
        original.rename(real)
        original.symlink_to(real)
        with self.assertRaisesRegex(ValueError, 'consumer_input_path'):
            proof.load_snapshots(self.plan, self.hash, folder)

    def native_graph(self):
        selected = {'example': {'id': 'Example', 'content_hash': self.graph['example']['content_hash'],
            'dependency_groups': [{'framework': 'net8.0', 'dependencies': [{'id': 'External', 'version': '[1.2.3, )'}]}]}}
        catalog = {('external', '1.2.3'): {'id': 'External', 'content_hash': self.graph['external']['content_hash'],
                    'groups': [{'framework': 'net8.0', 'dependencies': []}]}}
        targets, libraries, locked = {}, {}, {}
        for folded, row in self.graph.items():
            deps = {'External': '[1.2.3, )'} if folded == 'example' else {}
            key = row['id'] + '/' + row['version']
            targets[key] = {'type': 'package', 'dependencies': deps}
            libraries[key] = {'type': 'package', 'sha512': row['content_hash']}
            locked[row['id']] = {'resolved': row['version'], 'contentHash': row['content_hash'],
                'type': 'Direct' if folded == 'example' else 'Transitive', 'dependencies': deps}
        locked['Example']['requested'] = '[3.8.999, 3.8.999]'
        return selected, catalog, {'targets': {'net8.0': targets}, 'libraries': libraries}, {
            'version': 1, 'dependencies': {'net8.0': locked}}

    def test_native_graph_uses_full_nuspec_edges_and_one_exact_direct_root(self):
        selected, catalog, assets, lock = self.native_graph()
        graph = proof.resolution.audit_native_graph(assets, lock, 'Example', 'net8.0', selected,
                                                     catalog, '3.8.999', Semantics())
        self.assertEqual({'example', 'external'}, set(graph))
        self.assertEqual({'External': '[1.2.3, )'}, graph['example']['dependencies'])

    def test_native_graph_rejects_unreviewed_versions_hashes_missing_edges_and_extra_directs(self):
        selected, catalog, original, original_lock = self.native_graph()
        for mutate, code in (
                (lambda a, l: a['targets']['net8.0'].update({'External/9.9.9': {'type': 'package'}}), 'consumer_native'),
                (lambda a, l: a['libraries']['External/1.2.3'].update(sha512='changed'), 'consumer_native_content_hash'),
                (lambda a, l: a['targets']['net8.0']['Example/3.8.999']['dependencies'].clear(), 'consumer_native_declared_edges'),
                (lambda a, l: l['dependencies']['net8.0']['External'].update(type='Direct'), 'consumer_native_lock_identity'),
                (lambda a, l: l['dependencies']['net8.0']['Example'].update(requested='[3.8.999, )'), 'consumer_native_root_range')):
            assets, lock = deepcopy(original), deepcopy(original_lock)
            mutate(assets, lock)
            with self.subTest(code=code), self.assertRaises((ValueError, KeyError)):
                proof.resolution.audit_native_graph(assets, lock, 'Example', 'net8.0', selected,
                                                     catalog, '3.8.999', Semantics())
        with patch.object(Semantics, 'call', side_effect=lambda op, **kw:
                [{'normalized': '[3.8.999, 3.8.999]', 'satisfies': False}] if op == 'ranges' else
                [{'compatible': True, 'nearest': item['candidates'][0]} for item in kw['values']]):
            with self.assertRaisesRegex(ValueError, 'consumer_dependency_range_conflict'):
                proof.resolution.audit_native_graph(original, original_lock, 'Example', 'net8.0', selected,
                                                     catalog, '3.8.999', Semantics())

    def test_remote_mapping_never_contains_selected_ids_or_wildcard(self):
        config = ET.fromstring(proof.render_config(self.artifacts, self.graph, self.feeds))
        groups = {row.get('key'): [item.get('pattern') for item in row] for row in config.find('packageSourceMapping')}
        self.assertEqual(['Example'], groups[proof.LOCAL])
        self.assertEqual(['External'], groups['original'])
        self.assertNotIn('*', str(groups))

    def restored(self):
        root = self.root / 'consumer'
        cache = root / 'packages'
        assets = {'targets': {'net8.0': {row['id'] + '/' + row['version']: {'type': 'package'} for row in self.graph.values()}},
                  'libraries': {row['id'] + '/' + row['version']: {'type': 'package', 'sha512': row['content_hash']} for row in self.graph.values()},
                  'packageFolders': {str(cache): {}}, 'project': {'restore': {
                      'sources': {proof.planner.NUGET_INDEX: {}, str(self.artifacts): {}},
                      'configFilePaths': [str(root / 'NuGet.Config')]}}}
        return root, cache, assets

    def test_exact_restored_closure_accepted(self):
        root, cache, assets = self.restored()
        proof.validate_restored(assets, 'net8.0', self.graph, root, cache, self.artifacts, self.feeds)

    def test_restore_rejects_wrong_version_project_fallback_and_inherited_sources(self):
        root, cache, original = self.restored()
        for mutate in (lambda p: p['targets']['net8.0']['Example/3.8.999'].update(type='project'),
                       lambda p: p['targets']['net8.0'].update({'External/9.9.9': {'type': 'package'}}),
                       lambda p: p['project']['restore']['sources'].update({'https://wrong.invalid/': {}}),
                       lambda p: p['project']['restore'].update(fallbackFolders=['/shared/cache']),
                       lambda p: p['libraries']['External/1.2.3'].update(sha512='wrong')):
            assets = deepcopy(original)
            mutate(assets)
            with self.assertRaises(ValueError):
                proof.validate_restored(assets, 'net8.0', self.graph, root, cache, self.artifacts, self.feeds)

    def test_cache_requires_selected_original_archive_and_local_origin(self):
        receipt = self.artifact_receipt()
        selected = proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts, self.controller_root)
        source = self.artifacts / self.plan['expected_artifacts'][0]
        graph = {'example': self.graph['example'] | {'content_hash': selected['example']['content_hash']}}
        cache = self.root / 'packages'
        folder = cache / 'example/3.8.999'
        folder.mkdir(parents=True)
        archive = folder / source.name.lower()
        archive.write_bytes(source.read_bytes())
        archive.with_suffix('.nupkg.sha512').write_text(selected['example']['content_hash'])
        metadata_path = folder / '.nupkg.metadata'
        metadata_path.write_text(json.dumps({'source': str(self.artifacts)}))
        proof.verify_cache(graph, self.feeds, cache, self.artifacts, selected, {}, self.root / "inspector.dll", {})
        metadata_path.write_text(json.dumps({'source': proof.planner.NUGET_INDEX}))
        with self.assertRaisesRegex(RuntimeError, 'Unexpected package source'):
            proof.verify_cache(graph, self.feeds, cache, self.artifacts, selected, {}, self.root / "inspector.dll", {})
        metadata_path.write_text(json.dumps({'source': str(self.artifacts)}))
        archive.write_bytes(b'wrong bytes')
        with self.assertRaisesRegex(RuntimeError, 'exact verified nupkg'):
            proof.verify_cache(graph, self.feeds, cache, self.artifacts, selected, {}, self.root / "inspector.dll", {})

    def test_signed_external_native_content_hash_is_separate_from_raw_archive_hash(self):
        cache = self.root / 'external-cache'
        folder = cache / 'external/1.2.3'
        folder.mkdir(parents=True)
        archive = folder / 'external.1.2.3.nupkg'
        with zipfile.ZipFile(archive, 'w') as package:
            package.writestr('.signature.p7s', b'synthetic signature; native inspector is mocked')
            package.writestr('External.nuspec', b'original metadata')
        raw_hash = metadata.sha256(archive.read_bytes())
        native_hash = self.graph['external']['content_hash']
        (folder / 'external.1.2.3.nupkg.sha512').write_text(proof.consumers.base64_sha512(archive.read_bytes()))
        cache_metadata = folder / '.nupkg.metadata'
        cache_metadata.write_text(json.dumps({'source': proof.planner.NUGET_INDEX}))
        graph = {'external': self.graph['external']}
        assets = {'libraries': {'External/1.2.3': {'type': 'package', 'sha512': native_hash}}, 'packageFolders': {str(cache): {}}}
        catalog = {('external', '1.2.3'): {'archive_sha256': raw_hash}}
        def inspect(*args, **kwargs):
            self.assertEqual(['dotnet', str(self.root / 'inspector.dll'), '--inspect-archive', str(archive)], args[0])
            return json.dumps({'signed': True, 'content_hash': native_hash, 'archive_sha256': raw_hash})
        with patch.object(proof.archives, 'run', side_effect=inspect):
            result = proof.verify_cache(graph, self.feeds, cache, self.artifacts, {}, assets, self.root / 'inspector.dll', catalog)
            self.assertEqual(native_hash, result[0]['nuget_content_hash'])
            self.assertNotEqual(native_hash, proof.consumers.base64_sha512(archive.read_bytes()))
            cache_metadata.write_text(json.dumps({'source': 'https://wrong.invalid/'}))
            with self.assertRaisesRegex(ValueError, 'consumer_external_cache_source'):
                proof.verify_cache(graph, self.feeds, cache, self.artifacts, {}, assets, self.root / 'inspector.dll', catalog)
            catalog[('external', '1.2.3')]['archive_sha256'] = 'f' * 64
            with self.assertRaisesRegex(ValueError, 'consumer_external_archive_hash'):
                proof.verify_cache(graph, self.feeds, cache, self.artifacts, {}, assets, self.root / 'inspector.dll', catalog)
        with patch.object(proof.archives, 'run', return_value=json.dumps({'signed': True, 'content_hash': 'wrong', 'archive_sha256': raw_hash})):
            with self.assertRaisesRegex(ValueError, 'differs from restored dependency hash'):
                proof.verify_cache(graph, self.feeds, cache, self.artifacts, {}, assets, self.root / 'inspector.dll', catalog)

    def catalog_inputs(self):
        cache = self.root / 'original-cache'
        archive = cache / 'external/1.2.3/external.1.2.3.nupkg'
        archive.parent.mkdir(parents=True)
        with zipfile.ZipFile(archive, 'w') as package:
            package.writestr('External.nuspec', '<package><metadata><id>External</id><version>1.2.3</version></metadata></package>')
        assets = {'targets': {'net8.0': {'External/1.2.3': {'type': 'package'}}},
            'libraries': {'External/1.2.3': {'type': 'package',
            'sha512': proof.consumers.base64_sha512(archive.read_bytes())}}, 'packageFolders': {str(cache): {}}}
        native = {'id': 'External', 'version': '1.2.3', 'groups': []}
        feeds = {'sources': [{'name': 'original', 'url': proof.planner.NUGET_INDEX}],
                 'packages': [{'id': 'External', 'sources': ['original']}]}
        semantics = Semantics()
        semantics.call = lambda op, **kw: [native] if op == 'nuspecs' else feeds
        return archive, assets, semantics, feeds

    def test_offline_catalog_freezes_original_bytes_and_preserves_original_source_mapping(self):
        archive, assets, semantics, _ = self.catalog_inputs()
        restored_archive = proof.archives.restored_archive
        def verify_frozen(frozen, *args, **kwargs):
            self.assertEqual({str(self.root / 'catalog/archives')}, set(frozen['packageFolders']))
            return restored_archive(frozen, *args, **kwargs)
        with patch.object(proof.archives, 'run', side_effect=AssertionError('process forbidden for unsigned metadata')), \
                patch.object(proof.archives, 'restored_archive', side_effect=verify_frozen):
            catalog, policy = proof.resolution.archive_catalog({'project': assets}, {}, self.root / 'config',
                semantics, self.root / 'inspector.dll', self.root / 'catalog')
        row = catalog[('external', '1.2.3')]
        self.assertEqual(metadata.sha256(archive.read_bytes()), row['archive_sha256'])
        self.assertEqual(archive.read_bytes(), row['archive'].read_bytes())
        self.assertEqual({'original': proof.planner.NUGET_INDEX}, policy['sources'])
        self.assertEqual(archive.read_bytes(), (Path(policy['mirrors']['original']) / archive.name).read_bytes())
        archive.write_bytes(b'ambient cache changed after freeze')
        self.assertNotEqual(archive.read_bytes(), row['archive'].read_bytes())

    def test_offline_catalog_rejects_changed_original_bytes_and_unmapped_source(self):
        archive, assets, semantics, feeds = self.catalog_inputs()
        original = archive.read_bytes()
        with zipfile.ZipFile(archive, 'a') as package:
            package.writestr('changed', b'changed original bytes')
        with self.assertRaisesRegex(ValueError, 'differs from restored dependency hash'):
            proof.resolution.archive_catalog({'project': assets}, {}, self.root / 'config',
                semantics, self.root / 'inspector.dll', self.root / 'changed-catalog')
        archive.write_bytes(original)
        feeds['packages'][0]['sources'] = []
        with self.assertRaisesRegex(ValueError, 'consumer_external_feed_mapping'):
            proof.resolution.archive_catalog({'project': assets}, {}, self.root / 'config',
                semantics, self.root / 'inspector.dll', self.root / 'unmapped-catalog')

    def test_original_archive_candidates_and_conflicting_context_hashes_fail_closed(self):
        archive, assets, semantics, _ = self.catalog_inputs()
        extra = self.root / 'other-cache'
        duplicate = extra / 'external/1.2.3' / archive.name
        duplicate.parent.mkdir(parents=True)
        duplicate.write_bytes(archive.read_bytes())
        assets['packageFolders'][str(extra)] = {}
        with self.assertRaisesRegex(ValueError, 'consumer_original_archive_candidates'):
            proof.resolution.archive_catalog({'project': assets}, {}, self.root / 'config',
                semantics, self.root / 'inspector.dll', self.root / 'ambiguous-catalog')
        del assets['packageFolders'][str(extra)]
        conflicting = deepcopy(assets)
        conflicting['libraries']['External/1.2.3']['sha512'] = 'different original context hash'
        with self.assertRaisesRegex(ValueError, 'consumer_original_archive_ambiguity'):
            proof.resolution.archive_catalog({'first': assets, 'second': conflicting}, {}, self.root / 'config',
                semantics, self.root / 'inspector.dll', self.root / 'conflicting-catalog')

    def test_native_semantics_and_inspector_runtime_are_plan_bound(self):
        assembly = self.root / 'NuGet.Packaging.dll'
        assembly.write_bytes(b'exact original SDK assembly')
        identity = [{'name': 'NuGet.Packaging', 'sha256': metadata.sha256(assembly.read_bytes())}]
        plan = {'semantics': {'sdk_version': metadata.SDK, 'assemblies': identity}}
        semantics = Semantics()
        semantics.call = lambda operation: identity
        proof.resolution.validate_native_tools(plan, semantics, self.root / 'inspector.dll')
        assembly.write_bytes(b'changed SDK assembly')
        with self.assertRaisesRegex(ValueError, 'consumer_native_inspector_identity'):
            proof.resolution.validate_native_tools(plan, semantics, self.root / 'inspector.dll')
        semantics.call = lambda operation: []
        with self.assertRaisesRegex(ValueError, 'consumer_native_semantics_identity'):
            proof.resolution.validate_native_tools(plan, semantics, self.root / 'inspector.dll')

    def test_extracted_compile_content_and_runtime_payloads_join_archive_bytes(self):
        archive = self.artifacts / 'Example.3.8.999.nupkg'
        entries = {'lib/net8.0/Example.dll': b'genuine assembly', 'contentFiles/any/any/site.css': b'genuine content'}
        with zipfile.ZipFile(archive, 'w') as package:
            for name, data in entries.items():
                package.writestr(name, data)
        cache = self.root / 'packages'
        for name, data in entries.items():
            path = cache / 'example/3.8.999' / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
        assets = {'targets': {'net8.0': {'Example/3.8.999': {'compile': {'lib/net8.0/Example.dll': {}},
                   'contentFiles': {'contentFiles/any/any/site.css': {}}}}}}
        selected = {'example': {'nupkg': archive.name}}
        proof.verify_asset_payloads(assets, 'net8.0', self.graph, cache, self.artifacts, selected)
        (cache / 'example/3.8.999/lib/net8.0/Example.dll').write_bytes(b'mutated extracted DLL')
        with self.assertRaisesRegex(ValueError, 'consumer_input_hash'):
            proof.verify_asset_payloads(assets, 'net8.0', self.graph, cache, self.artifacts, selected)

    def run_private_consumer(self, freshness_error=None):
        self.policy.update(id='Elsa.Studio.Core', frameworks=['net8.0'])
        self.policy['metadata'].update(original_output_policy={'net8.0': {'IncludeBuildOutput': 'true'}},
                                       framework_reference_groups=[])
        self.plan['expected_artifacts'] = ['Elsa.Studio.Core.3.8.999.nupkg']
        self.plan['semantics'] = {'sdk_version': metadata.SDK, 'assemblies': []}
        self.plan['consumer_feed_policy'] = {'config_sha256': metadata.sha256(b'original config')}
        plan_path = self.root / 'plan.json'
        plan_path.write_text(json.dumps(self.plan))
        self.hash = metadata.sha256(plan_path.read_bytes())
        entry, dll = 'lib/net8.0/Elsa.Studio.Core.dll', b'exact archived runtime bytes'
        receipt = self.artifact_receipt({entry: dll})
        receipt_path = self.root / 'producer.json'
        receipt_path.write_text(json.dumps(receipt))
        output = self.root / 'proof'

        commands = []
        def command(args, cwd, environment, log, timeout):
            commands.append(args[1])
            if args[1] == 'restore':
                cache = cwd / 'packages/elsa.studio.core/3.8.999'
                cache.mkdir(parents=True)
                source = self.artifacts / self.plan['expected_artifacts'][0]
                (cache / source.name.lower()).write_bytes(source.read_bytes())
                (cache / (source.name.lower() + '.sha512')).write_text(proof.consumers.base64_sha512(source.read_bytes()))
                (cache / '.nupkg.metadata').write_text(json.dumps({'source': str(self.artifacts)}))
                asset = cache / entry
                asset.parent.mkdir(parents=True)
                asset.write_bytes(dll)
                assets = {'targets': {'net8.0': {'Elsa.Studio.Core/3.8.999': {
                    'type': 'package', 'runtime': {entry: {}}, 'compile': {entry: {}}}}},
                    'libraries': {'Elsa.Studio.Core/3.8.999': {'type': 'package',
                        'sha512': proof.consumers.base64_sha512(source.read_bytes())}},
                    'packageFolders': {str(cwd / 'packages'): {}}, 'project': {'restore': {
                        'sources': {item.get('value'): {} for item in ET.parse(cwd / 'NuGet.Config').findall('packageSources/add')},
                        'configFilePaths': [str(cwd / 'NuGet.Config')]}}}
                (cwd / 'obj').mkdir()
                (cwd / 'obj/project.assets.json').write_text(json.dumps(assets))
                if not (cwd / 'packages.lock.json').exists():
                    (cwd / 'packages.lock.json').write_text(json.dumps({'version': 1, 'dependencies': {'net8.0': {'Elsa.Studio.Core': {
                        'type': 'Direct', 'requested': '[3.8.999, 3.8.999]', 'resolved': '3.8.999',
                        'contentHash': proof.consumers.base64_sha512(source.read_bytes())}}}}))
            elif args[1] == 'run':
                location = cwd / 'bin/Release/net8.0/Elsa.Studio.Core.dll'
                location.parent.mkdir(parents=True)
                location.write_bytes(dll)
                runtime = {'backendUriPreserved': True, 'loadedAssemblies': [{
                    'name': 'Elsa.Studio.Core', 'version': '3.8.999.0',
                    'fullName': 'Elsa.Studio.Core, Version=3.8.999.0, Culture=neutral',
                    'informationalVersion': '3.8.999+' + self.source['commit'],
                    'sha256': metadata.sha256(dll), 'location': str(location), 'unexpectedPrivatePath': str(cwd)}]}
                log.write_text('SELECTED_CONSUMER_PROOF=' + json.dumps(runtime))
            return {'exit_code': 0}

        real_git_bytes = proof.producer.maintenance.git_bytes
        def git_bytes(root, commit, path):
            return b'original config' if path == 'NuGet.Config' else real_git_bytes(root, commit, path)

        def catalog(*args):
            args[-1].mkdir()
            (args[-1] / 'catalog.private.json').write_text('{}')
            return {}, {'sources': self.feeds['sources'], 'mapping': {'elsa.studio.core': []},
                        'mirrors': {'original': str(self.root / 'original-mirror')}}

        with patch.object(proof.producer, 'admit', return_value=self.plan), \
                patch.object(proof.producer, 'verify_controller', return_value={'commit': 'f' * 40, 'tree': 'e' * 40}), \
                patch.object(proof.producer.maintenance, 'git_bytes', side_effect=git_bytes), \
                patch.object(proof, 'load_snapshots', return_value={self.policy['project']: {'targets': {'net8.0': {}}, 'libraries': {}}}), \
                patch.dict(proof.STUDIO38_CONTRACT_SOURCE, {}, clear=True), \
                patch.object(proof.planner, 'build_helper', return_value=Semantics()), \
                patch.object(proof.producer, 'refresh_remote', side_effect=freshness_error, return_value={
                    'checked_at': proof.planner.now(), 'eligible': True, 'histories': [{'eligible': True}], 'prerequisites': []}), \
                patch.object(proof.resolution, 'build_inspector', return_value=self.root / 'inspector.dll') as inspector_mock, \
                patch.object(proof.resolution, 'archive_catalog', side_effect=catalog) as catalog_mock, \
                patch.object(proof.consumers, '_run_command', side_effect=command):
            def execute():
                return proof.execute(self.controller_root, plan_path, self.hash, receipt_path,
                    metadata.sha256(receipt_path.read_bytes()), self.artifacts, self.root / 'unused', output)
            if freshness_error:
                with self.assertRaisesRegex(ValueError, str(freshness_error)):
                    execute()
                self.assertFalse(inspector_mock.called)
                self.assertFalse(catalog_mock.called)
                self.assertEqual([], commands)
            else:
                execute()
        data = (output / 'retained/receipt.json').read_text()
        return json.loads(data), data, output, dll

    def test_current_ineligible_prerequisites_block_all_consumer_and_inspector_work(self):
        receipt, _, _, _ = self.run_private_consumer(ValueError('artifact_fresh_prerequisite_failed'))
        self.assertFalse(receipt['success'])
        self.assertFalse(receipt['current_consumer_admission']['eligible'])
        self.assertEqual('current-consumer-prerequisites', receipt['stage'])

    def test_current_prerequisite_metadata_drift_blocks_consumer_work(self):
        receipt, _, _, _ = self.run_private_consumer(ValueError('artifact_prerequisite_metadata_changed'))
        self.assertFalse(receipt['success'])
        self.assertFalse(receipt['current_consumer_admission']['eligible'])
        self.assertEqual('current-consumer-prerequisites', receipt['stage'])

    def test_full_retained_receipt_excludes_private_runtime_and_archive_paths(self):
        retained, data, output, dll = self.run_private_consumer()
        self.assertTrue(retained['success'])
        self.assertEqual('historical-producer-start-only', retained['producer_plan_admission']['scope'])
        self.assertTrue(retained['current_consumer_admission']['eligible'])
        self.assertEqual('current-complete-selected-product-prerequisites', retained['current_consumer_admission']['scope'])
        self.assertEqual(self.artifact_controller, retained['artifact_controller'])
        self.assertNotIn(str(self.root), data)
        self.assertNotIn('location', data)
        self.assertNotIn('unexpectedPrivatePath', data)
        self.assertEqual(proof.LOCAL, retained['coverage'][0]['restored'][0]['source'])
        self.assertEqual(metadata.sha256(dll), retained['runtime'][0]['runtime']['loaded_assemblies'][0]['sha256'])
        self.assertIn(str(output), (output / 'private/runtime-net8.0/runtime.log').read_text())

    def test_coverage_ledger_requires_every_selected_tfm_once_and_success(self):
        valid = [{'id': 'Example', 'framework': framework, 'success': True} for framework in self.policy['frameworks']]
        proof.validate_ledger(self.plan, valid)
        for invalid in (valid[:1], valid + valid[:1], [valid[0], valid[1] | {'success': False}]):
            with self.assertRaisesRegex(ValueError, 'consumer_coverage_ledger'):
                proof.validate_ledger(self.plan, invalid)

    def test_compile_witness_has_exact_single_package_and_no_projectreference(self):
        project = ET.fromstring(proof.render_project('Example', '3.8.999', 'net8.0', ['Microsoft.AspNetCore.App'], executable=False, managed=True))
        packages = project.findall('ItemGroup/PackageReference')
        self.assertEqual(1, len(packages))
        self.assertEqual({'Include': 'Example', 'Version': '[3.8.999]', 'Aliases': 'selected'}, packages[0].attrib)
        self.assertEqual([], project.findall('.//ProjectReference'))
        self.assertEqual('true', project.findtext('PropertyGroup/RestoreLockedMode'))


if __name__ == '__main__':
    unittest.main()
