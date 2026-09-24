import copy
import json
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from package_impact import InventoryGraph
from release_unit_manifest import (
    DEFAULT_UNIT_ID,
    MANIFEST_PATH,
    get_unit,
    load_manifest,
    require_tested_artifact_dependencies,
    validate_against_inventory,
)


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
INVENTORY_PATH = REPOSITORY_ROOT / "doc/integration-program/inventory/inventory.json"


class ReleaseUnitManifestTests(unittest.TestCase):
    def setUp(self):
        self.document = load_manifest(MANIFEST_PATH)
        self.unit = get_unit(self.document, DEFAULT_UNIT_ID)
        self.inventory = json.loads(INVENTORY_PATH.read_text(encoding="utf-8"))

    def test_slack_manifest_matches_inventory_and_per_package_policy(self):
        validate_against_inventory(self.unit, self.inventory)

        graph = InventoryGraph(self.inventory)
        source_project = (
            self.unit["source"]["repository"],
            self.unit["source"]["project_path"],
        )
        self.assertEqual({"Elsa.Slack"}, graph.package_ids([source_project]))
        self.assertEqual(
            {("elsa-extensions", self.unit["source"]["test_projects"][0]["project_path"])},
            graph.affected_tests([source_project]),
        )
        self.assertEqual("3.8.4", next(
            row["version"] for row in self.unit["tested_artifact_dependencies"] if row["package_id"] == "Elsa"
        ))
        self.assertEqual("3.8.0-preview.5557", next(
            row["version"]
            for row in next(
                row for row in self.inventory["project_inventory"]["elsa-extensions"]
                if row["path"] == self.unit["source"]["project_path"]
            )["package_references"]
            if row["id"] == "Elsa"
        ))
        self.assertFalse(self.unit["versioning"]["local_proof_is_release_allocation"])
        self.assertFalse(self.unit["versioning"]["local_proof_may_publish"])
        self.assertEqual("elsa-extensions", self.unit["publisher"]["repository"])

    def test_unknown_release_unit_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "Unknown release unit"):
            get_unit(self.document, "elsa-mqtt")

    def test_duplicate_package_owners_are_rejected(self):
        second_owner = copy.deepcopy(self.unit)
        second_owner["id"] = "duplicate-owner"
        duplicate_document = {"schema_version": 1, "release_units": [self.unit, second_owner]}

        with self.assertRaisesRegex(ValueError, "multiple release-unit owners"):
            self.load_document(duplicate_document)

    def test_unsafe_source_project_path_is_rejected(self):
        invalid = copy.deepcopy(self.document)
        invalid["release_units"][0]["source"]["project_path"] = "../Elsa.Slack.csproj"

        with self.assertRaisesRegex(ValueError, "safe relative .csproj path"):
            self.load_document(invalid)

    def test_inventory_target_framework_drift_is_rejected(self):
        invalid = copy.deepcopy(self.document)
        invalid["release_units"][0]["target_frameworks"].append("net11.0")
        unit = get_unit(self.load_document(invalid), DEFAULT_UNIT_ID)

        with self.assertRaisesRegex(ValueError, "target frameworks differ from inventory"):
            validate_against_inventory(unit, self.inventory)

    def test_non_string_dependency_scope_is_rejected_as_invalid_schema(self):
        invalid = copy.deepcopy(self.document)
        invalid["release_units"][0]["tested_artifact_dependencies"][0]["scope"] = []

        with self.assertRaisesRegex(ValueError, "scope is not supported"):
            self.load_document(invalid)

    def test_numeric_prerelease_identifiers_reject_leading_zeroes(self):
        invalid = copy.deepcopy(self.document)
        invalid["release_units"][0]["versioning"]["local_proof_version"] = "3.8.5-proof.01"

        with self.assertRaisesRegex(ValueError, "local_proof_version must be a SemVer proof prerelease"):
            self.load_document(invalid)

    def test_external_dependency_version_drift_is_rejected(self):
        invalid = copy.deepcopy(self.document)
        slacknet = next(
            row for row in invalid["release_units"][0]["tested_artifact_dependencies"]
            if row["package_id"] == "SlackNet"
        )
        slacknet["version"] = "0.17.6"
        unit = get_unit(self.load_document(invalid), DEFAULT_UNIT_ID)

        with self.assertRaisesRegex(ValueError, "External dependency version differs"):
            validate_against_inventory(unit, self.inventory)

    def test_package_proof_dependencies_are_required_with_a_domain_error(self):
        for package_id in ("Elsa", "SlackNet"):
            with self.subTest(package_id=package_id):
                unit = copy.deepcopy(self.unit)
                unit["tested_artifact_dependencies"] = [
                    row for row in unit["tested_artifact_dependencies"]
                    if row["package_id"].casefold() != package_id.casefold()
                ]

                with self.assertRaisesRegex(ValueError, f"must declare tested artifact dependencies: {package_id}"):
                    require_tested_artifact_dependencies(unit, ("Elsa", "SlackNet"))

    @staticmethod
    def load_document(document):
        with tempfile.TemporaryDirectory() as temporary_directory:
            path = Path(temporary_directory) / "release-units.json"
            path.write_text(json.dumps(document), encoding="utf-8")
            return load_manifest(path)


if __name__ == "__main__":
    unittest.main()
