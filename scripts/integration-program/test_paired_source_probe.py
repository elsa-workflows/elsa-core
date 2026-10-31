import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('pair_probe', Path(__file__).with_name('run_paired_source_probe.py'))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)


class ImportPatchTests(unittest.TestCase):
    def test_removes_only_exact_unused_import_preserving_line_endings(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / '_Imports.razor'
            before = b'@using Other\r\n@using Blazored.FluentValidation\r\n@using Last\r\n'
            path.write_bytes(before)
            receipt = probe.patch_import(path)
            self.assertEqual(path.read_bytes(), b'@using Other\r\n@using Last\r\n')
            self.assertNotEqual(receipt['beforeSha256'], receipt['afterSha256'])

    def test_missing_or_duplicate_import_fails_without_modifying_file(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / '_Imports.razor'
            for content in (b'@using Other\n', (probe.UNUSED_IMPORT + b'\n') * 2):
                with self.subTest(content=content):
                    path.write_bytes(content)
                    with self.assertRaises(ValueError):
                        probe.patch_import(path)
                    self.assertEqual(path.read_bytes(), content)


if __name__ == '__main__':
    unittest.main()
