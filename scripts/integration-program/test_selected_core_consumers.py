from copy import deepcopy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET
import zipfile

import product_release_metadata as metadata
import prove_product_release_consumers as proof
import selected_core_consumer as core
from selected_maintenance_test_support import patch_offline_local_execution
import test_selected_core_producer as producer_tests


class CoreConsumerContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        local_execution_patch = patch_offline_local_execution(proof)
        local_execution_patch.start()
        cls.addClassCleanup(local_execution_patch.stop)

    def setUp(self):
        fixture = producer_tests.OriginalCoreProducerContracts(); fixture.setUp()
        self.fixture = fixture
        self.rows = fixture.rows
        self.plan = fixture.plan(self.rows[0])
        self.plan['requested_version'] = '3.8.999'
        self.package = self.plan['inventory']['selected'][0]
        self.package.update(id='Elsa', frameworks=list(core.FRAMEWORKS))
        self.package['metadata'] = {'original_output_policy': {
            framework: {'IncludeBuildOutput': 'true'} for framework in core.FRAMEWORKS}}

    def plan_for(self, row):
        plan = self.fixture.plan(row)
        plan['requested_version'] = self.plan['requested_version']
        plan['inventory']['selected'] = deepcopy(self.plan['inventory']['selected'])
        return plan

    def selected(self):
        inventory = [{'path': f'lib/{framework}/Elsa.dll', 'sha256': metadata.sha256(framework.encode())}
                     for framework in core.FRAMEWORKS]
        return {'elsa': {'id': 'Elsa', 'nupkg': 'Elsa.3.8.999.nupkg', 'nupkg_sha256': 'c' * 64,
                         'policy': self.package, 'inventory': inventory,
                         'artifact_files': [{'name': 'Elsa.3.8.999.nupkg', 'sha256': 'c'*64, 'size': 4}]}}

    def native_receipt(self, selected):
        item = selected['elsa']
        return {'package_verification': [{'id': 'Elsa', 'version': self.plan['requested_version'],
            'assembly_name': 'Elsa', 'frameworks': list(core.FRAMEWORKS), 'include_build_output': True, 'satellites': [],
            'files': [{'name': item['nupkg'], 'sha256': item['nupkg_sha256'], 'size': 4}],
            'symbols': [{'assembly': entry['path'], 'assembly_sha256': entry['sha256'],
                'assembly_version': '1.0.1.0', 'informational_version': '1.0.1+' + self.rows[0]['commit']}
                for entry in item['inventory']]}]}

    def test_source_and_fixture_pins_both_original_lines_without_native_execution(self):
        for row in self.rows:
            plan = self.plan_for(row)
            core.validate_plan(plan)
            core.verify_source(proof.ROOT, plan)
        with patch.object(core, 'FIXTURE', core.FIXTURE.with_name('README.md')):
            with self.assertRaisesRegex(ValueError, 'core_consumer_fixture_hash'):
                core.verify_source(proof.ROOT, self.plan)
        with patch.object(core.metadata, 'sha256', return_value='changed'):
            with self.assertRaisesRegex(ValueError, 'core_consumer_source_contract'):
                core.verify_source(proof.ROOT, self.plan)

    def test_only_original_core_source_kind_and_both_lines_admitted(self):
        for change in ({'kind': 'maintenance'}, {'commit': 'a' * 40}, {'tree': 'b' * 40}, {'line': '3.10'}):
            plan = deepcopy(self.plan); plan['source'].update(change)
            with self.subTest(change=change), self.assertRaises(ValueError):
                core.validate_plan(plan)
        with self.assertRaisesRegex(ValueError, 'consumer_control_not_implemented'):
            proof.runtime_contract({'product': 'unknown'})

    def test_runtime_real_workflow_checks_and_exact_single_root_for_all_three_tfms(self):
        for row in self.rows:
            contract = proof.runtime_contract(self.plan_for(row))
            self.assertEqual('elsa', contract['package'])
            self.assertEqual({'variableNearestScope': True, 'outputLines': ['Sequence Value'],
                'status': 'Finished', 'subStatus': 'Finished', 'incidents': 0}, contract['checks'])
            self.assertIsNone(contract['assembly_release_version'])
            self.assertIn('Elsa.Workflows.Runtime', contract['required_packages'])
        for framework in core.FRAMEWORKS:
            project = ET.fromstring(proof.render_project('Elsa', '3.8.999', framework, [], executable=True, managed=True))
            self.assertEqual([{'Include': 'Elsa', 'Version': '[3.8.999]'}],
                             [item.attrib for item in project.findall('.//PackageReference')])
            self.assertEqual([], project.findall('.//ProjectReference'))
        selected = self.selected()
        selected.update({name.casefold(): {} for name in core.REQUIRED_ASSEMBLIES[1:]})
        core.validate_runtime(selected)
        for change in (['net10.0'], list(core.FRAMEWORKS) + ['net8.0']):
            selected['elsa']['policy']['frameworks'] = change
            with self.assertRaisesRegex(ValueError, 'core_consumer_runtime_frameworks'):
                core.validate_runtime(selected)

    def test_native_packaged_identity_is_per_asset_and_not_requested_version_default(self):
        selected = self.selected(); receipt = self.native_receipt(selected)
        core.bind_assemblies(self.plan, receipt, selected)
        self.assertEqual('1.0.1.0', selected['elsa']['assembly_policies']['lib/net8.0/Elsa.dll']['assembly_version'])
        for mutate in (
                lambda r: r.clear(),
                lambda r: r['package_verification'].append(deepcopy(r['package_verification'][0])),
                lambda r: r['package_verification'][0]['files'][0].update(sha256='changed'),
                lambda r: r['package_verification'][0]['symbols'].pop(),
                lambda r: r['package_verification'][0]['symbols'].append(deepcopy(r['package_verification'][0]['symbols'][0])),
                lambda r: r['package_verification'][0]['symbols'][0].update(assembly_sha256='changed'),
                lambda r: r['package_verification'][0]['symbols'][0].update(informational_version='1.0.1+'+'b'*40),
                lambda r: r['package_verification'][0].update(version='1.0.1'),
                lambda r: r['package_verification'][0].update(frameworks=['net10.0']),
                lambda r: r['package_verification'][0].update(satellites=[{'package_path': 'lib/net8.0/nl/Elsa.resources.dll', 'sha256': 'a'*64}])):
            changed = deepcopy(receipt); mutate(changed)
            with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                core.bind_assemblies(self.plan, changed, self.selected())

    def test_missing_runtime_root_or_output_only_native_policy_cannot_fabricate_dll_proof(self):
        with self.assertRaisesRegex(ValueError, 'core_consumer_runtime_packages'):
            core.validate_runtime(self.selected())
        selected = self.selected()
        for policy in selected['elsa']['policy']['metadata']['original_output_policy'].values():
            policy['IncludeBuildOutput'] = 'false'
        receipt = self.native_receipt(selected)
        with self.assertRaisesRegex(ValueError, 'core_consumer_native_frameworks'):
            core.bind_assemblies(self.plan, receipt, selected)
        receipt['package_verification'][0].update(include_build_output=False, frameworks=[], symbols=[])
        with self.assertRaisesRegex(ValueError, 'core_consumer_native_dll_inventory'):
            core.bind_assemblies(self.plan, receipt, selected)
        selected['elsa']['inventory'].clear()
        core.bind_assemblies(self.plan, receipt, selected)
        self.assertEqual({}, selected['elsa']['assembly_policies'])

    def test_full_cli_dispatch_keeps_current_gate_before_all_selected_and_runtime_tfms(self):
        with tempfile.TemporaryDirectory() as temporary:
            folder = Path(temporary).resolve()
            plan_bytes, receipt_bytes = b'bound plan', b'bound producer'
            plan_path, receipt_path = folder / 'plan.json', folder / 'producer.json'
            plan_path.write_bytes(plan_bytes); receipt_path.write_bytes(receipt_bytes)
            plan = deepcopy(self.plan)
            plan.update(semantics={'sdk_version': metadata.SDK, 'assemblies': []},
                        consumer_feed_policy={'config_sha256': metadata.sha256(b'original config')})
            selected = self.selected()
            calls = []
            def refresh(*args):
                calls.append('current-gate')
                return {'eligible': True, 'checked_at': proof.planner.now(), 'histories': [], 'prerequisites': []}
            def catalog(*args):
                calls.append('catalog'); args[-1].mkdir()
                (args[-1] / 'catalog.private.json').write_text('{}')
                return {}, {}
            def cell(*args, **kw):
                calls.append(('runtime' if kw.get('runtime') else 'compile', args[2]))
                return {'id': args[1]['id'], 'framework': args[2], 'success': True}
            receipt = {'execution': proof.local_execution({}), 'planner_controller': {}, 'artifact_controller': {}}
            with patch.object(proof, 'admit_producer_stage', return_value=(plan, receipt, {'scope': 'historical-producer-start-only'})), \
                    patch.object(proof.producer, 'verify_controller', return_value={}), \
                    patch.object(proof, 'admit_artifacts', return_value=selected), \
                    patch.object(proof, 'load_snapshots', return_value={}), \
                    patch.object(proof.core, 'verify_source') as source, \
                    patch.object(proof.producer.maintenance, 'git_bytes', return_value=b'original config'), \
                    patch.object(proof.planner, 'build_helper') as helper, \
                    patch.object(proof.producer, 'refresh_remote', side_effect=refresh), \
                    patch.object(proof.resolution, 'build_inspector', return_value=folder / 'unused.dll'), \
                    patch.object(proof.resolution, 'validate_native_tools'), \
                    patch.object(proof.resolution, 'archive_catalog', side_effect=catalog), \
                    patch.object(proof, 'cell', side_effect=cell):
                helper.return_value.call.return_value = []
                result = proof.execute(proof.ROOT, plan_path, metadata.sha256(plan_bytes), receipt_path,
                                       metadata.sha256(receipt_bytes), folder / 'archives', folder / 'snapshots', folder / 'output')
            source.assert_called_once_with(proof.ROOT, plan)
            self.assertEqual(['current-gate', 'catalog'] + [('compile', tfm) for tfm in core.FRAMEWORKS] +
                             [('runtime', tfm) for tfm in core.FRAMEWORKS], calls)
            self.assertTrue(result['success'])
            self.assertEqual(3, len(result['coverage'])); self.assertEqual(3, len(result['runtime']))
            self.assertTrue(result['current_consumer_admission']['eligible'])
            self.assertEqual(core.source_contract(plan), result['runtime_contract_source'])

    def test_loaded_core_dll_joins_native_policy_archive_cache_and_output_without_fallback(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve(); cache = root / 'cache'; archives = root / 'archives'; archives.mkdir()
            entry = 'lib/net8.0/Elsa.dll'; data = b'exact genuine archived DLL'; digest = metadata.sha256(data)
            cached = cache / 'elsa/3.8.999' / entry; cached.parent.mkdir(parents=True); cached.write_bytes(data)
            loaded = root / 'bin/Release/net8.0/Elsa.dll'; loaded.parent.mkdir(parents=True); loaded.write_bytes(data)
            with zipfile.ZipFile(archives / 'Elsa.nupkg', 'w') as archive: archive.writestr(entry, data)
            native = {'assembly_version': '1.0.1.0', 'informational_version': '1.0.1+' + self.rows[0]['commit'], 'sha256': digest}
            selected = {'elsa': {'nupkg': 'Elsa.nupkg', 'assembly_policies': {entry: native}}}
            row = {'name': 'Elsa', 'version': '1.0.1.0', 'informationalVersion': native['informational_version'],
                'fullName': 'Elsa, Version=1.0.1.0, Culture=neutral', 'location': str(loaded), 'sha256': digest}
            assets = {'targets': {'net8.0': {'Elsa/3.8.999': {'runtime': {entry: {}}}}}}
            def verify(records, policies=selected):
                return proof.consumers.verify_loaded_assemblies({'loadedAssemblies': records}, assets, 'net8.0',
                    root, cache, archives, policies, '3.8.999', self.rows[0]['commit'],
                    required_packages=('Elsa',), original_assembly_policies=True)
            self.assertEqual('1.0.1.0', verify([row])[0]['version'])
            for change in ({'version': '3.8.999.0'}, {'informationalVersion': '3.8.999+' + self.rows[0]['commit']},
                           {'sha256': 'a'*64}, {'location': str(cached)}):
                with self.subTest(change=change), self.assertRaises(RuntimeError): verify([row | change])
            for native_change in ({'sha256': 'a'*64}, {'assembly_version': '3.8.999.0'}, {'informational_version': '1.0.1+'+'b'*40}):
                changed = deepcopy(selected); changed['elsa']['assembly_policies'][entry].update(native_change)
                with self.assertRaises(RuntimeError): verify([row], changed)
            with self.assertRaises(RuntimeError): verify([row], {'elsa': {'nupkg': 'Elsa.nupkg'}})
            with self.assertRaises(RuntimeError): verify([], selected)
            public = proof.retain_runtime_rows(verify([row]))
            self.assertNotIn('location', json.dumps(public))


if __name__ == '__main__':
    unittest.main()
