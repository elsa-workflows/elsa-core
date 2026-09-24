"""Protect source history, unrelated work, and repeatable solution generation."""
import contextlib
import gzip
import hashlib
import io
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

import prepare_consolidated_build as build

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
                patch.object(build, 'CURRENT_CORE_COMMIT', actual_core), \
                contextlib.redirect_stdout(io.StringIO()):
            self.prepare_with_fake_packability()
        receipt = json.loads((self.output / 'consolidated-build-receipt.json').read_text())
        self.assertEqual(receipt['sourceCommits']['core'], actual_core)
        self.assertFalse(receipt['buildCompatibilityVerified'])
        self.assertFalse(receipt['canonicalBuildAndTestsVerified'])

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


if __name__ == '__main__':
    unittest.main()
