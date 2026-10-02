import subprocess
import tempfile
import unittest
from pathlib import Path

from run_current_import_affected_tests import assert_clean_source, classify, selected_nodes


class CurrentImportAffectedTestExecutionTests(unittest.TestCase):
    def test_skipped_test_keeps_correctness_closure_incomplete(self):
        counters = {"total": 2, "executed": 1, "passed": 1, "failed": 0, "error": 0}
        results = [{"name": "PassingTest", "outcome": "Passed"},
                   {"name": "ClusteredTest", "outcome": "NotExecuted"}]

        self.assertEqual("incomplete", classify(0, counters, results))
        self.assertEqual("failed", classify(0, None, results))

    def test_failed_test_or_nonzero_process_cannot_pass(self):
        counters = {"total": 1, "executed": 1, "passed": 1, "failed": 0, "error": 0}
        results = [{"name": "PassingTest", "outcome": "Passed"}]

        self.assertEqual("failed", classify(1, counters, results))
        self.assertEqual("failed", classify(0, counters | {"failed": 1}, results))
        self.assertEqual("failed", classify(0, counters | {"timeout": 1}, results))
        self.assertEqual("incomplete", classify(0, counters | {"total": 0, "executed": 0, "passed": 0}, []))
        self.assertEqual("passed", classify(0, counters, results))

    def test_unexpected_or_inconsistent_trx_results_cannot_pass(self):
        counters = {"total": 1, "executed": 1, "passed": 1, "failed": 0, "error": 0}

        self.assertEqual("failed", classify(0, counters, [{"outcome": "Inconclusive"}]))
        self.assertEqual("failed", classify(0, counters, [{"outcome": "Failed"}]))
        self.assertEqual("failed", classify(0, counters | {"executed": 0}, [{"outcome": "Passed"}]))
        self.assertEqual("failed", classify(0, counters | {"passed": 0}, [{"outcome": "Passed"}]))
        self.assertEqual("failed", classify(0, counters | {"total": 2}, [{"outcome": "Passed"}]))
        self.assertEqual("failed", classify(0, counters | {"notExecuted": 1}, [{"outcome": "Passed"}]))
        self.assertEqual("failed", classify(0, counters | {"notExecuted": -1}, [{"outcome": "Passed"}]))

    def test_selection_must_match_exact_source_and_contain_runnable_paths(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "Selected.csproj").write_text("<Project />", encoding="utf-8")
            selection = {
                "sourceRevision": "expected",
                "acceptanceEligible": True,
                "scenarios": {
                    "testExecutionPerformed": False,
                    "sharedCore": {"tests": [{"project": "Selected.csproj", "framework": "net10.0"}]},
                },
            }

            self.assertEqual(1, len(selected_nodes(selection, root, "expected")))
            with self.assertRaisesRegex(ValueError, "exact source revision"):
                selected_nodes(selection, root, "different")
            selection["scenarios"]["sharedCore"]["tests"].append(
                {"project": "../escape.csproj", "framework": "net10.0"}
            )
            with self.assertRaisesRegex(ValueError, "missing or outside"):
                selected_nodes(selection, root, "expected")

    def test_source_guard_rejects_wrong_revision_and_dirty_tree(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            subprocess.run(["git", "init", "-q", str(root)], check=True)
            source = root / "Selected.csproj"
            source.write_text("<Project />", encoding="utf-8")
            subprocess.run(["git", "-C", str(root), "add", "Selected.csproj"], check=True)
            subprocess.run(["git", "-C", str(root), "-c", "user.name=Test",
                            "-c", "user.email=test@example.invalid", "commit", "-qm", "fixture"], check=True)
            head = subprocess.check_output(["git", "-C", str(root), "rev-parse", "HEAD"], text=True).strip()

            assert_clean_source(root, head)
            with self.assertRaisesRegex(ValueError, "Source revision changed"):
                assert_clean_source(root, "wrong")
            source.write_text("<Project Sdk=\"Microsoft.NET.Sdk\" />", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "not clean"):
                assert_clean_source(root, head)


if __name__ == "__main__":
    unittest.main()
