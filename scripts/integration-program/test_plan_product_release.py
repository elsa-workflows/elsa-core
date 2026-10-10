from __future__ import annotations

from copy import deepcopy
from datetime import datetime, timedelta, timezone
from contextlib import redirect_stderr, redirect_stdout
import io
import json
from pathlib import Path
import subprocess
import tempfile
import threading
import unittest
from unittest.mock import patch

import plan_product_release as planner
import product_release_metadata as metadata


class ProductReleasePlanTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if not any(line.startswith(metadata.SDK + ' ') for line in subprocess.run(
                ['dotnet', '--list-sdks'], capture_output=True, text=True).stdout.splitlines()):
            raise unittest.SkipTest('Product release contracts require the explicitly selected SDK ' + metadata.SDK)
        cls.temporary = tempfile.TemporaryDirectory(prefix='product-plan-contracts-')
        cls.addClassCleanup(cls.temporary.cleanup)
        cls.semantics = planner.build_helper(Path(cls.temporary.name))

    def setUp(self):
        self.time = datetime.now(timezone.utc).isoformat()
        self.binding = {'product': 'extensions', 'line': '3.8', 'commit': 'a' * 40, 'tree': 'b' * 40}
        self.controller = {'commit': 'c' * 40, 'tree': 'd' * 40}
        self.project = {'path': 'src/Example/Example.csproj', 'package_id': 'Example', 'is_packable': True,
                        'is_test_project': False, 'target_frameworks': ['net8.0'],
                        'project_references': [], 'package_references': [], 'properties': {}}
        self.inventory = {'source_commit': self.binding['commit'], 'source_tree': self.binding['tree'],
            'requested_version': '3.8.5', 'projects': [self.project], 'excluded': [],
            'release_recipe': {'projects': [self.project['path']]},
            'ownership_policy': metadata.ownership_policy(),
            'selected': [{'id': 'Example', 'project': self.project['path'], 'frameworks': ['net8.0'], 'symbols': True,
                'metadata': {'status': 'observed', 'dependency_groups': [{'framework': 'net8.0', 'dependencies': []}],
                             '_assets': {'targets': {'net8.0': {}}}}}]}
        self.rehash()
        self.edge = {'consumer': 'Example', 'project': self.project['path'], 'framework': 'net8.0',
                     'id': 'Dependency', 'version': '1.2.3', 'range': '[1.0,2.0)'}

    def rehash(self):
        self.inventory['sha256'] = metadata.canonical_hash(metadata.public_inventory(self.inventory))

    def observation(self, expected_url, body, **changes):
        if isinstance(body, (dict, list)):
            body = json.dumps(body).encode()
        elif isinstance(body, str):
            body = body.encode()
        return {'url': expected_url, 'status': 'observed', 'observed_at': self.time,
                'sha256': metadata.sha256(body), 'bytes': len(body), '_body': body, **changes}

    def history(self, versions=('3.8.4', '3.9.10'), **changes):
        observation = self.observation(planner.history_url('Example'), {'versions': list(versions)}, **changes)
        return planner.check_history('Example', '3.8.5', '3.8', observation, self.semantics, self.time)

    def prerequisite(self, *, xml=None, edge=None, **changes):
        edge = edge or self.edge
        xml = xml or '<package><metadata><id>Dependency</id><version>1.2.3</version><dependencies><group targetFramework="netstandard2.0" /></dependencies></metadata></package>'
        observation = self.observation(planner.nuspec_url(edge['id'], edge['version']), xml, **changes)
        return planner.check_prerequisite(edge, observation, self.semantics, self.time)

    def plan(self, *, histories=None, prerequisites=None, internal=None, npm=None):
        return planner.assemble_plan(self.binding, self.inventory, '3.8.5', histories or [self.history()],
            prerequisites or [], internal or [], npm, self.controller, self.semantics)

    def test_all_six_product_line_choices_have_closed_membership(self):
        for product in ('core', 'studio', 'extensions'):
            for line in ('3.8', '3.9'):
                with self.subTest(product=product, line=line):
                    binding = {**self.binding, 'product': product, 'line': line}
                    inventory = deepcopy(self.inventory)
                    inventory['requested_version'] = line + '.5'
                    inventory['sha256'] = metadata.canonical_hash(metadata.public_inventory(inventory))
                    metadata_binding = metadata.DESCENDANTS.get((product, line))
                    self.assertEqual(product != 'core', metadata_binding is not None)
                    planner.validate_inventory(inventory, binding, line + '.5')

    def test_history_uses_line_local_native_ordering(self):
        self.assertTrue(self.history()['eligible'])
        self.assertFalse(self.history(('3.8.6',))['eligible'])
        self.assertFalse(self.history(('3.8.6-rc.1',))['eligible'])

    def test_normalized_reuse_including_zero_revision_and_metadata(self):
        for version in ('3.8.5', '3.8.5.0', '3.8.5+retained'):
            with self.subTest(version=version):
                self.assertEqual('version_reused', self.history((version,))['reason'])

    def test_history_ambiguous_normalized_aliases(self):
        self.assertEqual('history_ambiguous', self.history(('3.8.4', '3.8.4.0'))['reason'])

    def test_history_unavailable_missing_stale_and_wrong_origin(self):
        stale = (datetime.now(timezone.utc) - timedelta(seconds=planner.MAX_AGE_SECONDS + 1)).isoformat()
        for changes, reason in (({'status': 'unavailable'}, 'history_unavailable'),
                ({'status': 'missing'}, 'history_missing'), ({'observed_at': stale}, 'history_stale'),
                ({'url': 'https://foreign.example/index.json'}, 'history_malformed')):
            with self.subTest(reason=reason):
                self.assertEqual(reason, self.history(**changes)['reason'])

    def test_malformed_or_empty_history_cannot_authorize_version(self):
        self.assertEqual('history_malformed', self.history(())['reason'])
        self.assertEqual('history_malformed', self.history(('not-a-version',))['reason'])

    def test_prerelease_ordering_uses_native_nuget_comparer(self):
        decision = self.semantics.call('history', requested='3.8.5-rc.10', line='3.8', versions=['3.8.5-RC.2'])
        self.assertTrue(decision['monotonic'])
        decision = self.semantics.call('history', requested='3.8.5-rc.2', line='3.8', versions=['3.8.5-RC.2+old'])
        self.assertTrue(decision['reused'])

    def test_native_bare_exact_and_bounded_ranges(self):
        decisions = self.semantics.call('ranges', values=[
            {'range': '1.0', 'version': '1.2.3'}, {'range': '[1.0]', 'version': '1.2.3'},
            {'range': '(1.0,2.0]', 'version': '2.0'}, {'range': '[1.0,2.0)', 'version': '2.0'}])
        self.assertEqual([True, False, True, False], [row['satisfies'] for row in decisions])

    def test_prerequisite_available_netstandard_fallback(self):
        result = self.prerequisite()
        self.assertTrue(result['eligible'])
        self.assertEqual('netstandard2.0', result['applicable_dependency_group']['framework'])
        self.assertEqual('dependency-metadata-only', result['compatibility_scope'])

    def test_tfm_compatibility_is_directional(self):
        xml = '<package><metadata><id>Dependency</id><version>1.2.3</version><dependencies><group targetFramework="net9.0" /></dependencies></metadata></package>'
        self.assertEqual('prerequisite_incompatible_framework', self.prerequisite(xml=xml)['reason'])
        self.assertTrue(self.prerequisite(xml=xml, edge={**self.edge, 'framework': 'net10.0'})['eligible'])

    def test_empty_dependency_metadata_is_any_not_asset_proof(self):
        xml = '<package><metadata><id>Dependency</id><version>1.2.3</version></metadata></package>'
        result = self.prerequisite(xml=xml)
        self.assertTrue(result['eligible'])
        self.assertEqual('any', result['applicable_dependency_group']['framework'])

    def test_prerequisite_wrong_identity_version_or_range(self):
        for xml in ('<package><metadata><id>Wrong</id><version>1.2.3</version></metadata></package>',
                    '<package><metadata><id>Dependency</id><version>1.2.4</version></metadata></package>'):
            self.assertEqual('prerequisite_wrong_version', self.prerequisite(xml=xml)['reason'])
        self.assertEqual('prerequisite_wrong_version', self.prerequisite(edge={**self.edge, 'range': '[2.0]'})['reason'])

    def test_prerequisite_duplicate_casefold_ids_and_canonical_groups(self):
        for content in ('<group targetFramework="net8.0"><dependency id="Other" version="1.0"/><dependency id="other" version="1.0"/></group>',
                        '<group targetFramework="net8.0"/><group targetFramework=".NETCoreApp,Version=v8.0"/>'):
            xml = '<package><metadata><id>Dependency</id><version>1.2.3</version><dependencies>' + content + '</dependencies></metadata></package>'
            self.assertEqual('prerequisite_malformed', self.prerequisite(xml=xml)['reason'])

    def test_prerequisite_missing_unavailable_malformed_and_stale(self):
        for status in ('missing', 'unavailable'):
            self.assertEqual('prerequisite_' + status, self.prerequisite(status=status)['reason'])
        self.assertEqual('prerequisite_malformed', self.prerequisite(xml='broken')['reason'])
        self.assertEqual('prerequisite_malformed', self.prerequisite(edge={**self.edge, 'range': 'broken'})['reason'])
        self.assertEqual('prerequisite_stale', self.prerequisite(observed_at='2000-01-01T00:00:00+00:00')['reason'])

    def test_plan_does_not_allocate_publish_or_claim_artifacts(self):
        result = self.plan()
        self.assertTrue(result['eligible'])
        self.assertFalse(result['published'])
        self.assertFalse(result['version_allocated'])
        self.assertFalse(result['tag_created'])
        self.assertEqual(['Example.3.8.5.nupkg', 'Example.3.8.5.snupkg'], result['expected_artifacts'])
        self.assertIsNone(result['npm'])

    def test_changed_missing_or_overlapping_inventory_rejected(self):
        for change in ('hash', 'source', 'overlap', 'missing', 'duplicate'):
            with self.subTest(change=change):
                old = deepcopy(self.inventory)
                if change == 'hash': self.inventory['selected'][0]['id'] = 'Wrong'
                if change == 'source': self.inventory['source_commit'] = 'f' * 40
                if change == 'overlap': self.inventory['excluded'] = [{'project': self.project['path'], 'id': 'Example', 'reason': 'x'}]
                if change == 'missing': self.inventory['selected'] = []
                if change == 'duplicate': self.inventory['selected'] *= 2
                if change != 'hash': self.rehash()
                with self.assertRaises(ValueError): self.plan()
                self.inventory = old

    def test_histories_must_cover_each_selected_id_once(self):
        for histories in ([self.history(), self.history()], [{**self.history(), 'id': 'Wrong'}]):
            with self.assertRaisesRegex(ValueError, 'history_inventory'):
                self.plan(histories=histories)

    def test_outgoing_prerequisites_cannot_be_omitted_duplicated_or_selected(self):
        policy = self.inventory['selected'][0]['metadata']
        policy['dependency_groups'][0]['dependencies'] = [{'id': 'Dependency', 'version': '[1.0,2.0)'}]
        policy['_assets']['targets']['net8.0'] = {'Dependency/1.2.3': {'type': 'package'}}
        self.rehash()
        with self.assertRaisesRegex(ValueError, 'prerequisite_inventory'): self.plan()
        result = self.prerequisite()
        self.assertTrue(self.plan(prerequisites=[result])['eligible'])
        with self.assertRaisesRegex(ValueError, 'prerequisite_inventory'): self.plan(prerequisites=[result, result])
        with self.assertRaisesRegex(ValueError, 'prerequisite_widens_selection'):
            self.plan(prerequisites=[{**result, 'id': 'Example'}])

    def test_unavailable_source_metadata_blocks_without_deleting_member(self):
        self.inventory['selected'][0]['metadata'] = {'status': 'unavailable', 'reason': 'source_metadata_unavailable'}
        self.rehash()
        result = self.plan()
        self.assertFalse(result['eligible'])
        self.assertEqual(1, len(result['inventory']['selected']))

    def test_settled_ownership_preserves_five_noncanonical_occurrences(self):
        for identifier, owner in metadata.CANONICAL_OWNERS.items():
            project = {**self.project, 'package_id': identifier}
            self.assertEqual('canonical_owner_' + owner, metadata.project_scope(project, 'extensions', {project['path']}))
            self.assertIsNone(metadata.project_scope(project, owner, {project['path']}))
        self.assertIsNone(metadata.project_scope({**self.project, 'package_id': 'Elsa.Secrets.Api'}, 'extensions', {self.project['path']}))

    def test_packable_project_outside_original_recipe_is_excluded(self):
        self.assertEqual('outside_original_release_recipe', metadata.project_scope(self.project, 'core', set()))

    def test_dangling_project_reference_rejected_before_graph_assembly(self):
        self.project['project_references'] = [{'target_project': 'src/Missing/Missing.csproj'}]
        self.rehash()
        with self.assertRaisesRegex(ValueError, 'untracked_project_reference'):
            self.plan()

    def test_conditional_framework_references_are_retained_unioned_and_validated(self):
        with tempfile.TemporaryDirectory(dir=self.temporary.name) as directory:
            source = Path(directory)
            project = source / 'Example.csproj'
            project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
                '<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks></PropertyGroup>'
                '<ItemGroup><PackageReference Include="Shared" Version="1.0"/></ItemGroup>'
                '<ItemGroup Condition="\'$(TargetFramework)\' != \'net10.0\'">'
                '<PackageReference Include="Conditional" Version="2.0" PrivateAssets="all"/>'
                '<ProjectReference Include="Missing.csproj"/></ItemGroup></Project>')
            result = metadata.evaluate_project(source, project.name, '3.8.5')
            self.assertEqual([{'id': 'Conditional'}, {'id': 'Shared'}], result['package_references'])
            self.assertEqual([{'target_project': 'Missing.csproj'}], result['project_references'])
            for framework in ('net8.0', 'net9.0'):
                refs = result['references_by_framework'][framework]
                self.assertEqual(['Conditional', 'Shared'], [row['id'] for row in refs['package_references']])
                self.assertEqual('all', refs['package_references'][0]['PrivateAssets'])
            self.assertEqual(['Shared'], [row['id'] for row in result['references_by_framework']['net10.0']['package_references']])
            self.assertEqual([], result['references_by_framework']['net10.0']['project_references'])
            with self.assertRaisesRegex(ValueError, 'untracked_project_reference'):
                metadata.validate_project_references([result])
            self.assertFalse(list(source.rglob('*.dll')))

    def test_same_framework_casefold_duplicate_reference_is_ambiguous(self):
        with tempfile.TemporaryDirectory(dir=self.temporary.name) as directory:
            source = Path(directory)
            project = source / 'Example.csproj'
            project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework>'
                '</PropertyGroup><ItemGroup><PackageReference Include="Shared" Version="1.0"/>'
                '<PackageReference Include="shared" Version="2.0"/></ItemGroup></Project>')
            with self.assertRaisesRegex(ValueError, 'metadata_duplicate_package_reference'):
                metadata.evaluate_project(source, project.name, '3.8.5')

    def test_full_inventory_retains_ownership_exclusions_and_strips_private_state(self):
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory)
            identifiers = list(metadata.CANONICAL_OWNERS) + ['Elsa.Secrets.Api', 'Elsa.Secrets.Core']
            projects = [{**self.project, 'path': f'src/P{index}/P{index}.csproj', 'package_id': identifier,
                'properties': {'PackageVersion': '3.8.5', 'IncludeBuildOutput': 'true', 'IncludeSymbols': 'true',
                    'SymbolPackageFormat': 'snupkg', 'RepositoryUrl': 'https://github.com/elsa-workflows/elsa-core',
                    'PackageProjectUrl': 'https://github.com/elsa-workflows/elsa-core', 'PrivatePath': '/private/secret'}}
                for index, identifier in enumerate(identifiers)]
            for project in projects:
                path = source / project['path']
                path.parent.mkdir(parents=True)
                path.touch()
            (source / 'Elsa.Extensions.sln').write_text('\n'.join(
                f'Project("type") = "P", "{row["path"]}", "id"' for row in projects))
            (source / '.nuke').mkdir()
            (source / '.nuke/parameters.json').write_text('{"Solution":"Elsa.Extensions.sln"}')
            workflow = source / '.github/maintenance-inert-workflows/packages.yml.source'
            workflow.parent.mkdir(parents=True)
            workflow.write_text('Compile+Test+Pack')
            staged, writer_thread = [], threading.get_ident()
            def stage(root, project, version, destination):
                self.assertEqual(writer_thread, threading.get_ident())
                staged.append(project['path'])
                return {'status': 'observed', '_assets': {'private': '/private/assets'}}
            with patch.object(metadata, 'git', return_value='\n'.join(row['path'] for row in projects)), \
                 patch.object(metadata, 'evaluate_project', side_effect=lambda root, path, version: next(row for row in projects if row['path'] == path)), \
                 patch.object(metadata, 'stage_project', side_effect=stage):
                inventory = metadata.evaluate_inventory(source, self.binding, '3.8.5', source / 'private', workers=4)
            self.assertEqual(7, len(inventory['projects']))
            self.assertEqual(5, len(inventory['excluded']))
            self.assertEqual(['Elsa.Secrets.Api', 'Elsa.Secrets.Core'], [row['id'] for row in inventory['selected']])
            self.assertEqual([row['project'] for row in inventory['selected']], staged)
            public = metadata.public_inventory(inventory)
            self.assertNotIn('/private/', json.dumps(public))
            self.assertNotIn('properties', public['projects'][0])
            self.assertNotIn('_assets', public['selected'][0]['metadata'])
            planner.validate_inventory(inventory, self.binding, '3.8.5')

    def test_native_source_mapping_precedence_and_unknown_source_are_preserved(self):
        with tempfile.TemporaryDirectory() as directory:
            config = Path(directory) / 'NuGet.Config'
            config.write_text('<configuration><packageSources><clear/>'
                '<add key="public" value="https://api.nuget.org/v3/index.json"/>'
                '<add key="preview" value="https://f.feedz.io/elsa-workflows/elsa-3/nuget/index.json"/>'
                '</packageSources><packageSourceMapping>'
                '<packageSource key="public"><package pattern="*"/></packageSource>'
                '<packageSource key="preview"><package pattern="Elsa.*"/></packageSource>'
                '<packageSource key="missing"><package pattern="WebhooksCore.*"/></packageSource>'
                '</packageSourceMapping></configuration>')
            feeds = planner.FeedMetadata(config, ['Elsa.Core', 'Other', 'WebhooksCore.Api'], self.semantics, planner.Observations())
            preview = 'https://f.feedz.io/elsa-workflows/elsa-3/nuget/index.json'
            self.assertEqual([preview], feeds.indexes('Elsa.Core'))
            self.assertEqual([planner.NUGET_INDEX], feeds.indexes('Other'))
            self.assertEqual(sorted([preview, planner.NUGET_INDEX]), feeds.indexes('Elsa.Core', history=True))
            self.assertIsNone(feeds.indexes('WebhooksCore.Api'))

    def test_restore_scope_requires_only_exact_source_config_and_known_sdk_local_source(self):
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory)
            (source / 'NuGet.Config').write_text('<configuration/>')
            restore = {'configFilePaths': [str(source / 'NuGet.Config')],
                       'sources': {planner.NUGET_INDEX: {}, '/dotnet/library-packs': {}}}
            assets = {'project': {'restore': restore}}
            scope = metadata.restore_scope(source, assets, '/dotnet/sdk/10.0.300')
            self.assertNotIn(directory, json.dumps(scope))
            self.assertEqual(['selected-sdk-library-packs'], scope['local_sources'])
            restore['configFilePaths'].append('/private/user/NuGet.Config')
            with self.assertRaisesRegex(ValueError, 'restore_config_scope'):
                metadata.restore_scope(source, assets, '/dotnet/sdk/10.0.300')
            restore['configFilePaths'].pop()
            restore['sources']['/private/local-feed'] = {}
            with self.assertRaisesRegex(ValueError, 'restore_source_scope'):
                metadata.restore_scope(source, assets, '/dotnet/sdk/10.0.300')

    def test_applicable_feeds_preserve_missing_unavailable_and_conflicting_metadata(self):
        feeds = object.__new__(planner.FeedMetadata)
        available = self.prerequisite()
        missing = {**available, 'eligible': False, 'reason': 'prerequisite_missing'}
        with patch.object(feeds, 'checks', return_value=[available, missing]):
            self.assertTrue(feeds.prerequisite(self.edge, self.semantics, self.time)['eligible'])
        with patch.object(feeds, 'checks', return_value=[missing]):
            self.assertEqual('prerequisite_missing_from_applicable_feeds', feeds.prerequisite(self.edge, self.semantics, self.time)['reason'])
        unavailable = {**missing, 'reason': 'prerequisite_unavailable'}
        with patch.object(feeds, 'checks', return_value=[available, unavailable]):
            self.assertEqual('prerequisite_unavailable', feeds.prerequisite(self.edge, self.semantics, self.time)['reason'])
        conflict = {**available, 'observation': {**available['observation'], 'sha256': 'f' * 64}}
        with patch.object(feeds, 'checks', return_value=[available, conflict]):
            self.assertEqual('prerequisite_feed_ambiguity', feeds.prerequisite(self.edge, self.semantics, self.time)['reason'])

    def test_feed_discovery_rejects_foreign_package_endpoint_without_fetch(self):
        feeds = object.__new__(planner.FeedMetadata)
        feeds.services = {}
        feeds.observations = planner.Observations()
        document = {'resources': [{'@type': 'PackageBaseAddress/3.0.0', '@id': 'https://foreign.example/packages/'}]}
        observation = self.observation(planner.NUGET_INDEX, document)
        with patch.object(feeds.observations, 'get', return_value=observation) as get:
            self.assertFalse(feeds.service(planner.NUGET_INDEX)['eligible'])
            get.assert_called_once_with(planner.NUGET_INDEX)
        with self.assertRaisesRegex(ValueError, 'observation_origin'):
            planner.Observations().get('https://foreign.example/packages/')

    def test_json_root_and_nested_npm_shapes_fail_closed(self):
        identifier = planner.NPM_IDS[0]
        for document in ([], 'null', {'name': identifier, 'versions': [], 'time': {}},
                         {'name': identifier, 'versions': {}, 'time': []}):
            observation = self.observation(planner.history_url(identifier, True), document)
            self.assertFalse(planner.check_history(identifier, '3.8.5', '3.8', observation, self.semantics, self.time, npm=True)['eligible'])

    def test_source_wrong_product_line_or_unregistered_commit_rejected(self):
        for product, line, commit in (('core', '3.8', '0' * 40), ('studio', '3.8', metadata.DESCENDANTS[('studio', '3.9')]),
                                     ('extensions', '3.8', metadata.DESCENDANTS[('studio', '3.8')])):
            with self.subTest(product=product, line=line):
                with self.assertRaisesRegex(ValueError, 'source_selection'):
                    metadata.bind_source(metadata.ROOT, product, line, commit)

    def test_actual_four_registered_descendants_remain_admitted(self):
        for (product, line), commit in metadata.DESCENDANTS.items():
            with self.subTest(product=product, line=line):
                binding = metadata.bind_source(metadata.ROOT, product, line, commit)
                self.assertEqual(commit, binding['commit'])
                self.assertEqual('admitted-maintenance-descendant', binding['kind'])

    def test_studio_npm_pair_requires_both_member_histories_and_same_source_version(self):
        self.binding['product'] = 'studio'
        npm = {'atomic': True, 'line': self.binding['line'], 'source_commit': self.binding['commit'], 'source_tree': self.binding['tree'],
               'packages': [{'id': identifier, 'version': '3.8.5', 'expected_tarball': 'example-' + str(index) + '.tgz'}
                            for index, identifier in enumerate(planner.NPM_IDS)]}
        histories = [self.history()] + [{'id': identifier, 'eligible': True, 'reason': None} for identifier in planner.NPM_IDS]
        self.assertTrue(self.plan(histories=histories, npm=npm)['eligible'])
        for field in ('version', 'source'):
            broken = deepcopy(npm)
            if field == 'version': broken['packages'][1]['version'] = '3.8.6'
            else: broken['source_commit'] = 'f' * 40
            with self.assertRaisesRegex(ValueError, 'npm_pair_identity'):
                self.plan(histories=histories, npm=broken)
        with self.assertRaisesRegex(ValueError, 'history_inventory'):
            self.plan(histories=histories[:-1], npm=npm)

    def test_npm_tombstone_prevents_reuse_and_partial_packument_blocks(self):
        identifier = planner.NPM_IDS[0]
        document = {'name': identifier, 'versions': {'3.8.4': {}},
                    'time': {'3.8.4': self.time, '3.8.5': self.time}}
        observation = self.observation(planner.history_url(identifier, True), document)
        result = planner.check_history(identifier, '3.8.5', '3.8', observation, self.semantics, self.time, npm=True)
        self.assertEqual('version_reused', result['reason'])
        del document['time']
        observation = self.observation(planner.history_url(identifier, True), document)
        result = planner.check_history(identifier, '3.8.5', '3.8', observation, self.semantics, self.time, npm=True)
        self.assertFalse(result['eligible'])

    def test_flat_dependencies_keep_any_framework_and_reject_duplicates_or_mixed_structure(self):
        prefix = '<package><metadata><id>Dependency</id><version>1.2.3</version>'
        suffix = '</metadata></package>'
        flat = '<dependencies><dependency id="Other" version="1.0"/></dependencies>'
        self.assertTrue(self.prerequisite(xml=prefix + flat + suffix)['eligible'])
        for dependencies in (
            '<dependencies><dependency id="Other" version="1.0"/><dependency id="other" version="1.0"/></dependencies>',
            '<dependencies/><dependencies/>',
            '<dependencies><dependency id="Other" version="1.0"/><group targetFramework="net8.0"/></dependencies>'):
            self.assertEqual('prerequisite_malformed', self.prerequisite(xml=prefix + dependencies + suffix)['reason'])

    def test_unknown_group_child_cannot_be_silently_discarded(self):
        xml = '<package><metadata><id>Dependency</id><version>1.2.3</version><dependencies><group targetFramework="net8.0"><unexpected/></group></dependencies></metadata></package>'
        self.assertEqual('prerequisite_malformed', self.prerequisite(xml=xml)['reason'])

    def test_existing_output_is_path_free_failure_without_overwrite_or_tool_build(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory)
            sentinel = output / 'retained.txt'
            sentinel.write_text('retained')
            stdout = io.StringIO()
            with patch('sys.argv', ['planner', '--product', 'extensions', '--line', '3.8',
                                   '--version', '3.8.5', '--output', str(output)]), \
                 patch.object(planner, 'build_helper', side_effect=AssertionError('tool build forbidden')), redirect_stdout(stdout):
                self.assertEqual(1, planner.main())
            self.assertNotIn(directory, stdout.getvalue())
            self.assertEqual('plan_output_exists', json.loads(stdout.getvalue())['category'])
            self.assertEqual('retained', sentinel.read_text())
            self.assertFalse((output / 'plan.json').exists())

    def test_workflow_selects_explicit_six_cells_and_manual_one(self):
        cells = planner.workflow_cells({'EVENT': 'pull_request', 'REF': 'refs/pull/1/merge'})
        self.assertEqual({(product, line) for product in ('core', 'studio', 'extensions') for line in ('3.8', '3.9')},
                         {(row['product'], row['line']) for row in cells})
        manual = planner.workflow_cells({'EVENT': 'workflow_dispatch', 'REF': 'refs/heads/main',
            'PRODUCT': 'studio', 'LINE': '3.8', 'VERSION': '3.8.9'})
        self.assertEqual([{'product': 'studio', 'line': '3.8', 'version': '3.8.9'}], manual)
        for environment in ({'EVENT': 'push', 'REF': 'refs/heads/main'},
                {'EVENT': 'workflow_dispatch', 'REF': 'refs/heads/foreign'}, {'EVENT': 'release', 'REF': 'refs/tags/3.8.5'}):
            with self.assertRaises(ValueError): planner.workflow_cells(environment)

    def test_redirect_or_oversized_get_cannot_be_evidence(self):
        class Response:
            status = 200
            url = planner.history_url('Example')
            def __enter__(self): return self
            def __exit__(self, *args): pass
            def read(self, size): return b'x' * size
        observations = planner.Observations()
        with patch.object(observations.opener, 'open', return_value=Response()):
            self.assertEqual('unavailable', observations.get(Response.url)['status'])
        import urllib.error
        with patch.object(observations.opener, 'open', side_effect=urllib.error.HTTPError(Response.url, 302, 'redirect', {}, None)):
            self.assertEqual('unavailable', observations.get(Response.url)['status'])

    def test_310_is_explicitly_ineligible_without_source_or_feed_execution(self):
        with patch.object(metadata, 'git', side_effect=lambda root, *args: '' if args[0] == 'status' else 'a' * 40), \
             patch.object(planner.Observations, 'get', side_effect=AssertionError('feed not allowed')):
            result = planner.execute(metadata.ROOT, 'studio', '3.10', '3.10.0', Path(self.temporary.name) / '310', self.semantics)
        self.assertFalse(result['eligible'])
        self.assertEqual('aligned_baseline_pending', result['reasons'][0]['category'])


