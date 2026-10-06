import json
from pathlib import Path
import tempfile
import unittest

from verify_paired_browser_retention import verify_retained_inventory
import paired_package_released_documents as documents
from test_paired_package_released_documents import fixture_identity, write_released_fixture


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

    def released(self):
        key = ("3.9.0", "net10.0", "server")
        name = "cells/" + "-".join(key)
        path = self.root / name / "released-document.json"
        raw, child = write_released_fixture(path, key)
        execution = dict(zip(("version", "framework", "host"), key))
        execution.update(result="passed", stage="complete", owned_process_cleanup=True,
                         projects={"backend": {}}, resource_inventory={"assets": []},
                         runtime_readiness={"runtime": ".NET 10.0.8"}, loaded_assemblies={"backend": []},
                         browser_resources={"verified_assets": []})
        identity = fixture_identity()
        execution["released_document"] = documents.bind_released_document(path, key, child, execution, identity)
        self.write("inputs/provenance.json", {"browser_execution": identity})
        self.write(name + "/browser.json", child)
        self.write(name + "/execution.json", execution)
        return name, raw, child, execution, identity

    def test_exact_released_document_requires_all_source_bindings(self):
        name, raw, child, execution, identity = self.released()
        self.assertIn(name + "/released-document.json", verify_retained_inventory(self.root))
        mutations = [("inputs/provenance.json", {"browser_execution": {**identity, "fixture_source_commit": "c" * 40}}),
                     (name + "/execution.json", {**execution, "result": "failed"}),
                     (name + "/execution.json", {**execution, "loaded_assemblies": {"changed": []}}),
                     (name + "/browser.json", {**child, "browser_version": "150.0"})]
        for target, changed in mutations:
            original = (self.root / target).read_bytes()
            self.write(target, changed)
            with self.subTest(target=target), self.assertRaises(ValueError):
                verify_retained_inventory(self.root)
            (self.root / target).write_bytes(original)
        document = self.root / name / "released-document.json"
        document.write_bytes(raw + b" ")
        with self.assertRaises(ValueError):
            verify_retained_inventory(self.root)
        document.unlink()
        with self.assertRaisesRegex(ValueError, "missing its document"):
            verify_retained_inventory(self.root)

    def test_unbound_or_candidate_document_is_never_retained(self):
        self.released()
        path = self.root / "cells/3.9.0-net10.0-server/execution.json"
        path.unlink()
        with self.assertRaises(ValueError):
            verify_retained_inventory(self.root)
        self.write("cells/3.10.0-net10.0-server/released-document.json")
        with self.assertRaisesRegex(ValueError, "Unexpected browser evidence file"):
            verify_retained_inventory(self.root)

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
