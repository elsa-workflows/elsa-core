import copy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import consolidated_candidate_input as candidate

import run_paired_package_browser_matrix as matrix


class MatrixContracts(unittest.TestCase):
    def setUp(self):
        self.ledger = matrix.new_ledger()
        for cell in self.ledger["cells"]:
            cell.update(result="passed", browser_version="149.0.7827.55", resources=[], proof={}, failure_category=None, assertions=[{"name": name, "passed": True} for name in sorted(matrix.required_assertions(cell))])

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


if __name__ == "__main__":
    unittest.main()
