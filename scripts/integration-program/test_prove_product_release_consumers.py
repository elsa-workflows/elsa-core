from __future__ import annotations

from copy import deepcopy
from contextlib import redirect_stdout
from datetime import datetime, timedelta, timezone
import io
import json
import subprocess
from pathlib import Path
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
import zipfile
from unittest.mock import patch

import product_release_metadata as metadata
import test_prove_product_release_artifacts as artifact_contracts
import prove_product_release_consumers as proof
from product_artifact_execution import local_execution
from selected_maintenance_test_support import patch_offline_local_execution


class Semantics:
    def call(self, operation, **values):
        """Emulate range and framework semantics for deterministic consumer graph tests."""
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
        """Create committed controller fixtures and isolate tests from hosted execution identity."""
        super().setUpClass()
        local_execution_patch = patch_offline_local_execution(proof)
        local_execution_patch.start()
        cls.addClassCleanup(local_execution_patch.stop)

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
        """Create isolated selected-package archives, dependency graph, and feed policy fixtures."""
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.artifacts = self.root / 'archives'
        self.artifacts.mkdir()
        self.hash = 'a' * 64
        self.source = {'commit': 'b' * 40, 'tree': 'c' * 40}
        self.policy = {'id': 'Example', 'project': 'src/Example.csproj', 'frameworks': ['net8.0', 'net10.0'],
                       'metadata': {'restore_assets_sha256': '7' * 64,
                                    'dependency_groups': [{'framework': 'net8.0', 'dependencies': []}]}}
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
        """Create a producer receipt bound to the fixture's exact package archive bytes."""
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
        """Create an admitted producer plan whose execution predates the current consumer check."""
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
        """Encode producer evidence and exercise historical stage admission with its receipt
        hash.
        """
        raw = json.dumps(receipt).encode()
        return proof.admit_producer_stage(data, digest, raw, receipt_hash or metadata.sha256(raw))

    def test_expired_plan_is_historical_only_at_bound_successful_producer_start(self):
        """Verify expired plan is historical only at bound successful producer start."""
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
        """Verify producer start cannot be future invalid stale or before plan."""
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
        """Verify historical stage requires unchanged successful receipt identity."""
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
        """Verify exact archive receipt admitted."""
        result = proof.admit_artifacts(self.plan, self.hash, self.artifact_receipt(), self.artifacts, self.controller_root)
        self.assertEqual({'example'}, set(result))

    def test_native_framework_references_must_match_plan_before_fixture_can_supply_them(self):
        """Verify native framework references must match plan before fixture can supply them."""
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
        """Verify producer controller Git objects and planner inputs bound."""
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
        """Verify changed archive and wrong source receipt rejected."""
        receipt = self.artifact_receipt()
        path = self.artifacts / self.plan['expected_artifacts'][0]
        path.write_bytes(path.read_bytes() + b'changed')
        with self.assertRaisesRegex(ValueError, 'consumer_input_hash'):
            proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts, self.controller_root)
        receipt['source'] = {'commit': 'e' * 40}
        with self.assertRaisesRegex(ValueError, 'consumer_producer_identity'):
            proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts, self.controller_root)

    def test_private_or_duplicate_archives_cannot_enter_selection(self):
        """Verify private or duplicate archives cannot enter selection."""
        receipt = self.artifact_receipt()
        (self.artifacts / 'Excluded.3.8.999.nupkg').write_bytes(b'excluded')
        with self.assertRaisesRegex(ValueError, 'consumer_artifact_bijection'):
            proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts, self.controller_root)
        receipt['packages']['selected'].append(receipt['packages']['selected'][0])
        with self.assertRaisesRegex(ValueError, 'consumer_artifact_bijection'):
            proof.admit_artifacts(self.plan, self.hash, receipt, self.artifacts, self.controller_root)

    def snapshot(self):
        """Write original planning-assets fixtures and their bound private snapshot receipt."""
        folder = self.root / 'snapshots'
        folder.mkdir()
        raw = json.dumps({'targets': {'net8.0': {}, 'net10.0': {}},
                          'project': {'frameworks': {'net8.0': {}, 'net10.0': {}}}}).encode()
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
        """Verify snapshot uses raw hash and ignores derived targets."""
        folder, _ = self.snapshot()
        result = proof.load_snapshots(self.plan, self.hash, folder)
        self.assertEqual({'net8.0': {}, 'net10.0': {}}, result[self.policy['project']]['targets'])

    def test_wrong_snapshot_hash_identity_path_and_partition_rejected(self):
        """Verify wrong snapshot hash identity path and partition rejected."""
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
        """Verify symbolic snapshot file rejected."""
        folder, receipt = self.snapshot()
        row = receipt['selected'][0]
        original = folder / row['file']
        real = folder / 'original.json'
        original.rename(real)
        original.symlink_to(real)
        with self.assertRaisesRegex(ValueError, 'consumer_input_path'):
            proof.load_snapshots(self.plan, self.hash, folder)

    def native_graph(self, root_id='Example'):
        """Build matching assets, lockfile, selected-package, and external catalog fixtures."""
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
        if root_id != 'Example':
            selected[root_id.casefold()] = selected.pop('example') | {'id': root_id}
            key = 'Example/3.8.999'
            targets[root_id + '/3.8.999'] = targets.pop(key)
            libraries[root_id + '/3.8.999'] = libraries.pop(key)
            locked[root_id] = locked.pop('Example')
        return selected, catalog, {'targets': {'net8.0': targets}, 'libraries': libraries}, {
            'version': 1, 'dependencies': {'net8.0': locked}}

    def test_original_sdk_pruning_and_download_policy_reaches_generated_project(self):
        """Verify original SDK pruning and download policy reaches generated project."""
        assets = {'project': {'frameworks': {'net8.0': {
            'packagesToPrune': {'System.Threading.Channels': '(,8.0.32767]'},
            'downloadDependencies': [{'name': 'Microsoft.NETCore.App.Ref', 'version': '[8.0.27, 8.0.27]'}]}}}}
        policy = proof.resolution.sdk.original_policy(assets, 'net8.0')
        project = ET.fromstring(proof.render_project('Example', '3.8.999', 'net8.0', [],
            executable=True, managed=True, sdk_policy=policy))
        for name, value in {'RestoreEnablePackagePruning': 'true', 'DisableImplicitLibraryPacksFolder': 'true',
                            'DisableImplicitNuGetFallbackFolder': 'true', 'UseAppHost': 'false'}.items():
            self.assertEqual(value, project.find('PropertyGroup/' + name).text)
        self.assertEqual('$(MSBuildThisFileDirectory)targeting-packs',
                         project.find('PropertyGroup/NetCoreTargetingPackRoot').text)

    def test_original_sdk_pruned_external_edge_requires_exact_original_effective_evidence(self):
        """Verify original SDK pruned external edge requires exact original effective evidence."""
        selected, catalog, assets, lock = self.native_graph()
        catalog[('external', '1.2.3')]['groups'][0]['dependencies'] = [
            {'id': 'System.Threading.Channels', 'range': '[8.0.0, )'}]
        catalog[('external', '1.2.3')]['effective_contexts'] = [{'framework': 'net8.0', 'dependencies': {},
            'packages_to_prune': {'System.Threading.Channels': '(,8.0.32767]'}}]
        policy = {'pruning': {'System.Threading.Channels': '(,8.0.32767]'}, 'downloads': []}
        assets['project'] = {'frameworks': {'net8.0': {'packagesToPrune': dict(policy['pruning'])}}}
        graph = proof.resolution.audit_native_graph(assets, lock, 'Example', 'net8.0', selected,
            catalog, '3.8.999', Semantics(), sdk_policy=policy)
        self.assertEqual({}, graph['external']['dependencies'])
        for change in ('threshold', 'context', 'selected', 'effective-range'):
            actual, original_catalog, original_selected = deepcopy(assets), deepcopy(catalog), deepcopy(selected)
            if change == 'threshold': actual['project']['frameworks']['net8.0']['packagesToPrune']['System.Threading.Channels'] = '(,99.0]'
            elif change == 'context': original_catalog[('external', '1.2.3')]['effective_contexts'] = []
            elif change == 'effective-range': original_catalog[('external', '1.2.3')]['effective_contexts'][0]['dependencies']['System.Threading.Channels'] = '8.0.0'
            else: original_selected['example']['dependency_groups'][0]['dependencies'].append({'id':'System.Threading.Channels','version':'[8.0.0, )'})
            with self.subTest(change=change), self.assertRaises(ValueError):
                proof.resolution.audit_native_graph(actual, lock, 'Example', 'net8.0', original_selected,
                    original_catalog, '3.8.999', Semantics(), sdk_policy=policy)

    def sdk_archive_inputs(self):
        """Create an SDK reference archive and matching original restore-assets fixture."""
        cache = self.root / 'sdk-original-cache'
        identifier, version = 'Microsoft.NETCore.App.Ref', '8.0.27'
        archive = cache / identifier.lower() / version / (identifier.lower() + '.' + version + '.nupkg')
        archive.parent.mkdir(parents=True)
        with zipfile.ZipFile(archive, 'w') as package:
            package.writestr(identifier + '.nuspec', '<package><metadata><id>' + identifier + '</id><version>' + version + '</version></metadata></package>')
        archive.with_suffix('.nupkg.sha512').write_text(proof.consumers.base64_sha512(archive.read_bytes()))
        assets = {'packageFolders': {str(cache): {}}, 'project': {'frameworks': {'net8.0': {
            'downloadDependencies': [{'name': identifier, 'version': '[8.0.27, 8.0.27]'}]}}}}
        semantics = Semantics()
        semantics.call = lambda op, **kw: [{'id': identifier, 'version': version}] if op == 'nuspecs' else []
        return archive, assets, semantics

    def test_sdk_download_catalog_is_new_frozen_evidence_and_cold_https_bytes_are_separate(self):
        """Verify SDK download catalog is new frozen evidence and cold https bytes are separate."""
        archive, original, semantics = self.sdk_archive_inputs()
        sdk = proof.resolution.sdk
        catalog = sdk.freeze_downloads({'original': original}, self.root / 'inspector', self.root / 'frozen', semantics)
        row = catalog[('microsoft.netcore.app.ref', '8.0.27')]
        self.assertNotEqual(archive, row['archive'])
        cache = self.root / 'sdk-cold-proof'
        destination = cache / archive.relative_to(self.root / 'sdk-original-cache')
        destination.parent.mkdir(parents=True)
        destination.write_bytes(row['archive'].read_bytes())
        destination.with_suffix('.nupkg.sha512').write_text(proof.consumers.base64_sha512(destination.read_bytes()))
        metadata_path = destination.parent / '.nupkg.metadata'
        metadata_path.write_text(json.dumps({'source': proof.planner.NUGET_INDEX}))
        restored = deepcopy(original); restored['packageFolders'] = {str(cache): {}}
        policy = sdk.original_policy(original, 'net8.0')
        def verify():
            return sdk.verify_downloads(restored, 'net8.0', policy, catalog, cache,
                {'nuget': proof.planner.NUGET_INDEX}, {'microsoft.netcore.app.ref': ['nuget']}, self.root / 'inspector', proof=True)
        records = verify()
        self.assertEqual([{'id': 'Microsoft.NETCore.App.Ref', 'version': '8.0.27'}], policy['downloads'])
        self.assertEqual(row['archive_sha256'], records[0]['sha256'])
        self.assertNotIn('archive', records[0])
        archive.write_bytes(b'ambient cache changed')
        self.assertEqual(records, verify())
        metadata_path.write_text(json.dumps({'source': 'https://unreviewed.invalid/index.json'}))
        with self.assertRaisesRegex(ValueError, 'consumer_sdk_download_source'): verify()
        metadata_path.write_text(json.dumps({'source': proof.planner.NUGET_INDEX}))
        with zipfile.ZipFile(destination, 'a') as package: package.writestr('changed', b'x')
        destination.with_suffix('.nupkg.sha512').write_text(proof.consumers.base64_sha512(destination.read_bytes()))
        with self.assertRaisesRegex(ValueError, 'consumer_sdk_download_bytes'): verify()

    def test_sdk_download_candidate_ambiguity_symlink_and_unbound_identity_fail(self):
        """Verify SDK download candidate ambiguity symlink and unbound identity fail."""
        archive, original, semantics = self.sdk_archive_inputs()
        sdk = proof.resolution.sdk
        other = self.root / 'second-sdk-cache'
        copy = other / archive.relative_to(self.root / 'sdk-original-cache')
        copy.parent.mkdir(parents=True); copy.write_bytes(archive.read_bytes())
        ambiguous = deepcopy(original); ambiguous['packageFolders'][str(other)] = {}
        with self.assertRaisesRegex(ValueError, 'consumer_sdk_archive_candidates'):
            sdk.freeze_downloads({'project': ambiguous}, self.root / 'inspector', self.root / 'unused', semantics)
        sidecar = archive.with_suffix('.nupkg.sha512'); original_sidecar = sidecar.read_bytes()
        sidecar.unlink(); target = self.root / 'sidecar'; target.write_bytes(original_sidecar); sidecar.symlink_to(target)
        with self.assertRaisesRegex(ValueError, 'consumer_sdk_archive_path'):
            sdk.freeze_downloads({'project': original}, self.root / 'inspector', self.root / 'unused', semantics)
        for value in ('[8.0.27, )', '[8.0.27, 8.0.28]'):
            malformed = deepcopy(original); malformed['project']['frameworks']['net8.0']['downloadDependencies'][0]['version'] = value
            with self.assertRaisesRegex(ValueError, 'consumer_sdk_original_download'): sdk.original_policy(malformed, 'net8.0')
        original['project']['frameworks']['net8.0']['downloadDependencies'][0]['name'] = 'Unreviewed'
        with self.assertRaisesRegex(ValueError, 'consumer_sdk_original_download'): sdk.original_policy(original, 'net8.0')

    def test_signed_sdk_download_hashes_remain_native_and_raw_distinct(self):
        """Verify signed SDK download hashes remain native and raw distinct."""
        archive, original, semantics = self.sdk_archive_inputs()
        with zipfile.ZipFile(archive, 'a') as package: package.writestr('.signature.p7s', b'synthetic signed-fixture marker')
        archive.with_suffix('.nupkg.sha512').write_text(proof.consumers.base64_sha512(archive.read_bytes()))
        native_hash = 'A'*86+'=='
        with patch.object(proof.archives, 'run', return_value=json.dumps({'signed': True,
                'archive_sha256': metadata.sha256(archive.read_bytes()), 'content_hash': native_hash})) as inspect:
            catalog = proof.resolution.sdk.freeze_downloads({'original': original}, self.root/'inspector', self.root/'frozen', semantics)
        record = next(iter(catalog.values()))
        self.assertEqual(native_hash, record['nuget_content_hash'])
        self.assertNotEqual(proof.consumers.base64_sha512(archive.read_bytes()), native_hash)
        self.assertEqual(metadata.sha256(archive.read_bytes()), record['archive_sha256'])
        self.assertEqual(self.root/'frozen/microsoft.netcore.app.ref/8.0.27'/archive.name, Path(inspect.call_args.args[0][-1]))

    def test_selected_identity_cannot_be_pruned_even_with_original_omission(self):
        """Verify selected identity cannot be pruned even with original omission."""
        selected, catalog, assets, lock = self.native_graph()
        selected['external'] = {'id': 'External'}
        policy = {'pruning': {'External': '(,8.0.32767]'}, 'downloads': []}
        selected['example']['effective_contexts'] = [{'framework': 'net8.0', 'dependencies': {},
            'packages_to_prune': policy['pruning']}]
        assets['project'] = {'frameworks': {'net8.0': {'packagesToPrune': policy['pruning']}}}
        assets['targets']['net8.0']['Example/3.8.999']['dependencies'] = {}
        assets['targets']['net8.0'].pop('External/1.2.3'); assets['libraries'].pop('External/1.2.3')
        lock['dependencies']['net8.0']['Example']['dependencies'] = {}; lock['dependencies']['net8.0'].pop('External')
        with self.assertRaisesRegex(ValueError, 'consumer_native_declared_edges'):
            proof.resolution.audit_native_graph(assets, lock, 'Example', 'net8.0', selected, catalog,
                '3.8.999', Semantics(), sdk_policy=policy)

    def test_pruning_uses_native_range_satisfaction_at_sdk_maximum_not_minimum_guess(self):
        """Verify pruning uses native range satisfaction at SDK maximum not minimum guess."""
        sdk = proof.resolution.sdk
        policy = {'pruning': {'System.Threading.Channels': '(,8.0.32767]'}, 'downloads': []}
        package = {'effective_contexts': [{'framework': 'net8.0', 'dependencies': {}, 'packages_to_prune': policy['pruning']}]}
        for dependency, expected in (('[8.0.0, )', True), ('[8.0.0, 8.0.0]', False),
                ('[8.0.0, 8.0.32767)', False), ('[8.0.32768, )', False)):
            calls = []
            semantics = Semantics()
            def ranges(op, **kw):
                calls.extend(kw['values']); return [{'satisfies': expected}]
            semantics.call = ranges
            self.assertEqual(expected, sdk.pruned_edge(package, 'net8.0', 'System.Threading.Channels', dependency, policy, semantics))
            self.assertEqual([{'range': dependency, 'version': '8.0.32767'}], calls)
        package['effective_contexts'][0]['packages_to_prune'] = dict(policy['pruning'], Extra='(,1.0.0]')
        self.assertFalse(sdk.pruned_edge(package, 'net8.0', 'System.Threading.Channels', '[8.0.0, )', policy, semantics))

    def test_native_graph_uses_full_nuspec_edges_and_one_exact_direct_root(self):
        """Verify native graph uses full nuspec edges and one exact direct root."""
        selected, catalog, assets, lock = self.native_graph()
        graph = proof.resolution.audit_native_graph(assets, lock, 'Example', 'net8.0', selected,
                                                     catalog, '3.8.999', Semantics())
        self.assertEqual({'example', 'external'}, set(graph))
        self.assertEqual({'External': '[1.2.3, )'}, graph['example']['dependencies'])

    def range_failure(self, phase='discovery', *, count_mismatch=False, root_id='Example'):
        """Inject an unsatisfied native dependency range and return the resulting diagnostic
        error.
        """
        selected, catalog, assets, lock = self.native_graph(root_id)
        semantics = Semantics()
        original_call = semantics.call
        def call(operation, **values):
            result = original_call(operation, **values)
            if operation == 'ranges' and len(values['values']) == 3:
                if count_mismatch:
                    return result[:1]
                result[0]['satisfies'] = False
            return result
        semantics.call = call
        error = ValueError('consumer_dependency_range_conflict')
        original_require = proof.resolution.require
        def require(condition, code):
            if not condition and code == 'consumer_dependency_range_conflict':
                raise error
            original_require(condition, code)
        with patch.object(proof.resolution, 'require', side_effect=require), self.assertRaises(ValueError) as caught:
            proof.resolution.audit_native_graph(assets, lock, root_id, 'net8.0', selected,
                catalog, '3.8.999', semantics, phase=phase)
        self.assertIs(error, caught.exception)
        self.assertIs(type(error), ValueError)
        self.assertEqual(('consumer_dependency_range_conflict',), error.args)
        return error

    def test_native_range_failure_keeps_exception_identity_and_closed_edge_source(self):
        """Verify native range failure keeps exception identity and closed edge source."""
        detail = proof.resolution.public_range_failure(self.range_failure())['dependency_range_failure']
        self.assertEqual(detail, {'root_id': 'Example', 'framework': 'net8.0', 'phase': 'discovery',
            'reason': 'unsatisfied-range', 'requested_checks': 3, 'returned_checks': 3,
            'unsatisfied_checks': 1, 'ranges': [{'from_id': 'Example', 'to_id': 'External',
                'requested_range': '[1.2.3, )', 'resolved_version': '1.2.3', 'source_kind': 'selected-nuspec'}]})

    def test_native_range_count_mismatch_exposes_no_unbound_edges(self):
        """Verify native range count mismatch exposes no unbound edges."""
        detail = proof.resolution.public_range_failure(self.range_failure('locked-proof', count_mismatch=True))
        self.assertEqual(detail, {'dependency_range_failure': {'root_id': 'Example', 'framework': 'net8.0',
            'phase': 'locked-proof', 'reason': 'count-mismatch', 'requested_checks': 3, 'returned_checks': 1}})

    def assert_cli_range_phase(self, phase):
        """Require the consumer CLI to retain the failing range phase without private paths."""
        error = self.range_failure(phase, root_id='Elsa.Studio.Core')
        audit = proof.resolution.audit_native_graph
        phases = []
        def fail(*args, **kwargs):
            phases.append(kwargs['phase'])
            if kwargs['phase'] == phase:
                raise error
            return audit(*args, **kwargs)
        with patch.object(proof.resolution, 'audit_native_graph', side_effect=fail):
            receipt, data, _, _ = self.run_private_consumer(cli=True)
        expected = proof.resolution.public_range_failure(error)['dependency_range_failure']
        self.assertEqual(self.cli_diagnostic, {'success': False,
            'failure_code': 'consumer_dependency_range_conflict', 'failure_stage': 'selected-restore-compile',
            'retained_receipt_created': True, 'dependency_range_failure': expected})
        self.assertEqual(['discovery'] if phase == 'discovery' else ['discovery', 'locked-proof'], phases)
        self.assertEqual(expected, receipt['dependency_range_failure'])
        self.assertFalse(receipt['success'])
        self.assertEqual([], receipt['coverage'])
        self.assertNotIn(str(self.root), data)

    def test_cli_projects_native_range_failure_through_discovery(self):
        """Verify CLI projects native range failure through discovery."""
        self.assert_cli_range_phase('discovery')

    def test_cli_projects_native_range_failure_through_locked_proof(self):
        """Verify CLI projects native range failure through locked proof."""
        self.assert_cli_range_phase('locked-proof')

    def test_native_graph_rejects_unreviewed_versions_hashes_missing_edges_and_extra_directs(self):
        """Verify native graph rejects unreviewed versions hashes missing edges and extra
        directs.
        """
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
        """Verify remote mapping never contains selected ids or wildcard."""
        config = ET.fromstring(proof.render_config(self.artifacts, self.graph, self.feeds))
        groups = {row.get('key'): [item.get('pattern') for item in row] for row in config.find('packageSourceMapping')}
        self.assertEqual(['Example'], groups[proof.LOCAL])
        self.assertEqual(['External'], groups['original'])
        self.assertNotIn('*', str(groups))

    def test_unused_original_sources_remain_configured_without_empty_mapping_groups(self):
        """Verify unused original sources remain configured without empty mapping groups."""
        feeds = deepcopy(self.feeds)
        feeds['sources'].update({
            'elsa3.feedz.io': 'https://f.feedz.io/elsa-workflows/elsa/nuget/index.json',
            'webhooks-core.feedz.io': 'https://f.feedz.io/personal/webhooks-core/nuget/index.json'})
        feeds['mapping']['external'] = ['original', 'elsa3.feedz.io']
        feeds['mapping']['example'] = list(feeds['sources'])
        for discovery in (False, True):
            policy = deepcopy(feeds)
            if discovery:
                policy['sources'] = {name: str(self.root / 'finite-mirrors' / name) for name in policy['sources']}
            for selected_only in (False, True):
                graph = {'example': self.graph['example']} if selected_only else self.graph
                with self.subTest(discovery=discovery, selected_only=selected_only):
                    config = ET.fromstring(proof.render_config(self.artifacts, graph, policy))
                    sources = {row.get('key'): row.get('value') for row in config.find('packageSources') if row.tag == 'add'}
                    self.assertEqual(policy['sources'] | {proof.LOCAL: str(self.artifacts.resolve())}, sources)
                    groups = {row.get('key'): [item.get('pattern') for item in row] for row in config.find('packageSourceMapping')}
                    expected = {proof.LOCAL: ['Example']}
                    if not selected_only:
                        expected.update({'original': ['External'], 'elsa3.feedz.io': ['External']})
                    self.assertEqual(expected, groups)
                    self.assertTrue(all(groups.values()))

    def restored(self):
        """Build an isolated restored-assets fixture with exact sources and package folders."""
        root = self.root / 'consumer'
        cache = root / 'packages'
        assets = {'targets': {'net8.0': {row['id'] + '/' + row['version']: {'type': 'package'} for row in self.graph.values()}},
                  'libraries': {row['id'] + '/' + row['version']: {'type': 'package', 'sha512': row['content_hash']} for row in self.graph.values()},
                  'packageFolders': {str(cache): {}}, 'project': {'restore': {
                      'sources': {proof.planner.NUGET_INDEX: {}, str(self.artifacts): {}},
                      'configFilePaths': [str(root / 'NuGet.Config')]}}}
        return root, cache, assets

    def test_exact_restored_closure_accepted(self):
        """Verify exact restored closure accepted."""
        root, cache, assets = self.restored()
        proof.validate_restored(assets, 'net8.0', self.graph, root, cache, self.artifacts, self.feeds)

    def test_restore_rejects_wrong_version_project_fallback_and_inherited_sources(self):
        """Verify restore rejects wrong version project fallback and inherited sources."""
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
        """Verify cache requires selected original archive and local origin."""
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
        """Verify signed external native content hash is separate from raw archive hash."""
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
        """Create an original external archive, restored assets, and mocked native catalog
        inputs.
        """
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
        """Verify offline catalog freezes original bytes and preserves original source mapping."""
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
        """Verify offline catalog rejects changed original bytes and unmapped source."""
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
        """Verify original archive candidates and conflicting context hashes fail closed."""
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
        """Verify native semantics and inspector runtime are plan bound."""
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
        """Verify extracted compile content and runtime payloads join archive bytes."""
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

    def original_flat_build_fixture(self, *, helper_members=()):
        """Exact HTTP/net10 native group shape: one Swagger and two NSwag sentinels."""
        temporary = tempfile.TemporaryDirectory(dir=self.root)
        self.addCleanup(temporary.cleanup)
        values = {'cache': Path(temporary.name).resolve()}
        root, version, framework = 'Elsa.IO.Http', '3.8.999', 'net10.0'
        root_key = root + '/' + version
        root_archive = self.artifacts / (root_key.replace('/', '.') + '.nupkg')
        with zipfile.ZipFile(root_archive, 'w') as archive: archive.writestr('a.nuspec', b'<package />')
        targets = {root_key: {'type': 'package', 'dependencies': {'Elsa.Api.Common': '3.8.4'}}}
        libraries, catalog, graph = {}, {}, {root.lower(): {'selected': True, 'version': version}}
        for identifier, package_version, groups, dependency in (
            ('Elsa.Api.Common', '3.8.4', (), {'FastEndpoints.Swagger': '8.2.0'}),
            ('FastEndpoints.Swagger', '8.2.0', ('build',), {'NSwag.AspNetCore': '14.7.1'}),
            ('NSwag.AspNetCore', '14.7.1', ('build', 'buildMultiTargeting'), {})):
            key = identifier + '/' + package_version
            folder = values['cache'] / identifier.lower() / package_version
            folder.mkdir(parents=True)
            entries = {group + '/' + identifier + '.targets': b'<Project />' for group in groups}
            if identifier == 'FastEndpoints.Swagger':
                entries.update({name: b'<Project />' for name in helper_members})
            entries['lib/net10.0/' + identifier + '.dll'] = b'external DLL'
            archive = folder / (identifier.lower() + '.' + package_version + '.nupkg')
            with zipfile.ZipFile(archive, 'w') as package:
                for name, data in entries.items(): package.writestr(name, data)
            for name, data in entries.items():
                path = folder / name; path.parent.mkdir(parents=True, exist_ok=True); path.write_bytes(data)
            targets[key] = {'type': 'package', 'dependencies': dependency,
                'compile': {'lib/net10.0/' + identifier + '.dll': {}},
                **{group: {group + '/_._': {}} for group in groups}}
            libraries[key] = {'type': 'package', 'sha512': 'native-' + identifier, 'files': list(entries)}
            catalog[(identifier.lower(), package_version)] = {'archive_sha256': metadata.sha256(archive.read_bytes()),
                'content_hash': 'native-' + identifier}
            graph[identifier.lower()] = {'selected': False, 'version': package_version}
        assets = {'targets': {framework: targets}, 'libraries': libraries,
            'project': {'frameworks': {framework: {'dependencies': {
                root: {'target': 'Package', 'version': '[3.8.999, 3.8.999]'}}}}},
            'projectFileDependencyGroups': {framework: [root + ' >= 3.8.999 <= 3.8.999']}}
        original = deepcopy(assets)
        original['project']['restore'] = {'projectPath': '/owned/source/src/Elsa.IO.Http.csproj'}
        selected = {root.lower(): {'nupkg': root_archive.name,
            'policy': {'project': 'src/Elsa.IO.Http.csproj', 'metadata': {'restore_assets_sha256': 'a' * 64}}}}
        values.update(assets=assets, original_assets=original, selected=selected, catalog=catalog, graph=graph,
            framework=framework, key='FastEndpoints.Swagger/8.2.0', root_key=root_key)
        return values

    def test_original_flat_build_allows_archive_bound_unselected_helpers(self):
        # Grpc.Tools has flat package-named entrypoints plus nested _grpc and
        # _protobuf helpers; Proto.Cluster.CodeGen also has flat helper props.
        """Verify original flat build allows archive bound unselected helpers."""
        values = self.original_flat_build_fixture(helper_members=(
            'build/_grpc/_Grpc.Tools.props', 'build/_protobuf/Google.Protobuf.Tools.targets',
            'build/ProtoGrainGenerator.props', 'Build/_grpc/Other.PROPS'))
        rows = self.verify_empty_build(values)
        markers = [item for row in rows for item in row['payloads'] if 'accounting' in item]
        self.assertEqual(len(markers), 3)
        self.assertEqual(markers[0]['path'], 'build/_._')

    def test_original_flat_build_helpers_keep_inventory_and_cache_byte_checks(self):
        """Verify original flat build helpers keep inventory and cache byte checks."""
        for member in ('build/_grpc/_Grpc.Tools.props', 'build/_grpc/Other.PROPS', 'Build/_grpc/Other.TARGETS'):
            for change in ('original_inventory', 'current_inventory', 'cache_bytes', 'cache_symlink'):
                values = self.original_flat_build_fixture(helper_members=(member,))
                folder = values['cache'] / 'fastendpoints.swagger/8.2.0'
                key = values['key']
                if change == 'original_inventory': values['original_assets']['libraries'][key]['files'].remove(member)
                elif change == 'current_inventory': values['assets']['libraries'][key]['files'].remove(member)
                elif change == 'cache_bytes': (folder / member).write_bytes(b'changed helper')
                else:
                    (folder / member).unlink()
                    (folder / member).symlink_to(folder / 'build/FastEndpoints.Swagger.targets')
                with self.subTest(member=member, change=change), self.assertRaises(ValueError):
                    self.verify_empty_build(values)

    def test_original_flat_build_rejects_nonflat_package_named_entrypoint(self):
        """Verify original flat build rejects nonflat package named entrypoint."""
        values = self.original_flat_build_fixture(helper_members=(
            'build/net10.0/FastEndpoints.Swagger.props',))
        with self.assertRaisesRegex(ValueError, 'consumer_original_build_real_member'):
            self.verify_empty_build(values)

    def test_original_flat_build_and_multitargeting_markers_are_inert(self):
        """Verify original flat build and multitargeting markers are inert."""
        values = self.original_flat_build_fixture()
        rows = self.verify_empty_build(values)
        markers = [item for row in rows for item in row['payloads'] if 'accounting' in item]
        self.assertEqual([(item['kind'], item['path']) for item in markers],
            [('build', 'build/_._'), ('build', 'build/_._'), ('buildMultiTargeting', 'buildMultiTargeting/_._')])
        for item in markers:
            self.assertEqual(item['origin'], proof.ORIGINAL_EXCLUDED_BUILD_ORIGIN)
            self.assertEqual(item['accounting'], proof.ORIGINAL_EXCLUDED_BUILD_ACCOUNTING)
            self.assertEqual(item['original_assets_sha256'], 'a' * 64)
            self.assertNotIn('size', item)
            self.assertNotIn('sha256', item)

    def test_original_flat_build_requires_same_original_context_and_native_graph(self):
        """Verify original flat build requires same original context and native graph."""
        for change in ('missing_snapshot', 'framework', 'version', 'native_hash', 'archive_hash', 'original_inventory',
                       'current_inventory', 'project', 'original_metadata', 'current_metadata', 'original_mixed',
                       'current_mixed', 'current_mixed_reversed', 'missing_original_group', 'build_transitive',
                       'external_direct', 'original_external_direct', 'direct_flags', 'project_group',
                       'extra_incoming', 'original_incoming', 'duplicate_incoming', 'nonpackage', 'original_nonpackage', 'selected'):
            values = self.original_flat_build_fixture()
            assets, original = values['assets'], values['original_assets']
            key, framework = values['key'], values['framework']
            target, before = assets['targets'][framework][key], original['targets'][framework][key]
            if change == 'missing_snapshot': values['original_assets'] = None
            elif change == 'framework': original['targets']['net9.0'] = original['targets'].pop(framework)
            elif change == 'version': original['libraries'].pop(key)
            elif change == 'native_hash': original['libraries'][key]['sha512'] = 'wrong'
            elif change == 'archive_hash': values['catalog'][('fastendpoints.swagger', '8.2.0')]['archive_sha256'] = 'f' * 64
            elif change == 'original_inventory': original['libraries'][key]['files'] = []
            elif change == 'current_inventory': assets['libraries'][key]['files'] = []
            elif change == 'project': original['project']['restore']['projectPath'] = '/foreign/source/Wrong.csproj'
            elif change == 'original_metadata': before['build']['build/_._'] = {'copyToOutput': False}
            elif change == 'current_metadata': target['build']['build/_._'] = {'unexpected': False}
            elif change == 'original_mixed': before['build']['build/FastEndpoints.Swagger.targets'] = {}
            elif change in ('current_mixed', 'current_mixed_reversed'):
                target['build']['build/FastEndpoints.Swagger.targets'] = {}
                if change.endswith('reversed'): target['build'] = dict(reversed(list(target['build'].items())))
            elif change == 'missing_original_group': before.pop('build')
            elif change == 'build_transitive': before['buildTransitive'] = {'buildTransitive/Example.props': {}}
            elif change == 'original_external_direct': original['project']['frameworks'][framework]['dependencies']['FastEndpoints.Swagger'] = {'target': 'Package'}
            elif change == 'external_direct': assets['project']['frameworks'][framework]['dependencies']['FastEndpoints.Swagger'] = {'target': 'Package'}
            elif change == 'direct_flags': assets['project']['frameworks'][framework]['dependencies']['Elsa.IO.Http']['include'] = 'All'
            elif change == 'project_group': assets['projectFileDependencyGroups'][framework].append('FastEndpoints.Swagger >= 8.2.0')
            elif change == 'extra_incoming': assets['targets'][framework][values['root_key']]['dependencies']['FastEndpoints.Swagger'] = '8.2.0'
            elif change == 'original_incoming': original['targets'][framework]['Elsa.Api.Common/3.8.4']['dependencies']['FastEndpoints.Swagger'] = '[8.2.0]'
            elif change == 'duplicate_incoming': assets['targets'][framework]['Elsa.Api.Common/3.8.4']['dependencies']['fastendpoints.swagger'] = '8.2.0'
            elif change == 'original_nonpackage': before['type'] = 'project'
            elif change == 'selected':
                values['graph']['fastendpoints.swagger']['selected'] = True
                archive = values['cache'] / 'fastendpoints.swagger/8.2.0/fastendpoints.swagger.8.2.0.nupkg'
                values['selected']['fastendpoints.swagger'] = {'nupkg': archive.name}
                (self.artifacts / archive.name).write_bytes(archive.read_bytes())
            else: target['type'] = 'project'
            with self.subTest(change=change), self.assertRaisesRegex(ValueError, 'consumer_original_build_'):
                self.verify_empty_build(values)

    def test_original_flat_build_never_skips_physical_archive_or_cache_markers(self):
        """Verify original flat build never skips physical archive or cache markers."""
        for change in ('cache', 'case_cache', 'symlink', 'case_archive', 'real_archive', 'missing_real_member', 'archive_transitive', 'wrong_directory', 'real_member_symlink', 'real_member_changed'):
            values = self.original_flat_build_fixture()
            folder = values['cache'] / 'fastendpoints.swagger/8.2.0'
            archive = folder / 'fastendpoints.swagger.8.2.0.nupkg'
            path = folder / 'build/_._'
            if change == 'cache': path.write_bytes(b'physical')
            elif change == 'case_cache':
                collision = folder / 'BUILD/_._'; collision.parent.mkdir(exist_ok=True); collision.write_bytes(b'physical')
            elif change == 'symlink': path.symlink_to(folder / 'lib/net10.0/FastEndpoints.Swagger.dll')
            elif change == 'real_member_symlink':
                member = folder / 'build/FastEndpoints.Swagger.targets'
                member.unlink(); member.symlink_to(folder / 'lib/net10.0/FastEndpoints.Swagger.dll')
            elif change == 'real_member_changed': (folder / 'build/FastEndpoints.Swagger.targets').write_bytes(b'changed')
            else:
                with zipfile.ZipFile(archive) as package: entries = {name: package.read(name) for name in package.namelist()}
                if change == 'missing_real_member': entries.pop('build/FastEndpoints.Swagger.targets')
                elif change == 'archive_transitive': entries['buildTransitive/FastEndpoints.Swagger.targets'] = b'<Project />'
                elif change == 'wrong_directory': entries['build/net10.0/FastEndpoints.Swagger.targets'] = entries.pop('build/FastEndpoints.Swagger.targets')
                else: entries['build/_._' if change == 'real_archive' else 'BUILD/_._'] = b'physical'
                with zipfile.ZipFile(archive, 'w') as package:
                    for name, data in entries.items(): package.writestr(name, data)
                values['catalog'][('fastendpoints.swagger', '8.2.0')]['archive_sha256'] = metadata.sha256(archive.read_bytes())
            with self.subTest(change=change), self.assertRaises(ValueError): self.verify_empty_build(values)
            if change == 'real_archive':
                path.write_bytes(b'physical')
                rows = self.verify_empty_build(values)
                actual = next(row for row in rows if row['id'] == 'FastEndpoints.Swagger')['payloads'][-1]
                self.assertEqual(actual['sha256'], metadata.sha256(b'physical'))
                self.assertNotIn('accounting', actual)
                path.write_bytes(b'changed')
                with self.assertRaisesRegex(ValueError, 'consumer_input_hash'): self.verify_empty_build(values)

    def empty_build_fixture(self, framework='net8.0'):
        """Create source-bound package and restore evidence for an excluded build marker."""
        version = {'net8.0': '8.0.24', 'net9.0': '9.0.13', 'net10.0': '10.0.3'}[framework]
        identifier, root = 'Microsoft.AspNetCore.Components.WebAssembly', 'Elsa.Studio.Localization.BlazorWasm'
        key, root_key = identifier + '/' + version, root + '/3.8.999'
        temporary = tempfile.TemporaryDirectory(dir=self.root)
        self.addCleanup(temporary.cleanup)
        cache = Path(temporary.name).resolve()
        folder = cache / identifier.lower() / version
        folder.mkdir(parents=True)
        archive = folder / (identifier.lower() + '.' + version + '.nupkg')
        props = f'build/{framework}/{identifier}.props'
        dll = f'lib/{framework}/{identifier}.dll'
        entries = {props: b'<Project />', dll: b'original external DLL', f'build/{framework}/blazor.webassembly.js': b'js'}
        with zipfile.ZipFile(archive, 'w') as package:
            for name, data in entries.items(): package.writestr(name, data)
        for name, data in entries.items():
            path = folder / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
        root_archive = self.artifacts / (root + '.3.8.999.nupkg')
        with zipfile.ZipFile(root_archive, 'w') as package: package.writestr('a.nuspec', b'<package />')
        assets = {'targets': {framework: {
            root_key: {'type': 'package', 'dependencies': {identifier: version}},
            key: {'type': 'package', 'compile': {dll: {}}, 'build': {f'build/{framework}/_._': {}}}}},
            'libraries': {key: {'type': 'package', 'sha512': 'original-native-content-hash', 'files': list(entries)}},
            'project': {'frameworks': {framework: {'dependencies': {
                root: {'target': 'Package', 'version': '[3.8.999, 3.8.999]', 'aliases': 'selected'}}}}}}
        original = deepcopy(assets)
        assets['projectFileDependencyGroups'] = {framework: [root + ' >= 3.8.999 <= 3.8.999']}
        original['projectFileDependencyGroups'] = {framework: [identifier + ' >= ' + version]}
        original['targets'][framework][key]['build'] = {props: {}}
        original['project']['frameworks'][framework]['dependencies'] = {
            identifier: {'target': 'Package', 'version': '[' + version + ', )', 'versionCentrallyManaged': True}}
        graph = {identifier.lower(): {'selected': False, 'version': version}, root.lower(): {'selected': True, 'version': '3.8.999'}}
        selected = {root.lower(): {'nupkg': root_archive.name, 'dependency_groups': [{'framework': framework,
            'dependencies': [{'id': identifier, 'version': version, 'include': '', 'exclude': 'Build,Analyzers'}]}]}}
        catalog = {(identifier.lower(), version): {'archive_sha256': metadata.sha256(archive.read_bytes()),
                                                  'content_hash': 'original-native-content-hash'}}
        return {'assets': assets, 'original_assets': original, 'catalog': catalog, 'cache': cache,
                'folder': folder, 'archive': archive, 'graph': graph, 'selected': selected,
                'framework': framework, 'key': key, 'root_key': root_key, 'props': props, 'dll': dll}

    def verify_empty_build(self, values):
        """Verify restored payloads using the build-marker fixture's original assets and catalog."""
        return proof.verify_asset_payloads(values['assets'], values['framework'], values['graph'], values['cache'],
            self.artifacts, values['selected'], catalog=values['catalog'], original_assets=values['original_assets'])

    def test_source_derived_build_exclusion_is_inert_for_each_supported_framework(self):
        """Verify source derived build exclusion is inert for each supported framework."""
        for framework in ('net8.0', 'net9.0', 'net10.0'):
            values = self.empty_build_fixture(framework)
            marker = self.verify_empty_build(values)[-1]['payloads'][-1]
            with self.subTest(framework=framework):
                self.assertEqual(marker, {'path': f'build/{framework}/_._', 'kind': 'build',
                    'accounting': proof.EMPTY_BUILD_ACCOUNTING, 'metadata': {},
                    'archive_sha256': metadata.sha256(values['archive'].read_bytes()), 'origin': proof.EMPTY_BUILD_ORIGIN})
                self.assertNotIn('size', marker)
                self.assertNotIn('sha256', marker)

    def test_build_marker_requires_unique_excluding_actual_root_edge_and_default_flags(self):
        """Verify build marker requires unique excluding actual root edge and default flags."""
        values = self.empty_build_fixture()
        for change in ('include_without_exclude', 'unknown_include', 'unknown_exclude', 'direct_flags', 'extra_direct',
                       'wrong_root_version', 'nonpackage_root', 'second_incoming', 'duplicate_edge', 'edge_range',
                       'wrong_group', 'original_flags', 'original_transitive', 'original_buildtransitive',
                       'original_multitargeting', 'original_transitive_group', 'dependency_group', 'external_direct',
                       'original_dependency_group'):
            changed = deepcopy(values)
            assets, original = changed['assets'], changed['original_assets']
            root = changed['root_key'].rsplit('/', 1)[0]
            edge = next(iter(changed['selected'].values()))['dependency_groups'][0]['dependencies'][0]
            direct = assets['project']['frameworks']['net8.0']['dependencies']
            target = assets['targets']['net8.0'][changed['root_key']]
            if change == 'include_without_exclude': edge.update(include='All', exclude='Analyzers')
            elif change == 'unknown_include': edge['include'] = 'Unknown'
            elif change == 'unknown_exclude': edge['exclude'] += ',Unknown'
            elif change == 'direct_flags': direct[root]['include'] = 'All'
            elif change == 'extra_direct': direct['Extra'] = {'target': 'Package', 'version': '[1.0.0, 1.0.0]'}
            elif change == 'external_direct': direct[edge['id']] = {'target': 'Package', 'version': '[8.0.24, 8.0.24]'}
            elif change == 'dependency_group': assets['projectFileDependencyGroups']['net8.0'].append(edge['id'] + ' >= 8.0.24')
            elif change == 'original_dependency_group': original['projectFileDependencyGroups']['net8.0'] = []
            elif change == 'wrong_root_version': direct[root]['version'] = '[3.8.998, 3.8.998]'
            elif change == 'nonpackage_root': target['type'] = 'project'
            elif change == 'second_incoming':
                assets['targets']['net8.0']['Other/1.0.0'] = {'type': 'package', 'dependencies': {edge['id']: edge['version']}}
                changed['graph']['other'] = {'selected': False, 'version': '1.0.0'}
            elif change == 'duplicate_edge': target['dependencies'][edge['id'].lower()] = edge['version']
            elif change == 'edge_range': target['dependencies'][edge['id']] = '[8.0.24]'
            elif change == 'wrong_group': next(iter(changed['selected'].values()))['dependency_groups'][0]['framework'] = 'net9.0'
            elif change == 'original_flags': original['project']['frameworks']['net8.0']['dependencies'][edge['id']]['exclude'] = 'BuildTransitive'
            elif change == 'original_transitive': original['project']['frameworks']['net8.0']['dependencies'].clear()
            elif change == 'original_multitargeting': original['targets']['net8.0'][changed['key']]['buildMultiTargeting'] = {'buildMultiTargeting/Example.props': {}}
            elif change == 'original_transitive_group': original['targets']['net8.0'][changed['key']]['buildTransitive'] = {'buildTransitive/Example.props': {}}
            else: original['targets']['net8.0'][changed['key']]['build']['buildTransitive/Example.props'] = {}
            with self.subTest(change=change), self.assertRaisesRegex(ValueError, 'consumer_synthetic_build_'):
                self.verify_empty_build(changed)
        # Native subtracts exclude after include; explicit All cannot undo excluded Build.
        values['selected'][root.lower()]['dependency_groups'][0]['dependencies'][0]['include'] = 'All'
        self.verify_empty_build(values)

    def test_build_marker_rejects_wrong_origin_bytes_groups_and_metadata(self):
        """Verify build marker rejects wrong origin bytes groups and metadata."""
        values = self.empty_build_fixture()
        for change in ('snapshot', 'framework', 'version', 'native_hash', 'archive_hash', 'inventory',
                       'original_missing', 'original_marker', 'metadata', 'mixed', 'mixed-reversed', 'path', 'selected'):
            changed = deepcopy(values)
            original = changed['original_assets']; target = changed['assets']['targets']['net8.0'][changed['key']]
            group = original['targets']['net8.0'][changed['key']]['build']
            if change == 'snapshot': changed['original_assets'] = None
            elif change == 'framework': original['targets']['net9.0'] = original['targets'].pop('net8.0')
            elif change == 'version': original['libraries'].clear()
            elif change == 'native_hash': original['libraries'][changed['key']]['sha512'] = 'wrong'
            elif change == 'archive_hash': next(iter(changed['catalog'].values()))['archive_sha256'] = 'f' * 64
            elif change == 'inventory': original['libraries'][changed['key']]['files'] = []
            elif change == 'original_missing': group['build/net8.0/Missing.props'] = {}
            elif change == 'original_marker': group.clear(); group['build/net8.0/_._'] = {}
            elif change == 'metadata': target['build']['build/net8.0/_._']['unexpected'] = False
            elif change.startswith('mixed'):
                target['build'][changed['props']] = {}
                if change == 'mixed-reversed': target['build'] = dict(reversed(list(target['build'].items())))
            elif change == 'path': target['build'] = {'build/net8.0/arbitrary_._': {}}
            else:
                folded = changed['key'].rsplit('/', 1)[0].lower()
                changed['graph'][folded]['selected'] = True
                changed['selected'][folded] = {'nupkg': changed['archive'].name}
                (self.artifacts / changed['archive'].name).write_bytes(changed['archive'].read_bytes())
            with self.subTest(change=change), self.assertRaises(ValueError): self.verify_empty_build(changed)
        (values['folder'] / values['dll']).write_bytes(b'changed DLL')
        with self.assertRaisesRegex(ValueError, 'consumer_input_hash'): self.verify_empty_build(values)

    def test_build_marker_physical_members_and_cache_collisions_remain_byte_checked(self):
        """Verify build marker physical members and cache collisions remain byte checked."""
        for change in ('cache', 'case_cache', 'symlink', 'case_archive', 'real_archive'):
            values = self.empty_build_fixture(); path = values['folder'] / 'build/net8.0/_._'
            if change == 'cache': path.write_bytes(b'not synthetic')
            elif change == 'case_cache':
                path = values['folder'] / 'BUILD/net8.0/_._'
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(b'case-folded physical marker')
            elif change == 'symlink': path.symlink_to(values['folder'] / values['props'])
            else:
                name = 'build/net8.0/_._' if change == 'real_archive' else 'BUILD/NET8.0/_._'
                with zipfile.ZipFile(values['archive'], 'a') as package: package.writestr(name, b'physical marker')
                next(iter(values['catalog'].values()))['archive_sha256'] = metadata.sha256(values['archive'].read_bytes())
            with self.subTest(change=change), self.assertRaises(ValueError): self.verify_empty_build(values)
            if change == 'real_archive':
                path.write_bytes(b'physical marker'); rows = self.verify_empty_build(values)
                self.assertEqual(rows[-1]['payloads'][-1]['sha256'], metadata.sha256(b'physical marker'))
                self.assertNotIn('accounting', rows[-1]['payloads'][-1])
                path.write_bytes(b'changed physical marker')
                with self.assertRaisesRegex(ValueError, 'consumer_input_hash'): self.verify_empty_build(values)

    def empty_content_fixture(self):
        """Create archive and restore evidence for an original excluded content marker."""
        identifier, version = 'Microsoft.AspNetCore.Components.CustomElements', '9.0.13'
        folded, key = identifier.lower(), identifier + '/' + version
        cache = self.root / 'marker-cache'
        folder = cache / folded / version
        folder.mkdir(parents=True)
        archive = folder / (folded + '.' + version + '.nupkg')
        entries = {'contentFiles/any/net9.0/js/package.json': b'{"name":"original-content"}',
                   'lib/net9.0/CustomElements.dll': b'genuine assembly'}
        with zipfile.ZipFile(archive, 'w') as package:
            for name, data in entries.items():
                package.writestr(name, data)
        for name, data in entries.items():
            path = folder / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
        assets = {'targets': {'net9.0': {key: {'type': 'package',
            'contentFiles': {proof.EMPTY_CONTENT: dict(proof.EMPTY_CONTENT_METADATA)},
            'compile': {'lib/net9.0/CustomElements.dll': {}}}}},
            'libraries': {key: {'type': 'package', 'sha512': 'original-native-content-hash', 'files': list(entries)}},
            'project': {'frameworks': {'net9.0': {'dependencies': {
                'Elsa.Studio.Core': {'target': 'Package', 'version': '[3.8.999, 3.8.999]'}}}}}}
        catalog = {(folded, version): {'archive_sha256': metadata.sha256(archive.read_bytes()),
                                     'content_hash': 'original-native-content-hash'}}
        graph = {folded: {'selected': False}, 'elsa.studio.core': {'selected': True, 'version': '3.8.999'}}
        return assets, deepcopy(assets), catalog, cache, folder, archive, graph, key

    def verify_empty_content(self, values):
        """Verify restored payloads against the original content-marker fixture."""
        assets, original, catalog, cache, folder, archive, graph, _ = values
        selected = {key: {'nupkg': archive.name} for key, row in graph.items() if row['selected'] and key == values[-1].split('/')[0].lower()}
        return proof.verify_asset_payloads(assets, 'net9.0', graph, cache, folder if selected else self.artifacts, selected,
                                          catalog=catalog, original_assets=original)

    def test_original_native_excluded_content_marker_is_accounted_without_fake_bytes(self):
        """Verify original native excluded content marker is accounted without fake bytes."""
        values = self.empty_content_fixture()
        rows = self.verify_empty_content(values)
        marker = next(row for row in rows[0]['payloads'] if row['path'] == proof.EMPTY_CONTENT)
        self.assertEqual(marker, {'path': proof.EMPTY_CONTENT, 'kind': 'contentFiles',
            'accounting': proof.EMPTY_CONTENT_ACCOUNTING, 'metadata': proof.EMPTY_CONTENT_METADATA,
            'archive_sha256': metadata.sha256(values[5].read_bytes()), 'origin': 'original-excluded-content'})
        self.assertNotIn('size', marker)
        self.assertNotIn('sha256', marker)
        (values[4] / 'lib/net9.0/CustomElements.dll').write_bytes(b'changed')
        with self.assertRaisesRegex(ValueError, 'consumer_input_hash'):
            self.verify_empty_content(values)

    def test_original_direct_real_content_can_be_excluded_at_the_package_only_boundary(self):
        """Verify original direct real content can be excluded at the package only boundary."""
        values = self.empty_content_fixture()
        original = values[1]['targets']['net9.0'][values[-1]]
        original['contentFiles'] = {'contentFiles/any/net9.0/js/package.json':
            {'buildAction': 'Content', 'codeLanguage': 'any', 'copyToOutput': False}}
        rows = self.verify_empty_content(values)
        marker = next(row for row in rows[0]['payloads'] if 'accounting' in row)
        self.assertEqual(marker['origin'], 'package-reference-transitive-content-exclusion')
        for change in ('missing_member', 'direct_external', 'extra_direct', 'flags', 'version'):
            assets = deepcopy(values[0]); source = deepcopy(values[1])
            dependencies = assets['project']['frameworks']['net9.0']['dependencies']
            if change == 'missing_member': source['targets']['net9.0'][values[-1]]['contentFiles']['contentFiles/any/net9.0/missing.js'] = dict(proof.EMPTY_CONTENT_METADATA)
            elif change == 'direct_external': dependencies.clear(); dependencies[values[-1].split('/')[0]] = {'target': 'Package', 'version': '[9.0.13, 9.0.13]'}
            elif change == 'extra_direct': dependencies['Extra'] = {'target': 'Package', 'version': '[1.0.0, 1.0.0]'}
            elif change == 'flags': dependencies['Elsa.Studio.Core']['include'] = 'all'
            else: dependencies['Elsa.Studio.Core']['version'] = '[3.8.998, 3.8.998]'
            with self.subTest(change=change), self.assertRaises(ValueError):
                self.verify_empty_content((assets, source, *values[2:]))

    def test_synthetic_marker_requires_exact_original_root_context_archive_and_native_metadata(self):
        """Verify synthetic marker requires exact original root context archive and native
        metadata.
        """
        values = self.empty_content_fixture()
        baseline_assets, baseline_original, baseline_catalog = deepcopy(values[:3])
        for change in ('original', 'framework', 'version', 'content_hash', 'archive_hash', 'inventory',
                       'action', 'copy', 'copy_integer', 'extra', 'path', 'kind', 'selected'):
            assets, original, catalog = deepcopy((baseline_assets, baseline_original, baseline_catalog))
            key = values[-1]; target = assets['targets']['net9.0'][key]
            graph = deepcopy(values[-2])
            if change == 'original': original['targets']['net9.0'][key]['contentFiles'] = {}
            elif change == 'framework': original['targets']['net8.0'] = original['targets'].pop('net9.0')
            elif change == 'version': original['targets']['net9.0']['Microsoft.AspNetCore.Components.CustomElements/9.0.14'] = original['targets']['net9.0'].pop(key)
            elif change == 'content_hash': original['libraries'][key]['sha512'] = 'changed'
            elif change == 'archive_hash': next(iter(catalog.values()))['archive_sha256'] = '0' * 64
            elif change == 'inventory': original['libraries'][key]['files'] = []
            elif change == 'action': target['contentFiles'][proof.EMPTY_CONTENT]['buildAction'] = 'Content'
            elif change == 'copy': target['contentFiles'][proof.EMPTY_CONTENT]['copyToOutput'] = True
            elif change == 'copy_integer': target['contentFiles'][proof.EMPTY_CONTENT]['copyToOutput'] = 0
            elif change == 'extra': target['contentFiles'][proof.EMPTY_CONTENT]['outputPath'] = 'output'
            elif change == 'path': target['contentFiles']['contentFiles/any/any/arbitrary_._'] = target['contentFiles'].pop(proof.EMPTY_CONTENT)
            elif change == 'kind': target['runtime'] = target.pop('contentFiles')
            else: graph[next(iter(graph))]['selected'] = True
            with self.subTest(change=change), self.assertRaises(ValueError):
                self.verify_empty_content((assets, original, catalog, *values[3:6], graph, key))

    def test_synthetic_marker_rejects_cache_entries_symlinks_and_archive_case_collisions(self):
        """Verify synthetic marker rejects cache entries symlinks and archive case collisions."""
        values = self.empty_content_fixture(); marker = values[4] / proof.EMPTY_CONTENT
        marker.parent.mkdir(parents=True)
        for symlink in (False, True):
            if symlink: marker.symlink_to(self.root / 'nonexistent')
            else: marker.write_bytes(b'not synthetic')
            with self.subTest(symlink=symlink), self.assertRaisesRegex(ValueError, 'consumer_synthetic_content_cache'):
                self.verify_empty_content(values)
            marker.unlink()
        with zipfile.ZipFile(values[5], 'a') as package:
            package.writestr(proof.EMPTY_CONTENT.upper(), b'collision')
        next(iter(values[2].values()))['archive_sha256'] = metadata.sha256(values[5].read_bytes())
        with self.assertRaisesRegex(ValueError, 'consumer_synthetic_content_collision'):
            self.verify_empty_content(values)

    def test_real_archive_empty_marker_still_requires_extracted_byte_identity(self):
        """Verify real archive empty marker still requires extracted byte identity."""
        values = self.empty_content_fixture()
        with zipfile.ZipFile(values[5], 'a') as package:
            package.writestr(proof.EMPTY_CONTENT, b'physical marker')
        with self.assertRaisesRegex(ValueError, 'consumer_input_path'):
            self.verify_empty_content(values)
        marker = values[4] / proof.EMPTY_CONTENT
        marker.parent.mkdir(parents=True, exist_ok=True); marker.write_bytes(b'physical marker')
        row = self.verify_empty_content(values)[0]['payloads'][1]
        self.assertNotIn('accounting', row)
        marker.write_bytes(b'changed')
        with self.assertRaisesRegex(ValueError, 'consumer_input_hash'):
            self.verify_empty_content(values)

    def run_private_consumer(self, freshness_error=None, *, retire_caches=False, command_error=None, cli=False):
        """Run a mocked consumer flow with optional prerequisite, command, or cache-retirement
        failures.
        """
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
            if args[1] == command_error:
                raise ValueError('injected_' + command_error)
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

        def catalog(*args, **kwargs):
            args[-1].mkdir()
            (args[-1] / 'catalog.private.json').write_text('{}')
            return {}, {'sources': self.feeds['sources'], 'mapping': {'elsa.studio.core': []},
                        'mirrors': {'original': str(self.root / 'original-mirror')}, 'sdk_downloads': {},
                        'original_assets': args[0],
                        'sdk_projects': {self.policy['project']: {'net8.0': {'pruning': {}, 'downloads': []}}}}

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
                    metadata.sha256(receipt_path.read_bytes()), self.artifacts, self.root / 'unused', output,
                    retire_caches=retire_caches)
            if cli:
                arguments = ['consumer']
                for name, value in {'plan': plan_path, 'plan-sha256': self.hash, 'artifact-receipt': receipt_path,
                    'artifact-receipt-sha256': metadata.sha256(receipt_path.read_bytes()), 'artifacts': self.artifacts,
                    'planning-assets': self.root / 'unused', 'output': output}.items():
                    arguments.extend(['--' + name, str(value)])
                stdout = io.StringIO()
                with patch.object(proof, 'ROOT', self.controller_root), patch.object(sys, 'argv', arguments), redirect_stdout(stdout):
                    self.assertEqual(1, proof.main())
                self.cli_diagnostic = json.loads(stdout.getvalue())
            elif freshness_error:
                with self.assertRaisesRegex(ValueError, str(freshness_error)):
                    execute()
            elif command_error:
                with self.assertRaisesRegex(ValueError, 'injected_' + command_error):
                    execute()
            else:
                execute()
            if freshness_error:
                self.assertFalse(inspector_mock.called)
                self.assertFalse(catalog_mock.called)
                self.assertEqual([], commands)
        data = (output / 'retained/receipt.json').read_text()
        return json.loads(data), data, output, dll

    def test_current_ineligible_prerequisites_block_all_consumer_and_inspector_work(self):
        """Verify current ineligible prerequisites block all consumer and inspector work."""
        receipt, _, _, _ = self.run_private_consumer(ValueError('artifact_fresh_prerequisite_failed'))
        self.assertFalse(receipt['success'])
        self.assertFalse(receipt['current_consumer_admission']['eligible'])
        self.assertEqual('current-consumer-prerequisites', receipt['stage'])

    def test_cli_reports_real_execute_failure_stage_before_inspector_or_cell_work(self):
        """Verify CLI reports real execute failure stage before inspector or cell work."""
        receipt, _, _, _ = self.run_private_consumer(ValueError('artifact_fresh_prerequisite_failed'), cli=True)
        self.assertFalse(receipt['success'])
        self.assertEqual(self.cli_diagnostic, {'success': False, 'failure_code': 'artifact_fresh_prerequisite_failed',
            'failure_stage': 'current-consumer-prerequisites', 'retained_receipt_created': True})

    def test_cli_reports_fixed_cell_stage_for_unclassified_command_failure(self):
        """Verify CLI reports fixed cell stage for unclassified command failure."""
        receipt, _, _, _ = self.run_private_consumer(command_error='build', cli=True)
        self.assertFalse(receipt['success'])
        self.assertEqual(self.cli_diagnostic, {'success': False, 'failure_code': 'consumer_control_failed',
            'failure_stage': 'selected-restore-compile', 'retained_receipt_created': True})

    def test_current_prerequisite_metadata_drift_blocks_consumer_work(self):
        """Verify current prerequisite metadata drift blocks consumer work."""
        receipt, _, _, _ = self.run_private_consumer(ValueError('artifact_prerequisite_metadata_changed'))
        self.assertFalse(receipt['success'])
        self.assertFalse(receipt['current_consumer_admission']['eligible'])
        self.assertEqual('current-consumer-prerequisites', receipt['stage'])

    def test_full_retained_receipt_excludes_private_runtime_and_archive_paths(self):
        """Verify full retained receipt excludes private runtime and archive paths."""
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
        for cell in (output / 'private').iterdir():
            if (cell / 'Consumer.csproj').exists():
                self.assertTrue((cell / 'packages').is_dir())
                self.assertTrue((cell / 'discovery/packages').is_dir())
                self.assertFalse((cell / 'cell-proof.private.json').exists())

    def test_opt_in_retires_only_after_compile_and_runtime_proof(self):
        """Verify opt in retires only after compile and runtime proof."""
        receipt, _, output, _ = self.run_private_consumer(retire_caches=True)
        self.assertTrue(receipt['success'])
        for completed in receipt['coverage'] + receipt['runtime']:
            name = ('runtime-' + completed['framework'] if 'runtime' in completed else
                    metadata.sha256((completed['id'] + '/' + completed['framework']).encode())[:16])
            cell = output / 'private' / name
            self.assertEqual(completed, json.loads((cell / 'cell-proof.private.json').read_text()))
            self.assertFalse((cell / 'packages').exists())
            self.assertFalse((cell / 'discovery/packages').exists())
            for path in ('Consumer.csproj', 'NuGet.Config', 'packages.lock.json', 'obj/project.assets.json',
                         'discovery/obj/project.assets.json', 'discovery/packages.lock.json'):
                self.assertTrue((cell / path).is_file(), path)
        self.assertTrue((output / 'private/runtime-net8.0/runtime.log').is_file())
        self.assertTrue((output / 'private/runtime-net8.0/bin/Release/net8.0/Elsa.Studio.Core.dll').is_file())
        self.assertTrue((output / 'private/original-external-catalog/catalog.private.json').is_file())
        self.assertTrue(next(self.artifacts.glob('*.nupkg')).is_file())

    def test_failed_build_never_retires_its_caches_or_saves_completed_proof(self):
        """Verify failed build never retires its caches or saves completed proof."""
        receipt, _, output, _ = self.run_private_consumer(retire_caches=True, command_error='build')
        self.assertFalse(receipt['success'])
        self.assertEqual([], receipt['coverage'])
        name = metadata.sha256(b'Elsa.Studio.Core/net8.0')[:16]
        cell = output / 'private' / name
        self.assertTrue((cell / 'packages').is_dir())
        self.assertTrue((cell / 'discovery/packages').is_dir())
        self.assertFalse((cell / 'cell-proof.private.json').exists())

    def test_failed_runtime_never_retires_its_caches(self):
        """Verify failed runtime never retires its caches."""
        receipt, _, output, _ = self.run_private_consumer(retire_caches=True, command_error='run')
        self.assertFalse(receipt['success'])
        self.assertEqual([], receipt['runtime'])
        cell = output / 'private/runtime-net8.0'
        self.assertTrue((cell / 'packages').is_dir())
        self.assertTrue((cell / 'discovery/packages').is_dir())
        self.assertFalse((cell / 'cell-proof.private.json').exists())

    def test_retirement_failure_cannot_report_complete_acceptance(self):
        """Verify retirement failure cannot report complete acceptance."""
        with patch.object(proof, 'retire_successful_cell_caches', side_effect=ValueError('injected_retirement')):
            receipt, _, _, _ = self.run_private_consumer(retire_caches=True, command_error='retirement')
        self.assertFalse(receipt['success'])
        self.assertEqual([], receipt['coverage'])
        self.assertEqual('selected-restore-compile-failed', receipt['failure_code'])

    def test_cli_cache_retirement_is_explicitly_opt_in(self):
        """Verify CLI cache retirement is explicitly opt in."""
        arguments = ['consumer', '--plan', 'plan', '--plan-sha256', 'a'*64, '--artifact-receipt', 'receipt',
                     '--artifact-receipt-sha256', 'b'*64, '--artifacts', 'archives', '--planning-assets', 'snapshots',
                     '--output', 'output']
        for enabled in (False, True):
            with self.subTest(enabled=enabled), patch('sys.argv', arguments +
                    (['--retire-successful-cell-caches'] if enabled else [])), patch.object(proof, 'execute') as execute:
                self.assertEqual(0, proof.main())
                self.assertIs(enabled, execute.call_args.kwargs['retire_caches'])

    def test_coverage_ledger_requires_every_selected_tfm_once_and_success(self):
        """Verify coverage ledger requires every selected target framework once and success."""
        valid = [{'id': 'Example', 'framework': framework, 'success': True} for framework in self.policy['frameworks']]
        proof.validate_ledger(self.plan, valid)
        for invalid in (valid[:1], valid + valid[:1], [valid[0], valid[1] | {'success': False}]):
            with self.assertRaisesRegex(ValueError, 'consumer_coverage_ledger'):
                proof.validate_ledger(self.plan, invalid)

    def test_compile_witness_has_exact_single_package_and_no_projectreference(self):
        """Verify compile witness has exact single package and no ProjectReference."""
        project = ET.fromstring(proof.render_project('Example', '3.8.999', 'net8.0', ['Microsoft.AspNetCore.App'], executable=False, managed=True))
        packages = project.findall('ItemGroup/PackageReference')
        self.assertEqual(1, len(packages))
        self.assertEqual({'Include': 'Example', 'Version': '[3.8.999]', 'Aliases': 'selected'}, packages[0].attrib)
        self.assertEqual([], project.findall('.//ProjectReference'))
        self.assertEqual('true', project.findtext('PropertyGroup/RestoreLockedMode'))


