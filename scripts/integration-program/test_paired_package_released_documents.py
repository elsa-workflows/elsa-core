import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest

import paired_package_released_documents as documents
import run_paired_package_browser_matrix as browser


def released_document_fixture(cell):
    document = {
        "$schema": documents.SCHEMA, "id": "123456789abcdef0", "definitionId": "123456789abcdef1",
        "name": "paired-browser-123456abcdef", "createdAt": "2026-10-06T19:46:35.851212+00:00",
        "version": 1, "toolVersion": documents.TOOL_VERSIONS[cell[0]], "variables": [], "inputs": [], "outcomes": [],
        "customProperties": {}, "isReadonly": False, "isSystem": False, "isLatest": True,
        "isPublished": False, "options": {"autoUpdateConsumingWorkflows": False},
        "outputs": [{"type": "String", "name": "sentinel", "displayName": "sentinel", "description": "", "category": "Primitives"}],
        "root": {
            "id": "123456789abcdef2", "nodeId": "Workflow1:123456789abcdef2", "name": "Flowchart1",
            "type": "Elsa.Flowchart", "version": 1, "variables": [], "connections": [], "metadata": {},
            "customProperties": {"notFoundConnections": [], "canStartWorkflow": False, "runAsynchronously": False},
            "activities": [{
                "id": "123456789abcde3", "nodeId": "Workflow1:123456789abcdef2:123456789abcde3",
                "name": "SetOutput1", "type": "Elsa.SetOutput", "version": 1,
                "customProperties": {"canStartWorkflow": False, "runAsynchronously": False},
                "metadata": {"designer": {"position": {"x": -24.5, "y": -32}, "size": {"width": 174.15625, "height": 54}}},
                "outputName": {"typeName": "String", "expression": {"type": "Literal", "value": "sentinel"}},
                "outputValue": {"typeName": "Object", "expression": {"type": "Literal", "value": "synthetic-browser-value"}},
            }],
        },
    }
    receipt = dict(zip(("version", "framework", "host"), cell))
    receipt.update(result="incomplete", browser_version="149.0.7827.55", resources=[], proof={},
                        assertions=[{"name": name, "passed": name not in documents.PYTHON_ASSERTIONS}
                                    for name in sorted(browser.required_assertions(receipt))])
    return document, receipt


def write_released_fixture(path, cell):
    document, receipt = released_document_fixture(cell)
    raw = json.dumps(document, separators=(",", ":")).encode()
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(raw)
    path.chmod(0o600)
    receipt["proof"] = {
        "last_completed_stage": "released_document_exported", "released_document_sha256": hashlib.sha256(raw).hexdigest(),
        "definition_id_sha256": hashlib.sha256(document["definitionId"].encode()).hexdigest(),
        "activity_id_sha256": hashlib.sha256(document["root"]["activities"][0]["id"].encode()).hexdigest(),
        "value_sha256": hashlib.sha256(b"synthetic-browser-value").hexdigest(),
    }
    return raw, receipt


def fixture_identity():
    return {"fixture_source_commit": "a" * 40, "run_id": None, "run_attempt": None,
            "fixture_files_sha256": {"scripts/integration-program/paired_package_execution.py": "b" * 64}}


