from copy import deepcopy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile

import product_release_metadata as metadata
import prove_product_release_artifacts as artifacts
import prove_product_release_consumers as consumers
import prove_consolidated_packages as archives
import selected_extensions_contract as extensions
import selected_maintenance_39 as selected


class Maintenance39Contracts(unittest.TestCase):
    def plan(self, product):
        policy = selected.contract(product)
        identifier = 'Elsa.Studio.Core' if product == 'studio' else 'Elsa.IO.Http'
        return {'product': product, 'line': '3.9', 'requested_version': '3.9.999',
            'controller': {'commit': 'c'*40, 'tree': 'd'*40},
            'source': {'product': product, 'line': '3.9', 'kind': 'admitted-maintenance-descendant',
                       'commit': policy['commit'], 'tree': policy['tree']},
            'npm': {} if product == 'studio' else None,
            'inventory': {'release_recipe': {'solution': policy['solution'], 'workflow': policy['workflow'],
                'sha256': metadata.sha256(b'solution'), 'workflow_sha256': metadata.sha256(b'workflow')},
                'projects': [{'path': path, 'is_test_project': True, 'target_frameworks': frameworks}
                             for path, frameworks in policy['test_projects'].items()],
                'selected': [{'id': identifier, 'frameworks': ['net8.0', 'net9.0', 'net10.0']}],
                'excluded': deepcopy(policy['excluded_canonical'])}}

    def test_exact_git_runtime_recipe_compiler_and_test_pins_for_both_sources(self):
        for product, count, cells in [('studio', 16, 18), ('extensions', 17, 17)]:
            plan = self.plan(product)
            selected.verify_source(artifacts.ROOT, plan)
            tests = selected.contract(product)['test_projects']
            self.assertEqual(count, len(tests)); self.assertEqual(cells, sum(map(len, tests.values())))
        extensions.verify_source(artifacts.ROOT, selected.contract('extensions')['commit'])

    def test_wrong_product_line_source_tree_kind_test_scope_recipe_and_npm_fail_closed(self):
        for product in ('studio', 'extensions'):
            original = self.plan(product)
            for mutate in (lambda p: p.update(line='3.8'), lambda p: p.update(product='core'),
                    lambda p: p['source'].update(commit='a'*40), lambda p: p['source'].update(tree='b'*40),
                    lambda p: p['source'].update(kind='observed-core-release-branch'),
                    lambda p: p['inventory']['projects'].pop(),
                    lambda p: p['inventory']['projects'][0].update(target_frameworks=['net8.0']),
                    lambda p: p['inventory']['release_recipe'].update(solution='Elsa.sln'),
                    lambda p: p.update(npm=None if product == 'studio' else {})):
                changed = deepcopy(original); mutate(changed)
                with self.subTest(product=product, mutate=mutate), self.assertRaises(ValueError):
                    selected.validate_plan(changed)
        plan = self.plan('extensions'); plan['inventory']['excluded'].pop()
        with self.assertRaisesRegex(ValueError, 'maintenance39_canonical_exclusion'): selected.validate_plan(plan)
        plan = self.plan('extensions'); plan['inventory']['selected'].append({'id': plan['inventory']['excluded'][0]['id']})
        with self.assertRaisesRegex(ValueError, 'maintenance39_canonical_exclusion'): selected.validate_plan(plan)

    def test_changed_source_blob_or_runtime_fixture_rejected_without_tool_bootstrap(self):
        with patch.object(selected.metadata, 'git', return_value='a'*40):
            with self.assertRaisesRegex(ValueError, 'maintenance39_source'):
                selected.verify_source(artifacts.ROOT, self.plan('studio'))
        real_git = selected.metadata.git
        def git(root, *args):
            return real_git(root, *args) if args[-1].endswith('^{tree}') else 'a'*40
        with patch.object(selected.metadata, 'git', side_effect=git):
            with self.assertRaisesRegex(ValueError, 'maintenance39_blob'):
                selected.verify_source(artifacts.ROOT, self.plan('extensions'))
        with patch.object(selected.metadata, 'sha256', return_value='changed'):
            with self.assertRaisesRegex(ValueError, 'maintenance39_runtime_fixture'):
                selected.verify_source(artifacts.ROOT, self.plan('studio'))

    def test_original_39_recipes_keep_designer_gates_full_solution_and_extensions_nuke(self):
        rows = artifacts.maintenance.registered_core_candidates(artifacts.maintenance.load_register())
        for product in ('studio', 'extensions'):
            row = next(row for row in rows if row['commit'] == selected.contract(product)['commit'])
            commands = artifacts.maintenance.recipes(row, '3.9.999', Path('/private/proof'))
            if product == 'extensions':
                self.assertEqual([('.', ['./build.sh', 'Compile+Test+Pack', '--configuration', 'Release',
                    '--version', '3.9.999', '--analyseCode', 'true'])], commands)
            else:
                self.assertEqual(['dotnet', 'restore', 'src/modules/Elsa.Studio.Workflows.Designer/Elsa.Studio.Workflows.Designer.csproj'], commands[0][1])
                names = [command for _, command in commands]
                self.assertLess(names.index(['npm', 'run', 'check:generated']), names.index(['npm', 'test']))
                self.assertLess(names.index(['npm', 'test']), names.index(['npm', 'run', 'build']))
                self.assertEqual(['build', 'test', 'pack'], [command[1] for command in names if command[:1] == ['dotnet']][1:])

    def test_producer_dispatch_39_original_recipe_and_conditional_studio_npm(self):
        for product in ('studio', 'extensions'):
            with self.subTest(product=product), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary).resolve(); plan = self.plan(product); output = root / 'control'
                def checkout(_root, binding, source):
                    source.mkdir(); (source / selected.contract(product)['solution']).write_bytes(b'solution')
                    path = source / selected.contract(product)['workflow']; path.parent.mkdir(parents=True); path.write_bytes(b'workflow')
                row = {'commit': plan['source']['commit'], 'kind': 'maintenance'}
                package = {'id': plan['inventory']['selected'][0]['id'], 'package_manifest': {}, 'sdk_assets': []}
                with patch.object(artifacts, 'admit', return_value=plan), \
                        patch.object(artifacts, 'selected_execution', return_value={'id': 'offline-fixture'}), \
                        patch.object(artifacts, 'verify_controller', return_value=plan['controller']), \
                        patch.object(selected, 'verify_source') as bound, \
                        patch.object(extensions, 'verify_source'), \
                        patch.object(metadata, 'checkout_source', side_effect=checkout), \
                        patch.object(artifacts.planner, 'npm_intent', return_value=plan['npm']), \
                        patch.object(artifacts, 'preflight', return_value={'host_framework': 'net10.0'}), \
                        patch.object(artifacts.planner, 'build_helper'), patch.object(artifacts, 'refresh_remote'), \
                        patch.object(artifacts.maintenance, 'registered_core_candidates', return_value=[row]), \
                        patch.object(artifacts.maintenance, 'prepare', return_value={'success': True, 'tests': {}, 'packages': [package]}) as prepare, \
                        patch.object(artifacts, 'retain_selected', return_value={}), \
                        patch.object(artifacts.historical, 'prove', return_value={'success': True}) as npm:
                    result = artifacts.execute(artifacts.ROOT, b'{}', 'a'*64, output)
                bound.assert_called_once_with(artifacts.ROOT, plan)
                self.assertTrue(result['success']); self.assertEqual('3.9.999', prepare.call_args.args[2])
                self.assertEqual(product == 'studio', npm.called)

    def test_runtime_39_reuses_exact_fixtures_and_original_version_policies(self):
        for product in ('studio', 'extensions'):
            plan = self.plan(product); contract = consumers.runtime_contract(plan)
            self.assertIn('39', contract['description'])
            self.assertEqual('3.9.999' if product == 'studio' else '1.0.0', contract['assembly_release_version'])
            self.assertEqual(selected.contract(product)['fixture']['sha256'], metadata.sha256(contract['fixture'].read_bytes()))
            project = consumers.render_project(plan['inventory']['selected'][0]['id'], '3.9.999', 'net9.0', [], executable=True, managed=True)
            self.assertEqual(1, project.count('<PackageReference ')); self.assertNotIn('ProjectReference', project)

    def run_consumer_dispatch(self, product, freshness_error=None):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve(); plan = self.plan(product)
            plan.update(semantics={'sdk_version': metadata.SDK, 'assemblies': []},
                        consumer_feed_policy={'config_sha256': metadata.sha256(b'original config')})
            selected_packages = {row['id'].casefold(): {'id': row['id'], 'policy': row}
                                 for row in plan['inventory']['selected']}
            plan_path, receipt_path = root / 'plan.json', root / 'producer.json'
            plan_path.write_bytes(b'bound plan'); receipt_path.write_bytes(b'bound receipt')
            receipt = {'execution': {'id': 'offline-fixture'}, 'planner_controller': {}, 'artifact_controller': {}}
            calls = []
            def refresh(*args):
                calls.append('current-gate')
                if freshness_error: raise ValueError(freshness_error)
                return {'eligible': True, 'checked_at': consumers.planner.now(), 'histories': [], 'prerequisites': []}
            def catalog(*args):
                calls.append('catalog'); args[-1].mkdir(); (args[-1] / 'catalog.private.json').write_text('{}')
                return {}, {}
            def cell(*args, **kwargs):
                calls.append(('runtime' if kwargs.get('runtime') else 'compile', args[2]))
                return {'id': args[1]['id'], 'framework': args[2], 'success': True}
            with patch.object(consumers, 'admit_producer_stage', return_value=(plan, receipt, {'scope':'historical-producer-start-only'})), \
                    patch.object(consumers, 'selected_execution', return_value={'id':'offline-fixture'}), \
                    patch.object(consumers.producer, 'verify_controller', return_value={}), \
                    patch.object(consumers, 'admit_artifacts', return_value=selected_packages), \
                    patch.object(consumers, 'load_snapshots', return_value={}), \
                    patch.object(selected, 'verify_source') as source, \
                    patch.object(consumers.producer.maintenance, 'git_bytes', return_value=b'original config'), \
                    patch.object(consumers.planner, 'build_helper') as helper, \
                    patch.object(consumers.producer, 'refresh_remote', side_effect=refresh), \
                    patch.object(consumers.resolution, 'build_inspector') as inspector, \
                    patch.object(consumers.resolution, 'validate_native_tools'), \
                    patch.object(consumers.resolution, 'archive_catalog', side_effect=catalog), \
                    patch.object(consumers, 'cell', side_effect=cell):
                helper.return_value.call.return_value = []
                def execute():
                    return consumers.execute(artifacts.ROOT, plan_path, metadata.sha256(b'bound plan'), receipt_path,
                        metadata.sha256(b'bound receipt'), root / 'archives', root / 'snapshots', root / 'output')
                if freshness_error:
                    with self.assertRaisesRegex(ValueError, freshness_error): execute()
                    inspector.assert_not_called()
                else: execute()
            source.assert_called_once_with(artifacts.ROOT, plan)
            result = json.loads((root / 'output/retained/receipt.json').read_text())
            return calls, result

    def test_consumer39_dispatch_all_applicable_tfms_and_runtime_after_current_gate(self):
        for product in ('studio', 'extensions'):
            calls, receipt = self.run_consumer_dispatch(product)
            self.assertEqual(['current-gate', 'catalog'] + [(kind, tfm) for kind in ('compile','runtime')
                for tfm in ('net8.0','net9.0','net10.0')], calls)
            self.assertTrue(receipt['success']); self.assertTrue(receipt['current_consumer_admission']['eligible'])
            self.assertEqual(selected.contract(product)['files'], receipt['runtime_contract_source'])

    def test_consumer39_fresh_ineligibility_or_metadata_drift_prevents_all_consumer_work(self):
        for product in ('studio', 'extensions'):
            for error in ('artifact_fresh_prerequisite_failed', 'artifact_prerequisite_metadata_changed'):
                calls, receipt = self.run_consumer_dispatch(product, error)
                self.assertEqual(['current-gate'], calls)
                self.assertFalse(receipt['success']); self.assertFalse(receipt['current_consumer_admission']['eligible'])
                self.assertEqual('current-consumer-prerequisites', receipt['stage'])

    def test_both39_loaded_assembly_version_and_source_policies_join_exact_archive_bytes(self):
        for product in ('studio', 'extensions'):
            with tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary).resolve(); cache = root / 'cache'; packages = root / 'archives'; packages.mkdir()
                plan = self.plan(product); identifier = plan['inventory']['selected'][0]['id']
                entry = 'lib/net9.0/' + identifier + '.dll'; data = b'exact DLL bytes'; digest = metadata.sha256(data)
                cached = cache / identifier.lower() / '3.9.999' / entry; cached.parent.mkdir(parents=True); cached.write_bytes(data)
                loaded = root / 'bin/Release/net9.0' / (identifier+'.dll'); loaded.parent.mkdir(parents=True); loaded.write_bytes(data)
                with zipfile.ZipFile(packages / 'package.nupkg','w') as archive: archive.writestr(entry,data)
                policy = consumers.runtime_contract(plan)['assembly_release_version']
                row = {'name': identifier, 'version': policy+'.0', 'informationalVersion': policy+'+'+plan['source']['commit'],
                    'fullName': identifier+', Version='+policy+'.0, Culture=neutral', 'location':str(loaded), 'sha256':digest}
                assets = {'targets':{'net9.0':{identifier+'/3.9.999':{'runtime':{entry:{}}}}}}
                def verify(value):
                    return consumers.consumers.verify_loaded_assemblies({'loadedAssemblies':[value]}, assets,'net9.0',root,
                        cache,packages,{identifier.casefold():{'nupkg':'package.nupkg'}},'3.9.999',plan['source']['commit'],
                        required_packages=(identifier,),assembly_release_version=policy)
                self.assertEqual('3.9.999',verify(row)[0]['package_version'])
                for change in ({'version':'7.0.0.0'}, {'informationalVersion':policy+'+'+'f'*40}, {'sha256':'a'*64}):
                    with self.assertRaises(RuntimeError): verify(row | change)

    def test_generated_extensions39_manifest_keeps_qualified_dependency_and_requested_version(self):
        plan = self.plan('extensions'); source = plan['source']['commit']
        policy = {'id': 'Elsa.IO.Http', 'frameworks': ['net8.0', 'net9.0', 'net10.0'],
            'framework_properties': {tfm: {'manifest_required': True, 'manifest_path': 'elsa-package.json'}
                for tfm in ('net8.0','net9.0','net10.0')}}
        extensions.bind_manifest_contract(artifacts.ROOT, {'commit': source}, policy)
        self.assertEqual(extensions.MANIFEST_FEATURE, policy['manifest_expectation'])
        self.assertEqual(['Elsa.IO.Http.I/O'], policy['manifest_dependency_features'])
        manifest = {'schemaVersion': '1.0', 'package': {'id': 'Elsa.IO.Http', 'version': '3.9.999'},
            'compatibility': {'runtimeKinds': ['elsa.server']},
            'extensions': {'targetFrameworks': policy['frameworks'], 'repositoryUrl': archives.CORE_URL},
            'features': [dict(extensions.MANIFEST_FEATURE, id='Elsa.IO.Http.HttpIO', dependencies=[{'featureId':'Elsa.IO.Http.I/O'}])]}
        with tempfile.TemporaryDirectory() as temporary:
            archive = Path(temporary) / 'manifest.nupkg'
            for version in ('3.9.999', '1.0.0'):
                manifest['package']['version'] = version
                with zipfile.ZipFile(archive, 'w') as package: package.writestr('elsa-package.json', json.dumps(manifest))
                with zipfile.ZipFile(archive) as package:
                    if version == '3.9.999': artifacts.maintenance.verify_package_manifest(package, policy, '3.9.999', require_sdk_metadata=True)
                    else:
                        with self.assertRaises(ValueError): artifacts.maintenance.verify_package_manifest(package, policy, '3.9.999', require_sdk_metadata=True)


if __name__ == '__main__':
    unittest.main()
