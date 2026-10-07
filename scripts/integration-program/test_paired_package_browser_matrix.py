import copy
from contextlib import contextmanager
import hashlib
import json
import os
from pathlib import Path
import shutil
import tempfile
import unittest
from types import SimpleNamespace
from unittest.mock import patch

import consolidated_candidate_input as candidate
import paired_package_released_documents as documents
import paired_package_react_phase as react_phase
from test_paired_package_released_documents import fixture_identity, write_released_fixture

import run_paired_package_browser_matrix as matrix
from test_paired_package_wasm_boot import boot_receipt_fixture
from test_paired_package_browser_native_interop import json_proof, dom_proof
from test_paired_package_workflow_contexts import contexts_proof


def reopen_row(version, framework="net10.0", host="server"):
    return {"source_cell": {"version": version, "framework": framework, "host": host},
            **{name: "a" * 64 for name in ("source_binding_sha256", "document_sha256", "definition_id_sha256", "root_id_sha256", "activity_id_sha256", "value_sha256", "instance_id_sha256")},
            "tool_version": documents.TOOL_VERSIONS[version], "checks": {name: True for name in matrix.REOPEN_CHECKS}}


def sha256(value):
    return hashlib.sha256(value.encode("utf-8")).hexdigest()


def bpmn_proof(*, complete=True):
    checks = {name: complete for name in matrix.BPMN_CHECKS}
    result = {
        "input_xml_sha256": matrix.BPMN_INPUT_SHA256,
        "input_xml_bytes": matrix.BPMN_INPUT_BYTES,
        "semantic_sha256": matrix.BPMN_SEMANTIC_SHA256,
        **matrix.BPMN_ID_SHA256,
        "checks": checks,
    }
    if complete:
        result.update(first_definition_id_sha256=sha256("first definition"),
                      export_xml_sha256=sha256("exported BPMN"), export_xml_bytes=679,
                      second_definition_id_sha256=sha256("second definition"))
    return result


def clipboard_proof(instance_hash, value_hash, *, observed=True):
    result = {"instance_id_sha256": instance_hash, "expected_value_sha256": value_hash,
              "native_copy_observed": observed}
    if observed:
        result["actual_value_sha256"] = value_hash
    return result


def direct_backend_proof():
    return {"checks": dict.fromkeys(matrix.DIRECT_BACKEND_CHECKS, True),
            "login_status": 200, "descriptor_status": 200, "descriptor_count": 2,
            "descriptor_body_sha256": "a" * 64}


def assertion(record, name, passed):
    next(item for item in record["assertions"] if item["name"] == name)["passed"] = passed


def attach_native_interop(record):
    """Synthetic protocol fixture; no native browser acceptance is implied."""
    proof = record["proof"]
    if record["host"] not in matrix.NATIVE_JSON_HOSTS:
        proof.pop("json_roundtrip", None)
        proof.pop("dom_interop", None)
        for name in ("json_roundtrip", "dom_interop"):
            assertion(record, name, False)
        record["result"] = "incomplete"
        return
    value = json_proof(execution_complete=True)
    for name, parent in (("definition_id_sha256", "definition_id_sha256"),
                         ("root_id_sha256", "root_id_sha256"), ("activity_id_sha256", "activity_id_sha256"),
                         ("expected_value_sha256", "value_sha256"), ("instance_id_sha256", "instance_id_sha256")):
        value[name] = proof.setdefault(parent, value[name])
    value["actual_output_sha256"] = value["expected_value_sha256"]
    proof.update(json_roundtrip=value, dom_interop=dom_proof(value))


def attach_react_phase(record):
    """Synthetic combined evidence only; an X6 child never owns this summary."""
    if record["host"] not in matrix.REACT_PHASE_HOSTS:
        assertion(record, "reactflow_edit_save", False)
        record["result"] = "incomplete"
        return
    source = copy.deepcopy(record)
    source["proof"].pop("reactflow", None)
    assertion(source, "reactflow_edit_save", False)
    source["result"] = "incomplete"
    record["proof"]["reactflow"] = {
        "source_browser_sha256": react_phase.browser_receipt_sha256(source),
        "phase_receipt_sha256": "c" * 64,
        "checks": dict.fromkeys(react_phase.CHECKS, True),
        "hashes": {name: source["proof"][parent] for name, parent in react_phase.BEFORE_HASHES.items()} |
                  {"after_value_sha256": react_phase.AFTER_VALUE_SHA256},
    }


