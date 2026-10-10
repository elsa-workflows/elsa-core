from __future__ import annotations

from copy import deepcopy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

import selected_core_producer as core
import prepare_maintenance_build as maintenance
import prove_product_release_artifacts as artifacts


class OriginalCoreProducerContracts(unittest.TestCase):
    def setUp(self):
        self.contract = json.loads(core.CONTRACT.read_text())
        self.rows = [{key: value for key, value in source.items() if key in ('commit', 'tree')} |
                     {'product': 'core', 'line': line, 'kind': 'observed-core-release-branch'}
                     for line, source in self.contract['sources'].items()]

    def plan(self, row):
        policy = core.policy(row)
        return {'product': 'core', 'line': row['line'], 'source': row, 'npm': None,
                'inventory': {'release_recipe': {'solution': 'Elsa.sln', 'workflow': '.github/workflows/packages.yml'},
                              'projects': [{'path': path, 'is_test_project': True} for path in policy['test_projects']],
                              'selected': [{'id': 'Example', 'project': 'src/Example/Example.csproj', 'symbols': True}]}}

    def test_exact_original_pins_and_full_test_census_without_build(self):
        for row, count in zip(self.rows, (44, 61)):
            with self.subTest(line=row['line']):
                self.assertEqual(count, len(core.policy(row)['test_projects']))
                core.verify_source(artifacts.ROOT, row)
                core.validate_plan(self.plan(row))

    def test_source_kind_line_hash_and_no_npm_plan_are_closed(self):
        for change in ({'kind': 'maintenance'}, {'product': 'studio'}, {'line': '3.10'}, {'commit': 'a' * 40}, {'tree': 'b' * 40}):
            with self.subTest(change=change), self.assertRaises(ValueError):
                core.policy(self.rows[0] | change)
        plan = self.plan(self.rows[0]); plan['npm'] = {}
        with self.assertRaisesRegex(ValueError, 'core_original_plan'):
            core.validate_plan(plan)
        plan = self.plan(self.rows[0]); plan['inventory']['projects'].pop()
        with self.assertRaisesRegex(ValueError, 'core_original_test_inventory'):
            core.validate_plan(plan)

    def test_original_recipe_has_exported_version_release_and_all_net10_test_lanes(self):
        for row, count in zip(self.rows, (44, 61)):
            commands = core.recipes(row, '3.8.99' if row['line'] == '3.8' else '3.9.99', Path('/private/proof'))
            self.assertEqual(1 + count * 2, len(commands))
            self.assertEqual('Compile+Pack', commands[0][1][1])
            self.assertEqual(['--configuration', 'Release'], commands[0][1][-2:])
            builds = [cmd for _, cmd in commands if cmd[:2] == ['dotnet', 'build']]
            tests = [cmd for _, cmd in commands if cmd[:2] == ['dotnet', 'test']]
            self.assertEqual(count, len(builds)); self.assertEqual(count, len(tests))
            self.assertEqual(set(core.policy(row)['test_projects']), {cmd[2] for cmd in tests})
            for cmd in tests:
                self.assertIn('net10.0', cmd); self.assertIn('/p:CollectCoverage=true', cmd)
                self.assertNotIn('--filter', cmd)
                if cmd[2].startswith('test/component/'):
                    self.assertIn('--blame-hang', cmd); self.assertIn('2m', cmd)
            self.assertTrue(any('/integration/' in cmd[2] for cmd in tests))
            self.assertFalse(any('/performance/' in cmd[2] for cmd in tests))

    def test_environment_preserves_configured_provider_without_authority_or_server_claim(self):
        gate = core.PROVIDER_GATES[0]
        with patch.dict('os.environ', {'PATH': '/bin', 'CI': 'true', 'GITHUB_ACTIONS': 'true',
                                      'GITHUB_TOKEN': 'credential', gate: 'private-connection'}, clear=True):
            env = core.environment(self.rows[1], '3.9.99')
        self.assertEqual('3.9.99', env['VERSION']); self.assertEqual('private-connection', env[gate])
        self.assertFalse({'CI', 'GITHUB_ACTIONS', 'GITHUB_TOKEN'} & env.keys())
        self.assertNotIn('-p:Version=3.9.99', maintenance.metadata_command('Example.csproj', '3.9.99', self.rows[1]))
        self.assertIn('-p:Version=3.8.99', maintenance.metadata_command('Example.csproj', '3.8.99', {'product': 'studio'}))

    def test_no_symbol_exception_is_exact_project_and_plan_policy(self):
        plan = self.plan(self.rows[0]); plan['inventory']['selected'][0]['symbols'] = False
        with self.assertRaisesRegex(ValueError, 'core_original_symbols_policy'):
            core.validate_plan(plan)
        plan['inventory']['selected'] = [{'id': 'Elsa.SamplePackage', 'project': core.SAMPLE_PROJECT, 'symbols': False}]
        core.validate_plan(plan)
        with tempfile.TemporaryDirectory() as temp:
            source = Path(temp); item = plan['inventory']['selected'][0] | {'assembly_name': 'Sample'}
            folder = source / Path(core.SAMPLE_PROJECT).parent / 'bin/Release/net10.0'; folder.mkdir(parents=True)
            (folder / 'Sample.dll').write_bytes(b'actual'); (folder / 'Sample.pdb').write_bytes(b'pdb')
            self.assertEqual(folder / 'Sample.pdb', core.private_symbols(source, item, 'net10.0', b'actual'))
            with self.assertRaisesRegex(ValueError, 'core_original_private_symbols'):
                core.private_symbols(source, item, 'net10.0', b'changed')
            (folder / 'Sample.pdb').unlink()
            with self.assertRaises(ValueError): core.private_symbols(source, item, 'net10.0', b'actual')

    def test_core_assembly_policy_is_captured_not_a_global_version_default(self):
        row = self.rows[0]
        item = {'framework_properties': {'net10.0': {'assembly_policy':
                {'AssemblyVersion': '1.0.1.0', 'InformationalVersion': '1.0.1+' + row['commit']}}}}
        details = {'assembly_version': '1.0.1.0', 'informational_version': '1.0.1+' + row['commit']}
        core.verify_assembly(details, item, 'net10.0', row)
        for change in ({'assembly_version': '3.8.99.0'}, {'informational_version': '1.0.1+' + 'a' * 40}):
            with self.subTest(change=change), self.assertRaisesRegex(ValueError, 'core_original_assembly_policy'):
                core.verify_assembly(details | change, item, 'net10.0', row)

    def test_exact_declared_and_provider_skip_sets_preserve_configured_gate(self):
        self.assertEqual(3, len(core.expected_skips(self.rows[0], {})))
        self.assertEqual(146, len(core.expected_skips(self.rows[1], {})))
        self.assertEqual(98, len(core.expected_skips(self.rows[1], {core.PROVIDER_GATES[0]: 'configured'})))
        self.assertEqual(2, len(core.expected_skips(self.rows[1], dict.fromkeys(core.PROVIDER_GATES, 'configured'))))
        # The finite source contract carries 47 facts and one single skipped theory per provider.
        methods = core.policy(self.rows[1])['conditional']['methods']
        self.assertEqual(1, sum(item['attribute'] == 'ConformanceTheory' for item in methods))

    def outcomes(self, row, project, env=None):
        env = env or {}
        allowed = core.expected_skips(row, env)
        prefix = Path(project).stem + '.'
        expected = {key: value for key, value in allowed.items() if key.startswith(prefix)}
        definitions, outcomes = [], []
        for index, (identity, reason) in enumerate([('Example.Tests.Passes', None), *expected.items()]):
            cls, method = identity.rsplit('.', 1)
            definition = ET.Element('UnitTest', id=str(index)); ET.SubElement(definition, 'TestMethod', className=cls, name=method)
            result = ET.Element('UnitTestResult', testId=str(index), testName=identity, outcome='Passed' if reason is None else 'NotExecuted')
            if reason: ET.SubElement(ET.SubElement(ET.SubElement(result, 'Output'), 'ErrorInfo'), 'Message').text = reason
            definitions.append(definition); outcomes.append(result)
        counters = dict.fromkeys(maintenance.TRX_COUNTERS, 0) | {'total': len(outcomes), 'executed': 1, 'passed': 1}
        return definitions, outcomes, counters

    def test_skip_identity_reason_counts_and_provider_theory_fail_closed(self):
        for row, project in [(self.rows[0], 'test/component/Elsa.Workflows.ComponentTests/Elsa.Workflows.ComponentTests.csproj'),
                             (self.rows[1], 'test/unit/Elsa.UserTasks.Persistence.ConformanceTests/Elsa.UserTasks.Persistence.ConformanceTests.csproj')]:
            values = self.outcomes(row, project)
            self.assertEqual(len(values[1]) - 1, len(core.verify_outcomes(*values, row, {}, project)))
            for mutate in (lambda d, o, c: o[-1].set('testName', 'Alias'),
                           lambda d, o, c: o[-1].find('./Output/ErrorInfo/Message').__setattr__('text', 'Unknown reason'),
                           lambda d, o, c: d[-1].find('TestMethod').set('name', 'Unknown'),
                           lambda d, o, c: o[-1].set('outcome', 'Failed'),
                           lambda d, o, c: c.update(failed=1),
                           lambda d, o, c: o.pop()):
                data = deepcopy(values); mutate(*data)
                with self.subTest(line=row['line'], mutate=mutate), self.assertRaises(ValueError):
                    core.verify_outcomes(*data, row, {}, project)
        data = self.outcomes(self.rows[1], 'test/unit/Elsa.UserTasks.Persistence.ConformanceTests/Elsa.UserTasks.Persistence.ConformanceTests.csproj')
        with self.assertRaises(ValueError):
            core.verify_outcomes(*data, self.rows[1], dict.fromkeys(core.PROVIDER_GATES, 'configured'),
                                 'test/unit/Elsa.UserTasks.Persistence.ConformanceTests/Elsa.UserTasks.Persistence.ConformanceTests.csproj')

    def test_other_core_conformance_projects_do_not_inherit_user_task_skips(self):
        values = self.outcomes(self.rows[1], 'test/integration/Elsa.Workflows.Persistence.ConformanceTests/Elsa.Workflows.Persistence.ConformanceTests.csproj')
        self.assertEqual([], core.verify_outcomes(*values, self.rows[1], {},
            'test/integration/Elsa.Workflows.Persistence.ConformanceTests/Elsa.Workflows.Persistence.ConformanceTests.csproj'))

    def test_unknown_product_cannot_fall_through_to_studio_recipe(self):
        with self.assertRaisesRegex(ValueError, 'Unsupported producer product'):
            maintenance.recipes({'product': 'unknown'}, '3.8.99', Path('/private/proof'))
        with self.assertRaises(ValueError):
            maintenance.recipes({'product': 'core', 'kind': 'wrong'}, '3.8.99', Path('/private/proof'))

    def test_core_preflight_uses_only_sdk_and_never_npm_or_host(self):
        with tempfile.TemporaryDirectory() as temp:
            source = Path(temp); private = source / 'private'; private.mkdir()
            commands = []
            def run(command, *_args, **_kwargs):
                commands.append(command)
                return '10.0.300 [/sdk]' if command[-1] == '--list-sdks' else '10.0.300'
            with patch.object(artifacts, 'run', side_effect=run):
                value = artifacts.preflight(source, {'product': 'core', 'npm': None}, private)
            self.assertEqual([['dotnet', '--list-sdks'], ['dotnet', '--version']], commands)
            self.assertEqual({'product_work_executed': False, 'sdk': '10.0.300'}, value)

    def test_unbound_core_prepare_rejects_before_source_or_build(self):
        with tempfile.TemporaryDirectory() as temp:
            output = Path(temp) / 'producer'
            with patch.object(maintenance, 'run_build_command', side_effect=AssertionError('build forbidden')):
                with self.assertRaisesRegex(ValueError, 'core_original_plan_binding'):
                    maintenance.prepare(artifacts.ROOT, self.rows[0] | {'source_repository': maintenance.CORE_REPOSITORY},
                                        '3.8.99', output)
            receipt = json.loads((output / 'receipt.json').read_text())
            self.assertFalse(receipt['success']); self.assertEqual([], receipt['commands'])
            self.assertFalse((output / 'source').exists())

    def test_full_core_trx_joins_linkage_and_keeps_skips_separate(self):
        row = self.rows[0]
        project = 'test/component/Elsa.Workflows.ComponentTests/Elsa.Workflows.ComponentTests.csproj'
        with tempfile.TemporaryDirectory() as temp:
            output = Path(temp); (output / 'test-results').mkdir()
            (output / 'test-inventory.json').write_text(json.dumps([{'project': project,
                'assembly_name': 'Elsa.Workflows.ComponentTests', 'frameworks': ['net10.0']}]))
            definitions, outcomes, counters = self.outcomes(row, project)
            tree = ET.Element('TestRun'); defs = ET.SubElement(tree, 'TestDefinitions')
            results = ET.SubElement(tree, 'Results'); entries = ET.SubElement(tree, 'TestEntries')
            for index, (definition, result) in enumerate(zip(definitions, outcomes)):
                execution = 'execution-' + str(index)
                ET.SubElement(definition, 'Execution', id=execution)
                definition.find('TestMethod').set('codeBase', str((output / 'source' /
                    Path(project).parent / 'bin/Release/net10.0/Elsa.Workflows.ComponentTests.dll').resolve()))
                result.set('executionId', execution)
                ET.SubElement(entries, 'TestEntry', testId=definition.get('id'), executionId=execution)
                defs.append(definition); results.append(result)
            summary = ET.SubElement(tree, 'ResultSummary', outcome='Completed')
            ET.SubElement(summary, 'Counters', **{key: str(value) for key, value in counters.items()})
            trx = output / 'test-results/component.trx'; ET.ElementTree(tree).write(trx)
            value = maintenance.verify_tests(output, row, environment={})
            self.assertEqual(1, value['executions'][0]['counters']['passed'])
            self.assertEqual(3, len(value['executions'][0]['expected_skips']))
            results[0].set('executionId', 'changed'); ET.ElementTree(tree).write(trx)
            with self.assertRaisesRegex(ValueError, 'Test evidence rejected'):
                maintenance.verify_tests(output, row, environment={})

    def test_core_contract_module_is_in_both_workflow_modes(self):
        workflow = (artifacts.ROOT / '.github/workflows/product-release-plan.yml').read_text()
        self.assertEqual(2, workflow.count(' test_selected_core_producer '))

    def test_core_tests_outside_solution_are_evaluated_without_widening_pack_scope(self):
        row = self.rows[0]
        projects = ['src/Package.csproj', 'test/unit/Inside/Inside.csproj', 'test/unit/Outside/Outside.csproj']
        with tempfile.TemporaryDirectory() as temp:
            source = Path(temp); output = source / 'output'; output.mkdir()
            (source / 'NuGet.Config').write_text('<configuration />')
            (source / 'Elsa.sln').write_text('\n'.join('Project("guid") = "name", "' + p + '", "id"' for p in projects[:2]))
            extra_is_test = True
            def run(command, *_args, **kwargs):
                self.assertEqual('3.8.99', kwargs['env']['VERSION'])
                self.assertFalse(any(arg.startswith('-p:Version=') for arg in command))
                project = command[2]
                return json.dumps({'Properties': {'IsPackable': 'true', 'IsTestProject': str(project != projects[0] and (project != projects[2] or extra_is_test)).lower(),
                    'AssemblyName': Path(project).stem, 'PackageId': Path(project).stem, 'PackageVersion': '3.8.99',
                    'TargetFrameworks': '', 'TargetFramework': 'net10.0', 'IncludeSymbols': 'true', 'IncludeBuildOutput': 'false'}})
            with patch.object(core, 'policy', return_value={'test_projects': projects[1:]}), \
                    patch.object(maintenance, 'run', side_effect=run):
                inventory = maintenance.evaluate_inventory(source, row, '3.8.99', output)
            self.assertEqual(projects[:2], [item['project'] for item in inventory])
            self.assertEqual(set(projects[1:]), {item['project'] for item in json.loads((output / 'test-inventory.json').read_text())})
            extra_is_test = False
            with patch.object(core, 'policy', return_value={'test_projects': projects[1:]}), \
                    patch.object(maintenance, 'run', side_effect=run), \
                    self.assertRaisesRegex(ValueError, 'Core original extra test project'):
                maintenance.evaluate_inventory(source, row, '3.8.99', output)
