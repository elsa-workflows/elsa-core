"""Safety regressions for the disposable import mapping."""
import importlib.util
from pathlib import Path
import unittest
import tempfile
import subprocess
import json
import os
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
            repos, refs, statuses = {}, {}, {}
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
                if name == 'extensions':
                    blob = rehearsal.git(repo, 'hash-object', '-w', '--stdin', data=b'raw path contents').strip()
                    rehearsal.git(repo, 'update-index', '-z', '--index-info',
                                  data=b'100644 ' + blob + b'\tsrc/modules/raw-\xff.cs\0')
                rehearsal.git(repo, '-c', 'user.name=Test',
                              '-c', 'user.email=test@example.invalid',
                              '-c', 'commit.gpgsign=false', 'commit', '--quiet', '-m', 'Initial')
                repos[name] = repo
                refs[name] = rehearsal.git(repo, 'rev-parse', 'HEAD').decode().strip()
                statuses[name] = rehearsal.git(repo, 'status', '--porcelain')
            with patch.dict(rehearsal.PINS, {k: refs[k] for k in ('extensions', 'studio')}):
                for repo in repos.values():
                    for candidate in (repo / 'nested-rehearsal', repo / '.git' / 'nested-rehearsal'):
                        with self.subTest(output=candidate):
                            with self.assertRaisesRegex(ValueError, 'outside every source repository'):
                                rehearsal.rehearse(repos['core'], {k: repos[k] for k in ('extensions', 'studio')},
                                                   candidate)
                            self.assertFalse(candidate.exists())
            output = root / 'output'
            original_git = rehearsal.git
            def fail_fetch(repo, *args, **kwargs):
                if args[0] == 'fetch':
                    raise subprocess.CalledProcessError(1, ['git', 'fetch'])
                return original_git(repo, *args, **kwargs)
            with patch.dict(rehearsal.PINS, {k: refs[k] for k in ('extensions', 'studio')}):
                with patch.object(rehearsal, 'git', side_effect=fail_fetch):
                    with self.assertRaises(subprocess.CalledProcessError):
                        rehearsal.rehearse(repos['core'], {k: repos[k] for k in ('extensions', 'studio')}, output)
                self.assertFalse(output.exists())
                rehearsal.rehearse(repos['core'], {k: repos[k] for k in ('extensions', 'studio')}, output)
            tip = rehearsal.git(output, 'rev-parse', 'rehearsal').strip()
            self.assertEqual(rehearsal.git(output, 'rev-parse', 'HEAD').strip(), tip)
            self.assertEqual(rehearsal.git(output, 'diff', '--name-only', 'HEAD'), b'')
            self.assertEqual(rehearsal.git(output, 'diff', '--cached', '--name-only'), b'')
            self.assertEqual(rehearsal.git(output, 'status', '--porcelain', '-z'),
                             b'?? import-receipt.json\0')
            for name, repo in repos.items():
                self.assertEqual(rehearsal.git(repo, 'rev-parse', 'HEAD').decode().strip(), refs[name])
                self.assertEqual(rehearsal.git(repo, 'status', '--porcelain'), statuses[name])
                rehearsal.git(output, 'merge-base', '--is-ancestor', refs[name], tip.decode())
            self.assertEqual(len(rehearsal.tree(output, 'rehearsal')), 4)
            receipt = json.loads((output / 'import-receipt.json').read_text())
            raw = next(r for r in receipt['mapping'] if 'raw-' in r['source'])
            self.assertEqual(raw['destination'].encode('utf-8', errors='surrogateescape'),
                             b'src/extensions/raw-\xff.cs')
            self.assertIn(b'src/extensions/raw-\xff.cs\0', rehearsal.git(output, 'ls-tree', '-r', '-z', 'rehearsal'))
            if not rehearsal._is_unrepresentable_worktree_path(raw['destination']):
                raw_worktree_path = os.fsencode(output) + b'/src/extensions/raw-\xff.cs'
                with open(raw_worktree_path, 'rb') as materialized:
                    self.assertEqual(materialized.read(), b'raw path contents')
            with patch.dict(rehearsal.PINS, {k: refs[k] for k in ('extensions', 'studio')}):
                with self.assertRaisesRegex(ValueError, 'Output must not exist'):
                    rehearsal.rehearse(repos['core'], {k: repos[k] for k in ('extensions', 'studio')}, output)
            current_output = root / 'current-tip-output'
            with patch.dict(rehearsal.CURRENT_TIP_PINS, {k: refs[k] for k in ('extensions', 'studio')}):
                rehearsal.rehearse(repos['core'], {k: repos[k] for k in ('extensions', 'studio')},
                                   current_output, source_profile='current-tip')
            current_receipt = json.loads((current_output / 'import-receipt.json').read_text())
            self.assertEqual(current_receipt['sourceCommits'], refs)
            self.assertTrue(current_receipt['exactBlobAndModeMapping'])
            with self.assertRaisesRegex(ValueError, 'Unsupported source profile'):
                rehearsal.rehearse(repos['core'], {k: repos[k] for k in ('extensions', 'studio')},
                                   root / 'invalid-profile', source_profile='unreviewed')


class WorktreePathTests(unittest.TestCase):
    def test_surrogateescape_bytes_are_checked_against_host_filesystem(self):
        path = 'raw-\udcff.cs'
        encoded = os.fsencode(path)
        with tempfile.TemporaryDirectory() as temporary_directory:
            candidate = os.fsencode(temporary_directory) + b'/' + encoded
            try:
                descriptor = os.open(candidate, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
                os.close(descriptor)
            except OSError:
                expected = True
            else:
                os.unlink(candidate)
                expected = False
        self.assertEqual(rehearsal._is_unrepresentable_worktree_path(path), expected)

    def test_non_surrogateescape_surrogate_is_unrepresentable(self):
        self.assertTrue(rehearsal._is_unrepresentable_worktree_path('raw-\ud800.cs'))


if __name__ == '__main__':
    unittest.main()