class ProductReleaseMetadataProjectionTests(unittest.TestCase):
    def setUp(self):
        self.directory = Path(self.enterContext(tempfile.TemporaryDirectory(prefix='metadata-projection-contract-')))
        self.source = self.directory / 'source'
        self.source.mkdir()
        (self.source / 'NuGet.Config').write_text('<configuration/>')
        self.project_path = 'src/Example/Example.csproj'
        path = self.source / self.project_path
        path.parent.mkdir(parents=True)
        path.write_text('<Project Sdk="Microsoft.NET.Sdk.Razor"/>')
        self.project = {'path': self.project_path, 'package_id': 'Example', 'is_packable': True,
                        'target_frameworks': ['net8.0']}

    def test_three_file_collection_projections_retain_original_policy_and_sdk_groups(self):
        sdk = self.directory / 'dotnet/sdk/10.0.300'
        sdk.mkdir(parents=True)
        (sdk / 'NuGet.Build.Tasks.Pack.targets').write_text('sdk-target-identity')
        properties = {key: 'true' for key in ('IncludeBuildOutput', 'IncludeContentInPack', 'IncludeSymbols',
            'GenerateElsaPackageManifest', 'ElsaPackageManifestIncludeInPackage')}
        properties.update(PackageId='Example', PackageVersion='3.8.5', SymbolPackageFormat='snupkg',
                          ElsaPackageManifestPackagePath='manifest.json', MSBuildToolsPath=str(sdk))
        destination = self.directory / 'metadata'
        xml = '<package><metadata><id>Example</id><version>3.8.5</version><dependencies><group targetFramework="net8.0"><dependency id="Other" version="[1.0]"/></group></dependencies></metadata></package>'
        commands = []
        def sdk_metadata(command, source, **kwargs):
            commands.append(command)
            if '-restore' in command:
                (destination / 'Example.nuspec').write_text(xml)
                assets = self.source / 'src/Example/obj/project.assets.json'
                assets.parent.mkdir()
                assets.write_text(json.dumps({'project': {'restore': {'configFilePaths': [str(self.source / 'NuGet.Config')],
                    'sources': {planner.NUGET_INDEX: {}}}}, 'libraries': {}}))
                return ''
            return json.dumps({'Properties': properties})
        with patch.object(metadata, 'run', side_effect=sdk_metadata), \
             patch.object(metadata, 'evaluate_project', return_value={**self.project, 'properties': properties}):
            result = metadata.stage_project(self.source, self.project, '3.8.5', destination)
        self.assertEqual('observed', result['status'])
        self.assertEqual([{'framework': 'net8.0', 'dependencies': [
            {'id': 'Other', 'version': '[1.0]', 'include': '', 'exclude': ''}]}], result['dependency_groups'])
        for name in ('IncludeBuildOutput', 'IncludeContentInPack', 'ElsaPackageManifestIncludeInPackage'):
            self.assertIn('-p:' + name + '=false', commands[0])
            self.assertFalse(result['projection'][name])
            self.assertEqual('true', result['original_output_policy']['net8.0'][name])
        self.assertEqual(metadata.sha256((self.source / self.project_path).read_bytes()), result['original_content_project_sha256'])
        self.assertFalse((self.source / 'src/Example/wwwroot').exists())

    def test_unavailable_metadata_retains_closed_stage_and_category_without_private_error(self):
        for error, category in ((ValueError('Command failed (1): /private/command'), 'command_failed'),
                (OSError('/private/file'), 'metadata_io_failed'),
                (subprocess.TimeoutExpired('/private/command', 300), 'command_timeout')):
            with self.subTest(category=category), patch.object(metadata, 'run', side_effect=error):
                result = metadata.stage_project(self.source, self.project, '3.8.5', self.directory / category)
                self.assertEqual({'status': 'unavailable', 'reason': 'source_metadata_unavailable',
                    'failure_stage': 'generate_nuspec', 'failure_category': category}, result)
                self.assertNotIn('/private/', json.dumps(result))

    def test_nuspec_identity_failure_is_distinct_from_command_failure(self):
        destination = self.directory / 'metadata'
        def wrong_identity(*args, **kwargs):
            (destination / 'wrong.nuspec').write_text('<package><metadata><id>Wrong</id><version>3.8.5</version></metadata></package>')
        with patch.object(metadata, 'run', side_effect=wrong_identity):
            result = metadata.stage_project(self.source, self.project, '3.8.5', destination)
        self.assertEqual('nuspec_metadata', result['failure_stage'])
        self.assertEqual('metadata_contract_failed', result['failure_category'])


class HistoricalStudioNpmIntentTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temporary = tempfile.TemporaryDirectory(prefix='historical-studio-intent-')
        cls.addClassCleanup(cls.temporary.cleanup)
        cls.workflow = '.github/maintenance-inert-workflows/packages.yml.source'
        cls.sources, cls.bindings = {}, {}
        paths = (cls.workflow, 'src/hosts/Elsa.Studio.Host.CustomElements/npm/package.json',
                 'src/wrappers/wrappers/react-wrapper/package.json')
        for line in ('3.8', '3.9'):
            commit = metadata.DESCENDANTS[('studio', line)]
            source = Path(cls.temporary.name) / line
            for relative in paths:
                path = source / relative
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(subprocess.check_output(['git', 'show', commit + ':' + relative], cwd=metadata.ROOT))
            cls.sources[line] = source
            cls.bindings[line] = {'product': 'studio', 'line': line, 'commit': commit,
                                 'tree': metadata.git(metadata.ROOT, 'rev-parse', commit + '^{tree}')}

    def test_both_immutable_historical_workflows_plan_the_atomic_same_version_pair(self):
        for line in ('3.8', '3.9'):
            with self.subTest(line=line):
                version = line + '.5'
                intent = planner.npm_intent(self.sources[line], self.bindings[line], version)
                self.assertTrue(intent['atomic'])
                self.assertEqual([{'id': identifier, 'version': version,
                    'expected_tarball': identifier[1:].replace('/', '-') + '-' + version + '.tgz'}
                    for identifier in planner.NPM_IDS], intent['packages'])
                self.assertEqual({planner.NPM_IDS[0]: version}, intent['wrapper_dependency_intent'])
                self.assertFalse(intent['historical_workflow_executed'])

    def test_wrong_line_or_changed_workflow_hash_rejected(self):
        binding = {**self.bindings['3.9'], 'line': '3.8'}
        with self.assertRaisesRegex(ValueError, 'npm_workflow_identity'):
            planner.npm_intent(self.sources['3.9'], binding, '3.8.5')
        path = self.sources['3.9'] / self.workflow
        data = path.read_bytes()
        self.addCleanup(path.write_bytes, data)
        path.write_bytes(data + b'\n# drift\n')
        with self.assertRaisesRegex(ValueError, 'npm_workflow_identity'):
            planner.npm_intent(self.sources['3.9'], self.bindings['3.9'], '3.9.5')

    def test_each_line_rejects_missing_pair_version_or_nonlocal_tarball_intent(self):
        for line in ('3.8', '3.9'):
            path = self.sources[line] / self.workflow
            original = path.read_bytes()
            recipe = planner.STUDIO_NPM_RECIPES[line]
            pair = next(token for token in recipe['tokens'] if b'dependencies.' in token)
            tarball = next(token for token in recipe['tokens'] if b'.tgz' in token)
            for token in (pair, tarball):
                with self.subTest(line=line, removed_intent='pair' if token == pair else 'tarball'):
                    data = original.replace(token, b'# removed required intent')
                    self.assertNotEqual(original, data)
                    path.write_bytes(data)
                    try:
                        # Isolate semantic validation from the independently tested hash guard.
                        with patch.dict(planner.STUDIO_NPM_RECIPES,
                                {line: {**recipe, 'sha256': metadata.sha256(data)}}):
                            with self.assertRaisesRegex(ValueError, 'npm_workflow_identity'):
                                planner.npm_intent(self.sources[line], self.bindings[line], line + '.5')
                    finally:
                        path.write_bytes(original)

    def test_missing_pair_manifest_member_is_rejected(self):
        path = self.sources['3.9'] / 'src/wrappers/wrappers/react-wrapper/package.json'
        original = path.read_bytes()
        self.addCleanup(path.write_bytes, original)
        manifest = json.loads(original)
        del manifest['dependencies'][planner.NPM_IDS[0]]
        path.write_text(json.dumps(manifest))
        with self.assertRaisesRegex(ValueError, 'npm_source_identity'):
            planner.npm_intent(self.sources['3.9'], self.bindings['3.9'], '3.9.5')


