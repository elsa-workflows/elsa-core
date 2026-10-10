from copy import deepcopy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile

import product_release_metadata as metadata
import prove_consolidated_packages as packages
import prove_consolidated_package_consumers as consumers
import prove_product_release_artifacts as artifacts
import prove_product_release_consumers as proof
import selected_extensions_contract as contract
from selected_maintenance_test_support import patch_offline_local_execution


class ExtensionsControlTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        local_execution_patch = patch_offline_local_execution(artifacts)
        local_execution_patch.start()
        cls.addClassCleanup(local_execution_patch.stop)

    def test_contract_is_bound_to_exact_original_source_and_all_blobs(self):
        contract.verify_source(artifacts.ROOT, contract.SOURCE)
        with self.assertRaisesRegex(ValueError, 'extensions_contract_source'):
            contract.verify_source(artifacts.ROOT, 'f' * 40)
        with patch.object(contract.metadata, 'git', return_value='f' * 40):
            with self.assertRaisesRegex(ValueError, 'extensions_contract_blob'):
                contract.verify_source(artifacts.ROOT, contract.SOURCE)

    def policy(self):
        policy = {'id': 'Elsa.IO.Http', 'frameworks': ['net8.0', 'net9.0', 'net10.0'],
                  'framework_properties': {tfm: {'manifest_required': True, 'manifest_path': 'elsa-package.json'}
                                           for tfm in ('net8.0', 'net9.0', 'net10.0')}}
        contract.bind_manifest_contract(artifacts.ROOT, {'commit': contract.SOURCE}, policy)
        return policy

    def manifest(self):
        return {'schemaVersion': '1.0', 'package': {'id': 'Elsa.IO.Http', 'version': '3.8.999'},
                'compatibility': {'runtimeKinds': ['elsa.server']},
                'extensions': {'targetFrameworks': ['net8.0', 'net9.0', 'net10.0'], 'repositoryUrl': packages.CORE_URL},
                'features': [dict(contract.MANIFEST_FEATURE, id='Elsa.IO.Http.HttpIO',
                    dependencies=[{'featureId': 'Elsa.IO.Http.I/O'}])]}

    def check_manifest(self, folder, data, policy=None):
        path = folder / 'manifest.nupkg'
        with zipfile.ZipFile(path, 'w') as archive:
            if data is not None:
                archive.writestr('elsa-package.json', json.dumps(data))
        with zipfile.ZipFile(path) as archive:
            return packages.verify_package_manifest(archive, policy or self.policy(), '3.8.999', require_sdk_metadata=True)

    def test_generated_http_manifest_checks_original_schema_feature_and_dependency(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            result = self.check_manifest(folder, self.manifest())
            self.assertEqual('HTTP I/O', result['selectable_features'][0]['displayName'])
            mutations = [lambda d: d.update(schemaVersion='2.0'),
                lambda d: d['compatibility'].update(runtimeKinds=['elsa.studio']),
                lambda d: d['features'].clear(),
                lambda d: d['features'][0].update(typeName='Elsa.IO.Http.Features.IOHttpFeature'),
                lambda d: d['features'][0].update(displayName='I/O'),
                lambda d: d['features'][0].update(dependencies=[{'featureId': 'I/O'}]),
                lambda d: d['features'][0]['dependencies'][0].update(packageId='Unknown'),
                lambda d: d['features'][0]['dependencies'][0].update(unknown='Hidden dependency'),
                lambda d: d['extensions'].update(targetFrameworks=['net10.0']),
                lambda d: d['package'].update(version='1.0.0')]
            for mutate in mutations:
                data = self.manifest(); mutate(data)
                with self.subTest(data=data), self.assertRaises(ValueError):
                    self.check_manifest(folder, data)
            policy = self.policy()
            for properties in policy['framework_properties'].values():
                properties['manifest_required'] = False
            with self.assertRaisesRegex(ValueError, 'manifest is required'):
                self.check_manifest(folder, None, policy)

    def test_sdk_manifest_and_build_assets_require_exact_emitted_bytes(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory).resolve()
            emitted = folder / 'obj/elsa-package.json'; emitted.parent.mkdir(); emitted.write_text(json.dumps(self.manifest()))
            policy = self.policy()
            data = ('<package><files><file src="' + str(emitted) + '" target="elsa-package.json"/></files></package>').encode()
            policy['expected_sdk_assets'] = packages.capture_sdk_assets(folder, policy, data)
            for content in (emitted.read_bytes(), b'changed'):
                path = folder / 'package.nupkg'
                with zipfile.ZipFile(path, 'w') as archive:
                    archive.writestr('elsa-package.json', content)
                with zipfile.ZipFile(path) as archive:
                    if content == emitted.read_bytes():
                        packages.verify_sdk_assets(archive, policy, required=True)
                    else:
                        with self.assertRaisesRegex(ValueError, 'differs from generated output'):
                            packages.verify_sdk_assets(archive, policy, required=True)

    def test_extensions_preflight_never_requires_node_npm_or_a_studio_host(self):
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory)
            commands = []
            def run(command, cwd, **kwargs):
                commands.append(command)
                return '10.0.300 [/sdk]' if command[-1] == '--list-sdks' else '10.0.300'
            with patch.object(artifacts, 'run', side_effect=run):
                result = artifacts.preflight(source, {'product': 'extensions', 'npm': None,
                    'source': {'product': 'extensions', 'line': '3.8'}, 'requested_version': '3.8.999'}, source)
            self.assertEqual([['dotnet', '--list-sdks'], ['dotnet', '--version']], commands)
            self.assertEqual({'sdk': '10.0.300', 'product_work_executed': False}, result)
            self.assertEqual('["dotnet", "--list-sdks"]\n10.0.300 [/sdk]',
                (source / 'preflight-sdks.log').read_text())
            self.assertEqual('["dotnet", "--version"]\n10.0.300',
                (source / 'preflight-sdk-selection.log').read_text())
            with patch.object(artifacts.maintenance, 'run', return_value='10.0.300 [/sdk]') as inspect:
                self.assertEqual({'dotnet': ['10.0.300']},
                    artifacts.maintenance.inspect_toolchain(source, {'product': 'extensions'}))
                self.assertEqual(['dotnet', '--list-sdks'], inspect.call_args.args[0])

    def test_extensions_original_recipe_explicitly_selects_release_for_both_lines(self):
        candidates = artifacts.maintenance.registered_core_candidates(artifacts.maintenance.load_register())
        for line in ('3.8', '3.9'):
            row = next(row for row in candidates if row['product'] == 'extensions' and row['line'] == line and
                row['commit'] == metadata.DESCENDANTS[('extensions', line)])
            with self.subTest(line=line):
                self.assertEqual(artifacts.maintenance.recipes(row, line + '.999', Path('/unused')), [('.',
                    ['./build.sh', 'Compile+Test+Pack', '--configuration', 'Release', '--version', line + '.999',
                     '--analyseCode', 'true'])])

    def test_extensions_preflight_rejects_missing_debug_or_duplicate_configuration_before_processes(self):
        command = ['./build.sh', 'Compile+Test+Pack', '--configuration', 'Release', '--version', '3.8.999',
                   '--analyseCode', 'true']
        malformed = [command[:2] + command[4:], command[:3] + ['Debug'] + command[4:],
                     command + ['--configuration', 'Debug'], command[:2] + ['--configuration'],
                     command + ['--configuration=Debug'], command + ['--CONFIGURATION', 'Debug']]
        plan = {'product': 'extensions', 'npm': None, 'source': {'product': 'extensions', 'line': '3.8'},
                'requested_version': '3.8.999'}
        with tempfile.TemporaryDirectory() as directory:
            for invalid in malformed:
                with self.subTest(command=invalid), patch.object(artifacts.maintenance, 'recipes', return_value=[('.', invalid)]), \
                        patch.object(artifacts, 'run', side_effect=AssertionError('process before configuration admission')) as run:
                    with self.assertRaisesRegex(ValueError, 'artifact_extensions_recipe_configuration'):
                        artifacts.preflight(Path(directory), plan, Path(directory))
                    run.assert_not_called()

    def test_extensions_adapter_runs_original_recipe_and_skips_studio_npm(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve(); controller = root / 'controller'; controller.mkdir()
            output = root / 'control'
            plan = {'product': 'extensions', 'line': '3.8', 'requested_version': '3.8.999',
                    'controller': {'commit': 'c' * 40, 'tree': 'd' * 40}, 'source': {'commit': contract.SOURCE},
                    'npm': None, 'inventory': {'selected': [{'id': 'Elsa.IO.Http'}], 'release_recipe': {
                        'solution': 'Elsa.Extensions.sln', 'sha256': metadata.sha256(b'solution'),
                        'workflow': 'inert.yml', 'workflow_sha256': metadata.sha256(b'workflow')}}}
            def checkout(root, binding, source):
                source.mkdir(); (source / 'Elsa.Extensions.sln').write_bytes(b'solution')
                (source / 'inert.yml').write_bytes(b'workflow')
            row = {'commit': contract.SOURCE, 'kind': 'maintenance'}
            package = {'id': 'Elsa.IO.Http', 'package_manifest': {'sha256': 'a' * 64}, 'sdk_assets': []}
            with patch.object(artifacts, 'admit', return_value=plan), \
                    patch.object(artifacts, 'verify_controller', return_value=plan['controller']), \
                    patch.object(metadata, 'checkout_source', side_effect=checkout), \
                    patch.object(contract, 'verify_source') as bound, \
                    patch.object(artifacts.planner, 'npm_intent', return_value=None), \
                    patch.object(artifacts, 'preflight', return_value={'sdk': metadata.SDK}), \
                    patch.object(artifacts.planner, 'build_helper'), patch.object(artifacts, 'refresh_remote'), \
                    patch.object(artifacts.maintenance, 'registered_core_candidates', return_value=[row]), \
                    patch.object(artifacts.maintenance, 'prepare', return_value={
                        'success': True, 'tests': {'passed': 1}, 'packages': [package, dict(package, id='Excluded')]} ) as recipe, \
                    patch.object(artifacts, 'retain_selected', return_value={'selected': []}), \
                    patch.object(artifacts.historical, 'prove') as npm:
                result = artifacts.execute(controller, b'{}', 'a' * 64, output)
            self.assertTrue(result['success'])
            self.assertEqual([package], result['manifest_verification'])
            self.assertEqual('3.8.999', recipe.call_args.args[2])
            bound.assert_called_once_with(controller, contract.SOURCE)
            npm.assert_not_called()
            self.assertNotIn('npm', result)

    def test_runtime_uses_only_selected_http_root_and_distinct_assembly_policy(self):
        plan = {'product': 'extensions', 'requested_version': '3.8.999'}
        chosen = proof.runtime_contract(plan)
        self.assertEqual('elsa.io.http', chosen['package'])
        self.assertEqual('1.0.0', chosen['assembly_release_version'])
        project = proof.render_project('Elsa.IO.Http', '3.8.999', 'net9.0', [], executable=True, managed=True)
        self.assertEqual(1, project.count('<PackageReference '))
        self.assertNotIn('ProjectReference', project)
        fixture = chosen['fixture'].read_text()
        self.assertIn('new FakeHandler()', fixture)
        self.assertIn('new HttpIOShellFeature().ConfigureServices', fixture)
        self.assertNotIn('new IOShellFeature', fixture)
        self.assertNotIn('new CompressionIOShellFeature', fixture)

    def test_extensions_loaded_archive_bytes_keep_package_and_assembly_versions_separate(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve(); cache = root / 'packages'; archives = root / 'archives'; archives.mkdir()
            entry = 'lib/net9.0/Elsa.IO.Http.dll'; data = b'exact DLL bytes'
            stored = cache / 'elsa.io.http/3.8.999' / entry; stored.parent.mkdir(parents=True); stored.write_bytes(data)
            loaded = root / 'bin/Release/net9.0/Elsa.IO.Http.dll'; loaded.parent.mkdir(parents=True); loaded.write_bytes(data)
            with zipfile.ZipFile(archives / 'Elsa.IO.Http.3.8.999.nupkg', 'w') as archive:
                archive.writestr(entry, data)
            asset = {'targets': {'net9.0': {'Elsa.IO.Http/3.8.999': {'runtime': {entry: {}}}}}}
            selected = {'elsa.io.http': {'nupkg': 'Elsa.IO.Http.3.8.999.nupkg'}}
            row = {'name': 'Elsa.IO.Http', 'version': '1.0.0.0', 'informationalVersion': '1.0.0+' + contract.SOURCE,
                   'fullName': 'Elsa.IO.Http, Version=1.0.0.0, Culture=neutral', 'location': str(loaded),
                   'sha256': metadata.sha256(data)}
            def verify(value, **kwargs):
                return consumers.verify_loaded_assemblies({'loadedAssemblies': [value]}, asset, 'net9.0', root,
                    cache, archives, selected, '3.8.999', contract.SOURCE, required_packages=('Elsa.IO.Http',), **kwargs)
            self.assertEqual('3.8.999', verify(row, assembly_release_version='1.0.0')[0]['package_version'])
            for changed in (row, dict(row, informationalVersion='3.8.999+' + contract.SOURCE),
                            dict(row, informationalVersion='1.0.0+' + 'f' * 40)):
                with self.assertRaisesRegex(RuntimeError, 'release/source identity mismatch'):
                    verify(changed, **({} if changed is row else {'assembly_release_version': '1.0.0'}))
            loaded.write_bytes(b'changed')
            with self.assertRaisesRegex(RuntimeError, 'file differs'):
                verify(row, assembly_release_version='1.0.0')


if __name__ == '__main__':
    unittest.main()
