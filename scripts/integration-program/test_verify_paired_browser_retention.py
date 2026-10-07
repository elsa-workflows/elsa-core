import copy
import json
from pathlib import Path
import tempfile
import unittest

from verify_paired_browser_retention import verify_retained_inventory
import paired_package_released_documents as documents
import paired_package_react_phase as react_phase
from verify_browser_package_resources import verify_browser_resources
from test_paired_package_react_phase import phase_fixture
from test_paired_package_browser_matrix import attach_native_interop, bpmn_proof, clipboard_proof, reopen_row
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

    def react(self, *, passed=True, prefix=""):
        request, original, assets, phase = phase_fixture("hosted-wasm" if prefix else "server")
        key = (request.version, request.framework, request.host)
        name = "cells/" + "-".join(key)
        for assertion in original["assertions"]:
            assertion.update(passed=assertion["name"] != "reactflow_edit_save", reason_category=None)
        original["proof"].update(instance_id_sha256="a" * 64,
            baseline_reopens=[reopen_row(version, request.framework, request.host) for version in documents.TOOL_VERSIONS],
            bpmn_roundtrip=bpmn_proof(), clipboard=clipboard_proof("a" * 64, original["proof"]["value_sha256"]))
        attach_native_interop(original)
        phase["source_browser_sha256"] = react_phase.browser_receipt_sha256(original)
        if prefix:
            for asset in assets + phase["resources"]:
                asset["path"] = "/" + prefix + asset["path"]
        if not passed:
            phase.update(result="failed", failure_category=react_phase.FAILURE)
            phase["checks"]["identity_preserved"] = False
        summary = react_phase.summarize_react_phase(phase)
        combined = copy.deepcopy(original)
        combined.update(result="passed", resources=original["resources"] + phase["resources"])
        combined["proof"]["reactflow"] = summary
        for assertion in combined["assertions"]:
            assertion.update(passed=True, reason_category=None)
        evidence = dict(zip(("version", "framework", "host"), key), result="passed" if passed else "failed",
                        route_prefix=prefix,
                        stage="complete" if passed else "browser_contract", owned_process_cleanup=True,
                        resource_inventory={"assets": assets}, react_phase=summary, react_runtime_continuity=True,
                        react_loaded_assemblies={}, browser_resources=verify_browser_resources(
                            assets, combined["resources"], route_prefix="/" + prefix if prefix else ""))
        for filename, record in (("browser", original), ("react-phase", phase), ("execution", evidence)):
            path = self.write(name + "/" + filename + ".json", record)
            path.write_text(json.dumps(record, indent=2, sort_keys=True) + "\n")
        self.write("matrix.json", {"cells": [combined] if passed else []})
        return name, original, phase, evidence, combined

    def test_react_crossbinds_original_phase_execution_and_combined_resources(self):
        name, original, phase, evidence, combined = self.react()
        self.assertIn(name + "/react-phase.json", verify_retained_inventory(self.root))
        self.assertEqual([], original["resources"])
        self.assertEqual(phase["resources"], combined["resources"])
        self.assertNotIn("reactflow", original["proof"])
        mutations = [
            (name + "/execution.json", dict(evidence, owned_process_cleanup=False)),
            (name + "/execution.json", dict(evidence, react_phase={})),
            (name + "/execution.json", dict(evidence, browser_resources={})),
            ("matrix.json", {"cells": []}),
            ("matrix.json", {"cells": [dict(combined, resources=[])]}),
            (name + "/react-phase.json", dict(phase, password="PRIVATE")),
            (name + "/react-phase.json", dict(phase, source_browser_sha256="b" * 64)),
        ]
        for target, changed in mutations:
            path = self.root / target
            raw = path.read_bytes()
            self.write(target, changed)
            with self.subTest(target=target, keys=list(changed)), self.assertRaises(ValueError):
                verify_retained_inventory(self.root)
            path.write_bytes(raw)
        for filename in ("browser", "react-phase"):
            path = self.root / name / (filename + ".json")
            raw = path.read_bytes()
            path.write_bytes(raw + b" ")
            with self.subTest(whitespace=filename), self.assertRaisesRegex(ValueError, "retained bytes"):
                verify_retained_inventory(self.root)
            path.write_bytes(raw)

    def test_failed_react_phase_retains_safe_partial_evidence_without_a_matrix_claim(self):
        name, _, _, _, _ = self.react(passed=False)
        self.assertIn(name + "/react-phase.json", verify_retained_inventory(self.root))

    def test_prefixed_hosted_phase_requires_the_same_safe_inventory_prefix(self):
        name, _, _, evidence, _ = self.react(prefix="compat")
        self.assertIn(name + "/react-phase.json", verify_retained_inventory(self.root))
        for prefix in ("", "wrong", "../private", "/private", None):
            self.write(name + "/execution.json", dict(evidence, route_prefix=prefix))
            with self.subTest(prefix=prefix), self.assertRaises(ValueError):
                verify_retained_inventory(self.root)

    def test_missing_or_unsupported_react_phase_fails_closed(self):
        name, _, _, _, _ = self.react()
        (self.root / name / "react-phase.json").unlink()
        with self.assertRaisesRegex(ValueError, "missing its React phase"):
            verify_retained_inventory(self.root)
        for cell in ("3.9.0-net10.0-server", "3.10.0-net10.0-custom-elements"):
            path = self.write("cells/" + cell + "/react-phase.json")
            with self.subTest(cell=cell), self.assertRaisesRegex(ValueError, "Unexpected browser evidence file"):
                verify_retained_inventory(self.root)
            path.unlink()

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
