from __future__ import annotations

from copy import deepcopy
import json
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET
import zipfile

import product_release_metadata as metadata
import prove_product_release_consumers as proof
from product_artifact_execution import local_execution


class Semantics:
    def call(self, operation, **values):
        if operation == 'ranges':
            return [{'satisfies': True} for _ in values['values']]
        if operation == 'frameworks':
            return [{'compatible': True, 'nearest': item['candidates'][0]} for item in values['values']]
        raise AssertionError(operation)


class SelectedProductConsumerTests(unittest.TestCase):
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
        self.plan = {'source': self.source, 'controller': {'commit': 'd' * 40}, 'product': 'studio', 'line': '3.8',
                     'requested_version': '3.8.999', 'inventory': {'selected': [self.policy]},
                     'expected_artifacts': ['Example.3.8.999.nupkg']}
        self.graph = {'example': {'id': 'Example', 'version': '3.8.999', 'content_hash': 'A' * 86 + '==',
                                 'dependencies': {}, 'selected': True},
                      'external': {'id': 'External', 'version': '1.2.3', 'content_hash': 'B' * 86 + '==',
                                   'dependencies': {}, 'selected': False}}
        self.feeds = {'sources': {'original': proof.planner.NUGET_INDEX},
                      'mapping': {'example': ['original'], 'external': ['original']}}

    def artifact_receipt(self):
        path = self.artifacts / self.plan['expected_artifacts'][0]
        with zipfile.ZipFile(path, 'w') as archive:
            archive.writestr('Example.nuspec', '<package><metadata><id>Example</id><version>3.8.999</version>'
                '<repository commit="' + self.source['commit'] + '"/><dependencies><group targetFramework="net8.0"/>'
                '</dependencies></metadata></package>')
        with zipfile.ZipFile(path) as archive:
            inventory = [{'path': name, 'size': len(archive.read(name)), 'sha256': metadata.sha256(archive.read(name))}
                         for name in sorted(archive.namelist())]
        return {'success': True, 'artifact_proof': True, 'published': False, 'plan_sha256': self.hash,
                'source': self.source, 'product': 'studio', 'line': '3.8', 'version': '3.8.999',
                'execution': local_execution({}), 'packages': {'selected': [{'file': path.name, 'id': 'Example',
                'version': '3.8.999', 'sha256': metadata.sha256(path.read_bytes()), 'size': path.stat().st_size,
                'inventory': inventory}]}}

    def test_exact_archive_receipt_admitted(self):
        result = proof.admit_artifacts(self.plan, self.hash, self.artifact_receipt(), self.artifacts)
        self.assertEqual({'example'}, set(result))

    def test_changed_archive_and_wrong_source_receipt_rejected(self):
        receipt = self.artifact_receipt()
        path = self.artifacts / self.plan['expected_artifacts'][0]
        path.write_bytes(path.read_bytes() + b'changed')
        with self.assertRaisesRegex(ValueError, 'consumer_input_hash'):
            proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts)
        receipt['source'] = {'commit': 'e' * 40}
        with self.assertRaisesRegex(ValueError, 'consumer_producer_identity'):
            proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts)

    def test_private_or_duplicate_archives_cannot_enter_selection(self):
        receipt = self.artifact_receipt()
        (self.artifacts / 'Excluded.3.8.999.nupkg').write_bytes(b'excluded')
        with self.assertRaisesRegex(ValueError, 'consumer_artifact_bijection'):
            proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts)
        receipt['packages']['selected'].append(receipt['packages']['selected'][0])
        with self.assertRaisesRegex(ValueError, 'consumer_artifact_bijection'):
            proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts)

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

    def closure(self):
        selected = {'example': {'id': 'Example', 'content_hash': self.graph['example']['content_hash'],
                    'dependency_groups': [{'framework': 'net8.0', 'dependencies': [{'id': 'External', 'version': '[1.2.3, )'}]}]}}
        assets = {'targets': {'net8.0': {'External/1.2.3': {'type': 'package', 'dependencies': {'Transitive': '2.0.0'}},
                   'Transitive/2.0.0': {'type': 'package'}, 'Private.Compiler/9.0.0': {'type': 'package'}}},
                  'libraries': {'External/1.2.3': {'type': 'package', 'sha512': 'B' * 86 + '=='},
                   'Transitive/2.0.0': {'type': 'package', 'sha512': 'C' * 86 + '=='},
                   'Private.Compiler/9.0.0': {'type': 'package', 'sha512': 'D' * 86 + '=='}}}
        return selected, assets

    def test_published_closure_pins_transitives_excludes_private_compiler(self):
        selected, assets = self.closure()
        graph = proof.closure('Example', 'net8.0', assets, selected, '3.8.999', Semantics())
        self.assertEqual({'example', 'external', 'transitive'}, set(graph))
        self.assertEqual('2.0.0', graph['transitive']['version'])
        self.assertEqual('C' * 86 + '==', graph['transitive']['content_hash'])

    def test_external_project_and_missing_transitive_snapshot_rejected(self):
        selected, assets = self.closure()
        assets['targets']['net8.0']['Transitive/2.0.0']['type'] = 'project'
        with self.assertRaisesRegex(ValueError, 'consumer_external_project_fallback'):
            proof.closure('Example', 'net8.0', assets, selected, '3.8.999', Semantics())
        del assets['targets']['net8.0']['Transitive/2.0.0']
        with self.assertRaisesRegex(ValueError, 'consumer_snapshot_missing_dependency'):
            proof.closure('Example', 'net8.0', assets, selected, '3.8.999', Semantics())

    def test_package_edge_missing_from_root_uses_reached_selected_original_snapshot(self):
        selected, assets = self.closure()
        selected['example']['original_assets'] = deepcopy(assets)
        del assets['targets']['net8.0']['External/1.2.3']
        del assets['targets']['net8.0']['Transitive/2.0.0']
        graph = proof.closure('Example', 'net8.0', assets, selected, '3.8.999', Semantics())
        self.assertEqual({'example', 'external', 'transitive'}, set(graph))
        self.assertEqual('2.0.0', graph['transitive']['version'])

    def test_lock_keeps_one_direct_reference_and_pins_exact_transitive_hashes(self):
        lock = proof.lock_document('Example', 'net8.0', self.graph, '3.8.999')['dependencies']['net8.0']
        self.assertEqual('[3.8.999, 3.8.999]', lock['Example']['requested'])
        self.assertEqual('Transitive', lock['External']['type'])
        self.assertEqual('B' * 86 + '==', lock['External']['contentHash'])

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
        selected = proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts)
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
        proof.verify_cache(graph, self.feeds, cache, self.artifacts, selected)
        metadata_path.write_text(json.dumps({'source': proof.planner.NUGET_INDEX}))
        with self.assertRaisesRegex(RuntimeError, 'Unexpected package source'):
            proof.verify_cache(graph, self.feeds, cache, self.artifacts, selected)
        metadata_path.write_text(json.dumps({'source': str(self.artifacts)}))
        archive.write_bytes(b'wrong bytes')
        with self.assertRaisesRegex(RuntimeError, 'exact verified nupkg'):
            proof.verify_cache(graph, self.feeds, cache, self.artifacts, selected)

    def test_external_cache_requires_original_hash_and_mapped_source(self):
        cache = self.root / 'external-cache'
        folder = cache / 'external/1.2.3'
        folder.mkdir(parents=True)
        archive = folder / 'external.1.2.3.nupkg'
        archive.write_bytes(b'exact original externally restored archive')
        digest = proof.consumers.base64_sha512(archive.read_bytes())
        graph = {'external': self.graph['external'] | {'content_hash': digest}}
        (folder / 'external.1.2.3.nupkg.sha512').write_text(digest)
        cache_metadata = folder / '.nupkg.metadata'
        cache_metadata.write_text(json.dumps({'source': proof.planner.NUGET_INDEX}))
        proof.verify_cache(graph, self.feeds, cache, self.artifacts, {})
        cache_metadata.write_text(json.dumps({'source': 'https://wrong.invalid/'}))
        with self.assertRaisesRegex(ValueError, 'consumer_external_cache_source'):
            proof.verify_cache(graph, self.feeds, cache, self.artifacts, {})
        archive.write_bytes(b'different remote archive at same ID/version')
        with self.assertRaisesRegex(ValueError, 'consumer_external_archive_hash'):
            proof.verify_cache(graph, self.feeds, cache, self.artifacts, {})

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