def attach_embedding(record):
    """Synthetic callback receipts, never browser acceptance."""
    if record["host"] != "custom-elements":
        return
    from paired_package_embedding import CHECKS
    fields = ("definition_id_sha256", "activity_id_sha256", "instance_id_sha256")
    values = {name: record["proof"].setdefault(name, sha256(name)) for name in fields}
    record["proof"]["embedding"] = {"checks": dict.fromkeys(CHECKS, True),
        **values, "version_id_sha256": sha256("version")}


def tsx_command(script):
    local = matrix.JOURNEY.parent / "node_modules/.bin/tsx"
    executable = local if local.is_file() else shutil.which("tsx")
    return [str(executable), script] if executable else ["npm", "exec", "--no", "--", "tsx", script]


@contextmanager
def tracked_bpmn_fixture():
    with tempfile.TemporaryDirectory() as directory:
        path = Path(directory) / "paired-browser.bpmn"
        raw = b"test-only tracked BPMN bytes"
        path.write_bytes(raw)
        with patch.object(matrix, "BPMN_INPUT_PATH", path), \
             patch.object(matrix, "BPMN_INPUT_SHA256", hashlib.sha256(raw).hexdigest()), \
             patch.object(matrix, "BPMN_INPUT_BYTES", len(raw)):
            yield


