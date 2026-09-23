"""Safety regressions for the disposable import mapping."""
import importlib.util
from pathlib import Path
import unittest
import tempfile
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('rehearsal', Path(__file__).with_name('rehearse-import.py'))
rehearsal = importlib.util.module_from_spec(spec)
spec.loader.exec_module(rehearsal)
BLOB = ('100644', 'a' * 40)


class ImportMappingTests(unittest.TestCase):
    def test_import_never_activates_upstream_workflows_or_root_build_inputs(self):
        for product in rehearsal.RULES:
            for path in ('.github/workflows/packages.yml', 'Directory.Build.props',
                         'Directory.Packages.props', 'build/Build.cs', 'AGENTS.md'):
                target = rehearsal.destination(product, path)
                self.assertTrue(target.startswith('doc/integration-program/legacy/'))
                self.assertTrue(target.endswith('.source'))

    def test_competing_packages_are_retained_but_cannot_be_built_as_projects(self):
        for path in rehearsal.DUPLICATES:
            target = rehearsal.destination('extensions', path + '/package.csproj')
            self.assertTrue(target.endswith('.csproj.source'))

    def test_existing_core_file_cannot_be_overwritten(self):
        with self.assertRaisesRegex(ValueError, 'Destination collision'):
            rehearsal.relocation_plan({'src/extensions/Elsa.Slack/a.cs': BLOB},
                                      {'extensions': {'src/modules/Elsa.Slack/a.cs': BLOB}})

    def test_file_directory_collision_is_rejected(self):
        with self.assertRaisesRegex(ValueError, 'File/directory collision'):
            rehearsal.relocation_plan({'src/studio': BLOB},
                                      {'studio': {'src/modules/a.cs': BLOB}})

    def test_two_source_paths_cannot_collapse(self):
        with self.assertRaisesRegex(ValueError, 'Destination collision'):
            rehearsal.relocation_plan({}, {'studio': {'doc/docs/a.md': BLOB, 'docs/a.md': BLOB}})

    def test_every_file_retains_its_blob_and_mode(self):
        core = {'src/core.cs': BLOB}
        files = {'src/modules/a.cs': BLOB, 'LICENSE': ('100755', 'b' * 40)}
        result, receipt = rehearsal.relocation_plan(core, {'extensions': files})
        self.assertEqual(len(result), len(core) + len(files))
        self.assertEqual({r['source'] for r in receipt}, set(files))
        for row in receipt:
            self.assertEqual(result[row['destination']], files[row['source']])

    def test_path_escape_is_rejected(self):
        for path in ('/tmp/a.cs', '../a.cs', 'src/../a.cs'):
            with self.assertRaises(ValueError):
                rehearsal.destination('studio', path)


class FullHistoryTests(unittest.TestCase):
    def test_rehearsal_retains_ancestors_without_modifying_inputs(self):
        with tempfile.TemporaryDirectory() as root:
            root = Path(root)
            repos, refs = {}, {}
            for name, path in [('core', 'src/core.cs'),
                               ('extensions', 'src/modules/Module/a.cs'),
                               ('studio', 'src/modules/UI/a.razor')]:
                repo = root / name
                repo.mkdir()
                rehearsal.git(repo, 'init', '--quiet')
                source = repo / path
                source.parent.mkdir(parents=True)
                source.write_text(name)
                rehearsal.git(repo, 'add', '.')
                rehearsal.git(repo, '-c', 'user.name=Test',
                              '-c', 'user.email=test@example.invalid',
                              '-c', 'commit.gpgsign=false', 'commit', '--quiet', '-m', 'Initial')
                repos[name] = repo
                refs[name] = rehearsal.git(repo, 'rev-parse', 'HEAD').decode().strip()
            output = root / 'output'
            with patch.dict(rehearsal.PINS, {k: refs[k] for k in ('extensions', 'studio')}):
                rehearsal.rehearse(repos['core'], {k: repos[k] for k in ('extensions', 'studio')}, output)
            tip = rehearsal.git(output, 'rev-parse', 'rehearsal').strip()
            for name, repo in repos.items():
                self.assertEqual(rehearsal.git(repo, 'rev-parse', 'HEAD').decode().strip(), refs[name])
                self.assertEqual(rehearsal.git(repo, 'status', '--porcelain'), b'')
                rehearsal.git(output, 'merge-base', '--is-ancestor', refs[name], tip.decode())
            self.assertEqual(len(rehearsal.tree(output, 'rehearsal')), 3)
            with patch.dict(rehearsal.PINS, {k: refs[k] for k in ('extensions', 'studio')}):
                with self.assertRaisesRegex(ValueError, 'Output must not exist'):
                    rehearsal.rehearse(repos['core'], {k: repos[k] for k in ('extensions', 'studio')}, output)


if __name__ == '__main__':
    unittest.main()
