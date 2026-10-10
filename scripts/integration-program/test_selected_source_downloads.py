from copy import deepcopy
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

import core_source_continuation as continuation
import product_release_metadata as metadata
import prove_product_release_consumers as proof
import selected_consumer_sdk as sdk


class Semantics:
    def call(self, operation, **values):
        if operation == 'versions':
            return [{'normalized': value} for value in values['values']]
        if operation == 'feeds':
            return {'sources': [{'name': 'original', 'url': proof.planner.NUGET_INDEX}], 'packages': []}
        raise AssertionError(operation)


class SourceDownloadTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.project = 'src/Designer/Designer.csproj'
        self.project_bytes = b'<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageDownload Include="Bpmn.Model" Version="[$(BpmnModelVersion)]" /></ItemGroup></Project>'
        self.frameworks = ['net8.0', 'net9.0', 'net10.0']
        self.plan = {'source': {'commit': 'a' * 40, 'tree': 'b' * 40}, 'requested_version': '3.9.999',
            'inventory': {'selected': [{'id': 'Designer', 'project': self.project, 'frameworks': self.frameworks,
                'metadata': {'original_content_project_sha256': metadata.sha256(self.project_bytes),
                             'restore_assets_sha256': 'c' * 64}}]}}
        self.assets = {'project': {'frameworks': {framework: {'downloadDependencies': [
            {'name': 'Bpmn.Model', 'version': '[0.2.0, 0.2.0]'}] + ([
            {'name': 'Microsoft.NETCore.App.Ref', 'version': f'[{version}, {version}]'}] if
            (version := sdk.DOWNLOAD_VERSIONS.get(framework)) else [])} for framework in self.frameworks}}}
        self.evaluated = {framework: {'Properties': {'PackageId': 'Designer', 'TargetFramework': framework,
            'NETCoreSdkVersion': metadata.SDK}, 'Items': {'PackageDownload': [
                {'Identity': 'Bpmn.Model', 'Version': '[0.2.0]'}]}} for framework in self.frameworks}

    def bind(self, *, plan=None, assets=None, evaluated=None, check_evaluation=None):
        plan, assets = plan or self.plan, assets or self.assets
        evaluated = evaluated or self.evaluated
        temporary = tempfile.TemporaryDirectory(dir=self.root)
        self.addCleanup(temporary.cleanup)
        output = Path(temporary.name) / 'evaluation'
        def checkout(controller, binding, destination):
            path = destination / self.project
            path.parent.mkdir(parents=True)
            path.write_bytes(self.project_bytes)
            (destination / 'NuGet.Config').write_text('<configuration />')
        def evaluate(command, cwd, **kwargs):
            self.assertEqual('dotnet', command[0])
            self.assertIn('-getItem:PackageDownload', command)
            self.assertFalse(any(arg == '-restore' or arg.startswith('-target:') for arg in command))
            self.assertEqual(metadata.SDK, json.loads((cwd.parent / 'global.json').read_text())['sdk']['version'])
            self.assertEqual('disable', json.loads((cwd.parent / 'global.json').read_text())['sdk']['rollForward'])
            self.assertTrue(Path(kwargs['env']['HOME']).is_relative_to(output))
            self.assertNotIn('GITHUB_TOKEN', kwargs['env'])
            if check_evaluation:
                check_evaluation(command, cwd, kwargs['env'])
            framework = next(arg.split('=', 1)[1] for arg in command if arg.startswith('-p:TargetFramework='))
            return json.dumps(evaluated[framework])
        with patch.object(metadata, 'checkout_source', side_effect=checkout) as source, \
                patch.object(metadata, 'git', return_value=''), \
                patch.object(metadata, 'run', side_effect=evaluate) as native:
            result = sdk.bind_source_downloads(self.root, plan, {self.project: assets}, output, Semantics())
        return result, source.call_count, native.call_count

    def test_admitted_source_version_policy_preserves_cold_isolation(self):
        cells = json.loads(Path(__file__).with_name('selected_product_plan_shapes.json').read_bytes())['cells']
        catalog = continuation.load_contract()
        ambient = {key: 'untrusted-ambient' for key in ('VERSION', 'RestoreConfigFile', 'HOME', 'DOTNET_CLI_HOME',
            'NUGET_PACKAGES', 'NUGET_HTTP_CACHE_PATH', 'NUGET_PLUGINS_CACHE_PATH', 'GITHUB_TOKEN', 'GH_TOKEN',
            'NUGET_AUTH_TOKEN', 'NuGetPackageSourceCredentials_original', 'CI', 'GITHUB_ACTIONS',
            'ELSA_USERTASKS_TEST_SQLSERVER', 'ELSA_USERTASKS_TEST_POSTGRES', 'ELSA_USERTASKS_TEST_ORACLE')}
        extra_authority = {key: 'untrusted-recipe' for key in ambient.keys() - {'VERSION', 'RestoreConfigFile'}}
        metadata_environment = metadata.metadata_environment
        def recipe_environment(*args):
            return metadata_environment(*args) | extra_authority
        for line in ('3.8', '3.9'):
            for product in ('core', 'studio', 'extensions'):
                original = cells[product + '-' + line]['source']
                bindings = [original]
                if product == 'core':
                    bindings.append(continuation.bind(line, original['observation'], catalog))
                for binding in bindings:
                    plan = deepcopy(self.plan)
                    plan.update(source=binding, requested_version=line + '.999')
                    def check(command, source, environment):
                        version = plan['requested_version']
                        for name in ('Version', 'PackageVersion'):
                            self.assertEqual(product != 'core', f'-p:{name}={version}' in command)
                        if product == 'core':
                            self.assertFalse(any(arg.startswith(('-p:Version=', '-p:PackageVersion=')) for arg in command))
                            self.assertEqual(version, environment['VERSION'])
                            self.assertEqual(str(source / 'NuGet.Config'), environment['RestoreConfigFile'])
                        else:
                            self.assertNotIn('VERSION', environment)
                            self.assertNotIn('RestoreConfigFile', environment)
                        for name, directory in (('HOME', 'home'), ('DOTNET_CLI_HOME', 'home'),
                                ('NUGET_PACKAGES', 'packages'), ('NUGET_HTTP_CACHE_PATH', 'http-cache'),
                                ('NUGET_PLUGINS_CACHE_PATH', 'plugins-cache')):
                            self.assertEqual(str(source.parent / directory), environment[name])
                        for name in extra_authority.keys() - {'HOME', 'DOTNET_CLI_HOME', 'NUGET_PACKAGES',
                                'NUGET_HTTP_CACHE_PATH', 'NUGET_PLUGINS_CACHE_PATH'}:
                            self.assertNotIn(name, environment)
                    with self.subTest(product=product, line=line, kind=binding['kind']), \
                            patch.dict(os.environ, ambient), \
                            patch.object(metadata, 'metadata_environment', side_effect=recipe_environment) as recipe:
                        result, checkouts, evaluations = self.bind(plan=plan, check_evaluation=check)
                        self.assertEqual((1, 3), (checkouts, evaluations))
                        self.assertEqual(binding, result['source'])
                        recipe.assert_called_once()
                        self.assertEqual((binding, plan['requested_version']), recipe.call_args.args[1:])

    def test_source_downloads_bind_all_selected_tfms_and_never_enter_sdk_projection(self):
        binding, checkouts, evaluations = self.bind()
        self.assertEqual((1, 3), (checkouts, evaluations))
        self.assertEqual(self.plan['source'], binding['source'])
        project = binding['projects'][self.project]
        self.assertEqual('c' * 64, project['original_assets_sha256'])
        self.assertEqual(metadata.sha256(self.project_bytes), project['project_sha256'])
        for framework in self.frameworks:
            declarations = project['frameworks'][framework]
            self.assertEqual([{'id': 'Bpmn.Model', 'version': '0.2.0'}], declarations)
            policy = sdk.original_policy(self.assets, framework, source_downloads=declarations)
            self.assertTrue(all(row['id'] in sdk.PACK_IDS for row in policy['downloads']))
            generated = ET.fromstring(proof.render_project('Designer', '3.9.999', framework, [],
                executable=False, managed=True, sdk_policy=policy))
            self.assertEqual([], generated.findall('.//PackageDownload'))
            current = deepcopy(self.assets)
            current['project']['frameworks'][framework]['downloadDependencies'].pop(0)
            sdk.validate_projection(current, framework, policy)
            with self.assertRaisesRegex(ValueError, 'consumer_sdk_original_download'):
                sdk.validate_projection(self.assets, framework, policy)

    def test_unbound_source_rows_and_source_original_disagreement_fail_closed(self):
        with self.assertRaisesRegex(ValueError, 'consumer_sdk_original_download'):
            sdk.original_policy(self.assets, 'net8.0')
        for change in ('missing', 'extra', 'wrong-version', 'duplicate', 'range', 'wrong-project', 'wrong-tfm', 'wrong-sdk'):
            evaluated = deepcopy(self.evaluated)
            cell = evaluated['net8.0']
            items = cell['Items']['PackageDownload']
            if change == 'missing': items.clear()
            elif change == 'extra': items.append({'Identity': 'Unreviewed', 'Version': '[1.0.0]'})
            elif change == 'wrong-version': items[0]['Version'] = '[0.2.1]'
            elif change == 'duplicate': items.append(deepcopy(items[0]))
            elif change == 'range': items[0]['Version'] = '[0.2.0, )'
            else: cell['Properties'][{'wrong-project': 'PackageId', 'wrong-tfm': 'TargetFramework',
                                      'wrong-sdk': 'NETCoreSdkVersion'}[change]] = 'wrong'
            with self.subTest(change=change), self.assertRaises(ValueError):
                self.bind(evaluated=evaluated)

    def test_missing_original_tfm_row_and_changed_project_hash_fail_closed(self):
        assets = deepcopy(self.assets)
        assets['project']['frameworks']['net9.0']['downloadDependencies'].clear()
        with self.assertRaises(ValueError): self.bind(assets=assets)
        plan = deepcopy(self.plan)
        plan['inventory']['selected'][0]['metadata']['original_content_project_sha256'] = 'd' * 64
        with self.assertRaisesRegex(ValueError, 'consumer_source_download_project_hash'): self.bind(plan=plan)

    def test_sdk_only_snapshots_do_not_evaluate_source(self):
        assets = deepcopy(self.assets)
        for frame in assets['project']['frameworks'].values(): frame['downloadDependencies'].pop(0)
        with patch.object(metadata, 'metadata_environment', side_effect=AssertionError('SDK-only source evaluation')):
            binding, checkouts, evaluations = self.bind(assets=assets)
        self.assertEqual({}, binding['projects'])
        self.assertEqual((0, 0), (checkouts, evaluations))

    def test_source_only_original_rows_stay_private_and_outside_frozen_sdk_catalog(self):
        binding, _, _ = self.bind()
        assets = deepcopy(self.assets)
        assets['project']['frameworks'] = {'net10.0': assets['project']['frameworks']['net10.0']}
        assets.update(targets={'net10.0': {}}, libraries={})
        with patch.object(proof.archives, 'restored_archive', side_effect=AssertionError('source prerequisite is not an SDK archive')):
            catalog, policy = proof.resolution.archive_catalog({self.project: assets}, {}, self.root / 'NuGet.Config',
                Semantics(), self.root / 'inspector', self.root / 'catalog', source_downloads=binding)
        self.assertEqual({}, catalog)
        self.assertEqual({}, policy['sdk_downloads'])
        self.assertEqual({'pruning': {}, 'downloads': []}, policy['sdk_projects'][self.project]['net10.0'])
        private = json.loads((self.root / 'catalog/catalog.private.json').read_text())
        self.assertEqual(binding, private['source_downloads'])
        self.assertEqual([], private['toolchain_downloads'])

    def test_source_binding_cannot_override_sdk_pack_case_or_version(self):
        for identifier in ('Microsoft.NETCore.App.Ref', 'microsoft.netcore.app.ref'):
            with self.subTest(identifier=identifier), self.assertRaisesRegex(ValueError, 'consumer_source_download_declarations'):
                sdk.original_policy(self.assets, 'net8.0', source_downloads=[{'id': identifier, 'version': '99.0.0'}])
        assets = deepcopy(self.assets)
        assets['project']['frameworks']['net8.0']['downloadDependencies'][1]['version'] = '[8.0.28, 8.0.28]'
        with self.assertRaisesRegex(ValueError, 'consumer_sdk_download_version'):
            sdk.original_policy(assets, 'net8.0', source_downloads=[{'id': 'Bpmn.Model', 'version': '0.2.0'}])


if __name__ == '__main__':
    unittest.main()
