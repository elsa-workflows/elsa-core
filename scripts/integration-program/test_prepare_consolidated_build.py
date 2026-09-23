"""Protect source history, unrelated work, and repeatable solution generation."""
import contextlib
import io
import json
from pathlib import Path
import tempfile
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
        self.root = Path(self.temp.name)
        repos, refs = {}, {}
        for name, files in {
            'core': {'Elsa.sln': SOLUTION, '.gitignore': 'ignored/\n'},
            'extensions': {'src/modules/Module/Module.csproj': '<Project />\n'},
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

    def test_applies_once_preserves_history_and_reports_unverified_build(self):
        before = build.rehearsal.git(self.output, 'rev-parse', 'HEAD')
        with contextlib.redirect_stdout(io.StringIO()):
            build.prepare(self.output)
        receipt = json.loads((self.output / 'consolidated-build-receipt.json').read_text())
        self.assertFalse(receipt['buildCompatibilityVerified'])
        self.assertFalse(receipt['publicationAuthorized'])
        self.assertEqual(receipt['importedProjects'], 2)
        self.assertEqual(build.rehearsal.git(self.output, 'rev-parse', 'HEAD'), before)
        self.assertIn('Module.csproj', (self.output / 'Consolidated.sln').read_text())
        with self.assertRaises(ValueError):
            build.prepare(self.output)

    def test_current_core_profile_preserves_actual_pins_and_parent_checks(self):
        actual_core = build.SOURCE_COMMITS['core']
        with patch.dict(build.SOURCE_COMMITS, {'core': 'f' * 40}), \
                patch.object(build, 'CURRENT_CORE_COMMIT', actual_core), \
                contextlib.redirect_stdout(io.StringIO()):
            build.prepare(self.output)
        receipt = json.loads((self.output / 'consolidated-build-receipt.json').read_text())
        self.assertEqual(receipt['sourceCommits']['core'], actual_core)
        self.assertFalse(receipt['buildCompatibilityVerified'])

    def test_rejects_dirty_tracked_source_without_overwriting(self):
        path = self.output / 'src/studio/UI/UI.csproj'
        path.write_text('user edit')
        with self.assertRaisesRegex(ValueError, 'changes'):
            build.prepare(self.output)
        self.assertEqual(path.read_text(), 'user edit')
        self.assertFalse((self.output / 'Consolidated.sln').exists())

    def test_rejects_even_ignored_ambient_files(self):
        path = self.output / 'ignored/Directory.Build.targets'
        path.parent.mkdir()
        path.write_text('ambient input')
        with self.assertRaisesRegex(ValueError, 'untracked or ignored'):
            build.prepare(self.output)
        self.assertEqual(path.read_text(), 'ambient input')

    def test_rejects_symlink_root_before_modifying_target(self):
        link = self.root / 'linked-rehearsal'
        link.symlink_to(self.output, target_is_directory=True)
        with self.assertRaisesRegex(ValueError, 'real disposable'):
            build.prepare(link)
        self.assertFalse((self.output / 'Consolidated.sln').exists())
        self.assertFalse((self.output / build.ADDED_TEST).exists())

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


if __name__ == '__main__':
    unittest.main()
