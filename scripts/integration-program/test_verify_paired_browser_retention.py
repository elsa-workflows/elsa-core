import json
from pathlib import Path
import tempfile
import unittest

from verify_paired_browser_retention import verify_retained_inventory


class BrowserRetentionTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.write("matrix.json")

    def write(self, name, body=None):
        path = self.root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps({} if body is None else body))
        return path

    def test_accepts_partial_failure_inventory_without_demanding_passing_cells(self):
        self.write("inputs/provenance.json")
        self.write("cells/3.10.0-net10.0-hosted-wasm/browser.json")
        self.assertEqual(len(verify_retained_inventory(self.root)), 3)

    def test_rejects_raw_logs_unknown_cells_and_non_object_payloads(self):
        for name, body in (("stdout.log", {}), ("cells/3.11.0-net10.0-server/browser.json", {}),
                           ("inputs/provenance.json", [])):
            with self.subTest(name=name):
                with tempfile.TemporaryDirectory() as directory:
                    root = Path(directory)
                    (root / "matrix.json").write_text("{}")
                    path = root / name
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text(json.dumps(body))
                    with self.assertRaises(ValueError):
                        verify_retained_inventory(root)

    def test_rejects_file_directory_and_root_links(self):
        with tempfile.TemporaryDirectory() as directory:
            outside = Path(directory)
            (outside / "matrix.json").write_text("{}")
            for target, name in ((outside / "matrix.json", "inputs/provenance.json"), (outside, "cells")):
                with self.subTest(name=name):
                    path = self.root / name
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.symlink_to(target)
                    with self.assertRaises((ValueError, RuntimeError)):
                        verify_retained_inventory(self.root)
                    path.unlink()
            alias = self.root / "linked"
            alias.symlink_to(outside)
            with self.assertRaises((ValueError, RuntimeError)):
                verify_retained_inventory(alias)


if __name__ == "__main__":
    unittest.main()
