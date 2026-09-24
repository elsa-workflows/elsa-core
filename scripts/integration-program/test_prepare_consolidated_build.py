"""Protect source history, unrelated work, and repeatable solution generation."""
import contextlib
import gzip
import hashlib
import io
import json
import os
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

import prepare_consolidated_build as build

SOURCE_INTEGRATION_PATCH = build.PATCH

SOLUTION = '''Microsoft Visual Studio Solution File, Format Version 12.00
Global
\tGlobalSection(SolutionConfigurationPlatforms) = preSolution
\t\tDebug|Any CPU = Debug|Any CPU
\t\tRelease|x64 = Release|x64
\tEndGlobalSection
\tGlobalSection(ProjectConfigurationPlatforms) = postSolution
\tEndGlobalSection
EndGlobal
'''


class ConsolidatedPreparationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        # Git can spawn auto-maintenance after fixture commits. Keep the
        # temporary object stores quiescent before unittest removes them.
        self.enterContext(patch.dict(os.environ, {
            'GIT_CONFIG_COUNT': '2',
            'GIT_CONFIG_KEY_0': 'maintenance.auto',
            'GIT_CONFIG_VALUE_0': 'false',
            'GIT_CONFIG_KEY_1': 'gc.auto',
            'GIT_CONFIG_VALUE_1': '0',
        }))
        self.root = Path(self.temp.name)
        repos, refs = {}, {}
        for name, files in {
            'core': {
                'Elsa.sln': SOLUTION,
                '.gitignore': 'ignored/\n',
            },
            'extensions': {
                'src/modules/Module/Module.csproj': '<Project />\n',
                'test/workbench/Elsa.TestServer.Web/Elsa.TestServer.Web.csproj': '<Project />\n',
            },
            'studio': {'src/UI/UI.csproj': '<Project />\n'},
        }.items():
            repo = self.root / name
            repo.mkdir()
            build.rehearsal.git(repo, 'init', '--quiet')
            for relative, content in files.items():
                path = repo / relative
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(content)
            build.rehearsal.git(repo, 'add', '.')
            build.rehearsal.git(repo, '-c', 'user.name=Test', '-c', 'user.email=test@example.invalid',
                                '-c', 'commit.gpgsign=false', 'commit', '--quiet', '-m', 'Fixture')
            repos[name] = repo
            refs[name] = build.rehearsal.git(repo, 'rev-parse', 'HEAD').decode().strip()
        self.enterContext(patch.dict(build.SOURCE_COMMITS, refs, clear=True))
        self.enterContext(patch.dict(build.rehearsal.PINS, {k: refs[k] for k in ('extensions', 'studio')}, clear=True))
        self.output = self.root / 'rehearsal'
        with contextlib.redirect_stdout(io.StringIO()):
            build.rehearsal.rehearse(repos['core'], {k: repos[k] for k in ('extensions', 'studio')}, self.output)
        build.rehearsal.git(self.output, 'checkout', '--quiet', 'rehearsal')
        build.rehearsal.git(self.output, 'restore', '--source=HEAD', '--worktree', '.')
        self.fixture_patch = self.root / 'fixture.patch'
        self.fixture_patch.write_text(f'''diff --git a/{build.ADDED_TEST} b/{build.ADDED_TEST}
new file mode 100644
--- /dev/null
+++ b/{build.ADDED_TEST}
@@ -0,0 +1 @@
+<Project />
        ''')
        self.enterContext(patch.object(build, 'PATCH', self.fixture_patch))

    def prepare_with_fake_packability(self):
        with patch.object(build, 'evaluate_packability', return_value={
                'sdkVersion': '10.0.300', 'projectCount': 4, 'evaluationCount': 24,
                'configurations': ['Debug', 'Release'], 'referenceModes': build.REFERENCE_MODES,
                'allProjectsNonPackable': True, 'allProjectsDisablePackageOnBuild': True,
                'projects': [],
        }):
            build.prepare(self.output)

    def test_applies_once_preserves_history_and_reports_unverified_build(self):
        before = build.rehearsal.git(self.output, 'rev-parse', 'HEAD')
        with contextlib.redirect_stdout(io.StringIO()):
            self.prepare_with_fake_packability()
        receipt = json.loads((self.output / 'consolidated-build-receipt.json').read_text())
        self.assertEqual(receipt['canonicalSolution'], 'Elsa.sln')
        self.assertFalse(receipt['buildCompatibilityVerified'])
        self.assertFalse(receipt['canonicalBuildAndTestsVerified'])
        self.assertFalse(receipt['publicationAuthorized'])
        self.assertEqual(receipt['importedProjects'], 3)
        self.assertEqual(build.rehearsal.git(self.output, 'rev-parse', 'HEAD'), before)
        solution = (self.output / 'Elsa.sln').read_text()
        self.assertIn('Module.csproj', solution)
        self.assertIn('Elsa.TestServer.Web.csproj', solution)
        self.assertIn('Elsa.Studio.Agents.Tests.csproj', solution)
        self.assertFalse((self.output / 'Consolidated.sln').exists())
        self.assertEqual(receipt['nukeTestDiscovery']['projects'], 2)
        self.assertEqual(len(receipt['nukeTestDiscovery']['selectedByNameSuffix']), 1)
        self.assertEqual(receipt['nukeTestDiscovery']['nonTestHostProjects'], [
            'test/extensions/workbench/Elsa.TestServer.Web/Elsa.TestServer.Web.csproj'
        ])
        packability_path = self.output / 'canonical-packability-report.json'
        packability_entry = next(row for row in receipt['files'] if row['path'] == packability_path.name)
        self.assertEqual(packability_entry['sha256'], build.sha256(packability_path.read_bytes()))
        with self.assertRaises(ValueError):
            build.prepare(self.output)

    def test_current_core_profile_preserves_actual_pins_and_parent_checks(self):
        actual_core = build.SOURCE_COMMITS['core']
        with patch.dict(build.SOURCE_COMMITS, {'core': 'f' * 40}), \
                patch.object(build, 'SUPPORTED_CORE_PROFILE_COMMITS', (actual_core,)), \
                contextlib.redirect_stdout(io.StringIO()):
            self.prepare_with_fake_packability()
        receipt = json.loads((self.output / 'consolidated-build-receipt.json').read_text())
        self.assertEqual(receipt['sourceCommits']['core'], actual_core)
        self.assertFalse(receipt['buildCompatibilityVerified'])
        self.assertFalse(receipt['canonicalBuildAndTestsVerified'])

    def test_current_core_candidate_preserves_prior_profiles_and_refreshes_source_pins(self):
        expected_core_profiles = (
            '076f022cc174d497af26fc8e26414970e61a79b1',
            '95a658b96107ad4dbb280a13972479af74bc6a30',
        )
        self.assertEqual(build.SUPPORTED_CORE_PROFILE_COMMITS, expected_core_profiles)
        ledger_path = build.HERE.parent.parent / 'doc/integration-program/consolidation/canonical-source-profile-ledger.json'
        ledger = json.loads(ledger_path.read_text())
        self.assertEqual([row['sourceCommits']['core'] for row in ledger['supportedRehearsalProfiles']], [
            '8e893e02c4ac089d526b0a0d294a8546f021d072', *expected_core_profiles,
        ])
        upstream = ledger['upstreamMainAtVerification']
        candidate = ledger['supportedRehearsalProfiles'][-1]['sourceCommits']
        self.assertEqual(upstream['core']['commit'], '77f3ca92eb3e514af959404f67ea538b07cba98f')
        self.assertEqual(candidate['core'], expected_core_profiles[-1])
        self.assertNotEqual(candidate['core'], upstream['core']['commit'])
        self.assertEqual(candidate['extensions'], upstream['extensions']['commit'])
        self.assertEqual(candidate['studio'], upstream['studio']['commit'])
        raw_rehearsal = ledger['raw95aRehearsalEvidence']
        self.assertEqual(raw_rehearsal['rehearsalCommit'], '8fe2bdbdedacd06c99d83b72764fdca9f325bc0c')
        self.assertEqual(raw_rehearsal['totalExactBlobsAndModes'], 9993)
        self.assertTrue(raw_rehearsal['rootIndependentVerification'])
        patch_review = ledger['core076To95a658BuildInputComparison']['sourceIntegrationPatchPathReview']
        self.assertEqual(patch_review['sha256'], hashlib.sha256(SOURCE_INTEGRATION_PATCH.read_bytes()).hexdigest())

    def test_current_tip_profile_is_additive_and_prunes_only_obsolete_blazored_refs(self):
        current = {
            'core': '1855a2ef2719d536a66181dec604e781bfdd42a9',
            'extensions': 'ba8b71d91c15ffe5be4b2c539cf9f712e74af775',
            'studio': '20ceaeeed7e671f0c9662003e82063026f2216de',
        }
        profiles = build.supported_source_profiles()
        self.assertEqual(profiles[-1], current)
        self.assertEqual(profiles[0], build.SOURCE_COMMITS)
        self.assertEqual(len(profiles), 4)

        agent = self.root / 'src/extensions/agents/Elsa.Studio.Agents/Elsa.Studio.Agents.csproj'
        contexts = self.root / 'src/extensions/workflows/Elsa.Studio.WorkflowContexts/Elsa.Studio.WorkflowContexts.csproj'
        secrets = self.root / 'doc/integration-program/legacy/extensions/src/modules/secrets/Elsa.Studio.Secrets/Elsa.Studio.Secrets.csproj.source'
        for path in (agent, contexts, secrets):
            path.parent.mkdir(parents=True)
            path.write_text('<Project>\n\n  <ItemGroup>\n    <PackageReference Include="Blazored.FluentValidation"/>\n  </ItemGroup>\n\n</Project>\n')
        agent.write_bytes(agent.read_bytes().replace(b'\n', b'\r\n'))

        build.remove_unused_blazored_references(self.root, current)

        for path in (agent, contexts):
            self.assertNotIn('Blazored.FluentValidation', path.read_text())
        self.assertNotIn(b'\n', agent.read_bytes().replace(b'\r\n', b''))
        self.assertIn('Blazored.FluentValidation', secrets.read_text())

    def test_current_tip_rehearsal_receipt_prepares_and_applies_patch(self):
        repos = {name: self.root / name for name in ('core', 'extensions', 'studio')}
        extension_files = (
            'src/modules/agents/Elsa.Studio.Agents/Elsa.Studio.Agents.csproj',
            'src/modules/workflows/Elsa.Studio.WorkflowContexts/Elsa.Studio.WorkflowContexts.csproj',
        )
        for relative in extension_files:
            path = repos['extensions'] / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text('<Project>\n\n  <ItemGroup>\n'
                            '    <PackageReference Include="Blazored.FluentValidation"/>\n'
                            '  </ItemGroup>\n\n</Project>\n')
        build.rehearsal.git(repos['extensions'], 'add', '.')
        build.rehearsal.git(repos['extensions'], '-c', 'user.name=Test',
                            '-c', 'user.email=test@example.invalid', '-c', 'commit.gpgsign=false',
                            'commit', '--quiet', '-m', 'Current tip fixture')
        refs = {name: build.rehearsal.git(repo, 'rev-parse', 'HEAD').decode().strip()
                for name, repo in repos.items()}
        output = self.root / 'current-tip-rehearsal'
        with patch.dict(build.CURRENT_TIP_SOURCE_COMMITS, refs, clear=True), \
                patch.dict(build.rehearsal.CURRENT_TIP_PINS,
                           {name: refs[name] for name in ('extensions', 'studio')}, clear=True):
            with contextlib.redirect_stdout(io.StringIO()):
                build.rehearsal.rehearse(repos['core'],
                                          {name: repos[name] for name in ('extensions', 'studio')},
                                          output, source_profile='current-tip')
            build.rehearsal.git(output, 'checkout', '--quiet', 'rehearsal')
            build.rehearsal.git(output, 'restore', '--source=HEAD', '--worktree', '.')
            with patch.object(build, 'evaluate_packability', return_value={
                    'sdkVersion': '10.0.300', 'projectCount': 6, 'evaluationCount': 36,
                    'configurations': ['Debug', 'Release'], 'referenceModes': build.REFERENCE_MODES,
                    'allProjectsNonPackable': True, 'allProjectsDisablePackageOnBuild': True,
                    'projects': [],
            }), contextlib.redirect_stdout(io.StringIO()):
                build.prepare(output)
        receipt = json.loads((output / 'consolidated-build-receipt.json').read_text())
        self.assertEqual(receipt['sourceCommits'], refs)
        self.assertTrue((output / build.ADDED_TEST).is_file())
        for relative in extension_files:
            mapped = relative.replace('src/modules/', 'src/extensions/', 1)
            self.assertNotIn('Blazored.FluentValidation', (output / mapped).read_text())

    def test_old_profiles_leave_blazored_references_unchanged(self):
        path = self.root / 'src/extensions/agents/Elsa.Studio.Agents/Elsa.Studio.Agents.csproj'
        path.parent.mkdir(parents=True)
        original = '<Project><PackageReference Include="Blazored.FluentValidation"/></Project>\n'
        path.write_text(original)

        build.remove_unused_blazored_references(self.root, build.SOURCE_COMMITS)

        self.assertEqual(path.read_text(), original)

    def test_rejects_dirty_tracked_source_without_overwriting(self):
        original_solution = (self.output / 'Elsa.sln').read_text()
        path = self.output / 'src/studio/UI/UI.csproj'
        path.write_text('user edit')
        with self.assertRaisesRegex(ValueError, 'changes'):
            build.prepare(self.output)
        self.assertEqual(path.read_text(), 'user edit')
        self.assertEqual((self.output / 'Elsa.sln').read_text(), original_solution)

    def test_rejects_even_ignored_ambient_files(self):
        path = self.output / 'ignored/Directory.Build.targets'
        path.parent.mkdir()
        path.write_text('ambient input')
        with self.assertRaisesRegex(ValueError, 'untracked or ignored'):
            build.prepare(self.output)
        self.assertEqual(path.read_text(), 'ambient input')

    def test_rejects_symlink_root_before_modifying_target(self):
        original_solution = (self.output / 'Elsa.sln').read_text()
        link = self.root / 'linked-rehearsal'
        link.symlink_to(self.output, target_is_directory=True)
        with self.assertRaisesRegex(ValueError, 'real disposable'):
            build.prepare(link)
        self.assertEqual((self.output / 'Elsa.sln').read_text(), original_solution)
        self.assertFalse((self.output / build.ADDED_TEST).exists())

    def test_packability_failure_leaves_original_rehearsal_untouched(self):
        original_solution = (self.output / 'Elsa.sln').read_bytes()
        original_status = build.rehearsal.git(self.output, 'status', '--porcelain', '-z')
        with patch.object(build, 'evaluate_packability', side_effect=ValueError('matrix failure')):
            with self.assertRaisesRegex(ValueError, 'matrix failure'):
                build.prepare(self.output)
        self.assertEqual((self.output / 'Elsa.sln').read_bytes(), original_solution)
        self.assertEqual(build.rehearsal.git(self.output, 'status', '--porcelain', '-z'), original_status)
        self.assertFalse((self.output / build.ADDED_TEST).exists())
        self.assertFalse((self.output / 'canonical-packability-report.json').exists())
        self.assertFalse((self.output / 'consolidated-build-receipt.json').exists())

    def test_concurrent_source_edit_during_evaluation_blocks_copyback(self):
        original_solution = (self.output / 'Elsa.sln').read_bytes()
        concurrent_path = self.output / 'src/studio/UI/UI.csproj'

        def edit_source_during_matrix(root, projects):
            concurrent_path.write_text('concurrent user edit\n')
            return {
                'sdkVersion': '10.0.300', 'projectCount': 4, 'evaluationCount': 24,
                'configurations': ['Debug', 'Release'], 'referenceModes': build.REFERENCE_MODES,
                'allProjectsNonPackable': True, 'allProjectsDisablePackageOnBuild': True,
                'projects': [],
            }

        output = io.StringIO()
        with patch.object(build, 'evaluate_packability', side_effect=edit_source_during_matrix):
            with contextlib.redirect_stdout(output):
                with self.assertRaisesRegex(ValueError, 'changes'):
                    build.prepare(self.output)
        self.assertEqual(output.getvalue(), '')
        self.assertEqual(concurrent_path.read_text(), 'concurrent user edit\n')
        self.assertEqual((self.output / 'Elsa.sln').read_bytes(), original_solution)
        self.assertFalse((self.output / build.ADDED_TEST).exists())
        self.assertFalse((self.output / 'canonical-packability-report.json').exists())
        self.assertFalse((self.output / 'consolidated-build-receipt.json').exists())

    def test_rejects_remote(self):
        build.rehearsal.git(self.output, 'remote', 'add', 'origin', 'https://example.invalid/repo')
        with self.assertRaisesRegex(ValueError, 'no remotes'):
            build.prepare(self.output)

    def test_rejects_forged_receipt(self):
        path = self.output / 'import-receipt.json'
        receipt = json.loads(path.read_text())
        receipt['mapping'][0]['blob'] = '0' * 40
        path.write_text(json.dumps(receipt))
        with self.assertRaisesRegex(ValueError, 'Receipt mapping'):
            build.prepare(self.output)

    def test_rejects_unreviewed_source_pin(self):
        path = self.output / 'import-receipt.json'
        receipt = json.loads(path.read_text())
        receipt['sourceCommits']['core'] = '0' * 40
        path.write_text(json.dumps(receipt))
        with self.assertRaisesRegex(ValueError, 'Unsupported source pins'):
            build.prepare(self.output)

    def test_solution_is_stable_and_rejects_path_escape(self):
        paths = ['src/studio/UI/UI.csproj', 'src/extensions/Module/Module.csproj']
        self.assertEqual(build.solution_with_projects(SOLUTION, paths), build.solution_with_projects(SOLUTION, paths[::-1]))
        with self.assertRaisesRegex(ValueError, 'Invalid project path'):
            build.solution_with_projects(SOLUTION, ['../outside.csproj'])

    def test_packability_matrix_covers_configurations_frameworks_and_reference_modes(self):
        root = self.root / 'matrix'
        project_path = root / 'src/extensions/Example/Example.csproj'
        project_path.parent.mkdir(parents=True)
        project_path.write_text('<Project />\n')
        calls = []

        def evaluator(path, configuration, target_framework, use_project_references):
            calls.append((configuration, target_framework, use_project_references))
            return {
                'TargetFramework': target_framework,
                'TargetFrameworks': '' if target_framework else 'net8.0;net9.0',
                'PackageId': 'Example.Package',
                'IsPackable': 'false',
                'GeneratePackageOnBuild': 'false',
                'UseProjectReferences': use_project_references or 'true',
            }

        report = build.evaluate_packability(root, ['src/extensions/Example/Example.csproj'], evaluator=evaluator)
        self.assertEqual(report['projectCount'], 1)
        self.assertEqual(report['evaluationCount'], 12)
        self.assertEqual({row['configuration'] for row in report['projects'][0]['evaluations']}, {'Debug', 'Release'})
        self.assertEqual({row['targetFramework'] for row in report['projects'][0]['evaluations']}, {'net8.0', 'net9.0'})
        self.assertEqual({row['referenceMode'] for row in report['projects'][0]['evaluations']}, {'default', 'source', 'package'})
        self.assertTrue(all(row['properties']['PackageId'] == 'Example.Package' for row in report['projects'][0]['evaluations']))
        self.assertEqual(len(calls), 18)  # 6 outer discovery + 12 inner evaluations.

    def test_packability_matrix_rejects_any_packable_framework_configuration(self):
        root = self.root / 'matrix'
        project_path = root / 'src/extensions/Example/Example.csproj'
        project_path.parent.mkdir(parents=True)
        project_path.write_text('<Project />\n')

        def evaluator(path, configuration, target_framework, use_project_references):
            is_packable = 'true' if (configuration, target_framework, use_project_references) == ('Release', 'net9.0', 'false') else 'false'
            return {
                'TargetFramework': target_framework,
                'TargetFrameworks': '' if target_framework else 'net8.0;net9.0',
                'PackageId': 'Example.Package',
                'IsPackable': is_packable,
                'GeneratePackageOnBuild': 'false',
                'UseProjectReferences': use_project_references or 'true',
            }

        with self.assertRaisesRegex(ValueError, r'Example.csproj \(Release, package, net9.0\): IsPackable'):
            build.evaluate_packability(root, ['src/extensions/Example/Example.csproj'], evaluator=evaluator)

    def test_packability_matrix_rejects_missing_or_conditionally_changed_package_id(self):
        root = self.root / 'matrix'
        project_path = root / 'src/extensions/Example/Example.csproj'
        project_path.parent.mkdir(parents=True)
        project_path.write_text('<Project />\n')

        def evaluator(path, configuration, target_framework, use_project_references):
            package_id = 'Example.Package'
            if configuration == 'Release' and target_framework == 'net9.0' and use_project_references == 'false':
                package_id = 'Example.Package.PackageMode'
            return {
                'TargetFramework': target_framework,
                'TargetFrameworks': '' if target_framework else 'net8.0;net9.0',
                'PackageId': package_id,
                'IsPackable': 'false',
                'GeneratePackageOnBuild': 'false',
                'UseProjectReferences': use_project_references or 'true',
            }

        with self.assertRaisesRegex(ValueError, 'effective PackageId is missing or varies'):
            build.evaluate_packability(root, ['src/extensions/Example/Example.csproj'], evaluator=evaluator)

    def test_retained_packability_evidence_is_pinned_and_property_only(self):
        path = build.HERE.parent.parent / 'doc/integration-program/consolidation/canonical-packability-076-evidence.json.gz'
        compressed = path.read_bytes()
        self.assertEqual(hashlib.sha256(compressed).hexdigest(), '640afa1a61b472fb2f054312168a11c6ba85595a312441976785dd04ac7537a7')
        evidence = json.loads(gzip.decompress(compressed))
        self.assertEqual(evidence['sourceCommits'], {
            'core': '076f022cc174d497af26fc8e26414970e61a79b1',
            'extensions': '33fa0bfd28c7585240e3d4f665058c067b17e287',
            'studio': '9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822',
        })
        self.assertTrue(evidence['workspaceStatusUnchanged'])
        self.assertFalse(evidence['canonicalBuildAndTestsVerified'])
        matrix = evidence['propertyMatrix']
        self.assertEqual((matrix['projectCount'], matrix['evaluationCount']), (167, 2586))
        self.assertTrue(matrix['allProjectsNonPackable'])
        self.assertTrue(matrix['allProjectsDisablePackageOnBuild'])
        for project in matrix['projects']:
            evaluations = project['evaluations']
            self.assertEqual(len({row['properties']['PackageId'] for row in evaluations}), 1)
            self.assertTrue(all(row['properties']['PackageId'] for row in evaluations))
            self.assertTrue(all(row['properties']['IsPackable'] == 'false' for row in evaluations))
            self.assertTrue(all(row['properties']['GeneratePackageOnBuild'] == 'false' for row in evaluations))

    def test_msbuild_evaluation_uses_project_directory_for_sdk_selection(self):
        project = self.root / 'rehearsal' / 'src' / 'Example' / 'Example.csproj'
        project.parent.mkdir(parents=True)
        project.write_text('<Project />\n')
        payload = {'Properties': {name: '' for name in build.PACK_PROPERTIES}}
        completed = SimpleNamespace(returncode=0, stdout=json.dumps(payload), stderr='')
        with patch.object(build.subprocess, 'run', return_value=completed) as run:
            build.msbuild_properties(project, 'Debug', 'net10.0', 'true')
        self.assertEqual(run.call_args.kwargs['cwd'], project.parent.resolve())

    def test_sdk_version_uses_rehearsal_root_for_sdk_selection(self):
        completed = SimpleNamespace(stdout='10.0.300\n')
        with patch.object(build.subprocess, 'run', return_value=completed) as run:
            self.assertEqual(build.sdk_version(self.root), '10.0.300')
        self.assertEqual(run.call_args.kwargs['cwd'], self.root.resolve())
    def test_slack_preparation_preserves_package_and_project_reference_modes(self):
        patch = SOURCE_INTEGRATION_PATCH.read_text()
        marker = 'diff --git a/src/extensions/communication/Elsa.Slack/Elsa.Slack.csproj '
        slack_diff = patch.split(marker, 1)[1].split('\ndiff --git ', 1)[0]
        package_mode = slack_diff.split("Condition=\"'$(UseProjectReferences)' != 'true'\">", 1)[1].split('</ItemGroup>', 1)[0]
        project_mode = slack_diff.split("Condition=\"'$(UseProjectReferences)' == 'true'\">", 1)[1].split('</ItemGroup>', 1)[0]

        self.assertIn('<PackageReference Include="Elsa" />', package_mode)
        self.assertNotIn('ProjectReference', package_mode)
        self.assertIn('+        <ProjectReference Include="../../../modules/Elsa/Elsa.csproj" />', project_mode)
        self.assertNotIn('PackageReference', project_mode)

    def test_extensions_package_icon_remains_linked_from_the_canonical_root_file(self):
        patch = SOURCE_INTEGRATION_PATCH.read_text()
        marker = 'diff --git a/src/extensions/Directory.Build.props '
        extensions_props_diff = patch.split(marker, 1)[1].split('\ndiff --git ', 1)[0]

        self.assertIn('+    <None Include="..\\..\\..\\..\\icon.png" Pack="true" PackagePath="\\" />', extensions_props_diff)


if __name__ == '__main__':
    unittest.main()
