"""Regression coverage for malformed integration catalog records."""

from copy import deepcopy
import json
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


HERE = Path(__file__).resolve().parent


class CatalogValidationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.catalog = json.loads((HERE / "candidate-catalog.json").read_text())

    def assert_catalog_rejected(self, catalog, expected_message, optimized=False):
        with tempfile.TemporaryDirectory() as temporary_directory:
            directory = Path(temporary_directory)
            for filename in ("validate_catalog.py", "schema.json"):
                shutil.copy2(HERE / filename, directory / filename)
            (directory / "candidate-catalog.json").write_text(json.dumps(catalog))
            command = [sys.executable]
            if optimized:
                command.append("-O")
            command.append("validate_catalog.py")
            result = subprocess.run(
                command,
                cwd=directory,
                capture_output=True,
                text=True,
                check=False,
            )

        self.assertNotEqual(result.returncode, 0, result.stdout)
        self.assertIn(expected_message, result.stderr)

    def test_workflow_stages_must_be_contiguous_and_ordered(self):
        for name, steps in (
            ("duplicate", [1, 1, 3, 4]),
            ("skipped", [1, 3, 4, 5]),
            ("out of order", [2, 1, 3, 4]),
        ):
            for optimized in (False, True):
                with self.subTest(name=name, optimized=optimized):
                    catalog = deepcopy(self.catalog)
                    for stage, step in zip(catalog["workflowHypotheses"][0]["stages"], steps):
                        stage["step"] = step
                    self.assert_catalog_rejected(
                        catalog,
                        "workflow stage steps must be contiguous and ordered from 1",
                        optimized=optimized,
                    )

    def test_nested_contract_objects_reject_undeclared_fields(self):
        mutations = (
            ("provider recommendation", lambda c: c["providers"][0]["recommendation"]),
            ("workflow stage", lambda c: c["workflowHypotheses"][0]["stages"][0]),
            ("rubric entry", lambda c: c["rubric"][0]),
            ("hard gate", lambda c: c["hardGates"][0]),
            ("ranked cohort", lambda c: c["rankedRecommendation"]["cohorts"][0]),
        )
        for name, select_object in mutations:
            for optimized in (False, True):
                with self.subTest(name=name, optimized=optimized):
                    catalog = deepcopy(self.catalog)
                    select_object(catalog)["unexpectedField"] = "must be rejected"
                    self.assert_catalog_rejected(
                        catalog,
                        "Additional properties are not allowed",
                        optimized=optimized,
                    )


if __name__ == "__main__":
    unittest.main()