class ReleasedDocumentContracts(unittest.TestCase):
    def setUp(self):
        # Generated contract input, never a claim of a package-authored document.
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.path = Path(self.directory.name) / "document.json"
        self.cell = ("3.9.0", "net10.0", "server")
        self.document, self.receipt = released_document_fixture(self.cell)

    def write(self, document=None, raw=None):
        document = self.document if document is None else document
        raw = json.dumps(document, separators=(",", ":")).encode() if raw is None else raw
        self.path.write_bytes(raw)
        self.path.chmod(0o600)
        self.receipt["proof"] = {
            "last_completed_stage": "released_document_exported", "released_document_sha256": hashlib.sha256(raw).hexdigest(),
            "definition_id_sha256": hashlib.sha256(self.document["definitionId"].encode()).hexdigest(),
            "activity_id_sha256": hashlib.sha256(self.document["root"]["activities"][0]["id"].encode()).hexdigest(),
            "value_sha256": hashlib.sha256(b"synthetic-browser-value").hexdigest(),
        }
        return raw

    def validate(self):
        return documents.validate_released_document(self.path, self.cell, self.receipt)

    def test_binding_preserves_bytes_and_child_without_acceptance_or_private_fields(self):
        raw = self.write()
        original = copy.deepcopy(self.receipt)
        binding = self.validate()
        self.assertEqual(raw, self.path.read_bytes())
        self.assertEqual(original, self.receipt)
        self.assertEqual(len(raw), binding["bytes"])
        self.assertEqual(hashlib.sha256(raw).hexdigest(), binding["document_sha256"])
        digest = binding.pop("binding_sha256")
        self.assertEqual(hashlib.sha256(json.dumps(binding, sort_keys=True, separators=(",", ":")).encode()).hexdigest(), digest)
        for private in (str(self.path), self.document["definitionId"], "synthetic-browser-value", "createdAt", "passed", "complete_matrix"):
            self.assertNotIn(private, json.dumps(binding))
        with self.assertRaises(ValueError):
            browser.check_cell(self.receipt)

    def test_browser_evidence_must_be_complete_unique_matching_and_successful(self):
        self.write()
        original = copy.deepcopy(self.receipt)
        mutations = [lambda r: r.update(result="failed"), lambda r: r.update(framework="net8.0"),
                     lambda r: r["assertions"].pop(), lambda r: r["assertions"].append(copy.deepcopy(r["assertions"][0])),
                     lambda r: next(a for a in r["assertions"] if a["name"] == "editor_smoke").update(passed=False),
                     lambda r: r["proof"].update(last_completed_stage="edit_reloaded")]
        mutations += [lambda r, key=key: r["proof"].update({key: "0" * 64}) for key in
                      ("released_document_sha256", "definition_id_sha256", "activity_id_sha256", "value_sha256")]
        for mutate in mutations:
            self.receipt = copy.deepcopy(original)
            mutate(self.receipt)
            with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                self.validate()

    def test_shape_rejects_extra_configuration_and_nonliteral_payloads(self):
        mutations = [lambda d: d.update(tenantId="private"), lambda d: d.update(inputs=[{"name": "secret"}]),
                     lambda d: d.update(toolVersion="3.10.0.0"), lambda d: d.update(isPublished=True),
                     lambda d: d["root"]["activities"].append(copy.deepcopy(d["root"]["activities"][0])),
                     lambda d: d["root"]["activities"][0]["outputValue"]["expression"].update(type="JavaScript", value="fetch('https://example.com')"),
                     lambda d: d["outputs"][0].update(isArray=False), lambda d: d["root"]["customProperties"].update(runAsynchronously=0),
                     lambda d: d["options"].update(autoUpdateConsumingWorkflows=0), lambda d: d.update(version=True)]
        for mutate in mutations:
            changed = copy.deepcopy(self.document)
            mutate(changed)
            self.write(changed)
            with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                self.validate()

    def test_identity_timestamp_and_geometry_are_strict(self):
        mutations = [lambda d: d.update(definitionId="01234567-uuid"), lambda d: d.update(name="real-workflow"),
                     lambda d: d.update(createdAt="2026-02-31T12:00:00Z"), lambda d: d.update(createdAt="2026-10-06T12:00:00+02:00"),
                     lambda d: d["root"]["activities"][0].update(nodeId="different-parent"),
                     lambda d: d["root"]["activities"][0]["metadata"]["designer"]["size"].update(width=0),
                     lambda d: d["root"]["activities"][0]["metadata"]["designer"]["position"].update(x=True),
                     lambda d: d["root"]["activities"][0]["metadata"]["designer"]["position"].update(y=10001)]
        for mutate in mutations:
            changed = copy.deepcopy(self.document)
            mutate(changed)
            self.write(changed)
            with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                self.validate()

    def test_duplicate_keys_invalid_utf8_nonfinite_and_nested_or_large_values_fail(self):
        nested = 0
        for _ in range(18):
            nested = [nested]
        malformed = [b'{"id":"first","id":"second"}', b'\xff', b'{"value":NaN}', b'{"value":1e400}',
                     json.dumps({"value": nested}).encode(), json.dumps({"value": "x" * 257}).encode(),
                     json.dumps({"value": [0] * 33}).encode(), b"x" * (documents.MAX_BYTES + 1)]
        for raw in malformed:
            self.write(raw=raw)
            with self.subTest(bytes=len(raw)), self.assertRaises(ValueError):
                self.validate()

    def test_raw_download_must_be_private_regular_external_and_not_symlinked(self):
        self.write()
        self.path.chmod(0o644)
        with self.assertRaises(ValueError):
            self.validate()
        self.path.chmod(0o600)
        leaf = self.path.with_name("leaf.json")
        leaf.symlink_to(self.path)
        alias = self.path.parent / "alias"
        alias.symlink_to(self.path.parent, target_is_directory=True)
        for path in (leaf, alias / self.path.name, browser.JOURNEY.parent / "document.json", self.path.parent):
            with self.subTest(path=path), self.assertRaises((ValueError, RuntimeError)):
                documents.validate_released_document(path, self.cell, self.receipt)

    def test_observed_38_tool_marker_is_not_inferred_from_package_version(self):
        self.cell = ("3.8.4", "net10.0", "server")
        self.receipt["version"] = "3.8.4"
        self.document["toolVersion"] = "3.8.0.0"
        self.write()
        self.assertEqual("3.8.0.0", self.validate()["tool_version"])
        for marker in ("3.8.4.0", "3.9.0.0"):
            self.document["toolVersion"] = marker
            self.write()
            with self.subTest(marker=marker), self.assertRaises(ValueError):
                self.validate()

    def test_candidate_cannot_author_a_released_document(self):
        self.write()
        self.cell = ("3.10.0", "net10.0", "server")
        self.receipt["version"] = "3.10.0"
        with self.assertRaisesRegex(ValueError, "no observed shape"):
            self.validate()


if __name__ == "__main__":
    unittest.main()
