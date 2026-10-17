from __future__ import annotations

import hashlib
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch

import selected_consumer_cache_retirement as retirement


class ConsumerCacheRetirementTests(unittest.TestCase):
    def setUp(self):
        """Create owned consumer caches, successful proof data, and unrelated retained evidence."""
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.private = self.root / 'private'
        self.result = {'id': 'Example', 'framework': 'net9.0', 'success': True,
                       'restored_payloads': [{'path': 'lib/net9.0/Example.dll', 'sha256': 'a'*64}]}
        self.cell = self.private / hashlib.sha256(b'Example/net9.0').hexdigest()[:16]
        self.caches = [self.cell / 'packages', self.cell / 'discovery/packages']
        for cache in self.caches:
            cache.mkdir(parents=True)
            (cache / 'archive.nupkg').write_bytes(b'only ephemeral extracted cache')
        for relative in ('Consumer.csproj', 'NuGet.Config', 'packages.lock.json', 'restore.log', 'build.log',
                         'obj/project.assets.json', 'bin/Release/net9.0/Consumer.dll', 'home/state', 'http-cache/state',
                         'discovery/obj/project.assets.json', 'discovery/packages.lock.json', 'discovery/restore.log'):
            path = self.cell / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(relative.encode())
        self.outside = self.root / 'producer-evidence'
        self.outside.mkdir()
        (self.outside / 'keep').write_bytes(b'original producer evidence')

    def retire(self, **kwargs):
        """Invoke cache retirement with optional overrides to the fixture paths or result."""
        retirement.retire_successful_cell_caches(kwargs.get('private', self.private),
            kwargs.get('cell', self.cell), kwargs.get('result', self.result))

    def assert_caches_retained(self):
        """Require both caches and unrelated evidence to survive without a persisted cell proof."""
        self.assertTrue(all(cache.is_dir() for cache in self.caches))
        self.assertFalse((self.cell / 'cell-proof.private.json').exists())
        self.assertEqual(b'original producer evidence', (self.outside / 'keep').read_bytes())

    def test_success_persists_exact_proof_before_only_two_cache_deletions(self):
        """Verify success persists exact proof before only two cache deletions."""
        kept = {path: path.read_bytes() for path in self.root.rglob('*') if path.is_file()
                and not any(path.is_relative_to(cache) for cache in self.caches)}
        real_rmtree = shutil.rmtree
        removed = []
        def remove(path):
            self.assertEqual(self.result, json.loads((self.cell / 'cell-proof.private.json').read_text()))
            removed.append(path)
            real_rmtree(path)
        remove.avoids_symlink_attacks = True
        with patch.object(retirement.shutil, 'rmtree', remove):
            self.retire()
        self.assertEqual(self.caches, removed)
        self.assertTrue(all(not cache.exists() for cache in self.caches))
        self.assertEqual(kept, {path: path.read_bytes() for path in kept})

    def test_incomplete_or_false_result_never_cleans(self):
        """Verify incomplete or false result never cleans."""
        for mutation in ({'success': False}, {'success': 1}, {'success': None}, {'framework': '../outside'}, {'id': ''}):
            with self.subTest(mutation=mutation), self.assertRaises(ValueError):
                self.retire(result=self.result | mutation)
            self.assert_caches_retained()

    def test_outside_relative_dotdot_and_wrong_cell_paths_fail_closed(self):
        """Verify outside relative parent traversal and wrong cell paths fail closed."""
        for private, cell in ((self.private, self.outside), (Path('private'), self.cell),
                              (self.private, self.cell / '..' / self.cell.name),
                              (self.private, self.private / 'runtime-net9.0')):
            with self.subTest(cell=cell), self.assertRaises(ValueError):
                self.retire(private=private, cell=cell)
            self.assert_caches_retained()

    def test_both_caches_are_prevalidated_before_first_deletion(self):
        """Verify both caches are prevalidated before first deletion."""
        (self.caches[1] / 'linked').symlink_to(self.outside, target_is_directory=True)
        with self.assertRaisesRegex(ValueError, 'consumer_cache_retirement_symlink'):
            self.retire()
        self.assert_caches_retained()

    def test_symlink_cache_root_and_ancestor_are_rejected(self):
        """Verify symlink cache root and ancestor are rejected."""
        real = self.caches[1].with_name('original-packages')
        self.caches[1].rename(real)
        self.caches[1].symlink_to(real, target_is_directory=True)
        with self.assertRaisesRegex(ValueError, 'consumer_cache_retirement_path'):
            self.retire()
        self.assert_caches_retained()
        self.caches[1].unlink(); real.rename(self.caches[1])
        linked = self.root / 'linked-private'
        linked.symlink_to(self.private, target_is_directory=True)
        with self.assertRaisesRegex(ValueError, 'consumer_cache_retirement_path'):
            self.retire(private=linked, cell=linked / self.cell.name)
        self.assert_caches_retained()

    def test_missing_second_cache_never_deletes_first(self):
        """Verify missing second cache never deletes first."""
        shutil.rmtree(self.caches[1])
        with self.assertRaises(FileNotFoundError):
            self.retire()
        self.assertTrue(self.caches[0].is_dir())
        self.assertFalse((self.cell / 'cell-proof.private.json').exists())

    def test_exclusive_proof_and_persistence_failure_never_clean(self):
        """Verify exclusive proof and persistence failure never clean."""
        path = self.cell / 'cell-proof.private.json'
        path.write_bytes(b'existing evidence')
        with self.assertRaises(FileExistsError):
            self.retire()
        self.assertEqual(b'existing evidence', path.read_bytes())
        path.unlink()
        with patch.object(retirement.os, 'fsync', side_effect=OSError('persistence failed')):
            with self.assertRaisesRegex(OSError, 'persistence failed'):
                self.retire()
        self.assertTrue(all(cache.is_dir() for cache in self.caches))

    def test_second_cleanup_failure_propagates_and_keeps_proof(self):
        """Verify second cleanup failure propagates and keeps proof."""
        real_rmtree = shutil.rmtree
        def remove(path):
            if path == self.caches[1]:
                raise OSError('cleanup failed')
            real_rmtree(path)
        remove.avoids_symlink_attacks = True
        with patch.object(retirement.shutil, 'rmtree', remove):
            with self.assertRaisesRegex(OSError, 'cleanup failed'):
                self.retire()
        self.assertFalse(self.caches[0].exists())
        self.assertTrue(self.caches[1].is_dir())
        self.assertEqual(self.result, json.loads((self.cell / 'cell-proof.private.json').read_text()))

    def test_unsafe_platform_or_nonfinite_proof_never_clean(self):
        """Verify unsafe platform or nonfinite proof never clean."""
        with patch.object(retirement.shutil.rmtree, 'avoids_symlink_attacks', False):
            with self.assertRaisesRegex(ValueError, 'consumer_cache_retirement_unsafe_platform'):
                self.retire()
        self.assert_caches_retained()
        with self.assertRaises(ValueError):
            self.retire(result=self.result | {'invalid': float('nan')})
        self.assert_caches_retained()


if __name__ == '__main__':
    unittest.main()
