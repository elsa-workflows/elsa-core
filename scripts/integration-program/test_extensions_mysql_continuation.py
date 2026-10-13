"""Historical MySQL support admission and plan/inventory regressions; no restore."""
from concurrent.futures import ThreadPoolExecutor
from copy import deepcopy
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET

import extensions_mysql_continuation as mysql
import prepare_maintenance_build as maintenance
import product_release_metadata as metadata
import selected_extensions_contract as extensions
import selected_maintenance_39 as maintenance39


class ExtensionsMySqlContinuationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.contract = mysql.load_contract()
        cls.rows = {row['commit']: row for row in maintenance.registered_core_candidates(maintenance.load_register())}

    def inventory(self):
        """Model the two supported packages and an unaffected .NET10 package."""
        result = {'projects': [], 'selected': []}
        for item in self.contract['support_policy']['projects']:
            frameworks = ['net8.0', 'net9.0']
            result['projects'].append({'path': item['project'], 'package_id': item['id'],
                'target_frameworks': frameworks, 'references_by_framework': dict.fromkeys(frameworks, {})})
            result['selected'].append({**item, 'frameworks': frameworks, 'metadata': {
                'original_output_policy': dict.fromkeys(frameworks, {}),
                'dependency_groups': [{'framework': f, 'dependencies': []} for f in frameworks]}})
        policy = self.contract['support_policy']
        result['projects'].append({'path': policy['quartz_test_project'], 'target_frameworks': policy['quartz_test_frameworks']})
        result['selected'].append({'id': 'Elsa.Sql.MySql', 'project': 'src/modules/sql/Elsa.Sql.MySql/Elsa.Sql.MySql.csproj',
                                   'frameworks': ['net8.0', 'net9.0', 'net10.0']})
        return result

    def test_exact_children_keep_original_recipes_and_other_net10_paths(self):
        """Accept the complete source closure and keep old failed sources separate."""
        for line, expected in self.contract['sources'].items():
            with self.subTest(line=line):
                row, parent = self.rows[expected['commit']], self.rows[expected['parent']]
                maintenance.verify_source(maintenance.ROOT, row)
                binding = metadata.bind_source(maintenance.ROOT, 'extensions', line, row['commit'])
                self.assertEqual(mysql.policy_hash(self.contract), binding['support_policy_sha256'])
                extensions.verify_source(maintenance.ROOT, row['commit'])
                for stage in expected['chain']:
                    child, stage_parent = self.rows[stage['commit']], self.rows[stage['parent']]
                    for change in child['delta']:
                        mysql.verify_delta(child, stage_parent, change)
                for item in self.contract['support_policy']['projects']:
                    project = ET.fromstring(maintenance.git_bytes(maintenance.ROOT, row['commit'], item['project']))
                    self.assertEqual('net8.0;net9.0', project.findtext('PropertyGroup/TargetFrameworks'))
                    self.assertFalse(project.findall('.//NoWarn'))
                    self.assertFalse([group for group in project.findall('ItemGroup') if 'net10.0' in group.get('Condition', '')])
                for path in ('Directory.Packages.props', 'src/modules/Directory.Build.props',
                             'src/modules/sql/Elsa.Sql.MySql/Elsa.Sql.MySql.csproj',
                             '.github/maintenance-inert-workflows/packages.yml.source'):
                    self.assertEqual(maintenance.git_bytes(maintenance.ROOT, parent['commit'], path),
                                     maintenance.git_bytes(maintenance.ROOT, row['commit'], path))
                before = maintenance.git_bytes(maintenance.ROOT, parent['commit'], 'build/Build.cs')
                after = maintenance.git_bytes(maintenance.ROOT, row['commit'], 'build/Build.cs')
                signature = b'public Configure<DotNetTestSettings, Project> TestProjectSettings'
                self.assertEqual(before.split(signature)[0], after.split(signature)[0])
                self.assertIn(b'project.Name == "Elsa.Scheduling.Quartz.UnitTests"', after)
                self.assertIn(b'RemoveLoggers($"trx;LogFileName={project.Name}.trx")', after)
                self.assertIn(b'AddLoggers($"trx;LogFilePrefix={project.Name}")', after)
                self.assertEqual(before.split(b'.When(_ => GitHubActions.Instance')[1],
                                 after.split(b'.When(_ => GitHubActions.Instance')[1])
                with self.assertRaisesRegex(ValueError, 'source_selection'):
                    metadata.bind_source(maintenance.ROOT, 'extensions', line, parent['commit'])
                mysql.validate_inventory(binding, self.inventory(), self.contract)
        self.assertEqual(self.contract['sources']['3.9']['commit'], maintenance39.contract('extensions')['commit'])

    def test_servicebus_removes_only_unused_mysql_reference_and_suppression(self):
        """Keep actual PostgreSQL fixtures, test bodies, framework and original recipes."""
        project = 'test/modules/servicebus/Elsa.ServiceBus.AzureServiceBus.ComponentTests/Elsa.ServiceBus.AzureServiceBus.ComponentTests.csproj'
        suppression = (b'    <PropertyGroup>\n'
            b"        <!-- Pomelo.EntityFrameworkCore.MySql 9.0.0 doesn't support EF Core 10; no Pomelo 10.x exists yet -->\n"
            b'        <NoWarn>$(NoWarn);NU1608</NoWarn>\n'
            b'    </PropertyGroup>\n\n')
        reference = b'        <PackageReference Include="Elsa.Persistence.EFCore.MySql" />\n'
        for expected in self.contract['sources'].values():
            row, parent = self.rows[expected['commit']], self.rows[expected['parent']]
            before = maintenance.git_bytes(maintenance.ROOT, parent['commit'], project)
            after = maintenance.git_bytes(maintenance.ROOT, row['commit'], project)
            self.assertEqual(before.replace(suppression, b'').replace(reference, b''), after)
            self.assertNotEqual(before, after)
            files = metadata.git(maintenance.ROOT, 'ls-tree', '-r', '--name-only', row['commit'],
                                 str(Path(project).parent)).splitlines()
            for path in files:
                if path != project:
                    self.assertEqual(maintenance.git_bytes(maintenance.ROOT, parent['commit'], path),
                                     maintenance.git_bytes(maintenance.ROOT, row['commit'], path))
            fixture = maintenance.git_bytes(maintenance.ROOT, row['commit'],
                str(Path(project).parent / 'Fixtures/WorkflowServer.cs'))
            self.assertEqual(4, fixture.count(b'UsePostgreSql(dbConnectionString)'))
            self.assertNotIn(b'MySql', fixture)

    def test_live_evaluated_test_universe_rejects_previous_servicebus_dependency(self):
        """Evaluate every tracked test project without restore; catch real imports before staging."""
        version = '3.8.4-8693-mysql-preflight'
        for line, expected in self.contract['sources'].items():
            with self.subTest(line=line), tempfile.TemporaryDirectory() as private:
                row = self.rows[expected['commit']]
                source = Path(private) / 'source'
                metadata.checkout_source(maintenance.ROOT, row, source)
                paths = metadata.git(source, 'ls-files', 'test/**/*.csproj').splitlines()
                self.assertTrue(paths)
                with ThreadPoolExecutor(max_workers=4) as pool:
                    projects = list(pool.map(lambda path: metadata.evaluate_project(
                        source, path, version, binding=row), paths))
                mysql.validate_net10_references(projects, self.contract)
                project = 'test/modules/servicebus/Elsa.ServiceBus.AzureServiceBus.ComponentTests/Elsa.ServiceBus.AzureServiceBus.ComponentTests.csproj'
                servicebus = next(item for item in projects if item['path'] == project)
                self.assertEqual(['net10.0'], servicebus['target_frameworks'])
                self.assertIn('Elsa.Persistence.EFCore.PostgreSql',
                    [item['id'] for item in servicebus['references_by_framework']['net10.0']['package_references']])
                quartz = next(item for item in projects if item['path'] == self.contract['support_policy']['quartz_test_project'])
                self.assertEqual(['net8.0', 'net9.0', 'net10.0'], quartz['target_frameworks'])
                parent = self.rows[expected['parent']]
                metadata.git(source, 'checkout', '--quiet', '--detach', parent['commit'])
                previous = metadata.evaluate_project(source, project, version, binding=parent)
                with self.assertRaisesRegex(ValueError, 'inventory_mysql_net10_reference'):
                    mysql.validate_net10_references([previous], self.contract)

    def test_protected_exception_rejects_unregistered_project_edits_and_ancestry(self):
        """A broader TFM cut, dependency edit or invented descendant cannot use this exception."""
        expected = self.contract['sources']['3.8']
        row, parent = self.rows[expected['commit']], self.rows[expected['parent']]
        for mutate in (lambda r: r.update(commit=parent['commit']), lambda r: r.update(tree='f'*40),
                       lambda r: r.update(product='studio'), lambda r: r.update(line='3.9'),
                       lambda r: r.update(parents=[parent['original_commit']]),
                       lambda r: r['delta'][0]['after'].update(blob='f'*40),
                       lambda r: r['delta'][0].update(path='Directory.Packages.props'),
                       lambda r: r['delta'].append(deepcopy(r['delta'][0]))):
            changed = deepcopy(row); mutate(changed)
            with self.subTest(row=changed), self.assertRaises(ValueError):
                mysql.verify_delta(changed, parent, changed['delta'][0])

    def test_plan_rejects_net10_fallback_missing_mysql_or_incomplete_supported_metadata(self):
        """Support policy cannot be weakened by omitted packages, net9-only coverage or net10 aliases."""
        row = self.rows[self.contract['sources']['3.8']['commit']]
        source = {**row, **mysql.binding(row, self.contract)}
        for mutate in (lambda p: p['selected'][0].update(frameworks=['net8.0', 'net9.0', 'net10.0']),
                       lambda p: p['selected'].pop(0),
                       lambda p: p['projects'][0].update(target_frameworks=['net9.0']),
                       lambda p: p['projects'][0]['references_by_framework'].update({'net10.0': {}}),
                       lambda p: p['selected'][0]['metadata']['original_output_policy'].update({'net10.0': {}}),
                       lambda p: p['selected'][0]['metadata']['dependency_groups'].pop(),
                       lambda p: p['projects'][-1].update(target_frameworks=['net10.0'])):
            inventory = self.inventory(); mutate(inventory)
            with self.subTest(inventory=inventory), self.assertRaises(ValueError):
                mysql.validate_inventory(source, inventory, self.contract)
        for key, value in (('support_policy_sha256', 'f'*64), ('commit', row['parents'][0]), ('tree', 'f'*40)):
            with self.subTest(key=key), self.assertRaises(ValueError):
                mysql.validate_inventory(source | {key: value}, self.inventory(), self.contract)

    def test_workflow_covers_support_contract_in_both_modes(self):
        """Run support admission in both modes whenever any consumed source input changes."""
        workflow = (maintenance.ROOT / '.github/workflows/product-release-plan.yml').read_text()
        for prefix in ('python3 -m unittest ', 'python3 -O -m unittest '):
            command = next(line for line in workflow.splitlines() if line.strip().startswith(prefix))
            self.assertIn('test_extensions_mysql_continuation', command)
        self.assertIn('scripts/integration-program/extensions_mysql_continuation_contract.json', metadata.PLANNER_INPUTS)

    def test_net10_hosts_and_test_imports_cannot_fall_back_to_net9_mysql(self):
        """Reject a MySQL EF dependency even when the selected MySQL package has only 8/9 assets."""
        row = self.rows[self.contract['sources']['3.8']['commit']]
        source = {**row, **mysql.binding(row, self.contract)}
        for package in ('Elsa.Persistence.EFCore.MySql', 'Pomelo.EntityFrameworkCore.MySql',
                        'Elsa.Agents.Persistence.EFCore.MySql'):
            inventory = self.inventory()
            inventory['projects'][-1]['references_by_framework'] = {'net10.0': {
                'package_references': [{'id': package}], 'project_references': []}}
            with self.subTest(package=package), self.assertRaisesRegex(ValueError, 'inventory_mysql_net10_reference'):
                mysql.validate_inventory(source, inventory, self.contract)
        inventory = self.inventory()
        inventory['selected'][-1]['metadata'] = {'dependency_groups': [{'framework': 'net10.0',
            'dependencies': [{'id': 'Elsa.Scheduling.Quartz.EFCore.MySql'}]}]}
        with self.assertRaisesRegex(ValueError, 'inventory_mysql_net10_dependency'):
            mysql.validate_inventory(source, inventory, self.contract)


if __name__ == '__main__':
    unittest.main()