class ProductReleaseCliDiagnosticsTests(unittest.TestCase):
    def setUp(self):
        parent = Path(self.enterContext(tempfile.TemporaryDirectory(prefix='plan-diagnostic-contract-')))
        self.output = parent / 'output'
        self.stdout, self.stderr = io.StringIO(), io.StringIO()
        self.enterContext(patch('sys.argv', ['planner', '--product', 'core', '--line', '3.8',
            '--version', '3.8.5', '--output', str(self.output)]))

    def failed_main(self):
        with redirect_stdout(self.stdout), redirect_stderr(self.stderr):
            self.assertEqual(1, planner.main())
        self.assertEqual('', self.stderr.getvalue())
        public = json.loads(self.stdout.getvalue())
        receipt = json.loads((self.output / 'plan.json').read_text())
        self.assertEqual('incomplete', public['status'])
        self.assertEqual('plan_input_or_metadata_unavailable', public['category'])
        self.assertFalse(receipt['eligible'])
        for flag in ('published', 'version_allocated', 'tag_created'):
            self.assertFalse(receipt[flag])
        self.assertEqual(public['diagnostic'], receipt['diagnostic'])
        self.assertNotIn('PRIVATE_SENTINEL', self.stdout.getvalue() + json.dumps(receipt))
        return public['diagnostic']

    def test_bootstrap_failure_identifies_exception_without_exposing_message(self):
        with patch.object(planner, 'build_helper', side_effect=PermissionError('/PRIVATE_SENTINEL/key')):
            diagnostic = self.failed_main()
        self.assertEqual({'phase': 'helper_bootstrap', 'exception_class': 'PermissionError'}, diagnostic)

    def failed_helper_build(self, body):
        def native(command, cwd, **kwargs):
            if command == ['dotnet', '--version']:
                return metadata.SDK
            kwargs['log'].write_bytes(b'["PRIVATE_SENTINEL error CS0999"]\n' + body)
            kwargs['outcome'].update(status='exited', exit_code=1)
            raise ValueError('PRIVATE_SENTINEL env=secret argv=private stderr=private')
        with patch.object(planner, 'run', side_effect=native):
            return self.failed_main()

    def test_failed_helper_build_retains_native_exit_and_codes_without_output(self):
        diagnostic = self.failed_helper_build(b'/PRIVATE_SENTINEL/key: error MSB1008: secret\n' +
            b'error NETSDK1045: PRIVATE_SENTINEL\nerror MSB1008: repeated\n')
        self.assertEqual({'phase': 'helper_build', 'exception_class': 'ValueError',
            'native': {'status': 'exited', 'exit_code': 1, 'codes': ['MSB1008', 'NETSDK1045']}}, diagnostic)

    def test_helper_log_limit_counts_bytes_and_excludes_private_argv(self):
        diagnostic = self.failed_helper_build(('\u20ac' * (planner.NATIVE_DIAGNOSTIC_BYTES // 3 + 1)).encode() +
            b'\nerror CS0001: outside byte limit PRIVATE_SENTINEL')
        self.assertEqual([], diagnostic['native']['codes'])

    def test_semantics_failure_retains_exit_status_without_payload_or_output(self):
        semantics = planner.Semantics(Path('/PRIVATE_SENTINEL/helper.dll'))
        result = subprocess.CompletedProcess(['PRIVATE_SENTINEL'], -9,
            stdout='error CS1001: PRIVATE_SENTINEL', stderr='error NU1301: PRIVATE_SENTINEL')
        with patch.object(planner, 'build_helper', return_value=semantics), \
             patch.object(metadata, 'git', side_effect=lambda root, *args: '' if args[0] == 'status' else 'a' * 40), \
             patch.object(planner.subprocess, 'run', return_value=result):
            diagnostic = self.failed_main()
        self.assertEqual({'phase': 'native_semantics', 'exception_class': 'ValueError',
            'native': {'status': 'exited', 'exit_code': -9, 'codes': ['CS1001', 'NU1301']}}, diagnostic)

    def test_native_start_timeout_and_invalid_json_stay_closed_failures(self):
        failures = (
            (FileNotFoundError('/PRIVATE_SENTINEL/host'), 'FileNotFoundError', 'start-failed', None),
            (subprocess.TimeoutExpired(['PRIVATE_SENTINEL'], 60,
                output=b'PRIVATE_SENTINEL', stderr=b'PRIVATE_SENTINEL'), 'TimeoutExpired', 'timed-out', None),
            (subprocess.CompletedProcess(['PRIVATE_SENTINEL'], 0, 'PRIVATE_SENTINEL invalid JSON', ''),
                'JSONDecodeError', 'exited', 0),
        )
        for failure, exception_class, status, exit_code in failures:
            with self.subTest(exception_class=exception_class):
                self.stdout.seek(0)
                self.stdout.truncate()
                (self.output / 'plan.json').unlink(missing_ok=True)
                if self.output.exists():
                    self.output.rmdir()
                semantics = planner.Semantics(Path('/PRIVATE_SENTINEL/helper.dll'))
                native = {'side_effect': failure} if isinstance(failure, Exception) else {'return_value': failure}
                with patch.object(planner, 'build_helper', return_value=semantics), \
                     patch.object(metadata, 'git', side_effect=lambda root, *args: '' if args[0] == 'status' else 'a' * 40), \
                     patch.object(planner.subprocess, 'run', **native):
                    diagnostic = self.failed_main()
                self.assertEqual({'phase': 'native_semantics', 'exception_class': exception_class,
                    'native': {'status': status, 'exit_code': exit_code, 'codes': []}}, diagnostic)

    def test_semantics_timeout_projects_partial_codes_without_partial_output(self):
        error = subprocess.TimeoutExpired(['PRIVATE_SENTINEL'], 60,
            output=b'error CS1001: PRIVATE_SENTINEL', stderr=b'warning NU1301: PRIVATE_SENTINEL')
        with patch.object(planner, 'build_helper', return_value=planner.Semantics(Path('/PRIVATE_SENTINEL/helper.dll'))), \
             patch.object(metadata, 'git', side_effect=lambda root, *args: '' if args[0] == 'status' else 'a' * 40), \
             patch.object(planner.subprocess, 'run', side_effect=error):
            diagnostic = self.failed_main()
        self.assertEqual({'phase': 'native_semantics', 'exception_class': 'TimeoutExpired',
            'native': {'status': 'timed-out', 'exit_code': None, 'codes': ['CS1001', 'NU1301']}}, diagnostic)

    def test_history_and_prerequisite_preserve_original_native_exception_policy(self):
        def observation(url, body):
            return {'url': url, 'status': 'observed', 'observed_at': planner.now(),
                '_body': body, 'sha256': metadata.sha256(body), 'bytes': len(body)}
        semantics = planner.Semantics(Path('/PRIVATE_SENTINEL/helper.dll'))
        history = observation(planner.history_url('Example'), b'{"versions": ["3.8.4"]}')
        edge = {'id': 'Example', 'version': '3.8.4', 'range': '[3.8.4]', 'framework': 'net8.0'}
        prerequisite = observation(planner.nuspec_url('Example', '3.8.4'), b'<package/>')
        checks = (
            ('history_malformed', lambda: planner.check_history('Example', '3.8.5', '3.8', history, semantics, planner.now())),
            ('prerequisite_malformed', lambda: planner.check_prerequisite(edge, prerequisite, semantics, planner.now())),
        )
        for reason, check in checks:
            for error in (OSError('PRIVATE_SENTINEL'), subprocess.TimeoutExpired(['PRIVATE_SENTINEL'], 60)):
                with self.subTest(reason=reason, exception_class=type(error).__name__), \
                     patch.object(planner.subprocess, 'run', side_effect=error):
                    with self.assertRaises(type(error)) as caught:
                        check()
                    self.assertIs(error, caught.exception)
            for result in (subprocess.CompletedProcess(['PRIVATE_SENTINEL'], 1, '', 'PRIVATE_SENTINEL'),
                           subprocess.CompletedProcess(['PRIVATE_SENTINEL'], 0, 'PRIVATE_SENTINEL', '')):
                with self.subTest(reason=reason, exit_code=result.returncode), \
                     patch.object(planner.subprocess, 'run', return_value=result):
                    self.assertEqual(reason, check()['reason'])

    def test_selected_planning_error_never_emits_an_arbitrary_exception_class_or_message(self):
        class PRIVATE_SENTINEL(ValueError):
            pass
        with patch.object(planner, 'build_helper', return_value=object()), \
             patch.object(planner, 'execute', side_effect=PRIVATE_SENTINEL('PRIVATE_SENTINEL env=secret')):
            diagnostic = self.failed_main()
        self.assertEqual({'phase': 'selected_source_planning', 'exception_class': 'ValueError'}, diagnostic)

    def test_called_process_error_projects_codes_not_command_or_message(self):
        error = subprocess.CalledProcessError(128, ['PRIVATE_SENTINEL'], output=b'error MSB1008: PRIVATE_SENTINEL',
            stderr='error PRIVATE_SENTINEL1000: secret')
        with patch.object(planner, 'build_helper', return_value=object()), \
             patch.object(planner, 'execute', side_effect=error):
            diagnostic = self.failed_main()
        self.assertEqual({'phase': 'selected_source_planning', 'exception_class': 'CalledProcessError',
            'native': {'status': 'exited', 'exit_code': 128, 'codes': ['MSB1008']}}, diagnostic)

    def test_native_code_projection_is_bounded_normalized_and_deduplicated(self):
        text = '\n'.join(f'error NU{value:04}: PRIVATE_SENTINEL' for value in range(1000, 1050))
        text += '\nwarning nu1000: duplicate\nerror CS10001: invalid\nerror MSB1008_private: invalid'
        text += 'x' * planner.NATIVE_DIAGNOSTIC_BYTES + '\nerror CS0001: outside bound'
        result = subprocess.CompletedProcess(['PRIVATE_SENTINEL'], 1, text, 'PRIVATE_SENTINEL')
        with patch.object(planner, 'build_helper', return_value=planner.Semantics(Path('/PRIVATE_SENTINEL/helper.dll'))), \
             patch.object(metadata, 'git', side_effect=lambda root, *args: '' if args[0] == 'status' else 'a' * 40), \
             patch.object(planner.subprocess, 'run', return_value=result):
            diagnostic = self.failed_main()
        self.assertEqual([f'NU{value}' for value in range(1000, 1032)], diagnostic['native']['codes'])

    def test_malformed_native_metadata_cannot_become_public_values(self):
        for status in ('PRIVATE_SENTINEL', True, None):
            for exit_code in (True, 'PRIVATE_SENTINEL', 2 ** 31, -2 ** 31 - 1):
                with self.subTest(status=status, exit_code=exit_code):
                    self.assertEqual({'status': 'unavailable', 'exit_code': None, 'codes': []},
                        planner.native_diagnostic({'status': status, 'exit_code': exit_code},
                            'PRIVATE_SENTINEL\ud800', object()))
        self.assertEqual({'phase': 'unclassified', 'exception_class': 'ValueError'},
            planner.failure_diagnostic(ValueError('PRIVATE_SENTINEL'), 'PRIVATE_SENTINEL'))
        error = ValueError('PRIVATE_SENTINEL')
        error._product_release_diagnostic = {'phase': 'PRIVATE_SENTINEL', 'native': 'PRIVATE_SENTINEL'}
        self.assertEqual({'phase': 'helper_bootstrap', 'exception_class': 'ValueError'},
            planner.failure_diagnostic(error, 'helper_bootstrap'))

    def test_wrong_sdk_identity_is_a_failure_before_build_or_execution(self):
        def wrong_sdk(command, cwd, **kwargs):
            self.assertEqual(['dotnet', '--version'], command)
            kwargs['outcome'].update(status='exited', exit_code=0)
            return '10.0.101 PRIVATE_SENTINEL'
        with patch.object(planner, 'run', side_effect=wrong_sdk), \
             patch.object(planner, 'execute', side_effect=AssertionError('execution forbidden')):
            diagnostic = self.failed_main()
        self.assertEqual({'phase': 'helper_sdk_identity', 'exception_class': 'ValueError',
            'native': {'status': 'exited', 'exit_code': 0, 'codes': []}}, diagnostic)

    def test_successful_ineligible_plan_keeps_existing_success_output(self):
        with patch.object(planner, 'build_helper', return_value=object()), \
             patch.object(planner, 'execute', return_value={'eligible': False}), \
             redirect_stdout(self.stdout), redirect_stderr(self.stderr):
            self.assertEqual(0, planner.main())
        self.assertEqual({'eligible': False, 'published': False}, json.loads(self.stdout.getvalue()))
        self.assertEqual('', self.stderr.getvalue())


class ProductReleaseCliFailureTests(unittest.TestCase):
    def test_git_called_process_error_produces_path_free_incomplete_receipt_without_build(self):
        with tempfile.TemporaryDirectory() as directory:
            parent = Path(directory)
            sentinel = parent / 'sentinel.txt'
            sentinel.write_text('retained')
            output = parent / 'output'
            stdout, stderr = io.StringIO(), io.StringIO()
            failure = subprocess.CalledProcessError(128, ['git', '-C', '/private/source', 'rev-parse'],
                output='/private/output', stderr='/private/stderr')
            with patch('sys.argv', ['planner', '--product', 'core', '--line', '3.8', '--version', '3.8.5',
                                   '--output', str(output)]), patch.object(planner, 'build_helper', return_value=object()), \
                 patch.object(metadata.maintenance, 'git', side_effect=failure), \
                 redirect_stdout(stdout), redirect_stderr(stderr):
                self.assertEqual(1, planner.main())
            self.assertEqual('', stderr.getvalue())
            self.assertNotIn('/private/', stdout.getvalue())
            receipt = json.loads((output / 'plan.json').read_text())
            self.assertEqual('incomplete', receipt['status'])
            self.assertFalse(receipt['eligible'])
            self.assertNotIn('/private/', json.dumps(receipt))
            self.assertEqual('retained', sentinel.read_text())

    def test_failure_receipt_io_is_path_free_and_preserves_sentinel_without_build(self):
        for regular_file_parent in (True, False):
            with self.subTest(regular_file_parent=regular_file_parent), tempfile.TemporaryDirectory() as directory:
                parent = Path(directory) / 'parent'
                if regular_file_parent:
                    parent.write_text('retained')
                    sentinel = parent
                else:
                    parent.mkdir()
                    sentinel = parent / 'sentinel.txt'
                    sentinel.write_text('retained')
                output = parent / 'output'
                stdout, stderr = io.StringIO(), io.StringIO()
                with patch('sys.argv', ['planner', '--product', 'extensions', '--line', '3.8',
                                       '--version', '3.8.5', '--output', str(output)]), \
                     patch.object(planner, 'build_helper', side_effect=OSError(str(parent))), \
                     patch.object(Path, 'write_text', side_effect=PermissionError(str(parent))), \
                     redirect_stdout(stdout), redirect_stderr(stderr):
                    self.assertEqual(1, planner.main())
                self.assertEqual({'status': 'incomplete', 'category': 'plan_input_or_metadata_unavailable',
                                  'published': False, 'diagnostic': {'phase': 'helper_bootstrap',
                                      'exception_class': 'OSError'}}, json.loads(stdout.getvalue()))
                self.assertNotIn(directory, stdout.getvalue())
                self.assertEqual('', stderr.getvalue())
                self.assertEqual('retained', sentinel.read_text())
                self.assertFalse((output / 'plan.json').exists())


if __name__ == '__main__':
    unittest.main()
