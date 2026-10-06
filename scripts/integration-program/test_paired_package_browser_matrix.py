import copy
import hashlib
import json
from pathlib import Path
import tempfile
import subprocess
import unittest
from types import SimpleNamespace
from unittest.mock import patch

import consolidated_candidate_input as candidate
import paired_package_released_documents as documents
from test_paired_package_released_documents import fixture_identity, write_released_fixture

import run_paired_package_browser_matrix as matrix


def reopen_row(version, framework="net10.0", host="server"):
    return {"source_cell": {"version": version, "framework": framework, "host": host},
            **{name: "a" * 64 for name in ("source_binding_sha256", "document_sha256", "definition_id_sha256", "root_id_sha256", "activity_id_sha256", "value_sha256", "instance_id_sha256")},
            "tool_version": documents.TOOL_VERSIONS[version], "checks": {name: True for name in matrix.REOPEN_CHECKS}}


class MatrixContracts(unittest.TestCase):
    def setUp(self):
        self.ledger = matrix.new_ledger()
        self.handle = SimpleNamespace(studio_url="http://localhost:1", backend_url="http://localhost:2/elsa/api", username="private", password="private", safe_ids={})
        for cell in self.ledger["cells"]:
            cell.update(result="passed", browser_version="149.0.7827.55", resources=[], proof={}, failure_category=None, assertions=[{"name": name, "passed": True} for name in sorted(matrix.required_assertions(cell))])
            if cell["version"] == "3.10.0":
                cell["proof"]["baseline_reopens"] = [reopen_row(version, cell["framework"], cell["host"]) for version in documents.TOOL_VERSIONS]

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

    def test_node_independently_checks_bytes_and_actual_graph_semantics(self):
        with tempfile.TemporaryDirectory() as directory:
            inputs = matrix._released_inputs(("3.10.0", "net10.0", "server"), self.released_inputs(Path(directory)))
            result = subprocess.run(["npm", "exec", "--no", "--", "tsx", "native-import.contract.ts"],
                                    cwd=matrix.JOURNEY.parent, input=json.dumps(inputs), capture_output=True, text=True, timeout=60)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual("native import contracts passed\n", result.stdout)

    def test_returned_reopen_proof_is_crossbound_to_each_supplied_source(self):
        key = ("3.10.0", "net10.0", "server")
        with tempfile.TemporaryDirectory() as directory:
            inputs = self.released_inputs(Path(directory))
            payload = matrix._released_inputs(key, inputs)
            record = copy.deepcopy(next(cell for cell in self.ledger["cells"] if matrix.identity(cell) == key))
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
                with self.subTest(field=field), patch.object(matrix.subprocess, "run", return_value=SimpleNamespace(stdout=json.dumps(changed), returncode=0)):
                    if field:
                        with self.assertRaisesRegex(ValueError, "supplied bytes/source"):
                            matrix.run_browser(self.handle, dict(zip(("version", "framework", "host"), key)), [], released_document_inputs=inputs)
                    else:
                        self.assertEqual(record, matrix.run_browser(self.handle, dict(zip(("version", "framework", "host"), key)), [], released_document_inputs=inputs))

    def test_exact_complete_matrix_and_required_assertions(self):
        matrix.check_matrix(self.ledger)
        self.assertEqual(36, len(self.ledger["cells"]))
        for mutate in (lambda c: c.pop(), lambda c: c.append(copy.deepcopy(c[0])),
                       lambda c: c[0].update(result="not_run"), lambda c: c[0].update(result="skipped"),
                       lambda c: c[0]["assertions"].pop(), lambda c: c[0]["assertions"].append(c[0]["assertions"][0]),
                       lambda c: c[0]["assertions"][0].update(passed=False)):
            with self.subTest(mutate=mutate):
                changed = copy.deepcopy(self.ledger)
                mutate(changed["cells"])
                with self.assertRaises(ValueError):
                    matrix.check_matrix(changed)

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
                with patch.object(matrix.subprocess, "run", side_effect=child):
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
                with self.subTest(size=size, mode=mode, include_hash=include_hash), patch.object(matrix.subprocess, "run", side_effect=child), self.assertRaises(ValueError):
                    matrix.run_browser(self.handle, cell, [], released_document_output=destination)
                destination.unlink()


if __name__ == "__main__":
    unittest.main()