class ConsumerCliDiagnosticsTests(unittest.TestCase):
    def setUp(self):
        """Create isolated input and output paths for consumer CLI diagnostic tests."""
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.plan = self.root / 'plan.json'
        self.plan.write_bytes(b'{}')
        self.output = self.root / 'output'

    def invoke(self):
        """Run the failing consumer CLI and parse its public JSON diagnostic."""
        arguments = ['consumer', '--plan', str(self.plan), '--plan-sha256', 'f' * 64,
            '--artifact-receipt', str(self.root / 'producer.json'), '--artifact-receipt-sha256', 'e' * 64,
            '--artifacts', str(self.root / 'artifacts'), '--planning-assets', str(self.root / 'snapshots'),
            '--output', str(self.output)]
        stdout = io.StringIO()
        with patch.object(sys, 'argv', arguments), redirect_stdout(stdout):
            status = proof.main()
        self.assertEqual(status, 1)
        return json.loads(stdout.getvalue())

    def test_genuine_early_input_hash_failure_has_closed_code_and_no_receipt(self):
        """Verify genuine early input hash failure has closed code and no receipt."""
        self.assertEqual(self.invoke(), {'success': False, 'failure_code': 'consumer_input_hash',
            'failure_stage': 'unclassified', 'retained_receipt_created': False})
        self.assertFalse(self.output.exists())

    def failed_receipt(self, error, *, changes=None, data=None):
        """Return a callback that writes a synthetic failed receipt before raising the supplied
        error.
        """
        def fail(*args, **kwargs):
            retained = self.output / 'retained'
            retained.mkdir(parents=True)
            receipt = {'schema': 1, 'mode': 'selected-product-consumers', 'success': False,
                'stage': 'current-consumer-prerequisites', 'failure_code': 'current-consumer-prerequisites-failed',
                'private': '/private/secret/token'}
            receipt.update(changes or {})
            (retained / 'receipt.json').write_bytes(data if data is not None else json.dumps(receipt).encode())
            raise error
        return fail

    def test_new_failed_receipt_reports_only_fixed_stage_and_literal_exception_code(self):
        """Verify new failed receipt reports only fixed stage and literal exception code."""
        with patch.object(proof, 'execute', side_effect=self.failed_receipt(ValueError('artifact_fresh_prerequisite_failed'))):
            self.assertEqual(self.invoke(), {'success': False, 'failure_code': 'artifact_fresh_prerequisite_failed',
                'failure_stage': 'current-consumer-prerequisites', 'retained_receipt_created': True})

    def test_native_sdk_and_retirement_literal_codes_are_public_without_raw_details(self):
        """Verify native SDK and retirement literal codes are public without raw details."""
        for code in ('consumer_native_content_hash', 'consumer_sdk_download_bytes', 'consumer_cache_retirement_path'):
            with self.subTest(code=code), patch.object(proof, 'execute', side_effect=ValueError(code)):
                self.assertEqual(self.invoke(), {'success': False, 'failure_code': code,
                    'failure_stage': 'unclassified', 'retained_receipt_created': False})

    def bounded_range_error(self):
        """Create a dependency-range error with enough rejected edges to exercise diagnostic
        limits.
        """
        error = ValueError('consumer_dependency_range_conflict')
        ranges = [{'range': '[9.0.0, 9.0.999]', 'version': '10.0.9'}] * 15
        edges = [('Pomelo.EntityFrameworkCore.MySql', 'Microsoft.EntityFrameworkCore.Relational', False)] * 5
        proof.resolution.attach_range_failure(error, 'Elsa.Agents.Persistence.EFCore.MySql', 'net10.0',
            'discovery', edges, ranges, [{'satisfies': False}] * 15)
        return error

    def test_range_samples_are_bounded_and_never_include_private_exception_attributes(self):
        """Verify range samples are bounded and never include private exception attributes."""
        error = self.bounded_range_error()
        error.private = '/private/token'
        with patch.object(proof, 'execute', side_effect=error):
            result = self.invoke()
        detail = result['dependency_range_failure']
        self.assertEqual(15, detail['unsatisfied_checks'])
        self.assertEqual(8, len(detail['ranges']))
        self.assertEqual({'original-nuspec', 'assets', 'lock'}, {row['source_kind'] for row in detail['ranges']})
        self.assertNotIn('/private/token', json.dumps(result))

    def test_malformed_range_details_are_omitted_without_changing_original_failure(self):
        """Verify malformed range details are omitted without changing original failure."""
        valid = self.bounded_range_error().consumer_range_failure
        changes = ({'root_id': '/private/token'}, {'root_id': 'x' * 101}, {'framework': 'net10.0 /private/token'},
            {'phase': 'restore --secret'}, {'reason': 'private-token'}, {'requested_checks': True},
            {'returned_checks': -1}, {'unsatisfied_checks': 16}, {'ranges': valid['ranges'] * 2},
            {'private': '/private/token'}, {'ranges': None})
        malformed = [None, [], 'private-token'] + [valid | change for change in changes]
        for key, value in (('from_id', '/private/token'), ('to_id', 'x' * 101),
                ('requested_range', '[9.0.0, /private/token]'), ('requested_range', '[9.0.0,\n10.0.0]'),
                ('requested_range', 'x' * 257), ('resolved_version', '/private/token'),
                ('source_kind', 'private-cache-path'), ('private', '/private/token')):
            detail = deepcopy(valid)
            detail['ranges'][0][key] = value
            malformed.append(detail)
        for detail in malformed:
            error = ValueError('consumer_dependency_range_conflict')
            error.consumer_range_failure = detail
            with self.subTest(detail=detail), patch.object(proof, 'execute', side_effect=error):
                self.assertEqual(self.invoke(), {'success': False, 'failure_code': 'consumer_dependency_range_conflict',
                    'failure_stage': 'unclassified', 'retained_receipt_created': False})

    def test_range_details_on_wrong_exception_types_or_codes_stay_private(self):
        """Verify range details on wrong exception types or codes stay private."""
        class SpecificError(ValueError):
            pass
        detail = self.bounded_range_error().consumer_range_failure
        for error in (SpecificError('consumer_dependency_range_conflict'), RuntimeError('consumer_dependency_range_conflict'),
                ValueError('consumer_input_hash')):
            error.consumer_range_failure = detail
            with self.subTest(error=error), patch.object(proof, 'execute', side_effect=error):
                self.assertNotIn('dependency_range_failure', self.invoke())

    def test_arbitrary_exception_strings_and_valueerror_subclasses_stay_generic(self):
        """Verify arbitrary exception strings and valueerror subclasses stay generic."""
        class SpecificError(ValueError):
            pass
        for error in (ValueError('consumer_input_hash /private/token'), RuntimeError('consumer_input_hash'),
                      SpecificError('consumer_input_hash'), OSError('/private/token')):
            with self.subTest(error=error), patch.object(proof, 'execute', side_effect=error):
                self.assertEqual(self.invoke(), {'success': False, 'failure_code': 'consumer_control_failed',
                    'failure_stage': 'unclassified', 'retained_receipt_created': False})

    def test_malformed_or_unreviewed_receipt_fields_never_become_public_stages(self):
        """Verify malformed or unreviewed receipt fields never become public stages."""
        changes = ({'stage': 'selected-restore-compile /private/token'}, {'stage': 'complete'}, {'stage': []},
            {'schema': True}, {'mode': 'selected-product-artifact-control'}, {'success': True},
            {'failure_code': 'current-consumer-prerequisites-failed /private/token'})
        for index, change in enumerate(changes):
            self.output = self.root / str(index)
            with self.subTest(change=change), patch.object(proof, 'execute',
                    side_effect=self.failed_receipt(RuntimeError('/private/token'), changes=change)):
                self.assertEqual(self.invoke(), {'success': False, 'failure_code': 'consumer_control_failed',
                    'failure_stage': 'unclassified', 'retained_receipt_created': True})
        self.output = self.root / 'malformed'
        with patch.object(proof, 'execute', side_effect=self.failed_receipt(RuntimeError('/private/token'), data=b'private-not-json')):
            self.assertEqual(self.invoke()['failure_stage'], 'unclassified')

    def test_oversized_receipt_is_not_parsed_or_reflected(self):
        """Verify oversized receipt is not parsed or reflected."""
        def fail(*args, **kwargs):
            retained = self.output / 'retained'; retained.mkdir(parents=True)
            with (retained / 'receipt.json').open('wb') as receipt:
                receipt.truncate(64 * 1024 ** 2 + 1)
            raise RuntimeError('/private/token')
        with patch.object(proof, 'execute', side_effect=fail):
            self.assertEqual(self.invoke(), {'success': False, 'failure_code': 'consumer_control_failed',
                'failure_stage': 'unclassified', 'retained_receipt_created': True})

    def test_previous_output_and_symlink_receipts_or_ancestors_are_not_new_receipts(self):
        """Verify previous output and symlink receipts or ancestors are not new receipts."""
        for kind in ('previous', 'receipt', 'retained', 'output', 'parent', 'dangling'):
            self.output = self.root / kind
            foreign = self.root / (kind + '-foreign'); foreign.mkdir()
            if kind == 'previous':
                self.output.mkdir()
            elif kind in ('output', 'dangling'):
                self.output.symlink_to(foreign if kind == 'output' else foreign / 'missing', target_is_directory=True)
            elif kind == 'parent':
                parent = self.root / 'linked-parent'; parent.symlink_to(foreign, target_is_directory=True)
                self.output = parent / 'new'
            def fail(*args, **kwargs):
                if kind != 'dangling':
                    retained = self.output / 'retained'
                    retained.mkdir(parents=True, exist_ok=True)
                    if kind == 'retained':
                        retained.rmdir(); retained.symlink_to(foreign, target_is_directory=True)
                    target = retained / 'receipt.json'
                    if kind == 'receipt':
                        (foreign / 'receipt.json').write_bytes(b'private')
                        target.symlink_to(foreign / 'receipt.json')
                    else:
                        target.write_bytes(b'private')
                raise ValueError('consumer_input_hash')
            with self.subTest(kind=kind), patch.object(proof, 'execute', side_effect=fail):
                self.assertEqual(self.invoke(), {'success': False, 'failure_code': 'consumer_input_hash',
                    'failure_stage': 'unclassified', 'retained_receipt_created': False})


if __name__ == '__main__':
    unittest.main()