class MatrixContracts(unittest.TestCase):
    def setUp(self):
        self.ledger = matrix.new_ledger()
        self.handle = SimpleNamespace(studio_url="http://localhost:1", backend_url="http://localhost:2/elsa/api", username="private", password="private", safe_ids={})
        for cell in self.ledger["cells"]:
            cell.update(result="passed", browser_version="149.0.7827.55", resources=[], proof={}, failure_category=None, assertions=[{"name": name, "passed": True} for name in sorted(matrix.required_assertions(cell))])
            if cell["host"] == "wasm":
                cell["proof"]["direct_backend"] = direct_backend_proof()
                # Source-derived synthetic protocol fixtures, never runtime proof.
                boot_proof, _, observed = boot_receipt_fixture(cell["framework"])
                cell["proof"].update(wasm_boot=boot_proof, interactive_validation_observed=True)
                cell["resources"] = observed
            if cell["version"] == "3.10.0":
                cell["proof"]["baseline_reopens"] = [reopen_row(version, cell["framework"], cell["host"]) for version in documents.TOOL_VERSIONS]
                instance_hash, value_hash = sha256("matrix candidate instance"), sha256("matrix candidate sentinel")
                cell["proof"].update(instance_id_sha256=instance_hash, value_sha256=value_hash,
                                      bpmn_roundtrip=bpmn_proof(),
                                      clipboard=clipboard_proof(instance_hash, value_hash))
                attach_native_interop(cell)
                cell["proof"]["workflow_contexts"] = contexts_proof()
            attach_embedding(cell)
            if cell["version"] == "3.10.0":
                attach_react_phase(cell)

    def released_inputs(self, root):
        inputs = []
        for version in documents.TOOL_VERSIONS:
            key = (version, "net10.0", "server")
            path = root / version / "document.json"
            _, child = write_released_fixture(path, key)
            execution = dict(zip(("version", "framework", "host"), key))
            execution.update(owned_process_cleanup=True, **{name: {"contract": True} for name in
                             ("projects", "resource_inventory", "runtime_readiness", "loaded_assemblies", "browser_resources")})
            inputs.append({"private_path": str(path), "binding": documents.bind_released_document(path, key, child, execution, fixture_identity())})
        return inputs

    def test_private_input_pair_has_exact_source_cells_and_immutable_bounded_bytes(self):
        key = ("3.10.0", "net10.0", "server")
        with tempfile.TemporaryDirectory() as directory:
            inputs = self.released_inputs(Path(directory))
            before = copy.deepcopy(inputs)
            payload = matrix._released_inputs(key, inputs)
            self.assertEqual(before, inputs)
            self.assertEqual(["3.8.4", "3.9.0"], [p["binding"]["document"]["source_cell"]["version"] for p in payload])
            mutations = [lambda p: p.pop(), lambda p: p.append(p[0]), lambda p: p.__setitem__(1, copy.deepcopy(p[0])),
                         lambda p: p[0]["binding"]["document"]["source_cell"].update(host="wasm"),
                         lambda p: p[0]["binding"]["document"].update(bytes=True),
                         lambda p: p[0]["binding"]["document"].update(document_sha256="b" * 64),
                         lambda p: p[0]["binding"].update(raw_error="private")]
            for mutate in mutations:
                changed = copy.deepcopy(inputs); mutate(changed)
                with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                    matrix._released_inputs(key, changed)
            path = Path(inputs[0]["private_path"])
            path.write_bytes(path.read_bytes() + b" ")
            with self.assertRaisesRegex(ValueError, "bytes differ"):
                matrix._released_inputs(key, inputs)

    def test_private_released_input_symlink_permissions_and_baseline_consumer_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            inputs = self.released_inputs(Path(directory))
            original = Path(inputs[0]["private_path"])
            alias = original.with_name("alias.json"); alias.symlink_to(original)
            for version, path in (("3.9.0", original), ("3.10.0", alias)):
                changed = copy.deepcopy(inputs); changed[0]["private_path"] = str(path)
                with self.subTest(version=version, path=path), self.assertRaises((ValueError, RuntimeError)):
                    matrix._released_inputs((version, "net10.0", "server"), changed)
            original.chmod(0o644)
            with self.assertRaises(ValueError):
                matrix._released_inputs(("3.10.0", "net10.0", "server"), inputs)

    def test_reopen_proof_requires_exact_both_completed_journeys_without_private_fields(self):
        key = ("3.10.0", "net10.0", "server")
        record = copy.deepcopy(next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == key))
        mutations = [lambda r: r["proof"].pop("baseline_reopens"), lambda r: r["proof"]["baseline_reopens"].pop(),
                     lambda r: r["proof"]["baseline_reopens"].__setitem__(1, copy.deepcopy(r["proof"]["baseline_reopens"][0])),
                     lambda r: r["proof"]["baseline_reopens"][0]["checks"].update(saved=False),
                     lambda r: r["proof"]["baseline_reopens"][0].pop("instance_id_sha256"),
                     lambda r: r["proof"]["baseline_reopens"][0].update(private_path="private"),
                     lambda r: r["proof"]["baseline_reopens"][0]["source_cell"].update(framework="net8.0")]
        for mutate in mutations:
            changed = copy.deepcopy(record); mutate(changed)
            with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                matrix.validate_browser_receipt(changed, key)
            with self.subTest(direct_acceptance=mutate), self.assertRaises(ValueError):
                matrix.check_cell(changed)
        partial = copy.deepcopy(record)
        partial.update(result="failed")
        next(a for a in partial["assertions"] if a["name"] == "baseline_reopen")["passed"] = False
        partial["proof"]["baseline_reopens"] = [partial["proof"]["baseline_reopens"][0]]
        partial["proof"]["baseline_reopens"][0]["checks"]["saved"] = False
        self.assertEqual(partial, matrix.validate_browser_receipt(partial, key))

    def run_node_contract(self, script, expected_output, *, stdin=None):
        result = matrix._run_browser_process(tsx_command(script), cwd=matrix.JOURNEY.parent,
                                             input=stdin or "", env=os.environ.copy(), timeout=60)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(expected_output + "\n", result.stdout)

    def test_node_json_roundtrip_checks_native_document_semantics(self):
        self.run_node_contract("json-roundtrip.contract.ts", "JSON roundtrip contracts passed")

    def test_original_child_cannot_supply_combined_react_claim(self):
        key = ("3.10.0", "net10.0", "server")
        record = copy.deepcopy(next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == key))
        with patch.object(matrix, "_run_browser_process", return_value=SimpleNamespace(stdout=json.dumps(record), returncode=0)):
            with self.assertRaisesRegex(ValueError, "X6 browser child"):
                matrix.run_browser(self.handle, dict(zip(("version", "framework", "host"), key)), [])
        record["proof"].pop("reactflow")
        with self.assertRaisesRegex(ValueError, "Missing independent React"):
            matrix.validate_browser_receipt(record, key)

    def test_node_direct_backend_contract_requires_native_auth_and_cors(self):
        self.run_node_contract("direct-backend.contract.ts", "direct backend observer contracts passed")

    def test_node_wasm_boot_contract_requires_original_bytes_and_executed_callback(self):
        self.run_node_contract("wasm-boot.contract.ts", "WASM bootstrap parser and observer contracts passed")

    def test_node_custom_elements_contract_requires_native_auth_and_bound_callbacks(self):
        self.run_node_contract("custom-elements.contract.ts", "CustomElements callback contracts passed")

    def test_embedding_claims_are_validated_at_browser_receipt_boundary(self):
        for version in matrix.VERSIONS:
            key = (version, "net10.0", "custom-elements")
            base = next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == key)
            for mutate in (lambda p: p.pop("embedding"),
                           lambda p: p["embedding"].update(instance_id_sha256="f" * 64),
                           lambda p: p["embedding"]["checks"].update(native_authentication=False)):
                changed = copy.deepcopy(base)
                mutate(changed["proof"])
                with self.subTest(version=version, mutation=mutate), self.assertRaises(ValueError):
                    matrix.validate_browser_receipt(changed, key)

    def test_standalone_boot_format_cannot_cross_framework_receipt_boundary(self):
        for framework in matrix.FRAMEWORKS:
            key = ("3.9.0", framework, "wasm")
            base = next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == key)
            matrix.validate_browser_receipt(base, key)
            for other in set(matrix.FRAMEWORKS) - {framework}:
                changed = copy.deepcopy(base)
                changed["proof"]["wasm_boot"] = boot_receipt_fixture(other)[0]
                with self.subTest(framework=framework, other=other), self.assertRaises(ValueError):
                    matrix.validate_browser_receipt(changed, key)

    def test_standalone_net10_boot_receipt_binds_original_requested_resource_metadata(self):
        key = ("3.9.0", "net10.0", "wasm")
        record = copy.deepcopy(next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == key))
        _, expected, _ = boot_receipt_fixture()
        with patch.object(matrix, "_run_browser_process", return_value=SimpleNamespace(stdout=json.dumps(record), returncode=0)):
            self.assertEqual(record, matrix.run_browser(self.handle, dict(zip(("version", "framework", "host"), key)), expected))
            with self.assertRaisesRegex(ValueError, "original requested"):
                matrix.run_browser(self.handle, dict(zip(("version", "framework", "host"), key)), [])

    def test_node_independently_checks_bytes_and_actual_graph_semantics(self):
        with tempfile.TemporaryDirectory() as directory:
            inputs = matrix._released_inputs(("3.10.0", "net10.0", "server"), self.released_inputs(Path(directory)))
            self.run_node_contract("native-import.contract.ts", "native import contracts passed", stdin=json.dumps(inputs))

    def test_node_bpmn_contract_checks_the_tracked_fixture_and_semantic_mutations(self):
        tracked = matrix.BPMN_INPUT_PATH.read_bytes()
        self.assertEqual(matrix.BPMN_INPUT_BYTES, len(tracked))
        self.assertEqual(matrix.BPMN_INPUT_SHA256, hashlib.sha256(tracked).hexdigest())
        self.run_node_contract("bpmn-roundtrip.contract.ts", "BPMN roundtrip contracts passed")

    def test_returned_reopen_proof_is_crossbound_to_each_supplied_source(self):
        key = ("3.10.0", "net10.0", "server")
        with tempfile.TemporaryDirectory() as directory:
            inputs = self.released_inputs(Path(directory))
            payload = matrix._released_inputs(key, inputs)
            record = copy.deepcopy(next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == key))
            record["proof"].pop("reactflow")
            assertion(record, "reactflow_edit_save", False)
            record["result"] = "incomplete"
            rows = record["proof"]["baseline_reopens"]
            for row, item in zip(rows, payload):
                document = item["binding"]["document"]
                for field in ("document_sha256", "definition_id_sha256", "activity_id_sha256", "value_sha256"):
                    row[field] = document[field]
                row.update(source_binding_sha256=item["source_binding_sha256"], root_id_sha256=item["root_id_sha256"])
            for field in (None, "source_binding_sha256", "document_sha256", "root_id_sha256", "activity_id_sha256"):
                changed = copy.deepcopy(record)
                if field:
                    changed["proof"]["baseline_reopens"][0][field] = "0" * 64
                with self.subTest(field=field), patch.object(matrix, "_run_browser_process", return_value=SimpleNamespace(stdout=json.dumps(changed), returncode=0)):
                    if field:
                        with self.assertRaisesRegex(ValueError, "supplied bytes/source"):
                            matrix.run_browser(self.handle, dict(zip(("version", "framework", "host"), key)), [], released_document_inputs=inputs)
                    else:
                        self.assertEqual(record, matrix.run_browser(self.handle, dict(zip(("version", "framework", "host"), key)), [], released_document_inputs=inputs))

    def test_bpmn_roundtrip_pass_requires_exact_tracked_input_and_complete_native_journey(self):
        key = ("3.10.0", "net10.0", "server")
        record = copy.deepcopy(next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == key))
        record["result"] = "incomplete"
        assertion(record, "clipboard", False)
        record["proof"].pop("clipboard")
        with tracked_bpmn_fixture():
            record["proof"]["bpmn_roundtrip"] = bpmn_proof()
            self.assertEqual(record, matrix.validate_browser_receipt(record, key))

    def test_bpmn_partial_failure_retains_only_safe_bounded_fields(self):
        key = ("3.10.0", "net10.0", "server")
        record = copy.deepcopy(next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == key))
        record["result"] = "incomplete"
        assertion(record, "bpmn_roundtrip", False)
        assertion(record, "clipboard", False)
        record["proof"].pop("clipboard")
        with tracked_bpmn_fixture():
            record["proof"]["bpmn_roundtrip"] = bpmn_proof(complete=False)
            sanitized = matrix.validate_browser_receipt(record, key)
        self.assertEqual(record, sanitized)
        self.assertNotIn("paired-process", json.dumps(sanitized))
        self.assertNotIn("<definitions", json.dumps(sanitized).lower())

    def test_bpmn_export_observation_survives_later_semantic_failure(self):
        key = ("3.10.0", "net10.0", "server")
        record = copy.deepcopy(next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == key))
        record["result"] = "incomplete"
        assertion(record, "bpmn_roundtrip", False)
        assertion(record, "clipboard", False)
        record["proof"].pop("clipboard")
        with tracked_bpmn_fixture():
            proof = bpmn_proof(complete=False)
            proof["checks"].update(imported=True, rendered=True, selection_callback=True, exported=True)
            proof.update(first_definition_id_sha256=sha256("first definition"),
                         export_xml_sha256=sha256("exported BPMN"), export_xml_bytes=679)
            record["proof"]["bpmn_roundtrip"] = proof
            self.assertEqual(record, matrix.validate_browser_receipt(record, key))

    def test_bpmn_proof_rejects_unbound_or_inconsistent_shapes(self):
        key = ("3.10.0", "net10.0", "server")
        base = copy.deepcopy(next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == key))
        base["result"] = "incomplete"
        assertion(base, "clipboard", False)
        base["proof"].pop("clipboard")
        mutations = [
            lambda r: r["proof"].pop("bpmn_roundtrip"),
            lambda r: r["proof"]["bpmn_roundtrip"].update(raw_xml="<definitions/>"),
            lambda r: r["proof"]["bpmn_roundtrip"].update(input_xml_sha256="0" * 64),
            lambda r: r["proof"]["bpmn_roundtrip"].update(input_xml_bytes=1024 * 1024 + 1),
            lambda r: r["proof"]["bpmn_roundtrip"].update(input_xml_bytes=True),
            lambda r: r["proof"]["bpmn_roundtrip"].update(semantic_sha256="0" * 64),
            lambda r: r["proof"]["bpmn_roundtrip"].update(process_id_sha256="0" * 64),
            lambda r: r["proof"]["bpmn_roundtrip"]["checks"].update(rendered=1),
            lambda r: r["proof"]["bpmn_roundtrip"]["checks"].pop("selection_callback"),
            lambda r: r["proof"]["bpmn_roundtrip"].pop("first_definition_id_sha256"),
            lambda r: r["proof"]["bpmn_roundtrip"].update(second_definition_id_sha256=r["proof"]["bpmn_roundtrip"]["first_definition_id_sha256"]),
            lambda r: r["proof"]["bpmn_roundtrip"]["checks"].update(reimported=False),
            lambda r: r["proof"]["bpmn_roundtrip"]["checks"].update(semantic_preserved=False),
            lambda r: assertion(r, "bpmn_roundtrip", False),
            lambda r: r["proof"]["bpmn_roundtrip"].update(export_xml_bytes=1024 * 1024 + 1),
        ]
        with tracked_bpmn_fixture():
            base["proof"]["bpmn_roundtrip"] = bpmn_proof()
            for mutate in mutations:
                changed = copy.deepcopy(base)
                mutate(changed)
                with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                    matrix.validate_browser_receipt(changed, key)
                with self.subTest(check_cell=mutate), self.assertRaises(ValueError):
                    matrix.check_cell(changed)

    def test_bpmn_proof_rejects_changed_tracked_input_and_wrong_release_cell(self):
        key = ("3.10.0", "net10.0", "server")
        record = copy.deepcopy(next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == key))
        record["result"] = "incomplete"
        assertion(record, "clipboard", False)
        record["proof"].pop("clipboard")
        with tracked_bpmn_fixture():
            record["proof"]["bpmn_roundtrip"] = bpmn_proof()
            matrix.BPMN_INPUT_PATH.write_bytes(b"changed tracked source")
            with self.assertRaisesRegex(ValueError, "Tracked BPMN input"):
                matrix.validate_browser_receipt(record, key)
        baseline = copy.deepcopy(next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == ("3.9.0", "net10.0", "server")))
        with tracked_bpmn_fixture():
            baseline["proof"]["bpmn_roundtrip"] = bpmn_proof()
            with self.assertRaisesRegex(ValueError, "Unexpected BPMN"):
                matrix.validate_browser_receipt(baseline, ("3.9.0", "net10.0", "server"))

    def test_clipboard_pass_requires_native_copy_and_equal_hashes_bound_to_candidate(self):
        key = ("3.10.0", "net10.0", "server")
        record = copy.deepcopy(next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == key))
        record["result"] = "incomplete"
        assertion(record, "bpmn_roundtrip", False)
        record["proof"].pop("bpmn_roundtrip")
        instance_hash, value_hash = sha256("original instance"), sha256("synthetic output")
        record["proof"].update(instance_id_sha256=instance_hash, value_sha256=value_hash,
                               clipboard={"instance_id_sha256": instance_hash,
                                          "expected_value_sha256": value_hash,
                                          "actual_value_sha256": value_hash,
                                          "native_copy_observed": True})
        attach_native_interop(record)
        attach_react_phase(record)
        self.assertEqual(record, matrix.validate_browser_receipt(record, key))

    def test_clipboard_partial_failure_is_sanitized_and_never_promoted(self):
        key = ("3.10.0", "net10.0", "server")
        record = copy.deepcopy(next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == key))
        record["result"] = "incomplete"
        assertion(record, "bpmn_roundtrip", False)
        assertion(record, "clipboard", False)
        record["proof"].pop("bpmn_roundtrip")
        instance_hash, value_hash = sha256("instance"), sha256("expected")
        record["proof"].update(instance_id_sha256=instance_hash, value_sha256=value_hash)
        record["proof"]["clipboard"] = {"instance_id_sha256": instance_hash,
                                          "expected_value_sha256": value_hash,
                                          "native_copy_observed": False}
        attach_native_interop(record)
        attach_react_phase(record)
        self.assertEqual(record, matrix.validate_browser_receipt(record, key))

    def test_clipboard_proof_rejects_raw_fields_hash_mismatch_and_false_claims(self):
        key = ("3.10.0", "net10.0", "server")
        instance_hash, value_hash, actual_hash = sha256("instance"), sha256("expected"), sha256("actual")
        base = copy.deepcopy(next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == key))
        base["result"] = "incomplete"
        assertion(base, "bpmn_roundtrip", False)
        base["proof"].pop("bpmn_roundtrip")
        base["proof"].update(instance_id_sha256=instance_hash, value_sha256=value_hash,
                              clipboard={"instance_id_sha256": instance_hash,
                                         "expected_value_sha256": value_hash,
                                         "actual_value_sha256": value_hash,
                                         "native_copy_observed": True})
        attach_native_interop(base)
        attach_react_phase(base)
        mutations = [
            lambda r: r["proof"].pop("clipboard"),
            lambda r: r["proof"]["clipboard"].update(raw_value="private"),
            lambda r: r["proof"]["clipboard"].update(native_copy_observed=1),
            lambda r: r["proof"]["clipboard"].pop("actual_value_sha256"),
            lambda r: r["proof"]["clipboard"].update(actual_value_sha256=actual_hash),
            lambda r: r["proof"]["clipboard"].update(instance_id_sha256="0" * 64),
            lambda r: r["proof"]["clipboard"].update(expected_value_sha256="0" * 64),
            lambda r: r["proof"].pop("instance_id_sha256"),
            lambda r: r["proof"].pop("value_sha256"),
            lambda r: assertion(r, "clipboard", False),
            lambda r: r["proof"].update(instance_id_sha256="0" * 64),
        ]
        for mutate in mutations:
            changed = copy.deepcopy(base)
            mutate(changed)
            with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                matrix.validate_browser_receipt(changed, key)
            with self.subTest(check_cell=mutate), self.assertRaises(ValueError):
                matrix.check_cell(changed)
        partial = copy.deepcopy(base)
        partial["result"] = "incomplete"
        assertion(partial, "clipboard", False)
        partial["proof"]["clipboard"]["native_copy_observed"] = False
        partial["proof"]["clipboard"]["actual_value_sha256"] = actual_hash
        self.assertEqual(partial, matrix.validate_browser_receipt(partial, key))

    def test_exact_matrix_topology_accepts_complete_synthetic_protocol_only(self):
        # These generated records exercise protocol shape, never actual runtime acceptance.
        matrix.check_matrix(self.ledger)
        self.assertEqual(36, len(self.ledger["cells"]))
        for cell in self.ledger["cells"]:
            matrix.validate_browser_receipt(cell, matrix.identity(cell))
            matrix.check_cell(cell)
        incomplete = copy.deepcopy(self.ledger)
        custom = next(cell for cell in incomplete["cells"] if cell["version"] == "3.10.0" and cell["host"] == "custom-elements")
        custom["result"] = "incomplete"
        custom["proof"].pop("reactflow")
        assertion(custom, "reactflow_edit_save", False)
        with self.assertRaisesRegex(ValueError, "Required browser assertion failed"):
            matrix.check_matrix(incomplete)
        for mutate in (lambda c: c.pop(), lambda c: c.append(copy.deepcopy(c[0]))):
            with self.subTest(mutate=mutate):
                changed = copy.deepcopy(self.ledger)
                mutate(changed["cells"])
                with self.assertRaisesRegex(ValueError, "All 36 unique"):
                    matrix.check_matrix(changed)

    def test_complete_known_net10_receipt_is_accepted_but_failed_cell_metadata_is_not(self):
        base = next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == ("3.9.0", "net10.0", "wasm"))
        matrix.check_cell(base)
        for mutate in (lambda c: c.update(result="not_run"), lambda c: c.update(result="skipped"),
                       lambda c: c["assertions"].pop(), lambda c: c["assertions"].append(c["assertions"][0]),
                       lambda c: c["assertions"][0].update(passed=False)):
            changed = copy.deepcopy(base); mutate(changed)
            with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                matrix.check_cell(changed)

    def test_filtered_and_failed_runs_retain_all_unexecuted_cells_without_acceptance(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "receipt.json"
            chosen = self.ledger["cells"][0]
            matrix.run_matrix(lambda _: chosen, [matrix.identity(chosen)], output)
            actual = json.loads(output.read_text())
            self.assertTrue(actual["development_only"])
            self.assertFalse(actual["complete_matrix"])
            self.assertFalse(actual["passed"])
            self.assertEqual(35, sum(c["result"] == "not_run" for c in actual["cells"]))

    def test_private_fields_and_missing_assertions_never_enter_retained_receipts(self):
        chosen = self.ledger["cells"][0]
        for mutate in (lambda c: c.update(password="private"), lambda c: c["proof"].update(raw_error="private"), lambda c: c["assertions"].pop()):
            changed = copy.deepcopy(chosen)
            mutate(changed)
            with self.subTest(mutate=mutate), tempfile.TemporaryDirectory() as directory:
                output = Path(directory) / "receipt.json"
                with self.assertRaises(ValueError):
                    matrix.run_matrix(lambda _: changed, [matrix.identity(chosen)], output)
                self.assertNotIn("private", output.read_text())
                self.assertEqual("failed", next(c for c in json.loads(output.read_text())["cells"] if matrix.identity(c) == matrix.identity(chosen))["result"])

    def test_execution_identity_and_raw_transport_rejected_before_candidate_verification(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            inputs = root / "inputs"
            inputs.mkdir()
            for name in ("candidate.zip", "artifact.json", "producer-run.json", "live-retrieval.json"):
                (inputs / name).write_text("{}")
            alias = root / "alias"
            alias.symlink_to(inputs, target_is_directory=True)
            with patch.object(matrix.subprocess, "check_output", side_effect=lambda command, **_: "a" * 40 if command[1] == "rev-parse" else ""), patch.object(candidate, "verify_candidate_inputs") as verify:
                for source, run, attempt, transport in (("b" * 40, None, None, inputs), ("a" * 40, 1, None, inputs), ("a" * 40, None, None, alias)):
                    with self.subTest(source=source, run=run, transport=transport), self.assertRaises((ValueError, RuntimeError)):
                        matrix.prepare_candidate(transport, root / "extracted", root / "retained", fixture_source=source, fixture_run=run, fixture_attempt=attempt)
                verify.assert_not_called()

    def test_output_symlink_ancestor_rejected_and_system_alias_is_canonical(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            alias = root / "linked"
            alias.symlink_to(root, target_is_directory=True)
            with self.assertRaisesRegex(ValueError, "ancestor"):
                matrix._external_path(alias / "new-output")
            self.assertEqual(root.resolve() / "new-output", matrix._external_path(root / "new-output"))

    def test_private_export_destination_rejected_before_browser_start(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            existing = root / "existing.json"
            existing.write_text("untouched")
            alias = root / "alias"
            alias.symlink_to(root, target_is_directory=True)
            leaf = root / "leaf.json"
            leaf.symlink_to(existing)
            cell = {"version": "3.9.0", "framework": "net10.0", "host": "server"}
            for destination in (existing, leaf, alias / "new.json", root / "missing" / "new.json", matrix.JOURNEY.parent / "new.json"):
                with self.subTest(destination=destination), patch.object(matrix.subprocess, "run") as run, self.assertRaises(ValueError):
                    matrix.run_browser(self.handle, cell, [], released_document_output=destination)
                run.assert_not_called()
            self.assertEqual("untouched", existing.read_text())

    def test_private_export_hash_binds_download_without_promoting_document_assertion(self):
        cell = next(c for c in self.ledger["cells"] if matrix.identity(c) == ("3.9.0", "net10.0", "server"))
        cell = copy.deepcopy(cell)
        cell.update(result="incomplete")
        for assertion in cell["assertions"]:
            if assertion["name"] == "released_document":
                assertion["passed"] = False
        document = b'{"synthetic":true}'
        with tempfile.TemporaryDirectory() as directory:
            destination = Path(directory) / "workflow.json"
            def child(*_args, **kwargs):
                payload = json.loads(kwargs["input"])
                output = Path(payload["released_document_output"])
                with output.open("xb") as stream:
                    stream.write(document)
                output.chmod(0o600)
                return SimpleNamespace(stdout=json.dumps(cell), returncode=0)
            for digest in ("0" * 64, hashlib.sha256(document).hexdigest()):
                cell["proof"] = {"released_document_sha256": digest, "last_completed_stage": "released_document_exported"}
                with patch.object(matrix, "_run_browser_process", side_effect=child):
                    if digest == "0" * 64:
                        with self.assertRaisesRegex(ValueError, "bytes differ"):
                            matrix.run_browser(self.handle, cell, [], released_document_output=destination)
                    else:
                        receipt = matrix.run_browser(self.handle, cell, [], released_document_output=destination)
                        self.assertFalse(next(a for a in receipt["assertions"] if a["name"] == "released_document")["passed"])
                        self.assertNotIn(str(destination), json.dumps(receipt))
                destination.unlink()

    def test_private_export_rejects_oversize_permissions_and_missing_hash(self):
        cell = copy.deepcopy(next(c for c in self.ledger["cells"] if matrix.identity(c) == ("3.9.0", "net10.0", "server")))
        cell["result"] = "incomplete"
        with tempfile.TemporaryDirectory() as directory:
            destination = Path(directory) / "workflow.json"
            for size, mode, include_hash in ((1024 * 1024 + 1, 0o600, True), (2, 0o644, True), (2, 0o600, False)):
                document = b"x" * size
                cell["proof"] = {"released_document_sha256": hashlib.sha256(document).hexdigest()} if include_hash else {}
                def child(*_args, **_kwargs):
                    destination.write_bytes(document)
                    destination.chmod(mode)
                    return SimpleNamespace(stdout=json.dumps(cell), returncode=0)
                with self.subTest(size=size, mode=mode, include_hash=include_hash), patch.object(matrix, "_run_browser_process", side_effect=child), self.assertRaises(ValueError):
                    matrix.run_browser(self.handle, cell, [], released_document_output=destination)
                destination.unlink()


if __name__ == "__main__":
    unittest.main()
